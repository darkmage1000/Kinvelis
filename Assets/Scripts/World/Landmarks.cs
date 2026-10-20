using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>Hand-placed signature landmarks per biome + alpha creature sites. Built once, distance-culled.</summary>
    public static class Landmarks
    {
        public struct Landmark
        {
            public string name;
            public Vector3 pos;
            public Color color;
            public BiomeId biome;
            public GameObject go;
            /// <summary>The picture this place gets on the minimap and the map.</summary>
            public MapIcon icon;
        }

        public struct AlphaSite
        {
            public string id;
            public string species;
            public Vector3 pos;
        }

        public static readonly List<Landmark> List = new List<Landmark>();
        public static readonly List<AlphaSite> AlphaSites = new List<AlphaSite>();

        static Transform root;

        public static void Build(Transform parent)
        {
            root = parent;
            List.Clear();
            AlphaSites.Clear();
            var mat = RenderAssets.Building;

            // ---- Sunmeadow Vale ----
            var bloom = FindLand(new Vector3(260f, 0, -930f));
            Add(parent, mat, "Great Bloomtree", bloom, BiomeId.Meadow, new Color(1f, 0.6f, 0.8f), BloomTree, 3.2f, MapIcon.Tree);
            var circle = FindLand(WorldGen.LegacySpawnPoint() + new Vector3(-90f, 0, 110f));
            StartArea.Build(parent);
            Add(parent, mat, "Old Hearth Circle", circle, BiomeId.Meadow, new Color(0.8f, 0.9f, 0.7f), StoneCircle, 0f, MapIcon.Stones);
            AddAlpha("alpha_barkhorn", "barkhorn", bloom + new Vector3(24f, 0, 18f));
            AddAlpha("alpha_voltbeak", "voltbeak", FindLand(new Vector3(-320f, 0, -420f)));

            // ---- Cinderreach Badlands ----
            var arch = FindLand(new Vector3(1230f, 0, 560f));
            Add(parent, mat, "Sunforge Arch", arch, BiomeId.Badlands, new Color(1f, 0.6f, 0.3f), SunforgeArch, 0f, MapIcon.Arch);
            AddAlpha("alpha_cindermaw", "cindermaw", arch + new Vector3(-20f, 0, -26f));
            AddAlpha("alpha_emberkit", "emberkit", FindLand(new Vector3(900f, 0, 150f)));

            // ---- Rimefang Peaks ----
            var spire = FindLand(new Vector3(-1100f, 0, 700f), 0.8f);
            Add(parent, mat, "Shard Spire", spire, BiomeId.Peaks, new Color(0.6f, 0.95f, 1f), ShardSpire, 2.5f, MapIcon.Crystal);
            AddAlpha("alpha_frostuft", "frostuft", spire + new Vector3(28f, 0, -20f));
            AddAlpha("alpha_glimmoth", "glimmoth", spire + new Vector3(-22f, 0, 24f));
        }

        static void AddAlpha(string id, string sp, Vector3 p)
        {
            p = FindLand(p);
            AlphaSites.Add(new AlphaSite { id = id, species = sp, pos = p });
        }

        /// <summary>
        /// Two alpha lairs on each outer land: a ring of great ribs around a trampled nest, on the map once you have been near.
        /// Runs after the settlements, Wardens and hunts have chosen their ground, so a lair never lands on one of them.
        /// </summary>
        public static void BuildOuterLairs()
        {
            Lair(BiomeId.Isles, "Tidewrack Lair", "krakling", 0.5f, 150f);
            Lair(BiomeId.Isles, "Breaker's Rest", "wavecrest", 0.55f, 320f);
            Lair(BiomeId.Swamp, "Snapjaw Wallow", "bogsnap", 0.5f, 10f);
            Lair(BiomeId.Swamp, "Lanternmere", "fenlight", 0.55f, 190f);
            Lair(BiomeId.Desert, "Jackal's Bonefield", "sandjackal", 0.5f, 110f);
            Lair(BiomeId.Desert, "Wyrmsink", "sandwyrm", 0.55f, 290f);
            Lair(BiomeId.Blight, "Fumepit", "fumeskull", 0.5f, 50f);
            Lair(BiomeId.Blight, "Miasma Hollow", "miasmare", 0.55f, 230f);
            Lair(BiomeId.Glow, "Prismfox Glade", "prismfox", 0.5f, 170f);
            Lair(BiomeId.Glow, "Myceliath's Ring", "myceliath", 0.55f, 340f);
        }

        static void Lair(BiomeId biome, string name, string species, float frac, float angle)
        {
            if (!WorldGen.TryLand(biome, out var land) || root == null) return;
            float a = angle * Mathf.Deg2Rad;
            var near = new Vector3(land.center.x + Mathf.Cos(a) * land.radius * frac, 0f, land.center.y + Mathf.Sin(a) * land.radius * frac);
            Vector3 p = FindLand(near, 0.9f);
            bool ok = false;
            for (int ring = 0; ring < 30 && !ok; ring++)
            for (int i = 0; i < 12 && !ok; i++)
            {
                float t = (i + ring * 0.41f) / 12f * Mathf.PI * 2f;
                float x = near.x + Mathf.Cos(t) * ring * 22f, z = near.z + Mathf.Sin(t) * ring * 22f;
                float h = WorldGen.Height(x, z);
                if (h < 2.5f || WorldGen.Normal(x, z, 4f).y < 0.94f || WorldGen.Biome(x, z) != biome) { if (ring == 0) break; continue; }
                var c = new Vector3(x, h, z);
                if (NearLandmark(c, 60f) || !LairClear(c)) { if (ring == 0) break; continue; }
                p = c; ok = true;
            }
            Color col = Color.Lerp(BiomeInfo.Tint(biome), new Color(1f, 0.55f, 0.2f), 0.5f);
            Add(root, RenderAssets.Building, name, p, biome, col, AlphaLair, 0f, MapIcon.Lair);
            AlphaSites.Add(new AlphaSite { id = "alpha_" + species, species = species, pos = p + new Vector3(0f, 0f, 1f) });
        }

        static bool LairClear(Vector3 p)
        {
            foreach (var w in Wardens.All) { var d = w.pos - p; d.y = 0; if (d.sqrMagnitude < 110f * 110f) return false; }
            foreach (var h in LegendHunts.All)
            {
                var d = h.lairPos - p; d.y = 0;
                if (d.sqrMagnitude < 120f * 120f) return false;
                if (h.clues != null) foreach (var c in h.clues) { var e = c.pos - p; e.y = 0; if (e.sqrMagnitude < 50f * 50f) return false; }
            }
            return true;
        }

        static void Add(Transform parent, Material mat, string name, Vector3 pos, BiomeId biome, Color color, System.Action<MeshBuilder, List<(Vector3, float)>> build, float obstacleR, MapIcon icon)
        {
            var mb = new MeshBuilder();
            var obstacles = new List<(Vector3, float)>();
            buildAt = pos;   // so a model can reach down to the real ground under each of its feet (GroundRel)
            build(mb, obstacles);
            var go = new GameObject("Landmark_" + name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh(name);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            foreach (var o in obstacles) WorldCollision.AddObstacle(pos + o.Item1, o.Item2, go);
            List.Add(new Landmark { name = name, pos = pos, biome = biome, color = color, go = go, icon = icon });
        }

        /// <summary>Nearest dry, gentle ground to p.</summary>
        public static Vector3 FindLand(Vector3 p, float minNy = 0.85f)
        {
            for (int r = 0; r < 30; r++)
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                float x = p.x + Mathf.Cos(a) * r * 10f, z = p.z + Mathf.Sin(a) * r * 10f;
                float h = WorldGen.Height(x, z);
                if (h > 1.5f && WorldGen.Normal(x, z, 2f).y > minNy) return new Vector3(x, h, z);
            }
            p.y = WorldGen.Height(p.x, p.z);
            return p;
        }

        public static bool NearLandmark(Vector3 p, float r)
        {
            foreach (var l in List)
            {
                Vector3 d = l.pos - p; d.y = 0;
                if (d.magnitude < r + 12f) return true;
            }
            return false;
        }

        public static void Cull(Vector3 focus, float range)
        {
            float r2 = range * range;
            foreach (var l in List)
            {
                bool on = (l.pos - focus).sqrMagnitude < r2;
                if (l.go.activeSelf != on) l.go.SetActive(on);
            }
        }

        static Vector3 buildAt;

        /// <summary>
        /// The lowest ground within r of a point beside the landmark, relative to its own base: a model is built round the
        /// height at its middle, and on a hump a leg 14 m out would end in the air (user, 2026-10-20: the Sunforge Arch
        /// "floating").
        /// </summary>
        static float GroundRel(float dx, float dz, float r)
        {
            float low = float.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                float a = i / 8f * Mathf.PI * 2f, k = i == 8 ? 0f : r;
                low = Mathf.Min(low, WorldGen.Height(buildAt.x + dx + Mathf.Cos(a) * k, buildAt.z + dz + Mathf.Sin(a) * k));
            }
            return low - buildAt.y;
        }

        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        static Color32 C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        // ---------------------------------------------------------------- MODELS
        static void BloomTree(MeshBuilder mb, List<(Vector3, float)> obs)
        {
            mb.Cylinder(M(0, 12f, 0, 6f, 26f, 6f), C("#6a4630"), C("#8a6040"), 12, 0.55f, false);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                mb.Cylinder(M(Mathf.Cos(a) * 3.5f, 1.5f, Mathf.Sin(a) * 3.5f, 2f, 5f, 2f, Mathf.Sin(a) * 30f, 0, -Mathf.Cos(a) * 30f), C("#5a3a26"), C("#6a4630"), 8, 0.4f, false);
            }
            mb.Sphere(M(0, 28f, 0, 26f, 16f, 26f), C("#ff9ac8"), C("#e070a8"), 16, 10);
            mb.Sphere(M(9f, 25f, 4f, 14f, 10f, 14f), C("#ffb4d8"), C("#e07aa8"), 12, 8);
            mb.Sphere(M(-8f, 26f, -6f, 15f, 10f, 15f), C("#ff8abe"), C("#d0609a"), 12, 8);
            mb.Sphere(M(2f, 33f, -3f, 12f, 8f, 12f), C("#ffd0e8"), C("#f090c0"), 12, 8);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                mb.Sphere(M(Mathf.Cos(a) * 11f, 20f + (i % 3), Mathf.Sin(a) * 11f, 0.9f, 0.9f, 0.9f), C("#ffe46a"), 6, 4);
            }
            obs.Add((Vector3.zero, 3.4f));
        }

        static void StoneCircle(MeshBuilder mb, List<(Vector3, float)> obs)
        {
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                float g = GroundRel(Mathf.Cos(a) * 11f, Mathf.Sin(a) * 11f, 0.8f);   // the ground under this stone
                var p = new Vector3(Mathf.Cos(a) * 11f, 1.8f + g, Mathf.Sin(a) * 11f);
                bool fallen = i == 3 || i == 7;
                float h = 3.8f + (i % 3) * 0.6f + 0.6f;   // a little taller, the extra sunk into the ground
                if (fallen) mb.Box(M(p.x, 0.5f + g, p.z, 1.2f, 3.6f, 1.0f, 90, a * Mathf.Rad2Deg, 0), C("#8a8e90"), C("#9ab08a"));
                else mb.Box(M(p.x, g + h * 0.5f - 0.6f, p.z, 1.3f, h, 1.0f, 0, -a * Mathf.Rad2Deg, (i % 2) * 4f), C("#8a8e90"), C("#9ab08a"));
                obs.Add((new Vector3(p.x, 0, p.z), 0.9f));
            }
            mb.Cylinder(M(0, 0.2f, 0, 5f, 0.5f, 5f), C("#7a7e80"), C("#9aa0a0"), 12, 1f, true);
            mb.Crystal(M(0, 0.4f, 0, 0.8f, 2.2f, 0.8f), C("#9af0c0"), C("#e8fff0"));
            obs.Add((Vector3.zero, 2.5f));
        }

        static void SunforgeArch(MeshBuilder mb, List<(Vector3, float)> obs)
        {
            // each leg runs from 2 m under the lowest ground at its foot up into the span (it was a fixed -1..23 m)
            for (int k = -1; k <= 1; k += 2)
            {
                float bottom = Mathf.Min(-1f, GroundRel(k * 14f, 0f, 4.5f) - 2f), top = 23f;
                mb.Rock(M(k * 14f, (bottom + top) * 0.5f, 0, 9f, top - bottom, 9f), C("#b0583a"), C("#d0784a"), k < 0 ? 71u : 73u, 8, 6, 0.12f);
            }
            mb.Rock(M(0, 24f, 0, 36f, 8f, 10f), C("#c06a40"), C("#e0905a"), 79u, 9, 6, 0.1f);
            mb.Rock(M(0, 29f, 0, 20f, 4f, 7f), C("#d88a5a"), C("#f0b07a"), 83u, 8, 5, 0.1f);
            mb.Sphere(M(0, 33f, 0, 3f, 3f, 3f), C("#ffd060"), 10, 7);
            obs.Add((new Vector3(-14f, 0, 0), 4.2f));
            obs.Add((new Vector3(14f, 0, 0), 4.2f));
        }

        /// <summary>An alpha's lair: great curved ribs around a trampled nest, a skull, clawed stones.</summary>
        static void AlphaLair(MeshBuilder mb, List<(Vector3, float)> obs)
        {
            Color32 bone = C("#e6dfcc"), boneDark = C("#b8ae96"), nest = C("#6b5234"), nestTop = C("#8a6c44"), stone = C("#7c7a78"), stoneTop = C("#9a9894");
            // ribs: two rows leaning in over the nest
            for (int i = 0; i < 6; i++)
            {
                float z = -6f + i * 2.4f;
                float hgt = 4.6f + Mathf.Sin(i / 5f * Mathf.PI) * 2.6f;
                for (int k = -1; k <= 1; k += 2)
                {
                    mb.Cylinder(M(k * 4.6f, hgt * 0.45f, z, 0.85f, hgt, 0.85f, 0, 0, k * 18f), boneDark, bone, 6, 0.6f, true);
                    mb.Cylinder(M(k * 2.9f, hgt * 0.97f, z, 0.5f, hgt * 0.5f, 0.5f, 0, 0, k * 58f), bone, bone, 6, 0.25f, true);
                    if (i % 2 == 0) obs.Add((new Vector3(k * 4.9f, 0, z), 0.6f));
                }
            }
            // spine along the ground behind
            for (int i = 0; i < 7; i++) mb.Sphere(M(0, 0.35f, -7.5f + i * 2.5f, 0.9f, 0.6f, 1f), boneDark, 6, 4);
            // nest
            mb.Cylinder(M(0, 0.25f, 0, 6.4f, 0.5f, 6.4f), nest, nestTop, 12, 0.85f, true);
            for (int i = 0; i < 14; i++)
            {
                float a = i * 0.449f * 2f;
                mb.Cylinder(M(Mathf.Cos(a) * 3.1f, 0.55f, Mathf.Sin(a) * 3.1f, 0.16f, 2.6f, 0.16f, 84f, a * Mathf.Rad2Deg + 70f, 0), nestTop, 5);
            }
            // skull at the head of the ribs
            mb.Sphere(M(0, 1.2f, 9.2f, 2.4f, 1.9f, 2.9f), bone, boneDark, 9, 6);
            mb.Sphere(M(0, 0.7f, 10.8f, 1.6f, 1.1f, 1.8f), bone, boneDark, 8, 5);
            mb.Sphere(M(0.62f, 1.45f, 10.1f, 0.5f, 0.5f, 0.4f), C("#1a1614"), 6, 4);
            mb.Sphere(M(-0.62f, 1.45f, 10.1f, 0.5f, 0.5f, 0.4f), C("#1a1614"), 6, 4);
            mb.Cone(M(0.9f, 2.5f, 8.6f, 0.4f, 1.8f, 0.4f, -25f, 0, -20f), boneDark, 6);
            mb.Cone(M(-0.9f, 2.5f, 8.6f, 0.4f, 1.8f, 0.4f, -25f, 0, 20f), boneDark, 6);
            obs.Add((new Vector3(0, 0, 9.6f), 1.7f));
            // clawed marker stones at the way in
            mb.Rock(M(6.5f, 1.6f, -9f, 2.2f, 3.8f, 2f), stone, stoneTop, 5u, 7, 5, 0.12f);
            mb.Rock(M(-6.5f, 1.4f, -9f, 2f, 3.2f, 2.2f), stone, stoneTop, 9u, 7, 5, 0.12f);
            // a horned skull set on top of each
            for (int k = -1; k <= 1; k += 2)
            {
                mb.Sphere(M(6.5f * k, 3.75f, -9f, 0.9f, 0.75f, 1f), bone, boneDark, 7, 5);
                mb.Cone(M(6.5f * k + 0.45f, 4.3f, -9f, 0.18f, 0.8f, 0.18f, 0, 0, -28f), boneDark, 5);
                mb.Cone(M(6.5f * k - 0.45f, 4.3f, -9f, 0.18f, 0.8f, 0.18f, 0, 0, 28f), boneDark, 5);
            }
            obs.Add((new Vector3(6.5f, 0, -9f), 1.2f));
            obs.Add((new Vector3(-6.5f, 0, -9f), 1.2f));
        }

        static void ShardSpire(MeshBuilder mb, List<(Vector3, float)> obs)
        {
            mb.Rock(M(0, 3f, 0, 14f, 7f, 14f), C("#6a7384"), C("#9aa6b8"), 91u, 9, 6, 0.15f);
            mb.Crystal(M(0, 4f, 0, 5f, 34f, 5f), C("#7fdcff"), C("#ffffff"));
            mb.Crystal(M(3.5f, 4f, 1f, 2.6f, 16f, 2.6f, 0, 0, -14f), C("#9ae8ff"), C("#ffffff"));
            mb.Crystal(M(-3f, 4f, -2f, 2.2f, 13f, 2.2f, 8f, 0, 16f), C("#a8c8ff"), C("#ffffff"));
            mb.Crystal(M(-1f, 4f, 3.5f, 1.8f, 10f, 1.8f, -16f, 0, 4f), C("#8fe8ff"), C("#ffffff"));
            obs.Add((Vector3.zero, 3.2f));
        }
    }
}
