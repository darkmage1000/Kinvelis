using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The three shinobi kin (user request 2026-10-09): edgy, masked, each a different animal and element, and each
    /// with its own Blink skill (AbilityKind.Blink: vanish, reappear behind the target, a light strike that stuns it
    /// for a moment, and the user's next hit is a certain crit - CreatureActor.TickAbility). The step is their first
    /// move, so every one of them knows it from level 1; the heavy move it sets up comes at level 5.
    ///   #188 Kagekit   - Shade / Gale cat in a mask and red headband, a short blade across its back (Sunmeadow, Peaks)
    ///   #189 Raikunai  - Volt / Shade weasel with a lightning scarf and a kunai for a tail (Peaks, Badlands)
    ///   #190 Kasamantis - Flora / Gale mantis under a straw hat, scythes for arms (Mirefen, Sunmeadow)
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildNinjaAbilities(Bundle b)
        {
            void Step(string id, string name, Element e, float range)
            {
                Ab(b, id, name, e, AbilityKind.Blink, 14, range, 7f);
                b.abilities[b.abilities.Count - 1].windup = 0.2f;   // no time to see it coming
            }
            Step("shadow_step", "Shadowstep", Element.Shade, 11f);
            Step("flash_step", "Flashstep", Element.Volt, 12f);
            Step("leaf_step", "Leafstep", Element.Flora, 11f);
        }

        static void BuildNinjas(Bundle b)
        {
            // ---------------------------------------------------------------- 188 KAGEKIT
            {
                var d = K(b, 188, "kagekit", "Kagekit", Element.Shade, Element.Gale, Rarity.Uncommon, BiomeMask.Meadow | BiomeMask.Peaks, Temperament.Territorial, Locomotion.Biped, 0.8f, 'F',
                    "A cat that walks upright and is never where you last saw it. Kagekit train alone on rooftops and treetops, and settle every quarrel with a single strike from behind.",
                    "Silent, proud, keeps score.", A("shadow_step", "night_slash", "gust_slash"), "shadow_pounce",
                    Work((WorkType.Hauling, 2), (WorkType.Foraging, 2)), CompanionPerk.ForageBoost, "pelt", "fiber",
                    Pal(Hex("#2b2d47"), Hex("#4a4d6e"), Hex("#d8283a"), Hex("#8af4ff")));
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Biped(0.3f, 0.34f, 0.36f, 0.4f, 0.11f, cP, cP, cD)
                    // sash with a knot at the hip
                    .Add(Cyl, gB, cA, V(0, 0.36f, 0), V(0.37f, 0.07f, 0.34f))
                    .Add(Bx, gB, cA, V(0.19f, 0.3f, 0.05f), V(0.05f, 0.17f, 0.04f), V(0, 0, 12))
                    // mask over the muzzle, headband with a steel plate, its tails streaming behind
                    .Add(Sph, gH, cD, V(0, 0.72f, 0.05f), V(0.42f, 0.23f, 0.41f))
                    .Add(Cyl, gH, cA, V(0, 0.91f, 0.02f), V(0.42f, 0.07f, 0.41f))
                    .Add(Bx, gH, cW, V(0, 0.91f, 0.2f), V(0.17f, 0.075f, 0.05f))
                    .Face(0.075f, cG, 0.1f, 0.25f).Brows(cD, 0.07f, 0.25f)
                    .Ears(Con, cP, 0.15f, 0.22f, 16f)
                    .Whip(gH, cA, V(0.06f, 0.91f, -0.19f), V(0.02f, -0.005f, -0.09f), V(0, 0.012f, 0), 5, 0.07f, 0.03f)
                    .Whip(gH, cA, V(-0.06f, 0.89f, -0.19f), V(-0.025f, -0.03f, -0.08f), V(0, 0.008f, 0), 4, 0.07f, 0.03f)
                    // a short blade slung across the back, grip over the left shoulder
                    .Add(Bx, gB, cW, V(0.02f, 0.5f, -0.19f), V(0.045f, 0.5f, 0.03f), V(0, 0, 38))
                    .Add(Bx, gB, cA, V(-0.115f, 0.675f, -0.19f), V(0.13f, 0.03f, 0.05f), V(0, 0, 38))
                    .Add(Bx, gB, cD, V(-0.17f, 0.745f, -0.19f), V(0.05f, 0.15f, 0.045f), V(0, 0, 38))
                    .Tail(cP, 0.3f, -0.14f, 0.38f, 0.08f, 0.04f, 0.035f, cW, 0.075f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 189 RAIKUNAI
            {
                var d = K(b, 189, "raikunai", "Raikunai", Element.Volt, Element.Shade, Rarity.Rare, BiomeMask.Peaks | BiomeMask.Badlands, Temperament.Aggressive, Locomotion.Quadruped, 1f, 'F',
                    "A lean black weasel that moves between heartbeats. The scarf round its neck is a live strip of lightning, and the blade at the end of its tail was never forged by anyone.",
                    "Restless, sharp-tongued, hates waiting.", A("flash_step", "volt_whip", "night_slash"), "thunder_charge",
                    Work((WorkType.Charging, 2), (WorkType.Kindling, 1)), CompanionPerk.Swiftness, "pelt", "crystal",
                    Pal(Hex("#26262e"), Hex("#e8e2c8"), Hex("#ffd428"), Hex("#fff27a")));
                Effects(d, AuraFx.Sparks);
                new Plan()
                    .Quad(0.36f, 0.3f, 0.28f, 0.86f, 0.3f, 0.24f, 0.08f, cP, cP, cD, 0.12f, 0.04f)
                    .Add(Sph, gB, cS, V(0, 0.3f, 0.06f), V(0.24f, 0.18f, 0.7f))
                    .Snout(cP, 0.13f, 0.9f)
                    // a pale bandit mask across the eyes, lit from inside
                    .Add(Sph, gH, cS, V(0, 0.515f, 0.555f), V(0.315f, 0.1f, 0.25f))
                    .Face(0.06f, cG, 0.12f, 0.27f).Brows(cD, 0.1f, 0.27f)
                    .Ears(Con, cP, 0.1f, 0.2f, 22f)
                    // the lightning scarf: a wrap at the neck and a jagged tail of it thrown back over the shoulder
                    .Add(Cyl, gB, cA, V(0, 0.46f, 0.37f), V(0.27f, 0.055f, 0.27f), V(62, 0, 0))
                    .Whip(gB, cA, V(0.06f, 0.53f, 0.3f), V(0.03f, 0.07f, -0.11f), V(0, -0.034f, 0), 7, 0.075f, 0.02f)
                    // black hood over the top of the head, so only the lit mask shows
                    .Add(Sph, gH, cD, V(0, 0.56f, 0.5f), V(0.31f, 0.2f, 0.3f))
                    // a row of kunai down the spine, and one for a tail
                    .Spikes(gB, cW, 0.47f, 0.2f, -0.32f, 4, 0.06f, 0.22f, 0.02f, -38f)
                    .Tail(cP, 0.38f, -0.42f, 0.5f, 0.09f, 0.05f, 0.045f)
                    .Tip(Cyl, gT, cA, V(0.11f, 0.05f, 0.11f))
                    .Tip(Con, gT, cW, V(0.1f, 0.34f, 0.045f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 190 KASAMANTIS
            {
                var d = K(b, 190, "kasamantis", "Kasamantis", Element.Flora, Element.Gale, Rarity.Rare, BiomeMask.Swamp | BiomeMask.Meadow, Temperament.Territorial, Locomotion.Biped, 1.1f, 'G',
                    "A wandering mantis that never lifts the brim of its reed hat. It stands so still that birds land on it, and it has never needed a second cut.",
                    "Patient, courteous, utterly merciless.", A("leaf_step", "gust_slash", "vine_whip"), "cyclone",
                    Work((WorkType.Logging, 2), (WorkType.Cultivating, 2)), CompanionPerk.LumberBoost, "fiber", "wood",
                    Pal(Hex("#2f6a48"), Hex("#d8c48a"), Hex("#b8323a"), Hex("#ffe060")));
                Effects(d, AuraFx.Breeze);
                new Plan()
                    .Biped(0.5f, 0.24f, 0.44f, 0.3f, 0.075f, cP, cP, cD, false)
                    // long abdomen hanging behind, paler underneath
                    .Add(Sph, gT, cP, V(0, 0.5f, -0.3f), V(0.26f, 0.24f, 0.58f), V(-22, 0, 0))
                    .Add(Sph, gT, cS, V(0, 0.45f, -0.3f), V(0.2f, 0.16f, 0.5f), V(-22, 0, 0))
                    .Pivot(gT, V(0, 0.56f, -0.08f))
                    // narrow chin, eyes glowing under the brim
                    .Add(Con, gH, cP, V(0, 0.93f, 0.07f), V(0.2f, 0.2f, 0.2f), V(180, 0, 0))
                    .Face(0.09f, cG, -0.02f, 0.32f)
                    // the reed hat: a wide shallow cone with a darker band, antennae through the crown
                    .Add(Con, gH, cS, V(0, 1.2f, 0f), V(0.82f, 0.26f, 0.82f))
                    .Add(Cyl, gH, cA, V(0, 1.115f, 0f), V(0.62f, 0.025f, 0.62f))
                    .Mirror(Cyl, gH, cD, V(0.07f, 1.37f, 0.05f), V(0.02f, 0.22f, 0.02f), V(18, 0, -16))
                    // scythe arms: forearm held forward, the blade folded up along it
                    .Mirror(Cap, gAL, gAR, cP, V(0.2f, 0.78f, 0.13f), V(0.075f, 0.32f, 0.075f), V(62, 0, -8))
                    // a slim curved blade: three short lengths leaning further back, then the point
                    .Mirror(Bx, gAL, gAR, cW, V(0.23f, 0.8f, 0.3f), V(0.022f, 0.17f, 0.07f), V(-8, 0, 0))
                    .Mirror(Bx, gAL, gAR, cW, V(0.23f, 0.94f, 0.27f), V(0.022f, 0.16f, 0.06f), V(-22, 0, 0))
                    .Mirror(Bx, gAL, gAR, cW, V(0.23f, 1.06f, 0.2f), V(0.022f, 0.14f, 0.05f), V(-40, 0, 0))
                    .Mirror(Con, gAL, gAR, cW, V(0.23f, 1.14f, 0.11f), V(0.022f, 0.14f, 0.045f), V(-58, 0, 0))
                    .PivotPair(gAL, gAR, V(0.16f, 0.86f, 0.02f))
                    // a red scarf at the neck, blowing back
                    .Add(Cyl, gB, cA, V(0, 0.92f, 0f), V(0.24f, 0.07f, 0.22f))
                    .Whip(gB, cA, V(0.04f, 0.92f, -0.1f), V(0.02f, -0.015f, -0.1f), V(0, 0.012f, 0), 5, 0.08f, 0.03f)
                    // folded wing-cases down the back, like a cloak
                    .Mirror(Sph, gB, cP, V(0.09f, 0.66f, -0.2f), V(0.18f, 0.05f, 0.5f), V(-26, 0, -14))
                    .Done(d);
            }
        }
    }
}
