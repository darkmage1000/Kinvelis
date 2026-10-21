using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Crafting stations come in tiers that grow with the journey (user, 2026-10-20: "as we progress we need new benches
    /// that basically replace old ones, a workbench shouldn't be making the best stuff"). A station kind
    /// (<see cref="CraftStation"/>) is a family - Workbench -> Forge Bench -> Artisan's Anvil -> Starforge, Campfire ->
    /// Kitchen - and a bench of tier N works every order of its family up to tier N, so the newest bench replaces the old
    /// one. A recipe's tier is worked out from what it is made of (the land its scarcest material comes from), so a new
    /// recipe lands on the right bench with nothing to set; cooking works the same way (a meal with an ingredient from past the
    /// Sunmeadow needs the Kitchen).
    /// </summary>
    public static class Stations
    {
        public const int MaxTier = 4;

        /// <summary>A placed station's tier (old assets without one are tier 1).</summary>
        public static int Tier(BuildingDef d) => d == null ? 0 : Mathf.Max(1, d.benchTier);
        public static int Tier(Building b) => b == null ? 0 : Tier(b.Def);

        /// <summary>Material -> the tier of bench that works it. Anything not listed is tier 1 (Sunmeadow basics, ingots).</summary>
        static readonly Dictionary<string, int> MaterialTier = new Dictionary<string, int>
        {
            // tier 2: the first lands across the meadow - Badlands, Peaks, Driftkey Isles, Mirefen
            ["cinder"] = 2, ["crystal"] = 2, ["obsidian"] = 2, ["saltglass"] = 2, ["saltglass_bar"] = 2, ["sea_pearl"] = 2,
            ["bog_iron"] = 2, ["bogsteel_bar"] = 2, ["frostcap"] = 2, ["lanternbud"] = 2, ["prism_shard"] = 2, ["warden_heart"] = 2,
            // food from past the meadow: Kitchen cooking
            ["emberpepper"] = 2, ["frostmint"] = 2, ["reef_snapper"] = 2, ["mirefish"] = 2, ["goldscale"] = 2,
            // tier 3: Sunscour Dunes, Blightmoor
            ["sunsteel_ore"] = 3, ["sunsteel_bar"] = 3, ["blight_ore"] = 3, ["blightsteel_bar"] = 3, ["sunfig"] = 3, ["sporecap"] = 3,
            // tier 4: Glowdeep and the deep caves
            ["starcrystal"] = 4, ["starsteel_bar"] = 4, ["starmoss"] = 4, ["geode_core"] = 4,
        };

        public static int TierOfMaterial(string id) => id != null && MaterialTier.TryGetValue(id, out int t) ? t : 1;

        /// <summary>The bench tier a recipe needs (1 for hand recipes, the Alloy Forge and research).</summary>
        public static int Tier(RecipeDef r)
        {
            if (r == null) return 1;
            switch (r.station)
            {
                case CraftStation.Workbench:
                {
                    int t = 1;
                    if (r.inputs != null) foreach (var i in r.inputs) t = Mathf.Max(t, TierOfMaterial(i.itemId));
                    return t;
                }
                case CraftStation.Campfire:
                {
                    // Sunmeadow food at the Campfire; anything with an ingredient from another land at the Kitchen
                    int t = 1;
                    if (r.inputs != null) foreach (var i in r.inputs) t = Mathf.Max(t, TierOfMaterial(i.itemId));
                    return Mathf.Min(t, 2);
                }
                default: return 1;
            }
        }

        /// <summary>The station structure of a family and tier (for names and "build a ..." hints).</summary>
        public static BuildingDef Bench(CraftStation st, int tier)
        {
            BuildingDef best = null;
            foreach (var b in ContentDB.BuildingList)
            {
                if (b.station != st) continue;
                int t = Tier(b);
                if (t == tier) return b;
                if (t < tier && (best == null || t > Tier(best))) best = b;
            }
            return best;
        }

        public static string BenchName(CraftStation st, int tier)
        {
            var b = Bench(st, tier);
            return b != null ? b.displayName : st.ToString();
        }

        /// <summary>
        /// How much longer a camp order takes than the recipe's own work, by its bench tier (user, 2026-10-21: "why not just
        /// insta craft if it only takes 1 min"; chose tier scaling + a bit slower overall). Better crafters are faster, so
        /// these are tuned against the crafter you would have by then: with a matching kin (Crafting 2 / 3 / 3 / 4) a basic
        /// order is about a minute, Forge Bench / Kitchen items 2-3 minutes, the Anvil 5-6 and the Starforge about 10.
        /// Hand crafting is unchanged.
        /// </summary>
        public static readonly float[] TimeMul = { 1f, 2f, 4.5f, 7f, 11.5f };
        public static float OrderWork(RecipeDef r) => r == null ? 1f : r.station == CraftStation.Hand ? r.work : r.work * TimeMul[Mathf.Clamp(Tier(r), 1, MaxTier)];

        /// <summary>"About 3 min with your Crafting 3 kin" for recipe rows: the best kin in camp for the job, at camp pace.</summary>
        public static string TimeText(RecipeDef r)
        {
            var bm = BaseManager.I;
            if (r == null || r.station == CraftStation.Hand || r.station == CraftStation.Research) return null;
            var bench = Bench(r.station, Tier(r));
            var job = bench != null ? bench.workType : WorkType.Crafting;
            int best = 0;
            if (bm != null && bm.HasBase) foreach (var d in GameManager.I.Party.In(KinLocation.Base)) best = Mathf.Max(best, d.WorkLevel(job));
            int lvl = Mathf.Max(1, best);
            float rate = lvl * BaseManager.Pace * 1.15f * GameManager.I.Player.Stats.CraftSpeedMul;   // rested camp
            float secs = OrderWork(r) / Mathf.Max(0.01f, rate);
            string t = secs < 90f ? $"{Mathf.CeilToInt(secs / 5f) * 5} s" : $"{secs / 60f:0.#} min";
            return best > 0 ? $"Takes about {t} with your {job} {best} kin." : $"Takes about {t} with a {job} 1 kin.";
        }

        /// <summary>Recipes a bench tier can take (its own and every lower tier).</summary>
        public static bool Makes(int benchTier, RecipeDef r) => Tier(r) <= benchTier;
    }
}
