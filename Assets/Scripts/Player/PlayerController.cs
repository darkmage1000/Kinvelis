using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Third-person explorer: kinematic movement on the heightfield (no physics), sprint/sneak/jump/dodge,
    /// swimming, stamina + hunger survival, melee combat with combo + hit-stop, auto-tool gathering,
    /// soft-lock targeting and capture seed throwing.
    /// </summary>
    public partial class PlayerController : MonoBehaviour, ICombatant
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.4f;
        [SerializeField] float runSpeed = 7.4f;
        [SerializeField] float sneakSpeed = 2.3f;
        [SerializeField] float swimSpeed = 3.2f;
        [SerializeField] float jumpVelocity = 7.5f;
        [SerializeField] float gravity = 24f;
        [SerializeField] float dodgeSpeed = 12f;
        [SerializeField] float dodgeTime = 0.32f;
        [SerializeField] float radius = 0.4f;

        [Header("Stamina / Survival")]
        [SerializeField] float sprintCost = 12f;
        [SerializeField] float dodgeCost = 22f;
        [SerializeField] float jumpCost = 8f;
        [SerializeField] float staminaRegen = 20f;
        [SerializeField] float hungerPerSecond = 100f / (25f * 60f);

        [Header("Combat")]
        [SerializeField] float swingTime = 0.36f;
        [SerializeField] float meleeRange = 2.3f;
        [SerializeField] float targetRange = 18f;
        [SerializeField] float gatherRange = 1.8f;

        public PlayerStats Stats { get; set; }
        public CreatureRig Rig { get; private set; }
        public CreatureActor CombatTarget { get; private set; }
        public ResourceNode GatherTarget { get; private set; }

        public bool IsSprinting { get; private set; }
        /// <summary>RUN button state (toggle). Auto-cancels when you stop, swim, sneak or run dry.</summary>
        public bool SprintToggled { get; private set; }
        /// <summary>Stamina hit zero: no sprinting until it recovers to 25%.</summary>
        public bool Exhausted { get; private set; }
        float stillTime;
        public bool IsCrouching { get; private set; }
        public bool IsSwimming { get; private set; }
        public float Speed { get; private set; }
        public Vector3 Forward => transform.forward;
        public Vector3 Right => transform.right;

        CameraRig cam;
        Vector3 hVel;
        /// <summary>Ground speed and direction (cave rockfalls aim a step ahead of you).</summary>
        public Vector3 Velocity => hVel;
        float vVel;
        bool grounded = true;
        bool launched;
        /// <summary>Set every frame by Hazards (quicksand, icy water): 1 = normal.</summary>
        [System.NonSerialized] public float SlowMul = 1f;
        public bool Grounded => grounded;

        /// <summary>Thrown into the air (geysers). The landing that follows does no fall damage.</summary>
        public void Launch(float up)
        {
            if (IsMounted || IsSwimming) return;
            vVel = up;
            grounded = false;
            launched = true;
        }

        public void DrainStamina(float amount)
        {
            Stats.stamina = Mathf.Max(0f, Stats.stamina - amount);
            staminaDelay = 0.8f;
        }
        float dodgeTimer;
        Vector3 dodgeDir;
        float staminaDelay;
        float swingTimer;
        bool swingHitDone;
        // the swing in progress: how the thing in hand is used (HandStyles), what it is for, how long it takes
        HandStyle swingStyle, heldStyle;
        SwingAct swingAct;
        float swingDur = 0.36f, swingHitAt = 0.14f;
        bool swingHandFull;
        int combo;
        float comboWindow;
        float targetTimer;
        float lastHurt = -99f;
        float stunTimer;
        public bool Stunned => stunTimer > 0f;

        /// <summary>A Blink skill landed on you: barely able to move, and no swinging or shooting, for a moment. A roll's safe frames avoid it.</summary>
        public void Stun(float seconds)
        {
            if (dead || invuln > 0f) return;
            stunTimer = Mathf.Max(stunTimer, seconds);
            HUD.DamageNumber(transform.position + Vector3.up * 2.4f, "STUNNED", new Color(1f, 0.92f, 0.4f), 0.8f);
        }
        float invuln;
        float starveTick;
        float stepTimer;
        Transform weaponVisual;
        string weaponVisualId;
        bool dead;
        string autoSwapItem;     // tool auto-used for gathering (shown in hand briefly)
        float autoSwapTimer;

        /// <summary>Last creature that hurt the player (companion Defensive stance retaliates).</summary>
        public ICombatant LastAttacker { get; private set; }
        public float LastAttackedTime { get; private set; } = -99f;
        /// <summary>Item currently shown in hand (held item or a temporarily auto-used tool).</summary>
        public string DisplayedItem => autoSwapTimer > 0f && !string.IsNullOrEmpty(autoSwapItem) ? autoSwapItem : Stats.held;
        /// <summary>Contextual label for the ATTACK button (ATTACK / CHOP / MINE / PICK).</summary>
        public string ContextAction { get; private set; } = "ATTACK";

        // ================================================================ ICombatant
        public Vector3 Position => transform.position;
        public float Radius => radius;
        public bool IsAlive => !dead;
        public Team Team => Team.Player;
        public int Level => Stats.level;
        public float AttackStat => Stats.Attack;
        public float DefenseStat => Stats.Defense * Status.DefMul;

        // ================================================================ STATUS / EXPOSURE / GEAR VISUALS
        /// <summary>Status effect on the player (Burn, Chill, Shock, Drowsy, Soaked). Gear can grant immunity.</summary>
        public StatusState Status;
        /// <summary>0..1 after resistances (Snow/Blizzard = cold, Heatwave/Ashfall = heat).</summary>
        public float ColdExposure { get; private set; }
        public float HeatExposure { get; private set; }
        readonly System.Collections.Generic.List<GameObject> gearObjects = new System.Collections.Generic.List<GameObject>();
        int gearVersionShown = -1;
        bool hatHidden;

        bool ImmuneTo(StatusType t) =>
            (t == StatusType.Poison && Stats.PoisonWarded) ||
            (t == StatusType.Burn && Stats.HasEffect(UniqueEffect.Emberward)) ||
            (t == StatusType.Chill && (Stats.HasEffect(UniqueEffect.Frostward) || Stats.HasEffect(UniqueEffect.Rimecloak)));

        void TickStatusAndExposure(float dt)
        {
            if (Status.Active)
            {
                Status.time -= dt;
                if (Status.type == StatusType.Burn || Status.type == StatusType.Poison)
                {
                    Status.tick -= dt;
                    if (Status.tick <= 0f) { Status.tick = 1f; Damage(2f + Stats.MaxHp * 0.01f, false); HUD.DamageNumber(transform.position + Vector3.up * 2.2f, Status.type == StatusType.Burn ? "BURN" : "POISON", StatusState.Color(Status.type), 0.6f); }
                }
                if (Status.time <= 0f || ImmuneTo(Status.type)) Status = default;
            }
            var env = EnvironmentSystem.I;
            float cold = 0f, heat = 0f;
            if (env != null && !env.Indoor)   // no weather underground
            {
                switch (env.Weather)
                {
                    case WeatherType.Blizzard: cold = 1f; break;
                    case WeatherType.Snow: cold = 0.5f; break;
                    case WeatherType.Heatwave: heat = env.IsNight ? 0.4f : 1f; break;
                    case WeatherType.Ashfall: heat = 0.5f; break;
                }
                // the Sunscour Dunes bake by day whatever the weather
                if (env.CurrentBiome == BiomeId.Desert && !env.IsNight) heat = Mathf.Max(heat, 0.6f);
            }
            if (IsMounted && (mount.Data.Element1 == Element.Frost || mount.Data.Element2 == Element.Frost)) cold = 0f;
            ColdExposure = cold * (1f - Stats.ColdResist) * KinAid.ColdMul;   // an Ember kin out with you keeps you warm (KinAid)
            HeatExposure = heat * (1f - Stats.HeatResist) * KinAid.HeatMul;   // a Frost kin keeps you cool
        }

        void UpdateGearVisual()
        {
            if (Stats.GearVersion == gearVersionShown) return;
            gearVersionShown = Stats.GearVersion;
            bool wantHide = !string.IsNullOrEmpty(Stats.gear[GearSlots.Head]);
            if (wantHide != hatHidden)
            {
                hatHidden = wantHide;
                // rebuild the base body without (or with) what the explorer wears on the head
                RebuildBody();
            }
            ArmorVisuals.Apply(Rig, Stats, gearObjects);
        }

        void RebuildBody()
        {
            if (Rig != null && Rig.Model != null) Destroy(Rig.Model.gameObject);
            Rig = AvatarLook.BuildRig(Stats.look, hatHidden, transform);
            weaponVisual = null; weaponVisualId = null;
            gearObjects.Clear();
        }

        /// <summary>The explorer's look was changed (Gear tab > CHANGE LOOK): build the new body and put the gear back on it.</summary>
        public void RebuildLook()
        {
            RebuildBody();
            ArmorVisuals.Apply(Rig, Stats, gearObjects);
        }
        public Element Element1 => Element.Normal;
        public Element Element2 => Element.Normal;

        public void Init(PlayerStats stats, CameraRig camRig)
        {
            Stats = stats;
            cam = camRig;
            if (Stats.look == null) Stats.look = new Appearance();
            Rig = AvatarLook.BuildRig(Stats.look, false, transform);
        }

        public void Teleport(Vector3 p, float yaw)
        {
            p.y = GroundHeight(p);
            transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            hVel = Vector3.zero;
            vVel = 0f;
            cam?.SnapTo(p);
        }

        float GroundHeight(Vector3 p) => Ground.HeightNear(p.x, p.z, p.y);
        /// <summary>How high a step onto a floor (or anything built) you take in your stride - no jump needed.</summary>
        public const float BuiltStepUp = 0.6f;   // upper storeys and stairs count when they are within a step of your feet

        // ================================================================ TICK
        public void Tick(float dt)
        {
            if (dead) { ClearLock(); UpdateLockMark(); return; }
            if (stunTimer > 0f) stunTimer -= dt;
            TickMarks(dt);
            var q = Stats;
            float maxSta = q.MaxStamina;

            // ---------------- input ----------------
            Vector2 mv = InputHub.Move;
            Vector3 wish = cam.Forward * mv.y + cam.Right * mv.x;
            float wishMag = Mathf.Clamp01(wish.magnitude);
            if (wishMag > 0.001f) wish /= wish.magnitude;

            if (InputHub.Down(Btn.Sneak) && !IsMounted) IsCrouching = !IsCrouching;
            // sprint is deliberate: RUN toggle (touch/pad) or hold Shift - never from pushing the stick
            if (InputHub.Down(Btn.SprintToggle))
            {
                SprintToggled = !SprintToggled;
                if (SprintToggled && Exhausted) { SprintToggled = false; GameEvents.Warn("Too tired to run - catch your breath."); }
                if (SprintToggled) IsCrouching = false;
            }
            if (wishMag < 0.1f) { stillTime += dt; if (stillTime > 0.6f) SprintToggled = false; } else stillTime = 0f;
            if (IsSwimming || IsCrouching) SprintToggled = false;
            if (q.stamina <= 0.5f && !Exhausted) { Exhausted = true; SprintToggled = false; }
            if (Exhausted && q.stamina >= q.MaxStamina * 0.25f) Exhausted = false;
            bool wantSprint = (InputHub.Held(Btn.Sprint) || SprintToggled || WorldRules.Current.simpleControls) && !IsCrouching && !Exhausted && !Aiming;
            IsSprinting = wantSprint && wishMag > 0.1f && q.stamina > 1f && !IsSwimming;
            if (IsSprinting) IsCrouching = false;

            // ---------------- stamina ----------------
            var compPerk = GameManager.I.Party.Companion != null ? GameManager.I.Party.Companion.Sp.perk : CompanionPerk.None;
            TickStatusAndExposure(dt);
            KinAid.Tick(this, dt);
            float staMul = KinAid.StaminaCostMul * (compPerk == CompanionPerk.Tailwind ? 0.65f : 1f) * (IsMounted ? 0.5f * mount.Data.MountStaminaMul * q.KinStaminaMul : 1f) * WorldRules.Current.staminaDrain * (1f + 0.3f * HeatExposure);
            if (Dev.Stamina) q.stamina = q.MaxStamina;
            if (IsSprinting) { q.stamina -= sprintCost * staMul * (q.HasEffect(UniqueEffect.Ventwalker) ? 0.7f : 1f) * dt; staminaDelay = 0.8f; }
            else if (Gliding) { q.stamina -= GlideDrain * dt; staminaDelay = 0.8f; if (q.stamina <= 0f) SetGliding(false); }
            else if (IsSwimming && wishMag > 0.1f && !IsMounted && !WorldRules.Current.safeTravel && !q.HasEffect(UniqueEffect.Buoyant)) { q.stamina -= 4f * q.SwimStaminaMul * KinAid.SwimStaminaMul * dt; staminaDelay = 0.8f; }
            else
            {
                staminaDelay -= dt;
                float regen = staminaRegen * q.RegenMul * KinAid.StaminaRegenMul * (CreatureActor.AnthemMul(Team.Player, transform.position) > 1f ? SkillEffects.AnthemStamina : 1f) * (1f - 0.5f * ColdExposure) * (Status.Active && Status.type == StatusType.Drowsy ? 0.35f : 1f);
                if (staminaDelay <= 0f) q.stamina = Mathf.Min(maxSta, q.stamina + regen * dt);
            }
            q.stamina = Mathf.Max(0f, q.stamina);

            // ---------------- survival ----------------
            var comp = GameManager.I.Party.Companion;
            float hungerMul = (comp != null && comp.Sp.perk == CompanionPerk.Warmth ? 0.7f : 1f) * q.HungerMul * (1f + 0.6f * HeatExposure);
            q.hunger = Mathf.Max(0f, q.hunger - hungerPerSecond * hungerMul * WorldRules.Current.hungerRate * dt);
            if (q.hunger <= 0f)
            {
                starveTick -= dt;
                if (starveTick <= 0f) { starveTick = 1f; Damage(1.5f, false); }
            }
            else if (q.hunger > 40f && Time.time - lastHurt > 6f && q.hp < q.MaxHp) q.hp = Mathf.Min(q.MaxHp, q.hp + 0.8f * (q.HasEffect(UniqueEffect.Bloomwalk) ? 2f : 1f) * dt);
            // Blightmoor: its pools are poison to wade through (riding a kin keeps you out of it)
            var envNow = EnvironmentSystem.I;
            if (envNow != null && envNow.CurrentBiome == BiomeId.Blight && !IsMounted)
            {
                Vector3 tp = transform.position;
                if (Ground.Height(tp.x, tp.z) < WorldGen.WaterLevel - 0.25f && tp.y < WorldGen.WaterLevel + 0.5f)   // (no tide in Blightmoor)
                {
                    toxicTick -= dt;
                    if (toxicTick <= 0f)
                    {
                        toxicTick = 1f;
                        Damage(4f * KinAid.ToxicWaterMul, false);
                        HUD.DamageNumber(tp + Vector3.up * 2.2f, "TOXIC", new Color(0.7f, 1f, 0.3f), 0.8f);
                        if (Time.time - toxicHint > 40f) { toxicHint = Time.time; GameEvents.Warn("The pools here are poison - get out of the water."); }
                    }
                }
            }
            if (IsSwimming && q.stamina <= 0f && !WorldRules.Current.safeTravel)
            {
                starveTick -= dt;
                if (starveTick <= 0f) { starveTick = 1f; Damage(5f, false); GameEvents.Warn("Exhausted! Get to shore."); }
            }

            // ---------------- dodge / jump ----------------
            invuln -= dt;
            if (InputHub.Down(Btn.Dodge) && dodgeTimer <= 0f && q.stamina >= dodgeCost * 0.5f && !IsSwimming && !IsMounted)
            {
                q.stamina -= dodgeCost * staMul * (q.HasEffect(UniqueEffect.LightStep) ? 0.5f : 1f); staminaDelay = 0.8f;
                dodgeTimer = dodgeTime;
                dodgeDir = wishMag > 0.1f ? wish : transform.forward;
                invuln = dodgeTime + 0.05f;
                swingTimer = 0f;
                SynthAudio.Play(Sfx.Whoosh, 1.2f, 0.5f);
                Fx.Burst(transform.position + Vector3.up * 0.2f, new Color(0.9f, 0.9f, 0.85f, 0.7f), 6, 2f, 0.4f, 0.4f);
            }
            if (InputHub.Down(Btn.Jump) && grounded && !IsSwimming && q.stamina > 2f)
            {
                vVel = (IsMounted ? (mount.Sp.mountable ? mount.Sp.mountJump : jumpVelocity * 1.15f) : jumpVelocity * (q.HasEffect(UniqueEffect.Featherstep) ? 1.2f : 1f)) * Mathf.Sqrt(Mathf.Max(0.25f, WorldRules.Current.jumpPower));   // height goes with the square of the speed
                grounded = false;
                q.stamina -= jumpCost * staMul; staminaDelay = 0.6f;
                IsCrouching = false;
                SynthAudio.Play(Sfx.Jump, 1f, 0.5f);
            }
            else if (InputHub.Down(Btn.Jump) && (Gliding || CanGlideNow)) ToggleGlide();
            if (grappleCd > 0f) grappleCd -= dt;
            TickHookTarget(dt);
            if (InputHub.Down(Btn.Grapple)) TryGrapple();

            // ---------------- movement ----------------
            // blizzard slow scales with cold exposure (frost mounts / cold gear ignore it)
            float envMul = EnvironmentSystem.I != null ? Mathf.Lerp(1f, EnvironmentSystem.I.PlayerSpeedMul, ColdExposure) : 1f;
            if (Status.SpeedMul < 1f) envMul *= Mathf.Lerp(Status.SpeedMul, 1f, 0.5f);   // statuses slow the player half as much as kin
            float perkMul = comp != null && comp.Sp.perk == CompanionPerk.Swiftness ? 1.12f : 1f;
            float tailwind = compPerk == CompanionPerk.Tailwind ? 1.15f : 1f;
            float target = IsSwimming ? swimSpeed * q.SwimMul : (IsCrouching ? sneakSpeed : (IsSprinting ? runSpeed * perkMul : walkSpeed)) * tailwind;
            if (IsMounted) target = MountTargetSpeed() * (IsSwimming ? 0.75f : 1f);
            else target *= q.MoveMul * (Aiming ? 0.75f : 1f);   // careful steps with a seed ready
            target *= SlowMul * WorldRules.Current.moveSpeed;
            if (stunTimer > 0f) target *= 0.15f;
            if (swingTimer > 0f) target *= IsMounted ? 0.7f : swingAct == SwingAct.Shoot ? 0.55f : 0.35f;
            Vector3 desired = wish * target * wishMag * envMul;
            if (Gliding) desired = (wishMag > 0.1f ? wish : transform.forward) * GlideSpeed * q.MoveMul;
            float accel = grounded ? 40f : 12f;
            hVel = Vector3.MoveTowards(hVel, desired, accel * dt);
            Vector3 step = hVel * dt;
            if (dodgeTimer > 0f) { dodgeTimer -= dt; step = dodgeDir * dodgeSpeed * dt; }

            Vector3 pos = transform.position;
            Vector3 next = pos + step;
            if (Time.time < tetherUntil) ClampTether(ref next);   // pinned by a Strings skill

            // slope limit: block steep uphill steps - but step up onto a floor or anything else built that is no higher than BuiltStepUp
            float hNow = GroundHeight(pos), hNext = GroundHeight(next);
            float run = new Vector2(step.x, step.z).magnitude;
            bool stepUp = hNext - hNow <= BuiltStepUp && hNext > WorldGen.Height(next.x, next.z) + 0.02f;
            if (grounded && run > 0.0001f && (hNext - hNow) / run > 1.35f + q.ClimbBonus && !stepUp)
            {
                next.x = pos.x; next.z = pos.z;
                hNext = hNow;
            }
            WorldCollision.PushOut(ref next, IsMounted ? Mathf.Max(radius, mount.BodyRadius) : radius);
            next.x = Mathf.Clamp(next.x, -WorldGen.HalfWorld + 20f, WorldGen.HalfWorld - 20f);
            next.z = Mathf.Clamp(next.z, -WorldGen.HalfWorld + 20f, WorldGen.HalfWorld - 20f);
            hNext = GroundHeight(next);

            // vertical
            float sea = Tides.Surface(next);   // the Isles have tides; everywhere else this is WaterLevel
            float waterSurface = sea - 1.05f;
            // a small water kin floats high enough to keep its rider's seat just clear of the water
            if (IsMounted && MountSwims) waterSurface = Mathf.Max(waterSurface, sea + 0.12f - SeatY);
            IsSwimming = hNext < waterSurface - 0.1f && next.y <= waterSurface + 0.3f;
            if (IsSwimming && IsMounted && !MountSwims) { Dismount(true); GameEvents.Say("You slide off into the water - only water kin can carry you across. Summon a Tide kin and tap SURF."); }
            TickSurf(dt);
            if (IsSwimming && Gliding) SetGliding(false);
            if (IsSwimming)
            {
                vVel = 0f;
                launched = false;
                next.y = Mathf.Lerp(pos.y, waterSurface + Mathf.Sin(Time.time * 2f) * 0.06f, dt * 6f);
                grounded = false;
            }
            else
            {
                if (FlyingMount && (!grounded || InputHub.Held(Btn.Jump))) { FlyStep(dt, hNext, staMul); grounded = false; }
                else vVel -= gravity * dt * (Gliding ? 0.15f : 1f);
                if (Gliding) vVel = Mathf.Max(vVel, -GlideFall * KinAid.GlideFallMul);
                next.y = pos.y + vVel * dt;
                if (next.y <= hNext)
                {
                    float fallSafe = IsMounted ? 18f : 14f;
                    var wr = WorldRules.Current;   // safe-travel worlds and toy-box jumps never hurt on landing
                    if (!grounded && vVel < -fallSafe && !launched && !q.HasEffect(UniqueEffect.Featherstep) && !wr.safeTravel && wr.jumpPower <= 1.05f && !KinAid.NoFallDamage) Damage((-vVel - fallSafe) * 3f, false);
                    launched = false;
                    if (Gliding) SetGliding(false);
                    next.y = hNext;
                    vVel = 0f;
                    grounded = true;
                }
                else if (grounded && next.y - hNext < 0.45f && vVel <= 0f)
                {
                    next.y = hNext; // stick to ground going downhill
                    vVel = 0f;
                }
                else grounded = false;
            }
            if (grappling) { next = GrappleStep(dt); if (grappling) vVel = 0f; grounded = false; }   // (the last step may fling you on)
            airTime = grounded || IsSwimming ? 0f : airTime + dt;
            transform.position = next;

            Vector3 flatVel = (next - pos) / Mathf.Max(dt, 0.0001f); flatVel.y = 0;
            Speed = flatVel.magnitude;

            // facing
            // a swing turns you to the target; a shot turns you to the kin it was drawn on, or to where the camera looks
            Vector3 face;
            if (dodgeTimer > 0f) face = dodgeDir;
            else if (swingTimer > 0f && swingAct == SwingAct.Shoot) face = shotTarget != null ? shotTarget.Position - pos : cam.Forward;
            else if (swingTimer > 0f && CombatTarget != null) face = CombatTarget.Position - pos;
            else face = wishMag > 0.1f ? wish : Vector3.zero;
            face.y = 0;
            if (face.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 900f * dt);

            // footsteps
            if (grounded && Speed > 1f)
            {
                stepTimer -= dt * Speed;
                if (stepTimer <= 0f) { stepTimer = 2.4f; SynthAudio.Play(Sfx.Step, Random.Range(0.9f, 1.1f), IsCrouching ? 0.15f : 0.3f); }
            }

            // ---------------- targeting ----------------
            TickLock(dt, pos);   // LOCK / tap a kin / next-previous (PlayerController.Lock)
            targetTimer -= dt;
            if (targetTimer <= 0f)
            {
                targetTimer = 0.15f;
                var fwd = cam.Forward;
                // a locked kin is the target, full stop. Otherwise the soft target: ahead of where you walk, or - with a
                // ranged weapon out - whatever the camera looks at, out to the weapon's range
                bool ranged = HandStyles.IsRanged(heldStyle);
                if (LockTarget != null) CombatTarget = LockTarget;
                else CombatTarget = CreatureRegistry.FindTarget(pos, wishMag > 0.1f && !ranged ? wish : fwd, ranged ? Mathf.Max(targetRange, HandStyles.Ranged(heldStyle).range) : targetRange);
                GatherTarget = ChunkManager.I != null ? ChunkManager.I.FindNode(pos, transform.forward, GatherReach) : null;
                ContextAction = ComputeContextAction(pos);
                Stats.ValidateEquipment();
            }
            if (autoSwapTimer > 0f) autoSwapTimer -= dt;

            // ---------------- actions ----------------
            comboWindow -= dt;
            if (swingTimer > 0f)
            {
                swingTimer -= dt;
                if (!swingHitDone && swingDur - swingTimer >= swingHitAt) { swingHitDone = true; ResolveSwing(); }
            }
            if (!Aiming && stunTimer <= 0f)   // with a seed out, ATTACK throws instead (PlayerController.Aim); stunned, nothing
            {
                if (InputHub.Down(Btn.Attack) && swingTimer <= 0.08f && dodgeTimer <= 0f && !IsSwimming) StartSwing();
                else if (InputHub.Held(Btn.Attack) && swingTimer <= 0f && dodgeTimer <= 0f && !IsSwimming && HoldTargetInReach(pos)) StartSwing();   // hold to keep gathering / attacking
                // while holding with no stick input, turn toward the node being gathered
                if (InputHub.Held(Btn.Attack) && wishMag < 0.1f && CombatTarget == null && GatherTarget != null && GatherTarget.alive)
                {
                    Vector3 toNode = GatherTarget.pos - pos; toNode.y = 0;
                    if (toNode.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toNode), 720f * dt);
                }
            }
            TickThrowInput(dt);   // SEED = ready stance toggle, ATTACK throws while ready (PlayerController.Aim)

            UpdateMountFollow();
            if (InputHub.Down(Btn.Ride)) ToggleRide();

            // ---------------- animation ----------------
            // the base pose: walk / run with real knees and elbows, a crouch for sneaking and rolling, a tuck in the air
            Rig.Crouch = !IsMounted && IsCrouching ? 1f : 0f;
            Rig.Air = !IsMounted && !grounded && !IsSwimming && !grappling && airTime > 0.1f ? 1f : 0f;
            if (IsMounted) AnimateRider(dt);
            else Rig.Animate(dt, Speed * (IsCrouching ? 1.6f : 1f), false, false);
            if (dodgeTimer > 0f && !IsMounted && Rig.Model != null) PoseRoll();
            // then whatever the body is busy with, laid over it
            if (!IsMounted && Rig.Human)
            {
                if (IsSwimming) PoseSwim();
                else if (Gliding) PoseGlide();
            }
            if (weaponVisual != null)
            {
                // hands are busy in the water and on the glider: whatever was held is put away for the moment
                bool stowed = !IsMounted && (IsSwimming || Gliding) && !Aiming;
                if (weaponVisual.gameObject.activeSelf == stowed) weaponVisual.gameObject.SetActive(!stowed);
            }
            if (swingTimer > 0f) ApplySwingPose();
            else if (weaponVisual != null) weaponVisual.localRotation = Aiming ? Quaternion.identity : HandStyles.Carry(heldStyle);
            if (Aiming && swingTimer <= 0f && !IsMounted) PoseAim();
            UpdateGearVisual();
            UpdateWeaponVisual();
        }

        /// <summary>
        /// The dodge roll: tuck into a ball and turn once around the middle of the body, a little above the ground.
        /// (The model's origin is at the feet, so turning it there swings the whole body under the ground.)
        /// </summary>
        void PoseRoll()
        {
            float k = Mathf.Clamp01(1f - dodgeTimer / dodgeTime);
            var spin = Quaternion.Euler(k * 360f, 0f, 0f);
            if (!Rig.Human) { Rig.Model.localRotation = spin; return; }
            float w = Mathf.Clamp01(Mathf.Min(k, 1f - k) / 0.16f);
            w = w * w * (3f - 2f * w);   // into the tuck at the start, out of it to land
            for (int i = 0; i < 2; i++)
            {
                bool right = i == 0;
                float sd = right ? 1f : -1f;
                HumanIK.Blend(Rig, right ? BodyGroup.LegFR : BodyGroup.LegFL, Quaternion.Euler(-122f, 0f, -6f * sd), w);
                HumanIK.Blend(Rig, right ? BodyGroup.ShinR : BodyGroup.ShinL, Quaternion.Euler(136f, 0f, 0f), w);
                HumanIK.Blend(Rig, right ? BodyGroup.ArmR : BodyGroup.ArmL, Quaternion.Euler(-64f, 0f, -10f * sd), w);
                HumanIK.Blend(Rig, right ? BodyGroup.ForearmR : BodyGroup.ForearmL, Quaternion.Euler(-96f, 0f, 0f), w);
            }
            HumanIK.Blend(Rig, BodyGroup.Torso, Quaternion.Euler(88f, 0f, 0f), w);
            HumanIK.Blend(Rig, BodyGroup.Head, Quaternion.Euler(40f, 0f, 0f), w);
            // the tucked body fits a ball about 0.61 m in radius around this point (measured: chest folded onto the
            // knees); that middle rides at RollHeight while everything turns around it, so nothing dips below the ground
            Vector3 middle = new Vector3(0f, 0.77f, 0.43f), ride = new Vector3(0f, RollHeight, 0f);
            Rig.Model.localRotation = spin;
            Rig.Model.localPosition = Vector3.Lerp(Rig.Model.localPosition, ride - spin * middle, w);
            // half-way into (and out of) the tuck the legs are still long: a small hop keeps the feet clear
            Rig.Model.localPosition += Vector3.up * (0.14f * 4f * w * (1f - w));
        }
        const float RollHeight = 0.63f;

        /// <summary>Seed raised by the ear, elbow cocked, body turned side-on: ready to throw.</summary>
        void PoseAim()
        {
            HumanIK.Arm(new Vector3(0.14f, 0.2f, -0.2f), true, out var up, out var fore);
            HumanIK.Blend(Rig, BodyGroup.ArmR, up, 1f);
            HumanIK.Blend(Rig, BodyGroup.ForearmR, fore, 1f);
            var torso = Rig.GetGroup(BodyGroup.Torso);
            if (torso != null) torso.localRotation = torso.localRotation * Quaternion.Euler(-4f, 22f, 0f);
        }

        /// <summary>The throw itself: a quick overarm (the seed has already left the hand).</summary>
        public void PlayThrow()
        {
            swingStyle = HandStyle.Fists;
            swingAct = SwingAct.Throw;
            swingHandFull = false;
            combo = 0;
            swingDur = HandStyles.Info(swingStyle, swingAct).time;
            swingHitAt = swingDur * 2f;   // nothing to hit
            swingHitDone = true;
            swingTimer = swingDur;
        }

        /// <summary>In the water: tip forward and crawl when moving, tread upright when still.</summary>
        void PoseSwim()
        {
            float go = Mathf.Clamp01(Speed / 2f), t = Time.time;
            float stroke = t * 5.5f, kick = t * 9f;
            if (Rig.Model != null) Rig.Model.localRotation = Quaternion.Euler(40f * go, 0f, Mathf.Sin(stroke) * 5f * go);
            // crawl: the arms reach forward over the water in turn; treading: they scull out to the sides
            for (int i = 0; i < 2; i++)
            {
                bool right = i == 0;
                float ph = stroke + (right ? 0f : Mathf.PI), sd = right ? 1f : -1f;
                var crawl = new Vector3(sd * 0.1f, 0.55f + Mathf.Sin(ph) * 0.45f, 0.75f + Mathf.Cos(ph) * 0.35f).normalized * (0.48f * (0.75f + 0.25f * Mathf.Sin(ph)));
                var tread = new Vector3(sd * (0.38f + Mathf.Sin(t * 3f + i) * 0.06f), -0.05f, 0.18f + Mathf.Cos(t * 3f + i) * 0.08f);
                HumanIK.Arm(Vector3.Lerp(tread, crawl, go), right, out var up, out var fore);
                HumanIK.Blend(Rig, right ? BodyGroup.ArmR : BodyGroup.ArmL, up, 1f);
                HumanIK.Blend(Rig, right ? BodyGroup.ForearmR : BodyGroup.ForearmL, fore, 1f);
                float k = Mathf.Sin(kick + (right ? 0f : Mathf.PI));
                Rig.OverrideRot(right ? BodyGroup.LegFR : BodyGroup.LegFL, Quaternion.Euler(8f + k * (10f + 14f * go), 0f, 0f));
                Rig.OverrideRot(right ? BodyGroup.ShinR : BodyGroup.ShinL, Quaternion.Euler(14f + Mathf.Max(0f, k) * 24f, 0f, 0f));
            }
            Rig.OverrideRot(BodyGroup.Torso, Quaternion.Euler(6f * go, Mathf.Sin(stroke) * 10f * go, 0f));
        }

        /// <summary>Hanging from the glider's bar: both hands up on it, legs trailing.</summary>
        void PoseGlide()
        {
            for (int i = 0; i < 2; i++)
            {
                bool right = i == 0;
                Vector3 bar = new Vector3(right ? 0.34f : -0.34f, 1.75f, -0.1f);
                HumanIK.Arm(bar - (right ? PlayerLook.ShoulderR : PlayerLook.ShoulderL), right, out var up, out var fore);
                HumanIK.Blend(Rig, right ? BodyGroup.ArmR : BodyGroup.ArmL, up, 1f);
                HumanIK.Blend(Rig, right ? BodyGroup.ForearmR : BodyGroup.ForearmL, fore, 1f);
                Rig.OverrideRot(right ? BodyGroup.LegFR : BodyGroup.LegFL, Quaternion.Euler(16f + Mathf.Sin(Time.time * 2f + i) * 4f, 0f, 0f));
                Rig.OverrideRot(right ? BodyGroup.ShinR : BodyGroup.ShinL, Quaternion.Euler(22f, 0f, 0f));
            }
            Rig.OverrideRot(BodyGroup.Torso, Quaternion.identity);
        }

        /// <summary>Arms, the item in the hand and the body for this moment of the swing (each kind of weapon and tool has its own moves: HandStyles).</summary>
        void ApplySwingPose()
        {
            float k = Mathf.Clamp01(1f - swingTimer / Mathf.Max(0.01f, swingDur));
            HandStyles.Pose(swingStyle, swingAct, combo, k, swingHandFull, out var pose);
            float w = pose.weight;
            if (pose.useR) { HumanIK.Blend(Rig, BodyGroup.ArmR, pose.armR, w); HumanIK.Blend(Rig, BodyGroup.ForearmR, pose.foreR, w); }
            if (pose.useL) { HumanIK.Blend(Rig, BodyGroup.ArmL, pose.armL, w); HumanIK.Blend(Rig, BodyGroup.ForearmL, pose.foreL, w); }
            if (weaponVisual != null) weaponVisual.localRotation = pose.item;
            if (dodgeTimer > 0f || Rig.Model == null) return;
            // turn and lean at the waist; in the saddle that is all the body does
            HumanIK.Blend(Rig, BodyGroup.Torso, pose.torso, w * (IsMounted || IsSwimming ? 0.5f : 1f));
            if (IsMounted || IsSwimming || !grounded) return;
            // step into it: the hips sink and move forward, the back foot stays where it was, the front one steps out
            float drop = Mathf.Max(pose.drop, Mathf.Min(0.24f, pose.lunge * 0.42f));
            Rig.Model.localPosition += new Vector3(0f, -drop, pose.lunge);
            if (drop > 0.01f || pose.lunge > 0.02f) HumanIK.Stance(Rig, drop, pose.lunge, Mathf.Clamp01(Mathf.Max(drop / 0.08f, pose.lunge / 0.08f)) * w);
        }

        static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

        // ================================================================ COMBAT / GATHER
        /// <summary>Held ATTACK auto-repeats only while there's something to hit: a wild kin in melee reach or a resource node.</summary>
        bool HoldTargetInReach(Vector3 pos)
        {
            if (HandStyles.IsRanged(heldStyle) && CombatTarget != null && CombatTarget.IsAlive && WantsShot(pos) && (CombatTarget == LockTarget || CombatTarget.Aggro)
                && Vector3.Distance(CombatTarget.Position, pos) - CombatTarget.Radius < HandStyles.Ranged(heldStyle).range) return true;
            if (CombatTarget != null && CombatTarget.IsAlive && Vector3.Distance(CombatTarget.Position, pos) - CombatTarget.Radius < meleeRange + 0.5f) return true;
            return GatherTarget != null && GatherTarget.alive;
        }

        /// <summary>
        /// Auto-tool before the swing: pull out the best tool for the tree / rock / plant in front (so the tool is in
        /// hand for the hit) and keep it out while you keep gathering; a wild kin in reach puts the weapon back at once.
        /// The held weapon returns ~2 s after the last gathering swing.
        /// </summary>
        void PrepareHandForSwing()
        {
            Vector3 pos = transform.position;
            bool foeClose = CombatTarget != null && CombatTarget.IsAlive && Vector3.Distance(CombatTarget.Position, pos) - CombatTarget.Radius < meleeRange + 0.6f;
            if (foeClose) { autoSwapTimer = 0f; return; }
            if (HandStyles.IsRanged(HandStyles.Of(Stats.HeldDef)) && Stats.Bag.Count(Stats.held) > 0 && WantsShot(pos)) { autoSwapTimer = 0f; return; }
            if (!Stats.autoTool) return;
            var node = GatherTarget != null && GatherTarget.alive ? GatherTarget : (ChunkManager.I != null ? ChunkManager.I.FindNode(pos, transform.forward, GatherReach) : null);
            if (node == null || node.Info.pickup || node.Info.tool == ToolKind.None) return;
            var kind = node.Info.tool;
            int best = Stats.BestToolPower(kind, out string bestId);
            if (bestId != null && bestId != Stats.held && best > Stats.HeldToolPower(kind)) { autoSwapItem = bestId; autoSwapTimer = 2f; }
            else if (autoSwapTimer > 0f) autoSwapTimer = 2f;   // still gathering: keep the tool out
        }

        void StartSwing()
        {
            PrepareHandForSwing();
            combo = comboWindow > 0f ? (combo + 1) % 3 : 0;
            // what is in the hand decides the move; what is in front decides what it is for
            string shown = DisplayedItem;
            var def = !string.IsNullOrEmpty(shown) && Stats.Bag.Count(shown) > 0 ? ContentDB.Item(shown) : null;
            swingStyle = HandStyles.Of(def);
            swingHandFull = def != null;
            swingAct = SwingAct.Strike;
            Vector3 pos = transform.position;
            shotTarget = null;
            if (HandStyles.IsRanged(swingStyle) && !HasAmmoFor(swingStyle)) WarnNoAmmo(swingStyle);
            if (HandStyles.IsRanged(swingStyle) && WantsShot(pos))
            {
                // a ranged weapon and something to shoot (or nothing to gather): draw on the target, if there is one in range
                swingAct = SwingAct.Shoot;
                combo = 0;
                var rt = CombatTarget;
                if (rt != null && rt.IsAlive && Vector3.Distance(rt.Position, pos) - rt.Radius < HandStyles.Ranged(swingStyle).range + 4f) shotTarget = rt;
            }
            bool foeClose = CombatTarget != null && CombatTarget.IsAlive &&
                            Vector3.Distance(CombatTarget.Position, pos) - CombatTarget.Radius < meleeRange + 0.6f + Mathf.Max(0f, HandStyles.Info(swingStyle).reach);
            if (!foeClose && swingAct != SwingAct.Shoot)
            {
                var node = GatherTarget != null && GatherTarget.alive ? GatherTarget : (ChunkManager.I != null ? ChunkManager.I.FindNode(pos, transform.forward, GatherReach) : null);
                if (node != null)
                {
                    var ni = node.Info;
                    if (ni.pickup || (ni.Plant && swingStyle != HandStyle.Sickle)) swingAct = SwingAct.Reach;        // bend down and take it
                    else if (swingStyle == HandStyle.Axe && ni.tool == ToolKind.Axe) swingAct = SwingAct.Chop;      // side-on into the trunk
                }
            }
            var info = HandStyles.Info(swingStyle, swingAct);
            swingDur = info.time * (swingTime / HandStyles.BaseTime);
            swingHitAt = swingDur * info.hit;
            comboWindow = swingDur + 0.35f;
            swingTimer = swingDur;
            swingHitDone = false;
            SynthAudio.Play(Sfx.Swing, info.pitch + combo * 0.08f, swingAct == SwingAct.Reach || swingAct == SwingAct.Shoot ? 0.2f : 0.5f);
        }

        readonly System.Collections.Generic.List<CreatureActor> swingHits = new System.Collections.Generic.List<CreatureActor>(8);

        void ResolveSwing()
        {
            Vector3 pos = transform.position;
            Vector3 fwd = transform.forward;
            if (swingAct == SwingAct.Shoot) { FireShot(); return; }
            var info = HandStyles.Info(swingStyle, swingAct);
            bool finisher = combo == 2;
            // a spear's lunge reaches further still; a glaive's sweep is wide, a thrust narrow
            float reach = meleeRange + info.reach + (finisher && swingStyle == HandStyle.Spear ? 0.5f : 0f);
            var comp = GameManager.I.Party.Companion;
            float vanguard = comp != null && comp.Sp.perk == CompanionPerk.Vanguard ? 1.15f : 1f;
            // slow weapons hit harder and quick ones lighter, by exactly their swing time: damage per second is what it always was
            float weight = info.time / HandStyles.BaseTime;
            swingHits.Clear();
            if (swingAct != SwingAct.Reach)
            {
                var all = CreatureRegistry.All;
                float nearest = float.MaxValue;
                bool lockHit = false;   // a one-foe weapon picks the locked kin out of a crowd
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    var a = all[i];
                    if (a.Mode != CreatureMode.Wild || !a.IsAlive) continue;
                    Vector3 d = a.Position - pos; d.y = 0;
                    float dist = d.magnitude - a.Radius;
                    if (dist > reach) continue;
                    if (dist > 0.3f && Vector3.Dot(d.normalized, fwd) < info.arc) continue;
                    if (info.single)
                    {
                        if (a == LockTarget) lockHit = true;
                        else if (lockHit || dist >= nearest) continue;
                        nearest = dist;
                        swingHits.Clear();
                    }
                    swingHits.Add(a);
                }
            }
            foreach (var a in swingHits)
            {
                if (a == null || !a.IsAlive) continue;
                Vector3 d = a.Position - pos; d.y = 0;
                float power = Stats.WeaponDamage * (finisher ? 1.5f : 1f) * vanguard * weight;
                WeaponHit(out var hitEl, out var hitSt, out float hitCh);   // an elemental weapon, or the Venomous ring
                CombatSystem.Apply(this, a, power, hitEl, hitSt, hitCh, d.normalized * (info.knock * (finisher ? 2f : 1f)));
            }
            if (swingHits.Count > 0)
            {
                swingHits.Clear();
                GameManager.I.HitStop((finisher ? 0.07f : 0.045f) * Mathf.Clamp(weight, 0.7f, 1.4f));
                cam.Shake((finisher ? 0.5f : 0.25f) * Mathf.Clamp(weight, 0.7f, 1.4f));
                return;
            }
            // no creature => gather the node in front
            var node = GatherTarget != null && GatherTarget.alive ? GatherTarget : (ChunkManager.I != null ? ChunkManager.I.FindNode(pos, fwd, GatherReach) : null);
            if (node != null) Gather(node);
        }

        void Gather(ResourceNode n)
        {
            if (IsMounted && MountGather(n)) return;   // the kin under you logs / quarries / forages with you
            var info = n.Info;
            int power;
            if (info.Plant)
            {
                // plants: hands work (power 2); a sickle cuts harder, yields more and sweeps an arc
                int tier = Stats.HeldToolPower(ToolKind.Sickle);
                if (Stats.autoTool)
                {
                    int best = Stats.BestToolPower(ToolKind.Sickle, out string bestId);
                    if (best > tier && bestId != null)
                    {
                        tier = best;
                        if (bestId != Stats.held) { autoSwapItem = bestId; autoSwapTimer = 2f; }
                    }
                }
                if (tier >= 2) { SweepHarvest(n, 2 + tier, 0.5f * (tier - 1), tier); return; }
                HitNode(n, 2, 0f);
                return;
            }
            if (info.pickup) { PickUp(n); return; }
            if (info.tool == ToolKind.None) power = 2;
            else
            {
                power = Stats.HeldToolPower(info.tool);
                if (Stats.autoTool)
                {
                    // contextual auto-use: swing the best valid tool you own, then go back to what you held
                    int best = Stats.BestToolPower(info.tool, out string bestId);
                    if (best > power && bestId != null)
                    {
                        power = best;
                        if (bestId != Stats.held) { autoSwapItem = bestId; autoSwapTimer = 2f; }
                    }
                }
            }
            if (power < info.hardness)
            {
                string toolName = info.tool == ToolKind.Axe ? "Axe" : "Pick";
                int owned = Stats.BestToolPower(info.tool, out string ownedId);
                if (!Stats.autoTool && owned >= info.hardness)
                    GameEvents.Warn($"Hold your {ContentDB.ItemName(ownedId)} (hotbar) - Auto-tool is off");
                else
                    GameEvents.Warn($"{info.label} needs {PropCatalog.ToolTierName(info.hardness, info.tool)}");
                SynthAudio.Play(Sfx.Clunk, 0.8f, 0.6f);
                ChunkManager.I.Wobble(n);
                return;
            }
            float dmg = power;
            if (info.tool != ToolKind.None && power <= 1)
            {
                // bare hands: trees are slow, rocks very slow - tools matter
                dmg = info.tool == ToolKind.Pick ? BareHandMine : BareHandChop;
                if (Time.time - lastBareHandHint > 45f)
                {
                    lastBareHandHint = Time.time;
                    GameEvents.Say(info.tool == ToolKind.Pick ? "Mining by hand is very slow - craft a Stone Pick (Craft tab). Loose stones on the ground are quicker."
                                                              : "Chopping by hand is slow - craft a Stone Hatchet (Craft tab). Fallen sticks on the ground are quicker.", new Color(1f, 0.9f, 0.6f));
                }
            }
            HitNode(n, dmg, 0f);
        }

        float toxicTick, toxicHint = -99f;
        const float BareHandChop = 0.4f, BareHandMine = 0.25f;
        float lastBareHandHint = -99f;

        /// <summary>The node the player last gathered from, and when: the companion's cue to pitch in (CreatureActor.Gather).</summary>
        public ResourceNode LastGatherNode { get; private set; }
        public float LastGatherTime { get; private set; } = -99f;

        /// <summary>A helping companion's hit on a node. Yields go to the player like any other gathering hit.</summary>
        public void KinHitNode(ResourceNode n, float power) { if (n != null && n.alive) HitNode(n, power, 0f, true); }
        public void KinPickUp(ResourceNode n) { if (n != null && n.alive && n.Info.pickup) PickUp(n, true); }

        /// <summary>Ground pickups: one tap, a couple of items.</summary>
        void PickUp(ResourceNode n, bool byKin = false)
        {
            if (n.type == PropType.PearlOyster && n.pos.y + 0.12f < Tides.Surface(n.pos))
            {
                // shut tight while the sea covers it
                if (!byKin) GameEvents.Say("The oyster is clamped shut under the water. Come back at low tide.", new Color(0.56f, 0.89f, 1f));
                return;
            }
            if (!byKin) { LastGatherNode = n; LastGatherTime = Time.time; }
            var info = n.Info;
            Vector3 p = n.pos + Vector3.up * 0.3f;
            int amount = Mathf.Max(1, RoundRandom(Random.Range(info.pickupMin, info.pickupMax + 1) * (1f + Stats.GatherBonus) * WorldRules.Current.gatherRate));
            GameManager.I.GiveItem(info.item, amount, p);
            if (!string.IsNullOrEmpty(info.bonusItem) && Random.value < info.bonusChance) GameManager.I.GiveItem(info.bonusItem, 1, p);
            n.hp = 0;
            Fx.Debris(p, ContentDB.Item(info.item)?.iconColor ?? Color.white, 5);
            SynthAudio.Play(Sfx.Pickup, Random.Range(0.95f, 1.1f), 0.7f);
            ChunkManager.I.Deplete(n);
        }

        /// <summary>Applies one gathering hit: damage, feedback, yield (+tool/perk/skill bonus), depletion.</summary>
        void HitNode(ResourceNode n, float power, float toolYield, bool byKin = false)
        {
            if (!byKin) { LastGatherNode = n; LastGatherTime = Time.time; }
            var info = n.Info;
            n.hp -= power;
            // tool nodes: yield scales with damage dealt, so a node gives the same total whatever you hit it with (tools are just faster)
            float share = info.tool == ToolKind.Axe || info.tool == ToolKind.Pick ? power / 2f : 1f;
            ChunkManager.I.Wobble(n);
            Vector3 fxPos = n.pos + Vector3.up * (PropCatalog.IsTree(n.type) && n.type != PropType.Spinepalm ? 1.2f : 0.6f);
            var itemCol = ContentDB.Item(info.item)?.iconColor ?? Color.white;
            Fx.Debris(fxPos, itemCol, 6);
            SynthAudio.Play(info.tool == ToolKind.Pick ? Sfx.Mine : (info.tool == ToolKind.Axe ? Sfx.Chop : Sfx.Pluck), Random.Range(0.9f, 1.1f), byKin ? 0.5f : 0.8f);
            if (!byKin) cam.Shake(0.12f);

            // yield per hit (+ tool, companion perk, skill)
            float mul = (1f + Stats.GatherBonus + PerkBonus(n.type) + toolYield) * WorldRules.Current.gatherRate * (info.yieldMul > 0f ? info.yieldMul : 1f);
            int amount = RoundRandom(mul * share);
            bool final = n.hp <= 0.001f;
            if (final) amount += RoundRandom(mul * 1.5f);
            GameManager.I.GiveItem(info.item, amount, fxPos);
            if (!string.IsNullOrEmpty(info.bonusItem) && Random.value < info.bonusChance * (final ? 2f : 0.5f * share))
                GameManager.I.GiveItem(info.bonusItem, 1, fxPos);
            if (final)
            {
                // felled trees always shed some bark fiber
                if (info.finalFiberMax > 0)
                {
                    int fib = RoundRandom(Random.Range(info.finalFiberMin, info.finalFiberMax + 1) * (1f + Stats.GatherBonus));
                    if (fib > 0) GameManager.I.GiveItem("fiber", fib, fxPos);
                }
                if (Stats.HasEffect(UniqueEffect.Magpie) && Random.value < 0.2f)
                    GameManager.I.GiveItem(!string.IsNullOrEmpty(info.bonusItem) ? info.bonusItem : info.item, 1, fxPos);
                if (Stats.HasEffect(UniqueEffect.GreenThumb) && Random.value < 0.1f) GameManager.I.GiveItem("tether_seed", 1, fxPos);
                ChunkManager.I.Deplete(n);
                Fx.Burst(fxPos, itemCol, 12, 4f, 0.3f, 0.6f);
                GameManager.I.AddPlayerXp(2);
            }
        }

        float PerkBonus(PropType t)
        {
            var comp = GameManager.I.Party.Companion;
            if (comp == null) return 0f;
            switch (comp.Sp.perk)
            {
                case CompanionPerk.LumberBoost: return PropCatalog.IsTree(t) ? 0.5f : 0f;
                case CompanionPerk.StoneBoost: return t == PropType.Rock || t == PropType.OreRock ? 0.5f : 0f;
                case CompanionPerk.OreSense: return t == PropType.OreRock || t == PropType.CinderRock || t == PropType.CrystalCluster || PropCatalog.IsOuterOre(t) ? 0.5f : 0f;
                case CompanionPerk.ForageBoost: return t == PropType.BerryBush || t == PropType.FiberPlant || (t >= PropType.GrainStalks && t <= PropType.MintPatch) || (PropCatalog.IsRegional(t) && t != PropType.ObsidianSpire) ? 0.5f : 0f;
                case CompanionPerk.Prospector: return 0.35f;
            }
            return 0f;
        }

        static int RoundRandom(float v)
        {
            int i = Mathf.FloorToInt(v);
            return i + (Random.value < v - i ? 1 : 0);
        }

        public void ReceiveHit(in HitInfo hit)
        {
            if (dead || invuln > 0f) return;
            if (hit.attacker != null) { LastAttacker = hit.attacker; LastAttackedTime = Time.time; }
            // Mirage: now and then the blow lands where you only seemed to be
            if (hit.damage > 0f && Stats.HasEffect(UniqueEffect.Mirage) && Random.value < 0.12f)
            {
                HUD.DamageNumber(transform.position + Vector3.up * 2.4f, "MISS", new Color(1f, 0.9f, 0.6f), 0.9f);
                return;
            }
            // Guard: a companion with its shield planted takes this one for you
            var guard = GameManager.I.Party.Companion;
            if (guard != null && guard.TakeGuardFor(this, hit)) return;
            Damage(hit.damage * Stats.DamageTakenMul, true);
            if (hit.status != StatusType.None && !Status.Active && !ImmuneTo(hit.status) && Random.value < hit.statusChance * 0.7f)
            {
                Status = new StatusState { type = hit.status, time = StatusState.Duration(hit.status) * 0.7f, tick = 1f };
                HUD.DamageNumber(transform.position + Vector3.up * 2.4f, hit.status.ToString().ToUpperInvariant(), StatusState.Color(hit.status), 0.8f);
            }
            // Thornweave: reflect part of the damage at the attacker
            if (Stats.HasEffect(UniqueEffect.Thornweave) && hit.attacker is CreatureActor ca && ca.IsAlive)
            {
                float back = Mathf.Max(1f, hit.damage * 0.25f);
                ca.ReceiveHit(new HitInfo { attacker = this, damage = back, element = Element.Flora, effectiveness = 1f });
                HUD.DamageNumber(ca.Position + Vector3.up * (ca.Radius + 0.8f), Mathf.RoundToInt(back).ToString(), new Color(0.6f, 0.9f, 0.4f), 0.8f);
            }
            if (hit.knockback.sqrMagnitude > 0f) hVel += hit.knockback * 0.6f;
            Rig.Flash(new Color(1f, 0.3f, 0.3f));
            cam.Shake(0.35f);
        }

        float secondWindAt = -999f;

        void Damage(float amount, bool fromHit)
        {
            if (dead || Dev.God) return;
            Stats.hp -= amount;
            lastHurt = Time.time;
            if (fromHit) SynthAudio.Play(Sfx.Hurt, Random.Range(0.95f, 1.05f));
            if (Stats.hp <= 0f && WorldRules.Current.cantFaint)
            {
                // Can't-faint worlds: you get straight back up, with a moment nothing can touch you
                Stats.hp = Stats.MaxHp * 0.6f;
                invuln = Mathf.Max(invuln, 3f);
                Fx.Sparkle(transform.position + Vector3.up, new Color(0.6f, 1f, 0.8f), 18, 0.8f);
                HUD.DamageNumber(transform.position + Vector3.up * 2.4f, "BACK UP!", new Color(0.6f, 1f, 0.8f), 1.1f);
                SynthAudio.Play(Sfx.Summon, 1.3f, 0.6f);
                return;
            }
            if (Stats.hp <= 0f && Stats.HasEffect(UniqueEffect.SecondWind) && Time.time - secondWindAt > 180f)
            {
                // Second Wind: once every three minutes the blow that would have downed you does not
                secondWindAt = Time.time;
                Stats.hp = Stats.MaxHp * 0.3f;
                invuln = Mathf.Max(invuln, 1.5f);
                Fx.Sparkle(transform.position + Vector3.up, new Color(0.4f, 0.9f, 0.75f), 18, 0.8f);
                HUD.DamageNumber(transform.position + Vector3.up * 2.4f, "SECOND WIND", new Color(0.5f, 1f, 0.85f), 1.1f);
                SynthAudio.Play(Sfx.Summon, 1.2f, 0.6f);
                return;
            }
            if (Stats.hp <= 0f)
            {
                Stats.hp = 0f;
                dead = true;
                GameManager.I.OnPlayerDied();
            }
        }

        public void Heal(float v) { if (!dead) Stats.hp = Mathf.Min(Stats.MaxHp, Stats.hp + v); }

        public void Revive(Vector3 at)
        {
            dead = false;
            Status = default;
            ClearMarks();
            Stats.hp = Stats.MaxHp * 0.6f;
            Stats.stamina = Stats.MaxStamina;
            Stats.hunger = Mathf.Max(Stats.hunger, 40f);
            Teleport(at, transform.eulerAngles.y);
        }

        /// <summary>A sling, bow or crossbow is in your hand (it shoots only at a locked kin).</summary>
        public bool RangedInHand => HandStyles.IsRanged(HandStyles.Of(Stats.HeldDef)) && Stats.Bag.Count(Stats.held) > 0;

        string ComputeContextAction(Vector3 pos)
        {
            if (WantsShot(pos)) return "SHOOT";
            if (CombatTarget != null && Vector3.Distance(CombatTarget.Position, pos) - CombatTarget.Radius < meleeRange + 1.5f) return "ATTACK";
            if (GatherTarget != null && GatherTarget.alive)
            {
                switch (GatherTarget.Info.tool)
                {
                    case ToolKind.Axe: return "CHOP";
                    case ToolKind.Pick: return "MINE";
                    case ToolKind.Sickle:
                        return Stats.HeldToolPower(ToolKind.Sickle) > 1 || (Stats.autoTool && Stats.BestToolPower(ToolKind.Sickle, out _) > 1) ? "CUT" : "PICK";
                    default: return GatherTarget.Info.pickup ? "PICK UP" : "PICK";
                }
            }
            return "ATTACK";
        }

        /// <summary>Hotbar / menu: hold an item (tap the held one again to go bare-handed).</summary>
        public void ToggleHold(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (Stats.held == id) Stats.Hold("");
            else if (!Stats.Hold(id)) { GameEvents.Warn("You don't have that anymore."); return; }
            autoSwapTimer = 0f;
            SynthAudio.Play(Sfx.UiClick, 0.9f);
            GameEvents.RaiseInventoryChanged();
        }

        // ================================================================ HELD ITEM VISUAL
        void UpdateWeaponVisual()
        {
            string want = DisplayedItem ?? "";
            if (want.Length > 0 && Stats.Bag.Count(want) <= 0) want = "";
            ItemDef readySeed = Aiming ? GameManager.I.Capture.SelectedSeed(Stats) : null;
            if (readySeed != null) want = readySeed.id;   // seed in hand while ready
            if (want == weaponVisualId) return;
            weaponVisualId = want;
            heldStyle = readySeed != null || string.IsNullOrEmpty(want) ? HandStyle.Fists : HandStyles.Of(ContentDB.Item(want));
            if (weaponVisual != null) Destroy(weaponVisual.gameObject);
            weaponVisual = null;
            if (string.IsNullOrEmpty(want)) return;
            var arm = Rig.GetGroup(BodyGroup.ForearmR) ?? Rig.GetGroup(BodyGroup.ArmR);   // in the palm
            if (arm == null) return;
            var mesh = readySeed != null ? SeedHandMesh(readySeed) : HeldItemMesh.Build(want);
            if (mesh == null) return;
            var go = new GameObject("HeldItem");
            go.transform.SetParent(arm, false);
            go.transform.localPosition = PlayerLook.Grip;
            go.transform.localRotation = readySeed != null ? Quaternion.identity : HandStyles.Carry(heldStyle);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            weaponVisual = go.transform;
        }
    }

    /// <summary>Procedural in-hand meshes for every tool and weapon (cached).</summary>
    public static class HeldItemMesh
    {
        static readonly System.Collections.Generic.Dictionary<string, Mesh> cache = new System.Collections.Generic.Dictionary<string, Mesh>();

        public static Mesh Build(string id)
        {
            if (cache.TryGetValue(id, out var m)) return m;
            var d = ContentDB.Item(id);
            if (d == null) return null;
            var mb = new MeshBuilder();
            Color wood = new Color(0.55f, 0.38f, 0.22f), stone = new Color(0.62f, 0.62f, 0.66f), metal = new Color(0.86f, 0.88f, 0.95f);
            Color head = d.toolPower >= 4 ? Color.Lerp(d.iconColor, metal, 0.25f) : d.toolPower >= 3 || id.StartsWith("iron") ? metal : stone;
            Color tint = d.iconColor, tintLt = Color.Lerp(d.iconColor, Color.white, 0.45f), tintDk = Color.Lerp(d.iconColor, Color.black, 0.35f), grip = new Color(0.3f, 0.22f, 0.18f);
            switch (id)
            {
                case "wood_club":
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.35f), new Vector3(90, 0, 0), new Vector3(0.08f, 0.6f, 0.08f)), wood, new Color(0.62f, 0.44f, 0.28f), 6, 1.8f, true);
                    break;
                case "stone_spear":
                    // held a third of the way up the shaft, so the butt trails behind the hand
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.28f), new Vector3(90, 0, 0), new Vector3(0.05f, 1.36f, 0.05f)), wood, wood, 5, 1f, true);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0f, 1.08f), new Vector3(90, 0, 0), new Vector3(0.13f, 0.28f, 0.13f)), stone, 5);
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.93f), new Vector3(90, 0, 0), new Vector3(0.07f, 0.08f, 0.07f)), new Color(0.75f, 0.68f, 0.5f), 5);
                    break;
                case "bogsteel_glaive":
                    // a long pole with a heavy curved blade (edge toward -Y) and a back-spike
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.3f), new Vector3(90, 0, 0), new Vector3(0.055f, 1.5f, 0.055f)), wood, wood, 5, 1f, true);
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 1.02f), new Vector3(90, 0, 0), new Vector3(0.09f, 0.1f, 0.09f)), tintDk, 6);
                    for (int k = 0; k < 5; k++)
                    {
                        float t = k / 4f;
                        mb.Box(MeshBuilder.TRS(new Vector3(0, -0.05f - t * t * 0.1f, 1.12f + t * 0.42f), new Vector3(t * 22f, 0, 0), new Vector3(0.035f, 0.2f - t * 0.1f, 0.14f)), tint, tintLt);
                    }
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0.1f, 1.08f), Vector3.zero, new Vector3(0.05f, 0.16f, 0.05f)), tintDk, 4);
                    break;
                case "saltglass_saber":
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.1f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.2f, 0.05f)), grip, 5, 1f, true);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.03f, 0.21f), Vector3.zero, new Vector3(0.06f, 0.2f, 0.04f)), tintDk);
                    // the blade curves back (+Y) toward the tip; the edge is the -Y side
                    for (int k = 0; k < 6; k++)
                    {
                        float t = k / 5f;
                        mb.Box(MeshBuilder.TRS(new Vector3(0, t * t * 0.14f, 0.3f + t * 0.62f), new Vector3(-t * 22f, 0, 0), new Vector3(0.03f, 0.1f - t * 0.045f, 0.15f)), tint, tintLt);
                    }
                    break;
                case "sunsteel_blade":
                    // a broad two-edged sword with a wide guard
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.1f), new Vector3(90, 0, 0), new Vector3(0.055f, 0.22f, 0.055f)), grip, 5, 1f, true);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, -0.03f), Vector3.zero, new Vector3(0.09f, 0.09f, 0.09f)), tint, 6, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.23f), Vector3.zero, new Vector3(0.06f, 0.32f, 0.05f)), tintDk);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.62f), Vector3.zero, new Vector3(0.045f, 0.15f, 0.74f)), tint, tintLt);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0f, 1.06f), new Vector3(90, 0, 0), new Vector3(0.045f, 0.16f, 0.15f)), tintLt, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.6f), Vector3.zero, new Vector3(0.05f, 0.03f, 0.66f)), new Color(1f, 0.95f, 0.7f, 0f));
                    break;
                case "blightsteel_fang":
                    // short, with the point hooking over toward the edge (-Y)
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.07f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.16f, 0.05f)), grip, 5, 1f, true);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.16f), Vector3.zero, new Vector3(0.06f, 0.14f, 0.035f)), tintDk);
                    for (int k = 0; k < 5; k++)
                    {
                        float t = k / 4f;
                        mb.Box(MeshBuilder.TRS(new Vector3(0, -t * t * 0.13f, 0.22f + t * 0.3f), new Vector3(t * 38f, 0, 0), new Vector3(0.03f, 0.09f - t * 0.045f, 0.11f)), tint, tintLt);
                    }
                    break;
                case "starsteel_edge":
                    // long and narrow, with a line of light down the middle
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.1f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.22f, 0.05f)), grip, 5, 1f, true);
                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, 0f, 0.2f), new Vector3(90, 90, 0), new Vector3(0.12f, 0.12f, 0.12f)), tint, tintLt);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.72f), Vector3.zero, new Vector3(0.035f, 0.1f, 0.96f)), tint, tintLt);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0f, 1.27f), new Vector3(90, 0, 0), new Vector3(0.035f, 0.14f, 0.1f)), tintLt, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.72f), Vector3.zero, new Vector3(0.042f, 0.025f, 0.94f)), new Color(1f, 1f, 1f, 0f));
                    break;
                case "sling":
                    // a cord from the hand with a leather pouch and its stone at the end
                    mb.Box(MeshBuilder.TRS(new Vector3(0.012f, 0f, 0.2f), Vector3.zero, new Vector3(0.014f, 0.014f, 0.4f)), tintLt);
                    mb.Box(MeshBuilder.TRS(new Vector3(-0.012f, 0f, 0.2f), Vector3.zero, new Vector3(0.014f, 0.014f, 0.4f)), tintLt);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, 0.43f), Vector3.zero, new Vector3(0.13f, 0.1f, 0.16f)), tintDk, 6, 4);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0.03f, 0.43f), Vector3.zero, new Vector3(0.09f, 0.08f, 0.1f)), stone, 6, 4);
                    break;
                case "short_bow":
                case "sunsteel_longbow":
                case "starsteel_greatbow":
                case "rimeshard_bow":
                {
                    // held by the middle with the limbs along Z (HandStyles stands it upright); the belly bows away from
                    // the archer (-Y) and the string runs tip to tip on the archer's side (+Y)
                    float half = id == "short_bow" ? 0.52f : id == "rimeshard_bow" ? 0.6f : id == "sunsteel_longbow" ? 0.66f : 0.78f;
                    float bend = half * 0.3f;
                    bool fine = id != "short_bow";
                    Color limb = fine ? tint : wood, limbLt = fine ? tintLt : new Color(0.68f, 0.5f, 0.3f);
                    const int Seg = 7;
                    for (int side = -1; side <= 1; side += 2)
                        for (int k = 0; k < Seg; k++)
                        {
                            float tm = (k + 0.5f) / Seg;
                            float y = bend * tm * tm;                                                // the grip is in the hand, the tips come back
                            float slope = Mathf.Atan2(2f * bend * tm, half) * Mathf.Rad2Deg;         // limbs sweep back toward the tips
                            mb.Box(MeshBuilder.TRS(new Vector3(0, y, side * tm * half), new Vector3(-side * slope, 0, 0), new Vector3(0.05f - tm * 0.02f, 0.045f - tm * 0.015f, half / Seg * 1.25f)), limb, limbLt);
                        }
                    float tipY = bend;
                    mb.Box(MeshBuilder.TRS(new Vector3(0, tipY, 0), Vector3.zero, new Vector3(0.008f, 0.008f, half * 2f)), new Color(0.92f, 0.9f, 0.82f));
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0f), new Vector3(90, 0, 0), new Vector3(0.065f, 0.2f, 0.065f)), grip, 6, 1f, true);
                    if (fine)
                    {
                        // metal tips, and a glowing nock for the greatbow
                        mb.Cone(MeshBuilder.TRS(new Vector3(0, tipY, half + 0.04f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.12f, 0.05f)), tintLt, 4);
                        mb.Cone(MeshBuilder.TRS(new Vector3(0, tipY, -half - 0.04f), new Vector3(-90, 0, 0), new Vector3(0.05f, 0.12f, 0.05f)), tintLt, 4);
                        if (id == "starsteel_greatbow") mb.Sphere(MeshBuilder.TRS(new Vector3(0, tipY, 0f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f)), new Color(1f, 1f, 1f, 0f), 6, 4);
                        if (id == "rimeshard_bow")
                        {
                            // shards of rime standing off each limb, lit from inside
                            for (int side = -1; side <= 1; side += 2)
                                for (int k = 1; k <= 3; k++)
                                {
                                    float tm = k / 4f;
                                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, bend * tm * tm - 0.02f, side * tm * half), new Vector3(180, 0, 0), new Vector3(0.05f, 0.14f - k * 0.02f, 0.05f)), tintLt, new Color(1f, 1f, 1f, 0f));
                                }
                        }
                    }
                    break;
                }
                case "ingot_crossbow":
                case "bogsteel_crossbow":
                case "blightcoil_crossbow":
                {
                    // stock along Z (it points where it shoots), the prod across the front, a stirrup on the nose
                    bool heavy = id != "ingot_crossbow";
                    if (id == "blightcoil_crossbow")
                    {
                        // a coil of blight ore round the stock, and a sick green light where the bolt sits
                        for (int k = 0; k < 4; k++) mb.Cylinder(MeshBuilder.TRS(new Vector3(0, -0.01f, 0.02f + k * 0.09f), new Vector3(90, 0, 0), new Vector3(0.1f, 0.03f, 0.11f)), tintDk, 6);
                        mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0.05f, 0.2f), Vector3.zero, new Vector3(0.06f, 0.06f, 0.06f)), new Color(0.72f, 1f, 0.25f, 0f), 6, 4);
                    }
                    Color prod = heavy ? tint : metal, prodLt = heavy ? tintLt : Color.white;
                    float span = heavy ? 0.42f : 0.34f;
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.01f, 0.22f), Vector3.zero, new Vector3(0.06f, 0.07f, 0.74f)), wood, new Color(0.62f, 0.44f, 0.28f));
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.06f, -0.12f), new Vector3(-18, 0, 0), new Vector3(0.055f, 0.13f, 0.2f)), wood);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        mb.Box(MeshBuilder.TRS(new Vector3(side * span * 0.5f, 0.02f, 0.5f), new Vector3(0, side * 16f, 0), new Vector3(span, 0.03f, 0.05f)), prod, prodLt);
                        // string from the tip back to the nut
                        Vector3 tip = new Vector3(side * span * 0.96f, 0.03f, 0.44f), nut = new Vector3(0f, 0.03f, 0.2f);
                        Vector3 mid = (tip + nut) * 0.5f, dir = nut - tip;
                        mb.Box(Matrix4x4.TRS(mid, Quaternion.LookRotation(dir.normalized, Vector3.up), new Vector3(0.008f, 0.008f, dir.magnitude)), new Color(0.92f, 0.9f, 0.82f));
                    }
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0.03f, 0.36f), Vector3.zero, new Vector3(0.022f, 0.022f, 0.36f)), new Color(0.75f, 0.68f, 0.5f));   // the bolt in the groove
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0.03f, 0.58f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.1f, 0.05f)), prodLt, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.02f, 0.63f), Vector3.zero, new Vector3(0.1f, 0.012f, 0.07f)), prod);
                    break;
                }
                case "honey_mace":
                    // a stout stick with a wild hive bound to the end: a fat comb, cells standing out of it, a drip
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.28f), new Vector3(90, 0, 0), new Vector3(0.065f, 0.6f, 0.065f)), wood, new Color(0.62f, 0.44f, 0.28f), 6, 1f, true);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, 0.68f), Vector3.zero, new Vector3(0.28f, 0.26f, 0.32f)), tint, tintDk, 8, 6);
                    for (int k = 0; k < 7; k++)
                    {
                        float a = k * 51.4f * Mathf.Deg2Rad;
                        mb.Sphere(MeshBuilder.TRS(new Vector3(Mathf.Cos(a) * 0.115f, Mathf.Sin(a) * 0.105f, 0.68f + (k % 2 == 0 ? 0.07f : -0.06f)), Vector3.zero, new Vector3(0.095f, 0.095f, 0.095f)), tintLt, 6, 4);
                    }
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.53f), new Vector3(90, 0, 0), new Vector3(0.11f, 0.05f, 0.11f)), new Color(0.75f, 0.68f, 0.5f), 6);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0.02f, -0.15f, 0.72f), Vector3.zero, new Vector3(0.05f, 0.09f, 0.05f)), new Color(1f, 0.82f, 0.3f, 0f), 6, 4);
                    break;
                case "tidefang_harpoon":
                    // long shaft held a third of the way up; a flat saltglass head with two barbs swept back, a float on the butt
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.28f), new Vector3(90, 0, 0), new Vector3(0.05f, 1.36f, 0.05f)), wood, wood, 5, 1f, true);
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.92f), new Vector3(90, 0, 0), new Vector3(0.075f, 0.12f, 0.075f)), tintDk, 6);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0f, 1.14f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.36f, 0.16f)), tint, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.09f, 1.0f), new Vector3(38, 0, 0), new Vector3(0.03f, 0.045f, 0.2f)), tintLt);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0.09f, 1.0f), new Vector3(-38, 0, 0), new Vector3(0.03f, 0.045f, 0.2f)), tintLt);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, -0.43f), Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f)), new Color(1f, 0.48f, 0.54f), 6, 4);
                    break;
                case "cinder_cleaver":
                    // a heavy blade that widens toward a squared tip; the edge (-Y) still glows
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.1f), new Vector3(90, 0, 0), new Vector3(0.055f, 0.22f, 0.055f)), grip, 5, 1f, true);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.23f), Vector3.zero, new Vector3(0.07f, 0.22f, 0.05f)), tintDk);
                    for (int k = 0; k < 5; k++)
                    {
                        float t = k / 4f;
                        mb.Box(MeshBuilder.TRS(new Vector3(0, -0.02f - t * 0.035f, 0.33f + t * 0.5f), Vector3.zero, new Vector3(0.045f, 0.15f + t * 0.11f, 0.135f)), tint, tintLt);
                    }
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.145f, 0.6f), new Vector3(-4, 0, 0), new Vector3(0.05f, 0.03f, 0.62f)), new Color(1f, 0.5f, 0.12f, 0f));
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0.07f, 0.62f), Vector3.zero, new Vector3(0.05f, 0.04f, 0.1f)), tintDk);
                    break;
                case "abyss_edge":
                    // black glass, long and keen, a geode core in the guard and violet light along the edge
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.1f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.22f, 0.05f)), grip, 5, 1f, true);
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, 0.23f), Vector3.zero, new Vector3(0.1f, 0.13f, 0.08f)), new Color(0.7f, 0.5f, 1f, 0f), 7, 5);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.23f), Vector3.zero, new Vector3(0.05f, 0.26f, 0.06f)), tintDk);
                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, 0f, 0.27f), new Vector3(90, 0, 0), new Vector3(0.07f, 1.05f, 0.12f)), new Color(0.12f, 0.08f, 0.18f), new Color(0.35f, 0.25f, 0.5f));
                    mb.Box(MeshBuilder.TRS(new Vector3(0, -0.035f, 0.72f), Vector3.zero, new Vector3(0.02f, 0.012f, 0.78f)), new Color(0.75f, 0.55f, 1f, 0f));
                    break;
                case "mirefang":
                    // a wavy knife: the blade snakes side to side as it narrows, a green bead in the pommel
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.07f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.16f, 0.05f)), grip, 5, 1f, true);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.16f), Vector3.zero, new Vector3(0.06f, 0.15f, 0.035f)), tintDk);
                    for (int k = 0; k < 7; k++)
                    {
                        float t = k / 6f;
                        mb.Box(MeshBuilder.TRS(new Vector3(0, Mathf.Sin(t * 7.2f) * 0.028f, 0.2f + t * 0.38f), new Vector3(Mathf.Cos(t * 7.2f) * -18f, 0, 0), new Vector3(0.03f, 0.085f - t * 0.05f, 0.075f)), tint, tintLt);
                    }
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, -0.03f), Vector3.zero, new Vector3(0.07f, 0.07f, 0.07f)), new Color(0.72f, 1f, 0.3f, 0f), 6, 4);
                    break;
                case "sunspike_glaive":
                    // a pole with a crescent of sun ore (edge toward -Y) round a small bright disc
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.3f), new Vector3(90, 0, 0), new Vector3(0.055f, 1.5f, 0.055f)), wood, wood, 5, 1f, true);
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 1.04f), new Vector3(90, 0, 0), new Vector3(0.09f, 0.1f, 0.09f)), tintDk, 6);
                    for (int k = 0; k < 7; k++)
                    {
                        float a = (-70f + k * 23f) * Mathf.Deg2Rad;
                        mb.Box(MeshBuilder.TRS(new Vector3(0, -Mathf.Cos(a) * 0.2f + 0.04f, 1.3f + Mathf.Sin(a) * 0.3f), new Vector3(-a * Mathf.Rad2Deg, 0, 0), new Vector3(0.035f, 0.11f - Mathf.Abs(k - 3) * 0.022f, 0.13f)), tint, tintLt);
                    }
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0.03f, 1.3f), Vector3.zero, new Vector3(0.05f, 0.13f, 0.13f)), new Color(1f, 0.95f, 0.6f, 0f), 8, 5);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0f, 1.66f), new Vector3(90, 0, 0), new Vector3(0.04f, 0.16f, 0.06f)), tintLt, 4);
                    break;
                case "starfall_blade":
                    // long, slim and bright: a star for a guard, a seam of light down the blade, a chip of it trailing at the tip
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.1f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.22f, 0.05f)), grip, 5, 1f, true);
                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, 0f, 0.22f), new Vector3(0, 0, 0), new Vector3(0.07f, 0.2f, 0.07f)), tint, tintLt);
                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, 0f, 0.22f), new Vector3(180, 0, 0), new Vector3(0.07f, 0.2f, 0.07f)), tint, tintLt);
                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, 0f, 0.22f), new Vector3(0, 0, 90), new Vector3(0.06f, 0.12f, 0.06f)), tintLt, Color.white);
                    mb.Crystal(MeshBuilder.TRS(new Vector3(0, 0f, 0.22f), new Vector3(0, 0, -90), new Vector3(0.06f, 0.12f, 0.06f)), tintLt, Color.white);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.76f), Vector3.zero, new Vector3(0.035f, 0.11f, 1.0f)), tint, tintLt);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0f, 1.33f), new Vector3(90, 0, 0), new Vector3(0.035f, 0.16f, 0.11f)), tintLt, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.76f), Vector3.zero, new Vector3(0.042f, 0.03f, 0.98f)), new Color(1f, 1f, 1f, 0f));
                    mb.Sphere(MeshBuilder.TRS(new Vector3(0, 0f, -0.04f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f)), new Color(0.85f, 0.8f, 1f, 0f), 6, 4);
                    break;
                case "iron_blade":
                    mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.12f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.2f, 0.05f)), new Color(0.35f, 0.25f, 0.2f), 5, 1f, true);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.22f), Vector3.zero, new Vector3(0.22f, 0.04f, 0.05f)), metal);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.6f), Vector3.zero, new Vector3(0.05f, 0.1f, 0.7f)), metal);
                    break;
                default:
                    if (d.category == ItemCategory.Tool)
                    {
                        // handle + head: axe = wedge blade on the swing side (-Y is the leading edge when the arm comes down), pick = two-pointed bar
                        mb.Cylinder(MeshBuilder.TRS(new Vector3(0, 0f, 0.3f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.62f, 0.05f)), wood, wood, 5, 1f, true);
                        if (d.toolKind == ToolKind.Axe)
                            mb.Box(MeshBuilder.TRS(new Vector3(0, -0.1f, 0.58f), Vector3.zero, new Vector3(0.05f, 0.22f, 0.16f)), head, head);
                        else if (d.toolKind == ToolKind.Sickle)
                        {
                            // curved blade: short segments along an arc
                            for (int k = 0; k < 6; k++)
                            {
                                float a = k * 26f * Mathf.Deg2Rad;
                                var p = new Vector3(0, -Mathf.Sin(a) * 0.26f, 0.6f + (1f - Mathf.Cos(a)) * -0.1f + Mathf.Cos(a) * 0.06f);
                                mb.Box(MeshBuilder.TRS(p, new Vector3(k * 26f, 0, 0), new Vector3(0.03f, 0.06f, 0.13f)), head, head);
                            }
                        }
                        else
                        {
                            mb.Cone(MeshBuilder.TRS(new Vector3(0, 0.12f, 0.58f), Vector3.zero, new Vector3(0.07f, 0.24f, 0.07f)), head, 5);
                            mb.Cone(MeshBuilder.TRS(new Vector3(0, -0.12f, 0.58f), new Vector3(180, 0, 0), new Vector3(0.07f, 0.24f, 0.07f)), head, 5);
                        }
                    }
                    else mb.Box(MeshBuilder.TRS(new Vector3(0, 0f, 0.4f), Vector3.zero, new Vector3(0.06f, 0.08f, 0.7f)), d.iconColor);
                    break;
            }
            m = mb.ToMesh("held_" + id);
            cache[id] = m;
            return m;
        }
    }
}
