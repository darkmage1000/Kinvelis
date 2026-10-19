using UnityEngine;

namespace Wildbond
{
    public enum CreatureMode { Wild, Companion, Worker }

    public enum AIState { Idle, Wander, Flee, Alert, Chase, Attack, Recover, Return, Follow, ToWork, Working, ToEat, Eating, Rest, Haul, Captured, Dead, Defend }

    /// <summary>
    /// Runtime body of a kin in the world (wild, companion or base worker). Pooled.
    /// Movement is kinematic on the analytic heightfield. AI lives in partial files by mode.
    /// </summary>
    public partial class CreatureActor : MonoBehaviour, ICombatant
    {
        public CreatureData Data { get; private set; }
        public SpeciesDef Sp { get; private set; }
        public CreatureMode Mode { get; private set; }
        public CreatureRig Rig { get; private set; }
        public AIState State { get; private set; }
        public string PoolKey { get; private set; }

        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public StatusState Status;

        public Vector3 Home;
        public bool Aggro;
        public ICombatant Target;
        public float DistToPlayer;
        /// <summary>Already counted as "seen" in the Kin Dex this spawn.</summary>
        public bool DexSeen;
        /// <summary>Part of a camp raid: never leashes, hunts camp workers.</summary>
        public bool Raider { get; private set; }

        /// <summary>Cave inhabitants: never distance-despawned by the spawner.</summary>
        public bool Persistent { get; set; }

        /// <summary>World event herds: run to a far point, then live there.</summary>
        public void Migrate(Vector3 dest)
        {
            Home = dest;
            Aggro = false;
            Target = null;
            SetState(AIState.Return);
        }

        /// <summary>Start fighting a target right away (cave guardian waking up).</summary>
        public void Provoke(ICombatant target)
        {
            Aggro = true;
            Target = target;
            SetState(AIState.Chase);
        }

        /// <summary>A rival tamer's kin in a match: can't be caught, drops nothing, never flees or gives up the chase.</summary>
        public bool Duelist { get; private set; }

        public void MakeDuelist(Vector3 ring, ICombatant target)
        {
            Duelist = true;
            Persistent = true;
            Home = ring;
            Aggro = true;
            Target = target;
            SetState(AIState.Chase);
        }

        /// <summary>A Warden: a duelist with a deep health pool that Wardens.cs drives through its phases.</summary>
        public bool Boss { get; private set; }
        /// <summary>Hits bounce off (phase changes, shields).</summary>
        public bool Invulnerable;
        public float BossAtkMul = 1f;
        float bossHpMul = 1f;

        public void MakeBoss(Vector3 arena, ICombatant target, float hpMul)
        {
            MakeDuelist(arena, target);
            Boss = true;
            bossHpMul = hpMul;
            MaxHp = Data.MaxHp * hpMul;
            Hp = MaxHp;
        }

        public void BossHeal(float v) { if (IsAlive) Hp = Mathf.Min(MaxHp, Hp + v); }

        /// <summary>A hunted legend: a much deeper health pool, but still an ordinary wild kin that can be caught.</summary>
        public void Toughen(float hpMul)
        {
            bossHpMul = hpMul;
            MaxHp = Data.MaxHp * hpMul;
            Hp = MaxHp;
        }

        public void MakeRaider(Vector3 camp, ICombatant target)
        {
            Raider = true;
            Home = camp;
            Hp = MaxHp * 0.7f;   // worn out from the march
            Aggro = true;
            Target = target;
            SetState(AIState.Chase);
        }
        public bool Unaware => (Mode == CreatureMode.Wild) && !Aggro && State != AIState.Flee && State != AIState.Alert;
        public bool Engaged => Aggro && Target != null;

        float stateTimer;
        float yaw;
        Vector3 vel;
        Vector3 knock;
        Vector3 moveTarget;
        float moveSpeed;
        bool moving;

        readonly float[] cooldowns = new float[4];
        AbilityDef[] abilities = new AbilityDef[0];
        int pendingAbility = -1;
        // Blink skills: the kin it lands behind reels for a moment, and the user's next hit is a certain crit
        public const float BlinkStun = 0.9f, BlinkStunPlayer = 0.55f, CritWindow = 6f;
        float stunTimer, critUntil, fxTick;
        public bool Stunned => stunTimer > 0f;
        public bool CritPrimed => Time.time < critUntil;
        /// <summary>Uses up a primed crit (CombatSystem.Compute asks once per hit).</summary>
        public bool TakeCrit() { if (!CritPrimed) return false; critUntil = 0f; return true; }

        /// <summary>Knocked reeling: no moving or attacking, and whatever it was winding up is lost. Wardens and alphas shrug off half of it.</summary>
        /// <summary>Ends a stun early (a kin thawed out of a Warden's ice).</summary>
        public void ClearStun() => stunTimer = 0f;

