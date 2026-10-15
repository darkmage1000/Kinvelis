using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Cave layout (cave pass, 2026-10-15). A cave is planned as a graph of chambers - a main path from the mouth to its
    /// guardian plus side branches - then carved out of a signed-distance field sampled every metre: each chamber is a
    /// wobbly ellipse with side pockets (a ruin hall is a box), each tunnel a winding capsule chain, and value noise
    /// roughens the edge. The rock face is the field's zero line (marching squares, `Caves.Mesh`), so walls are natural
    /// curves, not grid squares. The same field gives the floor's dips and rises, the ceiling's dome and the collision.
    /// What a cave holds depends on its tier (`TierOf`, by the entrance's top level):
    ///   Early (&lt; 15): two or three chambers, one boss (a big common kin), a little treasure.
    ///   Mid (15-29): + two branches - an egg nest and a mini-boss with a cave blueprint - a guarded ore hoard, an alpha at the end.
    ///   End (30+): + four branches, rare kin, hazards everywhere, Geode Cores, valuable blueprints, two mini-bosses and two alphas.
    /// </summary>
    public partial class Caves
    {
        public const float Res = 1f;                 // field spacing (m)
        public const int N = 184;                    // cells a side: the zone is 184 m square
        public const float ZoneSize = N * Res;
        const int S1 = N + 1;                        // corners a side

        public enum CaveTier { Early, Mid, End }
        public static CaveTier TierOf(Entrance e) => e.maxLv >= 30 ? CaveTier.End : e.maxLv >= 15 ? CaveTier.Mid : CaveTier.Early;
        public static string TierWord(CaveTier t) => t == CaveTier.End ? "deep" : t == CaveTier.Mid ? "winding" : "shallow";

        enum Role { Entrance, Pass, Colony, Hoard, Eggs, Hazard, Rare, Mini, Alpha, Boss }

        class Room
        {
            public Vector2 c;              // world x, z
            public float r, rx, rz, ang, ceil;
            public float p1, p2, p3;       // edge wobble phases
            public Role role;
            public int branch = -1, parent = -1;
            public bool hall;
            public readonly List<Vector3> lobes = new List<Vector3>();   // x, z, radius
            public Vector3 Center => new Vector3(c.x, FloorY, c.y);
        }

        class Tunnel { public Vector2[] pts; public float w, ceil, phase; public int a, b; }

        // ================================================================ PLAN
        static float RoleRadius(Role role, System.Random rng)
        {
            float u = (float)rng.NextDouble();
            switch (role)
            {
                case Role.Entrance: return 7f + u;
                case Role.Colony: return 9f + u * 1.5f;
                case Role.Hoard: return 7.5f + u * 1.5f;
                case Role.Eggs: return 7f + u;
                case Role.Hazard: return 9f + u * 2f;
                case Role.Rare: return 8f + u;
                case Role.Mini: return 9f + u;
                case Role.Alpha: return 11f + u * 1.5f;
                case Role.Boss: return 9.5f + u;
                default: return 7.5f + u * 1.5f;
            }
        }

        static void PlanLayout(Run r, System.Random rng)
        {
            var e = r.e;
            float sc = e.kind == CaveKind.Burrow ? 0.8f : 1f;
            // main path
            var main = new List<Role>();
            switch (r.tier)
            {
                case CaveTier.Early:
                    main.Add(Role.Entrance);
                    if (rng.NextDouble() < 0.55) main.Add(Role.Pass);
                    main.Add(Role.Boss);
                    break;
                case CaveTier.Mid:
                    main.Add(Role.Entrance); main.Add(rng.NextDouble() < 0.5 ? Role.Colony : Role.Pass); main.Add(Role.Hoard); main.Add(Role.Alpha);
                    break;
                default:
                    main.Add(Role.Entrance); main.Add(Role.Colony); main.Add(Role.Hoard); main.Add(Role.Hazard); main.Add(Role.Alpha);
                    break;
            }
            // the mouth chamber sits by the west edge
            var start = new Room { role = Role.Entrance, r = RoleRadius(Role.Entrance, rng) * sc };
            start.c = new Vector2(Origin.x + 8f + start.r * 1.25f, Origin.y + 46f + (float)rng.NextDouble() * 92f);
            r.rooms.Add(Shape(start, e, rng));
            Vector2 dir = Vector2.right;
            for (int i = 1; i < main.Count; i++)
            {
                int prev = r.rooms.Count - 1;
                int got = PlaceRoom(r, prev, main[i], -1, dir, sc, rng);
                if (got < 0)
                {
                    // boxed in: the guardian's chamber still has to fit, so drop to it straight away
                    if (main[i] != Role.Alpha && main[i] != Role.Boss) { got = PlaceRoom(r, prev, main[main.Count - 1], -1, dir, sc, rng); if (got >= 0) break; }
                    if (got < 0) break;
                }
                var d = r.rooms[got].c - r.rooms[prev].c;
                dir = (d.normalized + Vector2.right * 0.8f).normalized;   // wander, but keep heading in
            }
            // a guardian chamber is required: if planning failed to reach one, the last chamber becomes it
            var last = r.rooms[r.rooms.Count - 1];
            if (last.role != Role.Alpha && last.role != Role.Boss && r.rooms.Count > 1) last.role = r.tier == CaveTier.Early ? Role.Boss : Role.Alpha;
            int mainCount = r.rooms.Count;

            // branches: (parent index on the main path, chambers along it)
            var branches = new List<(int, Role[])>();
            if (r.tier == CaveTier.Mid)
            {
                branches.Add((1, new[] { Role.Eggs }));
                branches.Add((Mathf.Min(2, mainCount - 1), new[] { Role.Mini }));
            }
            else if (r.tier == CaveTier.End)
            {
                branches.Add((1, new[] { Role.Eggs }));
                branches.Add((Mathf.Min(2, mainCount - 1), new[] { Role.Pass, Role.Mini }));
                branches.Add((Mathf.Min(3, mainCount - 1), new[] { Role.Hazard, Role.Mini }));
                branches.Add((mainCount - 1, new[] { Role.Rare, Role.Alpha }));
            }
            int bi = 0;
            foreach (var br in branches)
            {
                int parent = Mathf.Clamp(br.Item1, 0, mainCount - 1);
                // try the side with more room first, then the other, then any parent on the main path
                bool placed = false;
                for (int tryParent = 0; tryParent < mainCount && !placed; tryParent++)
                {
                    int p = tryParent == 0 ? parent : (parent + tryParent) % mainCount;
                    if (p == 0 && br.Item2[br.Item2.Length - 1] == Role.Alpha) continue;   // the second alpha is deep, not by the door
                    var along = p + 1 < mainCount ? r.rooms[p + 1].c - r.rooms[p].c : r.rooms[p].c - r.rooms[Mathf.Max(0, p - 1)].c;
                    if (along.sqrMagnitude < 0.01f) along = Vector2.right;
                    along.Normalize();
                    var side = new Vector2(-along.y, along.x) * ((bi & 1) == 0 ? 1f : -1f);
                    for (int s = 0; s < 2 && !placed; s++)
                    {
                        var want = (s == 0 ? side : -side);
                        int from = p, count0 = r.rooms.Count;
                        bool ok = true;
                        foreach (var role in br.Item2)
                        {
                            int got = PlaceRoom(r, from, role, bi, (want + along * 0.3f).normalized, sc, rng);
                            if (got < 0) { ok = false; break; }
                            from = got;
                        }
                        if (ok) placed = true;
                        else
                        {
                            // undo a half-built branch
                            while (r.rooms.Count > count0) r.rooms.RemoveAt(r.rooms.Count - 1);
                            r.tunnels.RemoveAll(t => t.a >= count0 || t.b >= count0);
                        }
                    }
                }
                bi++;
            }
        }

        /// <summary>Puts a chamber of this role next to 'parent', joined by a tunnel. Returns its index or -1.</summary>
        static int PlaceRoom(Run r, int parent, Role role, int branch, Vector2 pref, float sc, System.Random rng)
        {
            var p = r.rooms[parent];
            float rr = RoleRadius(role, rng) * sc;
            float baseAng = Mathf.Atan2(pref.y, pref.x);
            for (int attempt = 0; attempt < 70; attempt++)
            {
                float spread = (25f + attempt * 2.6f) * Mathf.Deg2Rad;
                float ang = baseAng + ((float)rng.NextDouble() * 2f - 1f) * spread;
                float corr = (attempt < 45 ? 8f + (float)rng.NextDouble() * 9f : 5f + (float)rng.NextDouble() * 4f) * sc;
                var c = p.c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (p.r * 1.1f + rr * 1.1f + corr);
                float m = rr * 1.35f + 4f;
                if (c.x - m < Origin.x || c.x + m > Origin.x + ZoneSize || c.y - m < Origin.y || c.y + m > Origin.y + ZoneSize) continue;
                bool bad = false;
                for (int i = 0; i < r.rooms.Count && !bad; i++)
                {
                    var q = r.rooms[i];
                    if ((q.c - c).magnitude < rr * 1.25f + q.r * 1.25f + 5f) bad = true;
                    else if (i != parent && SegDist(q.c, p.c, c) < q.r * 1.2f + 3.5f) bad = true;   // the tunnel would cut through another chamber
                }
                if (bad) continue;
                var room = new Room { role = role, r = rr, c = c, branch = branch, parent = parent };
                r.rooms.Add(Shape(room, r.e, rng));
                r.tunnels.Add(MakeTunnel(p, room, parent, r.rooms.Count - 1, r.e, rng));
                return r.rooms.Count - 1;
            }
            return -1;
        }

        /// <summary>Fills in a chamber's ellipse, wobble, side pockets and ceiling.</summary>
        static Room Shape(Room rm, Entrance e, System.Random rng)
        {
            float u() => (float)rng.NextDouble();
            rm.rx = rm.r * (0.92f + u() * 0.26f);
            rm.rz = rm.r * (0.74f + u() * 0.24f);
            rm.ang = u() * Mathf.PI;
            rm.p1 = u() * 6.28f; rm.p2 = u() * 6.28f; rm.p3 = u() * 6.28f;
            rm.hall = e.kind == CaveKind.Ruin && rm.role != Role.Colony && rm.role != Role.Eggs && rm.role != Role.Hoard;
            if (!rm.hall)
            {
                int lobes = rm.r > 8.5f ? 1 + rng.Next(0, 3) : rng.Next(0, 2);
                for (int k = 0; k < lobes; k++)
                {
                    float a = u() * 6.28f;
                    float lr = rm.r * (0.34f + u() * 0.2f);
                    rm.lobes.Add(new Vector3(rm.c.x + Mathf.Cos(a) * rm.r * 0.78f, rm.c.y + Mathf.Sin(a) * rm.r * 0.78f, lr));
                }
            }
            bool burrow = e.kind == CaveKind.Burrow;
            rm.ceil = burrow ? Mathf.Clamp(rm.r * 0.55f + 1.2f, 4.2f, 6.2f) : rm.hall ? 9f : Mathf.Clamp(rm.r * 0.85f + 2.4f, 7f, 13.5f);
            return rm;
        }

        static Tunnel MakeTunnel(Room a, Room b, int ia, int ib, Entrance e, System.Random rng)
        {
            var d = b.c - a.c;
            var side = new Vector2(-d.y, d.x).normalized;
            float bend = (float)rng.NextDouble() * 2f - 1f;
            var pts = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float off = Mathf.Sin(t * Mathf.PI) * bend * Mathf.Min(6f, d.magnitude * 0.18f) + (i > 0 && i < 4 ? ((float)rng.NextDouble() - 0.5f) * 2.4f : 0f);
                pts[i] = a.c + d * t + side * off;
            }
            bool burrow = e.kind == CaveKind.Burrow;
            return new Tunnel { pts = pts, a = ia, b = ib, w = burrow ? 1.7f + (float)rng.NextDouble() * 0.4f : 2f + (float)rng.NextDouble() * 0.7f,
                ceil = burrow ? 3.8f : 4.8f + (float)rng.NextDouble() * 0.8f, phase = (float)rng.NextDouble() * 6.28f };
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            return (a + ab * t - p).magnitude;
        }

        // ================================================================ FIELD
        /// <summary>Carves the plan into the field: F = metres inside the rock face (negative = solid), C = ceiling, Flo = floor offset.</summary>
        static void BuildField(Run r)
        {
            int n = S1 * S1;
            r.F = new float[n]; r.C = new float[n]; r.Flo = new float[n];
            var rough = new float[n];
            var hall = new bool[n];
            for (int k = 0; k < n; k++) r.F[k] = -99f;
            int seed = r.seed;

            void Stamp(float x0, float z0, float x1, float z1, System.Func<float, float, float> sd, float ceil, float rgh, bool isHall)
            {
                int i0 = Mathf.Max(0, Mathf.FloorToInt((x0 - Origin.x) / Res)), i1 = Mathf.Min(N, Mathf.CeilToInt((x1 - Origin.x) / Res));
                int j0 = Mathf.Max(0, Mathf.FloorToInt((z0 - Origin.y) / Res)), j1 = Mathf.Min(N, Mathf.CeilToInt((z1 - Origin.y) / Res));
                for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    float x = Origin.x + i * Res, z = Origin.y + j * Res;
                    float v = sd(x, z);
                    int k = j * S1 + i;
                    if (v > r.F[k]) { r.F[k] = v; r.C[k] = ceil; rough[k] = rgh; hall[k] = isHall; }
                }
            }

            foreach (var rm in r.rooms)
            {
                var room = rm;
                float ext = rm.r * 1.4f + 2f;
                if (rm.hall)
                {
                    float hx = rm.rx * 0.86f, hz = rm.rz * 0.92f;
                    // halls are squared to the world so their built walls run along the grid
                    Stamp(rm.c.x - ext, rm.c.y - ext, rm.c.x + ext, rm.c.y + ext, (x, z) => Mathf.Min(hx - Mathf.Abs(x - room.c.x), hz - Mathf.Abs(z - room.c.y)), rm.ceil, 0f, true);
                }
                else
                {
                    float ca = Mathf.Cos(rm.ang), sa = Mathf.Sin(rm.ang), mr = Mathf.Min(rm.rx, rm.rz);
                    Stamp(rm.c.x - ext, rm.c.y - ext, rm.c.x + ext, rm.c.y + ext, (x, z) =>
                    {
                        float dx = x - room.c.x, dz = z - room.c.y;
                        float lx = (dx * ca + dz * sa) / room.rx, lz = (-dx * sa + dz * ca) / room.rz;
                        float len = Mathf.Sqrt(lx * lx + lz * lz), th = Mathf.Atan2(lz, lx);
                        float mod = 1f + 0.1f * Mathf.Sin(2f * th + room.p1) + 0.07f * Mathf.Sin(3f * th + room.p2) + 0.05f * Mathf.Sin(5f * th + room.p3);
                        return (mod - len) * mr;
                    }, rm.ceil, 1f, false);
                    foreach (var lb in rm.lobes)
                    {
                        var l = lb;
                        Stamp(l.x - l.z - 2f, l.y - l.z - 2f, l.x + l.z + 2f, l.y + l.z + 2f, (x, z) => l.z - Mathf.Sqrt((x - l.x) * (x - l.x) + (z - l.y) * (z - l.y)), rm.ceil * 0.8f, 1f, false);
                    }
                }
            }
            foreach (var t in r.tunnels)
            {
                var tt = t;
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (var p in t.pts) { x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y); x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y); }
                float pad = t.w * 1.6f + 2f;
                Stamp(x0 - pad, z0 - pad, x1 + pad, z1 + pad, (x, z) =>
                {
                    float best = 1e9f, along = 0f, acc = 0f;
                    var q = new Vector2(x, z);
                    for (int s = 0; s + 1 < tt.pts.Length; s++)
                    {
                        var a = tt.pts[s]; var ab = tt.pts[s + 1] - a;
                        float L = ab.magnitude;
                        float u = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-4f, L * L));
                        float d = (a + ab * u - q).magnitude;
                        if (d < best) { best = d; along = acc + u * L; }
                        acc += L;
                    }
                    float w = tt.w * (1f + 0.22f * Mathf.Sin(along * 0.33f + tt.phase));
                    return w - best;
                }, t.ceil, 0.45f, false);
            }

            // roughen the rock face (not the built halls), keep the border solid
            for (int j = 0; j < S1; j++)
            for (int i = 0; i < S1; i++)
            {
                int k = j * S1 + i;
                float x = Origin.x + i * Res, z = Origin.y + j * Res;
                if (rough[k] > 0f && r.F[k] > -6f)
                    r.F[k] += ((VN(x * 0.24f, z * 0.24f, seed) - 0.5f) * 1.7f + (VN(x * 0.8f, z * 0.8f, seed + 7) - 0.5f) * 0.55f) * rough[k];
                if (i < 3 || j < 3 || i > N - 3 || j > N - 3) r.F[k] = Mathf.Min(r.F[k], -1f);
            }

            // ceiling: blend chamber heights, then dome them (low at the rock face, full height toward the middle)
            var tmp = new float[n];
            for (int pass = 0; pass < 4; pass++)
            {
                for (int j = 1; j < N; j++)
                for (int i = 1; i < N; i++)
                {
                    int k = j * S1 + i;
                    if (r.F[k] < -4f) { tmp[k] = r.C[k]; continue; }
                    float sum = 0f; int cnt = 0;
                    for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        int kk = (j + dj) * S1 + i + di;
                        if (r.F[kk] < -4f) continue;
                        sum += r.C[kk]; cnt++;
                    }
                    tmp[k] = cnt > 0 ? sum / cnt : r.C[k];
                }
                var sw = r.C; r.C = tmp; tmp = sw;
            }
            bool burrow = r.e.kind == CaveKind.Burrow;
            float edgeH = burrow ? 3.6f : 4.6f;
            for (int j = 0; j < S1; j++)
            for (int i = 0; i < S1; i++)
            {
                int k = j * S1 + i;
                float x = Origin.x + i * Res, z = Origin.y + j * Res;
                float f = r.F[k];
                float top = Mathf.Max(edgeH, r.C[k]);
                float low = Mathf.Min(edgeH, top);
                float dome = hall[k] ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f / 5.5f));
                r.C[k] = Mathf.Lerp(low, top, dome) + (hall[k] ? 0f : (VN(x * 0.3f, z * 0.3f, seed + 13) - 0.5f) * 0.9f);
                // the floor rises a little toward the rock face and rolls gently elsewhere; halls are flat
                r.Flo[k] = hall[k] ? 0f : 0.55f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f / 2.4f))) + (VN(x * 0.13f, z * 0.13f, seed + 3) - 0.5f) * 0.6f * Mathf.Max(0.35f, rough[k]);
            }
            r.hall = hall;
        }

        /// <summary>Smooth value noise in 0..1 (lattice of hashed values, smoothstep blend).</summary>
        static float VN(float x, float z, int seed)
        {
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            float fx = x - ix, fz = z - iz;
            fx = fx * fx * (3f - 2f * fx); fz = fz * fz * (3f - 2f * fz);
            float a = Hash.F(ix, iz, seed), b = Hash.F(ix + 1, iz, seed), c = Hash.F(ix, iz + 1, seed), d = Hash.F(ix + 1, iz + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        // ---------------------------------------------------------------- field queries
        static float Sample(float[] a, float x, float z)
        {
            float gx = (x - Origin.x) / Res, gz = (z - Origin.y) / Res;
            int i = Mathf.Clamp(Mathf.FloorToInt(gx), 0, N - 1), j = Mathf.Clamp(Mathf.FloorToInt(gz), 0, N - 1);
            float fx = Mathf.Clamp01(gx - i), fz = Mathf.Clamp01(gz - j);
            int k = j * S1 + i;
            return Mathf.Lerp(Mathf.Lerp(a[k], a[k + 1], fx), Mathf.Lerp(a[k + S1], a[k + S1 + 1], fx), fz);
        }

        /// <summary>Metres from the rock face at this point (negative = inside rock). Outside a cave: -1.</summary>
        public static float Open(Vector3 p) => I != null && I.run != null && InZone(p.x, p.z) ? Sample(I.run.F, p.x, p.z) : -1f;

        /// <summary>The cave floor's height here (Ground.Height in the cave zone).</summary>
        public static float FloorAt(float x, float z) => I != null && I.run != null && I.run.Flo != null ? FloorY + Sample(I.run.Flo, x, z) : FloorY;
        static float CeilAt(Run r, float x, float z) => FloorY + Sample(r.C, x, z);

        /// <summary>Unit direction toward open space (up the field).</summary>
        static Vector2 Grad(Run r, float x, float z)
        {
            float gx = Sample(r.F, x + 0.35f, z) - Sample(r.F, x - 0.35f, z), gz = Sample(r.F, x, z + 0.35f) - Sample(r.F, x, z - 0.35f);
            var g = new Vector2(gx, gz);
            return g.sqrMagnitude > 1e-8f ? g.normalized : Vector2.right;
        }

        /// <summary>A floor point near 'c' (within 'rad') at least 'clear' metres from the rock. Falls back to c.</summary>
        static Vector3 OpenSpot(Run r, Vector2 c, float rad, float clear, System.Random rng)
        {
            for (int k = 0; k < 40; k++)
            {
                float a = (float)rng.NextDouble() * 6.28f, d = Mathf.Sqrt((float)rng.NextDouble()) * rad;
                float x = c.x + Mathf.Cos(a) * d, z = c.y + Mathf.Sin(a) * d;
                if (Sample(r.F, x, z) >= clear) return new Vector3(x, FloorY + Sample(r.Flo, x, z), z);
            }
            return new Vector3(c.x, FloorY + Sample(r.Flo, c.x, c.y), c.y);
        }

        // ================================================================ COLLISION
        /// <summary>Pushes a circle out of the rock face, stalagmites and chests (called from WorldCollision.PushOut).</summary>
        public static bool PushOut(ref Vector3 p, float rad)
        {
            if (I == null || I.run == null || !InZone(p.x, p.z)) return false;
            var run = I.run;
            bool hit = false;
            // buried in rock (a shove, a fast dash): climb back out along the field
            for (int it = 0; it < 4; it++)
            {
                float f = Sample(run.F, p.x, p.z);
                if (f >= 0.05f) break;
                var g = Grad(run, p.x, p.z);
                float step = Mathf.Max(0.15f, -f + 0.1f);
                p.x += g.x * step; p.z += g.y * step;
                hit = true;
            }
            // the face itself: every wall segment near the circle
            int bi = Mathf.Clamp(Mathf.FloorToInt((p.x - Origin.x) / Bucket), 0, BN - 1), bj = Mathf.Clamp(Mathf.FloorToInt((p.z - Origin.y) / Bucket), 0, BN - 1);
            for (int dj = -1; dj <= 1; dj++)
            for (int di = -1; di <= 1; di++)
            {
                int i = bi + di, j = bj + dj;
                if (i < 0 || j < 0 || i >= BN || j >= BN) continue;
                var list = run.buckets[j * BN + i];
                if (list == null) continue;
                for (int s = 0; s < list.Count; s++)
                {
                    var sg = run.segs[list[s]];
                    var on = run.segNormals[list[s]];   // toward the open side
                    float ax = sg.x, az = sg.y, bx = sg.z, bz = sg.w;
                    float ex = bx - ax, ez = bz - az;
                    float t = Mathf.Clamp01(((p.x - ax) * ex + (p.z - az) * ez) / Mathf.Max(1e-6f, ex * ex + ez * ez));
                    float qx = ax + ex * t, qz = az + ez * t;
                    float dx = p.x - qx, dz = p.z - qz;
                    float d2 = dx * dx + dz * dz;
                    if (d2 >= rad * rad) continue;
                    if (dx * on.x + dz * on.y <= 0f && t > 0f && t < 1f)
                    {
                        // on the rock side of this stretch of wall: out along its normal, not further in
                        p.x = qx + on.x * rad; p.z = qz + on.y * rad;
                        hit = true;
                        continue;
                    }
                    if (d2 < 1e-8f) continue;
                    float d = Mathf.Sqrt(d2);
                    p.x += dx / d * (rad - d); p.z += dz / d * (rad - d);
                    hit = true;
                }
            }
            foreach (var pl in run.pillars)
            {
                float dx = p.x - pl.x, dz = p.z - pl.y, rr = rad + pl.z;
                float d2 = dx * dx + dz * dz;
                if (d2 >= rr * rr || d2 < 1e-6f) continue;
                float d = Mathf.Sqrt(d2);
                p.x += dx / d * (rr - d); p.z += dz / d * (rr - d);
                hit = true;
            }
            return hit;
        }

        /// <summary>True if a point is inside cave rock or above its ceiling (projectiles stop there).</summary>
        public static bool Solid(Vector3 p)
        {
            if (I == null || I.run == null || !InZone(p.x, p.z)) return false;
            var run = I.run;
            return Sample(run.F, p.x, p.z) < -0.15f || p.y > CeilAt(run, p.x, p.z) + 0.5f;
        }

        const float Bucket = 4f;
        const int BN = (int)(ZoneSize / Bucket) + 1;

        static void IndexSegments(Run r)
        {
            r.buckets = new List<int>[BN * BN];
            for (int s = 0; s < r.segs.Count; s++)
            {
                var sg = r.segs[s];
                float x0 = Mathf.Min(sg.x, sg.z), x1 = Mathf.Max(sg.x, sg.z), z0 = Mathf.Min(sg.y, sg.w), z1 = Mathf.Max(sg.y, sg.w);
                int i0 = Mathf.Clamp(Mathf.FloorToInt((x0 - Origin.x) / Bucket), 0, BN - 1), i1 = Mathf.Clamp(Mathf.FloorToInt((x1 - Origin.x) / Bucket), 0, BN - 1);
                int j0 = Mathf.Clamp(Mathf.FloorToInt((z0 - Origin.y) / Bucket), 0, BN - 1), j1 = Mathf.Clamp(Mathf.FloorToInt((z1 - Origin.y) / Bucket), 0, BN - 1);
                for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    int k = j * BN + i;
                    if (r.buckets[k] == null) r.buckets[k] = new List<int>(8);
                    r.buckets[k].Add(s);
                }
            }
        }
    }
}
