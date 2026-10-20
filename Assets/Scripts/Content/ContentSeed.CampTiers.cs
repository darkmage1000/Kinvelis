using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>
        /// The camp grows with the journey (user, 2026-10-20: "new benches that basically replace old ones... make camp
        /// progress with us and give us a reason to gather more" + "more types of quarries for the different ores and
        /// crop plots for different crops"). Each structure costs the material of the land it belongs to.
        ///  - Crafting benches, a ladder (Base/Stations.cs): Workbench (1) -> Forge Bench (2) -> Artisan's Anvil (3) ->
        ///    Starforge (4). A recipe's tier comes from its materials; a bench works every order up to its own tier.
        ///  - Campfire (1) -> Kitchen (2): meals with ingredients from past the Sunmeadow. Smelter -> Blast Furnace: ingots twice as fast.
        ///  - Ore works, one per ore, each needing a Quarrying kin of a minimum level (`minJobLevel`).
        ///  - Gardens: each land's own plant, farmed in steps like a Crop Plot (Crops.Gardens).
        /// </summary>
        static void BuildCampTiers(Bundle b)
        {
            // ---------------------------------------------------------------- crafting benches
            var forgeP = Pal(Hex("#6e6460"), Hex("#3e3836"), Hex("#c07a4a"), Hex("#ff7a2a"));
            var forge = Bld(b, "forge_bench", "Forge Bench",
                "Tier 2 crafting bench, with a hearth and an anvil: works cinder, Rime Crystal, saltglass and bog iron, and makes everything the Workbench makes, so it can replace it. Crafting kin work its orders.",
                7, 1.5f, ("ingot", 20), ("cinder", 12), ("stone", 40), ("wood", 20));
            Bench(forge, 2, forgeP, new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.45f, 0, 2.4f, 0.9f, 1.2f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.95f, 0, 2.5f, 0.12f, 1.3f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.55f, 1.15f, 0, 0.7f, 0.28f, 0.34f),          // anvil
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.95f, 1.2f, 0, 0.3f, 0.16f, 0.2f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, -0.7f, 1.4f, -0.2f, 0.9f, 0.9f, 0.8f),           // hearth
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, -0.7f, 1.25f, 0.2f, 0.55f, 0.35f, 0.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, -0.7f, 2.3f, -0.3f, 0.4f, 1.0f, 0.4f),     // flue
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.2f, 1.08f, 0.35f, 0.08f, 0.5f, 0.08f, 0, 0, 80),
            });

            var anvilP = Pal(Hex("#4a4e5a"), Hex("#2a2c34"), Hex("#e0b050"), Hex("#ffd060"));
            var anvil = Bld(b, "artisan_anvil", "Artisan's Anvil",
                "Tier 3 crafting bench: a master's anvil with a sun-metal hammer. Works sunsteel and blightsteel and everything below, so it replaces the Forge Bench.",
                20, 1.6f, ("bogsteel_bar", 12), ("ingot", 30), ("crystal", 10), ("stone", 60));
            Bench(anvil, 3, anvilP, new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.3f, 0, 2.6f, 0.6f, 2.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.9f, 0, 0.8f, 0.7f, 0.7f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 1.38f, 0, 1.7f, 0.32f, 0.75f),                // anvil face
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, 1.05f, 1.38f, 0, 0.3f, 0.6f, 0.3f, 0, 0, -90), // horn
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -0.2f, 1.62f, 0.05f, 0.22f, 0.22f, 0.5f),        // gold hammer head
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, -0.2f, 1.62f, 0.55f, 0.05f, 0.5f, 0.05f, 90, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -1.25f, 1.1f, -0.6f, 0.12f, 1.6f, 0.12f),      // tool rack
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 1.25f, 1.1f, -0.6f, 0.12f, 1.6f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 1.85f, -0.6f, 2.6f, 0.1f, 0.12f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 1.56f, 0, 0.5f, 0.06f, 0.4f),
            });

            var starP = Pal(Hex("#2a2848"), Hex("#16142a"), Hex("#b0a0ff"), Hex("#d8d0ff"));
            var star = Bld(b, "starforge", "Starforge",
                "Tier 4 crafting bench: crystal spires hold a point of starlight over the anvil. Works starcrystal, starsteel and geode cores - the best things in the game - and everything below.",
                32, 1.7f, ("starcrystal", 12), ("blightsteel_bar", 8), ("sunsteel_bar", 8), ("crystal", 20), ("stone", 80));
            Bench(star, 4, starP, new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.25f, 0, 3.0f, 0.5f, 3.0f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.75f, 0, 1.6f, 0.6f, 1.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 1.2f, 0, 1.4f, 0.3f, 0.7f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Accent, 1.2f, 0.5f, 0.7f, 0.35f, 2.2f, 0.35f, 0, 0, -10),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Accent, -1.2f, 0.5f, 0.7f, 0.35f, 2.0f, 0.35f, 0, 0, 10),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Accent, 0, 0.5f, -1.3f, 0.4f, 2.4f, 0.4f, 10, 0, 0),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 2.4f, 0, 0.45f, 0.45f, 0.45f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 1.38f, 0, 0.6f, 0.08f, 0.4f),
            });

            // ---------------------------------------------------------------- cooking
            var kitchen = Bld(b, "kitchen", "Kitchen",
                "Tier 2 cooking station: a brick stove with an oven and a pot. Cooks meals with ingredients from beyond the Sunmeadow (peppers, mint, reef and mire fish, land plants) and everything the Campfire makes, so it can replace it. Kindling kin keep it hot.",
                6, 1.4f, ("stone", 30), ("wood", 25), ("ingot", 6), ("gel", 4));
            kitchen.station = CraftStation.Campfire; kitchen.benchTier = 2; kitchen.workType = WorkType.Kindling; kitchen.workerSlots = 2;
            kitchen.palette = Pal(Hex("#a8584a"), Hex("#5a4440"), Hex("#d8c8a0"), Hex("#ff8a2a"));
            kitchen.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.55f, 0, 2.2f, 1.1f, 1.2f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 1.15f, 0, 2.3f, 0.12f, 1.3f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.5f, 0.45f, 0.61f, 0.7f, 0.5f, 0.04f),       // oven door
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, -0.5f, 0.45f, 0.6f, 0.5f, 0.35f, 0.04f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0.5f, 1.4f, 0, 0.7f, 0.5f, 0.7f),         // pot
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.5f, 1.66f, 0, 0.6f, 0.04f, 0.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, -0.8f, 2.0f, -0.35f, 0.45f, 1.7f, 0.45f),        // chimney
            };

            var blast = Bld(b, "blast_furnace", "Blast Furnace",
                "A tall furnace that smelts Rustore into Ingots twice as fast as the Smelter, with room for two kin and twice the shelf. Kindling kin charge it, then it blasts by itself.",
                12, 1.6f, ("stone", 60), ("ingot", 20), ("cinder", 15));
            blast.workType = WorkType.Kindling; blast.workerSlots = 2; blast.outputItem = "ingot"; blast.outputCount = 1; blast.workPerUnit = 8f;   // the Smelter is 16: half the work per ingot
            blast.inputItem = "ore"; blast.inputCount = 2; blast.bufferCap = 60;
            blast.palette = Pal(Hex("#5a4e4a"), Hex("#2e2826"), Hex("#b08a6a"), Hex("#ff6a1a"));
            blast.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.3f, 0, 2.0f, 2.6f, 2.0f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 2.9f, 0, 1.2f, 0.7f, 1.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.12f, 0, 2.5f, 0.24f, 2.5f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 0.7f, 0.95f, 0.8f, 0.5f, 0.25f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.6f, 1.0f, 1.6f, 0.12f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 2.3f, 1.0f, 1.4f, 0.12f, 0.12f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 1.15f, 1.0f, 0, 0.3f, 2.0f, 0.3f),      // bellows pipe
            };

            // ---------------------------------------------------------------- ore works (Quarrying, a minimum level each)
            Ore(b, "cinder_pit", "Cinder Pit", "cinder", 1, 16f, 8, 1, "#ff6a2a",
                "A pit over a cinder vent. Quarrying kin rake the vents (any level), then the Cinder Shards cool by itself.",
                ("stone", 30), ("ingot", 6), ("cinder", 8));
            Ore(b, "saltglass_dredge", "Saltglass Dredge", "saltglass", 1, 18f, 9, 2, "#9ae8e0",
                "Scoops that sink into wet sand and come up with Saltglass. Needs a Quarrying kin of Lv 2 or more.",
                ("wood", 30), ("ingot", 6), ("saltglass", 8));
            Ore(b, "obsidian_cutter", "Obsidian Cutter", "obsidian", 1, 20f, 10, 2, "#3a2a4a",
                "Cleaves Obsidian from a block of volcanic glass. Needs a Quarrying kin of Lv 2 or more.",
                ("stone", 30), ("ingot", 8), ("obsidian", 6));
            Ore(b, "crystal_drill", "Crystal Drill", "crystal", 1, 20f, 13, 2, "#8fe8ff",
                "A cold drill that bores Rime Crystal from a frozen core. Needs a Quarrying kin of Lv 2 or more.",
                ("ingot", 10), ("crystal", 8), ("stone", 30));
            Ore(b, "bog_sluice", "Bog Iron Sluice", "bog_iron", 1, 20f, 15, 2, "#6a7a4a",
                "Mud goes in the top and Bog Iron settles in the riffles. Needs a Quarrying kin of Lv 2 or more.",
                ("wood", 30), ("ingot", 10), ("bog_iron", 8));
            Ore(b, "sunsteel_mine", "Sunsteel Mine", "sunsteel_ore", 1, 26f, 24, 3, "#f0c040",
                "A shaft into a gold-flecked seam. Needs a Quarrying kin of Lv 3 or more.",
                ("stone", 50), ("bogsteel_bar", 6), ("sunsteel_ore", 8));
            Ore(b, "blight_shaft", "Blight Shaft", "blight_ore", 1, 26f, 30, 3, "#7a4aa0",
                "A shored shaft where Blight Ore weeps out of the rock. Needs a Quarrying kin of Lv 3 or more.",
                ("stone", 50), ("bogsteel_bar", 8), ("blight_ore", 8));
            Ore(b, "star_excavator", "Star Excavator", "starcrystal", 1, 32f, 36, 3, "#b0a0ff",
                "Lenses draw starlight into a seed crystal until Starcrystal grows. Needs a Quarrying kin of Lv 3 or more.",
                ("stone", 60), ("blightsteel_bar", 8), ("starcrystal", 8));

            // ---------------------------------------------------------------- gardens (Crops.Gardens): each land's own plant
            Garden(b, "flower_bed", "Flower Bed", "honeybloom", 4, "#ffd860",
                "Grows Honeybloom from Petalrun.", ("wood", 12), ("fiber", 10), ("honeybloom", 3));
            Garden(b, "frost_bed", "Frost Bed", "frostcap", 12, "#a8d8ff",
                "Grows Frostcaps from the Shiverglass Field in cold, packed earth.", ("stone", 12), ("crystal", 2), ("frostcap", 3));
            Garden(b, "lantern_bog", "Lantern Bog", "lanternbud", 13, "#ffe070",
                "A wet bed that grows Lanternbuds from Lanternfen.", ("wood", 15), ("gel", 6), ("lanternbud", 3));
            Garden(b, "fig_orchard", "Fig Orchard", "sunfig", 21, "#e07a4a",
                "Grows Sunfigs from the Verdant Oasis.", ("wood", 25), ("fiber", 10), ("sunfig", 3));
            Garden(b, "spore_log", "Spore Log Bed", "sporecap", 28, "#c8a0e0",
                "Logs in damp soil that grow Sporecaps from Sporefall.", ("wood", 20), ("gel", 8), ("sporecap", 3));
            Garden(b, "moss_terrace", "Moss Terrace", "starmoss", 34, "#a0f0ff",
                "Stone terraces that grow Starmoss from the Starmoss Fields.", ("stone", 25), ("starcrystal", 2), ("starmoss", 3));

            // pearls: Fishing kin seed the beds, then the pearls grow by themselves
            var oyster = Bld(b, "oyster_bed", "Oyster Bed", "A tank of seawater and shells. Fishing kin seed the oysters, then Sea Pearls grow by themselves.",
                9, 1.5f, ("stone", 15), ("saltglass", 4), ("sea_pearl", 3));
            oyster.workType = WorkType.Fishing; oyster.workerSlots = 1; oyster.outputItem = "sea_pearl"; oyster.outputCount = 1; oyster.workPerUnit = 30f;
            oyster.palette = Pal(Hex("#8e8a92"), Hex("#3a7aa8"), Hex("#f0e8f4"), Hex("#c8f0ff"));
            oyster.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.35f, 0, 2.6f, 0.7f, 1.8f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.68f, 0, 2.3f, 0.06f, 1.5f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.6f, 0.74f, -0.3f, 0.4f, 0.12f, 0.32f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.3f, 0.74f, 0.35f, 0.4f, 0.12f, 0.32f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.8f, 0.74f, -0.4f, 0.36f, 0.12f, 0.3f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0.3f, 0.82f, 0.35f, 0.1f, 0.1f, 0.1f),
            };
        }

        static void Bench(BuildingDef d, int tier, Palette pal, BodyPart[] parts)
        {
            d.station = CraftStation.Workbench; d.benchTier = tier; d.workType = WorkType.Crafting; d.workerSlots = 2;
            d.palette = pal; d.parts = parts;
        }

        static void Ore(Bundle b, string id, string name, string output, int count, float work, int lvl, int minLevel, string col, string desc, params (string, int)[] cost)
        {
            var d = Bld(b, id, name, desc, lvl, 1.6f, cost);
            d.workType = WorkType.Quarrying; d.workerSlots = 2; d.outputItem = output; d.outputCount = count; d.workPerUnit = work;
            d.minJobLevel = minLevel;
            d.palette = Pal(Hex("#7a7068"), Hex("#4a4440"), Hex("#b0a090"), Hex(col));
            d.parts = new[]
            {
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, 0, 0.6f, 0, 2.4f, 1.3f, 2.0f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.5f, 0.8f, 0.3f, 0.65f, 1.5f, 0.65f, 0, 0, -15),     // the ore, in its own colour
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -0.4f, 0.9f, -0.3f, 0.55f, 1.2f, 0.55f, 10, 0, 20),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.1f, 1.0f, -0.6f, 0.45f, 1.0f, 0.45f, -15, 0, 0),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Glow, 1.5f, 0.22f, -0.7f, 0.6f, 0.4f, 0.55f),                 // a heap of what it has dug
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Glow, 1.85f, 0.18f, -0.25f, 0.45f, 0.32f, 0.45f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Glow, 1.3f, 0.16f, -1.15f, 0.4f, 0.3f, 0.4f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -1.2f, 1.2f, 0.8f, 0.14f, 2.2f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 1.2f, 1.2f, 0.8f, 0.14f, 2.2f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 2.3f, 0.8f, 2.5f, 0.14f, 0.18f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 1.9f, 0.8f, 0.35f, 0.2f, 0.35f, 90, 0, 0),
            };
        }

        static void Garden(Bundle b, string id, string name, string crop, int lvl, string col, string what, params (string, int)[] cost)
        {
            var d = Bld(b, id, name,
                what + " Planted, watered, grown and harvested like a Crop Plot: Cultivating kin plant it, Irrigating kin water it, it grows by itself, then Cultivating or Foraging kin harvest. This is the only way to farm it.",
                lvl, 1.8f, cost);
            d.workType = WorkType.Cultivating; d.secondaryWorkType = WorkType.Irrigating; d.workerSlots = 2;
            d.outputItem = crop; d.outputCount = 2; d.workPerUnit = 16f;
            d.palette = Pal(Hex("#5a3e28"), Hex("#8a7a6a"), Hex(col), Hex(col));
            d.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.12f, 0, 3.0f, 0.25f, 2.2f),            // the soil (CropVisual finds it)
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.16f, 1.15f, 3.2f, 0.32f, 0.14f),     // border
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.16f, -1.15f, 3.2f, 0.32f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 1.55f, 0.16f, 0, 0.14f, 0.32f, 2.3f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -1.55f, 0.16f, 0, 0.14f, 0.32f, 2.3f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 1.55f, 0.55f, 1.15f, 0.12f, 0.8f, 0.12f),     // a marker post in the plant's colour
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 1.55f, 0.95f, 1.15f, 0.4f, 0.24f, 0.06f),
            };
        }
    }
}
