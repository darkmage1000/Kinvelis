using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Styled kin, part 3: the three ladies (#207-209, user request 2026-10-09) - humanoid kin in the mould of Floressa,
    /// built on Plan.Lady: Sirelle the mermaid (Tide), Sekhara the lioness of the sun (Shade) and Cindra the fire
    /// hound (Ember). Glamorous rather than cute; each is a Rare with her own signature skill. See ContentSeed.Styled.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildStyledC(Bundle b)
        {
            // ---------------------------------------------------------------- 207 SIRELLE (mermaid)
            {
                var d = K(b, 207, "sirelle", "Sirelle", Element.Tide, Element.Lumen, Rarity.Rare, BiomeMask.Isles, Temperament.Curious, Locomotion.Flyer, 1.15f, 'B',
                    "She swims through the air as easily as the lagoon, trailing her hair like weed in a current. Sailors of the Driftkeys say her song pulls boats onto the reef, and that nobody on board minded.",
                    "Playful, flirtatious, collects shiny things from wrecks.", A("siren_song", "tidal_lash", "glint_beam"), "wave_crash",
                    Work((WorkType.Irrigating, 4), (WorkType.Cultivating, 2)), CompanionPerk.Mending, "gel", "crystal",
                    Pal(Hex("#f6d6c4"), Hex("#ff7fb4"), Hex("#1fb5a8"), Hex("#b8fff0"), Hex("#2a6ad0")));
                d.nearWater = true;
                Effects(d, AuraFx.Bubbles);
                const float hip = 0.52f, hy = 1.149f;
                new Plan()
                    .Lady(hip, cP, cP, cG, cA, false)
                    // scales climbing to the waist, a string of pearls on the hips, the long tail curling back to its fluke
                    .Add(Sph, gB, cA, V(0, hip + 0.09f, -0.003f), V(0.29f, 0.14f, 0.23f))
                    .Ring(Sph, gB, cW, V(0, hip + 0.13f, 0), 0.105f, 8, V(0.04f, 0.04f, 0.04f))
                    .Mirror(Sph, gB, cG, V(0.17f, hip + 0.01f, -0.03f), V(0.03f, 0.17f, 0.15f), V(0, 0, -26))
                    .Whip(gT, cA, V(0, hip - 0.04f, 0), V(0, -0.085f, -0.018f), V(0, 0.011f, -0.012f), 9, 0.3f, 0.1f)
                    .Fronds(gT, cG, 1, 0.4f, 0.22f, 0.22f, null, 140f)
                    .Fronds(gT, cG, 1, 0.4f, 0.22f, 0.22f, null, 220f)
                    .Tip(Sph, gT, cA, V(0.12f, 0.12f, 0.12f))
                    .Pivot(gT, V(0, hip, 0))
                    // pearls at the throat, bracelets, a fin along each forearm
                    .Add(Sph, gB, cW, V(0, hip + 0.435f, 0.052f), V(0.05f, 0.05f, 0.04f))
                    .Mirror(Sph, gB, cW, V(0.035f, hip + 0.445f, 0.045f), V(0.035f, 0.035f, 0.03f))
                    .Mirror(Cyl, gAL, gAR, cG, V(0.228f, hip + 0.04f, 0.002f), V(0.085f, 0.03f, 0.085f))
                    .Mirror(Sph, gAL, gAR, cA, V(0.245f, hip + 0.15f, -0.05f), V(0.025f, 0.17f, 0.12f))
                    // face, long coral hair, fins for ears, a starfish and a pearl circlet
                    .LadyFace(cS)
                    .LongHair(cS, 13, 0.25f, cG)
                    .Mirror(Sph, gH, cA, V(0.128f, hy, -0.01f), V(0.03f, 0.15f, 0.12f), V(0, 0, -25))
                    .Fan(Con, gH, cG, V(0.13f, hy + 0.1f, 0.105f), 0.035f, 5, V(0.035f, 0.08f, 0.03f))
                    .Add(Sph, gH, cG, V(0, hy + 0.118f, 0.122f), V(0.05f, 0.055f, 0.04f))
                    .Mirror(Sph, gH, cW, V(0.055f, hy + 0.11f, 0.112f), V(0.032f, 0.032f, 0.03f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 208 SEKHARA (lioness of the sun)
            {
                var d = K(b, 208, "sekhara", "Sekhara", Element.Shade, Element.Ember, Rarity.Rare, BiomeMask.Desert | BiomeMask.Badlands, Temperament.Territorial, Locomotion.Biped, 1.2f, 'G',
                    "The dunes remember a queen with a lioness's head who carried the evening sun on her brow. She walks them still, tall and unhurried, and what her shadow falls on at dusk does not see morning.",
                    "Regal, exacting, merciless to the disrespectful.", A("night_slash", "sun_wrath", "flare_horn"), "solar_flare",
                    Work((WorkType.Hauling, 3), (WorkType.Kindling, 2)), CompanionPerk.Warmth, "pelt", "crystal",
                    Pal(Hex("#6b4028"), Hex("#f2ead6"), Hex("#e8b030"), Hex("#ffa028")));
                Effects(d, AuraFx.Embers);
                const float hip = 0.8f, hy = 1.4376f;
                new Plan()
                    .Lady(hip, cP, cS, cS, cS, true, 0.28f)
                    // a wrap of linen: panels front and back with gold hems, a belt, a broad gold collar with a sunstone
                    .Add(Bx, gB, cS, V(0, 0.5f, 0.118f), V(0.17f, 0.62f, 0.02f))
                    .Add(Bx, gB, cS, V(0, 0.5f, -0.128f), V(0.25f, 0.62f, 0.02f))
                    .Add(Bx, gB, cA, V(0, 0.2f, 0.12f), V(0.18f, 0.045f, 0.022f))
                    .Add(Bx, gB, cA, V(0, 0.2f, -0.13f), V(0.26f, 0.045f, 0.022f))
                    .Add(Cyl, gB, cA, V(0, hip + 0.08f, -0.004f), V(0.32f, 0.05f, 0.25f))
                    .Add(Sph, gB, cG, V(0, hip + 0.07f, 0.125f), V(0.06f, 0.07f, 0.04f))
                    .Add(Sph, gB, cA, V(0, hip + 0.43f, 0.012f), V(0.38f, 0.07f, 0.29f), V(8, 0, 0))
                    .Add(Sph, gB, cG, V(0, hip + 0.4f, 0.13f), V(0.06f, 0.07f, 0.04f))
                    // gold at the wrists, arms and ankles
                    .Mirror(Cyl, gAL, gAR, cA, V(0.226f, hip + 0.04f, 0.002f), V(0.088f, 0.1f, 0.088f))
                    .Mirror(Cyl, gAL, gAR, cA, V(0.188f, hip + 0.3f, 0f), V(0.088f, 0.04f, 0.088f))
                    .Mirror(Cyl, gFL, gFR, cA, V(0.085f, 0.1f, 0f), V(0.1f, 0.035f, 0.1f))
                    // lioness's head: muzzle, round ears, eyes like coals, under a black wig with gold-tipped lappets
                    .Snout(cP, 0.13f, 0.6f)
                    .LadyFace(cG, 0.052f, cG)
                    .Add(Sph, gH, cD, V(0, hy + 0.03f, -0.05f), V(0.33f, 0.3f, 0.3f))
                    .Add(Sph, gH, cD, V(0, hy - 0.14f, -0.1f), V(0.3f, 0.36f, 0.14f))
                    .Mirror(Sph, gH, cP, V(0.127f, hy + 0.165f, 0.005f), V(0.1f, 0.1f, 0.05f))
                    .Mirror(Cap, gH, cD, V(0.135f, hy - 0.2f, 0.04f), V(0.09f, 0.34f, 0.08f))
                    .Mirror(Cyl, gH, cA, V(0.135f, hy - 0.11f, 0.04f), V(0.095f, 0.03f, 0.085f))
                    .Mirror(Cyl, gH, cA, V(0.135f, hy - 0.36f, 0.04f), V(0.095f, 0.045f, 0.085f))
                    // the sun on her brow, held between two horns, a cobra rearing in front of it
                    .Add(Cyl, gH, cG, V(0, hy + 0.37f, -0.02f), V(0.28f, 0.03f, 0.28f), V(90, 0, 0))
                    .Mirror(Con, gH, cA, V(0.115f, hy + 0.28f, -0.02f), V(0.045f, 0.24f, 0.04f), V(0, 0, -26))
                    .Whip(gH, cA, V(0, hy + 0.17f, 0.1f), V(0, 0.035f, 0.012f), V(0, 0, 0.008f), 3, 0.045f, 0.035f)
                    .Tip(Sph, gH, cA, V(0.07f, 0.06f, 0.05f))
                    // an ankh-headed staff, and a lion's tail
                    .Add(Cyl, gAR, cA, V(0.262f, 0.72f, 0.06f), V(0.026f, 1.0f, 0.026f))
                    .Add(Bx, gAR, cA, V(0.262f, 1.21f, 0.06f), V(0.17f, 0.028f, 0.028f))
                    .Add(Sph, gAR, cA, V(0.262f, 1.32f, 0.06f), V(0.11f, 0.18f, 0.03f))
                    .Add(Sph, gAR, cD, V(0.262f, 1.32f, 0.06f), V(0.055f, 0.11f, 0.036f))
                    .Tail(cP, hip + 0.02f, -0.12f, 0.62f, 0.05f, 0.035f, -0.075f, cD, 0.09f, 0.013f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 209 CINDRA (fire hound)
            {
                var d = K(b, 209, "cindra", "Cindra", Element.Ember, Element.Normal, Rarity.Rare, BiomeMask.Badlands | BiomeMask.Meadow, Temperament.Curious, Locomotion.Biped, 1.1f, 'F',
                    "A hound that walks on two legs, with hair and tail of open flame. She chooses one person and keeps to their heel for good; when she throws back her head and howls, everyone on her side stands a little taller.",
                    "Bold, teasing, fiercely loyal.", A("flame_dash", "heat_howl", "ember_spit"), "lava_plume",
                    Work((WorkType.Kindling, 2), (WorkType.Hauling, 2), (WorkType.Foraging, 2)), CompanionPerk.Warmth, "pelt", "cinder",
                    Pal(Hex("#f2cfae"), Hex("#fff0d8"), Hex("#f0602a"), Hex("#ffd040"), Hex("#b8520a")));
                Effects(d, AuraFx.Embers);
                const float hip = 0.78f, hy = 1.4092f;
                new Plan()
                    .Lady(hip, cP, cP, cD, cD)
                    // cropped top, belt, a collar with a tag
                    .Add(Sph, gB, cD, V(0, hip + 0.33f, -0.012f), V(0.285f, 0.15f, 0.2f))
                    .Add(Cyl, gB, cA, V(0, hip + 0.1f, -0.004f), V(0.3f, 0.035f, 0.235f))
                    .Add(Bx, gB, cG, V(0, hip + 0.1f, 0.12f), V(0.06f, 0.05f, 0.02f))
                    .Add(Cyl, gB, cD, V(0, hip + 0.475f, 0f), V(0.085f, 0.03f, 0.085f))
                    .Add(Sph, gB, cG, V(0, hip + 0.45f, 0.046f), V(0.04f, 0.045f, 0.03f))
                    // flame-furred shoulders, paw gloves and paw boots with cream cuffs
                    .Mirror(Sph, gAL, gAR, cA, V(0.185f, hip + 0.36f, 0f), V(0.12f, 0.15f, 0.12f))
                    .Mirror(Sph, gAL, gAR, cA, V(0.24f, hip - 0.04f, 0.012f), V(0.12f, 0.14f, 0.13f))
                    .Mirror(Sph, gAL, gAR, cS, V(0.228f, hip + 0.045f, 0f), V(0.115f, 0.065f, 0.115f))
                    .Mirror(Cap, gFL, gFR, cA, V(0.085f, 0.19f, -0.005f), V(0.12f, 0.32f, 0.125f))
                    .Mirror(Sph, gFL, gFR, cA, V(0.085f, 0.06f, 0.045f), V(0.14f, 0.13f, 0.23f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.085f, 0.34f, 0f), V(0.145f, 0.075f, 0.15f))
                    // face with a fang, flame hair, tall ears
                    .LadyFace(cA, 0.055f)
                    .Cheeks(cA)
                    .Add(Sph, gH, cD, V(0, hy - 0.035f, 0.135f), V(0.028f, 0.022f, 0.02f))
                    .Add(Con, gH, cW, V(0.032f, hy - 0.087f, 0.108f), V(0.018f, 0.035f, 0.018f), V(180, 0, 0))
                    .LongHair(cA, 8, 0.27f, cG)
                    .Fan(Con, gH, cA, V(0, hy + 0.12f, -0.08f), 0.12f, 5, V(0.09f, 0.22f, 0.08f), -60, 120)
                    .Fan(Con, gH, cG, V(0, hy + 0.13f, -0.11f), 0.17f, 4, V(0.05f, 0.15f, 0.05f), -45, 90)
                    .Mirror(Con, gH, cA, V(0.13f, hy + 0.2f, -0.01f), V(0.12f, 0.23f, 0.07f), V(-6, 0, -20))
                    .Mirror(Con, gH, cS, V(0.13f, hy + 0.185f, 0.014f), V(0.07f, 0.15f, 0.04f), V(-6, 0, -20))
                    // a tail that is one big flame
                    .Whip(gT, cA, V(0, hip + 0.02f, -0.13f), V(0, 0.03f, -0.1f), V(0, 0.024f, 0.004f), 6, 0.2f, 0.13f)
                    .Tip(Con, gT, cG, V(0.17f, 0.28f, 0.17f))
                    .Pivot(gT, V(0, hip, -0.1f))
                    .Done(d);
            }
        }
    }
}
