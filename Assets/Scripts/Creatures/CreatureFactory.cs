using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Rolls wild individuals: level, traits, and rare variants (shimmer, alpha, regional, mutation, rare ability).</summary>
    public static class CreatureFactory
    {
        // 1 in 2000 (was 1 in 150; user 2026-10-19: "3 shimmer wild kin in 5 mins"): galloping on a mount passes ~190 new
        // wild kin a minute (measured with Editor/SpawnSurvey), so 1/150 was more than one shimmer a minute of riding.
        // Now about one per 10 minutes of hard riding, far fewer on foot.
        public static float ShimmerChance = 1f / 2000f;
        public static float MutationChance = 0.025f;
        public static float RareAbilityChance = 0.06f;

        static readonly List<TraitDef> pool = new List<TraitDef>();

        /// <summary>Glintstone relics (outer lands) raise shimmer odds.</summary>
        static float RelicShimmer => GameManager.I != null && GameManager.I.Player != null ? GameManager.I.Player.Stats.ShimmerMul : 1f;

        /// <summary>Chance a wild kin past Lv 20 is Ascended; an Exalted one (Lv 35+) needs the roll twice.</summary>
        public const float WildEvolveChance = 0.35f;

        /// <summary>
        /// A kin at the evolution levels may already have evolved: wild ones sometimes (chance per stage, so Exalted is
        /// rarer), a tamer's kin always (chance 1). Never legendaries, bosses, partners or alphas (an alpha is big enough).
        /// Returns the stage it ended on.
        /// </summary>
        public static int EvolveForLevel(CreatureData d, float chance)
        {
            var sp = d.Species;
            if (sp == null || sp.rarity == Rarity.Legendary || sp.bossOnly || sp.starter || d.alpha) return d.stage;
            int max = d.level >= KinGrowth.Stage2Level ? 2 : d.level >= KinGrowth.Stage1Level ? 1 : 0;
            int st = 0;
            while (st < max && (chance >= 1f || Random.value < chance)) st++;
            if (st > d.stage) { d.stage = st; d.ResetLearnset(); }
            return d.stage;
        }

        /// <summary>RollWild + the wild evolution roll: what the open world, caves, nests, herds and raids spawn.</summary>
        public static CreatureData RollWildMaybeEvolved(SpeciesDef sp, int level, BiomeId biome, bool alpha)
        {
            var d = RollWild(sp, level, biome, alpha);
            EvolveForLevel(d, WildEvolveChance);
            return d;
        }

        public static CreatureData RollWild(SpeciesDef sp, int level, BiomeId biome, bool alpha)
        {
            var d = new CreatureData
            {
                uid = CreatureData.NewUid(),
                speciesId = sp.id,
                level = Mathf.Clamp(level, 1, 60),
                alpha = alpha,
                shimmer = Random.value < ShimmerChance * WorldRules.Current.shimmerRate * (alpha ? 3f : 1f) * WorldEvents.ShimmerMul * RelicShimmer * Nightfall.ShimmerMul,
                regional = sp.regional.enabled && sp.regional.biome == biome,
                location = KinLocation.Box
            };

            // abilities (+ uncommon combos)
            var abil = new List<string>(sp.abilities);
            if (sp.rareAbilities != null && sp.rareAbilities.Length > 0 && Random.value < RareAbilityChance)
            {
                string rare = sp.rareAbilities[Random.Range(0, sp.rareAbilities.Length)];
                // the rare move takes the second slot - unless that is the species' signature skill, which it never displaces
                int at = 1;
                var second = abil.Count >= 2 ? ContentDB.Ability(abil[1]) : null;
                if (second != null && (second.effect != AbilityEffect.None || second.kind == AbilityKind.Blink)) at = 2;
                if (abil.Count > at) abil[at] = rare; else abil.Add(rare);
            }
            d.abilities = abil.ToArray();

            // passive traits: 0-2 random
            int count = Random.value < 0.35f ? 0 : (Random.value < 0.7f ? 1 : 2);
            for (int i = 0; i < count; i++)
            {
                var t = RollTrait(d.traits);
                if (t != null) d.traits.Add(t.id);
            }

            // mutations
            if (Random.value < MutationChance * (alpha ? 2f : 1f))
            {
                d.mutation = (Mutation)Random.Range(1, 5);
                string mt = d.mutation == Mutation.Crystalline ? "crystalline" : d.mutation == Mutation.Giant ? "colossal" :
                    d.mutation == Mutation.Twintail ? "twinned" : "luminous";
                d.traits.Add(mt);
            }
            if (alpha) d.traits.Add("apex");
            if (d.shimmer) Shimmer.Make(d);
            Aptitude.Roll(d);
            return d;
        }

        static TraitDef RollTrait(List<string> existing)
        {
            pool.Clear();
            float total = 0f;
            foreach (var t in ContentDB.TraitList)
            {
                if (t.weight <= 0f || existing.Contains(t.id)) continue;
                pool.Add(t);
                total += t.weight;
            }
            float r = Random.value * total;
            foreach (var t in pool)
            {
                r -= t.weight;
                if (r <= 0f) return t;
            }
            return pool.Count > 0 ? pool[pool.Count - 1] : null;
        }

        /// <summary>Weighted species pick for a biome and conditions. Returns null if nothing fits.</summary>
        /// <param name="gentle">Starter area: no Aggressive species, no Epic/Legendary.</param>
        /// <param name="region">Named sub-region at the spawn point (its favoured elements turn up more), or null.</param>
        public static SpeciesDef PickSpecies(BiomeId biome, bool night, bool nearWater, EnvironmentSystem env, bool gentle = false, SubRegions.SubRegion region = null)
        {
            float total = 0f;
            foreach (var s in ContentDB.SpeciesList) total += GentleWeight(s, biome, night, nearWater, env, gentle, region);
            if (total <= 0f) return null;
            float r = Random.value * total;
            foreach (var s in ContentDB.SpeciesList)
            {
                r -= GentleWeight(s, biome, night, nearWater, env, gentle, region);
                if (r <= 0f) return s;
            }
            return null;
        }

        static float GentleWeight(SpeciesDef s, BiomeId biome, bool night, bool nearWater, EnvironmentSystem env, bool gentle, SubRegions.SubRegion region)
        {
            if (gentle && (s.temperament == Temperament.Aggressive || s.rarity >= Rarity.Epic)) return 0f;
            if (WorldRules.Current.kidRoster && (s.id == "sirelle" || s.id == "sekhara" || s.id == "cindra")) return 0f;   // kids' roster worlds
            float w = Weight(s, biome, night, nearWater, env);
            if (w <= 0f) return 0f;
            return w * SubRegions.SpawnMul(region, s) * Nightfall.SpawnMul(s, biome, night);
        }

        static float Weight(SpeciesDef s, BiomeId biome, bool night, bool nearWater, EnvironmentSystem env)
        {
            if (!s.LivesIn(biome)) return 0f;
            if (s.spawnTime == SpawnTime.Night && !night) return 0f;
            if (s.spawnTime == SpawnTime.Day && night) return 0f;
            float w = s.spawnWeight;
            if (s.spawnCondition != SpawnCondition.None)
            {
                if (env == null || !env.ConditionMet(s.spawnCondition)) return 0f;
                w *= 4f;   // their window is short: make it count
            }
            if (s.nearWater) w *= nearWater ? 2.5f : 0.25f;
            if (env != null) w *= Mathf.Max(env.ElementSpawnBonus(s.element1), env.ElementSpawnBonus(s.element2));
            // rarity already reflected in spawnWeight, but thin out rares a touch at night for readability
            return w;
        }
    }
}
