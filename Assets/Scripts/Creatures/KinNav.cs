using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Real paths on the ground (user, 2026-10-14: "knock out all pathways so it stops being a to do"). Camp kin and
    /// your companion ask `Steer` where to head next. While nothing is in the straight way (round obstacles from
    /// WorldCollision.FirstBlocker, house walls from Housing.WallBetween) that is the goal itself. Otherwise an A*
    /// search on a `CellSize` 0.5 m grid round both ends: every structure, landmark piece, tree, rock and piece of
    /// furniture is stamped as a disc grown by the walker's radius, every house wall, stair side and railing at the
    /// walker's height as a thick line (doorways stay open for whoever fits - the grid is laid so a cell centre sits
    /// on every doorway's middle). The cell path is pulled tight to a few corner points; the walker heads for the next
    /// one, skips ahead when the way to the one after is clear, and plans again when the goal moves, every 4 s, or when
    /// it stops getting anywhere. The window is both ends + 6 m, widened to 14 m and 26 m when that is boxed in (a long
    /// row of structures). A goal inside something (the structure it is walking up to) is reached from the
    /// nearest free cell and the last step slides in as before. At most `PerFrame` searches a frame; a walker waiting
    /// for one, or with no way at all, falls back to CreatureActor.AvoidObstacles. Storeys and stairs stay with
    /// Housing.Steer; this does the ground.
    /// </summary>
    public static class KinNav
    {
        public const float CellSize = 0.5f;
        const int MaxSide = 220;            // 110 m across
        const int PerFrame = 1, MaxExpand = 30000;   // 0.1-3 ms a search in the editor: one a frame keeps phones smooth
        static readonly float[] Margins = { 6f, 14f, 26f };   // a wider look round when the near one is boxed in

        public class Route
        {
            public readonly List<Vector3> pts = new List<Vector3>();
            public int next;
            public Vector3 goal, checkedGoal, progressFrom;
            public float until, failUntil, checkAt, progressT, giveUpUntil;
            public bool ok, clear;
            public int stuck;
        }

        static int frame = -1, used;
        static bool Budget()
        {
            if (Time.frameCount != frame) { frame = Time.frameCount; used = 0; }
            if (used >= PerFrame) return false;
            used++; return true;
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>Is the straight way from a to b blocked for a walker of radius r on the ground (goal = where it is really going)?</summary>
        public static bool Blocked(Vector3 a, Vector3 b, float r, Vector3 goal) =>
            WorldCollision.FirstBlocker(a, b, r, goal, out _, out _) || (Housing.I != null && Housing.I.WallBetween(a, b, r));

        /// <summary>
        /// Where to head on the way from `from` to `to`. False = blocked and no path yet (or none at all): the caller steers
        /// round things itself. True = aim is the goal (nothing in the way) or the next corner of the path.
        /// </summary>
        public static bool Steer(Route rt, Vector3 from, Vector3 to, float r, out Vector3 aim)
        {
            aim = to;
            float dist = Flat(to - from);
            if (dist < 0.6f) return true;
            if (Time.time < rt.giveUpUntil) return false;
            float now = Time.time;
            // is the straight way clear? (looked at four times a second, or at once when the goal moves)
            if (now >= rt.checkAt || Flat(rt.checkedGoal - to) > 1.5f)
            {
                rt.checkAt = now + 0.25f; rt.checkedGoal = to;
                Vector3 look = dist > 30f ? from + (to - from) * (30f / dist) : to;
                rt.clear = !Blocked(from, look, r, to);
                if (rt.clear) { rt.ok = false; rt.stuck = 0; }
                else if (rt.ok && rt.next < rt.pts.Count - 1 && !Blocked(from, rt.pts[rt.next + 1], r, to)) rt.next++;   // cut ahead
            }
            if (rt.clear) return true;
            // on a path but not getting anywhere (another kin in the gap, something new built): plan again, then give up for a moment
            bool stale = !rt.ok || now > rt.until || Flat(rt.goal - to) > 1.5f;
            rt.progressT += Time.deltaTime;
            if (rt.progressT > 1.5f)
            {
                if (rt.ok && Flat(from - rt.progressFrom) < 0.4f)
                {
                    stale = true;
                    if (++rt.stuck >= 3) { rt.stuck = 0; rt.ok = false; rt.giveUpUntil = now + 1.2f; return false; }
                }
                rt.progressT = 0f; rt.progressFrom = from;
            }
            if (stale && now >= rt.failUntil && Budget())
            {
                rt.goal = to; rt.until = now + 4f; rt.next = 0;
                rt.ok = FindPath(from, to, r, rt.pts);
                if (!rt.ok) rt.failUntil = now + 2f;
            }
            if (!rt.ok || rt.pts.Count == 0) return false;
            while (rt.next < rt.pts.Count - 1 && Flat(rt.pts[rt.next] - from) < 0.45f) rt.next++;
            aim = rt.pts[rt.next];
            return true;
        }

        // ============================================================ THE SEARCH
        static byte[] grid = new byte[MaxSide * MaxSide];
        static readonly float[] gCost = new float[MaxSide * MaxSide];
        static readonly int[] parent = new int[MaxSide * MaxSide];
        static readonly int[] seen = new int[MaxSide * MaxSide];
        static readonly int[] closed = new int[MaxSide * MaxSide];
        static readonly int[] heap = new int[MaxSide * MaxSide];
        static readonly float[] fCost = new float[MaxSide * MaxSide];
        static int searchId, heapN, W, H;
        static float ox, oz;
        static readonly List<Vector3> circles = new List<Vector3>();
        static readonly List<Vector4> walls = new List<Vector4>();
        static readonly List<int> cellPath = new List<int>();

        /// <summary>Plans a path; fills pts with corner points ending at `to`. False = too far, boxed in, or no way.</summary>
        public static bool FindPath(Vector3 from, Vector3 to, float r, List<Vector3> pts)
        {
            pts.Clear();
            if (Caves.InZone(from.x, from.z)) return false;
            foreach (var m in Margins)
            {
                int tried = Search(from, to, r, pts, m);
                if (tried > 0) return true;
                if (tried < 0) return false;   // the window can't grow any more
            }
            return false;
        }

        /// <summary>One search in a window `margin` round both ends: 1 = found, 0 = no way inside it, -1 = window too big.</summary>
        static int Search(Vector3 from, Vector3 to, float r, List<Vector3> pts, float Margin)
        {
            // the window: both ends and a margin, laid so cell centres fall on every multiple of 0.5 m (doorway middles)
            float x0 = Mathf.Floor((Mathf.Min(from.x, to.x) - Margin) / CellSize) * CellSize - CellSize * 0.5f;
            float z0 = Mathf.Floor((Mathf.Min(from.z, to.z) - Margin) / CellSize) * CellSize - CellSize * 0.5f;
            W = Mathf.CeilToInt((Mathf.Max(from.x, to.x) + Margin - x0) / CellSize);
            H = Mathf.CeilToInt((Mathf.Max(from.z, to.z) + Margin - z0) / CellSize);
            if (W > MaxSide || H > MaxSide) return -1;
            ox = x0; oz = z0;
            System.Array.Clear(grid, 0, W * H);
            // stamp what is in the way
            Vector3 c = new Vector3(x0 + W * CellSize * 0.5f, from.y, z0 + H * CellSize * 0.5f);
            float half = Mathf.Max(W, H) * CellSize * 0.75f;
            WorldCollision.CirclesIn(c, half, from.y, circles);
            foreach (var k in circles) StampCircle(k.x, k.z, (k.y + r) * 0.92f);
            if (Housing.I != null)
            {
                Housing.I.SegsIn(x0, z0, x0 + W * CellSize, z0 + H * CellSize, from.y + 0.3f, walls);
                foreach (var s in walls) StampSeg(new Vector2(s.x, s.y), new Vector2(s.z, s.w), (Housing.WallT * 0.5f + r) * 0.92f);
            }
            int start = CellOf(from), goal = CellOf(to);
            if (start < 0 || goal < 0) return 0;
            if (grid[start] != 0) { start = NearestFree(start, 6); if (start < 0) return 0; }
            if (grid[goal] != 0) { goal = NearestFree(goal, 8); if (goal < 0) return 0; }
            if (!AStar(start, goal)) return 0;
            // pull the cell path tight: corner points only
            int anchor = start;
            for (int i = 1; i < cellPath.Count; i++)
                if (!LineFree(anchor, cellPath[i])) { anchor = cellPath[i - 1]; pts.Add(Center(anchor, from.y)); }
            if (Flat(Center(goal, from.y) - to) > 0.3f) pts.Add(Center(goal, from.y));
            pts.Add(to);
            return 1;
        }

        static int CellOf(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - ox) / CellSize), z = Mathf.FloorToInt((p.z - oz) / CellSize);
            return x < 0 || z < 0 || x >= W || z >= H ? -1 : x + z * W;
        }
        static Vector3 Center(int i, float y) => new Vector3(ox + (i % W + 0.5f) * CellSize, y, oz + (i / W + 0.5f) * CellSize);

        static void StampCircle(float cx, float cz, float R)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt((cx - R - ox) / CellSize)), ix1 = Mathf.Min(W - 1, Mathf.FloorToInt((cx + R - ox) / CellSize));
            int iz0 = Mathf.Max(0, Mathf.FloorToInt((cz - R - oz) / CellSize)), iz1 = Mathf.Min(H - 1, Mathf.FloorToInt((cz + R - oz) / CellSize));
            float r2 = R * R;
            for (int z = iz0; z <= iz1; z++)
            {
                float dz = oz + (z + 0.5f) * CellSize - cz;
                for (int x = ix0; x <= ix1; x++)
                {
                    float dx = ox + (x + 0.5f) * CellSize - cx;
                    if (dx * dx + dz * dz < r2) grid[x + z * W] = 1;
                }
            }
        }

        static void StampSeg(Vector2 a, Vector2 b, float R)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - R - ox) / CellSize)), ix1 = Mathf.Min(W - 1, Mathf.FloorToInt((Mathf.Max(a.x, b.x) + R - ox) / CellSize));
            int iz0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - R - oz) / CellSize)), iz1 = Mathf.Min(H - 1, Mathf.FloorToInt((Mathf.Max(a.y, b.y) + R - oz) / CellSize));
            Vector2 ab = b - a; float len2 = Mathf.Max(1e-5f, ab.sqrMagnitude);
            for (int z = iz0; z <= iz1; z++)
                for (int x = ix0; x <= ix1; x++)
                {
                    Vector2 p = new Vector2(ox + (x + 0.5f) * CellSize, oz + (z + 0.5f) * CellSize);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    if ((p - (a + ab * t)).sqrMagnitude < R * R) grid[x + z * W] = 1;
                }
        }

        /// <summary>The nearest free cell within `rings` cells (ring by ring), or -1.</summary>
        static int NearestFree(int i, int rings)
        {
            int cx = i % W, cz = i / W;
            for (int k = 1; k <= rings; k++)
            {
                int best = -1; float bd = float.MaxValue;
                for (int z = cz - k; z <= cz + k; z++)
                    for (int x = cx - k; x <= cx + k; x++)
                    {
                        if (Mathf.Abs(x - cx) != k && Mathf.Abs(z - cz) != k) continue;
                        if (x < 0 || z < 0 || x >= W || z >= H || grid[x + z * W] != 0) continue;
                        float d = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                        if (d < bd) { bd = d; best = x + z * W; }
                    }
                if (best >= 0) return best;
            }
            return -1;
        }

        static bool AStar(int start, int goal)
        {
            searchId++;
            heapN = 0; cellPath.Clear();
            int gx = goal % W, gz = goal / W;
            gCost[start] = 0f; parent[start] = start; seen[start] = searchId;
            Push(start, H8(start % W, start / W, gx, gz));
            int expanded = 0;
            while (heapN > 0)
            {
                int cur = Pop();
                if (closed[cur] == searchId) continue;
                closed[cur] = searchId;
                if (cur == goal)
                {
                    for (int n = goal; ; n = parent[n]) { cellPath.Add(n); if (n == start) break; }
                    cellPath.Reverse();
                    return true;
                }
                if (++expanded > MaxExpand) return false;
                int x = cur % W, z = cur / W;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = x + dx, nz = z + dz;
                        if (nx < 0 || nz < 0 || nx >= W || nz >= H) continue;
                        int n = nx + nz * W;
                        if (grid[n] != 0 || closed[n] == searchId) continue;
                        if (dx != 0 && dz != 0 && (grid[nx + z * W] != 0 || grid[x + nz * W] != 0)) continue;   // no squeezing past a corner
                        float g = gCost[cur] + (dx != 0 && dz != 0 ? 1.4142f : 1f);
                        if (seen[n] == searchId && g >= gCost[n]) continue;
                        seen[n] = searchId; gCost[n] = g; parent[n] = cur;
                        Push(n, g + H8(nx, nz, gx, gz));
                    }
            }
            return false;
        }

        static float H8(int x, int z, int gx, int gz)
        {
            int dx = Mathf.Abs(x - gx), dz = Mathf.Abs(z - gz);
            return (dx + dz) + (1.4142f - 2f) * Mathf.Min(dx, dz);
        }

        static void Push(int n, float f)
        {
            fCost[n] = f;
            int i = heapN++;
            heap[i] = n;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (fCost[heap[p]] <= f) break;
                heap[i] = heap[p]; i = p;
            }
            heap[i] = n;
        }

        static int Pop()
        {
            int top = heap[0], last = heap[--heapN];
            int i = 0; float f = fCost[last];
            while (true)
            {
                int l = i * 2 + 1;
                if (l >= heapN) break;
                int c = l + 1 < heapN && fCost[heap[l + 1]] < fCost[heap[l]] ? l + 1 : l;
                if (fCost[heap[c]] >= f) break;
                heap[i] = heap[c]; i = c;
            }
            if (heapN > 0) heap[i] = last;
            return top;
        }

        /// <summary>Every cell a straight line between two cell centres passes over is free.</summary>
        static bool LineFree(int a, int b)
        {
            float ax = a % W + 0.5f, az = a / W + 0.5f, bx = b % W + 0.5f, bz = b / W + 0.5f;
            float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 3f));
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                float x = ax + (bx - ax) * t, z = az + (bz - az) * t;
                // the cells either side of the line too, so the walker's body doesn't clip a corner
                for (int k = 0; k < 4; k++)
                {
                    int cx = Mathf.FloorToInt(x + (k == 1 ? 0.3f : k == 2 ? -0.3f : 0f)), cz = Mathf.FloorToInt(z + (k == 3 ? 0.3f : k == 0 ? -0.3f : 0f));
                    if (cx < 0 || cz < 0 || cx >= W || cz >= H || grid[cx + cz * W] != 0) return false;
                }
            }
            return true;
        }
    }
}
