using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wildbond
{
    /// <summary>
    /// The hatching show (MenuUI.Hatch): an egg on its own little stage far below the world, rendered to a texture.
    /// It rocks, gets three knocks from inside (light leaks out of the crack each time), shakes, bursts - the cap
    /// flies off, shell shards and sparks everywhere - and the kin pops out and stays on a slow turntable.
    /// Everything is driven by unscaled time (the menu pauses the world) and drawn by the stage's own camera, so the
    /// world's particle systems are not used. Egg colours come from the kin's elements; the glow round it hints at
    /// its rarity; a shimmer egg has golden, self-lit spots.
    /// </summary>
    public class HatchStage
    {
        const int Layer = 30;
        static readonly Vector3 Origin = new Vector3(400f, -3200f, 0f);   // KinStage sits at x 0: far enough apart for 80 m cameras
        public const float AppearTime = 0.45f, BurstAt = 3.4f;
        static readonly float[] Knocks = { 1.15f, 1.95f, 2.6f };
        const float EggR = 0.5f, EggH = 1.3f;

        public RenderTexture Texture { get; private set; }
        public float T { get; private set; }
        public bool Burst => T >= BurstAt;
        /// <summary>The knock that just happened (0-2) this frame, -1 otherwise; the burst is 3. The UI plays sounds from it.</summary>
        public int Event { get; private set; } = -1;

        readonly GameObject root;
        readonly Camera cam;
        readonly Transform eggRoot, top, bottom, kinRoot, rays;
        readonly MeshFilter topF, bottomF, raysF;
        readonly Mesh fxMesh, shardMesh;
        Color bg, rarityCol, elCol, el2Col, shellCol, spotCol;
        bool shimmer;
        CreatureRig rig;
        float yaw, focusY, gap, raysBack;
        Vector3 eggFocus, eggCam, kinFocus, kinCam;
        Vector3 topVel, topSpin;
        readonly System.Random rnd = new System.Random();

        struct Spark { public Vector3 p, v; public Color c; public float life, age, size, grav; }
        struct Shard { public Vector3 p, v, axis; public float spin, ang, size, age, life; public Color c; }
        readonly List<Spark> sparks = new List<Spark>(256);
        readonly List<Shard> shards = new List<Shard>(64);
        readonly List<Vector3> fv = new List<Vector3>(); readonly List<Color> fc = new List<Color>(); readonly List<Vector2> fu = new List<Vector2>(); readonly List<int> ft = new List<int>();
        readonly List<Vector3> sv = new List<Vector3>(); readonly List<Color> sc = new List<Color>(); readonly List<Vector3> sn = new List<Vector3>(); readonly List<int> st = new List<int>();

        public HatchStage()
        {
            root = new GameObject("HatchStage");
            root.transform.position = Origin;
            var camGo = new GameObject("HatchStageCam");
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 80f;
            cam.allowHDR = false;
            var extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = false;
            extra.renderShadows = false;
            Texture = new RenderTexture(1000, 860, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "HatchStageRT" };
            Texture.Create();
            cam.targetTexture = Texture;

            rays = Child("Rays", root.transform, RenderAssets.ParticleAdd, out raysF);
            eggRoot = new GameObject("Egg").transform; eggRoot.SetParent(root.transform, false); eggRoot.gameObject.layer = Layer;
            bottom = Child("Bottom", eggRoot, RenderAssets.Creature, out bottomF);
            top = Child("Top", eggRoot, RenderAssets.Creature, out topF);
            kinRoot = new GameObject("Kin").transform; kinRoot.SetParent(root.transform, false);
            fxMesh = new Mesh { name = "HatchFx" }; fxMesh.MarkDynamic();
            Child("Fx", root.transform, RenderAssets.ParticleAdd, out var fxF).localPosition = Vector3.zero; fxF.sharedMesh = fxMesh;
            shardMesh = new Mesh { name = "HatchShards" }; shardMesh.MarkDynamic();
            Child("Shards", root.transform, RenderAssets.Creature, out var shF); shF.sharedMesh = shardMesh;
            root.SetActive(false);
        }

        static Transform Child(string name, Transform parent, Material mat, out MeshFilter mf)
        {
            var go = new GameObject(name);
            go.layer = Layer;
            go.transform.SetParent(parent, false);
            mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        // ============================================================ SET UP
        public void Begin(CreatureData d)
        {
            var sp = d.Species;
            root.SetActive(true);
            T = 0f; Event = -1; gap = 0f; yaw = 0f;
            sparks.Clear(); shards.Clear();
            shimmer = d.shimmer;
            elCol = ElementInfo.Color(sp.element1);
            el2Col = sp.element2 != sp.element1 && sp.element2 != Element.Normal ? ElementInfo.Color(sp.element2) : Color.Lerp(elCol, Color.black, 0.35f);
            rarityCol = sp.rarity <= Rarity.Common ? new Color(0.92f, 0.92f, 0.85f) : RarityInfo.Color(sp.rarity);
            shellCol = Color.Lerp(new Color(0.98f, 0.95f, 0.88f), elCol, 0.32f);
            spotCol = shimmer ? new Color(1f, 0.82f, 0.3f) : Color.Lerp(el2Col, Color.white, 0.1f);
            bg = Color.Lerp(new Color(0.05f, 0.06f, 0.14f), elCol, 0.16f);

            // the egg: two halves split along a zig-zag crack, each with a glowing inside
            int seed = (d.uid ?? "egg").GetHashCode();
            BuildEgg(seed, out var bm, out var tm);
            if (bottomF.sharedMesh != null) Object.Destroy(bottomF.sharedMesh);
            if (topF.sharedMesh != null) Object.Destroy(topF.sharedMesh);
            bottomF.sharedMesh = bm; topF.sharedMesh = tm;
            eggRoot.gameObject.SetActive(true);
            eggRoot.localPosition = Vector3.zero; eggRoot.localRotation = Quaternion.identity; eggRoot.localScale = Vector3.zero;
            top.localPosition = Vector3.zero; top.localRotation = Quaternion.identity; top.localScale = Vector3.one;
            bottom.localPosition = Vector3.zero; bottom.localScale = Vector3.one;
            if (raysF.sharedMesh != null) Object.Destroy(raysF.sharedMesh);
            raysF.sharedMesh = BuildRays(Color.Lerp(rarityCol, Color.white, 0.25f), sp.rarity <= Rarity.Common ? 0.28f : sp.rarity == Rarity.Uncommon ? 0.4f : 0.55f);   // rarer = brighter
            rays.localScale = Vector3.zero;

            // the kin, hidden until the burst
            if (rig != null && rig.Model != null) Object.Destroy(rig.Model.gameObject);
            kinRoot.localRotation = Quaternion.identity; kinRoot.localScale = Vector3.one; kinRoot.localPosition = Vector3.zero;
            rig = ModelBuilder.Build(d, kinRoot, RenderAssets.Creature);
            rig.Animate(0.016f, 0f, false, false);
            foreach (var t in kinRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            var b = new Bounds(kinRoot.position, Vector3.zero); bool any = false;
            foreach (var r in kinRoot.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            // two framings: the egg close up, then (gliding out after the burst) the whole kin with room under its feet
            float radius = Mathf.Max(0.5f, b.extents.magnitude * 0.9f);
            focusY = Mathf.Max(0.25f, b.center.y - Origin.y) - radius * 0.12f;
            float dist = radius / Mathf.Tan(13f * Mathf.Deg2Rad);
            kinFocus = Origin + new Vector3(0f, focusY, 0f);
            kinCam = kinFocus + new Vector3(0f, radius * 0.3f, dist);
            eggFocus = Origin + new Vector3(0f, EggH * 0.52f, 0f);
            eggCam = eggFocus + new Vector3(0f, 0.3f, 1.05f / Mathf.Tan(13f * Mathf.Deg2Rad));
            raysBack = Mathf.Max(0.9f, radius * 0.9f);
            PlaceCamera(0f);
            kinRoot.localScale = Vector3.zero;
            cam.backgroundColor = bg;
        }

        void PlaceCamera(float k)
        {
            k = k * k * (3f - 2f * k);
            Vector3 focus = Vector3.Lerp(eggFocus, kinFocus, k);
            cam.transform.position = Vector3.Lerp(eggCam, kinCam, k);
            cam.transform.LookAt(focus);
            rays.position = focus + (focus - cam.transform.position).normalized * raysBack;
            var spin = rays.localEulerAngles.z;
            rays.rotation = Quaternion.LookRotation(rays.position - cam.transform.position) * Quaternion.Euler(0f, 0f, spin);
        }

        public void Skip() { if (T < BurstAt - 0.3f) T = BurstAt - 0.3f; }

        public void Hide()
        {
            root.SetActive(false);
            if (rig != null && rig.Model != null) Object.Destroy(rig.Model.gameObject);
            rig = null;
        }

        // ============================================================ PLAY
        public void Tick(float dt)
        {
            dt = Mathf.Min(dt, 0.05f);
            float was = T;
            T += dt;
            Event = -1;
            for (int k = 0; k < Knocks.Length; k++) if (was < Knocks[k] && T >= Knocks[k]) { Event = k; Knock(k); }
            if (was < BurstAt && T >= BurstAt) { Event = 3; DoBurst(); }

            if (!Burst) TickEgg(dt);
            else TickAfter(dt);

            // aura motes: rarity colour round the egg, the kin's colours afterwards
            float rate = Burst ? (shimmer ? 26f : 12f) : 10f + 14f * Mathf.Clamp01(T / BurstAt);
            if (Rand() < rate * dt)
            {
                float a = Rand() * Mathf.PI * 2f, r = 0.55f + Rand() * 0.5f;
                Color c = Burst ? (shimmer && Rand() < 0.6f ? new Color(1f, 0.85f, 0.35f) : Rand() < 0.5f ? elCol : rarityCol) : rarityCol;
                Add(new Vector3(Mathf.Cos(a) * r, Rand() * 0.5f, Mathf.Sin(a) * r * 0.6f), new Vector3(0, 0.35f + Rand() * 0.35f, 0), c, 1.6f + Rand(), 0.07f + Rand() * 0.05f, -0.1f);
            }
            TickFx(dt);
        }

        void TickEgg(float dt)
        {
            float appear = Mathf.Clamp01(T / AppearTime);
            float s = EaseOutBack(appear);
            float lastKnock = -10f; int kn = -1;
            for (int k = 0; k < Knocks.Length; k++) if (T >= Knocks[k]) { lastKnock = Knocks[k]; kn = k; }
            float since = T - lastKnock;
            float ang = 3.5f * Mathf.Sin(T * 4.2f);   // a gentle rock
            if (since < 0.45f) ang += (9f + 6f * kn) * Mathf.Sin(since * 30f) * (1f - since / 0.45f);
            float shake = Mathf.Clamp01((T - (BurstAt - 0.55f)) / 0.55f);
            if (shake > 0f) { ang += 9f * shake * Mathf.Sin(T * 52f); s *= 1f + 0.08f * shake + 0.03f * Mathf.Sin(T * 40f); }
            eggRoot.localScale = Vector3.one * s;
            eggRoot.localRotation = Quaternion.Euler(0f, 0f, ang);
            // the crack opens a little at each knock, so the light inside shows through
            float want = kn < 0 ? 0f : 0.018f * (kn + 1) + 0.02f * shake;
            gap = Mathf.MoveTowards(gap, want, dt * 0.25f);
            top.localPosition = new Vector3(0f, gap, 0f);
            float rs = 0.25f * Mathf.Clamp01((T - Knocks[0]) / (BurstAt - Knocks[0])) + 0.6f * shake;
            rays.localScale = Vector3.one * rs;
            rays.Rotate(0f, 0f, dt * 25f, Space.Self);
            if (shake > 0f && Rand() < 30f * shake * dt) SeamSpark(1);
        }

        void TickAfter(float dt)
        {
            float t = T - BurstAt;
            // the cap flies up and away, the cup sinks out of sight
            topVel += Vector3.down * 9f * dt;
            top.localPosition += topVel * dt;
            top.Rotate(topSpin * dt, Space.Self);
            top.localScale = Vector3.one * Mathf.Clamp01(1f - (t - 0.5f) / 0.4f);
            bottom.localScale = Vector3.one * Mathf.Clamp01(1f - t / 0.45f);
            eggRoot.localRotation = Quaternion.identity;
            if (t > 1f) eggRoot.gameObject.SetActive(false);

            // the kin pops out: overshoot, settle, one little hop
            float pop = 1f - Mathf.Exp(-t * 7f) * Mathf.Cos(t * 13f);
            kinRoot.localScale = Vector3.one * Mathf.Max(0f, pop);
            float hop = t > 0.55f && t < 1.05f ? Mathf.Sin((t - 0.55f) / 0.5f * Mathf.PI) * 0.22f : 0f;
            kinRoot.localPosition = new Vector3(0f, hop, 0f);
            yaw += dt * 18f;
            kinRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(yaw * Mathf.Deg2Rad) * 32f, 0f);
            rig?.Animate(dt, 0f, false, false);
            PlaceCamera(Mathf.Clamp01((t - 0.05f) / 0.7f));

            float rs = t < 0.3f ? Mathf.Lerp(0.85f, 1.35f, t / 0.3f) : Mathf.Lerp(1.35f, 0.75f, Mathf.Clamp01((t - 0.3f) / 1.4f));
            rays.localScale = Vector3.one * rs;
            rays.Rotate(0f, 0f, dt * 14f, Space.Self);
            cam.backgroundColor = Color.Lerp(Color.Lerp(bg, rarityCol, 0.35f), Color.Lerp(bg, rarityCol, 0.1f), Mathf.Clamp01(t / 1.2f));
        }

        void Knock(int k)
        {
            for (int i = 0; i < 10 + 6 * k; i++) SeamSpark(1.4f + 0.3f * k);
            for (int i = 0; i < 2 + k; i++) AddShard(SeamPoint(out var n) , n * (0.8f + Rand()) + Vector3.up * 1.2f, 0.035f + Rand() * 0.03f, Rand() < 0.5f ? shellCol : spotCol);
        }

        void DoBurst()
        {
            topVel = new Vector3((Rand() - 0.5f) * 1.6f, 4.2f, 0.6f);
            topSpin = new Vector3(Rand() * 200f - 100f, Rand() * 100f, 160f + Rand() * 200f);
            Color[] confetti = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.9f, 0.5f), new Color(0.4f, 0.7f, 1f), new Color(0.85f, 0.5f, 1f) };
            for (int i = 0; i < 110; i++)
            {
                Vector3 dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 0.9f + 0.15f; dir.z *= 0.6f;
                Color c = i % 3 == 0 ? Color.white : i % 3 == 1 ? elCol : (shimmer ? new Color(1f, 0.85f, 0.35f) : rarityCol);
                Add(new Vector3(0f, 0.65f, 0f), dir * (2f + Rand() * 3.2f), c, 0.9f + Rand() * 0.8f, 0.09f + Rand() * 0.09f, 2.5f);
            }
            for (int i = 0; i < 50; i++)
            {
                Vector3 dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) + 0.6f; dir.z *= 0.5f;
                Add(new Vector3(0f, 0.9f, 0f), dir * (1.5f + Rand() * 2f), confetti[i % confetti.Length], 1.6f + Rand(), 0.06f + Rand() * 0.04f, 3.2f);
            }
            if (shimmer) for (int i = 0; i < 60; i++)
            {
                Vector3 dir = Random.onUnitSphere; dir.z *= 0.5f;
                Add(new Vector3(0f, 0.7f, 0f), dir * (0.6f + Rand() * 1.4f), new Color(1f, 0.88f, 0.45f), 1.8f + Rand() * 1.2f, 0.07f + Rand() * 0.06f, -0.2f);
            }
            for (int i = 0; i < 22; i++)
            {
                Vector3 p = SeamPoint(out var n);
                AddShard(p, n * (1.5f + Rand() * 2.5f) + Vector3.up * (1.5f + Rand() * 2f), 0.05f + Rand() * 0.05f, Rand() < 0.65f ? shellCol : spotCol);
            }
        }

        // ============================================================ RENDER
        public void Render()
        {
            if (!root.activeSelf) return;
            var sun = RenderSettings.sun;
            bool fog = RenderSettings.fog;
            var probe = RenderSettings.ambientProbe;
            Quaternion sr = Quaternion.identity; float si = 0f; Color scol = Color.white; LightShadows ss = LightShadows.None;
            if (sun != null) { sr = sun.transform.rotation; si = sun.intensity; scol = sun.color; ss = sun.shadows; sun.transform.rotation = Quaternion.Euler(38f, 165f, 0f); sun.intensity = 1.15f; sun.color = Color.white; sun.shadows = LightShadows.None; }
            RenderSettings.fog = false;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(0.52f, 0.52f, 0.6f));
            sh.AddDirectionalLight(Vector3.up, new Color(0.25f, 0.25f, 0.3f), 1f);
            RenderSettings.ambientProbe = sh;

            var req = new UniversalRenderPipeline.SingleCameraRequest { destination = Texture };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else cam.Render();

            RenderSettings.fog = fog;
            RenderSettings.ambientProbe = probe;
            if (sun != null) { sun.transform.rotation = sr; sun.intensity = si; sun.color = scol; sun.shadows = ss; }
        }

        // ============================================================ EFFECTS
        float Rand() => (float)rnd.NextDouble();

        void Add(Vector3 p, Vector3 v, Color c, float life, float size, float grav)
        {
            if (sparks.Count >= 400) return;
            sparks.Add(new Spark { p = p, v = v, c = c, life = life, size = size, grav = grav });
        }

        void AddShard(Vector3 p, Vector3 v, float size, Color c)
        {
            if (shards.Count >= 60) return;
            shards.Add(new Shard { p = p, v = v, axis = Random.onUnitSphere, spin = 300f + Rand() * 500f, size = size, c = c, life = 1.4f + Rand() * 0.6f });
        }

        /// <summary>A point on the crack (stage space) and its outward direction.</summary>
        Vector3 SeamPoint(out Vector3 n)
        {
            float th = Rand() * Mathf.PI * 2f;
            float y = Crack(th);
            float rho = Rho(y) * eggRoot.localScale.x;
            n = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
            if (n.z < -0.2f) n.z = -n.z;   // mostly toward the camera, where they can be seen
            return eggRoot.localRotation * (n * rho + Vector3.up * (y + 1f) * EggH * 0.5f * eggRoot.localScale.x);
        }

        void SeamSpark(float speed)
        {
            Vector3 p = SeamPoint(out var n);
            Add(p, (n + Vector3.up * 0.4f) * speed * (0.5f + Rand()), Rand() < 0.5f ? Color.white : rarityCol, 0.5f + Rand() * 0.4f, 0.05f + Rand() * 0.05f, 1.5f);
        }

        void TickFx(float dt)
        {
            Vector3 right = cam.transform.right, up = cam.transform.up;
            fv.Clear(); fc.Clear(); fu.Clear(); ft.Clear();
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i];
                s.age += dt;
                if (s.age >= s.life) { sparks.RemoveAt(i); continue; }
                s.v += Vector3.down * s.grav * dt;
                s.v *= 1f - Mathf.Min(1f, dt * 0.9f);
                s.p += s.v * dt;
                sparks[i] = s;
                float k = s.age / s.life;
                float a = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                float size = s.size * (0.6f + 0.6f * a);
                int b = fv.Count;
                fv.Add(s.p - right * size - up * size); fv.Add(s.p - right * size + up * size); fv.Add(s.p + right * size + up * size); fv.Add(s.p + right * size - up * size);
                var c = s.c; c.a = a;
                fc.Add(c); fc.Add(c); fc.Add(c); fc.Add(c);
                fu.Add(new Vector2(0, 0)); fu.Add(new Vector2(0, 1)); fu.Add(new Vector2(1, 1)); fu.Add(new Vector2(1, 0));
                ft.Add(b); ft.Add(b + 1); ft.Add(b + 2); ft.Add(b); ft.Add(b + 2); ft.Add(b + 3);
            }
            fxMesh.Clear();
            fxMesh.SetVertices(fv); fxMesh.SetColors(fc); fxMesh.SetUVs(0, fu); fxMesh.SetTriangles(ft, 0);
            fxMesh.bounds = new Bounds(Vector3.up, Vector3.one * 30f);

            sv.Clear(); sc.Clear(); sn.Clear(); st.Clear();
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                var s = shards[i];
                s.age += dt;
                if (s.age >= s.life) { shards.RemoveAt(i); continue; }
                s.v += Vector3.down * 9f * dt;
                s.p += s.v * dt;
                if (s.p.y < 0f) { s.p.y = 0f; s.v = new Vector3(s.v.x * 0.5f, -s.v.y * 0.3f, s.v.z * 0.5f); s.spin *= 0.6f; }
                s.ang += s.spin * dt;
                shards[i] = s;
                float sz = s.size * Mathf.Clamp01((s.life - s.age) / 0.3f);
                var q = Quaternion.AngleAxis(s.ang, s.axis);
                Vector3 a = s.p + q * new Vector3(0, sz, 0), b2 = s.p + q * new Vector3(-sz * 0.9f, -sz * 0.6f, 0), c2 = s.p + q * new Vector3(sz * 0.9f, -sz * 0.4f, 0);
                Vector3 n = q * Vector3.forward;
                int bi = sv.Count;
                sv.Add(a); sv.Add(b2); sv.Add(c2); sv.Add(a); sv.Add(c2); sv.Add(b2);
                for (int j = 0; j < 3; j++) { sc.Add(s.c); sn.Add(n); }
                for (int j = 0; j < 3; j++) { sc.Add(s.c); sn.Add(-n); }
                for (int j = 0; j < 6; j++) st.Add(bi + j);
            }
            shardMesh.Clear();
            shardMesh.SetVertices(sv); shardMesh.SetColors(sc); shardMesh.SetNormals(sn); shardMesh.SetTriangles(st, 0);
            shardMesh.bounds = new Bounds(Vector3.up, Vector3.one * 30f);
        }

        static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x -= 1f; return 1f + c3 * x * x * x + c1 * x * x; }

        // ============================================================ MESHES
        static float Rho(float y) => Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)) * (1f - 0.13f * y) * EggR;   // wider at the bottom

        /// <summary>Height of the zig-zag crack (-1 bottom .. 1 top) at an angle round the egg.</summary>
        static float Crack(float theta)
        {
            const int teeth = 9;
            float f = theta / (Mathf.PI * 2f) * teeth;
            float tri = Mathf.Abs(f - Mathf.Floor(f) - 0.5f) * 4f - 1f;
            return 0.12f + 0.11f * tri;
        }

        void BuildEgg(int seed, out Mesh bottomMesh, out Mesh topMesh)
        {
            var r = new System.Random(seed);
            var spots = new List<Vector3>();
            int n = 6 + r.Next(5);
            for (int i = 0; i < n; i++)
            {
                var v = new Vector3((float)r.NextDouble() * 2f - 1f, (float)r.NextDouble() * 2f - 1f, (float)r.NextDouble() * 2f - 1f);
                if (v.sqrMagnitude < 0.01f) v = Vector3.up;
                spots.Add(v.normalized);
            }
            Color inner = Color.Lerp(Color.white, rarityCol, 0.35f); inner.a = 0f;   // self-lit: light inside the egg
            bottomBuild(true, out bottomMesh);
            bottomBuild(false, out topMesh);

            void bottomBuild(bool isBottom, out Mesh m)
            {
                const int Lon = 30, Lat = 10;
                var v = new List<Vector3>(); var c = new List<Color>(); var nn = new List<Vector3>(); var t = new List<int>();
                // outer and inner shells
                for (int layer = 0; layer < 2; layer++)
                {
                    float shrink = layer == 0 ? 1f : 0.93f;
                    int b0 = v.Count;
                    for (int i = 0; i <= Lon; i++)
                    {
                        float th = i / (float)Lon * Mathf.PI * 2f;
                        float yc = Crack(th);
                        float phiC = Mathf.Acos(yc);
                        for (int j = 0; j <= Lat; j++)
                        {
                            float u = j / (float)Lat;
                            float phi = isBottom ? Mathf.Lerp(Mathf.PI, phiC, u) : Mathf.Lerp(phiC, 0f, u);
                            float y = Mathf.Cos(phi);
                            float rho = Rho(y) * shrink;
                            var p = new Vector3(Mathf.Cos(th) * rho, (y + 1f) * EggH * 0.5f * (layer == 0 ? 1f : 0.97f) + (layer == 0 ? 0f : EggH * 0.015f), Mathf.Sin(th) * rho);
                            var dir = new Vector3(Mathf.Cos(th) * Mathf.Sin(phi), y, Mathf.Sin(th) * Mathf.Sin(phi));
                            var norm = new Vector3(dir.x / EggR, dir.y / (EggH * 0.5f), dir.z / EggR).normalized;
                            v.Add(p);
                            nn.Add(layer == 0 ? norm : -norm);
                            if (layer == 1) c.Add(inner);
                            else
                            {
                                Color col = shellCol;
                                foreach (var s in spots) { float dd = Vector3.Dot(dir.normalized, s); if (dd > 0.93f) { col = spotCol; break; } if (dd > 0.9f) col = Color.Lerp(shellCol, spotCol, 0.5f); }
                                if (shimmer && col == spotCol) col.a = 0f;   // gold spots that glow
                                c.Add(col);
                            }
                        }
                    }
                    for (int i = 0; i < Lon; i++)
                        for (int j = 0; j < Lat; j++)
                        {
                            int a = b0 + i * (Lat + 1) + j, b = a + Lat + 1;
                            bool flip = layer == 1;   // both halves run upward in j, so only the inside is turned round
                            // winding: outer faces out
                            if (!flip) { t.Add(a); t.Add(a + 1); t.Add(b); t.Add(b); t.Add(a + 1); t.Add(b + 1); }
                            else { t.Add(a); t.Add(b); t.Add(a + 1); t.Add(b); t.Add(b + 1); t.Add(a + 1); }
                        }
                }
                // the rim between them along the crack: glowing edge
                int outerStart = 0, innerStart = (Lon + 1) * (Lat + 1);
                int rimJ = isBottom ? Lat : 0;
                int rb = v.Count;
                for (int i = 0; i <= Lon; i++)
                {
                    int o = outerStart + i * (Lat + 1) + rimJ, n2 = innerStart + i * (Lat + 1) + rimJ;
                    v.Add(v[o]); c.Add(inner); nn.Add(isBottom ? Vector3.up : Vector3.down);
                    v.Add(v[n2]); c.Add(inner); nn.Add(isBottom ? Vector3.up : Vector3.down);
                }
                for (int i = 0; i < Lon; i++)
                {
                    int a = rb + i * 2, b = a + 2;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                    t.Add(a); t.Add(a + 1); t.Add(b); t.Add(a + 1); t.Add(b + 1); t.Add(b);   // both sides: the rim is thin and seen from above and below
                }
                m = new Mesh { name = isBottom ? "EggBottom" : "EggTop" };
                m.SetVertices(v); m.SetColors(c); m.SetNormals(nn); m.SetTriangles(t, 0);
                m.RecalculateBounds();
            }
        }

        /// <summary>A fan of soft light rays (additive, vertex colours: bright centre, dark tips) behind the egg.</summary>
        static Mesh BuildRays(Color col, float alpha)
        {
            const int N = 16;
            var v = new List<Vector3>(); var c = new List<Color>(); var u = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i < N; i++)
            {
                float a = i / (float)N * Mathf.PI * 2f, w = (i % 2 == 0 ? 0.075f : 0.045f);
                float len = i % 2 == 0 ? 3.4f : 2.4f;
                int b = v.Count;
                v.Add(Vector3.zero);
                v.Add(new Vector3(Mathf.Cos(a - w), Mathf.Sin(a - w), 0f) * len);
                v.Add(new Vector3(Mathf.Cos(a + w), Mathf.Sin(a + w), 0f) * len);
                c.Add(new Color(col.r, col.g, col.b, alpha)); c.Add(new Color(col.r, col.g, col.b, 0f)); c.Add(new Color(col.r, col.g, col.b, 0f));
                u.Add(new Vector2(0.5f, 0.5f)); u.Add(new Vector2(0.5f, 0.5f)); u.Add(new Vector2(0.5f, 0.5f));
                t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 1);
            }
            var m = new Mesh { name = "HatchRays" };
            m.SetVertices(v); m.SetColors(c); m.SetUVs(0, u); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
