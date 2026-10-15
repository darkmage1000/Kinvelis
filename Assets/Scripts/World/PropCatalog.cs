using UnityEngine;

namespace Wildbond
{
    public enum PropType
    {
        // gatherable nodes
        OakTree, PineTree, DeadTree, Spinepalm, BerryBush, FiberPlant, Rock, OreRock, CrystalCluster, CinderRock, PrismVein,
        GrainStalks, MelonVine, PepperBush, MintPatch,   // wild crops (keep these four together)
        MangroveTree, BlightTree, GlowTree, Cactus, PalmTree,   // outer-land nodes
        SaltglassVein, BogIronVein, SunsteelVein, BlightVein, StarcrystalVein,   // outer-land ores (keep these five together)
        // one per land, each found only in one named sub-region (see SubRegions) - keep these eight together
        Honeybloom, ObsidianSpire, Frostcap, PearlOyster, Lanternbud, Sunfig, Sporecap, Starmoss,
        // caves only (Caves places these itself; no chunk ever rolls them)
        GeodeVein,
        // ground pickups (no tool, one tap): early-game wood & stone before you have tools
        LooseStones, FallenSticks, FiberBundle,
        // decor only
        GrassTuft, Flower, Pebble, DryGrass, SnowTuft,
        Reeds, ToxicShroom, GlowShroom, GlowTuft,        // outer-land decor
        Count
    }

    public struct PropInfo
    {
        public bool isNode;
        public string item;
        public string bonusItem;
        public float bonusChance;
        public int hp;
        public int hardness;
        public ToolKind tool;
        public float radius;       // collision radius at scale 1 (0 = walk through)
        public float respawn;      // seconds
        public string label;
        public int finalFiberMin, finalFiberMax;   // guaranteed fiber when the node is depleted (tree bark strips)
        public bool Plant => tool == ToolKind.Sickle;
        /// <summary>Loose item on the ground: one tap picks it up.</summary>
        public bool pickup;
        public int pickupMin, pickupMax;
        /// <summary>Scales how much the node gives (0 = normal). Rare nodes use a fraction.</summary>
        public float yieldMul;
    }

    /// <summary>Static data + procedural meshes for every world prop type (2 LODs each).</summary>
    public static class PropCatalog
    {
        public static readonly PropInfo[] Info = new PropInfo[(int)PropType.Count];
        static Mesh[] hi, lo;

