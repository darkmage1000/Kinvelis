using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Early Ember kin (user request 2026-10-09). The Sunmeadow had no Ember kin at all (0 of 50), so the first fire
    /// type was a walk to the Badlands away. Three natives of the meadow (#185-187: two Commons and an Uncommon, all
    /// gentle, so they turn up in the starter zone too) plus two small Badlands kin that now wander in (Cindling,
    /// Emberkit). They Kindle at level 1-2: enough for a first Campfire and Smelter crew, while level 3-4 still
    /// belongs to the Badlands (see ContentSeed.Jobs).
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildEarlyEmber(Bundle b)
        {
            const BiomeMask L = BiomeMask.Meadow;

            // ---------------------------------------------------------------- 185 WICKIT
            {
                var d = K(b, 185, "wickit", "Wickit", Element.Ember, Element.Normal, Rarity.Common, L, Temperament.Docile, Locomotion.Hopper, 0.6f, 'B',
                    "A soft, waxy little kin with a flame that never goes out on its head. Meadow folk used to coax one home at dusk instead of lighting a lamp.",
                    "Sleepy, trusting, afraid of puddles.", A("ember_spit", "tackle"), "flame_dash",
                    Work((WorkType.Kindling, 1), (WorkType.Hauling, 1)), CompanionPerk.Glow, "cinder", "berry",
                    Pal(Hex("#f4e2b8"), Hex("#fff6e0"), Hex("#f0905a"), Hex("#ffb428")));
                Effects(d, AuraFx.Embers, ExtraAnim.Pulse);
                new Plan()
                    .Blob(0.31f, 0.5f, 0.52f, 0.46f)
                    .Add(Sph, gB, cS, V(0, 0.22f, 0.07f), V(0.38f, 0.28f, 0.34f))
                    // melted-wax drips for ears and a collar of wax round the wick
                    .Mirror(Sph, gB, cP, V(0.2f, 0.5f, -0.02f), V(0.15f, 0.2f, 0.12f), V(0, 0, -28))
                    .Add(Sph, gB, cS, V(0, 0.57f, 0f), V(0.2f, 0.08f, 0.2f))
                    .Face(0.1f, ColorSlot.Eye, 0.02f, 0.26f).Cheeks(cA)
                    .Add(Cyl, gB, cD, V(0, 0.64f, 0f), V(0.03f, 0.1f, 0.03f))
                    // the flame: a round base and a tapering tongue, bright yellow with a hotter orange tip
                    .Add(Sph, gX, cG, V(0, 0.74f, 0f), V(0.17f, 0.16f, 0.17f))
                    .Add(Con, gX, cG, V(0, 0.85f, 0f), V(0.15f, 0.22f, 0.15f))
                    .Add(Con, gX, cA, V(0, 0.94f, 0f), V(0.07f, 0.13f, 0.07f))
                    .Mirror(Sph, gAL, gAR, cP, V(0.25f, 0.24f, 0.1f), V(0.12f, 0.14f, 0.14f))
                    .PivotPair(gAL, gAR, V(0.22f, 0.28f, 0.06f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.13f, 0.05f, 0.06f), V(0.17f, 0.1f, 0.24f))
                    .PivotPair(gFL, gFR, V(0.13f, 0.1f, 0f))
                    .Pivot(gX, V(0, 0.68f, 0f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 186 HEARTHOG
            {
                var d = K(b, 186, "hearthog", "Hearthog", Element.Ember, Element.Stone, Rarity.Common, L, Temperament.Docile, Locomotion.Quadruped, 0.65f, 'T',
                    "A hedgehog whose quills are slow-burning coals. It banks them low to sleep and fans them bright when startled; farmers bake potatoes beside a drowsy one.",
                    "Grumpy when woken, generous with warmth.", A("ember_spit", "pebble_toss"), "flare_horn",
                    Work((WorkType.Kindling, 2), (WorkType.Quarrying, 1)), CompanionPerk.Warmth, "cinder", "stone",
                    Pal(Hex("#3c3038"), Hex("#d8b088"), Hex("#e8682a"), Hex("#ffc040")));
                Effects(d, AuraFx.Embers);
                new Plan()
                    .Quad(0.25f, 0.46f, 0.34f, 0.56f, 0.3f, 0.12f, 0.1f, cS, cS, cD, 0f, 0.02f)
                    .Shell(cP, 0.2f, 0.56f, 0.62f, 0.66f, -0.07f)
                    // coal quills: a bright row down the spine, dimmer rows either side
                    .Spikes(gB, cG, 0.45f, 0.14f, -0.32f, 5, 0.1f, 0.22f, 0.05f, -28f)
                    .Spikes(gB, cA, 0.4f, 0.12f, -0.3f, 4, 0.09f, 0.19f, 0.04f, -28f, 0.13f, -32f)
                    .Spikes(gB, cA, 0.31f, 0.06f, -0.28f, 3, 0.08f, 0.15f, 0.03f, -28f, 0.23f, -58f)
                    .Face(0.075f, ColorSlot.Eye, 0.12f, 0.27f).Snout(cS, 0.13f, 0.8f).Cheeks(cA)
                    .Ears(Sph, cP, 0.1f, 0.09f, 20f)
                    .Tail(cS, 0.22f, -0.3f, 0.1f, 0.06f, 0.03f, -0.01f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 187 FLICKADEE
            {
                var d = K(b, 187, "flickadee", "Flickadee", Element.Ember, Element.Gale, Rarity.Uncommon, L, Temperament.Curious, Locomotion.Flyer, 0.6f, 'F',
                    "A finch with a crest and tail of real flame. Flocks follow grass fires across the meadow, snapping up whatever runs from them - and now and then they start one.",
                    "Cheeky, restless, sings at sparks.", A("ember_spit", "seed_gust"), "flame_dash",
                    Work((WorkType.Kindling, 2), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "cinder", "pelt",
                    Pal(Hex("#e2502a"), Hex("#ffe0a0"), Hex("#ff9a30"), Hex("#fff070")));
                Effects(d, AuraFx.Embers);
                new Plan()
                    .Blob(0.7f, 0.3f, 0.3f, 0.42f)
                    .Add(Sph, gB, cS, V(0, 0.65f, 0.05f), V(0.23f, 0.21f, 0.3f))
                    .Add(Sph, gH, cP, V(0, 0.87f, 0.17f), V(0.27f, 0.26f, 0.27f)).Head(gH, 0.87f, 0.17f, 0.27f)
                    .Face(0.065f).Beak(cD, 0.08f, 0.14f).Cheeks(cS)
                    // flame crest swept back over the head
                    .Add(Con, gH, cA, V(0, 1.04f, 0.1f), V(0.12f, 0.22f, 0.1f), V(-28, 0, 0))
                    .Add(Con, gH, cG, V(0, 1.02f, 0.13f), V(0.07f, 0.15f, 0.06f), V(-28, 0, 0))
                    .Add(Con, gH, cA, V(0, 0.99f, 0.02f), V(0.09f, 0.16f, 0.08f), V(-52, 0, 0))
                    .Fins(cA, V(0.12f, 0.74f, 0f), 0.44f, 0.24f, 12, 0.2f)
                    .Mirror(Sph, gWL, gWR, cG, V(0.52f, 0.83f, -0.07f), V(0.16f, 0.035f, 0.13f), V(0, 0, 12))
                    // a tail that is a streak of fire
                    .Add(Con, gT, cA, V(0, 0.72f, -0.34f), V(0.17f, 0.34f, 0.1f), V(-104, 0, 0))
                    .Add(Con, gT, cG, V(0, 0.72f, -0.32f), V(0.1f, 0.24f, 0.07f), V(-104, 0, 0))
                    .Mirror(Sph, gB, cD, V(0.06f, 0.54f, 0.02f), V(0.04f, 0.07f, 0.09f))
                    .Pivot(gH, V(0, 0.79f, 0.11f)).Pivot(gT, V(0, 0.71f, -0.19f))
                    .Done(d);
            }
        }

        /// <summary>Two small, friendly Badlands fire kin also roam the Sunmeadow now (applied after every species is built).</summary>
        static void ApplyEarlyEmber(Bundle b)
        {
            foreach (var id in new[] { "cindling", "emberkit" })
            {
                var sp = b.species.Find(x => x.id == id);
                if (sp != null) sp.biomes |= BiomeMask.Meadow;
            }
        }
    }
}
