using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Collectible relic kinds. Each grants a small permanent bonus per piece found (7 of each).</summary>
    public enum RelicType { Tetherbloom, Windfeather, Kinspring, Hearthmoss, Tidepearl, CragClaw, Wardstone, Kinheart, Glintstone, Count }

    /// <summary>
    /// Hidden relics placed deterministically from the world seed in fun, hard-to-reach spots:
    /// mountain ledges most of the way up, summits, mesa tops, canyon floors, and out on lakes.
    /// Placement runs on a worker thread (WorldGen is pure); objects exist only near the player.
    /// </summary>
    public class Relics : MonoBehaviour
    {
        public static Relics I { get; private set; }
        public const int PerType = 7;
        /// <summary>The first six kinds hide on the old island (7 each); the last three are one per outer land (5 each).</summary>
        public const int IslandTypes = 6, OuterPerType = 5;
        public static int CountOf(RelicType t) => (int)t < IslandTypes ? PerType : OuterPerType;

        public struct Relic
        {
            public string id;
            public RelicType type;
            public Vector3 pos;
            public string spot;
        }

        public readonly List<Relic> All = new List<Relic>();
        public bool Ready { get; private set; }
        public int Total => All.Count;

        Task<List<Relic>> job;
        readonly Dictionary<string, GameObject> live = new Dictionary<string, GameObject>();
        readonly HashSet<string> sensed = new HashSet<string>();
        float sparkleTimer, checkTimer;
        static Mesh[] meshes;

        public static string TypeName(RelicType t)
        {
            switch (t)
            {
                case RelicType.Tetherbloom: return "Tetherbloom";
                case RelicType.Windfeather: return "Windfeather";
                case RelicType.Kinspring: return "Kinspring";
                case RelicType.Hearthmoss: return "Hearthmoss";
                case RelicType.Tidepearl: return "Tidepearl";
                case RelicType.Wardstone: return "Wardstone";
                case RelicType.Kinheart: return "Kinheart";
                case RelicType.Glintstone: return "Glintstone";
                default: return "Crag Claw";
            }
        }

        public static string TypeBonus(RelicType t)
        {
            switch (t)
            {
                case RelicType.Tetherbloom: return "+3% catch rate";
                case RelicType.Windfeather: return "+2% move speed";
                case RelicType.Kinspring: return "-6% stamina used while riding kin";
                case RelicType.Hearthmoss: return "-4% camp kin hunger";
                case RelicType.Tidepearl: return "+6% swim speed, -8% swim stamina";
                case RelicType.Wardstone: return "-3% damage taken";
                case RelicType.Kinheart: return "+4% XP for your kin";
                case RelicType.Glintstone: return "+8% shimmer odds";
                default: return "climb steeper slopes";
            }
        }

        public static Color TypeColor(RelicType t)
        {
            switch (t)
            {
                case RelicType.Tetherbloom: return new Color(0.5f, 1f, 0.45f);
                case RelicType.Windfeather: return new Color(0.75f, 0.9f, 1f);
                case RelicType.Kinspring: return new Color(1f, 0.65f, 0.25f);
                case RelicType.Hearthmoss: return new Color(1f, 0.55f, 0.75f);
                case RelicType.Tidepearl: return new Color(0.4f, 0.8f, 1f);
                case RelicType.Wardstone: return new Color(0.75f, 0.8f, 0.9f);
                case RelicType.Kinheart: return new Color(1f, 0.4f, 0.45f);
                case RelicType.Glintstone: return new Color(1f, 0.9f, 0.45f);
                default: return new Color(0.72f, 0.45f, 1f);
            }
        }

        public void Init()
        {
            I = this;
            int seed = WorldGen.Seed;
            job = Task.Run(() => Generate(seed));
        }

        // ================================================================ PLACEMENT (thread)
        enum Spot { Ledge, Summit, Mesa, Canyon, Lake, Hilltop }

        static List<Relic> Generate(int seed)
        {
            var rng = new System.Random(seed * 7919 + 17);
            var cands = new List<(Spot spot, Vector3 p)>[6];
            for (int i = 0; i < cands.Length; i++) cands[i] = new List<(Spot, Vector3)>();
            var peaks = new HashSet<Vector2Int>();
            var dirs = new Vector2[8];
            for (int i = 0; i < 8; i++) dirs[i] = new Vector2(Mathf.Cos(i * Mathf.PI / 4f), Mathf.Sin(i * Mathf.PI / 4f));

            for (int n = 0; n < 5000; n++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * 1580f;
                float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
                float h = WorldGen.Height(x, z);

                if (h < -2.5f)
                {
                    // out on a lake (not the open ocean): land must be close in some direction
                    int land = 0;
                    for (int d = 0; d < 8; d++) if (WorldGen.Height(x + dirs[d].x * 45f, z + dirs[d].y * 45f) > 0.5f) land++;
                    if (land >= 2 && rad < 1500f) cands[(int)Spot.Lake].Add((Spot.Lake, new Vector3(x, WorldGen.WaterLevel - 0.35f, z)));
                    continue;
                }
                if (h < 2f) continue;
                var biome = WorldGen.Biome(x, z);

                // climb to the local peak (tells us how far up the mountain this spot is)
                float px = x, pz = z, ph = h;
                if (h > 14f || (n & 3) == 0)
                {
                    for (int step = 0; step < 45; step++)
                    {
                        float bx = px, bz = pz, bh = ph;
                        for (int d = 0; d < 8; d++)
                        {
                            float qx = px + dirs[d].x * 7f, qz = pz + dirs[d].y * 7f, qh = WorldGen.Height(qx, qz);
                            if (qh > bh) { bx = qx; bz = qz; bh = qh; }
                        }
                        if (bh <= ph + 0.05f) break;
                        px = bx; pz = bz; ph = bh;
                    }
                    // the peak itself is a candidate (deduped on a coarse grid)
                    var key = new Vector2Int(Mathf.RoundToInt(px / 25f), Mathf.RoundToInt(pz / 25f));
                    if (peaks.Add(key) && Flat(px, pz, ph, 1.2f))
                    {
                        int drop = 0;
                        for (int d = 0; d < 8; d++) if (WorldGen.Height(px + dirs[d].x * 12f, pz + dirs[d].y * 12f) < ph - 3f) drop++;
                        var pp = new Vector3(px, ph + 1.1f, pz);
                        var pb = WorldGen.Biome(px, pz);
                        if (ph > 38f && pb != BiomeId.Badlands) cands[(int)Spot.Summit].Add((Spot.Summit, pp));
                        else if (pb == BiomeId.Badlands && ph > 26f && drop >= 4) cands[(int)Spot.Mesa].Add((Spot.Mesa, pp));
                        else if (drop >= 6 && ph > 8f) cands[(int)Spot.Hilltop].Add((Spot.Hilltop, pp));
                    }
                }

                if (!Flat(x, z, h, 1.1f)) continue;
                var p = new Vector3(x, h + 1.1f, z);
                // ledge: a standable spot well up a real mountain, with plenty of mountain still above it
                if (h > 40f && ph - h > 12f && h >= ph * 0.55f)
                {
                    int lower = 0;
                    for (int d = 0; d < 8; d++) if (WorldGen.Height(x + dirs[d].x * 9f, z + dirs[d].y * 9f) < h - 4f) lower++;
                    if (lower >= 2) { cands[(int)Spot.Ledge].Add((Spot.Ledge, p)); continue; }
                }
                // canyon floor: walled in on most sides
                if (biome == BiomeId.Badlands)
                {
                    int higher = 0;
                    for (int d = 0; d < 8; d++) if (WorldGen.Height(x + dirs[d].x * 16f, z + dirs[d].y * 16f) > h + 6f) higher++;
                    if (higher >= 5) cands[(int)Spot.Canyon].Add((Spot.Canyon, p));
                }
            }

            // preferred spots per relic type (fallbacks follow)
            var prefs = new Dictionary<RelicType, Spot[]>
            {
                { RelicType.CragClaw, new[] { Spot.Ledge, Spot.Summit, Spot.Mesa, Spot.Hilltop, Spot.Canyon } },
                { RelicType.Windfeather, new[] { Spot.Summit, Spot.Mesa, Spot.Ledge, Spot.Hilltop, Spot.Canyon } },
                { RelicType.Tidepearl, new[] { Spot.Lake, Spot.Hilltop, Spot.Canyon, Spot.Mesa } },
                { RelicType.Hearthmoss, new[] { Spot.Canyon, Spot.Hilltop, Spot.Mesa, Spot.Ledge, Spot.Lake } },
                { RelicType.Kinspring, new[] { Spot.Mesa, Spot.Ledge, Spot.Hilltop, Spot.Summit, Spot.Lake } },
                { RelicType.Tetherbloom, new[] { Spot.Hilltop, Spot.Canyon, Spot.Summit, Spot.Ledge, Spot.Mesa, Spot.Lake } },
            };
            foreach (var l in cands) Shuffle(l, rng);
            var result = new List<Relic>();
            var used = new HashSet<Vector3>();
            for (int k = 0; k < PerType; k++)
            {
                for (int t = 0; t < IslandTypes; t++)
                {
                    var type = (RelicType)t;
                    bool placed = false;
                    foreach (var s in prefs[type])
                    {
                        foreach (var c in cands[(int)s])
                        {
                            if (used.Contains(c.p) || TooClose(result, c.p, 150f)) continue;
                            used.Add(c.p);
                            result.Add(new Relic { id = t + "_" + k, type = type, pos = c.p, spot = SpotName(c.spot) });
                            placed = true;
                            break;
                        }
                        if (placed) break;
                    }
                }
            }
            PlaceOuter(seed, result, dirs);
            return result;
        }

        /// <summary>
        /// One Wardstone, Kinheart and Glintstone on each outer land, on its most prominent heights, well apart and away
        /// from where you land. Uses its own random stream, so the old island's 42 never move.
        /// </summary>
        static void PlaceOuter(int seed, List<Relic> result, Vector2[] dirs)
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
                        for (int d = 0; d < 8; d++)
                        {
                            float qx = px + dirs[d].x * 7f, qz = pz + dirs[d].y * 7f, qh = WorldGen.Height(qx, qz);
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
                    for (int d = 0; d < 8; d++) if (WorldGen.Height(px + dirs[d].x * 14f, pz + dirs[d].y * 14f) < ph - 2.5f) drop++;
                    cands.Add((new Vector3(px, ph + 1.1f, pz), drop * 10 + Mathf.Min(9, (int)(ph / 4f))));
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
                        mine.Add(new Relic { id = (int)type + "_" + li, type = type, pos = c.p, spot = (c.drop >= 60 ? "a lone height in " : "high ground in ") + where });
                    }
                result.AddRange(mine);
            }
        }

        static bool Flat(float x, float z, float h, float tol)
        {
            return Mathf.Abs(WorldGen.Height(x + 1.5f, z) - h) < tol && Mathf.Abs(WorldGen.Height(x - 1.5f, z) - h) < tol &&
                   Mathf.Abs(WorldGen.Height(x, z + 1.5f) - h) < tol && Mathf.Abs(WorldGen.Height(x, z - 1.5f) - h) < tol;
        }

        static string SpotName(Spot s)
        {
            switch (s)
            {
                case Spot.Ledge: return "a high mountain ledge";
                case Spot.Summit: return "a summit";
                case Spot.Mesa: return "a mesa top";
                case Spot.Canyon: return "a canyon floor";
                case Spot.Lake: return "the middle of a lake";
                default: return "a lonely hilltop";
            }
        }

        static bool TooClose(List<Relic> list, Vector3 p, float min)
        {
            float m2 = min * min;
            foreach (var r in list) { float dx = r.pos.x - p.x, dz = r.pos.z - p.z; if (dx * dx + dz * dz < m2) return true; }
            return false;
        }

        static void Shuffle<T>(List<T> l, System.Random rng)
        {
            for (int i = l.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = l[i]; l[i] = l[j]; l[j] = t; }
        }

        // ================================================================ RUNTIME
        public void Tick(float dt, PlayerController pl)
        {
            if (!Ready)
            {
                if (job == null || !job.IsCompleted) return;
                if (job.Status == TaskStatus.RanToCompletion) All.AddRange(job.Result);
                else Debug.LogWarning("Relic placement failed: " + job.Exception);
                job = null;
                Ready = true;
            }
            var stats = pl.Stats;
            Vector3 pp = pl.Position;
            checkTimer -= dt;
            bool check = checkTimer <= 0f;
            if (check) checkTimer = 0.5f;
            sparkleTimer -= dt;
            bool sparkle = sparkleTimer <= 0f;
            if (sparkle) sparkleTimer = 0.9f;

            foreach (var r in All)
            {
                float dx = r.pos.x - pp.x, dz = r.pos.z - pp.z;
                float d2 = dx * dx + dz * dz;
                live.TryGetValue(r.id, out var go);
                if (check)
                {
                    bool found = stats.relicsFound.Contains(r.id);
                    bool want = !found && d2 < 130f * 130f;
                    if (want && go == null) live[r.id] = go = MakeObject(r);
                    else if (!want && go != null) { Destroy(go); live.Remove(r.id); go = null; }
                    if (want && d2 < 55f * 55f && sensed.Add(r.id))
                        GameEvents.Say($"You sense a {TypeName(r.type)} relic nearby...", TypeColor(r.type));
                }
                if (go == null) continue;
                float bob = Mathf.Sin(Time.time * 2f + r.pos.x) * 0.18f;
                go.transform.SetPositionAndRotation(r.pos + Vector3.up * bob, Quaternion.Euler(0f, Time.time * 70f, 0f));
                if (sparkle && d2 < 60f * 60f) Fx.Sparkle(r.pos + Vector3.up * 0.4f, TypeColor(r.type), 5, 0.6f);
                if (d2 < 2.2f * 2.2f && Mathf.Abs(pp.y - r.pos.y) < 3.2f) Collect(r, stats);
            }
        }

        void Collect(Relic r, PlayerStats stats)
        {
            stats.AddRelic(r.id);
            if (live.TryGetValue(r.id, out var go)) { Destroy(go); live.Remove(r.id); }
            int n = stats.RelicCount(r.type);
            GameEvents.Good($"{TypeName(r.type)} relic ({n}/{CountOf(r.type)})! Now {TypeBonusTotal(r.type, n)}");
            Fx.Burst(r.pos, TypeColor(r.type), 24, 6f, 0.35f, 0.8f, true);
            SynthAudio.Play(Sfx.LevelUp, 1.35f);
            GameManager.I.AddPlayerXp(40);
            GameEvents.RaiseInventoryChanged();
        }

        public static string TypeBonusTotal(RelicType t, int n)
        {
            switch (t)
            {
                case RelicType.Tetherbloom: return $"+{3 * n}% catch rate";
                case RelicType.Windfeather: return $"+{2 * n}% move speed";
                case RelicType.Kinspring: return $"-{6 * n}% riding stamina";
                case RelicType.Hearthmoss: return $"-{4 * n}% camp kin hunger";
                case RelicType.Tidepearl: return $"+{6 * n}% swim speed, -{Mathf.Min(70, 8 * n)}% swim stamina";
                case RelicType.Wardstone: return $"-{3 * n}% damage taken";
                case RelicType.Kinheart: return $"+{4 * n}% XP for your kin";
                case RelicType.Glintstone: return $"+{8 * n}% shimmer odds";
                default: return $"climb {n * 12}% steeper slopes";
            }
        }

        GameObject MakeObject(Relic r)
        {
            if (meshes == null) BuildMeshes();
            var go = new GameObject("Relic_" + r.id);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = meshes[(int)r.type];
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.position = r.pos;
            return go;
        }

        static Matrix4x4 M(float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(px, py, pz), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        /// <summary>Glowing crystal casing around a small emblem, tinted per relic type.</summary>
        static void BuildMeshes()
        {
            meshes = new Mesh[(int)RelicType.Count];
            for (int t = 0; t < meshes.Length; t++)
            {
                var type = (RelicType)t;
                Color c = TypeColor(type);
                Color32 bright = Color.Lerp(c, Color.white, 0.45f), deep = c, dark = Color.Lerp(c, Color.black, 0.35f);
                var mb = new MeshBuilder();
                mb.Crystal(M(0, -0.35f, 0, 0.32f, 0.9f, 0.32f), deep, bright);
                mb.Crystal(M(0.22f, -0.35f, 0.05f, 0.18f, 0.5f, 0.18f, 0, 0, -25), deep, bright);
                mb.Crystal(M(-0.2f, -0.35f, -0.06f, 0.16f, 0.45f, 0.16f, 0, 0, 25), deep, bright);
                switch (type)
                {
                    case RelicType.Tetherbloom:
                        for (int i = 0; i < 5; i++) mb.Sphere(M(Mathf.Cos(i * 1.256f) * 0.22f, 0.7f, Mathf.Sin(i * 1.256f) * 0.22f, 0.2f, 0.08f, 0.2f), bright, 6, 4);
                        mb.Sphere(M(0, 0.72f, 0, 0.16f, 0.16f, 0.16f), new Color(1f, 0.9f, 0.4f), 6, 4);
                        break;
                    case RelicType.Windfeather:
                        mb.Sphere(M(0, 0.75f, 0, 0.14f, 0.6f, 0.05f, 0, 0, 20), Color.white, bright, 8, 6);
                        break;
                    case RelicType.Kinspring:
                        mb.Sphere(M(0, 0.68f, 0, 0.28f, 0.2f, 0.2f), bright, 8, 5);
                        for (int i = 0; i < 3; i++) mb.Sphere(M(-0.14f + i * 0.14f, 0.86f, 0, 0.1f, 0.1f, 0.1f), bright, 6, 4);
                        break;
                    case RelicType.Hearthmoss:
                        mb.Rock(M(0, 0.62f, 0, 0.36f, 0.22f, 0.3f), new Color(0.5f, 0.5f, 0.55f), new Color(0.6f, 0.6f, 0.65f), 3u, 6, 4);
                        mb.Dome(M(0, 0.7f, 0, 0.3f, 0.18f, 0.26f), bright, 8, 3);
                        break;
                    case RelicType.Tidepearl:
                        mb.Dome(M(0, 0.55f, 0, 0.5f, 0.12f, 0.4f), dark, 8, 3);
                        mb.Sphere(M(0, 0.72f, 0, 0.24f, 0.24f, 0.24f), new Color(0.95f, 0.97f, 1f), bright, 8, 6);
                        break;
                    case RelicType.Wardstone:
                        // a little shield
                        mb.Sphere(M(0, 0.78f, 0, 0.44f, 0.52f, 0.1f), bright, dark, 8, 6);
                        mb.Sphere(M(0, 0.8f, 0.06f, 0.16f, 0.16f, 0.08f), Color.white, 6, 4);
                        break;
                    case RelicType.Kinheart:
                        // a heart: two lobes over a point
                        mb.Sphere(M(0.11f, 0.84f, 0, 0.26f, 0.26f, 0.2f), bright, 7, 5);
                        mb.Sphere(M(-0.11f, 0.84f, 0, 0.26f, 0.26f, 0.2f), bright, 7, 5);
                        mb.Cone(M(0, 0.66f, 0, 0.36f, 0.34f, 0.2f, 180f, 0, 0), bright, 7);
                        break;
                    case RelicType.Glintstone:
                        // a four-point star
                        for (int i = 0; i < 4; i++) mb.Cone(M(0, 0.8f, 0, 0.14f, 0.3f, 0.1f, 0, 0, i * 90f) * Matrix4x4.Translate(new Vector3(0, 0.5f, 0)), Color.white, 5);
                        mb.Sphere(M(0, 0.8f, 0, 0.2f, 0.2f, 0.14f), bright, 6, 4);
                        break;
                    default:
                        for (int i = 0; i < 3; i++) mb.Cone(M(-0.12f + i * 0.12f, 0.8f, 0, 0.08f, 0.34f, 0.08f, 0, 0, -12 + i * 12), new Color(0.92f, 0.88f, 0.8f), 6);
                        break;
                }
                meshes[t] = mb.ToMesh("relic_" + type);
            }
        }
    }
}
