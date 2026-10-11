using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 3: Blightmoor natives (#149-163) - ooze, spore, venom and rot kin. Lv 28-38.</summary>
        static void BuildWave3Blight(Bundle b)
        {
            const BiomeMask L = BiomeMask.Blight;

            // ---------------------------------------------------------------- 149 OOZLET
            {
                var d = K(b, 149, "oozlet", "Oozlet", Element.Shade, Element.Tide, Rarity.Common, L, Temperament.Docile, Locomotion.Hopper, 0.55f, 'T',
                    "A blob of something from the bottom of a blight pool that woke up friendly. It leaves a glowing trail and means no harm by it.",
                    "Sweet, sticky, doesn't know it's toxic.", A("venom_spit", "tackle"), "toxic_cloud",
                    Work((WorkType.Irrigating, 1)), CompanionPerk.Mending, "gel", "",
                    Pal(Hex("#7ad03a"), Hex("#d0ff90"), Hex("#b05ac0"), Hex("#e8ff70")));
                Effects(d, AuraFx.Gloom, ExtraAnim.Bob);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.1f, 0), V(0.66f, 0.2f, 0.62f))
                    .Blob(0.28f, 0.5f, 0.46f, 0.48f)
                    .Add(Sph, gB, cS, V(-0.1f, 0.42f, 0.1f), V(0.14f, 0.1f, 0.08f))
                    .Add(Sph, gB, cP, V(0.1f, 0.52f, -0.04f), V(0.16f, 0.18f, 0.16f))
                    .Face(0.1f, cD, 0.05f)
                    .Add(Sph, gB, cD, V(0, 0.2f, 0.232f), V(0.1f, 0.05f, 0.03f))
                    .Add(Sph, gX, cG, V(0.16f, 0.7f, 0), V(0.1f, 0.1f, 0.1f))
                    .Add(Sph, gX, cG, V(-0.12f, 0.82f, 0.04f), V(0.07f, 0.07f, 0.07f))
                    .Add(Sph, gX, cG, V(0.02f, 0.94f, -0.06f), V(0.05f, 0.05f, 0.05f))
                    .Pivot(gX, V(0, 0.7f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 150 BLIGHTRAT
            {
                var d = K(b, 150, "blightrat", "Blightrat", Element.Shade, Element.Normal, Rarity.Common, L, Temperament.Aggressive, Locomotion.Quadruped, 0.6f, 'F',
                    "A mangy scavenger that eats what even the moor won't. It travels in packs and the packs travel towards your food.",
                    "Bold, greedy, strangely house-proud.", A("acid_bite", "quick_kick"), "shadow_pounce",
                    Work((WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "pelt", "",
                    Pal(Hex("#5a5060"), Hex("#3a3040"), Hex("#d08a9a"), Hex("#b0ff50")));
                new Plan()
                    .Quad(0.26f, 0.3f, 0.3f, 0.5f, 0.3f, 0.16f, 0.07f, foot: cA, headUp: 0.08f)
                    .Face(0.06f, cG).Snout(cP, 0.14f, 1f).Brows(cD)
                    .Add(Bx, gH, cW, V(0, 0.25f, 0.5f), V(0.06f, 0.08f, 0.02f))
                    .Ears(Sph, cA, 0.16f, 0.16f, 35)
                    .Spikes(gB, cS, 0.38f, 0.14f, -0.2f, 4, 0.05f, 0.14f, 0.02f, -30)
                    .Add(Sph, gB, cG, V(0.1f, 0.36f, -0.08f), V(0.08f, 0.06f, 0.08f))
                    .Tail(cA, 0.24f, -0.24f, 0.6f, 0.05f, 0.025f, 0.02f, null, 0f, 0.01f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 151 SPORELING
            {
                var d = K(b, 151, "sporeling", "Sporeling", Element.Shade, Element.Flora, Rarity.Common, L, Temperament.Timid, Locomotion.Biped, 0.5f, 'B',
                    "A toadstool toddler under a cap three sizes too big. When scared it sits down, and from above it is just another mushroom.",
                    "Bashful, clumsy, hides in plain sight.", A("spore_puff", "tackle"), "toxic_cloud",
                    Work((WorkType.Cultivating, 1), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "fiber", "",
                    Pal(Hex("#8a3ab0"), Hex("#e8dcc8"), Hex("#ff8ab0"), Hex("#c8ff70")));
                new Plan()
                    .Biped(0.12f, 0.26f, 0.26f, 0.3f, 0.08f, cS, cS, null)
                    .Add(Dom, gH, cP, V(0, 0.57f, -0.02f), V(0.58f, 0.4f, 0.58f))
                    .Add(Cyl, gH, cA, V(0, 0.57f, -0.02f), V(0.52f, 0.03f, 0.52f))
                    .Ring(Sph, gH, cG, V(0, 0.66f, -0.02f), 0.2f, 5, V(0.09f, 0.06f, 0.09f))
                    .Add(Sph, gH, cG, V(0, 0.76f, -0.02f), V(0.1f, 0.04f, 0.1f))
                    .Face(0.08f).Cheeks(cA)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 152 STINGNAT
            {
                var d = K(b, 152, "stingnat", "Stingnat", Element.Shade, Element.Gale, Rarity.Common, L, Temperament.Aggressive, Locomotion.Flyer, 0.6f, 'G',
                    "A striped moor-wasp with a temper and a barb to back it up. A swarm sounds like a kettle about to boil.",
                    "Irritable, territorial, holds a grudge for one day exactly.", A("sting_tail", "gust_slash"), "venom_spit",
                    Work((WorkType.Foraging, 1)), CompanionPerk.Swiftness, "gel", "",
                    Pal(Hex("#3a2a40"), Hex("#6a4a70"), Hex("#c8e040"), Hex("#e8ff70")));
                new Plan()
                    .Blob(0.8f, 0.26f, 0.26f, 0.3f)
                    .Add(Sph, gH, cP, V(0, 0.84f, 0.2f), V(0.22f, 0.2f, 0.2f)).Head(gH, 0.84f, 0.2f, 0.22f)
                    .Face(0.08f, cG, 0.05f, 0.32f).Brows(cD, 0.05f, 0.32f)
                    .Mirror(Con, gH, cD, V(0.05f, 0.75f, 0.3f), V(0.03f, 0.1f, 0.03f), V(150, 0, -20))
                    .WhipPair(gH, gH, cD, V(0.05f, 0.93f, 0.24f), V(0.02f, 0.05f, 0.03f), V(0.004f, -0.01f, 0.006f), 4, 0.025f, 0.02f)
                    .Whip(gT, cA, V(0, 0.76f, -0.16f), V(0, -0.04f, -0.08f), V(0, -0.006f, 0.012f), 5, 0.26f, 0.1f, cD)
                    .Tip(Con, gT, cW, V(0.05f, 0.16f, 0.05f))
                    .Fins(cW, V(0.06f, 0.92f, 0), 0.46f, 0.18f, 25, 0.2f)
                    .Pivot(gH, V(0, 0.82f, 0.12f)).Pivot(gT, V(0, 0.78f, -0.12f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 153 ROTGRUB
            {
                var d = K(b, 153, "rotgrub", "Rotgrub", Element.Shade, Element.Stone, Rarity.Common, L, Temperament.Docile, Locomotion.Slither, 0.7f, 'T',
                    "A fat pale grub that chews through dead wood and dreams of being something with wings. It has been dreaming a very long time.",
                    "Hopeful, slow, eats when anxious.", A("acid_bite", "mud_shot"), "roll_crush",
                    Work((WorkType.Quarrying, 1), (WorkType.Cultivating, 1)), CompanionPerk.StoneBoost, "gel", "stone",
                    Pal(Hex("#d8d0a8"), Hex("#b0a878"), Hex("#7a4a30"), Hex("#c0ff60")));
                new Plan()
                    .Serpent(cP, cS, 0.26f, 0.48f, 5, 0.26f)
                    .Add(Sph, gH, cA, V(0, 0.28f, 0.2f), V(0.4f, 0.38f, 0.34f)).Head(gH, 0.28f, 0.2f, 0.4f)
                    .Face(0.07f, cD, 0.1f, 0.26f)
                    .Mirror(Con, gH, cD, V(0.08f, 0.18f, 0.38f), V(0.05f, 0.12f, 0.05f), V(110, 0, 20))
                    .Spikes(gB, cA, 0.0f, 0.0f, -0.5f, 3, 0.07f, 0.1f, 0, 180, 0.17f, 0)
                    .Mirror(Sph, gB, cG, V(0.2f, 0.32f, -0.26f), V(0.08f, 0.08f, 0.08f))
                    .Mirror(Sph, gT, cG, V(0.14f, 0.26f, -0.8f), V(0.07f, 0.07f, 0.07f))
                    .Pivot(gH, V(0, 0.24f, 0.06f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 154 VENOMOTH
            {
                var d = K(b, 154, "venomoth", "Venomoth", Element.Shade, Element.Gale, Rarity.Uncommon, L, Temperament.Curious, Locomotion.Flyer, 0.9f, 'B',
                    "A great soft moth whose wings stare back at you. The dust it sheds is a slow poison, which it finds very embarrassing.",
                    "Gentle, drawn to lanterns, apologises with its whole body.", A("toxic_cloud", "gust_slash"), "cyclone",
                    Work((WorkType.Crafting, 2), (WorkType.Cultivating, 1)), CompanionPerk.Glow, "fiber", "gel",
                    Pal(Hex("#6a4a8a"), Hex("#d8c8b0"), Hex("#e85a9a"), Hex("#c8ff60")));
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Add(Cap, gB, cS, V(0, 0.8f, -0.04f), V(0.24f, 0.56f, 0.24f), V(70, 0, 0))
                    .Add(Sph, gB, cW, V(0, 0.84f, 0.12f), V(0.3f, 0.26f, 0.26f))
                    .Add(Sph, gH, cS, V(0, 0.9f, 0.3f), V(0.24f, 0.22f, 0.22f)).Head(gH, 0.9f, 0.3f, 0.24f)
                    .Face(0.08f, cG, 0.05f, 0.3f)
                    .WhipPair(gH, gH, cA, V(0.05f, 1.0f, 0.34f), V(0.03f, 0.06f, 0.02f), V(0.006f, -0.008f, 0), 5, 0.04f, 0.03f)
                    .Wings(cP, cP, cA, V(0.1f, 0.86f, 0.02f), 0.8f, 0.6f, 20)
                    .Mirror(Sph, gWL, gWR, cG, V(0.57f, 1.03f, 0.02f), V(0.26f, 0.07f, 0.26f), V(0, 0, 20))
                    .Mirror(Sph, gWL, gWR, cD, V(0.57f, 1.03f, 0.02f), V(0.12f, 0.09f, 0.12f), V(0, 0, 20))
                    .PivotPair(gWL, gWR, V(0.1f, 0.86f, 0.02f)).Pivot(gH, V(0, 0.86f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 155 ACIDRAKE
            {
                var d = K(b, 155, "acidrake", "Acidrake", Element.Shade, Element.Ember, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Biped, 0.95f, 'G',
                    "A wiry little drake that spits something that smokes on stone. It can't fly on those wings and takes it out on everyone.",
                    "Snappish, cocky, has a lot to prove.", A("venom_spit", "flame_dash"), "lava_plume",
                    Work((WorkType.Kindling, 2), (WorkType.Hauling, 1)), CompanionPerk.Warmth, "cinder", "pelt",
                    Pal(Hex("#5a7a3a"), Hex("#c8d080"), Hex("#8a3ab0"), Hex("#d0ff40")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.56f, -0.04f), V(0.36f, 0.4f, 0.6f), V(-20, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.5f, 0.06f), V(0.26f, 0.3f, 0.44f), V(-20, 0, 0))
                    .Legs2(cP, 0.12f, 0.4f, 0.11f, cD)
                    .Whip(gH, cP, V(0, 0.7f, 0.22f), V(0, 0.07f, 0.06f), V(0, -0.004f, 0.008f), 3, 0.2f, 0.17f)
                    .TipHead(gH, cP, 0.3f, 0.055f, cG, false, cS).Snout(cP, 0.18f, 0.9f, false).Brows(cD).Horns(cA, 0.05f, 0.18f, 15, -50)
                    .Mirror(Cap, gAL, gAR, cP, V(0.16f, 0.56f, 0.2f), V(0.07f, 0.2f, 0.07f), V(50, 0, 0))
                    .Spikes(gB, cA, 0.72f, 0.1f, -0.3f, 4, 0.06f, 0.16f, 0.02f, -25)
                    .Tail(cP, 0.5f, -0.3f, 0.7f, 0.2f, 0.05f, 0.01f)
                    .Fins(cA, V(0.14f, 0.7f, -0.06f), 0.3f, 0.22f, 35, 0.3f)
                    .PivotPair(gAL, gAR, V(0.16f, 0.64f, 0.16f)).Pivot(gH, V(0, 0.7f, 0.2f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 156 SILKSPIN
            {
                var d = K(b, 156, "silkspin", "Silkspin", Element.Shade, Element.Normal, Rarity.Uncommon, L, Temperament.Territorial, Locomotion.Quadruped, 0.8f, 'B',
                    "A moor spider that weaves in patterns, never the same twice. Its webs are stronger than rope and it charges for them in bugs.",
                    "Fussy, artistic, hates being watched while it works.", A("acid_bite", "hex_flicker"), "coil_strike",
                    Work((WorkType.Crafting, 3), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "fiber", "",
                    Pal(Hex("#3a3048"), Hex("#5a4a6a"), Hex("#e8e0f0"), Hex("#e83a6a")));
                var p = new Plan()
                    .Add(Sph, gB, cP, V(0, 0.4f, -0.26f), V(0.56f, 0.5f, 0.64f))
                    .Add(Sph, gB, cG, V(0, 0.62f, -0.3f), V(0.2f, 0.08f, 0.26f))
                    .Add(Sph, gH, cP, V(0, 0.36f, 0.16f), V(0.36f, 0.32f, 0.36f)).Head(gH, 0.36f, 0.16f, 0.36f)
                    .Face(0.07f, cG, 0.12f, 0.2f)
                    .Mirror(Sph, gH, cG, V(0.14f, 0.43f, 0.27f), V(0.04f, 0.04f, 0.03f))
                    .Mirror(Sph, gH, cG, V(0.05f, 0.49f, 0.26f), V(0.035f, 0.035f, 0.03f))
                    .Mirror(Con, gH, cA, V(0.06f, 0.24f, 0.31f), V(0.05f, 0.12f, 0.05f), V(180, 0, 0));
                float[] zs = { 0.22f, 0.08f, -0.06f, -0.2f }, sw = { 0.06f, 0.02f, -0.03f, -0.07f };
                for (int i = 0; i < 4; i++)
                    p.WhipPair(i % 2 == 0 ? BodyGroup.LegFL : BodyGroup.LegBL, i % 2 == 0 ? BodyGroup.LegFR : BodyGroup.LegBR, cS,
                        V(0.14f, 0.36f, zs[i]), V(0.08f, 0.07f, sw[i]), V(0.004f, -0.05f, 0), 8, 0.06f, 0.035f);
                p.PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.14f, 0.36f, 0.1f)).PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.14f, 0.36f, -0.1f))
                    .Pivot(gH, V(0, 0.36f, 0.02f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 157 FUMESKULL
            {
                var d = K(b, 157, "fumeskull", "Fumeskull", Element.Shade, Element.Ember, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Flyer, 0.8f, 'G',
                    "The skull of something horned, drifting on the fumes that leak out of it. It chatters its teeth at travellers and cackles at its own jokes.",
                    "Rude, cackling, lonely underneath.", A("hex_flicker", "ember_spit"), "gloom_pulse",
                    Work((WorkType.Kindling, 2), (WorkType.Charging, 1)), CompanionPerk.Warmth, "cinder", "",
                    Pal(Hex("#e8e0c8"), Hex("#7a5a9a"), Hex("#b8a888"), Hex("#b0ff40")));
                Effects(d, AuraFx.Gloom, ExtraAnim.Bob);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.86f, 0), V(0.5f, 0.5f, 0.52f))
                    .Add(Sph, gT, cP, V(0, 0.64f, 0.08f), V(0.34f, 0.2f, 0.3f))
                    .Mirror(Sph, gB, cD, V(0.11f, 0.88f, 0.2f), V(0.16f, 0.18f, 0.1f))
                    .Mirror(Sph, gB, cG, V(0.11f, 0.88f, 0.235f), V(0.06f, 0.07f, 0.04f))
                    .Add(Con, gB, cD, V(0, 0.77f, 0.25f), V(0.06f, 0.08f, 0.04f))
                    .Mirror(Bx, gT, cD, V(0.05f, 0.64f, 0.225f), V(0.012f, 0.1f, 0.02f))
                    .Mirror(Bx, gT, cD, V(0.12f, 0.64f, 0.19f), V(0.012f, 0.1f, 0.02f))
                    .Add(Bx, gT, cD, V(0, 0.64f, 0.232f), V(0.012f, 0.1f, 0.02f))
                    .Head(gB, 0.86f, 0, 0.5f).Horns(cA, 0.09f, 0.28f, 35, -10)
                    .Add(Sph, gX, cS, V(0, 1.2f, -0.06f), V(0.3f, 0.26f, 0.3f))
                    .Add(Sph, gX, cS, V(0.12f, 1.42f, -0.1f), V(0.22f, 0.2f, 0.22f))
                    .Add(Sph, gX, cS, V(-0.06f, 1.6f, -0.14f), V(0.14f, 0.13f, 0.14f))
                    .Ring(Con, gB, cS, V(0, 0.42f, -0.04f), 0.1f, 5, V(0.12f, 0.26f, 0.12f), 175)
                    .Pivot(gX, V(0, 1.2f, 0)).Pivot(gT, V(0, 0.72f, -0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 158 BLIGHTBLOOM
            {
                var d = K(b, 158, "blightbloom", "Blightbloom", Element.Flora, Element.Shade, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Biped, 0.9f, 'T',
                    "An enormous red flower on two stubby legs. It smells like the worst day of your life and has no idea why it has so few friends.",
                    "Warm-hearted, oblivious, a hugger.", A("toxic_cloud", "vine_whip"), "petal_storm",
                    Work((WorkType.Cultivating, 3), (WorkType.Irrigating, 1)), CompanionPerk.ForageBoost, "fiber", "berry",
                    Pal(Hex("#c83a3a"), Hex("#6a7a3a"), Hex("#f0c8a0"), Hex("#e8ff90")));
                Effects(d, AuraFx.Petals);
                new Plan()
                    .Add(Sph, gB, cS, V(0, 0.3f, 0), V(0.5f, 0.44f, 0.46f))
                    .Legs2(cS, 0.14f, 0.14f, 0.14f, cD)
                    .Head(gB, 0.32f, 0f, 0.44f).Face(0.075f, cG).Cheeks(cA)
                    .Fronds(gH, cP, 5, 0.52f, 0.42f, 0.25f, V(0, 0.54f, 0))
                    .Add(Cyl, gH, cA, V(0, 0.6f, 0), V(0.4f, 0.14f, 0.4f))
                    .Add(Sph, gH, cD, V(0, 0.67f, 0), V(0.28f, 0.06f, 0.28f))
                    .Ring(Sph, gH, cG, V(0, 0.65f, 0), 0.32f, 5, V(0.09f, 0.05f, 0.09f))
                    .Ring(Sph, gH, cG, V(0, 0.68f, 0), 0.44f, 5, V(0.06f, 0.04f, 0.06f), 0, 20)
                    .Mirror(Sph, gAL, gAR, cS, V(0.3f, 0.3f, 0.04f), V(0.14f, 0.2f, 0.14f), V(0, 0, -30))
                    .Pivot(gH, V(0, 0.5f, 0)).PivotPair(gAL, gAR, V(0.24f, 0.36f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 159 RUSTCRAWLER
            {
                var d = K(b, 159, "rustcrawler", "Rustcrawler", Element.Stone, Element.Shade, Rarity.Uncommon, L, Temperament.Territorial, Locomotion.Quadruped, 1f, 'T',
                    "A plated crawler that eats metal and wears the rust. Miners used to follow them to ore, then run from them with it.",
                    "Dour, methodical, clanks when it's happy.", A("rock_jaw", "acid_bite"), "roll_crush",
                    Work((WorkType.Quarrying, 2), (WorkType.Hauling, 2)), CompanionPerk.OreSense, "ore", "stone",
                    Pal(Hex("#a85a30"), Hex("#6a4030"), Hex("#d8c0a0"), Hex("#c0ff50")));
                new Plan()
                    .Quad(0.28f, 0.5f, 0.3f, 0.9f, 0.34f, 0.14f, 0.07f, cD, cD, null, 0f, -0.04f)
                    .Shell(cP, 0.24f, 0.62f, 0.6f, 0.36f, 0.3f).Shell(cS, 0.24f, 0.66f, 0.66f, 0.36f, 0.06f)
                    .Shell(cP, 0.24f, 0.66f, 0.66f, 0.36f, -0.18f).Shell(cS, 0.24f, 0.58f, 0.56f, 0.34f, -0.4f)
                    .Face(0.055f, cG)
                    .Mirror(Con, gH, cA, V(0.1f, 0.2f, 0.62f), V(0.06f, 0.18f, 0.06f), V(100, 0, 25))
                    .WhipPair(gH, gH, cA, V(0.08f, 0.4f, 0.56f), V(0.04f, 0.04f, 0.06f), V(0.004f, -0.01f, 0), 5, 0.03f, 0.02f)
                    .Mirror(Cyl, gB, cD, V(0.3f, 0.1f, 0.14f), V(0.04f, 0.2f, 0.04f), V(0, 0, -40))
                    .Mirror(Cyl, gB, cD, V(0.3f, 0.1f, -0.1f), V(0.04f, 0.2f, 0.04f), V(0, 0, -40))
                    .Mirror(Con, gT, cA, V(0.08f, 0.26f, -0.62f), V(0.05f, 0.2f, 0.05f), V(-100, 0, -10))
                    .Pivot(gT, V(0, 0.26f, -0.5f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 160 ROTHOUND (mount)
            {
                var d = K(b, 160, "rothound", "Rothound", Element.Shade, Element.Normal, Rarity.Rare, L, Temperament.Aggressive, Locomotion.Quadruped, 1.3f, 'F',
                    "A gaunt hound with its ribs on the outside, tireless and never quite alive. It runs down anything on the moor, then waits to be told what to do with it.",
                    "Relentless, obedient, starved for praise.", A("acid_bite", "shadow_pounce"), "night_slash",
                    Work((WorkType.Hauling, 2), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "pelt", "",
                    Pal(Hex("#4a4450"), Hex("#2e2a34"), Hex("#7a4a8a"), Hex("#b8ff40")));
                Mount(d, 13f, 8.5f, V(0, 0.98f, -0.06f));
                new Plan()
                    .Quad(0.74f, 0.4f, 0.44f, 1.0f, 0.4f, 0.56f, 0.11f, foot: cD, headUp: 0.22f, headOut: 0.04f)
                    .Add(Sph, gB, cP, V(0, 0.8f, 0.3f), V(0.46f, 0.54f, 0.5f))
                    .Add(Cap, gH, cP, V(0, 0.88f, 0.5f), V(0.2f, 0.3f, 0.2f), V(40, 0, 0))
                    .Spikes(gB, cW, 0.6f, 0.14f, -0.26f, 4, 0.04f, 0.3f, 0, 0, 0.2f, -8, Cap)
                    .Face(0.06f, cG).Snout(cP, 0.2f, 1f).Brows(cD)
                    .Mirror(Con, gH, cW, V(0.06f, 0.8f, 0.86f), V(0.03f, 0.09f, 0.03f), V(180, 0, 0))
                    .Ears(Con, cP, 0.1f, 0.22f, 20, 0.04f)
                    .Spikes(gB, cA, 0.94f, 0.4f, -0.4f, 5, 0.06f, 0.2f, 0.02f, -35)
                    .Tail(cP, 0.8f, -0.5f, 0.5f, 0.07f, 0.03f, -0.05f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 161 MIASMARE (mount)
            {
                var d = K(b, 161, "miasmare", "Miasmare", Element.Shade, Element.Gale, Rarity.Rare, L, Temperament.Territorial, Locomotion.Quadruped, 1.4f, 'F',
                    "A dark horse with a mane of drifting green fog. Where it gallops the mist follows, and whole valleys have vanished behind it.",
                    "Wild, haughty, gentles for exactly one rider.", A("toxic_cloud", "stampede"), "cyclone",
                    Work((WorkType.Hauling, 3)), CompanionPerk.Swiftness, "pelt", "gel",
                    Pal(Hex("#2e2a3a"), Hex("#4a4458"), Hex("#7ad06a"), Hex("#c0ff80")));
                Effects(d, AuraFx.Gloom);
                Mount(d, 13.5f, 9f, V(0, 1.12f, -0.06f));
                new Plan()
                    .Quad(0.86f, 0.42f, 0.46f, 1.06f, 0.3f, 0.66f, 0.1f, foot: cG, headUp: 0.52f, headOut: 0.16f)
                    .Add(Cap, gH, cP, V(0, 1.1f, 0.6f), V(0.22f, 0.6f, 0.24f), V(30, 0, 0))
                    .Add(Sph, gH, cP, V(0, 1.3f, 0.9f), V(0.2f, 0.2f, 0.34f), V(25, 0, 0))
                    .Face(0.055f, cG)
                    .Ears(Con, cP, 0.06f, 0.14f, 10)
                    .Add(Sph, gH, cG, V(0, 1.54f, 0.64f), V(0.16f, 0.2f, 0.2f))
                    .Add(Sph, gH, cA, V(0, 1.4f, 0.52f), V(0.16f, 0.24f, 0.22f))
                    .Add(Sph, gH, cG, V(0, 1.24f, 0.42f), V(0.16f, 0.24f, 0.22f))
                    .Add(Sph, gH, cA, V(0, 1.1f, 0.34f), V(0.14f, 0.2f, 0.2f))
                    .Whip(gT, cG, V(0, 0.96f, -0.54f), V(0, -0.04f, -0.1f), V(0, -0.01f, 0), 6, 0.2f, 0.08f, cA)
                    .Pivot(gT, V(0, 0.96f, -0.5f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 162 SLUDGEBACK
            {
                var d = K(b, 162, "sludgeback", "Sludgeback", Element.Shade, Element.Tide, Rarity.Rare, L, Temperament.Docile, Locomotion.Quadruped, 1.5f, 'T',
                    "A wallowing giant that carries its own blight pool in a hollow on its back. Small kin bathe in it. Nobody else should.",
                    "Easygoing, yawny, moves for no one.", A("wave_crash", "toxic_cloud"), "quake_stomp",
                    Work((WorkType.Irrigating, 3), (WorkType.Hauling, 2)), CompanionPerk.Mending, "gel", "stone",
                    Pal(Hex("#5a5a70"), Hex("#9a98a8"), Hex("#4a4458"), Hex("#a8ff50")));
                d.nearWater = true;
                Effects(d, AuraFx.Gloom, ExtraAnim.Bob);
                new Plan()
                    .Quad(0.6f, 1.0f, 0.74f, 1.3f, 0.7f, 0.3f, 0.26f, foot: cD, headUp: -0.04f, headOut: -0.1f)
                    .Add(Sph, gB, cS, V(0, 0.46f, 0.1f), V(0.84f, 0.5f, 1.1f))
                    .Add(Sph, gH, cS, V(0, 0.4f, 0.98f), V(0.6f, 0.26f, 0.36f))
                    .Mirror(Sph, gH, cP, V(0.2f, 0.88f, 0.72f), V(0.16f, 0.16f, 0.16f))
                    .Eyes(gH, V(0.2f, 0.9f, 0.79f), 0.07f, cG)
                    .Mirror(Sph, gH, cP, V(0.3f, 0.84f, 0.56f), V(0.1f, 0.12f, 0.06f))
                    .Mirror(Sph, gH, cD, V(0.1f, 0.5f, 1.14f), V(0.06f, 0.04f, 0.04f))
                    .Add(Cyl, gB, cA, V(0, 0.98f, -0.1f), V(0.84f, 0.1f, 0.96f))
                    .Add(Sph, gB, cG, V(0, 1.02f, -0.1f), V(0.7f, 0.08f, 0.82f))
                    .Mirror(Sph, gB, cG, V(0.46f, 0.8f, -0.1f), V(0.1f, 0.3f, 0.14f))
                    .Add(Sph, gX, cG, V(0.16f, 1.14f, 0f), V(0.14f, 0.14f, 0.14f))
                    .Add(Sph, gX, cG, V(-0.2f, 1.1f, -0.3f), V(0.1f, 0.1f, 0.1f))
                    .Add(Sph, gX, cG, V(0.04f, 1.2f, -0.34f), V(0.08f, 0.08f, 0.08f))
                    .Tail(cP, 0.6f, -0.66f, 0.2f, 0.1f, 0.05f, -0.03f)
                    .Pivot(gX, V(0, 1.1f, -0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 163 VENOMARCH (epic)
            {
                var d = K(b, 163, "venomarch", "Venomarch", Element.Shade, Element.Stone, Rarity.Epic, L, Temperament.Aggressive, Locomotion.Slither, 1.8f, 'G',
                    "The crowned serpent of the Blightmoor. Every pool on the moor is said to have dripped from its fangs, and it counts them all as its own.",
                    "Imperious, jealous, never forgets a debt.", A("venom_spit", "coil_strike"), "gloom_pulse",
                    Work((WorkType.Quarrying, 2), (WorkType.Crafting, 2), (WorkType.Hauling, 2)), CompanionPerk.OreSense, "crystal", "ore",
                    Pal(Hex("#3a2a4a"), Hex("#8a7a5a"), Hex("#b03ac0"), Hex("#c8ff40")));
                Effects(d, AuraFx.Gloom);
                new Plan()
                    .Serpent(cP, cS, 0.26f, 0.46f, 10, 0.3f)
                    .Mirror(Sph, gH, cP, V(0.26f, 0.8f, 0.2f), V(0.5f, 0.66f, 0.08f), V(-12, 0, -12))
                    .Mirror(Sph, gH, cA, V(0.24f, 0.8f, 0.235f), V(0.34f, 0.46f, 0.06f), V(-12, 0, -12))
                    .Mirror(Sph, gH, cG, V(0.26f, 0.84f, 0.262f), V(0.12f, 0.14f, 0.04f), V(-12, 0, -12))
                    .Whip(gH, cP, V(0, 0.34f, 0.1f), V(0, 0.14f, 0.02f), V(0, -0.006f, 0.012f), 6, 0.36f, 0.26f, cS)
                    .TipHead(gH, cP, 0.4f, 0.06f, cG, false, cS).Brows(cD)
                    .Horns(cG, 0.06f, 0.2f, 28, 0).Horn(cG, 0.07f, 0.28f, 0)
                    .Mirror(Con, gH, cW, V(0.07f, 0.96f, 0.6f), V(0.04f, 0.12f, 0.04f), V(180, 0, 0))
                    .Spikes(gB, cA, 0.44f, -0.3f, -1.2f, 4, 0.07f, 0.2f, 0, -30)
                    .Add(Con, gT, cG, V(0, 0.2f, -2.9f), V(0.1f, 0.34f, 0.1f), V(-80, 0, 0))
                    .Pivot(gH, V(0, 0.34f, 0.08f))
                    .Done(d);
            }
        }
    }
}
