using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 3: Driftkey Isles natives (#104-118) - beach, lagoon and reef kin. Lv 5-12.</summary>
        static void BuildWave3Isles(Bundle b)
        {
            const BiomeMask L = BiomeMask.Isles;

            // ---------------------------------------------------------------- 104 COCORAB
            {
                var d = K(b, 104, "cocorab", "Cocorab", Element.Stone, Element.Tide, Rarity.Common, L, Temperament.Timid, Locomotion.Quadruped, 0.6f, 'T',
                    "A beach crab that never found a shell, so it moved into a coconut. It rolls downhill inside it when frightened.",
                    "Skittish, house-proud, pinches first.", A("pebble_toss", "tackle"), "shell_slam",
                    Work((WorkType.Quarrying, 1), (WorkType.Hauling, 1)), CompanionPerk.StoneBoost, "stone", "gel",
                    Pal(Hex("#7a5636"), Hex("#a88860"), Hex("#f07a4a"), Hex("#ffe0a0")));
                d.nearWater = true;
                new Plan()
                    .Add(Sph, gB, cA, V(0, 0.2f, 0.04f), V(0.44f, 0.3f, 0.42f))
                    .Shell(cP, 0.2f, 0.58f, 0.86f, 0.58f, -0.05f)
                    .Shell(cS, 0.5f, 0.3f, 0.3f, 0.3f, -0.05f)
                    .Mirror(Cyl, gB, cA, V(0.1f, 0.38f, 0.25f), V(0.035f, 0.18f, 0.035f), V(20, 0, 0))
                    .Eyes(gB, V(0.1f, 0.49f, 0.29f), 0.085f)
                    .Mirror(Sph, gAL, gAR, cA, V(0.3f, 0.18f, 0.24f), V(0.2f, 0.16f, 0.26f))
                    .Mirror(Con, gAL, gAR, cW, V(0.3f, 0.18f, 0.4f), V(0.07f, 0.12f, 0.07f), V(90, 0, 0))
                    .PivotPair(gAL, gAR, V(0.24f, 0.2f, 0.1f))
                    .Legs4(cA, 0.2f, 0.08f, -0.14f, 0.12f, 0.06f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 105 SHELLBY
            {
                var d = K(b, 105, "shellby", "Shellby", Element.Tide, Element.Normal, Rarity.Common, L, Temperament.Docile, Locomotion.Quadruped, 0.6f, 'T',
                    "A turtle hatchling that paddles the lagoon in little convoys. Each one decorates its shell with whatever washes up.",
                    "Patient, trusting, collects sea glass.", A("bubble_burst", "tackle"), "shell_slam",
                    Work((WorkType.Irrigating, 1), (WorkType.Hauling, 1)), CompanionPerk.Mending, "gel", "stone",
                    Pal(Hex("#4a9a8a"), Hex("#a8e0a0"), Hex("#ffd070"), Hex("#d0fff0")));
                d.nearWater = true;
                new Plan()
                    .Quad(0.2f, 0.42f, 0.24f, 0.5f, 0.3f, 0.12f, 0.11f, cS, cS, null, 0.08f, 0.04f)
                    .Shell(cP, 0.18f, 0.56f, 0.62f, 0.62f, -0.04f)
                    .Shell(cA, 0.34f, 0.3f, 0.36f, 0.34f, -0.04f)
                    .Face(0.09f).Cheeks(cA)
                    .Tail(cS, 0.18f, -0.3f, 0.14f, 0.08f, 0.04f, -0.01f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 106 STARLET
            {
                var d = K(b, 106, "starlet", "Starlet", Element.Lumen, Element.Tide, Rarity.Common, L, Temperament.Curious, Locomotion.Hopper, 0.6f, 'B',
                    "A sea star that cartwheels along the tideline at dusk, glowing faintly. Sailors once steered by whole beaches of them.",
                    "Cheerful, show-off, sticks to everything.", A("glint_beam", "tackle"), "prism_dust",
                    Work((WorkType.Crafting, 1), (WorkType.Irrigating, 1)), CompanionPerk.Glow, "gel", "",
                    Pal(Hex("#ff9a5a"), Hex("#ffe0a8"), Hex("#ff6a8a"), Hex("#fff6a0")));
                d.nearWater = true;
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Fan(Con, gB, cP, V(0, 0.42f, 0), 0.2f, 5, V(0.26f, 0.38f, 0.16f))
                    .Blob(0.42f, 0.36f, 0.36f, 0.22f)
                    .Add(Sph, gB, cS, V(0, 0.42f, 0.05f), V(0.26f, 0.26f, 0.14f))
                    .Fan(Sph, gB, cG, V(0, 0.42f, 0.02f), 0.3f, 5, V(0.06f, 0.06f, 0.14f))
                    .Face(0.085f).Cheeks(cA)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 107 GULLWING
            {
                var d = K(b, 107, "gullwing", "Gullwing", Element.Gale, Element.Tide, Rarity.Common, L, Temperament.Timid, Locomotion.Flyer, 0.7f, 'F',
                    "A noisy white seabird that follows anything carrying food. A flock can strip a picnic in the time it takes to shout.",
                    "Loud, greedy, loyal to whoever fed it last.", A("seed_gust", "quick_kick"), "gust_slash",
                    Work((WorkType.Hauling, 1), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "pelt", "berry",
                    Pal(Hex("#f4f4f0"), Hex("#9aa4b0"), Hex("#ffb030"), Hex("#d8f0ff")));
                new Plan()
                    .Blob(0.7f, 0.34f, 0.32f, 0.52f)
                    .Add(Sph, gH, cP, V(0, 0.88f, 0.22f), V(0.27f, 0.26f, 0.27f)).Head(gH, 0.88f, 0.22f, 0.27f)
                    .Face(0.06f).Beak(cA, 0.09f, 0.2f)
                    .Fins(cS, V(0.14f, 0.76f, 0f), 0.6f, 0.26f, 10, 0.2f)
                    .Mirror(Sph, gWL, gWR, cD, V(0.7f, 0.86f, -0.08f), V(0.2f, 0.04f, 0.16f), V(0, 0, 10))
                    .Add(Sph, gT, cS, V(0, 0.7f, -0.34f), V(0.2f, 0.04f, 0.24f))
                    .Mirror(Sph, gB, cA, V(0.07f, 0.53f, 0.02f), V(0.05f, 0.08f, 0.1f))
                    .Pivot(gH, V(0, 0.8f, 0.14f)).Pivot(gT, V(0, 0.7f, -0.22f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 108 PALMUNK
            {
                var d = K(b, 108, "palmunk", "Palmunk", Element.Flora, Element.Normal, Rarity.Common, L, Temperament.Curious, Locomotion.Biped, 0.6f, 'F',
                    "A striped island squirrel whose tail has grown into a palm frond. It naps underneath it at noon.",
                    "Nosy, quick, hoards coconuts it can't open.", A("leaf_dart", "quick_kick"), "vine_whip",
                    Work((WorkType.Foraging, 2), (WorkType.Logging, 1)), CompanionPerk.ForageBoost, "fiber", "wood",
                    Pal(Hex("#c8894a"), Hex("#fbe8c8"), Hex("#4aa850"), Hex("#c8ff90")));
                new Plan()
                    .Biped(0.16f, 0.32f, 0.3f, 0.36f, 0.09f, foot: cD)
                    .Add(Sph, gB, cS, V(0, 0.28f, 0.08f), V(0.22f, 0.24f, 0.16f))
                    .Add(Sph, gH, cD, V(0, 0.7f, -0.02f), V(0.08f, 0.14f, 0.3f))
                    .Face(0.085f).Snout(cS, 0.15f, 0.5f).Cheeks(cS)
                    .Ears(Sph, cP, 0.1f, 0.12f, 22)
                    .Tail(cP, 0.2f, -0.14f, 0.5f, 0.12f, 0.09f, 0.07f, curl: 0.014f)
                    .Fronds(gT, cA, 6, 0.3f, 0.1f, 0.25f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 109 BUBBLEFIN
            {
                var d = K(b, 109, "bubblefin", "Bubblefin", Element.Tide, Element.Gale, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Flyer, 0.75f, 'B',
                    "A round reef fish that swims through the sea air inside a film of water, trailing a ring of bubbles it refuses to let pop.",
                    "Dreamy, gentle, counts its bubbles.", A("bubble_burst", "aqua_tail"), "wave_crash",
                    Work((WorkType.Irrigating, 2), (WorkType.Chilling, 1)), CompanionPerk.Mending, "gel", "",
                    Pal(Hex("#ffb040"), Hex("#fff0d0"), Hex("#3a7ad0"), Hex("#c8f4ff")));
                d.nearWater = true;
                Effects(d, AuraFx.Bubbles, ExtraAnim.Orbit);
                new Plan()
                    .Blob(0.7f, 0.4f, 0.48f, 0.58f)
                    .Add(Sph, gB, cS, V(0, 0.62f, 0.04f), V(0.32f, 0.3f, 0.48f))
                    .Add(Sph, gB, cA, V(0, 0.72f, -0.06f), V(0.42f, 0.5f, 0.12f))
                    .Face(0.1f, cD, 0.1f, 0.3f).Cheeks(cA)
                    .Add(Sph, gB, cA, V(0, 0.64f, 0.285f), V(0.1f, 0.06f, 0.05f))
                    .Fins(cA, V(0.18f, 0.66f, 0.02f), 0.26f, 0.2f, -20)
                    .Add(Sph, gB, cA, V(0, 0.98f, -0.04f), V(0.05f, 0.24f, 0.3f))
                    .Add(Sph, gT, cA, V(0, 0.7f, -0.42f), V(0.05f, 0.46f, 0.3f))
                    .Ring(Sph, gX, cG, V(0, 0.76f, 0), 0.52f, 3, V(0.11f, 0.11f, 0.11f))
                    .Add(Sph, gX, cG, V(0.2f, 1.14f, 0.2f), V(0.07f, 0.07f, 0.07f))
                    .Pivot(gT, V(0, 0.7f, -0.26f)).Pivot(gX, V(0, 0.76f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 110 CORALOPE
            {
                var d = K(b, 110, "coralope", "Coralope", Element.Tide, Element.Flora, Rarity.Uncommon, L, Temperament.Timid, Locomotion.Quadruped, 1f, 'F',
                    "A slender antelope that wades the shallows at low tide. Its antlers are living coral, and small fish follow it between islets.",
                    "Graceful, wary, never far from water.", A("aqua_tail", "antler_charge"), "riptide",
                    Work((WorkType.Irrigating, 2), (WorkType.Cultivating, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "pelt", "gel",
                    Pal(Hex("#e8c8a0"), Hex("#fff6e8"), Hex("#ff6a7a"), Hex("#ffd0e0")));
                d.nearWater = true;
                new Plan()
                    .Quad(0.66f, 0.36f, 0.38f, 0.74f, 0.28f, 0.5f, 0.09f, foot: cD, headUp: 0.34f, headOut: 0.02f)
                    .Add(Cap, gH, cP, V(0, 0.84f, 0.34f), V(0.16f, 0.44f, 0.16f), V(28, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.6f, 0.02f), V(0.3f, 0.26f, 0.6f))
                    .Face(0.06f).Snout(cS, 0.14f, 0.7f)
                    .Ears(Sph, cP, 0.08f, 0.16f, 55)
                    .WhipPair(gH, gH, cA, V(0.08f, 1.12f, 0.4f), V(0.03f, 0.085f, -0.01f), V(0.012f, 0, 0), 5, 0.065f, 0.045f)
                    .TipPair(Sph, gH, gH, cG, V(0.07f, 0.07f, 0.07f))
                    .Mirror(Cap, gH, cA, V(0.21f, 1.3f, 0.38f), V(0.045f, 0.18f, 0.045f), V(0, 0, -55))
                    .Mirror(Cap, gH, cA, V(0.1f, 1.4f, 0.37f), V(0.04f, 0.16f, 0.04f), V(0, 0, 30))
                    .Tail(cS, 0.76f, -0.36f, 0.14f, 0.08f, 0.06f, 0.02f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 111 SEASTRIDE
            {
                var d = K(b, 111, "seastride", "Seastride", Element.Tide, Element.Normal, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Flyer, 0.9f, 'B',
                    "An upright sea-horse that bobs just above the waves, anchoring its tail round driftwood to sleep.",
                    "Polite, old-fashioned, easily seasick on land.", A("bubble_burst", "tidal_lash"), "steam_blast",
                    Work((WorkType.Irrigating, 2), (WorkType.Crafting, 1)), CompanionPerk.Mending, "gel", "pelt",
                    Pal(Hex("#f0a03a"), Hex("#ffe6a8"), Hex("#e05a8a"), Hex("#a0f0ff")));
                d.nearWater = true;
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.72f, 0), V(0.3f, 0.5f, 0.3f), V(8, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.68f, 0.1f), V(0.2f, 0.36f, 0.16f))
                    .Add(Sph, gH, cP, V(0, 1.08f, 0.08f), V(0.28f, 0.27f, 0.3f)).Head(gH, 1.08f, 0.08f, 0.28f)
                    .Face(0.07f)
                    .Add(Cyl, gH, cP, V(0, 1.03f, 0.3f), V(0.08f, 0.22f, 0.08f), V(90, 0, 0))
                    .Add(Cyl, gH, cA, V(0, 1.03f, 0.42f), V(0.1f, 0.03f, 0.1f), V(90, 0, 0))
                    .Spikes(gH, cA, 1.2f, 0.12f, -0.06f, 3, 0.06f, 0.14f, 0.02f, -10)
                    .Add(Sph, gB, cA, V(0, 0.76f, -0.2f), V(0.04f, 0.3f, 0.2f))
                    .Fins(cA, V(0.13f, 0.84f, 0f), 0.18f, 0.12f, 0)
                    .Whip(gT, cP, V(0, 0.48f, 0f), V(0, -0.08f, -0.02f), V(0, 0.012f, 0.02f), 8, 0.2f, 0.07f, cS)
                    .Pivot(gH, V(0, 0.96f, 0.02f)).Pivot(gT, V(0, 0.5f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 112 PARROKEET
            {
                var d = K(b, 112, "parrokeet", "Parrokeet", Element.Gale, Element.Flora, Rarity.Uncommon, L, Temperament.Curious, Locomotion.Flyer, 0.7f, 'F',
                    "A gaudy island parrot that repeats the last thing it heard, usually at the worst moment. Its tail feathers are prized and it knows it.",
                    "Vain, talkative, a terrible secret-keeper.", A("seed_gust", "gust_slash"), "cyclone",
                    Work((WorkType.Foraging, 2), (WorkType.Crafting, 1), (WorkType.Hauling, 1)), CompanionPerk.ForageBoost, "pelt", "berry",
                    Pal(Hex("#e83a3a"), Hex("#ffd040"), Hex("#2a8ae0"), Hex("#7af0a0")));
                new Plan()
                    .Blob(0.72f, 0.3f, 0.4f, 0.34f)
                    .Add(Sph, gB, cS, V(0, 0.68f, 0.06f), V(0.22f, 0.3f, 0.24f))
                    .Add(Sph, gH, cP, V(0, 1.0f, 0.06f), V(0.3f, 0.28f, 0.3f)).Head(gH, 1.0f, 0.06f, 0.3f)
                    .Mirror(Sph, gH, cW, V(0.08f, 1.02f, 0.14f), V(0.13f, 0.14f, 0.08f))
                    .Face(0.065f)
                    .Add(Sph, gH, cD, V(0, 0.97f, 0.2f), V(0.12f, 0.14f, 0.15f))
                    .Add(Con, gH, cD, V(0, 0.9f, 0.26f), V(0.07f, 0.1f, 0.07f), V(180, 0, 0))
                    .Spikes(gH, cS, 1.12f, 0.1f, -0.06f, 3, 0.06f, 0.2f, 0.01f, -25)
                    .Wings(cP, cA, cS, V(0.12f, 0.8f, 0), 0.5f, 0.28f, 16)
                    .Whip(gT, cA, V(0, 0.56f, -0.14f), V(0, -0.04f, -0.08f), V(0, 0, 0), 5, 0.08f, 0.04f, cS)
                    .PivotPair(gWL, gWR, V(0.12f, 0.8f, 0)).Pivot(gH, V(0, 0.88f, 0.02f)).Pivot(gT, V(0, 0.56f, -0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 113 CLAMPEARL
            {
                var d = K(b, 113, "clampearl", "Clampearl", Element.Tide, Element.Lumen, Rarity.Uncommon, L, Temperament.Timid, Locomotion.Hopper, 0.7f, 'T',
                    "A clam whose pearl grew eyes and opinions. The shell snaps shut on anything rude, and stays shut until apologised to.",
                    "Prim, sulky, shines when praised.", A("glint_beam", "shell_slam"), "glow_pulse",
                    Work((WorkType.Crafting, 2), (WorkType.Irrigating, 1)), CompanionPerk.Glow, "gel", "crystal",
                    Pal(Hex("#b8a8d0"), Hex("#f0e8ff"), Hex("#ff9ab8"), Hex("#fff8e0")));
                d.nearWater = true;
                Effects(d, AuraFx.Glitter, ExtraAnim.Pulse);
                new Plan()
                    .Add(Dom, gB, cP, V(0, 0.2f, 0), V(0.62f, 0.38f, 0.58f), V(180, 0, 0))
                    .Add(Sph, gB, cA, V(0, 0.2f, 0), V(0.54f, 0.1f, 0.5f))
                    .Add(Dom, gH, cP, V(0, 0.36f, -0.02f), V(0.62f, 0.4f, 0.58f), V(-30, 0, 0))
                    .Add(Dom, gH, cS, V(0, 0.4f, -0.04f), V(0.4f, 0.4f, 0.38f), V(-30, 0, 0))
                    .Add(Sph, gX, cG, V(0, 0.32f, 0.04f), V(0.26f, 0.26f, 0.26f))
                    .Eyes(gX, V(0.06f, 0.35f, 0.15f), 0.06f)
                    .Pivot(gH, V(0, 0.22f, -0.28f)).Pivot(gX, V(0, 0.32f, 0.04f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 114 JELLUME
            {
                var d = K(b, 114, "jellume", "Jellume", Element.Tide, Element.Lumen, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Flyer, 0.8f, 'G',
                    "A lantern jellyfish that drifts inland on warm nights. Islanders hang their washing out by its light.",
                    "Placid, forgetful, hums off-key.", A("tidal_lash", "glow_pulse"), "aurora_veil",
                    Work((WorkType.Irrigating, 1), (WorkType.Charging, 1)), CompanionPerk.Glow, "gel", "",
                    Pal(Hex("#7ab8f0"), Hex("#d8f0ff"), Hex("#ff8ad0"), Hex("#b0fff0")));
                d.nearWater = true;
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Add(Dom, gB, cP, V(0, 0.8f, 0), V(0.6f, 0.62f, 0.6f))
                    .Add(Sph, gB, cS, V(0, 0.8f, 0), V(0.56f, 0.14f, 0.56f))
                    .Eyes(gB, V(0.11f, 0.9f, 0.245f), 0.08f)
                    .Mirror(Sph, gB, cA, V(0.19f, 0.86f, 0.2f), V(0.08f, 0.05f, 0.05f))
                    .Ring(Sph, gB, cG, V(0, 0.83f, 0), 0.28f, 6, V(0.08f, 0.08f, 0.08f), 0, 30)
                    .Add(Sph, gB, cG, V(0, 1.1f, 0), V(0.16f, 0.08f, 0.16f))
                    .Ring(Cap, gT, cS, V(0, 0.58f, 0), 0.16f, 5, V(0.06f, 0.44f, 0.06f), 8)
                    .WhipPair(gT, gT, cA, V(0.08f, 0.74f, 0.02f), V(0.01f, -0.09f, 0), V(0.004f, 0, -0.004f), 8, 0.05f, 0.03f, cG)
                    .Pivot(gT, V(0, 0.78f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 115 KRAKLING
            {
                var d = K(b, 115, "krakling", "Krakling", Element.Tide, Element.Shade, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Hopper, 0.85f, 'G',
                    "The bad-tempered young of something much larger that lives past the reef. It practises sinking toy boats.",
                    "Grumpy, ambitious, holds grudges with all eight arms.", A("tidal_lash", "hex_flicker"), "gloom_pulse",
                    Work((WorkType.Hauling, 2), (WorkType.Irrigating, 1)), CompanionPerk.OreSense, "gel", "pelt",
                    Pal(Hex("#7a3a6a"), Hex("#d8a0c0"), Hex("#ff7a4a"), Hex("#ffe070")));
                d.nearWater = true;
                var p = new Plan()
                    .Add(Sph, gB, cP, V(0, 0.52f, -0.08f), V(0.56f, 0.72f, 0.6f), V(-15, 0, 0))
                    .Blob(0.34f, 0.5f, 0.4f, 0.48f)
                    .Face(0.1f, cG, 0.12f, 0.26f).Brows(cD, 0.12f, 0.26f)
                    .Spikes(gB, cA, 0.84f, 0.02f, -0.3f, 3, 0.08f, 0.16f, 0.03f, -20);
                for (int i = 0; i < 6; i++)
                {
                    float a = (30f + i * 60f) * Mathf.Deg2Rad;
                    p.Whip(gB, cP, V(Mathf.Sin(a) * 0.2f, 0.14f, Mathf.Cos(a) * 0.2f), V(Mathf.Sin(a) * 0.09f, -0.02f, Mathf.Cos(a) * 0.09f), V(0, 0.006f, 0), 5, 0.13f, 0.06f, cS);
                }
                p.WhipPair(gAL, gAR, cP, V(0.24f, 0.36f, 0.06f), V(0.07f, 0.06f, 0.04f), V(0, -0.012f, 0.004f), 8, 0.1f, 0.06f, cS)
                    .TipPair(Sph, gAL, gAR, cA, V(0.16f, 0.1f, 0.16f))
                    .PivotPair(gAL, gAR, V(0.22f, 0.36f, 0.04f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 116 TORTIDE (surf mount)
            {
                var d = K(b, 116, "tortide", "Tortide", Element.Tide, Element.Stone, Rarity.Rare, L, Temperament.Docile, Locomotion.Quadruped, 1.5f, 'T',
                    "A sea turtle old enough that a palm has taken root on its shell. It ferries smaller kin between the Driftkeys and has never once hurried.",
                    "Ancient, kindly, tells very long stories.", A("shell_slam", "wave_crash"), "quake_stomp",
                    Work((WorkType.Hauling, 3), (WorkType.Irrigating, 2)), CompanionPerk.Mending, "gel", "stone",
                    Pal(Hex("#6a7a5a"), Hex("#8ac0a0"), Hex("#4aa850"), Hex("#d0ffe0")));
                d.nearWater = true;
                Mount(d, 9f, 6f, V(0, 1.2f, 0.12f));
                new Plan()
                    .Quad(0.62f, 1.0f, 0.5f, 1.3f, 0.46f, 0.4f, 0.26f, cS, cS, cD, 0.12f, 0.22f)
                    .Add(Cap, gH, cS, V(0, 0.68f, 0.72f), V(0.3f, 0.5f, 0.3f), V(80, 0, 0))
                    .Shell(cP, 0.58f, 1.24f, 1.1f, 1.5f, -0.04f)
                    .Shell(cA, 0.9f, 0.76f, 0.5f, 0.96f, -0.04f)
                    .Face(0.07f).Snout(cS, 0.26f, 0.5f, false)
                    .Add(Cyl, gB, cD, V(0.2f, 1.38f, -0.34f), V(0.07f, 0.5f, 0.07f), V(0, 0, -8))
                    .Fronds(gB, cA, 6, 0.42f, 0.13f, 0.15f, V(0.24f, 1.62f, -0.34f))
                    .Add(Sph, gB, cD, V(0.26f, 1.58f, -0.3f), V(0.1f, 0.1f, 0.1f))
                    .Tail(cS, 0.56f, -0.7f, 0.2f, 0.14f, 0.06f, -0.02f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 117 WAVECREST (surf mount)
            {
                var d = K(b, 117, "wavecrest", "Wavecrest", Element.Tide, Element.Gale, Rarity.Rare, L, Temperament.Curious, Locomotion.Flyer, 1.3f, 'F',
                    "A dolphin that leaps so high it stopped coming down. It races boats for fun and lets the slow ones win now and then.",
                    "Playful, clever, loves an audience.", A("aqua_tail", "riptide"), "sky_dive",
                    Work((WorkType.Irrigating, 3), (WorkType.Hauling, 2)), CompanionPerk.Swiftness, "gel", "pelt",
                    Pal(Hex("#4a8ad0"), Hex("#eaf6ff"), Hex("#2a5aa0"), Hex("#a0f0ff")));
                d.nearWater = true;
                Mount(d, 12.5f, 8f, V(0, 1.08f, 0.08f));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.8f, 0), V(0.5f, 0.5f, 1.3f))
                    .Add(Sph, gB, cS, V(0, 0.72f, 0.1f), V(0.42f, 0.38f, 1.0f))
                    .Add(Sph, gH, cP, V(0, 0.84f, 0.54f), V(0.44f, 0.42f, 0.5f)).Head(gH, 0.84f, 0.56f, 0.44f)
                    .Face(0.065f)
                    .Add(Sph, gH, cP, V(0, 0.77f, 0.86f), V(0.18f, 0.12f, 0.34f))
                    .Add(Sph, gH, cS, V(0, 0.73f, 0.84f), V(0.16f, 0.08f, 0.3f))
                    .Add(Con, gB, cA, V(0, 1.14f, -0.1f), V(0.08f, 0.36f, 0.3f), V(-25, 0, 0))
                    .Fins(cA, V(0.2f, 0.7f, 0.2f), 0.4f, 0.22f, -25, 0.3f)
                    .Whip(gT, cP, V(0, 0.8f, -0.6f), V(0, 0, -0.1f), V(0, 0.004f, 0), 4, 0.26f, 0.12f)
                    .Mirror(Sph, gT, cA, V(0.16f, 0.84f, -1.04f), V(0.36f, 0.05f, 0.2f), V(0, -25, 0))
                    .Pivot(gH, V(0, 0.8f, 0.4f)).Pivot(gT, V(0, 0.8f, -0.56f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 118 REEFWARDEN (epic)
            {
                var d = K(b, 118, "reefwarden", "Reefwarden", Element.Tide, Element.Stone, Rarity.Epic, L, Temperament.Territorial, Locomotion.Biped, 1.7f, 'T',
                    "A walking piece of the reef itself, crowned in coral and studded with pearls. It rises when the Driftkeys are threatened and sinks back when they are not.",
                    "Slow to anger, impossible to stop, gentle with hatchlings.", A("rock_jaw", "wave_crash"), "quake_stomp",
                    Work((WorkType.Quarrying, 3), (WorkType.Irrigating, 3), (WorkType.Hauling, 2)), CompanionPerk.StoneBoost, "stone", "crystal",
                    Pal(Hex("#5a7a8a"), Hex("#a8c8c0"), Hex("#ff6a7a"), Hex("#b0fff0")));
                d.nearWater = true;
                Effects(d, AuraFx.Bubbles);
                new Plan()
                    .Biped(0.6f, 0.9f, 0.7f, 0.42f, 0.28f, cP, cP, cD, false)
                    .Add(Rok, gB, cP, V(0, 1.0f, -0.02f), V(1.04f, 0.72f, 0.82f))
                    .Add(Sph, gB, cS, V(0, 0.86f, 0.24f), V(0.5f, 0.44f, 0.3f))
                    .Face(0.07f, cG).Brows(cD)
                    .Mirror(Cap, gAL, gAR, cP, V(0.64f, 0.88f, 0.04f), V(0.26f, 0.6f, 0.26f), V(0, 0, -14))
                    .Mirror(Rok, gAL, gAR, cS, V(0.74f, 0.5f, 0.08f), V(0.46f, 0.42f, 0.46f))
                    .PivotPair(gAL, gAR, V(0.52f, 1.14f, 0))
                    .Mirror(Cap, gB, cA, V(0.38f, 1.52f, -0.1f), V(0.1f, 0.46f, 0.1f), V(0, 0, -20))
                    .Mirror(Cap, gB, cA, V(0.56f, 1.6f, -0.1f), V(0.07f, 0.26f, 0.07f), V(0, 0, -62))
                    .Mirror(Cap, gB, cA, V(0.26f, 1.66f, -0.16f), V(0.07f, 0.26f, 0.07f), V(0, 0, 24))
                    .Add(Cap, gB, cA, V(0, 1.5f, -0.3f), V(0.1f, 0.5f, 0.1f), V(-16, 0, 0))
                    .Ring(Sph, gB, cG, V(0, 1.2f, -0.02f), 0.5f, 6, V(0.1f, 0.1f, 0.1f), 0, 30)
                    .Done(d);
            }
        }
    }
}
