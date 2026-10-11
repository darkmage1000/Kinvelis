using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Fluent helper to author procedural body plans. +X = right, +Z = forward, feet at y=0.</summary>
        partial class Plan
        {
            public readonly List<BodyPart> parts = new List<BodyPart>();
            public readonly List<GroupPivot> pivots = new List<GroupPivot>();

            public Plan Add(PartShape s, BodyGroup g, ColorSlot c, Vector3 p, Vector3 sc, Vector3 r = default)
            {
                parts.Add(new BodyPart(s, g, c, p, sc, r));
                return this;
            }

            /// <summary>Adds a right-side part at +x and its mirror at -x (possibly in different groups).</summary>
            public Plan Mirror(PartShape s, BodyGroup left, BodyGroup right, ColorSlot c, Vector3 p, Vector3 sc, Vector3 r = default)
            {
                parts.Add(new BodyPart(s, right, c, p, sc, r));
                parts.Add(new BodyPart(s, left, c, new Vector3(-p.x, p.y, p.z), sc, new Vector3(r.x, -r.y, -r.z)));
                return this;
            }

            public Plan Mirror(PartShape s, BodyGroup g, ColorSlot c, Vector3 p, Vector3 sc, Vector3 r = default) => Mirror(s, g, g, c, p, sc, r);

            /// <summary>Two eyes with a white catch-light.</summary>
            public Plan Eyes(BodyGroup g, Vector3 p, float size, ColorSlot slot = ColorSlot.Eye)
            {
                Mirror(PartShape.Sphere, g, slot, p, new Vector3(size, size * 1.15f, size * 0.7f));
                Mirror(PartShape.Sphere, g, ColorSlot.White, p + new Vector3(0.015f, size * 0.25f, size * 0.3f), Vector3.one * size * 0.35f);
                return this;
            }

            public Plan Pivot(BodyGroup g, Vector3 p) { pivots.Add(new GroupPivot(g, p)); return this; }
            public Plan PivotPair(BodyGroup left, BodyGroup right, Vector3 p)
            {
                pivots.Add(new GroupPivot(right, p));
                pivots.Add(new GroupPivot(left, new Vector3(-p.x, p.y, p.z)));
                return this;
            }

            public void ApplyTo(SpeciesDef d) { d.parts = parts.ToArray(); d.pivots = pivots.ToArray(); }
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        static SpeciesDef Sp(Bundle b, int dex, string id, string name, Element e1, Element e2, Rarity r, BiomeMask biomes,
            Temperament temp, Locomotion loco, int hp, int atk, int def, float walk, float run, float size)
        {
            var d = ScriptableObject.CreateInstance<SpeciesDef>();
            d.name = $"Species_{dex:000}_{id}";
            d.id = id; d.dexNumber = dex; d.displayName = name; d.element1 = e1; d.element2 = e2; d.rarity = r; d.biomes = biomes;
            d.temperament = temp; d.locomotion = loco; d.baseHp = hp; d.baseAtk = atk; d.baseDef = def; d.walkSpeed = walk; d.runSpeed = run; d.size = size;
            b.species.Add(d);
            return d;
        }

        static WorkSkill[] Work(params (WorkType, int)[] w)
        {
            var a = new WorkSkill[w.Length];
            for (int i = 0; i < w.Length; i++) a[i] = new WorkSkill(w[i].Item1, w[i].Item2);
            return a;
        }

        static DropEntry[] Drops(params (string, int, int, float)[] d)
        {
            var a = new DropEntry[d.Length];
            for (int i = 0; i < d.Length; i++) a[i] = new DropEntry(d[i].Item1, d[i].Item2, d[i].Item3, d[i].Item4);
            return a;
        }

        static void BuildSpecies(Bundle b)
        {
            // ------------------------------------------------------------------ 001 SPROUTLE
            {
                var d = Sp(b, 1, "sproutle", "Sproutle", Element.Flora, Element.Normal, Rarity.Common, BiomeMask.Meadow,
                    Temperament.Timid, Locomotion.Biped, 50, 8, 9, 2.2f, 5.5f, 0.85f);
                d.lore = "A walking flower bulb that naps in sunbeams. Scared easily, but plants anything it touches.";
                d.personality = "Shy, sun-loving, hums when content.";
                d.spawnWeight = 3f; d.minGroup = 2; d.maxGroup = 4;
                d.abilities = new[] { "tackle", "leaf_dart" }; d.rareAbilities = new[] { "spore_puff" };
                d.work = Work((WorkType.Cultivating, 2), (WorkType.Foraging, 1), (WorkType.Crafting, 1));
                d.perk = CompanionPerk.ForageBoost; d.overworldRole = "Companion: +50% berries and fiber when gathering.";
                d.drops = Drops(("fiber", 1, 3, 1f), ("berry", 1, 2, 0.5f));
                d.palette = Pal(Hex("#8ed86a"), Hex("#f6efc8"), Hex("#3fa04a"), Hex("#ff8fc0"));
                d.shimmerPalette = Pal(Hex("#ffe27a"), Hex("#fff6dc"), Hex("#e0602a"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.4f, 0), V(0.64f, 0.62f, 0.6f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.33f, 0.13f), V(0.44f, 0.42f, 0.38f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0, 0.76f, 0), V(0.05f, 0.18f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0.13f, 0.9f, 0), V(0.14f, 0.36f, 0.05f), V(0, 0, -38))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0, 0.88f, 0.02f), V(0.11f, 0.11f, 0.11f))
                    .Eyes(BodyGroup.Head, V(0.12f, 0.47f, 0.27f), 0.1f)
                    .Mirror(PartShape.Capsule, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.14f, 0.08f, 0), V(0.14f, 0.2f, 0.14f))
                    .Mirror(PartShape.Sphere, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Accent, V(0.31f, 0.4f, 0.02f), V(0.08f, 0.17f, 0.06f), V(0, 0, -40))
                    .Pivot(BodyGroup.Head, V(0, 0.62f, 0))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.14f, 0.17f, 0))
                    .PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.27f, 0.46f, 0));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 002 PEBBLUMP
            {
                var d = Sp(b, 2, "pebblump", "Pebblump", Element.Stone, Element.Normal, Rarity.Common, BiomeMask.Meadow | BiomeMask.Badlands,
                    Temperament.Docile, Locomotion.Quadruped, 75, 9, 15, 1.6f, 3.6f, 1f);
                d.lore = "A tortoise whose shell is a pile of river stones. It rearranges them when bored.";
                d.personality = "Patient, stubborn, loves sunbathing on warm rocks.";
                d.spawnWeight = 2.5f; d.minGroup = 1; d.maxGroup = 2;
                d.abilities = new[] { "tackle", "shell_slam" }; d.rareAbilities = new[] { "pebble_toss" };
                d.work = Work((WorkType.Quarrying, 2), (WorkType.Hauling, 1));
                d.perk = CompanionPerk.StoneBoost; d.overworldRole = "Companion: +50% stone and ore from rocks.";
                d.drops = Drops(("stone", 2, 4, 1f), ("ore", 1, 1, 0.25f));
                d.palette = Pal(Hex("#8f8a80"), Hex("#d4b47c"), Hex("#6f9a52"), Hex("#b8e0ff"));
                d.shimmerPalette = Pal(Hex("#4a5a8a"), Hex("#e8d8b0"), Hex("#c9a0ff"), Hex("#ffffff"));
                d.regional = new RegionalForm
                {
                    enabled = true, biome = BiomeId.Peaks, formName = "Rimeshell", element2 = Element.Frost,
                    palette = Pal(Hex("#cfe8f5"), Hex("#9ab2c8"), Hex("#7de0ff"), Hex("#e0faff"))
                };
                var p = new Plan()
                    .Add(PartShape.Dome, BodyGroup.Body, ColorSlot.Primary, V(0, 0.2f, 0), V(1.0f, 0.8f, 1.1f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.2f, 0), V(0.96f, 0.1f, 1.06f))
                    .Add(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, V(0.2f, 0.52f, 0.1f), V(0.24f, 0.2f, 0.24f))
                    .Add(PartShape.Rock, BodyGroup.Body, ColorSlot.Primary, V(-0.18f, 0.55f, -0.14f), V(0.26f, 0.22f, 0.26f))
                    .Add(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, V(0.02f, 0.6f, -0.02f), V(0.3f, 0.24f, 0.3f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.32f, 0.62f), V(0.34f, 0.3f, 0.36f))
                    .Eyes(BodyGroup.Head, V(0.09f, 0.37f, 0.76f), 0.07f)
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.3f, 0.1f, 0.32f), V(0.2f, 0.22f, 0.2f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Secondary, V(0.3f, 0.1f, -0.32f), V(0.2f, 0.22f, 0.2f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Secondary, V(0, 0.2f, -0.6f), V(0.1f, 0.22f, 0.1f), V(-90, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.3f, 0.45f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.3f, 0.21f, 0.32f))
                    .PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.3f, 0.21f, -0.32f))
                    .Pivot(BodyGroup.Tail, V(0, 0.2f, -0.5f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 003 EMBERKIT
            {
                var d = Sp(b, 3, "emberkit", "Emberkit", Element.Ember, Element.Normal, Rarity.Uncommon, BiomeMask.Badlands,
                    Temperament.Curious, Locomotion.Quadruped, 55, 14, 9, 3f, 8f, 0.9f);
                d.lore = "A quick fox whose tail is a living flame. It flicks sparks to start campfires for travellers it likes.";
                d.personality = "Mischievous, loyal once befriended, hates rain.";
                d.spawnWeight = 2f; d.minGroup = 1; d.maxGroup = 2;
                d.abilities = new[] { "ember_spit", "flame_dash" }; d.rareAbilities = new[] { "shadow_pounce" };
                d.work = Work((WorkType.Kindling, 2), (WorkType.Crafting, 1));
                d.perk = CompanionPerk.Warmth; d.overworldRole = "Companion: warmth aura (slower hunger) and lights the dark.";
                d.drops = Drops(("pelt", 1, 2, 0.8f), ("cinder", 1, 1, 0.4f));
                d.palette = Pal(Hex("#f06a32"), Hex("#fff0d6"), Hex("#ff9a1a"), Hex("#ffe45a"));
                d.shimmerPalette = Pal(Hex("#5a4ad8"), Hex("#e8e4ff"), Hex("#3ad8ff"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 0.42f, 0), V(0.32f, 0.72f, 0.32f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.4f, 0.22f), V(0.3f, 0.3f, 0.3f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.63f, 0.42f), V(0.34f, 0.3f, 0.32f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.59f, 0.6f), V(0.15f, 0.18f, 0.14f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.595f, 0.69f), V(0.05f, 0.05f, 0.05f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.1f, 0.82f, 0.4f), V(0.11f, 0.22f, 0.08f), V(0, 0, -12))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Dark, V(0.1f, 0.84f, 0.415f), V(0.06f, 0.14f, 0.04f), V(0, 0, -12))
                    .Eyes(BodyGroup.Head, V(0.09f, 0.67f, 0.55f), 0.07f)
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Primary, V(0.12f, 0.15f, 0.2f), V(0.08f, 0.3f, 0.08f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Primary, V(0.12f, 0.15f, -0.2f), V(0.08f, 0.3f, 0.08f))
                    .Mirror(PartShape.Sphere, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Dark, V(0.12f, 0.02f, 0.22f), V(0.09f, 0.06f, 0.11f))
                    .Mirror(PartShape.Sphere, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Dark, V(0.12f, 0.02f, -0.18f), V(0.09f, 0.06f, 0.11f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Accent, V(0, 0.6f, -0.5f), V(0.24f, 0.46f, 0.24f), V(-50, 0, 0))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.74f, -0.62f), V(0.15f, 0.32f, 0.15f), V(-38, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.55f, 0.3f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.12f, 0.3f, 0.2f))
                    .PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.12f, 0.3f, -0.2f))
                    .Pivot(BodyGroup.Tail, V(0, 0.46f, -0.33f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 004 DRIPLET
            {
                var d = Sp(b, 4, "driplet", "Driplet", Element.Tide, Element.Normal, Rarity.Common, BiomeMask.Meadow,
                    Temperament.Timid, Locomotion.Hopper, 48, 9, 10, 2f, 5f, 0.8f);
                d.lore = "A wobbling droplet that gathers morning dew. Its bubble crown pops when startled.";
                d.personality = "Bouncy, curious about reflections, cries when thirsty.";
                d.nearWater = true; d.spawnWeight = 2.5f; d.minGroup = 2; d.maxGroup = 3;
                d.abilities = new[] { "tackle", "bubble_burst" }; d.rareAbilities = new[] { "frost_breath" };
                d.work = Work((WorkType.Irrigating, 2), (WorkType.Hauling, 1));
                d.perk = CompanionPerk.Mending; d.overworldRole = "Companion: slowly restores your HP.";
                d.drops = Drops(("gel", 1, 2, 1f));
                d.palette = Pal(Hex("#4ab8ff"), Hex("#2a7ee0"), Hex("#d8f4ff"), Hex("#aef0ff"));
                d.shimmerPalette = Pal(Hex("#ff8ad8"), Hex("#d04aa8"), Hex("#fff0fa"), Hex("#ffffff"));
                d.regional = new RegionalForm
                {
                    enabled = true, biome = BiomeId.Badlands, formName = "Mire", element2 = Element.Stone,
                    palette = Pal(Hex("#8a7a5a"), Hex("#5a6a4a"), Hex("#c9e0a0"), Hex("#d8ff9a"))
                };
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.28f, 0), V(0.64f, 0.52f, 0.64f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.04f, 0), V(0.74f, 0.07f, 0.74f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.White, V(0.14f, 0.44f, 0.12f), V(0.1f, 0.06f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0, 0.6f, 0), V(0.15f, 0.15f, 0.15f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0.13f, 0.56f, -0.06f), V(0.1f, 0.1f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(-0.11f, 0.57f, 0.05f), V(0.1f, 0.1f, 0.1f))
                    .Eyes(BodyGroup.Head, V(0.12f, 0.34f, 0.28f), 0.1f)
                    .Pivot(BodyGroup.Head, V(0, 0.3f, 0));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 005 VOLTBEAK
            {
                var d = Sp(b, 5, "voltbeak", "Voltbeak", Element.Volt, Element.Gale, Rarity.Uncommon, BiomeMask.Meadow | BiomeMask.Badlands,
                    Temperament.Territorial, Locomotion.Biped, 58, 13, 10, 2.6f, 7f, 1f);
                d.lore = "A strutting bird that stores static in its zig-zag crest. Rival flocks settle disputes with crackling dances.";
                d.personality = "Proud, loud, fiercely protective of its nest.";
                d.spawnWeight = 1.6f; d.minGroup = 1; d.maxGroup = 3;
                d.abilities = new[] { "spark_peck", "thunder_chirp" }; d.rareAbilities = new[] { "ember_spit" };
                d.work = Work((WorkType.Charging, 2), (WorkType.Hauling, 2));
                d.perk = CompanionPerk.Swiftness; d.overworldRole = "Companion: +12% run speed for you.";
                d.drops = Drops(("pelt", 1, 2, 0.8f), ("fiber", 1, 2, 0.5f));
                d.palette = Pal(Hex("#ffd83a"), Hex("#fff6d8"), Hex("#3a3a48"), Hex("#56f0ff"));
                d.shimmerPalette = Pal(Hex("#2a2a36"), Hex("#6a6a7a"), Hex("#e0e0e0"), Hex("#ff4ad8"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.55f, 0), V(0.5f, 0.56f, 0.62f), V(-15, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.5f, 0.11f), V(0.38f, 0.42f, 0.42f))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.44f, -0.36f), V(0.26f, 0.05f, 0.32f), V(-25, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.38f, -0.52f), V(0.2f, 0.04f, 0.12f), V(-25, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.93f, 0.16f), V(0.34f, 0.32f, 0.34f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0, 0.9f, 0.4f), V(0.12f, 0.26f, 0.1f), V(90, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 1.12f, 0.12f), V(0.05f, 0.2f, 0.09f), V(22, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 1.21f, 0.04f), V(0.05f, 0.16f, 0.08f), V(-32, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 1.29f, 0.08f), V(0.05f, 0.12f, 0.07f), V(26, 0, 0))
                    .Eyes(BodyGroup.Head, V(0.12f, 0.97f, 0.28f), 0.07f)
                    .Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Primary, V(0.34f, 0.55f, -0.05f), V(0.1f, 0.34f, 0.5f), V(0, 0, -15))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Glow, V(0.37f, 0.42f, -0.22f), V(0.04f, 0.12f, 0.2f), V(0, 0, -15))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Accent, V(0.1f, 0.16f, 0.02f), V(0.05f, 0.32f, 0.05f))
                    .Mirror(PartShape.Box, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Accent, V(0.1f, 0.015f, 0.08f), V(0.12f, 0.03f, 0.16f))
                    .Pivot(BodyGroup.Head, V(0, 0.8f, 0.1f))
                    .PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.24f, 0.64f, 0))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.1f, 0.32f, 0.02f))
                    .Pivot(BodyGroup.Tail, V(0, 0.45f, -0.25f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 006 FROSTUFT
            {
                var d = Sp(b, 6, "frostuft", "Frostuft", Element.Frost, Element.Normal, Rarity.Common, BiomeMask.Peaks,
                    Temperament.Docile, Locomotion.Quadruped, 90, 11, 13, 1.8f, 4.5f, 1.25f);
                d.lore = "A shaggy mountain grazer whose wool is so cold snow never melts on it. Herds huddle into drifts during blizzards.";
                d.personality = "Calm, sleepy, fiercely loyal to its herd.";
                d.spawnWeight = 3f; d.minGroup = 2; d.maxGroup = 4;
                d.abilities = new[] { "tackle", "frost_breath" }; d.rareAbilities = new[] { "shell_slam" };
                d.work = Work((WorkType.Chilling, 2), (WorkType.Hauling, 2), (WorkType.Foraging, 1));
                d.perk = CompanionPerk.LumberBoost; d.overworldRole = "Companion: +50% wood (it headbutts trees for you). MOUNT: steady ride that shrugs off blizzards.";
                d.mountable = true; d.mountSpeed = 10.5f; d.mountJump = 7.5f; d.mountSeat = V(0, 1.3f, -0.05f);
                d.drops = Drops(("pelt", 2, 4, 1f));
                d.palette = Pal(Hex("#eef6ff"), Hex("#6a7f9e"), Hex("#9fb4d0"), Hex("#b8f0ff"));
                d.shimmerPalette = Pal(Hex("#ffc4de"), Hex("#9e6a8a"), Hex("#ffe0f0"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.78f, 0), V(0.95f, 0.8f, 1.2f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.26f, 0.88f, 0.3f), V(0.6f, 0.6f, 0.6f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.22f, 0.88f, -0.36f), V(0.6f, 0.6f, 0.6f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, V(0, 0.5f, 0), V(0.92f, 0.45f, 1.12f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.74f, 0.76f), V(0.4f, 0.4f, 0.36f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0, 0.65f, 0.91f), V(0.25f, 0.18f, 0.14f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.93f, 0.8f), V(0.44f, 0.2f, 0.22f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.White, V(0.26f, 0.95f, 0.72f), V(0.1f, 0.36f, 0.1f), V(0, 0, -62))
                    .Eyes(BodyGroup.Head, V(0.1f, 0.8f, 0.9f), 0.07f)
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.3f, 0.18f, 0.38f), V(0.2f, 0.36f, 0.2f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Secondary, V(0.3f, 0.18f, -0.38f), V(0.2f, 0.36f, 0.2f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Dark, V(0.3f, 0.025f, 0.38f), V(0.22f, 0.05f, 0.22f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Dark, V(0.3f, 0.025f, -0.38f), V(0.22f, 0.05f, 0.22f))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.82f, -0.74f), V(0.22f, 0.22f, 0.26f))
                    .Pivot(BodyGroup.Head, V(0, 0.8f, 0.55f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.3f, 0.36f, 0.38f))
                    .PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.3f, 0.36f, -0.38f))
                    .Pivot(BodyGroup.Tail, V(0, 0.82f, -0.6f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 007 BARKHORN
            {
                var d = Sp(b, 7, "barkhorn", "Barkhorn", Element.Flora, Element.Stone, Rarity.Rare, BiomeMask.Meadow,
                    Temperament.Territorial, Locomotion.Quadruped, 110, 17, 15, 2.4f, 7.5f, 1.35f);
                d.lore = "A towering stag whose antlers are living branches. Old Barkhorns grow whole thickets on their heads.";
                d.personality = "Regal, wary, gentle with fawns of any species.";
                d.spawnWeight = 0.7f; d.minGroup = 1; d.maxGroup = 1;
                d.abilities = new[] { "antler_charge", "spore_puff" }; d.rareAbilities = new[] { "thunder_chirp" };
                d.work = Work((WorkType.Logging, 3), (WorkType.Cultivating, 1));
                d.perk = CompanionPerk.LumberBoost; d.overworldRole = "Companion: +50% wood. MOUNT: fastest ground ride, high jumps. Workers: master logger.";
                d.mountable = true; d.mountSpeed = 13.5f; d.mountJump = 10.5f; d.mountSeat = V(0, 1.36f, -0.05f);
                d.drops = Drops(("wood", 3, 6, 1f), ("pelt", 1, 2, 0.6f));
                d.palette = Pal(Hex("#8a5a34"), Hex("#c9a472"), Hex("#6b4424"), Hex("#5cc24a"));
                d.shimmerPalette = Pal(Hex("#e8e4dc"), Hex("#ffffff"), Hex("#b8b0a0"), Hex("#ff7a2a"));
                var p = new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.52f, 1.25f, 0.56f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.92f, 0.05f), V(0.42f, 0.34f, 0.9f))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.White, V(0, 1.12f, -0.64f), V(0.14f, 0.18f, 0.12f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Primary, V(0, 1.36f, 0.6f), V(0.22f, 0.52f, 0.22f), V(32, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.63f, 0.79f), V(0.3f, 0.3f, 0.42f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 1.57f, 0.96f), V(0.18f, 0.16f, 0.2f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 1.59f, 1.06f), V(0.07f, 0.06f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.19f, 1.72f, 0.72f), V(0.17f, 0.06f, 0.1f), V(0, 0, -20))
                    .Mirror(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0.12f, 1.95f, 0.72f), V(0.06f, 0.52f, 0.06f), V(-10, 0, -25))
                    .Mirror(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0.3f, 2.02f, 0.72f), V(0.05f, 0.3f, 0.05f), V(0, 0, -62))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.24f, 2.24f, 0.7f), V(0.3f, 0.22f, 0.3f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.44f, 2.12f, 0.72f), V(0.2f, 0.16f, 0.2f))
                    .Eyes(BodyGroup.Head, V(0.11f, 1.69f, 0.88f), 0.06f)
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.18f, 0.4f, 0.42f), V(0.12f, 0.82f, 0.12f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Secondary, V(0.18f, 0.4f, -0.42f), V(0.12f, 0.82f, 0.12f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Dark, V(0.18f, 0.03f, 0.42f), V(0.14f, 0.06f, 0.14f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Dark, V(0.18f, 0.03f, -0.42f), V(0.14f, 0.06f, 0.14f))
                    .Pivot(BodyGroup.Head, V(0, 1.15f, 0.45f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.18f, 0.82f, 0.42f))
                    .PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.18f, 0.82f, -0.42f))
                    .Pivot(BodyGroup.Tail, V(0, 1.12f, -0.58f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 008 CINDERMAW
            {
                var d = Sp(b, 8, "cindermaw", "Cindermaw", Element.Ember, Element.Stone, Rarity.Rare, BiomeMask.Badlands,
                    Temperament.Aggressive, Locomotion.Quadruped, 120, 19, 17, 2f, 6f, 1.3f);
                d.lore = "A basalt-plated lizard that swallows hot stones to keep its magma belly glowing. Charges anything that stands on its vent.";
                d.personality = "Hot-headed, territorial, secretly loves belly rubs.";
                d.spawnWeight = 0.8f; d.minGroup = 1; d.maxGroup = 1;
                d.abilities = new[] { "magma_maw", "ember_spit" }; d.rareAbilities = new[] { "pebble_toss" };
                d.work = Work((WorkType.Kindling, 3), (WorkType.Quarrying, 2));
                d.perk = CompanionPerk.OreSense; d.overworldRole = "Companion: +50% ore and cinder when mining.";
                d.drops = Drops(("cinder", 1, 3, 1f), ("ore", 1, 2, 0.5f));
                d.palette = Pal(Hex("#3a3032"), Hex("#8a2e1a"), Hex("#5a4a48"), Hex("#ff7a1a"));
                d.shimmerPalette = Pal(Hex("#1a2240"), Hex("#2a4a8a"), Hex("#3a4a6a"), Hex("#3af0ff"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.46f, 0), V(0.72f, 0.46f, 1.3f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.38f, 0.05f), V(0.6f, 0.3f, 1.1f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Glow, V(0, 0.74f, 0.3f), V(0.14f, 0.3f, 0.14f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Glow, V(0, 0.78f, 0), V(0.17f, 0.34f, 0.17f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Glow, V(0, 0.74f, -0.3f), V(0.14f, 0.3f, 0.14f))
                    .Mirror(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, V(0.2f, 0.66f, 0.05f), V(0.26f, 0.2f, 0.4f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.54f, 0.82f), V(0.46f, 0.3f, 0.52f))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.42f, 0.88f), V(0.38f, 0.1f, 0.44f))
                    .Mirror(PartShape.Rock, BodyGroup.Head, ColorSlot.Accent, V(0.14f, 0.67f, 0.9f), V(0.13f, 0.1f, 0.14f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.15f, 0.61f, 0.97f), V(0.07f, 0.06f, 0.05f))
                    .Mirror(PartShape.Capsule, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Primary, V(0.42f, 0.22f, 0.4f), V(0.16f, 0.42f, 0.16f), V(0, 0, 38))
                    .Mirror(PartShape.Capsule, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Primary, V(0.42f, 0.22f, -0.4f), V(0.16f, 0.42f, 0.16f), V(0, 0, 38))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.4f, -1.05f), V(0.32f, 0.9f, 0.3f), V(-95, 0, 0))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.37f, -1.42f), V(0.13f, 0.26f, 0.13f), V(-95, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.5f, 0.6f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.32f, 0.36f, 0.4f))
                    .PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.32f, 0.36f, -0.4f))
                    .Pivot(BodyGroup.Tail, V(0, 0.45f, -0.6f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 009 GLIMMOTH
            {
                var d = Sp(b, 9, "glimmoth", "Glimmoth", Element.Frost, Element.Lumen, Rarity.Rare, BiomeMask.Peaks,
                    Temperament.Timid, Locomotion.Flyer, 62, 15, 10, 2.8f, 6.5f, 1f);
                d.lore = "A moth with wings of living crystal. On clear nights, swarms refract moonlight into ribbons over the peaks.";
                d.personality = "Dreamy, drawn to light, startles at loud noises.";
                d.spawnTime = SpawnTime.Any; d.spawnWeight = 0.9f; d.minGroup = 1; d.maxGroup = 2;
                d.abilities = new[] { "crystal_shard", "prism_dust" }; d.rareAbilities = new[] { "hex_flicker" };
                d.work = Work((WorkType.Chilling, 2), (WorkType.Crafting, 2), (WorkType.Charging, 1));
                d.perk = CompanionPerk.Glow; d.overworldRole = "Companion: a soft light that follows you at night.";
                d.drops = Drops(("crystal", 1, 1, 0.5f), ("gel", 1, 1, 0.3f));
                d.palette = Pal(Hex("#d8d4ff"), Hex("#8ff0ff"), Hex("#d0b0ff"), Hex("#fffbd0"));
                d.shimmerPalette = Pal(Hex("#fff0c0"), Hex("#ffc84a"), Hex("#ffe08a"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.2f, 0.5f, 0.2f), V(80, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.White, V(0, 1.05f, 0.14f), V(0.28f, 0.24f, 0.24f))
                    .Add(PartShape.Capsule, BodyGroup.Tail, ColorSlot.Accent, V(0, 0.95f, -0.25f), V(0.18f, 0.36f, 0.18f), V(100, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.1f, 0.3f), V(0.23f, 0.21f, 0.22f))
                    .Eyes(BodyGroup.Head, V(0.08f, 1.12f, 0.38f), 0.09f)
                    .Mirror(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0.05f, 1.28f, 0.36f), V(0.02f, 0.22f, 0.02f), V(30, 0, -20))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.09f, 1.38f, 0.42f), V(0.06f, 0.06f, 0.06f))
                    .Mirror(PartShape.Crystal, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(0.08f, 1.06f, 0.05f), V(0.9f, 0.75f, 0.22f), V(0, 0, -70))
                    .Mirror(PartShape.Crystal, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Accent, V(0.08f, 1.0f, -0.06f), V(0.6f, 0.5f, 0.18f), V(0, 20, -115))
                    .Pivot(BodyGroup.Head, V(0, 1.05f, 0.2f))
                    .PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.08f, 1.05f, 0))
                    .Pivot(BodyGroup.Tail, V(0, 0.98f, -0.1f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ 010 DUSKIT
            {
                var d = Sp(b, 10, "duskit", "Duskit", Element.Shade, Element.Normal, Rarity.Uncommon, BiomeMask.All,
                    Temperament.Curious, Locomotion.Quadruped, 52, 14, 9, 2.8f, 8.5f, 0.85f);
                d.lore = "An imp-cat that only exists where shadows are long. Follows travellers at night, stealing shiny things.";
                d.personality = "Sneaky, playful, collects anything that glints.";
                d.spawnTime = SpawnTime.Night; d.spawnWeight = 2.2f; d.minGroup = 1; d.maxGroup = 2;
                d.abilities = new[] { "shadow_pounce", "hex_flicker" }; d.rareAbilities = new[] { "prism_dust" };
                d.work = Work((WorkType.Crafting, 2), (WorkType.Foraging, 2));
                d.perk = CompanionPerk.ForageBoost; d.overworldRole = "Companion: +50% berries and fiber. Night-worker.";
                d.drops = Drops(("pelt", 1, 1, 0.6f), ("crystal", 1, 1, 0.1f));
                d.palette = Pal(Hex("#2e2548"), Hex("#4a3d6e"), Hex("#9a5ad0"), Hex("#c8ff6a"));
                d.shimmerPalette = Pal(Hex("#f0ecf8"), Hex("#d8d0ea"), Hex("#ffc84a"), Hex("#ffb0f0"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.3f, 0), V(0.34f, 0.32f, 0.5f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.5f, 0.28f), V(0.42f, 0.36f, 0.36f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.16f, 0.78f, 0.24f), V(0.17f, 0.4f, 0.08f), V(0, 0, -25))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0.155f, 0.76f, 0.27f), V(0.09f, 0.27f, 0.03f), V(0, 0, -25))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.1f, 0.53f, 0.43f), V(0.12f, 0.11f, 0.06f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0.1f, 0.53f, 0.46f), V(0.035f, 0.08f, 0.02f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.1f, 0.08f, 0.14f), V(0.07f, 0.18f, 0.07f))
                    .Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, ColorSlot.Secondary, V(0.1f, 0.08f, -0.14f), V(0.07f, 0.18f, 0.07f))
                    .Add(PartShape.Cylinder, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.55f, -0.36f), V(0.05f, 0.5f, 0.05f), V(-25, 0, 0))
                    .Add(PartShape.Crystal, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.76f, -0.46f), V(0.13f, 0.2f, 0.13f))
                    .Pivot(BodyGroup.Head, V(0, 0.4f, 0.18f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.1f, 0.17f, 0.14f))
                    .PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(0.1f, 0.17f, -0.14f))
                    .Pivot(BodyGroup.Tail, V(0, 0.34f, -0.24f));
                p.ApplyTo(d);
            }
        }
    }
}
