using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// One special gatherable per land, each growing in a single named sub-region (see SubRegions / PropCatalog), and
    /// the thing it makes. Several point you at another land: the Peaks' Frostcap keeps you cool in the desert, the
    /// desert's Sunfig keeps you warm in the Peaks.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildRegionItems(Bundle b)
        {
            var N = UniqueEffect.None;
            // ---- the eight finds ----
            var honey = Item(b, "honeybloom", "Honeybloom", ItemCategory.Food, "Hb", Hex("#ffd23a"), "A heavy golden flower dripping nectar. Grows only on the hills of Petalrun, in the Sunmeadow. Bakes into Honeybloom Cake.");
            honey.foodValue = 5f;
            Item(b, "obsidian", "Obsidian", ItemCategory.Resource, "Ob", Hex("#3a3050"), "Black volcanic glass with an edge like a razor. Mined from spires in the Glasswaste, in the Cinderreach Badlands. Makes the Obsidian Ring.");
            var frostcap = Item(b, "frostcap", "Frostcap", ItemCategory.Food, "Fc", Hex("#7ac8ff"), "A blue mushroom, cold to the touch. Grows only on Shiverglass Field, in the Rimefang Peaks. Simmers into Frostcap Broth.");
            frostcap.foodValue = 4f;
            Item(b, "sea_pearl", "Sea Pearl", ItemCategory.Resource, "Pl", Hex("#fff4f0"), "A pearl from the oysters of Shellstrand, on the Driftkey Isles. They only open when low tide leaves them bare. Makes the Pearl Amulet.");
            var bud = Item(b, "lanternbud", "Lanternbud", ItemCategory.Food, "Lb", Hex("#ffd070"), "A bud that glows like a lamp. Grows only in Lanternfen, in Mirefen. Steeps into Lantern Tea.");
            bud.foodValue = 4f;
            var fig = Item(b, "sunfig", "Sunfig", ItemCategory.Food, "Sf", Hex("#ffb03a"), "A sugar-sweet fig, warm from the sun. Grows only in the Verdant Oasis, in the Sunscour Dunes. Boils down to Sunfig Jam.");
            fig.foodValue = 12f; fig.healValue = 3f;
            Item(b, "sporecap", "Sporecap", ItemCategory.Resource, "Sc", Hex("#c04ad0"), "A violet toadstool. Poison raw, medicine when brewed. Grows only in Sporefall, in Blightmoor. Brews into Sporecap Draught.");
            Item(b, "starmoss", "Starmoss", ItemCategory.Resource, "Sm", Hex("#2ad0b0"), "Moss flecked with points of light. Grows only on the Starmoss Fields, in Glowdeep. Weaves into the Starmoss Band.");

            // ---- what they make ----
            Meal(b, "honey_cake", "Honeybloom Cake", "Hc", Hex("#f4c860"), "Sticky, golden and gone too fast. You could run all day on it.", 45, 10, 10,
                new StatMod(StatKind.MaxStamina, 20f), new StatMod(StatKind.StaminaRegen, 0.3f));
            Meal(b, "frostcap_broth", "Frostcap Broth", "Fb", Hex("#8ad0ff"), "A cold blue broth that chills you from the inside. Made for desert days.", 40, 10, 12,
                new StatMod(StatKind.HeatResist, 0.7f), new StatMod(StatKind.MaxHp, 20f));
            Meal(b, "sunfig_jam", "Sunfig Jam", "Sj", Hex("#f09a30"), "Sun in a jar. Keeps the mountain cold out and puts strength in your arm.", 40, 10, 12,
                new StatMod(StatKind.ColdResist, 0.7f), new StatMod(StatKind.Attack, 4f));
            Meal(b, "lantern_tea", "Lantern Tea", "Lt", Hex("#ffe08a"), "It glows in the cup. Kin are drawn to the light on your breath.", 25, 5, 12,
                new StatMod(StatKind.CaptureChance, 0.08f), new StatMod(StatKind.MoveSpeed, 0.05f));
            Meal(b, "sporecap_draught", "Sporecap Draught", "Sd", Hex("#b060d0"), "Bitter as anything. Clears poison from your blood and keeps it out: spore clouds can't touch you.", 15, 15, 10,
                new StatMod(StatKind.PoisonWard, 1f), new StatMod(StatKind.Defense, 3f));

            Gear(b, "obsidian_ring", "Obsidian Ring", EquipSlot.Ring, Rarity.Rare, 2, "#2a2438", "#7a6ab0", "A ring of black glass, ground to an edge. Your blows find the gaps.", N,
                S(StatKind.Crit, 0.08f), S(StatKind.Attack, 3f));
            Gear(b, "pearl_amulet", "Pearl Amulet", EquipSlot.Amulet, Rarity.Rare, 2, "#fff4f0", "#8ad8d0", "Sea pearls on saltglass wire. Kin settle when they see it.", N,
                S(StatKind.CaptureChance, 0.06f), S(StatKind.MaxStamina, 10f));
            Gear(b, "starmoss_band", "Starmoss Band", EquipSlot.Ring, Rarity.Epic, 2, "#2ad0b0", "#e8fff8", "A living ring of starmoss. It mends you as you walk.", N,
                S(StatKind.MaxHp, 25f), S(StatKind.StaminaRegen, 0.2f), S(StatKind.GatherYield, 0.1f));

            Recipe(b, "honey_cake", 1, CraftStation.Campfire, 12, 3, ("honeybloom", 3), ("goldgrain", 2));
            Recipe(b, "frostcap_broth", 1, CraftStation.Campfire, 12, 6, ("frostcap", 3), ("frostmint", 1));
            Recipe(b, "sunfig_jam", 1, CraftStation.Campfire, 12, 8, ("sunfig", 3), ("berry", 2));
            Recipe(b, "lantern_tea", 1, CraftStation.Campfire, 10, 7, ("lanternbud", 3), ("frostmint", 1));
            Recipe(b, "sporecap_draught", 1, CraftStation.Campfire, 14, 10, ("sporecap", 3), ("gel", 1));
            Recipe(b, "obsidian_ring", 1, CraftStation.Workbench, 30, 8, ("obsidian", 8), ("ingot", 2));
            Recipe(b, "pearl_amulet", 1, CraftStation.Workbench, 30, 10, ("sea_pearl", 6), ("saltglass", 4), ("fiber", 6));
            Recipe(b, "starmoss_band", 1, CraftStation.Workbench, 40, 14, ("starmoss", 10), ("starcrystal", 3), ("crystal", 4));
        }
    }
}
