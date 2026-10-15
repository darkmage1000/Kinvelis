using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// A level house on uneven ground, and steps up to it (user playtest, 2026-10-15: "if i build on an uneven surface,
    /// i want my floors to snap to the same height instead of respecting the ground ... i had to jump to go thru my
    /// doorway cus it was slightly elevated").
    ///
    /// Level: every ground-floor floor joined to others (side by side) sits at one height - the house's. A new floor
    /// next to the house takes its height and its stone footing reaches down to the ground. Where the ground under the
    /// new square is higher than the house, the whole house is lifted to stay level (`LiftHouse`): every piece standing
    /// on those squares - upper floors, walls, stairs, roofs, furniture, porch steps - moves up with it. A house with
    /// pieces still being built is not lifted (they would be left behind): the snap says to let them finish.
    ///
    /// Steps: Porch Steps go on the outside of a ground-floor edge and run from the ground up to the floor (`StepsGeom`
    /// works out how long they need to be from the ground under them, 0.9-2.8 m), and their slope is a walking surface
    /// like the stairs (`Surf` ramps in `SurfaceAt`). The explorer also steps up onto anything built that is no more
    /// than `PlayerController.BuiltStepUp` above their feet, so a floor laid on flat ground needs no steps at all.
    /// </summary>
    public partial class Housing
    {
        public const float StepsW = 1.8f, MaxLift = 4.5f;

        readonly Dictionary<long, Piece>[] porches = { new Dictionary<long, Piece>(), new Dictionary<long, Piece>() };   // by edge, like walls

        // ============================================================ LEVEL
        /// <summary>The ground-floor squares joined to (cx, cz), built or being built (side by side, not corner to corner).</summary>
        void GroupOf(int cx, int cz, HashSet<long> into)
        {
            var open = new Queue<long>();
            long k0 = Key(cx, cz);
            if (!FloorOrSite(cx, cz, 0, out _) || !into.Add(k0)) return;
            open.Enqueue(k0);
            while (open.Count > 0 && into.Count < 600)
            {
                long k = open.Dequeue();
                int x = (int)(k >> 32), z = (int)(uint)k;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + DX[d], nz = z + DZ[d];
                    long nk = Key(nx, nz);
                    if (into.Contains(nk) || !FloorOrSite(nx, nz, 0, out _)) continue;
                    into.Add(nk); open.Enqueue(nk);
                }
            }
        }

        /// <summary>The highest ground-floor height of the house(s) next to a square, and whether any part of them is lower than y.</summary>
        float HouseHeightBeside(int cx, int cz, out bool any, HashSet<long> squares)
        {
            any = false;
            float best = float.NegativeInfinity;
            squares.Clear();
            for (int d = 0; d < 4; d++) GroupOf(cx + DX[d], cz + DZ[d], squares);
            foreach (var k in squares)
            {
                int x = (int)(k >> 32), z = (int)(uint)k;
                if (FloorOrSite(x, z, 0, out float y)) { any = true; best = Mathf.Max(best, y); }
            }
            return best;
        }

        readonly HashSet<long> liftSquares = new HashSet<long>();

        /// <summary>Called as a ground floor is placed: the house beside it comes up to its height if it is lower.</summary>
        public void LevelWith(HousePieceDef def, Spot s)
        {
            if (def.kind != HouseKind.Floor || s.level != 0) return;
            HouseHeightBeside(s.cx, s.cz, out bool any, liftSquares);
            if (any) LiftHouse(liftSquares, s.pos.y);
        }

        bool NeedsLift(HashSet<long> squares, float y)
        {
            foreach (var k in squares)
            {
                int x = (int)(k >> 32), z = (int)(uint)k;
                if (FloorOrSite(x, z, 0, out float fy) && fy < y - 0.01f) return true;
            }
            return false;
        }

        /// <summary>Lifts every ground floor of these squares to y, and everything standing on them by the same amount.</summary>
        void LiftHouse(HashSet<long> squares, float y)
        {
            var delta = new Dictionary<long, float>();
            foreach (var k in squares)
            {
                int x = (int)(k >> 32), z = (int)(uint)k;
                if (floors.TryGetValue(LKey(x, z, 0), out var f) && f.Y < y - 0.01f) delta[k] = y - f.Y;
            }
            if (delta.Count == 0) return;
            float D(int x, int z) => delta.TryGetValue(Key(x, z), out var d) ? d : 0f;
            void Shift(Piece p, float d) { if (d <= 0f) return; p.pos.y += d; if (p.go != null) p.go.transform.position = p.pos; }

            foreach (var f in floors.Values)
            {
                float d = D(f.cx, f.cz);
                if (d <= 0f) continue;
                Shift(f, d);
                if (f.level == 0 && f.go != null) f.go.GetComponent<MeshFilter>().sharedMesh = MeshFor(f.def, FootingDepth(f.cx, f.cz, f.Y));
            }
            foreach (var r in roofs.Values) Shift(r, D(r.cx, r.cz));
            foreach (var st in stairs.Values) Shift(st, D(st.cx, st.cz));
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values)
                {
                    int nx = side == 1 ? w.cx + 1 : w.cx, nz = side == 0 ? w.cz + 1 : w.cz;
                    Shift(w, Mathf.Max(D(w.cx, w.cz), D(nx, nz)));
                }
            foreach (var f in furniture)
            {
                float d = D(f.cx, f.cz);
                if (d <= 0f) continue;
                Shift(f, d);
                if (f.obs != null) { WorldCollision.RemoveObstacle(f.obs); f.obs = null; AddFurnitureObstacle(f); }
            }
            for (int side = 0; side < 2; side++)
                foreach (var p in porches[side].Values)
                {
                    int nx = side == 1 ? p.cx + 1 : p.cx, nz = side == 0 ? p.cz + 1 : p.cz;
                    float d = Mathf.Max(D(p.cx, p.cz), D(nx, nz));
                    if (d <= 0f) continue;
                    p.pos.y += d;
                    if (p.go != null) { p.go.transform.position = p.pos; p.go.GetComponent<MeshFilter>().sharedMesh = StepsMeshFor(p); }
                }
            Publish(); RebuildSegs(); RefreshRails(); RecalcBounds(); Version++;
            GameEvents.Say("The house was lifted to stay level with the new floor.", new Color(0.8f, 0.9f, 1f));
        }

        /// <summary>How deep a ground floor's stone footing must go to reach the lowest ground under its square.</summary>
        static float FootingDepth(int cx, int cz, float top)
        {
            var c = CellCenter(cx, cz);
            float lo = top;
            for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) lo = Mathf.Min(lo, WorldGen.Height(c.x + i * Cell * 0.5f, c.z + j * Cell * 0.5f));
            return top - lo + 0.5f;
        }

        void AddFurnitureObstacle(Piece pc)
        {
            if (pc.def.radius <= 0f) return;
            pc.obs = WorldCollision.AddObstacle(pc.pos, pc.def.radius, pc.go);
            // only in the way on its own storey (a table upstairs doesn't stop you downstairs)
            pc.obs.yLo = pc.level == 0 ? float.NegativeInfinity : pc.Y - 0.8f;
            pc.obs.yHi = pc.Y + 2.4f;
        }

        // ============================================================ STEPS
        /// <summary>
        /// Porch steps on an edge: `mid` the edge's middle, `top` the floor height, `outDir` the way out (0-3). They need
        /// to be long enough for a gentle climb from the ground at their foot: 0.7 m up per metre along, 0.9-2.8 m.
        /// </summary>
        static void StepsGeom(Vector3 mid, float top, int outDir, out float len, out float rise)
        {
            Vector3 outv = new Vector3(DX[outDir], 0f, DZ[outDir]);
            len = 1.5f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 foot = mid + outv * len;
                rise = top - WorldGen.Height(foot.x, foot.z);
                len = Mathf.Clamp(rise / 0.7f, 0.9f, 2.8f);
            }
            Vector3 f2 = mid + outv * len;
            rise = Mathf.Max(0.05f, top - WorldGen.Height(f2.x, f2.z));
        }

        static Vector3 EdgeMid(int ex, int ez, int side) =>
            side == 0 ? new Vector3((ex + 0.5f) * Cell, 0f, (ez + 1) * Cell) : new Vector3((ex + 1) * Cell, 0f, (ez + 0.5f) * Cell);

        Spot SnapSteps(HousePieceDef def, Spot s, Vector3 t, int cx, int cz)
        {
            // the nearest edge to the point ahead that has a floor on one side only (round the square ahead and the
            // squares beside it), so it finds the floor's edge wherever you stand; ROTATE steps to the next one
            var cands = new List<(float d, int ex, int ez, int side)>();
            for (int ox = -1; ox <= 1; ox++)
                for (int oz = -1; oz <= 1; oz++)
                    for (int e0 = 0; e0 < 4; e0++)
                    {
                        EdgeOf(cx + ox, cz + oz, e0, out int x, out int z, out int sd);
                        bool a = FloorOrSite(x, z, 0, out _), b = FloorOrSite(sd == 1 ? x + 1 : x, sd == 0 ? z + 1 : z, 0, out _);
                        if (a == b) continue;
                        float dd = Flat(EdgeMid(x, z, sd) - t);
                        bool dup = false;
                        foreach (var c in cands) if (c.ex == x && c.ez == z && c.side == sd) dup = true;
                        if (!dup) cands.Add((dd, x, z, sd));
                    }
            cands.Sort((p, q) => p.d.CompareTo(q.d));
            int ex, ez, side;
            if (cands.Count > 0 && cands[0].d < 3.5f) { var pick = cands[Mathf.Min(s.rot, cands.Count - 1)]; ex = pick.ex; ez = pick.ez; side = pick.side; }
            else
            {
                float u = t.x - cx * Cell, v = t.z - cz * Cell;
                float[] d = { Cell - v, Cell - u, v, u };
                int best = 0; for (int i = 1; i < 4; i++) if (d[i] < d[best]) best = i;
                EdgeOf(cx, cz, best, out ex, out ez, out side);
            }
            int bx = side == 1 ? ex + 1 : ex, bz = side == 0 ? ez + 1 : ez;
            bool fa = FloorOrSite(ex, ez, 0, out float ya), fb = FloorOrSite(bx, bz, 0, out float yb);
            int outDir = fa ? (side == 0 ? 0 : 1) : (side == 0 ? 2 : 3);
            float top = fa ? ya : yb;
            Vector3 mid = EdgeMid(ex, ez, side);
            s.cx = ex; s.cz = ez; s.side = side; s.rot = outDir; s.level = 0;
            s.pos = new Vector3(mid.x, fa || fb ? top : WorldGen.Height(mid.x, mid.z), mid.z);
            s.q = Quaternion.Euler(0f, ((outDir + 2) % 4) * 90f, 0f);
            if (s.reason != null) return s;
            if (fa == fb) { s.reason = fa ? "Steps go on the outside edge of a floor" : "Steps lead up to a floor"; return s; }
            StepsGeom(mid, top, outDir, out float len, out float rise);
            Vector3 outv = new Vector3(DX[outDir], 0f, DZ[outDir]);
            if (porches[side].ContainsKey(LKey(ex, ez, 0)) || pending.Contains($"P{side}:{ex},{ez}")) { s.reason = "There are steps here already"; return s; }
            if (rise < 0.2f) { s.reason = "No steps needed - the floor is level with the ground here"; return s; }
            if (rise > 3.6f) { s.reason = "Too high for steps here"; return s; }
            if (WorldCollision.Overlaps(mid + outv * (len * 0.5f + 0.1f), 0.8f, true)) { s.reason = "Something is in the way"; return s; }
            s.ok = true;
            return s;
        }

        /// <summary>The steps' walking slope, for SurfaceAt.</summary>
        Surf StepsSurf(Piece p)
        {
            Vector3 mid = new Vector3(p.pos.x, 0f, p.pos.z), outv = new Vector3(DX[p.rot], 0f, DZ[p.rot]);
            StepsGeom(mid, p.Y, p.rot, out float len, out float rise);
            Vector3 c = mid + outv * (len * 0.5f);
            return new Surf { ramp = true, cx = c.x, cz = c.z, dx = -outv.x, dz = -outv.z, halfLen = len * 0.5f, halfW = StepsW * 0.5f, y = p.Y - rise, y1 = p.Y };
        }

        static readonly Dictionary<string, Mesh> stepMeshes = new Dictionary<string, Mesh>();

        /// <summary>Steps built in their own space: the top at the origin (the floor edge), climbing toward +z from z = -len.</summary>
        static Mesh StepsMeshFor(Piece p)
        {
            Vector3 mid = new Vector3(p.pos.x, 0f, p.pos.z);
            StepsGeom(mid, p.Y, p.rot, out float len, out float rise);
            string key = $"{len:0.0}x{rise:0.0}";
            if (stepMeshes.TryGetValue(key, out var mesh)) return mesh;
            var mb = new MeshBuilder();
            int n = Mathf.Max(2, Mathf.CeilToInt(rise / 0.22f));
            float run = len / n;
            for (int i = 0; i < n; i++)
            {
                float top = -rise + (i + 1) * rise / n, bottom = -rise - 0.4f;
                mb.Box(M(new Vector3(0f, (top + bottom) * 0.5f, -len + (i + 0.5f) * run), new Vector3(StepsW - 0.12f, top - bottom, run)), StoneDark, i % 2 == 0 ? Plank : PlankLight);
            }
            float ang = Mathf.Atan2(rise, len) * Mathf.Rad2Deg, sl = Mathf.Sqrt(rise * rise + len * len);
            for (int sd = -1; sd <= 1; sd += 2)
                mb.Box(M(new Vector3(sd * (StepsW * 0.5f - 0.05f), -rise * 0.5f + 0.06f, -len * 0.5f), new Vector3(0.1f, 0.24f, sl), 0f, -ang), Post);
            mesh = mb.ToMesh("house_steps_" + key);
            stepMeshes[key] = mesh;
            return mesh;
        }
    }
}
