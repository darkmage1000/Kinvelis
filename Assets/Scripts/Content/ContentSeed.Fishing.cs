using UnityEngine;

namespace Wildbond
{
    /// <summary>Fishing: the rod, five fish, what to cook from them, and the camp's Fishing Pond (worked by kin with the Fishing job).</summary>
    public static partial class ContentSeed
    {
        static void BuildFishingItems(Bundle b)
        {
            Item(b, "fishing_rod", "Fishing Rod", ItemCategory.KeyItem, "Rd", Hex("#b88a4a"),
                "A springy pole and a line. Keep it in your bag, stand by rings on the water and tap FISH; when the float dips, tap REEL! A companion with the Fishing job brings the fish in faster.", 1);

            var silver = Item(b, "silverfin", "Silverfin", ItemCategory.Food, "Sv", Hex("#c8d8e8"), "A small silver fish found in every water. Camp kin will eat it. Grill two for a proper meal.");
            silver.foodValue = 14f; silver.healValue = 2f;
            var sun = Item(b, "sunfin", "Sunfin", ItemCategory.Food, "Sn", Hex("#f0b84a"), "A golden-bellied lake fish from the old island's waters. Goes into Fisher's Stew.");
            sun.foodValue = 18f; sun.healValue = 3f;
            var reef = Item(b, "reef_snapper", "Reef Snapper", ItemCategory.Food, "Rs", Hex("#ff7a6a"), "A bright sea fish from the Driftkey Isles and the Sunscour coast. Makes a Reef Platter.");
            reef.foodValue = 18f; reef.healValue = 3f;
            var mire = Item(b, "mirefish", "Mirefish", ItemCategory.Food, "Mf", Hex("#7a9a6a"), "A whiskered bottom-feeder from Mirefen, Blightmoor and Glowdeep. Better in a broth than it looks.");
            mire.foodValue = 16f; mire.healValue = 2f;
            var gold = Item(b, "goldscale", "Goldscale", ItemCategory.Food, "Gs", Hex("#ffd84a"), "A rare fish with scales like coins. Bites anywhere, more often at night. The heart of a Golden Feast.");
            gold.foodValue = 25f; gold.healValue = 8f;

            Meal(b, "grilled_fish", "Grilled Silverfin", "Gf", Hex("#d8c8a8"), "Two fish on a stick, crisp at the edges. Simple and filling.", 50, 20, 10,
                new StatMod(StatKind.MaxHp, 15f));
            Meal(b, "fishers_stew", "Fisher's Stew", "Fs", Hex("#e0a850"), "Sunfin and grain, thick enough to stand a spoon in. Sets you up for a long day.", 65, 20, 12,
                new StatMod(StatKind.Defense, 4f), new StatMod(StatKind.MaxStamina, 20f));
            Meal(b, "reef_platter", "Reef Platter", "Rp", Hex("#ff8a70"), "Snapper with sweet berries. Sharpens the eye and the arm.", 55, 15, 12,
                new StatMod(StatKind.Attack, 5f), new StatMod(StatKind.Crit, 0.05f));
            Meal(b, "mire_broth", "Mire Broth", "Mb", Hex("#8aa878"), "Dark, peppery and oddly good. You shake off whatever the land throws at you.", 45, 15, 12,
                new StatMod(StatKind.StaminaRegen, 0.25f), new StatMod(StatKind.MoveSpeed, 0.06f));
            Meal(b, "golden_feast", "Golden Feast", "Go", Hex("#ffd84a"), "A Goldscale done properly. Luck seems to follow whoever eats one.", 80, 30, 15,
                new StatMod(StatKind.CaptureChance, 0.12f), new StatMod(StatKind.GatherYield, 0.2f));

            Recipe(b, "fishing_rod", 1, CraftStation.Hand, 0, 2, ("wood", 4), ("fiber", 6));
            Recipe(b, "grilled_fish", 1, CraftStation.Campfire, 8, 2, ("silverfin", 2));
            Recipe(b, "fishers_stew", 1, CraftStation.Campfire, 12, 4, ("sunfin", 2), ("goldgrain", 1));
            Recipe(b, "reef_platter", 1, CraftStation.Campfire, 12, 6, ("reef_snapper", 2), ("berry", 3));
            Recipe(b, "mire_broth", 1, CraftStation.Campfire, 12, 8, ("mirefish", 2), ("frostmint", 1));
            Recipe(b, "golden_feast", 1, CraftStation.Campfire, 16, 6, ("goldscale", 1), ("silverfin", 2));
        }

        static void BuildFishingBuildings(Bundle b)
        {
            var pond = Bld(b, "fish_pond", "Fishing Pond", "A stocked pool with a little jetty. Kin with the Fishing job (Tide kin, and kin that live by water) pull Silverfin from it - food for the camp, or for your own fire.",
                4, 2.2f, ("wood", 12), ("stone", 10), ("fiber", 6));
            pond.workType = WorkType.Fishing; pond.workerSlots = 2; pond.outputItem = "silverfin"; pond.outputCount = 1; pond.workPerUnit = 14f;
            pond.palette = Pal(Hex("#8e8a92"), Hex("#4aa8d8"), Hex("#8a5a32"), Hex("#c8f4ff"));
            pond.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.2f, 0, 3.8f, 0.4f, 3.8f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.36f, 0, 3.2f, 0.12f, 3.2f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0.6f, 0.43f, -0.4f, 0.9f, 0.03f, 0.6f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, -0.7f, 0.43f, 0.5f, 0.6f, 0.03f, 0.4f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, 1.7f, 0.35f, 0.9f, 0.9f, 0.7f, 0.8f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, -1.6f, 0.3f, -1.1f, 0.8f, 0.6f, 0.9f),
                // jetty
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 0.5f, 1.9f, 1.1f, 0.1f, 2.2f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -0.45f, 0.25f, 2.7f, 0.14f, 0.5f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0.45f, 0.25f, 2.7f, 0.14f, 0.5f, 0.14f),
                // a rod left propped on a post, and a basket
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0.9f, 0.8f, 1.5f, 0.12f, 0.9f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0.55f, 1.35f, 0.7f, 0.05f, 0.05f, 2.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, -0.9f, 0.72f, 2.1f, 0.5f, 0.36f, 0.5f),
            };
        }
    }
}
