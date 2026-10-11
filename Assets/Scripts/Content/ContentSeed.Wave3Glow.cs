using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 3: Glowdeep natives (#164-178) - lantern, crystal and starlight kin. Lv 34-45.</summary>
        static void BuildWave3Glow(Bundle b)
        {
            const BiomeMask L = BiomeMask.Glow;

            // ---------------------------------------------------------------- 164 GLOWMITE
            {
                var d = K(b, 164, "glowmite", "Glowmite", Element.Lumen, Element.Normal, Rarity.Common, L, Temperament.Timid, Locomotion.Hopper, 0.45f, 'F',
                    "A bead of light with feet. Glowmites gather wherever something is growing, and the Glowdeep's paths are mostly just where they sit.",
                    "Tiny, trusting, dims when sad.", A("glint_beam", "tackle"), "prism_dust",
                    Work((WorkType.Charging, 1)), CompanionPerk.Glow, "gel", "",
                    Pal(Hex("#3a4a6a"), Hex("#6a7a9a"), Hex("#ff9ad0"), Hex("#90fff0")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Blob(0.24f, 0.4f, 0.38f, 0.42f)
                    .Add(Sph, gB, cG, V(0, 0.26f, -0.14f), V(0.32f, 0.32f, 0.26f))
                    .Face(0.1f, cG, 0.08f, 0.26f).Cheeks(cA)
                    .WhipPair(gB, gB, cS, V(0.08f, 0.4f, 0.08f), V(0.02f, 0.06f, 0.02f), V(0.006f, -0.006f, 0.004f), 4, 0.03f, 0.025f)
                    .TipPair(Sph, gB, gB, cG, V(0.08f, 0.08f, 0.08f))
                    .Mirror(Sph, gFL, gFR, cD, V(0.12f, 0.03f, 0.06f), V(0.1f, 0.06f, 0.14f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 165 NEONEWT
            {
                var d = K(b, 165, "neonewt", "Neonewt", Element.Lumen, Element.Tide, Rarity.Common, L, Temperament.Curious, Locomotion.Quadruped, 0.6f, 'B',
                    "A cave newt striped in living neon with frilly pink gills. It smiles at everything, which is unsettling in the dark.",
                    "Cheery, nosy, grins through anything.", A("bubble_burst", "glint_beam"), "aqua_tail",
                    Work((WorkType.Irrigating, 1), (WorkType.Charging, 1)), CompanionPerk.Mending, "gel", "",
                    Pal(Hex("#2a3a5a"), Hex("#5a6a8a"), Hex("#ff7ab8"), Hex("#60ffd8")));
                d.nearWater = true;
                new Plan()
                    .Quad(0.18f, 0.3f, 0.22f, 0.6f, 0.3f, 0.1f, 0.07f, foot: cG, headUp: 0.04f)
                    .Add(Sph, gB, cG, V(0, 0.27f, 0), V(0.08f, 0.06f, 0.5f))
                    .Mirror(Sph, gB, cG, V(0.13f, 0.2f, 0), V(0.04f, 0.05f, 0.4f))
                    .Face(0.08f, cD, 0.14f, 0.3f)
                    .Add(Sph, gH, cD, V(0, 0.17f, 0.495f), V(0.14f, 0.02f, 0.03f))
                    .Mirror(Sph, gH, cA, V(0.19f, 0.28f, 0.3f), V(0.16f, 0.05f, 0.05f), V(0, -30, 25))
                    .Mirror(Sph, gH, cA, V(0.2f, 0.21f, 0.29f), V(0.14f, 0.04f, 0.05f), V(0, -30, -5))
                    .Mirror(Sph, gH, cA, V(0.18f, 0.15f, 0.28f), V(0.12f, 0.04f, 0.05f), V(0, -30, -30))
                    .Tail(cP, 0.18f, -0.3f, 0.5f, 0.12f, 0.03f, 0f)
                    .Add(Sph, gT, cG, V(0, 0.24f, -0.56f), V(0.03f, 0.16f, 0.4f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 166 CAPGLOW
            {
                var d = K(b, 166, "capglow", "Capglow", Element.Lumen, Element.Flora, Rarity.Common, L, Temperament.Docile, Locomotion.Biped, 0.7f, 'T',
                    "A tall-capped mushroom that lights its own way and everyone else's. Rows of them line the Glowdeep like street lamps that wander off.",
                    "Helpful, sleepy, hums to keep its light steady.", A("spore_puff", "glint_beam"), "glow_pulse",
                    Work((WorkType.Cultivating, 2), (WorkType.Foraging, 1)), CompanionPerk.Glow, "fiber", "",
                    Pal(Hex("#3a6ab8"), Hex("#e8e4f4"), Hex("#ff9ad0"), Hex("#80fff0")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Biped(0.14f, 0.26f, 0.4f, 0.26f, 0.08f, cS, cS, null)
                    .Add(Dom, gH, cP, V(0, 0.73f, -0.02f), V(0.46f, 0.76f, 0.46f))
                    .Add(Sph, gH, cG, V(0, 0.73f, -0.02f), V(0.42f, 0.06f, 0.42f))
                    .Ring(Sph, gH, cG, V(0, 0.86f, -0.02f), 0.2f, 5, V(0.08f, 0.08f, 0.06f))
                    .Ring(Sph, gH, cG, V(0, 0.99f, -0.02f), 0.13f, 3, V(0.06f, 0.06f, 0.05f), 0, 40)
                    .Face(0.07f).Cheeks(cA)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 167 PIXIWING
            {
                var d = K(b, 167, "pixiwing", "Pixiwing", Element.Lumen, Element.Gale, Rarity.Common, L, Temperament.Timid, Locomotion.Flyer, 0.5f, 'F',
                    "A finger-high sprite with four wings of light. It steals single buttons, never the pair, and nobody has found where they go.",
                    "Flighty, teasing, bursts into giggles.", A("prism_dust", "seed_gust"), "aurora_veil",
                    Work((WorkType.Crafting, 1), (WorkType.Foraging, 1)), CompanionPerk.Glow, "fiber", "",
                    Pal(Hex("#f0d8ff"), Hex("#b890e0"), Hex("#ff9ad0"), Hex("#a0fff0")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.72f, 0), V(0.16f, 0.3f, 0.16f))
                    .Add(Sph, gH, cP, V(0, 0.94f, 0.02f), V(0.26f, 0.25f, 0.25f)).Head(gH, 0.94f, 0.02f, 0.26f)
                    .Face(0.08f, cD, 0.06f, 0.26f).Cheeks(cA)
                    .WhipPair(gH, gH, cS, V(0.05f, 1.05f, 0.04f), V(0.02f, 0.05f, 0.01f), V(0.008f, -0.008f, 0), 4, 0.025f, 0.02f)
                    .TipPair(Sph, gH, gH, cG, V(0.06f, 0.06f, 0.06f))
                    .Fins(cG, V(0.05f, 0.82f, -0.04f), 0.44f, 0.3f, 35)
                    .Mirror(Sph, gWL, gWR, cA, V(0.2f, 0.64f, -0.06f), V(0.3f, 0.04f, 0.2f), V(0, 0, -25))
                    .Mirror(Sph, gB, cS, V(0.04f, 0.54f, 0.02f), V(0.04f, 0.12f, 0.04f))
                    .Pivot(gH, V(0, 0.86f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 168 GLIMWORM
            {
                var d = K(b, 168, "glimworm", "Glimworm", Element.Lumen, Element.Shade, Rarity.Common, L, Temperament.Docile, Locomotion.Slither, 0.7f, 'B',
                    "A chubby glow-worm that lights up one band at a time, like a very slow sentence. Others answer from across the dark.",
                    "Thoughtful, quiet, talks in blinks.", A("glint_beam", "coil_strike"), "hex_flicker",
                    Work((WorkType.Cultivating, 1), (WorkType.Charging, 1)), CompanionPerk.Glow, "gel", "",
                    Pal(Hex("#4a3a6a"), Hex("#7a6a9a"), Hex("#ff9ad0"), Hex("#b0ff80")));
                new Plan()
                    .Serpent(cP, cG, 0.18f, 0.3f, 7, 0.2f)
                    .Add(Sph, gH, cP, V(0, 0.22f, 0.18f), V(0.32f, 0.3f, 0.3f)).Head(gH, 0.22f, 0.18f, 0.32f)
                    .Face(0.08f, cD, 0.1f, 0.26f).Cheeks(cA)
                    .WhipPair(gH, gH, cS, V(0.07f, 0.34f, 0.2f), V(0.02f, 0.05f, 0.02f), V(0.006f, -0.008f, 0.004f), 4, 0.03f, 0.02f)
                    .TipPair(Sph, gH, gH, cG, V(0.07f, 0.07f, 0.07f))
                    .Add(Sph, gT, cG, V(0, 0.16f, -1.3f), V(0.16f, 0.16f, 0.16f))
                    .Pivot(gH, V(0, 0.2f, 0.06f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 169 PRISMFOX
            {
                var d = K(b, 169, "prismfox", "Prismfox", Element.Lumen, Element.Frost, Rarity.Uncommon, L, Temperament.Curious, Locomotion.Quadruped, 0.85f, 'F',
                    "A pale fox with three tails, each tipped in crystal. When it runs, the light it throws paints the whole grove in rainbows.",
                    "Quick-witted, elusive, loves to be chased.", A("crystal_shard", "glint_beam"), "radiant_charge",
                    Work((WorkType.Chilling, 2), (WorkType.Foraging, 1), (WorkType.Crafting, 1)), CompanionPerk.Swiftness, "pelt", "crystal",
                    Pal(Hex("#e8ecff"), Hex("#ffffff"), Hex("#9a8ae0"), Hex("#90f0ff")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Quad(0.46f, 0.28f, 0.3f, 0.62f, 0.32f, 0.34f, 0.07f, foot: cA, headUp: 0.2f)
                    .Add(Sph, gB, cS, V(0, 0.42f, 0.18f), V(0.26f, 0.28f, 0.3f))
                    .Face(0.07f, cG).Snout(cS, 0.13f, 0.9f).Cheeks(cS)
                    .Ears(Con, cP, 0.13f, 0.26f, 14)
                    .Ears(Con, cA, 0.07f, 0.16f, 14, -0.02f)
                    .Add(Cry, gH, cG, V(0, 0.88f, 0.42f), V(0.1f, 0.14f, 0.1f), V(20, 0, 0))
                    .WhipPair(gT, gT, cP, V(0.06f, 0.5f, -0.3f), V(0.05f, 0.03f, -0.08f), V(0.004f, 0.012f, 0), 6, 0.13f, 0.15f)
                    .TipPair(Cry, gT, gT, cG, V(0.14f, 0.2f, 0.14f))
                    .Whip(gT, cP, V(0, 0.52f, -0.3f), V(0, 0.05f, -0.08f), V(0, 0.012f, 0), 6, 0.14f, 0.16f)
                    .Tip(Cry, gT, cG, V(0.16f, 0.24f, 0.16f))
                    .Pivot(gT, V(0, 0.5f, -0.28f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 170 NOCTOWLET
            {
                var d = K(b, 170, "noctowlet", "Noctowlet", Element.Shade, Element.Lumen, Rarity.Uncommon, L, Temperament.Curious, Locomotion.Flyer, 0.75f, 'B',
                    "A round little owl whose eyes glow like twin moons. It sees perfectly in the dark and stares at you to prove it.",
                    "Wise-looking, actually clueless, loves a head scratch.", A("hex_flicker", "gust_slash"), "glow_pulse",
                    Work((WorkType.Crafting, 2), (WorkType.Hauling, 1)), CompanionPerk.Glow, "pelt", "",
                    Pal(Hex("#3a3460"), Hex("#b8b0d8"), Hex("#f0b040"), Hex("#b0f0ff")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.72f, 0), V(0.44f, 0.52f, 0.42f))
                    .Add(Sph, gB, cS, V(0, 0.66f, 0.08f), V(0.34f, 0.36f, 0.3f))
                    .Mirror(Sph, gB, cS, V(0.1f, 0.86f, 0.12f), V(0.2f, 0.2f, 0.1f))
                    .Eyes(gB, V(0.1f, 0.86f, 0.165f), 0.1f, cG)
                    .Add(Con, gB, cA, V(0, 0.8f, 0.19f), V(0.06f, 0.1f, 0.06f), V(160, 0, 0))
                    .Mirror(Con, gB, cP, V(0.14f, 1.02f, 0.02f), V(0.1f, 0.18f, 0.08f), V(0, 0, -25))
                    .Fins(cP, V(0.2f, 0.76f, -0.02f), 0.36f, 0.3f, -30)
                    .Add(Sph, gB, cG, V(0, 0.62f, 0.215f), V(0.07f, 0.07f, 0.04f))
                    .Mirror(Sph, gB, cG, V(0.08f, 0.56f, 0.2f), V(0.05f, 0.05f, 0.04f))
                    .Add(Sph, gT, cP, V(0, 0.52f, -0.2f), V(0.2f, 0.05f, 0.2f))
                    .Mirror(Sph, gB, cA, V(0.08f, 0.46f, 0.06f), V(0.07f, 0.06f, 0.1f))
                    .Pivot(gT, V(0, 0.54f, -0.14f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 171 BULBFROG
            {
                var d = K(b, 171, "bulbfrog", "Bulbfrog", Element.Lumen, Element.Tide, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Hopper, 0.7f, 'B',
                    "A frog with a lantern for a throat and a little lure bobbing over its head. It croaks in flashes, and the pond flashes back.",
                    "Mellow, musical, a patient listener.", A("tongue_lash", "glow_pulse"), "wave_crash",
                    Work((WorkType.Irrigating, 2), (WorkType.Charging, 2)), CompanionPerk.Glow, "gel", "",
                    Pal(Hex("#2a6a6a"), Hex("#5aa8a0"), Hex("#ffd070"), Hex("#f0ff90")));
                d.nearWater = true;
                Effects(d, AuraFx.Glitter, ExtraAnim.Bob);
                new Plan()
                    .Blob(0.26f, 0.54f, 0.4f, 0.5f)
                    .Add(Sph, gB, cG, V(0, 0.18f, 0.16f), V(0.36f, 0.3f, 0.3f))
                    .Mirror(Sph, gB, cP, V(0.15f, 0.44f, 0.12f), V(0.17f, 0.17f, 0.17f))
                    .Eyes(gB, V(0.15f, 0.46f, 0.195f), 0.09f)
                    .Mirror(Sph, gFL, gFR, cP, V(0.24f, 0.1f, -0.08f), V(0.17f, 0.2f, 0.28f))
                    .Mirror(Sph, gFL, gFR, cS, V(0.26f, 0.03f, 0.1f), V(0.14f, 0.05f, 0.24f))
                    .Mirror(Sph, gB, cG, V(0.2f, 0.36f, -0.12f), V(0.07f, 0.05f, 0.07f))
                    .Whip(gX, cS, V(0, 0.46f, 0.04f), V(0, 0.07f, 0.03f), V(0, -0.012f, 0.014f), 5, 0.035f, 0.025f)
                    .Tip(Sph, gX, cG, V(0.12f, 0.12f, 0.12f))
                    .Pivot(gX, V(0, 0.46f, 0.04f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 172 SHARDILLO
            {
                var d = K(b, 172, "shardillo", "Shardillo", Element.Stone, Element.Lumen, Rarity.Uncommon, L, Temperament.Territorial, Locomotion.Quadruped, 0.8f, 'T',
                    "An armadillo that grows crystals instead of armor. Curled up it is a geode, and more than one miner has taken a pick to the wrong rock.",
                    "Prickly, cautious, warms up slowly.", A("crystal_shard", "roll_crush"), "quake_stomp",
                    Work((WorkType.Quarrying, 3), (WorkType.Hauling, 1)), CompanionPerk.OreSense, "crystal", "stone",
                    Pal(Hex("#5a5068"), Hex("#b8a8a0"), Hex("#8a7a9a"), Hex("#c090ff")));
                new Plan()
                    .Quad(0.26f, 0.44f, 0.3f, 0.64f, 0.26f, 0.14f, 0.09f, cS, cS, cD, 0f, 0.04f)
                    .Shell(cP, 0.22f, 0.56f, 0.64f, 0.3f, 0.2f).Shell(cA, 0.22f, 0.6f, 0.72f, 0.3f, 0f).Shell(cP, 0.22f, 0.56f, 0.64f, 0.3f, -0.2f)
                    .Face(0.06f).Snout(cS, 0.12f, 1.1f)
                    .Ears(Sph, cS, 0.07f, 0.14f, 25)
                    .Spikes(gB, cG, 0.5f, 0.24f, -0.24f, 4, 0.1f, 0.28f, 0.02f, -15, 0, 0, Cry)
                    .Spikes(gB, cG, 0.4f, 0.18f, -0.2f, 3, 0.07f, 0.2f, 0.02f, -15, 0.18f, -35, Cry)
                    .Tail(cS, 0.24f, -0.34f, 0.3f, 0.08f, 0.03f, -0.01f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 173 SPOREGLIDE
            {
                var d = K(b, 173, "sporeglide", "Sporeglide", Element.Flora, Element.Lumen, Rarity.Uncommon, L, Temperament.Timid, Locomotion.Flyer, 0.7f, 'F',
                    "A seed with a face, hanging under a parasol of glowing threads. It drifts wherever the air is warm and plants itself for a nap.",
                    "Airy, wistful, never in a hurry.", A("pollen_burst", "seed_gust"), "aurora_veil",
                    Work((WorkType.Cultivating, 2), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "fiber", "",
                    Pal(Hex("#f0f4ff"), Hex("#c8a878"), Hex("#ff9ab8"), Hex("#a0ffd8")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Bob);
                new Plan()
                    .Blob(0.56f, 0.24f, 0.26f, 0.24f, cS)
                    .Face(0.075f, cD, 0.06f, 0.26f).Cheeks(cA)
                    .Add(Cyl, gB, cS, V(0, 0.8f, 0), V(0.03f, 0.3f, 0.03f))
                    .Fronds(gX, cP, 10, 0.34f, 0.04f, 0.5f, V(0, 0.94f, 0))
                    .Ring(Sph, gX, cG, V(0, 1.09f, 0), 0.3f, 10, V(0.06f, 0.06f, 0.06f))
                    .Add(Sph, gX, cG, V(0, 0.95f, 0), V(0.08f, 0.08f, 0.08f))
                    .Mirror(Sph, gAL, gAR, cS, V(0.14f, 0.5f, 0.02f), V(0.07f, 0.1f, 0.07f))
                    .Pivot(gX, V(0, 0.94f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 174 LUNELK (mount)
            {
                var d = K(b, 174, "lunelk", "Lunelk", Element.Lumen, Element.Frost, Rarity.Rare, L, Temperament.Docile, Locomotion.Quadruped, 1.45f, 'B',
                    "A silver elk whose antlers curve into a crescent moon. It walks the Glowdeep in silence, and the lights dim as it passes, out of respect.",
                    "Serene, dignified, chooses its rider carefully.", A("radiant_charge", "ice_fang"), "aurora_veil",
                    Work((WorkType.Hauling, 3), (WorkType.Chilling, 2), (WorkType.Logging, 1)), CompanionPerk.Glow, "pelt", "crystal",
                    Pal(Hex("#c8d0e8"), Hex("#f4f6ff"), Hex("#6a7ab8"), Hex("#d0f0ff")));
                Effects(d, AuraFx.Glitter);
                Mount(d, 13f, 9f, V(0, 1.2f, -0.08f));
                new Plan()
                    .Quad(0.92f, 0.44f, 0.5f, 1.08f, 0.32f, 0.7f, 0.1f, foot: cA, headUp: 0.5f, headOut: 0.14f)
                    .Add(Cap, gH, cP, V(0, 1.16f, 0.6f), V(0.22f, 0.6f, 0.24f), V(28, 0, 0))
                    .Add(Sph, gB, cS, V(0, 1.0f, 0.44f), V(0.4f, 0.5f, 0.36f))
                    .Face(0.055f, cA).Snout(cS, 0.16f, 1f)
                    .Ears(Sph, cP, 0.07f, 0.16f, 65)
                    .WhipPair(gH, gH, cG, V(0.08f, 1.56f, 0.72f), V(0.1f, 0.04f, -0.01f), V(-0.032f, 0.008f, 0), 8, 0.08f, 0.03f)
                    .Add(Sph, gH, cG, V(0, 1.86f, 0.7f), V(0.14f, 0.14f, 0.1f))
                    .Mirror(Sph, gB, cG, V(0.215f, 0.96f, -0.1f), V(0.03f, 0.16f, 0.16f))
                    .Mirror(Sph, gB, cG, V(0.2f, 1.0f, -0.3f), V(0.03f, 0.08f, 0.08f))
                    .Tail(cS, 1.0f, -0.52f, 0.16f, 0.1f, 0.07f, 0.03f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 175 GLOWMAW
            {
                var d = K(b, 175, "glowmaw", "Glowmaw", Element.Shade, Element.Lumen, Rarity.Rare, L, Temperament.Aggressive, Locomotion.Quadruped, 1.3f, 'G',
                    "The friendliest light in the Glowdeep hangs in front of the biggest mouth. A Glowmaw waits in the dark behind its lure and has never had to chase anything.",
                    "Lazy, cunning, smiles too much.", A("acid_bite", "glow_pulse"), "night_slash",
                    Work((WorkType.Hauling, 2), (WorkType.Charging, 2)), CompanionPerk.OreSense, "gel", "crystal",
                    Pal(Hex("#2a2a44"), Hex("#4a4a6a"), Hex("#6a5aa0"), Hex("#a0fff0")));
                d.nearWater = true;
                Effects(d, AuraFx.None, ExtraAnim.Bob);
                var p = new Plan()
                    .Add(Sph, gB, cP, V(0, 0.64f, 0), V(0.84f, 0.8f, 1.0f))
                    .Legs4(cP, 0.26f, 0.22f, -0.3f, 0.26f, 0.2f, cD)
                    .Add(Sph, gB, cD, V(0, 0.54f, 0.38f), V(0.68f, 0.22f, 0.36f))
                    .Add(Sph, gH, cP, V(0, 0.4f, 0.38f), V(0.76f, 0.3f, 0.54f))
                    .Mirror(Sph, gB, cG, V(0.26f, 0.86f, 0.36f), V(0.07f, 0.07f, 0.05f))
                    .Mirror(Bx, gB, cD, V(0.26f, 0.92f, 0.36f), V(0.14f, 0.03f, 0.05f), V(0, 0, 22))
                    .Mirror(Con, gB, cW, V(0.08f, 0.6f, 0.52f), V(0.06f, 0.16f, 0.06f), V(180, 0, 0))
                    .Mirror(Con, gB, cW, V(0.2f, 0.62f, 0.46f), V(0.06f, 0.14f, 0.06f), V(180, 0, 0));
                for (int i = 0; i < 3; i++)
                {
                    float x = 0.1f + i * 0.11f;
                    p.Mirror(Con, gH, cW, V(x, 0.58f, 0.6f - x * 0.5f), V(0.06f, 0.2f - i * 0.03f, 0.06f));
                }
                p.Add(Sph, gB, cA, V(0, 1.04f, -0.2f), V(0.05f, 0.3f, 0.5f))
                    .Mirror(Sph, gB, cA, V(0.46f, 0.6f, -0.1f), V(0.3f, 0.04f, 0.3f), V(0, 0, -20))
                    .Mirror(Sph, gB, cG, V(0.4f, 0.56f, -0.2f), V(0.06f, 0.06f, 0.06f))
                    .Mirror(Sph, gB, cG, V(0.36f, 0.5f, -0.34f), V(0.05f, 0.05f, 0.05f))
                    .Whip(gT, cP, V(0, 0.6f, -0.5f), V(0, 0, -0.1f), V(0, 0, 0), 3, 0.3f, 0.14f)
                    .Add(Sph, gT, cA, V(0, 0.6f, -0.86f), V(0.05f, 0.5f, 0.3f))
                    .Whip(gX, cS, V(0, 1.0f, 0.2f), V(0, 0.08f, 0.06f), V(0, -0.02f, 0.014f), 7, 0.04f, 0.025f)
                    .Tip(Sph, gX, cG, V(0.18f, 0.18f, 0.18f))
                    .Pivot(gH, V(0, 0.5f, 0.1f)).Pivot(gT, V(0, 0.6f, -0.46f)).Pivot(gX, V(0, 1.0f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 176 MYCELIATH
            {
                var d = K(b, 176, "myceliath", "Myceliath", Element.Flora, Element.Lumen, Rarity.Rare, L, Temperament.Territorial, Locomotion.Biped, 1.6f, 'T',
                    "A mushroom the size of a house that stood up one day. Every glowing thing in the Glowdeep is said to be threaded back to it underground.",
                    "Deliberate, ancient, speaks once a decade.", A("root_ram", "glow_pulse"), "petal_storm",
                    Work((WorkType.Cultivating, 3), (WorkType.Logging, 3), (WorkType.Hauling, 2)), CompanionPerk.LumberBoost, "wood", "fiber",
                    Pal(Hex("#4a3a8a"), Hex("#d8d0e8"), Hex("#8a6ac0"), Hex("#90fff0")));
                Effects(d, AuraFx.Glitter);
                new Plan()
                    .Biped(0.5f, 0.7f, 0.76f, 0.4f, 0.24f, cS, cS, cD, false)
                    .Add(Dom, gH, cP, V(0, 1.44f, 0), V(1.1f, 0.7f, 1.1f))
                    .Add(Sph, gH, cG, V(0, 1.44f, 0), V(1.0f, 0.08f, 1.0f))
                    .Ring(Sph, gH, cG, V(0, 1.6f, 0), 0.46f, 6, V(0.14f, 0.08f, 0.14f))
                    .Add(Sph, gH, cG, V(0, 1.78f, 0), V(0.2f, 0.06f, 0.2f))
                    .Face(0.07f, cG).Brows(cD)
                    .Mirror(Cap, gAL, gAR, cS, V(0.5f, 0.84f, 0.02f), V(0.24f, 0.7f, 0.24f), V(0, 0, -12))
                    .Mirror(Sph, gAL, gAR, cS, V(0.58f, 0.46f, 0.04f), V(0.34f, 0.3f, 0.34f))
                    .Mirror(Dom, gAL, gAR, cP, V(0.5f, 1.14f, 0), V(0.44f, 0.3f, 0.44f))
                    .Mirror(Dom, gB, cA, V(0.3f, 0.66f, -0.2f), V(0.3f, 0.16f, 0.3f), V(0, 0, -60))
                    .Mirror(Dom, gB, cA, V(0.26f, 0.86f, -0.26f), V(0.24f, 0.14f, 0.24f), V(0, 0, -60))
                    .PivotPair(gAL, gAR, V(0.42f, 1.14f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 177 STARWHAL (mount)
            {
                var d = K(b, 177, "starwhal", "Starwhal", Element.Lumen, Element.Tide, Rarity.Rare, L, Temperament.Docile, Locomotion.Flyer, 1.4f, 'B',
                    "A sky-swimming whale with a star on the end of its horn. It sings the Glowdeep to sleep, and the mushrooms sway in time.",
                    "Gentle, dreamy, hums through everything.", A("radiant_charge", "wave_crash"), "aurora_veil",
                    Work((WorkType.Irrigating, 3), (WorkType.Charging, 2), (WorkType.Hauling, 2)), CompanionPerk.Mending, "gel", "crystal",
                    Pal(Hex("#3a4a9a"), Hex("#c8d8ff"), Hex("#7a8ae0"), Hex("#fff4a0")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                Mount(d, 12.5f, 8f, V(0, 1.24f, 0f));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.9f, 0), V(0.7f, 0.66f, 1.3f))
                    .Add(Sph, gB, cS, V(0, 0.8f, 0.1f), V(0.58f, 0.46f, 1.06f))
                    .Head(gB, 0.92f, 0.36f, 0.56f).Face(0.07f).Cheeks(cA)
                    .Add(Con, gB, cG, V(0, 1.08f, 0.86f), V(0.09f, 0.7f, 0.09f), V(65, 0, 0))
                    .Fan(Con, gB, cG, V(0, 1.24f, 1.2f), 0.07f, 5, V(0.07f, 0.14f, 0.04f))
                    .Fins(cA, V(0.3f, 0.74f, 0.2f), 0.44f, 0.26f, -20, 0.3f)
                    .Mirror(Sph, gB, cG, V(0.3f, 1.08f, -0.1f), V(0.06f, 0.06f, 0.06f))
                    .Mirror(Sph, gB, cG, V(0.22f, 1.16f, 0.14f), V(0.05f, 0.05f, 0.05f))
                    .Mirror(Sph, gB, cG, V(0.26f, 1.06f, -0.34f), V(0.04f, 0.04f, 0.04f))
                    .Whip(gT, cP, V(0, 0.9f, -0.6f), V(0, 0.01f, -0.1f), V(0, 0.004f, 0), 4, 0.3f, 0.12f)
                    .Mirror(Sph, gT, cA, V(0.18f, 0.98f, -1.04f), V(0.4f, 0.05f, 0.22f), V(0, -25, 0))
                    .Ring(Sph, gX, cG, V(0, 1.0f, 0), 0.9f, 3, V(0.1f, 0.1f, 0.1f))
                    .Pivot(gT, V(0, 0.9f, -0.56f)).Pivot(gX, V(0, 1.0f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 178 ASTRALISK (epic)
            {
                var d = K(b, 178, "astralisk", "Astralisk", Element.Lumen, Element.Shade, Rarity.Epic, L, Temperament.Territorial, Locomotion.Slither, 1.9f, 'B',
                    "A serpent of night sky, winged, crystal-spined and haloed in slow-turning stars. The Glowdeep glows because it once slept there, and it has come back to check.",
                    "Remote, curious, weighs every word.", A("solar_flare", "coil_strike"), "gloom_pulse",
                    Work((WorkType.Charging, 3), (WorkType.Crafting, 3), (WorkType.Hauling, 2)), CompanionPerk.Glow, "crystal", "gel",
                    Pal(Hex("#2a2a5a"), Hex("#5a4a9a"), Hex("#8a6ae0"), Hex("#b0f4ff")));
                Effects(d, AuraFx.Glitter, ExtraAnim.Orbit);
                new Plan()
                    .Serpent(cP, cS, 0.3f, 0.5f, 10, 0.32f)
                    .Whip(gH, cP, V(0, 0.4f, 0.1f), V(0, 0.14f, 0.03f), V(0, -0.008f, 0.014f), 6, 0.4f, 0.3f, cS)
                    .TipHead(gH, cP, 0.46f, 0.065f, cG, false, cS).Snout(cS, 0.24f, 0.9f, false).Horns(cG, 0.08f, 0.4f, 25, -45).Brows(cD)
                    .WhipPair(gH, gH, cG, V(0.1f, 1.1f, 0.72f), V(0.05f, -0.01f, 0.05f), V(0.004f, -0.012f, -0.012f), 7, 0.035f, 0.02f)
                    .Wings(cA, cA, cG, V(0.2f, 0.7f, -0.1f), 1.0f, 0.5f, 25)
                    .Spikes(gB, cG, 0.5f, -0.3f, -1.3f, 4, 0.08f, 0.24f, 0, -30, 0, 0, Cry)
                    .Spikes(gT, cG, 0.34f, -1.7f, -2.7f, 4, 0.06f, 0.18f, 0, -30, 0, 0, Cry)
                    .Add(Sph, gT, cG, V(0, 0.2f, -3.0f), V(0.24f, 0.24f, 0.24f))
                    .Ring(Sph, gX, cG, V(0, 1.75f, 0.5f), 0.5f, 6, V(0.1f, 0.1f, 0.1f))
                    .PivotPair(gWL, gWR, V(0.2f, 0.7f, -0.1f)).Pivot(gH, V(0, 0.4f, 0.08f)).Pivot(gX, V(0, 1.75f, 0.5f))
                    .Done(d);
            }
        }
    }
}
