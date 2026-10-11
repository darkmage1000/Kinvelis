using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Every land changes after dark, in a way you can see and a way that matters:
    /// Meadow - fireflies, drowsy kin (+10% catch).            Badlands - rising embers, Shade kin take over from Ember.
    /// Peaks - an aurora (+25% XP for your kin).                Isles - the surf glows, Tide kin come ashore.
    /// Swamp - drifting wisps you can catch for Glimmer.        Desert - shooting stars, Shade hunters.
    /// Blight - thick miasma, wild kin +2 levels, drops x1.5.   Glowdeep - everything blooms, shimmer odds x2.
    /// The rules are static getters read at the use site (capture, XP, spawns, drops, fog); the visuals run here.
    /// </summary>
    public class Nightfall : MonoBehaviour
    {
        public static Nightfall I { get; private set; }

        static bool night;
        static BiomeId biome;
        /// <summary>Night, outdoors, at the player.</summary>
        public static bool Active => night && !Caves.InCave;

        public static float CatchMul => Active && biome == BiomeId.Meadow ? 1.1f : 1f;
        public static float KinXpMul => Active && biome == BiomeId.Peaks ? 1.25f : 1f;
        public static float ShimmerMul => Active && biome == BiomeId.Glow ? 2f : 1f;
        public static float DropMul => Active && biome == BiomeId.Blight ? 1.5f : 1f;
        public static float FogMul => Active && biome == BiomeId.Blight ? 0.6f : 1f;
        public static int LevelBonus(BiomeId b, bool isNight) => isNight && b == BiomeId.Blight ? 2 : 0;

        public static string HudLine => Active ? Title(biome) : null;

        public static string Title(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Meadow: return "Still night: +10% catch";
                case BiomeId.Badlands: return "Emberglow: Shade kin stir";
                case BiomeId.Peaks: return "Aurora: +25% kin XP";
                case BiomeId.Isles: return "Glowtide: Tide kin ashore";
                case BiomeId.Swamp: return "Lanternlight: catch the wisps";
                case BiomeId.Desert: return "Cold sands: Shade kin hunt";
                case BiomeId.Blight: return "Deep miasma: kin +2 Lv, drops x1.5";
                default: return "Full bloom: shimmer odds x2";
            }
        }

        static string Story(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Meadow: return "Night settles on the vale. Fireflies rise, and drowsy kin are easier to catch (+10%).";
                case BiomeId.Badlands: return "The Badlands glow after dark. Ember kin bed down; Shade kin come out to hunt.";
                case BiomeId.Peaks: return "An aurora spreads over the Peaks. Your kin learn faster under it (+25% XP).";
                case BiomeId.Isles: return "The surf lights up blue. Tide kin are coming ashore.";
                case BiomeId.Swamp: return "Wisps drift out over the fen. Catch one before it gutters - each holds a little Glimmer.";
                case BiomeId.Desert: return "The sand turns cold under falling stars. The heat is gone, and the night hunters are out.";
                case BiomeId.Blight: return "The miasma thickens. Wild kin are stronger tonight (+2 levels), and carry more (drops x1.5).";
                default: return "Glowdeep comes into full bloom. Rare colours are twice as likely tonight.";
            }
        }

        /// <summary>Which kin turn up more (or less) after dark, land by land.</summary>
        public static float SpawnMul(SpeciesDef s, BiomeId b, bool isNight)
        {
            if (!isNight) return 1f;
            switch (b)
            {
                case BiomeId.Badlands:
                    if (Is(s, Element.Shade)) return 3f;
                    if (s.element1 == Element.Ember) return 0.5f;
                    return 1f;
                case BiomeId.Peaks: return Is(s, Element.Frost) || Is(s, Element.Lumen) ? 1.6f : 1f;
                case BiomeId.Isles: return Is(s, Element.Tide) ? 2f : 1f;
                case BiomeId.Swamp: return Is(s, Element.Lumen) ? 2.5f : 1f;
                case BiomeId.Desert: return Is(s, Element.Shade) ? 3f : 1f;
                case BiomeId.Glow: return Is(s, Element.Lumen) ? 1.5f : 1f;
                default: return 1f;
            }
        }

        static bool Is(SpeciesDef s, Element e) => s.element1 == e || s.element2 == e;

        // ================================================================ RUNTIME
        const int WispsPerNight = 15;
        class Wisp { public Vector3 pos; public float phase, life; }

        readonly List<Wisp> wisps = new List<Wisp>();
        readonly HashSet<BiomeId> told = new HashSet<BiomeId>();
        float moteTimer, wispTimer, starTimer, checkTimer, wispDraw;
        int wispsCaught;
        float fade;
        GameObject aurora;
        Mesh auroraMesh;
        Vector3[] auroraVerts;
        Color32[] auroraCols;
        const int Ribbons = 3, Segs = 28;

        public void Init()
        {
            I = this;
            night = false;
        }

        public void Tick(float dt, PlayerController pl)
        {
            var env = EnvironmentSystem.I;
            if (env == null) return;
            checkTimer -= dt;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.5f;
                bool was = night;
                night = env.IsNight;
                biome = env.CurrentBiome;
                if (!night) { if (was) { told.Clear(); wispsCaught = 0; } }
                else if (!Caves.InCave && told.Add(biome)) GameEvents.Say(Story(biome), new Color(0.66f, 0.78f, 1f));
            }
            bool on = Active;
            fade = Mathf.MoveTowards(fade, on ? 1f : 0f, dt * 0.25f);
            TickAurora(pl, on && biome == BiomeId.Peaks);
            if (!on) { if (wisps.Count > 0) wisps.Clear(); return; }

            bool lean = QualityProfile.Current.particleBudget < 200;
            Vector3 pp = pl.Position;
            moteTimer -= dt;
            if (moteTimer <= 0f)
            {
                moteTimer = (lean ? 0.2f : 0.1f) / Mathf.Max(0.2f, fade);
                float a = Random.value * Mathf.PI * 2f, d = Random.Range(3f, 24f);
                var p = new Vector3(pp.x + Mathf.Cos(a) * d, 0f, pp.z + Mathf.Sin(a) * d);
                float h = WorldGen.Height(p.x, p.z);
                Vector3 j = Random.insideUnitSphere;
                switch (biome)
                {
                    case BiomeId.Meadow:
                        if (h > 0.5f) Fx.Mote(new Vector3(p.x, h + Random.Range(0.5f, 2.4f), p.z), new Vector3(j.x * 0.5f, j.y * 0.25f, j.z * 0.5f), new Color(0.85f, 1f, 0.4f), 0.1f, Random.Range(1.6f, 2.8f), true);
                        break;
                    case BiomeId.Badlands:
                        if (h > 0.5f) Fx.Mote(new Vector3(p.x, h + 0.1f, p.z), new Vector3(j.x * 0.3f, Random.Range(0.8f, 1.8f), j.z * 0.3f), Random.value < 0.6f ? new Color(1f, 0.5f, 0.15f) : new Color(1f, 0.8f, 0.3f), 0.09f, Random.Range(1.4f, 2.4f), true);
                        break;
                    case BiomeId.Isles:
                        // sparks of light where the water meets the sand
                        if (h < 0.15f && h > -1.4f)
                        {
                            float wy = Tides.Surface(p.x, p.z) + 0.06f;
                            Fx.Mote(new Vector3(p.x, wy, p.z), new Vector3(j.x * 0.3f, 0.05f, j.z * 0.3f), Random.value < 0.7f ? new Color(0.3f, 0.95f, 1f) : new Color(0.7f, 1f, 0.9f), Random.Range(0.1f, 0.2f), Random.Range(1f, 2f), true);
                        }
                        break;
                    case BiomeId.Blight:
                        if (h > 0.5f) Fx.Mote(new Vector3(p.x, h + Random.Range(0.3f, 1.6f), p.z), new Vector3(j.x * 0.4f, 0.1f, j.z * 0.4f), new Color(0.5f, 0.25f, 0.6f, 0.3f), Random.Range(0.5f, 0.9f), Random.Range(2f, 3.4f), false);
                        break;
                    case BiomeId.Glow:
                        if (h > 0.5f) Fx.Mote(new Vector3(p.x, h + 0.2f, p.z), new Vector3(j.x * 0.2f, Random.Range(0.5f, 1.1f), j.z * 0.2f), Random.value < 0.6f ? new Color(0.4f, 1f, 0.9f) : new Color(1f, 0.6f, 0.95f), 0.11f, Random.Range(2f, 3.2f), true);
                        break;
                    case BiomeId.Swamp:
                        if (Random.value < 0.4f) Fx.Mote(new Vector3(p.x, Mathf.Max(h, 0f) + Random.Range(0.4f, 1.6f), p.z), new Vector3(j.x * 0.3f, 0.1f, j.z * 0.3f), new Color(0.7f, 1f, 0.4f), 0.07f, 2f, true);
                        break;
                }
            }

            if (biome == BiomeId.Desert)
            {
                starTimer -= dt;
                if (starTimer <= 0f)
                {
                    starTimer = Random.Range(3f, 9f);
                    float a = Random.value * Mathf.PI * 2f;
                    var from = pp + new Vector3(Mathf.Cos(a) * 120f, Random.Range(70f, 110f), Mathf.Sin(a) * 120f);
                    var vel = new Vector3(-Mathf.Sin(a) * 90f, -22f, Mathf.Cos(a) * 90f);
                    for (int i = 0; i < 9; i++) Fx.Mote(from - vel * (i * 0.012f), vel, i == 0 ? Color.white : new Color(0.8f, 0.9f, 1f, 1f - i * 0.1f), 1.6f - i * 0.14f, 0.9f, true);
                }
            }

            if (biome == BiomeId.Swamp) TickWisps(dt, pl);
            else if (wisps.Count > 0) wisps.Clear();
        }

        // ---------------------------------------------------------------- wisps (Swamp)
        void TickWisps(float dt, PlayerController pl)
        {
            Vector3 pp = pl.Position;
            wispTimer -= dt;
            if (wispTimer <= 0f && wisps.Count < 4 && wispsCaught < WispsPerNight)
            {
                wispTimer = Random.Range(4f, 9f);
                float a = Random.value * Mathf.PI * 2f, d = Random.Range(20f, 42f);
                var p = new Vector3(pp.x + Mathf.Cos(a) * d, 0f, pp.z + Mathf.Sin(a) * d);
                p.y = Mathf.Max(WorldGen.Height(p.x, p.z), Tides.Surface(p.x, p.z)) + 1.3f;
                if (WorldGen.Biome(p.x, p.z) == BiomeId.Swamp) wisps.Add(new Wisp { pos = p, phase = Random.value * 6f, life = 45f });
            }
            wispDraw -= dt;
            bool draw = wispDraw <= 0f;
            if (draw) wispDraw = 0.07f;
            for (int i = wisps.Count - 1; i >= 0; i--)
            {
                var w = wisps[i];
                w.life -= dt;
                // they drift in a slow circle, bobbing
                w.pos += new Vector3(Mathf.Cos(Time.time * 0.4f + w.phase), 0f, Mathf.Sin(Time.time * 0.4f + w.phase)) * (0.9f * dt);
                Vector3 p = w.pos + Vector3.up * (Mathf.Sin(Time.time * 1.7f + w.phase) * 0.3f);
                if (draw)
                {
                    Fx.Mote(p, Vector3.zero, new Color(0.9f, 1f, 0.7f), 0.9f, 0.2f, true);
                    Fx.Mote(p, Vector3.zero, new Color(0.5f, 1f, 0.5f, 0.3f), 2.6f, 0.2f, true);
                    Fx.Mote(p, new Vector3(Random.Range(-0.3f, 0.3f), -0.6f, Random.Range(-0.3f, 0.3f)), new Color(0.6f, 1f, 0.7f, 0.8f), 0.2f, 0.9f, true);
                }
                Vector3 d = p - pp; float dy = d.y; d.y = 0f;
                if (d.sqrMagnitude < 1.7f * 1.7f && dy < 3.2f && dy > -1f)
                {
                    int g = Random.Range(4, 10);
                    pl.Stats.glimmer += g;
                    wispsCaught++;
                    Fx.Burst(p, new Color(0.75f, 1f, 0.55f), 18, 4f, 0.2f, 0.6f, true);
                    SynthAudio.Play(Sfx.Pickup, 1.5f);
                    GameEvents.Say(wispsCaught >= WispsPerNight ? $"+{g} Glimmer. The last wisps scatter for tonight." : $"Caught a wisp: +{g} Glimmer", new Color(0.75f, 1f, 0.55f));
                    GameEvents.RaiseInventoryChanged();
                    wisps.RemoveAt(i);
                    continue;
                }
                if (w.life <= 0f || d.sqrMagnitude > 80f * 80f) wisps.RemoveAt(i);
            }
        }

        // ---------------------------------------------------------------- aurora (Peaks)
        void TickAurora(PlayerController pl, bool want)
        {
            if (!want && (aurora == null || fade <= 0.001f || biome != BiomeId.Peaks))
            {
                if (aurora != null && aurora.activeSelf) aurora.SetActive(false);
                return;
            }
            if (aurora == null) BuildAurora();
            if (!aurora.activeSelf) aurora.SetActive(true);
            Vector3 pp = pl.Position;
            aurora.transform.position = new Vector3(pp.x, pp.y + 95f, pp.z);
            float t = Time.time;
            int v = 0;
            for (int r = 0; r < Ribbons; r++)
            {
                float z0 = -150f + r * 150f + Mathf.Sin(t * 0.05f + r) * 30f;
                for (int s = 0; s <= Segs; s++)
                {
                    float u = s / (float)Segs;
                    float x = Mathf.Lerp(-330f, 330f, u);
                    float z = z0 + Mathf.Sin(u * 5f + t * 0.22f + r * 1.7f) * 42f + Mathf.Sin(u * 13f - t * 0.35f + r) * 12f;
                    float lift = Mathf.Sin(u * 3f + t * 0.15f + r * 2.1f) * 14f;
                    float edge = Mathf.Sin(u * Mathf.PI);                                   // fades at both ends
                    float flick = 0.55f + 0.45f * Mathf.Sin(u * 21f + t * 1.3f + r * 3f);   // bright folds
                    byte a = (byte)(255f * fade * edge * flick * 0.85f);
                    auroraVerts[v] = new Vector3(x, lift, z);
                    auroraVerts[v + 1] = new Vector3(x, lift + 26f, z + 5f);
                    auroraVerts[v + 2] = new Vector3(x, lift + 58f, z + 12f);
                    Color32 low = r == 1 ? new Color32(90, 255, 200, 0) : new Color32(80, 255, 140, 0);
                    Color32 mid = r == 1 ? new Color32(80, 230, 255, a) : new Color32(110, 255, 170, a);
                    Color32 top = new Color32(190, 110, 255, 0);
                    auroraCols[v] = low; auroraCols[v + 1] = mid; auroraCols[v + 2] = top;
                    v += 3;
                }
            }
            auroraMesh.vertices = auroraVerts;
            auroraMesh.colors32 = auroraCols;
        }

        void BuildAurora()
        {
            aurora = new GameObject("Aurora");
            aurora.transform.SetParent(transform, false);
            int per = (Segs + 1) * 3;
            auroraVerts = new Vector3[Ribbons * per];
            auroraCols = new Color32[Ribbons * per];
            var uv = new Vector2[Ribbons * per];
            var tris = new List<int>();
            for (int r = 0; r < Ribbons; r++)
            for (int s = 0; s <= Segs; s++)
            {
                int b = r * per + s * 3;
                uv[b] = uv[b + 1] = uv[b + 2] = new Vector2(0.5f, 0.5f);   // centre of the soft particle texture
                if (s == Segs) continue;
                for (int row = 0; row < 2; row++)
                {
                    int a0 = b + row, a1 = b + row + 1, b0 = b + 3 + row, b1 = b + 3 + row + 1;
                    tris.Add(a0); tris.Add(a1); tris.Add(b1); tris.Add(a0); tris.Add(b1); tris.Add(b0);
                    tris.Add(a0); tris.Add(b1); tris.Add(a1); tris.Add(a0); tris.Add(b0); tris.Add(b1);   // both sides
                }
            }
            auroraMesh = new Mesh { name = "aurora" };
            auroraMesh.MarkDynamic();
            auroraMesh.vertices = auroraVerts;
            auroraMesh.colors32 = auroraCols;
            auroraMesh.uv = uv;
            auroraMesh.SetTriangles(tris, 0);
            auroraMesh.bounds = new Bounds(Vector3.zero, new Vector3(900f, 200f, 900f));
            aurora.AddComponent<MeshFilter>().sharedMesh = auroraMesh;
            var mr = aurora.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.ParticleAdd;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }
}
