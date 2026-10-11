using UnityEngine;

namespace Wildbond
{
    /// <summary>Styled kin, part 1: warriors (#191-194) and casters (#195-198). See ContentSeed.Styled.</summary>
    public static partial class ContentSeed
    {
        static void BuildStyledA(Bundle b)
        {
            // ---------------------------------------------------------------- 191 KABUTARO (samurai beetle)
            {
                var d = K(b, 191, "kabutaro", "Kabutaro", Element.Stone, Element.Gale, Rarity.Rare, BiomeMask.Peaks | BiomeMask.Badlands, Temperament.Territorial, Locomotion.Biped, 1.15f, 'T',
                    "A rhinoceros beetle whose shell grew into lacquered plates. It can stand in a mountain pass for a day without moving; travellers who took it for a statue did not get a second look.",
                    "Stern, ceremonious, bows before a fight.", A("iaido_cut", "gust_slash", "rock_jaw"), "quake_stomp",
                    Work((WorkType.Logging, 3), (WorkType.Quarrying, 2)), CompanionPerk.LumberBoost, "stone", "ore",
                    Pal(Hex("#33272a"), Hex("#8a6a3a"), Hex("#a8262c"), Hex("#ffd060")));
                Effects(d, AuraFx.Breeze);
                new Plan()
                    .Biped(0.34f, 0.46f, 0.46f, 0.36f, 0.13f, cP, cD, cD)
                    // lacquered wing-cases down the back, a breastplate with a gold edge, a skirt of plates
                    .Mirror(Sph, gB, cA, V(0.11f, 0.56f, -0.15f), V(0.27f, 0.5f, 0.2f), V(8, 0, -6))
                    .Add(Bx, gB, cA, V(0, 0.58f, 0.2f), V(0.32f, 0.26f, 0.06f))
                    .Add(Bx, gB, cG, V(0, 0.44f, 0.21f), V(0.34f, 0.03f, 0.06f))
                    .Ring(Bx, gB, cA, V(0, 0.36f, 0), 0.25f, 6, V(0.18f, 0.2f, 0.04f), -14, 30)
                    .Mirror(Bx, gAL, gAR, cA, V(0.3f, 0.72f, 0), V(0.12f, 0.17f, 0.2f), V(0, 0, -18))
                    .Mirror(Bx, gAL, gAR, cG, V(0.325f, 0.635f, 0), V(0.11f, 0.022f, 0.21f), V(0, 0, -18))
                    // helmet with a neck guard, the horn rising from its brow, a gold crescent either side
                    .Add(Dom, gH, cA, V(0, 0.97f, 0.02f), V(0.45f, 0.32f, 0.45f))
                    .Add(Sph, gH, cA, V(0, 0.9f, -0.15f), V(0.48f, 0.14f, 0.26f))
                    .Add(Sph, gH, cD, V(0, 0.86f, 0.14f), V(0.24f, 0.12f, 0.14f))
                    .Face(0.07f, cG, 0.03f, 0.24f)
                    .Add(Con, gH, cS, V(0, 1.2f, 0.17f), V(0.1f, 0.46f, 0.1f), V(28, 0, 0))
                    .Mirror(Con, gH, cS, V(0.05f, 1.42f, 0.3f), V(0.05f, 0.14f, 0.05f), V(28, 0, -40))
                    .Mirror(Con, gH, cG, V(0.14f, 1.13f, 0.16f), V(0.045f, 0.26f, 0.03f), V(0, 0, -36))
                    // the blade stays in its sheath at the hip until the one cut
                    .Add(Bx, gB, cD, V(-0.27f, 0.42f, -0.06f), V(0.05f, 0.055f, 0.62f), V(-18, 0, 0))
                    .Add(Cyl, gB, cG, V(-0.27f, 0.51f, 0.22f), V(0.1f, 0.02f, 0.1f), V(72, 0, 0))
                    .Add(Bx, gB, cW, V(-0.27f, 0.545f, 0.31f), V(0.04f, 0.04f, 0.18f), V(-18, 0, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 192 BULWARDEN (little knight, big shield)
            {
                var d = K(b, 192, "bulwarden", "Bulwarden", Element.Stone, Element.Lumen, Rarity.Uncommon, BiomeMask.Meadow | BiomeMask.Peaks, Temperament.Docile, Locomotion.Biped, 0.8f, 'T',
                    "Nobody has seen what is inside the armour. A Bulwarden picks someone smaller than its shield and stands in front of them for the rest of its life.",
                    "Earnest, stubborn, takes oaths very seriously.", A("shell_slam", "shield_wall", "glint_beam"), "quake_stomp",
                    Work((WorkType.Quarrying, 2), (WorkType.Hauling, 2)), CompanionPerk.StoneBoost, "stone", "ore",
                    Pal(Hex("#6f7f9a"), Hex("#c7d0de"), Hex("#3d5fc4"), Hex("#ffe680")));
                new Plan()
                    .Biped(0.22f, 0.36f, 0.34f, 0.36f, 0.11f, cP, cP, cD)
                    .Add(Sph, gB, cA, V(0, 0.38f, -0.17f), V(0.34f, 0.44f, 0.08f), V(8, 0, 0))
                    .Mirror(Sph, gAL, gAR, cS, V(0.24f, 0.5f, 0), V(0.17f, 0.13f, 0.17f))
                    // helm: a dome with a dark visor slit, two lights behind it, a plume on top
                    .Add(Dom, gH, cS, V(0, 0.7f, 0.02f), V(0.43f, 0.34f, 0.43f))
                    .Add(Bx, gH, cD, V(0, 0.69f, 0.17f), V(0.27f, 0.06f, 0.07f))
                    .Mirror(Sph, gH, cG, V(0.065f, 0.69f, 0.19f), V(0.05f, 0.045f, 0.04f))
                    .Whip(gH, cA, V(0, 0.88f, 0f), V(0, 0.05f, -0.05f), V(0, -0.022f, 0), 5, 0.11f, 0.04f)
                    // the tower shield, taller than it is, carried on the left; a short mace in the right
                    .Add(Bx, gB, cS, V(-0.3f, 0.43f, 0.24f), V(0.46f, 0.8f, 0.06f), V(0, -18, 0))
                    .Add(Bx, gB, cA, V(-0.305f, 0.43f, 0.262f), V(0.37f, 0.7f, 0.05f), V(0, -18, 0))
                    .Add(Bx, gB, cG, V(-0.31f, 0.46f, 0.283f), V(0.21f, 0.21f, 0.04f), V(0, -18, 0))
                    .Add(Bx, gB, cG, V(-0.31f, 0.46f, 0.283f), V(0.21f, 0.21f, 0.04f), V(0, -18, 45))
                    .Add(Sph, gB, cW, V(-0.315f, 0.46f, 0.3f), V(0.13f, 0.13f, 0.05f), V(0, -18, 0))
                    .Add(Bx, gAR, cW, V(0.27f, 0.3f, 0.15f), V(0.04f, 0.04f, 0.3f))
                    .Add(Sph, gAR, cD, V(0.27f, 0.3f, 0.33f), V(0.12f, 0.12f, 0.12f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 193 JABAROO (boxer kangaroo)
            {
                var d = K(b, 193, "jabaroo", "Jabaroo", Element.Normal, Element.Ember, Rarity.Uncommon, BiomeMask.Badlands | BiomeMask.Desert, Temperament.Territorial, Locomotion.Hopper, 1f, 'F',
                    "It bounces on its toes even when asleep. Jabaroo settle everything in three punches, shake hands afterwards, and remember every bout they have lost.",
                    "Cocky, fair, never hits first.", A("one_two", "quick_kick", "flame_dash"), "pummel_rush",
                    Work((WorkType.Hauling, 3), (WorkType.Kindling, 1)), CompanionPerk.Swiftness, "pelt", "cinder",
                    Pal(Hex("#c98a52"), Hex("#f0d9b0"), Hex("#d8283a"), Hex("#ffd060")));
                new Plan()
                    .Biped(0.42f, 0.34f, 0.4f, 0.3f, 0.12f, cP, cP)
                    .Add(Sph, gB, cS, V(0, 0.56f, 0.08f), V(0.24f, 0.3f, 0.2f))
                    .Snout(cP, 0.14f, 0.9f).Ears(Sph, cP, 0.09f, 0.28f, 12f).Face(0.06f, ColorSlot.Eye, 0.12f, 0.25f).Brows(cD, 0.12f, 0.25f)
                    // trunks with a champion's buckle, and the gloves
                    .Add(Cyl, gB, cA, V(0, 0.42f, 0), V(0.38f, 0.15f, 0.35f))
                    .Add(Cyl, gB, cW, V(0, 0.5f, 0), V(0.37f, 0.04f, 0.34f))
                    .Add(Bx, gB, cG, V(0, 0.5f, 0.17f), V(0.11f, 0.08f, 0.03f))
                    .Mirror(Sph, gAL, gAR, cA, V(0.245f, 0.42f, 0.07f), V(0.18f, 0.18f, 0.2f))
                    .Mirror(Cyl, gAL, gAR, cW, V(0.225f, 0.52f, 0.04f), V(0.12f, 0.05f, 0.12f))
                    // long springy feet and a tail thick enough to lean on
                    .Mirror(Sph, gFL, gFR, cP, V(0.09f, 0.045f, 0.12f), V(0.13f, 0.09f, 0.36f))
                    .Tail(cP, 0.42f, -0.14f, 0.62f, 0.17f, 0.06f, -0.025f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 194 RAPIERON (fencer heron)
            {
                var d = K(b, 194, "rapieron", "Rapieron", Element.Tide, Element.Gale, Rarity.Rare, BiomeMask.Isles | BiomeMask.Swamp, Temperament.Territorial, Locomotion.Biped, 1.2f, 'G',
                    "A heron that fences with its own beak. It waits on one leg in the shallows for a challenger to make the first move, because the first move is always a mistake.",
                    "Vain, gallant, insufferable when it wins.", A("tidal_lash", "riposte", "gust_slash"), "riptide",
                    Work((WorkType.Irrigating, 3), (WorkType.Foraging, 2)), CompanionPerk.Swiftness, "pelt", "gel",
                    Pal(Hex("#5d7fa8"), Hex("#e8f0f5"), Hex("#c0304a"), Hex("#a8f0ff")));
                Effects(d, AuraFx.Breeze);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.66f, 0), V(0.36f, 0.4f, 0.54f), V(-20, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.62f, 0.12f), V(0.25f, 0.28f, 0.32f), V(-20, 0, 0))
                    .Legs2(cD, 0.08f, 0.5f, 0.045f, cD)
                    // S-curved neck up to a small head, and the beak: a rapier
                    .Whip(gH, cS, V(0, 0.8f, 0.18f), V(0, 0.085f, 0.03f), V(0, 0, -0.007f), 5, 0.12f, 0.085f)
                    .TipHead(gH, cP, 0.2f, 0.05f)
                    .Beak(cW, 0.04f, 0.46f)
                    // a cavalier's hat with a long red feather
                    .Add(Cyl, gH, cD, V(0, 1.35f, 0.23f), V(0.34f, 0.02f, 0.34f), V(-8, 0, 6))
                    .Add(Cyl, gH, cD, V(0, 1.39f, 0.23f), V(0.17f, 0.09f, 0.17f), V(-8, 0, 6))
                    .Whip(gH, cA, V(0.09f, 1.41f, 0.21f), V(0.012f, 0.05f, -0.07f), V(0, -0.022f, 0), 6, 0.08f, 0.02f)
                    // half-cape over the left wing, folded wings, a fan of tail feathers
                    .Mirror(Sph, gWL, gWR, cS, V(0.18f, 0.68f, -0.04f), V(0.08f, 0.3f, 0.52f), V(-20, 0, 0))
                    .Add(Sph, gB, cA, V(-0.2f, 0.66f, -0.08f), V(0.09f, 0.48f, 0.52f), V(-20, 0, 6))
                    .Add(Sph, gT, cS, V(0, 0.56f, -0.32f), V(0.18f, 0.05f, 0.3f), V(24, 0, 0))
                    .Pivot(gH, V(0, 0.8f, 0.16f)).Pivot(gT, V(0, 0.6f, -0.2f)).PivotPair(gWL, gWR, V(0.16f, 0.8f, 0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 195 HEXPURR (witch cat on a broom)
            {
                var d = K(b, 195, "hexpurr", "Hexpurr", Element.Shade, Element.Lumen, Rarity.Uncommon, BiomeMask.Meadow | BiomeMask.Swamp, Temperament.Curious, Locomotion.Flyer, 0.75f, 'G',
                    "A cat that found a hat, then a broom, then worked out the rest by itself. Its curses are small and petty - until something else hits you while one is on you.",
                    "Smug, nosy, purrs at misfortune.", A("hex_mark", "hex_flicker", "glint_beam"), "gloom_pulse",
                    Work((WorkType.Crafting, 2), (WorkType.Foraging, 2)), CompanionPerk.Glow, "pelt", "crystal",
                    Pal(Hex("#2e2440"), Hex("#c9a26a"), Hex("#c43a9a"), Hex("#b8ff6a")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    // robe, head, the hat with ears through the brim
                    .Add(Con, gB, cD, V(0, 0.5f, 0), V(0.46f, 0.5f, 0.42f))
                    .Add(Cyl, gB, cA, V(0, 0.7f, 0), V(0.26f, 0.05f, 0.24f))
                    .Add(Sph, gH, cP, V(0, 0.86f, 0.04f), V(0.4f, 0.36f, 0.38f)).Head(gH, 0.86f, 0.04f, 0.4f)
                    .Face(0.085f, cG, 0.04f, 0.25f).Snout(cS, 0.1f, 0.5f)
                    .Mirror(Bx, gH, cW, V(0.18f, 0.81f, 0.2f), V(0.17f, 0.012f, 0.012f), V(0, -20, 8))
                    .Add(Cyl, gH, cD, V(0, 1.02f, 0.02f), V(0.62f, 0.03f, 0.62f))
                    .Add(Con, gH, cD, V(0, 1.22f, 0f), V(0.34f, 0.42f, 0.34f), V(-10, 0, 0))
                    .Add(Con, gH, cD, V(0, 1.42f, -0.1f), V(0.13f, 0.2f, 0.13f), V(-62, 0, 0))
                    .Add(Cyl, gH, cA, V(0, 1.06f, 0.02f), V(0.33f, 0.06f, 0.33f))
                    .Add(Bx, gH, cG, V(0, 1.06f, 0.18f), V(0.09f, 0.08f, 0.03f))
                    .Mirror(Con, gH, cP, V(0.22f, 1.1f, 0.02f), V(0.13f, 0.2f, 0.07f), V(0, 0, -24))
                    // the broom it sits on: handle forward, bristles behind
                    .Add(Cyl, gB, cS, V(0, 0.27f, 0.08f), V(0.045f, 1.1f, 0.045f), V(90, 0, 0))
                    .Add(Con, gB, cA, V(0, 0.27f, -0.62f), V(0.26f, 0.42f, 0.26f), V(90, 0, 0))
                    .Add(Cyl, gB, cD, V(0, 0.27f, -0.43f), V(0.11f, 0.05f, 0.11f), V(90, 0, 0))
                    .Mirror(Sph, gB, cP, V(0.1f, 0.34f, 0.2f), V(0.1f, 0.1f, 0.1f))
                    .Mirror(Sph, gB, cP, V(0.1f, 0.2f, 0.02f), V(0.09f, 0.13f, 0.1f))
                    .Tail(cP, 0.4f, -0.16f, 0.4f, 0.07f, 0.04f, 0.06f)
                    // three little lights that circle it
                    .Add(Sph, gX, cG, V(0.48f, 0.82f, 0f), V(0.08f, 0.08f, 0.08f))
                    .Add(Sph, gX, cA, V(-0.3f, 0.6f, 0.36f), V(0.06f, 0.06f, 0.06f))
                    .Add(Sph, gX, cG, V(-0.26f, 1.0f, -0.38f), V(0.07f, 0.07f, 0.07f))
                    .Pivot(gH, V(0, 0.72f, 0)).Pivot(gX, V(0, 0.75f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 196 PRESTOFOX (stage magician)
            {
                var d = K(b, 196, "prestofox", "Prestofox", Element.Lumen, Element.Shade, Rarity.Rare, BiomeMask.Glow | BiomeMask.Meadow, Temperament.Curious, Locomotion.Biped, 0.95f, 'F',
                    "It bows, the lights flare, and the fox you are looking at is no longer the fox. Prestofox never explain a trick and never do the same one twice in a town.",
                    "A show-off, generous with applause, hates hecklers.", A("glint_beam", "vanishing_act", "night_slash"), "prism_dust",
                    Work((WorkType.Crafting, 3), (WorkType.Hauling, 1)), CompanionPerk.Glow, "pelt", "crystal",
                    Pal(Hex("#e07a2a"), Hex("#fff0d8"), Hex("#c0286a"), Hex("#ffe680")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Biped(0.34f, 0.3f, 0.38f, 0.36f, 0.09f, cP, cD, cD)
                    // tailcoat, shirt front, bow tie, white gloves, a cape with a high collar
                    .Add(Cap, gB, cD, V(0, 0.49f, -0.015f), V(0.33f, 0.4f, 0.3f))
                    .Add(Sph, gB, cW, V(0, 0.53f, 0.13f), V(0.15f, 0.26f, 0.08f))
                    .Mirror(Con, gB, cA, V(0.05f, 0.67f, 0.16f), V(0.07f, 0.1f, 0.04f), V(0, 0, 90))
                    .Mirror(Sph, gAL, gAR, cW, V(0.205f, 0.34f, 0.02f), V(0.1f, 0.1f, 0.1f))
                    .Add(Sph, gB, cA, V(0, 0.46f, -0.19f), V(0.42f, 0.54f, 0.07f), V(8, 0, 0))
                    .Mirror(Con, gB, cA, V(0.14f, 0.73f, -0.09f), V(0.1f, 0.2f, 0.06f), V(0, 0, -25))
                    .Snout(cS, 0.14f, 0.9f).Face(0.07f, ColorSlot.Eye, 0.1f, 0.25f).Cheeks(cS)
                    .Mirror(Con, gH, cP, V(0.18f, 1.03f, 0f), V(0.13f, 0.26f, 0.07f), V(-8, 0, -30))
                    // top hat with a ribbon
                    .Add(Cyl, gH, cD, V(0, 1.03f, 0.02f), V(0.4f, 0.025f, 0.4f))
                    .Add(Cyl, gH, cD, V(0, 1.16f, 0.02f), V(0.25f, 0.24f, 0.25f))
                    .Add(Cyl, gH, cA, V(0, 1.07f, 0.02f), V(0.26f, 0.05f, 0.26f))
                    // wand, and four cards that circle it
                    .Add(Cyl, gAR, cD, V(0.23f, 0.38f, 0.14f), V(0.025f, 0.3f, 0.025f), V(70, 0, 0))
                    .Add(Sph, gAR, cW, V(0.23f, 0.43f, 0.28f), V(0.04f, 0.04f, 0.04f))
                    .Ring(Bx, gX, cW, V(0, 0.62f, 0), 0.56f, 4, V(0.12f, 0.17f, 0.012f), 0, 20)
                    .Ring(Bx, gX, cA, V(0, 0.62f, 0), 0.565f, 4, V(0.06f, 0.08f, 0.012f), 0, 20)
                    .Tail(cP, 0.36f, -0.14f, 0.45f, 0.17f, 0.1f, 0.04f, cW, 0.15f)
                    .Pivot(gX, V(0, 0.62f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 197 MARIONOIR (puppeteer spider)
            {
                var d = K(b, 197, "marionoir", "Marionoir", Element.Shade, Element.Flora, Rarity.Rare, BiomeMask.Blight | BiomeMask.Swamp, Temperament.Aggressive, Locomotion.Quadruped, 1.1f, 'B',
                    "It carries a doll stitched in its own likeness and makes it dance for anything that comes close. By the time the audience notices the threads, they are already tied.",
                    "Patient, theatrical, talks through the doll.", A("puppet_strings", "acid_bite", "vine_whip"), "toxic_cloud",
                    Work((WorkType.Crafting, 3), (WorkType.Cultivating, 2)), CompanionPerk.ForageBoost, "fiber", "gel",
                    Pal(Hex("#3a2444"), Hex("#e8d8b8"), Hex("#c8324a"), Hex("#b4ff5a")));
                Effects(d, AuraFx.Gloom, ExtraAnim.Bob);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.56f, -0.32f), V(0.58f, 0.5f, 0.64f))
                    .Add(Sph, gB, cA, V(0, 0.79f, -0.32f), V(0.2f, 0.07f, 0.34f))
                    .Add(Sph, gB, cP, V(0, 0.5f, 0.1f), V(0.36f, 0.32f, 0.4f))
                    .Add(Sph, gH, cP, V(0, 0.56f, 0.36f), V(0.3f, 0.28f, 0.28f)).Head(gH, 0.56f, 0.36f, 0.3f)
                    .Face(0.07f, cG, 0.08f, 0.24f)
                    .Mirror(Sph, gH, cG, V(0.04f, 0.67f, 0.46f), V(0.04f, 0.045f, 0.03f))
                    .Mirror(Sph, gH, cG, V(0.125f, 0.63f, 0.42f), V(0.035f, 0.04f, 0.03f))
                    .Mirror(Con, gH, cW, V(0.05f, 0.45f, 0.47f), V(0.04f, 0.11f, 0.04f), V(180, 0, 0))
                    // eight legs: out, up over a knee, down to the ground
                    .WhipPair(gFL, gFR, cD, V(0.15f, 0.5f, 0.22f), V(0.1f, 0.09f, 0.06f), V(0, -0.062f, 0), 8, 0.06f, 0.03f)
                    .WhipPair(gFL, gFR, cD, V(0.17f, 0.5f, 0.1f), V(0.11f, 0.09f, 0.015f), V(0, -0.062f, 0), 8, 0.06f, 0.03f)
                    .WhipPair(BodyGroup.LegBL, BodyGroup.LegBR, cD, V(0.17f, 0.5f, -0.02f), V(0.11f, 0.09f, -0.03f), V(0, -0.062f, 0), 8, 0.06f, 0.03f)
                    .WhipPair(BodyGroup.LegBL, BodyGroup.LegBR, cD, V(0.15f, 0.5f, -0.14f), V(0.1f, 0.09f, -0.07f), V(0, -0.062f, 0), 8, 0.06f, 0.03f)
                    .PivotPair(gFL, gFR, V(0.15f, 0.5f, 0.16f)).PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.15f, 0.5f, -0.08f))
                    // the doll on its strings, held out to one side
                    .Add(Bx, gX, cS, V(0.46f, 1.12f, 0.5f), V(0.3f, 0.03f, 0.04f))
                    .Add(Bx, gX, cS, V(0.46f, 1.12f, 0.5f), V(0.04f, 0.03f, 0.3f))
                    .Add(Cyl, gX, cW, V(0.34f, 0.92f, 0.5f), V(0.012f, 0.4f, 0.012f))
                    .Add(Cyl, gX, cW, V(0.58f, 0.92f, 0.5f), V(0.012f, 0.4f, 0.012f))
                    .Add(Cyl, gX, cW, V(0.46f, 1f, 0.5f), V(0.012f, 0.24f, 0.012f))
                    .Add(Sph, gX, cA, V(0.46f, 0.66f, 0.5f), V(0.15f, 0.19f, 0.13f))
                    .Add(Sph, gX, cS, V(0.46f, 0.81f, 0.5f), V(0.14f, 0.14f, 0.14f))
                    .Add(Sph, gX, cD, V(0.43f, 0.82f, 0.56f), V(0.03f, 0.03f, 0.02f))
                    .Add(Sph, gX, cD, V(0.49f, 0.82f, 0.56f), V(0.03f, 0.03f, 0.02f))
                    .Add(Cap, gX, cA, V(0.37f, 0.62f, 0.5f), V(0.04f, 0.15f, 0.04f), V(0, 0, -32))
                    .Add(Cap, gX, cA, V(0.55f, 0.62f, 0.5f), V(0.04f, 0.15f, 0.04f), V(0, 0, 32))
                    .Pivot(gH, V(0, 0.52f, 0.26f)).Pivot(gX, V(0.46f, 1.12f, 0.5f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 198 MORBEAK (plague doctor crow)
            {
                var d = K(b, 198, "morbeak", "Morbeak", Element.Shade, Element.Flora, Rarity.Uncommon, BiomeMask.Blight | BiomeMask.Swamp, Temperament.Docile, Locomotion.Biped, 0.9f, 'B',
                    "A crow in a bone mask that walks the Blightmoor with a satchel of vials. One colour mends and one colour rots, and it has never told anyone which is which.",
                    "Solemn, meticulous, bedside manner of a headstone.", A("tonic_toxin", "venom_spit", "night_slash"), "toxic_cloud",
                    Work((WorkType.Foraging, 3), (WorkType.Crafting, 2)), CompanionPerk.Mending, "pelt", "fiber",
                    Pal(Hex("#2b2b33"), Hex("#d9cdb0"), Hex("#a8323a"), Hex("#9aff6a")));
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Biped(0.3f, 0.34f, 0.42f, 0.32f, 0.08f, cP, cD, cD)
                    .Add(Con, gB, cP, V(0, 0.42f, 0), V(0.5f, 0.6f, 0.44f))
                    .Mirror(Sph, gAL, gAR, cD, V(0.24f, 0.34f, -0.03f), V(0.1f, 0.28f, 0.17f))
                    // the bone mask: long beak, round green lenses, under a wide flat hat
                    .Add(Sph, gH, cS, V(0, 0.82f, 0.1f), V(0.27f, 0.25f, 0.2f))
                    .Add(Con, gH, cS, V(0, 0.76f, 0.36f), V(0.12f, 0.44f, 0.12f), V(100, 0, 0))
                    .Mirror(Cyl, gH, cD, V(0.075f, 0.87f, 0.195f), V(0.11f, 0.03f, 0.11f), V(90, 0, 0))
                    .Mirror(Sph, gH, cG, V(0.075f, 0.87f, 0.21f), V(0.075f, 0.075f, 0.03f))
                    .Add(Cyl, gH, cD, V(0, 0.99f, 0.02f), V(0.54f, 0.025f, 0.54f))
                    .Add(Cyl, gH, cD, V(0, 1.06f, 0.02f), V(0.28f, 0.13f, 0.28f))
                    .Add(Cyl, gH, cA, V(0, 1.02f, 0.02f), V(0.29f, 0.04f, 0.29f))
                    // satchel on a strap, one vial of each
                    .Add(Bx, gB, cS, V(0.02f, 0.52f, 0.19f), V(0.04f, 0.42f, 0.03f), V(0, 0, 35))
                    .Add(Bx, gB, cS, V(0.2f, 0.34f, 0.17f), V(0.17f, 0.14f, 0.1f))
                    .Add(Cyl, gB, cG, V(0.16f, 0.45f, 0.17f), V(0.04f, 0.1f, 0.04f))
                    .Add(Cyl, gB, cA, V(0.24f, 0.45f, 0.17f), V(0.04f, 0.1f, 0.04f))
                    .Add(Sph, gT, cD, V(0, 0.3f, -0.24f), V(0.22f, 0.06f, 0.32f), V(20, 0, 0))
                    .Pivot(gT, V(0, 0.34f, -0.14f))
                    .Done(d);
            }
        }
    }
}
