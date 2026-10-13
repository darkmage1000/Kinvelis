using UnityEngine;

namespace Wildbond
{
    public enum Team { Player, Wild }

    public interface ICombatant
    {
        Vector3 Position { get; }
        float Radius { get; }
        bool IsAlive { get; }
        Team Team { get; }
        int Level { get; }
        float AttackStat { get; }
        float DefenseStat { get; }
        Element Element1 { get; }
        Element Element2 { get; }
        void ReceiveHit(in HitInfo hit);
    }

    public struct HitInfo
    {
        public ICombatant attacker;
        public float damage;
        public Element element;
        public StatusType status;
        public float statusChance;
        public Vector3 knockback;
        public bool crit;
        public float effectiveness;
    }

    /// <summary>Timed status effect container (one active status at a time - simple & readable on mobile).</summary>
    public struct StatusState
    {
        public StatusType type;
        public float time;
        public float tick;

        public bool Active => type != StatusType.None && time > 0f;

        public static float Duration(StatusType t)
        {
            switch (t)
            {
                case StatusType.Burn: return 5f;
                case StatusType.Chill: return 5f;
                case StatusType.Shock: return 1.4f;
                case StatusType.Drowsy: return 6f;
                case StatusType.Soaked: return 7f;
                case StatusType.Poison: return 7f;
                default: return 0f;
            }
        }

        public static Color Color(StatusType t)
        {
            switch (t)
            {
                case StatusType.Burn: return new Color(1f, 0.45f, 0.15f);
                case StatusType.Chill: return new Color(0.6f, 0.9f, 1f);
                case StatusType.Shock: return new Color(1f, 0.9f, 0.2f);
                case StatusType.Drowsy: return new Color(0.8f, 0.6f, 1f);
                case StatusType.Soaked: return new Color(0.3f, 0.6f, 1f);
                case StatusType.Poison: return new Color(0.65f, 0.95f, 0.25f);
                default: return UnityEngine.Color.white;
            }
        }

        /// <summary>Capture multiplier while the target suffers this status.</summary>
        public static float CaptureBonus(StatusType t)
        {
            switch (t)
            {
                case StatusType.Drowsy: return 1.8f;
                case StatusType.Shock: return 1.5f;
                case StatusType.Chill: return 1.3f;
                case StatusType.Soaked: return 1.25f;
                case StatusType.Burn: return 1.2f;
                case StatusType.Poison: return 1.3f;
                default: return 1f;
            }
        }

        public float SpeedMul => !Active ? 1f : (type == StatusType.Chill ? 0.6f : (type == StatusType.Shock ? 0f : (type == StatusType.Drowsy ? 0.5f : 1f)));
        public float DefMul => Active && type == StatusType.Soaked ? 0.75f : 1f;
    }

    public static class CombatSystem
    {
        // ---- pacing ----
        // Kin HP grows 12% per level, so kin damage grows at the same rate: an even-level fight lasts about as long at
        // Lv 1 as at Lv 40 (a typical same-element hit takes roughly a tenth of an average kin's HP), while a level gap
        // compounds through both the level factor and the attack/defense ratio - a max-level kin one-shots a low one.
        /// <summary>Share of an ability's power a kin deals at level 1 against equal defense.</summary>
        public const float KinPowerScale = 0.32f;
        /// <summary>Same for the player's weapon swings (they land ~3x a second, so each is a small chip).</summary>
        public const float PlayerPowerScale = 0.2f;
        public const float KinLevelGrowth = 0.12f;
        /// <summary>The player's weapons and Attack stat already carry most of their progression.</summary>
        public const float PlayerLevelGrowth = 0.04f;
        /// <summary>How hard an attack/defense mismatch bites (0 = ignored, 1 = linear).</summary>
        public const float StatExponent = 0.6f;

        /// <summary>Damage multiplier from the attacker's level and kind.</summary>
        public static float LevelScale(ICombatant attacker)
        {
            int lv = Mathf.Max(1, attacker.Level);
            return attacker is PlayerController
                ? PlayerPowerScale * (1f + PlayerLevelGrowth * (lv - 1))
                : KinPowerScale * (1f + KinLevelGrowth * (lv - 1));
        }

