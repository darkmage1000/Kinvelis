namespace Wildbond
{
    /// <summary>
    /// The capture seed ladder (user, 2026-10-20: "we need more seeds like palworld and aniimo have... ironbark is op early
    /// and falls off early mid game"). One tier about every 8 levels, each made from the material of the land where you
    /// start to need it. A seed's only real difference is how high it reaches: above its rating the catch falls off
    /// (-6% a level, CaptureSystem.Chance); below it every seed gets the same small edge (`CaptureSystem.SeedEdge`), so a
    /// better seed is never a sure thing on low kin. Legend seeds (and anything with `legendFor`) count as any level.
    /// </summary>
    public static class Seeds
    {
        public const int AnyLevel = 99;

        /// <summary>The ladder, weakest first: item id and the highest kin level it is rated for.</summary>
        public static readonly (string id, int rating)[] Ladder =
        {
            ("tether_seed", 12),      // by hand - Sunmeadow
            ("ironbark_seed", 20),    // ingot - Sunmeadow / Driftkey Isles
            ("cinder_seed", 28),      // Cinder Shard - Badlands, Mirefen
            ("rime_seed", 36),        // Rime Crystal - Peaks, Dunes
            ("sunforged_seed", 44),   // Sunsteel Ore - Blightmoor, Glowdeep
            ("starbloom_seed", AnyLevel),   // Starcrystal - end game and legends
        };

        /// <summary>Highest kin level a seed is rated for (0 = not a seed).</summary>
        public static int Rating(ItemDef seed)
        {
            if (seed == null || seed.category != ItemCategory.CaptureSeed) return 0;
            if (!string.IsNullOrEmpty(seed.legendFor)) return AnyLevel;
            foreach (var t in Ladder) if (t.id == seed.id) return t.rating;
            return seed.captureMult >= 2.2f ? AnyLevel : 20;   // an unknown seed: old rule of thumb
        }

        /// <summary>Position on the ladder (0 = Tether); legend seeds and unknown seeds sort last.</summary>
        public static int Rank(ItemDef seed)
        {
            if (seed == null) return Ladder.Length;
            for (int i = 0; i < Ladder.Length; i++) if (Ladder[i].id == seed.id) return i;
            return Ladder.Length;
        }

        /// <summary>The weakest seed rated for kin of this level - what a reward of that level should hand out.</summary>
        public static string ForLevel(int level)
        {
            foreach (var t in Ladder) if (t.rating >= level) return t.id;
            return Ladder[Ladder.Length - 1].id;
        }

        /// <summary>One rung up (or down) the ladder from a seed id, clamped.</summary>
        public static string Step(string id, int steps)
        {
            int i = 0;
            for (int k = 0; k < Ladder.Length; k++) if (Ladder[k].id == id) i = k;
            i = System.Math.Max(0, System.Math.Min(Ladder.Length - 1, i + steps));
            return Ladder[i].id;
        }
    }
}
