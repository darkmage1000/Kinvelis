using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Legend stats: well above an Epic, and never part of the wild spawn tables (LegendHunts brings them out).</summary>
        static void Legend(SpeciesDef d, int hp, int atk, int def)
        {
            d.baseHp = hp; d.baseAtk = atk; d.baseDef = def;
            d.spawnWeight = 0f; d.minGroup = 1; d.maxGroup = 1;
        }

        /// <summary>
        /// The five hunted legendaries (#180-184), one per outer land. They never spawn on their own: Maro the Seeker's
        /// hunts (World/LegendHunts.cs) lead you clue by clue to each one's lair. Stormcrest (#041) is the sixth
        /// legendary and belongs to the Warden journey, so it no longer roams the Peaks either.
        /// </summary>
        static void LegendSeed(Bundle b, string id, string name, string legend, string col, string desc, bool shimmer = false)
        {
            var d = Item(b, id, name, ItemCategory.CaptureSeed, "Ls", Hex(col), desc, 10);
            d.captureMult = 2.4f;        // rated for any level, like a Starbloom
            d.legendFor = legend;
            d.shimmerSeed = shimmer;
        }

        static void BuildLegendSeeds(Bundle b)
        {
            const string rule = " It binds nothing else, works at any level, and once the legend is at 10% health or less it cannot fail (ordinary seeds top out near 55% there). Readied for you when the legend appears.";
            LegendSeed(b, "moonpearl_seed", "Moonpearl Seed", "marelune", "#c8e0ff", "Bound from the pearls Marelune left on the sand." + rule);
            LegendSeed(b, "lanternfruit_seed", "Lanternfruit Seed", "oldroot", "#ffd070", "Grown round a pip of Oldroot's lantern fruit." + rule);
            LegendSeed(b, "emberglass_seed", "Emberglass Seed", "solphyre", "#ff8a3a", "Cast from the glass and ash of Solphyre's nest." + rule);
            LegendSeed(b, "blightscale_seed", "Blightscale Seed", "noxaris", "#b8ff40", "Wrapped in a shed scale of Noxaris." + rule);
            LegendSeed(b, "starmane_seed", "Starmane Seed", "kiralux", "#a0f4ff", "Tied with a strand of Kiralux's mane." + rule);
            // the Kraken's own (CaptureSystem.BossSeedHp): the only way any boss can be bound
            LegendSeed(b, "deepcall_seed", "Deepcall Seed", "krakolos", "#7a5ad0",
                "A black pearl wound in kelp. It answers only to Krakolos, the Kraken, and cannot take hold while the Kraken is above 15% health - below that it binds three times in four, and more surely the weaker it is. A seed that slides off comes back to you. Readied for you when the fight begins.");
            Recipe(b, "deepcall_seed", 1, CraftStation.Workbench, 40, 12, ("sea_pearl", 3), ("warden_heart", 1), ("crystal", 2));
            LegendSeed(b, "prismatic_seed", "Prismatic Seed", "*", "#ffb0f0",
                "Maro's masterwork. It works on any hunted legend like that legend's own seed - certain at 10% health or less - and the legend caught with it comes out a shimmer. " +
                "If a throw misses or the legend breaks free, it returns to your hand.", true);
        }

        static void BuildLegends(Bundle b)
        {
            BuildLegendSeeds(b);
            // ---------------------------------------------------------------- 041 STORMCREST (Journey finale): rebuilt to match the hunted legends
            var storm = b.species.Find(x => x.id == "stormcrest");
            if (storm != null)
            {
                storm.spawnWeight = 0f;
                storm.size = 2.2f;
                Effects(storm, AuraFx.Sparks, ExtraAnim.Orbit);
                var p = new Plan()
                    .Add(Sph, gB, cP, V(0, 1.2f, -0.05f), V(0.7f, 0.74f, 1.0f), V(-20, 0, 0))
                    .Add(Sph, gB, cS, V(0, 1.1f, 0.2f), V(0.54f, 0.6f, 0.56f))
                    .Add(Sph, gB, cG, V(0, 1.16f, 0.46f), V(0.16f, 0.24f, 0.08f))
                    .Ring(Sph, gB, cS, V(0, 1.5f, 0.22f), 0.28f, 8, V(0.28f, 0.24f, 0.28f))
                    .Add(Sph, gH, cP, V(0, 1.8f, 0.36f), V(0.46f, 0.44f, 0.5f)).Head(gH, 1.8f, 0.36f, 0.46f)
                    .Face(0.07f, cG).Brows(cA)
                    .Beak(cA, 0.16f, 0.36f)
                    .Add(Con, gH, cA, V(0, 1.66f, 0.8f), V(0.08f, 0.14f, 0.08f), V(180, 0, 0))
                    // lightning-bolt crest
                    .Add(Bx, gH, cG, V(0, 2.12f, 0.3f), V(0.07f, 0.36f, 0.14f), V(-22, 0, 0))
                    .Add(Bx, gH, cG, V(0, 2.4f, 0.14f), V(0.07f, 0.36f, 0.13f), V(36, 0, 0))
                    .Add(Bx, gH, cG, V(0, 2.68f, 0.22f), V(0.06f, 0.34f, 0.11f), V(-26, 0, 0))
                    .Mirror(Con, gH, cA, V(0.16f, 2.02f, 0.2f), V(0.07f, 0.4f, 0.12f), V(-30, 0, -28))
                    .Mirror(Con, gH, cA, V(0.24f, 1.92f, 0.08f), V(0.06f, 0.32f, 0.1f), V(-50, 0, -46))
                    // two layers of wing: storm-blue over white, gold and lightning tips
                    .Wings(cP, cP, cA, V(0.3f, 1.42f, 0f), 2.3f, 1.05f, 36)
                    .Wings(cS, cS, cG, V(0.3f, 1.34f, -0.26f), 1.6f, 0.8f, 16)
                    .Mirror(Bx, gWL, gWR, cG, V(1.2f, 2.12f, 0.36f), V(1.0f, 0.06f, 0.09f), V(0, 0, 36))
                    .Mirror(Bx, gWL, gWR, cG, V(1.86f, 2.6f, 0.26f), V(0.7f, 0.06f, 0.08f), V(0, -16, 36))
                    .Mirror(Cyl, gB, cA, V(0.17f, 0.72f, 0.05f), V(0.09f, 0.4f, 0.09f))
                    .Mirror(Con, gB, cD, V(0.17f, 0.5f, 0.15f), V(0.09f, 0.22f, 0.09f), V(140, 0, 0))
                    .Mirror(Con, gB, cD, V(0.17f, 0.5f, -0.04f), V(0.08f, 0.2f, 0.08f), V(215, 0, 0));
                for (int i = -3; i <= 3; i++)
                {
                    float len = 1.3f - Mathf.Abs(i) * 0.14f;
                    var dir = Quaternion.Euler(-8f, i * 13f, 0) * Vector3.back;
                    p.Add(Sph, gT, i % 2 == 0 ? cP : cS, V(0, 1.0f, -0.5f) + dir * (len * 0.5f), V(0.22f, 0.05f, len), V(-8f, i * 13f, 0));
                    p.Add(Con, gT, cG, V(0, 1.0f, -0.5f) + dir * (len + 0.05f), V(0.1f, 0.3f, 0.05f), V(-98f, i * 13f, 0));
                }
                p.Ring(Sph, gX, cS, V(0, 1.4f, -0.1f), 1.8f, 3, V(0.5f, 0.32f, 0.5f))
                    .Ring(Sph, gX, cS, V(0, 1.5f, -0.1f), 1.6f, 3, V(0.34f, 0.26f, 0.34f), 0, 18)
                    .Ring(Cry, gX, cG, V(0, 1.3f, -0.1f), 1.75f, 3, V(0.14f, 0.6f, 0.14f), 160, 8)
                    .Ring(Sph, gX, cG, V(0, 2.3f, -0.1f), 1.0f, 4, V(0.1f, 0.1f, 0.1f), 0, 45)
                    .PivotPair(gWL, gWR, V(0.3f, 1.42f, 0)).Pivot(gH, V(0, 1.58f, 0.26f)).Pivot(gT, V(0, 1.0f, -0.46f)).Pivot(gX, V(0, 1.4f, -0.1f))
                    .Done(storm);
            }

            // ---------------------------------------------------------------- 180 MARELUNE (Isles, surf mount)
            {
                var d = K(b, 180, "marelune", "Marelune", Element.Tide, Element.Lumen, Rarity.Legendary, BiomeMask.Isles, Temperament.Docile, Locomotion.Flyer, 2.3f, 'B',
                    "The moon-tide. Islanders say the lagoon rises and falls because Marelune breathes, and that it carries the drowned moon on its brow so the sea is never dark.",
                    "Vast, gentle, older than the reef.", A("wave_crash", "glow_pulse"), "aurora_veil",
                    Work((WorkType.Irrigating, 5), (WorkType.Charging, 3), (WorkType.Hauling, 3)), CompanionPerk.Mending, "gel", "crystal",
                    Pal(Hex("#2a4a8a"), Hex("#c8e0ff"), Hex("#7ab0ff"), Hex("#fff4c0")));
                Legend(d, 215, 28, 24);
                d.nearWater = true;
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                Mount(d, 13.5f, 9f, V(0, 1.36f, -0.34f));
                new Plan()
                    .Serpent(cP, cS, 1.0f, 0.74f, 11, 0.36f)
                    .Add(Sph, gH, cP, V(0, 1.1f, 0.4f), V(0.8f, 0.72f, 0.86f)).Head(gH, 1.1f, 0.4f, 0.8f)
                    .Add(Sph, gH, cS, V(0, 0.92f, 0.6f), V(0.6f, 0.34f, 0.6f))
                    .Face(0.09f, cG).Cheeks(cA)
                    // the crescent crown, with the drowned moon held inside it
                    .WhipPair(gH, gH, cG, V(0.14f, 1.44f, 0.36f), V(0.14f, 0.07f, -0.01f), V(-0.046f, 0.016f, 0), 9, 0.14f, 0.04f)
                    .Add(Sph, gH, cG, V(0, 1.98f, 0.34f), V(0.36f, 0.36f, 0.16f))
                    .Fan(Con, gH, cW, V(0, 1.98f, 0.3f), 0.3f, 8, V(0.05f, 0.16f, 0.03f), 22.5f)
                    .Mirror(Con, gH, cG, V(0.3f, 1.34f, 0.2f), V(0.08f, 0.34f, 0.06f), V(-30, 0, -50))
                    // long glowing barbels
                    .WhipPair(gH, gH, cG, V(0.26f, 0.9f, 0.76f), V(0.07f, -0.03f, -0.06f), V(0.004f, -0.006f, -0.012f), 12, 0.06f, 0.03f, cA)
                    // great fins, a second pair behind, and a crystal sail down the back
                    .Wings(cA, cA, cG, V(0.34f, 0.9f, -0.1f), 1.3f, 0.6f, -8)
                    .Mirror(Sph, gB, cA, V(0.6f, 0.86f, -1.3f), V(0.7f, 0.05f, 0.34f), V(0, -30, -14))
                    .Mirror(Con, gB, cG, V(0.92f, 0.8f, -1.44f), V(0.05f, 0.3f, 0.12f), V(0, -30, -104))
                    .Spikes(gB, cG, 1.14f, -0.9f, -1.6f, 4, 0.11f, 0.56f, 0.02f, -32, 0, 0, Cry)   // the sail starts behind the rider
                    .Spikes(gT, cG, 0.98f, -1.9f, -3.2f, 5, 0.08f, 0.3f, 0, -32, 0, 0, Cry)
                    .Mirror(Sph, gB, cG, V(0.33f, 1.12f, -0.3f), V(0.09f, 0.09f, 0.09f))
                    .Mirror(Sph, gB, cG, V(0.3f, 1.06f, -0.74f), V(0.08f, 0.08f, 0.08f))
                    .Mirror(Sph, gB, cG, V(0.27f, 1.0f, -1.16f), V(0.07f, 0.07f, 0.07f))
                    .Mirror(Sph, gT, cG, V(0.2f, 0.94f, -2.0f), V(0.06f, 0.06f, 0.06f))
                    // crescent tail
                    .WhipPair(gT, gT, cG, V(0.06f, 0.82f, -3.7f), V(0.1f, 0.02f, -0.08f), V(0.012f, 0.012f, 0.034f), 7, 0.16f, 0.04f)
                    .Ring(Sph, gX, cG, V(0, 1.3f, -0.8f), 1.5f, 6, V(0.16f, 0.16f, 0.16f))
                    .Ring(Sph, gX, cW, V(0, 1.9f, -0.8f), 1.0f, 4, V(0.09f, 0.09f, 0.09f), 0, 45)
                    .PivotPair(gWL, gWR, V(0.34f, 0.9f, -0.1f)).Pivot(gH, V(0, 1.0f, 0.1f)).Pivot(gX, V(0, 1.3f, -0.8f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 181 OLDROOT (Swamp)
            {
                var d = K(b, 181, "oldroot", "Oldroot", Element.Flora, Element.Stone, Rarity.Legendary, BiomeMask.Swamp, Temperament.Territorial, Locomotion.Quadruped, 2.4f, 'T',
                    "The first tree of the Mirefen, which got tired of standing. Its antlers are two whole canopies hung with lantern fruit, and the fen grows wherever it has walked.",
                    "Slow, watchful, forgives nothing done to the fen.", A("root_ram", "quake_stomp"), "petal_storm",
                    Work((WorkType.Cultivating, 5), (WorkType.Logging, 4), (WorkType.Hauling, 3)), CompanionPerk.LumberBoost, "wood", "crystal",
                    Pal(Hex("#6a5a44"), Hex("#4a3a2c"), Hex("#4a9a4a"), Hex("#ffd070")));
                Legend(d, 240, 27, 28);
                Effects(d, AuraFx.Petals, ExtraAnim.Orbit);
                new Plan()
                    .Quad(1.05f, 1.0f, 0.9f, 1.6f, 0.56f, 0.8f, 0.3f, cP, cS, cD, 0.42f, 0.18f)
                    .Add(Cap, gH, cP, V(0, 1.3f, 0.84f), V(0.42f, 0.76f, 0.44f), V(30, 0, 0))
                    .Add(Sph, gB, cP, V(0, 1.2f, 0.5f), V(1.04f, 0.9f, 0.8f))
                    .Mirror(Rok, gB, cS, V(0.5f, 1.34f, 0.5f), V(0.5f, 0.5f, 0.7f))
                    .Mirror(Rok, gB, cS, V(0.46f, 1.24f, -0.5f), V(0.44f, 0.44f, 0.6f))
                    .Add(Sph, gB, cA, V(0, 1.5f, -0.1f), V(0.96f, 0.36f, 1.5f))
                    // a little grove riding on its back
                    .Add(Cyl, gB, cS, V(0, 2.0f, -0.1f), V(0.12f, 0.8f, 0.12f)).Add(Sph, gB, cA, V(0, 2.56f, -0.1f), V(0.8f, 0.6f, 0.8f))
                    .Add(Cyl, gB, cS, V(0.26f, 1.86f, -0.56f), V(0.09f, 0.56f, 0.09f), V(0, 0, -12)).Add(Sph, gB, cA, V(0.34f, 2.26f, -0.56f), V(0.56f, 0.44f, 0.56f))
                    .Add(Cyl, gB, cS, V(-0.28f, 1.84f, 0.3f), V(0.09f, 0.5f, 0.09f), V(0, 0, 12)).Add(Sph, gB, cA, V(-0.36f, 2.2f, 0.3f), V(0.5f, 0.4f, 0.5f))
                    .Ring(Sph, gB, cG, V(0, 2.44f, -0.1f), 0.42f, 5, V(0.11f, 0.13f, 0.11f))
                    .Face(0.07f, cG).Snout(cS, 0.28f, 0.9f).Brows(cD)
                    .Whip(gH, cA, V(0, 1.2f, 1.24f), V(0, -0.09f, -0.01f), V(0, 0, -0.004f), 5, 0.22f, 0.06f)
                    .Ears(Sph, cP, 0.12f, 0.24f, 62)
                    // antlers: each one a tree, three canopies high, hung with lantern fruit and trailing vines
                    .WhipPair(gH, gH, cS, V(0.16f, 1.8f, 1.0f), V(0.1f, 0.14f, -0.03f), V(0.008f, 0.002f, -0.004f), 8, 0.17f, 0.08f)
                    .TipPair(Sph, gH, gH, cA, V(0.9f, 0.6f, 0.9f))
                    .Mirror(Cap, gH, cS, V(0.36f, 2.34f, 0.9f), V(0.09f, 0.5f, 0.09f), V(0, 0, 30))
                    .Mirror(Sph, gH, cA, V(0.24f, 2.64f, 0.9f), V(0.5f, 0.38f, 0.5f))
                    .Mirror(Cap, gH, cS, V(0.9f, 2.4f, 0.84f), V(0.08f, 0.5f, 0.08f), V(0, 0, -62))
                    .Mirror(Sph, gH, cA, V(1.18f, 2.54f, 0.84f), V(0.54f, 0.4f, 0.54f))
                    .Mirror(Sph, gH, cG, V(1.02f, 2.66f, 1.02f), V(0.13f, 0.15f, 0.13f))
                    .Mirror(Sph, gH, cG, V(0.5f, 2.4f, 1.06f), V(0.12f, 0.14f, 0.12f))
                    .Mirror(Sph, gH, cG, V(1.26f, 2.3f, 0.92f), V(0.11f, 0.13f, 0.11f))
                    .Mirror(Sph, gH, cG, V(0.2f, 2.4f, 1.04f), V(0.1f, 0.12f, 0.1f))
                    .WhipPair(gH, gH, cA, V(1.24f, 2.4f, 0.74f), V(0.01f, -0.1f, -0.01f), V(0, 0, 0), 5, 0.05f, 0.03f)
                    .TipPair(Sph, gH, gH, cG, V(0.12f, 0.14f, 0.12f))
                    .WhipPair(gH, gH, cA, V(0.76f, 2.9f, 0.6f), V(0.0f, -0.1f, -0.02f), V(0, 0, 0), 6, 0.05f, 0.03f)
                    .TipPair(Sph, gH, gH, cG, V(0.12f, 0.14f, 0.12f))
                    .WhipPair(gB, gB, cS, V(0.46f, 0.95f, -0.2f), V(0.09f, -0.07f, 0), V(-0.004f, -0.02f, 0), 6, 0.12f, 0.05f)
                    .WhipPair(gB, gB, cS, V(0.44f, 0.95f, -0.62f), V(0.08f, -0.07f, -0.04f), V(-0.004f, -0.02f, 0), 6, 0.11f, 0.05f)
                    .Tail(cS, 1.05f, -0.82f, 0.6f, 0.14f, 0.06f, -0.04f, cA, 0.3f)
                    .Ring(Sph, gX, cG, V(0, 2.0f, 0.2f), 1.6f, 5, V(0.12f, 0.14f, 0.12f))
                    .Ring(Sph, gX, cA, V(0, 1.2f, 0.2f), 1.4f, 4, V(0.2f, 0.04f, 0.12f), 0, 30)
                    .Pivot(gX, V(0, 2.0f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 182 SOLPHYRE (Desert)
            {
                var d = K(b, 182, "solphyre", "Solphyre", Element.Ember, Element.Gale, Rarity.Legendary, BiomeMask.Desert, Temperament.Territorial, Locomotion.Flyer, 2.2f, 'G',
                    "The sun-bird of the Sunscour. It burns down to a single ember every dusk and is whole again by dawn, and the dunes are said to be the ash of all its yesterdays.",
                    "Radiant, proud, impossible to look at for long.", A("lava_plume", "sky_dive"), "solar_flare",
                    Work((WorkType.Kindling, 5), (WorkType.Charging, 3), (WorkType.Hauling, 3)), CompanionPerk.Warmth, "cinder", "crystal",
                    Pal(Hex("#e8602a"), Hex("#ffd070"), Hex("#c8302a"), Hex("#fff0a0")));
                Legend(d, 200, 33, 21);
                Effects(d, AuraFx.Embers, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 1.2f, -0.05f), V(0.6f, 0.66f, 0.95f), V(-18, 0, 0))
                    .Add(Sph, gB, cS, V(0, 1.1f, 0.18f), V(0.46f, 0.52f, 0.54f))
                    .Add(Sph, gB, cG, V(0, 1.2f, 0.42f), V(0.16f, 0.2f, 0.1f))
                    .Whip(gH, cP, V(0, 1.4f, 0.3f), V(0, 0.11f, 0.07f), V(0, 0.004f, -0.006f), 5, 0.24f, 0.15f, cA)
                    .TipHead(gH, cP, 0.34f, 0.06f, cG).Beak(cD, 0.11f, 0.3f).Brows(cD)
                    // a crown of flame
                    .Fan(Con, gH, cG, V(0, 2.06f, 0.5f), 0.22f, 5, V(0.1f, 0.56f, 0.06f), -52, 104)
                    .Fan(Con, gH, cA, V(0, 2.04f, 0.44f), 0.2f, 4, V(0.08f, 0.4f, 0.05f), -38, 76)
                    // the sun behind it
                    .Add(Sph, gB, cG, V(0, 1.6f, -0.34f), V(0.9f, 0.9f, 0.05f))
                    .Fan(Con, gB, cG, V(0, 1.6f, -0.36f), 0.78f, 12, V(0.14f, 0.7f, 0.04f))
                    .Fan(Con, gB, cS, V(0, 1.6f, -0.38f), 0.66f, 12, V(0.1f, 0.4f, 0.04f), 15)
                    // five burning orbs circle it (CreatureActor.Orbs gives them their flames)
                    .Ring(Sph, gX, cG, V(0, 1.4f, 0), 1.4f, 5, V(0.18f, 0.22f, 0.18f))
                    .Ring(Sph, gX, cW, V(0, 2.1f, 0), 0.9f, 3, V(0.1f, 0.1f, 0.1f), 0, 60)
                    // three layers of burning wing
                    .Wings(cP, cA, cG, V(0.24f, 1.36f, 0f), 2.1f, 0.95f, 42)
                    .Wings(cA, cS, cG, V(0.24f, 1.3f, -0.2f), 1.5f, 0.76f, 22)
                    .Wings(cS, cG, cW, V(0.22f, 1.24f, -0.34f), 1.0f, 0.56f, 2)
                    .Mirror(Cyl, gB, cD, V(0.14f, 0.82f, 0.02f), V(0.07f, 0.3f, 0.07f))
                    .Mirror(Con, gB, cD, V(0.14f, 0.64f, 0.1f), V(0.07f, 0.2f, 0.07f), V(140, 0, 0))
                    // five tail plumes, each ending in a flame
                    .WhipPair(gT, gT, cP, V(0.2f, 1.05f, -0.46f), V(0.13f, -0.02f, -0.12f), V(0.006f, 0.008f, 0), 8, 0.11f, 0.04f, cG)
                    .TipPair(Sph, gT, gT, cG, V(0.22f, 0.32f, 0.22f))
                    .WhipPair(gT, gT, cA, V(0.14f, 1.05f, -0.5f), V(0.07f, -0.03f, -0.15f), V(0.006f, 0.006f, 0), 10, 0.13f, 0.05f, cG)
                    .TipPair(Sph, gT, gT, cG, V(0.28f, 0.4f, 0.28f))
                    .Whip(gT, cA, V(0, 1.05f, -0.52f), V(0, -0.04f, -0.16f), V(0, 0.006f, 0), 12, 0.16f, 0.05f, cG)
                    .Tip(Sph, gT, cG, V(0.36f, 0.5f, 0.36f))
                    .PivotPair(gWL, gWR, V(0.24f, 1.36f, 0)).Pivot(gH, V(0, 1.38f, 0.26f)).Pivot(gT, V(0, 1.05f, -0.46f)).Pivot(gX, V(0, 1.4f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 183 NOXARIS (Blight)
            {
                var d = K(b, 183, "noxaris", "Noxaris", Element.Shade, Element.Gale, Rarity.Legendary, BiomeMask.Blight, Temperament.Aggressive, Locomotion.Quadruped, 2.4f, 'B',
                    "The plague-wing. Where Noxaris sleeps the ground goes soft and green, and the Blightmoor is simply the shape of its bed.",
                    "Brooding, territorial, lonelier than it lets on.", A("toxic_cloud", "night_slash"), "gloom_pulse",
                    Work((WorkType.Hauling, 4), (WorkType.Quarrying, 3), (WorkType.Kindling, 3)), CompanionPerk.OreSense, "gel", "crystal",
                    Pal(Hex("#3a304a"), Hex("#6a5a7a"), Hex("#6a3a8a"), Hex("#b8ff40")));
                Legend(d, 225, 31, 24);
                Effects(d, AuraFx.Gloom, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 1.0f, 0), V(0.9f, 0.84f, 1.6f))
                    .Add(Sph, gB, cS, V(0, 0.86f, 0.06f), V(0.7f, 0.56f, 1.3f))
                    .Add(Sph, gB, cP, V(0, 1.12f, 0.5f), V(0.96f, 0.9f, 0.8f))
                    .Add(Sph, gB, cG, V(0, 0.86f, 0.84f), V(0.32f, 0.34f, 0.16f))
                    .Legs4(cP, 0.34f, 0.5f, -0.52f, 0.62f, 0.3f, cD)
                    .Mirror(Sph, gB, cP, V(0.34f, 0.74f, 0.5f), V(0.46f, 0.6f, 0.5f))
                    .Mirror(Sph, gB, cP, V(0.34f, 0.72f, -0.52f), V(0.48f, 0.62f, 0.52f))
                    .Mirror(Con, gB, cW, V(0.26f, 0.06f, 0.7f), V(0.09f, 0.22f, 0.09f), V(90, 0, 0))
                    .Mirror(Con, gB, cW, V(0.42f, 0.06f, 0.7f), V(0.09f, 0.22f, 0.09f), V(90, 0, 0))
                    .Mirror(Con, gB, cW, V(0.34f, 0.06f, -0.32f), V(0.09f, 0.22f, 0.09f), V(90, 0, 0))
                    .Whip(gH, cP, V(0, 1.3f, 0.76f), V(0, 0.12f, 0.11f), V(0, -0.004f, 0.014f), 6, 0.44f, 0.28f, cS)
                    .TipHead(gH, cP, 0.52f, 0.07f, cG, false, cS).Snout(cP, 0.3f, 1.1f, false).Brows(cD)
                    // a crown of horns and a frill of spines down the neck
                    .Horns(cA, 0.1f, 0.6f, 26, -55).Horns(cD, 0.07f, 0.4f, 52, -40).Horn(cA, 0.07f, 0.22f, 40)
                    .Add(Con, gH, cA, V(0, 1.68f, 0.82f), V(0.09f, 0.34f, 0.09f), V(-38, 0, 0))
                    .Add(Con, gH, cA, V(0, 1.86f, 1.02f), V(0.09f, 0.34f, 0.09f), V(-38, 0, 0))
                    .Add(Con, gH, cA, V(0, 2.02f, 1.22f), V(0.08f, 0.3f, 0.08f), V(-38, 0, 0))
                    // two layers of wing
                    .Wings(cA, cA, cD, V(0.34f, 1.46f, 0.2f), 2.2f, 1.1f, 44)
                    .Wings(cP, cA, cG, V(0.34f, 1.4f, -0.04f), 1.4f, 0.8f, 24)
                    .Mirror(Con, gWL, gWR, cD, V(0.56f, 1.82f, 0.56f), V(0.08f, 0.44f, 0.08f), V(60, 0, -30))
                    .Spikes(gB, cA, 1.38f, 0.3f, -0.7f, 6, 0.1f, 0.36f, 0.04f, -25)
                    .Mirror(Sph, gB, cG, V(0.42f, 1.02f, 0.26f), V(0.2f, 0.26f, 0.24f))
                    .Mirror(Sph, gB, cG, V(0.44f, 0.96f, -0.1f), V(0.18f, 0.22f, 0.22f))
                    .Mirror(Sph, gB, cG, V(0.4f, 0.92f, -0.44f), V(0.14f, 0.18f, 0.18f))
                    .Whip(gT, cP, V(0, 0.96f, -0.82f), V(0, -0.02f, -0.17f), V(0, 0.008f, 0), 10, 0.42f, 0.08f, cS)
                    .Tip(Con, gT, cG, V(0.28f, 0.8f, 0.08f))
                    .Spikes(gT, cA, 1.06f, -0.9f, -1.8f, 4, 0.07f, 0.24f, 0, -30)
                    .Ring(Sph, gX, cG, V(0, 1.2f, 0), 1.7f, 5, V(0.15f, 0.15f, 0.15f))
                    .Ring(Sph, gX, cA, V(0, 1.8f, 0), 1.3f, 4, V(0.22f, 0.16f, 0.22f), 0, 40)
                    .PivotPair(gWL, gWR, V(0.34f, 1.46f, 0.2f)).Pivot(gH, V(0, 1.3f, 0.7f)).Pivot(gT, V(0, 0.96f, -0.76f)).Pivot(gX, V(0, 1.2f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 184 KIRALUX (Glow, mount)
            {
                var d = K(b, 184, "kiralux", "Kiralux", Element.Lumen, Element.Volt, Rarity.Legendary, BiomeMask.Glow, Temperament.Docile, Locomotion.Quadruped, 2.0f, 'F',
                    "The star-kirin. It runs the Glowdeep once each night and every mushroom it passes lights up behind it. Nothing has ever kept pace.",
                    "Fleet, shy, drawn to quiet people.", A("radiant_charge", "thunder_charge"), "aurora_veil",
                    Work((WorkType.Charging, 5), (WorkType.Hauling, 4), (WorkType.Chilling, 2)), CompanionPerk.Swiftness, "crystal", "pelt",
                    Pal(Hex("#e8ecff"), Hex("#ffffff"), Hex("#ffd860"), Hex("#a0f4ff")));
                Legend(d, 205, 30, 22);
                Effects(d, AuraFx.Sparks, ExtraAnim.Orbit);
                Mount(d, 15f, 10f, V(0, 1.36f, -0.08f));
                new Plan()
                    .Quad(1.0f, 0.5f, 0.56f, 1.2f, 0.34f, 0.74f, 0.13f, foot: cA, headUp: 0.62f, headOut: 0.2f)
                    .Add(Sph, gB, cP, V(0, 1.08f, 0.44f), V(0.56f, 0.66f, 0.5f))
                    .Add(Sph, gB, cS, V(0, 0.92f, 0.1f), V(0.4f, 0.34f, 0.9f))
                    .Add(Sph, gB, cP, V(0, 1.04f, -0.42f), V(0.54f, 0.6f, 0.5f))
                    .Add(Cap, gH, cP, V(0, 1.3f, 0.7f), V(0.26f, 0.72f, 0.28f), V(28, 0, 0))
                    .Add(Sph, gH, cP, V(0, 1.54f, 1.06f), V(0.2f, 0.2f, 0.36f), V(22, 0, 0))
                    .Face(0.06f, cG)
                    .Ears(Sph, cP, 0.07f, 0.18f, 40)
                    // the great horn with a star on its tip, and swept gold antlers
                    .Add(Cry, gH, cG, V(0, 2.16f, 0.98f), V(0.15f, 0.9f, 0.15f), V(22, 0, 0))
                    .Fan(Con, gH, cW, V(0, 2.6f, 1.16f), 0.1f, 5, V(0.09f, 0.24f, 0.05f))
                    .WhipPair(gH, gH, cA, V(0.1f, 1.84f, 0.8f), V(0.05f, 0.07f, -0.09f), V(0.006f, 0.004f, -0.004f), 6, 0.07f, 0.03f)
                    .Mirror(Con, gH, cA, V(0.22f, 2.04f, 0.6f), V(0.04f, 0.22f, 0.04f), V(-20, 0, -30))
                    // a mane and beard of light
                    .Whip(gH, cG, V(0, 1.8f, 0.78f), V(0, -0.06f, -0.1f), V(0, -0.004f, 0.004f), 10, 0.28f, 0.1f, cA)
                    .WhipPair(gH, gH, cG, V(0.09f, 1.7f, 0.74f), V(0.03f, -0.07f, -0.09f), V(0.002f, -0.004f, 0.004f), 8, 0.17f, 0.06f, cA)
                    .Whip(gH, cG, V(0, 1.42f, 1.08f), V(0, -0.06f, -0.01f), V(0, 0, -0.004f), 4, 0.1f, 0.04f)
                    .Add(Sph, gB, cA, V(0, 0.94f, 0.66f), V(0.34f, 0.4f, 0.12f))
                    .Spikes(gB, cG, 1.3f, 0.3f, -0.56f, 5, 0.08f, 0.28f, 0.02f, -30, 0, 0, Cry)
                    .Mirror(Sph, gB, cA, V(0.25f, 1.04f, -0.1f), V(0.04f, 0.2f, 0.2f))
                    .Mirror(Sph, gB, cG, V(0.24f, 1.14f, 0.2f), V(0.05f, 0.08f, 0.08f))
                    .Mirror(Sph, gB, cG, V(0.26f, 0.98f, -0.4f), V(0.05f, 0.08f, 0.08f))
                    // feathered fetlocks
                    .Mirror(Sph, BodyGroup.LegFL, BodyGroup.LegFR, cG, V(0.15f, 0.2f, 0.34f), V(0.22f, 0.3f, 0.24f))
                    .Mirror(Sph, BodyGroup.LegBL, BodyGroup.LegBR, cG, V(0.15f, 0.2f, -0.41f), V(0.22f, 0.3f, 0.24f))
                    // wings of light and a streaming tail
                    .Wings(cG, cG, cW, V(0.26f, 1.16f, -0.34f), 1.3f, 0.5f, 30)        // set behind the saddle so the rider stays in view
                    .Wings(cA, cG, cW, V(0.26f, 1.12f, -0.48f), 0.8f, 0.36f, 10)
                    .WhipPair(gT, gT, cA, V(0.06f, 1.1f, -0.58f), V(0.05f, 0f, -0.11f), V(0.004f, -0.01f, 0), 9, 0.13f, 0.04f, cG)
                    .Whip(gT, cG, V(0, 1.12f, -0.6f), V(0, 0.02f, -0.12f), V(0, -0.012f, 0), 11, 0.22f, 0.06f, cA)
                    .Ring(Sph, gX, cG, V(0, 1.5f, 0), 1.2f, 6, V(0.1f, 0.1f, 0.1f))
                    .Ring(Sph, gX, cW, V(0, 2.1f, 0), 0.8f, 4, V(0.07f, 0.07f, 0.07f), 0, 45)
                    .PivotPair(gWL, gWR, V(0.26f, 1.16f, -0.34f)).Pivot(gT, V(0, 1.12f, -0.56f)).Pivot(gX, V(0, 1.5f, 0))
                    .Done(d);
            }
        }
    }
}
