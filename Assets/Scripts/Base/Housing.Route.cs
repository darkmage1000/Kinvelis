using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Finding the way round the house (user, 2026-10-14: kin got stuck against house walls). Anything that walks by
    /// itself (camp kin, your companion, wild kin) asks `Steer` where to head next on its way to a point. Far from the
    /// house, or with nothing in the way, that is the point itself. Otherwise a search over the house grid - 3 m squares
    /// on each storey - finds a way: walls and window walls block, doorways let through anything that fits the gap,
    /// stairs are entered only at their foot and lead to the square their top opens onto, upstairs only floors can be
    /// walked on. The walker heads for the furthest square centre on its own storey it can walk straight to. The path
    /// is kept per walker (`Route`) and searched again when the house changes, the goal moves, the walker strays off
    /// it, or every 2 s. No way through = straight at the point, as before.
    /// </summary>
    public partial class Housing
    {
        public class Route
        {
            public Vector3 goal;
            public int ver = -1;
            public float until;
            public bool ok;
            public readonly List<int> nodes = new List<int>();   // packed (x, z, storey) from the square after the start to the goal
        }

        // ---- bounds of everything built, in squares (a quick "is the house anywhere near" test)
        int bx0, bx1, bz0, bz1;
        bool anyBuilt;

        void RecalcBounds()
        {
            anyBuilt = false;
            void Grow(int x, int z)
            {
                if (!anyBuilt) { bx0 = bx1 = x; bz0 = bz1 = z; anyBuilt = true; return; }
                if (x < bx0) bx0 = x; if (x > bx1) bx1 = x; if (z < bz0) bz0 = z; if (z > bz1) bz1 = z;
            }
            foreach (var f in floors.Values) Grow(f.cx, f.cz);
            foreach (var s in stairs.Values) Grow(s.cx, s.cz);
        }

        /// <summary>Does a walk from a to b come anywhere near the house (with walls, stairs or upper floors to mind)?</summary>
        bool Near(Vector3 a, Vector3 b)
        {
            if (!anyBuilt || (segs.Count == 0 && floors.Count == 0)) return false;
            float x0 = (bx0 - 1) * Cell, x1 = (bx1 + 2) * Cell, z0 = (bz0 - 1) * Cell, z1 = (bz1 + 2) * Cell;
            return Mathf.Max(a.x, b.x) > x0 && Mathf.Min(a.x, b.x) < x1 && Mathf.Max(a.z, b.z) > z0 && Mathf.Min(a.z, b.z) < z1;
        }

        /// <summary>Where to head next on the way from `from` to `to` for a walker of radius r (its collision radius).</summary>
        public Vector3 Steer(Route rt, Vector3 from, Vector3 to, float r)
        {
            if (rt == null || !Near(from, to)) return to;
            int la = LevelAt(from), lb = LevelAt(to);
            if (la == 0 && lb == 0 && Clear(from, to, r, from.y, 0)) { rt.ok = false; return to; }   // nothing in the way
            int here = NodeAt(from);
            if (rt.ver != Version || Time.time > rt.until || (rt.goal - to).sqrMagnitude > 1f || (rt.ok && !rt.nodes.Contains(here) && !Leads(rt, here)))
            {
                rt.ver = Version; rt.goal = to; rt.until = Time.time + 2f;
                rt.ok = FindPath(from, to, r, rt.nodes);
            }
            if (!rt.ok || rt.nodes.Count == 0) return to;
            // where along the path the walker is (-1 = still in the square it started from)
            int j = rt.nodes.LastIndexOf(here);
            if (j == rt.nodes.Count - 1) return to;   // in the goal's square: straight there
            int next = j + 1;
            Vector3 aim = NodePoint(rt.nodes[next]);
            if (Unpack(rt.nodes[next], out _, out _) == la && !Clear(from, aim, r, from.y, la))
            {
                // a doorway or the foot of the stairs is not in line from here: go to the middle of this square first
                Unpack(here, out int hx, out int hz);
                Vector3 c = CellCenter(hx, hz);
                if (Flat(c - from) > 0.3f) return new Vector3(c.x, from.y, c.z);
                return aim;
            }
            // cut corners: the furthest square on this storey in a straight, clear line
            for (int i = next + 1; i < rt.nodes.Count; i++)
            {
                int n = rt.nodes[i];
                if (Unpack(n, out _, out _) != la || IsStairNode(n) || IsStairNode(rt.nodes[i - 1])) break;
                Vector3 p = i == rt.nodes.Count - 1 ? to : NodePoint(n);
                if (!Clear(from, p, r, from.y, la)) break;
                aim = p;
            }
            return aim;
        }

        /// <summary>The walker is in the square the path starts from (it hasn't reached the first step yet).</summary>
        bool Leads(Route rt, int here) => rt.nodes.Count > 0 && Adjacent(here, rt.nodes[0]);
        static bool Adjacent(int a, int b)
        {
            Unpack(a, out int ax, out int az); Unpack(b, out int bx, out int bz);
            return Mathf.Abs(ax - bx) + Mathf.Abs(az - bz) <= 1;
        }

        /// <summary>Can a walker of radius r get from a to b at all (beds and play pieces upstairs or behind walls)?</summary>
        public bool Reachable(Vector3 a, Vector3 b, float r)
        {
            if (!Near(a, b)) return true;
            if (LevelAt(a) == 0 && LevelAt(b) == 0 && Clear(a, b, r, a.y, 0)) return true;
            return FindPath(a, b, r, scratchPath);
        }
        readonly List<int> scratchPath = new List<int>();

        readonly Dictionary<Piece, bool> reachCache = new Dictionary<Piece, bool>();
        int reachVer = -1;
        /// <summary>Can a camp kin walk from the Hearthstone to this piece? (Kept until the house changes.)</summary>
        public bool ReachableFromCamp(Piece pc)
        {
            var bm = BaseManager.I;
            if (pc == null || pc.removed || bm == null || !bm.HasBase) return false;
            if (reachVer != Version) { reachCache.Clear(); reachVer = Version; }
            if (reachCache.TryGetValue(pc, out bool ok)) return ok;
            Vector3 core = bm.CorePosition; core.y = Ground.Height(core.x, core.z);
            ok = Reachable(core, pc.pos + Vector3.up * 0.1f, 0.42f);
            reachCache[pc] = ok;
            return ok;
        }

        // ============================================================ THE GRID
        // a node = square (x, z) on a storey, packed into an int: 13 bits each for x and z (offset), 2 for the storey
        const int Off = 4096;
        static int Pack(int x, int z, int l) => ((x + Off) & 0x1FFF) | (((z + Off) & 0x1FFF) << 13) | (l << 26);
        static int Unpack(int n, out int x, out int z) { x = (n & 0x1FFF) - Off; z = ((n >> 13) & 0x1FFF) - Off; return n >> 26; }

        int NodeAt(Vector3 p) => Pack(CellOf(p.x), CellOf(p.z), LevelAt(p));

        bool IsStairNode(int n) { int l = Unpack(n, out int x, out int z); return StairsAt(x, z, l) != null; }

        /// <summary>Where a walker aims for a node: the middle of the square at that storey's height (halfway up for stairs).</summary>
        Vector3 NodePoint(int n)
        {
            int l = Unpack(n, out int x, out int z);
            var c = CellCenter(x, z);
            var st = StairsAt(x, z, l);
            if (st != null) c.y = st.Y + LevelH * 0.5f;
            else if (l > 0 && FloorTop(x, z, l, out float y)) c.y = y;
            else c.y = Ground.Height(c.x, c.z);
            return c;
        }

        bool Walkable(int x, int z, int l) => l == 0 || floors.ContainsKey(LKey(x, z, l));

        /// <summary>Does the edge between a square and its neighbour (direction dir) stop a walker of radius r on this storey?</summary>
        bool EdgeBlocks(int x, int z, int dir, int l, float r)
        {
            var e = EdgeAt(x, z, dir, l);
            if (e == null) return false;
            return e.def.kind != HouseKind.Doorway || r > DoorGap * 0.5f - WallT * 0.5f - 0.04f;
        }

        static readonly Queue<int> open = new Queue<int>();
        static readonly Dictionary<int, int> came = new Dictionary<int, int>();

        /// <summary>Breadth-first over squares and storeys, inside the house's bounds grown to take in both ends. Fills `path` (start excluded).</summary>
        bool FindPath(Vector3 from, Vector3 to, float r, List<int> path)
        {
            path.Clear();
            int start = NodeAt(from), goal = NodeAt(to);
            if (start == goal) return true;
            int gl = Unpack(goal, out int gx, out int gz);
            Unpack(start, out int sx, out int sz);
            int x0 = Mathf.Min(bx0, Mathf.Min(sx, gx)) - 2, x1 = Mathf.Max(bx1, Mathf.Max(sx, gx)) + 2;
            int z0 = Mathf.Min(bz0, Mathf.Min(sz, gz)) - 2, z1 = Mathf.Max(bz1, Mathf.Max(sz, gz)) + 2;
            if ((x1 - x0 + 1) * (z1 - z0 + 1) > 4000) return false;   // a long walk across camp: straight is fine
            if (gl > 0 && !Walkable(gx, gz, gl) && StairsAt(gx, gz, gl) == null) return false;
            bool fitsStairs = r <= StairW * 0.5f - 0.15f;
            open.Clear(); came.Clear();
            open.Enqueue(start); came[start] = start;
            bool found = false;
            while (open.Count > 0)
            {
                int n = open.Dequeue();
                if (n == goal) { found = true; break; }
                int l = Unpack(n, out int x, out int z);
                var st = StairsAt(x, z, l);
                for (int k = 0; k < 4; k++)
                {
                    if (st != null && k != (st.rot + 2) % 4) continue;   // stairs are left at their foot (or up at their top, below)
                    int nx = x + DX[k], nz = z + DZ[k];
                    if (nx < x0 || nx > x1 || nz < z0 || nz > z1) continue;
                    var nst = StairsAt(nx, nz, l);
                    if (nst != null && (k != nst.rot || !fitsStairs)) continue;   // and entered only at their foot, facing up
                    if (!Walkable(nx, nz, l) || EdgeBlocks(x, z, k, l, r)) continue;
                    Visit(n, Pack(nx, nz, l));
                }
                // up a flight to the square its top opens onto
                if (st != null && fitsStairs && l < MaxLevel)
                {
                    int hx = x + DX[st.rot], hz = z + DZ[st.rot];
                    if (Walkable(hx, hz, l + 1) && !EdgeBlocks(x, z, st.rot, l + 1, r)) Visit(n, Pack(hx, hz, l + 1));
                }
                // down a flight whose top opens onto this square
                if (l > 0 && fitsStairs)
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + DX[k], nz = z + DZ[k];
                        var down = StairsAt(nx, nz, l - 1);
                        if (down == null || nx + DX[down.rot] != x || nz + DZ[down.rot] != z) continue;
                        if (EdgeBlocks(x, z, k, l, r)) continue;
                        Visit(n, Pack(nx, nz, l - 1));
                    }
            }
            if (!found) return false;
            for (int n = goal; n != start; n = came[n]) path.Add(n);
            path.Reverse();
            return true;
        }

        static void Visit(int from, int to) { if (came.ContainsKey(to)) return; came[to] = from; open.Enqueue(to); }

        /// <summary>
        /// Can a walker of radius r go straight from a to b at height y on storey l? No wall, stair side or railing in
        /// the way at that height, and upstairs, floor under every step.
        /// </summary>
        bool Clear(Vector3 a, Vector3 b, float r, float y, int l)
        {
            Vector2 p = new Vector2(a.x, a.z), q = new Vector2(b.x, b.z);
            int cx0 = CellOf(Mathf.Min(a.x, b.x)) - 1, cx1 = CellOf(Mathf.Max(a.x, b.x)) + 1;
            int cz0 = CellOf(Mathf.Min(a.z, b.z)) - 1, cz1 = CellOf(Mathf.Max(a.z, b.z)) + 1;
            if ((cx1 - cx0 + 1) * (cz1 - cz0 + 1) > 900) return true;
            float min = r + WallT * 0.5f - 0.02f, yy = y + 0.3f;
            for (int x = cx0; x <= cx1; x++)
                for (int z = cz0; z <= cz1; z++)
                {
                    if (!segs.TryGetValue(Key(x, z), out var list)) continue;
                    foreach (var s in list)
                        if (yy >= s.lo && yy <= s.hi && SegDist(p, q, s.a, s.b) < min) return false;
                }
            if (l > 0)
            {
                float len = (q - p).magnitude;
                int steps = Mathf.Max(1, Mathf.CeilToInt(len / 0.5f));
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 m = Vector2.Lerp(p, q, i / (float)steps);
                    int x = CellOf(m.x), z = CellOf(m.y);
                    if (!floors.ContainsKey(LKey(x, z, l)) && StairsAt(x, z, l) == null) return false;
                }
            }
            return true;
        }

        /// <summary>Shortest distance between two flat segments.</summary>
        static float SegDist(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d1 = Cross(q2 - q1, p1 - q1), d2 = Cross(q2 - q1, p2 - q1), d3 = Cross(p2 - p1, q1 - p1), d4 = Cross(p2 - p1, q2 - p1);
            if (d1 * d2 < 0f && d3 * d4 < 0f) return 0f;
            return Mathf.Min(Mathf.Min(DistSeg(p1, q1, q2), DistSeg(p2, q1, q2)), Mathf.Min(DistSeg(q1, p1, p2), DistSeg(q2, p1, p2)));
        }
    }
}