        static PropCatalog()
        {
            Set(PropType.OakTree, "Sunoak Tree", "wood", "fiber", 0.2f, 6, 1, ToolKind.Axe, 0.55f, 420f);
            Set(PropType.PineTree, "Rimepine", "wood", "pelt", 0.0f, 6, 1, ToolKind.Axe, 0.5f, 420f);
            Set(PropType.DeadTree, "Ashwood Snag", "wood", "cinder", 0.1f, 4, 1, ToolKind.Axe, 0.4f, 420f);
            Set(PropType.Spinepalm, "Spinepalm", "wood", "fiber", 0.5f, 3, 1, ToolKind.Axe, 0.35f, 360f);
            Set(PropType.BerryBush, "Sunberry Bush", "berry", "fiber", 0.35f, 3, 0, ToolKind.Sickle, 0.45f, 300f);
            Set(PropType.FiberPlant, "Fiberleaf", "fiber", "berry", 0.08f, 2, 0, ToolKind.Sickle, 0f, 240f);
            Fiber(PropType.OakTree, 1, 2);
            Fiber(PropType.PineTree, 1, 2);
            Fiber(PropType.DeadTree, 0, 1);
            Fiber(PropType.Spinepalm, 2, 3);
            Set(PropType.Rock, "Boulder", "stone", "ore", 0.08f, 6, 1, ToolKind.Pick, 0.8f, 480f);
            Set(PropType.OreRock, "Rustore Vein", "ore", "stone", 0.6f, 8, 2, ToolKind.Pick, 0.8f, 600f);
            Set(PropType.CrystalCluster, "Rime Crystal", "crystal", "stone", 0.3f, 8, 3, ToolKind.Pick, 0.6f, 900f);
            Set(PropType.CinderRock, "Cinder Vent", "cinder", "stone", 0.5f, 6, 2, ToolKind.Pick, 0.7f, 600f);
            // rare: only on the far, high slopes of the Rimefang Peaks; ~2 shards per vein, 20 min regrowth
            Set(PropType.PrismVein, "Prism Vein", Shimmer.ShardItem, "crystal", 0.3f, 10, 3, ToolKind.Pick, 0.6f, 1200f);
            Info[(int)PropType.PrismVein].yieldMul = 0.32f;
            Set(PropType.GeodeVein, "Geode Vein", Caves.CoreItem, "crystal", 0.5f, 12, 3, ToolKind.Pick, 0.8f, 1e9f);
            Info[(int)PropType.GeodeVein].yieldMul = 0.3f;
            // wild crops: a few of each plant a Crop Plot
            Set(PropType.GrainStalks, "Goldgrain", "goldgrain", "fiber", 0.3f, 2, 0, ToolKind.Sickle, 0f, 360f);
            Set(PropType.MelonVine, "Dewmelon Vine", "dewmelon", "fiber", 0.3f, 2, 0, ToolKind.Sickle, 0.4f, 420f);
            Set(PropType.PepperBush, "Emberpepper Shrub", "emberpepper", "fiber", 0.2f, 3, 0, ToolKind.Sickle, 0.4f, 360f);
            Set(PropType.MintPatch, "Frostmint", "frostmint", "fiber", 0.2f, 2, 0, ToolKind.Sickle, 0f, 360f);
            // outer lands
            Set(PropType.MangroveTree, "Mangrove", "wood", "fiber", 0.4f, 6, 1, ToolKind.Axe, 0.5f, 420f);
            Set(PropType.BlightTree, "Blightwood Snag", "wood", "gel", 0.25f, 5, 1, ToolKind.Axe, 0.4f, 420f);
            Set(PropType.GlowTree, "Glowcap Tree", "wood", "crystal", 0.08f, 7, 2, ToolKind.Axe, 0.55f, 480f);
            Set(PropType.Cactus, "Barrel Cactus", "fiber", "gel", 0.4f, 3, 1, ToolKind.Axe, 0.45f, 360f);
            Set(PropType.PalmTree, "Driftpalm", "wood", "fiber", 0.5f, 5, 1, ToolKind.Axe, 0.4f, 420f);
            Fiber(PropType.PalmTree, 2, 3);
            Fiber(PropType.MangroveTree, 1, 2);
            Fiber(PropType.GlowTree, 0, 1);
            Set(PropType.SaltglassVein, "Saltglass Vein", "saltglass", "stone", 0.3f, 8, 3, ToolKind.Pick, 0.7f, 720f);
            Set(PropType.BogIronVein, "Bog Iron Vein", "bog_iron", "stone", 0.3f, 9, 3, ToolKind.Pick, 0.8f, 720f);
            Set(PropType.SunsteelVein, "Sunsteel Vein", "sunsteel_ore", "stone", 0.3f, 10, 4, ToolKind.Pick, 0.8f, 780f);
            Set(PropType.BlightVein, "Blight Vein", "blight_ore", "gel", 0.3f, 10, 4, ToolKind.Pick, 0.8f, 780f);
            Set(PropType.StarcrystalVein, "Starcrystal Vein", "starcrystal", "crystal", 0.3f, 12, 5, ToolKind.Pick, 0.7f, 900f);
            // region-only gatherables
            Set(PropType.Honeybloom, "Honeybloom", "honeybloom", "fiber", 0.2f, 2, 0, ToolKind.Sickle, 0f, 480f);
            Set(PropType.ObsidianSpire, "Obsidian Spire", "obsidian", "stone", 0.3f, 8, 2, ToolKind.Pick, 0.6f, 720f);
            Set(PropType.Frostcap, "Frostcap", "frostcap", null, 0f, 2, 0, ToolKind.Sickle, 0f, 480f);
            Set(PropType.PearlOyster, "Pearl Oyster", "sea_pearl", null, 0f, 1, 0, ToolKind.None, 0f, 600f);
            Pickup(PropType.PearlOyster, 1, 1);
            Set(PropType.Lanternbud, "Lanternbud", "lanternbud", "fiber", 0.2f, 2, 0, ToolKind.Sickle, 0f, 480f);
            Set(PropType.Sunfig, "Sunfig Bush", "sunfig", "fiber", 0.3f, 3, 0, ToolKind.Sickle, 0.4f, 480f);
            Set(PropType.Sporecap, "Sporecap", "sporecap", "gel", 0.15f, 2, 0, ToolKind.Sickle, 0f, 480f);
            Set(PropType.Starmoss, "Starmoss Clump", "starmoss", "fiber", 0.2f, 2, 0, ToolKind.Sickle, 0f, 480f);
            Set(PropType.LooseStones, "Loose Stones", "stone", null, 0f, 1, 0, ToolKind.None, 0f, 200f);
            Set(PropType.FallenSticks, "Fallen Sticks", "wood", "fiber", 0.25f, 1, 0, ToolKind.None, 0f, 200f);
            Set(PropType.FiberBundle, "Fiber Bundle", "fiber", "berry", 0.06f, 1, 0, ToolKind.None, 0f, 200f);
            Pickup(PropType.LooseStones, 1, 2);
            Pickup(PropType.FallenSticks, 1, 2);
            Pickup(PropType.FiberBundle, 1, 2);
        }

        static void Pickup(PropType t, int min, int max)
        {
            Info[(int)t].pickup = true;
            Info[(int)t].pickupMin = min;
            Info[(int)t].pickupMax = max;
        }

        static void Fiber(PropType t, int min, int max)
        {
            Info[(int)t].finalFiberMin = min;
            Info[(int)t].finalFiberMax = max;
        }

        static void Set(PropType t, string label, string item, string bonus, float bonusChance, int hp, int hardness, ToolKind tool, float radius, float respawn)
        {
            Info[(int)t] = new PropInfo
            {
                isNode = true, label = label, item = item, bonusItem = bonus, bonusChance = bonusChance,
                hp = hp, hardness = hardness, tool = tool, radius = radius, respawn = respawn
            };
        }

        public static bool IsNode(PropType t) => t < PropType.GrassTuft;

        /// <summary>The eight plants / ores that grow in only one named sub-region each.</summary>
        public static bool IsRegional(PropType t) => t >= PropType.Honeybloom && t <= PropType.Starmoss;

        /// <summary>The five outer-land ore veins.</summary>
        public static bool IsOuterOre(PropType t) => t >= PropType.SaltglassVein && t <= PropType.StarcrystalVein;

        /// <summary>The tool that breaks a node of this hardness ("an Ingot Pick").</summary>
        public static string ToolTierName(int hardness, ToolKind kind)
        {
            string tool = kind == ToolKind.Axe ? "Axe" : "Pick";
            return hardness >= 5 ? "a Blightsteel " + tool : hardness == 4 ? "a Bogsteel " + tool : hardness == 3 ? "an Ingot " + tool : "a Stone " + tool;
        }

