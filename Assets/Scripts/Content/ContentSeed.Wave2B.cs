using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 2: Tide (#063-067), Frost (#068-072), Volt (#073-077).</summary>
        static void BuildWave2B(Bundle b)
        {
            const BiomeMask M = BiomeMask.Meadow, Bl = BiomeMask.Badlands, Pk = BiomeMask.Peaks;

            // ================================================================ TIDE
            // ---------------------------------------------------------------- 063 DRIBBLET (cute)
            {
                var d = Kin(b, 63, "dribblet", "Dribblet", Element.Tide, Element.Normal, Rarity.Common, M, Temperament.Curious, Locomotion.Hopper,
                    46, 9, 9, 2.2f, 5.5f, 0.55f,
                    "A raindrop that decided not to land. It bounces from puddle to puddle blowing bubbles, and cries (more water) when the sun comes out.",
                    "Giggly, clingy, terrified of sponges.", 2.4f, 2, 4, A("bubble_burst", "tackle"), A("aqua_tail"),
                    Work((WorkType.Irrigating, 2)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 2, 0.9f)),
                    Pal(Hex("#5ab8f0"), Hex("#c8eeff"), Hex("#ff9ab0"), Hex("#d8f6ff")), Pal(Hex("#ffb0d8"), Hex("#fff0f8"), Hex("#ffe070"), Hex("#ffffff")));
                d.nearWater = true;
                Effects(d, AuraFx.Bubbles, ExtraAnim.Bob);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.28f, 0), V(0.5f, 0.48f, 0.5f))
                    .Add(Con, gB, cP, V(0, 0.6f, 0), V(0.3f, 0.3f, 0.3f))
                    .Add(Sph, gB, cP, V(0.04f, 0.78f, 0), V(0.1f, 0.1f, 0.1f))
                    .Add(Sph, gB, cS, V(0, 0.22f, 0.1f), V(0.36f, 0.3f, 0.34f))
                    .Add(Sph, gB, cW, V(-0.13f, 0.42f, 0.15f), V(0.1f, 0.14f, 0.05f))
                    .Eyes(gB, V(0.1f, 0.3f, 0.21f), 0.1f)
                    .Mirror(Sph, gB, cA, V(0.17f, 0.22f, 0.19f), V(0.08f, 0.06f, 0.04f))
                    .Mirror(Sph, gB, cP, V(0.27f, 0.22f, 0), V(0.14f, 0.06f, 0.12f), V(0, 0, -20))
                    .Mirror(Sph, gFL, gFR, cP, V(0.1f, 0.03f, 0.05f), V(0.14f, 0.06f, 0.18f))
                    .Add(Sph, gX, cG, V(0.16f, 0.92f, 0), V(0.12f, 0.12f, 0.12f))
                    .Add(Sph, gX, cG, V(-0.12f, 1.02f, 0.04f), V(0.07f, 0.07f, 0.07f))
                    .Pivot(gX, V(0, 0.9f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 064 WHIPSQUID (wacky: tentacle whips)
            {
                var d = Kin(b, 64, "whipsquid", "Whipsquid", Element.Tide, Element.Shade, Rarity.Uncommon, M, Temperament.Curious, Locomotion.Biped,
                    60, 14, 10, 2.2f, 5.8f, 0.95f,
                    "A squid that walked out of the lake to see what all the noise was. Its two long arms snap like wet towels and steal anything shiny.",
                    "Inquisitive, slippery, an incurable pickpocket.", 1.3f, 1, 2, A("tidal_lash", "bubble_burst"), A("hex_flicker"),
                    Work((WorkType.Irrigating, 2), (WorkType.Crafting, 2)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 2, 1f)),
                    Pal(Hex("#e86a8a"), Hex("#ffb0c0"), Hex("#6a3ac0"), Hex("#a0f0ff")), Pal(Hex("#4a6ad8"), Hex("#b0c8ff"), Hex("#ffd040"), Hex("#ffffff")));
                d.nearWater = true;
                new Plan()
                    .Add(Cap, gH, cP, V(0, 0.96f, -0.04f), V(0.44f, 0.8f, 0.44f), V(-10, 0, 0))
                    .Mirror(Sph, gH, cS, V(0.26f, 1.26f, -0.1f), V(0.24f, 0.3f, 0.05f), V(0, 0, -30))
                    .Mirror(Sph, gH, cA, V(0.11f, 1.12f, 0.15f), V(0.08f, 0.08f, 0.04f))
                    .Add(Sph, gH, cA, V(0, 1.28f, 0.1f), V(0.07f, 0.07f, 0.04f))
                    .Add(Sph, gB, cP, V(0, 0.52f, 0), V(0.42f, 0.36f, 0.4f))
                    .Eyes(gB, V(0.13f, 0.56f, 0.16f), 0.1f)
                    .Ring(Cap, gB, cP, V(0, 0.28f, 0), 0.15f, 6, V(0.1f, 0.36f, 0.1f), 18, 30)
                    .Mirror(Cap, gFL, gFR, cS, V(0.1f, 0.18f, 0.1f), V(0.11f, 0.36f, 0.11f))
                    .WhipPair(gAL, gAR, cP, V(0.22f, 0.5f, 0.04f), V(0.08f, -0.02f, 0.04f), V(0, -0.006f, 0.004f), 10, 0.1f, 0.06f)
                    .TipPair(Sph, gAL, gAR, cA, V(0.16f, 0.1f, 0.16f))
                    .Pivot(gH, V(0, 0.66f, 0)).PivotPair(gAL, gAR, V(0.2f, 0.5f, 0)).PivotPair(gFL, gFR, V(0.1f, 0.34f, 0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 065 MAELSTRAKE (big)
            {
                var d = Kin(b, 65, "maelstrake", "Maelstrake", Element.Tide, Element.Gale, Rarity.Rare, M | Bl, Temperament.Aggressive, Locomotion.Slither,
                    115, 21, 15, 2.6f, 8f, 1.5f,
                    "A horned river serpent that stirs whirlpools just to watch them. Fisherfolk used to leave it the first catch of every season.",
                    "Imperious, playful in a dangerous way, loves storms.", 0.45f, 1, 1, A("aqua_tail", "riptide"), A("cyclone"),
                    Work((WorkType.Irrigating, 3), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("gel", 2, 3, 1f), ("pelt", 1, 2, 0.6f)),
                    Pal(Hex("#2a6ab0"), Hex("#a8e0f0"), Hex("#40d0c0"), Hex("#fff0a0")), Pal(Hex("#6a2a8a"), Hex("#f0c8ff"), Hex("#ff6ab0"), Hex("#aef0ff")));
                d.nearWater = true;
                new Plan()
                    .Serpent(cP, cS, 0.34f, 0.52f, 8, 0.36f)
                    .Spikes(gB, cA, 0.54f, -0.05f, -1.0f, 4, 0.06f, 0.32f, 0, -30)
                    .Spikes(gT, cA, 0.4f, -1.45f, -2.3f, 3, 0.05f, 0.24f, 0, -30)
                    .Add(Sph, gH, cP, V(0, 0.58f, 0.44f), V(0.46f, 0.4f, 0.56f))
                    .Add(Sph, gH, cS, V(0, 0.46f, 0.56f), V(0.34f, 0.14f, 0.4f))
                    .Add(Con, gH, cG, V(0, 0.9f, 0.5f), V(0.1f, 0.4f, 0.1f), V(25, 0, 0))
                    .Mirror(Sph, gH, cA, V(0.26f, 0.66f, 0.3f), V(0.05f, 0.3f, 0.26f), V(0, 0, -40))
                    .Mirror(Con, gH, cW, V(0.1f, 0.4f, 0.74f), V(0.04f, 0.1f, 0.04f), V(180, 0, 0))
                    .Eyes(gH, V(0.15f, 0.66f, 0.66f), 0.06f, cG)
                    .WhipPair(gH, gH, cA, V(0.12f, 0.48f, 0.76f), V(0.05f, -0.02f, 0.06f), V(0.004f, -0.012f, -0.01f), 7, 0.05f, 0.03f)
                    .Add(Sph, gT, cA, V(0, 0.3f, -2.72f), V(0.05f, 0.5f, 0.4f))
                    .Pivot(gH, V(0, 0.44f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 066 PUFFSPINE (wacky: spikes all over + orbiting bubbles)
            {
                var d = Kin(b, 66, "puffspine", "Puffspine", Element.Tide, Element.Stone, Rarity.Uncommon, M | Bl, Temperament.Timid, Locomotion.Flyer,
                    58, 11, 16, 2f, 5f, 0.8f,
                    "A puffer that learned to float on air the way it floats on water. Startle one and it doubles in size and bristles from every side.",
                    "Nervous, easily offended, secretly loves a tummy rub.", 1.3f, 1, 3, A("bubble_burst", "spike_shot"), A("steam_blast"),
                    Work((WorkType.Irrigating, 2), (WorkType.Chilling, 1)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 2, 1f), ("stone", 1, 1, 0.3f)),
                    Pal(Hex("#f0c040"), Hex("#fff4d0"), Hex("#e0803a"), Hex("#c8f0ff")), Pal(Hex("#6ad0c0"), Hex("#f0fff8"), Hex("#2a8a9a"), Hex("#ffd0f0")));
                d.nearWater = true;
                Effects(d, AuraFx.Bubbles, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.9f, 0), V(0.72f, 0.68f, 0.72f))
                    .Add(Sph, gB, cS, V(0, 0.8f, 0.06f), V(0.6f, 0.5f, 0.6f))
                    .Ring(Con, gB, cA, V(0, 0.9f, 0), 0.4f, 8, V(0.08f, 0.2f, 0.08f), 90, 22.5f)
                    .Ring(Con, gB, cA, V(0, 1.12f, 0), 0.28f, 6, V(0.08f, 0.2f, 0.08f), 50, 30)
                    .Ring(Con, gB, cA, V(0, 0.68f, 0), 0.28f, 6, V(0.08f, 0.2f, 0.08f), 130, 30)
                    .Add(Con, gB, cA, V(0, 1.3f, 0), V(0.08f, 0.2f, 0.08f))
                    .Eyes(gB, V(0.15f, 0.96f, 0.31f), 0.09f)
                    .Add(Cyl, gB, cS, V(0, 0.82f, 0.35f), V(0.1f, 0.05f, 0.1f), V(90, 0, 0))
                    .Add(Sph, gB, cD, V(0, 0.82f, 0.375f), V(0.05f, 0.05f, 0.02f))
                    .Mirror(Sph, gWL, gWR, cA, V(0.4f, 0.86f, -0.05f), V(0.2f, 0.04f, 0.16f), V(0, 0, -20))
                    .Add(Sph, gT, cA, V(0, 0.9f, -0.46f), V(0.05f, 0.3f, 0.22f))
                    .Ring(Sph, gX, cG, V(0, 0.95f, 0), 0.64f, 3, V(0.12f, 0.12f, 0.12f))
                    .Add(Sph, gX, cG, V(0.3f, 1.3f, 0.3f), V(0.07f, 0.07f, 0.07f))
                    .PivotPair(gWL, gWR, V(0.34f, 0.86f, -0.05f)).Pivot(gT, V(0, 0.9f, -0.36f)).Pivot(gX, V(0, 0.95f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 067 TIDEWING (big)
            {
                var d = Kin(b, 67, "tidewing", "Tidewing", Element.Tide, Element.Gale, Rarity.Rare, M | Bl, Temperament.Docile, Locomotion.Flyer,
                    105, 17, 16, 3f, 8.5f, 1.5f,
                    "A manta that swims through the sky on sea mist. Whole flights of them cross the island at dusk like slow, silent kites.",
                    "Serene, gentle, impossible to hurry.", 0.5f, 1, 2, A("tidal_lash", "cyclone"), A("aurora_veil"),
                    Work((WorkType.Irrigating, 3), (WorkType.Hauling, 2)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("gel", 2, 3, 1f), ("pelt", 1, 1, 0.5f)),
                    Pal(Hex("#3a4a8a"), Hex("#e8f0ff"), Hex("#7ad0ff"), Hex("#aef0ff")), Pal(Hex("#2a2a30"), Hex("#f0f0f0"), Hex("#ff9ac0"), Hex("#ffe070")));
                d.nearWater = true;
                new Plan()
                    .Add(Sph, gB, cP, V(0, 1.0f, 0), V(0.7f, 0.24f, 0.9f))
                    .Add(Sph, gB, cS, V(0, 0.94f, 0.04f), V(0.56f, 0.16f, 0.76f))
                    .Mirror(Con, gB, cP, V(0.16f, 1.0f, 0.5f), V(0.12f, 0.3f, 0.08f), V(80, 0, -10))
                    .Eyes(gB, V(0.24f, 1.04f, 0.34f), 0.06f)
                    .Mirror(Sph, gB, cG, V(0.14f, 1.115f, -0.1f), V(0.1f, 0.03f, 0.1f))
                    .Add(Sph, gB, cG, V(0, 1.125f, 0.12f), V(0.12f, 0.03f, 0.12f))
                    .Wings(cP, cP, cA, V(0.26f, 1.0f, 0.04f), 1.25f, 0.74f, 10)
                    .Whip(gT, cA, V(0, 0.98f, -0.46f), V(0, 0, -0.1f), V(0, 0.002f, 0), 12, 0.08f, 0.03f)
                    .PivotPair(gWL, gWR, V(0.3f, 1.0f, 0)).Pivot(gT, V(0, 0.98f, -0.42f))
                    .Done(d);
            }

            // ================================================================ FROST
            // ---------------------------------------------------------------- 068 CHILLPIP (cute)
            {
                var d = Kin(b, 68, "chillpip", "Chillpip", Element.Frost, Element.Normal, Rarity.Common, Pk, Temperament.Curious, Locomotion.Biped,
                    46, 9, 10, 2.2f, 5.6f, 0.6f,
                    "A penguin chick with an icicle for a cowlick. It toboggans downhill on its belly and waddles all the way back up to do it again.",
                    "Fearless, chirpy, follows anyone who looks warm.", 2.4f, 2, 5, A("snowball", "tackle"), A("ice_fang"),
                    Work((WorkType.Chilling, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("pelt", 1, 1, 0.8f)),
                    Pal(Hex("#4a6aa8"), Hex("#e8f4ff"), Hex("#ffb040"), Hex("#aef0ff")), Pal(Hex("#a86a8a"), Hex("#fff0f8"), Hex("#ffe070"), Hex("#ffc0e8")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.34f, 0), V(0.48f, 0.6f, 0.46f))
                    .Add(Sph, gB, cW, V(0, 0.3f, 0.08f), V(0.36f, 0.46f, 0.34f))
                    .Add(Sph, gB, cS, V(0, 0.54f, 0.02f), V(0.44f, 0.12f, 0.42f))
                    .Add(Sph, gH, cP, V(0, 0.76f, 0.02f), V(0.42f, 0.38f, 0.4f))
                    .Add(Sph, gH, cW, V(0, 0.72f, 0.12f), V(0.32f, 0.26f, 0.22f))
                    .Add(Con, gH, cA, V(0, 0.7f, 0.26f), V(0.1f, 0.12f, 0.08f), V(95, 0, 0))
                    .Eyes(gH, V(0.1f, 0.78f, 0.2f), 0.085f)
                    .Add(Cry, gH, cG, V(0, 1.0f, 0), V(0.1f, 0.22f, 0.1f))
                    .Mirror(Cry, gH, cG, V(0.07f, 0.96f, 0), V(0.07f, 0.14f, 0.07f), V(0, 0, -28))
                    .Mirror(Sph, gAL, gAR, cP, V(0.26f, 0.38f, 0), V(0.08f, 0.3f, 0.16f), V(0, 0, -15))
                    .Mirror(Sph, gFL, gFR, cA, V(0.1f, 0.03f, 0.06f), V(0.16f, 0.06f, 0.22f))
                    .Pivot(gH, V(0, 0.6f, 0)).PivotPair(gAL, gAR, V(0.23f, 0.5f, 0)).PivotPair(gFL, gFR, V(0.1f, 0.08f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 069 SEALANCE (wacky: spiral ice horn)
            {
                var d = Kin(b, 69, "sealance", "Sealance", Element.Frost, Element.Tide, Rarity.Uncommon, Pk, Temperament.Docile, Locomotion.Quadruped,
                    70, 13, 14, 1.8f, 4.6f, 1f,
                    "A plump seal with a single spiral horn of blue ice, longer than its body. It uses it to drill fishing holes and to win arguments.",
                    "Jolly, lazy, honks at the moon.", 1.3f, 1, 3, A("icicle_spear", "aqua_tail"), A("ice_fang"),
                    Work((WorkType.Chilling, 2), (WorkType.Irrigating, 1)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("pelt", 1, 2, 0.9f), ("gel", 1, 1, 0.5f)),
                    Pal(Hex("#b8c8e0"), Hex("#f0f6ff"), Hex("#6a8ac0"), Hex("#9ae8ff")), Pal(Hex("#4a4a5a"), Hex("#d8d8e8"), Hex("#ffb0d0"), Hex("#ffe070")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.36f, -0.05f), V(0.56f, 0.52f, 1.0f))
                    .Add(Sph, gB, cS, V(0, 0.3f, 0.06f), V(0.46f, 0.38f, 0.76f))
                    .Mirror(Sph, gB, cA, V(0.16f, 0.56f, -0.2f), V(0.1f, 0.04f, 0.12f))
                    .Add(Sph, gH, cP, V(0, 0.5f, 0.5f), V(0.44f, 0.42f, 0.42f))
                    .Add(Sph, gH, cS, V(0, 0.42f, 0.66f), V(0.26f, 0.2f, 0.18f))
                    .Add(Sph, gH, cD, V(0, 0.47f, 0.75f), V(0.07f, 0.05f, 0.04f))
                    .Add(Con, gH, cG, V(0, 0.78f, 0.86f), V(0.09f, 0.8f, 0.09f), V(60, 0, 0))
                    .Add(Cyl, gH, cW, V(0, 0.66f, 0.65f), V(0.1f, 0.03f, 0.1f), V(60, 0, 0))
                    .Add(Cyl, gH, cW, V(0, 0.74f, 0.79f), V(0.085f, 0.03f, 0.085f), V(60, 0, 0))
                    .Add(Cyl, gH, cW, V(0, 0.82f, 0.93f), V(0.065f, 0.03f, 0.065f), V(60, 0, 0))
                    .Mirror(Con, gH, cW, V(0.17f, 0.42f, 0.7f), V(0.03f, 0.16f, 0.03f), V(0, 0, -80))
                    .Eyes(gH, V(0.13f, 0.56f, 0.66f), 0.06f)
                    .Mirror(Sph, gFL, gFR, cP, V(0.3f, 0.12f, 0.2f), V(0.3f, 0.08f, 0.2f), V(0, 0, -15))
                    .Mirror(Sph, gT, cP, V(0.14f, 0.2f, -0.62f), V(0.2f, 0.06f, 0.3f), V(0, 20, 0))
                    .Pivot(gH, V(0, 0.42f, 0.34f)).Pivot(gT, V(0, 0.26f, -0.5f)).PivotPair(gFL, gFR, V(0.24f, 0.2f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 070 TUNDRUSK (big, mount)
            {
                var d = Kin(b, 70, "tundrusk", "Tundrusk", Element.Frost, Element.Stone, Rarity.Rare, Pk, Temperament.Territorial, Locomotion.Quadruped,
                    150, 20, 24, 2f, 6f, 1.55f,
                    "A woolly giant with tusks of clear ice that never melt. Snow settles on its back and stays there; some carry whole drifts for years.",
                    "Ancient, patient, mourns its herd-mates.", 0.5f, 1, 2, A("ice_fang", "quake_stomp"), A("stampede"),
                    Work((WorkType.Hauling, 4), (WorkType.Logging, 2), (WorkType.Chilling, 2)), CompanionPerk.LumberBoost, "Companion: +50% wood. Tanky mount that ploughs through snow.",
                    Drops(("pelt", 3, 5, 1f), ("crystal", 1, 1, 0.3f)),
                    Pal(Hex("#7a5a48"), Hex("#a88a70"), Hex("#e8e0d0"), Hex("#9ae8ff")), Pal(Hex("#e8eef8"), Hex("#ffffff"), Hex("#8a9ab8"), Hex("#ffc0e8")));
                Mount(d, 10.5f, 6.5f, V(0, 1.36f, 0.12f));
                Effects(d, AuraFx.Snow);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.92f, -0.05f), V(0.86f, 0.86f, 1.16f))
                    .Add(Sph, gB, cS, V(0, 0.7f, -0.05f), V(0.9f, 0.5f, 1.1f))
                    .Add(Cry, gB, cG, V(0, 1.42f, -0.3f), V(0.16f, 0.34f, 0.16f))
                    .Add(Cry, gB, cG, V(0.08f, 1.36f, -0.5f), V(0.12f, 0.24f, 0.12f), V(-15, 0, -12))
                    .Add(Sph, gH, cP, V(0, 1.14f, 0.62f), V(0.56f, 0.6f, 0.52f))
                    .Add(Sph, gH, cS, V(0, 1.4f, 0.56f), V(0.34f, 0.22f, 0.3f))
                    .Mirror(Sph, gH, cP, V(0.32f, 1.14f, 0.5f), V(0.08f, 0.4f, 0.34f), V(0, -20, -10))
                    .Whip(gH, cP, V(0, 0.94f, 0.84f), V(0, -0.1f, 0.03f), V(0, 0.004f, -0.008f), 7, 0.2f, 0.11f)
                    .WhipPair(gH, gH, cG, V(0.16f, 0.9f, 0.8f), V(0.03f, -0.06f, 0.07f), V(-0.004f, 0.034f, 0.002f), 7, 0.13f, 0.05f)
                    .Eyes(gH, V(0.17f, 1.2f, 0.84f), 0.05f)
                    .Add(Sph, gT, cS, V(0, 0.8f, -0.66f), V(0.1f, 0.36f, 0.1f))
                    .Pivot(gH, V(0, 1.1f, 0.4f)).Pivot(gT, V(0, 0.98f, -0.6f))
                    .Legs4(cP, 0.27f, 0.34f, -0.44f, 0.5f, 0.24f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 071 SHIVERWISP (wacky: orbiting ice shards)
            {
                var d = Kin(b, 71, "shiverwisp", "Shiverwisp", Element.Frost, Element.Shade, Rarity.Uncommon, Pk, Temperament.Timid, Locomotion.Flyer,
                    54, 13, 9, 2.4f, 6.2f, 0.75f,
                    "The breath of a blizzard that never blew away. Shards of ice circle it like tiny moons, and it leaves frost flowers on every window it peeks through.",
                    "Lonely, shy, hums like wind in a keyhole.", 1.3f, 1, 2, A("frost_breath", "hex_flicker"), A("hail_burst"),
                    Work((WorkType.Chilling, 3)), CompanionPerk.Glow, "Companion: soft light at night.",
                    Drops(("crystal", 1, 1, 0.2f), ("gel", 1, 1, 0.6f)),
                    Pal(Hex("#d8f0ff"), Hex("#9ac8e8"), Hex("#6a8ad0"), Hex("#7ae8ff")), Pal(Hex("#f0d8ff"), Hex("#c89ae8"), Hex("#8a5ad0"), Hex("#ffb0f0")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Snow, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.95f, 0), V(0.46f, 0.5f, 0.44f))
                    .Add(Con, gT, cP, V(0, 0.62f, -0.06f), V(0.36f, 0.5f, 0.34f), V(170, 0, 0))
                    .Add(Con, gT, cS, V(0, 0.42f, -0.12f), V(0.16f, 0.3f, 0.16f), V(160, 0, 0))
                    .Add(Cry, gB, cG, V(0, 1.3f, 0), V(0.14f, 0.3f, 0.14f))
                    .Mirror(Cry, gB, cG, V(0.16f, 1.2f, 0), V(0.1f, 0.24f, 0.1f), V(0, 0, -30))
                    .Eyes(gB, V(0.1f, 0.98f, 0.19f), 0.08f, cG)
                    .Mirror(Sph, gAL, gAR, cS, V(0.3f, 0.86f, 0.04f), V(0.14f, 0.1f, 0.12f))
                    .Ring(Cry, gX, cG, V(0, 0.95f, 0), 0.6f, 5, V(0.1f, 0.26f, 0.1f), 15)
                    .Pivot(gT, V(0, 0.8f, 0)).PivotPair(gAL, gAR, V(0.26f, 0.9f, 0)).Pivot(gX, V(0, 0.95f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 072 CRYODRAKE (big)
            {
                var d = Kin(b, 72, "cryodrake", "Cryodrake", Element.Frost, Element.Stone, Rarity.Rare, Pk, Temperament.Aggressive, Locomotion.Biped,
                    105, 23, 15, 2.8f, 8.5f, 1.4f,
                    "A raptor of the high passes with a crest of living crystal. It hunts by sound, tapping the ice and listening for what moves beneath.",
                    "Cunning, relentless, respects only a good chase.", 0.5f, 1, 2, A("ice_fang", "crystal_shard"), A("roll_crush"),
                    Work((WorkType.Chilling, 2), (WorkType.Quarrying, 2)), CompanionPerk.OreSense, "Companion: +50% ore, cinder and crystal when mining.",
                    Drops(("pelt", 1, 2, 0.9f), ("crystal", 1, 1, 0.35f)),
                    Pal(Hex("#5a7a9a"), Hex("#d8e8f0"), Hex("#3a4a6a"), Hex("#9af0ff")), Pal(Hex("#3a3a3a"), Hex("#d0d0d0"), Hex("#202020"), Hex("#ff6ad0")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.86f, 0), V(0.46f, 0.5f, 0.9f), V(-10, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.78f, 0.12f), V(0.36f, 0.36f, 0.6f))
                    .Add(Cap, gB, cP, V(0, 1.1f, 0.42f), V(0.24f, 0.46f, 0.26f), V(30, 0, 0))
                    .Add(Sph, gH, cP, V(0, 1.36f, 0.62f), V(0.3f, 0.28f, 0.36f))
                    .Add(Sph, gH, cP, V(0, 1.3f, 0.86f), V(0.22f, 0.16f, 0.34f))
                    .Add(Sph, gH, cS, V(0, 1.22f, 0.82f), V(0.18f, 0.07f, 0.3f))
                    .Mirror(Con, gH, cW, V(0.07f, 1.23f, 0.92f), V(0.03f, 0.07f, 0.03f), V(180, 0, 0))
                    .Eyes(gH, V(0.12f, 1.42f, 0.74f), 0.05f, cG)
                    .Add(Cry, gH, cG, V(0, 1.6f, 0.56f), V(0.1f, 0.3f, 0.1f), V(-20, 0, 0))
                    .Add(Cry, gH, cG, V(0, 1.56f, 0.44f), V(0.09f, 0.24f, 0.09f), V(-35, 0, 0))
                    .Add(Cry, gB, cG, V(0, 1.22f, 0.06f), V(0.1f, 0.3f, 0.1f), V(-15, 0, 0))
                    .Add(Cry, gB, cG, V(0, 1.16f, -0.16f), V(0.1f, 0.28f, 0.1f), V(-25, 0, 0))
                    .Add(Cry, gB, cG, V(0, 1.08f, -0.36f), V(0.08f, 0.22f, 0.08f), V(-35, 0, 0))
                    .Mirror(Cap, gAL, gAR, cP, V(0.2f, 0.82f, 0.34f), V(0.08f, 0.26f, 0.08f), V(50, 0, 0))
                    .Mirror(Con, gAL, gAR, cW, V(0.2f, 0.7f, 0.46f), V(0.05f, 0.1f, 0.05f), V(140, 0, 0))
                    .Mirror(Sph, gFL, gFR, cP, V(0.17f, 0.62f, -0.02f), V(0.22f, 0.36f, 0.3f))
                    .Whip(gT, cP, V(0, 0.84f, -0.46f), V(0, -0.012f, -0.15f), V(0, 0.003f, 0), 7, 0.3f, 0.08f)
                    .Tip(Cry, gT, cG, V(0.1f, 0.3f, 0.1f))
                    .Pivot(gH, V(0, 1.26f, 0.5f)).PivotPair(gAL, gAR, V(0.2f, 0.92f, 0.28f)).Pivot(gT, V(0, 0.86f, -0.4f))
                    .Legs2(cP, 0.17f, 0.62f, 0.16f, cD)
                    .Done(d);
            }

            // ================================================================ VOLT
            // ---------------------------------------------------------------- 073 FUZZAP (cute)
            {
                var d = Kin(b, 73, "fuzzap", "Fuzzap", Element.Volt, Element.Normal, Rarity.Common, M | Pk, Temperament.Timid, Locomotion.Quadruped,
                    46, 10, 9, 2.2f, 5.8f, 0.6f,
                    "A lamb so fluffy it is permanently charged. Pet one and your hair stands up for an hour; pet two and you can power a lantern.",
                    "Skittish, affectionate, crackles when excited.", 2.4f, 2, 5, A("static_dart", "tackle"), A("thunder_chirp"),
                    Work((WorkType.Charging, 1), (WorkType.Foraging, 1)), CompanionPerk.Glow, "Companion: a crackling light at night.",
                    Drops(("pelt", 1, 2, 0.9f)),
                    Pal(Hex("#fff2a8"), Hex("#3a3440"), Hex("#ffb030"), Hex("#ffe84a")), Pal(Hex("#c8f0ff"), Hex("#2a3a5a"), Hex("#4ac0ff"), Hex("#aef0ff")));
                Effects(d, AuraFx.Sparks);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.36f, 0), V(0.56f, 0.5f, 0.62f))
                    .Mirror(Sph, gB, cP, V(0.18f, 0.46f, -0.1f), V(0.3f, 0.3f, 0.3f))
                    .Mirror(Sph, gB, cP, V(0.16f, 0.42f, 0.16f), V(0.28f, 0.28f, 0.28f))
                    .Add(Sph, gB, cP, V(0, 0.56f, -0.02f), V(0.36f, 0.3f, 0.4f))
                    .Add(Sph, gB, cP, V(0, 0.4f, -0.3f), V(0.3f, 0.3f, 0.24f))
                    .Add(Sph, gH, cS, V(0, 0.42f, 0.34f), V(0.3f, 0.3f, 0.28f))
                    .Add(Sph, gH, cP, V(0, 0.58f, 0.3f), V(0.26f, 0.18f, 0.22f))
                    .Mirror(Sph, gH, cS, V(0.19f, 0.44f, 0.3f), V(0.16f, 0.07f, 0.08f), V(0, 0, 20))
                    .Mirror(Con, gH, cG, V(0.1f, 0.7f, 0.3f), V(0.06f, 0.16f, 0.06f), V(0, 0, -25))
                    .Eyes(gH, V(0.08f, 0.46f, 0.46f), 0.075f, cG)
                    .Add(Sph, gT, cP, V(0, 0.42f, -0.4f), V(0.14f, 0.14f, 0.14f))
                    .Pivot(gH, V(0, 0.42f, 0.22f)).Pivot(gT, V(0, 0.42f, -0.34f))
                    .Legs4(cS, 0.14f, 0.16f, -0.18f, 0.16f, 0.08f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 074 LASHVOLT (wacky: antenna whips)
            {
                var d = Kin(b, 74, "lashvolt", "Lashvolt", Element.Volt, Element.Tide, Rarity.Uncommon, Bl | M, Temperament.Aggressive, Locomotion.Slither,
                    62, 15, 10, 2.4f, 7f, 1f,
                    "A mud eel with two long feelers that end in glowing bulbs. It dangles them like bait, then cracks them like live wires.",
                    "Sly, twitchy, holds a grudge and a charge.", 1.3f, 1, 2, A("volt_whip", "static_dart"), A("aqua_tail"),
                    Work((WorkType.Charging, 2), (WorkType.Irrigating, 1)), CompanionPerk.Glow, "Companion: a crackling light at night.",
                    Drops(("gel", 1, 2, 0.9f), ("crystal", 1, 1, 0.08f)),
                    Pal(Hex("#2a3a5a"), Hex("#e8e0a0"), Hex("#5ad0e0"), Hex("#fff04a")), Pal(Hex("#5a2a4a"), Hex("#ffd0e0"), Hex("#ff6ad0"), Hex("#aef0ff")));
                Effects(d, AuraFx.Sparks);
                new Plan()
                    .Serpent(cP, cS, 0.22f, 0.36f, 7, 0.26f)
                    .Mirror(Sph, gB, cG, V(0.15f, 0.26f, -0.26f), V(0.08f, 0.08f, 0.08f))
                    .Mirror(Sph, gB, cG, V(0.14f, 0.25f, -0.52f), V(0.07f, 0.07f, 0.07f))
                    .Mirror(Sph, gT, cG, V(0.1f, 0.22f, -1.04f), V(0.06f, 0.06f, 0.06f))
                    .Spikes(gB, cA, 0.36f, -0.1f, -0.7f, 3, 0.04f, 0.2f, 0, -35)
                    .Add(Sph, gH, cP, V(0, 0.3f, 0.3f), V(0.36f, 0.28f, 0.44f))
                    .Add(Sph, gH, cS, V(0, 0.22f, 0.38f), V(0.28f, 0.1f, 0.32f))
                    .Eyes(gH, V(0.12f, 0.36f, 0.46f), 0.05f, cG)
                    .WhipPair(gH, gH, cA, V(0.1f, 0.42f, 0.3f), V(0.03f, 0.08f, -0.02f), V(0.008f, -0.014f, -0.012f), 10, 0.05f, 0.035f)
                    .TipPair(Sph, gH, gH, cG, V(0.14f, 0.14f, 0.14f))
                    .Add(Sph, gT, cG, V(0, 0.12f, -1.74f), V(0.12f, 0.12f, 0.12f))
                    .Pivot(gH, V(0, 0.26f, 0.12f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 075 STORMHORN (big, mount)
            {
                var d = Kin(b, 75, "stormhorn", "Stormhorn", Element.Volt, Element.Stone, Rarity.Rare, Bl, Temperament.Territorial, Locomotion.Quadruped,
                    135, 21, 22, 2.2f, 7.5f, 1.45f,
                    "A plated brute whose horn draws lightning out of a clear sky. The coils along its spine hum for a full day after a storm.",
                    "Gruff, dependable, charges first and apologises never.", 0.5f, 1, 1, A("thunder_charge", "horn_toss"), A("quake_stomp"),
                    Work((WorkType.Charging, 3), (WorkType.Quarrying, 2), (WorkType.Hauling, 2)), CompanionPerk.OreSense, "Companion: +50% ore and cinder when mining. Armored mount.",
                    Drops(("pelt", 2, 3, 1f), ("ore", 1, 2, 0.6f), ("crystal", 1, 1, 0.2f)),
                    Pal(Hex("#6a7080"), Hex("#4a4e5a"), Hex("#c8a040"), Hex("#ffe84a")), Pal(Hex("#8a5a4a"), Hex("#5a3a30"), Hex("#e8e8f0"), Hex("#7ae0ff")));
                Mount(d, 12f, 7.5f, V(0, 1.16f, -0.04f));
                Effects(d, AuraFx.Sparks);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.74f, -0.05f), V(0.8f, 0.74f, 1.2f))
                    .Add(Dom, gB, cS, V(0, 0.86f, 0.26f), V(0.78f, 0.5f, 0.6f))
                    .Add(Dom, gB, cS, V(0, 0.84f, -0.36f), V(0.74f, 0.44f, 0.6f))
                    .Add(Cyl, gB, cA, V(0, 1.2f, 0.3f), V(0.07f, 0.24f, 0.07f))
                    .Add(Sph, gB, cG, V(0, 1.36f, 0.3f), V(0.14f, 0.14f, 0.14f))
                    .Add(Cyl, gB, cA, V(0, 1.12f, -0.42f), V(0.06f, 0.2f, 0.06f))
                    .Add(Sph, gB, cG, V(0, 1.26f, -0.42f), V(0.12f, 0.12f, 0.12f))
                    .Add(Sph, gH, cP, V(0, 0.6f, 0.72f), V(0.46f, 0.44f, 0.6f))
                    .Add(Sph, gH, cP, V(0, 0.5f, 0.98f), V(0.34f, 0.3f, 0.3f))
                    .Add(Con, gH, cG, V(0, 0.86f, 1.06f), V(0.14f, 0.52f, 0.14f), V(20, 0, 0))
                    .Add(Con, gH, cG, V(0, 0.84f, 0.84f), V(0.1f, 0.24f, 0.1f), V(10, 0, 0))
                    .Mirror(Con, gH, cP, V(0.2f, 0.86f, 0.56f), V(0.1f, 0.18f, 0.06f), V(0, 0, -25))
                    .Eyes(gH, V(0.2f, 0.66f, 0.9f), 0.045f)
                    .Add(Sph, gT, cA, V(0, 0.7f, -0.7f), V(0.08f, 0.3f, 0.08f), V(15, 0, 0))
                    .Pivot(gH, V(0, 0.66f, 0.5f)).Pivot(gT, V(0, 0.84f, -0.62f))
                    .Legs4(cP, 0.27f, 0.36f, -0.42f, 0.42f, 0.22f, cD)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 076 PLASMOTH (wacky: orbiting plasma orbs)
            {
                var d = Kin(b, 76, "plasmoth", "Plasmoth", Element.Volt, Element.Lumen, Rarity.Uncommon, M | Bl, Temperament.Docile, Locomotion.Flyer,
                    56, 13, 9, 2.4f, 6.2f, 0.85f,
                    "A moth that mistook a thunderhead for the moon and never came back the same. Three motes of ball lightning follow it everywhere.",
                    "Dreamy, drawn to lamps, hums in the key of static.", 1.3f, 1, 3, A("static_dart", "prism_dust"), A("thunder_chirp"),
                    Work((WorkType.Charging, 2), (WorkType.Crafting, 1)), CompanionPerk.Glow, "Companion: bright lantern light at night.",
                    Drops(("fiber", 1, 2, 0.7f), ("crystal", 1, 1, 0.1f)),
                    Pal(Hex("#e8d8ff"), Hex("#fff8e0"), Hex("#7a5ad0"), Hex("#ffe84a")), Pal(Hex("#ffd8c8"), Hex("#fff8f0"), Hex("#e05a4a"), Hex("#7af0ff")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cS, V(0, 1.0f, 0), V(0.3f, 0.3f, 0.5f))
                    .Add(Sph, gB, cP, V(0, 0.96f, -0.3f), V(0.24f, 0.24f, 0.4f))
                    .Add(Cyl, gB, cA, V(0, 0.96f, -0.3f), V(0.25f, 0.05f, 0.25f), V(90, 0, 0))
                    .Add(Sph, gH, cS, V(0, 1.04f, 0.3f), V(0.26f, 0.24f, 0.24f))
                    .Eyes(gH, V(0.09f, 1.07f, 0.39f), 0.075f, cG)
                    .Mirror(Sph, gH, cA, V(0.12f, 1.26f, 0.34f), V(0.07f, 0.3f, 0.03f), V(20, 0, -25))
                    .Mirror(Sph, gWL, gWR, cP, V(0.42f, 1.14f, 0.08f), V(0.72f, 0.04f, 0.56f), V(0, -15, 18))
                    .Mirror(Sph, gWL, gWR, cA, V(0.36f, 1.1f, -0.3f), V(0.52f, 0.04f, 0.44f), V(0, 20, 18))
                    .Mirror(Sph, gWL, gWR, cG, V(0.5f, 1.19f, 0.1f), V(0.2f, 0.03f, 0.2f), V(0, 0, 18))
                    .Mirror(Sph, gWL, gWR, cD, V(0.5f, 1.2f, 0.1f), V(0.09f, 0.03f, 0.09f), V(0, 0, 18))
                    .Ring(Sph, gX, cG, V(0, 1.0f, 0), 0.82f, 3, V(0.12f, 0.12f, 0.12f))
                    .Pivot(gH, V(0, 1.02f, 0.2f)).PivotPair(gWL, gWR, V(0.1f, 1.04f, 0)).Pivot(gX, V(0, 1.0f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 077 RAIJOLT (big, mount)
            {
                var d = Kin(b, 77, "raijolt", "Raijolt", Element.Volt, Element.Normal, Rarity.Rare, Pk | Bl, Temperament.Aggressive, Locomotion.Quadruped,
                    100, 23, 13, 3.2f, 10f, 1.3f,
                    "A storm cat striped with living lightning. You hear the thunder of its paws a heartbeat after it has already passed you.",
                    "Restless, competitive, adores a rider with nerve.", 0.5f, 1, 1, A("thunder_charge", "spark_peck"), A("shadow_pounce"),
                    Work((WorkType.Charging, 3), (WorkType.Hauling, 2)), CompanionPerk.Swiftness, "Companion: +12% run speed. The fastest mount on the island.",
                    Drops(("pelt", 2, 3, 1f), ("crystal", 1, 1, 0.2f)),
                    Pal(Hex("#f0c030"), Hex("#fff4c8"), Hex("#2a2a3a"), Hex("#7ae0ff")), Pal(Hex("#e8e8f4"), Hex("#ffffff"), Hex("#3a2a5a"), Hex("#ff6ad0")));
                Mount(d, 15f, 10f, V(0, 0.98f, -0.22f));
                Effects(d, AuraFx.Sparks);
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.74f, -0.02f), V(0.44f, 1.06f, 0.46f), V(90, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.66f, 0.3f), V(0.4f, 0.4f, 0.4f))
                    .Mirror(Sph, gB, cG, V(0.2f, 0.8f, 0.1f), V(0.05f, 0.3f, 0.1f), V(0, 0, -25))
                    .Mirror(Sph, gB, cG, V(0.205f, 0.78f, -0.18f), V(0.05f, 0.28f, 0.1f), V(0, 0, -25))
                    .Mirror(Sph, gB, cG, V(0.2f, 0.76f, -0.42f), V(0.05f, 0.22f, 0.09f), V(0, 0, -25))
                    .Spikes(gB, cA, 0.92f, 0.38f, 0.02f, 4, 0.1f, 0.28f, 0.02f, -35)
                    .Add(Sph, gH, cP, V(0, 0.98f, 0.62f), V(0.38f, 0.36f, 0.4f))
                    .Add(Sph, gH, cS, V(0, 0.9f, 0.8f), V(0.22f, 0.17f, 0.18f))
                    .Add(Sph, gH, cD, V(0, 0.94f, 0.89f), V(0.06f, 0.05f, 0.04f))
                    .Mirror(Con, gH, cP, V(0.15f, 1.2f, 0.56f), V(0.1f, 0.16f, 0.06f), V(0, 0, -15))
                    .Mirror(Con, gH, cA, V(0.22f, 0.92f, 0.62f), V(0.08f, 0.24f, 0.08f), V(0, 0, -100))
                    .Mirror(Con, gH, cW, V(0.07f, 0.82f, 0.84f), V(0.035f, 0.12f, 0.035f), V(180, 0, 0))
                    .Eyes(gH, V(0.11f, 1.04f, 0.78f), 0.05f, cG)
                    .Add(Bx, gT, cG, V(0, 0.84f, -0.66f), V(0.07f, 0.07f, 0.3f), V(-30, 0, 0))
                    .Add(Bx, gT, cG, V(0, 0.98f, -0.86f), V(0.07f, 0.07f, 0.3f), V(20, 0, 0))
                    .Add(Bx, gT, cG, V(0, 0.98f, -1.1f), V(0.07f, 0.07f, 0.3f), V(-40, 0, 0))
                    .Add(Con, gT, cG, V(0, 1.14f, -1.3f), V(0.12f, 0.22f, 0.12f), V(-50, 0, 0))
                    .Pivot(gH, V(0, 0.88f, 0.46f)).Pivot(gT, V(0, 0.78f, -0.54f))
                    .Legs4(cP, 0.16f, 0.34f, -0.4f, 0.52f, 0.12f, cD)
                    .Done(d);
            }
        }
    }
}
