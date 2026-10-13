using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What treasure chests are for (World/Chests.cs): the Chest Key that opens Gilded chests, and sixteen accessories
    /// that can only be made once their blueprint has been found in a chest - two per land, each made from that land's
    /// own rock. They need the raw ore, not bars, so the blueprint is the only gate: no research and no Sigil.
    /// Which land holds which blueprint is `Chests.Blueprints`; the recipe id is the item id.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildChestItems(Bundle b)
        {
            var N = UniqueEffect.None;
            var key = Item(b, Chests.KeyItem, "Chest Key", ItemCategory.Consumable, "Ky", Hex("#f0cc5a"), "Opens one Gilded chest. Found in Wooden and Iron chests, on alphas and rival tamers, or made at a Workbench.", 20);
            key.rarity = Rarity.Rare;
            Recipe(b, Chests.KeyItem, 1, CraftStation.Workbench, 30, 12, ("ingot", 4), ("crystal", 2));

            // ---------------- Sunmeadow ----------------
            Gear(b, "meadow_charm", "Sunmeadow Charm", EquipSlot.Amulet, Rarity.Uncommon, 0, "#ffd36a", "#9fd36b", "Pressed meadow flowers in a fluff pouch. You breathe easier.", N,
                S(StatKind.MaxHp, 15), S(StatKind.StaminaRegen, 0.1f));
            Recipe(b, "meadow_charm", 1, CraftStation.Workbench, 20, 2, ("fiber", 12), ("pelt", 4), ("berry", 10));
            Gear(b, "forager_band", "Forager's Band", EquipSlot.Bracelet, Rarity.Uncommon, 0, "#9fd36b", "#8a5a30", "A woven cuff with a hooked thumb loop. Nothing slips your grip.", N,
                S(StatKind.GatherYield, 0.2f), S(StatKind.MoveSpeed, 0.03f));
            Recipe(b, "forager_band", 1, CraftStation.Workbench, 20, 3, ("fiber", 15), ("wood", 8), ("gel", 2));

            // ---------------- Driftkey Isles ----------------
            Gear(b, "tidecaller_bangle", "Tidecaller Bangle", EquipSlot.Bracelet, Rarity.Rare, 2, "#8ad8d0", "#fff4f0", "Saltglass that hums with the surf. You tire slowly.", N,
                S(StatKind.MaxStamina, 20), S(StatKind.StaminaRegen, 0.15f));
            Recipe(b, "tidecaller_bangle", 1, CraftStation.Workbench, 30, 6, ("saltglass", 6), ("gel", 4), ("fiber", 8));
            Gear(b, "coral_ring", "Coral Ring", EquipSlot.Ring, Rarity.Rare, 2, "#ff7a8a", "#fff4f0", "A sprig of reef coral set in saltglass. Kin of the sea trust it.", N,
                S(StatKind.CaptureChance, 0.07f), Res(Element.Tide, 0.15f));
            Recipe(b, "coral_ring", 1, CraftStation.Workbench, 30, 7, ("saltglass", 5), ("pelt", 4));

            // ---------------- Cinderreach Badlands ----------------
            Gear(b, "cinder_signet", "Cinder Signet", EquipSlot.Ring, Rarity.Rare, 3, "#4a3434", "#ff7a2a", "A coal that never quite goes out, set in iron.", N,
                S(StatKind.Attack, 4), S(StatKind.HeatResist, 0.3f));
            Recipe(b, "cinder_signet", 1, CraftStation.Workbench, 30, 8, ("cinder", 6), ("ingot", 3));
            Gear(b, "ashwalker_amulet", "Ashwalker Amulet", EquipSlot.Amulet, Rarity.Rare, 3, "#6a5a52", "#ff6a1a", "Vent glass on a fluff cord. Heat washes round you.", N,
                S(StatKind.Defense, 5), S(StatKind.MaxHp, 15), Res(Element.Ember, 0.2f));
            Recipe(b, "ashwalker_amulet", 1, CraftStation.Workbench, 35, 9, ("cinder", 8), ("pelt", 5), ("ingot", 2));

            // ---------------- Rimefang Peaks ----------------
            Gear(b, "rimeglass_ring", "Rimeglass Ring", EquipSlot.Ring, Rarity.Rare, 2, "#8fe8ff", "#e8f8ff", "Clear as ice and twice as sharp. Your eye finds the weak spot.", N,
                S(StatKind.Crit, 0.07f), S(StatKind.ColdResist, 0.3f));
            Recipe(b, "rimeglass_ring", 1, CraftStation.Workbench, 35, 12, ("crystal", 5), ("ingot", 3));
            Gear(b, "aurora_pendant", "Aurora Pendant", EquipSlot.Amulet, Rarity.Epic, 2, "#9ae0ff", "#c8a0ff", "It shimmers the way the night sky does up there. Kin stop to look.", N,
                S(StatKind.CaptureChance, 0.08f), S(StatKind.MaxStamina, 15), Res(Element.Frost, 0.15f));
            Recipe(b, "aurora_pendant", 1, CraftStation.Workbench, 45, 13, ("crystal", 8), ("ingot", 4), ("pelt", 4));

            // ---------------- Mirefen ----------------
            Gear(b, "mire_bracer", "Mire Bracer", EquipSlot.Bracelet, Rarity.Rare, 1, "#5a6a4a", "#8ac86a", "Bog iron beaten round the forearm. Ugly, and it works.", N,
                S(StatKind.Defense, 6), S(StatKind.MaxHp, 20));
            Recipe(b, "mire_bracer", 1, CraftStation.Workbench, 35, 13, ("bog_iron", 8), ("fiber", 10));
            Gear(b, "lantern_ring", "Lantern Ring", EquipSlot.Ring, Rarity.Rare, 1, "#ffd36a", "#5a6a4a", "A fen light caught in a loop of bog iron. Your hands keep going.", N,
                S(StatKind.GatherYield, 0.12f), S(StatKind.StaminaRegen, 0.12f));
            Recipe(b, "lantern_ring", 1, CraftStation.Workbench, 35, 14, ("bog_iron", 6), ("gel", 6));

            // ---------------- Sunscour Dunes ----------------
            Gear(b, "sunscour_band", "Sunscour Band", EquipSlot.Bracelet, Rarity.Epic, 1, "#f0c040", "#fff0a0", "Raw sun ore, hammered thin. It makes your arm feel heavy in the right way.", N,
                S(StatKind.Attack, 6), S(StatKind.HeatResist, 0.4f));
            Recipe(b, "sunscour_band", 1, CraftStation.Workbench, 45, 20, ("sunsteel_ore", 8), ("cinder", 4));
            Gear(b, "oasis_amulet", "Oasis Amulet", EquipSlot.Amulet, Rarity.Epic, 0, "#5ac8a0", "#f0c040", "A stoppered drop of oasis water. One sip and you could walk all day.", N,
                S(StatKind.MaxHp, 35), S(StatKind.HungerReduction, 0.2f));
            Recipe(b, "oasis_amulet", 1, CraftStation.Workbench, 45, 21, ("sunsteel_ore", 6), ("gel", 8), ("pelt", 6));

            // ---------------- Blightmoor ----------------
            Gear(b, "blightfang_ring", "Blightfang Ring", EquipSlot.Ring, Rarity.Epic, 4, "#6a4a8a", "#b8ff40", "A splinter of blight ore, barbed. It wants to bite.", N,
                S(StatKind.Crit, 0.1f), S(StatKind.Attack, 5));
            Recipe(b, "blightfang_ring", 1, CraftStation.Workbench, 55, 28, ("blight_ore", 8), ("ingot", 4));
            Gear(b, "miasma_ward", "Miasma Ward", EquipSlot.Amulet, Rarity.Epic, 4, "#4a3a5a", "#b8ff40", "Blight ore sealed in gel. The bad air parts round it.", N,
                S(StatKind.Defense, 8), S(StatKind.MaxHp, 20), Res(Element.Shade, 0.25f));
            Recipe(b, "miasma_ward", 1, CraftStation.Workbench, 55, 29, ("blight_ore", 10), ("gel", 6));

            // ---------------- Glowdeep ----------------
            Gear(b, "starlight_bracelet", "Starlight Bracelet", EquipSlot.Bracelet, Rarity.Epic, 2, "#b0a0ff", "#e8fff8", "A string of starcrystal chips. Kin follow it like a lure.", N,
                S(StatKind.CaptureChance, 0.12f), S(StatKind.MoveSpeed, 0.06f));
            Recipe(b, "starlight_bracelet", 1, CraftStation.Workbench, 65, 34, ("starcrystal", 8), ("crystal", 6));
            Gear(b, "nebula_ring", "Nebula Ring", EquipSlot.Ring, Rarity.Epic, 2, "#7a6ad0", "#ffe98a", "Looking into it is like looking a long way up.", N,
                S(StatKind.Attack, 6), S(StatKind.Crit, 0.08f), S(StatKind.MaxStamina, 20));
            Recipe(b, "nebula_ring", 1, CraftStation.Workbench, 65, 35, ("starcrystal", 10), ("ingot", 6));
        }
    }
}