        static void Vein(MeshBuilder mb, bool low, string rockA, string rockB, string oreA, string oreB, bool shards, uint seed)
        {
            mb.Rock(M(0, 0.5f, 0, 1.7f, 1.25f, 1.6f), C(rockA), C(rockB), seed, low ? 5 : 7, low ? 4 : 5);
            if (shards)
            {
                mb.Crystal(M(0.1f, 0.7f, 0.1f, 0.55f, 1.6f, 0.55f, 6, 0, -8), C(oreA), C(oreB));
                mb.Crystal(M(0.55f, 0.5f, 0.2f, 0.4f, 1.1f, 0.4f, 0, 0, -34), C(oreA), C(oreB));
                if (!low)
                {
                    mb.Crystal(M(-0.5f, 0.5f, -0.1f, 0.38f, 1.0f, 0.38f, 10, 0, 32), C(oreA), C(oreB));
                    mb.Crystal(M(-0.1f, 0.6f, 0.6f, 0.3f, 0.8f, 0.3f, -38, 0, 4), C(oreA), C(oreB));
                }
            }
            else
            {
                mb.Rock(M(0.45f, 0.9f, 0.3f, 0.5f, 0.4f, 0.5f), C(oreA), C(oreB), seed + 2, 5, 4);
                mb.Rock(M(-0.2f, 1.1f, -0.1f, 0.55f, 0.4f, 0.5f), C(oreA), C(oreB), seed + 4, 5, 4);
                if (!low)
                {
                    mb.Rock(M(-0.45f, 0.75f, 0.45f, 0.4f, 0.32f, 0.4f), C(oreA), C(oreB), seed + 6, 5, 4);
                    mb.Rock(M(0.2f, 0.6f, -0.6f, 0.42f, 0.34f, 0.4f), C(oreA), C(oreB), seed + 8, 5, 4);
                }
            }
        }

        /// <summary>Tall things you chop for wood (taller hit effects, bigger size spread, Logging perks).</summary>
        public static bool IsTree(PropType t) =>
            t <= PropType.Spinepalm || t == PropType.MangroveTree || t == PropType.BlightTree || t == PropType.GlowTree || t == PropType.PalmTree;

        public static Mesh Mesh(PropType t, bool lowDetail)
        {
            if (hi == null) BuildMeshes();
            return lowDetail ? lo[(int)t] : hi[(int)t];
        }