        /// <summary>Core damage formula. power ~ ability/weapon power. Returns final HitInfo (not yet applied).</summary>
        public static HitInfo Compute(ICombatant attacker, ICombatant target, float power, Element elem, StatusType st, float stChance, float elementBoost = 1f)
        {
            float atk = attacker.AttackStat;
            float def = Mathf.Max(1f, target.DefenseStat);
            float ratio = Mathf.Clamp(Mathf.Pow(atk / def, StatExponent), 0.35f, 4f) * LevelScale(attacker);   // x LevelScale when even
            float eff = ElementInfo.Multiplier(elem, target.Element1, target.Element2);
            // Hex: the next hit on a marked target lands as super effective, whatever its element
            if (eff < SkillEffects.HexMul && SkillEffects.TakeHex(target)) eff = SkillEffects.HexMul;
            // same-element bonus
            if (attacker.Element1 == elem || attacker.Element2 == elem) power *= 1.2f;
            var pAtk = attacker as PlayerController;
            bool crit = Random.value < (pAtk != null ? pAtk.Stats.CritChance : 0.08f);
            if (attacker is CreatureActor primed && primed.TakeCrit()) crit = true;   // set up by a Blink skill
            if (pAtk != null && pAtk.TakeTemper()) crit = true;                        // your blacksmith kin's Temper
            float dmg = power * ratio * eff * elementBoost * Random.Range(0.9f, 1.1f) * (crit ? 1.5f : 1f);
            dmg *= SkillEffects.OutMul(attacker) * SkillEffects.InMul(target);          // Wrath, Rally
            if (target is PlayerController pDef) dmg *= 1f - pDef.Stats.Resist(elem);   // gear element resistance
            return new HitInfo
            {
                attacker = attacker, damage = Mathf.Max(1f, dmg), element = elem, status = st, statusChance = stChance,
                crit = crit, effectiveness = eff
            };
        }

        public static void Apply(ICombatant attacker, ICombatant target, float power, Element elem, StatusType st, float stChance, Vector3 knock, float elementBoost = 1f,
            AbilityEffect fx = AbilityEffect.None)
        {
            if (target == null || !target.IsAlive) return;
            if (fx != AbilityEffect.None) power *= SkillEffects.PowerMul(fx, target);   // Reap, Sear
            var rules = WorldRules.Current;
            if (attacker != null && attacker.Team == Team.Wild) power *= rules.wildDamage;
            else if (attacker != null && attacker.Team == Team.Player) power *= rules.playerDamage;
            if (Dev.OneHit && attacker != null && attacker.Team == Team.Player) power *= 500f;
            var hit = Compute(attacker, target, power, elem, st, stChance, elementBoost);
            hit.knockback = knock;
            target.ReceiveHit(hit);

            var p = target.Position + Vector3.up * (target.Radius + 0.6f);
            Color c = hit.effectiveness > 1.1f ? new Color(1f, 0.85f, 0.2f) : (hit.effectiveness < 0.9f ? new Color(0.7f, 0.7f, 0.75f) : Color.white);
            if (hit.crit) c = new Color(1f, 0.4f, 0.3f);
            if (target.Team == Team.Player) c = new Color(1f, 0.35f, 0.35f);
            HUD.DamageNumber(p, Mathf.RoundToInt(hit.damage).ToString() + (hit.crit ? "!" : ""), c, hit.effectiveness > 1.1f ? 1.25f : 1f);
            Fx.Burst(target.Position + Vector3.up * target.Radius, ElementInfo.Color(elem), hit.crit ? 14 : 8, 5f, 0.28f, 0.45f, true);
            SynthAudio.Play(hit.crit ? Sfx.HitHeavy : Sfx.Hit, 0.9f + Random.value * 0.2f);
            if (hit.effectiveness > 1.1f) HUD.DamageNumber(p + Vector3.up * 0.5f, "Super effective", new Color(1f, 0.85f, 0.2f), 0.7f);
            if (fx != AbilityEffect.None) SkillEffects.Landed(fx, attacker, target, hit);
        }
    }
}
