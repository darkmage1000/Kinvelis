using UnityEngine;

namespace Wildbond
{
    public partial class CreatureActor
    {
        Light glowLight;
        float perkTimer;
        float secondWindCd;
        ICombatant commandTarget;
        float commandTimer;
        // a skill the player called for: the kin runs in (or dashes) until it can land it - it never blinks to the target
        int commandSlot = -1;
        float commandRun;
        const float CommandRunSpeed = 1.6f, CommandGiveUp = 6f;
        const float RetaliateWindow = 8f;
        const float AggroRadius = 13f;

        partial void OnSpawnMode()
        {
            bool lumen = Data != null && (Data.Element1 == Element.Lumen || Data.Element2 == Element.Lumen);   // KinAid: a Lumen kin lights the dark
            if (Mode == CreatureMode.Companion && (Sp.perk == CompanionPerk.Glow || Sp.perk == CompanionPerk.Warmth || lumen))
            {
                if (glowLight == null)
                {
                    var go = new GameObject("CompanionLight");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.up * 1.4f;
                    glowLight = go.AddComponent<Light>();
                    glowLight.type = LightType.Point;
                    glowLight.range = 9f;
                    glowLight.shadows = LightShadows.None;
                }
                glowLight.color = Sp.perk == CompanionPerk.Warmth ? new Color(1f, 0.6f, 0.3f) : new Color(0.75f, 0.9f, 1f);
                glowLight.enabled = true;
            }
            else if (glowLight != null) glowLight.enabled = false;
        }

        partial void OnDespawnMode()
        {
            if (glowLight != null) glowLight.enabled = false;
            if (Mode == CreatureMode.Worker) OnWorkerDespawn();
            if (Mode == CreatureMode.Companion) StopCampHelp();
        }

        partial void OnWorkerDespawn();

        void ThinkCompanion(float dt)
        {
            var player = GameManager.I.Player;
            float dPlayer = DistToPlayer;

            // perks
            perkTimer -= dt;
            if (perkTimer <= 0f)
            {
                perkTimer = 1f;
                if (Sp.perk == CompanionPerk.Mending && player.IsAlive) player.Heal(1.5f);
                secondWindCd -= 1f;
                if (Sp.perk == CompanionPerk.Tailwind && player.IsAlive && secondWindCd <= 0f && player.Stats.hp < player.Stats.MaxHp * 0.35f)
                {
                    secondWindCd = 45f;
                    player.Heal(player.Stats.MaxHp * 0.3f);
                    Fx.Sparkle(player.Position + Vector3.up, new Color(0.6f, 1f, 0.9f), 20, 0.8f);
                    GameEvents.Good($"{Data.DisplayName}'s Second Wind restored your HP!");
                    SynthAudio.Play(Sfx.Summon, 1.3f, 0.7f);
                }
                if (glowLight != null) glowLight.intensity = EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight ? 2.2f : 0f;
            }

            // teleport back if left behind (not while it is busy across the camp: TickCampHelp sends it back when you leave;
            // and with more rope while it is fighting a kin you sent it at)
            if (dPlayer > (commandTimer > 0f ? 52f : 32f) && CampHelpStatus == null)
            {
                var p = player.Position - player.Forward * 2f;
                transform.position = new Vector3(p.x, Ground.Height(p.x, p.z), p.z);
                Target = null;
                commandSlot = -1;
                SetState(AIState.Follow);
                Fx.Sparkle(transform.position, new Color(0.6f, 1f, 0.7f), 12);
                return;
            }

            if (State == AIState.Recover)
            {
                if (stateTimer <= 0f) SetState(AIState.Follow);
                return;
            }

            // choose target according to the stance (an explicit SKILL command always wins)
            commandTimer -= dt;
            if (commandTimer > 0f && commandTarget != null && commandTarget.IsAlive) Target = commandTarget;
            else
            {
                commandTarget = null;
                commandSlot = -1;
                if (Target != null && (!Target.IsAlive || FlatDist(Target.Position, player.Position) > 22f)) Target = null;
                var stance = GameManager.I.Party.Stance;
                if (stance == CompanionStance.Passive) Target = null;
                else if (Target == null) Target = PickStanceTarget(stance, player);
            }

            if (Target != null)
            {
                if (CampHelpStatus != null || haulCount > 0) StopCampHelp();
                State = AIState.Chase;
                if (commandSlot >= 0 && TickCommand(dt)) return;
                FightTarget(dt);
                return;
            }

            // not fighting: pitch in when the player is gathering something this kin knows how to work
            if (TickGatherAssist(dt, player)) { if (CampHelpStatus != null) StopCampHelp(); return; }

            // in your camp: do the camp work it knows instead of trailing behind you (CreatureActor.CampHelp)
            if (TickCampHelp(dt, player)) return;

            // follow: stay behind & to the right of the player
            Vector3 slot = player.Position - player.Forward * 2.2f + player.Right * 1.6f;
            // simple buttons: when the star on the HUD is about a place, the kin runs ahead towards it and waits
            var lead = Quests.LeadPoint(player.Position);
            if (lead.HasValue) slot = lead.Value;
            float d = FlatDist(transform.position, slot);
            if (d > 1.2f)
            {
                State = AIState.Follow;
                MoveTo(slot, d > 6f ? RunSpeed * 1.15f : Mathf.Max(WalkSpeed, player.Speed));
            }
            else State = AIState.Idle;
        }

