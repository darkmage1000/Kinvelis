using UnityEngine;

namespace Wildbond
{
    /// <summary>Styled kin, part 2: performers and makers (#199-202), and the night crowd (#203-206). See ContentSeed.Styled.</summary>
    public static partial class ContentSeed
    {
        static void BuildStyledB(Bundle b)
        {
            // ---------------------------------------------------------------- 199 LILUTE (bard frog)
            {
                var d = K(b, 199, "lilute", "Lilute", Element.Tide, Element.Lumen, Rarity.Uncommon, BiomeMask.Swamp | BiomeMask.Meadow, Temperament.Docile, Locomotion.Hopper, 0.75f, 'B',
                    "A frog with a lute it grew from a lily pad. When a Lilute plays at the edge of a pond the whole bank works faster, and nobody remembers deciding to.",
                    "Easygoing, vain about its voice, never off-key.", A("bubble_burst", "anthem", "tongue_lash"), "glow_pulse",
                    Work((WorkType.Irrigating, 2), (WorkType.Cultivating, 2)), CompanionPerk.Glow, "gel", "berry",
                    Pal(Hex("#4fb07a"), Hex("#e8f5c8"), Hex("#ff7fa8"), Hex("#ffe680")));
                Effects(d, AuraFx.Bubbles, ExtraAnim.Bob);
                new Plan()
                    .Blob(0.3f, 0.5f, 0.42f, 0.46f)
                    .Add(Sph, gB, cS, V(0, 0.24f, 0.08f), V(0.4f, 0.3f, 0.36f))
                    // eyes up on top, a wide mouth, a feathered cap between them
                    .Mirror(Sph, gB, cP, V(0.14f, 0.52f, 0.1f), V(0.17f, 0.17f, 0.17f))
                    .Mirror(Sph, gB, ColorSlot.Eye, V(0.14f, 0.54f, 0.17f), V(0.09f, 0.1f, 0.06f))
                    .Mirror(Sph, gB, cW, V(0.155f, 0.565f, 0.2f), V(0.03f, 0.03f, 0.03f))
                    .Add(Bx, gB, cD, V(0, 0.34f, 0.222f), V(0.2f, 0.014f, 0.02f))
                    .Cheeks(cA)
                    .Add(Sph, gB, cA, V(-0.03f, 0.56f, -0.06f), V(0.3f, 0.1f, 0.3f), V(0, 0, 10))
                    .Whip(gB, cW, V(-0.12f, 0.6f, -0.1f), V(-0.02f, 0.06f, -0.04f), V(0, -0.016f, 0), 5, 0.07f, 0.02f)
                    // the lily-pad lute across its belly
                    .Add(Sph, gB, cG, V(-0.09f, 0.2f, 0.25f), V(0.27f, 0.3f, 0.1f), V(0, 0, -35))
                    .Add(Cyl, gB, cD, V(-0.09f, 0.2f, 0.3f), V(0.09f, 0.012f, 0.09f), V(90, 0, 0))
                    .Add(Bx, gB, cD, V(0.09f, 0.45f, 0.25f), V(0.05f, 0.36f, 0.04f), V(0, 0, -35))
                    .Add(Sph, gB, cA, V(0.2f, 0.61f, 0.25f), V(0.1f, 0.1f, 0.06f))
                    .Mirror(Sph, gAL, gAR, cP, V(0.24f, 0.26f, 0.14f), V(0.1f, 0.15f, 0.12f), V(0, 0, -20))
                    .PivotPair(gAL, gAR, V(0.22f, 0.32f, 0.08f))
                    .Mirror(Sph, gFL, gFR, cP, V(0.17f, 0.05f, 0.1f), V(0.2f, 0.09f, 0.3f))
                    .PivotPair(gFL, gFR, V(0.15f, 0.1f, 0f))
                    // two notes hanging in the air
                    .Add(Sph, gX, cG, V(0.42f, 0.72f, 0f), V(0.08f, 0.07f, 0.06f))
                    .Add(Bx, gX, cG, V(0.455f, 0.81f, 0f), V(0.016f, 0.18f, 0.016f))
                    .Add(Sph, gX, cA, V(-0.4f, 0.86f, 0.1f), V(0.07f, 0.06f, 0.05f))
                    .Add(Bx, gX, cA, V(-0.37f, 0.94f, 0.1f), V(0.014f, 0.16f, 0.014f))
                    .Pivot(gX, V(0, 0.7f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 200 ENCORINE (idol bird)
            {
                var d = K(b, 200, "encorine", "Encorine", Element.Gale, Element.Lumen, Rarity.Rare, BiomeMask.Isles | BiomeMask.Glow, Temperament.Curious, Locomotion.Biped, 1f, 'F',
                    "It dances whether or not anyone is watching, but it knows when someone is. An Encorine never ends on its best number - it does that one again.",
                    "Radiant, tireless, lives for the second bow.", A("wind_ribbon", "encore", "glint_beam"), "cyclone",
                    Work((WorkType.Foraging, 2), (WorkType.Hauling, 2)), CompanionPerk.Swiftness, "pelt", "crystal",
                    Pal(Hex("#7fd8e8"), Hex("#fff4fa"), Hex("#ff6fb0"), Hex("#ffe36a")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Biped(0.46f, 0.24f, 0.34f, 0.3f, 0.055f, cP, cS, cA)
                    // feather skirt in two layers, a ruff at the chest
                    .Ring(Sph, gB, cA, V(0, 0.5f, 0), 0.2f, 8, V(0.15f, 0.24f, 0.05f), -58)
                    .Ring(Sph, gB, cW, V(0, 0.53f, 0), 0.15f, 8, V(0.13f, 0.2f, 0.05f), -42, 22)
                    .Add(Sph, gB, cS, V(0, 0.67f, 0.07f), V(0.2f, 0.16f, 0.14f))
                    .Beak(cG, 0.07f, 0.12f).Face(0.07f, ColorSlot.Eye, 0.08f, 0.26f).Cheeks(cA)
                    .Fan(Con, gH, cA, V(0, 1.03f, -0.02f), 0.09f, 3, V(0.06f, 0.26f, 0.04f), -32, 64)
                    .Add(Sph, gH, cG, V(0.1f, 1f, 0.11f), V(0.07f, 0.07f, 0.05f))
                    // wing-arms with long sleeves of feathers, leg warmers
                    .Mirror(Sph, gAL, gAR, cS, V(0.2f, 0.52f, -0.04f), V(0.06f, 0.36f, 0.2f), V(0, 0, -14))
                    .Mirror(Sph, gAL, gAR, cA, V(0.225f, 0.38f, -0.05f), V(0.05f, 0.17f, 0.14f), V(0, 0, -14))
                    .Mirror(Cyl, gFL, gFR, cA, V(0.062f, 0.12f, 0), V(0.085f, 0.12f, 0.085f))
                    // two long ribbons for a tail
                    .Whip(gT, cA, V(0.05f, 0.5f, -0.12f), V(0.022f, -0.01f, -0.1f), V(0, 0.012f, 0), 8, 0.07f, 0.03f)
                    .Whip(gT, cG, V(-0.05f, 0.5f, -0.12f), V(-0.022f, 0f, -0.1f), V(0, 0.014f, 0), 8, 0.07f, 0.03f)
                    .Pivot(gT, V(0, 0.5f, -0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 201 FLAMBEAR (chef bear)
            {
                var d = K(b, 201, "flambear", "Flambear", Element.Ember, Element.Normal, Rarity.Rare, BiomeMask.Badlands | BiomeMask.Desert, Temperament.Docile, Locomotion.Biped, 1.3f, 'T',
                    "A bear that learned to cook over the Badlands vents and never looked back. It will feed anyone who sits down politely, and set fire to anyone who touches the pan.",
                    "Warm, loud, deadly serious about seasoning.", A("flambe", "brace_bash", "ember_spit"), "lava_plume",
                    Work((WorkType.Kindling, 4), (WorkType.Hauling, 2)), CompanionPerk.Warmth, "pelt", "cinder",
                    Pal(Hex("#8a5a3a"), Hex("#e0c090"), Hex("#d83a2a"), Hex("#ffb030")));
                Effects(d, AuraFx.Embers);
                d.overworldRole += " Camp: the best cook there is.";
                new Plan()
                    .Biped(0.36f, 0.62f, 0.56f, 0.46f, 0.18f, cP, cP, cD)
                    // apron, neckerchief
                    .Add(Sph, gB, cW, V(0, 0.56f, 0.2f), V(0.46f, 0.52f, 0.22f))
                    .Add(Cyl, gB, cA, V(0, 0.9f, 0), V(0.4f, 0.07f, 0.36f))
                    .Add(Con, gB, cA, V(0, 0.84f, 0.22f), V(0.2f, 0.18f, 0.08f), V(180, 0, 0))
                    .Snout(cS, 0.2f, 0.7f).Ears(Sph, cP, 0.15f, 0.15f, 34f).Face(0.065f, ColorSlot.Eye, 0.1f, 0.22f)
                    // the toque
                    .Add(Cyl, gH, cW, V(0, 1.31f, 0), V(0.3f, 0.14f, 0.3f))
                    .Add(Sph, gH, cW, V(0, 1.47f, 0), V(0.42f, 0.3f, 0.42f))
                    .Mirror(Sph, gH, cW, V(0.13f, 1.45f, 0), V(0.26f, 0.24f, 0.3f))
                    // a wok on its back, a cleaver in one paw and a ladle in the other
                    .Add(Dom, gB, cD, V(0, 0.66f, -0.3f), V(0.54f, 0.24f, 0.54f), V(-90, 0, 0))
                    .Add(Bx, gB, cS, V(0, 1f, -0.34f), V(0.05f, 0.22f, 0.04f))
                    .Add(Bx, gAR, cW, V(0.43f, 0.42f, 0.2f), V(0.03f, 0.24f, 0.3f))
                    .Add(Bx, gAR, cS, V(0.43f, 0.44f, -0.01f), V(0.045f, 0.06f, 0.14f))
                    .Add(Cyl, gAL, cD, V(-0.43f, 0.5f, 0.12f), V(0.03f, 0.4f, 0.03f), V(40, 0, 0))
                    .Add(Sph, gAL, cD, V(-0.43f, 0.66f, 0.26f), V(0.13f, 0.08f, 0.13f))
                    .Add(Sph, gB, cP, V(0, 0.42f, -0.3f), V(0.15f, 0.15f, 0.15f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 202 FORGEMOLE (blacksmith mole)
            {
                var d = K(b, 202, "forgemole", "Forgemole", Element.Ember, Element.Stone, Rarity.Uncommon, BiomeMask.Badlands, Temperament.Docile, Locomotion.Biped, 0.85f, 'T',
                    "It digs to where the rock is still warm and sets up shop. The anvil on its back is never cold, and anything a Forgemole has struck once holds an edge for good.",
                    "Gruff, short-sighted, proud of honest work.", A("flare_horn", "temper", "pebble_toss"), "magma_maw",
                    Work((WorkType.Kindling, 4), (WorkType.Quarrying, 2), (WorkType.Crafting, 2)), CompanionPerk.OreSense, "cinder", "ore",
                    Pal(Hex("#4a3a36"), Hex("#b07a44"), Hex("#f09a9a"), Hex("#ffa028")));
                Effects(d, AuraFx.Embers);
                d.overworldRole += " Camp: born for the forge.";
                new Plan()
                    .Biped(0.2f, 0.46f, 0.4f, 0.38f, 0.14f, cP, cP, cD)
                    // leather apron
                    .Add(Bx, gB, cS, V(0, 0.34f, 0.2f), V(0.34f, 0.4f, 0.06f))
                    .Add(Cyl, gB, cS, V(0, 0.55f, 0), V(0.45f, 0.04f, 0.41f))
                    // long pink snout, tiny eyes, goggles pushed up on the brow
                    .Snout(cA, 0.15f, 1.1f).Face(0.035f, ColorSlot.Eye, 0.1f, 0.2f)
                    .Add(Cyl, gH, cD, V(0, 0.84f, 0.02f), V(0.39f, 0.035f, 0.37f))
                    .Mirror(Cyl, gH, cD, V(0.08f, 0.86f, 0.15f), V(0.12f, 0.04f, 0.12f), V(72, 0, 0))
                    .Mirror(Sph, gH, cG, V(0.08f, 0.868f, 0.165f), V(0.085f, 0.085f, 0.03f), V(-18, 0, 0))
                    // digging paws with three claws each
                    .Mirror(Sph, gAL, gAR, cS, V(0.3f, 0.22f, 0.07f), V(0.17f, 0.14f, 0.19f))
                    .Mirror(Con, gAL, gAR, cW, V(0.25f, 0.18f, 0.18f), V(0.045f, 0.14f, 0.045f), V(110, 0, 0))
                    .Mirror(Con, gAL, gAR, cW, V(0.3f, 0.18f, 0.19f), V(0.045f, 0.15f, 0.045f), V(110, 0, 0))
                    .Mirror(Con, gAL, gAR, cW, V(0.35f, 0.18f, 0.18f), V(0.045f, 0.14f, 0.045f), V(110, 0, 0))
                    // the anvil strapped to its back, an ingot glowing on it
                    .Add(Bx, gB, cD, V(0, 0.34f, -0.3f), V(0.28f, 0.07f, 0.2f))
                    .Add(Bx, gB, cD, V(0, 0.43f, -0.3f), V(0.16f, 0.12f, 0.14f))
                    .Add(Bx, gB, cD, V(0, 0.53f, -0.3f), V(0.32f, 0.1f, 0.2f))
                    .Add(Con, gB, cD, V(0.22f, 0.53f, -0.3f), V(0.1f, 0.16f, 0.1f), V(0, 0, -90))
                    .Add(Bx, gB, cG, V(-0.03f, 0.6f, -0.3f), V(0.17f, 0.05f, 0.08f))
                    // a tail that ends in a hammer
                    .Tail(cP, 0.22f, -0.2f, 0.3f, 0.09f, 0.07f, 0.07f)
                    .Tip(Bx, gT, cD, V(0.14f, 0.12f, 0.24f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 203 MORTAMOTH (reaper moth)
            {
                var d = K(b, 203, "mortamoth", "Mortamoth", Element.Shade, Element.Frost, Rarity.Rare, BiomeMask.Peaks | BiomeMask.Blight, Temperament.Territorial, Locomotion.Flyer, 1.1f, 'G',
                    "A moth that drifts down out of the snow in a hood of its own wings. It only comes for the weak and the wounded, and it is never in a hurry.",
                    "Quiet, gentle with the dying, unsettlingly polite.", A("reap", "night_slash", "icicle_spear"), "gloom_pulse",
                    Work((WorkType.Chilling, 3), (WorkType.Logging, 2)), CompanionPerk.LumberBoost, "fiber", "crystal",
                    Pal(Hex("#2a2440"), Hex("#e6e0d0"), Hex("#6fc0e8"), Hex("#b8f4ff")));
                Effects(d, AuraFx.Snow);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.72f, 0), V(0.3f, 0.5f, 0.28f))
                    .Add(Sph, gB, cS, V(0, 0.92f, 0), V(0.42f, 0.2f, 0.38f))
                    // a pale face in the shadow of a hood, antennae like sickles
                    .Add(Sph, gH, cS, V(0, 1.08f, 0.04f), V(0.3f, 0.28f, 0.28f)).Head(gH, 1.08f, 0.04f, 0.3f)
                    .Face(0.09f, cG, 0f, 0.26f)
                    .Add(Sph, gH, cP, V(0, 1.12f, -0.04f), V(0.39f, 0.37f, 0.36f))
                    .Add(Con, gH, cP, V(0, 1.31f, -0.1f), V(0.2f, 0.2f, 0.2f), V(-34, 0, 0))
                    .WhipPair(gH, gH, cD, V(0.08f, 1.27f, 0.07f), V(0.03f, 0.07f, 0.02f), V(0.014f, -0.03f, 0.014f), 6, 0.035f, 0.015f)
                    // wings worn like a ragged cloak, a pale eye on each
                    .Wings(cP, cP, cD, V(0.12f, 0.88f, -0.08f), 0.78f, 0.5f, 26f)
                    .Mirror(Sph, gWL, gWR, cA, V(0.34f, 0.56f, -0.1f), V(0.5f, 0.05f, 0.4f), V(0, 0, -28))
                    .Mirror(Sph, gWL, gWR, cG, V(0.57f, 1.1f, -0.08f), V(0.17f, 0.07f, 0.17f), V(0, 0, 26))
                    .PivotPair(gWL, gWR, V(0.12f, 0.88f, -0.08f))
                    // thin arms on the scythe
                    .Mirror(Cap, gAL, gAR, cD, V(0.17f, 0.74f, 0.1f), V(0.05f, 0.3f, 0.05f), V(40, 0, -10))
                    .Add(Cyl, gAR, cD, V(0.3f, 0.84f, 0.22f), V(0.035f, 1.1f, 0.035f), V(0, 0, -8))
                    .Add(Bx, gAR, cW, V(0.25f, 1.4f, 0.22f), V(0.28f, 0.075f, 0.02f), V(0, 0, 10))
                    .Add(Bx, gAR, cW, V(0.04f, 1.34f, 0.22f), V(0.2f, 0.06f, 0.02f), V(0, 0, 28))
                    .Add(Con, gAR, cW, V(-0.1f, 1.23f, 0.22f), V(0.05f, 0.2f, 0.02f), V(0, 0, 142))
                    .PivotPair(gAL, gAR, V(0.15f, 0.86f, 0.04f))
                    .Add(Sph, gT, cP, V(0, 0.42f, -0.06f), V(0.22f, 0.42f, 0.22f), V(15, 0, 0))
                    .Add(Sph, gT, cS, V(0, 0.36f, -0.07f), V(0.23f, 0.05f, 0.23f), V(15, 0, 0))
                    .Pivot(gH, V(0, 0.96f, 0)).Pivot(gT, V(0, 0.58f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 204 VESPERTINE (vampire bat noble)
            {
                var d = K(b, 204, "vespertine", "Vespertine", Element.Shade, Element.Tide, Rarity.Rare, BiomeMask.Blight | BiomeMask.Glow, Temperament.Aggressive, Locomotion.Biped, 1.05f, 'B',
                    "It wears its wings as a cloak and expects to be announced. A Vespertine takes only a little from each guest, which is how it has lived long enough to have this many manners.",
                    "Courtly, thirsty, offended by garlic.", A("blood_drain", "night_slash", "bubble_burst"), "shadow_pounce",
                    Work((WorkType.Chilling, 2), (WorkType.Hauling, 2)), CompanionPerk.Mending, "pelt", "gel",
                    Pal(Hex("#5a4a6a"), Hex("#d8c8d8"), Hex("#b01e3c"), Hex("#ff4a5a")));
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Biped(0.4f, 0.3f, 0.42f, 0.34f, 0.08f, cP, cD, cD)
                    // waistcoat, cravat with a jewel, a collar that stands up past its ears
                    .Add(Sph, gB, cA, V(0, 0.58f, 0.1f), V(0.21f, 0.32f, 0.12f))
                    .Add(Sph, gB, cW, V(0, 0.75f, 0.13f), V(0.13f, 0.1f, 0.06f))
                    .Add(Sph, gB, cG, V(0, 0.76f, 0.165f), V(0.04f, 0.04f, 0.04f))
                    .Mirror(Con, gB, cD, V(0.15f, 0.88f, -0.06f), V(0.15f, 0.32f, 0.06f), V(0, 0, -16))
                    .Mirror(Con, gB, cA, V(0.14f, 0.87f, -0.04f), V(0.11f, 0.27f, 0.05f), V(0, 0, -16))
                    // wings folded down as a cloak with a red lining
                    .Mirror(Sph, gWL, gWR, cD, V(0.21f, 0.5f, -0.1f), V(0.27f, 0.72f, 0.1f), V(0, 12, -6))
                    .Mirror(Sph, gWL, gWR, cA, V(0.2f, 0.5f, -0.085f), V(0.2f, 0.63f, 0.08f), V(0, 12, -6))
                    .Mirror(Con, gWL, gWR, cW, V(0.3f, 0.9f, -0.1f), V(0.04f, 0.13f, 0.04f))
                    .PivotPair(gWL, gWR, V(0.14f, 0.84f, -0.08f))
                    // big ears, a pushed-up nose, fangs, hair slicked back
                    .Ears(Con, cP, 0.17f, 0.36f, 20f)
                    .Mirror(Con, gH, cA, V(0.105f, 1.19f, 0.035f), V(0.09f, 0.25f, 0.04f), V(-8, 0, -20))
                    .Snout(cS, 0.1f, 0.5f).Face(0.07f, cG, 0.1f, 0.25f).Brows(cD, 0.1f, 0.25f)
                    .Mirror(Con, gH, cW, V(0.035f, 0.845f, 0.18f), V(0.03f, 0.075f, 0.03f), V(180, 0, 0))
                    .Add(Sph, gH, cD, V(0, 1.06f, -0.04f), V(0.31f, 0.17f, 0.3f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 205 ARMADRAW (gunslinger armadillo)
            {
                var d = K(b, 205, "armadraw", "Armadraw", Element.Stone, Element.Volt, Rarity.Uncommon, BiomeMask.Desert | BiomeMask.Badlands, Temperament.Territorial, Locomotion.Biped, 0.85f, 'B',
                    "It stands in the road at noon with its hat pulled low and a spark sitting ready in its paw. Nobody has ever seen an Armadraw fire second.",
                    "Laconic, squints at everything, a soft touch underneath.", A("quickdraw", "static_dart", "sand_kick"), "roll_crush",
                    Work((WorkType.Charging, 2), (WorkType.Quarrying, 2)), CompanionPerk.OreSense, "stone", "ore",
                    Pal(Hex("#8a7a68"), Hex("#e0c8a0"), Hex("#c8502a"), Hex("#ffe040")));
                Effects(d, AuraFx.Sparks);
                new Plan()
                    .Biped(0.24f, 0.42f, 0.38f, 0.34f, 0.12f, cS, cS, cD)
                    // banded shell down the back
                    .Add(Sph, gB, cP, V(0, 0.44f, -0.12f), V(0.5f, 0.52f, 0.34f))
                    .Add(Sph, gB, cD, V(0, 0.52f, -0.14f), V(0.51f, 0.03f, 0.34f))
                    .Add(Sph, gB, cD, V(0, 0.42f, -0.14f), V(0.52f, 0.03f, 0.35f))
                    .Add(Sph, gB, cD, V(0, 0.32f, -0.14f), V(0.5f, 0.03f, 0.33f))
                    // poncho with a stripe
                    .Add(Con, gB, cA, V(0, 0.52f, 0.01f), V(0.62f, 0.34f, 0.5f))
                    .Add(Cyl, gB, cG, V(0, 0.42f, 0.01f), V(0.47f, 0.03f, 0.38f))
                    .Snout(cS, 0.13f, 1.2f).Ears(Con, cS, 0.08f, 0.18f, 30f).Face(0.055f, ColorSlot.Eye, 0.1f, 0.24f).Brows(cD, 0.1f, 0.24f)
                    // wide hat pulled low
                    .Add(Sph, gH, cP, V(0, 0.9f, 0.03f), V(0.68f, 0.05f, 0.62f), V(-6, 0, 0))
                    .Add(Cyl, gH, cP, V(0, 0.97f, 0.02f), V(0.26f, 0.14f, 0.26f))
                    .Add(Cyl, gH, cA, V(0, 0.93f, 0.02f), V(0.27f, 0.04f, 0.27f))
                    // the spark-shooter in its right paw, a holster on the left hip
                    .Add(Cyl, gAR, cD, V(0.3f, 0.3f, 0.17f), V(0.06f, 0.24f, 0.06f), V(90, 0, 0))
                    .Add(Bx, gAR, cD, V(0.3f, 0.24f, 0.07f), V(0.05f, 0.12f, 0.06f))
                    .Add(Sph, gAR, cG, V(0.3f, 0.3f, 0.3f), V(0.06f, 0.06f, 0.06f))
                    .Add(Bx, gB, cD, V(-0.27f, 0.26f, 0.03f), V(0.08f, 0.16f, 0.1f))
                    .Tail(cP, 0.26f, -0.26f, 0.3f, 0.1f, 0.03f, -0.02f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 206 CUTLOTTER (pirate otter)
            {
                var d = K(b, 206, "cutlotter", "Cutlotter", Element.Tide, Element.Stone, Rarity.Uncommon, BiomeMask.Isles | BiomeMask.Swamp, Temperament.Curious, Locomotion.Biped, 0.8f, 'F',
                    "An otter with an eyepatch it does not need and a hook it does. Whatever a Cutlotter hits loses something, and the otter is usually wearing it by evening.",
                    "Cheerful, light-fingered, loyal to a fault.", A("plunder", "aqua_tail", "pebble_toss"), "riptide",
                    Work((WorkType.Hauling, 3), (WorkType.Irrigating, 1)), CompanionPerk.ForageBoost, "gel", "ore",
                    Pal(Hex("#7a5238"), Hex("#ecd8b0"), Hex("#c8323c"), Hex("#ffd060")));
                Effects(d, AuraFx.Bubbles);
                new Plan()
                    .Biped(0.26f, 0.3f, 0.44f, 0.32f, 0.1f, cP, cP, cD)
                    .Add(Sph, gB, cS, V(0, 0.44f, 0.07f), V(0.22f, 0.36f, 0.2f))
                    .Add(Cyl, gB, cA, V(0, 0.33f, 0), V(0.33f, 0.07f, 0.3f))
                    .Add(Bx, gB, cG, V(0, 0.33f, 0.15f), V(0.08f, 0.07f, 0.03f))
                    .Snout(cS, 0.16f, 0.6f).Ears(Sph, cP, 0.07f, 0.07f, 40f).Face(0.06f, ColorSlot.Eye, 0.1f, 0.24f)
                    .Mirror(Bx, gH, cW, V(0.15f, 0.77f, 0.2f), V(0.14f, 0.01f, 0.01f), V(0, -15, 6))
                    // eyepatch and strap
                    .Add(Sph, gH, cD, V(-0.077f, 0.845f, 0.155f), V(0.1f, 0.1f, 0.05f))
                    .Add(Cyl, gH, cD, V(0, 0.87f, 0.02f), V(0.33f, 0.025f, 0.31f), V(0, 0, -18))
                    // bandana under a tricorn
                    .Add(Sph, gH, cA, V(0, 0.93f, 0f), V(0.33f, 0.14f, 0.32f))
                    .Add(Sph, gH, cD, V(0, 0.99f, 0f), V(0.52f, 0.05f, 0.46f))
                    .Add(Sph, gH, cD, V(0, 1.03f, 0f), V(0.28f, 0.14f, 0.26f))
                    .Mirror(Bx, gH, cD, V(0.21f, 1.04f, 0f), V(0.03f, 0.12f, 0.36f), V(0, 0, 25))
                    .Add(Sph, gH, cW, V(0, 1.04f, 0.13f), V(0.07f, 0.07f, 0.04f))
                    // a hook for a left paw, a cutlass in the right
                    .Add(Cyl, gAL, cD, V(-0.2f, 0.28f, 0.03f), V(0.085f, 0.05f, 0.085f))
                    .Whip(gAL, cW, V(-0.2f, 0.24f, 0.03f), V(0, -0.035f, 0.01f), V(0, 0.013f, 0.013f), 5, 0.035f, 0.02f)
                    .Add(Bx, gAR, cW, V(0.22f, 0.37f, 0.28f), V(0.02f, 0.07f, 0.4f), V(-20, 0, 0))
                    .Add(Sph, gAR, cG, V(0.22f, 0.3f, 0.08f), V(0.06f, 0.1f, 0.08f))
                    .Tail(cP, 0.26f, -0.14f, 0.5f, 0.14f, 0.05f, -0.01f)
                    .Done(d);
            }
        }
    }
}
