using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    public enum HouseKind { Floor, Wall, Doorway, Window, Roof, RoofSlope, Furniture }

    [Serializable] public class HouseSave { public string id; public int cx, cz, side, rot; public float x, y, z; }

    public class HousePieceDef
    {
        public string id, name, desc;
        public HouseKind kind;
        public Ingredient[] cost;
        public float radius;          // furniture: collision circle
        public bool stone, bed, wardrobe, lamp;
    }

    /// <summary>
    /// Your house, piece by piece (user, 2026-10-14: "we need player house building, i just realized i cant sleep
    /// thru the night" - chose snap-together pieces). Pieces snap to a world grid of `Cell` 3 m squares inside the
    /// camp ring: floors fill a square (their top becomes the ground there - `FloorAt` feeds `Ground.Height`), walls,
    /// doorways and window walls sit on the edge between two squares (`side` 0 = the north edge of (cx, cz), 1 = its
    /// east edge) and need a floor beside them, roofs cover a floored square at wall height, furniture snaps to a
    /// 0.5 m grid on a floor. Collision: wall segments (`PushOut`, called from `WorldCollision.PushOut`; a doorway
    /// leaves a 1.2 m gap) and furniture circles (ordinary obstacles). While you are under a roof the roofs nearby and
    /// any wall between the camera and you are hidden, and doors swing open when someone comes near. A bed with a roof
    /// over it lets you SLEEP (GameManager.Sleep). Saved as `SaveData.house`.
    /// </summary>
    public class Housing : MonoBehaviour
    {
        public static Housing I { get; private set; }
        public const float Cell = 3f, WallH = 3f, WallT = 0.22f, DoorGap = 1.2f;

        // ============================================================ PIECES
        static Ingredient[] C(params (string, int)[] c) { var r = new Ingredient[c.Length]; for (int i = 0; i < c.Length; i++) r[i] = new Ingredient(c[i].Item1, c[i].Item2); return r; }

        public static readonly List<HousePieceDef> Pieces = new List<HousePieceDef>
        {
            new HousePieceDef { id = "house_floor", name = "Wooden Floor", kind = HouseKind.Floor, cost = C(("wood", 8), ("stone", 4)), desc = "A 3 m square of planks on a stone footing. Everything else stands on floors." },
            new HousePieceDef { id = "house_wall", name = "Wooden Wall", kind = HouseKind.Wall, cost = C(("wood", 6)), desc = "A plank wall along one edge of a floor." },
            new HousePieceDef { id = "house_wall_stone", name = "Stone Wall", kind = HouseKind.Wall, stone = true, cost = C(("stone", 10), ("wood", 2)), desc = "A sturdier-looking wall of fitted stone." },
            new HousePieceDef { id = "house_door", name = "Doorway", kind = HouseKind.Doorway, cost = C(("wood", 8)), desc = "A wall with a door that swings open as you come near." },
            new HousePieceDef { id = "house_window", name = "Window Wall", kind = HouseKind.Window, cost = C(("wood", 6), ("fiber", 2)), desc = "A wall with a window in it." },
            new HousePieceDef { id = "house_roof", name = "Flat Roof", kind = HouseKind.Roof, cost = C(("wood", 6), ("fiber", 4)), desc = "A roof over one floor square. A bed needs a roof over it to sleep in." },
            new HousePieceDef { id = "house_roof_slope", name = "Sloped Roof", kind = HouseKind.RoofSlope, cost = C(("wood", 6), ("fiber", 4)), desc = "A roof that slopes one way (ROTATE turns it). Two back to back make a ridge." },
            new HousePieceDef { id = "house_bed", name = "Bed", kind = HouseKind.Furniture, bed = true, radius = 0.75f, cost = C(("wood", 10), ("fiber", 12), ("pelt", 2)), desc = "SLEEP here from evening on to skip to morning (needs a roof over it). The camp keeps working while you sleep." },
            new HousePieceDef { id = "house_table", name = "Table", kind = HouseKind.Furniture, radius = 0.6f, cost = C(("wood", 6)), desc = "A sturdy table." },
            new HousePieceDef { id = "house_chair", name = "Chair", kind = HouseKind.Furniture, radius = 0.3f, cost = C(("wood", 3)), desc = "A chair." },
            new HousePieceDef { id = "house_lamp", name = "Lamp", kind = HouseKind.Furniture, lamp = true, radius = 0.25f, cost = C(("wood", 2), ("gel", 1)), desc = "A standing lamp that glows." },
            new HousePieceDef { id = "house_rug", name = "Rug", kind = HouseKind.Furniture, radius = 0f, cost = C(("fiber", 6)), desc = "A woven rug. You can walk over it." },
            new HousePieceDef { id = "house_shelf", name = "Bookshelf", kind = HouseKind.Furniture, radius = 0.45f, cost = C(("wood", 8)), desc = "Shelves of books and keepsakes." },
            new HousePieceDef { id = "house_wardrobe", name = "Wardrobe", kind = HouseKind.Furniture, wardrobe = true, radius = 0.45f, cost = C(("wood", 10), ("fiber", 4)), desc = "CHANGE LOOK here: your explorer's body, hair and outfit." },
        };

        public static HousePieceDef Def(string id) { foreach (var p in Pieces) if (p.id == id) return p; return null; }

        public class Piece
        {
            public HousePieceDef def;
            public int cx, cz, side, rot;
            public Vector3 pos;
            public GameObject go;
            public Obstacle obs;
            public Transform door;
            public float doorOpen;
            public Renderer[] rends;
            public bool hidden;
            public float Y => pos.y;
        }

        readonly Dictionary<long, Piece> floors = new Dictionary<long, Piece>();
        readonly Dictionary<long, Piece> roofs = new Dictionary<long, Piece>();
        readonly Dictionary<long, Piece>[] edges = { new Dictionary<long, Piece>(), new Dictionary<long, Piece>() };
        readonly List<Piece> furniture = new List<Piece>();
        public int Count => floors.Count + roofs.Count + edges[0].Count + edges[1].Count + furniture.Count;

        public static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
        public static int CellOf(float v) => Mathf.FloorToInt(v / Cell);
        public static Vector3 CellCenter(int cx, int cz) => new Vector3((cx + 0.5f) * Cell, 0f, (cz + 0.5f) * Cell);

        // ---- floors as ground: published as a fresh dictionary on every change, so any thread can read it
        static volatile Dictionary<long, float> floorSnap = new Dictionary<long, float>();
        /// <summary>The top of a floor at this point, or -infinity (Ground.Height takes the higher of this and the terrain).</summary>
        public static float FloorAt(float x, float z) => floorSnap.TryGetValue(Key(CellOf(x), CellOf(z)), out var y) ? y : float.NegativeInfinity;
        void Publish()
        {
            var d = new Dictionary<long, float>();
            foreach (var kv in floors) d[kv.Key] = kv.Value.Y;
            floorSnap = d;
        }

        // ---- wall segments, indexed by the two squares beside them
        struct Seg { public Vector2 a, b; public float y0, y1; }
        readonly Dictionary<long, List<Seg>> segs = new Dictionary<long, List<Seg>>();

        public void Init() { I = this; floorSnap = new Dictionary<long, float>(); }

        // ============================================================ PLACING
        public struct Spot { public bool ok; public string reason; public int cx, cz, side, rot; public Vector3 pos; public Quaternion q; public float depth; }

        /// <summary>Where a piece would go for a player standing at p facing fwd (turn = ROTATE presses).</summary>
        public Spot Snap(HousePieceDef def, Vector3 p, Vector3 fwd, int turn)
        {
            var s = new Spot { ok = false };
            var bm = BaseManager.I;
            fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            float reach = def.kind == HouseKind.Floor ? 3.2f : def.kind == HouseKind.Furniture ? 1.7f : def.kind == HouseKind.Roof || def.kind == HouseKind.RoofSlope ? 2.6f : 2.1f;
            Vector3 t = p + fwd * reach;
            int cx = CellOf(t.x), cz = CellOf(t.z);
            s.cx = cx; s.cz = cz;
            s.rot = ((turn % 4) + 4) % 4;
            string Fail(string why) { s.reason = why; return why; }
            if (bm == null || !bm.HasBase) { Fail("Place a Hearthstone first - houses go inside your camp"); }
            switch (def.kind)
            {
                case HouseKind.Floor:
                {
                    var c = CellCenter(cx, cz);
                    float hi = float.MinValue, lo = float.MaxValue;
                    for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) { float h = WorldGen.Height(c.x + i * Cell * 0.5f, c.z + j * Cell * 0.5f); hi = Mathf.Max(hi, h); lo = Mathf.Min(lo, h); }
                    float y = hi + 0.3f;
                    // flush with a neighbouring floor when the ground allows it
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = cx + (k == 0 ? 1 : k == 1 ? -1 : 0), nz = cz + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (floors.TryGetValue(Key(nx, nz), out var nb) && nb.Y >= hi + 0.12f && nb.Y - hi < 1.6f) y = Mathf.Max(y, nb.Y);
                    }
                    s.pos = new Vector3(c.x, y, c.z); s.q = Quaternion.identity; s.depth = y - lo + 0.5f;
                    if (s.reason != null) break;
                    if (floors.ContainsKey(Key(cx, cz))) { Fail("There's a floor here already"); break; }
                    if (hi - lo > 2.2f) { Fail("Too steep here"); break; }
                    if (lo < WorldGen.WaterLevel + 0.15f) { Fail("Too wet here"); break; }
                    if (!bm.IsInsideBase(c, 1.5f)) { Fail("Inside your camp ring only"); break; }
                    if (WorldCollision.Overlaps(c, 1.45f, true)) { Fail("Something is in the way"); break; }
                    s.ok = true;
                    break;
                }
                case HouseKind.Wall:
                case HouseKind.Doorway:
                case HouseKind.Window:
                {
                    // the nearest edge of the square in front of you; ROTATE steps round its four edges
                    float u = t.x - cx * Cell, v = t.z - cz * Cell;
                    float[] d = { Cell - v, Cell - u, v, u };   // north, east, south, west
                    int best = 0; for (int i = 1; i < 4; i++) if (d[i] < d[best]) best = i;
                    int e = (best + s.rot) % 4;
                    int ex = cx, ez = cz, side = 0;
                    if (e == 0) { side = 0; } else if (e == 1) { side = 1; } else if (e == 2) { side = 0; ez = cz - 1; } else { side = 1; ex = cx - 1; }
                    s.cx = ex; s.cz = ez; s.side = side;
                    Vector3 mid = side == 0 ? new Vector3((ex + 0.5f) * Cell, 0f, (ez + 1) * Cell) : new Vector3((ex + 1) * Cell, 0f, (ez + 0.5f) * Cell);
                    float y = EdgeBase(ex, ez, side);
                    s.pos = new Vector3(mid.x, float.IsNegativeInfinity(y) ? WorldGen.Height(mid.x, mid.z) : y, mid.z);
                    s.q = Quaternion.Euler(0f, side == 0 ? 0f : 90f, 0f);
                    if (s.reason != null) break;
                    if (float.IsNegativeInfinity(y)) { Fail("Walls go on the edge of a floor"); break; }
                    if (edges[side].ContainsKey(Key(ex, ez))) { Fail("There's a wall here already"); break; }
                    if (FurnitureNearSegment(mid, side, 0.15f)) { Fail("Furniture is in the way"); break; }
                    s.ok = true;
                    break;
                }
                case HouseKind.Roof:
                case HouseKind.RoofSlope:
                {
                    var c = CellCenter(cx, cz);
                    floors.TryGetValue(Key(cx, cz), out var f);
                    s.pos = new Vector3(c.x, (f != null ? f.Y : WorldGen.Height(c.x, c.z)) + WallH, c.z);
                    s.q = Quaternion.Euler(0f, s.rot * 90f, 0f);
                    if (s.reason != null) break;
                    if (f == null) { Fail("Roofs go over a floor"); break; }
                    if (roofs.ContainsKey(Key(cx, cz))) { Fail("There's a roof here already"); break; }
                    s.ok = true;
                    break;
                }
                default:
                {
                    Vector3 q = new Vector3(Mathf.Round(t.x * 2f) * 0.5f, 0f, Mathf.Round(t.z * 2f) * 0.5f);
                    float fy = FloorAt(q.x, q.z);
                    s.cx = CellOf(q.x); s.cz = CellOf(q.z);
                    s.pos = new Vector3(q.x, float.IsNegativeInfinity(fy) ? WorldGen.Height(q.x, q.z) : fy, q.z);
                    s.q = Quaternion.Euler(0f, s.rot * 90f, 0f);
                    if (s.reason != null) break;
                    if (float.IsNegativeInfinity(fy)) { Fail("Furniture goes on a floor"); break; }
                    // keep clear of walls and of other furniture
                    if (WallClearance(s.pos) < Mathf.Max(0.25f, def.radius * 0.8f)) { Fail("Too close to a wall"); break; }
                    bool hit = false;
                    foreach (var o in furniture)
                    {
                        if (o.def.radius <= 0f || def.radius <= 0f) { if (o.def.id == def.id && (o.pos - s.pos).sqrMagnitude < 0.3f) hit = true; continue; }
                        float m = o.def.radius + def.radius - 0.1f;
                        if ((o.pos - s.pos).sqrMagnitude < m * m) hit = true;
                    }
                    if (hit) { Fail("Something is in the way"); break; }
                    s.ok = true;
                    break;
                }
            }
            if (s.ok && Caves.InCave) { s.ok = false; s.reason = "Not underground"; }
            return s;
        }

        /// <summary>The floor height an edge stands on (the higher floor beside it), or -infinity if neither side has one.</summary>
        float EdgeBase(int cx, int cz, int side)
        {
            float y = float.NegativeInfinity;
            if (floors.TryGetValue(Key(cx, cz), out var a)) y = a.Y;
            int nx = side == 1 ? cx + 1 : cx, nz = side == 0 ? cz + 1 : cz;
            if (floors.TryGetValue(Key(nx, nz), out var b)) y = Mathf.Max(y, b.Y);
            return y;
        }

        bool FurnitureNearSegment(Vector3 mid, int side, float pad)
        {
            Vector2 dir = side == 0 ? Vector2.right : Vector2.up;
            Vector2 m = new Vector2(mid.x, mid.z), a = m - dir * Cell * 0.5f, b = m + dir * Cell * 0.5f;
            foreach (var o in furniture)
            {
                if (o.def.radius <= 0f) continue;
                if (DistSeg(new Vector2(o.pos.x, o.pos.z), a, b) < o.def.radius + WallT * 0.5f + pad) return true;
            }
            return false;
        }

        float WallClearance(Vector3 p)
        {
            float best = 99f;
            Vector2 q = new Vector2(p.x, p.z);
            int cx = CellOf(p.x), cz = CellOf(p.z);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (segs.TryGetValue(Key(cx + i, cz + j), out var list))
                        foreach (var s in list) best = Mathf.Min(best, DistSeg(q, s.a, s.b) - WallT * 0.5f);
            return best;
        }

        static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        public Piece Place(HousePieceDef def, Spot s)
        {
            var pc = new Piece { def = def, cx = s.cx, cz = s.cz, side = s.side, rot = s.rot, pos = s.pos };
            Build(pc, s.depth);
            Register(pc);
            return pc;
        }

        void Register(Piece pc)
        {
            switch (pc.def.kind)
            {
                case HouseKind.Floor: floors[Key(pc.cx, pc.cz)] = pc; Publish(); break;
                case HouseKind.Roof: case HouseKind.RoofSlope: roofs[Key(pc.cx, pc.cz)] = pc; break;
                case HouseKind.Furniture:
                    furniture.Add(pc);
                    if (pc.def.radius > 0f) pc.obs = WorldCollision.AddObstacle(pc.pos, pc.def.radius, pc.go);
                    break;
                default: edges[pc.side][Key(pc.cx, pc.cz)] = pc; RebuildSegs(); break;
            }
        }

        void RebuildSegs()
        {
            segs.Clear();
            for (int side = 0; side < 2; side++)
                foreach (var pc in edges[side].Values)
                {
                    Vector2 dir = side == 0 ? Vector2.right : Vector2.up;
                    Vector2 m = new Vector2(pc.pos.x, pc.pos.z), a = m - dir * Cell * 0.5f, b = m + dir * Cell * 0.5f;
                    float y0 = pc.Y - 0.5f, y1 = pc.Y + WallH;
                    var list = new List<Seg>();
                    if (pc.def.kind == HouseKind.Doorway)
                    {
                        float w = (Cell - DoorGap) * 0.5f;
                        list.Add(new Seg { a = a, b = a + dir * w, y0 = y0, y1 = y1 });
                        list.Add(new Seg { a = b - dir * w, b = b, y0 = y0, y1 = y1 });
                    }
                    else list.Add(new Seg { a = a, b = b, y0 = y0, y1 = y1 });
                    int nx = side == 1 ? pc.cx + 1 : pc.cx, nz = side == 0 ? pc.cz + 1 : pc.cz;
                    foreach (var k in new[] { Key(pc.cx, pc.cz), Key(nx, nz) })
                    {
                        if (!segs.TryGetValue(k, out var l)) segs[k] = l = new List<Seg>();
                        l.AddRange(list);
                    }
                }
        }

        /// <summary>The piece nearest a point in front of the player (REMOVE A PIECE): furniture first, then walls, roofs, floors.</summary>
        public Piece PieceAt(Vector3 p, Vector3 fwd)
        {
            fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            Vector3 t = p + fwd * 1.8f;
            Piece best = null; float bd = 1.6f;
            foreach (var f in furniture) { float d = Flat(f.pos - t); if (d < bd) { bd = d; best = f; } }
            if (best != null) return best;
            bd = 1.3f;
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values) { float d = Flat(w.pos - t); if (d < bd) { bd = d; best = w; } }
            if (best != null) return best;
            int cx = CellOf(t.x), cz = CellOf(t.z);
            if (roofs.TryGetValue(Key(cx, cz), out var r) && p.y < r.Y) return r;
            return floors.TryGetValue(Key(cx, cz), out var fl) ? fl : null;
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>Why a piece can't come down yet (null = it can).</summary>
        public string RemoveBlock(Piece pc)
        {
            if (pc == null) return "Nothing here to remove";
            if (pc.def.kind != HouseKind.Floor) return null;
            if (roofs.ContainsKey(Key(pc.cx, pc.cz))) return "Take the roof off first";
            foreach (var f in furniture) if (f.cx == pc.cx && f.cz == pc.cz) return "Move the furniture off first";
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values)
                {
                    bool touches = (w.cx == pc.cx && w.cz == pc.cz) || (side == 0 && w.cx == pc.cx && w.cz + 1 == pc.cz) || (side == 1 && w.cx + 1 == pc.cx && w.cz == pc.cz);
                    if (!touches) continue;
                    int ox = side == 1 ? (w.cx == pc.cx ? w.cx + 1 : w.cx) : w.cx, oz = side == 0 ? (w.cz == pc.cz ? w.cz + 1 : w.cz) : w.cz;
                    if (!floors.ContainsKey(Key(ox, oz))) return "Take down the walls that stand on it first";
                }
            return null;
        }

        public void Remove(Piece pc)
        {
            if (pc == null) return;
            switch (pc.def.kind)
            {
                case HouseKind.Floor: floors.Remove(Key(pc.cx, pc.cz)); Publish(); break;
                case HouseKind.Roof: case HouseKind.RoofSlope: roofs.Remove(Key(pc.cx, pc.cz)); break;
                case HouseKind.Furniture: furniture.Remove(pc); WorldCollision.RemoveObstacle(pc.obs); break;
                default: edges[pc.side].Remove(Key(pc.cx, pc.cz)); RebuildSegs(); break;
            }
            if (pc.go != null) Destroy(pc.go);
        }

        // ============================================================ COLLISION
        /// <summary>Pushes a circle out of every wall it overlaps (called from WorldCollision.PushOut).</summary>
        public static bool PushOut(ref Vector3 pos, float r)
        {
            var h = I;
            if (h == null || h.segs.Count == 0) return false;
            int cx = CellOf(pos.x), cz = CellOf(pos.z);
            bool hit = false;
            Vector2 p = new Vector2(pos.x, pos.z);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (!h.segs.TryGetValue(Key(cx + i, cz + j), out var list)) continue;
                    foreach (var s in list)
                    {
                        if (pos.y > s.y1 + 0.2f || pos.y < s.y0 - 1.5f) continue;
                        Vector2 ab = s.b - s.a;
                        float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                        Vector2 q = s.a + ab * t, d = p - q;
                        float min = r + WallT * 0.5f, len = d.magnitude;
                        if (len >= min) continue;
                        if (len < 1e-4f) { d = new Vector2(-ab.y, ab.x).normalized; len = 0f; } else d /= len;
                        p += d * (min - len);
                        hit = true;
                    }
                }
            if (hit) { pos.x = p.x; pos.z = p.y; }
            return hit;
        }

        /// <summary>True if a circle at pos overlaps any house piece (camp structures may not be placed on a house).</summary>
        public bool Occupies(Vector3 pos, float r)
        {
            int c0x = CellOf(pos.x - r), c1x = CellOf(pos.x + r), c0z = CellOf(pos.z - r), c1z = CellOf(pos.z + r);
            for (int x = c0x; x <= c1x; x++) for (int z = c0z; z <= c1z; z++) if (floors.ContainsKey(Key(x, z))) return true;
            return false;
        }

        // ============================================================ SLEEP
        public Piece BedNear(Vector3 p, float reach)
        {
            Piece best = null; float bd = reach;
            foreach (var f in furniture) { if (!f.def.bed) continue; float d = Flat(f.pos - p) - f.def.radius; if (d < bd) { bd = d; best = f; } }
            return best;
        }

        public Piece WardrobeNear(Vector3 p, float reach)
        {
            Piece best = null; float bd = reach;
            foreach (var f in furniture) { if (!f.def.wardrobe) continue; float d = Flat(f.pos - p) - f.def.radius; if (d < bd) { bd = d; best = f; } }
            return best;
        }

        /// <summary>A floor under it and a roof over it.</summary>
        public bool Indoors(Vector3 p)
        {
            long k = Key(CellOf(p.x), CellOf(p.z));
            return floors.ContainsKey(k) && roofs.ContainsKey(k);
        }

        // ============================================================ EVERY FRAME
        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null || Count == 0) return;
            Vector3 pp = gm.Player.Position;
            var cam = Camera.main;
            bool inside = Indoors(pp) && pp.y < RoofY(pp) - 0.5f;
            Vector2 a = new Vector2(pp.x, pp.z), b = cam != null ? new Vector2(cam.transform.position.x, cam.transform.position.z) : a;
            foreach (var r in roofs.Values) SetHidden(r, inside && Flat(r.pos - pp) < 16f);
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values)
                {
                    bool hide = false;
                    if (inside && Flat(w.pos - pp) < 14f)
                    {
                        Vector2 dir = side == 0 ? Vector2.right : Vector2.up, m = new Vector2(w.pos.x, w.pos.z);
                        hide = SegmentsCross(a, b, m - dir * Cell * 0.5f, m + dir * Cell * 0.5f);
                    }
                    SetHidden(w, hide);
                    if (w.door != null)
                    {
                        bool near = Flat(w.pos - pp) < 2.4f;
                        if (!near && BaseManager.I != null) foreach (var k in BaseManager.I.WorkerActors) if (k != null && Flat(k.Position - w.pos) < 2f) { near = true; break; }
                        if (!near && gm.Party.Companion != null && Flat(gm.Party.Companion.Position - w.pos) < 2f) near = true;
                        float want = near ? 1f : 0f;
                        if (Mathf.Abs(w.doorOpen - want) > 0.001f)
                        {
                            if (want > w.doorOpen && w.doorOpen < 0.02f) SynthAudio.Play(Sfx.Clunk, 1.6f, 0.35f);
                            w.doorOpen = Mathf.MoveTowards(w.doorOpen, want, Time.deltaTime * 3f);
                            float e = w.doorOpen * w.doorOpen * (3f - 2f * w.doorOpen);
                            w.door.localRotation = Quaternion.Euler(0f, -100f * e, 0f);
                        }
                    }
                }
        }

        float RoofY(Vector3 p) => roofs.TryGetValue(Key(CellOf(p.x), CellOf(p.z)), out var r) ? r.Y : float.PositiveInfinity;

        static void SetHidden(Piece pc, bool hide)
        {
            if (pc.hidden == hide || pc.rends == null) return;
            pc.hidden = hide;
            foreach (var r in pc.rends) if (r != null) r.enabled = !hide;
        }

        static bool SegmentsCross(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d1 = Cross(q2 - q1, p1 - q1), d2 = Cross(q2 - q1, p2 - q1), d3 = Cross(p2 - p1, q1 - p1), d4 = Cross(p2 - p1, q2 - p1);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // ============================================================ SAVE
        public List<HouseSave> Save()
        {
            var l = new List<HouseSave>();
            void Add(Piece p) => l.Add(new HouseSave { id = p.def.id, cx = p.cx, cz = p.cz, side = p.side, rot = p.rot, x = p.pos.x, y = p.pos.y, z = p.pos.z });
            foreach (var p in floors.Values) Add(p);
            for (int s = 0; s < 2; s++) foreach (var p in edges[s].Values) Add(p);
            foreach (var p in roofs.Values) Add(p);
            foreach (var p in furniture) Add(p);
            return l;
        }

        public void Load(List<HouseSave> list)
        {
            if (list == null) return;
            // floors first: everything else stands on them
            foreach (var pass in new[] { 0, 1 })
                foreach (var s in list)
                {
                    var def = Def(s.id);
                    if (def == null || (def.kind == HouseKind.Floor) != (pass == 0)) continue;
                    var pc = new Piece { def = def, cx = s.cx, cz = s.cz, side = s.side, rot = s.rot, pos = new Vector3(s.x, s.y, s.z) };
                    float lo = s.y;
                    if (def.kind == HouseKind.Floor)
                    {
                        var c = CellCenter(s.cx, s.cz);
                        for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) lo = Mathf.Min(lo, WorldGen.Height(c.x + i * Cell * 0.5f, c.z + j * Cell * 0.5f));
                    }
                    Build(pc, s.y - lo + 0.5f);
                    Register(pc);
                }
        }

        // ============================================================ MESHES
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static Color32 Col(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static readonly Color32 Plank = Col("#b07a48"), PlankDark = Col("#8a5a32"), PlankLight = Col("#c8955c"), Post = Col("#6a4426"),
            StoneC = Col("#9a958c"), StoneDark = Col("#77736c"), Thatch = Col("#b8954a"), Shingle = Col("#8a4a3a"), Cloth = Col("#5a7ab0"), White = Col("#efe8da");

        static Matrix4x4 M(Vector3 p, Vector3 s, float yaw = 0f, float pitch = 0f, float roll = 0f) => Matrix4x4.TRS(p, Quaternion.Euler(pitch, yaw, roll), s);

        /// <summary>The look of a piece in its own space (the ghost uses it too). Floors are drawn with the footing depth given.</summary>
        public static Mesh MeshFor(HousePieceDef def, float depth = 0.8f)
        {
            string key = def.id + (def.kind == HouseKind.Floor ? "|" + Mathf.RoundToInt(depth * 10f) : "");
            if (meshes.TryGetValue(key, out var mesh)) return mesh;
            var mb = new MeshBuilder();
            float h = Cell, half = Cell * 0.5f;
            switch (def.kind)
            {
                case HouseKind.Floor:
                    mb.Box(M(new Vector3(0, -0.12f - depth * 0.5f, 0), new Vector3(h - 0.06f, depth, h - 0.06f)), StoneDark, StoneC);
                    for (int i = 0; i < 5; i++)
                        mb.Box(M(new Vector3(-half + 0.3f + i * 0.6f, -0.06f, 0), new Vector3(0.58f, 0.12f, h)), i % 2 == 0 ? Plank : PlankLight);
                    break;
                case HouseKind.Wall:
                    if (def.stone)
                    {
                        for (int row = 0; row < 5; row++)
                            for (int k = 0; k < 3; k++)
                            {
                                float off = row % 2 == 0 ? 0f : 0.5f, w = 1f;
                                float x = -half + (k + off) * w + w * 0.5f;
                                if (x + w * 0.5f > half + 0.01f) x -= (x + w * 0.5f) - half;
                                mb.Box(M(new Vector3(x, 0.3f + row * 0.6f, 0), new Vector3(w - 0.04f, 0.56f, WallT)), (row + k) % 2 == 0 ? StoneC : StoneDark);
                            }
                    }
                    else
                    {
                        for (int row = 0; row < 6; row++) mb.Box(M(new Vector3(0, 0.25f + row * 0.5f, 0), new Vector3(h, 0.48f, WallT)), row % 2 == 0 ? Plank : PlankLight);
                        WallPosts(mb, half);
                    }
                    break;
                case HouseKind.Doorway:
                {
                    float w = (h - DoorGap) * 0.5f;
                    for (int s = -1; s <= 1; s += 2)
                        for (int row = 0; row < 6; row++)
                            mb.Box(M(new Vector3(s * (half - w * 0.5f), 0.25f + row * 0.5f, 0), new Vector3(w, 0.48f, WallT)), row % 2 == 0 ? Plank : PlankLight);
                    mb.Box(M(new Vector3(0, 2.55f, 0), new Vector3(DoorGap, 0.9f, WallT)), PlankDark);
                    mb.Box(M(new Vector3(0, 2.12f, 0), new Vector3(DoorGap + 0.1f, 0.1f, WallT + 0.06f)), Post);
                    WallPosts(mb, half);
                    break;
                }
                case HouseKind.Window:
                {
                    float w = (h - 1.2f) * 0.5f;
                    for (int s = -1; s <= 1; s += 2)
                        for (int row = 0; row < 6; row++)
                            mb.Box(M(new Vector3(s * (half - w * 0.5f), 0.25f + row * 0.5f, 0), new Vector3(w, 0.48f, WallT)), row % 2 == 0 ? Plank : PlankLight);
                    mb.Box(M(new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1.0f, WallT)), Plank);
                    mb.Box(M(new Vector3(0, 2.6f, 0), new Vector3(1.2f, 0.8f, WallT)), PlankLight);
                    mb.Box(M(new Vector3(0, 1.0f, 0), new Vector3(1.34f, 0.08f, WallT + 0.12f)), Post);   // sill
                    mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(1.2f, 0.05f, 0.05f)), Post);            // cross bars
                    mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(0.05f, 1.2f, 0.05f)), Post);
                    WallPosts(mb, half);
                    break;
                }
                case HouseKind.Roof:
                    mb.Box(M(new Vector3(0, 0.1f, 0), new Vector3(h + 0.3f, 0.2f, h + 0.3f)), Shingle, Thatch);
                    mb.Box(M(new Vector3(0, -0.04f, 0), new Vector3(h + 0.1f, 0.12f, h + 0.1f)), PlankDark);
                    break;
                case HouseKind.RoofSlope:
                {
                    // rises 1.5 m toward +z (rot turns it); a beam along the high edge, open gables
                    float rise = 1.5f, run = h + 0.3f;
                    float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg, len = Mathf.Sqrt(rise * rise + run * run);
                    mb.Box(M(new Vector3(0, rise * 0.5f + 0.1f, 0), new Vector3(h + 0.3f, 0.18f, len), 0f, -ang), Shingle, Thatch);
                    mb.Box(M(new Vector3(0, rise + 0.05f, half + 0.1f), new Vector3(h + 0.4f, 0.16f, 0.16f)), Post);
                    for (int s = -1; s <= 1; s += 2)   // triangular-ish gable ends: stacked boards
                        for (int k = 0; k < 3; k++)
                        {
                            float zz = -half + (k + 0.5f) * (h / 3f), hh = rise * (k + 0.5f) / 3f;
                            mb.Box(M(new Vector3(s * (half - 0.05f), hh * 0.5f, zz), new Vector3(0.1f, hh, h / 3f)), PlankDark);
                        }
                    break;
                }
                default: Furniture(mb, def); break;
            }
            mesh = mb.ToMesh("house_" + key);
            meshes[key] = mesh;
            return mesh;
        }

        static void WallPosts(MeshBuilder mb, float half)
        {
            for (int s = -1; s <= 1; s += 2) mb.Box(M(new Vector3(s * (half - 0.07f), WallH * 0.5f, 0), new Vector3(0.16f, WallH + 0.02f, WallT + 0.08f)), Post);
            mb.Box(M(new Vector3(0, WallH - 0.05f, 0), new Vector3(half * 2f, 0.12f, WallT + 0.06f)), Post);
        }

        static void Furniture(MeshBuilder mb, HousePieceDef d)
        {
            switch (d.id)
            {
                case "house_bed":
                    mb.Box(M(new Vector3(0, 0.22f, 0), new Vector3(1.1f, 0.3f, 2.1f)), PlankDark);
                    mb.Box(M(new Vector3(0, 0.45f, 0.05f), new Vector3(1.0f, 0.18f, 1.95f)), White);
                    mb.Box(M(new Vector3(0, 0.58f, 0.75f), new Vector3(0.7f, 0.12f, 0.35f)), White);
                    mb.Box(M(new Vector3(0, 0.56f, -0.25f), new Vector3(1.04f, 0.1f, 1.3f)), Cloth);
                    mb.Box(M(new Vector3(0, 0.55f, 1.02f), new Vector3(1.14f, 0.7f, 0.1f)), Post);
                    break;
                case "house_table":
                    mb.Box(M(new Vector3(0, 0.78f, 0), new Vector3(1.3f, 0.08f, 0.85f)), Plank);
                    for (int i = 0; i < 4; i++) mb.Box(M(new Vector3((i % 2 == 0 ? -1 : 1) * 0.55f, 0.38f, (i < 2 ? -1 : 1) * 0.32f), new Vector3(0.08f, 0.76f, 0.08f)), PlankDark);
                    break;
                case "house_chair":
                    mb.Box(M(new Vector3(0, 0.45f, 0), new Vector3(0.48f, 0.07f, 0.48f)), Plank);
                    mb.Box(M(new Vector3(0, 0.8f, -0.21f), new Vector3(0.48f, 0.65f, 0.06f)), PlankDark);
                    for (int i = 0; i < 4; i++) mb.Box(M(new Vector3((i % 2 == 0 ? -1 : 1) * 0.2f, 0.22f, (i < 2 ? -1 : 1) * 0.2f), new Vector3(0.06f, 0.44f, 0.06f)), PlankDark);
                    break;
                case "house_lamp":
                    mb.Cylinder(M(new Vector3(0, 0.06f, 0), new Vector3(0.36f, 0.12f, 0.36f)), Post, 8, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 0.75f, 0), new Vector3(0.06f, 1.4f, 0.06f)), Post, 6, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 1.5f, 0), new Vector3(0.42f, 0.32f, 0.42f)), new Color32(240, 220, 170, 255), new Color32(240, 220, 170, 255), 8, 0.6f, false);
                    break;
                case "house_rug":
                    mb.Box(M(new Vector3(0, 0.015f, 0), new Vector3(1.8f, 0.03f, 1.2f)), Col("#9a3a3a"));
                    mb.Box(M(new Vector3(0, 0.02f, 0), new Vector3(1.4f, 0.03f, 0.8f)), Col("#d8b25a"));
                    mb.Box(M(new Vector3(0, 0.025f, 0), new Vector3(0.9f, 0.03f, 0.4f)), Col("#3a5a8a"));
                    break;
                case "house_shelf":
                    mb.Box(M(new Vector3(0, 1.0f, -0.18f), new Vector3(1.1f, 2.0f, 0.06f)), PlankDark);
                    for (int s = -1; s <= 1; s += 2) mb.Box(M(new Vector3(s * 0.52f, 1.0f, 0), new Vector3(0.06f, 2.0f, 0.42f)), PlankDark);
                    for (int k = 0; k < 4; k++)
                    {
                        mb.Box(M(new Vector3(0, 0.08f + k * 0.6f, 0), new Vector3(1.0f, 0.05f, 0.4f)), Plank);
                        if (k == 3) continue;
                        for (int bk = 0; bk < 6; bk++)
                        {
                            Color32 bc = (bk + k) % 3 == 0 ? Col("#8a3a3a") : (bk + k) % 3 == 1 ? Col("#3a6a5a") : Col("#c8a050");
                            float bh = 0.34f + ((bk * 7 + k * 3) % 4) * 0.04f;
                            mb.Box(M(new Vector3(-0.42f + bk * 0.15f, 0.1f + k * 0.6f + bh * 0.5f, 0), new Vector3(0.12f, bh, 0.3f)), bc);
                        }
                    }
                    break;
                case "house_wardrobe":
                    mb.Box(M(new Vector3(0, 1.05f, 0), new Vector3(1.1f, 2.1f, 0.6f)), PlankDark, Plank);
                    mb.Box(M(new Vector3(-0.27f, 1.05f, 0.31f), new Vector3(0.5f, 1.9f, 0.03f)), Plank);
                    mb.Box(M(new Vector3(0.27f, 1.05f, 0.31f), new Vector3(0.5f, 1.9f, 0.03f)), Plank);
                    mb.Box(M(new Vector3(-0.06f, 1.1f, 0.34f), new Vector3(0.04f, 0.2f, 0.04f)), Col("#d8b25a"));
                    mb.Box(M(new Vector3(0.06f, 1.1f, 0.34f), new Vector3(0.04f, 0.2f, 0.04f)), Col("#d8b25a"));
                    break;
            }
        }

        static Mesh doorMesh, glowMesh;

        void Build(Piece pc, float depth)
        {
            var go = new GameObject("House_" + pc.def.id);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(pc.pos, Quaternion.Euler(0f, pc.def.kind == HouseKind.Wall || pc.def.kind == HouseKind.Doorway || pc.def.kind == HouseKind.Window ? (pc.side == 0 ? 0f : 90f) : pc.rot * 90f, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = MeshFor(pc.def, depth);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = pc.def.kind == HouseKind.Floor || pc.def.id == "house_rug" ? ShadowCastingMode.Off : ShadowCastingMode.On;
            var rends = new List<Renderer> { mr };
            if (pc.def.kind == HouseKind.Doorway)
            {
                if (doorMesh == null)
                {
                    var mb = new MeshBuilder();
                    for (int i = 0; i < 4; i++) mb.Box(M(new Vector3(0.15f + i * 0.29f, 1.05f, 0), new Vector3(0.28f, 2.1f, 0.07f)), i % 2 == 0 ? PlankDark : Plank);
                    mb.Box(M(new Vector3(0.58f, 0.5f, 0.02f), new Vector3(1.16f, 0.1f, 0.08f)), Post);
                    mb.Box(M(new Vector3(0.58f, 1.6f, 0.02f), new Vector3(1.16f, 0.1f, 0.08f)), Post);
                    mb.Sphere(M(new Vector3(1.0f, 1.05f, 0.07f), Vector3.one * 0.08f), Col("#d8b25a"), 6, 4);
                    doorMesh = mb.ToMesh("house_door_leaf");
                }
                var hinge = new GameObject("Door").transform;
                hinge.SetParent(go.transform, false);
                hinge.localPosition = new Vector3(-DoorGap * 0.5f, 0f, 0f);
                hinge.gameObject.AddComponent<MeshFilter>().sharedMesh = doorMesh;
                var dr = hinge.gameObject.AddComponent<MeshRenderer>(); dr.sharedMaterial = RenderAssets.Building;
                rends.Add(dr);
                pc.door = hinge;
            }
            if (pc.def.lamp)
            {
                if (glowMesh == null)
                {
                    var mb = new MeshBuilder();
                    mb.Sphere(M(new Vector3(0, 1.48f, 0), Vector3.one * 0.26f), new Color32(255, 235, 160, 0), 8, 6);   // vertex alpha 0 = self-lit on the creature shader
                    glowMesh = mb.ToMesh("house_lamp_glow");
                }
                var g = new GameObject("Glow");
                g.transform.SetParent(go.transform, false);
                g.AddComponent<MeshFilter>().sharedMesh = glowMesh;
                var gr = g.AddComponent<MeshRenderer>(); gr.sharedMaterial = RenderAssets.Creature; gr.shadowCastingMode = ShadowCastingMode.Off;
            }
            pc.go = go;
            pc.rends = rends.ToArray();
        }
    }
}