        /// <summary>
        /// Defensive: whoever hurt the player or this kin recently, or anything currently hunting either of us.
        /// Aggressive: the above, else the player's focused kin, else the nearest wild kin around the player.
        /// </summary>
        ICombatant PickStanceTarget(CompanionStance stance, PlayerController player)
        {
            float now = Time.time;
            if (player.LastAttacker != null && player.LastAttacker.IsAlive && now - player.LastAttackedTime < RetaliateWindow
                && FlatDist(player.LastAttacker.Position, player.Position) < 22f) return player.LastAttacker;
            if (LastAttacker != null && LastAttacker.IsAlive && now - LastAttackedTime < RetaliateWindow
                && FlatDist(LastAttacker.Position, player.Position) < 22f) return LastAttacker;
            var hunter = CreatureRegistry.FindHunter(player.Position, 18f, player, this);
            if (hunter != null) return hunter;
            if (stance != CompanionStance.Aggressive) return null;
            var focus = player.CombatTarget;
            if (focus != null && focus.IsAlive && FlatDist(focus.Position, player.Position) < AggroRadius) return focus;
            return CreatureRegistry.FindNearestWild(player.Position, AggroRadius);
        }

        /// <summary>Player-issued special: use the strongest ready ability on the given target immediately.</summary>
        public bool CommandAttack(ICombatant target)
        {
            if (target == null || !target.IsAlive || pendingAbility >= 0) return false;
            Target = target;
            commandTarget = target;
            commandTimer = 10f;
            int best = -1; float bp = -1;
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == null || cooldowns[i] > 0f) continue;
                if (abilities[i].power > bp) { bp = abilities[i].power; best = i; }
            }
            if (best < 0) return false;
            QueueCommand(best);
            return true;
        }

        /// <summary>Player-issued special with a specific skill slot (skill bar). Returns a reason when it can't fire.</summary>
        public string CommandSkill(int slot, ICombatant target)
        {
            if (slot < 0 || slot >= abilities.Length || abilities[slot] == null) return "Not learned yet.";
            var ab = abilities[slot];
            // a skill it uses on itself or on you needs no target and no run-up
            bool self = SkillEffects.SelfCast(ab.effect) || (ab.effect == AbilityEffect.Tonic && TonicWantsHeal());
            if (!self && (target == null || !target.IsAlive)) return "No target in range.";
            if (cooldowns[slot] > 0f) return $"{abilities[slot].displayName} recharging ({cooldowns[slot]:0.0}s).";
            if (pendingAbility >= 0 || busyTimer > 0f) return "Busy...";
            if (self)
            {
                commandSlot = -1;
                StartAbility(slot, target != null && target.IsAlive ? target : this);
                return null;
            }
            Target = target;
            commandTarget = target;
            commandTimer = 10f;
            QueueCommand(slot);
            return null;
        }

        /// <summary>
        /// How close (edge to edge) the kin must be before a commanded skill is worth starting: in arm's reach for a
        /// melee move (the wind-up keeps closing), inside its range for a shot, well inside the ring for a burst, and
        /// anywhere its dash can cover for a dash - which is the move's own way of crossing the gap.
        /// </summary>
        static float CommandReach(AbilityDef a)
        {
            switch (a.kind)
            {
                case AbilityKind.Melee: return a.range * 0.8f;
                case AbilityKind.Projectile: return a.range * 0.9f;
                case AbilityKind.Burst: return a.radius * 0.6f;
                default: return a.range + 1f;
            }
        }

        void QueueCommand(int slot)
        {
            commandSlot = slot;
            commandRun = 0f;
            var a = abilities[slot];
            float gap = FlatDist(transform.position, commandTarget.Position) - (commandTarget.Radius + BodyRadius);
            if (gap <= CommandReach(Acting(a))) return;   // close enough already: it goes off on the next think
            // it has ground to cover: a kick of dust and it is away
            Fx.Puff(transform.position + Vector3.up * 0.15f, new Color(0.85f, 0.8f, 0.7f, 0.7f), 0.5f, 0.4f);
            SynthAudio.Play(Sfx.Whoosh, 1.3f, 0.35f);
        }

        /// <summary>Close the distance at a sprint, then use the skill. True while the command is still being carried out.</summary>
        bool TickCommand(float dt)
        {
            int i = commandSlot;
            var t = commandTarget;
            if (t == null || !t.IsAlive || i < 0 || i >= abilities.Length || abilities[i] == null || cooldowns[i] > 0f) { commandSlot = -1; return false; }
            var a = abilities[i];
            float gap = FlatDist(transform.position, t.Position) - (t.Radius + BodyRadius);
            if (gap <= CommandReach(Acting(a)))
            {
                commandSlot = -1;
                StartAbility(i, t);
                return true;
            }
            commandRun += dt;
            if (commandRun > CommandGiveUp)
            {
                commandSlot = -1;
                GameEvents.Say($"{Data.DisplayName} couldn't get close enough for {a.displayName}.");
                return false;
            }
            MoveTo(t.Position, RunSpeed * CommandRunSpeed);
            return true;
        }

        public float AbilityCooldown01(int i)
        {
            if (i >= abilities.Length || abilities[i] == null) return 1f;
            return Mathf.Clamp01(1f - cooldowns[i] / abilities[i].cooldown);
        }

        public void HealFull()
        {
            Hp = MaxHp;
            Status = default;
        }

        public void HealAmount(float v) => Hp = Mathf.Min(MaxHp, Hp + v);
    }
}
