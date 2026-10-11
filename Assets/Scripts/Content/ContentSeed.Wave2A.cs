using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 2: Normal (#048-052), Flora (#053-057), Ember (#058-062).</summary>
        static void BuildWave2A(Bundle b)
        {
            const BiomeMask M = BiomeMask.Meadow, Bl = BiomeMask.Badlands, Pk = BiomeMask.Peaks;

            // ================================================================ NORMAL
            // ---------------------------------------------------------------- 048 BIBBIT (cute)
            {
                var d = Kin(b, 48, "bibbit", "Bibbit", Element.Normal, Element.Normal, Rarity.Common, M | Pk, Temperament.Timid, Locomotion.Hopper,
                    44, 8, 8, 2.2f, 6f, 0.6f,
                    "A ball of fluff with ears twice as long as the rest of it. Bibbits hide by sitting very still and hoping.",
                    "Jumpy, soft-hearted, falls asleep mid-hop.", 2.4f, 2, 4, A("tackle", "quick_kick"), A("dazzle_hop"),
                    Work((WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("pelt", 1, 1, 0.7f), ("berry", 1, 2, 0.5f)),
                    Pal(Hex("#f4e6d2"), Hex("#fffaf0"), Hex("#ffb0b8"), Hex("#fff0a0")), Pal(Hex("#b8d8ff"), Hex("#f0f8ff"), Hex("#ffd070"), Hex("#ffffff")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.3f, 0), V(0.56f, 0.52f, 0.54f))
                    .Add(Sph, gB, cS, V(0, 0.24f, 0.12f), V(0.4f, 0.36f, 0.36f))
                    .Mirror(Sph, gH, cP, V(0.13f, 0.74f, -0.02f), V(0.13f, 0.48f, 0.08f), V(-8, 0, -14))
                    .Mirror(Sph, gH, cA, V(0.13f, 0.74f, 0.01f), V(0.07f, 0.36f, 0.05f), V(-8, 0, -14))
                    .Mirror(Sph, gB, cA, V(0.2f, 0.26f, 0.2f), V(0.12f, 0.09f, 0.07f))
                    .Add(Sph, gB, cD, V(0, 0.3f, 0.275f), V(0.05f, 0.04f, 0.03f))
                    .Eyes(gB, V(0.11f, 0.38f, 0.23f), 0.09f)
                    .Mirror(Sph, gFL, gFR, cS, V(0.13f, 0.04f, 0.1f), V(0.16f, 0.08f, 0.22f))
                    .Add(Sph, gT, cW, V(0, 0.26f, -0.28f), V(0.16f, 0.16f, 0.16f))
                    .Pivot(gH, V(0, 0.5f, 0)).Pivot(gT, V(0, 0.26f, -0.24f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 049 TUSKAROO (wacky: tusks + mace tail)
            {
                var d = Kin(b, 49, "tuskaroo", "Tuskaroo", Element.Normal, Element.Stone, Rarity.Uncommon, Bl, Temperament.Territorial, Locomotion.Hopper,
                    70, 14, 12, 2.4f, 7f, 1f,
                    "A desert bounder with boar tusks and a tail that ends in a spiked knot of bone. It swings first and sizes you up later.",
                    "Hot-headed, proud, a surprisingly gentle parent.", 1.4f, 1, 2, A("mace_swing", "quick_kick"), A("pummel_rush"),
                    Work((WorkType.Hauling, 2), (WorkType.Quarrying, 1), (WorkType.Logging, 1)), CompanionPerk.LumberBoost, "Companion: +50% wood.",
                    Drops(("pelt", 1, 2, 0.9f), ("stone", 1, 2, 0.4f)),
                    Pal(Hex("#c89860"), Hex("#f0dcb8"), Hex("#7a4a30"), Hex("#ffe0a0")), Pal(Hex("#6a80a8"), Hex("#e0e8f4"), Hex("#30405a"), Hex("#ffd040")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.62f, -0.02f), V(0.46f, 0.66f, 0.5f), V(-18, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.56f, 0.14f), V(0.32f, 0.42f, 0.24f))
                    .Add(Sph, gH, cP, V(0, 1.05f, 0.2f), V(0.3f, 0.28f, 0.36f))
                    .Add(Sph, gH, cS, V(0, 1.0f, 0.36f), V(0.2f, 0.16f, 0.2f))
                    .Add(Sph, gH, cD, V(0, 1.03f, 0.46f), V(0.06f, 0.05f, 0.04f))
                    .Mirror(Con, gH, cW, V(0.1f, 1.0f, 0.43f), V(0.07f, 0.16f, 0.07f), V(20, 0, -14))
                    .Mirror(Sph, gH, cP, V(0.12f, 1.26f, 0.12f), V(0.09f, 0.26f, 0.06f), V(0, 0, -15))
                    .Eyes(gH, V(0.1f, 1.1f, 0.32f), 0.055f)
                    .Mirror(Cap, gAL, gAR, cP, V(0.2f, 0.72f, 0.16f), V(0.09f, 0.26f, 0.09f), V(40, 0, 0))
                    .Mirror(Sph, gFL, gFR, cP, V(0.2f, 0.3f, -0.06f), V(0.2f, 0.36f, 0.3f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.2f, 0.05f, 0.1f), V(0.14f, 0.1f, 0.4f))
                    .Whip(gT, cP, V(0, 0.42f, -0.26f), V(0, -0.05f, -0.13f), V(0, 0.012f, 0), 6, 0.2f, 0.1f)
                    .Tip(Sph, gT, cA, V(0.24f, 0.24f, 0.24f))
                    .Add(Con, gT, cW, V(0, 0.4f, -1.12f), V(0.07f, 0.16f, 0.07f))
                    .Mirror(Con, gT, cW, V(0.14f, 0.26f, -1.12f), V(0.07f, 0.16f, 0.07f), V(0, 0, -90))
                    .Add(Con, gT, cW, V(0, 0.26f, -1.27f), V(0.07f, 0.16f, 0.07f), V(-90, 0, 0))
                    .Pivot(gH, V(0, 0.92f, 0.1f)).Pivot(gT, V(0, 0.42f, -0.22f))
                    .PivotPair(gAL, gAR, V(0.2f, 0.82f, 0.1f)).PivotPair(gFL, gFR, V(0.2f, 0.4f, -0.06f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 050 RAMPARTH (big, mount)
            {
                var d = Kin(b, 50, "ramparth", "Ramparth", Element.Normal, Element.Stone, Rarity.Rare, M | Pk, Temperament.Territorial, Locomotion.Quadruped,
                    120, 19, 18, 2.4f, 7.5f, 1.35f,
                    "A hill-sized ram whose horns grow a new curl every winter. Old herds are said to have carved the mountain passes just by walking them.",
                    "Unhurried, immovable, remembers every slight.", 0.6f, 1, 2, A("horn_toss", "stampede"), A("roll_crush"),
                    Work((WorkType.Hauling, 3), (WorkType.Logging, 2)), CompanionPerk.LumberBoost, "Companion: +50% wood. Camp: tireless hauler.",
                    Drops(("pelt", 2, 4, 1f), ("stone", 1, 2, 0.4f)),
                    Pal(Hex("#8a6a50"), Hex("#d8c8a8"), Hex("#f0e4c8"), Hex("#ffd890")), Pal(Hex("#3a3a44"), Hex("#b8b8c8"), Hex("#ffd040"), Hex("#ffffff")));
                Mount(d, 11.5f, 8f, V(0, 1.14f, -0.14f));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.78f, -0.1f), V(0.72f, 0.66f, 1.1f))
                    .Add(Sph, gB, cS, V(0, 0.92f, 0.28f), V(0.8f, 0.78f, 0.66f))
                    .Add(Sph, gB, cS, V(0, 0.62f, 0.42f), V(0.5f, 0.5f, 0.4f))
                    .Add(Sph, gH, cP, V(0, 0.78f, 0.74f), V(0.4f, 0.42f, 0.44f))
                    .Add(Sph, gH, cD, V(0, 0.66f, 0.92f), V(0.24f, 0.2f, 0.2f))
                    .Add(Sph, gH, cS, V(0, 0.5f, 0.84f), V(0.14f, 0.26f, 0.12f))
                    .Eyes(gH, V(0.14f, 0.84f, 0.9f), 0.05f)
                    .WhipPair(gH, gH, cA, V(0.2f, 0.96f, 0.7f), V(0.09f, 0.03f, -0.07f), V(-0.012f, -0.022f, 0.02f), 8, 0.17f, 0.07f)
                    .Add(Sph, gT, cS, V(0, 0.8f, -0.68f), V(0.12f, 0.3f, 0.12f), V(20, 0, 0))
                    .Pivot(gH, V(0, 0.8f, 0.55f)).Pivot(gT, V(0, 0.9f, -0.62f))
                    .Legs4(cP, 0.24f, 0.34f, -0.44f, 0.5f, 0.17f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 051 LASHTAIL (wacky: whip tail)
            {
                var d = Kin(b, 51, "lashtail", "Lashtail", Element.Normal, Element.Gale, Rarity.Uncommon, M | Bl, Temperament.Curious, Locomotion.Biped,
                    56, 13, 9, 2.8f, 7.5f, 0.85f,
                    "A ring-tailed trickster that cracks its tail like a whip to knock fruit out of trees - and hats off explorers.",
                    "Cheeky, quick-fingered, never where you left it.", 1.5f, 1, 3, A("whip_crack", "quick_kick"), A("gust_slash"),
                    Work((WorkType.Crafting, 2), (WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("pelt", 1, 2, 0.8f), ("fiber", 1, 2, 0.4f)),
                    Pal(Hex("#9a8a80"), Hex("#f0e8e0"), Hex("#3a3238"), Hex("#ffc83a")), Pal(Hex("#d8704a"), Hex("#fff0e0"), Hex("#4a2a20"), Hex("#7af0ff")));
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.5f, 0), V(0.3f, 0.5f, 0.28f))
                    .Add(Sph, gB, cS, V(0, 0.46f, 0.08f), V(0.22f, 0.34f, 0.18f))
                    .Add(Sph, gH, cP, V(0, 0.88f, 0.04f), V(0.38f, 0.34f, 0.34f))
                    .Add(Sph, gH, cW, V(0, 0.86f, 0.14f), V(0.3f, 0.24f, 0.2f))
                    .Add(Sph, gH, cD, V(0, 0.82f, 0.25f), V(0.07f, 0.05f, 0.04f))
                    .Mirror(Sph, gH, cP, V(0.2f, 1.02f, -0.02f), V(0.16f, 0.16f, 0.06f))
                    .Mirror(Sph, gH, cA, V(0.2f, 1.02f, 0.01f), V(0.1f, 0.1f, 0.05f))
                    .Mirror(Sph, gH, cA, V(0.09f, 0.9f, 0.2f), V(0.13f, 0.13f, 0.06f))
                    .Eyes(gH, V(0.09f, 0.9f, 0.225f), 0.07f, cG)
                    .Mirror(Cap, gAL, gAR, cP, V(0.2f, 0.52f, 0.02f), V(0.08f, 0.34f, 0.08f), V(0, 0, -12))
                    .Whip(gT, cP, V(0, 0.34f, -0.14f), V(0, 0.02f, -0.09f), V(0, 0.02f, 0.006f), 13, 0.12f, 0.08f, cA)
                    .Tip(Sph, gT, cA, V(0.14f, 0.2f, 0.14f))
                    .Pivot(gH, V(0, 0.74f, 0)).Pivot(gT, V(0, 0.34f, -0.1f)).PivotPair(gAL, gAR, V(0.18f, 0.66f, 0))
                    .Legs2(cP, 0.09f, 0.3f, 0.1f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 052 KINGMANE (big)
            {
                var d = Kin(b, 52, "kingmane", "Kingmane", Element.Normal, Element.Ember, Rarity.Rare, Bl, Temperament.Aggressive, Locomotion.Quadruped,
                    105, 22, 14, 2.8f, 8.5f, 1.3f,
                    "A great cat crowned with three horns of old gold. Its roar rolls across the mesas like distant thunder and every lesser kin falls quiet.",
                    "Regal, short-tempered, fiercely protective of its pride.", 0.55f, 1, 2, A("brace_bash", "war_cry"), A("flame_dash"),
                    Work((WorkType.Hauling, 2), (WorkType.Logging, 2)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("pelt", 2, 3, 1f), ("cinder", 1, 1, 0.3f)),
                    Pal(Hex("#e0b060"), Hex("#f8e8c0"), Hex("#a05a28"), Hex("#ffd040")), Pal(Hex("#f0f0f4"), Hex("#ffffff"), Hex("#6a6a80"), Hex("#7ae0ff")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.66f, -0.05f), V(0.52f, 0.52f, 1.0f))
                    .Add(Sph, gB, cS, V(0, 0.6f, 0.3f), V(0.5f, 0.5f, 0.4f))
                    .Fan(Sph, gH, cA, V(0, 0.9f, 0.46f), 0.28f, 10, V(0.24f, 0.36f, 0.26f))
                    .Add(Sph, gH, cA, V(0, 0.92f, 0.36f), V(0.6f, 0.62f, 0.3f))
                    .Add(Sph, gH, cP, V(0, 0.9f, 0.56f), V(0.4f, 0.38f, 0.4f))
                    .Add(Sph, gH, cS, V(0, 0.82f, 0.74f), V(0.22f, 0.16f, 0.16f))
                    .Add(Sph, gH, cD, V(0, 0.86f, 0.82f), V(0.07f, 0.05f, 0.04f))
                    .Add(Con, gH, cG, V(0, 1.28f, 0.52f), V(0.09f, 0.26f, 0.09f))
                    .Mirror(Con, gH, cG, V(0.13f, 1.23f, 0.52f), V(0.08f, 0.2f, 0.08f), V(0, 0, -26))
                    .Eyes(gH, V(0.11f, 0.95f, 0.73f), 0.05f)
                    .Whip(gT, cP, V(0, 0.7f, -0.54f), V(0, 0.03f, -0.1f), V(0, -0.012f, 0), 7, 0.09f, 0.07f)
                    .Tip(Sph, gT, cA, V(0.18f, 0.24f, 0.18f))
                    .Pivot(gH, V(0, 0.8f, 0.4f)).Pivot(gT, V(0, 0.7f, -0.5f))
                    .Legs4(cP, 0.18f, 0.3f, -0.38f, 0.44f, 0.14f, cD)
                    .Done(d);
            }

            // ================================================================ FLORA
            // ---------------------------------------------------------------- 053 ACORNIP (cute)
            {
                var d = Kin(b, 53, "acornip", "Acornip", Element.Flora, Element.Normal, Rarity.Common, M, Temperament.Timid, Locomotion.Biped,
                    42, 8, 10, 2.2f, 5.6f, 0.6f,
                    "An acorn that grew legs instead of roots. It tucks its feet under its cap and pretends to be a nut whenever anything looks at it.",
                    "Bashful, tidy, hoards one more seed than it needs.", 2.4f, 2, 4, A("leaf_dart", "tackle"), A("spore_puff"),
                    Work((WorkType.Foraging, 2), (WorkType.Cultivating, 1)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("berry", 1, 2, 0.7f), ("wood", 1, 1, 0.4f)),
                    Pal(Hex("#e0b070"), Hex("#7a4a28"), Hex("#ff9a8a"), Hex("#7ad050")), Pal(Hex("#f0e8d0"), Hex("#c8a040"), Hex("#ffb0d0"), Hex("#ff8ac0")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.3f, 0), V(0.5f, 0.52f, 0.5f))
                    .Add(Dom, gH, cS, V(0, 0.4f, 0), V(0.6f, 0.5f, 0.6f))
                    .Add(Cyl, gH, cS, V(0, 0.7f, 0), V(0.05f, 0.12f, 0.05f))
                    .Add(Sph, gH, cG, V(0.07f, 0.77f, 0), V(0.18f, 0.05f, 0.1f), V(0, 0, -25))
                    .Eyes(gB, V(0.11f, 0.3f, 0.21f), 0.1f)
                    .Mirror(Sph, gB, cA, V(0.17f, 0.2f, 0.2f), V(0.09f, 0.07f, 0.05f))
                    .Add(Sph, gB, cD, V(0, 0.22f, 0.245f), V(0.04f, 0.03f, 0.02f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.12f, 0.04f, 0.04f), V(0.15f, 0.08f, 0.2f))
                    .Mirror(Sph, gAL, gAR, cP, V(0.27f, 0.28f, 0.04f), V(0.1f, 0.14f, 0.1f))
                    .Add(Sph, gT, cG, V(0, 0.34f, -0.32f), V(0.2f, 0.34f, 0.06f), V(-30, 0, 0))
                    .Pivot(gH, V(0, 0.42f, 0)).Pivot(gT, V(0, 0.2f, -0.24f))
                    .PivotPair(gFL, gFR, V(0.12f, 0.1f, 0)).PivotPair(gAL, gAR, V(0.25f, 0.32f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 054 VINELASH (wacky: vine whips + spike collar)
            {
                var d = Kin(b, 54, "vinelash", "Vinelash", Element.Flora, Element.Shade, Rarity.Uncommon, M, Temperament.Territorial, Locomotion.Biped,
                    64, 14, 11, 2f, 5.5f, 1f,
                    "A snapping pod on a bulb, with two thorned vines it cracks like whips. It is always hungry and not at all picky.",
                    "Grabby, gluttonous, sulks when scolded.", 1.3f, 1, 2, A("vine_whip", "needle_volley"), A("coil_strike"),
                    Work((WorkType.Cultivating, 2), (WorkType.Crafting, 1), (WorkType.Logging, 1)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("fiber", 2, 4, 1f)),
                    Pal(Hex("#4f9a48"), Hex("#c8407a"), Hex("#2f6a34"), Hex("#ffe060")), Pal(Hex("#7a6ad0"), Hex("#ffd040"), Hex("#3a2a7a"), Hex("#aef0ff")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.36f, 0), V(0.5f, 0.56f, 0.5f))
                    .Ring(Con, gB, cA, V(0, 0.62f, 0), 0.2f, 7, V(0.12f, 0.3f, 0.06f), 55)
                    .Add(Cyl, gB, cP, V(0, 0.74f, 0.02f), V(0.14f, 0.28f, 0.14f))
                    .Add(Sph, gH, cS, V(0, 1.02f, 0.08f), V(0.46f, 0.4f, 0.52f))
                    .Add(Sph, gH, cD, V(0, 0.98f, 0.22f), V(0.36f, 0.1f, 0.34f))
                    .Mirror(Con, gH, cW, V(0.1f, 1.02f, 0.33f), V(0.05f, 0.1f, 0.05f), V(180, 0, 0))
                    .Mirror(Con, gH, cW, V(0.05f, 0.93f, 0.35f), V(0.05f, 0.09f, 0.05f))
                    .Eyes(gH, V(0.14f, 1.14f, 0.24f), 0.06f, cG)
                    .WhipPair(gAL, gAR, cP, V(0.24f, 0.56f, 0), V(0.09f, 0.03f, 0.03f), V(0, -0.012f, 0.006f), 10, 0.1f, 0.06f)
                    .TipPair(Con, gAL, gAR, cG, V(0.1f, 0.2f, 0.1f))
                    .Mirror(Sph, gFL, gFR, cA, V(0.14f, 0.06f, 0.02f), V(0.2f, 0.12f, 0.26f))
                    .Pivot(gH, V(0, 0.86f, 0.02f)).PivotPair(gAL, gAR, V(0.22f, 0.56f, 0)).PivotPair(gFL, gFR, V(0.14f, 0.12f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 055 THORNBACK (big)
            {
                var d = Kin(b, 55, "thornback", "Thornback", Element.Flora, Element.Stone, Rarity.Rare, M, Temperament.Territorial, Locomotion.Quadruped,
                    130, 18, 24, 1.8f, 5f, 1.4f,
                    "A walking bramble hill. Birds nest between the thorns on its bark shell, and the bloom on its tail opens only for those it trusts.",
                    "Slow to anger, slower to forgive, gentle with small things.", 0.55f, 1, 1, A("root_ram", "roll_crush"), A("petal_storm"),
                    Work((WorkType.Logging, 3), (WorkType.Cultivating, 2), (WorkType.Quarrying, 1)), CompanionPerk.LumberBoost, "Companion: +50% wood. Camp: master logger.",
                    Drops(("fiber", 3, 5, 1f), ("wood", 2, 3, 0.8f)),
                    Pal(Hex("#6a8a4a"), Hex("#6a4a30"), Hex("#d8c8a0"), Hex("#ff6aa0")), Pal(Hex("#8aa8c8"), Hex("#3a3a50"), Hex("#f0f0ff"), Hex("#ffd040")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.5f, 0), V(0.9f, 0.56f, 1.2f))
                    .Add(Dom, gB, cS, V(0, 0.52f, -0.02f), V(0.86f, 0.6f, 1.1f))
                    .Spikes(gB, cA, 0.76f, 0.36f, -0.42f, 5, 0.12f, 0.28f, 0.05f, -10)
                    .Spikes(gB, cA, 0.66f, 0.3f, -0.36f, 4, 0.11f, 0.24f, 0.04f, -10, 0.26f, -38)
                    .Add(Sph, gH, cP, V(0, 0.46f, 0.72f), V(0.36f, 0.3f, 0.4f))
                    .Add(Con, gH, cS, V(0, 0.42f, 0.94f), V(0.14f, 0.16f, 0.12f), V(100, 0, 0))
                    .Mirror(Con, gH, cA, V(0.19f, 0.54f, 0.66f), V(0.08f, 0.2f, 0.08f), V(-20, 0, -60))
                    .Eyes(gH, V(0.12f, 0.52f, 0.87f), 0.045f)
                    .Whip(gT, cP, V(0, 0.44f, -0.62f), V(0, -0.01f, -0.13f), V(0, 0.004f, 0), 5, 0.22f, 0.12f)
                    .Tip(Sph, gT, cG, V(0.3f, 0.3f, 0.3f))
                    .Ring(Con, gT, cA, V(0, 0.42f, -1.32f), 0.13f, 5, V(0.08f, 0.18f, 0.08f), 80)
                    .Pivot(gH, V(0, 0.46f, 0.56f)).Pivot(gT, V(0, 0.46f, -0.56f))
                    .Legs4(cP, 0.3f, 0.36f, -0.4f, 0.3f, 0.18f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 056 FLORESSA (humanoid bloom)
            {
                var d = Kin(b, 56, "floressa", "Floressa", Element.Flora, Element.Lumen, Rarity.Rare, M, Temperament.Docile, Locomotion.Biped,
                    88, 17, 13, 2.4f, 6.5f, 1.15f,
                    "Grown, not born. Her gown is living petals, her slippers are furled leaves and her hair is a waterfall of vines. Gardens bloom where she has walked.",
                    "Graceful, curious about people, hums to seedlings.", 0.45f, 1, 1, A("petal_storm", "vine_whip"), A("aurora_veil"),
                    Work((WorkType.Cultivating, 4), (WorkType.Crafting, 2), (WorkType.Foraging, 2)), CompanionPerk.Mending, "Companion: slowly restores your HP. Camp: gifted gardener.",
                    Drops(("fiber", 2, 4, 1f), ("berry", 2, 3, 0.8f), ("crystal", 1, 1, 0.15f)),
                    Pal(Hex("#a8e090"), Hex("#ff8ac0"), Hex("#3f9a50"), Hex("#ffe880"), Hex("#5a2a6a")),
                    Pal(Hex("#c8e8ff"), Hex("#b08aff"), Hex("#4a6ac0"), Hex("#fff8d0"), Hex("#2a2a6a")));
                Effects(d, AuraFx.Petals);
                new Plan()
                    // gown: petal skirt over a bell, leaf belt, petal bodice and sleeves
                    .Add(Con, gB, cS, V(0, 0.55f, 0), V(0.6f, 0.5f, 0.6f))
                    .Ring(Sph, gB, cS, V(0, 0.42f, 0), 0.24f, 7, V(0.24f, 0.4f, 0.08f), -18)
                    .Ring(Sph, gB, cW, V(0, 0.5f, 0), 0.2f, 7, V(0.2f, 0.34f, 0.08f), -14, 25)
                    .Ring(Sph, gB, cA, V(0, 0.76f, 0), 0.12f, 6, V(0.14f, 0.16f, 0.05f), -35)
                    .Add(Sph, gB, cS, V(0, 0.92f, 0), V(0.26f, 0.34f, 0.2f))
                    .Add(Cyl, gB, cG, V(0, 0.78f, 0), V(0.25f, 0.04f, 0.21f))
                    .Mirror(Sph, gB, cS, V(0.16f, 1.07f, 0), V(0.14f, 0.1f, 0.14f))
                    .Add(Cyl, gB, cP, V(0, 1.14f, 0), V(0.07f, 0.1f, 0.07f))
                    // head, face, hair
                    .Add(Sph, gH, cP, V(0, 1.3f, 0.01f), V(0.3f, 0.32f, 0.3f))
                    .Add(Sph, gH, cA, V(0, 1.36f, -0.04f), V(0.34f, 0.3f, 0.34f))
                    .Mirror(Sph, gH, cA, V(0.1f, 1.42f, 0.1f), V(0.16f, 0.1f, 0.12f), V(0, 0, -20))
                    .Eyes(gH, V(0.075f, 1.3f, 0.135f), 0.055f)
                    .Mirror(Sph, gH, cS, V(0.1f, 1.24f, 0.12f), V(0.06f, 0.035f, 0.03f))
                    .Add(Sph, gH, cS, V(0, 1.21f, 0.145f), V(0.05f, 0.02f, 0.02f))
                    .WhipPair(gH, gH, cA, V(0.15f, 1.26f, 0.04f), V(0.004f, -0.04f, 0.003f), V(0, 0, 0), 9, 0.1f, 0.06f)
                    .Add(Sph, gH, cS, V(0.15f, 1.47f, 0.06f), V(0.15f, 0.1f, 0.15f))
                    .Add(Sph, gH, cG, V(0.15f, 1.51f, 0.07f), V(0.07f, 0.06f, 0.07f))
                    .Add(Sph, gH, cW, V(0.05f, 1.5f, 0.1f), V(0.07f, 0.05f, 0.07f))
                    // long hair down the back (sways with the tail group)
                    .Whip(gT, cA, V(0, 1.3f, -0.16f), V(0, -0.045f, -0.007f), V(0, 0, 0.001f), 21, 0.22f, 0.1f)
                    .Tip(Sph, gT, cS, V(0.1f, 0.08f, 0.1f))
                    .WhipPair(gT, gT, cA, V(0.12f, 1.28f, -0.12f), V(0.006f, -0.045f, -0.004f), V(0, 0, 0.0006f), 19, 0.17f, 0.08f)
                    .TipPair(Sph, gT, gT, cG, V(0.07f, 0.07f, 0.07f))
                    // arms with leaf bracelets
                    .Mirror(Cap, gAL, gAR, cP, V(0.2f, 0.88f, 0), V(0.07f, 0.42f, 0.07f), V(0, 0, -10))
                    .Mirror(Sph, gAL, gAR, cA, V(0.235f, 0.72f, 0), V(0.1f, 0.05f, 0.1f))
                    .Mirror(Sph, gAL, gAR, cP, V(0.24f, 0.66f, 0), V(0.08f, 0.08f, 0.08f))
                    // stem legs in furled-leaf slippers with a bud on each toe
                    .Legs2(cP, 0.07f, 0.34f, 0.07f)
                    .Mirror(Sph, gFL, gFR, cA, V(0.07f, 0.035f, 0.05f), V(0.1f, 0.07f, 0.22f))
                    .Mirror(Sph, gFL, gFR, cG, V(0.07f, 0.07f, 0.16f), V(0.05f, 0.05f, 0.05f))
                    .Pivot(gH, V(0, 1.16f, 0)).Pivot(gT, V(0, 1.3f, -0.12f)).PivotPair(gAL, gAR, V(0.17f, 1.06f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 057 CAPSHROOM (wacky: orbiting spores)
            {
                var d = Kin(b, 57, "capshroom", "Capshroom", Element.Flora, Element.Shade, Rarity.Uncommon, M | Pk, Temperament.Docile, Locomotion.Hopper,
                    60, 11, 13, 1.8f, 4.6f, 0.8f,
                    "A toadstool that wanders at night, trailing a slow halo of glowing spores. Follow one and you will find water - or get very lost.",
                    "Sleepy, mysterious, speaks only in yawns.", 1.4f, 1, 3, A("spore_puff", "hex_flicker"), A("pollen_burst"),
                    Work((WorkType.Cultivating, 2), (WorkType.Foraging, 2)), CompanionPerk.Glow, "Companion: soft light at night.",
                    Drops(("fiber", 1, 2, 0.8f), ("berry", 1, 2, 0.5f)),
                    Pal(Hex("#7a4ac0"), Hex("#f0e4d0"), Hex("#4a2a7a"), Hex("#7af0e0")), Pal(Hex("#e04a4a"), Hex("#fff4e0"), Hex("#8a2a2a"), Hex("#ffe060")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Add(Cap, gB, cS, V(0, 0.3f, 0), V(0.36f, 0.56f, 0.36f))
                    .Add(Cyl, gB, cS, V(0, 0.5f, 0), V(0.44f, 0.06f, 0.44f))
                    .Add(Dom, gH, cP, V(0, 0.52f, 0), V(0.96f, 0.7f, 0.96f))
                    .Add(Cyl, gH, cA, V(0, 0.52f, 0), V(0.9f, 0.04f, 0.9f))
                    .Mirror(Sph, gH, cW, V(0.26f, 0.74f, 0.14f), V(0.16f, 0.08f, 0.16f))
                    .Mirror(Sph, gH, cW, V(0.2f, 0.7f, -0.26f), V(0.14f, 0.07f, 0.14f))
                    .Add(Sph, gH, cW, V(0, 0.85f, -0.04f), V(0.2f, 0.08f, 0.2f))
                    .Add(Sph, gH, cW, V(0, 0.7f, 0.36f), V(0.14f, 0.07f, 0.12f))
                    .Eyes(gB, V(0.09f, 0.32f, 0.165f), 0.075f, cG)
                    .Add(Sph, gB, cD, V(0, 0.22f, 0.18f), V(0.07f, 0.04f, 0.03f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.1f, 0.03f, 0.04f), V(0.16f, 0.07f, 0.2f))
                    .Ring(Sph, gX, cG, V(0, 0.4f, 0), 0.62f, 4, V(0.08f, 0.08f, 0.08f))
                    .Add(Sph, gX, cG, V(0.3f, 0.2f, 0.36f), V(0.05f, 0.05f, 0.05f))
                    .Pivot(gH, V(0, 0.52f, 0)).Pivot(gX, V(0, 0.4f, 0))
                    .Done(d);
            }

            // ================================================================ EMBER
            // ---------------------------------------------------------------- 058 CINDLING (cute)
            {
                var d = Kin(b, 58, "cindling", "Cindling", Element.Ember, Element.Normal, Rarity.Common, Bl, Temperament.Curious, Locomotion.Quadruped,
                    44, 11, 7, 2.6f, 6.5f, 0.6f,
                    "A fox kit with a candle-flame for a tail. It curls up on cooling lava like a hearth rug and purrs like a kettle.",
                    "Nosy, cuddly, sneezes sparks.", 2.4f, 1, 3, A("ember_spit", "tackle"), A("flame_dash"),
                    Work((WorkType.Kindling, 1), (WorkType.Hauling, 1)), CompanionPerk.Warmth, "Companion: warmth aura (slower hunger) + light.",
                    Drops(("cinder", 1, 1, 0.6f), ("pelt", 1, 1, 0.5f)),
                    Pal(Hex("#f08a3a"), Hex("#fff0d0"), Hex("#c8402a"), Hex("#ffd84a")), Pal(Hex("#5a8af0"), Hex("#e8f4ff"), Hex("#2a4ac0"), Hex("#aef0ff")));
                Effects(d, AuraFx.Embers);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.28f, -0.02f), V(0.32f, 0.3f, 0.44f))
                    .Add(Sph, gB, cS, V(0, 0.24f, 0.1f), V(0.24f, 0.2f, 0.22f))
                    .Add(Sph, gH, cP, V(0, 0.5f, 0.22f), V(0.42f, 0.38f, 0.38f))
                    .Add(Sph, gH, cS, V(0, 0.43f, 0.36f), V(0.2f, 0.14f, 0.14f))
                    .Add(Sph, gH, cD, V(0, 0.45f, 0.435f), V(0.05f, 0.04f, 0.03f))
                    .Mirror(Con, gH, cP, V(0.15f, 0.74f, 0.18f), V(0.16f, 0.26f, 0.08f), V(0, 0, -18))
                    .Mirror(Con, gH, cA, V(0.15f, 0.72f, 0.2f), V(0.09f, 0.16f, 0.05f), V(0, 0, -18))
                    .Add(Con, gH, cG, V(0, 0.74f, 0.26f), V(0.08f, 0.16f, 0.08f), V(-15, 0, 0))
                    .Eyes(gH, V(0.11f, 0.54f, 0.37f), 0.085f)
                    .Add(Sph, gT, cA, V(0, 0.36f, -0.3f), V(0.2f, 0.24f, 0.24f))
                    .Add(Con, gT, cG, V(0, 0.52f, -0.36f), V(0.2f, 0.34f, 0.2f), V(-25, 0, 0))
                    .Pivot(gH, V(0, 0.4f, 0.12f)).Pivot(gT, V(0, 0.3f, -0.2f))
                    .Legs4(cP, 0.1f, 0.12f, -0.16f, 0.16f, 0.08f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 059 SCORCHORN (wacky: lava horns + back spikes)
            {
                var d = Kin(b, 59, "scorchorn", "Scorchorn", Element.Ember, Element.Stone, Rarity.Uncommon, Bl, Temperament.Territorial, Locomotion.Quadruped,
                    66, 15, 13, 2.2f, 6f, 0.95f,
                    "A cliff goat whose horns never cooled. It headbutts boulders to light them, then naps beside the glow.",
                    "Stubborn, loud, oddly house-proud about its ledge.", 1.4f, 1, 3, A("flare_horn", "flame_dash"), A("roll_crush"),
                    Work((WorkType.Kindling, 2), (WorkType.Quarrying, 1)), CompanionPerk.OreSense, "Companion: +50% ore and cinder when mining.",
                    Drops(("cinder", 1, 2, 0.9f), ("stone", 1, 2, 0.5f)),
                    Pal(Hex("#5a4a48"), Hex("#8a7a70"), Hex("#ff6a2a"), Hex("#ffd040")), Pal(Hex("#e8e8f0"), Hex("#b8b8c8"), Hex("#4ac0ff"), Hex("#ffffff")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.5f, 0), V(0.5f, 0.48f, 0.76f))
                    .Spikes(gB, cG, 0.7f, 0.22f, -0.3f, 5, 0.1f, 0.22f, 0.03f, -20)
                    .Add(Sph, gH, cP, V(0, 0.62f, 0.46f), V(0.34f, 0.34f, 0.36f))
                    .Add(Sph, gH, cS, V(0, 0.52f, 0.6f), V(0.2f, 0.18f, 0.2f))
                    .Add(Sph, gH, cS, V(0, 0.38f, 0.56f), V(0.08f, 0.2f, 0.07f))
                    .WhipPair(gH, gH, cA, V(0.12f, 0.8f, 0.44f), V(0.05f, 0.07f, -0.02f), V(0.012f, -0.012f, -0.014f), 9, 0.14f, 0.06f)
                    .TipPair(Sph, gH, gH, cG, V(0.07f, 0.1f, 0.07f))
                    .Eyes(gH, V(0.1f, 0.66f, 0.61f), 0.05f, cG)
                    .Add(Con, gT, cG, V(0, 0.56f, -0.42f), V(0.1f, 0.2f, 0.1f), V(-60, 0, 0))
                    .Pivot(gH, V(0, 0.56f, 0.32f)).Pivot(gT, V(0, 0.54f, -0.36f))
                    .Legs4(cD, 0.16f, 0.24f, -0.26f, 0.3f, 0.1f, cA)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 060 PYROVERN (big)
            {
                var d = Kin(b, 60, "pyrovern", "Pyrovern", Element.Ember, Element.Gale, Rarity.Rare, Bl, Temperament.Aggressive, Locomotion.Flyer,
                    100, 22, 14, 3f, 9f, 1.4f,
                    "A wyvern that nests in vents and hunts on the thermals above them. Its wingbeats smell of smoke long before you see it.",
                    "Proud, territorial, loyal once its respect is earned.", 0.5f, 1, 1, A("ember_spit", "sky_dive"), A("magma_maw"),
                    Work((WorkType.Kindling, 3), (WorkType.Hauling, 2)), CompanionPerk.Warmth, "Companion: warmth aura + light. Camp: blazing smelter-master.",
                    Drops(("cinder", 2, 3, 1f), ("pelt", 1, 2, 0.7f)),
                    Pal(Hex("#b03a2a"), Hex("#f0a050"), Hex("#2a2020"), Hex("#ffe060")), Pal(Hex("#3a3a4a"), Hex("#7ad0ff"), Hex("#e8e8f0"), Hex("#aef0ff")));
                Effects(d, AuraFx.Embers);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 1.0f, 0), V(0.46f, 0.5f, 0.9f), V(-12, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.9f, 0.12f), V(0.36f, 0.36f, 0.6f))
                    .Spikes(gB, cA, 1.2f, 0.26f, -0.36f, 4, 0.09f, 0.22f, 0.03f, -25)
                    .Whip(gB, cP, V(0, 1.14f, 0.4f), V(0, 0.08f, 0.1f), V(0, -0.006f, 0.006f), 4, 0.26f, 0.2f)
                    .Add(Sph, gH, cP, V(0, 1.5f, 0.84f), V(0.3f, 0.26f, 0.4f))
                    .Add(Sph, gH, cP, V(0, 1.44f, 1.04f), V(0.2f, 0.14f, 0.22f))
                    .Mirror(Con, gH, cA, V(0.1f, 1.68f, 0.72f), V(0.07f, 0.3f, 0.07f), V(-40, 0, -12))
                    .Mirror(Con, gH, cW, V(0.06f, 1.37f, 1.1f), V(0.03f, 0.07f, 0.03f), V(180, 0, 0))
                    .Eyes(gH, V(0.11f, 1.56f, 0.96f), 0.05f, cG)
                    .Wings(cS, cS, cA, V(0.18f, 1.18f, -0.04f), 1.4f, 0.6f, 20)
                    .Mirror(Cap, gWL, gWR, cP, V(0.62f, 1.36f, 0.12f), V(0.08f, 1.0f, 0.08f), V(0, 0, -70))
                    .Whip(gT, cP, V(0, 0.94f, -0.5f), V(0, -0.02f, -0.15f), V(0, 0.004f, 0), 7, 0.24f, 0.08f)
                    .Tip(Con, gT, cG, V(0.2f, 0.4f, 0.2f))
                    .Mirror(Cap, gB, cP, V(0.16f, 0.64f, -0.1f), V(0.12f, 0.36f, 0.12f))
                    .Mirror(Sph, gB, cA, V(0.16f, 0.46f, -0.06f), V(0.14f, 0.08f, 0.2f))
                    .Pivot(gH, V(0, 1.4f, 0.66f)).PivotPair(gWL, gWR, V(0.18f, 1.2f, 0)).Pivot(gT, V(0, 0.96f, -0.44f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 061 MAGMITE (wacky: smoking volcano shell)
            {
                var d = Kin(b, 61, "magmite", "Magmite", Element.Ember, Element.Stone, Rarity.Uncommon, Bl, Temperament.Docile, Locomotion.Slither,
                    72, 12, 18, 1.4f, 3.4f, 0.9f,
                    "A snail that carries a tiny volcano. The shell puffs when it is thinking and rumbles when it is cross - which is rarely, and slowly.",
                    "Unbothered, warm, leaves a trail of glass.", 1.3f, 1, 2, A("ember_spit", "shell_slam"), A("lava_plume"),
                    Work((WorkType.Kindling, 3), (WorkType.Quarrying, 1)), CompanionPerk.Warmth, "Companion: warmth aura (slower hunger) + light.",
                    Drops(("cinder", 2, 3, 1f), ("ore", 1, 1, 0.3f)),
                    Pal(Hex("#c8502a"), Hex("#b8b0a8"), Hex("#4a3a38"), Hex("#ffb030")), Pal(Hex("#4a8ac8"), Hex("#f0f0f8"), Hex("#2a3a5a"), Hex("#7af0ff")));
                Effects(d, AuraFx.Embers, ExtraAnim.Bob);
                new Plan()
                    .Serpent(cP, cP, 0.18f, 0.34f, 5, 0.22f)
                    .Add(Con, gB, cA, V(0, 0.62f, -0.18f), V(0.74f, 0.7f, 0.74f))
                    .Add(Cyl, gB, cG, V(0, 0.93f, -0.18f), V(0.2f, 0.07f, 0.2f))
                    .Mirror(Sph, gB, cG, V(0.13f, 0.72f, -0.1f), V(0.07f, 0.2f, 0.07f), V(0, 0, -22))
                    .Add(Sph, gB, cG, V(0, 0.66f, -0.38f), V(0.07f, 0.24f, 0.07f), V(-22, 0, 0))
                    .Add(Sph, gH, cP, V(0, 0.26f, 0.2f), V(0.3f, 0.26f, 0.3f))
                    .Mirror(Cyl, gH, cP, V(0.08f, 0.46f, 0.24f), V(0.04f, 0.24f, 0.04f), V(10, 0, -10))
                    .Mirror(Sph, gH, cW, V(0.1f, 0.6f, 0.27f), V(0.1f, 0.1f, 0.1f))
                    .Mirror(Sph, gH, cD, V(0.1f, 0.61f, 0.31f), V(0.05f, 0.05f, 0.04f))
                    .Add(Sph, gX, cS, V(0, 1.1f, -0.18f), V(0.18f, 0.16f, 0.18f))
                    .Add(Sph, gX, cS, V(0.08f, 1.28f, -0.2f), V(0.14f, 0.12f, 0.14f))
                    .Add(Sph, gX, cS, V(-0.04f, 1.42f, -0.16f), V(0.1f, 0.09f, 0.1f))
                    .Pivot(gH, V(0, 0.24f, 0.08f)).Pivot(gX, V(0, 1.1f, -0.18f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 062 EMBERHOOF (big, mount)
            {
                var d = Kin(b, 62, "emberhoof", "Emberhoof", Element.Ember, Element.Normal, Rarity.Rare, Bl, Temperament.Territorial, Locomotion.Quadruped,
                    95, 18, 13, 3.2f, 9.5f, 1.35f,
                    "A charger with a mane of living flame. It leaves smoking hoofprints across the mesas and will only carry a rider who can keep up on foot first.",
                    "Wild, proud, races the sunset every evening.", 0.55f, 1, 2, A("flame_dash", "quick_kick"), A("stampede"),
                    Work((WorkType.Hauling, 3), (WorkType.Kindling, 2)), CompanionPerk.Swiftness, "Companion: +12% run speed. Fast mount with a blazing mane.",
                    Drops(("pelt", 2, 3, 1f), ("cinder", 1, 2, 0.7f)),
                    Pal(Hex("#3a2a2a"), Hex("#5a4640"), Hex("#ff6a2a"), Hex("#ffc840")), Pal(Hex("#f0f0f4"), Hex("#c8c8d8"), Hex("#4a8aff"), Hex("#aef0ff")));
                Mount(d, 14f, 9.5f, V(0, 1.2f, -0.08f));
                Effects(d, AuraFx.Embers);
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.98f, -0.05f), V(0.44f, 1.0f, 0.46f), V(90, 0, 0))
                    .Add(Cap, gB, cP, V(0, 1.26f, 0.5f), V(0.26f, 0.56f, 0.3f), V(35, 0, 0))
                    .Add(Sph, gH, cP, V(0, 1.56f, 0.74f), V(0.26f, 0.26f, 0.36f))
                    .Add(Sph, gH, cS, V(0, 1.48f, 0.92f), V(0.18f, 0.16f, 0.22f))
                    .Mirror(Con, gH, cP, V(0.08f, 1.74f, 0.68f), V(0.07f, 0.14f, 0.05f))
                    .Eyes(gH, V(0.1f, 1.6f, 0.84f), 0.045f, cG)
                    .Add(Con, gH, cG, V(0, 1.76f, 0.6f), V(0.1f, 0.3f, 0.14f), V(-30, 0, 0))
                    .Add(Con, gB, cG, V(0, 1.62f, 0.44f), V(0.12f, 0.34f, 0.16f), V(-40, 0, 0))
                    .Add(Con, gB, cA, V(0, 1.46f, 0.3f), V(0.12f, 0.34f, 0.16f), V(-50, 0, 0))
                    .Add(Con, gB, cG, V(0, 1.3f, 0.14f), V(0.1f, 0.28f, 0.14f), V(-60, 0, 0))
                    .Add(Con, gT, cG, V(0, 0.98f, -0.8f), V(0.2f, 0.6f, 0.2f), V(-115, 0, 0))
                    .Add(Con, gT, cA, V(0, 0.96f, -0.74f), V(0.12f, 0.4f, 0.12f), V(-115, 0, 0))
                    .Pivot(gH, V(0, 1.42f, 0.6f)).Pivot(gT, V(0, 1.04f, -0.56f))
                    .Legs4(cP, 0.15f, 0.36f, -0.4f, 0.76f, 0.1f, cG)
                    .Done(d);
            }
        }
    }
}
