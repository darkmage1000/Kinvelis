using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The sea around the Driftkey Isles rises and falls twice a day (about -0.75 m to +0.45 m). At low tide sandbars
    /// surface between the islets; at high tide the beaches shrink. Only the Isles feel it: the offset fades to nothing
    /// well before the old island's coast, so nothing there ever changes. Main thread only.
    /// </summary>
    public static class Tides
    {
        const float Mid = -0.15f, Amp = 0.6f;
        static float height, rate;

        public static void Update(EnvironmentSystem env)
        {
            float a = (env.timeOfDay - 0.08f) * Mathf.PI * 4f;   // two tides a day
            height = Mid + Amp * Mathf.Sin(a);
            rate = Mathf.Cos(a);
        }

        static float Weight(float x, float z)
        {
            if (!WorldGen.TryLand(BiomeId.Isles, out var land)) return 0f;
            float dx = x - land.center.x, dz = z - land.center.y;
            float d2 = dx * dx + dz * dz;
            if (d2 > 1400f * 1400f) return 0f;
            return WorldGen.Smooth(1400f, 1050f, Mathf.Sqrt(d2));
        }

        /// <summary>Height of the sea surface at a point.</summary>
        public static float Surface(float x, float z) => WorldGen.WaterLevel + height * Weight(x, z);
        public static float Surface(Vector3 p) => Surface(p.x, p.z);

        // the swell drawn by WB_Water's vertex shader (same numbers as WB_Water.mat), so a surfing kin can ride it
        const float WaveAmp = 0.18f, WaveFreq = 0.12f;

        /// <summary>Height of the drawn swell above the sea surface at a point (about -0.27 to +0.27 m).</summary>
        public static float Swell(float x, float z)
        {
            float t = Time.timeSinceLevelLoad;   // the shader's _Time.y
            float w1 = Mathf.Sin(x * WaveFreq + t * 1.3f) * Mathf.Cos(z * WaveFreq * 0.8f + t * 0.9f);
            float w2 = Mathf.Sin((x + z) * WaveFreq * 2.1f - t * 1.7f) * 0.5f;
            return (w1 + w2) * WaveAmp;
        }

        /// <summary>"Low tide" etc. while you are on the Isles, otherwise null.</summary>
        public static string Label(Vector3 p)
        {
            if (Weight(p.x, p.z) < 0.5f) return null;
            if (height < Mid - Amp * 0.7f) return "Low tide: sandbars are up";
            if (height > Mid + Amp * 0.7f) return "High tide";
            return rate > 0f ? "Tide coming in" : "Tide going out";
        }

        public static bool IsLow(Vector3 p) => Weight(p.x, p.z) >= 0.5f && height < Mid - Amp * 0.55f;
    }

    /// <summary>
    /// Things in the land itself that can hurt (or help) you, each tied to a region:
    /// geysers in the Badlands (thickest on Ashfall Flats) that scald and throw you skyward, quicksand in the Sunscour Dunes,
    /// thin ice on Shiverglass Field, spore pods in Mirefen and Blightmoor (thickest in Sporefall).
    /// Placement is hashed per 42 m cell from the world seed, so a hazard is always in the same place; objects exist only
    /// near the player. None on the meadow, in settlements, arenas, camps or by cave mouths.
    /// </summary>
    public class Hazards : MonoBehaviour
    {
        public static Hazards I { get; private set; }
        enum Kind { Geyser, Quicksand, ThinIce, SporePod }

        class Hazard
        {
            public Kind kind;
            public Vector3 pos;
            public GameObject go;
            public Transform a, b;       // kind-specific parts
            public float phase, t, cool, stood;
            public int state;
            public bool fired;
            public Color col;
        }

        const float CellSize = 42f;
        const int Reach = 3;
        readonly Dictionary<long, Hazard> live = new Dictionary<long, Hazard>();
        readonly HashSet<long> empty = new HashSet<long>();
        readonly List<long> drop = new List<long>();
        float scanTimer, fxTimer;
        bool fx;
        static Mesh geyserMesh, sandMesh, iceMesh, crackMesh, holeMesh, podStalk, podBulb;
        static bool toldGeyser, toldSand, toldIce, toldPod;

        public void Init()
        {
            I = this;
            toldGeyser = toldSand = toldIce = toldPod = false;
        }

        static long Key(int cx, int cz) => ((long)(cx + 4096) << 20) | (long)(cz + 4096);

        public void Tick(float dt, PlayerController pl)
        {
            var env = EnvironmentSystem.I;
            if (env != null) Tides.Update(env);
            pl.SlowMul = 1f;
            if (Caves.InCave) { if (live.Count > 0) Clear(); return; }
            Vector3 pp = pl.Position;
            scanTimer -= dt;
            if (scanTimer <= 0f) { scanTimer = 1f; Scan(pp); }
            fxTimer -= dt;
            fx = fxTimer <= 0f;
            if (fx) fxTimer = 0.05f;
            foreach (var kv in live)
            {
                var h = kv.Value;
                float dx = h.pos.x - pp.x, dz = h.pos.z - pp.z;
                float d2 = dx * dx + dz * dz;
                if (d2 > 70f * 70f) continue;
                switch (h.kind)
                {
                    case Kind.Geyser: TickGeyser(h, dt, pl, d2); break;
                    case Kind.Quicksand: TickSand(h, dt, pl, d2); break;
                    case Kind.ThinIce: TickIce(h, dt, pl, d2); break;
                    default: TickPod(h, dt, pl, d2); break;
                }
            }
        }

        void Clear()
        {
            foreach (var kv in live) if (kv.Value.go != null) Destroy(kv.Value.go);
            live.Clear();
        }

        // ================================================================ PLACEMENT
        void Scan(Vector3 pp)
        {
            int pcx = Mathf.FloorToInt(pp.x / CellSize), pcz = Mathf.FloorToInt(pp.z / CellSize);
            for (int dz = -Reach; dz <= Reach; dz++)
            for (int dx = -Reach; dx <= Reach; dx++)
            {
                int cx = pcx + dx, cz = pcz + dz;
                long k = Key(cx, cz);
                if (live.ContainsKey(k) || empty.Contains(k)) continue;
                var h = TryMake(cx, cz);
                if (h == null) empty.Add(k); else live[k] = h;
            }
            if (empty.Count > 4000) empty.Clear();
            drop.Clear();
            foreach (var kv in live)
            {
                int cx = Mathf.FloorToInt(kv.Value.pos.x / CellSize), cz = Mathf.FloorToInt(kv.Value.pos.z / CellSize);
                if (Mathf.Abs(cx - pcx) > Reach + 1 || Mathf.Abs(cz - pcz) > Reach + 1) drop.Add(kv.Key);
            }
            foreach (var k in drop) { Destroy(live[k].go); live.Remove(k); }
        }

        Hazard TryMake(int cx, int cz)
        {
            int seed = WorldGen.Seed;
            float x = (cx + 0.15f + Hash.F(cx, cz, seed + 502) * 0.7f) * CellSize;
            float z = (cz + 0.15f + Hash.F(cx, cz, seed + 503) * 0.7f) * CellSize;
            if (!WorldGen.InsideWorld(x, z)) return null;
            float y = WorldGen.Height(x, z);
            if (y < 1.4f) return null;
            var b = WorldGen.Biome(x, z);
            if (b == BiomeId.Meadow || b == BiomeId.Isles || b == BiomeId.Glow) return null;
            int si = SubRegions.At(x, z, b, out float sw);
            string sub = si >= 0 && sw > 0.3f ? SubRegions.Get(si).id : null;
            Kind kind; float chance;
            switch (b)
            {
                case BiomeId.Badlands: kind = Kind.Geyser; chance = sub == "ashfall" ? 0.75f : 0.2f; break;
                case BiomeId.Desert: kind = Kind.Quicksand; chance = sub == "oasis" ? 0f : sub == "glassdunes" ? 0.5f : 0.3f; break;
                case BiomeId.Peaks: kind = Kind.ThinIce; chance = sub == "shiverglass" ? 0.85f : 0f; break;
                case BiomeId.Swamp: kind = Kind.SporePod; chance = 0.22f; break;
                default: kind = Kind.SporePod; chance = sub == "sporefall" ? 0.9f : 0.4f; break;   // Blight
            }
            if (Hash.F(cx, cz, seed + 501) >= chance) return null;
            // level enough for a flat disc, and clear of everything built
            float flat = kind == Kind.SporePod ? 2.5f : 4f, lo = y, hi = y;
            for (int i = 0; i < 4; i++)
            {
                float hh = WorldGen.Height(x + (i == 0 ? flat : i == 1 ? -flat : 0f), z + (i == 2 ? flat : i == 3 ? -flat : 0f));
                lo = Mathf.Min(lo, hh); hi = Mathf.Max(hi, hh);
            }
            if (hi - lo > (kind == Kind.SporePod || kind == Kind.Geyser ? 1.4f : 0.55f) || lo < 1f) return null;
            var p = new Vector3(x, y, z);
            if (Settlements.Inside(p, 30f) || Wardens.Inside(p, 12f) || Caves.BlocksNode(p) || Outposts.BlocksNode(p) || Pois.BlocksNode(p) || Landmarks.NearLandmark(p, 16f)) return null;
            if (BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.IsInsideBase(p, 10f)) return null;
            if (WorldGen.StartDistance(x, z) < 260f) return null;

            var h = new Hazard { kind = kind, pos = p, phase = Hash.F(cx, cz, seed + 504) * 10f };
            h.go = new GameObject("Hazard_" + kind);
            h.go.transform.SetParent(transform, false);
            h.go.transform.SetPositionAndRotation(kind == Kind.Quicksand || kind == Kind.ThinIce ? new Vector3(x, hi + 0.04f, z) : p, Quaternion.Euler(0f, Hash.F(cx, cz, seed + 505) * 360f, 0f));
            if (geyserMesh == null) BuildMeshes();
            switch (kind)
            {
                case Kind.Geyser: Part(h.go.transform, geyserMesh); break;
                case Kind.Quicksand: Part(h.go.transform, sandMesh); break;
                case Kind.ThinIce:
                    h.a = Part(h.go.transform, iceMesh);
                    Part(h.a, crackMesh).gameObject.SetActive(false);
                    h.b = Part(h.go.transform, holeMesh);
                    h.b.gameObject.SetActive(false);
                    break;
                default:
                    h.col = b == BiomeId.Swamp ? new Color(0.75f, 0.95f, 0.3f) : new Color(0.8f, 0.4f, 1f);
                    Part(h.go.transform, podStalk);
                    h.a = Part(h.go.transform, podBulb);
                    h.a.localPosition = new Vector3(0f, 0.9f, 0f);
                    // the bulb is tinted per land through a property block
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_FlashColor", new Color(h.col.r, h.col.g, h.col.b, 0.35f));
                    h.a.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
                    break;
            }
            return h;
        }



        static Transform Part(Transform parent, Mesh m)
        {
            var go = new GameObject(m.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        static void BuildMeshes()
        {

            // geyser: a pale mineral mound with a dark throat, ringed by stones
            var mb = new MeshBuilder();
            Color32 crust = new Color(0.86f, 0.82f, 0.7f), crustTop = new Color(0.96f, 0.93f, 0.8f), wet = new Color(0.5f, 0.7f, 0.72f), dark = new Color(0.06f, 0.07f, 0.09f);
            mb.Cylinder(M(0, 0.05f, 0, 5.6f, 0.5f, 5.6f), crust, crustTop, 12, 0.72f, true);
            mb.Cylinder(M(0, 0.35f, 0, 3.2f, 0.5f, 3.2f), crust, crustTop, 10, 0.7f, true);
            mb.Cylinder(M(0, 0.62f, 0, 1.9f, 0.08f, 1.9f), wet, 10);
            mb.Cylinder(M(0, 0.64f, 0, 1.2f, 0.08f, 1.2f), dark, 10);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.898f;
                mb.Rock(M(Mathf.Cos(a) * 2.9f, 0.15f, Mathf.Sin(a) * 2.9f, 0.9f + (i % 3) * 0.3f, 0.6f, 0.8f + (i % 2) * 0.3f), new Color(0.55f, 0.42f, 0.34f), new Color(0.7f, 0.6f, 0.5f), (uint)(i + 3), 6, 4);
            }
            geyserMesh = mb.ToMesh("hz_geyser");

            // quicksand: a dark, wet disc with slow rings
            mb = new MeshBuilder();
            Color32 s0 = new Color(0.62f, 0.5f, 0.3f), s1 = new Color(0.5f, 0.39f, 0.22f), s2 = new Color(0.42f, 0.32f, 0.18f);
            mb.Cylinder(M(0, -0.3f, 0, 10f, 0.66f, 10f), s0, 18);
            mb.Cylinder(M(0, 0.04f, 0, 7.6f, 0.02f, 7.6f), s1, 16);
            mb.Cylinder(M(0, 0.05f, 0, 5f, 0.02f, 5f), s0, 14);
            mb.Cylinder(M(0, 0.06f, 0, 2.8f, 0.02f, 2.8f), s2, 12);
            mb.Cylinder(M(0, 0.07f, 0, 1f, 0.02f, 1f), s1, 8);
            sandMesh = mb.ToMesh("hz_quicksand");

            // thin ice: a glassy pale disc; cracks and the hole are separate parts
            mb = new MeshBuilder();
            Color32 ice = new Color(0.82f, 0.95f, 1f), iceIn = new Color(0.62f, 0.86f, 0.98f);
            mb.Cylinder(M(0, -0.3f, 0, 8.4f, 0.66f, 8.4f), ice, 16);
            mb.Cylinder(M(0, 0.04f, 0, 6.2f, 0.02f, 6.2f), iceIn, 14);
            mb.Cylinder(M(1.2f, 0.05f, -0.8f, 2.2f, 0.02f, 1.6f), Color.white, 8);
            iceMesh = mb.ToMesh("hz_ice");
            mb = new MeshBuilder();
            Color32 crack = new Color(0.16f, 0.3f, 0.5f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51.4f;
                mb.Box(M(Mathf.Cos(a * Mathf.Deg2Rad) * 1.7f, 0.07f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.7f, 3.2f, 0.02f, 0.09f, 0, -a, 0), crack);
                mb.Box(M(Mathf.Cos((a + 18f) * Mathf.Deg2Rad) * 2.6f, 0.07f, Mathf.Sin((a + 18f) * Mathf.Deg2Rad) * 2.6f, 1.4f, 0.02f, 0.07f, 0, -a - 50f, 0), crack);
            }
            crackMesh = mb.ToMesh("hz_cracks");
            mb = new MeshBuilder();
            mb.Cylinder(M(0, -0.3f, 0, 8.4f, 0.66f, 8.4f), ice, 16);
            mb.Cylinder(M(0, 0.04f, 0, 6.4f, 0.02f, 6.4f), new Color(0.07f, 0.16f, 0.3f), 9);
            for (int i = 0; i < 6; i++)
                mb.Box(M(Mathf.Cos(i * 1.05f) * 2f, 0.1f, Mathf.Sin(i * 1.05f) * 2f, 1.1f, 0.14f, 0.8f, 8f, i * 57f, 6f), ice, Color.white);
            holeMesh = mb.ToMesh("hz_icehole");

            // spore pod: a fat bulb on a short stalk with a collar of leaves
            mb = new MeshBuilder();
            Color32 stalk = new Color(0.36f, 0.3f, 0.36f), leaf = new Color(0.28f, 0.34f, 0.24f);
            mb.Cylinder(M(0, 0.4f, 0, 0.5f, 0.8f, 0.5f), stalk, stalk, 7, 0.7f, true);
            for (int i = 0; i < 5; i++)
                mb.Sphere(M(Mathf.Cos(i * 1.256f) * 0.7f, 0.12f, Mathf.Sin(i * 1.256f) * 0.7f, 1f, 0.16f, 0.5f, 0, -i * 72f, 0), leaf, 6, 4);
            podStalk = mb.ToMesh("hz_podstalk");
            mb = new MeshBuilder();
            Color32 bulb = new Color(0.62f, 0.56f, 0.62f), bulbLow = new Color(0.42f, 0.36f, 0.44f), spot = new Color(0.95f, 0.95f, 0.8f);
            mb.Sphere(M(0, 0.55f, 0, 1.5f, 1.3f, 1.5f), bulb, bulbLow, 10, 7);
            for (int i = 0; i < 6; i++)
                mb.Sphere(M(Mathf.Cos(i * 1.05f) * 0.6f, 0.75f + (i % 2) * 0.25f, Mathf.Sin(i * 1.05f) * 0.6f, 0.3f, 0.3f, 0.3f), spot, 5, 4);
            mb.Cone(M(0, 1.3f, 0, 0.4f, 0.4f, 0.4f), bulbLow, 6);
            podBulb = mb.ToMesh("hz_podbulb");
        }

        // ================================================================ GEYSER
        void TickGeyser(Hazard h, float dt, PlayerController pl, float d2)
        {
            float c = (Time.time + h.phase) % 10f;
            Vector3 top = h.pos + Vector3.up * 0.7f;
            if (c < 6.5f)
            {
                h.fired = false;
                h.state = 0;
                if (fx && Random.value < 0.12f) Fx.Mote(top, new Vector3(Random.Range(-0.2f, 0.2f), 1.4f, Random.Range(-0.2f, 0.2f)), new Color(1f, 1f, 1f, 0.35f), 0.5f, 1.3f, false);
                return;
            }
            if (c < 8f)
            {
                // rumble: the warning
                if (h.state == 0) { h.state = 1; if (d2 < 40f * 40f) SynthAudio.Play(Sfx.Clunk, 0.5f); }
                if (fx)
                {
                    Fx.Mote(top, new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(2f, 4f), Random.Range(-0.6f, 0.6f)), new Color(1f, 1f, 1f, 0.5f), 0.5f, 0.8f, false);
                    if (Random.value < 0.3f) Fx.Mote(top, new Vector3(Random.Range(-2f, 2f), 3f, Random.Range(-2f, 2f)), new Color(0.7f, 0.9f, 1f), 0.12f, 0.5f, false);
                }
                return;
            }
            if (c < 9.7f)
            {
                if (h.state != 2) { h.state = 2; if (d2 < 55f * 55f) SynthAudio.Play(Sfx.Whoosh, 0.7f); }
                if (fx)
                    for (int i = 0; i < 3; i++)
                        Fx.Mote(top + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f)), new Vector3(Random.Range(-1f, 1f), Random.Range(13f, 20f), Random.Range(-1f, 1f)),
                            i == 0 ? new Color(0.75f, 0.92f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.8f), Random.Range(0.5f, 1f), Random.Range(0.6f, 1f), false);
                Vector3 pp = pl.Position;
                if (!h.fired && d2 < 1.9f * 1.9f && pp.y - h.pos.y < 3f && pl.IsAlive)
                {
                    h.fired = true;
                    pl.ReceiveHit(new HitInfo { damage = Mathf.Max(3f, pl.Stats.MaxHp * 0.04f), element = Element.Ember, effectiveness = 1f });
                    pl.Launch(30f);
                    if (!toldGeyser) { toldGeyser = true; GameEvents.Say("The geyser throws you skyward! Open a glider in the air (JUMP) to ride it further.", new Color(0.75f, 0.92f, 1f)); }
                }
            }
        }

        // ================================================================ QUICKSAND
        void TickSand(Hazard h, float dt, PlayerController pl, float d2)
        {
            Vector3 pp = pl.Position;
            if (fx && d2 < 35f * 35f && Random.value < 0.06f)
            {
                Vector2 r = Random.insideUnitCircle * 3.5f;
                Fx.Mote(h.go.transform.position + new Vector3(r.x, 0.12f, r.y), Vector3.up * 0.25f, new Color(0.4f, 0.3f, 0.16f, 0.7f), 0.3f, 0.9f, false);
            }
            if (d2 > 4.7f * 4.7f || pp.y - h.pos.y > 1.2f || !pl.IsAlive) { h.cool = 0f; return; }
            if (KinAid.QuicksandProof) return;   // a Stone kin out with you keeps you on firm footing
            pl.SlowMul = 0.32f;
            pl.DrainStamina(7f * dt);
            if (fx && Random.value < 0.4f) Fx.Mote(pp + Vector3.up * 0.15f, new Vector3(Random.Range(-1f, 1f), 0.8f, Random.Range(-1f, 1f)), new Color(0.6f, 0.48f, 0.28f, 0.8f), 0.25f, 0.5f, false);
            if (!toldSand) { toldSand = true; GameEvents.Warn("Quicksand! It drags at your legs - roll or jump to break free."); }
            if (pl.Stats.stamina <= 0.5f)
            {
                h.cool -= dt;
                if (h.cool <= 0f)
                {
                    h.cool = 1f;
                    pl.ReceiveHit(new HitInfo { damage = Mathf.Max(2f, pl.Stats.MaxHp * 0.04f), element = Element.Stone, effectiveness = 1f });
                    HUD.DamageNumber(pp + Vector3.up * 2.2f, "SINKING", new Color(0.9f, 0.75f, 0.4f), 0.8f);
                }
            }
        }

        // ================================================================ THIN ICE
        void TickIce(Hazard h, float dt, PlayerController pl, float d2)
        {
            Vector3 pp = pl.Position;
            bool on = d2 < 3.9f * 3.9f && pp.y - h.pos.y < 1.2f && pl.IsAlive;
            if (h.state == 2)
            {
                // open water: refreezes after a while
                h.t -= dt;
                if (h.t <= 0f) { h.state = 0; h.stood = 0f; h.b.gameObject.SetActive(false); h.a.gameObject.SetActive(true); h.a.GetChild(0).gameObject.SetActive(false); }
                else if (on) pl.SlowMul = 0.5f;
                return;
            }
            h.stood = on ? h.stood + dt : Mathf.Max(0f, h.stood - dt * 0.5f);
            if (h.state == 0 && h.stood > 0.8f)
            {
                h.state = 1;
                h.a.GetChild(0).gameObject.SetActive(true);
                SynthAudio.Play(Sfx.Clunk, 1.9f);
                if (!toldIce) { toldIce = true; GameEvents.Warn("The ice is cracking under you - keep moving!"); }
            }
            else if (h.state == 1 && h.stood <= 0.05f) { h.state = 0; h.a.GetChild(0).gameObject.SetActive(false); }
            if (h.state == 1 && h.stood > 1.7f)
            {
                h.state = 2;
                h.t = 25f;
                h.a.gameObject.SetActive(false);
                h.b.gameObject.SetActive(true);
                Fx.Burst(h.go.transform.position + Vector3.up * 0.3f, new Color(0.85f, 0.95f, 1f), 26, 6f, 0.3f, 0.7f, false, 9f);
                SynthAudio.Play(Sfx.HitHeavy, 1.4f);
                if (on)
                {
                    pl.ReceiveHit(new HitInfo { damage = Mathf.Max(4f, pl.Stats.MaxHp * 0.08f), element = Element.Frost, effectiveness = 1f, status = StatusType.Chill, statusChance = 2f });
                    GameEvents.Warn("The ice gives way! You haul yourself out, chilled to the bone.");
                }
            }
        }

        // ================================================================ SPORE POD
        void TickPod(Hazard h, float dt, PlayerController pl, float d2)
        {
            Vector3 pp = pl.Position;
            Vector3 top = h.pos + Vector3.up * 1.5f;
            switch (h.state)
            {
                case 0:   // waiting, breathing slowly
                    h.a.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2f + h.phase) * 0.05f);
                    if (d2 < 3.4f * 3.4f && pl.IsAlive && Mathf.Abs(pp.y - h.pos.y) < 3f)
                    {
                        h.state = 1; h.t = 0.9f;
                        SynthAudio.Play(Sfx.Shake, 0.7f);
                        if (!toldPod) { toldPod = true; GameEvents.Warn("A spore pod! Back away while it swells."); }
                    }
                    break;
                case 1:   // swelling: the warning
                    h.t -= dt;
                    h.a.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, h.t / 0.9f) * (1f + Mathf.Sin(Time.time * 40f) * 0.04f);
                    if (h.t <= 0f)
                    {
                        h.state = 2; h.t = 6f; h.cool = 0f;
                        h.a.gameObject.SetActive(false);
                        Fx.Burst(top, h.col, 30, 5f, 0.5f, 1.2f, false);
                        SynthAudio.Play(Sfx.Burst, 0.8f);
                    }
                    break;
                case 2:   // the cloud hangs for a few seconds
                    h.t -= dt;
                    if (fx)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 r = Random.insideUnitSphere * 4.4f; r.y = Mathf.Abs(r.y) * 0.55f;
                            Fx.Mote(h.pos + Vector3.up * 0.5f + r, new Vector3(r.x * 0.08f, 0.2f, r.z * 0.08f), new Color(h.col.r, h.col.g, h.col.b, 0.45f), Random.Range(1f, 1.8f), 1.5f, false);
                        }
                    }
                    h.cool -= dt;
                    if (h.cool <= 0f && d2 < 4.6f * 4.6f && Mathf.Abs(pp.y - h.pos.y) < 3.5f && pl.IsAlive)
                    {
                        h.cool = 1f;
                        pl.ReceiveHit(new HitInfo { damage = Mathf.Max(1f, pl.Stats.MaxHp * 0.015f), element = Element.Shade, effectiveness = 1f, status = StatusType.Poison, statusChance = 2f });
                    }
                    if (h.t <= 0f) { h.state = 3; h.t = 30f; }
                    break;
                default:  // spent: grows back
                    h.t -= dt;
                    if (h.t <= 5f)
                    {
                        if (!h.a.gameObject.activeSelf) h.a.gameObject.SetActive(true);
                        h.a.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, Mathf.Max(0f, h.t) / 5f);
                    }
                    if (h.t <= 0f) h.state = 0;
                    break;
            }
        }
    }
}
