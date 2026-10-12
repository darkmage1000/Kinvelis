using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp tech: the outer lands' ores and the bars smelted from them (Alloy Forge), the tools, weapons and armor made
    /// of those bars, the Outpost Kit, and the tech buildings. What is locked behind research is listed in Core/Tech.cs
    /// (by recipe / building id) - nothing here knows about it.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildTechItems(Bundle b)
        {
            var N = UniqueEffect.None;
            // ---------------- ores (veins on each outer land) ----------------
            Item(b, "saltglass", "Saltglass", ItemCategory.Resource, "Sg", Hex("#9ae8e0"), "Sea-clear crystal from veins on the Driftkey Isles (Ingot Pick). Smelts into Saltglass Bars at an Alloy Forge.");
            Item(b, "bog_iron", "Bog Iron", ItemCategory.Resource, "Bi", Hex("#6a7a4a"), "Heavy green-black ore from Mirefen mud (Ingot Pick). Smelts into Bogsteel.");
            Item(b, "sunsteel_ore", "Sunsteel Ore", ItemCategory.Resource, "So", Hex("#f0c040"), "Gold-flecked ore from the Sunscour Dunes. Needs a Bogsteel Pick.");
            Item(b, "blight_ore", "Blight Ore", ItemCategory.Resource, "Bo", Hex("#7a4aa0"), "Dark ore that weeps green from Blightmoor veins. Needs a Bogsteel Pick.");
            Item(b, "starcrystal", "Starcrystal", ItemCategory.Resource, "Sc", Hex("#b0a0ff"), "Crystal with a point of light inside, from the Glowdeep. Needs a Blightsteel Pick.");
            // ---------------- bars ----------------
            Item(b, "saltglass_bar", "Saltglass Bar", ItemCategory.Resource, "Sb", Hex("#b8f4ec"), "Clear, hard glass-metal. Greenhouse panes and the Saltglass Saber.");
            Item(b, "bogsteel_bar", "Bogsteel Bar", ItemCategory.Resource, "Bs", Hex("#5a6a4a"), "Tough dark steel. Tier 4 tools and the fourth Hearthstone.");
            Item(b, "sunsteel_bar", "Sunsteel Bar", ItemCategory.Resource, "Ss", Hex("#ffd860"), "Warm golden steel. Sunsteel gear, the Waypad and the fifth Hearthstone.");
            Item(b, "blightsteel_bar", "Blightsteel Bar", ItemCategory.Resource, "Bl", Hex("#6a4a8a"), "Dark steel veined with green. Tier 5 tools and thorned armor.");
            Item(b, "starsteel_bar", "Starsteel Bar", ItemCategory.Resource, "St", Hex("#c8c0ff"), "Steel that glows from inside. The best gear there is, and the last Hearthstone.");
            Recipe(b, "ingot", 3, CraftStation.AlloyForge, 12, 1, ("ore", 4));
            Recipe(b, "saltglass_bar", 1, CraftStation.AlloyForge, 14, 1, ("saltglass", 3), ("ingot", 1));
            Recipe(b, "bogsteel_bar", 1, CraftStation.AlloyForge, 16, 1, ("bog_iron", 3), ("ingot", 1));
            Recipe(b, "sunsteel_bar", 1, CraftStation.AlloyForge, 18, 1, ("sunsteel_ore", 3), ("cinder", 1));
            Recipe(b, "blightsteel_bar", 1, CraftStation.AlloyForge, 22, 1, ("blight_ore", 3), ("bogsteel_bar", 1));
            Recipe(b, "starsteel_bar", 1, CraftStation.AlloyForge, 26, 1, ("starcrystal", 3), ("crystal", 2));

            // ---------------- tools ----------------
            var bp = Item(b, "bogsteel_pick", "Bogsteel Pick", ItemCategory.Tool, "Pk", Hex("#5a6a4a"), "Tier 4 pick. Breaks Sunsteel and Blight veins.", 1); bp.toolKind = ToolKind.Pick; bp.toolPower = 4;
            var ba = Item(b, "bogsteel_axe", "Bogsteel Axe", ItemCategory.Tool, "Ax", Hex("#5a6a4a"), "Tier 4 axe. Fells any tree in a few swings.", 1); ba.toolKind = ToolKind.Axe; ba.toolPower = 4;
            var lp = Item(b, "blightsteel_pick", "Blightsteel Pick", ItemCategory.Tool, "Pk", Hex("#6a4a8a"), "Tier 5 pick. Breaks Starcrystal.", 1); lp.toolKind = ToolKind.Pick; lp.toolPower = 5;
            var la = Item(b, "blightsteel_axe", "Blightsteel Axe", ItemCategory.Tool, "Ax", Hex("#6a4a8a"), "Tier 5 axe. The best there is.", 1); la.toolKind = ToolKind.Axe; la.toolPower = 5;
            Recipe(b, "bogsteel_pick", 1, CraftStation.Workbench, 40, 8, ("bogsteel_bar", 4), ("wood", 4));
            Recipe(b, "bogsteel_axe", 1, CraftStation.Workbench, 40, 8, ("bogsteel_bar", 4), ("wood", 4));
            Recipe(b, "blightsteel_pick", 1, CraftStation.Workbench, 55, 14, ("blightsteel_bar", 5), ("wood", 4));
            Recipe(b, "blightsteel_axe", 1, CraftStation.Workbench, 55, 14, ("blightsteel_bar", 5), ("wood", 4));

            // ---------------- weapons ----------------
            Item(b, "saltglass_saber", "Saltglass Saber", ItemCategory.Weapon, "Sw", Hex("#b8f4ec"), "A curved blade you can see the sea through.", 1).weaponDamage = 36f;
            Item(b, "bogsteel_glaive", "Bogsteel Glaive", ItemCategory.Weapon, "Gl", Hex("#5a6a4a"), "Long, heavy and patient, like the fen it came from.", 1).weaponDamage = 46f;
            Item(b, "sunsteel_blade", "Sunsteel Blade", ItemCategory.Weapon, "Bl", Hex("#ffd860"), "Stays warm in the hand. Throws light when it swings.", 1).weaponDamage = 58f;
            Item(b, "blightsteel_fang", "Blightsteel Fang", ItemCategory.Weapon, "Fg", Hex("#6a4a8a"), "A hooked blade that hums near anything alive.", 1).weaponDamage = 72f;
            Item(b, "starsteel_edge", "Starsteel Edge", ItemCategory.Weapon, "Ed", Hex("#c8c0ff"), "It leaves a line of light behind every stroke.", 1).weaponDamage = 88f;
            Recipe(b, "saltglass_saber", 1, CraftStation.Workbench, 40, 6, ("saltglass_bar", 5), ("wood", 2), ("pelt", 2));
            Recipe(b, "bogsteel_glaive", 1, CraftStation.Workbench, 45, 8, ("bogsteel_bar", 6), ("wood", 4));
            Recipe(b, "sunsteel_blade", 1, CraftStation.Workbench, 55, 11, ("sunsteel_bar", 6), ("bogsteel_bar", 2), ("pelt", 2));
            Recipe(b, "blightsteel_fang", 1, CraftStation.Workbench, 65, 14, ("blightsteel_bar", 7), ("gel", 6));
            Recipe(b, "starsteel_edge", 1, CraftStation.Workbench, 80, 17, ("starsteel_bar", 8), ("crystal", 6));

            // ---------------- armor ----------------
            Gear(b, "sunsteel_helm", "Sunsteel Helm", EquipSlot.Head, Rarity.Epic, 1, "#e8c060", "#fff0a0", "Polished to a mirror. Turns the midday sun.", N,
                S(StatKind.Defense, 11), S(StatKind.HeatResist, 0.4f), Res(Element.Ember, 0.15f));
            Gear(b, "sunsteel_plate", "Sunsteel Plate", EquipSlot.Body, Rarity.Epic, 1, "#e0b850", "#fff0a0", "Broad golden plates. Warm, never hot.", N,
                S(StatKind.Defense, 20), S(StatKind.MaxHp, 30), S(StatKind.HeatResist, 0.5f));
            Gear(b, "sunsteel_greaves", "Sunsteel Greaves", EquipSlot.Boots, Rarity.Epic, 1, "#d8b048", "#fff0a0", "Light on sand, sure on stone.", N,
                S(StatKind.Defense, 8), S(StatKind.MoveSpeed, 0.06f), S(StatKind.MaxStamina, 20));
            Recipe(b, "sunsteel_helm", 1, CraftStation.Workbench, 55, 11, ("sunsteel_bar", 4), ("pelt", 3));
            Recipe(b, "sunsteel_plate", 1, CraftStation.Workbench, 70, 11, ("sunsteel_bar", 8), ("bogsteel_bar", 2), ("pelt", 4));
            Recipe(b, "sunsteel_greaves", 1, CraftStation.Workbench, 55, 11, ("sunsteel_bar", 4), ("fiber", 8));

            Gear(b, "blightsteel_hood", "Blightsteel Hood", EquipSlot.Head, Rarity.Epic, 4, "#4a4458", "#b8ff40", "A thorned cowl. Things in the dark keep their distance.", N,
                S(StatKind.Defense, 13), S(StatKind.Attack, 4), Res(Element.Shade, 0.2f));
            Gear(b, "blightsteel_mail", "Blightsteel Mail", EquipSlot.Body, Rarity.Epic, 4, "#40394e", "#b8ff40", "Barbed rings that never rust.", N,
                S(StatKind.Defense, 23), S(StatKind.MaxHp, 40), Res(Element.Shade, 0.25f));
            Gear(b, "blightsteel_boots", "Blightsteel Boots", EquipSlot.Boots, Rarity.Epic, 4, "#3a3446", "#b8ff40", "Quiet, quick and unbothered by mud.", N,
                S(StatKind.Defense, 10), S(StatKind.MoveSpeed, 0.08f), S(StatKind.StaminaRegen, 0.15f));
            Recipe(b, "blightsteel_hood", 1, CraftStation.Workbench, 65, 14, ("blightsteel_bar", 4), ("pelt", 4));
            Recipe(b, "blightsteel_mail", 1, CraftStation.Workbench, 85, 14, ("blightsteel_bar", 9), ("gel", 6), ("pelt", 4));
            Recipe(b, "blightsteel_boots", 1, CraftStation.Workbench, 65, 14, ("blightsteel_bar", 4), ("fiber", 10));

            Gear(b, "starsteel_crown", "Starsteel Crown", EquipSlot.Head, Rarity.Legendary, 2, "#8a8ae8", "#d0f4ff", "A circlet with a star caught in it. Kin look twice.", N,
                S(StatKind.Defense, 15), S(StatKind.CaptureChance, 0.08f), Res(Element.Lumen, 0.2f));
            Gear(b, "starsteel_raiment", "Starsteel Raiment", EquipSlot.Body, Rarity.Legendary, 2, "#7a7ad8", "#d0f4ff", "Plates like a clear night sky.", N,
                S(StatKind.Defense, 27), S(StatKind.MaxHp, 55), S(StatKind.Attack, 5));
            Gear(b, "starsteel_treads", "Starsteel Treads", EquipSlot.Boots, Rarity.Legendary, 2, "#6a6ac8", "#d0f4ff", "Every step lands exactly where you meant it.", N,
                S(StatKind.Defense, 12), S(StatKind.MoveSpeed, 0.1f), S(StatKind.MaxStamina, 25));
            Recipe(b, "starsteel_crown", 1, CraftStation.Workbench, 80, 17, ("starsteel_bar", 5), ("crystal", 6));
            Recipe(b, "starsteel_raiment", 1, CraftStation.Workbench, 100, 17, ("starsteel_bar", 10), ("blightsteel_bar", 3), ("pelt", 6));
            Recipe(b, "starsteel_treads", 1, CraftStation.Workbench, 80, 17, ("starsteel_bar", 5), ("pelt", 4));

            // ---------------- outposts ----------------
            Item(b, "outpost_kit", "Outpost Kit", ItemCategory.Consumable, "Ok", Hex("#d8b070"),
                "USE it while standing on dry ground in an outer land to found an outpost there (one per land). Station two kin and a caravan brings what they gather home. Packing an outpost up returns the kit.", 5);
            Recipe(b, "outpost_kit", 1, CraftStation.Workbench, 40, 5, ("wood", 20), ("fiber", 20), ("pelt", 6), ("ingot", 4));
        }

        static void BuildTechBuildings(Bundle b)
        {
            var woodP = Pal(Hex("#a8743f"), Hex("#7a5230"), Hex("#d9c08f"), Hex("#ffd36a"));

            var bench = Bld(b, "research_bench", "Research Bench", "Study blueprints here: new metals, machines and outposts. A Crafting kin does the work (or you, standing at it).", 3, 1.3f,
                ("wood", 16), ("stone", 8), ("ingot", 2));
            bench.station = CraftStation.Research; bench.workType = WorkType.Crafting; bench.workerSlots = 1;
            bench.palette = Pal(Hex("#8a6a48"), Hex("#5a4430"), Hex("#f0e4c0"), Hex("#7af0ff"));
            bench.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.85f, 0, 2.0f, 0.14f, 1.1f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.85f, 0.4f, 0, 0.16f, 0.8f, 0.9f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.85f, 0.4f, 0, 0.16f, 0.8f, 0.9f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 1.45f, -0.5f, 2.0f, 1.1f, 0.1f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -0.35f, 1.5f, -0.44f, 0.7f, 0.5f, 0.03f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0.5f, 1.6f, -0.44f, 0.5f, 0.6f, 0.03f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -0.4f, 0.95f, 0.1f, 0.6f, 0.04f, 0.45f, 0, 15, 0),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.3f, 1.0f, 0.2f, 0.12f, 0.36f, 0.12f, 0, 0, 80),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.72f, 0.92f, 0.25f, 0.22f, 0.5f, 0.22f),
            };

            var forge = Bld(b, "alloy_forge", "Alloy Forge", "Smelts the outer lands' ores into bars (and Rustore into Ingots, faster than a Smelter). Kindling kin work it.", 1, 1.6f,
                ("stone", 30), ("ingot", 10), ("cinder", 6));
            forge.station = CraftStation.AlloyForge; forge.workType = WorkType.Kindling; forge.workerSlots = 2;
            forge.palette = Pal(Hex("#5e5660"), Hex("#3a343c"), Hex("#b08a5a"), Hex("#ff8a2a"));
            forge.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.25f, 0, 2.6f, 0.5f, 2.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.3f, -0.2f, 1.9f, 1.7f, 1.9f),
                P(PartShape.Dome, BodyGroup.Body, ColorSlot.Primary, 0, 2.1f, -0.2f, 1.9f, 1.3f, 1.9f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0.45f, 3.0f, -0.5f, 0.5f, 1.5f, 0.5f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 1.0f, 0.72f, 0.8f, 0.7f, 0.3f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 0.56f, 0.95f, 1.2f, 0.12f, 0.5f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -1.0f, 0.7f, 0.8f, 0.5f, 0.4f, 0.4f),
            };

            var silo = Bld(b, "bulk_silo", "Bulk Silo", "+60 base storage slots in one tall tower.", 1, 1.3f, ("wood", 40), ("stone", 20), ("ingot", 6));
            silo.storageSlots = 60; silo.palette = woodP;
            silo.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.2f, 0, 2.4f, 0.4f, 2.4f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 2.0f, 0, 2.0f, 3.4f, 2.0f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 1.2f, 0, 2.08f, 0.14f, 2.08f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 2.8f, 0, 2.08f, 0.14f, 2.08f),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, 0, 4.3f, 0, 2.5f, 1.2f, 2.5f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.9f, 1.0f, 0.7f, 1.2f, 0.08f),
            };

            var clinic = Bld(b, "kin_clinic", "Kin Clinic", "Fainted kin resting in camp recover three times sooner and wake at 60% health. Resting kin heal twice as fast.", 1, 1.3f,
                ("wood", 20), ("pelt", 8), ("gel", 6), ("ingot", 4));
            clinic.palette = Pal(Hex("#f0ece0"), Hex("#b8a888"), Hex("#e85a6a"), Hex("#8affc0"));
            clinic.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 2.6f, 0.3f, 2.2f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 1.05f, -0.3f, 2.4f, 1.6f, 1.4f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 1.95f, -0.1f, 2.7f, 0.16f, 2.0f, 8, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.3f, 0.42f, 0.6f, 0.18f, 0.04f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.3f, 0.42f, 0.18f, 0.6f, 0.04f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, 0.8f, 0.45f, 0.75f, 0.8f, 0.3f, 0.5f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, -0.9f, 0.6f, 0.8f, 0.3f, 0.4f, 0.3f),
            };

            var yard = Bld(b, "training_yard", "Training Yard", "Camp kin earn work XP twice as fast while it stands.", 1, 1.5f, ("wood", 30), ("ingot", 8), ("pelt", 6));
            yard.palette = Pal(Hex("#a8743f"), Hex("#7a5230"), Hex("#c83a3a"), Hex("#ffd36a"));
            yard.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.08f, 0, 3.0f, 0.16f, 3.0f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.0f, 0, 0.26f, 2.0f, 0.26f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.4f, 0, 0.14f, 1.4f, 0.14f, 0, 0, 90),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0, 2.1f, 0, 0.5f, 0.5f, 0.5f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 1.1f, 0.7f, 0.6f, 0.16f, 1.4f, 0.16f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 1.1f, 1.1f, 0.68f, 0.7f, 0.08f, 0.7f, 90, 0, 0),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Glow, 1.1f, 1.1f, 0.73f, 0.3f, 0.08f, 0.3f, 90, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, -1.1f, 0.3f, -0.5f, 0.9f, 0.6f, 0.5f),
            };

            var green = Bld(b, "greenhouse", "Greenhouse", "A glass-roofed plot that plants, waters and harvests itself, kin or no kin. Cultivating and Irrigating kin make it faster. Tap CROP in Camp > Structures to choose what it grows.", 1, 1.8f,
                ("saltglass_bar", 8), ("wood", 20), ("ingot", 4));
            green.workType = WorkType.Cultivating; green.secondaryWorkType = WorkType.Irrigating; green.workerSlots = 1; green.outputItem = "berry"; green.outputCount = 2; green.workPerUnit = 12f;
            green.palette = Pal(Hex("#6b4a2e"), Hex("#4f9c3a"), Hex("#b8f4ec"), Hex("#d8fff8"));
            green.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.14f, 0, 3.0f, 0.28f, 2.2f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, -0.9f, 0.5f, 0, 0.7f, 0.6f, 1.4f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0f, 0.5f, 0, 0.7f, 0.6f, 1.4f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0.9f, 0.5f, 0, 0.7f, 0.6f, 1.4f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.75f, 0.55f, 3.0f, 0.06f, 1.3f, 28, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.75f, -0.55f, 3.0f, 0.06f, 1.3f, -28, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, -1.46f, 0.8f, 1.06f, 0.1f, 1.4f, 0.1f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 1.46f, 0.8f, 1.06f, 0.1f, 1.4f, 0.1f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, -1.46f, 0.8f, -1.06f, 0.1f, 1.4f, 0.1f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 1.46f, 0.8f, -1.06f, 0.1f, 1.4f, 0.1f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 2.06f, 0, 3.1f, 0.1f, 0.1f),
            };

            var turret = Bld(b, "volt_turret", "Volt Turret", "While a Volt Pylon is running, shocks any raider or hostile kin that comes near (about 20 m).", 1, 0.8f,
                ("ingot", 10), ("crystal", 6), ("stone", 10));
            turret.palette = Pal(Hex("#6c7288"), Hex("#3a3e4a"), Hex("#ffd84a"), Hex("#fff07a"));
            turret.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.3f, 0, 1.3f, 0.6f, 1.3f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.3f, 0, 0.5f, 1.6f, 0.5f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, 0, 2.2f, 0, 0.9f, 0.7f, 0.9f),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, 0, 2.9f, 0, 0.3f, 0.9f, 0.3f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 3.4f, 0, 0.34f, 0.34f, 0.34f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 2.2f, 0, 1.1f, 0.08f, 1.1f),
            };

            var pad = Bld(b, "waypad", "Waypad", "Links your outposts to the waystones: every outpost becomes a fast-travel destination (Map tab).", 1, 1.4f,
                ("sunsteel_bar", 6), ("crystal", 8), ("stone", 20));
            pad.palette = Pal(Hex("#8e8a92"), Hex("#6c6874"), Hex("#ffd860"), Hex("#7df2ff"));
            pad.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.12f, 0, 2.8f, 0.24f, 2.8f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.3f, 0, 2.2f, 0.16f, 2.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Glow, 0, 0.4f, 0, 1.5f, 0.06f, 1.5f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 1.1f, 0.3f, 0, 0.3f, 1.6f, 0.3f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -0.55f, 0.3f, 0.95f, 0.3f, 1.6f, 0.3f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -0.55f, 0.3f, -0.95f, 0.3f, 1.6f, 0.3f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 0.36f, 0, 2.3f, 0.05f, 2.3f),
            };
        }
    }
}
