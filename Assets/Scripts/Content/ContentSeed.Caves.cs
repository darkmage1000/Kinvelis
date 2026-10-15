using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The cave pass (2026-10-15, user: "caves can have underground only kin, nesting colonies, rare eggs, territorial
    /// kin guarding valuable resources ... unique blueprints ... special resources"). Eight kin that live nowhere but
    /// underground (`SpeciesDef.underground`, no land of their own, so nothing outside ever spawns, sells or nests them;
    /// `Caves.CaveKin` decides which caves hold which), the Geode Core that only the deepest caves give, and the
    /// cave-only blueprint items (`Chests.Blueprints` entries with a `cave` tier: mid caves hold the "unique" ones,
    /// end caves the "valuable" ones; world chests never drop them).
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildCaveItems(Bundle b)
        {
            var core = Item(b, Caves.CoreItem, "Geode Core", ItemCategory.Resource, "Gc", Hex("#b48aff"),
                "A fist of violet crystal grown in the dark for a thousand years. Only the deepest caves have them: mine their geode veins, or take them from what guards them.");
            core.rarity = Rarity.Rare;

            // ---------------- mid caves: unique ----------------
            Gear(b, "delvers_lantern", "Delver's Lantern", EquipSlot.Amulet, Rarity.Rare, 2, "#ffd36a", "#5a4a3a",
                "A little caged glowstone worn on a cord. Down in the dark, you see them before they see you.", UniqueEffect.Delver,
                S(StatKind.Defense, 3), S(StatKind.MaxStamina, 10));
            Recipe(b, "delvers_lantern", 1, CraftStation.Workbench, 35, 12, ("crystal", 4), ("ingot", 4), ("gel", 3));
            Gear(b, "spelunker_cap", "Spelunker's Cap", EquipSlot.Head, Rarity.Rare, 1, "#c8a050", "#ffe890",
                "A riveted helmet with a glowing crystal on the brow. Falling rocks bounce off and the dark stops mattering.", UniqueEffect.Delver,
                S(StatKind.Defense, 9), S(StatKind.MaxHp, 15));
            Recipe(b, "spelunker_cap", 1, CraftStation.Workbench, 35, 14, ("ingot", 6), ("crystal", 3), ("pelt", 3));

            // ---------------- end caves: valuable (need Geode Cores) ----------------
            Gear(b, "geode_plate", "Geodeheart Plate", EquipSlot.Body, Rarity.Epic, 2, "#7a5ac8", "#e0c8ff",
                "Armour grown, not forged: a geode split and fitted round you, violet crystal inside, old stone out. Very little gets through.", UniqueEffect.None,
                S(StatKind.Defense, 26), S(StatKind.MaxHp, 60), S(StatKind.ElementResist, 0.1f));
            Recipe(b, "geode_plate", 1, CraftStation.Workbench, 60, 30, (Caves.CoreItem, 5), ("ingot", 10), ("crystal", 6));
            Arm(b, "abyss_edge", "Abyssal Edge", "Ab", "#3a2a5a", "A blade of black glass from the bottom of the world, a geode core set in the guard. It leaves foes dazed and slow.",
                84f, HandStyle.Sword, Element.Shade, StatusType.Drowsy, 0.3f, Rarity.Epic);
            Recipe(b, "abyss_edge", 1, CraftStation.Workbench, 60, 32, (Caves.CoreItem, 4), ("ingot", 8), ("crystal", 4));
            Gear(b, "geode_band", "Geode Band", EquipSlot.Ring, Rarity.Epic, 2, "#b48aff", "#ffffff",
                "A ring cut from one piece of geode. Shimmering kin are drawn to it, and cave kin follow you home more easily.", UniqueEffect.Delver,
                S(StatKind.CaptureChance, 0.06f), S(StatKind.Attack, 3));
            Recipe(b, "geode_band", 1, CraftStation.Workbench, 50, 30, (Caves.CoreItem, 3), ("crystal", 4));
        }

        /// <summary>#210-217: the kin that only live underground.</summary>
        static void BuildCaveKin(Bundle b)
        {
            const BiomeMask U = BiomeMask.None;   // no land: caves place them (Caves.CaveKin)

            // ---------------------------------------------------------------- 210 ECHOBAT
            {
                var d = K(b, 210, "echobat", "Echobat", Element.Shade, Element.Gale, Rarity.Common, U, Temperament.Timid, Locomotion.Flyer, 0.45f, 'F',
                    "Hundreds of them hang from every cave roof, wrapped up like little plums. One squeak and the whole colony drops at once.",
                    "Jumpy, sociable, hates being alone.", A("hex_flicker", "wind_ribbon"), "shadow_pounce",
                    Work((WorkType.Hauling, 1), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "pelt", "gel",
                    Pal(Hex("#4a3a5a"), Hex("#7a6a8a"), Hex("#ff8aa0"), Hex("#c8a0ff")));
                d.underground = true;
                new Plan()
                    .Blob(0.5f, 0.34f, 0.32f, 0.3f)
                    .Head(gB, 0.56f, 0.06f, 0.32f)
                    .Face(0.06f, cD, 0.06f, 0.24f)
                    .Ears(Con, cS, 0.14f, 0.3f, 22f)
                    .Mirror(Con, gB, cW, V(0.05f, 0.46f, 0.15f), V(0.03f, 0.07f, 0.03f), V(180, 0, 0))
                    .Add(Sph, gB, cA, V(0, 0.5f, 0.15f), V(0.08f, 0.05f, 0.04f))
                    .Wings(cP, cS, cD, V(0.14f, 0.55f, -0.02f), 0.55f, 0.32f, 10f)
                    .Mirror(Sph, gFL, gFR, cD, V(0.08f, 0.3f, -0.02f), V(0.05f, 0.08f, 0.05f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 211 PEBBLIT
            {
                var d = K(b, 211, "pebblit", "Pebblit", Element.Stone, Element.Normal, Rarity.Common, U, Temperament.Docile, Locomotion.Quadruped, 0.45f, 'T',
                    "A pill bug the size of a loaf. Startle it and it rolls up into a perfect pebble - so perfect that cave floors are half Pebblit if you look closely.",
                    "Shy, patient, sleeps curled up.", A("pebble_toss", "roll_crush"), "quake_stomp",
                    Work((WorkType.Quarrying, 1), (WorkType.Hauling, 1)), CompanionPerk.StoneBoost, "stone", "ore",
                    Pal(Hex("#7a7468"), Hex("#a8a090"), Hex("#4a463e"), Hex("#ffd08a")));
                d.underground = true;
                new Plan()
                    .Quad(0.2f, 0.36f, 0.24f, 0.56f, 0.24f, 0.14f, 0.04f, cD, cD, null, 0.0f, 0.02f)
                    .Shell(cP, 0.24f, 0.44f, 0.36f, 0.6f)
                    .Add(Dom, gB, cS, V(0, 0.255f, -0.18f), V(0.46f, 0.37f, 0.08f))
                    .Add(Dom, gB, cS, V(0, 0.255f, 0.0f), V(0.47f, 0.38f, 0.08f))
                    .Add(Dom, gB, cS, V(0, 0.255f, 0.18f), V(0.46f, 0.37f, 0.08f))
                    .Face(0.06f, cD, 0.0f, 0.22f)
                    .Mirror(Cyl, gH, cA, V(0.06f, 0.36f, 0.36f), V(0.02f, 0.2f, 0.02f), V(-35, 0, -20))
                    .Mirror(Sph, gH, cG, V(0.1f, 0.44f, 0.42f), V(0.04f, 0.04f, 0.04f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 212 DRIPSLUG
            {
                var d = K(b, 212, "dripslug", "Dripslug", Element.Tide, Element.Stone, Rarity.Common, U, Temperament.Docile, Locomotion.Slither, 0.55f, 'B',
                    "It carries a stalactite on its back and drips wherever it goes. Every pool in a cave was started by a Dripslug standing still for a very long time.",
                    "Slow, content, leaves a shiny trail.", A("bubble_burst", "mud_shot"), "tidal_lash",
                    Work((WorkType.Irrigating, 1), (WorkType.Quarrying, 1)), CompanionPerk.Mending, "gel", "stone",
                    Pal(Hex("#4a7a8a"), Hex("#9ac8c8"), Hex("#6a6258"), Hex("#80f0ff")));
                d.underground = true;
                d.nearWater = true;
                new Plan()
                    .Blob(0.16f, 0.34f, 0.24f, 0.7f, cP)
                    .Add(Sph, gB, cS, V(0, 0.1f, 0), V(0.4f, 0.1f, 0.74f))
                    .Add(Sph, gH, cP, V(0, 0.24f, 0.34f), V(0.26f, 0.24f, 0.26f)).Head(gH, 0.24f, 0.34f, 0.26f)
                    .Mirror(Cyl, gH, cP, V(0.07f, 0.38f, 0.38f), V(0.03f, 0.18f, 0.03f), V(-15, 0, -12))
                    .Mirror(Sph, gH, cD, V(0.09f, 0.47f, 0.4f), V(0.06f, 0.06f, 0.06f))
                    .Add(Sph, gH, cD, V(0, 0.2f, 0.47f), V(0.1f, 0.03f, 0.03f))
                    .Add(Con, gB, cA, V(0, 0.42f, -0.06f), V(0.24f, 0.46f, 0.24f))
                    .Add(Con, gB, cA, V(0.1f, 0.34f, -0.2f), V(0.12f, 0.26f, 0.12f), V(0, 0, -14))
                    .Add(Sph, gB, cG, V(0, 0.66f, -0.06f), V(0.06f, 0.08f, 0.06f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 213 GLIMCRAB
            {
                var d = K(b, 213, "glimcrab", "Glimcrab", Element.Lumen, Element.Stone, Rarity.Uncommon, U, Temperament.Territorial, Locomotion.Quadruped, 0.7f, 'T',
                    "It grows a cluster of crystal on its back and guards the vein it grew it from like treasure. Which it is.",
                    "Grumpy, proud, polishes its shell.", A("shell_slam", "glint_beam"), "radiant_charge",
                    Work((WorkType.Quarrying, 2), (WorkType.Charging, 1)), CompanionPerk.OreSense, "crystal", "gel",
                    Pal(Hex("#5a6a8a"), Hex("#8a9ab8"), Hex("#3a4258"), Hex("#90f0ff")));
                d.underground = true;
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Quad(0.28f, 0.6f, 0.26f, 0.5f, 0.26f, 0.2f, 0.05f, cP, cD, null, -0.04f, -0.06f)
                    .Mirror(Cyl, gB, cD, V(0.36f, 0.22f, -0.05f), V(0.04f, 0.26f, 0.04f), V(0, 0, 60))
                    .Mirror(Cyl, gB, cD, V(0.34f, 0.22f, -0.2f), V(0.04f, 0.24f, 0.04f), V(0, 0, 60))
                    .Mirror(Sph, gAL, gAR, cS, V(0.3f, 0.34f, 0.32f), V(0.12f, 0.12f, 0.2f))
                    .Mirror(Sph, gAL, gAR, cS, V(0.34f, 0.38f, 0.48f), V(0.18f, 0.14f, 0.2f))
                    .Mirror(Con, gAL, gAR, cS, V(0.4f, 0.4f, 0.6f), V(0.06f, 0.14f, 0.06f), V(80, 0, 0))
                    .Mirror(Cyl, gH, cP, V(0.08f, 0.44f, 0.28f), V(0.025f, 0.14f, 0.025f))
                    .Mirror(Sph, gH, cD, V(0.08f, 0.52f, 0.28f), V(0.05f, 0.05f, 0.05f))
                    .Add(Cry, gB, cG, V(0, 0.6f, -0.04f), V(0.16f, 0.4f, 0.16f))
                    .Add(Cry, gB, cG, V(0.12f, 0.54f, -0.12f), V(0.11f, 0.28f, 0.11f), V(0, 0, -24))
                    .Add(Cry, gB, cG, V(-0.13f, 0.53f, -0.1f), V(0.1f, 0.26f, 0.1f), V(0, 0, 26))
                    .Add(Cry, gB, cW, V(0.02f, 0.5f, 0.14f), V(0.08f, 0.2f, 0.08f), V(20, 0, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 214 MUSHMAW
            {
                var d = K(b, 214, "mushmaw", "Mushmaw", Element.Flora, Element.Shade, Rarity.Uncommon, U, Temperament.Territorial, Locomotion.Hopper, 0.65f, 'G',
                    "A glowcap that learned to bite. It sits very still among ordinary mushrooms and waits for something to lean on it.",
                    "Patient, then suddenly not.", A("vine_whip", "toxic_cloud"), "acid_bite",
                    Work((WorkType.Cultivating, 2), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "fiber", "gel",
                    Pal(Hex("#8a3a5a"), Hex("#e8dcc8"), Hex("#2a1a24"), Hex("#c0ff70")));
                d.underground = true;
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Add(Cyl, gB, cS, V(0, 0.2f, 0), V(0.34f, 0.4f, 0.34f))
                    .Add(Dom, gH, cP, V(0, 0.42f, 0), V(0.8f, 0.5f, 0.8f)).Head(gH, 0.36f, 0.12f, 0.36f)
                    .Add(Sph, gH, cA, V(0, 0.4f, 0.02f), V(0.72f, 0.1f, 0.72f))
                    .Ring(Sph, gH, cG, V(0, 0.62f, 0), 0.24f, 6, V(0.08f, 0.06f, 0.08f))
                    .Add(Sph, gH, cG, V(0, 0.68f, 0), V(0.09f, 0.07f, 0.09f))
                    .Add(Sph, gB, cD, V(0, 0.26f, 0.16f), V(0.26f, 0.12f, 0.1f))
                    .Fan(Con, gB, cW, V(0, 0.27f, 0.2f), 0.09f, 5, V(0.03f, 0.06f, 0.03f), 120f, 120f)
                    .Mirror(Sph, gB, cG, V(0.08f, 0.36f, 0.16f), V(0.05f, 0.05f, 0.03f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.1f, 0.04f, 0.04f), V(0.12f, 0.08f, 0.16f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 215 STALAGRIN
            {
                var d = K(b, 215, "stalagrin", "Stalagrin", Element.Stone, Element.Frost, Rarity.Uncommon, U, Temperament.Territorial, Locomotion.Hopper, 0.8f, 'T',
                    "A stalagmite with a grin. Count the stalagmites in a cave when you go in, and again on the way out.",
                    "Still as stone, sudden as an icicle.", A("spike_shot", "icicle_spear"), "quake_stomp",
                    Work((WorkType.Quarrying, 2), (WorkType.Chilling, 2)), CompanionPerk.StoneBoost, "stone", "crystal",
                    Pal(Hex("#8a8a94"), Hex("#b8c0cc"), Hex("#4a4a54"), Hex("#9ae8ff")));
                d.underground = true;
                new Plan()
                    .Add(Con, gB, cP, V(0, 0.55f, 0), V(0.62f, 1.1f, 0.62f))
                    .Add(Sph, gB, cP, V(0, 0.2f, 0), V(0.66f, 0.4f, 0.66f))
                    .Ring(Rok, gB, cS, V(0, 0.08f, 0), 0.3f, 5, V(0.18f, 0.14f, 0.18f))
                    .Head(gB, 0.52f, 0.16f, 0.4f)
                    .Mirror(Sph, gB, cG, V(0.09f, 0.62f, 0.2f), V(0.07f, 0.04f, 0.04f))
                    .Mirror(Sph, gB, cD, V(0.11f, 0.68f, 0.18f), V(0.08f, 0.02f, 0.03f), V(0, 0, -20))
                    .Add(Sph, gB, cD, V(0, 0.46f, 0.25f), V(0.26f, 0.07f, 0.06f))
                    .Fan(Con, gB, cW, V(0, 0.48f, 0.27f), 0.11f, 5, V(0.03f, 0.05f, 0.02f), 140f, 80f)
                    .Add(Cry, gB, cG, V(0.12f, 1.0f, -0.04f), V(0.08f, 0.22f, 0.08f), V(0, 0, -20))
                    .Add(Cry, gB, cG, V(-0.1f, 0.94f, 0.04f), V(0.06f, 0.18f, 0.06f), V(0, 0, 25))
                    .Mirror(Cap, gAL, gAR, cS, V(0.3f, 0.42f, 0.04f), V(0.1f, 0.3f, 0.1f), V(0, 0, 30))
                    .Mirror(Con, gAL, gAR, cG, V(0.42f, 0.26f, 0.06f), V(0.06f, 0.14f, 0.06f), V(0, 0, 30))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 216 GEMHORN
            {
                var d = K(b, 216, "gemhorn", "Gemhorn", Element.Stone, Element.Lumen, Rarity.Rare, U, Temperament.Territorial, Locomotion.Quadruped, 1.25f, 'T',
                    "A cave rhino whose horn is one great crystal, lit from inside. It walks the deep tunnels like a lantern, and lets very few people ride.",
                    "Proud, slow to trust, loyal for life.", A("radiant_charge", "rock_jaw"), "quake_stomp",
                    Work((WorkType.Quarrying, 3), (WorkType.Charging, 2)), CompanionPerk.OreSense, "crystal", "ore",
                    Pal(Hex("#5a5468"), Hex("#8a849a"), Hex("#3a3448"), Hex("#ff9ae8")));
                d.underground = true;
                d.mountable = true; d.mountSpeed = 12.5f;
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Quad(0.6f, 0.62f, 0.56f, 1.0f, 0.42f, 0.38f, 0.14f, cP, cD, cD, 0.0f, 0.06f)
                    .Snout(cS, 0.3f, 0.5f, false)
                    .Ears(Sph, cP, 0.1f, 0.16f, 30f)
                    .Face(0.06f, cG, 0.12f, 0.3f)
                    .Add(Cry, gH, cG, V(0, 0.92f, 0.84f), V(0.16f, 0.6f, 0.16f), V(-38, 0, 0))
                    .Add(Cry, gH, cW, V(0, 0.84f, 0.72f), V(0.1f, 0.3f, 0.1f), V(-20, 0, 0))
                    .Spikes(gB, cG, 0.82f, 0.32f, -0.4f, 4, 0.16f, 0.32f, 0.04f, -10f, 0f, 0f, Cry)
                    .Mirror(Rok, gB, cS, V(0.26f, 0.72f, -0.1f), V(0.34f, 0.2f, 0.5f), V(0, 0, 30))
                    .Tail(cP, 0.62f, -0.5f, 0.3f, 0.08f, 0.04f, 0f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 217 DEEPCOIL
            {
                var d = K(b, 217, "deepcoil", "Deepcoil", Element.Shade, Element.Stone, Rarity.Rare, U, Temperament.Aggressive, Locomotion.Slither, 1.3f, 'G',
                    "An eyeless serpent from the bottom of the deepest caves, hunting by a little light it dangles in front of its own mouth. Do not follow the light.",
                    "Cold, curious, very hungry.", A("coil_strike", "rock_jaw"), "night_slash",
                    Work((WorkType.Quarrying, 2), (WorkType.Hauling, 2)), CompanionPerk.Glow, "pelt", "ore",
                    Pal(Hex("#d8d0c8"), Hex("#5a5068"), Hex("#2a2430"), Hex("#ffe070")));
                d.underground = true;
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Serpent(cP, cS, 0.36f, 0.5f, 8, 0.36f)
                    .Add(Sph, gH, cP, V(0, 0.46f, 0.36f), V(0.5f, 0.38f, 0.56f)).Head(gH, 0.46f, 0.36f, 0.5f)
                    .Add(Sph, gH, cD, V(0, 0.36f, 0.58f), V(0.38f, 0.1f, 0.24f))
                    .Fan(Con, gH, cW, V(0, 0.39f, 0.64f), 0.15f, 7, V(0.03f, 0.08f, 0.03f), 120f, 120f)
                    .Mirror(Sph, gH, cS, V(0.18f, 0.5f, 0.44f), V(0.12f, 0.06f, 0.12f))
                    .Add(Cyl, gH, cS, V(0, 0.76f, 0.5f), V(0.025f, 0.34f, 0.025f), V(40, 0, 0))
                    .Add(Sph, gH, cG, V(0, 0.86f, 0.74f), V(0.1f, 0.1f, 0.1f))
                    .Spikes(gB, cA, 0.48f, 0.0f, -1.6f, 6, 0.08f, 0.2f, 0f, -30f)
                    .Mirror(Sph, gB, cG, V(0.26f, 0.36f, -0.36f), V(0.05f, 0.05f, 0.05f))
                    .Mirror(Sph, gB, cG, V(0.22f, 0.3f, -0.9f), V(0.04f, 0.04f, 0.04f))
                    .Pivot(gH, V(0, 0.4f, 0.16f))
                    .Done(d);
            }
        }
    }
}
