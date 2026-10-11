using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Signature skills on the kin's side (see SkillEffects for the numbers and the menu text): the timers a move
    /// leaves on its user or its target, the moves that need more than "hit the target once" (the line cut, the three
    /// jabs, the copy, the self-buffs), what a braced or guarding kin does to a hit, and how the AI decides when a
    /// move that does no damage is worth using.
    /// </summary>
    public partial class CreatureActor
    {
        float stillTime, busyTimer, effectFx;
        float guardUntil, braceUntil, hexUntil, tetherUntil, tetherRadius, anthemUntil, wrathUntil, rallyUntil;
        Vector3 tetherPos;
        AbilityDef braceAbility, flurryAbility, lastUsed;
        float braceBoost, flurryBoost, flurryTimer;
        int flurryLeft, flurrySlot, plundered;
        ICombatant flurryTarget;
        Decoy decoy;
        // who is playing / howling for each side (index = Team)
        static readonly CreatureActor[] anthemBy = new CreatureActor[2], rallyBy = new CreatureActor[2];

        void ResetEffects()
        {
            stillTime = busyTimer = 0f;
            guardUntil = braceUntil = hexUntil = tetherUntil = anthemUntil = wrathUntil = rallyUntil = 0f;
            braceAbility = flurryAbility = lastUsed = null;
            flurryLeft = 0; flurryTarget = null; plundered = 0;
            if (decoy != null) { decoy.End(); decoy = null; }
        }

        /// <summary>Stunned or mounted: whatever it was in the middle of is lost.</summary>
        void CancelActions()
        {
            flurryLeft = 0;
            braceUntil = 0f;
            busyTimer = 0f;
        }

        void Float(string text, Color col, float size = 0.8f) =>
            HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1.1f), text, col, size);

        public float HpFrac => MaxHp > 0f ? Hp / MaxHp : 0f;
        /// <summary>The move that will actually happen: an Encore stands in for the last skill used.</summary>
        AbilityDef Acting(AbilityDef a) => a.effect == AbilityEffect.Encore && lastUsed != null ? lastUsed : a;
        public bool WindingUp => pendingAbility >= 0;

        // ---------------------------------------------------------------- marks other kin leave on this one
        public bool Hexed => Time.time < hexUntil;

        public void Hex(float seconds)
        {
            if (!IsAlive || Invulnerable) return;
            hexUntil = Time.time + seconds;
            Float("HEXED", new Color(0.75f, 0.5f, 1f));
        }

        public bool TakeHex() { if (!Hexed) return false; hexUntil = 0f; return true; }

        public bool Tethered => Time.time < tetherUntil;

        /// <summary>Pinned: it can't leave a small circle round the anchor. Wardens and alphas tear free in half the time.</summary>
        public void TetherTo(Vector3 anchor, float radius, float seconds)
        {
            if (!IsAlive || Invulnerable) return;
            if (Boss || Data.alpha) seconds *= 0.5f;
            tetherPos = anchor;
            tetherRadius = radius + BodyRadius;
            tetherUntil = Time.time + seconds;
            dashTime = 0f;
            Float("TETHERED", new Color(0.85f, 0.85f, 0.95f));
        }

        void ClampTether(ref Vector3 next)
        {
            Vector3 off = next - tetherPos; off.y = 0f;
            if (off.sqrMagnitude <= tetherRadius * tetherRadius) return;
            off = off.normalized * tetherRadius;
            next.x = tetherPos.x + off.x;
            next.z = tetherPos.z + off.z;
            dashTime = 0f;
        }

        /// <summary>A quick shot caught it mid wind-up: the skill is lost (its recharge is not refunded). Wardens ignore it.</summary>
        public bool Interrupt()
        {
            if (pendingAbility < 0 || Boss || Invulnerable) return false;
            pendingAbility = -1;
            SetState(AIState.Recover, 0.5f);
            Float("INTERRUPTED", new Color(1f, 0.9f, 0.4f));
            return true;
        }

        /// <summary>Plunder: a hit shakes one of its drops loose. Twice per kin at most, never from a tamer's kin.</summary>
        public bool TryPlunder()
        {
            if (Mode != CreatureMode.Wild || Duelist || plundered >= SkillEffects.PlunderMax) return false;
            if (Sp.drops == null || Sp.drops.Length == 0 || Random.value > SkillEffects.PlunderChance) return false;
            plundered++;
            var drop = Sp.drops[Random.Range(0, Sp.drops.Length)];
            GameManager.I.GiveItem(drop.itemId, 1, transform.position + Vector3.up * BodyRadius);
            Float("PLUNDERED", new Color(1f, 0.85f, 0.4f));
            return true;
        }

        /// <summary>The copy is gone (or was just made): whoever was after 'from' now goes for 'to'.</summary>
        public void Distract(ICombatant from, ICombatant to)
        {
            if (Target == from) Target = to;
            if (commandTarget == from) commandTarget = to;
            if (pendingTarget == from && to != null) pendingTarget = to;
        }

        // ---------------------------------------------------------------- buffs
        public float OutMul => (Time.time < wrathUntil ? SkillEffects.WrathOut : 1f) * RallyMul(Team, transform.position);
        public float InMul => Time.time < wrathUntil ? SkillEffects.WrathIn : 1f;

        static bool InAura(CreatureActor src, Team team, Vector3 pos, bool anthem)
        {
            if (src == null || !src.IsAlive || src.Team != team) return false;
            if (Time.time >= (anthem ? src.anthemUntil : src.rallyUntil)) return false;
            Vector3 d = src.transform.position - pos; d.y = 0f;
            return d.sqrMagnitude <= SkillEffects.AuraRange * SkillEffects.AuraRange;
        }

        /// <summary>How fast skills recharge for this side here (an Anthem is playing nearby = faster).</summary>
        public static float AnthemMul(Team team, Vector3 pos) => InAura(anthemBy[(int)team], team, pos, true) ? SkillEffects.AnthemRate : 1f;
        public static float RallyMul(Team team, Vector3 pos) => InAura(rallyBy[(int)team], team, pos, false) ? SkillEffects.RallyOut : 1f;

        // ---------------------------------------------------------------- taking hits
        /// <summary>
        /// Guard for the player: a companion with its shield planted steps into the next hit aimed at its tamer and
        /// takes it at half strength. True if it did (the player then takes nothing at all from that hit).
        /// </summary>
        public bool TakeGuardFor(PlayerController p, in HitInfo hit)
        {
            if (Time.time >= guardUntil || !IsAlive || Mode != CreatureMode.Companion) return false;
            if (FlatDist(transform.position, p.Position) > SkillEffects.GuardRange) return false;
            guardUntil = 0f;
            HUD.DamageNumber(p.Position + Vector3.up * 2.4f, "GUARDED", new Color(1f, 0.92f, 0.6f), 0.9f);
            Vector3 a = p.Position + Vector3.up, b = transform.position + Vector3.up * BodyRadius;
            for (int i = 0; i <= 6; i++) Fx.Mote(Vector3.Lerp(a, b, i / 6f), Vector3.up * 0.4f, new Color(1f, 0.92f, 0.6f), 0.16f, 0.35f, true);
            SynthAudio.Play(Sfx.HitHeavy, 1.4f, 0.6f);
            ReceiveHit(new HitInfo { attacker = hit.attacker, damage = hit.damage * SkillEffects.GuardShare, element = hit.element, effectiveness = 1f });
            return true;
        }

        /// <summary>A braced kin shrugs off most of a hit and answers it; a guarding one blocks most of it. Returns the damage that gets through.</summary>
        float ShieldHit(in HitInfo hit, float dmg)
        {
            float now = Time.time;
            if (now < braceUntil)
            {
                var ab = braceAbility;
                float bb = braceBoost;
                braceUntil = 0f;
                busyTimer = 0f;
                Float("RIPOSTE", new Color(0.8f, 0.95f, 1f), 0.9f);
                var foe = hit.attacker;
                if (ab != null && foe != null && foe.IsAlive && !ReferenceEquals(foe, this) && FlatDist(foe.Position, transform.position) < 7f)
                {
                    Vector3 d = foe.Position - transform.position; d.y = 0f;
                    Rig.Lunge();
                    Fx.Burst(transform.position + Vector3.up * BodyRadius, ab.color, 10, 4f, 0.25f, 0.35f, true);
                    CombatSystem.Apply(this, foe, ab.power, ab.element, ab.status, ab.statusChance, d.normalized * 6f, bb);
                }
                return dmg * SkillEffects.BraceShare;
            }
            if (now < guardUntil)
            {
                guardUntil = 0f;
                Float("BLOCKED", new Color(1f, 0.92f, 0.6f));
                SynthAudio.Play(Sfx.HitHeavy, 1.4f, 0.5f);
                return dmg * SkillEffects.BlockShare;
            }
            return dmg;
        }

        // ---------------------------------------------------------------- AI: is it worth using now?
        bool TonicWantsHeal()
        {
            if (HpFrac < (Mode == CreatureMode.Companion ? SkillEffects.TonicBelow : 0.5f)) return true;
            return Mode == CreatureMode.Companion && SkillEffects.HpFrac(GameManager.I.Player) < SkillEffects.TonicBelow && GameManager.I.Player.IsAlive;
        }

        /// <summary>Its mark or buff is still running, so using the move again now would waste it.</summary>
        bool EffectUseless(AbilityDef a, ICombatant target)
        {
            float now = Time.time;
            switch (a.effect)
            {
                case AbilityEffect.Guard: return now < guardUntil;
                // (a young kin whose only move is its mark keeps throwing it rather than standing idle)
                case AbilityEffect.Hex: return abilities.Length > 1 && SkillEffects.IsHexed(target);
                case AbilityEffect.Tether: return abilities.Length > 1 && SkillEffects.IsTethered(target);
                case AbilityEffect.Decoy: return decoy != null && decoy.IsAlive;
                case AbilityEffect.Anthem: return now < anthemUntil;
                case AbilityEffect.Wrath: return now < wrathUntil;
                case AbilityEffect.Rally: return now < rallyUntil;
                case AbilityEffect.Temper: return Mode == CreatureMode.Companion ? GameManager.I.Player.Tempered : CritPrimed;
            }
            return false;
        }

        /// <summary>The AI picks the ready move with the highest score. Set-up moves go first; ordinary ones rank by power.</summary>
        float EffectPriority(AbilityDef a, ICombatant target)
        {
            switch (a.effect)
            {
                case AbilityEffect.Guard:
                case AbilityEffect.Hex:
                case AbilityEffect.Tether:
                case AbilityEffect.Decoy:
                case AbilityEffect.Anthem:
                case AbilityEffect.Wrath:
                case AbilityEffect.Rally:
                case AbilityEffect.Temper:
                    return 100f;
                case AbilityEffect.Riposte:
                    // worth it when something is about to hit it; otherwise only when there is nothing better to do
                    return target is CreatureActor foe && foe.WindingUp && ReferenceEquals(foe.Target, this) ? 100f : 4f;
                case AbilityEffect.Tonic: return TonicWantsHeal() ? 100f : a.power;
                case AbilityEffect.Encore: return lastUsed != null ? lastUsed.power * 0.5f + 1f : a.power;
            }
            return a.power;
        }

        // ---------------------------------------------------------------- resolving a signature move
        /// <summary>
        /// Called when a signature move's wind-up ends. True = it was a move of its own and is done; false = carry on
        /// with the ordinary melee / shot / burst / dash (its effect then rides on the hit: CombatSystem.Apply).
        /// </summary>
        bool ResolveEffect(AbilityDef a, int slot, Vector3 dir, float boost)
        {
            bool ally = Mode == CreatureMode.Companion;
            float now = Time.time;
            Vector3 mid = transform.position + Vector3.up * BodyRadius;
            switch (a.effect)
            {
                case AbilityEffect.Iaido:
                {
                    // one cut straight down the line it faces; the longer it stood still, the deeper it bites
                    float mul = 1f + Mathf.Clamp01(stillTime / SkillEffects.IaidoFull);
                    float len = a.range + 1.5f;
                    Vector3 p0 = transform.position;
                    int hits = 0;
                    CreatureRegistry.ForEachEnemyInRange(this, p0 + dir * (len * 0.5f), len * 0.5f + 1.5f, t =>
                    {
                        Vector3 rel = t.Position - p0; rel.y = 0f;
                        float along = Vector3.Dot(rel, dir);
                        float across = (rel - dir * along).magnitude;
                        if (along < -0.6f || along > len + t.Radius || across > 1.1f + t.Radius) return;
                        hits++;
                        CombatSystem.Apply(this, t, a.power * mul, a.element, a.status, a.statusChance, dir * 7f, boost, a.effect);
                    });
                    for (int i = 0; i <= 10; i++)
                    {
                        Vector3 p = mid + dir * (len * i / 10f);
                        Fx.Mote(p, Vector3.up * 0.3f, Color.white, 0.4f, 0.22f, true);
                        Fx.Mote(p, Vector3.up * 0.6f, a.color, 0.28f, 0.4f, true);
                    }
                    SynthAudio.Play(Sfx.Whoosh, 0.75f, 0.9f);
                    if (hits > 0) Float($"IAIDO x{mul:0.0}", new Color(1f, 1f, 0.9f), 0.9f);
                    else if (ally) ShowMiss();
                    stillTime = 0f;
                    return true;
                }
                case AbilityEffect.Flurry:
                    flurryLeft = SkillEffects.FlurryHits;
                    flurryTimer = 0f;
                    flurryAbility = a;
                    flurryTarget = pendingTarget;
                    flurryBoost = boost;
                    flurrySlot = slot;
                    busyTimer = SkillEffects.FlurryHits * SkillEffects.FlurryGap + 0.1f;
                    return true;
                case AbilityEffect.Guard:
                    guardUntil = now + SkillEffects.GuardTime;
                    busyTimer = 0.35f;
                    Fx.Ring(transform.position, new Color(1f, 0.92f, 0.6f), BodyRadius + 1.2f, 20);
                    SynthAudio.Play(Sfx.HitHeavy, 1.5f, 0.45f);
                    Float("GUARD", new Color(1f, 0.92f, 0.6f));
                    return true;
                case AbilityEffect.Riposte:
                    braceUntil = now + SkillEffects.BraceTime;
                    braceAbility = a;
                    braceBoost = boost;
                    busyTimer = SkillEffects.BraceTime;
                    Fx.Sparkle(mid, new Color(0.8f, 0.95f, 1f), 10, BodyRadius + 0.3f);
                    SynthAudio.Play(Sfx.Whoosh, 1.9f, 0.4f);
                    if (ally) Float("EN GARDE", new Color(0.8f, 0.95f, 1f));
                    return true;
                case AbilityEffect.Decoy:
                    CastDecoy(dir);
                    return true;
                case AbilityEffect.Tonic:
                {
                    if (!TonicWantsHeal() && !ReferenceEquals(pendingTarget, this)) return false;   // nobody is hurt: throw the toxin
                    var pl = GameManager.I.Player;
                    bool onPlayer = ally && pl.IsAlive && SkillEffects.HpFrac(pl) < HpFrac;
                    Color green = new Color(0.55f, 1f, 0.6f);
                    if (onPlayer)
                    {
                        float v = pl.Stats.MaxHp * SkillEffects.TonicHeal;
                        pl.Heal(v);
                        HUD.DamageNumber(pl.Position + Vector3.up * 2.4f, "+" + Mathf.RoundToInt(v), green, 0.9f);
                        Fx.Sparkle(pl.Position + Vector3.up, green, 16, 0.8f);
                    }
                    else
                    {
                        float v = MaxHp * SkillEffects.TonicHeal;
                        HealAmount(v);
                        Float("+" + Mathf.RoundToInt(v), green, 0.9f);
                        Fx.Sparkle(mid, green, 16, BodyRadius + 0.4f);
                    }
                    SynthAudio.Play(Sfx.Summon, 1.4f, 0.5f);
                    return true;
                }
                case AbilityEffect.Anthem:
                    anthemUntil = now + SkillEffects.AnthemTime;
                    anthemBy[(int)Team] = this;
                    Fx.Ring(transform.position, a.color, SkillEffects.AuraRange * 0.5f, 30);
                    SynthAudio.Play(Sfx.Summon, 1.1f, 0.6f);
                    Float("ANTHEM", a.color);
                    return true;
                case AbilityEffect.Rally:
                    rallyUntil = now + SkillEffects.RallyTime;
                    rallyBy[(int)Team] = this;
                    Fx.Ring(transform.position, a.color, SkillEffects.AuraRange * 0.5f, 30);
                    Fx.Burst(mid, a.color, 14, 4f, 0.3f, 0.5f, true);
                    SynthAudio.Play(Sfx.Burst, 0.8f, 0.6f);
                    Float("RALLY", a.color);
                    return true;
                case AbilityEffect.Wrath:
                    wrathUntil = now + SkillEffects.WrathTime;
                    Fx.Burst(mid, new Color(1f, 0.25f, 0.2f), 18, 4.5f, 0.3f, 0.5f, true);
                    Fx.Ring(transform.position, new Color(1f, 0.3f, 0.2f), BodyRadius + 1.5f, 20);
                    SynthAudio.Play(Sfx.Burst, 0.7f, 0.6f);
                    Float("WRATH", new Color(1f, 0.35f, 0.3f));
                    return true;
                case AbilityEffect.Temper:
                    if (ally)
                    {
                        var pl = GameManager.I.Player;
                        pl.Temper(SkillEffects.TemperTime);
                        Fx.Sparkle(pl.Position + Vector3.up, new Color(1f, 0.6f, 0.25f), 14, 0.7f);
                    }
                    else critUntil = now + CritWindow;
                    Fx.Burst(mid, new Color(1f, 0.7f, 0.3f), 12, 4f, 0.22f, 0.4f, true);
                    SynthAudio.Play(Sfx.HitHeavy, 1.7f, 0.5f);
                    Float(ally ? "TEMPERED" : "CRIT READY", new Color(1f, 0.6f, 0.3f));
                    return true;
            }
            return false;
        }

        /// <summary>Leave a copy where it stands and slip a few steps away; everything that was after it goes for the copy.</summary>
        void CastDecoy(Vector3 dir)
        {
            if (decoy != null) decoy.End();
            Vector3 from = transform.position;
            decoy = Decoy.Spawn(this, from, transform.rotation, SkillEffects.DecoyTime);
            // step aside: to a flank first, behind if that is a wall
            Vector3 side = Vector3.Cross(Vector3.up, dir) * (Random.value < 0.5f ? 1f : -1f);
            Vector3 spot = from;
            for (int k = 0; k < 3; k++)
            {
                Vector3 c = from + (k == 0 ? side - dir * 0.5f : k == 1 ? -side - dir * 0.5f : -dir) * 4.5f;
                c.y = Ground.Height(c.x, c.z);
                if (Caves.Solid(c + Vector3.up * 0.5f) || Mathf.Abs(c.y - from.y) > 2.5f) continue;
                spot = c;
                break;
            }
            Color veil = Data.Palette.glow;
            Fx.Burst(from + Vector3.up * BodyRadius, veil, 14, 3f, 0.3f, 0.45f, true);
            transform.position = spot;
            vel = Vector3.zero;
            Fx.Sparkle(spot + Vector3.up * BodyRadius, veil, 10, BodyRadius + 0.4f);
            SynthAudio.Play(Sfx.Whoosh, 1.7f, 0.6f);

            var all = CreatureRegistry.All;
            var player = GameManager.I.Player;
            if (Team == Team.Player)
            {
                // wild kin hunting it or its tamer nearby fall for the copy
                for (int i = 0; i < all.Count; i++)
                {
                    var k = all[i];
                    if (k.Mode != CreatureMode.Wild || !k.IsAlive || k.Boss || k.Target == null) continue;
                    if (!ReferenceEquals(k.Target, this) && !ReferenceEquals(k.Target, player)) continue;
                    if (FlatDist(k.Position, from) > 22f) continue;
                    decoy.Fool(k, k.Target);
                }
                Float("DECOY", veil);
            }
            else
            {
                // the tamer loses their lock and their kin chases the copy
                if (ReferenceEquals(player.LockTarget, this)) player.ClearLock();
                for (int i = 0; i < all.Count; i++)
                {
                    var k = all[i];
                    if (k.Team != Team.Player || !k.IsAlive || !ReferenceEquals(k.Target, this)) continue;
                    decoy.Fool(k, this);
                }
            }
        }

        // ---------------------------------------------------------------- per-frame
        void TickFlurry(float dt)
        {
            var t = flurryTarget;
            var a = flurryAbility;
            if (t == null || !t.IsAlive || a == null) { flurryLeft = 0; busyTimer = 0f; return; }
            bool ally = Mode == CreatureMode.Companion;
            Vector3 to = t.Position - transform.position; to.y = 0f;
            float gap = to.magnitude - (t.Radius + BodyRadius);
            if (gap > a.range * 0.6f) MoveTo(t.Position, RunSpeed * 1.4f);   // stay on it between jabs
            faceTarget = t.Position;
            flurryTimer -= dt;
            if (flurryTimer > 0f) return;
            flurryTimer = SkillEffects.FlurryGap;
            flurryLeft--;
            Rig.Lunge();
            SynthAudio.Play(Sfx.Whoosh, 1.5f + 0.15f * (SkillEffects.FlurryHits - flurryLeft), 0.4f);
            if (gap <= a.range + (ally ? 1.2f : 0.5f))
            {
                Vector3 d = to.sqrMagnitude > 0.001f ? to.normalized : transform.forward;
                CombatSystem.Apply(this, t, a.power, a.element, a.status, a.statusChance, d * 2.5f, flurryBoost, a.effect);
                // every jab that lands brings the next round closer
                if (flurrySlot >= 0 && flurrySlot < cooldowns.Length) cooldowns[flurrySlot] *= SkillEffects.FlurryCut;
            }
            else if (ally && flurryLeft == 0) ShowMiss();
        }

        void TickEffects(float dt, bool full)
        {
            stillTime = animSpeed < 0.3f ? stillTime + dt : 0f;
            if (busyTimer > 0f) busyTimer -= dt;
            if (flurryLeft > 0) TickFlurry(dt);
            if (decoy != null && !decoy.IsAlive) decoy = null;
            if (!full) return;
            effectFx -= dt;
            if (effectFx > 0f) return;
            effectFx = 0.16f;
            float now = Time.time;
            Vector3 mid = transform.position + Vector3.up * (BodyRadius + Rig.HoverHeight);
            Vector3 top = transform.position + Vector3.up * (BodyRadius * 2f + 0.4f + Rig.HoverHeight);
            if (now < hexUntil)
            {
                // a violet sigil turning over its head
                float a = now * 4f;
                Fx.Mote(top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.3f, Vector3.zero, new Color(0.75f, 0.4f, 1f), 0.16f, 0.4f, true);
                Fx.Mote(top - new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.3f, Vector3.zero, new Color(0.5f, 0.3f, 0.9f), 0.12f, 0.4f, true);
            }
            if (now < tetherUntil)
            {
                // threads from the anchor to the kin
                Vector3 a0 = tetherPos + Vector3.up * 0.1f;
                for (int i = 0; i <= 5; i++) Fx.Mote(Vector3.Lerp(a0, mid, i / 5f), Vector3.zero, new Color(0.9f, 0.9f, 1f, 0.8f), 0.07f, 0.2f, true);
            }
            if (now < guardUntil)
            {
                float a = now * 3f;
                for (int i = 0; i < 3; i++)
                {
                    float b = a + i * 2.094f;
                    Fx.Mote(transform.position + new Vector3(Mathf.Cos(b), 0.15f, Mathf.Sin(b)) * (BodyRadius + 0.5f), Vector3.up * 0.8f, new Color(1f, 0.92f, 0.6f), 0.12f, 0.4f, true);
                }
            }
            if (now < braceUntil) Fx.Mote(mid + Random.insideUnitSphere * BodyRadius, Vector3.zero, Color.white, 0.2f, 0.15f, true);
            if (now < anthemUntil)
            {
                Color c = Color.HSVToRGB(Mathf.Repeat(now * 0.7f, 1f), 0.5f, 1f);
                Vector3 o = Random.insideUnitSphere * (BodyRadius + 0.6f);
                Fx.Mote(mid + o, new Vector3(o.x, 1.6f, o.z), c, 0.16f, 0.9f, true);
            }
            if (now < wrathUntil) Fx.Mote(mid + Random.insideUnitSphere * BodyRadius, Vector3.up * 1.8f, new Color(1f, 0.2f, 0.15f), 0.18f, 0.5f, true);
            if (now < rallyUntil) Fx.Mote(mid + Random.insideUnitSphere * (BodyRadius + 0.3f), Vector3.up * 1.4f, new Color(1f, 0.6f, 0.2f), 0.14f, 0.5f, true);
        }
    }
}
