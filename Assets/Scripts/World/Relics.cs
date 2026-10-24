using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Collectible relic kinds. Each grants a small permanent bonus per piece found (7 of each).</summary>
    public enum RelicType { Tetherbloom, Windfeather, Kinspring, Hearthmoss, Tidepearl, CragClaw, Wardstone, Kinheart, Glintstone, Count }

    /// <summary>
    /// Hidden relics placed deterministically from the world seed on places chosen to look designed: mountain summits,
    /// mesa tops, hill crowns, forest groves, rocks in lakes and canyon arches, each with a small built setting
    /// (Relics.Places). Placement runs on a worker thread (WorldGen is pure); objects exist only near the player.
    /// </summary>
    public partial class Relics : MonoBehaviour
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
            /// <summary>The kind of place and the ground point its setting stands on (an islet: the top of its rock).</summary>
            public Setting setting;
            public Vector3 basePos;
            public float seabed, clear;
        }

        public readonly List<Relic> All = new List<Relic>();
        public bool Ready { get; private set; }
        public int Total => All.Count;

        Task<List<Relic>> job;
        readonly Dictionary<string, GameObject> live = new Dictionary<string, GameObject>();
        readonly HashSet<string> sensed = new HashSet<string>();
        readonly Dictionary<string, GameObject> settings = new Dictionary<string, GameObject>();
        const float SettingRange = 230f;
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
            job = pending ?? StartJob();   // normally already running since Prepare
            pending = null;
        }

        static Task<List<Relic>> pending;
        static Task<List<Relic>> StartJob()
        {
            int seed = WorldGen.Seed;
            var avoid = AvoidSnapshot();
            return Task.Run(() => Generate(seed, avoid));
        }

        /// <summary>Starts placement as early as the sites it keeps clear of exist (called after Trails.Prepare).</summary>
        public static void Prepare() { pending = StartJob(); }

        /// <summary>Finishes placement now if it is still running (the first chunk that asks BlocksNode waits for it).</summary>
        void EnsureReady()
        {
            if (Ready) return;
            if (job == null) return;
            var waited = System.Diagnostics.Stopwatch.StartNew();
            try { job.Wait(); } catch (System.Exception) { }
            if (waited.ElapsedMilliseconds > 5) PlacementReport += $" | main thread waited {waited.ElapsedMilliseconds} ms";
            if (job.Status == TaskStatus.RanToCompletion) All.AddRange(job.Result);
            else Debug.LogWarning("Relic placement failed: " + job.Exception);
            job = null;
            Ready = true;
        }

        // ================================================================ RUNTIME
        public void Tick(float dt, PlayerController pl)
        {
            if (!Ready)
            {
                if (job == null || !job.IsCompleted) return;
                EnsureReady();
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
                    // the setting (cairn, stone ring, stump, rock...) stands whether or not the relic is still there
                    settings.TryGetValue(r.id, out var sg);
                    bool show = d2 < SettingRange * SettingRange;
                    if (show && sg == null) settings[r.id] = MakeSetting(r);
                    else if (!show && sg != null) { Destroy(sg.GetComponent<MeshFilter>().sharedMesh); Destroy(sg); settings.Remove(r.id); }
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
