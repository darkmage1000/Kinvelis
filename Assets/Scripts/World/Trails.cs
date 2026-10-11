using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Trails (user request 2026-10-09: rival tamers "along clearly defined paths between settlements").
    /// Worn dirt paths join the three settlements to each other and the start exit to Hearthmere. Each is found once
    /// per world by a search over a coarse grid of the old island that prefers level, dry ground and goes round
    /// settlements and Warden rings, then smoothed. A trail is only paint and furniture:
    ///   - the ground is tinted along it (WorldGen.TerrainColor -> Tint; heights never change), wider on far-away
    ///     terrain and on the map so it still reads as a line;
    ///   - trees, rocks and pickups don't grow on it (BlocksNode);
    ///   - a lantern post stands every ~45 m with two pennants, each in the colour of the settlement that way
    ///     (white toward the start), so you can tell which way a road runs;
    ///   - every settlement has a short spur from its fire out through the gate to a hub where its roads meet.
    /// Tamers stand beside these roads close to the gate (Tamers.Init asks Along()).
    /// Everything read from other threads (Weight / Tint, used by chunk and map builds) is built before Prepare
    /// publishes it and never changed afterwards.
    /// </summary>
    public class Trails : MonoBehaviour
    {
        public static Trails I { get; private set; }

        public class Trail
        {
            public string name;
            /// <summary>Settlement index at each end (-1 = the start exit, -2 = a settlement's own gate spur).</summary>
            public int a, b;
            public Vector2[] pts;
            public float[] dist;     // distance along the trail at each point
            public float length;
            public Color colA, colB;
        }

        public static readonly List<Trail> All = new List<Trail>();

        const float HalfWidth = 2.3f;
        const float HubOut = 42f;            // where a settlement's roads meet, north of its gate
        const float Nav = 24f;               // search grid
        const int NavHalf = 74;              // +-1776 m: the old island
        const int NavSide = NavHalf * 2 + 1;
        const float Bucket = 32f, Reach = 14f;   // spatial hash cell, and the widest a trail is ever drawn (half)

        struct Seg { public Vector2 p, d; public float len; }
        static Dictionary<long, Seg[]> hash = new Dictionary<long, Seg[]>();

        static long Key(int x, int z) => ((long)(x + 100000) << 32) | (uint)(z + 100000);

        // ============================================================ QUERIES (thread-safe)
        /// <summary>0..1: how much of a trail lies under this point. halfWidth = how wide to draw it.</summary>
        public static float Weight(float x, float z, float halfWidth = HalfWidth)
        {
            var h = hash;
            if (h.Count == 0) return 0f;
            if (!h.TryGetValue(Key(Mathf.FloorToInt(x / Bucket), Mathf.FloorToInt(z / Bucket)), out var segs)) return 0f;
            float best = float.MaxValue;
            var p = new Vector2(x, z);
            for (int i = 0; i < segs.Length; i++)
            {
                var s = segs[i];
                float t = Mathf.Clamp(Vector2.Dot(p - s.p, s.d), 0f, s.len);
                float dsq = (s.p + s.d * t - p).sqrMagnitude;
                if (dsq < best) best = dsq;
            }
            float d = Mathf.Sqrt(best);
            float fade = Mathf.Max(1.4f, halfWidth * 0.6f);
            return 1f - Mathf.Clamp01((d - halfWidth) / fade);
        }

        /// <summary>Ground colour on a trail. sample = spacing of whatever is being coloured (terrain vertices, map pixels).</summary>
        public static Color Tint(float x, float z, Color col, float h, float ny, float cn, float sample)
        {
            if (hash.Count == 0 || h < 0.35f) return col;
            // coarse terrain and map pixels would miss a 4.6 m path between samples: widen it there, and thin the paint
            float half = Mathf.Min(Reach - 2f, Mathf.Max(HalfWidth, sample * 0.8f));
            float w = Weight(x, z, half);
            if (w <= 0f) return col;
            w *= Mathf.Lerp(1f, 0.7f, Mathf.InverseLerp(HalfWidth, 10f, half));
            w *= Mathf.Lerp(0.5f, 1f, WorldGen.Smooth(0.55f, 0.85f, ny));   // rock faces stay rock
            // packed earth: the ground's own colour, darker and browner, a little patchy
            Color dirt = Color.Lerp(col * 0.72f, new Color(0.64f, 0.52f, 0.35f), 0.7f) * (0.92f + cn * 0.14f);
            // from far off and on the map it is drawn as a pale line, so it stands out from the land around it
            float far = Mathf.InverseLerp(3f, 8f, sample);
            dirt = Color.Lerp(dirt, new Color(0.84f, 0.74f, 0.52f), far * 0.75f);
            return Color.Lerp(col, dirt, Mathf.Min(1f, w * Mathf.Lerp(0.92f, 1.15f, far)));
        }

        /// <summary>Trees, rocks and loose pickups don't sit on a trail.</summary>
        public static bool BlocksNode(Vector3 p) => hash.Count > 0 && Weight(p.x, p.z, HalfWidth + 0.5f) > 0.3f;

        /// <summary>A trail leaving settlement s, the one toward 'toward' (-1 = the start) if there is one.</summary>
        public static Trail From(int s, int toward)
        {
            Trail any = null;
            foreach (var t in All)
            {
                if (t.a == -2 || (t.a != s && t.b != s)) continue;
                int other = t.a == s ? t.b : t.a;
                if (other == toward) return t;
                if (any == null) any = t;
            }
            return any;
        }

        /// <summary>The point 'd' metres along a trail measured from the end at settlement s, and the direction of travel away from s.</summary>
        public static bool Along(Trail t, int s, float d, out Vector3 pos, out Vector3 dir)
        {
            pos = default; dir = Vector3.forward;
            if (t == null || t.pts.Length < 2) return false;
            bool fromB = t.b == s && t.a != s;
            float want = Mathf.Clamp(fromB ? t.length - d : d, 0f, t.length);
            int i = 1;
            while (i < t.pts.Length - 1 && t.dist[i] < want) i++;
            float span = Mathf.Max(0.001f, t.dist[i] - t.dist[i - 1]);
            Vector2 p = Vector2.Lerp(t.pts[i - 1], t.pts[i], (want - t.dist[i - 1]) / span);
            Vector2 dd = (t.pts[i] - t.pts[i - 1]).normalized * (fromB ? -1f : 1f);
            pos = new Vector3(p.x, WorldGen.Height(p.x, p.y), p.y);
            dir = new Vector3(dd.x, 0f, dd.y);
            return true;
        }

        // ============================================================ PLACEMENT
        /// <summary>Find the trails for this world. Call after Settlements.Prepare and Wardens.Prepare, before chunks or the map are built.</summary>
        public static void Prepare()
        {
            All.Clear();
            hash = new Dictionary<long, Seg[]>();
            var ss = Settlements.All;
            if (ss.Count == 0) return;
            navH = new float[NavSide * NavSide];
            for (int i = 0; i < navH.Length; i++) navH[i] = float.NaN;

            var hubs = new Vector2[ss.Count];
            for (int i = 0; i < ss.Count; i++)
            {
                hubs[i] = new Vector2(ss[i].pos.x, ss[i].pos.z + HubOut);
                // the spur: from the fire, out under the gate banner, to the hub
                Register(new Trail { name = ss[i].name + " gate", a = -2, b = -2, colA = ss[i].color, colB = ss[i].color },
                    new List<Vector2> { new Vector2(ss[i].pos.x, ss[i].pos.z + 3.5f), hubs[i] });
            }
            Color startCol = new Color(0.95f, 0.95f, 0.9f);
            var exit = WorldGen.StartExit;
            Route("Dawnfield Path", -1, 0, new Vector2(exit.x, exit.z), hubs[0], startCol, ss[0].color);
            for (int i = 0; i < ss.Count; i++)
                for (int j = i + 1; j < ss.Count; j++)
                    Route($"{ss[i].name} - {ss[j].name} Road", i, j, hubs[i], hubs[j], ss[i].color, ss[j].color);
            navH = null;
            Publish();
        }

        static void Route(string name, int a, int b, Vector2 from, Vector2 to, Color ca, Color cb)
        {
            var path = Search(from, to);
            if (path == null) { Debug.LogWarning("[Trails] no route for " + name); return; }
            Register(new Trail { name = name, a = a, b = b, colA = ca, colB = cb }, path);
        }

        static void Register(Trail t, List<Vector2> raw)
        {
            // round the corners the grid left (Chaikin, twice), keeping both ends where they are
            for (int pass = 0; pass < 2 && raw.Count > 2; pass++)
            {
                var next = new List<Vector2>(raw.Count * 2) { raw[0] };
                for (int i = 0; i < raw.Count - 1; i++)
                {
                    next.Add(Vector2.Lerp(raw[i], raw[i + 1], 0.25f));
                    next.Add(Vector2.Lerp(raw[i], raw[i + 1], 0.75f));
                }
                next.Add(raw[raw.Count - 1]);
                raw = next;
            }
            t.pts = raw.ToArray();
            t.dist = new float[t.pts.Length];
            for (int i = 1; i < t.pts.Length; i++) t.dist[i] = t.dist[i - 1] + Vector2.Distance(t.pts[i - 1], t.pts[i]);
            t.length = t.dist[t.pts.Length - 1];
            All.Add(t);
        }

        /// <summary>Drop every segment into each hash cell it could colour, then publish the finished table in one assignment.</summary>
        static void Publish()
        {
            var lists = new Dictionary<long, List<Seg>>();
            foreach (var t in All)
                for (int i = 1; i < t.pts.Length; i++)
                {
                    Vector2 p = t.pts[i - 1], q = t.pts[i];
                    float len = Vector2.Distance(p, q);
                    if (len < 0.01f) continue;
                    var s = new Seg { p = p, d = (q - p) / len, len = len };
                    int x0 = Mathf.FloorToInt((Mathf.Min(p.x, q.x) - Reach) / Bucket), x1 = Mathf.FloorToInt((Mathf.Max(p.x, q.x) + Reach) / Bucket);
                    int z0 = Mathf.FloorToInt((Mathf.Min(p.y, q.y) - Reach) / Bucket), z1 = Mathf.FloorToInt((Mathf.Max(p.y, q.y) + Reach) / Bucket);
                    for (int x = x0; x <= x1; x++)
                        for (int z = z0; z <= z1; z++)
                        {
                            long k = Key(x, z);
                            if (!lists.TryGetValue(k, out var l)) lists[k] = l = new List<Seg>(4);
                            l.Add(s);
                        }
                }
            var done = new Dictionary<long, Seg[]>(lists.Count);
            foreach (var kv in lists) done[kv.Key] = kv.Value.ToArray();
            hash = done;
        }

        // ---------------------------------------------------------------- search
        static float[] navH;

        static float NavHeight(int ix, int iz)
        {
            int i = (iz + NavHalf) * NavSide + ix + NavHalf;
            float h = navH[i];
            if (float.IsNaN(h)) navH[i] = h = WorldGen.Height(ix * Nav, iz * Nav);
            return h;
        }

        /// <summary>How unwelcome a cell is to walk a road through (1 = fine).</summary>
        static float CellCost(int ix, int iz, float h)
        {
            float c = 1f;
            if (h < 1f) c += 30f;                       // wet feet: only if there is no way round
            var p = new Vector3(ix * Nav, 0f, iz * Nav);
            if (Settlements.Inside(p, 14f)) c += 25f;   // round the village, not through its houses
            if (Wardens.Inside(p, 44f)) c += 12f;
            return c;
        }

        static List<Vector2> Search(Vector2 from, Vector2 to)
        {
            int sx = Mathf.Clamp(Mathf.RoundToInt(from.x / Nav), -NavHalf, NavHalf), sz = Mathf.Clamp(Mathf.RoundToInt(from.y / Nav), -NavHalf, NavHalf);
            int gx = Mathf.Clamp(Mathf.RoundToInt(to.x / Nav), -NavHalf, NavHalf), gz = Mathf.Clamp(Mathf.RoundToInt(to.y / Nav), -NavHalf, NavHalf);
            int n = NavSide * NavSide;
            var g = new float[n];
            var came = new int[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = float.MaxValue; came[i] = -1; }
            var heapF = new List<float>(4096);
            var heapI = new List<int>(4096);
            int start = (sz + NavHalf) * NavSide + sx + NavHalf, goal = (gz + NavHalf) * NavSide + gx + NavHalf;
            g[start] = 0f;
            Push(heapF, heapI, 0f, start);
            int guard = 0;
            bool found = false;
            while (heapI.Count > 0 && guard++ < 120000)
            {
                int cur = Pop(heapF, heapI);
                if (closed[cur]) continue;
                closed[cur] = true;
                if (cur == goal) { found = true; break; }
                int cx = cur % NavSide - NavHalf, cz = cur / NavSide - NavHalf;
                float ch = NavHeight(cx, cz);
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (nx < -NavHalf || nx > NavHalf || nz < -NavHalf || nz > NavHalf) continue;
                        int ni = (nz + NavHalf) * NavSide + nx + NavHalf;
                        if (closed[ni]) continue;
                        float nh = NavHeight(nx, nz);
                        if (nh < -1.6f) continue;   // deep water: a road can't go there
                        float d = Nav * (dx != 0 && dz != 0 ? 1.4142f : 1f);
                        float slope = Mathf.Abs(nh - ch) / d;
                        float cost = d * (CellCost(nx, nz, nh) + 55f * slope * slope + 3f * slope);
                        float ng = g[cur] + cost;
                        if (ng >= g[ni]) continue;
                        g[ni] = ng;
                        came[ni] = cur;
                        float hx = (nx - gx) * Nav, hz = (nz - gz) * Nav;
                        Push(heapF, heapI, ng + Mathf.Sqrt(hx * hx + hz * hz) * 1.25f, ni);
                    }
            }
            if (!found) return null;
            var cells = new List<Vector2>();
            for (int c = goal; c >= 0; c = came[c]) cells.Add(new Vector2((c % NavSide - NavHalf) * Nav, (c / NavSide - NavHalf) * Nav));
            cells.Reverse();
            // the real ends in place of the cells nearest them
            if (cells.Count >= 2) { cells[0] = from; cells[cells.Count - 1] = to; }
            else { cells.Clear(); cells.Add(from); cells.Add(to); }
            return cells;
        }

        static void Push(List<float> f, List<int> idx, float key, int v)
        {
            f.Add(key); idx.Add(v);
            int i = f.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (f[p] <= f[i]) break;
                float tf = f[p]; f[p] = f[i]; f[i] = tf;
                int ti = idx[p]; idx[p] = idx[i]; idx[i] = ti;
                i = p;
            }
        }

        static int Pop(List<float> f, List<int> idx)
        {
            int top = idx[0];
            int last = f.Count - 1;
            f[0] = f[last]; idx[0] = idx[last];
            f.RemoveAt(last); idx.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < f.Count && f[l] < f[m]) m = l;
                if (r < f.Count && f[r] < f[m]) m = r;
                if (m == i) break;
                float tf = f[m]; f[m] = f[i]; f[i] = tf;
                int ti = idx[m]; idx[m] = idx[i]; idx[i] = ti;
                i = m;
            }
            return top;
        }

        // ============================================================ FURNITURE
        const float PostEvery = 45f, PostSide = 3.4f, PostRange = 240f;
        readonly List<GameObject> posts = new List<GameObject>();
        readonly List<Vector3> postPos = new List<Vector3>();
        float cullTimer;

        public void Init()
        {
            I = this;
            int ti = 0;
            foreach (var t in All)
            {
                ti++;
                if (t.a == -2 || t.length < 30f) continue;
                var mesh = PostMesh(t, ti);
                int n = Mathf.Max(2, Mathf.FloorToInt(t.length / PostEvery));
                for (int k = 0; k <= n; k++)
                {
                    float d = t.length * k / n;
                    if (d < 24f || d > t.length - 24f) continue;   // the hubs where roads meet would get a thicket of posts
                    if (!Along(t, t.a, d, out var p, out var dir)) continue;
                    // alternate sides; never in water, never inside a settlement
                    Vector3 side = Vector3.Cross(Vector3.up, dir) * (k % 2 == 0 ? PostSide : -PostSide);
                    Vector3 at = p + side;
                    at.y = WorldGen.Height(at.x, at.z);
                    if (at.y < 0.6f || Settlements.Inside(at, 2f)) continue;
                    var go = new GameObject("TrailPost");
                    go.transform.SetParent(transform, false);
                    go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(dir));
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = RenderAssets.Creature;   // vertex alpha 0 = self-lit: the lantern glows at night
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    go.SetActive(false);
                    posts.Add(go);
                    postPos.Add(at);
                }
            }
        }

        /// <summary>A post with a lantern and two pennants. +Z is the direction of travel toward the trail's B end.</summary>
        static Mesh PostMesh(Trail t, int index)
        {
            var mb = new MeshBuilder();
            Color32 wood = new Color(0.4f, 0.29f, 0.2f), woodLt = new Color(0.52f, 0.39f, 0.27f);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 1.25f, 0), Quaternion.identity, new Vector3(0.14f, 2.5f, 0.14f)), wood, woodLt, 6, 1f, true);
            mb.Box(Matrix4x4.TRS(new Vector3(0, 2.3f, 0), Quaternion.identity, new Vector3(0.1f, 0.08f, 1.0f)), wood, woodLt);
            // lantern hanging under the arm, on the trail side
            mb.Box(Matrix4x4.TRS(new Vector3(0, 2.02f, 0), Quaternion.identity, new Vector3(0.2f, 0.05f, 0.2f)), wood);
            mb.Sphere(Matrix4x4.TRS(new Vector3(0, 1.88f, 0), Quaternion.identity, new Vector3(0.22f, 0.26f, 0.22f)), new Color(1f, 0.86f, 0.5f, 0f), 6, 4);
            // pennants: B's colour pointing on toward B, A's colour pointing back toward A
            Pennant(mb, 1f, t.colB);
            Pennant(mb, -1f, t.colA);
            return mb.ToMesh("trail_post" + index);
        }

        static void Pennant(MeshBuilder mb, float dir, Color c)
        {
            Color32 a = c, b = Color.Lerp(c, Color.black, 0.25f);
            mb.Box(Matrix4x4.TRS(new Vector3(0, 2.12f, dir * 0.38f), Quaternion.identity, new Vector3(0.03f, 0.26f, 0.3f)), a, b);
            mb.Cone(Matrix4x4.TRS(new Vector3(0, 2.12f, dir * 0.62f), Quaternion.Euler(dir * 90f, 0, 0), new Vector3(0.03f, 0.2f, 0.26f)), a, 4);
        }

        public void Tick(float dt, Vector3 player)
        {
            cullTimer -= dt;
            if (cullTimer > 0f) return;
            cullTimer = 0.5f;
            bool hide = Caves.InCave;
            for (int i = 0; i < posts.Count; i++)
            {
                bool on = !hide && (postPos[i] - player).sqrMagnitude < PostRange * PostRange;
                if (posts[i].activeSelf != on) posts[i].SetActive(on);
            }
        }
    }
}