        public void Stun(float seconds)
        {
            if (!IsAlive || Invulnerable) return;
            if (Boss || Data.alpha) seconds *= 0.5f;
            stunTimer = Mathf.Max(stunTimer, seconds);
            pendingAbility = -1;
            CancelActions();
            HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 0.8f), "STUNNED", new Color(1f, 0.92f, 0.4f), 0.8f);
        }
        float windup;
        ICombatant pendingTarget;
        float dashTime;
        Vector3 dashVel;
        bool dashHit;
        float deathTimer;
        float atk, def, speedMul;
        float animSpeed;

        public float BodyRadius { get; private set; }

        /// <summary>Being ridden: AI off, pose driven by the rider, gait animated from rider speed.</summary>
        public bool Mounted { get; private set; }
        float mountAnimSpeed;

        public void SetMounted(bool on)
        {
            Mounted = on;
            pendingAbility = -1;
            stunTimer = 0f; critUntil = 0f;
            ResetEffects();
            dashTime = 0f;
            vel = Vector3.zero;
            knock = Vector3.zero;
            Target = null;
            SetState(AIState.Follow);
        }

        public void SetMountPose(Vector3 pos, float yawDeg, float speed)
        {
            transform.position = pos;
            yaw = yawDeg;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            mountAnimSpeed = speed;
        }
        public Vector3 Velocity => vel;

        // ================================================================ ICombatant
        public Vector3 Position => transform.position;
        public float Radius => BodyRadius;
        public bool IsAlive => Hp > 0f && State != AIState.Dead && State != AIState.Captured && gameObject.activeInHierarchy;
        public Team Team => Mode == CreatureMode.Wild ? Team.Wild : Team.Player;
        public int Level => Data.level;
        public float AttackStat => atk * (Mode == CreatureMode.Companion && Sp.perk == CompanionPerk.Vanguard && Target != null && Target == (ICombatant)GameManager.I.Player.CombatTarget ? 1.2f : 1f) * KinbondMul * HomeGround * BossAtkMul;
        float KinbondMul => Mode == CreatureMode.Companion ? (GameManager.I.Player.Stats.HasEffect(UniqueEffect.Kinbond) ? 1.2f : 1f) * Data.BondMul : 1f;   // Kinbond Ring + bond level

        /// <summary>Who last hurt this kin (companion Defensive stance retaliates).</summary>
        public ICombatant LastAttacker { get; private set; }
        public float LastAttackedTime { get; private set; } = -99f;
        public float DefenseStat => def * Status.DefMul * KinbondMul * HomeGround;
        /// <summary>Camp workers fight harder defending their home.</summary>
        float HomeGround => Mode == CreatureMode.Worker ? 1.2f : 1f;
        public Element Element1 => Data.Element1;
        public Element Element2 => Data.Element2;

        // ================================================================ LIFECYCLE
        /// <summary>Called once when the pooled object is first created.</summary>
        public void Build(CreatureData d)
        {
            PoolKey = d.ModelKey;
            Rig = ModelBuilder.Build(d, transform, RenderAssets.Creature);
        }

        public void Spawn(CreatureData d, CreatureMode mode, Vector3 pos)
        {
            Data = d;
            Sp = d.Species;
            Mode = mode;
            DexSeen = false;
            HideSkull();
            skullCheck = 0f;
            Raider = false;
            Duelist = false;
            Boss = false; Invulnerable = false; BossAtkMul = 1f; bossHpMul = 1f;
            Persistent = false;
            ResetFleeStamina();
            RecomputeStats();
            Hp = d.hp < 0f ? MaxHp : Mathf.Min(d.hp, MaxHp);
            if (mode != CreatureMode.Wild && Hp <= 0f) Hp = MaxHp * 0.25f;
            Status = default;
            Aggro = false;
            Target = null;
            pendingAbility = -1;
            stunTimer = 0f; critUntil = 0f;
            ResetEffects();
            dashTime = 0f;
            knock = Vector3.zero;
            vel = Vector3.zero;
            moving = false;
            deathTimer = 0f;
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Random.Range(0.5f, 2f);
            RefreshAbilities();
            EndMood(); greetedArrival = Time.time; sleeping = false; bedSet = false; ignoreObstacles = false; if (Rig != null) Rig.Lift = 0f;   // camp life starts clean (a kin that has only just appeared doesn't re-greet)
            ModelBuilder.ApplyLook(Rig, d, RenderAssets.Creature);   // a pooled model is this kin's now
            ApplyWorldSize();
            Home = pos;
            pos.y = GroundY(pos);
            transform.position = pos;
            yaw = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            gameObject.SetActive(true);
            Rig.SetVisible(true);
            Mounted = false;
            SetState(mode == CreatureMode.Wild ? AIState.Idle : (mode == CreatureMode.Companion ? AIState.Follow : AIState.Idle), Random.Range(0.5f, 2f));
            OnSpawnMode();
            CreatureRegistry.Add(this);
        }

        /// <summary>The Toy Box's kin-size rule: scales the whole kin (and how much room it takes up). Safe to call any time.</summary>
        public void ApplyWorldSize()
        {
            if (Data == null || Sp == null) return;
            float k = Mathf.Clamp(WorldRules.Current.kinSize, 0.2f, 4f);
            transform.localScale = Vector3.one * k;
            BodyRadius = 0.45f * Sp.size * Data.SizeScale * k;
        }

        int abilitySlots = -1;

        /// <summary>Rebuilds the usable skill list from the level-gated learnset (cooldowns per slot are kept).</summary>
        void RefreshAbilities()
        {
            var known = Data.KnownSkills;
            abilities = new AbilityDef[known.Length];
            for (int i = 0; i < abilities.Length; i++) abilities[i] = ContentDB.Ability(known[i]);
            abilitySlots = Data.SkillSlots;
        }

        public int SkillCount => abilities.Length;
        public AbilityDef Skill(int i) => i >= 0 && i < abilities.Length ? abilities[i] : null;
        public float SkillCooldownLeft(int i) => i >= 0 && i < abilities.Length ? Mathf.Max(0f, cooldowns[i]) : 0f;

        public void RecomputeStats()
        {
            if (Data != null && abilitySlots >= 0 && abilitySlots != Data.SkillSlots) RefreshAbilities();
            MaxHp = Data.MaxHp * bossHpMul;
            atk = Data.Atk;
            def = Data.Def;
            speedMul = Data.SpeedMul;
        }

        public void Despawn()
        {
            CreatureRegistry.Remove(this);
            OnDespawnMode();
            if (Mode != CreatureMode.Wild && Data != null) Data.hp = Hp >= MaxHp ? -1f : Mathf.Max(0f, Hp);
            gameObject.SetActive(false);
            CreatureSpawner.I?.ReturnToPool(this);
        }

        partial void OnSpawnMode();
        partial void OnDespawnMode();

        void SetState(AIState s, float timer = 0f)
        {
            State = s;
            stateTimer = timer;
        }

        // ================================================================ TICK
        float shimmerTimer, shimmerPhase;

        /// <summary>Shimmer kin sparkle. Each of the three coats has its own effect: rising glitter, orbiting twinkles, or a rippling pulse.</summary>
        void TickShimmer(float dt)
        {
            shimmerPhase += dt;
            shimmerTimer -= dt;
            if (shimmerTimer > 0f) return;
            float r = BodyRadius;
            float big = Mathf.Clamp(r / 0.3f, 1f, 2.4f);   // larger kin throw larger sparks
            Vector3 c = transform.position + Vector3.up * (r * 1.2f + Rig.HoverHeight);
            Color glow = Data.Palette.glow;
            switch (Data.shimmerVariant)
            {
                case 1:
                    // two twinkles circle the body and leave a ring of light
                    shimmerTimer = 0.05f;
                    for (int k = 0; k < 2; k++)
                    {
                        float a = shimmerPhase * 3.2f + k * Mathf.PI;
                        var p = c + new Vector3(Mathf.Cos(a), Mathf.Sin(shimmerPhase * 1.3f + k) * 0.3f, Mathf.Sin(a)) * (r * 1.5f);
                        Fx.Mote(p, Vector3.zero, k == 0 ? glow : Color.white, 0.16f * big, 0.5f, true);
                    }
                    break;
                case 2:
                    // a soft ring ripples out from its feet with a few stars
                    shimmerTimer = 1.3f;
                    Fx.Ring(transform.position + Vector3.up * 0.05f, glow, r * 2.4f, 16);
                    Fx.Sparkle(c, Color.white, 6, r * 1.2f);
                    Fx.Sparkle(c, glow, 4, r * 1.2f);
                    break;
                default:
                    // rising glitter with a bright glint now and then
                    shimmerTimer = 0.1f;
                    Fx.Mote(c + Random.insideUnitSphere * (r * 1.3f), Vector3.up * Random.Range(0.5f, 1.1f), Random.value < 0.3f ? Color.white : glow,
                        Random.Range(0.1f, 0.2f) * big, 0.8f, true);
                    if (Random.value < 0.08f) Fx.Sparkle(c, glow, 8, r * 1.2f);
                    break;
            }
        }

        float auraTimer;

        /// <summary>Species ambience: a trickle of particles in the kin's glow color (only while it is being fully simulated).</summary>
        void TickAura(float dt)
        {
            auraTimer -= dt;
            if (auraTimer > 0f) return;
            float r = BodyRadius;
            Vector3 c = transform.position + Vector3.up * (r * 1.3f + Rig.HoverHeight);
            Vector3 o = Random.insideUnitSphere * r;
            Color glow = Data.Palette.glow;
            switch (Sp.aura)
            {
                case AuraFx.Embers:
                    auraTimer = 0.16f;
                    Fx.Mote(c + o, new Vector3(o.x, Random.Range(0.8f, 1.6f), o.z), glow, 0.12f, 0.7f, true);
                    break;
                case AuraFx.Sparks:
                    auraTimer = 0.28f;
                    Fx.Mote(c + o, Random.onUnitSphere * 2.2f, glow, 0.1f, 0.22f, true);
                    Fx.Mote(c + o, Random.onUnitSphere * 1.6f, Color.white, 0.07f, 0.18f, true);
                    break;
                case AuraFx.Snow:
                    auraTimer = 0.2f;
                    Fx.Mote(c + new Vector3(o.x * 1.6f, r * 1.4f, o.z * 1.6f), new Vector3(0.2f, -0.7f, 0.1f), new Color(0.92f, 0.97f, 1f, 0.9f), 0.09f, 1.1f, false);
                    break;
                case AuraFx.Petals:
                    auraTimer = 0.3f;
                    Fx.Mote(c + new Vector3(o.x * 1.5f, r * 0.9f, o.z * 1.5f), new Vector3(Random.Range(-0.4f, 0.4f), -0.45f, Random.Range(-0.4f, 0.4f)), Data.Palette.secondary, 0.12f, 1.3f, false);
                    break;
                case AuraFx.Bubbles:
                    auraTimer = 0.35f;
                    Fx.Mote(c + o, new Vector3(o.x * 0.3f, Random.Range(0.4f, 0.8f), o.z * 0.3f), new Color(glow.r, glow.g, glow.b, 0.6f), Random.Range(0.1f, 0.2f), 1.1f, false);
                    break;
                case AuraFx.Gloom:
                    auraTimer = 0.2f;
                    Fx.Mote(c + o * 0.8f - Vector3.up * (r * 0.6f), new Vector3(o.x * 0.4f, Random.Range(0.3f, 0.7f), o.z * 0.4f),
                        new Color(glow.r * 0.35f, glow.g * 0.3f, glow.b * 0.45f, 0.55f), Random.Range(0.25f, 0.4f) * Mathf.Max(0.6f, Sp.size), 0.9f, false);
                    break;
                case AuraFx.Glitter:
                    auraTimer = 0.25f;
                    Fx.Mote(c + o * 1.6f, Vector3.up * 0.15f, glow, Random.Range(0.06f, 0.14f), 0.6f, true);
                    break;
                case AuraFx.Breeze:
                {
                    auraTimer = 0.22f;
                    Vector3 tan = new Vector3(-o.z, 0f, o.x).normalized;
                    Fx.Mote(c + o * 1.5f, tan * 1.8f + Vector3.up * 0.2f, new Color(0.9f, 1f, 0.95f, 0.5f), 0.1f, 0.5f, false);
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- danger skull
        Transform skull;
        bool skullOn;
        float skullCheck;
        static bool skullHintShown;

        void HideSkull()
        {
            if (!skullOn) return;
            skullOn = false;
            if (skull != null) skull.gameObject.SetActive(false);
        }

        /// <summary>Red skull over wild kin 10+ levels above the player. Bobs above the head and always faces the camera.</summary>
        void TickSkull(float dt)
        {
            skullCheck -= dt;
            if (skullCheck <= 0f)
            {
                skullCheck = 0.4f;
                bool want = Mode == CreatureMode.Wild && DistToPlayer < Mathf.Min(DangerSkull.ShowRange, QualityProfile.Current.fogEnd * 0.85f) && DangerSkull.IsDanger(Data);
                if (want && !skullOn)
                {
                    if (skull == null) skull = DangerSkull.Create(transform);
                    skull.gameObject.SetActive(true);
                    skullOn = true;
                    if (!skullHintShown && DistToPlayer < 40f)
                    {
                        skullHintShown = true;
                        GameEvents.Warn($"A red skull marks kin {DangerSkull.LevelGap}+ levels above you. Back off - this one is Lv {Data.level}.");
                    }
                }
                else if (!want) HideSkull();
            }
            if (!skullOn) return;
            float s = Mathf.Clamp(0.4f + BodyRadius * 0.55f, 0.5f, 1.1f);
            float top = DangerSkull.ModelTop(Sp) * Sp.size * Data.SizeScale + Rig.HoverHeight;
            Vector3 pos = transform.position + Vector3.up * (top + s * 0.75f + Mathf.Sin(Time.time * 2.4f) * 0.08f);
            skull.position = pos;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 to = cam.transform.position - pos; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) skull.rotation = Quaternion.LookRotation(to);
            }
            skull.localScale = Vector3.one * (s * (1f + Mathf.Sin(Time.time * 4.5f) * 0.05f));
        }

        public void Tick(float dt, bool full)
        {
            if (State == AIState.Dead || State == AIState.Captured) HideSkull();
            else TickSkull(dt);
            if (State == AIState.Dead)
            {
                deathTimer += dt;
                float k = 1f - deathTimer / 0.8f;
                if (Rig.Model != null) Rig.Model.localScale *= Mathf.Max(0.01f, 1f - dt * 3f);
                if (k <= 0f) Despawn();
                return;
            }
            if (State == AIState.Captured) return;

            // status effects
            if (Status.Active)
            {
                Status.time -= dt;
                if (Status.type == StatusType.Burn || Status.type == StatusType.Poison)
                {
                    Status.tick -= dt;
                    if (Status.tick <= 0f)
                    {
                        Status.tick = 1f;
                        Hp -= MaxHp * (Status.type == StatusType.Burn ? 0.03f : 0.022f);   // poison bites softer but lasts longer
                        if (full) Fx.Burst(transform.position + Vector3.up * BodyRadius, StatusState.Color(Status.type), 3, 1.5f, 0.2f, 0.4f, true);
                        if (Hp <= 0f) { Die(null); return; }
                    }
                }
                if (Status.time <= 0f) Status = default;
            }

            if (full && Sp.aura != AuraFx.None) TickAura(dt);
            if (full && Sp.rarity == Rarity.Legendary) TickOrbs(dt);
            if (full && Data.shimmer) TickShimmer(dt);

            if (Mounted)
            {
                // ridden: no thinking of its own, but its skills recharge and go off when you call them (MountedSkill)
                for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] -= dt;
                if (pendingAbility >= 0) TickMountedAbility(dt);
                if (full) Rig.Animate(dt, mountAnimSpeed, false, false);
                return;
            }

            // an Anthem playing nearby hurries every skill along (but not the Anthem itself)
            float cdRate = AnthemMul(Team, transform.position);
            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] -= dt * (cdRate > 1f && i < abilities.Length && abilities[i] != null && abilities[i].effect == AbilityEffect.Anthem ? 1f : cdRate);
            stateTimer -= dt;

            if (stunTimer > 0f) stunTimer -= dt;
            // busy = planted behind a shield, braced for a riposte or in the middle of a flurry: no new decisions
            bool stunned = stunTimer > 0f || busyTimer > 0f || (Status.Active && Status.type == StatusType.Shock);
            if (full && (stunTimer > 0f || CritPrimed))
            {
                // stars round a stunned head; a red glint on a kin whose next hit will crit
                fxTick -= dt;
                if (fxTick <= 0f)
                {
                    fxTick = 0.16f;
                    if (stunTimer > 0f) Fx.Sparkle(transform.position + Vector3.up * (BodyRadius * 2f + 0.35f), new Color(1f, 0.92f, 0.4f), 2, 0.3f);
                    else Fx.Sparkle(transform.position + Vector3.up * BodyRadius, new Color(1f, 0.3f, 0.25f), 2, BodyRadius + 0.25f);
                }
            }
            moving = false;
            TickEffects(dt, full);
            if (!stunned)
            {
                if (pendingAbility >= 0) TickAbility(dt);
                else
                {
                    switch (Mode)
                    {
                        case CreatureMode.Wild: ThinkWild(dt); break;
                        case CreatureMode.Companion: ThinkCompanion(dt); break;
                        case CreatureMode.Worker: ThinkWorker(dt); break;
                    }
                }
            }

            Integrate(dt, full);
            if (full) Rig.Animate(dt, animSpeed, State == AIState.Working, State == AIState.Rest || (Status.Active && Status.type == StatusType.Drowsy));
        }

        // ================================================================ MOVEMENT
        float GroundY(Vector3 p)
        {
            float h = Ground.HeightNear(p.x, p.z, p.y);
            float sea = Tides.Surface(p.x, p.z);
            if (h < sea - 0.4f) h = sea - 0.4f; // float on water
            return h;
        }

        protected void MoveTo(Vector3 target, float speed)
        {
            moveTarget = target;
            moveSpeed = speed;
            moving = true;
        }

        float WalkSpeed => Sp.walkSpeed * speedMul * Status.SpeedMul;
        float RunSpeed => Sp.runSpeed * speedMul * Status.SpeedMul;

        void Integrate(float dt, bool full)
        {
            Vector3 pos = transform.position;
            Vector3 desired = Vector3.zero;
            bool dashing = dashTime > 0f;
            if (dashing)
            {
                dashTime -= dt;
                desired = dashVel;
            }
            else if (moving)
            {
                Vector3 aim = moveTarget;
                Vector3 left = moveTarget - pos; left.y = 0f;
                if (full && !ignoreObstacles)
                {
                    // up and down storeys and stairs (Housing.Route); wild kin also take the house's way round its walls
                    var house = Housing.I;
                    bool upstairs = house != null && house.LevelAt(pos) > 0;
                    if (house != null && (upstairs || house.LevelAt(moveTarget) > 0 || Mode == CreatureMode.Wild))
                        aim = house.Steer(route ?? (route = new Housing.Route()), pos, moveTarget, BodyRadius * 0.8f);
                    // on the ground, camp kin and your companion follow a real path round everything (KinNav); anyone
                    // without one (wild kin, a walker waiting for a search, no way found) steers round what is in front
                    bool pathed = !upstairs && Mode != CreatureMode.Wild && KinNav.Steer(nav ?? (nav = new KinNav.Route()), pos, aim, BodyRadius * 0.8f, out aim);
                    if (!pathed && !upstairs) aim = AvoidObstacles(pos, aim, left.magnitude, dt);
                }
                Vector3 to = aim - pos; to.y = 0f;
                float d = to.magnitude;
                if (d > 0.15f) desired = to / d * Mathf.Min(moveSpeed, left.magnitude / Mathf.Max(dt, 0.001f));
            }
            // dashes move at full dash speed immediately (accelerating into them made every dash fall short)
            vel = dashing ? desired : Vector3.MoveTowards(vel, desired, dt * 18f);
            knock = Vector3.MoveTowards(knock, Vector3.zero, dt * 20f);
            Vector3 step = (vel + knock) * dt;
            Vector3 next = pos + step;

            // don't wander into deep water unless it's a water kin
            if (Mode == CreatureMode.Wild && !Sp.nearWater && Sp.locomotion != Locomotion.Flyer && Ground.Height(next.x, next.z) < -0.6f)
            {
                next = pos;
                vel = Vector3.zero;
                if (State == AIState.Wander) SetState(AIState.Idle, 1f);
            }
            if (Time.time < tetherUntil) ClampTether(ref next);
            if (full && !ignoreObstacles) WorldCollision.PushOut(ref next, BodyRadius * 0.8f);
            else if (full) Housing.PushOut(ref next, BodyRadius * 0.8f);   // a kin climbing onto its nest walks into it (house walls still stop it)
            next.x = Mathf.Clamp(next.x, -WorldGen.HalfWorld + 10f, WorldGen.HalfWorld - 10f);
            next.z = Mathf.Clamp(next.z, -WorldGen.HalfWorld + 10f, WorldGen.HalfWorld - 10f);
            next.y = Mathf.Lerp(pos.y, GroundY(next), full ? Mathf.Min(1f, dt * 14f) : 1f);
            transform.position = next;

            Vector3 flat = vel; flat.y = 0f;
            animSpeed = flat.magnitude;
            Vector3 face = Vector3.zero;
            if (pendingAbility >= 0 && pendingTarget != null) face = pendingTarget.Position - next;
            else if (animSpeed > 0.3f) face = flat;
            else if ((State == AIState.Alert || State == AIState.Working || faceAnyState) && faceTarget.sqrMagnitude > 0.01f) face = faceTarget - next;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
            {
                float targetYaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
                yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, dt * 540f);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
        }

        Vector3 faceTarget;
        Housing.Route route;
        KinNav.Route nav;

        // ---- steering round round things (structures, trees, rocks, furniture), and stepping aside when stuck
        Vector3 avoidAim, avoidC, stuckFrom, detour;
        float avoidT, stuckT, detourUntil;
        int avoidSide;

        /// <summary>
        /// Where to head instead of `aim` so as not to walk into the first obstacle in the way: past its near side, keeping
        /// to the same side while going round it. If a kin with somewhere to go has barely moved for 1.2 s anyway (a pocket
        /// between two structures), it steps aside for a moment.
        /// </summary>
        Vector3 AvoidObstacles(Vector3 pos, Vector3 aim, float leftToGo, float dt)
        {
            stuckT += dt;
            if (stuckT >= 1.2f)
            {
                Vector3 moved = pos - stuckFrom; moved.y = 0f;
                if (leftToGo > 1.2f && moved.magnitude < 0.35f && Time.time > detourUntil)
                {
                    Vector3 f = aim - pos; f.y = 0f; f = f.sqrMagnitude > 0.01f ? f.normalized : transform.forward;
                    Vector3 n = new Vector3(-f.z, 0f, f.x) * (Random.value < 0.5f ? -1f : 1f);
                    detour = pos + n * 2.2f - f * 0.6f;
                    detourUntil = Time.time + 0.9f;
                }
                stuckT = 0f; stuckFrom = pos;
            }
            if (Time.time < detourUntil) return detour;
            Vector3 to = aim - pos; to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.5f) return aim;
            avoidT -= dt;
            if (avoidT > 0f) return avoidAim;
            avoidT = 0.2f;
            Vector3 u = to / dist, look = pos + u * Mathf.Min(dist, 6f);
            float r = BodyRadius * 0.8f;
            if (WorldCollision.FirstBlocker(pos, look, r, moveTarget, out var c, out float cr))
            {
                // pass it on the side away from its middle, and keep to that side until the way is clear (round a whole cluster)
                Vector3 n = new Vector3(-u.z, 0f, u.x);
                int s = avoidSide != 0 ? avoidSide : (Vector3.Dot(c - pos, n) > 0f ? -1 : 1);
                avoidSide = s; avoidC = c;
                float side = Vector3.Dot(c - pos, n) * s + cr + r + 0.45f, ahead = Vector3.Dot(c - pos, u);
                // a point beside it that is inside the next thing along (structures side by side) moves out past that one too
                for (int k = 0; k < 6; k++)
                {
                    Vector3 p = pos + u * ahead + n * (s * side);
                    if (!WorldCollision.Overlapping(p, r, out var c2, out float cr2)) break;
                    side = Mathf.Max(side + 0.3f, Vector3.Dot(c2 - pos, n) * s + cr2 + r + 0.45f);
                }
                // aim beside it, no further on than its near edge: the kin slides along the face of a cluster until the way is clear
                avoidAim = pos + u * Mathf.Max(0f, ahead - cr - r - 0.1f) + n * (s * Mathf.Max(side, 0.6f));
                avoidAim.y = pos.y;
            }
            else { avoidAim = aim; avoidSide = 0; }
            return avoidAim;
        }

        Vector3 RandomPointNear(Vector3 center, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                var p = new Vector3(center.x + r.x, 0, center.z + r.y);
                float h = Ground.Height(p.x, p.z);
                if ((Sp.nearWater || h > 0.4f) && !Caves.Solid(p)) { p.y = h; return p; }
            }
            return center;
        }

        float FlatDist(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        // ================================================================ COMBAT
        public void ReceiveHit(in HitInfo hit)
        {
            if (!IsAlive) return;
            if (Invulnerable)
            {
                HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1.1f), "SHIELDED", new Color(0.8f, 0.9f, 1f), 0.7f);
                return;
            }
            if (hit.attacker != null) { LastAttacker = hit.attacker; LastAttackedTime = Time.time; }
            // Mercy: the player's and companion's hits never take a wild kin below 1 HP
            float dmg = ShieldHit(hit, hit.damage);   // a braced or guarding kin takes far less (CreatureActor.Effects)
            // Dex mastery: a species you know inside out takes 10% more from you and your companion
            if (Mode == CreatureMode.Wild && !Duelist && (hit.attacker is PlayerController || (hit.attacker is CreatureActor ally && ally.Mode == CreatureMode.Companion)))
                dmg *= DexRewards.DamageMul(GameManager.I.Player.Stats, Sp.id);
            bool mercy = Mode == CreatureMode.Wild && !Duelist && (hit.attacker is PlayerController || (hit.attacker is CreatureActor ca && ca.Mode == CreatureMode.Companion)) && GameManager.I.Player.Stats.HasEffect(UniqueEffect.Mercy);
            if (mercy && Hp - dmg < 1f)
            {
                if (Hp > 1f) HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1.1f), "SPARED", new Color(1f, 0.8f, 0.9f), 0.8f);
                Hp = Mathf.Min(Hp, 1f);
            }
            else Hp -= dmg;
            Rig.Flash(Color.white);
            Rig.Squash();
            if (hit.knockback.sqrMagnitude > 0f) knock = hit.knockback;
            if (hit.status != StatusType.None && Random.value < hit.statusChance && !Status.Active)
            {
                Status = new StatusState { type = hit.status, time = StatusState.Duration(hit.status), tick = 1f };
                HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 0.8f), hit.status.ToString().ToUpperInvariant(), StatusState.Color(hit.status), 0.8f);
            }
            // camp workers fight back when hit (then return to work)
            if (Mode == CreatureMode.Worker && hit.attacker != null && hit.attacker.Team == Team.Wild && State != AIState.Defend && Hp > MaxHp * 0.25f)
                BeginDefend(hit.attacker);
            if (Mode == CreatureMode.Wild && hit.attacker != null)
            {
                Aggro = true;
                Target = hit.attacker;
                if (Duelist) { if (State != AIState.Attack) SetState(AIState.Chase); }
                else if (Sp.temperament == Temperament.Timid && Hp < MaxHp * 0.6f) SetState(AIState.Flee, 3f);
                else if (State != AIState.Attack) SetState(AIState.Chase);
                CreatureRegistry.AlertPack(this, hit.attacker);
            }
            if (Hp <= 0f && Mode != CreatureMode.Wild && WorldRules.Current.cantFaint)
            {
                // Can't-faint worlds: your kin shake it off and carry on
                Hp = MaxHp * 0.5f;
                HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1f), "SHAKES IT OFF!", new Color(0.6f, 1f, 0.8f), 1f);
                Fx.Sparkle(transform.position + Vector3.up * BodyRadius, new Color(0.6f, 1f, 0.8f), 14, BodyRadius + 0.4f);
            }
            if (Hp <= 0f && Mode == CreatureMode.Companion && Data.BondLevel >= 5 && Time.time - Data.lastStand > 600f)
            {
                // max bond: refuses to fall once every 10 minutes
                Data.lastStand = Time.time;
                Hp = 1f;
                HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1f), "HOLDS ON!", new Color(1f, 0.6f, 0.75f), 1.1f);
                GameEvents.Good($"{Data.DisplayName} hangs on for you!");
            }
            if (Hp <= 0f) { Hp = 0f; Die(hit.attacker); }
        }

        /// <summary>Dev menu: sets its health to this share of full (never to nothing: use a hit for that).</summary>
        public void DevSetHp(float frac) { Hp = Mathf.Max(1f, MaxHp * Mathf.Clamp01(frac)); }

        void Die(ICombatant killer)
        {
            Hp = 0f;
            pendingAbility = -1;
            SetState(AIState.Dead);
            deathTimer = 0f;
            Fx.Burst(transform.position + Vector3.up * BodyRadius, Data.Palette.primary, 16, 4f, 0.35f, 0.7f);
            if (Mode == CreatureMode.Wild)
            {
                GameManager.I.OnWildDefeated(this, killer);
            }
            else
            {
                Data.hp = 0f;
                GameEvents.Warn($"{Data.DisplayName} fainted!");
                if (Mode == CreatureMode.Worker) BaseManager.I?.OnWorkerFainted(this);
                if (Mode == CreatureMode.Companion) GameManager.I.Party.OnCompanionFainted(this);
            }
        }

        /// <summary>
        /// The kin is being drawn into a capture seed: it stops acting at once. CaptureSystem shrinks the model into the
        /// seed and then hides it (it stays hidden while the seed shakes).
        /// </summary>
        public void BeginCapture()
        {
            pendingAbility = -1;
            dashTime = 0f;
            SetState(AIState.Captured);
        }

        /// <summary>Broke out of the seed: pops back out at full size, angry.</summary>
        public void EndCaptureFailed()
        {
            Rig.Hold = Color.clear;
            Rig.SetVisible(true);
            Rig.PopIn();
            Rig.Squash();
            Aggro = true;
            Target = GameManager.I.Player;
            SetState(Sp.temperament == Temperament.Timid ? AIState.Flee : AIState.Chase, 3f);
            Fx.Burst(transform.position + Vector3.up * BodyRadius, new Color(0.5f, 1f, 0.4f), 14, 5f, 0.3f, 0.5f, true);
        }

        public void ApplyStatus(StatusType t)
        {
            Status = new StatusState { type = t, time = StatusState.Duration(t), tick = 1f };
        }

        // ---------------------------------------------------------------- abilities
        int ReadyAbility(float dist, ICombatant target, out bool inRange)
        {
            inRange = false;
            int best = -1;
            float bestPower = -1f;
            for (int i = 0; i < abilities.Length; i++)
            {
                var a = abilities[i];
                if (a == null || cooldowns[i] > 0f) continue;
                if (a.kind == AbilityKind.Blink && CritPrimed) continue;   // spend the crit it set up first
                bool sig = a.effect != AbilityEffect.None;
                if (sig && EffectUseless(a, target)) continue;             // its mark or buff is still running
                var act = Acting(a);   // an Encore reaches as far as the move it will repeat
                float reach = a.effect == AbilityEffect.Riposte ? 4f : SkillEffects.SelfCast(act.effect) ? 9f : act.kind == AbilityKind.Burst ? act.radius * 0.8f : act.range;
                bool ok = dist <= reach + target.Radius + BodyRadius;
                float score = sig ? EffectPriority(a, target) : a.power;   // set-up moves first, then the hardest hitter
                if (ok && score > bestPower) { best = i; bestPower = score; inRange = true; }
            }
            if (best < 0)
            {
                // nothing in range: report the first ready attack so the caller approaches for it
                for (int i = 0; i < abilities.Length; i++)
                {
                    var a = abilities[i];
                    if (a == null || cooldowns[i] > 0f || SkillEffects.SelfCast(a.effect)) continue;
                    if (a.effect != AbilityEffect.None && EffectUseless(a, target)) continue;
                    return i;
                }
            }
            return best;
        }

        /// <summary>Shared chase/attack behaviour for wild and companion kin. Returns false if target invalid.</summary>
        bool FightTarget(float dt)
        {
            if (Target == null || !Target.IsAlive) { Target = null; return false; }
            float d = FlatDist(transform.position, Target.Position);
            int idx = ReadyAbility(d, Target, out bool inRange);
            // a crowd of wild kin takes turns on the player (Combat/AttackGate)
            bool waiting = AttackGate.Gated(this) && !(idx >= 0 && inRange && AttackGate.Allow(this)) && (idx < 0 || inRange);
            if (idx >= 0 && inRange && !waiting)
            {
                StartAbility(idx, Target);
                return true;
            }
            // approach: melee range for melee, keep some distance for ranged; one waiting its turn hangs back
            AbilityDef a = idx >= 0 ? abilities[idx] : null;
            float want = a != null && a.kind == AbilityKind.Projectile ? a.range * 0.7f : 1.2f + Target.Radius + BodyRadius;
            if (waiting) want = Mathf.Max(want, 4.5f + Target.Radius + BodyRadius);
            if (waiting && d < want - 1.2f)
            {
                Vector3 back = transform.position - Target.Position; back.y = 0f;
                MoveTo(transform.position + (back.sqrMagnitude > 0.01f ? back.normalized : transform.forward) * 2.5f, WalkSpeed * 1.3f);
                faceTarget = Target.Position;
            }
            else if (d > want) MoveTo(Target.Position, RunSpeed);
            else
            {
                // circle-strafe a little while waiting for cooldowns
                Vector3 side = Vector3.Cross(Vector3.up, (Target.Position - transform.position).normalized);
                MoveTo(transform.position + side * (Mathf.Sin(Time.time * 0.8f + GetInstanceID()) > 0 ? 2f : -2f), WalkSpeed);
                faceTarget = Target.Position;
            }
            return true;
        }

        void StartAbility(int i, ICombatant target)
        {
            var a = abilities[i];
            pendingAbility = i;
            pendingTarget = target;
            windup = a.windup * (Data.alpha ? 1.25f : 1f);
            cooldowns[i] = a.cooldown * Random.Range(0.9f, 1.2f) * (Mode == CreatureMode.Companion && Data.BondLevel >= 3 ? 0.9f : 1f)
                         * (Mode == CreatureMode.Companion && GameManager.I.Player.Stats.HasEffect(UniqueEffect.Starcaller) ? 0.85f : 1f);
            if (a.kind == AbilityKind.Burst) Fx.Ring(transform.position, a.color, a.radius * 0.5f, 18);
            Rig.Flash(a.color * 0.8f, 0.08f);
        }

        void TickAbility(float dt)
        {
            var a = abilities[pendingAbility];
            if (pendingTarget == null || !pendingTarget.IsAlive) { pendingAbility = -1; SetState(AIState.Recover, 0.3f); return; }
            bool ally = Mode == CreatureMode.Companion;
            windup -= dt;
            if (windup > 0f)
            {
                // your kin keep closing in during the wind-up so melee skills land on moving targets
                if (ally && a.kind == AbilityKind.Melee)
                {
                    float gap = FlatDist(transform.position, pendingTarget.Position) - (pendingTarget.Radius + BodyRadius);
                    if (gap > a.range * 0.6f) MoveTo(pendingTarget.Position, RunSpeed * 1.5f);
                }
                faceTarget = pendingTarget.Position;
                return;
            }

            // a move fired at empty ground still lands on a wild kin that happens to be standing there
            if (pendingTarget is AimPoint aimed && a.kind != AbilityKind.Projectile)
            {
                var there = CreatureRegistry.FindNearestWild(aimed.Position, Mathf.Max(2.5f, a.radius));
                if (there != null) pendingTarget = there;
            }
            Vector3 origin = transform.position + Vector3.up * (BodyRadius * 1.3f + Rig.HoverHeight);
            Vector3 to = pendingTarget.Position - transform.position; to.y = 0;
            Vector3 dir = to.sqrMagnitude > 0.001f ? to.normalized : transform.forward;
            float boost = Data.ElementBoost(a.element);
            Rig.Lunge();
            int slot = pendingAbility;
            if (a.effect == AbilityEffect.Encore && lastUsed != null)
            {
                // whatever it did last, once more with half the force
                a = lastUsed;
                boost = Data.ElementBoost(a.element) * 0.5f;
                Float("ENCORE", new Color(1f, 0.8f, 0.95f));
            }
            else if (a.effect != AbilityEffect.Encore) lastUsed = a;
            // signature moves that are more than one ordinary hit (CreatureActor.Effects)
            if (a.effect != AbilityEffect.None && ResolveEffect(a, slot, dir, boost))
            {
                pendingAbility = -1;
                SetState(AIState.Recover, 0.3f);
                return;
            }
            switch (a.kind)
            {
                case AbilityKind.Melee:
                    if (FlatDist(transform.position, pendingTarget.Position) <= a.range + pendingTarget.Radius + BodyRadius + (ally ? 1.4f : 0.6f))
                        CombatSystem.Apply(this, pendingTarget, a.power, a.element, a.status, a.statusChance, dir * 6f, boost, a.effect);
                    else if (ally) ShowMiss();
                    break;
                case AbilityKind.Projectile:
                {
                    Vector3 tp = pendingTarget.Position;
                    if (ally && pendingTarget is CreatureActor tca)
                    {
                        // lead a moving target
                        float travel = Vector3.Distance(origin, tp) / Mathf.Max(1f, a.projectileSpeed);
                        Vector3 lv = tca.Velocity; lv.y = 0f;
                        tp += lv * travel;
                    }
                    Vector3 aim = (tp + Vector3.up * pendingTarget.Radius) - origin;
                    ProjectileSystem.I.Fire(this, origin, aim.normalized * a.projectileSpeed, a, boost);
                    SynthAudio.Play(Sfx.Shoot, 1f + Random.Range(-0.1f, 0.1f), 0.6f);
                    break;
                }
                case AbilityKind.Burst:
                    Fx.Ring(transform.position, a.color, a.radius, 30);
                    Fx.Burst(transform.position + Vector3.up * 0.5f, a.color, 16, a.radius * 1.5f, 0.35f, 0.5f, true);
                    CreatureRegistry.ForEachEnemyInRange(this, transform.position, a.radius, t =>
                    {
                        Vector3 kd = t.Position - transform.position; kd.y = 0;
                        // a Pull drags them in instead of throwing them back
                        CombatSystem.Apply(this, t, a.power, a.element, a.status, a.statusChance, kd.normalized * (a.effect == AbilityEffect.Pull ? -9f : 5f), boost, a.effect);
                    });
                    SynthAudio.Play(Sfx.Burst, 1f, 0.7f);
                    break;
                case AbilityKind.Dash:
                {
                    dashTime = 0.28f;
                    float dd = FlatDist(transform.position, pendingTarget.Position) + 1f;
                    // your kin's dashes always reach the target (wild kin keep their fixed dash length)
                    float len = ally ? Mathf.Clamp(dd, 2f, a.range + 3f) : Mathf.Min(a.radius, dd);
                    dashVel = dir * len / 0.28f;
                    dashHit = false;
                    dashActive = true;
                    dashAbility = a;
                    dashBoost = boost;
                    SynthAudio.Play(Sfx.Whoosh, 1.1f, 0.6f);
                    break;
                }
                case AbilityKind.Blink:
                {
                    // gone in a puff of smoke and back behind the target: a light strike that leaves it reeling
                    Vector3 from = transform.position, tpos = pendingTarget.Position;
                    Vector3 tfwd = pendingTarget is CreatureActor tk ? tk.transform.forward : pendingTarget is PlayerController tpl ? tpl.Forward : dir;
                    tfwd.y = 0f;
                    tfwd = tfwd.sqrMagnitude > 0.01f ? tfwd.normalized : dir;
                    Vector3 side = Vector3.Cross(Vector3.up, tfwd);
                    float stand = pendingTarget.Radius + BodyRadius + 0.45f;
                    Vector3 spot = from;
                    // behind it if there is room, else at its flank, else it strikes from where it stands
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 c = tpos + (k == 0 ? -tfwd : k == 1 ? side : -side) * stand;
                        c.y = Ground.Height(c.x, c.z);
                        if (Caves.Solid(c + Vector3.up * 0.5f) || Mathf.Abs(c.y - tpos.y) > 2.5f) continue;
                        spot = c;
                        break;
                    }
                    Color smoke = new Color(0.16f, 0.15f, 0.2f, 0.85f);
                    Fx.Burst(from + Vector3.up * BodyRadius, smoke, 14, 2.6f, 0.45f, 0.5f);
                    Fx.Ring(from, a.color, BodyRadius + 0.6f, 14);
                    transform.position = spot;
                    vel = Vector3.zero;
                    Vector3 look = tpos - spot; look.y = 0f;
                    if (look.sqrMagnitude > 0.001f) { dir = look.normalized; transform.rotation = Quaternion.LookRotation(dir); }
                    faceTarget = tpos;
                    Fx.Burst(spot + Vector3.up * BodyRadius, smoke, 12, 2.6f, 0.4f, 0.45f);
                    Fx.Sparkle(spot + Vector3.up * BodyRadius, a.color, 10, BodyRadius + 0.4f);
                    SynthAudio.Play(Sfx.Whoosh, 1.7f, 0.7f);
                    CombatSystem.Apply(this, pendingTarget, a.power, a.element, a.status, a.statusChance, dir * 2f, boost);
                    if (pendingTarget.IsAlive)
                    {
                        if (pendingTarget is CreatureActor sk) sk.Stun(BlinkStun);
                        else if (pendingTarget is PlayerController sp) sp.Stun(BlinkStunPlayer);
                    }
                    // ...and whatever it does next lands as a crit
                    critUntil = Time.time + CritWindow;
                    if (ally) HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1.1f), "CRIT READY", new Color(1f, 0.4f, 0.3f), 0.75f);
                    break;
                }
            }
            bool blinked = a.kind == AbilityKind.Blink;
            pendingAbility = -1;
            SetState(AIState.Recover, blinked ? 0.2f : 0.45f);
        }

        AbilityDef dashAbility;
        float dashBoost;
        bool dashActive;

        void ShowMiss() => HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 0.8f), "MISS", new Color(0.8f, 0.8f, 0.85f), 0.8f);

        void LateUpdateDash()
        {
            if (dashActive && dashTime <= 0f)
            {
                dashActive = false;
                if (!dashHit && Mode == CreatureMode.Companion) ShowMiss();
            }
            if (dashTime <= 0f || dashHit || dashAbility == null || pendingTarget == null || !pendingTarget.IsAlive) return;
            bool ally = Mode == CreatureMode.Companion;
            if (ally)
            {
                // steer the dash onto a moving target
                Vector3 to = pendingTarget.Position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) dashVel = Vector3.RotateTowards(dashVel, to.normalized * dashVel.magnitude, 6f * Time.deltaTime, 0f);
            }
            if (FlatDist(transform.position, pendingTarget.Position) <= BodyRadius + pendingTarget.Radius + (ally ? 1.2f : 0.4f))
            {
                dashHit = true;
                CombatSystem.Apply(this, pendingTarget, dashAbility.power, dashAbility.element, dashAbility.status, dashAbility.statusChance, dashVel.normalized * 7f, dashBoost, dashAbility.effect);
                Fx.Burst(transform.position, dashAbility.color, 10, 4f, 0.3f, 0.4f, true);
            }
        }

        void LateUpdate()
        {
            if (dashTime > 0f || dashActive) LateUpdateDash();
        }
    }
}