        static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Matrix4x4 M(float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(px, py, pz), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        static void BuildMeshes()
        {
            int n = (int)PropType.Count;
            hi = new Mesh[n];
            lo = new Mesh[n];
            var mb = new MeshBuilder();
            for (int i = 0; i < n; i++)
            {
                for (int lod = 0; lod < 2; lod++)
                {
                    mb.Clear();
                    BuildProp(mb, (PropType)i, lod == 1);
                    var m = mb.ToMesh("prop_" + (PropType)i + "_" + lod);
                    if (lod == 0) hi[i] = m; else lo[i] = m;
                }
            }
        }

        static void BuildProp(MeshBuilder mb, PropType t, bool low)
        {
            int seg = low ? 5 : 8;
            int lon = low ? 6 : 9, lat = low ? 4 : 6;
            switch (t)
            {
                case PropType.OakTree:
                    mb.Cylinder(M(0, 1.3f, 0, 0.45f, 2.6f, 0.45f), C("#7a5232"), C("#8f6440"), seg, 0.7f, false);
                    mb.Sphere(M(0, 3.4f, 0, 3.2f, 2.6f, 3.2f), C("#5fbf4a"), C("#3d8f3a"), lon, lat);
                    if (!low)
                    {
                        mb.Sphere(M(1.1f, 3.0f, 0.4f, 1.9f, 1.6f, 1.9f), C("#6fcf52"), C("#3d8f3a"), lon, lat);
                        mb.Sphere(M(-0.9f, 3.1f, -0.6f, 2.0f, 1.7f, 2.0f), C("#58b546"), C("#3a8a38"), lon, lat);
                        mb.Sphere(M(0.2f, 4.4f, -0.2f, 1.6f, 1.3f, 1.6f), C("#7ad85c"), C("#4a9a3c"), lon, lat);
                    }
                    break;
                case PropType.PineTree:
                    mb.Cylinder(M(0, 1.0f, 0, 0.35f, 2.0f, 0.35f), C("#5a4030"), C("#6a4a36"), seg, 0.8f, false);
                    mb.Cylinder(M(0, 2.4f, 0, 2.6f, 2.0f, 2.6f), C("#2f6a5a"), C("#3f7f6a"), seg, 0f, true);
                    mb.Cylinder(M(0, 3.6f, 0, 2.0f, 1.8f, 2.0f), C("#36786a"), C("#dfeef5"), seg, 0f, true);
                    if (!low) mb.Cylinder(M(0, 4.7f, 0, 1.3f, 1.5f, 1.3f), C("#3f8272"), C("#f2f8ff"), seg, 0f, true);
                    break;
                case PropType.DeadTree:
                    mb.Cylinder(M(0, 1.4f, 0, 0.4f, 2.8f, 0.4f), C("#4a3a34"), C("#6a5850"), seg, 0.45f, true);
                    mb.Cylinder(M(0.4f, 2.2f, 0, 0.18f, 1.2f, 0.18f, 0, 0, -45), C("#4a3a34"), C("#6a5850"), 5, 0.3f, true);
                    if (!low) mb.Cylinder(M(-0.35f, 2.6f, 0.1f, 0.14f, 1.0f, 0.14f, 10, 0, 50), C("#4a3a34"), C("#6a5850"), 5, 0.3f, true);
                    break;
                case PropType.Spinepalm:
                    mb.Capsule(M(0, 0.9f, 0, 0.55f, 1.8f, 0.55f), C("#4f9a5a"), seg);
                    mb.Capsule(M(0.45f, 1.1f, 0, 0.3f, 0.8f, 0.3f, 0, 0, -50), C("#5aa864"), seg);
                    if (!low)
                    {
                        mb.Capsule(M(-0.4f, 1.3f, 0.1f, 0.26f, 0.7f, 0.26f, 0, 0, 55), C("#5aa864"), seg);
                        mb.Sphere(M(0, 1.85f, 0, 0.25f, 0.2f, 0.25f), C("#ff6ab0"), 6, 4);
                    }
                    break;
                case PropType.BerryBush:
                    mb.Sphere(M(0, 0.55f, 0, 1.4f, 1.1f, 1.4f), C("#4aa84a"), C("#2f7a38"), lon, lat);
                    if (!low)
                    {
                        mb.Sphere(M(0.5f, 0.75f, 0.4f, 0.2f, 0.2f, 0.2f), C("#ff4a6a"), 6, 4);
                        mb.Sphere(M(-0.4f, 0.8f, 0.45f, 0.2f, 0.2f, 0.2f), C("#ff4a6a"), 6, 4);
                        mb.Sphere(M(0.1f, 1.0f, -0.5f, 0.2f, 0.2f, 0.2f), C("#ff6a8a"), 6, 4);
                        mb.Sphere(M(-0.55f, 0.6f, -0.2f, 0.2f, 0.2f, 0.2f), C("#ff4a6a"), 6, 4);
                        mb.Sphere(M(0.2f, 1.05f, 0.3f, 0.2f, 0.2f, 0.2f), C("#ff6a8a"), 6, 4);
                    }
                    break;
                case PropType.MangroveTree:
                    // stilt roots, a leaning trunk and a low dark canopy
                    for (int i = 0; i < (low ? 3 : 5); i++)
                    {
                        float a = i * 72f * Mathf.Deg2Rad;
                        mb.Cylinder(M(Mathf.Cos(a) * 0.45f, 0.6f, Mathf.Sin(a) * 0.45f, 0.12f, 1.4f, 0.12f, Mathf.Sin(a) * -28f, 0, Mathf.Cos(a) * 28f), C("#5a4a36"), C("#6e5a40"), 5, 0.8f, false);
                    }
                    mb.Cylinder(M(0.05f, 1.9f, 0, 0.3f, 1.8f, 0.3f, 0, 0, -5), C("#5a4a36"), C("#6e5a40"), seg, 0.7f, true);
                    mb.Sphere(M(0, 3.1f, 0, 2.6f, 1.2f, 2.6f), C("#3c6a38"), C("#2a4e2c"), lon, lat);
                    mb.Sphere(M(0.9f, 2.7f, 0.5f, 1.5f, 0.9f, 1.5f), C("#46783e"), C("#2a4e2c"), lon, lat);
                    if (!low) mb.Sphere(M(-0.9f, 2.8f, -0.4f, 1.4f, 0.8f, 1.4f), C("#3c6a38"), C("#2a4e2c"), lon, lat);
                    break;
                case PropType.BlightTree:
                    mb.Cylinder(M(0, 1.2f, 0, 0.34f, 2.4f, 0.34f, 0, 0, 7), C("#4a3e52"), C("#5e5068"), seg, 0.6f, true);
                    mb.Cylinder(M(0.42f, 2.4f, 0, 0.16f, 1.3f, 0.16f, 0, 0, -42), C("#4a3e52"), C("#5e5068"), 5, 0.4f, true);
                    mb.Cylinder(M(-0.4f, 2.1f, 0.1f, 0.14f, 1.1f, 0.14f, 10, 0, 48), C("#4a3e52"), C("#5e5068"), 5, 0.4f, true);
                    mb.Sphere(M(0.82f, 2.86f, 0, 0.4f, 0.46f, 0.4f), C("#9ad83a"), C("#5a8a1e"), 7, 5);
                    mb.Sphere(M(-0.78f, 2.5f, 0.12f, 0.34f, 0.4f, 0.34f), C("#9ad83a"), C("#5a8a1e"), 7, 5);
                    if (!low) mb.Sphere(M(0.14f, 2.5f, 0, 0.3f, 0.34f, 0.3f), C("#b4f04a"), C("#5a8a1e"), 7, 5);
                    break;
                case PropType.GlowTree:
                    // a giant glowing mushroom
                    mb.Cylinder(M(0, 1.8f, 0, 0.5f, 3.6f, 0.5f), C("#c8e8e0"), C("#e8fff8"), seg, 0.65f, true);
                    mb.Dome(M(0, 3.4f, 0, 3.4f, 1.9f, 3.4f), C("#2ac8c0"), 12, 5);
                    mb.Cylinder(M(0, 3.4f, 0, 3.2f, 0.08f, 3.2f), C("#7af4e8"), C("#b8fff4"), 10, 1f, true);
                    for (int i = 0; i < (low ? 3 : 6); i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        mb.Sphere(M(Mathf.Cos(a) * 1.0f, 3.95f + (i % 2) * 0.16f, Mathf.Sin(a) * 1.0f, 0.42f, 0.14f, 0.42f), C("#d8fffa"), 6, 3);
                    }
                    break;
                case PropType.PalmTree:
                    // leaning trunk in three sections, a crown of drooping fronds and a couple of nuts
                    mb.Cylinder(M(0.06f, 0.8f, 0, 0.3f, 1.6f, 0.3f, 0, 0, -5), C("#8a6a48"), C("#a07c54"), seg, 0.85f, false);
                    mb.Cylinder(M(0.26f, 2.3f, 0, 0.25f, 1.6f, 0.25f, 0, 0, -11), C("#8a6a48"), C("#a07c54"), seg, 0.85f, false);
                    mb.Cylinder(M(0.62f, 3.7f, 0, 0.21f, 1.5f, 0.21f, 0, 0, -17), C("#8a6a48"), C("#a07c54"), seg, 0.8f, true);
                    for (int i = 0; i < (low ? 5 : 8); i++)
                    {
                        float a = i * (low ? 72f : 45f);
                        mb.Sphere(M(0.84f + Mathf.Sin(a * Mathf.Deg2Rad) * 0.8f, 4.3f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.8f, 0.42f, 0.1f, 1.9f, 22, a, 0), C("#4aa850"), C("#2f7a3c"), 6, 3);
                    }
                    if (!low)
                    {
                        mb.Sphere(M(0.74f, 4.2f, 0.14f, 0.26f, 0.26f, 0.26f), C("#6a4a2e"), 6, 4);
                        mb.Sphere(M(0.96f, 4.16f, -0.1f, 0.24f, 0.24f, 0.24f), C("#6a4a2e"), 6, 4);
                    }
                    break;
                case PropType.Cactus:
                    mb.Cylinder(M(0, 0.7f, 0, 0.6f, 1.4f, 0.6f), C("#4a8a4a"), C("#62a858"), seg, 0.85f, true);
                    mb.Cylinder(M(0.42f, 0.9f, 0, 0.26f, 0.7f, 0.26f, 0, 0, -30), C("#4a8a4a"), C("#62a858"), 6, 0.8f, true);
                    mb.Cylinder(M(-0.4f, 0.7f, 0.1f, 0.24f, 0.6f, 0.24f, 0, 0, 34), C("#4a8a4a"), C("#62a858"), 6, 0.8f, true);
                    mb.Sphere(M(0, 1.46f, 0, 0.26f, 0.16f, 0.26f), C("#ff6aa0"), 6, 4);
                    break;
                case PropType.Reeds:
                    for (int i = 0; i < (low ? 3 : 6); i++)
                    {
                        float a = i * 61f * Mathf.Deg2Rad, r = 0.1f + (i % 3) * 0.1f, hh = 1.1f + (i % 3) * 0.25f;
                        mb.Cylinder(M(Mathf.Cos(a) * r, hh * 0.5f, Mathf.Sin(a) * r, 0.05f, hh, 0.05f, (i % 2 == 0 ? 5 : -5), 0, (i % 3 - 1) * 6), C("#6a8a3a"), C("#9ab45a"), 4, 0.6f, false);
                        if (!low && i % 2 == 0) mb.Sphere(M(Mathf.Cos(a) * r, hh + 0.02f, Mathf.Sin(a) * r, 0.1f, 0.3f, 0.1f), C("#6a4a2a"), 5, 4);
                    }
                    break;
                case PropType.ToxicShroom:
                    for (int i = 0; i < (low ? 2 : 4); i++)
                    {
                        float a = i * 97f * Mathf.Deg2Rad, r = i * 0.13f, hh = 0.22f + (i % 2) * 0.16f;
                        mb.Cylinder(M(Mathf.Cos(a) * r, hh * 0.5f, Mathf.Sin(a) * r, 0.07f, hh, 0.07f), C("#c8c8a0"), 4, 1f, false);
                        mb.Dome(M(Mathf.Cos(a) * r, hh, Mathf.Sin(a) * r, 0.3f, 0.22f, 0.3f), C("#b4f04a"), 7, 3);
                    }
                    break;
                case PropType.GlowShroom:
                    for (int i = 0; i < (low ? 2 : 4); i++)
                    {
                        float a = i * 97f * Mathf.Deg2Rad, r = i * 0.14f, hh = 0.26f + (i % 2) * 0.2f;
                        mb.Cylinder(M(Mathf.Cos(a) * r, hh * 0.5f, Mathf.Sin(a) * r, 0.07f, hh, 0.07f), C("#d8f4f0"), 4, 1f, false);
                        mb.Dome(M(Mathf.Cos(a) * r, hh, Mathf.Sin(a) * r, 0.34f, 0.24f, 0.34f), i % 2 == 0 ? C("#5af4e4") : C("#e87af8"), 7, 3);
                    }
                    break;
                case PropType.GlowTuft:
                    for (int i = 0; i < (low ? 3 : 5); i++)
                    {
                        float a = i * 72f;
                        mb.Cylinder(M(Mathf.Cos(a * Mathf.Deg2Rad) * 0.1f, 0.3f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.1f, 0.1f, 0.6f, 0.04f, 0, a, 16), C("#2a8a8a"), C("#7af4e8"), 4, 0f, false);
                    }
                    break;
                case PropType.GrainStalks:
                    for (int i = 0; i < (low ? 4 : 7); i++)
                    {
                        float a = i * 51f * Mathf.Deg2Rad, r = 0.12f + (i % 3) * 0.1f;
                        float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r, h = 0.9f + (i % 3) * 0.14f;
                        mb.Cylinder(M(x, h * 0.5f, z, 0.05f, h, 0.05f, (i % 2 == 0 ? 6 : -6), 0, (i % 3 - 1) * 7), C("#c8a83a"), C("#e8cc5a"), 4, 0.6f, false);
                        mb.Sphere(M(x, h + 0.1f, z, 0.11f, 0.3f, 0.11f), C("#f0c84a"), C("#d8a030"), 5, 4);
                    }
                    break;
                case PropType.MelonVine:
                    mb.Sphere(M(0, 0.1f, 0, 1.3f, 0.16f, 1.3f), C("#3f8a4a"), C("#2f6a3a"), lon, 3);
                    mb.Sphere(M(0.25f, 0.24f, 0.1f, 0.5f, 0.42f, 0.5f), C("#7ad0a0"), C("#4aa878"), lon, lat);
                    mb.Sphere(M(-0.3f, 0.2f, -0.2f, 0.4f, 0.34f, 0.4f), C("#8ae0b0"), C("#4aa878"), lon, lat);
                    if (!low)
                    {
                        mb.Sphere(M(0.5f, 0.2f, -0.35f, 0.4f, 0.05f, 0.3f), C("#4fa85a"), 6, 3);
                        mb.Sphere(M(-0.45f, 0.2f, 0.4f, 0.4f, 0.05f, 0.3f), C("#4fa85a"), 6, 3);
                    }
                    break;
                case PropType.PepperBush:
                    mb.Sphere(M(0, 0.45f, 0, 1.0f, 0.9f, 1.0f), C("#4a6a3a"), C("#34502c"), lon, lat);
                    for (int i = 0; i < (low ? 3 : 6); i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        mb.Cylinder(M(Mathf.Cos(a) * 0.42f, 0.42f + (i % 2) * 0.22f, Mathf.Sin(a) * 0.46f, 0.17f, 0.42f, 0.17f, 180, 0, 0), C("#e84a2a"), C("#ff7a3a"), 5, 0f, true);
                    }
                    break;
                case PropType.Honeybloom:
                    // tall stem, a big golden bloom, a bee-striped heart
                    mb.Cylinder(M(0, 0.4f, 0, 0.07f, 0.8f, 0.07f), C("#4f9a3a"), 5, 1f, false);
                    mb.Sphere(M(0.18f, 0.3f, 0, 0.36f, 0.06f, 0.2f, 0, 0, 25), C("#5aae42"), 6, 3);
                    for (int i = 0; i < (low ? 5 : 8); i++)
                    {
                        float a = i * (low ? 72f : 45f) * Mathf.Deg2Rad;
                        mb.Sphere(M(Mathf.Cos(a) * 0.26f, 0.84f, Mathf.Sin(a) * 0.26f, 0.3f, 0.07f, 0.18f, 0, -a * Mathf.Rad2Deg, 18), C("#ffd23a"), C("#f0a020"), 6, 3);
                    }
                    mb.Sphere(M(0, 0.9f, 0, 0.26f, 0.2f, 0.26f), C("#b8701a"), C("#8a4a10"), lon, lat);
                    break;
                case PropType.ObsidianSpire:
                    mb.Rock(M(0, 0.2f, 0, 1.3f, 0.5f, 1.2f), C("#2a2630"), C("#3a3644"), 81u, low ? 5 : 7, low ? 3 : 4);
                    mb.Crystal(M(0, 0.2f, 0, 0.55f, 1.9f, 0.55f, 0, 0, 6), C("#17141f"), C("#6a5a9a"));
                    mb.Crystal(M(0.42f, 0.2f, 0.1f, 0.34f, 1.1f, 0.34f, 0, 0, -22), C("#1c1826"), C("#7a6ab0"));
                    if (!low) mb.Crystal(M(-0.38f, 0.2f, -0.16f, 0.3f, 0.9f, 0.3f, 10, 0, 24), C("#17141f"), C("#5a4a8a"));
                    break;
                case PropType.Frostcap:
                    for (int i = 0; i < (low ? 2 : 3); i++)
                    {
                        float ox = (i - 1) * 0.3f, hh = 0.34f + (i % 2) * 0.2f;
                        mb.Cylinder(M(ox, hh * 0.5f, i * 0.12f - 0.1f, 0.12f, hh, 0.12f), C("#dff4ff"), 5, 0.8f, false);
                        mb.Dome(M(ox, hh, i * 0.12f - 0.1f, 0.46f, 0.3f, 0.46f), C("#7ac8ff"), lon, 3);
                        mb.Sphere(M(ox, hh + 0.13f, i * 0.12f - 0.1f, 0.2f, 0.08f, 0.2f), C("#ffffff"), 5, 3);
                    }
                    break;
                case PropType.PearlOyster:
                    mb.Dome(M(0, 0.02f, 0, 0.9f, 0.3f, 0.7f), C("#8a7a8a"), lon, 3);
                    mb.Dome(M(0, 0.2f, -0.2f, 0.9f, 0.26f, 0.7f, -38, 0, 0), C("#b8a8b8"), lon, 3);
                    mb.Sphere(M(0, 0.2f, 0.06f, 0.26f, 0.26f, 0.26f), C("#fffaf0"), C("#f0d8e8"), lon, lat);
                    break;
                case PropType.Lanternbud:
                    mb.Cylinder(M(0, 0.45f, 0, 0.06f, 0.9f, 0.06f, 0, 0, 8), C("#3f7a4a"), 5, 1f, false);
                    mb.Sphere(M(0.14f, 0.82f, 0, 0.34f, 0.44f, 0.34f), C("#fff09a"), C("#ffb84a"), lon, lat);
                    mb.Cone(M(0.14f, 1.06f, 0, 0.2f, 0.2f, 0.2f), C("#3f7a4a"), 5);
                    if (!low)
                    {
                        mb.Cylinder(M(-0.2f, 0.3f, 0.1f, 0.05f, 0.6f, 0.05f, 0, 0, -14), C("#3f7a4a"), 5, 1f, false);
                        mb.Sphere(M(-0.3f, 0.56f, 0.1f, 0.24f, 0.3f, 0.24f), C("#fff09a"), C("#ffb84a"), 6, 4);
                    }
                    break;
                case PropType.Sunfig:
                    mb.Sphere(M(0, 0.5f, 0, 1.1f, 0.95f, 1.1f), C("#5a8a3a"), C("#3f6a2c"), lon, lat);
                    for (int i = 0; i < (low ? 3 : 6); i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        mb.Sphere(M(Mathf.Cos(a) * 0.46f, 0.5f + (i % 2) * 0.24f, Mathf.Sin(a) * 0.46f, 0.24f, 0.28f, 0.24f), C("#ffb03a"), C("#d86a2a"), 6, 4);
                    }
                    break;
                case PropType.Sporecap:
                    mb.Cylinder(M(0, 0.25f, 0, 0.16f, 0.5f, 0.16f), C("#d8c8e0"), 5, 0.8f, false);
                    mb.Dome(M(0, 0.48f, 0, 0.7f, 0.42f, 0.7f), C("#c04ad0"), lon, 3);
                    for (int i = 0; i < (low ? 3 : 5); i++)
                        mb.Sphere(M(Mathf.Cos(i * 1.256f) * 0.2f, 0.62f + (i % 2) * 0.06f, Mathf.Sin(i * 1.256f) * 0.2f, 0.12f, 0.08f, 0.12f), C("#f4e8a0"), 4, 3);
                    if (!low) { mb.Cylinder(M(0.34f, 0.14f, 0.1f, 0.1f, 0.28f, 0.1f), C("#d8c8e0"), 5, 0.8f, false); mb.Dome(M(0.34f, 0.27f, 0.1f, 0.36f, 0.22f, 0.36f), C("#a83ac0"), 6, 3); }
                    break;
                case PropType.Starmoss:
                    mb.Sphere(M(0, 0.12f, 0, 1.0f, 0.3f, 0.9f), C("#2ad0b0"), C("#1a8a80"), lon, lat);
                    mb.Sphere(M(0.3f, 0.2f, 0.2f, 0.5f, 0.26f, 0.5f), C("#4af0d0"), C("#1a8a80"), 6, 4);
                    for (int i = 0; i < (low ? 3 : 6); i++)
                        mb.Sphere(M(Mathf.Cos(i * 1.05f) * 0.3f, 0.3f + (i % 2) * 0.05f, Mathf.Sin(i * 1.05f) * 0.3f, 0.09f, 0.09f, 0.09f), C("#ffffff"), 4, 3);
                    break;
                case PropType.MintPatch:
                    for (int i = 0; i < (low ? 4 : 7); i++)
                    {
                        float a = i * 51f * Mathf.Deg2Rad, r = 0.1f + (i % 3) * 0.14f;
                        mb.Sphere(M(Mathf.Cos(a) * r, 0.2f + (i % 2) * 0.12f, Mathf.Sin(a) * r, 0.34f, 0.1f, 0.24f, 0, i * 51f, 14), C("#9ae0e8"), C("#6ab8c8"), 6, 3);
                    }
                    if (!low) mb.Sphere(M(0, 0.44f, 0, 0.12f, 0.12f, 0.12f), C("#ffffff"), 5, 4);
                    break;
                case PropType.FiberPlant:
                    for (int i = 0; i < (low ? 3 : 6); i++)
                    {
                        float a = i * 60f;
                        mb.Cylinder(M(Mathf.Cos(a * Mathf.Deg2Rad) * 0.15f, 0.45f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.15f, 0.12f, 0.9f, 0.05f, 0, a, 18),
                            C("#6aa83a"), C("#c8ec7a"), 4, 0f, false);
                    }
                    break;
                case PropType.Rock:
                    mb.Rock(M(0, 0.5f, 0, 1.8f, 1.3f, 1.6f), C("#8a8a92"), C("#a8a8b0"), 11u, low ? 5 : 7, low ? 4 : 5);
                    break;
                case PropType.OreRock:
                    mb.Rock(M(0, 0.55f, 0, 1.7f, 1.4f, 1.6f), C("#7a6e6a"), C("#96867e"), 23u, low ? 5 : 7, low ? 4 : 5);
                    mb.Rock(M(0.45f, 0.9f, 0.3f, 0.45f, 0.35f, 0.45f), C("#d0843e"), C("#e89a52"), 5u, 5, 4);
                    if (!low)
                    {
                        mb.Rock(M(-0.4f, 0.8f, 0.45f, 0.35f, 0.3f, 0.35f), C("#d0843e"), C("#e89a52"), 7u, 5, 4);
                        mb.Rock(M(0.1f, 1.2f, -0.35f, 0.4f, 0.3f, 0.4f), C("#d0843e"), C("#e89a52"), 9u, 5, 4);
                    }
                    break;
                case PropType.CrystalCluster:
                    mb.Rock(M(0, 0.2f, 0, 1.4f, 0.5f, 1.4f), C("#6a7280"), C("#8a92a0"), 31u, 6, 4);
                    mb.Crystal(M(0, 0.2f, 0, 0.7f, 2.2f, 0.7f), C("#7fdcff"), C("#e8fbff"));
                    mb.Crystal(M(0.4f, 0.2f, 0.2f, 0.45f, 1.4f, 0.45f, 0, 0, -22), C("#8fe8ff"), C("#ffffff"));
                    if (!low)
                    {
                        mb.Crystal(M(-0.35f, 0.2f, -0.2f, 0.4f, 1.2f, 0.4f, 10, 0, 25), C("#a0c8ff"), C("#ffffff"));
                        mb.Crystal(M(-0.1f, 0.2f, 0.45f, 0.3f, 0.9f, 0.3f, -20, 0, 5), C("#8fe8ff"), C("#ffffff"));
                    }
                    break;
                case PropType.SaltglassVein: Vein(mb, low, "#c8b890", "#e0d4b0", "#7ad8d0", "#e8fffa", true, 61u); break;
                case PropType.BogIronVein: Vein(mb, low, "#4a4a3a", "#5e6048", "#3a5a3a", "#8ab06a", false, 67u); break;
                case PropType.SunsteelVein: Vein(mb, low, "#b08a58", "#c8a470", "#f0b830", "#fff0a0", false, 71u); break;
                case PropType.BlightVein: Vein(mb, low, "#3a3444", "#4a4458", "#7a3aa8", "#b8ff40", false, 73u); break;
                case PropType.StarcrystalVein: Vein(mb, low, "#2e2e52", "#44447a", "#9a8aff", "#f0f4ff", true, 79u); break;
                case PropType.GeodeVein:
                    // a split boulder, violet crystal packed inside
                    mb.Rock(M(0, 0.45f, 0, 1.7f, 1.0f, 1.6f), C("#4a4450"), C("#625a6a"), 83u, low ? 5 : 7, low ? 4 : 5);
                    mb.Sphere(M(0, 0.7f, 0.42f, 1.1f, 0.7f, 0.3f), C("#2a1a3a"), C("#3a2a4a"), 8, 5);
                    mb.Crystal(M(0, 0.4f, 0.5f, 0.3f, 0.9f, 0.3f, 20, 0, 0), C("#b48aff"), C("#f0e0ff"));
                    mb.Crystal(M(0.3f, 0.4f, 0.48f, 0.22f, 0.6f, 0.22f, 20, 0, -24), C("#9a6aff"), C("#e8d8ff"));
                    mb.Crystal(M(-0.28f, 0.4f, 0.46f, 0.2f, 0.55f, 0.2f, 20, 0, 26), C("#9a6aff"), C("#e8d8ff"));
                    break;
                case PropType.PrismVein:
                    mb.Rock(M(0, 0.2f, 0, 1.5f, 0.55f, 1.5f), C("#3a3644"), C("#55506a"), 53u, 6, 4);
                    mb.Crystal(M(0, 0.2f, 0, 0.6f, 2.0f, 0.6f), C("#ff7ad0"), C("#ff4ab8"));
                    mb.Crystal(M(0.4f, 0.2f, 0.15f, 0.42f, 1.3f, 0.42f, 0, 0, -24), C("#7ae8ff"), C("#2ac8ff"));
                    mb.Crystal(M(-0.38f, 0.2f, -0.15f, 0.4f, 1.2f, 0.4f, 8, 0, 26), C("#ffe46a"), C("#ffc81e"));
                    if (!low)
                    {
                        mb.Crystal(M(-0.05f, 0.2f, 0.45f, 0.3f, 0.9f, 0.3f, -22, 0, 4), C("#b48aff"), C("#8a52ff"));
                        mb.Crystal(M(0.1f, 0.2f, -0.45f, 0.28f, 0.8f, 0.28f, 24, 0, -6), C("#7affb0"), C("#2ee88a"));
                    }
                    break;
                case PropType.CinderRock:
                    mb.Rock(M(0, 0.45f, 0, 1.6f, 1.1f, 1.6f), C("#3a3234"), C("#4e4446"), 41u, low ? 5 : 7, low ? 4 : 5);
                    mb.Sphere(M(0, 0.95f, 0, 0.55f, 0.25f, 0.55f), C("#ff7a2a"), C("#ffcc4a"), 6, 3);
                    break;
                case PropType.LooseStones:
                    mb.Rock(M(0, 0.13f, 0, 0.55f, 0.34f, 0.47f), C("#8e8e98"), C("#b0b0b8"), 61u, low ? 4 : 6, low ? 3 : 4);
                    mb.Rock(M(0.4f, 0.09f, 0.14f, 0.34f, 0.23f, 0.3f), C("#84848e"), C("#a6a6ae"), 67u, 5, 3);
                    if (!low) mb.Rock(M(-0.28f, 0.08f, 0.3f, 0.28f, 0.19f, 0.25f), C("#9a9aa2"), C("#bcbcc4"), 71u, 5, 3);
                    break;
                case PropType.FallenSticks:
                    mb.Cylinder(M(0, 0.09f, 0, 0.13f, 1.35f, 0.13f, 90, 0, 0), C("#7a5232"), C("#9a7048"), 5, 0.8f, true);
                    mb.Cylinder(M(0.1f, 0.16f, 0.05f, 0.11f, 1.1f, 0.11f, 90, 55, 0), C("#6e4a2c"), C("#8f6440"), 5, 0.7f, true);
                    if (!low) mb.Cylinder(M(-0.15f, 0.13f, -0.1f, 0.09f, 0.9f, 0.09f, 90, -40, 0), C("#80583a"), C("#a07a50"), 4, 0.7f, true);
                    break;
                case PropType.FiberBundle:
                    // a tied sheaf of dry fiber lying on the ground
                    for (int i = 0; i < (low ? 4 : 7); i++)
                    {
                        float off = (i - 3) * 0.045f;
                        mb.Cylinder(M(off, 0.07f + Mathf.Abs(off) * 0.3f, (i % 2) * 0.04f - 0.02f, 0.05f, 1.0f, 0.05f, 90, (i - 3) * 4f, 0), C("#a9c46a"), C("#e2e89a"), 4, 0.6f, false);
                    }
                    mb.Cylinder(M(0, 0.09f, 0, 0.36f, 0.08f, 0.24f, 0, 0, 90), C("#b88a4a"), 6, 1f, true);
                    break;
                case PropType.GrassTuft:
                    for (int i = 0; i < 4; i++)
                        mb.Cylinder(M((i - 1.5f) * 0.1f, 0.25f, (i % 2) * 0.08f, 0.1f, 0.5f, 0.04f, 0, i * 45f, (i - 1.5f) * 12f), C("#4f9a3a"), C("#9fd86a"), 3, 0f, false);
                    break;
                case PropType.Flower:
                    mb.Cylinder(M(0, 0.2f, 0, 0.04f, 0.4f, 0.04f), C("#4a9a3a"), 3, 1f, false);
                    mb.Sphere(M(0, 0.42f, 0, 0.2f, 0.1f, 0.2f), C("#ff8ac8"), 5, 3);
                    mb.Sphere(M(0, 0.45f, 0, 0.08f, 0.06f, 0.08f), C("#ffe44a"), 4, 3);
                    break;
                case PropType.Pebble:
                    mb.Rock(M(0, 0.1f, 0, 0.35f, 0.22f, 0.3f), C("#b5a08a"), C("#c9b49a"), 51u, 5, 3);
                    break;
                case PropType.DryGrass:
                    for (int i = 0; i < 3; i++)
                        mb.Cylinder(M((i - 1) * 0.1f, 0.22f, 0, 0.08f, 0.45f, 0.04f, 0, i * 60f, (i - 1) * 15f), C("#b58a4a"), C("#e8c87a"), 3, 0f, false);
                    break;
                case PropType.SnowTuft:
                    mb.Sphere(M(0, 0.08f, 0, 0.7f, 0.25f, 0.6f), C("#f2f8ff"), 6, 3);
                    mb.Cylinder(M(0.1f, 0.2f, 0, 0.06f, 0.4f, 0.04f, 0, 0, 10), C("#5a8a7a"), C("#9ac0b0"), 3, 0f, false);
                    break;
            }
        }
    }
}
