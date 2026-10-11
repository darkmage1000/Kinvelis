using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 3: Mirefen natives (#119-133) - mangrove, mud and marsh-light kin. Lv 12-20.</summary>
        static void BuildWave3Swamp(Bundle b)
        {
            const BiomeMask L = BiomeMask.Swamp;

            // ---------------------------------------------------------------- 119 MUDPUP
            {
                var d = K(b, 119, "mudpup", "Mudpup", Element.Stone, Element.Tide, Rarity.Common, L, Temperament.Curious, Locomotion.Quadruped, 0.65f, 'B',
                    "A marsh puppy that has never once been clean. It greets everyone by shaking itself dry beside them.",
                    "Joyful, sloppy, fetches things nobody threw.", A("mud_shot", "tackle"), "sand_kick",
                    Work((WorkType.Quarrying, 1), (WorkType.Hauling, 1), (WorkType.Irrigating, 1)), CompanionPerk.StoneBoost, "pelt", "stone",
                    Pal(Hex("#c8a070"), Hex("#6a4a34"), Hex("#ff8a9a"), Hex("#ffe0a0")));
                d.nearWater = true;
                new Plan()
                    .Quad(0.3f, 0.34f, 0.32f, 0.5f, 0.34f, 0.2f, 0.11f, foot: cS, headUp: 0.16f)
                    .Add(Sph, gB, cS, V(0, 0.4f, -0.06f), V(0.3f, 0.16f, 0.38f))
                    .Add(Sph, gH, cS, V(0.07f, 0.61f, 0.3f), V(0.16f, 0.08f, 0.16f))
                    .Face(0.085f).Snout(cW, 0.16f, 0.6f)
                    .Add(Sph, gH, cA, V(0, 0.36f, 0.53f), V(0.06f, 0.03f, 0.1f))
                    .Mirror(Sph, gH, cS, V(0.18f, 0.47f, 0.28f), V(0.1f, 0.24f, 0.07f), V(0, 0, -18))
                    .Tail(cP, 0.36f, -0.24f, 0.18f, 0.07f, 0.04f, 0.06f, cS, 0.08f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 120 TADPOLL
            {
                var d = K(b, 120, "tadpoll", "Tadpoll", Element.Tide, Element.Normal, Rarity.Common, L, Temperament.Timid, Locomotion.Hopper, 0.5f, 'F',
                    "All head, all tail, and two feet it hasn't worked out yet. Shoals of them wriggle across the mudflats after rain.",
                    "Wide-eyed, wobbly, follows the crowd.", A("bubble_burst", "tackle"), "aqua_tail",
                    Work((WorkType.Irrigating, 1)), CompanionPerk.Swiftness, "gel", "",
                    Pal(Hex("#4a6a7a"), Hex("#b8d8d0"), Hex("#ff9ab0"), Hex("#c8f4ff")));
                d.nearWater = true;
                new Plan()
                    .Blob(0.24f, 0.44f, 0.4f, 0.46f)
                    .Add(Sph, gB, cS, V(0, 0.18f, 0.06f), V(0.34f, 0.26f, 0.36f))
                    .Face(0.11f, cD, 0.16f, 0.27f).Cheeks(cA)
                    .Add(Sph, gB, cD, V(0, 0.2f, 0.225f), V(0.1f, 0.025f, 0.03f))
                    .Whip(gT, cP, V(0, 0.22f, -0.2f), V(0, 0.01f, -0.08f), V(0, 0, 0), 4, 0.16f, 0.06f)
                    .Add(Sph, gT, cS, V(0, 0.26f, -0.42f), V(0.03f, 0.26f, 0.34f))
                    .Mirror(Sph, gFL, gFR, cP, V(0.14f, 0.04f, 0.04f), V(0.12f, 0.07f, 0.16f))
                    .Pivot(gT, V(0, 0.22f, -0.18f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 121 SKEETER
            {
                var d = K(b, 121, "skeeter", "Skeeter", Element.Gale, Element.Shade, Rarity.Common, L, Temperament.Aggressive, Locomotion.Flyer, 0.6f, 'G',
                    "A mosquito the size of a cat with a needle to match. You hear the whine long before you find out where it is.",
                    "Persistent, shameless, loves ankles.", A("gust_slash", "venom_spit"), "sting_tail",
                    Work((WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "fiber", "gel",
                    Pal(Hex("#6a5a4a"), Hex("#c8b89a"), Hex("#e83a3a"), Hex("#ffd0a0")));
                new Plan()
                    .Blob(0.8f, 0.24f, 0.24f, 0.3f)
                    .Add(Sph, gH, cP, V(0, 0.84f, 0.2f), V(0.2f, 0.2f, 0.2f)).Head(gH, 0.84f, 0.2f, 0.2f)
                    .Face(0.085f, cA, 0.05f, 0.34f)
                    .Add(Con, gH, cD, V(0, 0.78f, 0.42f), V(0.03f, 0.34f, 0.03f), V(100, 0, 0))
                    .Whip(gT, cS, V(0, 0.78f, -0.16f), V(0, -0.03f, -0.08f), V(0, 0, 0), 5, 0.2f, 0.08f, cA)
                    .Fins(cW, V(0.06f, 0.92f, 0f), 0.5f, 0.16f, 25, 0.2f)
                    .Mirror(Cyl, gB, cD, V(0.12f, 0.62f, 0.06f), V(0.02f, 0.3f, 0.02f), V(0, 0, -25))
                    .Mirror(Cyl, gB, cD, V(0.12f, 0.62f, -0.06f), V(0.02f, 0.3f, 0.02f), V(20, 0, -25))
                    .Pivot(gH, V(0, 0.82f, 0.12f)).Pivot(gT, V(0, 0.78f, -0.12f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 122 LOTUSKIP
            {
                var d = K(b, 122, "lotuskip", "Lotuskip", Element.Flora, Element.Tide, Rarity.Common, L, Temperament.Docile, Locomotion.Hopper, 0.6f, 'B',
                    "A pond frog with a lotus rooted on its back. The flower opens when the frog is happy, so everyone always knows.",
                    "Calm, honest, a little smug about the flower.", A("leaf_dart", "tongue_lash"), "pollen_burst",
                    Work((WorkType.Cultivating, 2), (WorkType.Irrigating, 1)), CompanionPerk.ForageBoost, "fiber", "gel",
                    Pal(Hex("#5aa85a"), Hex("#d0f0b0"), Hex("#ff8ac0"), Hex("#fff0a0")));
                d.nearWater = true;
                Effects(d, AuraFx.Petals);
                new Plan()
                    .Blob(0.24f, 0.5f, 0.38f, 0.46f)
                    .Add(Sph, gB, cS, V(0, 0.18f, 0.08f), V(0.38f, 0.24f, 0.34f))
                    .Mirror(Sph, gB, cP, V(0.14f, 0.42f, 0.12f), V(0.16f, 0.16f, 0.16f))
                    .Eyes(gB, V(0.14f, 0.44f, 0.19f), 0.09f)
                    .Add(Sph, gB, cD, V(0, 0.22f, 0.225f), V(0.2f, 0.02f, 0.03f))
                    .Mirror(Sph, gFL, gFR, cP, V(0.22f, 0.1f, -0.08f), V(0.16f, 0.2f, 0.26f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.24f, 0.03f, 0.08f), V(0.14f, 0.05f, 0.24f))
                    .Add(Cyl, gB, cP, V(0, 0.42f, -0.12f), V(0.44f, 0.03f, 0.44f))
                    .Fronds(gB, cA, 6, 0.2f, 0.11f, 1.1f, V(0, 0.43f, -0.12f))
                    .Fronds(gB, cW, 5, 0.14f, 0.08f, 2.4f, V(0, 0.44f, -0.12f), 30)
                    .Add(Sph, gB, cG, V(0, 0.5f, -0.12f), V(0.08f, 0.08f, 0.08f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 123 FENFLY
            {
                var d = K(b, 123, "fenfly", "Fenfly", Element.Lumen, Element.Gale, Rarity.Common, L, Temperament.Timid, Locomotion.Flyer, 0.5f, 'F',
                    "A marsh firefly with a lantern for a tail. Travellers who follow one usually end up somewhere dry. Usually.",
                    "Shy, flickery, blinks when nervous.", A("glint_beam", "seed_gust"), "prism_dust",
                    Work((WorkType.Charging, 1), (WorkType.Foraging, 1)), CompanionPerk.Glow, "gel", "",
                    Pal(Hex("#4a3a30"), Hex("#8a6a4a"), Hex("#ffb040"), Hex("#d8ff70")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Blob(0.76f, 0.22f, 0.22f, 0.26f)
                    .Add(Sph, gH, cP, V(0, 0.8f, 0.16f), V(0.2f, 0.2f, 0.2f)).Head(gH, 0.8f, 0.16f, 0.2f)
                    .Face(0.075f, cG, 0.05f, 0.3f)
                    .WhipPair(gH, gH, cS, V(0.04f, 0.9f, 0.2f), V(0.02f, 0.05f, 0.03f), V(0.004f, -0.01f, 0.006f), 4, 0.025f, 0.02f)
                    .Add(Sph, gT, cG, V(0, 0.7f, -0.2f), V(0.28f, 0.28f, 0.34f))
                    .Add(Sph, gT, cA, V(0, 0.72f, -0.08f), V(0.24f, 0.24f, 0.14f))
                    .Fins(cW, V(0.06f, 0.86f, -0.02f), 0.4f, 0.16f, 25, 0.2f)
                    .Pivot(gH, V(0, 0.78f, 0.1f)).Pivot(gT, V(0, 0.74f, -0.08f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 124 BOGSNAP
            {
                var d = K(b, 124, "bogsnap", "Bogsnap", Element.Flora, Element.Shade, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Hopper, 0.9f, 'G',
                    "A flytrap that pulled up its roots to go and find lunch. It holds perfectly still among the reeds with its mouth open, and it is very patient.",
                    "Hungry, sneaky, snaps at shadows.", A("vine_whip", "acid_bite"), "pollen_burst",
                    Work((WorkType.Cultivating, 2), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "fiber", "berry",
                    Pal(Hex("#6aa83a"), Hex("#3a7a3a"), Hex("#e83a6a"), Hex("#f0ff70")));
                new Plan()
                    .Add(Sph, gB, cS, V(0, 0.16f, 0), V(0.5f, 0.3f, 0.5f))
                    .Fronds(gB, cS, 5, 0.36f, 0.16f, 0.1f, V(0, 0.12f, 0), 36)
                    .Whip(gB, cS, V(0, 0.28f, 0), V(0, 0.09f, 0.01f), V(0, 0, 0.004f), 5, 0.14f, 0.11f)
                    .Add(Dom, gH, cP, V(0, 0.8f, 0.1f), V(0.5f, 0.3f, 0.6f), V(180, 0, 0))
                    .Add(Sph, gH, cA, V(0, 0.79f, 0.1f), V(0.44f, 0.08f, 0.54f))
                    .Add(Dom, gH, cP, V(0, 0.9f, 0.04f), V(0.5f, 0.3f, 0.6f), V(-28, 0, 0))
                    .Add(Dom, gH, cA, V(0, 0.89f, 0.045f), V(0.44f, 0.22f, 0.54f), V(-28, 0, 0))
                    .Mirror(Con, gH, cW, V(0.2f, 0.84f, 0.2f), V(0.05f, 0.1f, 0.05f))
                    .Mirror(Con, gH, cW, V(0.12f, 0.84f, 0.33f), V(0.05f, 0.1f, 0.05f))
                    .Add(Con, gH, cW, V(0, 0.84f, 0.37f), V(0.05f, 0.1f, 0.05f))
                    .Mirror(Con, gH, cW, V(0.1f, 0.98f, 0.3f), V(0.05f, 0.1f, 0.05f), V(180, 0, 0))
                    .Eyes(gH, V(0.12f, 1.07f, 0.14f), 0.07f, cG)
                    .Pivot(gH, V(0, 0.8f, -0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 125 REEDSTALK
            {
                var d = K(b, 125, "reedstalk", "Reedstalk", Element.Gale, Element.Tide, Rarity.Uncommon, L, Temperament.Timid, Locomotion.Biped, 1.1f, 'F',
                    "A grey heron on stilt legs that stands so still the reeds grow round it. It moves exactly once, and by then the fish is gone.",
                    "Aloof, precise, judges your posture.", A("gust_slash", "bubble_burst"), "sky_dive",
                    Work((WorkType.Irrigating, 2), (WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "pelt", "gel",
                    Pal(Hex("#8a9aa8"), Hex("#e8eef0"), Hex("#f0b040"), Hex("#c8f0ff")));
                d.nearWater = true;
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.84f, -0.04f), V(0.36f, 0.34f, 0.56f), V(-12, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.8f, 0.08f), V(0.28f, 0.26f, 0.36f))
                    .Legs2(cA, 0.08f, 0.7f, 0.04f, cA)
                    .Whip(gH, cS, V(0, 0.94f, 0.2f), V(0, 0.09f, 0.04f), V(0, 0.01f, -0.012f), 5, 0.11f, 0.08f)
                    .TipHead(gH, cS, 0.2f, 0.05f).Beak(cA, 0.06f, 0.36f)
                    .Whip(gH, cD, V(0, 1.5f, 0.2f), V(0, 0.02f, -0.07f), V(0, -0.01f, 0), 3, 0.04f, 0.02f)
                    .Mirror(Sph, gWL, gWR, cD, V(0.17f, 0.86f, -0.08f), V(0.06f, 0.26f, 0.5f), V(-12, 0, 0))
                    .Add(Sph, gT, cD, V(0, 0.8f, -0.36f), V(0.16f, 0.05f, 0.24f))
                    .PivotPair(gWL, gWR, V(0.16f, 0.94f, -0.02f)).Pivot(gH, V(0, 0.94f, 0.18f)).Pivot(gT, V(0, 0.82f, -0.26f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 126 LEECHLASH
            {
                var d = K(b, 126, "leechlash", "Leechlash", Element.Shade, Element.Tide, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Slither, 0.9f, 'G',
                    "A fat marsh leech with two feelers it cracks like whips. It doesn't want much. Just a little. Just a taste.",
                    "Clingy, wheedling, never lets go first.", A("tidal_lash", "acid_bite"), "coil_strike",
                    Work((WorkType.Irrigating, 1), (WorkType.Hauling, 1)), CompanionPerk.Mending, "gel", "",
                    Pal(Hex("#3a3040"), Hex("#6a4a5a"), Hex("#e85a6a"), Hex("#c0ff80")));
                d.nearWater = true;
                new Plan()
                    .Serpent(cP, cS, 0.22f, 0.36f, 7, 0.24f)
                    .Add(Sph, gH, cP, V(0, 0.28f, 0.22f), V(0.38f, 0.34f, 0.36f)).Head(gH, 0.28f, 0.22f, 0.38f)
                    .Add(Cyl, gH, cA, V(0, 0.24f, 0.4f), V(0.22f, 0.04f, 0.22f), V(90, 0, 0))
                    .Add(Sph, gH, cD, V(0, 0.24f, 0.42f), V(0.13f, 0.13f, 0.03f))
                    .Fan(Con, gH, cW, V(0, 0.24f, 0.43f), 0.085f, 6, V(0.03f, 0.05f, 0.03f))
                    .Face(0.06f, cG, 0.28f, 0.3f)
                    .WhipPair(gH, gH, cA, V(0.12f, 0.4f, 0.26f), V(0.05f, 0.06f, 0.04f), V(0.004f, -0.012f, 0.004f), 8, 0.05f, 0.025f)
                    .Pivot(gH, V(0, 0.24f, 0.08f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 127 RUSHCAT
            {
                var d = K(b, 127, "rushcat", "Rushcat", Element.Flora, Element.Normal, Rarity.Uncommon, L, Temperament.Curious, Locomotion.Quadruped, 0.85f, 'F',
                    "A marsh cat with reeds growing along its spine and a cattail for a tail. It hunts by pretending to be scenery.",
                    "Sly, playful, pounces on anything that wobbles.", A("quick_kick", "needle_volley"), "shadow_pounce",
                    Work((WorkType.Foraging, 2), (WorkType.Cultivating, 1), (WorkType.Logging, 1)), CompanionPerk.Swiftness, "pelt", "fiber",
                    Pal(Hex("#b89a5a"), Hex("#f0e4c0"), Hex("#5a9a4a"), Hex("#e0ff90")));
                new Plan()
                    .Quad(0.46f, 0.3f, 0.32f, 0.66f, 0.32f, 0.34f, 0.08f, foot: cS, headUp: 0.2f)
                    .Add(Sph, gB, cS, V(0, 0.4f, 0.04f), V(0.24f, 0.2f, 0.5f))
                    .Face(0.07f, cG).Snout(cS, 0.14f, 0.4f).Cheeks(cS)
                    .Ears(Con, cP, 0.12f, 0.18f, 14)
                    .Spikes(gB, cA, 0.58f, 0.2f, -0.24f, 4, 0.05f, 0.2f, 0.01f, -30)
                    .Tail(cP, 0.52f, -0.32f, 0.5f, 0.07f, 0.05f, 0.08f, null, 0f, 0.01f)
                    .Tip(Cap, gT, cD, V(0.11f, 0.28f, 0.11f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 128 MURKFIN
            {
                var d = K(b, 128, "murkfin", "Murkfin", Element.Tide, Element.Shade, Rarity.Uncommon, L, Temperament.Territorial, Locomotion.Quadruped, 0.95f, 'T',
                    "A catfish that got tired of its pond drying up and grew legs out of spite. Its whiskers taste the mud for miles around.",
                    "Gruff, stubborn, a secret softie.", A("mud_shot", "aqua_tail"), "gloom_pulse",
                    Work((WorkType.Irrigating, 2), (WorkType.Quarrying, 1)), CompanionPerk.OreSense, "gel", "stone",
                    Pal(Hex("#4a5a50"), Hex("#b8c0a0"), Hex("#8a6aa0"), Hex("#d0ffb0")));
                d.nearWater = true;
                new Plan()
                    .Quad(0.3f, 0.5f, 0.34f, 0.8f, 0.5f, 0.16f, 0.12f, foot: cS, headUp: 0f, headOut: -0.06f)
                    .Add(Sph, gH, cS, V(0, 0.22f, 0.54f), V(0.44f, 0.16f, 0.3f))
                    .Face(0.065f, cG, 0.22f, 0.32f)
                    .WhipPair(gH, gH, cA, V(0.2f, 0.26f, 0.6f), V(0.07f, 0.01f, 0.04f), V(0, -0.006f, -0.01f), 6, 0.04f, 0.02f)
                    .WhipPair(gH, gH, cA, V(0.1f, 0.2f, 0.66f), V(0.03f, -0.03f, 0.05f), V(0, -0.004f, -0.006f), 4, 0.03f, 0.02f)
                    .Add(Sph, gB, cA, V(0, 0.52f, -0.06f), V(0.04f, 0.26f, 0.44f))
                    .Whip(gT, cP, V(0, 0.3f, -0.4f), V(0, 0, -0.1f), V(0, 0.002f, 0), 4, 0.26f, 0.1f)
                    .Add(Sph, gT, cA, V(0, 0.32f, -0.86f), V(0.04f, 0.4f, 0.3f))
                    .Pivot(gT, V(0, 0.3f, -0.36f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 129 SPORETOAD
            {
                var d = K(b, 129, "sporetoad", "Sporetoad", Element.Shade, Element.Flora, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Hopper, 0.9f, 'T',
                    "A heavy toad that sits so long in one spot that toadstools grow on it. Squeeze one and the whole clearing goes quiet and sleepy.",
                    "Drowsy, unbothered, burps spores.", A("toxic_cloud", "tongue_lash"), "spore_puff",
                    Work((WorkType.Cultivating, 2), (WorkType.Irrigating, 1)), CompanionPerk.Mending, "gel", "fiber",
                    Pal(Hex("#5a6a3a"), Hex("#c0c890"), Hex("#b05ac0"), Hex("#d0ff80")));
                d.nearWater = true;
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Blob(0.3f, 0.66f, 0.5f, 0.62f)
                    .Add(Sph, gB, cS, V(0, 0.22f, 0.1f), V(0.52f, 0.34f, 0.48f))
                    .Mirror(Sph, gB, cP, V(0.17f, 0.52f, 0.16f), V(0.18f, 0.18f, 0.18f))
                    .Eyes(gB, V(0.17f, 0.54f, 0.24f), 0.09f, cG)
                    .Add(Sph, gB, cD, V(0, 0.28f, 0.305f), V(0.3f, 0.02f, 0.03f))
                    .Mirror(Sph, gFL, gFR, cP, V(0.3f, 0.14f, -0.1f), V(0.22f, 0.28f, 0.34f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.3f, 0.04f, 0.14f), V(0.18f, 0.07f, 0.3f))
                    .Add(Cyl, gB, cW, V(0, 0.62f, -0.12f), V(0.08f, 0.22f, 0.08f)).Add(Dom, gB, cA, V(0, 0.7f, -0.12f), V(0.34f, 0.3f, 0.34f))
                    .Add(Cyl, gB, cW, V(0.2f, 0.52f, -0.06f), V(0.05f, 0.16f, 0.05f), V(0, 0, -20)).Add(Dom, gB, cA, V(0.23f, 0.58f, -0.06f), V(0.2f, 0.18f, 0.2f), V(0, 0, -20))
                    .Add(Cyl, gB, cW, V(-0.18f, 0.5f, -0.2f), V(0.05f, 0.16f, 0.05f), V(0, 0, 20)).Add(Dom, gB, cA, V(-0.21f, 0.56f, -0.2f), V(0.18f, 0.16f, 0.18f), V(0, 0, 20))
                    .Add(Sph, gB, cG, V(0.07f, 0.82f, -0.06f), V(0.06f, 0.04f, 0.06f)).Add(Sph, gB, cG, V(-0.08f, 0.8f, -0.16f), V(0.05f, 0.04f, 0.05f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 130 MIREJAW (surf mount)
            {
                var d = K(b, 130, "mirejaw", "Mirejaw", Element.Tide, Element.Stone, Rarity.Rare, L, Temperament.Aggressive, Locomotion.Quadruped, 1.5f, 'G',
                    "The log that isn't a log. A Mirejaw can lie in the channel for a week with only its eyes showing, and cross the whole fen in one lunge.",
                    "Cold, patient, oddly fond of its rider.", A("rock_jaw", "aqua_tail"), "riptide",
                    Work((WorkType.Hauling, 3), (WorkType.Irrigating, 2), (WorkType.Logging, 1)), CompanionPerk.LumberBoost, "pelt", "stone",
                    Pal(Hex("#4a6a40"), Hex("#c8d0a0"), Hex("#2a4a30"), Hex("#ffe060")));
                d.nearWater = true;
                Mount(d, 10.5f, 6.5f, V(0, 0.78f, 0f));
                new Plan()
                    .Quad(0.42f, 0.62f, 0.4f, 1.4f, 0.46f, 0.26f, 0.2f, foot: cD, headUp: 0.02f, headOut: 0.04f)
                    .Add(Sph, gB, cS, V(0, 0.34f, 0), V(0.5f, 0.26f, 1.2f))
                    .Add(Sph, gH, cP, V(0, 0.47f, 1.2f), V(0.34f, 0.14f, 0.72f))
                    .Add(Sph, gH, cS, V(0, 0.36f, 1.16f), V(0.3f, 0.1f, 0.66f))
                    .Mirror(Sph, gH, cP, V(0.08f, 0.53f, 1.5f), V(0.08f, 0.06f, 0.08f))
                    .Spikes(gH, cW, 0.38f, 0.98f, 1.44f, 4, 0.04f, 0.09f, 0, 0, 0.14f, 0)
                    .Mirror(Sph, gH, cP, V(0.15f, 0.62f, 0.86f), V(0.14f, 0.12f, 0.14f))
                    .Eyes(gH, V(0.15f, 0.64f, 0.92f), 0.06f, cG)
                    .Spikes(gB, cA, 0.58f, 0.5f, -0.6f, 6, 0.1f, 0.16f, 0.02f, -15, 0.14f, -12)
                    .Whip(gT, cP, V(0, 0.42f, -0.66f), V(0, -0.02f, -0.14f), V(0, 0.003f, 0), 7, 0.36f, 0.08f)
                    .Spikes(gT, cA, 0.5f, -0.72f, -1.1f, 3, 0.07f, 0.14f, 0, -20)
                    .Pivot(gT, V(0, 0.42f, -0.6f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 131 FENLIGHT
            {
                var d = K(b, 131, "fenlight", "Fenlight", Element.Lumen, Element.Shade, Rarity.Rare, L, Temperament.Curious, Locomotion.Flyer, 0.8f, 'G',
                    "A marsh-light in a ragged little cloak. It leads the lost out of the fen, or further in, depending on how polite they were.",
                    "Mischievous, fair-minded, loves riddles.", A("glint_beam", "hex_flicker"), "glow_pulse",
                    Work((WorkType.Kindling, 2), (WorkType.Charging, 2), (WorkType.Crafting, 1)), CompanionPerk.Glow, "gel", "crystal",
                    Pal(Hex("#3a4a5a"), Hex("#6a7a8a"), Hex("#70ffd0"), Hex("#c0fff0")));
                d.spawnTime = SpawnTime.Night;
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cG, V(0, 0.86f, 0), V(0.4f, 0.42f, 0.4f))
                    .Add(Con, gB, cG, V(0, 1.16f, -0.02f), V(0.26f, 0.4f, 0.26f), V(-10, 0, 0))
                    .Add(Con, gB, cA, V(0.1f, 1.08f, -0.06f), V(0.14f, 0.26f, 0.14f), V(-10, 0, -25))
                    .Add(Con, gB, cA, V(-0.1f, 1.04f, -0.04f), V(0.12f, 0.2f, 0.12f), V(-10, 0, 28))
                    .Head(gB, 0.86f, 0f, 0.4f).Face(0.08f, cD, 0.05f)
                    .Add(Cyl, gB, cP, V(0, 0.68f, 0), V(0.38f, 0.1f, 0.38f))
                    .Ring(Con, gT, cP, V(0, 0.5f, 0), 0.13f, 6, V(0.16f, 0.36f, 0.16f), 172)
                    .Mirror(Sph, gAL, gAR, cP, V(0.28f, 0.72f, 0.08f), V(0.1f, 0.1f, 0.1f))
                    .Ring(Sph, gX, cG, V(0, 0.9f, 0), 0.5f, 3, V(0.09f, 0.09f, 0.09f))
                    .Pivot(gT, V(0, 0.66f, 0)).Pivot(gX, V(0, 0.9f, 0)).PivotPair(gAL, gAR, V(0.2f, 0.74f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 132 MANGROWL
            {
                var d = K(b, 132, "mangrowl", "Mangrowl", Element.Flora, Element.Stone, Rarity.Rare, L, Temperament.Territorial, Locomotion.Quadruped, 1.4f, 'T',
                    "A bear-shaped tangle of mangrove with a whole thicket on its back. Herons nest in it. It has opinions about people who snap branches.",
                    "Protective, rumbling, slow to forgive.", A("root_ram", "quake_stomp"), "petal_storm",
                    Work((WorkType.Logging, 3), (WorkType.Cultivating, 2), (WorkType.Hauling, 2)), CompanionPerk.LumberBoost, "wood", "fiber",
                    Pal(Hex("#6a5a44"), Hex("#4a3a2c"), Hex("#3a8a4a"), Hex("#ffb060")));
                new Plan()
                    .Quad(0.8f, 0.8f, 0.7f, 1.1f, 0.5f, 0.56f, 0.2f, cP, cS, cD, 0.06f)
                    .Add(Sph, gB, cP, V(0, 0.94f, 0.26f), V(0.84f, 0.7f, 0.6f))
                    .Face(0.06f, cG).Snout(cS, 0.26f, 0.6f).Brows(cD)
                    .Ears(Sph, cP, 0.12f, 0.12f, 30)
                    .Add(Sph, gB, cA, V(0, 1.36f, -0.1f), V(0.9f, 0.5f, 0.9f))
                    .Add(Sph, gB, cA, V(0.3f, 1.3f, 0.26f), V(0.5f, 0.4f, 0.5f))
                    .Add(Sph, gB, cA, V(-0.3f, 1.26f, -0.4f), V(0.5f, 0.36f, 0.5f))
                    .Ring(Sph, gB, cG, V(0, 1.44f, -0.1f), 0.36f, 4, V(0.09f, 0.09f, 0.09f), 0, 45)
                    .WhipPair(gB, gB, cS, V(0.3f, 0.7f, -0.1f), V(0.1f, -0.04f, 0f), V(-0.006f, -0.03f, 0), 6, 0.1f, 0.05f)
                    .WhipPair(gB, gB, cS, V(0.26f, 0.7f, -0.4f), V(0.08f, -0.04f, -0.06f), V(-0.006f, -0.03f, 0), 6, 0.09f, 0.05f)
                    .Tail(cS, 0.8f, -0.56f, 0.4f, 0.1f, 0.05f, -0.03f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 133 HYDRABOG (epic)
            {
                var d = K(b, 133, "hydrabog", "Hydrabog", Element.Tide, Element.Shade, Rarity.Epic, L, Temperament.Aggressive, Locomotion.Quadruped, 1.8f, 'B',
                    "Three heads, one stomach, no agreement. The Mirefen's oldest terror spends most of its time arguing with itself, and the fen is safer for it.",
                    "Quarrelsome, cunning, each head sulks separately.", A("venom_spit", "wave_crash"), "gloom_pulse",
                    Work((WorkType.Irrigating, 3), (WorkType.Hauling, 3), (WorkType.Quarrying, 1)), CompanionPerk.OreSense, "gel", "crystal",
                    Pal(Hex("#3a5a5a"), Hex("#a8c8a0"), Hex("#8a4aa0"), Hex("#c0ff60")));
                d.nearWater = true;
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.66f, -0.1f), V(0.9f, 0.7f, 1.3f))
                    .Add(Sph, gB, cS, V(0, 0.54f, 0), V(0.72f, 0.5f, 1.1f))
                    .Legs4(cP, 0.3f, 0.34f, -0.5f, 0.42f, 0.24f, cD)
                    .Spikes(gB, cA, 0.96f, 0.2f, -0.6f, 5, 0.08f, 0.22f, 0.03f, -20)
                    .Whip(gT, cP, V(0, 0.6f, -0.7f), V(0, -0.03f, -0.14f), V(0, 0.004f, 0), 7, 0.4f, 0.08f)
                    .Pivot(gT, V(0, 0.6f, -0.64f))
                    .WhipPair(gH, gH, cP, V(0.26f, 0.8f, 0.36f), V(0.07f, 0.09f, 0.06f), V(0.002f, -0.006f, 0.01f), 6, 0.26f, 0.17f, cS)
                    .TipHead(gH, cP, 0.36f, 0.055f, cG, true, cS)
                    .Whip(gH, cP, V(0, 0.84f, 0.44f), V(0, 0.1f, 0.08f), V(0, -0.004f, 0.012f), 6, 0.3f, 0.2f, cS)
                    .TipHead(gH, cP, 0.42f, 0.06f, cG, false, cS).Horns(cA, 0.07f, 0.24f).Brows(cD, 0.12f)
                    .Pivot(gH, V(0, 0.8f, 0.4f))
                    .Done(d);
            }
        }
    }
}
