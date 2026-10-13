using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What treasure chests are for (World/Chests.cs): the Chest Key that opens Gilded chests, and the 24 blueprint
    /// items - a weapon, a piece of armour and an accessory for each of the eight lands (user's design, 2026-10-13:
    /// "sidegrades with a trick"). Each is about as strong as what you can craft in that land, and does one thing
    /// nothing else does: the weapons strike with their land's element and leave a status (which also makes kin
    /// easier to catch), the armour and accessories each carry a `UniqueEffect`. They are made from the land's raw
    /// ore, not bars, so the blueprint is the only gate: no research and no Sigil.
    /// Which land holds which blueprint is `Chests.Blueprints`; the recipe id is the item id.
    /// Looks: weapons in `HeldItemMesh`, armour as gear styles 5-12 in `GearModels`.
    /// </summary>
    public static partial class ContentSeed
    {
        static ItemDef Arm(Bundle b, string id, string name, string glyph, string col, string desc, float damage, HandStyle style, Element el, StatusType st, float chance, Rarity r)
        {
            var d = Item(b, id, name, ItemCategory.Weapon, glyph, Hex(col), desc, 1);
            d.weaponDamage = damage; d.handStyle = style; d.hitElement = el; d.hitStatus = st; d.hitStatusChance = chance; d.rarity = r;
            return d;
        }

        static void BuildChestItems(Bundle b)
        {
            var N = UniqueEffect.None;
            var key = Item(b, Chests.KeyItem, "Chest Key", ItemCategory.Consumable, "Ky", Hex("#f0cc5a"), "Opens one Gilded chest. Found in Wooden and Iron chests, on alphas and rival tamers, or made at a Workbench.", 20);
            key.rarity = Rarity.Rare;
            Recipe(b, Chests.KeyItem, 1, CraftStation.Workbench, 30, 12, ("ingot", 4), ("crystal", 2));

            // ---------------- Sunmeadow Vale ----------------
            Arm(b, "honey_mace", "Honeycomb Mace", "Mc", "#f0b030", "A club with a wild hive bound to the end. Whatever it hits goes sleepy and slow.", 15f, HandStyle.Club, Element.Flora, StatusType.Drowsy, 0.25f, Rarity.Uncommon);
            Recipe(b, "honey_mace", 1, CraftStation.Workbench, 20, 2, ("wood", 10), ("fiber", 8), ("berry", 10));
            Gear(b, "petal_crown", "Petal Crown", EquipSlot.Head, Rarity.Uncommon, 5, "#ff9ac0", "#9fd36b", "A wreath of meadow flowers that never wilts.", UniqueEffect.Bloomwalk,
                S(StatKind.Defense, 3), S(StatKind.CaptureChance, 0.05f));
            Recipe(b, "petal_crown", 1, CraftStation.Workbench, 20, 2, ("fiber", 14), ("pelt", 4), ("berry", 8));
            Gear(b, "forager_band", "Forager's Band", EquipSlot.Bracelet, Rarity.Uncommon, 0, "#9fd36b", "#8a5a30", "A woven cuff with a hooked thumb loop. Nothing slips your grip.", UniqueEffect.GreenThumb,
                S(StatKind.GatherYield, 0.15f));
            Recipe(b, "forager_band", 1, CraftStation.Workbench, 20, 3, ("fiber", 15), ("wood", 8), ("gel", 2));

            // ---------------- Driftkey Isles ----------------
            Arm(b, "tidefang_harpoon", "Tidefang Harpoon", "Hp", "#8ad8d0", "A barbed saltglass head on a long shaft. It hits like a wave.", 30f, HandStyle.Spear, Element.Tide, StatusType.Soaked, 0.35f, Rarity.Rare);
            Recipe(b, "tidefang_harpoon", 1, CraftStation.Workbench, 30, 6, ("saltglass", 6), ("wood", 8), ("fiber", 6));
            Gear(b, "shellback_mantle", "Shellback Mantle", EquipSlot.Body, Rarity.Rare, 6, "#e8d8c0", "#ff7a8a", "A great reef shell across the shoulders. It floats you like a cork.", UniqueEffect.Buoyant,
                S(StatKind.Defense, 9), S(StatKind.MaxHp, 20));
            Recipe(b, "shellback_mantle", 1, CraftStation.Workbench, 35, 7, ("saltglass", 10), ("pelt", 6), ("fiber", 10));
            Gear(b, "coral_ring", "Coral Ring", EquipSlot.Ring, Rarity.Rare, 2, "#ff7a8a", "#fff4f0", "A sprig of reef coral set in saltglass. Kin of the sea trust it.", UniqueEffect.SeaFriend,
                S(StatKind.CaptureChance, 0.03f));
            Recipe(b, "coral_ring", 1, CraftStation.Workbench, 30, 7, ("saltglass", 5), ("pelt", 4));

            // ---------------- Cinderreach Badlands ----------------
            Arm(b, "cinder_cleaver", "Cinder Cleaver", "Cv", "#5a3a34", "A slab of vent iron with an edge that stays red.", 31f, HandStyle.Sword, Element.Ember, StatusType.Burn, 0.35f, Rarity.Rare);
            Recipe(b, "cinder_cleaver", 1, CraftStation.Workbench, 35, 8, ("cinder", 8), ("ingot", 5), ("wood", 3));
            Gear(b, "ventwalker_greaves", "Ventwalker Greaves", EquipSlot.Boots, Rarity.Rare, 7, "#4a3434", "#ff7a2a", "Warm air rises through the soles. Running feels like being carried.", UniqueEffect.Ventwalker,
                S(StatKind.Defense, 6), S(StatKind.HeatResist, 0.5f));
            Recipe(b, "ventwalker_greaves", 1, CraftStation.Workbench, 35, 9, ("cinder", 6), ("ingot", 4), ("pelt", 3));
            Gear(b, "cinder_signet", "Cinder Signet", EquipSlot.Ring, Rarity.Rare, 3, "#4a3434", "#ff7a2a", "A coal that glows hotter the worse things get.", UniqueEffect.LastEmber,
                S(StatKind.Attack, 2));
            Recipe(b, "cinder_signet", 1, CraftStation.Workbench, 30, 8, ("cinder", 6), ("ingot", 3));

            // ---------------- Rimefang Peaks ----------------
            Arm(b, "rimeshard_bow", "Rimeshard Bow", "Bw", "#8fe8ff", "Limbs of rime crystal. Its arrows arrive cold.", 30f, HandStyle.Bow, Element.Frost, StatusType.Chill, 0.4f, Rarity.Rare);
            Recipe(b, "rimeshard_bow", 1, CraftStation.Workbench, 40, 12, ("crystal", 6), ("wood", 10), ("fiber", 8));
            Gear(b, "frostveil_hood", "Frostveil Hood", EquipSlot.Head, Rarity.Rare, 8, "#c8e8f8", "#ffffff", "A fur-lined hood the old trainers wore. Kin listen to whoever wears it.", UniqueEffect.Mentor,
                S(StatKind.Defense, 7), S(StatKind.ColdResist, 0.5f));
            Recipe(b, "frostveil_hood", 1, CraftStation.Workbench, 40, 12, ("crystal", 5), ("pelt", 8), ("fiber", 6));
            Gear(b, "aurora_pendant", "Aurora Pendant", EquipSlot.Amulet, Rarity.Epic, 2, "#9ae0ff", "#c8a0ff", "It shimmers the way the night sky does up there. After dark, kin stop to look.", UniqueEffect.NightsFavour,
                S(StatKind.MaxStamina, 15));
            Recipe(b, "aurora_pendant", 1, CraftStation.Workbench, 45, 13, ("crystal", 8), ("ingot", 4), ("pelt", 4));

            // ---------------- Mirefen ----------------
            Arm(b, "mirefang", "Mirefang", "Mf", "#7a9a4a", "A wavy bog-iron knife, quick in the hand and green along the edge.", 40f, HandStyle.Dagger, Element.Shade, StatusType.Poison, 0.4f, Rarity.Rare);
            Recipe(b, "mirefang", 1, CraftStation.Workbench, 40, 13, ("bog_iron", 8), ("wood", 3), ("gel", 4));
            Gear(b, "reedweave_cloak", "Reedweave Cloak", EquipSlot.Body, Rarity.Rare, 9, "#8a9a5a", "#d8c880", "Fen reeds woven tight and oiled. The bad air of the marsh cannot get in.", N,
                S(StatKind.Defense, 10), S(StatKind.MaxHp, 20), S(StatKind.PoisonWard, 1f));
            Recipe(b, "reedweave_cloak", 1, CraftStation.Workbench, 40, 14, ("bog_iron", 6), ("fiber", 20), ("pelt", 6));
            Gear(b, "lantern_ring", "Lantern Ring", EquipSlot.Ring, Rarity.Rare, 1, "#ffd36a", "#5a6a4a", "A fen light caught in a loop of bog iron. Kin are calmer near it, and eat less.", UniqueEffect.FenLantern,
                S(StatKind.StaminaRegen, 0.1f));
            Recipe(b, "lantern_ring", 1, CraftStation.Workbench, 35, 14, ("bog_iron", 6), ("gel", 6));

            // ---------------- Sunscour Dunes ----------------
            Arm(b, "sunspike_glaive", "Sunspike Glaive", "Sg", "#f0c040", "A crescent of raw sun ore on a long pole. The flash off it stops things in their tracks.", 52f, HandStyle.Glaive, Element.Lumen, StatusType.Shock, 0.3f, Rarity.Epic);
            Recipe(b, "sunspike_glaive", 1, CraftStation.Workbench, 50, 20, ("sunsteel_ore", 12), ("wood", 8), ("cinder", 4));
            Gear(b, "sunwrap", "Dunewalker's Wrap", EquipSlot.Head, Rarity.Epic, 10, "#f2e6c9", "#f0c040", "Wound the way the dune people wind it. In the glare you are never quite where you seem.", UniqueEffect.Mirage,
                S(StatKind.Defense, 8), S(StatKind.HeatResist, 0.7f));
            Recipe(b, "sunwrap", 1, CraftStation.Workbench, 45, 20, ("sunsteel_ore", 6), ("fiber", 20), ("pelt", 8));
            Gear(b, "oasis_amulet", "Oasis Amulet", EquipSlot.Amulet, Rarity.Epic, 0, "#5ac8a0", "#f0c040", "A stoppered drop of oasis water, for the moment you need it most.", UniqueEffect.SecondWind,
                S(StatKind.MaxHp, 30), S(StatKind.HungerReduction, 0.15f));
            Recipe(b, "oasis_amulet", 1, CraftStation.Workbench, 45, 21, ("sunsteel_ore", 6), ("gel", 8), ("pelt", 6));

            // ---------------- Blightmoor ----------------
            Arm(b, "blightcoil_crossbow", "Blightcoil Crossbow", "Cb", "#6a4a8a", "The prod is a strip of blight ore. Its bolts carry the moor with them.", 60f, HandStyle.Crossbow, Element.Shade, StatusType.Poison, 0.45f, Rarity.Epic);
            Recipe(b, "blightcoil_crossbow", 1, CraftStation.Workbench, 60, 28, ("blight_ore", 12), ("wood", 10), ("ingot", 6));
            Gear(b, "stalker_boots", "Stalker Boots", EquipSlot.Boots, Rarity.Epic, 11, "#3a3448", "#b8ff40", "Soft, wrapped and silent. You roll out of trouble without thinking about it.", UniqueEffect.LightStep,
                S(StatKind.Defense, 7), S(StatKind.MoveSpeed, 0.08f));
            Recipe(b, "stalker_boots", 1, CraftStation.Workbench, 55, 28, ("blight_ore", 8), ("pelt", 8), ("gel", 4));
            Gear(b, "blightfang_ring", "Blightfang Ring", EquipSlot.Ring, Rarity.Epic, 4, "#6a4a8a", "#b8ff40", "A barbed splinter of blight ore. Everything you hold picks up its bite.", UniqueEffect.Venomous,
                S(StatKind.Crit, 0.06f));
            Recipe(b, "blightfang_ring", 1, CraftStation.Workbench, 55, 29, ("blight_ore", 8), ("ingot", 4));

            // ---------------- Glowdeep ----------------
            Arm(b, "starfall_blade", "Starfall Blade", "Sf", "#b0a0ff", "A long blade of starcrystal. What it touches drifts off as if into a dream.", 80f, HandStyle.Sword, Element.Lumen, StatusType.Drowsy, 0.25f, Rarity.Epic);
            Recipe(b, "starfall_blade", 1, CraftStation.Workbench, 65, 34, ("starcrystal", 14), ("ingot", 8), ("crystal", 4));
            Gear(b, "starweave_mantle", "Starweave Mantle", EquipSlot.Body, Rarity.Epic, 12, "#3a3470", "#e8fff8", "Night-blue cloth with real light sewn into it. Rare kin come to see what is shining.", UniqueEffect.Shimmerlure,
                S(StatKind.Defense, 12), S(StatKind.MaxStamina, 20));
            Recipe(b, "starweave_mantle", 1, CraftStation.Workbench, 65, 34, ("starcrystal", 10), ("pelt", 10), ("crystal", 6));
            Gear(b, "starlight_bracelet", "Starlight Bracelet", EquipSlot.Bracelet, Rarity.Epic, 2, "#b0a0ff", "#e8fff8", "A string of starcrystal chips that hums when your partner is near.", UniqueEffect.Starcaller,
                S(StatKind.CaptureChance, 0.08f));
            Recipe(b, "starlight_bracelet", 1, CraftStation.Workbench, 65, 35, ("starcrystal", 8), ("crystal", 6));
        }
    }
}
