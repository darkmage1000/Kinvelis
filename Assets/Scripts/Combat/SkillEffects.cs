using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Signature skills (AbilityDef.effect). An effect is a twist layered on an ability's ordinary kind: the kind still
    /// says how the move reaches its target (melee, shot, burst, dash), the effect says what is special about it.
    /// Numbers and the shared "what happens when it lands" rules live here; the per-kin state (who is hexed, who is
    /// guarding) lives on the actors (CreatureActor.Effects, PlayerController.Effects). To add one: an enum value at
    /// the END of AbilityEffect (assets store the number), a line in Describe, and its rule where it applies.
    /// </summary>
    public static class SkillEffects
    {
        public const float HexTime = 8f, HexMul = 1.5f;
        public const float GuardTime = 6f, GuardShare = 0.5f, BlockShare = 0.3f, GuardRange = 16f;
        public const float BraceTime = 1.3f, BraceShare = 0.4f;
        public const float TetherTime = 5f, TetherRadius = 3.5f, TetherTimePlayer = 3f, TetherRadiusPlayer = 4f;
        public const float AuraRange = 16f;
        public const float AnthemTime = 8f, AnthemRate = 1.8f, AnthemStamina = 2f;
        public const float WrathTime = 8f, WrathOut = 1.4f, WrathIn = 1.2f;
        public const float RallyTime = 8f, RallyOut = 1.25f;
        public const float TemperTime = 12f;
        public const float DrainShare = 0.5f, SearMul = 1.5f, PlunderChance = 0.35f;
        public const float DecoyTime = 4.5f, DecoyHp = 0.3f;
        public const float IaidoFull = 3f;
        public const float TonicHeal = 0.16f, TonicBelow = 0.6f;
        public const float PullStun = 0.7f, PullStunPlayer = 0.4f;
        public const float FlurryGap = 0.18f, FlurryCut = 0.86f;
        public const int FlurryHits = 3, PlunderMax = 2;

        /// <summary>Cast on the user itself: needs no target, and the AI uses it as soon as a fight is close.</summary>
        public static bool SelfCast(AbilityEffect e) =>
            e == AbilityEffect.Guard || e == AbilityEffect.Riposte || e == AbilityEffect.Anthem || e == AbilityEffect.Temper
            || e == AbilityEffect.Wrath || e == AbilityEffect.Rally || e == AbilityEffect.Decoy;

        public static float HpFrac(ICombatant c)
        {
            if (c is CreatureActor k) return k.HpFrac;
            if (c is PlayerController p) return p.Stats.MaxHp > 0f ? p.Stats.hp / p.Stats.MaxHp : 0f;
            return 1f;
        }

        public static bool IsHexed(ICombatant c) => c is CreatureActor k ? k.Hexed : c is PlayerController p && p.Hexed;
        public static bool IsTethered(ICombatant c) => c is CreatureActor k ? k.Tethered : c is PlayerController p && p.Tethered;

        /// <summary>Power multiplier that depends on the state of the target (asked before the damage roll).</summary>
        public static float PowerMul(AbilityEffect fx, ICombatant target)
        {
            switch (fx)
            {
                case AbilityEffect.Reap: return 1f + (1f - Mathf.Clamp01(HpFrac(target)));   // up to double on a kin about to fall
                case AbilityEffect.Sear:
                {
                    bool burning = target is CreatureActor k ? k.Status.Active && k.Status.type == StatusType.Burn
                        : target is PlayerController p && p.Status.Active && p.Status.type == StatusType.Burn;
                    return burning ? SearMul : 1f;
                }
            }
            return 1f;
        }

        /// <summary>Wrath and Rally: what the attacker's own buffs do to everything it lands.</summary>
        public static float OutMul(ICombatant attacker)
        {
            if (attacker is CreatureActor k) return k.OutMul;
            if (attacker is PlayerController p) return CreatureActor.RallyMul(Team.Player, p.Position);
            return 1f;
        }

        public static float InMul(ICombatant target) => target is CreatureActor k ? k.InMul : 1f;

        /// <summary>Uses up a Hex mark: the hit that asks lands as super effective.</summary>
        public static bool TakeHex(ICombatant target) => target is CreatureActor k ? k.TakeHex() : target is PlayerController p && p.TakeHex();

        static void Say(ICombatant c, string text, Color col, float size = 0.8f) =>
            HUD.DamageNumber(c.Position + Vector3.up * (c.Radius * 2f + 1.1f), text, col, size);

        /// <summary>What a signature move does once it has actually connected (after the damage went in).</summary>
        public static void Landed(AbilityEffect fx, ICombatant attacker, ICombatant target, in HitInfo hit)
        {
            switch (fx)
            {
                case AbilityEffect.Drain:
                    if (attacker is CreatureActor leech && leech.IsAlive)
                    {
                        float back = hit.damage * DrainShare;
                        leech.HealAmount(back);
                        Say(leech, "+" + Mathf.RoundToInt(back), new Color(0.55f, 1f, 0.6f));
                        Fx.Sparkle(leech.Position + Vector3.up * leech.Radius, new Color(0.9f, 0.25f, 0.35f), 8, leech.Radius + 0.3f);
                    }
                    break;
                case AbilityEffect.Hex:
                    if (!target.IsAlive) break;
                    if (target is CreatureActor hk) hk.Hex(HexTime);
                    else if (target is PlayerController hp) hp.Hex(HexTime);
                    break;
                case AbilityEffect.Tether:
                    if (!target.IsAlive) break;
                    if (target is CreatureActor tk) tk.TetherTo(tk.Position, TetherRadius, TetherTime);
                    else if (target is PlayerController tp) tp.TetherTo(tp.Position, TetherRadiusPlayer, TetherTimePlayer);
                    break;
                case AbilityEffect.Quickdraw:
                    if (target is CreatureActor qk && qk.IsAlive) qk.Interrupt();
                    break;
                case AbilityEffect.Plunder:
                    if (attacker is CreatureActor thief && thief.Mode == CreatureMode.Companion && target is CreatureActor mark) mark.TryPlunder();
                    break;
                case AbilityEffect.Pull:
                    if (!target.IsAlive) break;
                    if (target is CreatureActor pk) pk.Stun(PullStun);
                    else if (target is PlayerController pp) pp.Stun(PullStunPlayer);
                    break;
            }
        }

        /// <summary>One plain line for menus: what the signature does. Empty for ordinary moves.</summary>
        public static string Describe(AbilityDef a)
        {
            if (a == null) return "";
            if (a.kind == AbilityKind.Blink) return "appears behind the target, stuns it, next hit crits";
            switch (a.effect)
            {
                case AbilityEffect.Iaido: return "one cut down a line, up to twice as hard the longer it stood still first";
                case AbilityEffect.Guard: return "plants its shield: takes the next hit aimed at you at half strength";
                case AbilityEffect.Flurry: return "three fast jabs, each one that lands shortens the recharge";
                case AbilityEffect.Riposte: return "braces for a moment: shrugs off most of a hit and strikes straight back";
                case AbilityEffect.Hex: return "marks the target: the next hit on it lands as super effective";
                case AbilityEffect.Decoy: return "leaves a copy of itself that enemies attack for a few seconds";
                case AbilityEffect.Tether: return "pins the target to within a few steps of where it was caught";
                case AbilityEffect.Tonic: return "heals you or itself when one is hurt, otherwise poisons the target";
                case AbilityEffect.Anthem: return "for 8 s skills near it recharge faster and your stamina refills twice as fast";
                case AbilityEffect.Encore: return "repeats the last skill it used at half strength";
                case AbilityEffect.Sear: return "half again as hard against a target that is already burning";
                case AbilityEffect.Temper: return "your next weapon hit is a certain crit";
                case AbilityEffect.Reap: return "hits harder the less health the target has left, up to double";
                case AbilityEffect.Drain: return "heals itself for half the damage dealt";
                case AbilityEffect.Quickdraw: return "near-instant shot that breaks a skill being wound up";
                case AbilityEffect.Plunder: return "may knock one of a wild kin's drops loose (twice per kin at most)";
                case AbilityEffect.Pull: return "drags everything nearby toward it and leaves it dazed";
                case AbilityEffect.Wrath: return "for 8 s it hits 40% harder and takes 20% more";
                case AbilityEffect.Rally: return "for 8 s you, it and your kin nearby hit 25% harder";
            }
            return "";
        }
    }
}
