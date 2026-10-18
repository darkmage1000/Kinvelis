using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Cave pass 2 (2026-10-18): what makes a cave more than a row of fights.
    ///   Secret rooms - a side chamber walled off by cracked rock. Light seeps through the cracks and a draught gives
    ///     it away when you are close; SMASH WALL three times. Hidden on the map until it is open. A Secret chest inside.
    ///   Puzzles - a chamber with a barred gate to a vault. Plates: four rune plates, step on (or send your kin onto)
    ///     every one before the glow fades. Levers: four lamps over the gate, four levers, each lever flips two lamps
    ///     (its two gems show which) - light them all. A Vault chest behind the gate.
    ///   Two levels - deep caves end their first level at a chasm (CLIMB DOWN); the second starts at the foot of the
    ///     rope, which is the checkpoint: faint down there and you wake by the rope, not outside (`CheckpointRespawn`).
    ///     What you took on either level stays taken while you go up and down (`Run.done`).
    ///   The map - what you haven't been near is drawn dim, and the minimap / Map tab mark the way out, chests, eggs,
    ///     guardians, the chasm, the rope and puzzle gates (`MapMarks`). Map tab: BACK TO THE MOUTH / CHECKPOINT.
    /// </summary>
    public partial class Caves
    {
        class Secret
        {
            public int room, tunnel; public Vector3 pos; public Vector2 dir; public float halfW;
            public GameObject go; public readonly List<int> pillars = new List<int>();
            public int hp = 3; public bool open, hinted;
        }

        enum PuzzleKind { Plates, Levers }

        class Puzzle
        {
            public PuzzleKind kind; public int room, vault;
            public Vector3 gatePos; public Vector2 gateDir; public float halfW;
            public Transform gate; public readonly List<int> pillars = new List<int>();
            public readonly List<Vector3> spots = new List<Vector3>();         // plates or levers
            public readonly List<MeshFilter> marks = new List<MeshFilter>();   // plate rings or gate lamps
            public readonly List<Transform> handles = new List<Transform>();
            public readonly List<Quaternion> faces = new List<Quaternion>();
            public readonly List<bool> pulled = new List<bool>();
            public bool[] on; public float timer, sink, cool; public bool solved, told;
        }

        public const float PlateWindow = 15f, PlateWindowKids = 30f;
        static readonly Color[] LampCols = { new Color(1f, 0.38f, 0.32f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 1f, 0.5f), new Color(0.4f, 0.72f, 1f) };
        static Mesh plateOn, plateOff, lampOff;
        static Mesh[] lampOn;

        static int TunnelTo(Run r, int room) { for (int i = 0; i < r.tunnels.Count; i++) if (r.tunnels[i].b == room) return i; return -1; }

        /// <summary>How far the open floor reaches either side of a tunnel point (for walls and gates across it).</summary>
        static float HalfWidth(Run r, Vector2 q, Vector2 side)
        {
            float w = 1.2f;
            for (int sgn = -1; sgn <= 1; sgn += 2)
                for (float d = 0.5f; d < 4.5f; d += 0.25f)
                    if (Sample(r.F, q.x + side.x * d * sgn, q.y + side.y * d * sgn) < 0.1f) { w = Mathf.Max(w, d); break; }
            return Mathf.Min(w, 4.2f);
        }

        // ================================================================ STATIC DECOR (before the light is baked)
        /// <summary>The chasm down, the rope up and the checkpoint stone - part of the cave's decor mesh, so they are lit with it.</summary>
        void DecorateSpecial(Run r, MeshBuilder mb, List<Lamp> lamps, System.Random rng)
        {
            float u() => (float)rng.NextDouble();
            var e = r.e;
            Color rock = Muted(e.rock), top = Muted(e.rockTop);
            Color32 rope = new Color(0.62f, 0.5f, 0.32f), ropeDark = new Color(0.45f, 0.35f, 0.22f);
            for (int ri = 0; ri < r.rooms.Count; ri++)
            {
                var rm = r.rooms[ri];
                if (rm.role == Role.Descent)
                {
                    var p = Nudge(r, rm.Center);
                    r.hasDescent = true;
                    r.descentPos = p;
                    var back = rm.parent >= 0 ? r.rooms[rm.parent].c - rm.c : Vector2.left;
                    back = back.sqrMagnitude > 0.01f ? back.normalized : Vector2.left;
                    r.descentArrive = Nudge(r, p + new Vector3(back.x, 0f, back.y) * 4.6f);
                    // a black hole in the floor with a broken rim, and a rope tied to a stake going over the edge
                    mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.02f, Quaternion.identity, new Vector3(5f, 0.05f, 4.4f)), new Color(0.01f, 0.01f, 0.015f), 16, 1f, true);
                    mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.03f, Quaternion.identity, new Vector3(3.6f, 0.04f, 3.1f)), new Color32(0, 0, 0, 255), 14, 1f, true);
                    for (int k = 0; k < 11; k++)
                    {
                        float a = k / 11f * 6.28f + u() * 0.3f;
                        var o = new Vector3(Mathf.Cos(a) * 2.6f, 0f, Mathf.Sin(a) * 2.25f);
                        float s = 0.6f + u() * 0.6f;
                        mb.Rock(Matrix4x4.TRS(p + o + Vector3.up * s * 0.2f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(s * 1.3f, s * 0.6f, s)), rock, top, (uint)rng.Next(), 5, 4, 0.25f);
                    }
                    var stake = p + new Vector3(back.x, 0f, back.y) * 3.1f;
                    mb.Cylinder(Matrix4x4.TRS(stake + Vector3.up * 0.45f, Quaternion.identity, new Vector3(0.16f, 0.9f, 0.16f)), ropeDark, 6);
                    var lip = p + new Vector3(back.x, 0f, back.y) * 1.7f;
                    var mid = (stake + lip) * 0.5f + Vector3.up * 0.25f;
                    mb.Cylinder(Matrix4x4.TRS(mid, Quaternion.FromToRotation(Vector3.up, (lip - stake).normalized), new Vector3(0.07f, (lip - stake).magnitude, 0.07f)), rope, 5);
                    mb.Cylinder(Matrix4x4.TRS(lip + Vector3.down * 0.6f, Quaternion.identity, new Vector3(0.07f, 1.4f, 0.07f)), rope, 5);
                    // a faint cold glow from far below
                    mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.035f, Quaternion.identity, new Vector3(1.4f, 0.02f, 1.2f)), new Color32((byte)(e.glow.r * 70), (byte)(e.glow.g * 70), (byte)(e.glow.b * 70), 0), 12, 1f, true);
                    lamps.Add(new Lamp { p = p + Vector3.up * 1.2f, c = Color.Lerp(Color.white, e.glow, 0.5f), r = 9f, k = 0.7f });
                    r.pillars.Add(new Vector4(p.x, p.z, 2.1f, 0));
                }
                else if (rm.role == Role.Landing)
                {
                    // the rope you came down, hanging from a crack in the roof, a coil at its foot
                    var p = r.exitPos;
                    float ceilY = CeilAt(r, p.x, p.z);
                    float len = ceilY - p.y;
                    mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * (len * 0.5f + 0.2f), Quaternion.Euler(2f, 0, 1f), new Vector3(0.08f, len, 0.08f)), rope, 5);
                    for (float y = 1f; y < len; y += 1.6f) mb.Sphere(Matrix4x4.TRS(p + Vector3.up * y, Quaternion.identity, Vector3.one * 0.18f), ropeDark, 5, 3);
                    for (int k = 0; k < 3; k++) mb.Cylinder(Matrix4x4.TRS(p + new Vector3(0.3f, 0.06f + k * 0.07f, 0.1f), Quaternion.Euler(0, k * 40f, 0), new Vector3(0.9f - k * 0.15f, 0.06f, 0.8f - k * 0.15f)), ropeDark, 10, 1f, false);
                    r.shaft = new Vector4(p.x, p.z, ceilY, 0f);
                    lamps.Add(new Lamp { p = p + Vector3.up * 1.4f, c = new Color(0.95f, 0.97f, 1f), r = 9f, k = 1f });
                    // the checkpoint: a standing stone with a green glow
                    var cp = Checkpoint(r);
                    Color32 green = new Color32(120, 255, 160, 0);
                    mb.Rock(Matrix4x4.TRS(cp + Vector3.up * 1f, Quaternion.Euler(0, 20f, 0), new Vector3(0.9f, 2.2f, 0.7f)), rock, top, 917u, 6, 5, 0.12f);
                    mb.Crystal(Matrix4x4.TRS(cp + Vector3.up * 2.1f, Quaternion.identity, new Vector3(0.35f, 0.8f, 0.35f)), green, new Color32(220, 255, 230, 0));
                    for (int k = 0; k < 3; k++) mb.Box(Matrix4x4.TRS(cp + new Vector3(0f, 0.7f + k * 0.42f, -0.36f), Quaternion.Euler(0, 20f, 0), new Vector3(0.36f, 0.08f, 0.04f)), green);
                    lamps.Add(new Lamp { p = cp + Vector3.up * 2f, c = new Color(0.6f, 1f, 0.7f), r = 8f, k = 1f });
                    r.pillars.Add(new Vector4(cp.x, cp.z, 0.6f, 0));
                }
            }
        }

        static Vector3 Checkpoint(Run r)
        {
            var rm = r.rooms[0];
            return Nudge(r, new Vector3(rm.c.x + rm.r * 0.25f, 0f, rm.c.y - rm.r * 0.35f));
        }

        // ================================================================ OBJECTS (walls, gates, plates, levers)
        void BuildSpecial(Run r, System.Random rng)
        {
            for (int ri = 0; ri < r.rooms.Count; ri++)
            {
                if (r.rooms[ri].role == Role.Secret) BuildSecret(r, ri, rng);
                if (r.rooms[ri].role == Role.Puzzle)
                {
                    int vi = r.rooms.FindIndex(x => x.parent == ri && x.role == Role.Vault);
                    if (vi >= 0) BuildPuzzle(r, ri, vi, rng);
                }
            }
        }

        GameObject Obj(Run r, string name, MeshBuilder mb, Material mat, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : r.root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cave_" + name.ToLowerInvariant());
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        void BuildSecret(Run r, int ri, System.Random rng)
        {
            int ti = TunnelTo(r, ri);
            if (ti < 0) return;
            float u() => (float)rng.NextDouble();
            var t = r.tunnels[ti];
            var q = t.pts[2];
            var dir = (t.pts[3] - t.pts[1]).normalized;
            var side = new Vector2(-dir.y, dir.x);
            var sc = new Secret { room = ri, tunnel = ti, dir = dir, halfW = HalfWidth(r, q, side) };
            sc.pos = new Vector3(q.x, FloorAt2(r, q.x, q.y), q.y);
            var e = r.e;
            Color rock = Muted(e.rock) * 0.95f, top = Muted(e.rockTop);
            Vector3 s3 = new Vector3(side.x, 0f, side.y), d3 = new Vector3(dir.x, 0f, dir.y);
            float h = CeilAt(r, q.x, q.y) - sc.pos.y;
            var mb = new MeshBuilder();
            // a heap of fallen rock from wall to wall and floor to roof
            float span = sc.halfW * 2f + 1.4f;
            int cols = Mathf.Max(2, Mathf.CeilToInt(span / 1.6f) + 1);
            for (float y = 0.8f; y < h + 0.6f; y += 1.5f)
                for (int c = 0; c < cols; c++)
                {
                    float off = -span * 0.5f + span * c / (cols - 1) + (u() - 0.5f) * 0.5f;
                    float s = 1.7f + u() * 0.6f;
                    mb.Rock(Matrix4x4.TRS(sc.pos + s3 * off + Vector3.up * y + d3 * ((u() - 0.5f) * 0.5f), Quaternion.Euler(u() * 40f, u() * 360f, u() * 40f), new Vector3(s * 1.1f, s * 0.95f, s)), rock, top, (uint)rng.Next(), 6, 4, 0.2f);
                }
            // light through the cracks, on both faces
            Color32 glow = new Color(Mathf.Lerp(e.glow.r, 1f, 0.3f), Mathf.Lerp(e.glow.g, 1f, 0.3f), Mathf.Lerp(e.glow.b, 1f, 0.3f), 0f);
            for (int face = -1; face <= 1; face += 2)
                for (int c = 0; c < 3; c++)
                {
                    // a jagged seam of light, set into the heap so the boulders cover parts of it
                    var at = sc.pos + s3 * ((c - 1) * sc.halfW * 0.55f + (u() - 0.5f) * 0.4f) + d3 * (face * 0.75f) + Vector3.up * (0.3f + u() * 0.4f);
                    float ang = u() * 40f - 20f;
                    for (int k = 0; k < 6; k++)
                    {
                        ang = Mathf.Clamp(-ang * 0.8f + (u() - 0.5f) * 50f, -40f, 40f);
                        var rot = Quaternion.LookRotation(d3) * Quaternion.Euler(0, 0, ang);
                        var step = rot * Vector3.up * 0.5f;
                        mb.Box(Matrix4x4.TRS(at + step * 0.5f, rot, new Vector3(0.09f, 0.56f, 0.6f)), glow);
                        at += step;
                    }
                }
            Light(mb, r.lamps, r, 0.68f);
            sc.go = Obj(r, "CrackedWall", mb, RenderAssets.Creature);
            int n = Mathf.CeilToInt(sc.halfW * 2f / 1.1f) + 1;
            for (int k = 0; k < n; k++)
            {
                float off = -sc.halfW + sc.halfW * 2f * k / Mathf.Max(1, n - 1);
                sc.pillars.Add(r.pillars.Count);
                r.pillars.Add(new Vector4(q.x + side.x * off, q.y + side.y * off, 0.95f, 0));
            }
            r.secrets.Add(sc);
        }

        static void EnsurePuzzleMeshes()
        {
            if (plateOn != null) return;
            var a = new MeshBuilder();
            a.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.13f, 0), Quaternion.identity, new Vector3(1.3f, 0.03f, 1.3f)), new Color32(40, 44, 52, 255), 14, 1f, true);
            for (int k = 0; k < 6; k++) a.Box(Matrix4x4.TRS(Quaternion.Euler(0, k * 60f, 0) * new Vector3(0, 0.15f, 0.42f), Quaternion.Euler(0, k * 60f, 0), new Vector3(0.12f, 0.02f, 0.3f)), new Color32(70, 80, 95, 255));
            plateOff = a.ToMesh("cave_plate_off");
            var b = new MeshBuilder();
            b.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.13f, 0), Quaternion.identity, new Vector3(1.3f, 0.03f, 1.3f)), new Color32(120, 230, 255, 0), 14, 1f, true);
            for (int k = 0; k < 6; k++) b.Box(Matrix4x4.TRS(Quaternion.Euler(0, k * 60f, 0) * new Vector3(0, 0.15f, 0.42f), Quaternion.Euler(0, k * 60f, 0), new Vector3(0.12f, 0.02f, 0.3f)), new Color32(240, 255, 255, 0));
            plateOn = b.ToMesh("cave_plate_on");
            var c = new MeshBuilder();
            c.Sphere(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 0.36f), new Color32(45, 45, 50, 255), 8, 5);
            lampOff = c.ToMesh("cave_lamp_off");
            lampOn = new Mesh[LampCols.Length];
            for (int i = 0; i < LampCols.Length; i++)
            {
                var d = new MeshBuilder();
                var col = LampCols[i];
                d.Sphere(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 0.4f), new Color(col.r, col.g, col.b, 0f), 8, 5);
                lampOn[i] = d.ToMesh("cave_lamp_on");
            }
        }

        void BuildPuzzle(Run r, int pi, int vi, System.Random rng)
        {
            int ti = TunnelTo(r, vi);
            if (ti < 0) return;
            EnsurePuzzleMeshes();
            float u() => (float)rng.NextDouble();
            var e = r.e;
            var pz = new Puzzle { room = pi, vault = vi, kind = e.kind == CaveKind.Ruin || u() < 0.5f ? PuzzleKind.Levers : PuzzleKind.Plates };
            var t = r.tunnels[ti];
            var q = t.pts[2];
            var dir = (t.pts[3] - t.pts[1]).normalized;
            var side = new Vector2(-dir.y, dir.x);
            pz.gateDir = dir; pz.halfW = HalfWidth(r, q, side);
            pz.gatePos = new Vector3(q.x, FloorAt2(r, q.x, q.y), q.y);
            Vector3 s3 = new Vector3(side.x, 0f, side.y);
            float h = Mathf.Min(CeilAt(r, q.x, q.y) - pz.gatePos.y - 0.15f, 4.4f);
            // the gate: iron bars from wall to wall in a stone frame, built in world space under a parent that sinks
            var gateRoot = new GameObject("Gate").transform;
            gateRoot.SetParent(r.root.transform, false);
            pz.gate = gateRoot;
            var mb = new MeshBuilder();
            Color32 iron = new Color(0.36f, 0.37f, 0.42f), ironHi = new Color(0.55f, 0.56f, 0.62f), stone = Color.Lerp(Muted(e.rock), Color.white, 0.2f);
            float w = pz.halfW + 0.6f;
            for (float x = -w; x <= w + 0.01f; x += 0.42f)
                mb.Cylinder(Matrix4x4.TRS(pz.gatePos + s3 * x + Vector3.up * (h * 0.5f - 0.3f), Quaternion.identity, new Vector3(0.11f, h + 0.6f, 0.11f)), iron, ironHi, 6, 1f, true);
            var rotG = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));
            mb.Box(Matrix4x4.TRS(pz.gatePos + Vector3.up * (h - 0.15f), rotG, new Vector3(w * 2f + 0.4f, 0.35f, 0.3f)), iron, ironHi);
            mb.Box(Matrix4x4.TRS(pz.gatePos + Vector3.up * 1.2f, rotG, new Vector3(w * 2f + 0.3f, 0.14f, 0.2f)), iron);
            Light(mb, r.lamps, r, 0.75f);
            Obj(r, "Bars", mb, RenderAssets.Creature, gateRoot);
            // levers: lamps on the gate; plates: a rune slab above it
            pz.on = new bool[4];
            Vector3 face = -new Vector3(dir.x, 0f, dir.y);   // toward the puzzle chamber
            if (pz.kind == PuzzleKind.Levers)
                for (int i = 0; i < 4; i++)
                {
                    // on the face of the top beam, toward the chamber (the roof is right above the beam)
                    var lamp = new GameObject("Lamp" + i);
                    lamp.transform.SetParent(gateRoot, false);
                    lamp.transform.position = pz.gatePos + s3 * ((i - 1.5f) * Mathf.Min(1.1f, w * 0.45f)) + Vector3.up * (h - 0.75f) + face * 0.35f;
                    var mf = lamp.AddComponent<MeshFilter>(); mf.sharedMesh = lampOff;
                    lamp.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
                    pz.marks.Add(mf);
                }
            for (int k = 0; k < Mathf.CeilToInt(pz.halfW * 2f / 0.9f) + 1; k++)
            {
                float off = -pz.halfW + pz.halfW * 2f * k / Mathf.Max(1, Mathf.CeilToInt(pz.halfW * 2f / 0.9f));
                pz.pillars.Add(r.pillars.Count);
                r.pillars.Add(new Vector4(q.x + side.x * off, q.y + side.y * off, 0.55f, 0));
            }

            var rm = r.rooms[pi];
            bool Spaced(Vector3 p) { foreach (var o in pz.spots) if (Flat2(o, p) < 4.6f * 4.6f) return false; return Flat2(p, pz.gatePos) > 4f * 4f; }
            if (pz.kind == PuzzleKind.Plates)
            {
                for (int tries = 0; tries < 80 && pz.spots.Count < 4; tries++)
                {
                    var p = OpenSpot(r, rm.c, rm.r * 0.72f, 2f, rng);
                    if (!Spaced(p)) continue;
                    pz.spots.Add(p);
                    var slab = new MeshBuilder();
                    slab.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.06f, Quaternion.identity, new Vector3(1.75f, 0.12f, 1.75f)), stone, stone, 14, 1f, true);
                    Light(slab, r.lamps, r, 0.8f);
                    Obj(r, "PlateSlab", slab, RenderAssets.Creature);
                    var ring = new GameObject("Plate");
                    ring.transform.SetParent(r.root.transform, false);
                    ring.transform.position = p;
                    var mf = ring.AddComponent<MeshFilter>(); mf.sharedMesh = plateOff;
                    ring.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
                    pz.marks.Add(mf);
                }
                if (pz.spots.Count < 2) { pz.solved = true; pz.gate.gameObject.SetActive(false); ClearPillars(r, pz.pillars); }   // no room for plates: leave the vault open
            }
            if (pz.kind == PuzzleKind.Levers)
            {
                for (int tries = 0; tries < 120 && pz.spots.Count < 4; tries++)
                {
                    int s = rng.Next(r.segs.Count);
                    var sg = r.segs[s];
                    var m = new Vector2((sg.x + sg.z) * 0.5f, (sg.y + sg.w) * 0.5f);
                    if ((m - rm.c).magnitude > rm.r * 1.25f) continue;
                    var n = r.segNormals[s];
                    var p = new Vector3(m.x + n.x * 1.1f, 0f, m.y + n.y * 1.1f);
                    if (Sample(r.F, p.x, p.z) < 0.8f || !Spaced(p)) continue;
                    p.y = FloorAt2(r, p.x, p.z);
                    int i = pz.spots.Count;
                    pz.spots.Add(p);
                    var lm = new MeshBuilder();
                    var face2 = Quaternion.LookRotation(new Vector3(n.x, 0f, n.y));
                    lm.Box(Matrix4x4.TRS(p + Vector3.up * 0.35f, face2, new Vector3(0.9f, 0.7f, 0.6f)), stone, stone);
                    // the two gems show which lamps this lever flips
                    for (int g = 0; g < 2; g++)
                    {
                        var col = LampCols[(i + g) % 4];
                        lm.Sphere(Matrix4x4.TRS(p + Vector3.up * 0.45f + face2 * new Vector3(g == 0 ? -0.22f : 0.22f, 0f, 0.31f), Quaternion.identity, Vector3.one * 0.2f), new Color(col.r, col.g, col.b, 0f), 6, 4);
                    }
                    Light(lm, r.lamps, r, 0.8f);
                    var baseGo = Obj(r, "Lever", lm, RenderAssets.Creature);
                    var hb = new MeshBuilder();
                    hb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.45f, 0), Quaternion.identity, new Vector3(0.09f, 0.9f, 0.09f)), iron, ironHi, 6, 1f, true);
                    hb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.92f, 0), Quaternion.identity, Vector3.one * 0.2f), new Color(0.5f, 0.3f, 0.2f), 6, 4);
                    var handle = Obj(r, "Handle", hb, RenderAssets.Creature, baseGo.transform).transform;
                    handle.position = p + Vector3.up * 0.7f;
                    handle.rotation = face2 * Quaternion.Euler(35f, 0, 0);
                    pz.handles.Add(handle);
                    pz.faces.Add(face2);
                }

                // start from all lit with one or two levers pulled, so it always takes a pull or two to solve
                for (int i = 0; i < 4; i++) pz.on[i] = true;
                int a = rng.Next(0, 4), b = (a + 1 + rng.Next(0, 3)) % 4;
                Flip(pz, a); if (u() < 0.75f) Flip(pz, b);
                ShowLamps(pz);
                if (pz.spots.Count < 4) { pz.solved = true; pz.gate.gameObject.SetActive(false); ClearPillars(r, pz.pillars); }   // nowhere for four levers: leave the vault open
            }
            r.puzzles.Add(pz);
        }

        static void Flip(Puzzle pz, int i) { pz.on[i] = !pz.on[i]; pz.on[(i + 1) % 4] = !pz.on[(i + 1) % 4]; }

        static void ShowLamps(Puzzle pz)
        {
            for (int i = 0; i < pz.marks.Count && i < 4; i++)
                pz.marks[i].sharedMesh = pz.kind == PuzzleKind.Levers ? (pz.on[i] ? lampOn[i] : lampOff) : (pz.on[i] ? plateOn : plateOff);
        }

        // ================================================================ WHAT YOU TOOK BEFORE
        /// <summary>After a level is rebuilt: everything taken on it earlier this visit is gone again.</summary>
        void ApplyDone(Run r)
        {
            if (r.done.Count == 0) return;
            for (int i = 0; i < r.chests.Count; i++)
                if (r.done.Contains(Key(r, "c", i))) { var c = r.chests[i]; c.opened = true; c.sealedUntilDown = false; if (c.lid != null) c.lid.localRotation = Quaternion.Euler(-100f, 0, 0); }
            for (int i = 0; i < r.nests.Count; i++)
                if (r.done.Contains(Key(r, "n", i))) { var n = r.nests[i]; n.taken = true; if (n.egg != null) n.egg.SetActive(false); }
            for (int i = 0; i < r.guards.Count; i++)
            {
                if (!r.done.Contains(Key(r, "g", i))) continue;
                var g = r.guards[i];
                g.down = true;
                if (g.a != null && g.a.gameObject.activeSelf && g.a.Data == g.d) g.a.Despawn();
                if (g.chest != null) g.chest.sealedUntilDown = false;
                if (g.main) { r.cleared = true; OpenGuardianPortal(r, g); }
            }
            for (int i = 0; i < r.nodes.Count; i++)
                if (r.done.Contains(Key(r, "v", i))) { r.nodes[i].alive = false; if (r.nodeGo[i] != null) r.nodeGo[i].SetActive(false); r.pillars[r.nodePillar[i]] = new Vector4(1e6f, 1e6f, 0f, 0f); }
            for (int i = 0; i < r.secrets.Count; i++)
                if (r.done.Contains(Key(r, "s", i))) OpenSecret(r, r.secrets[i], true);
            for (int i = 0; i < r.puzzles.Count; i++)
                if (r.done.Contains(Key(r, "p", i))) { var pz = r.puzzles[i]; pz.solved = true; pz.told = true; pz.gate.gameObject.SetActive(false); ClearPillars(r, pz.pillars); for (int k = 0; k < 4; k++) pz.on[k] = true; ShowLamps(pz); }
        }

        static void ClearPillars(Run r, List<int> idx) { foreach (int k in idx) r.pillars[k] = new Vector4(1e6f, 1e6f, 0f, 0f); }

        // ================================================================ PROMPTS
        string SpecialPrompt(Run r, Vector3 p, out System.Action act)
        {
            act = null;
            foreach (var sc in r.secrets)
            {
                if (sc.open) continue;
                var d = new Vector2(p.x - sc.pos.x, p.z - sc.pos.z);
                float along = Mathf.Abs(Vector2.Dot(d, sc.dir)), across = Mathf.Abs(Vector2.Dot(d, new Vector2(-sc.dir.y, sc.dir.x)));
                if (along < 2.6f && across < sc.halfW + 0.6f)
                {
                    var s = sc;
                    act = () => Smash(r, s);
                    return "SMASH WALL";
                }
            }
            foreach (var pz in r.puzzles)
            {
                if (pz.solved || pz.kind != PuzzleKind.Levers) continue;
                for (int i = 0; i < pz.spots.Count; i++)
                {
                    if (Flat2(pz.spots[i], p) > 1.9f * 1.9f) continue;
                    var z = pz; int li = i;
                    act = () => Pull(r, z, li);
                    return "PULL LEVER";
                }
            }
            return null;
        }

        void Smash(Run r, Secret sc)
        {
            if (sc.open) return;
            sc.hp--;
            var at = sc.pos + Vector3.up * 1.2f - new Vector3(sc.dir.x, 0f, sc.dir.y) * 1f;
            Fx.Debris(at, r.e.rock, 10);
            Fx.Burst(at, r.e.rock, 14, 4f, 0.25f, 0.6f, false, 6f);
            SynthAudio.Play(Sfx.Mine, 0.8f, 0.9f);
            GameManager.I.CameraRig.Shake(0.3f);
            if (sc.hp > 0) { if (sc.hp == 2) GameEvents.Say("The cracked rock shifts. Again!", new Color(0.85f, 0.8f, 1f)); return; }
            OpenSecret(r, sc, false);
        }

        void OpenSecret(Run r, Secret sc, bool quietly)
        {
            if (sc.open) return;
            sc.open = true;
            if (sc.go != null) Destroy(sc.go);
            ClearPillars(r, sc.pillars);
            r.done.Add(Key(r, "s", r.secrets.IndexOf(sc)));
            if (quietly) return;
            var at = sc.pos + Vector3.up * 1.5f;
            Fx.Burst(at, r.e.rock, 34, 6f, 0.35f, 0.8f, false, 8f);
            Fx.Sparkle(at, r.e.glow, 24, 1.5f);
            SynthAudio.Play(Sfx.Mine, 0.6f, 1f);
            GameManager.I.CameraRig.Shake(0.5f);
            GameManager.I.AddPlayerXp(40);
            GameEvents.Good("The wall gives way - a hidden chamber!");
            RefreshMap(r);
        }

        void Pull(Run r, Puzzle pz, int i)
        {
            if (pz.solved || pz.cool > 0f) return;
            pz.cool = 0.45f;
            Flip(pz, i);
            ShowLamps(pz);
            while (pz.pulled.Count < pz.handles.Count) pz.pulled.Add(false);
            pz.pulled[i] = !pz.pulled[i];
            pz.handles[i].rotation = pz.faces[i] * Quaternion.Euler(pz.pulled[i] ? -35f : 35f, 0, 0);
            SynthAudio.Play(Sfx.Clunk, 1.1f, 0.6f);
            bool all = true; foreach (bool b in pz.on) all &= b;
            if (all) Solve(r, pz);
        }

        void Solve(Run r, Puzzle pz)
        {
            pz.solved = true;
            r.done.Add(Key(r, "p", r.puzzles.IndexOf(pz)));
            ClearPillars(r, pz.pillars);
            SynthAudio.Play(Sfx.LevelUp, 0.8f);
            GameManager.I.CameraRig.Shake(0.25f);
            GameManager.I.AddPlayerXp(50);
            Fx.Sparkle(pz.gatePos + Vector3.up * 2f, new Color(0.6f, 0.95f, 1f), 30, 2f);
            GameEvents.Good("The gate grinds open!");
        }

        // ================================================================ TICK
        void TickSpecial(Run r, float dt, PlayerController pl)
        {
            var pp = pl.Position;
            foreach (var sc in r.secrets)
                if (!sc.open && !sc.hinted && Flat2(sc.pos, pp) < 9f * 9f)
                {
                    sc.hinted = true;
                    GameEvents.Say("A cold draught whistles through cracks in the rock here... SMASH the cracked wall.", new Color(0.85f, 0.8f, 1f));
                }
            var comp = GameManager.I.Party.Companion;
            bool compOut = comp != null && comp.gameObject.activeSelf && comp.IsAlive;
            foreach (var pz in r.puzzles)
            {
                pz.cool -= dt;
                if (pz.solved)
                {
                    if (pz.gate.gameObject.activeSelf)
                    {
                        pz.sink += dt / 2.2f;
                        pz.gate.localPosition = Vector3.down * Mathf.SmoothStep(0f, 4.8f, pz.sink);
                        if (Random.value < dt * 12f) Fx.Debris(pz.gatePos + new Vector3(Random.Range(-pz.halfW, pz.halfW) * Mathf.Abs(pz.gateDir.y), 0.1f, Random.Range(-pz.halfW, pz.halfW) * Mathf.Abs(pz.gateDir.x)), r.e.rock, 2);
                        if (pz.sink >= 1f) pz.gate.gameObject.SetActive(false);
                    }
                    continue;
                }
                var rm = r.rooms[pz.room];
                if (!pz.told && (new Vector2(pp.x, pp.z) - rm.c).magnitude < rm.r * 0.9f)
                {
                    pz.told = true;
                    GameEvents.Say(pz.kind == PuzzleKind.Plates
                        ? $"Rune plates and a barred gate. Step on all {pz.spots.Count} plates before their glow fades - your kin can stand on one too."
                        : "Four lamps over a barred gate, four levers. Each lever flips two lamps (its gems show which). Light all four.", new Color(0.6f, 0.95f, 1f));
                }
                if (pz.kind != PuzzleKind.Plates) continue;
                int lit = 0;
                for (int i = 0; i < pz.spots.Count; i++)
                {
                    var s = pz.spots[i];
                    bool pressed = (Flat2(s, pp) < 1.05f * 1.05f && Mathf.Abs(pp.y - s.y) < 1.2f) || (compOut && Flat2(s, comp.Position) < 1.1f * 1.1f);
                    if (pressed && !pz.on[i])
                    {
                        bool first = true; foreach (bool b in pz.on) if (b) first = false;
                        pz.on[i] = true;
                        if (first) pz.timer = WorldRules.Current.simpleControls ? PlateWindowKids : PlateWindow;
                        ShowLamps(pz);
                        SynthAudio.Play(Sfx.UiClick, 0.7f, 0.8f);
                        int nOn = 0; foreach (bool b in pz.on) if (b) nOn++;
                        HUD.DamageNumber(s + Vector3.up * 1.2f, $"{nOn}/{pz.spots.Count}", new Color(0.6f, 0.95f, 1f), 1f);
                    }
                    if (pz.on[i]) lit++;
                }
                if (lit == pz.spots.Count) { Solve(r, pz); continue; }
                if (lit > 0)
                {
                    pz.timer -= dt;
                    if (pz.timer <= 0f)
                    {
                        for (int i = 0; i < pz.on.Length; i++) pz.on[i] = false;
                        ShowLamps(pz);
                        SynthAudio.Play(Sfx.CaptureFail, 0.9f, 0.6f);
                        GameEvents.Say("The plates go dark. Step on every one before the glow fades.", new Color(0.7f, 0.8f, 0.9f));
                    }
                }
            }
            // the map fills in where you have been
            r.revealTimer -= dt;
            if (r.revealTimer <= 0f) { r.revealTimer = 0.35f; RevealAround(r, pp, 13f); }
        }

        // ================================================================ MAP
        static Color32 Dim(Color32 c) => new Color32((byte)(c.r * 0.42f), (byte)(c.g * 0.42f), (byte)(c.b * 0.48f), 255);

        /// <summary>Cells the map doesn't show yet: secret chambers whose wall still stands, and the far half of their tunnel.</summary>
        static bool[] HiddenCells(Run r)
        {
            var h = new bool[N * N];
            int si = 0;
            for (int ri = 0; ri < r.rooms.Count; ri++)
            {
                var rm = r.rooms[ri];
                if (rm.role != Role.Secret) continue;
                bool open = r.done.Contains(Key(r, "s", si)) || (si < r.secrets.Count && r.secrets[si].open);
                si++;
                if (open) continue;
                int ti = TunnelTo(r, ri);
                var t = ti >= 0 ? r.tunnels[ti] : null;
                float rad = rm.r * 1.5f;
                for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    var c = new Vector2(Origin.x + (i + 0.5f) * Res, Origin.y + (j + 0.5f) * Res);
                    if ((c - rm.c).sqrMagnitude < rad * rad) { h[j * N + i] = true; continue; }
                    if (t == null) continue;
                    var from = Vector2.Lerp(t.pts[2], t.pts[3], 0.08f);
                    float dd = Mathf.Min(SegDist(c, from, t.pts[3]), SegDist(c, t.pts[3], t.pts[4]));
                    if (dd < t.w * 1.5f + 1.5f) h[j * N + i] = true;
                }
            }
            return h;
        }

        static void RefreshMap(Run r)
        {
            var old = r.map;
            BuildMap(r);
            if (old != null) Destroy(old);
        }

        static void RevealAround(Run r, Vector3 p, float rad)
        {
            if (r.seen == null || r.mapPx == null || r.map == null) return;
            int ci = Mathf.FloorToInt((p.x - Origin.x) / Res), cj = Mathf.FloorToInt((p.z - Origin.y) / Res);
            int rr = Mathf.CeilToInt(rad / Res);
            bool changed = false;
            for (int j = Mathf.Max(0, cj - rr); j <= Mathf.Min(N - 1, cj + rr); j++)
            for (int i = Mathf.Max(0, ci - rr); i <= Mathf.Min(N - 1, ci + rr); i++)
            {
                int k = j * N + i;
                if (r.seen[k] || (i - ci) * (i - ci) + (j - cj) * (j - cj) > rr * rr) continue;
                r.seen[k] = true;
                r.mapPx[k] = r.mapBase[k];
                changed = true;
            }
            if (!changed) return;
            r.map.SetPixels32(r.mapPx);
            r.map.Apply(false, false);
        }

        static bool Seen(Run r, Vector3 p)
        {
            if (r.seen == null) return true;
            int i = Mathf.Clamp(Mathf.FloorToInt((p.x - Origin.x) / Res), 0, N - 1), j = Mathf.Clamp(Mathf.FloorToInt((p.z - Origin.y) / Res), 0, N - 1);
            return r.seen[j * N + i];
        }

        public struct CaveMark { public Vector3 pos; public MapIcon icon; public Color col; public string label; }

        /// <summary>What the minimap and the Map tab mark underground. Things you haven't been near stay unmarked (the way out always shows).</summary>
        public void MapMarks(List<CaveMark> list)
        {
            list.Clear();
            var r = run;
            if (r == null) return;
            void M(Vector3 p, MapIcon icon, Color c, string label, bool always = false) { if (always || Seen(r, p)) list.Add(new CaveMark { pos = p, icon = icon, col = c, label = label }); }
            if (r.level == 0) M(r.exitPos, MapIcon.Arch, new Color(0.7f, 0.95f, 1f), "Way out", true);
            else { M(r.exitPos, MapIcon.Cave, new Color(0.95f, 0.9f, 0.75f), "Rope up", true); M(Checkpoint(r), MapIcon.Waystone, new Color(0.55f, 1f, 0.65f), "Checkpoint", true); }
            if (r.bossPortal.HasValue) M(r.bossPortal.Value, MapIcon.Arch, new Color(1f, 0.85f, 0.4f), "Portal out", true);
            if (r.hasDescent) M(r.descentPos, MapIcon.Cave, new Color(0.75f, 0.6f, 1f), "Chasm down");
            foreach (var c in r.chests) if (!c.opened) M(c.pos, MapIcon.Chest, c.sealedUntilDown ? new Color(1f, 0.55f, 0.45f) : new Color(1f, 0.85f, 0.4f), "Chest");
            foreach (var n in r.nests) if (!n.taken) M(n.pos, MapIcon.Nest, n.rare ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.9f, 0.85f), n.rare ? "Rare egg" : "Egg");
            foreach (var g in r.guards) if (!g.down) M(g.home, MapIcon.Lair, g.role == Role.Alpha ? new Color(1f, 0.4f, 0.35f) : new Color(1f, 0.62f, 0.35f), g.role == Role.Alpha ? (g.main ? "Guardian" : "Deep alpha") : g.title);
            foreach (var pz in r.puzzles) if (!pz.solved) M(pz.gatePos, MapIcon.Pillars, new Color(0.55f, 0.9f, 1f), "Barred gate");
            foreach (var sc in r.secrets) if (!sc.open && sc.hinted) M(sc.pos, MapIcon.Relic, new Color(0.85f, 0.7f, 1f), "Cracked wall");
        }

        /// <summary>The part of the cave zone the chambers use, as a square in map UV (for the Map tab).</summary>
        public Rect MapBounds()
        {
            if (run == null) return new Rect(0, 0, 1, 1);
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var rm in run.rooms) { x0 = Mathf.Min(x0, rm.c.x - rm.r * 1.5f); z0 = Mathf.Min(z0, rm.c.y - rm.r * 1.5f); x1 = Mathf.Max(x1, rm.c.x + rm.r * 1.5f); z1 = Mathf.Max(z1, rm.c.y + rm.r * 1.5f); }
            float size = Mathf.Max(x1 - x0, z1 - z0) + 8f;
            float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;
            return new Rect((cx - size * 0.5f - Origin.x) / ZoneSize, (cz - size * 0.5f - Origin.y) / ZoneSize, size / ZoneSize, size / ZoneSize);
        }

        /// <summary>Lines for the Map tab: where you are and what is still to find.</summary>
        public List<string> MapSummary()
        {
            var lines = new List<string>();
            var r = run;
            if (r == null) return lines;
            lines.Add(r.levels > 1 ? (r.level == 0 ? "Upper level" : "Lower level - checkpoint at the rope") : TierWord(r.tier) + " " + r.e.Word);
            int chests = 0, opened = 0; foreach (var c in r.chests) { chests++; if (c.opened) opened++; }
            int eggs = 0, taken = 0; foreach (var n in r.nests) { if (n.egg == null) continue; eggs++; if (n.taken) taken++; }
            foreach (var g in r.guards) lines.Add($"{(g.role == Role.Alpha ? (g.main ? "Guardian" : "Deep alpha") : g.title)}: {(g.down ? "beaten" : Seen(r, g.home) ? g.d.DisplayName : "somewhere deeper")}");
            if (chests > 0) lines.Add($"Chests opened: {opened}/{chests}");
            if (eggs > 0) lines.Add($"Eggs taken: {taken}/{eggs}");
            foreach (var pz in r.puzzles) lines.Add(pz.solved ? "Vault gate: open" : Seen(r, pz.gatePos) ? (pz.kind == PuzzleKind.Plates ? "Vault gate: step on every plate" : "Vault gate: light all four lamps") : "Vault gate: not found yet");
            foreach (var sc in r.secrets) lines.Add(sc.open ? "Hidden chamber: found" : sc.hinted ? "A cracked wall - smash it" : "Something is hidden here...");
            if (r.hasDescent) lines.Add(Seen(r, r.descentPos) ? "A chasm leads down" : "A way down is somewhere ahead");
            if (r.quiet) lines.Add("Quiet: its treasure returns later");
            return lines;
        }

        public bool OnLowerLevel => run != null && run.level > 0;

        // ================================================================ DEV (Editor/CaveTest)
        public string DevSpecial()
        {
            if (run == null) return "not in a cave";
            var sb = new System.Text.StringBuilder();
            sb.Append("level ").Append(run.level).Append('/').Append(run.levels).Append(" done[").Append(string.Join(",", run.done)).Append("] ");
            foreach (var sc in run.secrets) sb.Append($"secret open={sc.open} w={sc.halfW:0.0}; ");
            foreach (var pz in run.puzzles) sb.Append($"{pz.kind} spots={pz.spots.Count} on={string.Join("", System.Array.ConvertAll(pz.on, b => b ? "1" : "0"))} solved={pz.solved}; ");
            if (run.hasDescent) sb.Append("descent ").Append(run.descentPos);
            return sb.ToString();
        }

        /// <summary>Walks the player onto every plate in turn / pulls the right levers / smashes every cracked wall.</summary>
        public string DevSolveAll()
        {
            if (run == null) return "not in a cave";
            var pl = GameManager.I.Player;
            foreach (var sc in run.secrets) { Smash(run, sc); Smash(run, sc); Smash(run, sc); }
            foreach (var pz in run.puzzles)
            {
                if (pz.solved) continue;
                if (pz.kind == PuzzleKind.Plates)
                    foreach (var s in pz.spots) { pl.Teleport(s, 0f); TickSpecial(run, 0.2f, pl); }
                else
                    for (int mask = 1; mask < 16 && !pz.solved; mask++)
                    {
                        var save = (bool[])pz.on.Clone();
                        for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) Flip(pz, i);
                        bool all = true; foreach (bool b in pz.on) all &= b;
                        pz.on = save;
                        if (!all) continue;
                        for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) { pz.cool = 0f; Pull(run, pz, i); }
                    }
            }
            return DevSpecial();
        }

        public Vector3 DevDescent => run != null && run.hasDescent ? run.descentPos : Vector3.zero;
        public Vector3 DevStart => run != null ? run.startPos : Vector3.zero;
    }
}
