using UnityEngine;

namespace Wildbond
{
    /// <summary>Crops and the Campfire meals cooked from them. Meals fill you up and give one timed bonus (the latest meal replaces the last).</summary>
    public static partial class ContentSeed
    {
        static ItemDef Meal(Bundle b, string id, string name, string glyph, Color col, string desc, float food, float heal, float minutes, params StatMod[] mods)
        {
            var d = Item(b, id, name, ItemCategory.Food, glyph, col, desc, 20);
            d.foodValue = food; d.healValue = heal; d.mealMinutes = minutes; d.mealMods = mods;
            return d;
        }

        static void BuildMeals(Bundle b)
        {
            // ---- crops: found wild in their biome, then grown on Crop Plots ----
            var grain = Item(b, "goldgrain", "Goldgrain", ItemCategory.Food, "Gg", Hex("#f0c84a"), "Nutty golden grain from the Sunmeadow. Plant it on a Crop Plot or cook with it.");
            grain.foodValue = 6f;
            var melon = Item(b, "dewmelon", "Dewmelon", ItemCategory.Food, "Dm", Hex("#7ad0a0"), "Cool, juicy melon from the Sunmeadow. Filling on its own, better in a bowl.");
            melon.foodValue = 18f; melon.healValue = 4f;
            var pepper = Item(b, "emberpepper", "Emberpepper", ItemCategory.Food, "Ep", Hex("#e84a2a"), "Fiery pepper from the Cinderreach Badlands. Warms you right through when cooked.");
            pepper.foodValue = 5f;
            var mint = Item(b, "frostmint", "Frostmint", ItemCategory.Food, "Fm", Hex("#9ae0e8"), "Icy-sweet sprigs from the Rimefang Peaks. Brews into a cooling tea.");
            mint.foodValue = 4f;

            // ---- meals ----
            Meal(b, "grain_loaf", "Hearth Loaf", "Lo", Hex("#d8a860"), "Crusty loaf that keeps you going.", 50, 5, 10,
                new StatMod(StatKind.MaxStamina, 25f));
            Meal(b, "trail_mix", "Trail Mix", "Tm", Hex("#c89a5a"), "Grain and berries for the road.", 30, 0, 10,
                new StatMod(StatKind.MoveSpeed, 0.08f));
            Meal(b, "melon_bowl", "Dewmelon Bowl", "Mb", Hex("#8ae0b0"), "Melon and berries. You won't be hungry for a long while.", 70, 25, 12,
                new StatMod(StatKind.HungerReduction, 0.3f));
            Meal(b, "pepper_skewer", "Ember Skewer", "Sk", Hex("#e0603a"), "Blistered peppers on a stick. Puts fire in your arm and keeps the cold out.", 40, 10, 8,
                new StatMod(StatKind.Attack, 5f), new StatMod(StatKind.ColdResist, 0.5f));
            Meal(b, "mint_tea", "Frostmint Tea", "Te", Hex("#a8e8e8"), "Cooling tea for hot country. You catch your breath faster.", 20, 0, 10,
                new StatMod(StatKind.HeatResist, 0.5f), new StatMod(StatKind.StaminaRegen, 0.25f));
            Meal(b, "forager_salad", "Forager's Salad", "Sa", Hex("#8ad06a"), "Crisp and bright. Sharpens your eye for what's worth picking.", 35, 5, 10,
                new StatMod(StatKind.GatherYield, 0.25f));
            Meal(b, "tamers_tart", "Tamer's Tart", "Tt", Hex("#f0a0c0"), "Kin can't resist the smell. Tether Seeds bind a little easier.", 40, 10, 8,
                new StatMod(StatKind.CaptureChance, 0.1f));
            Meal(b, "harvest_stew", "Harvest Stew", "St", Hex("#c87a3a"), "Everything good in one pot. Makes you hard to knock down.", 80, 30, 12,
                new StatMod(StatKind.MaxHp, 30f), new StatMod(StatKind.Defense, 4f));

            Recipe(b, "trail_mix", 1, CraftStation.Campfire, 8, 1, ("goldgrain", 2), ("berry", 3));
            Recipe(b, "grain_loaf", 1, CraftStation.Campfire, 12, 2, ("goldgrain", 4));
            Recipe(b, "melon_bowl", 1, CraftStation.Campfire, 10, 2, ("dewmelon", 2), ("berry", 3));
            Recipe(b, "forager_salad", 1, CraftStation.Campfire, 10, 3, ("dewmelon", 1), ("frostmint", 1), ("fiber", 2));
            Recipe(b, "pepper_skewer", 1, CraftStation.Campfire, 12, 4, ("emberpepper", 3), ("pelt", 1));
            Recipe(b, "mint_tea", 1, CraftStation.Campfire, 10, 4, ("frostmint", 3), ("berry", 2));
            Recipe(b, "tamers_tart", 1, CraftStation.Campfire, 14, 5, ("berry", 4), ("frostmint", 2), ("goldgrain", 2));
            Recipe(b, "harvest_stew", 1, CraftStation.Campfire, 16, 6, ("goldgrain", 2), ("dewmelon", 1), ("emberpepper", 1));
        }

        /// <summary>
        /// A few island kin have also settled the outer lands (added on top of their home biomes). Each outer land has
        /// its own 15 natives (wave 3), so these are only visitors.
        /// </summary>
        static void ApplyOuterBiomes(Bundle b)
        {
            void Add(BiomeMask land, params string[] ids)
            {
                foreach (var id in ids)
                {
                    var sp = b.species.Find(x => x.id == id);
                    if (sp != null) sp.biomes |= land;
                }
            }
            Add(BiomeMask.Isles, "brookit", "dribblet", "puffspine", "tidewing", "whipsquid", "zephyrkit");
            Add(BiomeMask.Swamp, "ribbloom", "gloomtoad", "mossnout", "lashvolt", "capshroom", "bramblebat");
            Add(BiomeMask.Desert, "dunestrider", "cactoad", "sandsnare", "scorpite", "tuskaroo", "mirageon");
            Add(BiomeMask.Blight, "hexhorn", "umbrask", "vinelash", "plasmoth", "thornback", "duskfang");
            Add(BiomeMask.Glow, "lumipup", "raybit", "windchime", "lumicorn", "prismite", "glimmoth");
        }

        /// <summary>Kin that only come out in certain weather or at the edges of the day (applied after every species is built).</summary>
        static void ApplySpawnConditions(Bundle b)
        {
            void Set(string id, SpawnCondition c, bool anyTime = false)
            {
                var sp = b.species.Find(x => x.id == id);
                if (sp == null) return;
                sp.spawnCondition = c;
                if (anyTime) sp.spawnTime = SpawnTime.Any;
            }
            Set("dribblet", SpawnCondition.Rain);
            Set("lashvolt", SpawnCondition.Rain);
            Set("capshroom", SpawnCondition.Rain);
            Set("plasmoth", SpawnCondition.Rain);
            Set("magmite", SpawnCondition.Heat);
            Set("mirageon", SpawnCondition.Heat);
            Set("shiverwisp", SpawnCondition.Snowfall);
            Set("cryodrake", SpawnCondition.Blizzard);
            Set("lumicorn", SpawnCondition.Dawn, true);
            Set("halcyrie", SpawnCondition.Dawn, true);
            Set("duskfang", SpawnCondition.Dusk, true);
        }
    }
}
