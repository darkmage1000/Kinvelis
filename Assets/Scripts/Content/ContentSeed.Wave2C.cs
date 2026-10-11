using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 2: Stone (#078-082), Gale (#083-087), Shade (#088-092), Lumen (#093-097).</summary>
        static void BuildWave2C(Bundle b)
        {
            const BiomeMask M = BiomeMask.Meadow, Bl = BiomeMask.Badlands, Pk = BiomeMask.Peaks, All = BiomeMask.All;

            // ================================================================ STONE
            // ---------------------------------------------------------------- 078 GEODELING (cute)
            {
                var d = Kin(b, 78, "geodeling", "Geodeling", Element.Stone, Element.Lumen, Rarity.Common, Bl | Pk, Temperament.Timid, Locomotion.Biped,
                    48, 8, 13, 1.9f, 4.8f, 0.55f,
                    "A hatchling still wearing its eggshell - except the shell is a geode, and the crack on top is full of amethyst. It rattles when it runs.",
                    "Shy, sparkly, hides by sitting among ordinary rocks.", 2.4f, 2, 4, A("pebble_toss", "tackle"), A("glint_beam"),
                    Work((WorkType.Quarrying, 1), (WorkType.Crafting, 1)), CompanionPerk.OreSense, "Companion: +50% ore, cinder and crystal when mining.",
                    Drops(("stone", 1, 2, 0.9f), ("crystal", 1, 1, 0.12f)),
                    Pal(Hex("#8a8078"), Hex("#b8a898"), Hex("#5a5048"), Hex("#c080ff")), Pal(Hex("#4a4a54"), Hex("#8a8a98"), Hex("#2a2a30"), Hex("#6af0c0")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Add(Rok, gB, cP, V(0, 0.32f, 0), V(0.56f, 0.56f, 0.54f))
                    .Add(Sph, gB, cS, V(0, 0.3f, 0.1f), V(0.4f, 0.36f, 0.38f))
                    .Add(Cyl, gB, cA, V(0, 0.58f, 0), V(0.3f, 0.04f, 0.3f))
                    .Add(Cry, gB, cG, V(0, 0.7f, 0), V(0.12f, 0.26f, 0.12f))
                    .Mirror(Cry, gB, cG, V(0.09f, 0.66f, 0.02f), V(0.09f, 0.2f, 0.09f), V(0, 0, -28))
                    .Add(Cry, gB, cG, V(0, 0.64f, -0.08f), V(0.08f, 0.18f, 0.08f), V(-25, 0, 0))
                    .Eyes(gB, V(0.1f, 0.34f, 0.25f), 0.09f)
                    .Mirror(Sph, gAL, gAR, cP, V(0.3f, 0.28f, 0.04f), V(0.12f, 0.12f, 0.12f))
                    .Mirror(Sph, gFL, gFR, cP, V(0.12f, 0.05f, 0.04f), V(0.18f, 0.1f, 0.22f))
                    .PivotPair(gAL, gAR, V(0.27f, 0.3f, 0)).PivotPair(gFL, gFR, V(0.12f, 0.1f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 079 HORNOLITH (wacky: three horns + slab frill)
            {
                var d = Kin(b, 79, "hornolith", "Hornolith", Element.Stone, Element.Normal, Rarity.Uncommon, Bl, Temperament.Territorial, Locomotion.Quadruped,
                    74, 13, 18, 1.9f, 5f, 1f,
                    "Three ivory horns, a frill of standing stones and a temper to match. It sharpens its horns on canyon walls and leaves spiral grooves a hand deep.",
                    "Blunt, sturdy, loyal to its herd above all.", 1.3f, 1, 3, A("horn_toss", "sand_kick"), A("roll_crush"),
                    Work((WorkType.Quarrying, 2), (WorkType.Hauling, 2)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks.",
                    Drops(("stone", 2, 3, 1f), ("ore", 1, 1, 0.3f)),
                    Pal(Hex("#b08a5a"), Hex("#8a6a48"), Hex("#f0e8d8"), Hex("#ffd070")), Pal(Hex("#6a7a8a"), Hex("#4a5664"), Hex("#ffe0a0"), Hex("#7af0ff")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.48f, -0.05f), V(0.6f, 0.52f, 0.86f))
                    .Add(Dom, gB, cS, V(0, 0.56f, -0.08f), V(0.52f, 0.34f, 0.7f))
                    .Fan(Rok, gH, cS, V(0, 0.56f, 0.34f), 0.26f, 5, V(0.24f, 0.28f, 0.1f), -70, 140)
                    .Add(Sph, gH, cP, V(0, 0.5f, 0.5f), V(0.4f, 0.38f, 0.44f))
                    .Mirror(Con, gH, cA, V(0.13f, 0.82f, 0.56f), V(0.08f, 0.36f, 0.08f), V(35, 0, -12))
                    .Add(Con, gH, cA, V(0, 0.58f, 0.8f), V(0.08f, 0.2f, 0.08f), V(55, 0, 0))
                    .Add(Con, gH, cS, V(0, 0.42f, 0.74f), V(0.16f, 0.16f, 0.14f), V(110, 0, 0))
                    .Eyes(gH, V(0.14f, 0.56f, 0.68f), 0.05f)
                    .Add(Con, gT, cP, V(0, 0.44f, -0.6f), V(0.24f, 0.4f, 0.24f), V(-95, 0, 0))
                    .Mirror(Con, gT, cA, V(0.08f, 0.46f, -0.74f), V(0.05f, 0.14f, 0.05f), V(-30, 0, -60))
                    .Pivot(gH, V(0, 0.48f, 0.34f)).Pivot(gT, V(0, 0.46f, -0.44f))
                    .Legs4(cP, 0.2f, 0.26f, -0.3f, 0.26f, 0.14f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 080 CRAGOLEM (big)
            {
                var d = Kin(b, 80, "cragolem", "Cragolem", Element.Stone, Element.Lumen, Rarity.Rare, Bl | Pk, Temperament.Territorial, Locomotion.Biped,
                    140, 22, 26, 1.7f, 4.6f, 1.5f,
                    "A hillside that stood up. A slow ember burns in its chest, moss keeps one shoulder and crystals the other, and it has never once been in a hurry.",
                    "Ponderous, protective, remembers the island before roads.", 0.45f, 1, 1, A("shell_slam", "quake_stomp"), A("solar_flare"),
                    Work((WorkType.Quarrying, 4), (WorkType.Hauling, 3)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks. Camp: master quarrier.",
                    Drops(("stone", 3, 5, 1f), ("ore", 1, 2, 0.6f), ("crystal", 1, 1, 0.3f)),
                    Pal(Hex("#7a746c"), Hex("#5a554e"), Hex("#6a9a50"), Hex("#ffa030")), Pal(Hex("#3a3a48"), Hex("#2a2a34"), Hex("#c060ff"), Hex("#7af0ff")));
                new Plan()
                    .Add(Rok, gB, cP, V(0, 0.8f, 0), V(0.8f, 0.7f, 0.6f))
                    .Add(Rok, gB, cS, V(0, 0.46f, 0), V(0.56f, 0.34f, 0.46f))
                    .Add(Sph, gB, cG, V(0, 0.84f, 0.26f), V(0.22f, 0.22f, 0.12f))
                    .Mirror(Rok, gB, cP, V(0.5f, 1.06f, 0), V(0.46f, 0.42f, 0.46f))
                    .Add(Cry, gB, cG, V(0.5f, 1.38f, -0.02f), V(0.14f, 0.3f, 0.14f), V(0, 0, -12))
                    .Add(Cry, gB, cG, V(0.38f, 1.32f, 0.06f), V(0.1f, 0.2f, 0.1f), V(10, 0, 14))
                    .Add(Dom, gB, cA, V(-0.5f, 1.2f, 0), V(0.42f, 0.22f, 0.42f))
                    .Add(Rok, gH, cS, V(0, 1.2f, 0.08f), V(0.34f, 0.3f, 0.34f))
                    .Mirror(Sph, gH, cG, V(0.08f, 1.22f, 0.24f), V(0.08f, 0.05f, 0.04f))
                    .Mirror(Rok, gAL, gAR, cS, V(0.58f, 0.72f, 0.02f), V(0.26f, 0.44f, 0.26f))
                    .Mirror(Rok, gAL, gAR, cP, V(0.62f, 0.38f, 0.06f), V(0.36f, 0.4f, 0.38f))
                    .Mirror(Rok, gFL, gFR, cP, V(0.2f, 0.16f, 0), V(0.3f, 0.34f, 0.34f))
                    .Pivot(gH, V(0, 1.1f, 0.04f)).PivotPair(gAL, gAR, V(0.52f, 0.98f, 0)).PivotPair(gFL, gFR, V(0.2f, 0.32f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 081 SCORPITE (wacky: stinger whip + pincers)
            {
                var d = Kin(b, 81, "scorpite", "Scorpite", Element.Stone, Element.Shade, Rarity.Uncommon, Bl, Temperament.Aggressive, Locomotion.Quadruped,
                    60, 16, 14, 2.2f, 6f, 0.9f,
                    "A scorpion with a stinger of violet crystal. One prick and you are yawning; two and you wake up somewhere else with your pockets lighter.",
                    "Patient ambusher, hoards shiny stones, hates boots.", 1.3f, 1, 2, A("sting_tail", "sand_kick"), A("coil_strike"),
                    Work((WorkType.Quarrying, 2), (WorkType.Crafting, 1)), CompanionPerk.OreSense, "Companion: +50% ore and cinder when mining.",
                    Drops(("stone", 1, 2, 0.8f), ("crystal", 1, 1, 0.12f), ("cinder", 1, 1, 0.3f)),
                    Pal(Hex("#6a4a3a"), Hex("#4a3428"), Hex("#e8d0a0"), Hex("#c060ff")), Pal(Hex("#2a2a34"), Hex("#1a1a22"), Hex("#ff6a4a"), Hex("#ffe040")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.26f, 0), V(0.56f, 0.26f, 0.7f))
                    .Add(Dom, gB, cS, V(0, 0.3f, -0.04f), V(0.5f, 0.22f, 0.6f))
                    .Mirror(Cyl, gB, cP, V(0.32f, 0.1f, 0), V(0.06f, 0.2f, 0.06f))
                    .Add(Sph, gH, cP, V(0, 0.26f, 0.38f), V(0.4f, 0.22f, 0.3f))
                    .Eyes(gH, V(0.08f, 0.34f, 0.48f), 0.045f, cG)
                    .Mirror(Sph, gH, cG, V(0.15f, 0.32f, 0.44f), V(0.03f, 0.03f, 0.03f))
                    .Mirror(Cap, gAL, gAR, cP, V(0.28f, 0.22f, 0.5f), V(0.1f, 0.36f, 0.1f), V(90, 25, 0))
                    .Mirror(Sph, gAL, gAR, cS, V(0.38f, 0.22f, 0.76f), V(0.24f, 0.14f, 0.3f))
                    .Mirror(Con, gAL, gAR, cA, V(0.44f, 0.22f, 0.96f), V(0.07f, 0.2f, 0.07f), V(90, 0, 0))
                    .Mirror(Con, gAL, gAR, cA, V(0.32f, 0.22f, 0.96f), V(0.06f, 0.18f, 0.06f), V(90, 0, 0))
                    .Whip(gT, cP, V(0, 0.34f, -0.34f), V(0, 0.1f, -0.1f), V(0, 0.006f, 0.03f), 9, 0.2f, 0.12f, cS)
                    .Tip(Cry, gT, cG, V(0.14f, 0.36f, 0.14f))
                    .Pivot(gH, V(0, 0.26f, 0.26f)).PivotPair(gAL, gAR, V(0.24f, 0.22f, 0.34f)).Pivot(gT, V(0, 0.3f, -0.3f))
                    .Legs4(cP, 0.3f, 0.2f, -0.2f, 0.2f, 0.06f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 082 BOULDERMAW (big)
            {
                var d = Kin(b, 82, "bouldermaw", "Bouldermaw", Element.Stone, Element.Tide, Rarity.Rare, Bl | M, Temperament.Aggressive, Locomotion.Quadruped,
                    125, 23, 20, 1.9f, 6.5f, 1.45f,
                    "What looks like a mossy ridge by the water has teeth. It can lie still for a season, and it closes its jaws faster than a blink.",
                    "Still, watchful, strangely tender with its hatchlings.", 0.45f, 1, 1, A("rock_jaw", "roll_crush"), A("riptide"),
                    Work((WorkType.Quarrying, 3), (WorkType.Hauling, 2)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks.",
                    Drops(("stone", 3, 4, 1f), ("pelt", 1, 2, 0.6f), ("crystal", 1, 1, 0.25f)),
                    Pal(Hex("#6a7a5a"), Hex("#4a5644"), Hex("#a8a090"), Hex("#6ae0d0")), Pal(Hex("#8a5a4a"), Hex("#5a3a30"), Hex("#e8d8c0"), Hex("#ffb040")));
                d.nearWater = true;
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.4f, -0.05f), V(0.56f, 1.3f, 0.5f), V(90, 0, 0))
                    .Add(Dom, gB, cS, V(0, 0.44f, -0.05f), V(0.5f, 0.3f, 1.0f))
                    .Spikes(gB, cG, 0.56f, 0.36f, -0.5f, 5, 0.12f, 0.28f, 0.02f, -15, 0, 0, Cry)
                    .Spikes(gB, cA, 0.5f, 0.3f, -0.44f, 4, 0.1f, 0.18f, 0, -10, 0.17f, -30)
                    .Add(Sph, gH, cP, V(0, 0.42f, 0.74f), V(0.46f, 0.34f, 0.46f))
                    .Add(Bx, gH, cP, V(0, 0.44f, 1.12f), V(0.32f, 0.14f, 0.6f))
                    .Add(Bx, gH, cS, V(0, 0.32f, 1.08f), V(0.28f, 0.08f, 0.54f))
                    .Mirror(Con, gH, cW, V(0.13f, 0.35f, 0.96f), V(0.04f, 0.08f, 0.04f), V(180, 0, 0))
                    .Mirror(Con, gH, cW, V(0.13f, 0.35f, 1.14f), V(0.04f, 0.08f, 0.04f), V(180, 0, 0))
                    .Mirror(Con, gH, cW, V(0.13f, 0.35f, 1.32f), V(0.04f, 0.08f, 0.04f), V(180, 0, 0))
                    .Mirror(Con, gH, cW, V(0.12f, 0.39f, 1.05f), V(0.035f, 0.07f, 0.035f))
                    .Mirror(Con, gH, cW, V(0.12f, 0.39f, 1.23f), V(0.035f, 0.07f, 0.035f))
                    .Mirror(Sph, gH, cP, V(0.14f, 0.6f, 0.76f), V(0.14f, 0.14f, 0.14f))
                    .Eyes(gH, V(0.14f, 0.62f, 0.82f), 0.05f, cG)
                    .Whip(gT, cP, V(0, 0.38f, -0.78f), V(0, -0.016f, -0.18f), V(0, 0.002f, 0), 7, 0.42f, 0.1f)
                    .Spikes(gT, cG, 0.5f, -0.85f, -1.5f, 3, 0.1f, 0.2f, 0, -20, 0, 0, Cry)
                    .Pivot(gH, V(0, 0.42f, 0.56f)).Pivot(gT, V(0, 0.4f, -0.7f))
                    .Legs4(cP, 0.32f, 0.36f, -0.42f, 0.26f, 0.15f, cD)
                    .Done(d);
            }

            // ================================================================ GALE
            // ---------------------------------------------------------------- 083 ZEPHYRKIT (cute)
            {
                var d = Kin(b, 83, "zephyrkit", "Zephyrkit", Element.Gale, Element.Normal, Rarity.Common, M | Pk, Temperament.Curious, Locomotion.Quadruped,
                    44, 10, 8, 3f, 7.5f, 0.6f,
                    "A kitten with wings too small to fly and too proud to admit it. It pounces off anything tall and glides, briefly, in a triumphant spiral.",
                    "Daring, purry, lands on its feet about half the time.", 2.4f, 1, 3, A("seed_gust", "quick_kick"), A("gust_slash"),
                    Work((WorkType.Hauling, 1), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("pelt", 1, 1, 0.8f)),
                    Pal(Hex("#c8e0f0"), Hex("#9ac0e0"), Hex("#ffa0b8"), Hex("#e8fff8")), Pal(Hex("#f0d8a8"), Hex("#e0a860"), Hex("#7ad0ff"), Hex("#ffffff")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.3f, -0.02f), V(0.34f, 0.32f, 0.48f))
                    .Add(Sph, gB, cW, V(0, 0.3f, 0.16f), V(0.3f, 0.26f, 0.2f))
                    .Add(Sph, gH, cP, V(0, 0.54f, 0.22f), V(0.44f, 0.4f, 0.4f))
                    .Mirror(Con, gH, cP, V(0.16f, 0.78f, 0.18f), V(0.16f, 0.2f, 0.08f), V(0, 0, -15))
                    .Mirror(Con, gH, cA, V(0.16f, 0.76f, 0.2f), V(0.09f, 0.12f, 0.05f), V(0, 0, -15))
                    .Add(Sph, gH, cW, V(0, 0.48f, 0.38f), V(0.2f, 0.13f, 0.1f))
                    .Add(Sph, gH, cA, V(0, 0.5f, 0.435f), V(0.045f, 0.035f, 0.03f))
                    .Eyes(gH, V(0.11f, 0.58f, 0.38f), 0.09f)
                    .Mirror(Sph, gWL, gWR, cW, V(0.26f, 0.52f, -0.08f), V(0.32f, 0.05f, 0.22f), V(0, 0, -30))
                    .Mirror(Sph, gWL, gWR, cS, V(0.38f, 0.6f, -0.12f), V(0.2f, 0.04f, 0.14f), V(0, 0, -30))
                    .Whip(gT, cP, V(0, 0.34f, -0.26f), V(0, 0.07f, -0.07f), V(0, 0.004f, 0.03f), 8, 0.1f, 0.08f)
                    .Tip(Sph, gT, cW, V(0.14f, 0.14f, 0.14f))
                    .Pivot(gH, V(0, 0.44f, 0.12f)).PivotPair(gWL, gWR, V(0.14f, 0.48f, -0.06f)).Pivot(gT, V(0, 0.32f, -0.22f))
                    .Legs4(cP, 0.1f, 0.14f, -0.16f, 0.16f, 0.08f, cW)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 084 TWISTAIL (wacky: spinning leaf propeller)
            {
                var d = Kin(b, 84, "twistail", "Twistail", Element.Gale, Element.Flora, Rarity.Uncommon, Bl | M, Temperament.Curious, Locomotion.Hopper,
                    56, 12, 11, 2.6f, 7f, 0.8f,
                    "A seed pod shaped like a spinning top, balanced on one foot under a propeller of leaves. It only falls over when it stops to think.",
                    "Dizzy, cheerful, cannot walk in a straight line.", 1.4f, 1, 3, A("cyclone", "seed_gust"), A("pollen_burst"),
                    Work((WorkType.Hauling, 2), (WorkType.Cultivating, 1), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("fiber", 1, 2, 0.8f), ("wood", 1, 1, 0.5f)),
                    Pal(Hex("#f0e0b0"), Hex("#fff4d8"), Hex("#e06a4a"), Hex("#8ad860")), Pal(Hex("#b0d8f0"), Hex("#e8f8ff"), Hex("#4a6ae0"), Hex("#ffd040")));
                Effects(d, AuraFx.Breeze, ExtraAnim.Spin);
                new Plan()
                    .Add(Con, gB, cP, V(0, 0.4f, 0), V(0.6f, 0.6f, 0.6f), V(180, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.72f, 0), V(0.62f, 0.3f, 0.62f))
                    .Add(Cyl, gB, cA, V(0, 0.5f, 0), V(0.42f, 0.05f, 0.42f))
                    .Add(Cyl, gB, cA, V(0, 0.34f, 0), V(0.25f, 0.04f, 0.25f))
                    .Add(Sph, gB, cA, V(0, 0.06f, 0), V(0.2f, 0.1f, 0.24f))
                    .Eyes(gB, V(0.12f, 0.74f, 0.25f), 0.085f)
                    .Add(Sph, gB, cD, V(0, 0.64f, 0.3f), V(0.08f, 0.03f, 0.03f))
                    .Mirror(Sph, gB, cS, V(0.34f, 0.6f, 0), V(0.14f, 0.08f, 0.1f))
                    .Add(Cyl, gX, cA, V(0, 0.94f, 0), V(0.05f, 0.16f, 0.05f))
                    .Ring(Sph, gX, cG, V(0, 1.04f, 0), 0.26f, 3, V(0.2f, 0.04f, 0.5f))
                    .Add(Sph, gX, cA, V(0, 1.04f, 0), V(0.1f, 0.1f, 0.1f))
                    .Pivot(gX, V(0, 0.9f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 085 ZEPHYRION (big)
            {
                var d = Kin(b, 85, "zephyrion", "Zephyrion", Element.Gale, Element.Lumen, Rarity.Rare, Pk, Temperament.Territorial, Locomotion.Flyer,
                    110, 20, 15, 3f, 9f, 1.5f,
                    "A sky serpent that swims between the peaks trailing scraps of cloud. Its whiskers taste the weather a full day ahead.",
                    "Wise, aloof, fond of riddles it never explains.", 0.45f, 1, 1, A("cyclone", "wind_ribbon"), A("aurora_veil"),
                    Work((WorkType.Hauling, 3), (WorkType.Charging, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed. Camp: expert hauler.",
                    Drops(("pelt", 2, 2, 0.9f), ("crystal", 1, 1, 0.3f)),
                    Pal(Hex("#5ac0b0"), Hex("#f0fff0"), Hex("#f0c050"), Hex("#fff0a0")), Pal(Hex("#c05a6a"), Hex("#fff0f0"), Hex("#f0e0a0"), Hex("#aef0ff")));
                Effects(d, AuraFx.Breeze);
                new Plan()
                    .Serpent(cP, cS, 1.0f, 0.44f, 9, 0.32f)
                    .Add(Sph, gB, cW, V(0, 1.16f, -0.3f), V(0.36f, 0.2f, 0.36f))
                    .Add(Sph, gB, cW, V(0, 1.12f, -0.96f), V(0.3f, 0.18f, 0.3f))
                    .Add(Sph, gT, cW, V(0, 1.02f, -1.7f), V(0.26f, 0.16f, 0.26f))
                    .Add(Sph, gT, cW, V(0, 0.9f, -2.66f), V(0.34f, 0.3f, 0.34f))
                    .Add(Sph, gH, cP, V(0, 1.14f, 0.4f), V(0.42f, 0.36f, 0.52f))
                    .Add(Sph, gH, cS, V(0, 1.06f, 0.66f), V(0.3f, 0.2f, 0.3f))
                    .Add(Sph, gH, cW, V(0, 1.26f, 0.16f), V(0.36f, 0.3f, 0.26f))
                    .Mirror(Con, gH, cA, V(0.12f, 1.46f, 0.3f), V(0.06f, 0.36f, 0.06f), V(-25, 0, -18))
                    .Mirror(Con, gH, cA, V(0.2f, 1.5f, 0.2f), V(0.04f, 0.18f, 0.04f), V(-10, 0, -60))
                    .Eyes(gH, V(0.14f, 1.22f, 0.6f), 0.055f, cG)
                    .WhipPair(gH, gH, cG, V(0.14f, 1.04f, 0.78f), V(0.06f, 0, 0.03f), V(0.002f, -0.004f, -0.014f), 12, 0.05f, 0.03f)
                    .Mirror(Cap, gB, cP, V(0.2f, 0.84f, -0.2f), V(0.08f, 0.24f, 0.08f), V(20, 0, -20))
                    .Mirror(Sph, gB, cA, V(0.26f, 0.7f, -0.16f), V(0.1f, 0.08f, 0.12f))
                    .Pivot(gH, V(0, 1.06f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 086 WINDCHIME (wacky: ribbon tendrils + bobbing clapper)
            {
                var d = Kin(b, 86, "windchime", "Windchime", Element.Gale, Element.Lumen, Rarity.Uncommon, Pk | M, Temperament.Docile, Locomotion.Flyer,
                    58, 12, 11, 2.2f, 5.8f, 0.85f,
                    "A bell of blown glass that drifts on mountain air trailing silk ribbons. When a dozen gather on a ridge the whole valley can hear them ring.",
                    "Peaceful, musical, goes silent when it is sad.", 1.3f, 1, 4, A("wind_ribbon", "prism_dust"), A("cyclone"),
                    Work((WorkType.Charging, 2), (WorkType.Hauling, 1)), CompanionPerk.Glow, "Companion: soft light at night.",
                    Drops(("gel", 1, 2, 0.8f), ("crystal", 1, 1, 0.1f)),
                    Pal(Hex("#a8e8d8"), Hex("#e8fff8"), Hex("#d8b060"), Hex("#fff4b0")), Pal(Hex("#e8a8d8"), Hex("#fff0fa"), Hex("#b0b0c8"), Hex("#aef0ff")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Bob);
                new Plan()
                    .Add(Dom, gB, cP, V(0, 0.9f, 0), V(0.7f, 0.7f, 0.7f))
                    .Add(Cyl, gB, cA, V(0, 0.9f, 0), V(0.74f, 0.06f, 0.74f))
                    .Add(Sph, gB, cA, V(0, 1.27f, 0), V(0.12f, 0.12f, 0.12f))
                    .Eyes(gB, V(0.12f, 1.04f, 0.29f), 0.08f)
                    .Add(Sph, gB, cD, V(0, 0.97f, 0.34f), V(0.06f, 0.03f, 0.02f))
                    .WhipPair(gT, gT, cS, V(0.2f, 0.86f, 0.12f), V(0, -0.08f, 0), V(0.002f, 0, -0.004f), 9, 0.07f, 0.04f)
                    .TipPair(Sph, gT, gT, cG, V(0.08f, 0.08f, 0.08f))
                    .WhipPair(gT, gT, cS, V(0.18f, 0.86f, -0.16f), V(0, -0.08f, 0), V(0.002f, 0, -0.006f), 11, 0.07f, 0.04f)
                    .TipPair(Sph, gT, gT, cG, V(0.08f, 0.08f, 0.08f))
                    .Add(Cyl, gX, cA, V(0, 0.82f, 0), V(0.03f, 0.16f, 0.03f))
                    .Add(Sph, gX, cG, V(0, 0.7f, 0), V(0.18f, 0.18f, 0.18f))
                    .Pivot(gT, V(0, 0.88f, 0)).Pivot(gX, V(0, 0.9f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 087 GALEGRYPH (big, mount)
            {
                var d = Kin(b, 87, "galegryph", "Galegryph", Element.Gale, Element.Normal, Rarity.Rare, Pk | Bl, Temperament.Territorial, Locomotion.Quadruped,
                    110, 20, 15, 3f, 9f, 1.4f,
                    "Lion behind, eagle before, and prouder than both. It bows to no one - but it will kneel, once, for a rider it has chosen.",
                    "Noble, exacting, preens for an hour every dawn.", 0.45f, 1, 1, A("gust_slash", "sky_dive"), A("pummel_rush"),
                    Work((WorkType.Hauling, 4), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed. High-leaping mount.",
                    Drops(("pelt", 2, 3, 1f)),
                    Pal(Hex("#c89a5a"), Hex("#6a4a30"), Hex("#f0c040"), Hex("#ffe890")), Pal(Hex("#4a4a58"), Hex("#2a2a34"), Hex("#e0e0f0"), Hex("#7ae0ff")));
                Mount(d, 13.5f, 11f, V(0, 1.12f, -0.14f));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.82f, -0.08f), V(0.54f, 0.56f, 1.06f))
                    .Add(Sph, gB, cW, V(0, 0.9f, 0.36f), V(0.54f, 0.6f, 0.5f))
                    .Add(Sph, gH, cW, V(0, 1.24f, 0.6f), V(0.36f, 0.36f, 0.38f))
                    .Add(Sph, gH, cA, V(0, 1.2f, 0.76f), V(0.16f, 0.12f, 0.12f))
                    .Add(Con, gH, cA, V(0, 1.18f, 0.84f), V(0.14f, 0.26f, 0.14f), V(115, 0, 0))
                    .Add(Con, gH, cS, V(0, 1.5f, 0.52f), V(0.08f, 0.24f, 0.1f), V(-35, 0, 0))
                    .Mirror(Con, gH, cS, V(0.08f, 1.46f, 0.5f), V(0.06f, 0.2f, 0.08f), V(-35, 0, -20))
                    .Eyes(gH, V(0.13f, 1.28f, 0.72f), 0.05f, cG)
                    .Wings(cS, cW, cS, V(0.22f, 1.06f, -0.06f), 1.25f, 0.6f, 22)
                    .Whip(gT, cP, V(0, 0.84f, -0.6f), V(0, 0.01f, -0.11f), V(0, -0.006f, 0), 7, 0.09f, 0.07f)
                    .Tip(Sph, gT, cS, V(0.2f, 0.26f, 0.2f))
                    .Pivot(gH, V(0, 1.1f, 0.44f)).PivotPair(gWL, gWR, V(0.24f, 1.06f, -0.06f)).Pivot(gT, V(0, 0.84f, -0.56f))
                    .Legs4(cP, 0.2f, 0.32f, -0.42f, 0.56f, 0.13f, cA)
                    .Done(d);
            }

            // ================================================================ SHADE
            // ---------------------------------------------------------------- 088 INKIT (cute)
            {
                var d = Kin(b, 88, "inkit", "Inkit", Element.Shade, Element.Normal, Rarity.Common, All, Temperament.Curious, Locomotion.Quadruped,
                    42, 11, 7, 2.8f, 7.2f, 0.55f,
                    "A kitten spilled from an inkwell. All you ever see at night is two lantern eyes - and, in the morning, small black pawprints on everything you own.",
                    "Sneaky, playful, pretends it was not following you.", 2.2f, 1, 3, A("hex_flicker", "quick_kick"), A("shadow_pounce"),
                    Work((WorkType.Crafting, 1), (WorkType.Foraging, 1)), CompanionPerk.Glow, "Companion: soft light at night.",
                    Drops(("pelt", 1, 1, 0.7f), ("gel", 1, 1, 0.4f)),
                    Pal(Hex("#2a2238"), Hex("#4a3a68"), Hex("#c060ff"), Hex("#ffe060")), Pal(Hex("#f0f0f8"), Hex("#d0d0e0"), Hex("#ff6ab0"), Hex("#7af0ff")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.26f, -0.02f), V(0.36f, 0.32f, 0.46f))
                    .Add(Sph, gH, cP, V(0, 0.5f, 0.2f), V(0.46f, 0.42f, 0.42f))
                    .Mirror(Con, gH, cP, V(0.17f, 0.76f, 0.16f), V(0.16f, 0.22f, 0.08f), V(0, 0, -20))
                    .Mirror(Con, gH, cA, V(0.17f, 0.74f, 0.18f), V(0.08f, 0.12f, 0.05f), V(0, 0, -20))
                    .Eyes(gH, V(0.12f, 0.52f, 0.36f), 0.11f, cG)
                    .Add(Sph, gH, cA, V(0, 0.44f, 0.41f), V(0.04f, 0.03f, 0.03f))
                    .Mirror(Sph, gH, cS, V(0.2f, 0.44f, 0.3f), V(0.1f, 0.06f, 0.06f))
                    .Whip(gT, cP, V(0, 0.3f, -0.24f), V(0, 0.07f, -0.06f), V(0.004f, 0.004f, 0.012f), 7, 0.12f, 0.07f)
                    .Tip(Con, gT, cA, V(0.12f, 0.22f, 0.12f))
                    .Pivot(gH, V(0, 0.4f, 0.1f)).Pivot(gT, V(0, 0.28f, -0.2f))
                    .Legs4(cP, 0.11f, 0.14f, -0.16f, 0.14f, 0.08f, cS)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 089 HEXHORN (wacky: curled horns + arrow tail)
            {
                var d = Kin(b, 89, "hexhorn", "Hexhorn", Element.Shade, Element.Ember, Rarity.Uncommon, Bl | M, Temperament.Curious, Locomotion.Biped,
                    58, 14, 10, 2.4f, 6.4f, 0.85f,
                    "A pot-bellied imp with horns too big for its head. It trades in dares, never lies, and never quite tells the truth either.",
                    "Mischievous, bargain-loving, cackles at its own jokes.", 1.3f, 1, 2, A("hex_flicker", "whip_crack"), A("ember_spit"),
                    Work((WorkType.Crafting, 2), (WorkType.Kindling, 1)), CompanionPerk.Warmth, "Companion: warmth aura (slower hunger) + light.",
                    Drops(("cinder", 1, 1, 0.6f), ("gel", 1, 1, 0.5f)),
                    Pal(Hex("#7a2a4a"), Hex("#c06a8a"), Hex("#f0d8b0"), Hex("#ffb030")), Pal(Hex("#2a5a6a"), Hex("#6ab0c0"), Hex("#f0f0d0"), Hex("#b0ff60")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.44f, 0), V(0.42f, 0.46f, 0.38f))
                    .Add(Sph, gB, cS, V(0, 0.4f, 0.1f), V(0.3f, 0.3f, 0.22f))
                    .Add(Sph, gH, cP, V(0, 0.84f, 0.04f), V(0.46f, 0.4f, 0.42f))
                    .Add(Sph, gH, cD, V(0, 0.76f, 0.2f), V(0.26f, 0.07f, 0.1f))
                    .Mirror(Con, gH, cW, V(0.07f, 0.78f, 0.25f), V(0.035f, 0.07f, 0.035f), V(180, 0, 0))
                    .Eyes(gH, V(0.12f, 0.9f, 0.2f), 0.08f, cG)
                    .WhipPair(gH, gH, cA, V(0.17f, 1.0f, 0.02f), V(0.07f, 0.07f, 0), V(-0.004f, 0.004f, 0.016f), 9, 0.14f, 0.05f)
                    .Mirror(Con, gH, cP, V(0.27f, 0.86f, 0), V(0.08f, 0.24f, 0.14f), V(0, 0, -75))
                    .Mirror(Bx, gWL, gWR, cS, V(0.3f, 0.72f, -0.2f), V(0.36f, 0.26f, 0.03f), V(0, 25, -10))
                    .Mirror(Con, gWL, gWR, cA, V(0.46f, 0.88f, -0.26f), V(0.05f, 0.14f, 0.05f))
                    .Mirror(Cap, gAL, gAR, cP, V(0.26f, 0.44f, 0.04f), V(0.08f, 0.3f, 0.08f), V(0, 0, -15))
                    .Whip(gT, cP, V(0, 0.3f, -0.2f), V(0, 0.03f, -0.09f), V(0, 0.012f, 0.004f), 10, 0.06f, 0.045f)
                    .Tip(Con, gT, cA, V(0.16f, 0.22f, 0.06f))
                    .Pivot(gH, V(0, 0.66f, 0)).PivotPair(gAL, gAR, V(0.24f, 0.58f, 0)).PivotPair(gWL, gWR, V(0.12f, 0.7f, -0.18f)).Pivot(gT, V(0, 0.3f, -0.16f))
                    .Legs2(cP, 0.11f, 0.26f, 0.11f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 090 DUSKFANG (big, mount)
            {
                var d = Kin(b, 90, "duskfang", "Duskfang", Element.Shade, Element.Normal, Rarity.Rare, Pk | Bl, Temperament.Aggressive, Locomotion.Quadruped,
                    100, 24, 12, 3.2f, 9.8f, 1.35f,
                    "A hunter cut from the dark between stars, striped with a slow violet glow. It does not chase; it is simply there when you turn around.",
                    "Silent, intense, gentle only with the one it trusts.", 0.45f, 1, 1, A("shadow_pounce", "night_slash"), A("ice_fang"),
                    Work((WorkType.Hauling, 2), (WorkType.Crafting, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed. Swift, silent mount.",
                    Drops(("pelt", 2, 3, 1f), ("crystal", 1, 1, 0.15f)),
                    Pal(Hex("#1e1a2c"), Hex("#3a3050"), Hex("#6a4aa0"), Hex("#b060ff")), Pal(Hex("#e8e8f0"), Hex("#b8b8c8"), Hex("#6a8ad0"), Hex("#6af0ff")));
                d.spawnTime = SpawnTime.Night;
                Mount(d, 14.5f, 10.5f, V(0, 0.98f, -0.14f));
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.74f, -0.04f), V(0.42f, 1.06f, 0.44f), V(90, 0, 0))
                    .Add(Sph, gB, cP, V(0, 0.8f, 0.3f), V(0.46f, 0.5f, 0.46f))
                    .Mirror(Sph, gB, cG, V(0.21f, 0.84f, 0.2f), V(0.04f, 0.2f, 0.1f), V(0, 0, -20))
                    .Mirror(Sph, gB, cG, V(0.2f, 0.78f, -0.1f), V(0.04f, 0.18f, 0.09f), V(0, 0, -20))
                    .Mirror(Sph, gB, cG, V(0.19f, 0.76f, -0.38f), V(0.04f, 0.14f, 0.08f), V(0, 0, -20))
                    .Spikes(gB, cS, 0.98f, 0.48f, 0.16f, 3, 0.12f, 0.32f, 0.02f, -40)
                    .Spikes(gB, cS, 0.92f, 0.44f, 0.2f, 2, 0.1f, 0.26f, 0, -40, 0.14f, -30)
                    .Add(Sph, gH, cP, V(0, 0.94f, 0.62f), V(0.36f, 0.32f, 0.4f))
                    .Add(Sph, gH, cP, V(0, 0.86f, 0.84f), V(0.2f, 0.16f, 0.26f))
                    .Mirror(Con, gH, cP, V(0.14f, 1.16f, 0.54f), V(0.1f, 0.24f, 0.06f), V(-10, 0, -12))
                    .Mirror(Con, gH, cW, V(0.07f, 0.76f, 0.9f), V(0.035f, 0.14f, 0.035f), V(180, 0, 0))
                    .Eyes(gH, V(0.11f, 0.99f, 0.78f), 0.05f, cG)
                    .Whip(gT, cP, V(0, 0.78f, -0.56f), V(0, 0.02f, -0.12f), V(0, -0.004f, 0), 10, 0.09f, 0.05f)
                    .Tip(Con, gT, cG, V(0.1f, 0.34f, 0.04f))
                    .Pivot(gH, V(0, 0.86f, 0.46f)).Pivot(gT, V(0, 0.78f, -0.52f))
                    .Legs4(cP, 0.16f, 0.34f, -0.4f, 0.52f, 0.11f, cS)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 091 LANTERNJAW (wacky: lure whip + big jaw)
            {
                var d = Kin(b, 91, "lanternjaw", "Lanternjaw", Element.Shade, Element.Lumen, Rarity.Uncommon, All, Temperament.Aggressive, Locomotion.Flyer,
                    64, 15, 10, 2.4f, 6.5f, 0.9f,
                    "A deep-water angler that swims through fog instead. Travellers follow the friendly little light for a long way before they notice the teeth behind it.",
                    "Patient, greedy, sulks when the trick does not work.", 1.2f, 1, 2, A("hex_flicker", "glint_beam"), A("gloom_pulse"),
                    Work((WorkType.Charging, 1), (WorkType.Crafting, 1)), CompanionPerk.Glow, "Companion: bright lantern light at night.",
                    Drops(("gel", 1, 2, 0.9f), ("crystal", 1, 1, 0.12f)),
                    Pal(Hex("#2a3a4a"), Hex("#4a5a6a"), Hex("#7a3a8a"), Hex("#aaffd0")), Pal(Hex("#5a2a3a"), Hex("#8a4a5a"), Hex("#e0a040"), Hex("#ffe070")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.95f, 0), V(0.7f, 0.62f, 0.74f))
                    .Add(Sph, gB, cS, V(0, 0.8f, 0.14f), V(0.6f, 0.3f, 0.6f))
                    .Add(Sph, gB, cD, V(0, 0.9f, 0.28f), V(0.46f, 0.1f, 0.3f))
                    .Mirror(Con, gB, cW, V(0.07f, 0.94f, 0.4f), V(0.05f, 0.14f, 0.05f))
                    .Mirror(Con, gB, cW, V(0.18f, 0.93f, 0.33f), V(0.05f, 0.12f, 0.05f))
                    .Mirror(Con, gB, cW, V(0.12f, 0.98f, 0.37f), V(0.04f, 0.1f, 0.04f), V(180, 0, 0))
                    .Eyes(gB, V(0.2f, 1.1f, 0.27f), 0.07f, cG)
                    .Whip(gH, cP, V(0, 1.24f, 0.1f), V(0, 0.08f, 0.03f), V(0, -0.012f, 0.014f), 9, 0.05f, 0.035f)
                    .Tip(Sph, gH, cG, V(0.2f, 0.2f, 0.2f))
                    .Mirror(Sph, gWL, gWR, cA, V(0.4f, 0.9f, -0.1f), V(0.24f, 0.04f, 0.2f), V(0, 0, -25))
                    .Add(Con, gT, cP, V(0, 0.92f, -0.5f), V(0.3f, 0.4f, 0.1f), V(-90, 0, 0))
                    .Add(Sph, gT, cA, V(0, 0.92f, -0.74f), V(0.05f, 0.4f, 0.24f))
                    .Pivot(gH, V(0, 1.22f, 0.08f)).PivotPair(gWL, gWR, V(0.34f, 0.9f, -0.1f)).Pivot(gT, V(0, 0.92f, -0.34f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 092 UMBRASK (big)
            {
                var d = Kin(b, 92, "umbrask", "Umbrask", Element.Shade, Element.Gale, Rarity.Rare, Bl | Pk, Temperament.Aggressive, Locomotion.Biped,
                    105, 24, 13, 2.6f, 8f, 1.45f,
                    "A horned shape in a tattered cloak with two pale eyes under the hood and a blade where each hand should be. Wheat fields are found harvested after it passes.",
                    "Solemn, exact, keeps every promise it makes.", 0.4f, 1, 1, A("night_slash", "coil_strike"), A("gloom_pulse"),
                    Work((WorkType.Logging, 3), (WorkType.Crafting, 2)), CompanionPerk.LumberBoost, "Companion: +50% wood. Camp: master logger.",
                    Drops(("gel", 1, 2, 0.8f), ("pelt", 1, 2, 0.6f), ("crystal", 1, 1, 0.25f)),
                    Pal(Hex("#2a2a3a"), Hex("#4a4458"), Hex("#8a80a0"), Hex("#7affc0")), Pal(Hex("#e8e0d0"), Hex("#b8b0a0"), Hex("#6a5a4a"), Hex("#ff6a6a")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Add(Con, gB, cP, V(0, 0.74f, 0), V(0.7f, 1.1f, 0.6f))
                    .Ring(Con, gB, cS, V(0, 0.3f, 0), 0.26f, 7, V(0.16f, 0.3f, 0.08f), 160)
                    .Add(Sph, gB, cS, V(0, 1.1f, 0.02f), V(0.4f, 0.36f, 0.32f))
                    .Mirror(Con, gB, cA, V(0.26f, 1.3f, 0), V(0.12f, 0.3f, 0.12f), V(0, 0, -40))
                    .Add(Sph, gH, cP, V(0, 1.46f, 0.02f), V(0.4f, 0.42f, 0.42f))
                    .Add(Sph, gH, cD, V(0, 1.44f, 0.12f), V(0.28f, 0.28f, 0.26f))
                    .Mirror(Sph, gH, cG, V(0.07f, 1.46f, 0.24f), V(0.07f, 0.04f, 0.03f))
                    .Mirror(Con, gH, cA, V(0.14f, 1.72f, -0.02f), V(0.07f, 0.3f, 0.07f), V(-15, 0, -15))
                    .Mirror(Cap, gAL, gAR, cS, V(0.34f, 1.0f, 0.06f), V(0.08f, 0.5f, 0.08f), V(25, 0, -12))
                    .Mirror(Con, gAL, gAR, cG, V(0.4f, 0.7f, 0.42f), V(0.035f, 0.72f, 0.2f), V(100, 0, 0))
                    .Pivot(gH, V(0, 1.3f, 0)).PivotPair(gAL, gAR, V(0.3f, 1.22f, 0))
                    .Legs2(cD, 0.1f, 0.3f, 0.08f)
                    .Done(d);
            }

            // ================================================================ LUMEN
            // ---------------------------------------------------------------- 093 TWINKLET (cute)
            {
                var d = Kin(b, 93, "twinklet", "Twinklet", Element.Lumen, Element.Normal, Rarity.Common, All, Temperament.Timid, Locomotion.Hopper,
                    42, 9, 9, 2.2f, 5.6f, 0.55f,
                    "A fallen star about the size of a loaf of bread. It hops everywhere, apologises to things it bumps into, and dims when embarrassed.",
                    "Polite, wishful, easily flustered.", 2.2f, 2, 4, A("glint_beam", "tackle"), A("prism_dust"),
                    Work((WorkType.Charging, 1), (WorkType.Crafting, 1)), CompanionPerk.Glow, "Companion: soft light at night.",
                    Drops(("gel", 1, 1, 0.6f), ("crystal", 1, 1, 0.1f)),
                    Pal(Hex("#fff2b0"), Hex("#fffbe0"), Hex("#ffd040"), Hex("#ffb0c8")), Pal(Hex("#c8e8ff"), Hex("#f0faff"), Hex("#7ab0ff"), Hex("#ffe070")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Fan(Con, gB, cA, V(0, 0.42f, -0.02f), 0.26f, 5, V(0.24f, 0.3f, 0.14f))
                    .Add(Sph, gB, cP, V(0, 0.42f, 0), V(0.5f, 0.5f, 0.4f))
                    .Add(Sph, gB, cS, V(0, 0.38f, 0.1f), V(0.36f, 0.34f, 0.26f))
                    .Eyes(gB, V(0.1f, 0.44f, 0.2f), 0.1f)
                    .Mirror(Sph, gB, cG, V(0.17f, 0.34f, 0.19f), V(0.08f, 0.05f, 0.04f))
                    .Mirror(Sph, gFL, gFR, cA, V(0.1f, 0.05f, 0.02f), V(0.14f, 0.1f, 0.18f))
                    .Ring(Cry, gX, cA, V(0, 0.5f, 0), 0.5f, 3, V(0.07f, 0.14f, 0.07f))
                    .Pivot(gX, V(0, 0.5f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 094 RAYBIT (wacky: bulb ears + orbiting halo)
            {
                var d = Kin(b, 94, "raybit", "Raybit", Element.Lumen, Element.Normal, Rarity.Uncommon, M | Pk, Temperament.Curious, Locomotion.Hopper,
                    56, 12, 10, 2.6f, 7.5f, 0.8f,
                    "A hare whose ears grew into two long lamp-stalks. A ring of little lights circles its head, and it is far too pleased with itself about that.",
                    "Vain, bouncy, signals friends with ear-flashes.", 1.4f, 1, 3, A("dazzle_hop", "glint_beam"), A("aurora_veil"),
                    Work((WorkType.Charging, 2), (WorkType.Foraging, 1)), CompanionPerk.Glow, "Companion: lights up the night around you.",
                    Drops(("pelt", 1, 2, 0.8f), ("crystal", 1, 1, 0.08f)),
                    Pal(Hex("#f0e8ff"), Hex("#d8c8f0"), Hex("#ff9ac0"), Hex("#fff080")), Pal(Hex("#3a3a4a"), Hex("#5a5a70"), Hex("#7af0ff"), Hex("#aef0ff")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.36f, -0.04f), V(0.46f, 0.5f, 0.56f))
                    .Add(Sph, gB, cW, V(0, 0.5f, 0.14f), V(0.42f, 0.26f, 0.3f))
                    .Add(Sph, gH, cP, V(0, 0.74f, 0.12f), V(0.4f, 0.38f, 0.38f))
                    .Add(Sph, gH, cW, V(0, 0.68f, 0.26f), V(0.2f, 0.14f, 0.14f))
                    .Add(Sph, gH, cA, V(0, 0.71f, 0.33f), V(0.05f, 0.04f, 0.03f))
                    .Eyes(gH, V(0.11f, 0.79f, 0.26f), 0.08f)
                    .WhipPair(gH, gH, cP, V(0.1f, 0.92f, 0.06f), V(0.02f, 0.1f, -0.01f), V(0.006f, -0.008f, -0.01f), 9, 0.11f, 0.07f)
                    .TipPair(Sph, gH, gH, cG, V(0.17f, 0.17f, 0.17f))
                    .Mirror(Sph, gFL, gFR, cP, V(0.15f, 0.06f, 0.1f), V(0.16f, 0.12f, 0.4f))
                    .Add(Sph, gT, cW, V(0, 0.34f, -0.34f), V(0.18f, 0.18f, 0.18f))
                    .Ring(Sph, gX, cG, V(0, 1.04f, 0.1f), 0.32f, 8, V(0.07f, 0.07f, 0.07f))
                    .Pivot(gH, V(0, 0.6f, 0.04f)).Pivot(gT, V(0, 0.34f, -0.3f)).Pivot(gX, V(0, 1.04f, 0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 095 LUMICORN (big, mount)
            {
                var d = Kin(b, 95, "lumicorn", "Lumicorn", Element.Lumen, Element.Normal, Rarity.Rare, M | Pk, Temperament.Timid, Locomotion.Quadruped,
                    95, 17, 14, 3f, 9f, 1.35f,
                    "A pale charger with a horn of spun light and a mane like dawn mist. Where its hooves touch, the grass keeps a faint glow until morning.",
                    "Gentle, wary of strangers, steadfast once befriended.", 0.5f, 1, 2, A("radiant_charge", "glint_beam"), A("aurora_veil"),
                    Work((WorkType.Charging, 3), (WorkType.Hauling, 2)), CompanionPerk.Mending, "Companion: slowly restores your HP. Graceful mount.",
                    Drops(("pelt", 1, 2, 0.9f), ("crystal", 1, 1, 0.3f)),
                    Pal(Hex("#fdfaf0"), Hex("#c8b0ff"), Hex("#f0d070"), Hex("#fff0a0")), Pal(Hex("#2a2a3a"), Hex("#ff8ac0"), Hex("#c0c0d0"), Hex("#aef0ff")));
                Mount(d, 13.5f, 10f, V(0, 1.2f, -0.08f));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.98f, -0.05f), V(0.42f, 0.98f, 0.44f), V(90, 0, 0))
                    .Add(Cap, gB, cP, V(0, 1.28f, 0.48f), V(0.24f, 0.6f, 0.28f), V(32, 0, 0))
                    .Add(Sph, gH, cP, V(0, 1.6f, 0.72f), V(0.26f, 0.26f, 0.34f))
                    .Add(Sph, gH, cP, V(0, 1.52f, 0.9f), V(0.17f, 0.15f, 0.2f))
                    .Add(Con, gH, cG, V(0, 1.96f, 0.84f), V(0.07f, 0.56f, 0.07f), V(22, 0, 0))
                    .Mirror(Con, gH, cP, V(0.09f, 1.78f, 0.64f), V(0.06f, 0.14f, 0.05f), V(0, 0, -15))
                    .Add(Sph, gH, cS, V(0, 1.74f, 0.72f), V(0.14f, 0.12f, 0.16f))
                    .Eyes(gH, V(0.1f, 1.64f, 0.83f), 0.045f)
                    .Whip(gB, cS, V(0, 1.66f, 0.54f), V(0, -0.07f, -0.05f), V(0, -0.002f, 0.004f), 8, 0.16f, 0.1f)
                    .Whip(gT, cS, V(0, 1.06f, -0.58f), V(0, -0.03f, -0.09f), V(0, -0.012f, 0.006f), 10, 0.16f, 0.08f)
                    .Tip(Sph, gT, cG, V(0.1f, 0.1f, 0.1f))
                    .Pivot(gH, V(0, 1.46f, 0.58f)).Pivot(gT, V(0, 1.06f, -0.54f))
                    .Legs4(cP, 0.14f, 0.36f, -0.4f, 0.76f, 0.12f, cG)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 096 PRISMITE (wacky: crystal spikes)
            {
                var d = Kin(b, 96, "prismite", "Prismite", Element.Lumen, Element.Stone, Rarity.Uncommon, Bl | Pk, Temperament.Docile, Locomotion.Quadruped,
                    62, 11, 17, 1.9f, 5f, 0.85f,
                    "A crab that grows prisms instead of barnacles. At noon it throws rainbows across the whole canyon floor and clacks happily at each one.",
                    "Fussy, house-proud, polishes itself on sandstone.", 1.3f, 1, 3, A("glint_beam", "shell_slam"), A("prism_dust"),
                    Work((WorkType.Quarrying, 2), (WorkType.Charging, 1), (WorkType.Crafting, 1)), CompanionPerk.OreSense, "Companion: +50% ore, cinder and crystal when mining.",
                    Drops(("crystal", 1, 1, 0.3f), ("stone", 1, 2, 0.7f)),
                    Pal(Hex("#7a8aa8"), Hex("#a8b8d0"), Hex("#7ae0ff"), Hex("#ff9ae0")), Pal(Hex("#a88a6a"), Hex("#d0b898"), Hex("#ffe070"), Hex("#7affc0")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.26f, 0), V(0.66f, 0.3f, 0.6f))
                    .Add(Dom, gB, cS, V(0, 0.28f, -0.02f), V(0.6f, 0.3f, 0.54f))
                    .Add(Cry, gB, cG, V(0, 0.6f, -0.04f), V(0.16f, 0.44f, 0.16f))
                    .Mirror(Cry, gB, cG, V(0.16f, 0.52f, 0.04f), V(0.12f, 0.32f, 0.12f), V(8, 0, -28))
                    .Mirror(Cry, gB, cA, V(0.12f, 0.5f, -0.2f), V(0.1f, 0.28f, 0.1f), V(-25, 0, -20))
                    .Add(Cry, gB, cA, V(0, 0.5f, 0.16f), V(0.09f, 0.22f, 0.09f), V(25, 0, 0))
                    .Mirror(Cry, gB, cG, V(0.26f, 0.42f, -0.08f), V(0.08f, 0.2f, 0.08f), V(-10, 0, -55))
                    .Mirror(Cyl, gH, cP, V(0.1f, 0.4f, 0.3f), V(0.04f, 0.2f, 0.04f))
                    .Mirror(Sph, gH, cW, V(0.1f, 0.52f, 0.31f), V(0.1f, 0.1f, 0.1f))
                    .Mirror(Sph, gH, cD, V(0.1f, 0.53f, 0.35f), V(0.05f, 0.05f, 0.04f))
                    .Mirror(Sph, gAL, gAR, cP, V(0.4f, 0.2f, 0.3f), V(0.22f, 0.14f, 0.26f))
                    .Mirror(Cry, gAL, gAR, cG, V(0.42f, 0.2f, 0.48f), V(0.07f, 0.2f, 0.07f), V(90, 0, 0))
                    .Pivot(gH, V(0, 0.3f, 0.24f)).PivotPair(gAL, gAR, V(0.3f, 0.2f, 0.2f))
                    .Legs4(cP, 0.32f, 0.14f, -0.18f, 0.16f, 0.05f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 097 HALCYRIE (Epic)
            {
                var d = Kin(b, 97, "halcyrie", "Halcyrie", Element.Lumen, Element.Gale, Rarity.Epic, All, Temperament.Docile, Locomotion.Flyer,
                    150, 25, 19, 3f, 9.5f, 1.5f,
                    "The dawn bird. It crosses the island once each morning with a halo over its crest and three ribbons of light behind it, and the day does not start until it has.",
                    "Radiant, unhurried, answers kindness and nothing else.", 0.1f, 1, 1, A("solar_flare", "radiant_charge"), A("cyclone"),
                    Work((WorkType.Charging, 4), (WorkType.Hauling, 3)), CompanionPerk.Glow, "Companion: bright lantern light at night. Camp: powers a whole base alone.",
                    Drops(("crystal", 1, 2, 0.7f), ("pelt", 2, 3, 1f)),
                    Pal(Hex("#fff6d8"), Hex("#ffd070"), Hex("#f08a30"), Hex("#fffbe8")), Pal(Hex("#d8e8ff"), Hex("#7ab0ff"), Hex("#c060ff"), Hex("#ffffff")));
                d.spawnTime = SpawnTime.Day;
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 1.0f, 0), V(0.46f, 0.5f, 0.8f), V(-15, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.94f, 0.14f), V(0.36f, 0.38f, 0.5f))
                    .Whip(gB, cP, V(0, 1.14f, 0.34f), V(0, 0.09f, 0.07f), V(0, 0.004f, -0.006f), 4, 0.24f, 0.18f)
                    .Mirror(Cyl, gB, cA, V(0.1f, 0.72f, 0.02f), V(0.04f, 0.24f, 0.04f))
                    .Add(Sph, gH, cP, V(0, 1.56f, 0.6f), V(0.28f, 0.26f, 0.3f))
                    .Add(Con, gH, cA, V(0, 1.52f, 0.8f), V(0.08f, 0.22f, 0.08f), V(100, 0, 0))
                    .Add(Con, gH, cG, V(0, 1.8f, 0.5f), V(0.06f, 0.3f, 0.08f), V(-25, 0, 0))
                    .Mirror(Con, gH, cG, V(0.07f, 1.76f, 0.48f), V(0.05f, 0.24f, 0.07f), V(-30, 0, -22))
                    .Eyes(gH, V(0.1f, 1.6f, 0.7f), 0.05f)
                    .Wings(cP, cS, cG, V(0.2f, 1.1f, -0.02f), 1.55f, 0.56f, 14)
                    .Whip(gT, cS, V(0, 0.92f, -0.42f), V(0, -0.01f, -0.12f), V(0, -0.003f, 0), 12, 0.07f, 0.04f)
                    .Tip(Sph, gT, cG, V(0.16f, 0.2f, 0.1f))
                    .WhipPair(gT, gT, cS, V(0.1f, 0.92f, -0.4f), V(0.02f, -0.016f, -0.11f), V(0.002f, -0.003f, 0), 10, 0.06f, 0.035f)
                    .TipPair(Sph, gT, gT, cG, V(0.12f, 0.16f, 0.08f))
                    .Ring(Sph, gX, cG, V(0, 1.9f, 0.56f), 0.2f, 14, V(0.1f, 0.035f, 0.05f))
                    .Pivot(gH, V(0, 1.44f, 0.5f)).PivotPair(gWL, gWR, V(0.2f, 1.1f, 0)).Pivot(gT, V(0, 0.94f, -0.36f)).Pivot(gX, V(0, 1.9f, 0.56f))
                    .Done(d);
            }
        }
    }
}
