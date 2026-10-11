using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Life that is only there to be seen: birds wheeling overhead by day (gulls over the Isles, dark birds over the
    /// Badlands and Blightmoor, glow-wings in Glowdeep at any hour), butterflies over the green lands, and fish that
    /// leap from the water, mostly at fishing spots. None of it can be fought or caught. A fixed pool of small
    /// objects that follow the player; nothing here touches gameplay or saves.
    /// </summary>
    public class AmbientLife : MonoBehaviour
    {
        public static AmbientLife I { get; private set; }

        class Bird { public Transform root, wingL, wingR; public float phase, radius, height, speed; }
        class Flutter { public Transform root, wingL, wingR; public Vector3 pos, target; public float timer, phase; public bool on; }
        class Leaper { public Transform root; public Vector3 from, dir; public float t; public bool on; }

        const int BirdCount = 10, FlutterCount = 8, LeaperCount = 3;
        readonly List<Bird> birds = new List<Bird>();
        readonly List<Flutter> flutters = new List<Flutter>();
        readonly List<Leaper> leapers = new List<Leaper>();
        Vector3 flockCentre;
        float flockAngle, leapTimer = 3f, biomeTimer;
        BiomeId biome = BiomeId.Meadow;
        int birdStyle = -1;
        bool birdsOn, fluttersOn;
        static Mesh fishMesh;
        static Mesh[] bodyMesh, wingMesh, flutterWing;
        static readonly Color[] BirdCols = { new Color(0.45f, 0.36f, 0.3f), new Color(0.95f, 0.95f, 0.97f), new Color(0.14f, 0.13f, 0.16f), new Color(0.45f, 1f, 0.9f) };
        static readonly Color[] FlutterCols = { new Color(1f, 0.75f, 0.2f), new Color(0.5f, 0.75f, 1f), new Color(1f, 0.55f, 0.8f), new Color(0.95f, 0.95f, 0.9f), new Color(0.5f, 1f, 0.9f) };

        public void Init()
        {
            I = this;
            BuildMeshes();
            for (int i = 0; i < BirdCount; i++)
            {
                var b = new Bird { phase = i * 0.63f, radius = 7f + (i % 5) * 3.2f, height = (i % 3) * 2.4f, speed = 0.55f + (i % 4) * 0.07f };
                b.root = Part("Bird", bodyMesh[0], transform);
                b.wingL = Part("WL", wingMesh[0], b.root);
                b.wingR = Part("WR", wingMesh[0], b.root);
                b.wingR.localScale = new Vector3(-1f, 1f, 1f);
                b.root.gameObject.SetActive(false);
                birds.Add(b);
            }
            for (int i = 0; i < FlutterCount; i++)
            {
                var f = new Flutter { phase = i * 1.7f };
                f.root = new GameObject("Butterfly").transform;
                f.root.SetParent(transform, false);
                f.wingL = Part("WL", flutterWing[i % FlutterCols.Length], f.root);
                f.wingR = Part("WR", flutterWing[i % FlutterCols.Length], f.root);
                f.wingR.localScale = new Vector3(-1f, 1f, 1f);
                f.root.gameObject.SetActive(false);
                flutters.Add(f);
            }
            for (int i = 0; i < LeaperCount; i++)
            {
                var l = new Leaper { root = Part("Fish", fishMesh, transform) };
                l.root.gameObject.SetActive(false);
                leapers.Add(l);
            }
        }

        static Transform Part(string name, Mesh m, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        static void BuildMeshes()
        {
            if (fishMesh != null) return;
            MeshBuilder mb;
            // one body and one wing per flock colour
            bodyMesh = new Mesh[BirdCols.Length];
            wingMesh = new Mesh[BirdCols.Length];
            for (int i = 0; i < BirdCols.Length; i++)
            {
                mb = new MeshBuilder();
                mb.Sphere(M(0, 0, 0, 0.3f, 0.26f, 0.9f), BirdCols[i], Color.Lerp(BirdCols[i], Color.white, 0.35f), 6, 4);
                mb.Cone(M(0, 0.02f, 0.5f, 0.12f, 0.24f, 0.12f, 90f, 0, 0), new Color(1f, 0.75f, 0.3f), 4);
                mb.Box(M(0, 0, -0.55f, 0.34f, 0.03f, 0.4f), Color.Lerp(BirdCols[i], Color.black, 0.2f));
                bodyMesh[i] = mb.ToMesh("amb_bird" + i);
                mb = new MeshBuilder();
                Color32 c = BirdCols[i], tip = Color.Lerp(BirdCols[i], i == 1 ? new Color(0.3f, 0.3f, 0.35f) : Color.white, 0.3f);
                mb.Box(M(0.55f, 0f, 0.05f, 1.1f, 0.03f, 0.5f), c);
                mb.Box(M(1.3f, 0f, -0.05f, 0.6f, 0.03f, 0.32f, 0, -14f, 0), tip);
                wingMesh[i] = mb.ToMesh("amb_wing" + i);
            }
            flutterWing = new Mesh[FlutterCols.Length];
            for (int i = 0; i < FlutterCols.Length; i++)
            {
                mb = new MeshBuilder();
                Color32 c = FlutterCols[i], edge = Color.Lerp(FlutterCols[i], Color.black, 0.55f);
                mb.Box(M(0.11f, 0f, 0.05f, 0.22f, 0.008f, 0.2f), c);
                mb.Box(M(0.09f, 0f, -0.1f, 0.16f, 0.008f, 0.14f), edge);
                flutterWing[i] = mb.ToMesh("amb_flutter" + i);
            }
            mb = new MeshBuilder();
            mb.Sphere(M(0, 0, 0, 0.2f, 0.26f, 0.7f), new Color(0.75f, 0.85f, 0.95f), new Color(0.95f, 0.97f, 1f), 7, 5);
            mb.Box(M(0, 0, -0.45f, 0.03f, 0.34f, 0.26f), new Color(0.55f, 0.7f, 0.85f));
            mb.Box(M(0, 0.16f, 0f, 0.03f, 0.16f, 0.26f), new Color(0.55f, 0.7f, 0.85f));
            fishMesh = mb.ToMesh("amb_fish");
        }

        static int StyleFor(BiomeId b, bool night)
        {
            switch (b)
            {
                case BiomeId.Glow: return 3;
                case BiomeId.Isles: case BiomeId.Peaks: return night ? -1 : 1;
                case BiomeId.Badlands: case BiomeId.Blight: return 2;   // dusk-coloured, out at any hour
                default: return night ? -1 : 0;
            }
        }

        public void Tick(float dt, PlayerController pl)
        {
            var env = EnvironmentSystem.I;
            bool indoors = Caves.InCave;
            bool night = env != null && env.IsNight;
            Vector3 pp = pl.Position;
            biomeTimer -= dt;
            if (biomeTimer <= 0f)
            {
                biomeTimer = 2f;
                biome = env != null ? env.CurrentBiome : BiomeId.Meadow;
                int style = indoors ? -1 : StyleFor(biome, night);
                if (style != birdStyle)
                {
                    birdStyle = style;
                    birdsOn = style >= 0;
                    if (birdsOn)
                    {
                        foreach (var b in birds)
                        {
                            b.root.GetComponent<MeshFilter>().sharedMesh = bodyMesh[style];
                            b.wingL.GetComponent<MeshFilter>().sharedMesh = wingMesh[style];
                            b.wingR.GetComponent<MeshFilter>().sharedMesh = wingMesh[style];
                        }
                        flockCentre = pp + new Vector3(40f, 0f, 0f);
                    }
                }
                bool lush = biome == BiomeId.Meadow || biome == BiomeId.Isles || biome == BiomeId.Swamp || biome == BiomeId.Glow;
                fluttersOn = !indoors && lush && (!night || biome == BiomeId.Glow);
            }
            bool lean = QualityProfile.Current.particleBudget < 200;

            // ---------------- birds: one loose flock circling a point that drifts around the player ----------------
            int wantBirds = birdsOn ? (lean ? 5 : BirdCount) : 0;
            if (wantBirds > 0)
            {
                flockAngle += dt * 0.05f;
                Vector3 want = pp + new Vector3(Mathf.Cos(flockAngle) * 38f, 0f, Mathf.Sin(flockAngle) * 38f);
                flockCentre = Vector3.Lerp(flockCentre, want, dt * 0.4f);
                if ((flockCentre - pp).sqrMagnitude > 200f * 200f) flockCentre = want;   // you teleported
            }
            for (int i = 0; i < birds.Count; i++)
            {
                var b = birds[i];
                bool on = i < wantBirds;
                if (b.root.gameObject.activeSelf != on) b.root.gameObject.SetActive(on);
                if (!on) continue;
                float a = Time.time * b.speed * 0.5f + b.phase;
                float y = Mathf.Max(Ground.Height(flockCentre.x, flockCentre.z), 0f) + 24f + b.height + Mathf.Sin(Time.time * 0.3f + b.phase) * 2f;
                var p = new Vector3(flockCentre.x + Mathf.Cos(a) * b.radius, y, flockCentre.z + Mathf.Sin(a) * b.radius);
                var fwd = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                b.root.SetPositionAndRotation(p, Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, -14f));
                // flap a while, glide a while
                float cyc = Mathf.Repeat(Time.time * 0.22f + b.phase, 1f);
                float flap = cyc < 0.6f ? Mathf.Sin(Time.time * 9f + b.phase * 5f) * 34f : 6f;
                b.wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                b.wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }

            // ---------------- butterflies ----------------
            int wantFl = fluttersOn ? (lean ? 4 : FlutterCount) : 0;
            for (int i = 0; i < flutters.Count; i++)
            {
                var f = flutters[i];
                bool on = i < wantFl;
                if (on != f.on)
                {
                    f.on = on;
                    f.root.gameObject.SetActive(on);
                    if (on) { f.pos = Around(pp, 4f, 14f); f.target = f.pos; f.timer = 0f; }
                }
                if (!on) continue;
                f.timer -= dt;
                Vector3 off = f.pos - pp; off.y = 0f;
                if (f.timer <= 0f || off.sqrMagnitude > 22f * 22f)
                {
                    f.timer = Random.Range(1.5f, 4f);
                    if (off.sqrMagnitude > 30f * 30f) f.pos = Around(pp, 6f, 14f);   // left behind: start again near you
                    f.target = Around(pp, 3f, 15f);
                }
                Vector3 to = f.target - f.pos;
                if (to.sqrMagnitude > 0.04f) f.pos += to.normalized * (1.6f * dt);
                float gy = Mathf.Max(Ground.Height(f.pos.x, f.pos.z), Tides.Surface(f.pos));
                float y = gy + 0.9f + Mathf.Sin(Time.time * 1.3f + f.phase) * 0.45f + Mathf.Sin(Time.time * 7f + f.phase) * 0.08f;
                f.pos.y = y;
                Vector3 look = to; look.y = 0f;
                f.root.SetPositionAndRotation(f.pos, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : f.root.rotation);
                float flap = Mathf.Abs(Mathf.Sin(Time.time * 16f + f.phase)) * 70f;
                f.wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                f.wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }

            // ---------------- leaping fish ----------------
            if (!indoors)
            {
                leapTimer -= dt;
                if (leapTimer <= 0f)
                {
                    leapTimer = Random.Range(2.5f, 6f) * (lean ? 1.6f : 1f);
                    StartLeap(pp);
                }
            }
            foreach (var l in leapers)
            {
                if (!l.on) continue;
                l.t += dt / 0.85f;
                if (l.t >= 1f)
                {
                    l.on = false;
                    l.root.gameObject.SetActive(false);
                    Vector3 end = l.from + l.dir * 1.7f;
                    Fx.Burst(end, new Color(0.85f, 0.95f, 1f), 8, 2.6f, 0.12f, 0.4f, false, 9f);
                    continue;
                }
                Vector3 p = l.from + l.dir * (1.7f * l.t) + Vector3.up * (Mathf.Sin(l.t * Mathf.PI) * 1.2f);
                l.root.SetPositionAndRotation(p, Quaternion.LookRotation(l.dir) * Quaternion.Euler(Mathf.Lerp(-65f, 65f, l.t), 0f, 0f));
            }
        }

        static Vector3 Around(Vector3 pp, float min, float max)
        {
            float a = Random.value * Mathf.PI * 2f, d = Random.Range(min, max);
            return new Vector3(pp.x + Mathf.Cos(a) * d, pp.y, pp.z + Mathf.Sin(a) * d);
        }

        void StartLeap(Vector3 pp)
        {
            Leaper free = null;
            foreach (var l in leapers) if (!l.on) { free = l; break; }
            if (free == null) return;
            Vector3? at = Fishing.I != null ? Fishing.I.SpotNear(pp, 45f) : null;
            if (!at.HasValue)
            {
                for (int i = 0; i < 6 && !at.HasValue; i++)
                {
                    var c = Around(pp, 8f, 38f);
                    if (WorldGen.Height(c.x, c.z) < -1f && !Caves.InZone(c.x, c.z)) at = c;
                }
            }
            if (!at.HasValue) return;
            Vector3 p = at.Value + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            if (WorldGen.Height(p.x, p.z) > -0.7f) p = at.Value;
            p.y = Tides.Surface(p);
            float a = Random.value * Mathf.PI * 2f;
            free.from = p;
            free.dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            free.t = 0f;
            free.on = true;
            free.root.gameObject.SetActive(true);
            Fx.Burst(p, new Color(0.85f, 0.95f, 1f), 8, 2.6f, 0.12f, 0.4f, false, 9f);
        }
    }
}
