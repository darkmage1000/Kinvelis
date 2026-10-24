using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Where relics sit (user, 2026-10-24: "I've seen some half way up a mountain, some just randomly in the plains,
    /// some under water - I envision them at mountain peaks, in a forest, on a small rock in the water, so it looks like
    /// we designed the map intentionally"). The old island is scanned on a 10 m grid for six kinds of place, each one
    /// that reads as chosen from a distance, and every relic gets a small built setting that says so:
    ///   Summit - the top of a real mountain (highest point within 50 m, 16 m+ above the ground 40 m round) - a cairn
    ///   Mesa   - a flat Badlands top with sheer sides - a stone altar
    ///   Crown  - the top of a distinct hill (9 m+ above everything 40 m round) - a ring of standing stones
    ///   Grove  - the densest heart of a forest (WorldGen.Forest peak) - a mossy stump in a clearing ringed by mushrooms
    ///   Islet  - open water 10-38 m from the shore of a lake or bay - a rock rising out of the water
    ///   Canyon - a Badlands floor walled in on most sides - a stone arch
    /// Settlements, Warden rings, landmarks, waystones and the start keep their distance. Relic ids are unchanged, so
    /// relics already found stay found; ones not yet found simply wait somewhere better. Settings stay after the relic is
    /// taken (an empty cairn is part of the map). Trees and rocks don't grow inside a setting (BlocksNode).
    /// </summary>
    public partial class Relics
    {
        public enum Setting : byte { Summit, Mesa, Crown, Grove, Islet, Canyon }

        /// <summary>Places to keep clear of, snapshot on the main thread before placement runs.</summary>
        static List<Vector4> AvoidSnapshot()
        {
            var a = new List<Vector4>();
            foreach (var s in Settlements.All) a.Add(new Vector4(s.pos.x, 0, s.pos.z, Settlements.Radius + 35f));
            foreach (var w in Wardens.All) a.Add(new Vector4(w.pos.x, 0, w.pos.z, Wardens.ArenaRadius + 25f));
            foreach (var l in Landmarks.List) a.Add(new Vector4(l.pos.x, 0, l.pos.z, 30f));
            if (Waystones.I != null) foreach (var w in Waystones.I.All) a.Add(new Vector4(w.pos.x, 0, w.pos.z, 25f));
            var st = WorldGen.StartSpawn;
            a.Add(new Vector4(st.x, 0, st.z, 90f));
            return a;
        }

        static bool Avoided(List<Vector4> avoid, float x, float z)
        {
            foreach (var v in avoid) { float dx = x - v.x, dz = z - v.z; if (dx * dx + dz * dz < v.w * v.w) return true; }
            return false;
        }

        struct Cand { public Setting set; public Vector3 basePos; public float score; public string where; }

        static readonly Vector2[] Dirs16 = MakeDirs(16), Dirs8 = MakeDirs(8);
        static Vector2[] MakeDirs(int n)
        {
            var d = new Vector2[n];
            for (int i = 0; i < n; i++) d[i] = new Vector2(Mathf.Cos(i * Mathf.PI * 2f / n), Mathf.Sin(i * Mathf.PI * 2f / n));
            return d;
        }

        /// <summary>How many places of each kind the last placement found (for checking a world).</summary>
        public static string PlacementReport = "";

        static List<Relic> Generate(int seed, List<Vector4> avoid)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var rng = new System.Random(seed * 7919 + 17);
            var cands = new List<Cand>[6];
            for (int i = 0; i < cands.Length; i++) cands[i] = new List<Cand>();

            const float Step = 10f, R = 1560f;
            int n = (int)(R * 2f / Step) + 1;
            var hg = new float[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float x = -R + i * Step, z = -R + j * Step;
                    hg[i, j] = x * x + z * z <= R * R ? WorldGen.Height(x, z) : -999f;
                }

            // heights from the grid, between its points: plenty for walls and shorelines, and far cheaper than WorldGen
            float G(float x, float z)
            {
                float fx = Mathf.Clamp((x + R) / Step, 0f, n - 1.001f), fz = Mathf.Clamp((z + R) / Step, 0f, n - 1.001f);
                int ix = (int)fx, iz = (int)fz; float tx = fx - ix, tz = fz - iz;
                float a = hg[ix, iz], b = hg[ix + 1, iz], c = hg[ix, iz + 1], d = hg[ix + 1, iz + 1];
                if (a == -999f || b == -999f || c == -999f || d == -999f) return -10f;
                return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
            }
            var tops = new List<Vector3>();
            for (int i = 2; i < n - 2; i++)
                for (int j = 2; j < n - 2; j++)
                {
                    float h = hg[i, j];
                    if (h == -999f) continue;
                    float x = -R + i * Step, z = -R + j * Step;

                    // ---- tops: summits, mesas, hill crowns
                    if (h > 5f && IsGridMax(hg, i, j))
                    {
                        Vector3 top = Climb(x, z);
                        if (Near(tops, top, 60f) || Avoided(avoid, top.x, top.z)) goto Water;
                        tops.Add(top);
                        if (!HighestAround(top)) goto Water;
                        float ring = float.MaxValue;
                        foreach (var d in Dirs16) ring = Mathf.Min(ring, WorldGen.Height(top.x + d.x * 40f, top.z + d.y * 40f));
                        float prom = top.y - ring;
                        var biome = WorldGen.Biome(top.x, top.z);
                        int sheer = 0;
                        foreach (var d in Dirs16) if (WorldGen.Height(top.x + d.x * 20f, top.z + d.y * 20f) < top.y - 6f) sheer++;
                        if (biome == BiomeId.Badlands && top.y > 16f && sheer >= 10 && Flat(top.x, top.z, top.y, 1.2f, 3f))
                            cands[(int)Setting.Mesa].Add(new Cand { set = Setting.Mesa, basePos = top, score = prom, where = "a mesa top" });
                        else if (top.y > 40f && prom > 16f)
                            cands[(int)Setting.Summit].Add(new Cand { set = Setting.Summit, basePos = top, score = top.y + prom, where = "a mountain summit" });
                        else if (prom >= 9f && Flat(top.x, top.z, top.y, 1.6f, 1.5f))
                            cands[(int)Setting.Crown].Add(new Cand { set = Setting.Crown, basePos = top, score = prom, where = "the crown of a hill" });
                    }

                    Water:
                    // ---- islets: open water a short swim from the shore of a lake or bay
                    if (h < -0.9f && h > -5f && (i & 1) == 0 && (j & 1) == 0 && !Avoided(avoid, x, z))
                    {
                        float nearest = float.MaxValue; int hits = 0;
                        foreach (var d in Dirs16)
                            for (float r = 4f; r <= 80f; r += 4f)
                                if (G(x + d.x * r, z + d.y * r) > 0.3f) { hits++; nearest = Mathf.Min(nearest, r); break; }
                        if (nearest >= 10f && nearest <= 38f && hits >= 4)
                        {
                            bool lake = hits >= 11;
                            cands[(int)Setting.Islet].Add(new Cand { set = Setting.Islet, basePos = new Vector3(x, h, z), score = hits + (lake ? 10f : 0f),
                                where = lake ? "a rock in the middle of a lake" : "a rock just off the shore" });
                        }
                        continue;
                    }
                    if (h < 2.5f || Avoided(avoid, x, z)) continue;

                    // ---- groves: the densest heart of a forest
                    var b = WorldGen.Biome(x, z);
                    if ((b == BiomeId.Meadow && h < 60f) || (b == BiomeId.Peaks && h < 70f))
                    {
                        float f = WorldGen.Forest(x, z);
                        if (f >= (b == BiomeId.Meadow ? 0.82f : 0.86f) && ForestPeak(x, z, f) && Flat(x, z, h, 1.0f, 2.5f))
                            cands[(int)Setting.Grove].Add(new Cand { set = Setting.Grove, basePos = new Vector3(x, h, z), score = f,
                                where = b == BiomeId.Peaks ? "the heart of a pine forest" : "the heart of a forest" });
                    }

                    // ---- canyon floors: a level floor with walls rising on two opposite sides (and higher ground round most of it)
                    if (b == BiomeId.Badlands)
                    {
                        bool slot = false; int walls = 0;
                        for (int d = 0; d < 4; d++)
                            if (G(x + Dirs8[d].x * 18f, z + Dirs8[d].y * 18f) > h + 8f && G(x - Dirs8[d].x * 18f, z - Dirs8[d].y * 18f) > h + 8f) slot = true;
                        foreach (var d in Dirs8) if (G(x + d.x * 16f, z + d.y * 16f) > h + 5f) walls++;
                        if (slot && walls >= 4 && Flat(x, z, h, 1.2f, 1.5f)) cands[(int)Setting.Canyon].Add(new Cand { set = Setting.Canyon, basePos = new Vector3(x, h, z), score = walls, where = "a canyon shrine" });
                    }
                }

            PlacementReport = "candidates:";
            for (int i = 0; i < cands.Length; i++) PlacementReport += " " + (Setting)i + "=" + cands[i].Count;
            // which places suit which relic (fallbacks follow)
            var prefs = new Dictionary<RelicType, Setting[]>
            {
                // roughly: the Peaks get summits, the Badlands mesas and canyons, the Sunmeadow groves, hill crowns and lakes
                { RelicType.CragClaw, new[] { Setting.Mesa, Setting.Summit, Setting.Canyon, Setting.Crown } },
                { RelicType.Windfeather, new[] { Setting.Summit, Setting.Crown, Setting.Mesa } },
                { RelicType.Tidepearl, new[] { Setting.Islet, Setting.Grove, Setting.Crown } },
                { RelicType.Hearthmoss, new[] { Setting.Grove, Setting.Crown, Setting.Canyon } },
                { RelicType.Kinspring, new[] { Setting.Canyon, Setting.Mesa, Setting.Crown, Setting.Grove } },
                { RelicType.Tetherbloom, new[] { Setting.Grove, Setting.Crown, Setting.Islet, Setting.Summit } },
            };
            // the best of each kind first, with a little shuffle so the same few never always win
            foreach (var l in cands)
            {
                var jit = new Dictionary<Vector3, float>();
                foreach (var c in l) jit[c.basePos] = c.score * (0.8f + 0.4f * (float)rng.NextDouble());
                l.Sort((a, c) => jit[c.basePos].CompareTo(jit[a.basePos]));
            }
            var result = new List<Relic>();
            for (float apart = 220f; apart >= 90f; apart -= 40f)
                for (int k = 0; k < PerType; k++)
                    for (int t = 0; t < IslandTypes; t++)
                    {
                        string id = t + "_" + k;
                        if (result.Exists(r => r.id == id)) continue;
                        foreach (var s in prefs[(RelicType)t])
                        {
                            int pick = cands[(int)s].FindIndex(c => !TooClose(result, c.basePos, apart));
                            if (pick < 0) continue;
                            var c = cands[(int)s][pick];
                            cands[(int)s].RemoveAt(pick);
                            result.Add(Make(id, (RelicType)t, c.set, c.basePos, c.where));
                            break;
                        }
                    }
            PlaceOuter(seed, result);
            PlacementReport += $" | {watch.ElapsedMilliseconds} ms";
            return result;
        }

        /// <summary>Height above the setting's base where the relic floats.</summary>
        static float Lift(Setting s)
        {
            switch (s)
            {
                case Setting.Summit: return 2.0f;
                case Setting.Mesa: return 1.8f;
                case Setting.Grove: return 1.6f;
                case Setting.Islet: return 1.0f;
                default: return 1.45f;
            }
        }

        static Relic Make(string id, RelicType type, Setting set, Vector3 basePos, string where)
        {
            // an islet's base is the top of its rock, a little above the water; its seabed is kept for the rock
            float seabed = basePos.y;
            if (set == Setting.Islet) basePos.y = WorldGen.WaterLevel + 0.75f;
            return new Relic { id = id, type = type, setting = set, basePos = basePos, seabed = seabed, pos = basePos + Vector3.up * Lift(set), spot = where,
                clear = set == Setting.Grove ? 6f : set == Setting.Crown ? 5f : set == Setting.Canyon ? 3.5f : 2.8f };
        }

        static bool IsGridMax(float[,] hg, int i, int j)
        {
            float h = hg[i, j];
            for (int a = -1; a <= 1; a++)
                for (int b = -1; b <= 1; b++)
                    if ((a != 0 || b != 0) && hg[i + a, j + b] > h) return false;
            return true;
        }

        /// <summary>Walks uphill in 2 m steps to the very top.</summary>
        static Vector3 Climb(float x, float z)
        {
            float h = WorldGen.Height(x, z);
            for (int step = 0; step < 60; step++)
            {
                float bx = x, bz = z, bh = h;
                foreach (var d in Dirs8)
                {
                    float qx = x + d.x * 2f, qz = z + d.y * 2f, qh = WorldGen.Height(qx, qz);
                    if (qh > bh) { bx = qx; bz = qz; bh = qh; }
                }
                if (bh <= h + 0.01f) break;
                x = bx; z = bz; h = bh;
            }
            return new Vector3(x, h, z);
        }

        /// <summary>Nothing higher within 50 m: a top, not a bump on the shoulder of something bigger.</summary>
        static bool HighestAround(Vector3 top)
        {
            foreach (var d in Dirs16)
                for (float r = 12f; r <= 50f; r += 12f)
                    if (WorldGen.Height(top.x + d.x * r, top.z + d.y * r) > top.y + 0.2f) return false;
            return true;
        }

        static bool ForestPeak(float x, float z, float f)
        {
            foreach (var d in Dirs8) if (WorldGen.Forest(x + d.x * 20f, z + d.y * 20f) > f) return false;
            return true;
        }

        static bool Near(List<Vector3> l, Vector3 p, float r)
        {
            foreach (var q in l) { float dx = q.x - p.x, dz = q.z - p.z; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        /// <summary>Level within tol everywhere round a circle of radius rad.</summary>
        static bool Flat(float x, float z, float h, float tol, float rad = 1.5f)
        {
            foreach (var d in Dirs8) if (Mathf.Abs(WorldGen.Height(x + d.x * rad, z + d.y * rad) - h) > tol) return false;
            return true;
        }

        static bool TooClose(List<Relic> list, Vector3 p, float min)
        {
            float m2 = min * min;
            foreach (var r in list) { float dx = r.pos.x - p.x, dz = r.pos.z - p.z; if (dx * dx + dz * dz < m2) return true; }
            return false;
        }

        /// <summary>
        /// One Wardstone, Kinheart and Glintstone on each outer land, on its most prominent heights, well apart and away
        /// from where you land. Uses its own random stream, so these never move with the island's. A lone height gets a
        /// cairn, high ground a ring of stones.
        /// </summary>
        static void PlaceOuter(int seed, List<Relic> result)
        {
            for (int li = 0; li < WorldGen.Lands.Length; li++)
            {
                var land = WorldGen.Lands[li];
                var rng = new System.Random(seed * 6151 + 101 * (li + 1));
                var gate = WorldGen.LandGate(land);
                var peaks = new HashSet<Vector2Int>();
                var cands = new List<(Vector3 p, int drop)>();
                for (int n = 0; n < 1400; n++)
                {
                    float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float rad = Mathf.Sqrt((float)rng.NextDouble()) * land.radius * 0.95f;
                    float px = land.center.x + Mathf.Cos(ang) * rad, pz = land.center.y + Mathf.Sin(ang) * rad;
                    float ph = WorldGen.Height(px, pz);
                    if (ph < 2f || WorldGen.Biome(px, pz) != land.biome) continue;
                    for (int step = 0; step < 40; step++)
                    {
                        float bx = px, bz = pz, bh = ph;
                        foreach (var d in Dirs8)
                        {
                            float qx = px + d.x * 7f, qz = pz + d.y * 7f, qh = WorldGen.Height(qx, qz);
                            if (qh > bh) { bx = qx; bz = qz; bh = qh; }
                        }
                        if (bh <= ph + 0.05f) break;
                        px = bx; pz = bz; ph = bh;
                    }
                    var key = new Vector2Int(Mathf.RoundToInt(px / 25f), Mathf.RoundToInt(pz / 25f));
                    if (!peaks.Add(key) || !Flat(px, pz, ph, 1.4f)) continue;
                    float gx = px - gate.x, gz = pz - gate.z;
                    if (gx * gx + gz * gz < 220f * 220f) continue;
                    int drop = 0;
                    foreach (var d in Dirs8) if (WorldGen.Height(px + d.x * 14f, pz + d.y * 14f) < ph - 2.5f) drop++;
                    cands.Add((new Vector3(px, ph, pz), drop * 10 + Mathf.Min(9, (int)(ph / 4f))));
                }
                cands.Sort((a, b) => b.drop.CompareTo(a.drop));
                var mine = new List<Relic>();
                string where = BiomeInfo.Name(land.biome);
                for (float apart = 320f; apart >= 60f && mine.Count < 3; apart *= 0.6f)
                    foreach (var c in cands)
                    {
                        if (mine.Count >= 3) break;
                        if (TooClose(mine, c.p, apart)) continue;
                        var type = (RelicType)(IslandTypes + (mine.Count + li) % 3);
                        bool lone = c.drop >= 60;
                        mine.Add(Make((int)type + "_" + li, type, lone ? Setting.Summit : Setting.Crown, c.p, (lone ? "a lone height in " : "high ground in ") + where));
                    }
                result.AddRange(mine);
            }
        }

        // ================================================================ SETTINGS (main thread)
        /// <summary>Trees and rocks keep out of a relic's setting (a clearing in a grove, an open summit).</summary>
        public static bool BlocksNode(Vector3 p)
        {
            var r = I;
            if (r == null) return false;
            r.EnsureReady();
            foreach (var rl in r.All)
            {
                float dx = p.x - rl.basePos.x, dz = p.z - rl.basePos.z;
                if (dx * dx + dz * dz < rl.clear * rl.clear) return true;
            }
            return false;
        }

        static Color32 C(float r, float g, float b) => new Color(r, g, b);

        GameObject MakeSetting(Relic r)
        {
            var mb = new MeshBuilder();
            var rng = new System.Random(r.id.GetHashCode());
            Color32 stone = C(0.56f, 0.55f, 0.53f), stoneTop = C(0.66f, 0.65f, 0.62f), dark = C(0.42f, 0.41f, 0.4f);
            switch (WorldGen.Biome(r.basePos.x, r.basePos.z))
            {
                case BiomeId.Badlands: stone = C(0.66f, 0.45f, 0.35f); stoneTop = C(0.77f, 0.57f, 0.44f); dark = C(0.5f, 0.33f, 0.26f); break;   // red sandstone
                case BiomeId.Peaks: stone = C(0.58f, 0.6f, 0.65f); stoneTop = C(0.82f, 0.85f, 0.9f); dark = C(0.42f, 0.44f, 0.5f); break;      // frosted granite
            }
            Color32 moss = C(0.36f, 0.55f, 0.27f), wood = C(0.42f, 0.29f, 0.18f), woodTop = C(0.62f, 0.48f, 0.3f);
            Vector3 b = r.basePos;
            float Foot(float dx, float dz) => WorldGen.Height(b.x + dx, b.z + dz) - b.y;
            uint Seed() => (uint)rng.Next(1, int.MaxValue);
            switch (r.setting)
            {
                case Setting.Summit:
                    // a cairn: flat stones stacked smaller and smaller, a few loose ones round its foot
                    mb.Rock(M(0, 0.15f, 0, 2.0f, 0.6f, 1.8f), dark, stone, Seed());
                    mb.Rock(M(0.05f, 0.55f, 0, 1.45f, 0.45f, 1.3f, 0, 30), stone, stoneTop, Seed());
                    mb.Rock(M(-0.05f, 0.88f, 0.03f, 1.05f, 0.38f, 0.95f, 0, 70), stone, stoneTop, Seed());
                    mb.Rock(M(0, 1.15f, 0, 0.65f, 0.32f, 0.6f, 0, 20), stone, stoneTop, Seed());
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 1.26f + 0.4f, d = 1.6f + (float)rng.NextDouble() * 0.6f, dx = Mathf.Cos(a) * d, dz = Mathf.Sin(a) * d;
                        mb.Rock(M(dx, Foot(dx, dz) + 0.08f, dz, 0.5f, 0.3f, 0.45f, 0, a * 57f), dark, stone, Seed());
                    }
                    break;
                case Setting.Mesa:
                    // a weathered altar with four short posts
                    Color32 sand = C(0.72f, 0.52f, 0.38f), sandTop = C(0.82f, 0.64f, 0.48f);
                    mb.Box(M(0, 0.25f, 0, 1.9f, 0.5f, 1.9f), sand, sandTop);
                    mb.Box(M(0, 0.62f, 0, 1.3f, 0.26f, 1.3f), sand, sandTop);
                    for (int i = 0; i < 4; i++)
                    {
                        float dx = (i & 1) == 0 ? -1.55f : 1.55f, dz = (i & 2) == 0 ? -1.55f : 1.55f;
                        mb.Cylinder(M(dx, Foot(dx, dz) + 0.5f, dz, 0.32f, 1.1f, 0.32f), sand, sandTop, 6, 0.7f, true);
                    }
                    break;
                case Setting.Crown:
                    // a ring of standing stones round a flat stone
                    mb.Cylinder(M(0, 0.12f, 0, 1.7f, 0.28f, 1.7f), stone, stoneTop, 9, 0.95f, true);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f + 0.3f, dx = Mathf.Cos(a) * 3.3f, dz = Mathf.Sin(a) * 3.3f;
                        float tall = 1.3f + (float)rng.NextDouble() * 0.7f;
                        mb.Box(M(dx, Foot(dx, dz) + tall * 0.5f - 0.15f, dz, 0.55f, tall, 0.38f, (float)rng.NextDouble() * 8f - 4f, -a * Mathf.Rad2Deg + 90f, (float)rng.NextDouble() * 8f - 4f), stone, stoneTop);
                    }
                    break;
                case Setting.Grove:
                    // a mossy stump in a clearing, ringed by mushrooms
                    mb.Cylinder(M(0, 0.32f, 0, 1.25f, 0.8f, 1.25f), wood, woodTop, 10, 0.88f, true);
                    mb.Dome(M(0.1f, 0.7f, 0, 0.9f, 0.16f, 0.8f), moss, 8, 3);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 2.1f + 0.5f;
                        mb.Cylinder(M(Mathf.Cos(a) * 0.75f, 0.12f, Mathf.Sin(a) * 0.75f, 0.32f, 0.22f, 0.9f, 0, -a * 57f, 80f), wood, wood, 6, 0.6f, true);   // roots
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f + (float)rng.NextDouble() * 0.4f, d = 2.2f + (float)rng.NextDouble() * 0.5f;
                        float dx = Mathf.Cos(a) * d, dz = Mathf.Sin(a) * d, y = Foot(dx, dz);
                        float s = 0.7f + (float)rng.NextDouble() * 0.5f;
                        mb.Cylinder(M(dx, y + 0.12f * s, dz, 0.1f * s, 0.26f * s, 0.1f * s), C(0.9f, 0.86f, 0.76f), 6);
                        mb.Dome(M(dx, y + 0.24f * s, dz, 0.36f * s, 0.2f * s, 0.36f * s), (i & 1) == 0 ? C(0.8f, 0.3f, 0.25f) : C(0.85f, 0.62f, 0.32f), 7, 3);
                    }
                    break;
                case Setting.Islet:
                {
                    // a rock rising out of the water from the bottom, moss on its back, a smaller one beside it
                    float depth = b.y - r.seabed;
                    mb.Rock(M(0, -depth * 0.5f + 0.05f, 0, 2.7f, depth + 0.6f, 2.4f), dark, stone, Seed(), 8, 6, 0.12f);
                    mb.Dome(M(-0.2f, 0.2f, 0.1f, 1.5f, 0.22f, 1.3f), moss, 8, 3);
                    float sx = 1.9f, sz = -0.9f;
                    mb.Rock(M(sx, -depth * 0.5f - 0.2f, sz, 1.2f, depth + 0.3f, 1.1f), dark, stone, Seed());
                    break;
                }
                default:
                {
                    // a stone gate across the canyon (the canyon runs the lowest way out), each pillar down to its own
                    // ground, the lintel level over the higher one; a low plinth under it
                    Vector2 along = Dirs16[0]; float low = float.MaxValue;
                    foreach (var d in Dirs16) { float hh = WorldGen.Height(b.x + d.x * 12f, b.z + d.y * 12f); if (hh < low) { low = hh; along = d; } }
                    var across = new Vector2(-along.y, along.x);
                    float yaw = Mathf.Atan2(-across.y, across.x) * Mathf.Rad2Deg;
                    float ax = across.x * 1.6f, az = across.y * 1.6f;
                    float fa = Foot(ax, az), fb = Foot(-ax, -az), top = Mathf.Max(fa, fb) + 2.7f;
                    foreach (var (px, pz, f) in new[] { (ax, az, fa), (-ax, -az, fb) })
                    {
                        float bottom = f - 0.4f, tall = top - bottom;
                        mb.Box(M(px, bottom + tall * 0.5f, pz, 0.65f, tall, 0.65f, 0, yaw, 0), stone, stoneTop);
                    }
                    mb.Box(M(0, top + 0.25f, 0, 4.1f, 0.5f, 0.9f, 0, yaw, 0), stone, stoneTop);
                    mb.Box(M(0, top + 0.62f, 0, 1.2f, 0.3f, 0.7f, 0, yaw, 0), dark, stone);   // keystone
                    mb.Cylinder(M(0, 0.2f, 0, 0.95f, 0.4f, 0.95f), stone, stoneTop, 8, 0.9f, true);
                    break;
                }
            }
            var go = new GameObject("RelicSetting_" + r.id);
            go.transform.SetParent(transform, false);
            go.transform.position = b;
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("relic_setting_" + r.id);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            return go;
        }
    }
}
