using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Wave 3: Sunscour Dunes natives (#134-148) - sand, sun and tomb kin. Lv 20-29.</summary>
        static void BuildWave3Desert(Bundle b)
        {
            const BiomeMask L = BiomeMask.Desert;

            // ---------------------------------------------------------------- 134 FENNICK
            {
                var d = K(b, 134, "fennick", "Fennick", Element.Normal, Element.Ember, Rarity.Common, L, Temperament.Timid, Locomotion.Quadruped, 0.6f, 'F',
                    "A pocket-sized dune fox with ears big enough to hear a beetle sneeze. It sheds the day's heat through them and sleeps till dusk.",
                    "Alert, skittish, snuggles for warmth at night.", A("quick_kick", "ember_spit"), "sand_kick",
                    Work((WorkType.Foraging, 1), (WorkType.Kindling, 1)), CompanionPerk.Swiftness, "pelt", "",
                    Pal(Hex("#f0d090"), Hex("#fff6e0"), Hex("#ffb0a0"), Hex("#ffe070")));
                new Plan()
                    .Quad(0.3f, 0.26f, 0.26f, 0.44f, 0.32f, 0.22f, 0.07f, foot: cD, headUp: 0.16f)
                    .Add(Sph, gB, cS, V(0, 0.26f, 0.06f), V(0.2f, 0.18f, 0.32f))
                    .Face(0.08f).Snout(cS, 0.13f, 0.6f).Cheeks(cS)
                    .Ears(Con, cP, 0.2f, 0.42f, 24)
                    .Ears(Con, cA, 0.12f, 0.3f, 24, -0.025f)
                    .Tail(cP, 0.34f, -0.2f, 0.36f, 0.14f, 0.16f, 0.03f, cS, 0.16f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 135 SCARABIT
            {
                var d = K(b, 135, "scarabit", "Scarabit", Element.Stone, Element.Lumen, Rarity.Common, L, Temperament.Docile, Locomotion.Quadruped, 0.55f, 'T',
                    "A jewel-backed beetle that rolls a ball of sand everywhere it goes. Nobody knows where it is taking it. Neither does the beetle.",
                    "Diligent, single-minded, polishes its shell.", A("pebble_toss", "shell_slam"), "roll_crush",
                    Work((WorkType.Quarrying, 2), (WorkType.Hauling, 1)), CompanionPerk.OreSense, "stone", "ore",
                    Pal(Hex("#2a8a9a"), Hex("#5ad0c0"), Hex("#f0c040"), Hex("#fff0a0")));
                new Plan()
                    .Quad(0.2f, 0.4f, 0.26f, 0.46f, 0.24f, 0.12f, 0.05f, cD, cD, null, 0f)
                    .Shell(cP, 0.16f, 0.5f, 0.6f, 0.54f, -0.04f)
                    .Add(Bx, gB, cD, V(0, 0.32f, -0.08f), V(0.02f, 0.31f, 0.42f))
                    .Add(Sph, gB, cG, V(0, 0.4f, 0.12f), V(0.16f, 0.08f, 0.1f))
                    .Mirror(Sph, gB, cS, V(0.12f, 0.36f, -0.1f), V(0.14f, 0.1f, 0.26f))
                    .Face(0.06f, cG)
                    .Horn(cA, 0.07f, 0.24f, 35)
                    .Mirror(Cyl, gB, cD, V(0.26f, 0.1f, -0.02f), V(0.04f, 0.2f, 0.04f), V(0, 0, -50))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 136 TUMBLIT
            {
                var d = K(b, 136, "tumblit", "Tumblit", Element.Gale, Element.Flora, Rarity.Common, L, Temperament.Timid, Locomotion.Hopper, 0.6f, 'F',
                    "A tumbleweed with a pair of eyes somewhere in the middle. It goes wherever the wind is going and calls that a plan.",
                    "Carefree, scatterbrained, impossible to offend.", A("seed_gust", "tackle"), "cyclone",
                    Work((WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "fiber", "wood",
                    Pal(Hex("#b89a60"), Hex("#5a4630"), Hex("#8ab04a"), Hex("#fff090")));
                Effects(d, AuraFx.Breeze);
                var p = new Plan().Blob(0.34f, 0.4f, 0.4f, 0.4f, cS);
                for (int i = 0; i < 11; i++)
                    p.Add(Cap, gB, i % 3 == 0 ? cS : cP, V(0, 0.34f, 0), V(0.04f, 0.66f, 0.04f), V(i * 37f, i * 71f, i * 53f));
                p.Ring(Sph, gB, cP, V(0, 0.34f, 0), 0.3f, 5, V(0.1f, 0.1f, 0.1f), 0, 20)
                    .Add(Sph, gB, cA, V(0.14f, 0.62f, 0.06f), V(0.12f, 0.03f, 0.07f), V(0, 30, 20))
                    .Add(Sph, gB, cA, V(-0.2f, 0.5f, -0.1f), V(0.1f, 0.03f, 0.06f), V(0, 70, -30))
                    .Face(0.1f, cG)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 137 MEERKIT
            {
                var d = K(b, 137, "meerkit", "Meerkit", Element.Normal, Element.Stone, Rarity.Common, L, Temperament.Curious, Locomotion.Biped, 0.7f, 'B',
                    "A sentry by nature. One Meerkit always stands watch on the dune crest while the rest dig, and they all swap when it whistles.",
                    "Watchful, bossy, takes turns very seriously.", A("sand_kick", "chip_shot"), "war_cry",
                    Work((WorkType.Quarrying, 1), (WorkType.Crafting, 1), (WorkType.Foraging, 1)), CompanionPerk.StoneBoost, "pelt", "stone",
                    Pal(Hex("#d0a868"), Hex("#f4e4c0"), Hex("#8a6a40"), Hex("#ffe090")));
                new Plan()
                    .Biped(0.22f, 0.26f, 0.5f, 0.3f, 0.08f, foot: cD)
                    .Add(Sph, gB, cS, V(0, 0.44f, 0.07f), V(0.18f, 0.38f, 0.14f))
                    .Mirror(Sph, gH, cD, V(0.075f, 0.85f, 0.115f), V(0.11f, 0.12f, 0.06f))
                    .Face(0.07f).Snout(cS, 0.12f, 0.7f)
                    .Ears(Sph, cD, 0.07f, 0.07f, 60)
                    .Add(Sph, gB, cA, V(0, 0.5f, -0.1f), V(0.2f, 0.3f, 0.08f))
                    .Tail(cP, 0.22f, -0.1f, 0.4f, 0.06f, 0.04f, -0.01f, cD, 0.06f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 138 DUNELET
            {
                var d = K(b, 138, "dunelet", "Dunelet", Element.Stone, Element.Gale, Rarity.Common, L, Temperament.Docile, Locomotion.Hopper, 0.55f, 'B',
                    "A handful of sand that the wind taught to walk. It leaves a tiny dune wherever it naps and is always sorry about your boots.",
                    "Mild, apologetic, gets everywhere.", A("sand_blast", "tackle"), "mud_shot",
                    Work((WorkType.Quarrying, 1)), CompanionPerk.StoneBoost, "stone", "",
                    Pal(Hex("#e8c880"), Hex("#f8e8b8"), Hex("#ffa0a0"), Hex("#fff4c0")));
                Effects(d, AuraFx.Breeze, ExtraAnim.Orbit);
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.12f, 0), V(0.6f, 0.24f, 0.56f))
                    .Blob(0.28f, 0.44f, 0.4f, 0.42f)
                    .Add(Con, gB, cP, V(0.02f, 0.58f, -0.04f), V(0.26f, 0.3f, 0.26f), V(-15, 0, -10))
                    .Add(Sph, gB, cS, V(0.07f, 0.74f, -0.1f), V(0.1f, 0.1f, 0.1f))
                    .Face(0.09f).Cheeks(cA)
                    .Mirror(Sph, gAL, gAR, cP, V(0.26f, 0.26f, 0.06f), V(0.14f, 0.1f, 0.12f))
                    .Ring(Sph, gX, cS, V(0, 0.2f, 0), 0.42f, 4, V(0.06f, 0.06f, 0.06f))
                    .Pivot(gX, V(0, 0.2f, 0)).PivotPair(gAL, gAR, V(0.2f, 0.26f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 139 RATTLECOIL
            {
                var d = K(b, 139, "rattlecoil", "Rattlecoil", Element.Shade, Element.Stone, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Slither, 1f, 'G',
                    "A diamond-backed viper that gives exactly one warning, and it is the rattle. Old caravan songs are mostly about not ignoring it.",
                    "Tense, proud, fair about warnings.", A("venom_spit", "coil_strike"), "sting_tail",
                    Work((WorkType.Hauling, 1), (WorkType.Crafting, 1)), CompanionPerk.OreSense, "pelt", "cinder",
                    Pal(Hex("#b08a58"), Hex("#6a4a30"), Hex("#f0e0b0"), Hex("#d0ff60")));
                new Plan()
                    .Serpent(cP, cS, 0.16f, 0.26f, 9, 0.2f)
                    .Whip(gH, cP, V(0, 0.2f, 0.06f), V(0, 0.1f, 0.03f), V(0, -0.008f, 0.014f), 4, 0.22f, 0.18f, cS)
                    .TipHead(gH, cP, 0.28f, 0.05f, cG).Brows(cD)
                    .Mirror(Con, gH, cW, V(0.05f, 0.53f, 0.37f), V(0.03f, 0.08f, 0.03f), V(180, 0, 0))
                    .Add(Sph, gH, cA, V(0, 0.57f, 0.44f), V(0.03f, 0.015f, 0.1f))
                    .Whip(gT, cA, V(0, 0.14f, -1.7f), V(0, 0.07f, -0.03f), V(0, 0, 0), 4, 0.1f, 0.06f, cW)
                    .Pivot(gH, V(0, 0.2f, 0.04f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 140 FRILLIZARD
            {
                var d = K(b, 140, "frillizard", "Frillizard", Element.Ember, Element.Stone, Rarity.Uncommon, L, Temperament.Territorial, Locomotion.Biped, 0.85f, 'B',
                    "A lizard that settles every argument by snapping its frill open like a sunrise. It has never needed a second argument.",
                    "Dramatic, hot-tempered, all bluff until it isn't.", A("ember_spit", "tail_slap"), "flame_dash",
                    Work((WorkType.Kindling, 2), (WorkType.Crafting, 1)), CompanionPerk.Warmth, "pelt", "cinder",
                    Pal(Hex("#c8703a"), Hex("#f0d8a0"), Hex("#e83a2a"), Hex("#ffd040")));
                new Plan()
                    .Fan(Sph, gH, cA, V(0, 0.78f, -0.04f), 0.24f, 9, V(0.16f, 0.32f, 0.03f), -120, 240)
                    .Fan(Sph, gH, cG, V(0, 0.78f, -0.02f), 0.18f, 7, V(0.08f, 0.2f, 0.03f), -100, 200)
                    .Biped(0.28f, 0.32f, 0.4f, 0.3f, 0.1f, foot: cD)
                    .Add(Sph, gB, cS, V(0, 0.44f, 0.08f), V(0.22f, 0.3f, 0.16f))
                    .Face(0.06f, cG).Snout(cP, 0.16f, 0.9f).Brows(cD)
                    .Tail(cP, 0.32f, -0.12f, 0.6f, 0.14f, 0.04f, 0f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 141 VULTURN
            {
                var d = K(b, 141, "vulturn", "Vulturn", Element.Gale, Element.Shade, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Flyer, 1f, 'G',
                    "A bald-headed circler that can wait longer than you can walk. Seeing one overhead is not bad luck. Seeing three is.",
                    "Morbid, patient, surprisingly tidy.", A("gust_slash", "night_slash"), "sky_dive",
                    Work((WorkType.Hauling, 2), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "pelt", "",
                    Pal(Hex("#4a3a34"), Hex("#f0e8d8"), Hex("#e08070"), Hex("#ffe060")));
                new Plan()
                    .Add(Sph, gB, cP, V(0, 0.8f, -0.04f), V(0.44f, 0.5f, 0.5f), V(-20, 0, 0))
                    .Ring(Sph, gB, cS, V(0, 1.02f, 0.1f), 0.14f, 7, V(0.14f, 0.12f, 0.14f))
                    .Whip(gH, cA, V(0, 1.04f, 0.12f), V(0, 0.06f, 0.06f), V(0, -0.012f, 0.01f), 4, 0.1f, 0.09f)
                    .TipHead(gH, cA, 0.2f, 0.045f, cG).Brows(cD).Beak(cD, 0.08f, 0.18f)
                    .Wings(cP, cP, cD, V(0.18f, 0.92f, -0.04f), 0.8f, 0.4f, 8)
                    .Add(Sph, gT, cP, V(0, 0.62f, -0.34f), V(0.26f, 0.05f, 0.3f))
                    .Mirror(Con, gB, cA, V(0.1f, 0.5f, 0.02f), V(0.05f, 0.14f, 0.05f), V(180, 0, 0))
                    .PivotPair(gWL, gWR, V(0.18f, 0.92f, -0.04f)).Pivot(gH, V(0, 1.02f, 0.1f)).Pivot(gT, V(0, 0.66f, -0.24f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 142 PRICKPEAR
            {
                var d = K(b, 142, "prickpear", "Prickpear", Element.Flora, Element.Stone, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Biped, 0.8f, 'T',
                    "A cactus that wants a hug more than anything in the world. It carries its fruit around to share and waves with both pads.",
                    "Affectionate, lonely, painfully friendly.", A("needle_volley", "root_ram"), "pollen_burst",
                    Work((WorkType.Cultivating, 2), (WorkType.Irrigating, 1)), CompanionPerk.ForageBoost, "fiber", "berry",
                    Pal(Hex("#5aa85a"), Hex("#3a7a40"), Hex("#e83a8a"), Hex("#ffe060")));
                new Plan()
                    .Add(Cap, gB, cP, V(0, 0.46f, 0), V(0.46f, 0.7f, 0.4f))
                    .Head(gB, 0.56f, 0f, 0.42f).Face(0.08f).Cheeks(cA)
                    .Add(Sph, gB, cD, V(0, 0.5f, 0.2f), V(0.08f, 0.02f, 0.03f))
                    .Mirror(Sph, gAL, gAR, cP, V(0.32f, 0.58f, 0), V(0.26f, 0.32f, 0.12f), V(0, 0, -25))
                    .Mirror(Sph, gAL, gAR, cA, V(0.42f, 0.76f, 0), V(0.1f, 0.1f, 0.1f))
                    .Add(Sph, gB, cP, V(0.06f, 0.92f, 0), V(0.28f, 0.3f, 0.12f), V(0, 0, -10))
                    .Fronds(gB, cG, 5, 0.12f, 0.08f, 0.8f, V(0.08f, 1.06f, 0))
                    .Add(Sph, gB, cA, V(0.08f, 1.08f, 0), V(0.07f, 0.07f, 0.07f))
                    .Ring(Con, gB, cW, V(0, 0.36f, 0), 0.22f, 6, V(0.025f, 0.08f, 0.025f), 90, 30)
                    .Ring(Con, gB, cW, V(0, 0.78f, 0), 0.19f, 5, V(0.025f, 0.08f, 0.025f), 70, 36)
                    .Legs2(cS, 0.1f, 0.14f, 0.1f, cD)
                    .PivotPair(gAL, gAR, V(0.22f, 0.5f, 0))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 143 DROMBLE (mount)
            {
                var d = K(b, 143, "dromble", "Dromble", Element.Normal, Element.Stone, Rarity.Uncommon, L, Temperament.Docile, Locomotion.Quadruped, 1.4f, 'T',
                    "A two-humped plodder that carries half a caravan and complains the whole way. It can cross the Sunscour without a drink and will never let you forget it.",
                    "Grumbly, dependable, spits when ignored.", A("sand_kick", "stampede"), "mud_shot",
                    Work((WorkType.Hauling, 3), (WorkType.Irrigating, 1)), CompanionPerk.Swiftness, "pelt", "",
                    Pal(Hex("#c8a060"), Hex("#8a6a40"), Hex("#c83a4a"), Hex("#ffd060")));
                Mount(d, 11f, 7f, V(0, 1.2f, -0.03f));
                new Plan()
                    .Quad(0.9f, 0.5f, 0.5f, 1.0f, 0.3f, 0.7f, 0.12f, cP, cP, cD, 0.5f, 0.2f)
                    .Add(Cap, gH, cP, V(0, 1.14f, 0.6f), V(0.2f, 0.66f, 0.2f), V(28, 0, 0))
                    .Add(Sph, gB, cP, V(0, 1.22f, 0.22f), V(0.34f, 0.4f, 0.36f))
                    .Add(Sph, gB, cP, V(0, 1.22f, -0.28f), V(0.34f, 0.4f, 0.36f))
                    .Add(Sph, gB, cS, V(0, 1.42f, 0.22f), V(0.2f, 0.1f, 0.2f))
                    .Add(Sph, gB, cS, V(0, 1.42f, -0.28f), V(0.2f, 0.1f, 0.2f))
                    .Add(Sph, gB, cA, V(0, 1.1f, -0.03f), V(0.58f, 0.14f, 0.3f))
                    .Mirror(Sph, gB, cG, V(0.28f, 1.0f, -0.03f), V(0.04f, 0.1f, 0.26f))
                    .Face(0.05f).Snout(cS, 0.18f, 0.8f)
                    .Ears(Sph, cP, 0.06f, 0.1f, 50)
                    .Tail(cP, 0.96f, -0.5f, 0.36f, 0.05f, 0.04f, -0.07f, cS, 0.1f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 144 SANDJACKAL
            {
                var d = K(b, 144, "sandjackal", "Sandjackal", Element.Shade, Element.Normal, Rarity.Uncommon, L, Temperament.Aggressive, Locomotion.Quadruped, 0.95f, 'F',
                    "A lean black jackal with a gold collar nobody remembers giving it. It guards old tombs out of habit and hunts trespassers out of interest.",
                    "Solemn, loyal, never blinks first.", A("night_slash", "sand_kick"), "shadow_pounce",
                    Work((WorkType.Foraging, 2), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "pelt", "",
                    Pal(Hex("#3a3438"), Hex("#c8a868"), Hex("#f0c040"), Hex("#70f0e0")));
                new Plan()
                    .Quad(0.56f, 0.28f, 0.32f, 0.72f, 0.3f, 0.44f, 0.07f, foot: cS, headUp: 0.24f, headOut: 0.02f)
                    .Add(Cap, gH, cP, V(0, 0.7f, 0.36f), V(0.16f, 0.3f, 0.16f), V(35, 0, 0))
                    .Add(Cyl, gH, cA, V(0, 0.7f, 0.36f), V(0.2f, 0.06f, 0.2f), V(35, 0, 0))
                    .Add(Sph, gB, cS, V(0, 0.5f, 0.04f), V(0.22f, 0.2f, 0.56f))
                    .Face(0.055f, cG).Snout(cP, 0.13f, 1f).Brows(cD)
                    .Ears(Con, cP, 0.12f, 0.34f, 8)
                    .Ears(Con, cA, 0.06f, 0.2f, 8, -0.02f)
                    .Tail(cP, 0.6f, -0.36f, 0.4f, 0.1f, 0.06f, -0.04f, cS, 0.1f)
                    .Done(d);
            }

            // ---------------------------------------------------------------- 145 SUNSPHINX (mount)
            {
                var d = K(b, 145, "sunsphinx", "Sunsphinx", Element.Lumen, Element.Stone, Rarity.Rare, L, Temperament.Territorial, Locomotion.Quadruped, 1.4f, 'B',
                    "A winged lion in a striped headdress that asks every traveller a riddle. It has forgotten the answers and accepts anything said with confidence.",
                    "Regal, vain, secretly bored.", A("glint_beam", "radiant_charge"), "solar_flare",
                    Work((WorkType.Crafting, 2), (WorkType.Kindling, 2), (WorkType.Hauling, 2)), CompanionPerk.Glow, "crystal", "pelt",
                    Pal(Hex("#e0b060"), Hex("#f8e8c0"), Hex("#2a6ab0"), Hex("#ffe070")));
                Effects(d, AuraFx.Glitter);
                Mount(d, 12.5f, 8.5f, V(0, 1.06f, -0.1f));
                new Plan()
                    .Quad(0.74f, 0.56f, 0.52f, 1.1f, 0.44f, 0.5f, 0.16f, foot: cS, headUp: 0.36f, headOut: -0.04f)
                    .Add(Sph, gB, cP, V(0, 0.86f, 0.4f), V(0.5f, 0.56f, 0.4f))
                    .Add(Dom, gH, cA, V(0, 1.2f, 0.54f), V(0.54f, 0.5f, 0.5f))
                    .Add(Cyl, gH, cG, V(0, 1.2f, 0.55f), V(0.5f, 0.05f, 0.48f))
                    .Mirror(Sph, gH, cA, V(0.23f, 0.94f, 0.58f), V(0.14f, 0.44f, 0.1f), V(0, 0, -8))
                    .Mirror(Sph, gH, cG, V(0.24f, 0.98f, 0.63f), V(0.1f, 0.04f, 0.03f))
                    .Mirror(Sph, gH, cG, V(0.25f, 0.86f, 0.63f), V(0.1f, 0.04f, 0.03f))
                    .Add(Sph, gH, cG, V(0, 1.26f, 0.78f), V(0.1f, 0.12f, 0.06f))
                    .Face(0.06f, cG).Snout(cS, 0.16f, 0.5f)
                    .Wings(cS, cA, cG, V(0.24f, 1.0f, 0.1f), 0.6f, 0.36f, 40)
                    .Tail(cP, 0.8f, -0.56f, 0.5f, 0.07f, 0.05f, 0.04f, cA, 0.14f, 0.012f)
                    .PivotPair(gWL, gWR, V(0.24f, 1.0f, 0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 146 SANDWYRM
            {
                var d = K(b, 146, "sandwyrm", "Sandwyrm", Element.Stone, Element.Gale, Rarity.Rare, L, Temperament.Aggressive, Locomotion.Slither, 1.6f, 'G',
                    "The dunes move because something is moving under them. A Sandwyrm surfaces mouth-first, and its mouth is most of it.",
                    "Ravenous, simple, follows footsteps.", A("rock_jaw", "sand_blast"), "roll_crush",
                    Work((WorkType.Quarrying, 3), (WorkType.Hauling, 2)), CompanionPerk.OreSense, "stone", "ore",
                    Pal(Hex("#c8a068"), Hex("#8a6a48"), Hex("#f0e0b8"), Hex("#ff7a3a")));
                Effects(d, AuraFx.Breeze);
                new Plan()
                    .Serpent(cP, cS, 0.36f, 0.6f, 9, 0.4f)
                    .Add(Sph, gH, cP, V(0, 0.44f, 0.3f), V(0.66f, 0.62f, 0.6f))
                    .Add(Cyl, gH, cD, V(0, 0.42f, 0.58f), V(0.4f, 0.06f, 0.4f), V(90, 0, 0))
                    .Fan(Con, gH, cW, V(0, 0.42f, 0.6f), 0.17f, 8, V(0.06f, 0.14f, 0.06f))
                    .Fan(Con, gH, cA, V(0, 0.42f, 0.5f), 0.34f, 4, V(0.1f, 0.36f, 0.1f), 45)
                    .Mirror(Sph, gH, cG, V(0.2f, 0.66f, 0.42f), V(0.07f, 0.07f, 0.05f))
                    .Mirror(Sph, gH, cG, V(0.1f, 0.72f, 0.38f), V(0.05f, 0.05f, 0.04f))
                    .Spikes(gB, cA, 0.56f, -0.1f, -1.3f, 4, 0.1f, 0.26f, 0, -30)
                    .Spikes(gT, cA, 0.36f, -1.9f, -3.0f, 3, 0.08f, 0.2f, 0, -30)
                    .Pivot(gH, V(0, 0.4f, 0.1f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 147 WRAPSCAL
            {
                var d = K(b, 147, "wrapscal", "Wrapscal", Element.Shade, Element.Normal, Rarity.Rare, L, Temperament.Curious, Locomotion.Biped, 0.8f, 'B',
                    "Nobody has ever seen what is under the bandages, and it unwinds a little more every year. It collects shiny things in case one of them is its own.",
                    "Light-fingered, giggly, afraid of loose threads.", A("whip_crack", "hex_flicker"), "gloom_pulse",
                    Work((WorkType.Crafting, 3), (WorkType.Hauling, 1)), CompanionPerk.OreSense, "fiber", "crystal",
                    Pal(Hex("#e8dcc0"), Hex("#b8a888"), Hex("#f0c040"), Hex("#70f0c0")));
                new Plan()
                    .Biped(0.24f, 0.34f, 0.4f, 0.38f, 0.1f, cP, cP, cD)
                    .Add(Cyl, gB, cS, V(0, 0.38f, 0), V(0.36f, 0.05f, 0.33f), V(0, 0, 12))
                    .Add(Cyl, gB, cS, V(0, 0.52f, 0), V(0.35f, 0.05f, 0.32f), V(0, 0, -10))
                    .Add(Cyl, gH, cS, V(0, 0.9f, 0.02f), V(0.33f, 0.04f, 0.32f), V(0, 0, 14))
                    .Add(Sph, gH, cD, V(0, 0.79f, 0.11f), V(0.3f, 0.14f, 0.16f))
                    .Face(0.09f, cG, 0.06f)
                    .Ears(Con, cP, 0.1f, 0.16f, 20)
                    .Add(Sph, gB, cA, V(0, 0.56f, 0.15f), V(0.1f, 0.1f, 0.05f))
                    .Whip(gT, cP, V(0, 0.5f, -0.14f), V(0, -0.02f, -0.08f), V(0, 0.012f, 0), 7, 0.07f, 0.05f, cS)
                    .Whip(gH, cP, V(0.1f, 0.9f, -0.1f), V(0.03f, 0f, -0.07f), V(0, -0.01f, 0), 5, 0.06f, 0.04f, cS)
                    .Pivot(gT, V(0, 0.5f, -0.12f))
                    .Done(d);
            }

            // ---------------------------------------------------------------- 148 DJINNADO (epic)
            {
                var d = K(b, 148, "djinnado", "Djinnado", Element.Gale, Element.Ember, Rarity.Epic, L, Temperament.Territorial, Locomotion.Flyer, 1.7f, 'G',
                    "A spirit of the hot wind, horned and gold-braced, riding the whirlwind it is made of. It grants exactly no wishes and finds the question hilarious.",
                    "Booming, theatrical, respects a good bargain.", A("cyclone", "lava_plume"), "sky_dive",
                    Work((WorkType.Kindling, 3), (WorkType.Hauling, 2), (WorkType.Crafting, 2)), CompanionPerk.Warmth, "cinder", "crystal",
                    Pal(Hex("#d07a3a"), Hex("#f0c890"), Hex("#f0c040"), Hex("#ff6a2a")));
                Effects(d, AuraFx.Embers, ExtraAnim.Orbit);
                var p = new Plan();
                for (int i = 0; i < 6; i++)
                {
                    float k = i / 5f;
                    p.Add(Sph, gT, i % 2 == 0 ? cS : cP, V(Mathf.Sin(i * 1.3f) * 0.05f, 0.14f + k * 0.62f, 0), V(0.14f + k * 0.52f, 0.17f, 0.14f + k * 0.52f));
                }
                p.Add(Sph, gB, cP, V(0, 1.1f, 0), V(0.7f, 0.6f, 0.5f))
                    .Add(Sph, gB, cS, V(0, 1.0f, 0.12f), V(0.44f, 0.36f, 0.3f))
                    .Add(Cyl, gB, cA, V(0, 0.84f, 0), V(0.62f, 0.1f, 0.5f))
                    .Add(Sph, gH, cP, V(0, 1.56f, 0.04f), V(0.4f, 0.4f, 0.38f)).Head(gH, 1.56f, 0.04f, 0.4f)
                    .Face(0.07f, cG).Brows(cD)
                    .Horns(cA, 0.08f, 0.3f, 30, -10)
                    .Add(Con, gH, cS, V(0, 1.33f, 0.2f), V(0.1f, 0.2f, 0.1f), V(170, 0, 0))
                    .Mirror(Sph, gAL, gAR, cP, V(0.46f, 1.2f, 0), V(0.3f, 0.3f, 0.3f))
                    .Mirror(Cap, gAL, gAR, cP, V(0.6f, 0.98f, 0.1f), V(0.2f, 0.5f, 0.2f), V(-30, 0, -20))
                    .Mirror(Cyl, gAL, gAR, cA, V(0.66f, 0.84f, 0.2f), V(0.24f, 0.08f, 0.24f), V(-30, 0, -20))
                    .Mirror(Sph, gAL, gAR, cG, V(0.7f, 0.72f, 0.28f), V(0.2f, 0.2f, 0.2f))
                    .Ring(Sph, gX, cG, V(0, 0.5f, 0), 0.6f, 4, V(0.12f, 0.12f, 0.12f))
                    .PivotPair(gAL, gAR, V(0.44f, 1.24f, 0)).Pivot(gH, V(0, 1.38f, 0)).Pivot(gT, V(0, 0.8f, 0)).Pivot(gX, V(0, 0.5f, 0))
                    .Done(d);
            }
        }
    }
}
