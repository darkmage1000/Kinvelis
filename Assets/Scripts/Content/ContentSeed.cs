using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Source-of-truth seed data for all Stage-1 content. The editor tool
    /// (Wildbond > Generate Content) writes these out as ScriptableObject assets under
    /// Assets/Resources/Content so designers can tune them in the inspector.
    /// </summary>
    public static partial class ContentSeed
    {
        public class Bundle
        {
            public readonly List<ItemDef> items = new List<ItemDef>();
            public readonly List<RecipeDef> recipes = new List<RecipeDef>();
            public readonly List<AbilityDef> abilities = new List<AbilityDef>();
            public readonly List<TraitDef> traits = new List<TraitDef>();
            public readonly List<SpeciesDef> species = new List<SpeciesDef>();
            public readonly List<BuildingDef> buildings = new List<BuildingDef>();
        }

        public static Bundle Build()
        {
            var b = new Bundle();
            BuildItems(b);
            BuildRecipes(b);
            BuildGear(b);
            BuildMeals(b);
            BuildTechItems(b);
            BuildRegionItems(b);
            BuildFishingItems(b);
            BuildRangedItems(b);
            BuildAbilities(b);
            BuildNinjaAbilities(b);
            BuildStyledAbilities(b);
            BuildTraits(b);
            BuildSpecies(b);
            BuildStarters(b);
            BuildRoster(b);
            BuildWave2(b);
            BuildWave3(b);
            BuildWardenContent(b);
            BuildLegends(b);
            BuildEarlyEmber(b);
            BuildNinjas(b);
            BuildStyled(b);
            ApplySpawnConditions(b);
            ApplyOuterBiomes(b);
            ApplyEarlyEmber(b);
            ApplyJobBalance(b);
            BuildBuildings(b);
            BuildTechBuildings(b);
            BuildStorageBuildings(b);
            BuildFishingBuildings(b);
            ApplyBuildCategories(b);
            return b;
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        static Palette Pal(Color p, Color s, Color a, Color glow, Color? eye = null) =>
            new Palette { primary = p, secondary = s, accent = a, glow = glow, eye = eye ?? new Color(0.08f, 0.07f, 0.1f) };

        // ================================================================ ITEMS
        static ItemDef Item(Bundle b, string id, string name, ItemCategory cat, string glyph, Color col, string desc, int stack = 99)
        {
            var d = ScriptableObject.CreateInstance<ItemDef>();
            d.name = "Item_" + id; d.id = id; d.displayName = name; d.category = cat; d.iconGlyph = glyph; d.iconColor = col; d.description = desc; d.maxStack = stack;
            b.items.Add(d);
            return d;
        }

        static void BuildItems(Bundle b)
        {
            Item(b, "wood", "Wood", ItemCategory.Resource, "Wd", Hex("#9b6a3c"), "Sturdy timber from trees.");
            Item(b, "stone", "Stone", ItemCategory.Resource, "St", Hex("#9a9aa3"), "Common rock. Building basics.");
            Item(b, "fiber", "Fiber", ItemCategory.Resource, "Fb", Hex("#9fd36b"), "Tough plant fibers for binding.");
            Item(b, "ore", "Rustore", ItemCategory.Resource, "Or", Hex("#c07a4a"), "Metal-bearing rock. Smelt into ingots.");
            Item(b, "ingot", "Ingot", ItemCategory.Resource, "In", Hex("#d8d8e6"), "Refined metal.");
            Item(b, "crystal", "Rime Crystal", ItemCategory.Resource, "Cr", Hex("#8fe8ff"), "Humming crystal from the Rimefang Peaks.");
            Item(b, "prism_shard", "Prism Shard", ItemCategory.Resource, "Ps", Hex("#ff9ae0"), "Rainbow-veined crystal from Prism Veins, found only on the far, high slopes of the Rimefang Peaks (needs an Ingot Pick). 100 make a Shimmer Prism.");
            Item(b, "cinder", "Cinder Shard", ItemCategory.Resource, "Cd", Hex("#ff6a2a"), "Still-warm shard from Cinderreach vents.");
            Item(b, "pelt", "Kin Fluff", ItemCategory.Resource, "Fl", Hex("#f2e6c9"), "Soft fluff shed by kin.");
            Item(b, "gel", "Dew Gel", ItemCategory.Resource, "Gl", Hex("#6fd0ff"), "Cool, springy gel.");

            var berry = Item(b, "berry", "Sunberry", ItemCategory.Food, "Be", Hex("#ff5a7a"), "Sweet berry. Restores a little hunger.");
            berry.foodValue = 12f; berry.healValue = 2f;
            var tart = Item(b, "berry_tart", "Berry Tart", ItemCategory.Food, "Ta", Hex("#ff9a5a"), "Baked treat. Big hunger + heal.", 30);
            tart.foodValue = 40f; tart.healValue = 20f;

            var salve = Item(b, "salve", "Healing Salve", ItemCategory.Consumable, "+", Hex("#7affa0"), "Restores 45 HP.", 30);
            salve.healValue = 45f;
            var treat = Item(b, "kin_treat", "Kin Treat", ItemCategory.Consumable, "Kt", Hex("#ffd36a"), "Fully heals and revives your party kin.", 30);
            treat.kinHealValue = 9999f;
            var satchel = Item(b, "satchel", "Explorer Satchel", ItemCategory.Upgrade, "Bg", Hex("#c9a36a"), "Use to permanently add 8 inventory slots.", 5);
            satchel.bonusInventorySlots = 8;

            var sa = Item(b, "stone_axe", "Stone Hatchet", ItemCategory.Tool, "Ax", Hex("#b0906a"), "Chops trees faster. Tier 2 wood.", 1); sa.toolKind = ToolKind.Axe; sa.toolPower = 2;
            var sp = Item(b, "stone_pick", "Stone Pick", ItemCategory.Tool, "Pk", Hex("#a0a0a8"), "Breaks rock and rustore.", 1); sp.toolKind = ToolKind.Pick; sp.toolPower = 2;
            var ia = Item(b, "iron_axe", "Ingot Axe", ItemCategory.Tool, "Ax", Hex("#e0e0f0"), "Tier 3 axe.", 1); ia.toolKind = ToolKind.Axe; ia.toolPower = 3;
            var ip = Item(b, "iron_pick", "Ingot Pick", ItemCategory.Tool, "Pk", Hex("#e0e0f0"), "Tier 3 pick. Breaks crystal.", 1); ip.toolKind = ToolKind.Pick; ip.toolPower = 3;

            var ss = Item(b, "stone_sickle", "Flint Sickle", ItemCategory.Tool, "Sk", Hex("#a6b07a"), "Sweeps through fiber plants and berry bushes in front of you. +50% yield.", 1); ss.toolKind = ToolKind.Sickle; ss.toolPower = 2;
            var isk = Item(b, "iron_sickle", "Ingot Sickle", ItemCategory.Tool, "Sk", Hex("#e0e0f0"), "Wider, sharper sweep. +100% fiber & berry yield.", 1); isk.toolKind = ToolKind.Sickle; isk.toolPower = 3;
            Item(b, "gale_glider", "Gale Glider", ItemCategory.KeyItem, "Gl", Hex("#7ac860"), "Tap JUMP again in mid-air to glide: slow fall, fast forward drift, no fall damage. Uses stamina. Not consumed.", 1);
            Item(b, "vine_grapple", "Vine Grapple", ItemCategory.KeyItem, "Hk", Hex("#5a9a3a"), "A HOOK button appears: aim the camera at ground or a cliff within 30 m and zip there. Not consumed.", 1);
            Item(b, "provision_pouch", "Provision Pouch", ItemCategory.KeyItem, "Pp", Hex("#c8884a"), "Auto-feed while it's in your bag: you eat when you get hungry, hungry camp kin are fed from storage or your bag while you're in camp, and a badly hurt companion gets a Kin Treat. Switches are on this page. Not consumed.", 1);
            Item(b, "shimmer_prism", "Shimmer Prism", ItemCategory.KeyItem, "Sp", Hex("#ffd0f0"), "Lets a shimmer kin switch between its three shimmer colours (Kin tab > pick the kin). Works as often as you like. Not consumed.", 1);
            Item(b, "kin_harness", "Kin Harness", ItemCategory.KeyItem, "Hr", Hex("#b07a3a"), "Lets you ride mountable kin (Barkhorn, Frostuft...). Summon one and tap RIDE. Not consumed.", 1);

            var club = Item(b, "wood_club", "Knotwood Club", ItemCategory.Weapon, "Cl", Hex("#8a5a30"), "A heavy stick. Attack power 11.", 1); club.weaponDamage = 11f;
            var spear = Item(b, "stone_spear", "Flint Spear", ItemCategory.Weapon, "Sp", Hex("#a8a8b0"), "Long reach. Attack power 17.", 1); spear.weaponDamage = 17f;
            var blade = Item(b, "iron_blade", "Ingot Blade", ItemCategory.Weapon, "Bl", Hex("#e8e8ff"), "Keen metal edge. Attack power 27.", 1); blade.weaponDamage = 27f;

            var s1 = Item(b, "tether_seed", "Tether Seed", ItemCategory.CaptureSeed, "T1", Hex("#8fd96a"), "Throw at a weakened kin. Vines bind it into the seed. Rated for kin up to Lv 20.", 50); s1.captureMult = 1f;
            var s2 = Item(b, "ironbark_seed", "Ironbark Seed", ItemCategory.CaptureSeed, "T2", Hex("#c9b27a"), "Metal-banded seed. Fewer break-frees; rated for kin up to Lv 40.", 50); s2.captureMult = 1.6f;
            var s3 = Item(b, "starbloom_seed", "Starbloom Seed", ItemCategory.CaptureSeed, "T3", Hex("#ffe98a"), "Crystal-cored seed. Rarely breaks; works on kin of any level.", 50); s3.captureMult = 2.4f;
        }

        // ================================================================ RECIPES
        static void Recipe(Bundle b, string output, int count, CraftStation st, float work, int lvl, params (string, int)[] inputs)
        {
            var r = ScriptableObject.CreateInstance<RecipeDef>();
            r.id = output; r.name = "Recipe_" + output; r.outputItem = output; r.outputCount = count; r.station = st; r.work = work; r.unlockLevel = lvl;
            r.inputs = new Ingredient[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) r.inputs[i] = new Ingredient(inputs[i].Item1, inputs[i].Item2);
            b.recipes.Add(r);
        }

        static void BuildRecipes(Bundle b)
        {
            Recipe(b, "tether_seed", 3, CraftStation.Hand, 0, 1, ("fiber", 1), ("wood", 2), ("stone", 1));
            Recipe(b, "wood_club", 1, CraftStation.Hand, 0, 1, ("wood", 5));
            Recipe(b, "stone_axe", 1, CraftStation.Hand, 0, 1, ("wood", 3), ("stone", 3), ("fiber", 2));
            Recipe(b, "stone_pick", 1, CraftStation.Hand, 0, 1, ("wood", 3), ("stone", 4), ("fiber", 2));
            Recipe(b, "salve", 1, CraftStation.Hand, 0, 2, ("berry", 3), ("fiber", 1));
            Recipe(b, "stone_sickle", 1, CraftStation.Hand, 0, 1, ("wood", 2), ("stone", 2));
            Recipe(b, "iron_sickle", 1, CraftStation.Workbench, 25, 4, ("ingot", 2), ("wood", 2));
            Recipe(b, "kin_harness", 1, CraftStation.Workbench, 20, 2, ("pelt", 3), ("fiber", 8), ("wood", 4));
            Recipe(b, "shimmer_prism", 1, CraftStation.Workbench, 60, 12, ("prism_shard", 100), ("crystal", 10), ("ingot", 5));
            Recipe(b, "provision_pouch", 1, CraftStation.Workbench, 20, 2, ("pelt", 2), ("fiber", 10), ("berry", 5));
            Recipe(b, "gale_glider", 1, CraftStation.Workbench, 30, 5, ("pelt", 6), ("fiber", 20), ("wood", 8), ("gel", 2));
            Recipe(b, "vine_grapple", 1, CraftStation.Workbench, 30, 6, ("fiber", 25), ("ingot", 3), ("gel", 4));
            Recipe(b, "stone_spear", 1, CraftStation.Workbench, 15, 2, ("wood", 4), ("stone", 3), ("fiber", 3));
            Recipe(b, "satchel", 1, CraftStation.Workbench, 25, 3, ("pelt", 4), ("fiber", 10));
            Recipe(b, "ironbark_seed", 3, CraftStation.Workbench, 20, 4, ("ingot", 1), ("wood", 3), ("fiber", 3));
            Recipe(b, "iron_axe", 1, CraftStation.Workbench, 30, 4, ("ingot", 3), ("wood", 3));
            Recipe(b, "iron_pick", 1, CraftStation.Workbench, 30, 4, ("ingot", 3), ("wood", 3));
            Recipe(b, "iron_blade", 1, CraftStation.Workbench, 40, 6, ("ingot", 5), ("wood", 2), ("pelt", 2));
            Recipe(b, "starbloom_seed", 2, CraftStation.Workbench, 35, 8, ("crystal", 2), ("ingot", 1), ("fiber", 2));
            Recipe(b, "berry_tart", 1, CraftStation.Campfire, 10, 1, ("berry", 5));
            Recipe(b, "kin_treat", 2, CraftStation.Campfire, 12, 2, ("berry", 3), ("gel", 1));
        }

        // ================================================================ ABILITIES
        static void Ab(Bundle b, string id, string name, Element e, AbilityKind k, float power, float range, float cd,
            StatusType st = StatusType.None, float stChance = 0f, float radius = 3f, float projSpeed = 16f)
        {
            var a = ScriptableObject.CreateInstance<AbilityDef>();
            a.id = id; a.name = "Ability_" + id; a.displayName = name; a.element = e; a.kind = k; a.power = power; a.range = range;
            a.cooldown = cd; a.status = st; a.statusChance = stChance; a.radius = radius; a.projectileSpeed = projSpeed;
            a.color = ElementInfo.Color(e);
            a.windup = k == AbilityKind.Melee ? 0.3f : (k == AbilityKind.Dash ? 0.4f : 0.45f);
            b.abilities.Add(a);
        }

        static void BuildAbilities(Bundle b)
        {
            Ab(b, "tackle", "Tumble Tackle", Element.Normal, AbilityKind.Melee, 12, 1.8f, 2.2f);
            Ab(b, "leaf_dart", "Leaf Dart", Element.Flora, AbilityKind.Projectile, 16, 12, 3f);
            Ab(b, "spore_puff", "Spore Puff", Element.Flora, AbilityKind.Burst, 10, 3.5f, 6f, StatusType.Drowsy, 0.4f, 3.5f);
            Ab(b, "pebble_toss", "Pebble Toss", Element.Stone, AbilityKind.Projectile, 18, 11, 3.2f, StatusType.None, 0, 3, 13f);
            Ab(b, "shell_slam", "Shell Slam", Element.Stone, AbilityKind.Melee, 24, 2f, 4f);
            Ab(b, "ember_spit", "Ember Spit", Element.Ember, AbilityKind.Projectile, 18, 12, 3f, StatusType.Burn, 0.25f);
            Ab(b, "flame_dash", "Flame Dash", Element.Ember, AbilityKind.Dash, 22, 9, 5f, StatusType.Burn, 0.2f, 7f);
            Ab(b, "bubble_burst", "Bubble Burst", Element.Tide, AbilityKind.Projectile, 16, 11, 2.8f, StatusType.Soaked, 0.3f, 3, 12f);
            Ab(b, "spark_peck", "Spark Peck", Element.Volt, AbilityKind.Melee, 16, 1.9f, 2.5f, StatusType.Shock, 0.3f);
            Ab(b, "thunder_chirp", "Thunder Chirp", Element.Volt, AbilityKind.Burst, 20, 4f, 6f, StatusType.Shock, 0.25f, 4.5f);
            Ab(b, "frost_breath", "Frost Breath", Element.Frost, AbilityKind.Projectile, 18, 10, 3.5f, StatusType.Chill, 0.35f, 3, 11f);
            Ab(b, "antler_charge", "Antler Charge", Element.Flora, AbilityKind.Dash, 28, 10, 5.5f, StatusType.None, 0, 8f);
            Ab(b, "magma_maw", "Magma Maw", Element.Ember, AbilityKind.Melee, 32, 2.4f, 4.5f, StatusType.Burn, 0.4f);
            Ab(b, "prism_dust", "Prism Dust", Element.Lumen, AbilityKind.Burst, 18, 4f, 6f, StatusType.Drowsy, 0.25f, 4f);
            Ab(b, "crystal_shard", "Crystal Shard", Element.Frost, AbilityKind.Projectile, 22, 13, 3.4f, StatusType.Chill, 0.15f, 3, 18f);
            Ab(b, "shadow_pounce", "Shadow Pounce", Element.Shade, AbilityKind.Dash, 20, 8, 4f, StatusType.Drowsy, 0.15f, 6f);
            Ab(b, "hex_flicker", "Hex Flicker", Element.Shade, AbilityKind.Projectile, 18, 12, 3f, StatusType.None, 0, 3, 15f);
        }

        // ================================================================ TRAITS
        static TraitDef Tr(Bundle b, string id, string name, string desc, bool pos, float weight)
        {
            var t = ScriptableObject.CreateInstance<TraitDef>();
            t.id = id; t.name = "Trait_" + id; t.displayName = name; t.description = desc; t.positive = pos; t.weight = weight;
            b.traits.Add(t);
            return t;
        }

        static void BuildTraits(Bundle b)
        {
            Tr(b, "swift", "Swift", "+15% move speed.", true, 1f).speedMul = 1.15f;
            Tr(b, "brawny", "Brawny", "+15% attack.", true, 1f).atkMul = 1.15f;
            Tr(b, "sturdy", "Sturdy", "+15% defense.", true, 1f).defMul = 1.15f;
            Tr(b, "hearty", "Hearty", "+15% max HP.", true, 1f).hpMul = 1.15f;
            Tr(b, "diligent", "Diligent", "+25% work speed.", true, 0.8f).workMul = 1.25f;
            Tr(b, "tireless", "Tireless", "Gets hungry 30% slower.", true, 0.8f).hungerMul = 0.7f;
            Tr(b, CreatureData.NightOwl, "Night Owl", "Stays awake at camp and works through the night.", true, 0.7f);
            Tr(b, "lazy", "Lazy", "-20% work speed.", false, 0.9f).workMul = 0.8f;
            Tr(b, "frail", "Frail", "-15% max HP.", false, 0.9f).hpMul = 0.85f;
            Tr(b, "glutton", "Glutton", "Gets hungry 50% faster.", false, 0.9f).hungerMul = 1.5f;
            Tr(b, "clumsy", "Clumsy", "-10% attack.", false, 0.9f).atkMul = 0.9f;
            var t1 = Tr(b, "sunborn", "Sunborn", "+20% Ember damage.", true, 0.5f); t1.elementBoost = Element.Ember; t1.elementBoostMul = 1.2f;
            var t2 = Tr(b, "tidecaller", "Tidecaller", "+20% Tide damage.", true, 0.5f); t2.elementBoost = Element.Tide; t2.elementBoostMul = 1.2f;
            var t3 = Tr(b, "overgrown", "Overgrown", "+20% Flora damage.", true, 0.5f); t3.elementBoost = Element.Flora; t3.elementBoostMul = 1.2f;
            var t4 = Tr(b, "stormtouched", "Stormtouched", "+20% Volt damage.", true, 0.5f); t4.elementBoost = Element.Volt; t4.elementBoostMul = 1.2f;
            var t5 = Tr(b, "frostbound", "Frostbound", "+20% Frost damage.", true, 0.5f); t5.elementBoost = Element.Frost; t5.elementBoostMul = 1.2f;
            var apex = Tr(b, "apex", "Apex", "Alpha bloodline: +20% HP, attack, defense.", true, 0f);
            apex.hpMul = 1.2f; apex.atkMul = 1.2f; apex.defMul = 1.2f;
            var rad = Tr(b, "radiant", "Radiant", "Shimmer coat: +10% HP and work speed, +1 level in every camp job.", true, 0f);
            rad.hpMul = 1.1f; rad.workMul = 1.1f;
            var cry = Tr(b, "crystalline", "Crystalline", "Mutation: crystal growths, +25% defense, -10% speed.", true, 0f);
            cry.defMul = 1.25f; cry.speedMul = 0.9f;
            var gi = Tr(b, "colossal", "Colossal", "Mutation: oversized, +25% HP, -10% speed.", true, 0f);
            gi.hpMul = 1.25f; gi.speedMul = 0.9f;
            var gl = Tr(b, "luminous", "Luminous", "Mutation: glowing markings, +15% attack.", true, 0f);
            gl.atkMul = 1.15f;
            var tw = Tr(b, "twinned", "Twinned", "Mutation: doubled tail, +15% speed, +10% work.", true, 0f);
            tw.speedMul = 1.15f; tw.workMul = 1.1f;
        }

        // ================================================================ BUILDINGS
        static BuildingDef Bld(Bundle b, string id, string name, string desc, int lvl, float footprint, params (string, int)[] cost)
        {
            var d = ScriptableObject.CreateInstance<BuildingDef>();
            d.id = id; d.name = "Building_" + id; d.displayName = name; d.description = desc; d.unlockLevel = lvl; d.footprint = footprint;
            d.cost = new Ingredient[cost.Length];
            for (int i = 0; i < cost.Length; i++) d.cost[i] = new Ingredient(cost[i].Item1, cost[i].Item2);
            b.buildings.Add(d);
            return d;
        }

        static void BuildBuildings(Bundle b)
        {
            var stoneP = Pal(Hex("#8e8a92"), Hex("#6c6874"), Hex("#b5b0a0"), Hex("#7df2ff"));
            var woodP = Pal(Hex("#a8743f"), Hex("#7a5230"), Hex("#d9c08f"), Hex("#ffd36a"));

            var core = Bld(b, "hearthstone", "Hearthstone", "The heart of your camp. Claims land, stores kin and items. Only one.", 1, 1.6f,
                ("stone", 10), ("wood", 5), ("fiber", 5));
            core.isCore = true; core.storageSlots = 24; core.workType = WorkType.Hauling; core.workerSlots = 2; core.palette = stoneP;
            core.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 3.2f, 0.3f, 3.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.4f, 0, 2.2f, 0.3f, 2.2f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0, 0.5f, 0, 0.9f, 2.4f, 0.9f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.5f, 0.5f, 0.2f, 0.4f, 1.1f, 0.4f, 0, 0, -20),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -0.45f, 0.5f, -0.25f, 0.35f, 0.9f, 0.35f, 10, 0, 25),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, 1.2f, 0.35f, 0.6f, 0.6f, 0.5f, 0.6f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, -1.1f, 0.35f, 0.8f, 0.5f, 0.45f, 0.5f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, 0.1f, 0.35f, -1.3f, 0.55f, 0.5f, 0.55f),
            };

            var wb = Bld(b, "workbench", "Workbench", "Crafting station. Queued orders are worked by Crafting kin (and you, if nearby).", 1, 1.3f, ("wood", 8), ("stone", 3));
            wb.station = CraftStation.Workbench; wb.workType = WorkType.Crafting; wb.workerSlots = 2; wb.palette = woodP;
            wb.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.9f, 0, 2.0f, 0.15f, 1.0f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.85f, 0.42f, -0.4f, 0.14f, 0.85f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.85f, 0.42f, -0.4f, 0.14f, 0.85f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.85f, 0.42f, 0.4f, 0.14f, 0.85f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.85f, 0.42f, 0.4f, 0.14f, 0.85f, 0.14f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0.4f, 1.05f, 0.1f, 0.5f, 0.12f, 0.3f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, -0.5f, 1.1f, 0.0f, 0.12f, 0.3f, 0.12f),
            };

            var chest = Bld(b, "storage_chest", "Storage Chest", "+20 storage slots with contents of their own: tap OPEN to put things in or take them out. Haulers deliver here.", 1, 1.0f, ("wood", 12));
            chest.storageSlots = 20; chest.palette = woodP;
            chest.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.4f, 0, 1.4f, 0.8f, 0.9f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.85f, 0, 1.5f, 0.15f, 1.0f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 0.6f, 0.46f, 0.2f, 0.25f, 0.05f),
            };

            var mill = Bld(b, "lumber_mill", "Lumber Mill", "Logging kin saw a batch of Wood, then it seasons by itself while they do something else.", 1, 1.6f, ("wood", 15), ("stone", 5));
            mill.workType = WorkType.Logging; mill.workerSlots = 2; mill.outputItem = "wood"; mill.outputCount = 1; mill.workPerUnit = 8f; mill.palette = woodP;
            mill.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.3f, 0, 2.4f, 0.6f, 1.4f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.8f, 0, 0.7f, 2.4f, 0.7f, 0, 0, 90),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.9f, 1.2f, 0.5f, 0.9f, 0.06f, 0.9f, 90, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.9f, 0.9f, 0.5f, 0.15f, 0.9f, 0.15f),
            };

            var quarry = Bld(b, "stone_quarry", "Stone Quarry", "Quarrying kin set the wedges, then the Stone splits by itself while they do something else.", 1, 1.8f, ("wood", 10), ("stone", 10));
            quarry.workType = WorkType.Quarrying; quarry.workerSlots = 2; quarry.outputItem = "stone"; quarry.outputCount = 1; quarry.workPerUnit = 8f; quarry.palette = stoneP;
            quarry.parts = new[]
            {
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, 0, 0.7f, 0, 2.6f, 1.6f, 2.2f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Secondary, 1.1f, 0.4f, 0.8f, 1.0f, 0.8f, 1.0f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -1.3f, 0.6f, 0.9f, 0.12f, 1.2f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, -1.3f, 1.2f, 0.9f, 0.9f, 0.1f, 0.1f),
            };

            var plot = Bld(b, "berry_plot", "Crop Plot", "Grows Sunberries, or any crop you plant (CROP in Camp > Structures), a batch at a time: Cultivating kin plant it, Irrigating kin water it (a farmer can, slowly), it grows by itself, then Cultivating or Foraging kin harvest.", 1, 1.8f, ("wood", 5), ("fiber", 5), ("berry", 3));
            plot.workType = WorkType.Cultivating; plot.secondaryWorkType = WorkType.Irrigating; plot.workerSlots = 2; plot.outputItem = "berry"; plot.outputCount = 2; plot.workPerUnit = 12f;
            plot.palette = Pal(Hex("#6b4a2e"), Hex("#4f9c3a"), Hex("#ff5a7a"), Hex("#ffe070"));
            plot.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.12f, 0, 3.0f, 0.25f, 2.2f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, -0.9f, 0.45f, -0.5f, 0.7f, 0.6f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0f, 0.45f, -0.5f, 0.7f, 0.6f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0.9f, 0.45f, -0.5f, 0.7f, 0.6f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, -0.9f, 0.45f, 0.5f, 0.7f, 0.6f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0f, 0.45f, 0.5f, 0.7f, 0.6f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0.9f, 0.45f, 0.5f, 0.7f, 0.6f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.7f, 0.7f, -0.35f, 0.16f, 0.16f, 0.16f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.2f, 0.72f, -0.6f, 0.16f, 0.16f, 0.16f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 1.0f, 0.7f, 0.65f, 0.16f, 0.16f, 0.16f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.2f, 0.72f, 0.4f, 0.16f, 0.16f, 0.16f),
            };

            var hut = Bld(b, "forage_hut", "Forage Hut", "Foraging kin bring in a batch of Fiber (and the odd berry), then it sorts and dries by itself.", 1, 1.4f, ("wood", 10), ("fiber", 10));
            hut.workType = WorkType.Foraging; hut.workerSlots = 1; hut.outputItem = "fiber"; hut.outputCount = 2; hut.workPerUnit = 8f;
            hut.palette = Pal(Hex("#c9a86a"), Hex("#8a6a40"), Hex("#6ab04c"), Hex("#ffd36a"));
            hut.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.5f, 0, 2.0f, 1.0f, 2.0f),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, 0, 1.5f, 0, 2.6f, 1.2f, 2.6f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 1.2f, 0.3f, 0.6f, 0.7f, 0.5f, 0.7f),
            };

            var smelter = Bld(b, "smelter", "Smelter", "Kindling kin stoke it with 4 Rustore from camp storage, then it smelts 2 Ingots by itself.", 3, 1.4f, ("stone", 20), ("wood", 5));
            smelter.workType = WorkType.Kindling; smelter.workerSlots = 1; smelter.outputItem = "ingot"; smelter.outputCount = 1; smelter.workPerUnit = 16f;
            smelter.inputItem = "ore"; smelter.inputCount = 2; smelter.palette = Pal(Hex("#7a6e6a"), Hex("#4d4442"), Hex("#b5a79a"), Hex("#ff7a2a"));
            smelter.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.8f, 0, 1.8f, 1.6f, 1.8f, 0, 0, 0),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 1.9f, 0, 0.6f, 0.8f, 0.6f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 0.6f, 0.75f, 0.7f, 0.5f, 0.3f),
            };

            var fire = Bld(b, "campfire", "Campfire", "Cooking station. Kindling kin keep the fire going.", 1, 1.0f, ("wood", 6), ("stone", 4));
            fire.station = CraftStation.Campfire; fire.workType = WorkType.Kindling; fire.workerSlots = 1;
            fire.palette = Pal(Hex("#8e8a92"), Hex("#6b4424"), Hex("#ffb13a"), Hex("#ff6a1a"));
            fire.parts = new[]
            {
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, 0.55f, 0.12f, 0, 0.35f, 0.25f, 0.35f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, -0.55f, 0.12f, 0, 0.35f, 0.25f, 0.35f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, 0, 0.12f, 0.55f, 0.35f, 0.25f, 0.35f),
                P(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, 0, 0.12f, -0.55f, 0.35f, 0.25f, 0.35f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 0.12f, 0.9f, 0.12f, 0, 0, 80),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 0.12f, 0.9f, 0.12f, 80, 0, 0),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Glow, 0, 0.45f, 0, 0.5f, 0.7f, 0.5f),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, 0, 0.45f, 0, 0.3f, 0.9f, 0.3f),
            };

            var feed = Bld(b, "feed_box", "Feed Box", "Camp kin eat from this box. Six slots of its own for kin food (tap FEED BOX); it keeps itself stocked from camp storage unless you switch that off.", 1, 0.9f, ("wood", 6));
            feed.feeds = true; feed.palette = Pal(Hex("#a8743f"), Hex("#7a5230"), Hex("#ff5a7a"), Hex("#ffd36a"));
            feed.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.3f, 0, 1.2f, 0.6f, 0.8f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.3f, 0.62f, 0, 0.3f, 0.25f, 0.3f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.2f, 0.62f, 0.1f, 0.3f, 0.25f, 0.3f),
            };

            // ---- automation tier 2 ----
            var pylon = Bld(b, "volt_pylon", "Volt Pylon", "Powers the whole camp (Conveyor Hub, Auto-Assembler, Sprinklers, Turrets). A Charging kin fills it like a battery, then is free for other work and comes back to top it up at half charge and before nightfall. Holds a fifth of a day; build Volt Batteries to store more.", 7, 1.0f,
                ("ingot", 6), ("crystal", 2), ("stone", 10));
            pylon.workType = WorkType.Charging; pylon.workerSlots = 1;
            pylon.palette = Pal(Hex("#6c7288"), Hex("#3a3e4a"), Hex("#ffd84a"), Hex("#fff07a"));
            pylon.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.2f, 0, 1.6f, 0.4f, 1.6f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.8f, 0, 0.35f, 3.2f, 0.35f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 2.6f, 0, 0.9f, 0.12f, 0.9f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 3.1f, 0, 0.7f, 0.12f, 0.7f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0, 3.4f, 0, 0.45f, 1.1f, 0.45f),
            };
            var batt = Bld(b, "volt_battery", "Volt Battery", "Stores charge from a Volt Pylon, so the camp's machines keep running while its kin sleeps. Each one holds about a third of a day: a pylon and one battery, filled by day, last the night.", 7, 0.9f,
                ("ingot", 8), ("crystal", 3), ("stone", 6));
            batt.palette = Pal(Hex("#5a6478"), Hex("#343a48"), Hex("#ffd84a"), Hex("#8af0ff"));
            batt.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.12f, 0, 1.6f, 0.24f, 1.1f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, -0.4f, 0.9f, 0, 0.62f, 1.32f, 0.62f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0.4f, 0.9f, 0, 0.62f, 1.32f, 0.62f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, -0.4f, 0.36f, 0, 0.7f, 0.1f, 0.7f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.4f, 0.36f, 0, 0.7f, 0.1f, 0.7f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, -0.4f, 1.5f, 0, 0.7f, 0.1f, 0.7f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.4f, 1.5f, 0, 0.7f, 0.1f, 0.7f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, -0.4f, 1.66f, 0, 0.24f, 0.22f, 0.24f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0.4f, 1.66f, 0, 0.24f, 0.22f, 0.24f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, -0.4f, 1.84f, 0, 0.26f, 0.26f, 0.26f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0.4f, 1.84f, 0, 0.26f, 0.26f, 0.26f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, -0.4f, 0.92f, -0.31f, 0.1f, 0.8f, 0.04f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, 0.4f, 0.92f, -0.31f, 0.1f, 0.8f, 0.04f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 1.2f, 0, 0.5f, 0.1f, 0.14f),
            };
            var hub = Bld(b, "conveyor_hub", "Conveyor Hub", "Powered: moves every producer's output into storage (no haulers needed).", 7, 1.3f,
                ("ingot", 8), ("wood", 10), ("stone", 10));
            hub.palette = Pal(Hex("#7a6a5a"), Hex("#3e3a36"), Hex("#d0a050"), Hex("#ffd36a"));
            hub.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.55f, 0, 1.6f, 1.1f, 1.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 1.3f, 0.25f, 0, 1.4f, 0.12f, 0.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -1.3f, 0.25f, 0, 1.4f, 0.12f, 0.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.25f, 1.3f, 0.6f, 0.12f, 1.4f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 1.2f, 0, 0.8f, 0.3f, 0.8f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 1.45f, 0, 0.35f, 0.35f, 0.35f),
            };
            var asm = Bld(b, "auto_assembler", "Auto-Assembler", "Powered: works queued Workbench and Campfire orders on its own.", 8, 1.3f,
                ("ingot", 10), ("crystal", 3), ("wood", 8));
            asm.palette = Pal(Hex("#8a8e9e"), Hex("#4a4e5a"), Hex("#c0c4d0"), Hex("#7df2ff"));
            asm.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.7f, 0, 1.8f, 1.4f, 1.3f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0.55f, 1.55f, 0, 0.7f, 0.18f, 0.7f, 90, 0, 0),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, -0.4f, 1.6f, 0, 0.5f, 0.18f, 0.5f, 90, 0, 0),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.75f, 0.68f, 1.2f, 0.5f, 0.06f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0.6f, 1.15f, 0.66f, 0.18f, 0.18f, 0.08f),
            };
            var spr = Bld(b, "sprinkler", "Sprinkler", "Waters every Crop Plot by itself and makes farms (Crop Plot, Forage Hut) work 40% faster while it runs: camp power or an Irrigating kin.", 6, 0.8f,
                ("ingot", 3), ("gel", 6), ("wood", 6));
            spr.workType = WorkType.Irrigating; spr.workerSlots = 1;
            spr.palette = Pal(Hex("#6a8a9a"), Hex("#3a5060"), Hex("#58c8ff"), Hex("#bff0ff"));
            spr.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 1.0f, 0.3f, 1.0f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.9f, 0, 0.2f, 1.5f, 0.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, 0, 1.7f, 0, 1.2f, 0.1f, 0.2f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0.6f, 1.68f, 0, 0.2f, 0.2f, 0.2f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, -0.6f, 1.68f, 0, 0.2f, 0.2f, 0.2f),
            };

            var hatch = Bld(b, "hatchery", "Hatchery", "Pair two kin to get an egg that inherits the best of both (Camp > Hatchery).", 3, 1.2f,
                ("wood", 20), ("fiber", 20), ("pelt", 6), ("stone", 10));
            hatch.palette = Pal(Hex("#c9a36a"), Hex("#8a6a40"), Hex("#fff2d8"), Hex("#ffb35a"));
            hatch.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.12f, 0, 2.2f, 0.24f, 2.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.35f, 0, 1.9f, 0.3f, 1.9f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0, 0.75f, 0, 0.62f, 0.8f, 0.62f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.45f, 0.62f, 0.2f, 0.4f, 0.5f, 0.4f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -0.8f, 0.3f, -0.6f, 0.35f, 1.3f, 0.35f, 0, 0, 12),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.8f, 0.3f, -0.55f, 0.3f, 1.1f, 0.3f, 0, 0, -12),
            };

            var nest = Bld(b, "kin_nest", "Kin Nest", "Beds for 2 workers, who sleep here from dusk to dawn. With a bed for every worker the camp works 15% faster.", 1, 1.0f, ("fiber", 10), ("wood", 4));
            nest.beds = 2; nest.palette = Pal(Hex("#d9c08f"), Hex("#8a6a40"), Hex("#f2e6c9"), Hex("#ffd36a"));
            nest.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 1.8f, 0.3f, 1.8f),
                P(PartShape.Dome, BodyGroup.Body, ColorSlot.Primary, 0, 0.2f, 0, 1.6f, 0.4f, 1.6f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0, 0.3f, 0, 1.0f, 0.2f, 1.0f),
            };

            var larder = Bld(b, "frost_larder", "Frost Larder", "Keeps food fresh: workers get hungry 40% slower while it holds cold. A Chilling kin fills it with cold, then is free for other work and comes back to top it up at half and before nightfall. One larder stays cold all night.", 4, 1.2f, ("stone", 12), ("wood", 6));
            larder.workType = WorkType.Chilling; larder.workerSlots = 1; larder.palette = Pal(Hex("#cfe6f2"), Hex("#7fa6c0"), Hex("#ffffff"), Hex("#8fe8ff"));
            larder.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.6f, 0, 1.4f, 1.2f, 1.2f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 1.25f, 0, 1.5f, 0.15f, 1.3f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.4f, 1.3f, 0.2f, 0.3f, 0.6f, 0.3f),
            };

            var dyn = Bld(b, "dynamo", "Spark Dynamo", "Charging kin power it: +25% production for the whole base.", 5, 1.2f, ("ingot", 4), ("stone", 10));
            dyn.workType = WorkType.Charging; dyn.workerSlots = 1; dyn.boostPercent = 25f;
            dyn.palette = Pal(Hex("#5a5f6e"), Hex("#d8d8e6"), Hex("#ffd84a"), Hex("#7af0ff"));
            dyn.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.4f, 0, 1.6f, 0.8f, 1.6f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 1.2f, 0, 0.4f, 1.0f, 0.4f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 1.9f, 0, 0.6f, 0.6f, 0.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.2f, 0, 1.2f, 0.1f, 0.1f, 0, 45, 0),
            };

            var drill = Bld(b, "deep_drill", "Deep Drill", "Quarrying kin wind the drill, then it bores Rustore out of deep rock by itself.", 6, 1.5f, ("ingot", 4), ("stone", 15));
            drill.workType = WorkType.Quarrying; drill.workerSlots = 1; drill.outputItem = "ore"; drill.outputCount = 1; drill.workPerUnit = 14f;
            drill.palette = Pal(Hex("#6e6258"), Hex("#d8d8e6"), Hex("#c07a4a"), Hex("#ffd84a"));
            drill.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.3f, 0, 2.0f, 0.6f, 2.0f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.8f, 1.5f, 0, 0.15f, 2.4f, 0.15f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.8f, 1.5f, 0, 0.15f, 2.4f, 0.15f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 2.6f, 0, 1.8f, 0.15f, 0.2f),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, 0, 1.2f, 0, 0.5f, 1.2f, 0.5f, 180, 0, 0),
            };
        }

        public static BodyPart P(PartShape s, BodyGroup g, ColorSlot c, float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            new BodyPart(s, g, c, new Vector3(px, py, pz), new Vector3(sx, sy, sz), new Vector3(rx, ry, rz));
    }
}
