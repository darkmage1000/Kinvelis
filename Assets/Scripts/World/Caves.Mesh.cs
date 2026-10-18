using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// What a cave looks like (cave pass, 2026-10-15, user: "make them look much more like a cave and not a museum of a
    /// cave"). Marching squares over the layout field (`Caves.Layout`) give the rock face as a smooth contour; from it:
    /// an uneven floor, a domed ceiling (faces point down, so a camera above it sees in), and walls in five bands that
    /// undercut at the foot, bulge and lean in toward the top. Decor is placed from the same contour: rubble at the wall
    /// foot, boulders, stalagmites, stalactites under tall domes, crystal clusters, glowcaps, puddles, bones, roots, and
    /// biome extras (lava seams, ice, coral, ooze, star specks). Light is baked into the vertex colours: every glowing
    /// thing is a light (`Lamp`) and everything near it is brightened and tinted, so the cave is dark between pools of
    /// glow. Glowing parts themselves are self-lit (vertex alpha 0 on the Creature material). Ruin halls get built walls.
    /// </summary>
    public partial class Caves
    {
        struct Lamp { public Vector3 p; public Color c; public float r, k; }

        // ================================================================ SHELL
        void BuildGeometry(Run r, System.Random rng)
        {
            r.root = new GameObject("Cave_" + r.e.id);
            r.root.transform.SetParent(transform, false);
            r.segs = new List<Vector4>(4096);
            r.segNormals = new List<Vector2>(4096);

            var floor = new MeshBuilder();
            var ceil = new MeshBuilder();
            Contour(r, floor, ceil);
            IndexSegments(r);

            // decor first: it decides where the light comes from
            var deco = new MeshBuilder();
            var lamps = new List<Lamp>();
            r.lamps = lamps;
            Decorate(r, deco, lamps, rng);
            DecorateSpecial(r, deco, lamps, rng);

            var walls = new MeshBuilder();
            BuildWalls(r, walls);

            Light(floor, lamps, r, 0.62f);
            Light(walls, lamps, r, 0.6f);
            Light(ceil, lamps, r, 0.5f);
            Light(deco, lamps, r, 0.75f);
            // in tiles, so the camera only draws the part of the cave in front of it
            PartTiled(r, "Floor", floor, RenderAssets.Building);
            PartTiled(r, "Walls", walls, RenderAssets.Building);
            PartTiled(r, "Ceiling", ceil, RenderAssets.Building);
            PartTiled(r, "Decor", deco, RenderAssets.Creature);

            if (r.level == 0) BuildPortal(r.exitPos, new Color(0.75f, 0.95f, 1f));
            BuildMap(r);
        }

        static void Part(Run r, string name, MeshBuilder mb, Material mat)
        {
            if (mb.VertexCount == 0) return;
            var go = new GameObject(name);
            go.transform.SetParent(r.root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cave_" + name.ToLowerInvariant());
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        const float Tile = 46f;

        /// <summary>Splits a big mesh into square tiles by triangle (each tile is culled on its own).</summary>
        static void PartTiled(Run r, string name, MeshBuilder mb, Material mat)
        {
            if (mb.VertexCount == 0) return;
            int tn = Mathf.CeilToInt(ZoneSize / Tile) + 1;
            var subs = new Dictionary<int, MeshBuilder>();
            var maps = new Dictionary<int, Dictionary<int, int>>();
            for (int t = 0; t + 2 < mb.tris.Count; t += 3)
            {
                Vector3 c = (mb.verts[mb.tris[t]] + mb.verts[mb.tris[t + 1]] + mb.verts[mb.tris[t + 2]]) / 3f;
                int ti = Mathf.Clamp(Mathf.FloorToInt((c.x - Origin.x) / Tile), 0, tn - 1), tj = Mathf.Clamp(Mathf.FloorToInt((c.z - Origin.y) / Tile), 0, tn - 1);
                int key = tj * tn + ti;
                if (!subs.TryGetValue(key, out var sub)) { sub = new MeshBuilder(); subs[key] = sub; maps[key] = new Dictionary<int, int>(); }
                var map = maps[key];
                for (int q = 0; q < 3; q++)
                {
                    int v = mb.tris[t + q];
                    if (!map.TryGetValue(v, out int nv))
                    {
                        nv = sub.verts.Count;
                        sub.verts.Add(mb.verts[v]); sub.norms.Add(mb.norms[v]); sub.cols.Add(mb.cols[v]);
                        map[v] = nv;
                    }
                    sub.tris.Add(nv);
                }
            }
            foreach (var kv in subs) Part(r, name + kv.Key, kv.Value, mat);
        }

        // ---------------------------------------------------------------- marching squares
        void Contour(Run r, MeshBuilder floor, MeshBuilder ceil)
        {
            var F = r.F;
            var e = r.e;
            int seed = r.seed;
            var cornerF = new int[S1 * S1]; var cornerC = new int[S1 * S1];
            var hEdgeF = new int[S1 * S1]; var hEdgeC = new int[S1 * S1];
            var vEdgeF = new int[S1 * S1]; var vEdgeC = new int[S1 * S1];
            for (int k = 0; k < cornerF.Length; k++) { cornerF[k] = cornerC[k] = hEdgeF[k] = hEdgeC[k] = vEdgeF[k] = vEdgeC[k] = -1; }
            Color fl0 = Muted(e.floor);
            Color moss = Color.Lerp(fl0, BiomeTint(e.biome), 0.3f);
            Color gravel = fl0 * 0.72f;
            bool green = e.biome == BiomeId.Meadow || e.biome == BiomeId.Swamp || e.biome == BiomeId.Glow || e.biome == BiomeId.Isles;

            Color FloorCol(float x, float z, float f, bool h)
            {
                if (h) { int a = Mathf.FloorToInt(x * 0.5f), b = Mathf.FloorToInt(z * 0.5f); return e.floor * (((a + b) & 1) == 0 ? 1.04f : 0.86f) * (0.94f + Hash.F(a, b, 77) * 0.12f); }
                float n = VN(x * 0.19f, z * 0.19f, seed + 21), m = VN(x * 0.07f, z * 0.07f, seed + 5);
                Color c = Color.Lerp(gravel, fl0, n);
                if (green && m > 0.58f) c = Color.Lerp(c, moss, Mathf.Clamp01((m - 0.58f) * 4f));
                c *= 0.88f + Hash.F(Mathf.FloorToInt(x * 2f), Mathf.FloorToInt(z * 2f), seed) * 0.16f;
                return c * (0.62f + 0.38f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f / 2f)));   // darker at the foot of the rock
            }
            Color ceilBase = Muted(e.rockTop);
            Color CeilCol(float x, float z) => ceilBase * (0.55f + VN(x * 0.2f, z * 0.2f, seed + 9) * 0.3f);

            int FV(int i, int j)
            {
                int k = j * S1 + i;
                if (cornerF[k] >= 0) return cornerF[k];
                float x = Origin.x + i * Res, z = Origin.y + j * Res;
                cornerF[k] = Vert(floor, new Vector3(x, FloorY + r.Flo[k], z), Vector3.up, FloorCol(x, z, F[k], r.hall[k]));
                return cornerF[k];
            }
            int CV(int i, int j)
            {
                int k = j * S1 + i;
                if (cornerC[k] >= 0) return cornerC[k];
                float x = Origin.x + i * Res, z = Origin.y + j * Res;
                cornerC[k] = Vert(ceil, new Vector3(x, FloorY + r.C[k], z), Vector3.down, CeilCol(x, z));
                return cornerC[k];
            }
            // edge points: horizontal edge (i,j)-(i+1,j), vertical edge (i,j)-(i,j+1)
            Vector2 EP(int i, int j, bool hor)
            {
                int k0 = j * S1 + i, k1 = hor ? k0 + 1 : k0 + S1;
                float t = Mathf.Clamp01(F[k0] / (F[k0] - F[k1]));
                return hor ? new Vector2(Origin.x + (i + t) * Res, Origin.y + j * Res) : new Vector2(Origin.x + i * Res, Origin.y + (j + t) * Res);
            }
            int EF(int i, int j, bool hor)
            {
                var arr = hor ? hEdgeF : vEdgeF; int k = j * S1 + i;
                if (arr[k] >= 0) return arr[k];
                var p = EP(i, j, hor);
                arr[k] = Vert(floor, new Vector3(p.x, FloorAt2(r, p.x, p.y), p.y), Vector3.up, FloorCol(p.x, p.y, 0f, false) * 0.9f);
                return arr[k];
            }
            int EC(int i, int j, bool hor)
            {
                var arr = hor ? hEdgeC : vEdgeC; int k = j * S1 + i;
                if (arr[k] >= 0) return arr[k];
                var p = EP(i, j, hor);
                arr[k] = Vert(ceil, new Vector3(p.x, CeilAt(r, p.x, p.y), p.y), Vector3.down, CeilCol(p.x, p.y));
                return arr[k];
            }

            // corners of a cell walked clockwise (seen from above): (i,j) (i,j+1) (i+1,j+1) (i+1,j)
            var ci = new int[4]; var cj = new int[4];
            var polyF = new List<int>(8); var polyC = new List<int>(8);
            var polyP = new List<Vector2>(8); var exit = new List<bool>(8); var isEdge = new List<bool>(8);
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                ci[0] = i; cj[0] = j; ci[1] = i; cj[1] = j + 1; ci[2] = i + 1; cj[2] = j + 1; ci[3] = i + 1; cj[3] = j;
                int mask = 0;
                for (int q = 0; q < 4; q++) if (F[cj[q] * S1 + ci[q]] > 0f) mask |= 1 << q;
                if (mask == 0) continue;
                bool saddle = mask == 5 || mask == 10;
                float centre = (F[j * S1 + i] + F[j * S1 + i + 1] + F[(j + 1) * S1 + i] + F[(j + 1) * S1 + i + 1]) * 0.25f;
                if (saddle && centre <= 0f)
                {
                    // two separate corners of floor: one triangle each
                    for (int q = 0; q < 4; q++)
                    {
                        if ((mask & (1 << q)) == 0) continue;
                        int qp = (q + 3) & 3, qn = (q + 1) & 3;
                        CellEdge(ci, cj, qp, q, out int ai, out int aj, out bool ah);
                        CellEdge(ci, cj, q, qn, out int bi2, out int bj2, out bool bh);
                        floor.tris.Add(EF(ai, aj, ah)); floor.tris.Add(FV(ci[q], cj[q])); floor.tris.Add(EF(bi2, bj2, bh));
                        ceil.tris.Add(EC(ai, aj, ah)); ceil.tris.Add(EC(bi2, bj2, bh)); ceil.tris.Add(CV(ci[q], cj[q]));
                        AddSeg(r, EP(bi2, bj2, bh), EP(ai, aj, ah));   // exit (after the corner) -> entry (before it)
                    }
                    continue;
                }
                polyF.Clear(); polyC.Clear(); polyP.Clear(); exit.Clear(); isEdge.Clear();
                for (int q = 0; q < 4; q++)
                {
                    int qn = (q + 1) & 3;
                    bool inQ = (mask & (1 << q)) != 0, inN = (mask & (1 << qn)) != 0;
                    if (inQ)
                    {
                        polyF.Add(FV(ci[q], cj[q])); polyC.Add(CV(ci[q], cj[q]));
                        polyP.Add(Vector2.zero); exit.Add(false); isEdge.Add(false);
                    }
                    if (inQ != inN)
                    {
                        CellEdge(ci, cj, q, qn, out int ei, out int ej, out bool eh);
                        polyF.Add(EF(ei, ej, eh)); polyC.Add(EC(ei, ej, eh));
                        polyP.Add(EP(ei, ej, eh)); exit.Add(inQ); isEdge.Add(true);
                    }
                }
                for (int t = 1; t + 1 < polyF.Count; t++)
                {
                    floor.tris.Add(polyF[0]); floor.tris.Add(polyF[t]); floor.tris.Add(polyF[t + 1]);
                    ceil.tris.Add(polyC[0]); ceil.tris.Add(polyC[t + 1]); ceil.tris.Add(polyC[t]);
                }
                for (int t = 0; t < polyP.Count; t++)
                {
                    if (!isEdge[t] || !exit[t]) continue;
                    int n2 = (t + 1) % polyP.Count;
                    if (isEdge[n2]) AddSeg(r, polyP[t], polyP[n2]);
                }
            }
        }

        static int Vert(MeshBuilder mb, Vector3 p, Vector3 n, Color c)
        {
            mb.verts.Add(p); mb.norms.Add(n); mb.cols.Add(c);
            return mb.verts.Count - 1;
        }

        /// <summary>The grid edge between corner slots a and b of the current cell.</summary>
        static void CellEdge(int[] ci, int[] cj, int a, int b, out int i, out int j, out bool hor)
        {
            hor = cj[a] == cj[b];
            i = Mathf.Min(ci[a], ci[b]); j = Mathf.Min(cj[a], cj[b]);
        }

        static float FloorAt2(Run r, float x, float z) => FloorY + Sample(r.Flo, x, z);

        /// <summary>A wall segment, stored with the open side on its right (seen from above).</summary>
        static void AddSeg(Run r, Vector2 a, Vector2 b)
        {
            if ((b - a).sqrMagnitude < 1e-6f) return;
            var mid = (a + b) * 0.5f;
            var d = (b - a).normalized;
            var n = new Vector2(d.y, -d.x);
            // make sure 'n' points into the open (the field rises that way)
            if (Sample(r.F, mid.x + n.x * 0.3f, mid.y + n.y * 0.3f) < Sample(r.F, mid.x - n.x * 0.3f, mid.y - n.y * 0.3f)) { var t = a; a = b; b = t; n = -n; }
            r.segs.Add(new Vector4(a.x, a.y, b.x, b.y));
            r.segNormals.Add(n);
        }

        // ---------------------------------------------------------------- walls
        /// <summary>
        /// The rock face: a strip of five bands up each contour segment. Vertices are shared between neighbouring
        /// segments (keyed by position), so the lumps flow along the wall and it is smoothly lit; band heights and the
        /// push in or out of the rock come from noise at that point, so neighbours always agree.
        /// </summary>
        void BuildWalls(Run r, MeshBuilder mb)
        {
            var e = r.e;
            int seed = r.seed;
            float[] rows = { 0f, 0.16f, 0.42f, 0.72f, 1f };
            float[] push = { 0f, 0.35f, 0.15f, -0.35f, -0.75f };   // + into the rock (undercut), - out over the floor (overhang)
            bool Built(Vector2 p) => r.hall[Mathf.Clamp(Mathf.RoundToInt((p.y - Origin.y) / Res), 0, N) * S1 + Mathf.Clamp(Mathf.RoundToInt((p.x - Origin.x) / Res), 0, N)];
            Color rock = Muted(e.rock), top = Muted(e.rockTop), veinCol = Color.Lerp(rock, e.glow, 0.3f);
            var maps = new Dictionary<long, int>[rows.Length];
            for (int i = 0; i < maps.Length; i++) maps[i] = new Dictionary<long, int>(r.segs.Count + 16);
            var acc = new List<Vector3>(r.segs.Count * 6);

            int V(Vector2 p, int row)
            {
                long key = ((long)Mathf.RoundToInt(p.x * 512f) << 32) ^ (uint)Mathf.RoundToInt(p.y * 512f);
                if (maps[row].TryGetValue(key, out int idx)) return idx;
                bool hall = Built(p);
                float f0 = FloorAt2(r, p.x, p.y), ceilY = CeilAt(r, p.x, p.y);
                var g = Grad(r, p.x, p.y);   // toward the open
                float t = rows[row];
                if (row > 0 && row < rows.Length - 1 && !hall) t += (VN(p.x * 0.35f + row * 4.1f, p.y * 0.35f, seed + 51) - 0.5f) * 0.14f;
                float y = Mathf.Lerp(f0, ceilY, t);
                Vector3 pos;
                if (hall || row == 0) pos = new Vector3(p.x, row == 0 ? f0 - 0.3f : y, p.y);
                else
                {
                    float n = (VN(p.x * 0.45f + row * 3.7f, p.y * 0.45f, seed + 41) - 0.5f) * 1.3f + (VN(p.x * 1.5f, p.y * 1.5f + row * 5.3f, seed + 43) - 0.5f) * 0.35f;
                    float o = push[row] + n;
                    pos = new Vector3(p.x - g.x * o, y, p.y - g.y * o);
                }
                Color c;
                if (hall)
                    c = row == 0 ? Color.Lerp(rock, Color.white, 0.22f) : row == 2 ? Color.Lerp(e.glow, rock, 0.55f) : row >= 3 ? top * 0.7f : rock;
                else
                {
                    float shade = 0.78f + VN(p.x * 0.3f, p.y * 0.3f + row * 2.3f, seed + 57) * 0.38f;
                    c = Color.Lerp(rock, top, rows[row]) * shade * (row == 0 ? 0.6f : 1f);
                    float vein = VN(p.x * 0.09f, p.y * 0.09f, seed + 61);
                    if ((row == 2 || row == 3) && vein > 0.68f) c = Color.Lerp(c, veinCol, Mathf.Clamp01((vein - 0.68f) * 5f));
                }
                mb.verts.Add(pos); mb.norms.Add(Vector3.zero); mb.cols.Add(c); acc.Add(Vector3.zero);
                idx = mb.verts.Count - 1;
                maps[row][key] = idx;
                return idx;
            }

            for (int s = 0; s < r.segs.Count; s++)
            {
                var sg = r.segs[s];
                var a = new Vector2(sg.x, sg.y); var b = new Vector2(sg.z, sg.w);
                for (int row = 0; row + 1 < rows.Length; row++)
                {
                    int a0 = V(a, row), a1 = V(a, row + 1), b1 = V(b, row + 1), b0 = V(b, row);
                    // front faces look into the cave (backs are culled, so a camera behind the rock sees through it)
                    mb.tris.Add(a0); mb.tris.Add(a1); mb.tris.Add(b1);
                    mb.tris.Add(a0); mb.tris.Add(b1); mb.tris.Add(b0);
                }
            }
            // smooth normals: each vertex averages the faces round it
            for (int t = 0; t < mb.tris.Count; t += 3)
            {
                int i0 = mb.tris[t], i1 = mb.tris[t + 1], i2 = mb.tris[t + 2];
                var n = Vector3.Cross(mb.verts[i1] - mb.verts[i0], mb.verts[i2] - mb.verts[i0]);
                acc[i0] += n; acc[i1] += n; acc[i2] += n;
            }
            for (int v = 0; v < acc.Count; v++) mb.norms[v] = acc[v].sqrMagnitude > 1e-10f ? acc[v].normalized : Vector3.up;
        }

        /// <summary>Pulls a colour a quarter of the way to grey, so a land's tint reads as a tint, not a paint job.</summary>
        static Color Muted(Color c) { float l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f; return Color.Lerp(new Color(l, l, l), c, 0.72f); }

        // ---------------------------------------------------------------- baked light
        static void Light(MeshBuilder mb, List<Lamp> lamps, Run r, float amb)
        {
            // lamps bucketed on a coarse grid so each vertex only looks at its neighbours
            const float LB = 16f;
            int lbn = Mathf.CeilToInt(ZoneSize / LB) + 1;
            var grid = new List<int>[lbn * lbn];
            for (int l = 0; l < lamps.Count; l++)
            {
                var lp = lamps[l];
                int i0 = Mathf.Clamp(Mathf.FloorToInt((lp.p.x - lp.r - Origin.x) / LB), 0, lbn - 1), i1 = Mathf.Clamp(Mathf.FloorToInt((lp.p.x + lp.r - Origin.x) / LB), 0, lbn - 1);
                int j0 = Mathf.Clamp(Mathf.FloorToInt((lp.p.z - lp.r - Origin.y) / LB), 0, lbn - 1), j1 = Mathf.Clamp(Mathf.FloorToInt((lp.p.z + lp.r - Origin.y) / LB), 0, lbn - 1);
                for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) { int k = j * lbn + i; if (grid[k] == null) grid[k] = new List<int>(); grid[k].Add(l); }
            }
            for (int v = 0; v < mb.verts.Count; v++)
            {
                var c = mb.cols[v];
                if (c.a == 0) continue;   // self-lit already
                var p = mb.verts[v];
                int gi = Mathf.Clamp(Mathf.FloorToInt((p.x - Origin.x) / LB), 0, lbn - 1), gj = Mathf.Clamp(Mathf.FloorToInt((p.z - Origin.y) / LB), 0, lbn - 1);
                var list = grid[gj * lbn + gi];
                float lr = amb, lg = amb, lbl = amb;
                if (list != null)
                    foreach (int l in list)
                    {
                        var lp = lamps[l];
                        float d = (lp.p - p).magnitude;
                        if (d >= lp.r) continue;
                        float w = 1f - d / lp.r; w *= w * lp.k;
                        lr += lp.c.r * w; lg += lp.c.g * w; lbl += lp.c.b * w;
                    }
                mb.cols[v] = new Color32((byte)Mathf.Min(255, c.r * lr), (byte)Mathf.Min(255, c.g * lg), (byte)Mathf.Min(255, c.b * lbl), c.a);
            }
        }

        // ================================================================ DECOR
        void Decorate(Run r, MeshBuilder mb, List<Lamp> lamps, System.Random rng)
        {
            var e = r.e;
            float u() => (float)rng.NextDouble();
            Color32 glow = new Color(e.glow.r, e.glow.g, e.glow.b, 0f), tip = new Color(Mathf.Lerp(e.glow.r, 1f, 0.6f), Mathf.Lerp(e.glow.g, 1f, 0.6f), Mathf.Lerp(e.glow.b, 1f, 0.6f), 0f);
            Color rock = Muted(e.rock), rockTop = Muted(e.rockTop);
            bool burrow = e.kind == CaveKind.Burrow, ruin = e.kind == CaveKind.Ruin;
            bool shrooms = burrow || e.biome == BiomeId.Meadow || e.biome == BiomeId.Swamp || e.biome == BiomeId.Glow;

            // which room each wall segment belongs to (or -1: a tunnel)
            var segRoom = new int[r.segs.Count];
            for (int s = 0; s < r.segs.Count; s++)
            {
                var sg = r.segs[s]; var m = new Vector2((sg.x + sg.z) * 0.5f, (sg.y + sg.w) * 0.5f);
                segRoom[s] = -1;
                float best = 1e9f;
                for (int k = 0; k < r.rooms.Count; k++)
                {
                    float d = (r.rooms[k].c - m).magnitude / (r.rooms[k].r * 1.45f);
                    if (d < 1f && d < best) { best = d; segRoom[s] = k; }
                }
            }
            Vector3 WallSpot(int s, float inward, out Vector2 n)
            {
                var sg = r.segs[s]; n = r.segNormals[s];
                float t = 0.2f + u() * 0.6f;
                float x = Mathf.Lerp(sg.x, sg.z, t) + n.x * inward, z = Mathf.Lerp(sg.y, sg.w, t) + n.y * inward;
                return new Vector3(x, FloorAt2(r, x, z), z);
            }
            int RandomSeg(int room)
            {
                for (int k = 0; k < 60; k++) { int s = rng.Next(r.segs.Count); if (segRoom[s] == room) return s; }
                return -1;
            }
            // light is mostly white with the glow's tint, so a green crystal doesn't paint the whole room green
            void AddLamp(Vector3 p, Color c, float rad, float k) => lamps.Add(new Lamp { p = p, c = Color.Lerp(Color.white, c, 0.5f), r = rad, k = k * 0.85f });

            void CrystalCluster(Vector3 p, Vector2 n, float s, Color32 g, Color32 t)
            {
                int cnt = 3 + rng.Next(0, 4);
                for (int q = 0; q < cnt; q++)
                {
                    float a = u() * 6.28f, d = u() * 0.5f * s;
                    var o = p + new Vector3(Mathf.Cos(a) * d, -0.15f, Mathf.Sin(a) * d);
                    float h = (0.7f + u() * 1.3f) * s * (q == 0 ? 1.4f : 1f);
                    // lean away from the rock
                    var lean = Quaternion.Euler(n.y * (15f + u() * 30f), u() * 360f, -n.x * (15f + u() * 30f));
                    mb.Crystal(Matrix4x4.TRS(o, lean, new Vector3(h * 0.32f, h, h * 0.32f)), g, t);
                }
                mb.Rock(Matrix4x4.TRS(p + Vector3.up * 0.05f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(1.1f * s, 0.4f * s, 1f * s)), rock, rockTop, (uint)rng.Next(), 6, 4);
            }
            void Glowcaps(Vector3 p, float s)
            {
                Color32 cap = new Color(Mathf.Lerp(e.glow.r, 1f, 0.3f), Mathf.Lerp(e.glow.g, 1f, 0.3f), Mathf.Lerp(e.glow.b, 1f, 0.3f), 0f);
                for (int q = 0; q < 5; q++)
                {
                    var o = p + new Vector3(Mathf.Cos(q * 1.9f) * (0.3f + q * 0.18f) * s, 0f, Mathf.Sin(q * 1.9f) * (0.3f + q * 0.18f) * s);
                    float hh = (0.25f + (q % 3) * 0.2f) * s;
                    mb.Cylinder(Matrix4x4.TRS(o + Vector3.up * hh * 0.5f, Quaternion.identity, new Vector3(0.08f * s, hh, 0.08f * s)), new Color(0.85f, 0.82f, 0.74f), 5);
                    mb.Dome(Matrix4x4.TRS(o + Vector3.up * hh, Quaternion.identity, new Vector3(0.42f * s, 0.24f * s, 0.42f * s)), cap, 8, 3);
                }
                AddLamp(p + Vector3.up * 0.8f, e.glow, 6.5f, 0.9f);
            }
            void Stalagmite(Vector3 p, float s, bool solid)
            {
                mb.Cone(Matrix4x4.TRS(p + Vector3.up * s * 1.5f, Quaternion.Euler(u() * 8f - 4f, 0, u() * 8f - 4f), new Vector3(s * 1.1f, s * 3f, s * 1.1f)), rock, 7);
                mb.Rock(Matrix4x4.TRS(p + Vector3.up * 0.15f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(s * 1.7f, s * 0.6f, s * 1.7f)), rock, rockTop, (uint)rng.Next(), 6, 4);
                if (u() < 0.6f) mb.Cone(Matrix4x4.TRS(p + new Vector3(s * 0.6f, s * 0.7f, s * 0.3f), Quaternion.Euler(0, 0, -10f), new Vector3(s * 0.55f, s * 1.4f, s * 0.55f)), rockTop, 6);
                if (solid) r.pillars.Add(new Vector4(p.x, p.z, s * 0.75f, 0));
            }

            for (int ri = 0; ri < r.rooms.Count; ri++)
            {
                var rm = r.rooms[ri];
                float area = rm.r / 9f;
                // crystals (and the light they give)
                int crystals = Mathf.RoundToInt((2 + rng.Next(0, 3)) * area) + (e.biome == BiomeId.Glow ? 2 : 0) + (rm.role == Role.Alpha || rm.role == Role.Mini ? 1 : 0) + (rm.role == Role.Hazard ? 2 : 0);
                if (ruin && rm.hall) crystals = 0;
                for (int k = 0; k < crystals; k++)
                {
                    int s = RandomSeg(ri); if (s < 0) break;
                    var p = WallSpot(s, 0.5f, out var n);
                    float sz = 0.6f + u() * 0.7f;
                    CrystalCluster(p, n, sz, glow, tip);
                    AddLamp(p + new Vector3(n.x, 0f, n.y) * 1.2f + Vector3.up * 1.4f, e.glow, 9f + sz * 3f, 1.15f);
                    r.pillars.Add(new Vector4(p.x, p.z, 0.5f * sz, 0));
                }
                // glowcaps
                if (shrooms)
                {
                    int caps = 1 + rng.Next(0, 3);
                    for (int k = 0; k < caps; k++) { int s = RandomSeg(ri); if (s < 0) break; Glowcaps(WallSpot(s, 0.8f, out _), 0.7f + u() * 0.6f); }
                }
                // rubble along the foot of the rock
                if (!(ruin && rm.hall))
                    for (int s = 0; s < r.segs.Count; s++)
                    {
                        if (segRoom[s] != ri || u() > 0.17f) continue;
                        var p = WallSpot(s, 0.25f + u() * 0.4f, out _);
                        float sz = 0.3f + u() * u() * 1.2f;
                        mb.Rock(Matrix4x4.TRS(p + Vector3.up * sz * 0.2f, Quaternion.Euler(u() * 30f, u() * 360f, 0), new Vector3(sz * 1.3f, sz * 0.8f, sz * 1.1f)), rock * 0.9f, rockTop, (uint)rng.Next(), 5, 3, 0.25f);
                    }
                // stalagmites: clumps near the walls, the odd one out in the open; none in the mouth chamber's middle
                if (!(ruin && rm.hall))
                {
                    int mites = Mathf.RoundToInt((2 + rng.Next(0, 4)) * area);
                    for (int k = 0; k < mites; k++)
                    {
                        int s = RandomSeg(ri); if (s < 0) break;
                        var p = WallSpot(s, 1.4f + u() * 1.6f, out _);
                        if (Open(p) < 0.9f) continue;
                        float sz = 0.45f + u() * 0.75f;
                        Stalagmite(p, sz, sz > 0.6f);
                        if (u() < 0.5f) Stalagmite(p + new Vector3(u() * 1.6f - 0.8f, 0f, u() * 1.6f - 0.8f), sz * 0.5f, false);
                    }
                    if (rm.role != Role.Entrance && rm.role != Role.Alpha && rm.role != Role.Boss && rm.role != Role.Mini && u() < 0.6f)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.6f, 3f, rng);
                        Stalagmite(p, 0.8f + u() * 0.5f, true);
                    }
                    // a boulder or two
                    int boulders = rng.Next(0, 3);
                    for (int k = 0; k < boulders; k++)
                    {
                        int s = RandomSeg(ri); if (s < 0) break;
                        var p = WallSpot(s, 1.2f, out _);
                        float sz = 1.3f + u() * 1.4f;
                        mb.Rock(Matrix4x4.TRS(p + Vector3.up * sz * 0.3f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(sz * 1.2f, sz * 0.85f, sz)), rock, rockTop, (uint)rng.Next(), 7, 5);
                        r.pillars.Add(new Vector4(p.x, p.z, sz * 0.5f, 0));
                    }
                }
                // stalactites hang under the tall domes only (a camera above a low ceiling would see them floating)
                if (!burrow && !(ruin && rm.hall) && rm.ceil >= 9f)
                {
                    int tites = Mathf.RoundToInt((4 + rng.Next(0, 6)) * area);
                    for (int k = 0; k < tites; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.75f, 1.5f, rng);
                        float top = CeilAt(r, p.x, p.z);
                        if (top - p.y < 7f) continue;
                        float len = 1.2f + u() * 2.6f, w = len * 0.28f;
                        mb.Cone(Matrix4x4.TRS(new Vector3(p.x, top - len * 0.5f + 0.2f, p.z), Quaternion.Euler(180f, 0, 0), new Vector3(w, len, w)), rockTop, 6);
                        if (e.biome == BiomeId.Peaks || e.biome == BiomeId.Glow) mb.Cone(Matrix4x4.TRS(new Vector3(p.x + w * 0.6f, top - len * 0.35f, p.z), Quaternion.Euler(180f, 0, 0), new Vector3(w * 0.4f, len * 0.6f, w * 0.4f)), tip, 5);
                    }
                }
                // roots through the roof of a den
                if (burrow)
                {
                    int roots = 3 + rng.Next(0, 4);
                    for (int k = 0; k < roots; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.8f, 1f, rng);
                        float top = CeilAt(r, p.x, p.z), len = 0.6f + u() * 1.1f;
                        var rootRot = Quaternion.Euler(u() * 30f - 15f, u() * 360f, u() * 30f - 15f);
                        mb.Cylinder(Matrix4x4.TRS(new Vector3(p.x, top - len * 0.5f, p.z), rootRot, new Vector3(0.07f, len, 0.07f)), new Color(0.38f, 0.27f, 0.17f), 4, 0.3f, false);
                        mb.Cylinder(Matrix4x4.TRS(new Vector3(p.x, top - len * 0.35f, p.z) + rootRot * new Vector3(0.12f, 0f, 0f), rootRot * Quaternion.Euler(0, 0, 35f), new Vector3(0.04f, len * 0.5f, 0.04f)), new Color(0.42f, 0.3f, 0.19f), 4, 0.3f, false);
                    }
                }
                // puddles in the low spots
                // still water in the low spots (none in the dry lands); dark, with a faint shine of the cave's glow
                bool dry = e.biome == BiomeId.Badlands || e.biome == BiomeId.Desert;
                int puddles = dry ? 0 : e.biome == BiomeId.Isles || e.biome == BiomeId.Swamp ? 2 + rng.Next(0, 2) : rng.Next(0, 2);
                for (int k = 0; k < puddles; k++)
                {
                    var p = OpenSpot(r, rm.c, rm.r * 0.8f, 2.2f, rng);
                    float sz = 1.2f + u() * 1.8f;
                    Color wc = e.biome == BiomeId.Swamp ? new Color(0.13f, 0.15f, 0.09f) : e.biome == BiomeId.Blight ? new Color(0.18f, 0.08f, 0.22f) : new Color(0.06f, 0.1f, 0.15f);
                    var rot = Quaternion.Euler(0, u() * 360f, 0);
                    float sx = sz, sz2 = sz * (0.6f + u() * 0.4f);
                    mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.015f, rot, new Vector3(sx, 0.02f, sz2)), wc, 12, 1f, true);
                    mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.03f, rot, new Vector3(sx * 0.35f, 0.01f, sz2 * 0.2f)), new Color(e.glow.r * 0.5f, e.glow.g * 0.5f, e.glow.b * 0.5f, 0f), 8, 1f, true);
                }
                // bones where big things live
                if (rm.role == Role.Alpha || rm.role == Role.Boss || rm.role == Role.Mini || rm.role == Role.Eggs || (burrow && u() < 0.6f))
                    for (int k = 0; k < 3 + rng.Next(0, 4); k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.85f, 1.2f, rng);
                        Color32 bone = new Color(0.88f, 0.85f, 0.76f);
                        if (k == 0) { mb.Sphere(Matrix4x4.TRS(p + Vector3.up * 0.2f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(0.5f, 0.42f, 0.6f)), bone, 8, 5); continue; }
                        mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.06f, Quaternion.Euler(90f, u() * 360f, 0), new Vector3(0.1f, 0.5f + u() * 0.5f, 0.1f)), bone, 5);
                    }
                // the way you came in: a shaft of daylight through a crack in the roof, ferns under it
                if (rm.role == Role.Entrance)
                {
                    // off to one side of where you arrive, so you walk past it rather than through it
                    var sc = new Vector2(rm.c.x + rm.r * 0.1f, rm.c.y + rm.r * 0.45f);
                    if (Sample(r.F, sc.x, sc.y) < 2f) sc = new Vector2(rm.c.x + rm.r * 0.1f, rm.c.y - rm.r * 0.45f);
                    if (Sample(r.F, sc.x, sc.y) < 2f) sc = rm.c + new Vector2(rm.r * 0.3f, 0f);
                    var sp = new Vector3(sc.x, FloorAt2(r, sc.x, sc.y), sc.y);
                    float top = CeilAt(r, sp.x, sp.z);
                    r.shaft = new Vector4(sp.x, sp.z, top, 0f);
                    AddLamp(sp + Vector3.up * 1.2f, new Color(0.95f, 0.97f, 1f), 13f, 1.4f);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 1.05f + u();
                        var fp = sp + new Vector3(Mathf.Cos(a) * (1f + u() * 1.4f), 0f, Mathf.Sin(a) * (1f + u() * 1.4f));
                        mb.Cone(Matrix4x4.TRS(fp + Vector3.up * 0.25f, Quaternion.Euler(u() * 30f - 15f, u() * 360f, u() * 30f - 15f), new Vector3(0.12f, 0.6f, 0.04f)), new Color(0.35f, 0.6f, 0.3f), 4);
                    }
                }
                // ruin halls: corner pillars and braziers
                if (ruin && rm.hall)
                {
                    float hx = rm.rx * 0.86f - 1.1f, hz = rm.rz * 0.92f - 1.1f;
                    for (int cx = -1; cx <= 1; cx += 2)
                    for (int cz = -1; cz <= 1; cz += 2)
                    {
                        var cp = new Vector3(rm.c.x + cx * hx, FloorY, rm.c.y + cz * hz);
                        float top = CeilAt(r, cp.x, cp.z);
                        bool broken = u() < 0.35f;
                        float h = broken ? 2f + u() * 2.5f : top - FloorY;
                        mb.Box(Matrix4x4.TRS(cp + Vector3.up * h * 0.5f, Quaternion.identity, new Vector3(1.3f, h, 1.3f)), rock, rockTop);
                        mb.Box(Matrix4x4.TRS(cp + Vector3.up * 0.3f, Quaternion.identity, new Vector3(1.8f, 0.6f, 1.8f)), Color.Lerp(rock, Color.white, 0.2f));
                        if (broken) mb.Cylinder(Matrix4x4.TRS(cp + new Vector3(cx * -1.8f, 0.55f, cz * 0.6f), Quaternion.Euler(0, u() * 90f, 90f), new Vector3(1.1f, 2.4f, 1.1f)), rock, 8, 1f, true);
                        r.pillars.Add(new Vector4(cp.x, cp.z, 0.95f, 0));
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        var bp = new Vector3(rm.c.x + (k == 0 ? -hx * 0.5f : hx * 0.5f), FloorY, rm.c.y + hz + 0.2f);
                        mb.Cylinder(Matrix4x4.TRS(bp + Vector3.up * 0.75f, Quaternion.identity, new Vector3(0.45f, 1.5f, 0.45f)), rockTop, 8);
                        mb.Cylinder(Matrix4x4.TRS(bp + Vector3.up * 1.62f, Quaternion.identity, new Vector3(1.1f, 0.32f, 1.1f)), Color.Lerp(rock, Color.white, 0.2f), 8, 1.4f);
                        mb.Sphere(Matrix4x4.TRS(bp + Vector3.up * 2.1f, Quaternion.identity, new Vector3(0.85f, 1.15f, 0.85f)), tip, glow, 8, 5);
                        AddLamp(bp + Vector3.up * 2f, Color.Lerp(e.glow, new Color(1f, 0.7f, 0.4f), 0.5f), 12f, 1.2f);
                        r.pillars.Add(new Vector4(bp.x, bp.z, 0.6f, 0));
                    }
                    // fallen blocks, lying against the walls (the middle of a hall is where things fight and chests stand)
                    for (int k = 0; k < 3; k++)
                    {
                        int s = RandomSeg(ri); if (s < 0) break;
                        var p = WallSpot(s, 1.6f, out _);
                        mb.Box(Matrix4x4.TRS(p + Vector3.up * 0.35f, Quaternion.Euler(u() * 20f, u() * 360f, u() * 20f), new Vector3(1f + u(), 0.7f, 0.8f + u() * 0.6f)), rock, rockTop);
                    }
                }
                BiomeDecor(r, rm, mb, lamps, rng);
            }

            // tunnels: rubble, the odd crystal for light
            for (int s = 0; s < r.segs.Count; s++)
            {
                if (segRoom[s] >= 0) continue;
                float roll = u();
                if (roll < 0.035f)
                {
                    var p = WallSpot(s, 0.4f, out var n);
                    CrystalCluster(p, n, 0.45f + u() * 0.3f, glow, tip);
                    AddLamp(p + new Vector3(n.x, 0f, n.y) + Vector3.up * 1.2f, e.glow, 8f, 1.05f);
                }
                else if (roll < 0.06f && shrooms) Glowcaps(WallSpot(s, 0.6f, out _), 0.55f);
                else if (roll < 0.17f)
                {
                    var p = WallSpot(s, 0.3f, out _);
                    float sz = 0.25f + u() * 0.6f;
                    mb.Rock(Matrix4x4.TRS(p + Vector3.up * sz * 0.2f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(sz * 1.3f, sz * 0.8f, sz)), rock * 0.85f, rockTop, (uint)rng.Next(), 5, 3, 0.25f);
                }
            }
        }

        /// <summary>One more thing per chamber that says which land this is.</summary>
        void BiomeDecor(Run r, Room rm, MeshBuilder mb, List<Lamp> lamps, System.Random rng)
        {
            var e = r.e;
            float u() => (float)rng.NextDouble();
            switch (e.biome)
            {
                case BiomeId.Badlands:
                    // seams of lava glowing through cracks in the floor
                    for (int k = 0; k < 2; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.7f, 2.5f, rng);
                        float a = u() * 180f;
                        for (int q = 0; q < 4; q++)
                        {
                            var o = p + Quaternion.Euler(0, a + q * 25f, 0) * new Vector3(0, 0, q * 0.9f);
                            mb.Box(Matrix4x4.TRS(o + Vector3.up * 0.03f, Quaternion.Euler(0, a + q * 25f, 0), new Vector3(0.18f, 0.05f, 1.1f)), new Color32(255, 120, 40, 0));
                        }
                        lamps.Add(new Lamp { p = p + Vector3.up * 0.8f, c = new Color(1f, 0.45f, 0.15f), r = 7f, k = 1.1f });
                    }
                    break;
                case BiomeId.Peaks:
                    // frost on the floor and blue ice spears
                    for (int k = 0; k < 3; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.85f, 1.2f, rng);
                        mb.Crystal(Matrix4x4.TRS(p, Quaternion.Euler(u() * 30f - 15f, 0, u() * 30f - 15f), new Vector3(0.25f, 0.9f + u(), 0.25f)), new Color(0.75f, 0.9f, 1f), Color.white);
                    }
                    break;
                case BiomeId.Isles:
                    // coral fans by the pools
                    for (int k = 0; k < 4; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.85f, 1.2f, rng);
                        Color32 cc = k % 2 == 0 ? new Color(1f, 0.5f, 0.55f) : new Color(1f, 0.75f, 0.4f);
                        for (int q = 0; q < 3; q++)
                            mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.35f, Quaternion.Euler(u() * 50f - 25f, u() * 360f, u() * 50f - 25f), new Vector3(0.08f, 0.7f, 0.08f)), cc, 4, 0.5f, false);
                    }
                    break;
                case BiomeId.Desert:
                    // sand drifts and a sun-bleached skull
                    for (int k = 0; k < 2; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.8f, 1f, rng);
                        mb.Dome(Matrix4x4.TRS(p, Quaternion.Euler(0, u() * 360f, 0), new Vector3(2.6f + u() * 2f, 0.5f, 1.6f + u())), new Color(0.78f, 0.66f, 0.46f), 10, 3);
                    }
                    break;
                case BiomeId.Blight:
                    // ooze dripping down the walls, pools glowing violet
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.7f, 2f, rng);
                        mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 0.05f, Quaternion.identity, new Vector3(2f, 0.06f, 1.5f)), new Color32(150, 60, 200, 0), 12, 1f, true);
                        lamps.Add(new Lamp { p = p + Vector3.up * 0.6f, c = new Color(0.7f, 0.3f, 1f), r = 7f, k = 0.9f });
                    }
                    break;
                case BiomeId.Glow:
                    // star specks over the roof (seen from below only)
                    for (int k = 0; k < 18; k++)
                    {
                        var p = OpenSpot(r, rm.c, rm.r * 0.9f, 0.5f, rng);
                        float top = CeilAt(r, p.x, p.z);
                        mb.Sphere(Matrix4x4.TRS(new Vector3(p.x, top - 0.05f, p.z), Quaternion.identity, Vector3.one * (0.06f + u() * 0.08f)), new Color32(220, 240, 255, 0), 4, 3);
                    }
                    break;
            }
        }

        static Color BiomeTint(BiomeId b) => BiomeInfo.Tint(b);

        // ================================================================ MINIMAP
        static void BuildMap(Run r)
        {
            var e = r.e;
            r.map = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "cave_map" };
            var px = new Color32[N * N];
            r.hidden = HiddenCells(r);
            Color32 fl = Color.Lerp(e.floor, Color.white, 0.4f), edge = Color.Lerp(e.rock, Color.white, 0.15f), bg = new Color(0.03f, 0.03f, 0.05f, 1f);
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float f = Sample(r.F, Origin.x + (i + 0.5f) * Res, Origin.y + (j + 0.5f) * Res);
                px[j * N + i] = r.hidden[j * N + i] ? bg : f > 0.6f ? fl : f > 0f ? edge : bg;
            }
            // guardian chambers in red, egg nests in gold
            foreach (var rm in r.rooms)
            {
                Color32 tint = rm.role == Role.Alpha || rm.role == Role.Boss ? new Color(0.8f, 0.35f, 0.3f) : rm.role == Role.Mini ? new Color(0.85f, 0.5f, 0.35f) : rm.role == Role.Eggs ? new Color(0.9f, 0.78f, 0.4f)
                    : rm.role == Role.Puzzle || rm.role == Role.Vault ? new Color(0.45f, 0.8f, 0.9f) : rm.role == Role.Secret ? new Color(0.75f, 0.55f, 0.95f) : rm.role == Role.Descent || rm.role == Role.Landing ? new Color(0.6f, 0.9f, 0.65f) : default;
                if (tint.a == 0) continue;
                int i0 = Mathf.Max(0, Mathf.FloorToInt((rm.c.x - rm.r * 1.4f - Origin.x) / Res)), i1 = Mathf.Min(N - 1, Mathf.CeilToInt((rm.c.x + rm.r * 1.4f - Origin.x) / Res));
                int j0 = Mathf.Max(0, Mathf.FloorToInt((rm.c.y - rm.r * 1.4f - Origin.y) / Res)), j1 = Mathf.Min(N - 1, Mathf.CeilToInt((rm.c.y + rm.r * 1.4f - Origin.y) / Res));
                for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    float x = Origin.x + (i + 0.5f) * Res, z = Origin.y + (j + 0.5f) * Res;
                    if (!r.hidden[j * N + i] && Sample(r.F, x, z) > 0.6f && (new Vector2(x, z) - rm.c).magnitude < rm.r * 1.2f) px[j * N + i] = tint;
                }
            }
            // what you haven't been near yet is drawn dim (Caves.Secrets: RevealAround)
            r.mapBase = px;
            if (r.seen == null) r.seen = new bool[N * N];
            r.mapPx = new Color32[N * N];
            for (int k = 0; k < px.Length; k++) r.mapPx[k] = r.seen[k] ? px[k] : Dim(px[k]);
            r.map.SetPixels32(r.mapPx);
            r.map.Apply(false, false);
        }
    }
}
