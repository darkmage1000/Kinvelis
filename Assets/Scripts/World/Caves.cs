using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Caves (dungeons). The overworld is a heightfield, so caves live in a reserved zone in the far ocean corner,
    /// high above the sea (FloorY), and are generated when entered (the same cave every time: seeded by the world and
    /// its id). This file: the 19 entrances and their mouths, discovery, entering and leaving, the run's state and
    /// prompts. The rest: `Caves.Layout` (tiers, chamber graph, rock field, collision), `Caves.Mesh` (rock faces, floor,
    /// ceiling, decor, baked light, minimap), `Caves.Life` (kin, guardians, eggs, veins, chests, loot) and
    /// `Caves.Hazards`. Leave by the mouth portal or the guardian's portal; dying or loading puts you back outside.
    /// </summary>
    public partial class Caves : MonoBehaviour
    {
        public static Caves I { get; private set; }
        public static bool InCave => I != null && I.run != null;

        public const float FloorY = 420f;
        static readonly Vector2 Origin = new Vector2(1832f, -2026f);    // ocean corner, inside the movement clamps; ZoneSize (184 m) square
        const double RefreshSeconds = 20 * 60;                          // guardians, chests, eggs and veins return after 20 min

        public static bool InZone(float x, float z) =>
            x >= Origin.x - 3f && x <= Origin.x + ZoneSize + 3f && z >= Origin.y - 3f && z <= Origin.y + ZoneSize + 3f;

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
        class Run
        {
            public Entrance e;
            public CaveTier tier;
            public int seed;
            public readonly List<Room> rooms = new List<Room>();
            public readonly List<Tunnel> tunnels = new List<Tunnel>();
            public float[] F, C, Flo;
            public bool[] hall;
            public List<Vector4> segs;
            public List<Vector2> segNormals;
            public List<int>[] buckets;
            public GameObject root;
            public readonly List<CreatureActor> kin = new List<CreatureActor>();
            public readonly List<Guardian> guards = new List<Guardian>();
            public readonly List<Watch> watches = new List<Watch>();
            public readonly List<Chest> chests = new List<Chest>();
            public readonly List<EggNest> nests = new List<EggNest>();
            public readonly List<ResourceNode> nodes = new List<ResourceNode>();
            public readonly List<GameObject> nodeGo = new List<GameObject>();
            public readonly List<int> nodePillar = new List<int>();
            public readonly List<Hazard> hazards = new List<Hazard>();
            public readonly List<Vector4> pillars = new List<Vector4>();   // x, z, radius
            public Vector3 startPos, exitPos;
            public Vector3? bossPortal;
            public Vector4 shaft;   // x, z, roof height of the mouth chamber's daylight crack (w unused)
            public bool quiet, cleared;
            public Texture2D map;
            public readonly List<Trap> traps = new List<Trap>();
            public Vector3 ambushPos;
            public SpeciesDef ambushSp;
            public bool ambushSet, ambushDone;
            // two-level caves, secrets, puzzles, the explored map (Caves.Secrets)
            public int level, levels = 1;
            public HashSet<string> done = new HashSet<string>();   // what has been taken on every level this visit
            public List<Lamp> lamps;
            public bool hasDescent;
            public Vector3 descentPos, descentArrive;
            public readonly List<Secret> secrets = new List<Secret>();
            public readonly List<Puzzle> puzzles = new List<Puzzle>();
            public bool[] seen, hidden;
            public Color32[] mapBase, mapPx;
            public float revealTimer;
            public bool checkpointTold;
            public Dictionary<int, bool[]> seenAll;   // the explored map of each level, kept while you go up and down
        }

        /// <summary>Deep caves have a second level below the first (a chasm and a rope); the others are one level.</summary>
        public static int LevelsOf(Entrance e) => TierOf(e) == CaveTier.End ? 2 : 1;
        /// <summary>A key for something taken on this level (chest, egg, guardian, vein, wall, puzzle), kept while you switch levels.</summary>
        static string Key(Run r, string what, int i = 0) => r.level + ":" + what + i;

        class Trap { public Vector3 pos; public Transform spikes; public float phase, cool; public bool wasUp; }

        Run run;
        bool busy, bossBar;
        public string CurrentName => run?.e.name;
        public Texture2D MapTexture => run?.map;
        /// <summary>Where to put the player if the game saves while underground.</summary>
        public Vector3 SafeOutside => run != null ? Outside(run.e) : GameManager.I.Player.Position;
        static Vector3 Outside(Entrance e) { var p = e.pos + e.outward * 3.5f; p.y = WorldGen.Height(p.x, p.z); return p; }

        public static Vector2 ZoneUV(Vector3 p) => new Vector2((p.x - Origin.x) / ZoneSize, (p.z - Origin.y) / ZoneSize);

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
                        var t = TierOf(e);
                        string hint = t == CaveTier.End ? "Deep and dangerous: two alphas, two mini-bosses, rare kin and hazards."
                                    : t == CaveTier.Mid ? "Side passages hide eggs and a mini-boss; an alpha guards the end."
                                    : "A short one - a single boss guards a little treasure.";
                        GameEvents.Good($"Discovered a {e.Word}: {e.name} (Lv {e.minLv}-{e.maxLv}). {hint}");
                        SynthAudio.Play(Sfx.UiOpen, 0.8f);
                    }
                }
                return;
            }
            var r = run;
            TickTraps(r, dt, pl);
            TickSpecial(r, dt, pl);
            if (r.ambushSet && !r.ambushDone && pl.IsAlive && Flat2(r.ambushPos, pl.Position) < 7f * 7f) SpringAmbush(r, pl);
            TickWatches(r, pl);
            TickGuardians(r, pl);
            TickHazards(r, dt, pl);
            foreach (var c in r.chests)
                if (c.opened && c.lid != null) c.lid.localRotation = Quaternion.Slerp(c.lid.localRotation, Quaternion.Euler(-100f, 0, 0), dt * 6f);
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
                float y = up ? 0f : c > 1.6f ? -0.62f : -1f;
                var lp = t.spikes.localPosition;
                lp.y = Mathf.MoveTowards(lp.y, t.pos.y + y, dt * (up ? 14f : 4f));
                t.spikes.localPosition = lp;
                if (up && !t.wasUp && Flat2(t.pos, pp) < 16f * 16f) SynthAudio.Play(Sfx.Clunk, 1.5f);
                t.wasUp = up;
                t.cool -= dt;
                if (!up || t.cool > 0f || !pl.IsAlive) continue;
                if (Mathf.Abs(pp.x - t.pos.x) < 1.2f && Mathf.Abs(pp.z - t.pos.z) < 1.2f && pp.y - t.pos.y < 1.1f)
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
                var p = Nudge(r, r.ambushPos + new Vector3(Mathf.Cos(a) * 5.5f, 0f, Mathf.Sin(a) * 5.5f));
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

        CaveRecord Record(string id)
        {
            var st = GameManager.I.Player.Stats;
            var rec = st.caveRecords.Find(x => x.id == id);
            if (rec == null) { rec = new CaveRecord { id = id, clearedAt = -1e9 }; st.caveRecords.Add(rec); }
            return rec;
        }

        /// <summary>Interaction under the player (ENTER / LEAVE / OPEN / SEALED / TAKE EGG). Returns null when nothing is close.</summary>
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
            if (run.level > 0 && Flat2(run.exitPos, p) < 3f * 3f)
            {
                act = () => ChangeLevel(run.level - 1);
                return "CLIMB UP";
            }
            if ((run.level == 0 && Flat2(run.exitPos, p) < 3.2f * 3.2f) || (run.bossPortal.HasValue && Flat2(run.bossPortal.Value, p) < 3.2f * 3.2f))
            {
                act = Leave;
                return "LEAVE";
            }
            if (run.hasDescent && Flat2(run.descentPos, p) < 4.2f * 4.2f)
            {
                act = () => ChangeLevel(run.level + 1);
                return "CLIMB DOWN";
            }
            var special = SpecialPrompt(run, p, out act);
            if (special != null) return special;
            foreach (var c in run.chests)
            {
                if (c.opened || Flat2(c.pos, p) > 2.6f * 2.6f) continue;
                if (c.sealedUntilDown)
                {
                    string who = c.guard != null && c.guard.a != null ? c.guard.a.Sp.displayName : "guardian";
                    act = () => GameEvents.Say($"Sealed - the {who} that guards it has to fall first.");
                    return "SEALED";
                }
                var cc = c;
                act = () => OpenChest(cc);
                return "OPEN";
            }
            foreach (var n in run.nests)
            {
                if (n.taken || Flat2(n.pos, p) > 2.3f * 2.3f) continue;
                var nn = n;
                act = () => TakeEgg(nn);
                return n.rare ? "TAKE RARE EGG" : "TAKE EGG";
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

            var rec = Record(e.id);
            BuildRun(e, 0, new HashSet<string>(), GameManager.WorldTime - rec.clearedAt < RefreshSeconds);
            var mouth = run.rooms[0];
            var look = run.rooms.Count > 1 ? run.rooms[1].Center : mouth.Center + Vector3.right * 5f;
            pl.Teleport(run.startPos, YawToward(run.startPos, look));
            gm.CameraRig.Init(gm.CameraRig.Cam, pl.Position, pl.transform.eulerAngles.y);
            var comp = gm.Party.Companion;
            if (comp != null && comp.gameObject.activeSelf) { var cp = Nudge(run, run.startPos + pl.Right * 1.8f); comp.transform.position = cp; comp.Home = cp; }
            EnvironmentSystem.I?.SetIndoor(true, e.glow);
            yield return null;
            yield return Fade(false, 0.5f);

            GameEvents.Good($"{e.name} - Lv {e.minLv}-{e.maxLv}");
            if (run.quiet)
            {
                double left = RefreshSeconds - (GameManager.WorldTime - rec.clearedAt);
                GameEvents.Say($"It is quiet in here - the guardians, their treasure, the eggs and the veins return in ~{Mathf.CeilToInt((float)(left / 60.0))} min.", new Color(0.8f, 0.85f, 1f));
            }
            else
            {
                string word = e.Word;
                string say = run.tier == CaveTier.End
                    ? $"A deep {word}. Loose roofs, vents and pools; rare kin; geode veins. Two mini-bosses and two alphas hold its treasure."
                    : run.tier == CaveTier.Mid
                        ? $"A winding {word}. Its side passages hide a nest of eggs and a mini-boss on a blueprint; an alpha guards the deepest chamber."
                        : $"A shallow {word}. One big kin lives at the back with a little treasure.";
                if (e.kind == CaveKind.Ruin) say += " Old halls: spikes rise from the dark plates - cross when they drop.";
                if (run.levels > 1) say += " A chasm leads down to a second level - a checkpoint waits at the foot of the rope.";
                GameEvents.Say(say, new Color(1f, 0.85f, 0.5f));
            }
            busy = false;
        }

        /// <summary>Generates one level of a cave and makes it the current run (the same level every time: seeded by the world, the cave and the level).</summary>
        Run BuildRun(Entrance e, int level, HashSet<string> done, bool quiet, Dictionary<int, bool[]> seenAll = null)
        {
            int seed = WorldGen.Seed * 31;
            foreach (char ch in e.id) seed = seed * 17 + ch;   // stable across runtimes
            var r = new Run { e = e, tier = TierOf(e), seed = seed & 0x7FFFFFF, level = level, levels = LevelsOf(e), done = done, quiet = quiet };
            if (level > 0) r.seed = (r.seed + level * 104729) & 0x7FFFFFF;
            r.seenAll = seenAll ?? new Dictionary<int, bool[]>();
            if (!r.seenAll.TryGetValue(level, out var seen)) { seen = new bool[N * N]; r.seenAll[level] = seen; }
            r.seen = seen;
            run = r;
            var rng = new System.Random(level == 0 ? seed : r.seed);
            PlanLayout(r, rng);
            BuildField(r);
            var mouth = r.rooms[0];
            // the way out (or the rope up) stands to one side of where you arrive, so the camera behind you looks into the cave, not at it
            r.exitPos = Nudge(r, new Vector3(mouth.c.x - mouth.r * 0.45f, 0f, mouth.c.y + mouth.r * 0.45f));
            if (Open(r.exitPos) < 1.5f) r.exitPos = Nudge(r, new Vector3(mouth.c.x - mouth.r * 0.45f, 0f, mouth.c.y - mouth.r * 0.45f));
            r.startPos = Nudge(r, new Vector3(mouth.c.x - mouth.r * 0.05f, 0f, mouth.c.y));
            BuildGeometry(r, rng);
            if (e.kind == CaveKind.Ruin) BuildTraps(r, rng);
            BuildShaft(r);
            BuildSpecial(r, rng);
            SpawnContents(r, rng);
            PlaceHazards(r, rng);
            ApplyDone(r);
            return r;
        }

        /// <summary>Down the chasm or up the rope: the level you leave is torn down, what you took on it is remembered.</summary>
        public void ChangeLevel(int to)
        {
            if (busy || run == null || to < 0 || to >= run.levels || to == run.level) return;
            StartCoroutine(LevelRoutine(to));
        }

        IEnumerator LevelRoutine(int to)
        {
            busy = true;
            var gm = GameManager.I;
            var pl = gm.Player;
            pl.CancelAim();
            if (pl.IsMounted) pl.Dismount(true);
            SynthAudio.Play(Sfx.Whoosh, 0.5f, 0.7f);
            yield return Fade(true, 0.45f);
            var old = run;
            bool down = to > old.level;
            TearDown(old);
            var r = BuildRun(old.e, to, old.done, old.quiet, old.seenAll);
            r.cleared = old.cleared || r.cleared;
            r.checkpointTold = old.checkpointTold;
            var at = down ? r.startPos : r.descentArrive;
            var look = down ? (r.rooms.Count > 1 ? r.rooms[1].Center : at + Vector3.right * 5f) : r.rooms[0].Center;
            pl.Teleport(at, YawToward(at, look));
            gm.CameraRig.Init(gm.CameraRig.Cam, pl.Position, pl.transform.eulerAngles.y);
            var comp = gm.Party.Companion;
            if (comp != null && comp.gameObject.activeSelf) { var cp = Nudge(r, at + pl.Right * 1.8f); comp.transform.position = cp; comp.Home = cp; }
            yield return null;
            yield return Fade(false, 0.5f);
            if (down)
            {
                GameEvents.Good($"{r.e.name} - the lower level");
                if (!r.checkpointTold)
                {
                    r.checkpointTold = true;
                    GameEvents.Say("A checkpoint: if you faint down here you wake at the foot of the rope, not outside. Climb the rope to go back up.", new Color(0.6f, 1f, 0.75f));
                }
            }
            else GameEvents.Say("Back on the upper level.", new Color(0.8f, 0.85f, 1f));
            busy = false;
        }

        /// <summary>
        /// Fainting on a lower level: back up at its checkpoint (the foot of the rope) instead of outside. Everything
        /// that was hunting you calms down and heals, so it's a fresh try - nothing you took comes back.
        /// </summary>
        public bool CheckpointRespawn(out Vector3 at)
        {
            at = default;
            if (run == null || run.level == 0) return false;
            foreach (var k in run.kin) if (k != null && k.gameObject.activeSelf && k.IsAlive && k.Mode == CreatureMode.Wild) { k.HealFull(); k.Migrate(k.Home); }
            foreach (var g in run.guards)
            {
                if (g.down || g.a == null || !g.a.gameObject.activeSelf || g.a.Data != g.d) continue;
                g.a.HealFull(); g.a.Migrate(g.home); g.woke = false;
            }
            foreach (var w in run.watches) w.woke = false;
            if (bossBar && HUD.I != null) { HUD.I.SetBoss(null, 0f, Color.white); bossBar = false; }
            at = run.startPos;
            return true;
        }

        /// <summary>Lost? Back to where this level starts (the mouth, or the foot of the rope), if nothing is hunting you.</summary>
        public void ReturnToStart()
        {
            if (busy || run == null) return;
            var pl = GameManager.I.Player;
            foreach (var k in run.kin)
                if (k != null && k.gameObject.activeSelf && k.IsAlive && k.Aggro && (k.Position - pl.Position).sqrMagnitude < 25f * 25f) { GameEvents.Warn("Not while something is hunting you."); return; }
            foreach (var g in run.guards)
                if (!g.down && g.woke && g.a != null && g.a.gameObject.activeSelf && (g.a.Position - pl.Position).sqrMagnitude < 30f * 30f) { GameEvents.Warn("Not while a guardian is after you."); return; }
            StartCoroutine(ReturnRoutine());
        }

        IEnumerator ReturnRoutine()
        {
            busy = true;
            var gm = GameManager.I;
            var pl = gm.Player;
            if (pl.IsMounted) pl.Dismount(true);
            yield return Fade(true, 0.35f);
            var r = run;
            var look = r.rooms.Count > 1 ? r.rooms[1].Center : r.startPos + Vector3.right * 5f;
            pl.Teleport(r.startPos, YawToward(r.startPos, look));
            gm.CameraRig.Init(gm.CameraRig.Cam, pl.Position, pl.transform.eulerAngles.y);
            var comp = gm.Party.Companion;
            if (comp != null && comp.gameObject.activeSelf) { var cp = Nudge(r, r.startPos + pl.Right * 1.8f); comp.transform.position = cp; comp.Home = cp; }
            yield return null;
            yield return Fade(false, 0.4f);
            GameEvents.Say(r.level > 0 ? "Back at the checkpoint by the rope." : "Back at the cave mouth.", new Color(0.8f, 0.85f, 1f));
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
            TearDown(r);
            EnvironmentSystem.I?.SetIndoor(false, Color.white);
        }

        /// <summary>Removes one level: its kin, guardians, shots, meshes and map.</summary>
        void TearDown(Run r)
        {
            foreach (var k in r.kin) if (k != null && k.gameObject.activeSelf && k.Mode == CreatureMode.Wild) k.Despawn();
            foreach (var g in r.guards) if (g.a != null && g.a.gameObject.activeSelf && g.a.Data == g.d) g.a.Despawn();
            CreatureSpawner.I.DespawnAllWild();
            ProjectileSystem.I.ClearAll();
            if (bossBar && HUD.I != null) { HUD.I.SetBoss(null, 0f, Color.white); bossBar = false; }
            if (r.root != null) Destroy(r.root);
            if (r.map != null) Destroy(r.map);
        }

        static float YawToward(Vector3 from, Vector3 to) { var d = to - from; return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }

        // ================================================================ PORTALS, DAYLIGHT, TRAPS
        void BuildPortal(Vector3 p, Color col)
        {
            var mb = new MeshBuilder();
            Color32 c = new Color(col.r, col.g, col.b, 0f), rim = new Color(0.55f, 0.55f, 0.6f);
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

        /// <summary>The mouth chamber's shaft of daylight: crossed soft quads from the roof crack to the floor.</summary>
        void BuildShaft(Run r)
        {
            if (r.shaft.z <= 0f) return;
            var verts = new List<Vector3>(); var cols = new List<Color>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            float top = r.shaft.z, bottom = FloorAt(r.shaft.x, r.shaft.y);
            Color hi = new Color(0.85f, 0.93f, 1f, 0.08f), lo = new Color(0.85f, 0.93f, 1f, 0.015f);
            for (int q = 0; q < 3; q++)
            {
                var side = Quaternion.Euler(0, q * 60f, 0) * Vector3.right;
                int b0 = verts.Count;
                verts.Add(-side * 0.9f + Vector3.up * top); verts.Add(side * 0.9f + Vector3.up * top);
                verts.Add(side * 1.7f + Vector3.up * bottom); verts.Add(-side * 1.7f + Vector3.up * bottom);
                cols.Add(hi); cols.Add(hi); cols.Add(lo); cols.Add(lo);
                for (int k = 0; k < 4; k++) uvs.Add(new Vector2(0.5f, 0.5f));
                tris.AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3, b0, b0 + 2, b0 + 1, b0, b0 + 3, b0 + 2 });
            }
            var m = new Mesh { name = "cave_shaft" };
            m.SetVertices(verts); m.SetColors(cols); m.SetUVs(0, uvs); m.SetTriangles(tris, 0); m.RecalculateBounds();
            var go = new GameObject("Daylight");
            go.transform.SetParent(r.root.transform, false);
            go.transform.position = new Vector3(r.shaft.x, 0f, r.shaft.y);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.ParticleAdd;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        static Mesh spikeMesh;

        /// <summary>Spike plates on a ruin's tunnels: narrower than the tunnel, so there is always a way round.</summary>
        void BuildTraps(Run r, System.Random rng)
        {
            if (spikeMesh == null)
            {
                var sb = new MeshBuilder();
                Color32 iron = new Color(0.62f, 0.64f, 0.7f), ironTip = new Color(0.9f, 0.92f, 1f);
                for (int x = 0; x < 3; x++)
                for (int z = 0; z < 3; z++)
                    sb.Cylinder(Matrix4x4.TRS(new Vector3(-0.75f + x * 0.75f, 0.5f, -0.75f + z * 0.75f), Quaternion.identity, new Vector3(0.22f, 1f, 0.22f)), iron, ironTip, 5, 0f, true);
                spikeMesh = sb.ToMesh("cave_spikes");
            }
            var mb = new MeshBuilder();
            Color32 plate = new Color(0.1f, 0.09f, 0.11f), rim = Color.Lerp(r.e.glow, Color.black, 0.45f);
            var used = new List<Vector3>();
            foreach (var t in r.tunnels)
                for (int k = 1; k < t.pts.Length - 1 && used.Count < 8; k++)
                {
                    if (rng.NextDouble() < 0.4) continue;
                    var q = t.pts[k];
                    // off to one side of the middle, so the other side stays clear
                    var dir = (t.pts[k + 1] - t.pts[k - 1]).normalized;
                    var side = new Vector2(-dir.y, dir.x) * (rng.NextDouble() < 0.5 ? 0.8f : -0.8f);
                    var p = new Vector3(q.x + side.x, 0f, q.y + side.y);
                    if (Sample(r.F, p.x, p.z) < 1.4f) continue;
                    bool close = false;
                    foreach (var u2 in used) if (Flat2(u2, p) < 6f * 6f) { close = true; break; }
                    if (close) continue;
                    p.y = FloorAt2(r, p.x, p.z);
                    used.Add(p);
                    mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.03f, Quaternion.identity, new Vector3(2.4f, 0.06f, 2.4f)), plate);
                    mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.04f, Quaternion.identity, new Vector3(2.6f, 0.04f, 0.14f)), rim);
                    mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.04f, Quaternion.Euler(0, 90f, 0), new Vector3(2.6f, 0.04f, 0.14f)), rim);
                    var go = new GameObject("Spikes");
                    go.transform.SetParent(r.root.transform, false);
                    go.transform.position = p + Vector3.down * 1f;
                    go.AddComponent<MeshFilter>().sharedMesh = spikeMesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = RenderAssets.Creature;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    r.traps.Add(new Trap { pos = p, spikes = go.transform, phase = (float)rng.NextDouble() * 3.4f });
                }
            if (mb.VertexCount > 0) Part(r, "TrapPlates", mb, RenderAssets.Creature);
        }
    }

    /// <summary>Cave: natural rock. Burrow: an earth den, low and rooty. Ruin: old built halls swallowed by the cave, with spike traps and an ambush.</summary>
    public enum CaveKind { Cave, Burrow, Ruin }

    [System.Serializable]
    public class CaveRecord
    {
        public string id;
        public double clearedAt = -1e9;
    }
}
