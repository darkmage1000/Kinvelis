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

        /// <summary>Recipes a bench tier can take (its own and every lower tier).</summary>
        public static bool Makes(int benchTier, RecipeDef r) => Tier(r) <= benchTier;
    }
}
