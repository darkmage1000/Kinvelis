using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Caves (dungeons). The overworld is a heightfield, so caves live in a reserved zone in the far ocean corner,
    /// high above the sea (FloorY), and are generated when entered: rooms + 2-cell corridors on a 4 m grid, inward-facing
    /// walls (the camera sees through their backs), grid collision, cave-flavoured wild kin a few levels tougher,
    /// treasure chests, and a boss guardian (an alpha of the biome's Epic) whose defeat opens a portal out and
    /// unseals the big chest. Leave by the entrance portal or the boss portal; dying or loading puts you back outside.
    /// </summary>
    public class Caves : MonoBehaviour
    {
        public static Caves I { get; private set; }
        public static bool InCave => I != null && I.run != null;

        public const float FloorY = 420f;
        public const float Cell = 4f;
        public const int N = 46;                                         // 184 m square
        static readonly Vector2 Origin = new Vector2(1832f, -2026f);    // ocean corner, inside the movement clamps
        const float WallH = 9f;
        const double RefreshSeconds = 20 * 60;                          // boss & chests return after 20 min

        public static bool InZone(float x, float z) =>
            x >= Origin.x - 3f && x <= Origin.x + N * Cell + 3f && z >= Origin.y - 3f && z <= Origin.y + N * Cell + 3f;

        // ================================================================ ENTRANCES
        public class Entrance
        {
            public string id, name, boss;
            public CaveKind kind;
            public BiomeId biome;
            public Vector3 pos, outward;
            public int minLv, maxLv;
            public Color rock, rockTop, floor, glow;
            public MapIcon Icon => kind == CaveKind.Ruin ? MapIcon.Dungeon : kind == CaveKind.Burrow ? MapIcon.Burrow : MapIcon.Cave;
            public string Word => kind == CaveKind.Ruin ? "ruin" : kind == CaveKind.Burrow ? "burrow" : "cave";
        }

        public readonly List<Entrance> Entrances = new List<Entrance>();
        readonly List<GameObject> mouths = new List<GameObject>();
        float cullTimer;
        Image fade;
        float discoverTimer;

        public void Init(Transform uiRoot)
        {
            I = this;
            Add("mossglow", "Mossglow Hollow", BiomeId.Meadow, new Vector3(420f, 0f, -560f), 6, 9, "bloomwyrm",
                new Color(0.32f, 0.36f, 0.3f), new Color(0.2f, 0.26f, 0.2f), new Color(0.3f, 0.33f, 0.26f), new Color(0.45f, 1f, 0.6f));
            Add("cinderdeep", "Cinderdeep", BiomeId.Badlands, new Vector3(980f, 0f, 260f), 15, 18, "solarion",
                new Color(0.42f, 0.27f, 0.22f), new Color(0.22f, 0.14f, 0.12f), new Color(0.36f, 0.24f, 0.2f), new Color(1f, 0.55f, 0.2f));
            Add("rimefrost", "Rimefrost Grotto", BiomeId.Peaks, new Vector3(-760f, 0f, 560f), 23, 26, "aurorox",
                new Color(0.34f, 0.4f, 0.48f), new Color(0.2f, 0.25f, 0.34f), new Color(0.36f, 0.42f, 0.5f), new Color(0.55f, 0.9f, 1f));

            // --- burrows: short dens with a pack leader, no guardian ---
            Site("thistle", "Thistle Burrow", CaveKind.Burrow, BiomeId.Meadow, new Vector3(-160f, 0f, -760f), 2, 4, null);
            Site("scorchden", "Scorchden", CaveKind.Burrow, BiomeId.Badlands, new Vector3(1420f, 0f, 240f), 12, 14, null);
            Site("snowpelt", "Snowpelt Den", CaveKind.Burrow, BiomeId.Peaks, new Vector3(-1320f, 0f, 420f), 20, 22, null);
            // --- ruins: built halls with spike traps, an ambush and a guarded vault ---
            Site("hearthvault", "Old Hearth Vault", CaveKind.Ruin, BiomeId.Meadow, new Vector3(-520f, 0f, -120f), 10, 13, null);
            Site("ashvault", "Ashen Vault", CaveKind.Ruin, BiomeId.Badlands, new Vector3(1080f, 0f, 900f), 19, 22, null);
            Site("frosthalls", "Frostbound Halls", CaveKind.Ruin, BiomeId.Peaks, new Vector3(-900f, 0f, 1080f), 27, 30, null);
            // --- the outer lands: one deep place and one den each ---
            Outer("coralhush", "Coralhush Grotto", CaveKind.Cave, BiomeId.Isles, 0.35f, 60f, 8, 12, "tortide");
            Outer("driftden", "Driftwood Den", CaveKind.Burrow, BiomeId.Isles, 0.45f, 230f, 5, 8, null);
            Outer("drowned", "Drowned Temple", CaveKind.Ruin, BiomeId.Swamp, 0.35f, 100f, 15, 20, "mirejaw");
            Outer("mudbank", "Mudbank Den", CaveKind.Burrow, BiomeId.Swamp, 0.45f, 290f, 12, 15, null);
            Outer("sandsunk", "Sandsunk Tomb", CaveKind.Ruin, BiomeId.Desert, 0.4f, 30f, 23, 29, "wrapscal");
            Outer("dunewarren", "Dune Warren", CaveKind.Burrow, BiomeId.Desert, 0.45f, 200f, 20, 23, null);
            Outer("rotmaw", "Rotmaw Pit", CaveKind.Cave, BiomeId.Blight, 0.4f, 140f, 31, 38, "sludgeback");
            Outer("gnawhole", "Gnawhole", CaveKind.Burrow, BiomeId.Blight, 0.45f, 320f, 28, 31, null);
            Outer("starwell", "Starwell Deep", CaveKind.Cave, BiomeId.Glow, 0.4f, 80f, 38, 45, "glowmaw");
            Outer("glimmerden", "Glimmer Den", CaveKind.Burrow, BiomeId.Glow, 0.45f, 250f, 34, 38, null);

            fade = UIFactory.Panel(uiRoot, "CaveFade", new Color(0f, 0f, 0f, 0f));
            fade.sprite = null;
            fade.rectTransform.Stretch(-50, -50, -50, -50);
            fade.raycastTarget = false;
            fade.gameObject.SetActive(false);
        }

        void Add(string id, string name, BiomeId biome, Vector3 near, int minLv, int maxLv, string boss, Color rock, Color top, Color floor, Color glow)
        {
            var p = Landmarks.FindLand(near, 0.9f);
            var o = new Vector3(WorldGen.MeadowCenter.x - p.x, 0f, WorldGen.MeadowCenter.y - p.z);
            o = o.sqrMagnitude > 1f ? o.normalized : Vector3.forward;
            var e = new Entrance { id = id, name = name, biome = biome, pos = p, outward = o, minLv = minLv, maxLv = maxLv, boss = boss, rock = rock, rockTop = top, floor = floor, glow = glow };
            Entrances.Add(e);
            BuildMouth(e);
        }

        /// <summary>A new place on the old island: searched for near a hint, on gentle dry ground of the right biome and clear of everything else.</summary>
        void Site(string id, string name, CaveKind kind, BiomeId biome, Vector3 near, int minLv, int maxLv, string boss)
        {
            Tones(biome, kind, out var rock, out var top, out var floor, out var glow);
            var p = FindSite(biome, near, kind);
            var e = new Entrance { id = id, name = name, kind = kind, biome = biome, pos = p, outward = Facing(p, new Vector3(WorldGen.MeadowCenter.x, 0f, WorldGen.MeadowCenter.y)),
                minLv = minLv, maxLv = maxLv, boss = boss, rock = rock, rockTop = top, floor = floor, glow = glow };
            Entrances.Add(e);
            BuildMouth(e);
        }

        /// <summary>A place on an outer land, 'frac' of the way out from its middle at 'angle' degrees.</summary>
        void Outer(string id, string name, CaveKind kind, BiomeId biome, float frac, float angle, int minLv, int maxLv, string boss)
        {
            if (!WorldGen.TryLand(biome, out var land)) return;
            float a = angle * Mathf.Deg2Rad;
            var near = new Vector3(land.center.x + Mathf.Cos(a) * land.radius * frac, 0f, land.center.y + Mathf.Sin(a) * land.radius * frac);
            Tones(biome, kind, out var rock, out var top, out var floor, out var glow);
            var p = FindSite(biome, near, kind);
            var e = new Entrance { id = id, name = name, kind = kind, biome = biome, pos = p, outward = Facing(p, new Vector3(land.center.x, 0f, land.center.y)),
                minLv = minLv, maxLv = maxLv, boss = boss, rock = rock, rockTop = top, floor = floor, glow = glow };
            Entrances.Add(e);
            BuildMouth(e);
        }

        Vector3 FindSite(BiomeId biome, Vector3 near, CaveKind kind)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float minNy = kind == CaveKind.Ruin && pass == 0 ? 0.99f : 0.93f;
                for (int ring = 0; ring < 40; ring++)
                for (int i = 0; i < 12; i++)
                {
                    float a = (i + ring * 0.37f) / 12f * Mathf.PI * 2f;
                    float x = near.x + Mathf.Cos(a) * ring * 22f, z = near.z + Mathf.Sin(a) * ring * 22f;
                    float h = WorldGen.Height(x, z);
                    if (h < 2.5f || WorldGen.Normal(x, z, 5f).y < minNy || WorldGen.Biome(x, z) != biome) { if (ring == 0) break; continue; }
                    var p = new Vector3(x, h, z);
                    if (Free(p)) return p;
                    if (ring == 0) break;
                }
                if (kind != CaveKind.Ruin) break;
            }
            return Landmarks.FindLand(near, 0.9f);
        }

        bool Free(Vector3 p)
        {
            foreach (var e in Entrances) if (Flat2(e.pos, p) < 140f * 140f) return false;
            if (Landmarks.NearLandmark(p, 45f)) return false;
            foreach (var s in Settlements.All) if (Flat2(s.pos, p) < 190f * 190f) return false;   // clear of the village and its tamers
            foreach (var w in Wardens.All) if (Flat2(w.pos, p) < 90f * 90f) return false;
            foreach (var t in Tamers.All) if (Flat2(t.pos, p) < 40f * 40f) return false;
            return WorldGen.StartDistance(p.x, p.z) > 170f;
        }

        static float Flat2(Vector3 a, Vector3 b) { float dx = a.x - b.x, dz = a.z - b.z; return dx * dx + dz * dz; }

        /// <summary>Which way the mouth opens: the most level dry ground in front of it, leaning toward 'toward'.</summary>
        static Vector3 Facing(Vector3 p, Vector3 toward)
        {
            Vector3 want = toward - p; want.y = 0f;
            want = want.sqrMagnitude > 1f ? want.normalized : Vector3.forward;
            Vector3 best = want; float bestScore = 1e9f;
            for (int i = 0; i < 8; i++)
            {
                var d = new Vector3(Mathf.Cos(i * Mathf.PI / 4f), 0f, Mathf.Sin(i * Mathf.PI / 4f));
                float h = WorldGen.Height(p.x + d.x * 7f, p.z + d.z * 7f);
                if (h < 1f) continue;
                float score = Mathf.Abs(h - p.y) + (1f - Vector3.Dot(d, want)) * 0.6f;
                if (score < bestScore) { bestScore = score; best = d; }
            }
            return best;
        }

        static void Tones(BiomeId b, CaveKind k, out Color rock, out Color top, out Color floor, out Color glow)
        {
            switch (b)
            {
                case BiomeId.Meadow: rock = new Color(0.32f, 0.36f, 0.3f); top = new Color(0.2f, 0.26f, 0.2f); floor = new Color(0.3f, 0.33f, 0.26f); glow = new Color(0.45f, 1f, 0.6f); break;
                case BiomeId.Badlands: rock = new Color(0.42f, 0.27f, 0.22f); top = new Color(0.22f, 0.14f, 0.12f); floor = new Color(0.36f, 0.24f, 0.2f); glow = new Color(1f, 0.55f, 0.2f); break;
                case BiomeId.Peaks: rock = new Color(0.34f, 0.4f, 0.48f); top = new Color(0.2f, 0.25f, 0.34f); floor = new Color(0.36f, 0.42f, 0.5f); glow = new Color(0.55f, 0.9f, 1f); break;
                case BiomeId.Isles: rock = new Color(0.36f, 0.43f, 0.45f); top = new Color(0.2f, 0.3f, 0.34f); floor = new Color(0.5f, 0.47f, 0.38f); glow = new Color(0.3f, 0.95f, 0.9f); break;
                case BiomeId.Swamp: rock = new Color(0.29f, 0.31f, 0.22f); top = new Color(0.16f, 0.2f, 0.14f); floor = new Color(0.27f, 0.27f, 0.2f); glow = new Color(0.7f, 1f, 0.35f); break;
                case BiomeId.Desert: rock = new Color(0.62f, 0.5f, 0.34f); top = new Color(0.42f, 0.32f, 0.2f); floor = new Color(0.6f, 0.5f, 0.36f); glow = new Color(1f, 0.8f, 0.3f); break;
                case BiomeId.Blight: rock = new Color(0.3f, 0.24f, 0.34f); top = new Color(0.16f, 0.12f, 0.2f); floor = new Color(0.26f, 0.22f, 0.3f); glow = new Color(0.75f, 0.4f, 1f); break;
                default: rock = new Color(0.17f, 0.21f, 0.33f); top = new Color(0.08f, 0.1f, 0.2f); floor = new Color(0.15f, 0.19f, 0.29f); glow = new Color(0.4f, 1f, 0.95f); break;
            }
            if (k == CaveKind.Ruin)
            {
                // worked stone: greyer, more even
                var brick = new Color(0.5f, 0.48f, 0.44f);
                rock = Color.Lerp(rock, brick, 0.5f); top = Color.Lerp(top, brick * 0.6f, 0.5f); floor = Color.Lerp(floor, brick * 0.9f, 0.5f);
            }
            else if (k == CaveKind.Burrow)
            {
                // packed earth
                var earth = new Color(0.5f, 0.38f, 0.25f);
                rock = Color.Lerp(rock, earth, 0.7f); top = Color.Lerp(top, earth * 0.6f, 0.7f); floor = Color.Lerp(floor, earth * 0.95f, 0.7f);
            }
        }

        void BuildMouth(Entrance e)
        {
            if (e.kind == CaveKind.Ruin) BuildDoorway(e);
            else if (e.kind == CaveKind.Burrow) BuildHole(e);
            else BuildCaveMouth(e);
        }

        void MouthObject(Entrance e, MeshBuilder mb)
        {
            var go = new GameObject("CaveMouth_" + e.id);
            go.transform.SetParent(transform, false);
            go.transform.position = e.pos;
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cavemouth_" + e.id);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mouths.Add(go);
        }

        /// <summary>A ruin's way in: a pillared stone doorway with steps, set into a slab of old wall.</summary>
        void BuildDoorway(Entrance e)
        {
            var mb = new MeshBuilder();
            Vector3 back = -e.outward, side = new Vector3(e.outward.z, 0f, -e.outward.x);
            var rot = Quaternion.LookRotation(e.outward);
            Color32 stone = Color.Lerp(e.rock, Color.white, 0.25f), dark = e.rock, trim = Color.Lerp(e.rock, Color.white, 0.45f), g = e.glow;
            float Gy(Vector3 o) => WorldGen.Height(e.pos.x + o.x, e.pos.z + o.z) - e.pos.y;
            // footing (hides any slope), wall slab, lower wings, dark door
            mb.Box(Matrix4x4.TRS(back * 1.9f + Vector3.down * 1.4f, rot, new Vector3(14.5f, 3f, 4.6f)), dark, stone);
            mb.Box(Matrix4x4.TRS(back * 2.8f + Vector3.up * 2.6f, rot, new Vector3(9.5f, 6.4f, 1.6f)), dark, stone);
            for (int k = -1; k <= 1; k += 2)
            {
                mb.Box(Matrix4x4.TRS(side * (5.9f * k) + back * 2.9f + Vector3.up * 1.6f, rot, new Vector3(2.6f, 4.2f, 1.3f)), dark, stone);
                mb.Box(Matrix4x4.TRS(side * (5.9f * k) + back * 2.9f + Vector3.up * 3.85f, rot, new Vector3(3f, 0.4f, 1.6f)), trim);
                mb.Box(Matrix4x4.TRS(side * (4.75f * k) + back * 2.3f + Vector3.up * 3.3f, rot, new Vector3(0.5f, 1.4f, 0.12f)), g);   // glowing rune slot
            }
            mb.Box(Matrix4x4.TRS(back * 1.95f + Vector3.up * 2.1f, rot, new Vector3(3.6f, 4.4f, 0.3f)), new Color(0.03f, 0.025f, 0.05f));
            // pillars, lintel, cap
            for (int k = -1; k <= 1; k += 2)
            {
                mb.Box(Matrix4x4.TRS(side * (2.5f * k) + back * 1.5f + Vector3.up * 2.5f, rot, new Vector3(1.1f, 5.4f, 1.1f)), stone, trim);
                mb.Box(Matrix4x4.TRS(side * (2.5f * k) + back * 1.5f + Vector3.up * 0.25f, rot, new Vector3(1.5f, 0.6f, 1.5f)), trim);
                // brazier
                Vector3 bz = side * (4.3f * k) + e.outward * 1.6f;
                bz.y = Gy(bz);
                mb.Cylinder(Matrix4x4.TRS(bz + Vector3.up * 0.5f, Quaternion.identity, new Vector3(0.45f, 1.6f, 0.45f)), dark, 8);
                mb.Cylinder(Matrix4x4.TRS(bz + Vector3.up * 1.4f, Quaternion.identity, new Vector3(1f, 0.3f, 1f)), stone, 8, 1.4f);
                mb.Sphere(Matrix4x4.TRS(bz + Vector3.up * 1.8f, Quaternion.identity, new Vector3(0.75f, 1f, 0.75f)), g, 8, 5);
                WorldCollision.AddObstacle(e.pos + side * (2.5f * k) + back * 1.5f, 0.8f, null);
                WorldCollision.AddObstacle(e.pos + side * (4.3f * k) + e.outward * 1.6f, 0.5f, null);
                WorldCollision.AddObstacle(e.pos + side * (3.8f * k) + back * 2.8f, 1.2f, null);
                WorldCollision.AddObstacle(e.pos + side * (6f * k) + back * 2.9f, 1.4f, null);
            }
            mb.Box(Matrix4x4.TRS(back * 1.5f + Vector3.up * 5.6f, rot, new Vector3(7.2f, 1f, 1.6f)), trim, stone);
            mb.Box(Matrix4x4.TRS(back * 1.5f + Vector3.up * 6.4f, rot, new Vector3(4.6f, 0.7f, 1.3f)), stone, trim);
            mb.Crystal(Matrix4x4.TRS(back * 1.5f + Vector3.up * 6.7f, Quaternion.identity, new Vector3(0.5f, 1.1f, 0.5f)), g, Color.white);
            // steps
            mb.Box(Matrix4x4.TRS(back * 0.6f + Vector3.up * 0.12f, rot, new Vector3(4.4f, 0.3f, 1.6f)), trim);
            mb.Box(Matrix4x4.TRS(e.outward * 0.7f + Vector3.up * 0.03f, rot, new Vector3(5.2f, 0.2f, 1.6f)), stone);
            // a toppled pillar and rubble
            Vector3 fp = side * 7.4f + e.outward * 3.4f, r1 = -side * 7.2f + e.outward * 2.6f, r2 = -side * 6f + e.outward * 4.2f;
            fp.y = Gy(fp); r1.y = Gy(r1); r2.y = Gy(r2);
            mb.Cylinder(Matrix4x4.TRS(fp + Vector3.up * 0.5f, Quaternion.Euler(0f, 30f, 90f) * rot, new Vector3(1f, 3.6f, 1f)), stone, trim, 8, 1f, true);
            mb.Rock(Matrix4x4.TRS(r1 + Vector3.up * 0.4f, Quaternion.identity, new Vector3(1.8f, 1.1f, 1.5f)), dark, stone, 17u, 6, 4);
            mb.Rock(Matrix4x4.TRS(r2 + Vector3.up * 0.25f, Quaternion.identity, new Vector3(1f, 0.6f, 0.9f)), dark, stone, 19u, 6, 4);
            WorldCollision.AddObstacle(e.pos + back * 2.6f, 1.7f, null);
            WorldCollision.AddObstacle(e.pos + fp, 1.2f, null);
            MouthObject(e, mb);
        }

        /// <summary>A burrow: a low earth bank with a dark hole, roots and a scatter of gnawed bones.</summary>
        void BuildHole(Entrance e)
        {
            var mb = new MeshBuilder();
            Vector3 back = -e.outward, side = new Vector3(e.outward.z, 0f, -e.outward.x);
            Color32 earth = new Color(0.4f, 0.3f, 0.2f), earthTop = Color.Lerp(BiomeInfo.Tint(e.biome), new Color(0.4f, 0.36f, 0.24f), 0.55f);
            Color32 bone = new Color(0.9f, 0.87f, 0.78f), root = new Color(0.3f, 0.2f, 0.12f);
            uint seed = 51;
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-100f, 100f, i / 6f) * Mathf.Deg2Rad;
                Vector3 dir = back * Mathf.Cos(a) + side * Mathf.Sin(a);
                var p = e.pos + dir * 3.1f;
                p.y = WorldGen.Height(p.x, p.z);
                float s = 2.3f + (i % 3) * 0.4f;
                mb.Rock(Matrix4x4.TRS(p - e.pos + Vector3.up * s * 0.22f, Quaternion.Euler(0, i * 53f, 0), new Vector3(s * 1.2f, s * 0.75f, s * 1.2f)), earth, earthTop, seed++, 7, 5, 0.12f);
                WorldCollision.AddObstacle(p, s * 0.5f, null);
            }
            mb.Rock(Matrix4x4.TRS(back * 1.3f + Vector3.up * 2.2f, Quaternion.Euler(0, 12f, 0), new Vector3(4.6f, 1.6f, 3.2f)), earth, earthTop, 61u, 8, 5, 0.12f);
            mb.Sphere(Matrix4x4.TRS(back * 1.5f + Vector3.up * 0.9f, Quaternion.LookRotation(e.outward), new Vector3(2.6f, 2.1f, 1f)), new Color(0.03f, 0.025f, 0.04f), 10, 7);
            // roots hanging over the hole
            for (int i = 0; i < 4; i++)
                mb.Cylinder(Matrix4x4.TRS(back * 0.9f + side * (-1.1f + i * 0.7f) + Vector3.up * (1.7f - (i % 2) * 0.25f), Quaternion.Euler(0, 0, -8f + i * 6f), new Vector3(0.09f, 1f + (i % 2) * 0.5f, 0.09f)), root, 5);
            // bones
            mb.Cylinder(Matrix4x4.TRS(e.outward * 1.6f + side * 1.8f + Vector3.up * 0.08f, Quaternion.Euler(90f, 35f, 0), new Vector3(0.1f, 0.8f, 0.1f)), bone, 5);
            mb.Cylinder(Matrix4x4.TRS(e.outward * 2.1f - side * 1.4f + Vector3.up * 0.08f, Quaternion.Euler(90f, -50f, 0), new Vector3(0.09f, 0.6f, 0.09f)), bone, 5);
            mb.Sphere(Matrix4x4.TRS(e.outward * 1.2f - side * 2.1f + Vector3.up * 0.14f, Quaternion.identity, new Vector3(0.34f, 0.28f, 0.4f)), bone, 7, 5);
            WorldCollision.AddObstacle(e.pos + back * 1.6f, 1.2f, null);
            MouthObject(e, mb);
        }

        /// <summary>A mound of boulders around a dark opening that faces 'outward'.</summary>
        void BuildCaveMouth(Entrance e)
        {
            var mb = new MeshBuilder();
            Vector3 back = -e.outward, side = new Vector3(e.outward.z, 0f, -e.outward.x);
            Color32 stone = new Color(0.5f, 0.5f, 0.54f), stoneTop = new Color(0.64f, 0.64f, 0.68f);
            uint seed = 3;
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(-110f, 110f, i / 8f) * Mathf.Deg2Rad;
                Vector3 dir = back * Mathf.Cos(a) + side * Mathf.Sin(a);
                var p = e.pos + dir * 4.6f;
                p.y = WorldGen.Height(p.x, p.z);
                float s = 3.2f + (i % 3) * 0.6f;
                mb.Rock(Matrix4x4.TRS(p - e.pos + Vector3.up * s * 0.35f, Quaternion.Euler(0, i * 37f, 0), new Vector3(s, s * 1.1f, s)), stone, stoneTop, seed++, 7, 5);
                WorldCollision.AddObstacle(p, s * 0.5f, null);
            }
            // lintel boulder over the opening + the dark mouth
            mb.Rock(Matrix4x4.TRS(back * 1.6f + Vector3.up * 4.2f, Quaternion.Euler(0, 20f, 0), new Vector3(6.5f, 2.6f, 4f)), stone, stoneTop, 41u, 8, 5);
            mb.Sphere(Matrix4x4.TRS(back * 2.1f + Vector3.up * 1.7f, Quaternion.LookRotation(e.outward), new Vector3(4.2f, 3.6f, 1.2f)), new Color(0.03f, 0.025f, 0.05f), 10, 7);
            Color32 g = e.glow;
            mb.Crystal(Matrix4x4.TRS(side * 2.6f + back * 0.8f, Quaternion.Euler(0, 0, -15f), new Vector3(0.5f, 1.4f, 0.5f)), g, Color.white);
            mb.Crystal(Matrix4x4.TRS(-side * 2.4f + back * 0.6f, Quaternion.Euler(0, 0, 18f), new Vector3(0.4f, 1.1f, 0.4f)), g, Color.white);
            WorldCollision.AddObstacle(e.pos + back * 2.2f, 1.6f, null);
            MouthObject(e, mb);
        }

        /// <summary>Keeps trees / rocks from growing inside a cave mouth.</summary>
        public static bool BlocksNode(Vector3 p)
        {
            if (I == null) return false;
            foreach (var e in I.Entrances)
            {
                float dx = p.x - e.pos.x, dz = p.z - e.pos.z;
                float br = e.kind == CaveKind.Ruin ? 13f : 10f;
                if (dx * dx + dz * dz < br * br) return true;
            }
            return false;
        }

        public bool IsFound(Entrance e) => GameManager.I.Player.Stats.cavesFound.Contains(e.id);

        // ================================================================ ACTIVE RUN
        class Chest { public Vector3 pos; public GameObject go; public Transform lid; public bool opened, big; }

        class Run
        {
            public Entrance e;
            public bool[,] floor;
            public List<RectInt> rooms = new List<RectInt>();
            public GameObject root;
            public readonly List<CreatureActor> kin = new List<CreatureActor>();
            public CreatureActor boss;
            public CreatureData bossData;
            public readonly List<Chest> chests = new List<Chest>();
            public readonly List<Vector4> pillars = new List<Vector4>();   // x, z, radius
            public Vector3 startPos, exitPos;
            public Vector3? bossPortal;
            public bool bossDown, quiet;
            public Texture2D map;
            public readonly List<Trap> traps = new List<Trap>();
            public Vector3 ambushPos;
            public SpeciesDef ambushSp;
            public bool ambushSet, ambushDone;
        }

        class Trap { public Vector3 pos; public Transform spikes; public float phase, cool; public bool wasUp; }

        Run run;
        bool busy;
        public string CurrentName => run?.e.name;
        public Texture2D MapTexture => run?.map;
        /// <summary>Where to put the player if the game saves while underground.</summary>
        public Vector3 SafeOutside => run != null ? Outside(run.e) : GameManager.I.Player.Position;
        static Vector3 Outside(Entrance e) { var p = e.pos + e.outward * 3.5f; p.y = WorldGen.Height(p.x, p.z); return p; }

        public static Vector2 ZoneUV(Vector3 p) => new Vector2((p.x - Origin.x) / (N * Cell), (p.z - Origin.y) / (N * Cell));
        public const float ZoneSize = N * Cell;

        static Vector3 CellCenter(int i, int j) => new Vector3(Origin.x + (i + 0.5f) * Cell, FloorY, Origin.y + (j + 0.5f) * Cell);
        bool IsFloor(int i, int j) => run != null && i >= 0 && j >= 0 && i < N && j < N && run.floor[i, j];

        // ================================================================ TICK / INTERACTION
        public void Tick(float dt, PlayerController pl)
        {
            if (run == null)
            {
                // discovery
                discoverTimer -= dt;
                if (discoverTimer > 0f) return;
                discoverTimer = 1f;
                var st = pl.Stats;
                for (int i = 0; i < Entrances.Count; i++)
                {
                    var e = Entrances[i];
                    float d2 = (e.pos - pl.Position).sqrMagnitude;
                    bool show = d2 < 420f * 420f;
                    if (mouths[i].activeSelf != show) mouths[i].SetActive(show);
                    if (st.cavesFound.Contains(e.id)) continue;
                    if (d2 < 90f * 90f)
                    {
                        st.cavesFound.Add(e.id);
                        string hint = e.kind == CaveKind.Burrow ? "A short den - its pack leader guards a cache." : e.kind == CaveKind.Ruin ? "Old halls: watch the floor." : "Find its mouth and ENTER.";
                        GameEvents.Good($"Discovered a {e.Word}: {e.name} (Lv {e.minLv}-{e.maxLv}). {hint}");
                        SynthAudio.Play(Sfx.UiOpen, 0.8f);
                    }
                }
                return;
            }
            var r = run;
            TickTraps(r, dt, pl);
            if (r.ambushSet && !r.ambushDone && pl.IsAlive && Flat2(r.ambushPos, pl.Position) < 7f * 7f) SpringAmbush(r, pl);
            // guardian
            if (!r.bossDown && r.boss != null)
            {
                bool gone = r.boss.Data != r.bossData || !r.boss.gameObject.activeSelf || r.boss.State == AIState.Dead;
                if (gone) BossDefeated();
                else if (!r.boss.Aggro && (r.boss.Position - pl.Position).sqrMagnitude < 15f * 15f && pl.IsAlive)
                {
                    r.boss.Provoke(pl);
                    GameEvents.Warn(r.e.kind == CaveKind.Burrow ? $"The pack leader, a big {r.boss.Sp.displayName}, bristles!" : $"The guardian {r.boss.Sp.displayName} awakens!");
                }
            }
            // animate portals / chest lids
            foreach (var c in r.chests)
                if (c.opened && c.lid != null) c.lid.localRotation = Quaternion.Slerp(c.lid.localRotation, Quaternion.Euler(-100f, 0, 0), dt * 6f);
        }

        void BossDefeated()
        {
            var r = run;
            r.bossDown = true;
            var rec = Record(r.e.id);
            rec.clearedAt = GameManager.WorldTime;
            Bounties.OnCaveCleared(r.e.id);
            var room = r.rooms[r.rooms.Count - 1];
            var c = RoomCenter(room);
            r.bossPortal = c + new Vector3(0f, 0f, -Cell);
            BuildPortal(r.bossPortal.Value, r.e.glow);
            GameEvents.Good(r.e.kind == CaveKind.Burrow ? "The pack leader is down. Its cache is yours, and a way out opens." : "The guardian falls! The sealed chest opens up, and a portal out appears.");
            SynthAudio.Play(Sfx.LevelUp, 0.9f);
        }

        void TickTraps(Run r, float dt, PlayerController pl)
        {
            if (r.traps.Count == 0) return;
            Vector3 pp = pl.Position;
            foreach (var t in r.traps)
            {
                // 3.4 s cycle: hidden, tips showing (warning), then up
                float c = (Time.time + t.phase) % 3.4f;
                bool up = c > 2.3f;
                float y = up ? 0f : c > 1.6f ? -0.72f : -1.15f;
                var lp = t.spikes.localPosition;
                lp.y = Mathf.MoveTowards(lp.y, y, dt * (up ? 14f : 4f));
                t.spikes.localPosition = lp;
                if (up && !t.wasUp && Flat2(t.pos, pp) < 16f * 16f) SynthAudio.Play(Sfx.Clunk, 1.5f);
                t.wasUp = up;
                t.cool -= dt;
                if (!up || t.cool > 0f || !pl.IsAlive) continue;
                if (Mathf.Abs(pp.x - t.pos.x) < 1.75f && Mathf.Abs(pp.z - t.pos.z) < 1.75f && pp.y - FloorY < 1.1f)
                {
                    t.cool = 1.6f;
                    pl.ReceiveHit(new HitInfo { damage = Mathf.Max(4f, pl.Stats.MaxHp * 0.08f), element = Element.Normal, effectiveness = 1f });
                    HUD.DamageNumber(pp + Vector3.up * 2.2f, "SPIKES", new Color(1f, 0.5f, 0.4f), 0.8f);
                }
            }
        }

        void SpringAmbush(Run r, PlayerController pl)
        {
            r.ambushDone = true;
            var e = r.e;
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.094f + 0.5f;
                var p = r.ambushPos + new Vector3(Mathf.Cos(a) * 5.5f, 0f, Mathf.Sin(a) * 5.5f);
                var d = CreatureFactory.RollWildMaybeEvolved(r.ambushSp, Random.Range(e.minLv, e.maxLv + 1), e.biome, false);
                var act = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, p);
                act.Persistent = true;
                act.Provoke(pl);
                r.kin.Add(act);
                Fx.Burst(p + Vector3.up, e.glow, 14, 4f, 0.3f, 0.6f, true);
            }
            GameEvents.Warn("Ambush! The hall was not empty.");
            SynthAudio.Play(Sfx.Summon, 0.6f);
        }

        static Vector3 RoomCenter(RectInt rm) => new Vector3(Origin.x + (rm.x + rm.width * 0.5f) * Cell, FloorY, Origin.y + (rm.y + rm.height * 0.5f) * Cell);

        CaveRecord Record(string id)
        {
            var st = GameManager.I.Player.Stats;
            var rec = st.caveRecords.Find(x => x.id == id);
            if (rec == null) { rec = new CaveRecord { id = id, clearedAt = -1e9 }; st.caveRecords.Add(rec); }
            return rec;
        }

        /// <summary>Interaction under the player (ENTER / LEAVE / OPEN / SEALED). Returns null when nothing is close.</summary>
        public string Prompt(Vector3 p, out System.Action act)
        {
            act = null;
            if (busy) return null;
            if (run == null)
            {
                foreach (var e in Entrances)
                {
                    var mouth = e.pos + e.outward * 0.3f;
                    if ((mouth - p).sqrMagnitude < 4.2f * 4.2f)
                    {
                        var ee = e;
                        act = () => Enter(ee);
                        return e.kind == CaveKind.Ruin ? "ENTER RUIN" : e.kind == CaveKind.Burrow ? "ENTER DEN" : "ENTER CAVE";
                    }
                }
                return null;
            }
            if ((run.exitPos - p).sqrMagnitude < 3.2f * 3.2f || (run.bossPortal.HasValue && (run.bossPortal.Value - p).sqrMagnitude < 3.2f * 3.2f))
            {
                act = Leave;
                return "LEAVE";
            }
            foreach (var c in run.chests)
            {
                if (c.opened || (c.pos - p).sqrMagnitude > 2.6f * 2.6f) continue;
                if (c.big && !run.bossDown) { act = () => GameEvents.Say("Sealed - defeat the guardian first."); return "SEALED"; }
                var cc = c;
                act = () => OpenChest(cc);
                return "OPEN";
            }
            return null;
        }

        // ================================================================ ENTER / LEAVE
        public void Enter(Entrance e)
        {
            if (busy || run != null) return;
            StartCoroutine(EnterRoutine(e));
        }

        public void Leave()
        {
            if (busy || run == null) return;
            StartCoroutine(LeaveRoutine());
        }

        IEnumerator Fade(bool toBlack, float time)
        {
            fade.gameObject.SetActive(true);
            fade.transform.SetAsLastSibling();
            for (float t = 0; t < time; t += Time.unscaledDeltaTime) { fade.color = new Color(0f, 0f, 0f, toBlack ? t / time : 1f - t / time); yield return null; }
            fade.color = new Color(0f, 0f, 0f, toBlack ? 1f : 0f);
            if (!toBlack) fade.gameObject.SetActive(false);
        }

        IEnumerator EnterRoutine(Entrance e)
        {
            busy = true;
            var gm = GameManager.I;
            var pl = gm.Player;
            pl.CancelAim();
            SynthAudio.Play(Sfx.Whoosh, 0.6f);
            yield return Fade(true, 0.4f);

            if (pl.IsMounted) pl.Dismount(true);
            CreatureSpawner.I.DespawnAllWild();
            ProjectileSystem.I.ClearAll();
            gm.Capture.Cancel();

            run = new Run { e = e };
            int seed = WorldGen.Seed * 31;
            foreach (char ch in e.id) seed = seed * 17 + ch;   // stable across runtimes
            var rng = new System.Random(seed);
            Generate(run, rng);
            BuildGeometry(run, rng);
            var rec = Record(e.id);
            run.quiet = GameManager.WorldTime - rec.clearedAt < RefreshSeconds;

            pl.Teleport(run.startPos, YawToward(run.startPos, RoomCenter(run.rooms[Mathf.Min(1, run.rooms.Count - 1)])));
            gm.CameraRig.Init(gm.CameraRig.Cam, pl.Position, pl.transform.eulerAngles.y);
            var comp = gm.Party.Companion;
            if (comp != null && comp.gameObject.activeSelf) { var cp = run.startPos + pl.Right * 1.8f; comp.transform.position = cp; comp.Home = cp; }
            EnvironmentSystem.I?.SetIndoor(true, e.glow);
            SpawnContents(run, rng);
            yield return null;
            yield return Fade(false, 0.5f);

            GameEvents.Good($"{e.name} - Lv {e.minLv}-{e.maxLv}");
            if (run.quiet)
            {
                double left = RefreshSeconds - (GameManager.WorldTime - rec.clearedAt);
                GameEvents.Say($"It is quiet in here - the {(e.kind == CaveKind.Burrow ? "pack leader and its cache" : "guardian and treasure")} return in ~{Mathf.CeilToInt((float)(left / 60.0))} min.", new Color(0.8f, 0.85f, 1f));
            }
            else GameEvents.Say(e.kind == CaveKind.Burrow ? "A short den. The pack leader keeps a cache at the back."
                : e.kind == CaveKind.Ruin ? "Old halls. Spikes rise from the dark plates - cross when they drop. A guardian holds the vault."
                : "A guardian waits in the deepest chamber. Treasure lies along the way.", new Color(1f, 0.85f, 0.5f));
            busy = false;
        }

        IEnumerator LeaveRoutine()
        {
            busy = true;
            var gm = GameManager.I;
            var pl = gm.Player;
            pl.CancelAim();
            SynthAudio.Play(Sfx.Summon, 0.7f);
            yield return Fade(true, 0.4f);
            var e = run.e;
            Cleanup();
            var outside = Outside(e);
            pl.Teleport(outside, YawToward(e.pos, outside));
            gm.CameraRig.Init(gm.CameraRig.Cam, pl.Position, pl.transform.eulerAngles.y);
            var comp = gm.Party.Companion;
            if (comp != null && comp.gameObject.activeSelf) { var cp = outside + pl.Right * 1.8f; cp.y = WorldGen.Height(cp.x, cp.z); comp.transform.position = cp; comp.Home = cp; }
            for (float t = 0; t < 4f && !ChunkManager.I.IsChunkReady(outside); t += Time.unscaledDeltaTime) yield return null;
            yield return Fade(false, 0.5f);
            GameEvents.Say("Back in the open air.", new Color(0.8f, 1f, 0.8f));
            busy = false;
        }

        /// <summary>Immediate teardown (death, quitting). The caller decides where the player goes.</summary>
        public void Abort()
        {
            if (run == null) return;
            Cleanup();
            if (fade != null) fade.gameObject.SetActive(false);
            busy = false;
        }

        void Cleanup()
        {
            var r = run;
            run = null;
            foreach (var k in r.kin) if (k != null && k.gameObject.activeSelf && k.Mode == CreatureMode.Wild) k.Despawn();
            if (r.boss != null && r.boss.gameObject.activeSelf && r.boss.Data == r.bossData) r.boss.Despawn();
            CreatureSpawner.I.DespawnAllWild();
            ProjectileSystem.I.ClearAll();
            if (r.root != null) Destroy(r.root);
            if (r.map != null) Destroy(r.map);
            EnvironmentSystem.I?.SetIndoor(false, Color.white);
        }

        static float YawToward(Vector3 from, Vector3 to) { var d = to - from; return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }

        // ================================================================ LAYOUT
        static void Generate(Run r, System.Random rng)
        {
            r.floor = new bool[N, N];
            var kind = r.e.kind;
            int count = kind == CaveKind.Burrow ? rng.Next(3, 5) : kind == CaveKind.Ruin ? rng.Next(7, 10) : rng.Next(5, 8);
            var start = new RectInt(2, rng.Next(4, N - 12), 6, 6);
            r.rooms.Add(start);
            var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
            for (int i = 1; i < count; i++)
            {
                var prev = r.rooms[r.rooms.Count - 1];
                bool boss = i == count - 1;
                bool placed = false;
                for (int attempt = 0; attempt < 60 && !placed; attempt++)
                {
                    int w = boss ? 10 : rng.Next(5, 9), h = boss ? 10 : rng.Next(5, 9);
                    if (kind == CaveKind.Burrow) { w = Mathf.Min(w, boss ? 7 : 6); h = Mathf.Min(h, boss ? 7 : 6); }
                    var d = dirs[attempt < 20 ? (rng.NextDouble() < 0.6 ? 0 : rng.Next(1, 3)) : rng.Next(0, 4)];
                    int gap = rng.Next(3, 7);
                    int cx = prev.x + prev.width / 2 + d.x * (prev.width / 2 + gap + w / 2) + (d.x == 0 ? rng.Next(-3, 4) : 0);
                    int cz = prev.y + prev.height / 2 + d.y * (prev.height / 2 + gap + h / 2) + (d.y == 0 ? rng.Next(-3, 4) : 0);
                    var rm = new RectInt(cx - w / 2, cz - h / 2, w, h);
                    if (rm.xMin < 1 || rm.yMin < 1 || rm.xMax > N - 1 || rm.yMax > N - 1) continue;
                    bool overlap = false;
                    foreach (var o in r.rooms)
                        if (new RectInt(o.x - 2, o.y - 2, o.width + 4, o.height + 4).Overlaps(rm)) { overlap = true; break; }
                    if (overlap) continue;
                    r.rooms.Add(rm);
                    Corridor(r.floor, new Vector2Int(prev.x + prev.width / 2, prev.y + prev.height / 2), new Vector2Int(rm.x + rm.width / 2, rm.y + rm.height / 2), rng);
                    placed = true;
                }
                if (!placed) break;
            }
            foreach (var rm in r.rooms)
                for (int x = rm.xMin; x < rm.xMax; x++)
                for (int z = rm.yMin; z < rm.yMax; z++)
                    r.floor[x, z] = true;
            // soften room corners (ruins keep their square halls)
            if (kind != CaveKind.Ruin)
            foreach (var rm in r.rooms)
            {
                r.floor[rm.xMin, rm.yMin] = false; r.floor[rm.xMax - 1, rm.yMin] = false;
                r.floor[rm.xMin, rm.yMax - 1] = false; r.floor[rm.xMax - 1, rm.yMax - 1] = false;
            }
            r.startPos = CellCenter(start.x + 1, start.y + start.height / 2);
            r.exitPos = CellCenter(start.x, start.y + start.height / 2) + new Vector3(-0.6f, 0f, 0f);
        }

        static void Corridor(bool[,] f, Vector2Int a, Vector2Int b, System.Random rng)
        {
            bool xFirst = rng.NextDouble() < 0.5;
            Vector2Int corner = xFirst ? new Vector2Int(b.x, a.y) : new Vector2Int(a.x, b.y);
            Line(f, a, corner);
            Line(f, corner, b);
        }

        static void Line(bool[,] f, Vector2Int a, Vector2Int b)
        {
            int x = a.x, z = a.y;
            while (true)
            {
                for (int dx = 0; dx <= 1; dx++)
                for (int dz = 0; dz <= 1; dz++)
                {
                    int xx = Mathf.Clamp(x + dx, 1, N - 2), zz = Mathf.Clamp(z + dz, 1, N - 2);
                    f[xx, zz] = true;
                }
                if (x == b.x && z == b.y) break;
                if (x != b.x) x += b.x > x ? 1 : -1; else z += b.y > z ? 1 : -1;
            }
        }

        // ================================================================ GEOMETRY
        void BuildGeometry(Run r, System.Random rng)
        {
            r.root = new GameObject("Cave_" + r.e.id);
            r.root.transform.SetParent(transform, false);
            var verts = new List<Vector3>(16000);
            var norms = new List<Vector3>(16000);
            var cols = new List<Color32>(16000);
            var tris = new List<int>(24000);
            var e = r.e;

            for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                if (!r.floor[i, j]) continue;
                float x0 = Origin.x + i * Cell, z0 = Origin.y + j * Cell;
                float n = Hash.F(i, j, 77) * 0.25f + 0.88f;
                if (e.kind == CaveKind.Ruin) n = ((i + j) & 1) == 0 ? 1.04f : 0.86f;   // flagstones
                Color fc = e.floor * n;
                if (e.kind == CaveKind.Ruin)
                {
                    float hc = Cell * 0.5f;
                    for (int a = 0; a < 2; a++)
                    for (int b = 0; b < 2; b++)
                    {
                        Color tc = e.floor * (((a + b) & 1) == 0 ? 1.05f : 0.84f) * (0.94f + Hash.F(i * 2 + a, j * 2 + b, 77) * 0.12f);
                        float xa = x0 + a * hc, za = z0 + b * hc;
                        Quad(verts, norms, cols, tris, new Vector3(xa, FloorY, za), new Vector3(xa, FloorY, za + hc), new Vector3(xa + hc, FloorY, za + hc), new Vector3(xa + hc, FloorY, za), Vector3.up, tc, tc);
                    }
                }
                else
                Quad(verts, norms, cols, tris,
                    new Vector3(x0, FloorY, z0), new Vector3(x0, FloorY, z0 + Cell), new Vector3(x0 + Cell, FloorY, z0 + Cell), new Vector3(x0 + Cell, FloorY, z0),
                    Vector3.up, fc, fc);
                // walls facing into this cell
                if (!IsFloorRaw(r, i - 1, j)) Wall(verts, norms, cols, tris, new Vector3(x0, 0, z0 + Cell), new Vector3(x0, 0, z0), Vector3.right, e);
                if (!IsFloorRaw(r, i + 1, j)) Wall(verts, norms, cols, tris, new Vector3(x0 + Cell, 0, z0), new Vector3(x0 + Cell, 0, z0 + Cell), Vector3.left, e);
                if (!IsFloorRaw(r, i, j - 1)) Wall(verts, norms, cols, tris, new Vector3(x0, 0, z0), new Vector3(x0 + Cell, 0, z0), Vector3.forward, e);
                if (!IsFloorRaw(r, i, j + 1)) Wall(verts, norms, cols, tris, new Vector3(x0 + Cell, 0, z0 + Cell), new Vector3(x0, 0, z0 + Cell), Vector3.back, e);
            }
            var mesh = new Mesh { name = "cave_" + e.id, indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            var go = new GameObject("Shell");
            go.transform.SetParent(r.root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            // decor: glowing crystals along walls, stalagmites (with collision) in rooms
            var mb = new MeshBuilder();
            Color32 glow = e.glow, tip = Color.Lerp(e.glow, Color.white, 0.6f);
            for (int ri = 0; ri < r.rooms.Count; ri++)
            {
                var rm = r.rooms[ri];
                int crystals = 3 + rng.Next(0, 4);
                for (int k = 0; k < crystals; k++)
                {
                    // hug a random wall of the room
                    int side = rng.Next(0, 4);
                    float t = (float)rng.NextDouble();
                    Vector3 p = side == 0 ? new Vector3(rm.xMin + 0.35f, 0, Mathf.Lerp(rm.yMin + 1, rm.yMax - 1, t)) :
                                side == 1 ? new Vector3(rm.xMax - 0.35f, 0, Mathf.Lerp(rm.yMin + 1, rm.yMax - 1, t)) :
                                side == 2 ? new Vector3(Mathf.Lerp(rm.xMin + 1, rm.xMax - 1, t), 0, rm.yMin + 0.35f) :
                                            new Vector3(Mathf.Lerp(rm.xMin + 1, rm.xMax - 1, t), 0, rm.yMax - 0.35f);
                    var wp = new Vector3(Origin.x + p.x * Cell, FloorY, Origin.y + p.z * Cell);
                    float s = 0.6f + (float)rng.NextDouble() * 0.9f;
                    if (ri == 0 && Flat2(wp, r.exitPos) < 6f * 6f) wp += new Vector3(0f, 0f, wp.z < r.exitPos.z ? -6f : 6f);
                    if (e.kind == CaveKind.Ruin)
                    {
                        // a brazier against the wall
                        mb.Cylinder(Matrix4x4.TRS(wp + Vector3.up * 0.75f, Quaternion.identity, new Vector3(0.45f, 1.5f, 0.45f)), e.rockTop, 8);
                        mb.Cylinder(Matrix4x4.TRS(wp + Vector3.up * 1.62f, Quaternion.identity, new Vector3(1.1f, 0.32f, 1.1f)), Color.Lerp(e.rock, Color.white, 0.2f), 8, 1.4f);
                        mb.Sphere(Matrix4x4.TRS(wp + Vector3.up * 2.1f, Quaternion.identity, new Vector3(0.85f, 1.15f, 0.85f)), tip, glow, 8, 5);
                        r.pillars.Add(new Vector4(wp.x, wp.z, 0.6f, 0));
                    }
                    else if (e.kind == CaveKind.Burrow)
                    {
                        // glowcaps and a root
                        for (int q = 0; q < 5; q++)
                        {
                            var o = wp + new Vector3(Mathf.Cos(q * 1.9f) * (0.5f + q * 0.22f) * s, 0f, Mathf.Sin(q * 1.9f) * (0.5f + q * 0.22f) * s);
                            float hh = (0.7f + (q % 3) * 0.45f) * s;
                            mb.Cylinder(Matrix4x4.TRS(o + Vector3.up * hh * 0.5f, Quaternion.identity, new Vector3(0.16f, hh, 0.16f)), tip, 5);
                            mb.Dome(Matrix4x4.TRS(o + Vector3.up * hh, Quaternion.identity, new Vector3(0.95f * s, 0.5f * s, 0.95f * s)), tip, 8, 3);
                            mb.Sphere(Matrix4x4.TRS(o + Vector3.up * (hh - 0.03f), Quaternion.identity, new Vector3(0.8f * s, 0.14f * s, 0.8f * s)), glow, 7, 4);
                        }
                        for (int q = 0; q < 3; q++)
                            mb.Cylinder(Matrix4x4.TRS(wp + new Vector3(q * 0.7f - 0.7f, WallH - 2.2f - q * 0.4f, 0f), Quaternion.Euler((float)rng.NextDouble() * 16f - 8f, 0, (float)rng.NextDouble() * 16f - 8f), new Vector3(0.22f, 4.4f + q * 0.8f, 0.22f)), new Color(0.42f, 0.3f, 0.18f), 5);
                    }
                    else
                    {
                    mb.Crystal(Matrix4x4.TRS(wp, Quaternion.Euler((float)rng.NextDouble() * 30f - 15f, 0, (float)rng.NextDouble() * 30f - 15f), new Vector3(s * 0.5f, s * 1.8f, s * 0.5f)), glow, tip);
                    mb.Crystal(Matrix4x4.TRS(wp + new Vector3(0.5f, 0, 0.3f), Quaternion.Euler(0, 0, -25f), new Vector3(s * 0.3f, s, s * 0.3f)), glow, tip);
                    }
                }
                if (e.kind == CaveKind.Ruin)
                {
                    // a square pillar in each corner of the hall
                    for (int cx = 0; cx < 2; cx++)
                    for (int cz = 0; cz < 2; cz++)
                    {
                        var cp = new Vector3(Origin.x + (cx == 0 ? rm.xMin + 0.75f : rm.xMax - 0.75f) * Cell, FloorY, Origin.y + (cz == 0 ? rm.yMin + 0.75f : rm.yMax - 0.75f) * Cell);
                        mb.Box(Matrix4x4.TRS(cp + Vector3.up * WallH * 0.5f, Quaternion.identity, new Vector3(1.3f, WallH, 1.3f)), e.rock, e.rockTop);
                        mb.Box(Matrix4x4.TRS(cp + Vector3.up * 0.3f, Quaternion.identity, new Vector3(1.8f, 0.6f, 1.8f)), Color.Lerp(e.rock, Color.white, 0.2f));
                        mb.Box(Matrix4x4.TRS(cp + Vector3.up * 4.02f, Quaternion.identity, new Vector3(1.42f, 0.26f, 1.42f)), Color.Lerp(e.glow, e.rock, 0.2f));
                        mb.Box(Matrix4x4.TRS(cp + Vector3.up * (WallH - 0.4f), Quaternion.identity, new Vector3(1.9f, 0.8f, 1.9f)), e.rockTop);
                        r.pillars.Add(new Vector4(cp.x, cp.z, 0.95f, 0));
                    }
                    continue;
                }
                if (ri == 0) continue;
                int mites = rng.Next(1, 4);
                for (int k = 0; k < mites; k++)
                {
                    float px = rm.xMin + 1.5f + (float)rng.NextDouble() * (rm.width - 3f);
                    float pz = rm.yMin + 1.5f + (float)rng.NextDouble() * (rm.height - 3f);
                    var wp = new Vector3(Origin.x + px * Cell, FloorY, Origin.y + pz * Cell);
                    float s = 0.8f + (float)rng.NextDouble() * 0.8f;
                    mb.Cone(Matrix4x4.TRS(wp + Vector3.up * s * 1.6f, Quaternion.identity, new Vector3(s * 1.4f, s * 3.2f, s * 1.4f)), e.rock, 7);
                    mb.Rock(Matrix4x4.TRS(wp + Vector3.up * 0.2f, Quaternion.identity, new Vector3(s * 1.8f, s * 0.7f, s * 1.8f)), e.rock, e.rockTop, (uint)(k + ri * 7), 6, 4);
                    r.pillars.Add(new Vector4(wp.x, wp.z, s * 0.8f, 0));
                }
            }
            if (e.kind == CaveKind.Ruin) BuildTraps(r, rng, mb);
            var deco = new GameObject("Decor");
            deco.transform.SetParent(r.root.transform, false);
            deco.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cave_decor");
            var dr = deco.AddComponent<MeshRenderer>();
            dr.sharedMaterial = RenderAssets.Creature;
            dr.shadowCastingMode = ShadowCastingMode.Off;

            BuildPortal(r.exitPos, new Color(0.75f, 0.95f, 1f));

            // minimap texture: floor cells
            r.map = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "cave_map" };
            var px2 = new Color32[N * N];
            Color32 fl = Color.Lerp(e.floor, Color.white, 0.35f), bg = new Color(0.03f, 0.03f, 0.05f, 1f);
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) px2[j * N + i] = r.floor[i, j] ? fl : bg;
            var last = r.rooms[r.rooms.Count - 1];
            for (int j = last.yMin; j < last.yMax; j++) for (int i = last.xMin; i < last.xMax; i++) if (r.floor[i, j]) px2[j * N + i] = new Color(0.75f, 0.35f, 0.3f, 1f);
            r.map.SetPixels32(px2);
            r.map.Apply(false, false);
        }

        static Mesh spikeMesh;

        /// <summary>Spike plates in a ruin's corridors: one cell wide in a two-cell passage, so there is always a way round.</summary>
        void BuildTraps(Run r, System.Random rng, MeshBuilder mb)
        {
            if (spikeMesh == null)
            {
                var sb = new MeshBuilder();
                Color32 iron = new Color(0.62f, 0.64f, 0.7f), ironTip = new Color(0.9f, 0.92f, 1f);
                for (int x = 0; x < 4; x++)
                for (int z = 0; z < 4; z++)
                    sb.Cylinder(Matrix4x4.TRS(new Vector3(-1.2f + x * 0.8f, 0.55f, -1.2f + z * 0.8f), Quaternion.identity, new Vector3(0.24f, 1.1f, 0.24f)), iron, ironTip, 5, 0f, true);
                spikeMesh = sb.ToMesh("cave_spikes");
            }
            var cells = new List<Vector2Int>();
            for (int i = 1; i < N - 1; i++)
            for (int j = 1; j < N - 1; j++)
            {
                if (!r.floor[i, j]) continue;
                bool inRoom = false;
                foreach (var rm in r.rooms) if (i >= rm.xMin - 1 && i < rm.xMax + 1 && j >= rm.yMin - 1 && j < rm.yMax + 1) { inRoom = true; break; }
                if (!inRoom) cells.Add(new Vector2Int(i, j));
            }
            for (int i = cells.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = cells[i]; cells[i] = cells[j]; cells[j] = t; }
            var used = new List<Vector2Int>();
            Color32 plate = new Color(0.1f, 0.09f, 0.11f), rim = Color.Lerp(r.e.glow, Color.black, 0.45f);
            foreach (var c in cells)
            {
                if (used.Count >= 9) break;
                bool close = false;
                foreach (var u in used) if (Mathf.Abs(u.x - c.x) + Mathf.Abs(u.y - c.y) < 3) { close = true; break; }
                if (close) continue;
                used.Add(c);
                var p = CellCenter(c.x, c.y);
                mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.03f, Quaternion.identity, new Vector3(3.5f, 0.06f, 3.5f)), plate);
                mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.04f, Quaternion.identity, new Vector3(3.8f, 0.04f, 0.16f) ), rim);
                mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.04f, Quaternion.Euler(0, 90f, 0), new Vector3(3.8f, 0.04f, 0.16f)), rim);
                var go = new GameObject("Spikes");
                go.transform.SetParent(r.root.transform, false);
                go.transform.position = p + Vector3.down * 1.15f;
                go.AddComponent<MeshFilter>().sharedMesh = spikeMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = RenderAssets.Creature;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                r.traps.Add(new Trap { pos = p, spikes = go.transform, phase = (float)rng.NextDouble() * 3.4f });
            }
        }

        static bool IsFloorRaw(Run r, int i, int j) => i >= 0 && j >= 0 && i < N && j < N && r.floor[i, j];

        static void Quad(List<Vector3> v, List<Vector3> n, List<Color32> c, List<int> t, Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector3 normal, Color lo, Color hi)
        {
            int k = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int i = 0; i < 4; i++) n.Add(normal);
            c.Add(lo); c.Add(hi); c.Add(hi); c.Add(lo);
            t.Add(k); t.Add(k + 1); t.Add(k + 2);
            t.Add(k); t.Add(k + 2); t.Add(k + 3);
        }

        /// <summary>Two-row rock wall from a to b (left to right as seen from inside), with hashed jitter shared by neighbours.</summary>
        static void Wall(List<Vector3> v, List<Vector3> n, List<Color32> c, List<int> t, Vector3 a, Vector3 b, Vector3 inward, Entrance e)
        {
            if (e.kind == CaveKind.Ruin)
            {
                // straight courses of dressed stone: a pale footing, alternating bands, a glowing line, dark above
                float[] hs = { 0f, 0.7f, 2.3f, 3.9f, 4.15f, 6.4f, WallH };
                for (int row = 0; row < 6; row++)
                {
                    Color cc = row == 0 ? Color.Lerp(e.rock, Color.white, 0.22f) : row == 3 ? Color.Lerp(e.glow, e.rock, 0.35f)
                             : row == 5 ? e.rockTop * 0.6f : row == 4 ? Color.Lerp(e.rock, e.rockTop, 0.6f) : (row == 1 ? e.rock : e.rock * 0.88f);
                    int k = v.Count;
                    v.Add(new Vector3(a.x, FloorY + hs[row], a.z)); v.Add(new Vector3(a.x, FloorY + hs[row + 1], a.z));
                    v.Add(new Vector3(b.x, FloorY + hs[row + 1], b.z)); v.Add(new Vector3(b.x, FloorY + hs[row], b.z));
                    for (int i = 0; i < 4; i++) { n.Add(inward); c.Add(cc); }
                    t.Add(k); t.Add(k + 2); t.Add(k + 1);
                    t.Add(k); t.Add(k + 3); t.Add(k + 2);
                }
                return;
            }
            float[] ys = { FloorY, FloorY + WallH * 0.45f, FloorY + WallH };
            for (int row = 0; row < 2; row++)
            {
                Vector3 a0 = Jitter(a, ys[row], row), b0 = Jitter(b, ys[row], row);
                Vector3 a1 = Jitter(a, ys[row + 1], row + 1), b1 = Jitter(b, ys[row + 1], row + 1);
                Color lo = Color.Lerp(e.rock, e.rockTop, row * 0.5f), hi = Color.Lerp(e.rock, e.rockTop, (row + 1) * 0.5f) * (row == 1 ? 0.55f : 1f);
                int k = v.Count;
                v.Add(a0); v.Add(a1); v.Add(b1); v.Add(b0);
                for (int i = 0; i < 4; i++) n.Add(inward);
                c.Add(lo); c.Add(hi); c.Add(hi); c.Add(lo);
                // front faces point into the room (backs are culled, so the camera can see in from behind a wall)
                t.Add(k); t.Add(k + 2); t.Add(k + 1);
                t.Add(k); t.Add(k + 3); t.Add(k + 2);
            }
        }

        static Vector3 Jitter(Vector3 p, float y, int row)
        {
            if (row == 0) return new Vector3(p.x, y, p.z);
            int hx = Mathf.RoundToInt(p.x * 10f), hz = Mathf.RoundToInt(p.z * 10f);
            float jx = (Hash.F(hx, hz, 11 + row) - 0.5f) * 0.9f, jz = (Hash.F(hx, hz, 23 + row) - 0.5f) * 0.9f;
            return new Vector3(p.x + jx, y + (Hash.F(hx, hz, 31 + row) - 0.5f) * 0.8f, p.z + jz);
        }

        void BuildPortal(Vector3 p, Color col)
        {
            var mb = new MeshBuilder();
            Color32 c = col, rim = new Color(0.55f, 0.55f, 0.6f);
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f * Mathf.Deg2Rad;
                var q = new Vector3(Mathf.Cos(a) * 1.3f, 1.5f + Mathf.Sin(a) * 1.3f, 0f);
                mb.Box(Matrix4x4.TRS(q, Quaternion.Euler(0, 0, i * 22.5f), new Vector3(0.28f, 0.55f, 0.28f)), i % 4 == 0 ? c : rim);
            }
            mb.Sphere(Matrix4x4.TRS(new Vector3(0, 1.5f, 0), Quaternion.identity, new Vector3(2.1f, 2.1f, 0.15f)), c, 12, 8);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.08f, 0), Quaternion.identity, new Vector3(2.6f, 0.16f, 2.6f)), rim, 12, 1f, true);
            var go = new GameObject("Portal");
            go.transform.SetParent(run.root.transform, false);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0, 90f, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cave_portal");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            Fx.Sparkle(p + Vector3.up * 1.5f, col, 20, 1f);
        }

        // ================================================================ CONTENTS
        void SpawnContents(Run r, System.Random rng)
        {
            var e = r.e;
            var pool = new List<SpeciesDef>();
            var weights = new List<float>();
            foreach (var s in ContentDB.SpeciesList)
            {
                if (s.starter || !s.LivesIn(e.biome) || s.rarity >= Rarity.Epic) continue;
                float w = 1f;
                if (s.element1 == Element.Stone || s.element1 == Element.Shade || s.element2 == Element.Stone || s.element2 == Element.Shade) w = 2.5f;
                pool.Add(s); weights.Add(w);
            }
            float total = 0f; foreach (var w in weights) total += w;
            SpeciesDef Pick()
            {
                float x = (float)rng.NextDouble() * total;
                for (int i = 0; i < pool.Count; i++) { x -= weights[i]; if (x <= 0f) return pool[i]; }
                return pool[pool.Count - 1];
            }

            SpeciesDef PickMax(Rarity max)
            {
                for (int i = 0; i < 24; i++) { var s = Pick(); if (s.rarity <= max) return s; }
                return Pick();
            }

            int last = r.rooms.Count - 1;
            int ambush = e.kind == CaveKind.Ruin && r.rooms.Count >= 5 ? r.rooms.Count / 2 : -1;
            for (int ri = 1; ri < last; ri++)
            {
                var rm = r.rooms[ri];
                if (ri == ambush)
                {
                    // looks empty, with a chest in plain sight
                    r.ambushPos = RoomCenter(rm);
                    r.ambushSp = PickMax(Rarity.Uncommon);
                    r.ambushSet = true;
                    if (!r.quiet) AddChest(r, RoomEdgeSpot(rm, rng), false);
                    continue;
                }
                int groups = rng.NextDouble() < 0.35 ? 2 : 1;
                for (int g = 0; g < groups; g++)
                {
                    var sp = Pick();
                    int n = rng.NextDouble() < 0.6 ? 1 : 2;
                    var c = new Vector3(Origin.x + (rm.xMin + 1.5f + (float)rng.NextDouble() * (rm.width - 3f)) * Cell, FloorY,
                                        Origin.y + (rm.yMin + 1.5f + (float)rng.NextDouble() * (rm.height - 3f)) * Cell);
                    for (int k = 0; k < n; k++)
                    {
                        var d = CreatureFactory.RollWildMaybeEvolved(sp, rng.Next(e.minLv, e.maxLv + 1), e.biome, false);
                        var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, c + new Vector3(k * 1.8f, 0f, k * 1.2f));
                        a.Persistent = true;
                        r.kin.Add(a);
                    }
                }
                // treasure along the way
                if (!r.quiet && rng.NextDouble() < 0.6) AddChest(r, RoomEdgeSpot(rm, rng), false);
            }

            // guardian + sealed chest in the last chamber
            var br = r.rooms[last];
            var bc = RoomCenter(br);
            if (r.quiet) { r.bossDown = true; return; }
            if (e.kind == CaveKind.Burrow)
            {
                // a pack: one big leader (not an alpha) and two of its kind
                var lsp = PickMax(Rarity.Uncommon);
                var ld = CreatureFactory.RollWild(lsp, e.maxLv + 2, e.biome, false);
                var leader = CreatureSpawner.I.SpawnActor(ld, CreatureMode.Wild, bc);
                leader.Persistent = true;
                leader.Toughen(1.6f);
                r.boss = leader;
                r.bossData = ld;
                for (int k = -1; k <= 1; k += 2)
                {
                    var d = CreatureFactory.RollWildMaybeEvolved(lsp, rng.Next(e.minLv, e.maxLv + 1), e.biome, false);
                    var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, bc + new Vector3(k * 3.2f, 0f, -2f));
                    a.Persistent = true;
                    r.kin.Add(a);
                }
            }
            else
            {
                var bsp = ContentDB.Sp(e.boss);
                if (bsp == null)
                {
                    // no named guardian: a Rare of this land
                    var rares = pool.FindAll(x => x.rarity == Rarity.Rare);
                    bsp = rares.Count > 0 ? rares[rng.Next(rares.Count)] : Pick();
                }
                var bd = CreatureFactory.RollWild(bsp, e.maxLv + 3, e.biome, true);
                var boss = CreatureSpawner.I.SpawnActor(bd, CreatureMode.Wild, bc);
                boss.Persistent = true;
                r.boss = boss;
                r.bossData = bd;
                if (e.kind == CaveKind.Ruin)
                {
                    // vault guards flank it
                    for (int k = -1; k <= 1; k += 2)
                    {
                        var d = CreatureFactory.RollWild(PickMax(Rarity.Uncommon), e.maxLv, e.biome, false);
                        var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, bc + new Vector3(k * 6f, 0f, -3f));
                        a.Persistent = true;
                        r.kin.Add(a);
                    }
                }
            }
            AddChest(r, bc + new Vector3(0f, 0f, br.height * Cell * 0.32f), true);
        }

        static Vector3 RoomEdgeSpot(RectInt rm, System.Random rng)
        {
            bool alongX = rng.NextDouble() < 0.5;
            float cx = alongX ? rm.xMin + 1.5f + (float)rng.NextDouble() * (rm.width - 3f) : (rng.NextDouble() < 0.5 ? rm.xMin + 0.9f : rm.xMax - 0.9f);
            float cz = alongX ? (rng.NextDouble() < 0.5 ? rm.yMin + 0.9f : rm.yMax - 0.9f) : rm.yMin + 1.5f + (float)rng.NextDouble() * (rm.height - 3f);
            return new Vector3(Origin.x + cx * Cell, FloorY, Origin.y + cz * Cell);
        }

        void AddChest(Run r, Vector3 p, bool big)
        {
            float s = big ? 1.5f : 1f;
            var mb = new MeshBuilder();
            Color32 wood = new Color(0.45f, 0.3f, 0.18f), band = big ? new Color(1f, 0.82f, 0.35f) : new Color(0.7f, 0.7f, 0.75f);
            mb.Box(Matrix4x4.TRS(new Vector3(0, 0.3f * s, 0), Quaternion.identity, new Vector3(1.1f * s, 0.6f * s, 0.75f * s)), wood, new Color(0.55f, 0.38f, 0.22f));
            mb.Box(Matrix4x4.TRS(new Vector3(0, 0.3f * s, 0.38f * s), Quaternion.identity, new Vector3(0.18f * s, 0.25f * s, 0.04f)), band);
            var go = new GameObject(big ? "BossChest" : "Chest");
            go.transform.SetParent(r.root.transform, false);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, (float)(p.x * 13.7 % 360.0), 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("chest");
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            // lid pivots at the back edge
            var lmb = new MeshBuilder();
            lmb.Box(Matrix4x4.TRS(new Vector3(0, 0.1f * s, 0.375f * s), Quaternion.identity, new Vector3(1.14f * s, 0.22f * s, 0.78f * s)), new Color(0.5f, 0.34f, 0.2f), band);
            var lid = new GameObject("Lid");
            lid.transform.SetParent(go.transform, false);
            lid.transform.localPosition = new Vector3(0, 0.6f * s, -0.375f * s);
            lid.AddComponent<MeshFilter>().sharedMesh = lmb.ToMesh("chest_lid");
            lid.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            r.chests.Add(new Chest { pos = p, go = go, lid = lid.transform, big = big });
            r.pillars.Add(new Vector4(p.x, p.z, 0.75f * s, 0));
        }

        void OpenChest(Chest c)
        {
            if (c.opened) return;
            c.opened = true;
            var gm = GameManager.I;
            var e = run.e;
            bool den = e.kind == CaveKind.Burrow;
            int mul = c.big && !den ? 2 : 1;
            var drop = c.pos + Vector3.up;
            int glim = 0;
            // materials by land
            switch (e.biome)
            {
                case BiomeId.Meadow:
                    gm.GiveItem("ore", Random.Range(3, 7) * mul, drop);
                    gm.GiveItem(Random.value < 0.5f ? "gel" : "pelt", Random.Range(2, 4) * mul, drop);
                    gm.GiveItem("tether_seed", Random.Range(3, 6) * mul, drop);
                    if (Random.value < 0.4f * mul) gm.GiveItem("ingot", Random.Range(1, 3), drop);
                    glim = 10;
                    break;
                case BiomeId.Badlands:
                    gm.GiveItem("ingot", Random.Range(2, 4) * mul, drop);
                    gm.GiveItem("cinder", Random.Range(2, 5) * mul, drop);
                    gm.GiveItem("ironbark_seed", Random.Range(2, 4) * mul, drop);
                    glim = 20;
                    break;
                case BiomeId.Peaks:
                    gm.GiveItem("crystal", Random.Range(2, 5) * mul, drop);
                    gm.GiveItem("ingot", Random.Range(2, 4) * mul, drop);
                    gm.GiveItem(Random.value < 0.6f ? "ironbark_seed" : "starbloom_seed", Random.Range(1, 3) * mul, drop);
                    glim = 30;
                    break;
                case BiomeId.Isles:
                    gm.GiveItem("saltglass", Random.Range(2, 5) * mul, drop);
                    gm.GiveItem("ingot", Random.Range(1, 3) * mul, drop);
                    gm.GiveItem(Random.value < 0.5f ? "tether_seed" : "ironbark_seed", Random.Range(2, 5) * mul, drop);
                    glim = 20;
                    break;
                case BiomeId.Swamp:
                    gm.GiveItem("bog_iron", Random.Range(2, 5) * mul, drop);
                    gm.GiveItem("ironbark_seed", Random.Range(2, 4) * mul, drop);
                    glim = 30;
                    break;
                case BiomeId.Desert:
                    gm.GiveItem("sunsteel_ore", Random.Range(2, 5) * mul, drop);
                    gm.GiveItem(Random.value < 0.6f ? "ironbark_seed" : "starbloom_seed", Random.Range(2, 4) * mul, drop);
                    glim = 45;
                    break;
                case BiomeId.Blight:
                    gm.GiveItem("blight_ore", Random.Range(2, 5) * mul, drop);
                    gm.GiveItem("starbloom_seed", Random.Range(1, 3) * mul, drop);
                    glim = 60;
                    break;
                default:
                    gm.GiveItem("starcrystal", Random.Range(2, 4) * mul, drop);
                    gm.GiveItem("starbloom_seed", Random.Range(2, 4) * mul, drop);
                    if (c.big) gm.GiveItem("prism_shard", Random.Range(2, 5), drop);
                    glim = 80;
                    break;
            }
            if (Random.value < 0.35f) gm.GiveItem("salve", Random.Range(1, 3), drop);
            glim = Mathf.RoundToInt(glim * (c.big ? (den ? 1.5f : 3f) : 1f) * Random.Range(0.8f, 1.25f));
            gm.Player.Stats.glimmer += glim;
            GameEvents.Say($"+{glim} Glimmer", new Color(1f, 0.85f, 0.4f));
            // gear: guaranteed from a guardian's chest, sometimes from the others
            if ((c.big && !den) || Random.value < (c.big ? 0.4f : 0.2f))
            {
                string g = GearForTier(e.biome);
                if (g != null) { gm.GiveItem(g, 1, drop); GameEvents.Good($"Found gear: {ContentDB.ItemName(g)}!"); }
            }
            gm.AddPlayerXp(c.big ? 80 : 25);
            Fx.Burst(drop, c.big ? new Color(1f, 0.85f, 0.35f) : new Color(0.9f, 0.9f, 1f), c.big ? 30 : 16, 5f, 0.3f, 0.7f, true);
            SynthAudio.Play(Sfx.CaptureSuccess, c.big ? 0.9f : 1.2f);
            GameEvents.RaiseInventoryChanged();
        }

        static string GearForTier(BiomeId b)
        {
            // the outer lands' own armour is forged from their ore (camp tech), so their chests hold older sets and trinkets
            string[] pool = b == BiomeId.Meadow
                ? new[] { "explorer_hood", "explorer_coat", "explorer_boots", "gatherers_bracelet", "binders_bracelet", "hearty_amulet", "mercy_charm" }
                : b == BiomeId.Badlands || b == BiomeId.Isles
                    ? new[] { "ironbark_helm", "ironbark_mail", "ironbark_greaves", "guard_ring", "windrunner_ring", "keen_ring", "thornweave_vest" }
                    : b == BiomeId.Peaks || b == BiomeId.Swamp
                        ? new[] { "rime_crown", "rimecloak", "rime_treads", "ember_helm", "ember_plate", "ember_greaves", "glowstone_pendant" }
                        : new[] { "guard_ring", "windrunner_ring", "keen_ring", "glowstone_pendant", "mercy_charm", "hearty_amulet", "binders_bracelet", "thornweave_vest" };
            var ok = new List<string>();
            foreach (var id in pool) if (ContentDB.Item(id) != null) ok.Add(id);
            return ok.Count > 0 ? ok[Random.Range(0, ok.Count)] : null;
        }

        // ================================================================ COLLISION
        /// <summary>Pushes a circle out of cave walls, stalagmites and chests (called from WorldCollision.PushOut).</summary>
        public static bool PushOut(ref Vector3 p, float r)
        {
            if (I == null || I.run == null || !InZone(p.x, p.z)) return false;
            var run = I.run;
            bool hit = false;
            int ci = Mathf.FloorToInt((p.x - Origin.x) / Cell), cj = Mathf.FloorToInt((p.z - Origin.y) / Cell);
            for (int di = -1; di <= 1; di++)
            for (int dj = -1; dj <= 1; dj++)
            {
                int i = ci + di, j = cj + dj;
                if (IsFloorRaw(run, i, j)) continue;
                float x0 = Origin.x + i * Cell, z0 = Origin.y + j * Cell;
                float qx = Mathf.Clamp(p.x, x0, x0 + Cell), qz = Mathf.Clamp(p.z, z0, z0 + Cell);
                float dx = p.x - qx, dz = p.z - qz;
                float d2 = dx * dx + dz * dz;
                if (d2 >= r * r) continue;
                if (d2 < 1e-6f)
                {
                    // inside a wall cell: shove toward this cell's open neighbour (or the cell we came from)
                    var c = CellCenter(ci, cj);
                    dx = p.x - (x0 + Cell * 0.5f); dz = p.z - (z0 + Cell * 0.5f);
                    if (dx * dx + dz * dz < 1e-6f) { dx = c.x - p.x; dz = c.z - p.z; }
                    d2 = dx * dx + dz * dz;
                }
                float d = Mathf.Sqrt(d2);
                p.x += dx / d * (r - Mathf.Min(d, r));
                p.z += dz / d * (r - Mathf.Min(d, r));
                hit = true;
            }
            foreach (var pl in run.pillars)
            {
                float dx = p.x - pl.x, dz = p.z - pl.y, rr = r + pl.z;
                float d2 = dx * dx + dz * dz;
                if (d2 >= rr * rr || d2 < 1e-6f) continue;
                float d = Mathf.Sqrt(d2);
                p.x += dx / d * (rr - d);
                p.z += dz / d * (rr - d);
                hit = true;
            }
            return hit;
        }

        /// <summary>True if a point is inside cave rock (projectiles stop there).</summary>
        public static bool Solid(Vector3 p)
        {
            if (I == null || I.run == null || !InZone(p.x, p.z)) return false;
            int i = Mathf.FloorToInt((p.x - Origin.x) / Cell), j = Mathf.FloorToInt((p.z - Origin.y) / Cell);
            return !IsFloorRaw(I.run, i, j);
        }
    }

    /// <summary>Cave: natural, a guardian at the bottom. Burrow: a short den with a pack leader. Ruin: built halls with traps, an ambush and vault guards.</summary>
    public enum CaveKind { Cave, Burrow, Ruin }

    [System.Serializable]
    public class CaveRecord
    {
        public string id;
        public double clearedAt = -1e9;
    }
}
