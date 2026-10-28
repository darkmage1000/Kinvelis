using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wildbond
{
    /// <summary>
    /// Inventory icons rendered at startup from procedural 3D item models (same style/material as the world).
    /// Transparent backgrounds via black/white difference matting, so it works regardless of URP alpha settings.
    /// New items get an icon automatically (fallback: a tinted generic model).
    /// </summary>
    public static class ItemIcons
    {
        const int Size = 192;
        const int Layer = 31;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Sprite Get(string id) => id != null && sprites.TryGetValue(id, out var s) ? s : null;

        /// <summary>The key of an evolved form's portrait (kin with a real evolution: "kin_id_f1" / "_f2").</summary>
        public static string FormKey(string speciesId, int stage) => $"kin_{speciesId}_f{stage}";
        /// <summary>This kin as it looks now: its evolved form's portrait if it has one, else its kind's.</summary>
        public static Sprite KinPortrait(CreatureData d)
        {
            if (d == null || d.Species == null) return null;
            if (d.stage > 0 && d.Species.FormAt(d.stage) != null) { var f = Get(FormKey(d.Species.id, d.stage)); if (f != null) return f; }
            return Get("kin_" + d.Species.id);
        }

        public static void RenderAll(Light sun)
        {
            // icons survive scene changes (menu <-> world); only render once per app run
            if (sprites.Count > 0)
            {
                bool valid = true;
                foreach (var s in sprites.Values) if (s == null || s.texture == null) { valid = false; break; }
                if (!valid) sprites.Clear();
                else
                {
                    // everything is there unless a kin was made since (the kin maker): then only the missing ones are drawn
                    bool missing = false;
                    foreach (var sp in ContentDB.SpeciesList)
                    {
                        if (!sprites.ContainsKey("kin_" + sp.id)) { missing = true; break; }
                        for (int st = 1; st <= 2; st++) if (sp.FormAt(st) != null && !sprites.ContainsKey(FormKey(sp.id, st))) missing = true;
                        if (missing) break;
                    }
                    if (!missing) return;
                }
            }
            // isolated, predictable lighting for the icon pass (EnvironmentSystem takes over afterwards)
            bool fog = RenderSettings.fog;
            var probe = RenderSettings.ambientProbe;
            var sunRot = sun.transform.rotation;
            float sunInt = sun.intensity;
            Color sunCol = sun.color;
            var sunShadows = sun.shadows;
            RenderSettings.fog = false;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(0.55f, 0.56f, 0.6f));
            sh.AddDirectionalLight(Vector3.up, new Color(0.25f, 0.25f, 0.28f), 1f);
            RenderSettings.ambientProbe = sh;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.intensity = 1.15f;
            sun.color = Color.white;
            sun.shadows = LightShadows.None;

            var camGo = new GameObject("IconCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 50f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            var extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = false;
            extra.renderShadows = false;

            Vector3 viewDir = new Vector3(0.55f, -0.62f, 1f).normalized; // camera looks down-forward (3/4 view)
            Vector3 origin = new Vector3(0f, -4000f, 0f);
            camGo.transform.SetPositionAndRotation(origin - viewDir * 10f, Quaternion.LookRotation(viewDir));

            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            rt.Create();
            cam.targetTexture = rt;
            var black = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var white = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

            var holder = new GameObject("IconModel");
            holder.layer = Layer;
            var mf = holder.AddComponent<MeshFilter>();
            var mr = holder.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            // render jobs: every item + every building (key "bld_<id>")
            var jobs = new List<(string key, Mesh mesh, bool longItem)>();
            foreach (var kv in ContentDB.Items)
            {
                if (sprites.ContainsKey(kv.Key)) continue;
                var m = ItemModels.Build(kv.Value);
                if (m != null) jobs.Add((kv.Key, m, kv.Value.category == ItemCategory.Tool || kv.Value.category == ItemCategory.Weapon));
            }
            foreach (var bd in ContentDB.BuildingList)
                if (!sprites.ContainsKey("bld_" + bd.id)) jobs.Add(("bld_" + bd.id, ModelBuilder.BuildStatic(bd.parts, bd.palette, "bldicon_" + bd.id), false));
            // house pieces (key "hp_<id>"): the build bar on the placing screen shows them
            foreach (var hp in Housing.Pieces)
            {
                if (sprites.ContainsKey("hp_" + hp.id)) continue;
                var hm = Housing.MeshFor(hp);
                if (hm != null && hm.vertexCount > 0) jobs.Add(("hp_" + hp.id, hm, false));
            }
            foreach (var sp in ContentDB.SpeciesList)
                if (sp.parts != null && sp.parts.Length > 0 && !sprites.ContainsKey("kin_" + sp.id))
                    jobs.Add(("kin_" + sp.id, ModelBuilder.BuildStatic(sp.parts, sp.palette, "kinicon_" + sp.id), false));
            // evolved forms have bodies of their own (Real evolutions): a portrait each, for kin cards and the wiki
            foreach (var sp in ContentDB.SpeciesList)
                for (int st = 1; st <= 2; st++)
                {
                    var f = sp.FormAt(st);
                    string key = FormKey(sp.id, st);
                    if (f == null || f.parts == null || f.parts.Length == 0 || sprites.ContainsKey(key)) continue;
                    jobs.Add((key, ModelBuilder.BuildStatic(f.parts, f.ownPalette ? f.palette : sp.palette, "kinicon_" + key), false));
                }

            foreach (var job in jobs)
            {
                var mesh = job.mesh;
                mf.sharedMesh = mesh;
                bool longItem = job.longItem;
                // long items stand diagonally across the icon; others get a gentle turn; kin are turned to face the
                // camera (their models look down +z, which is the way the camera looks, so unturned they show their backs)
                Quaternion rot = longItem
                    ? Quaternion.AngleAxis(-42f, viewDir) * Quaternion.LookRotation(Vector3.up, Vector3.Cross(Vector3.up, viewDir).normalized)
                    : Quaternion.Euler(0f, job.key.StartsWith("kin_") ? 180f : -20f, 0f);
                holder.transform.rotation = rot;
                // center + fit
                var b = mesh.bounds;
                holder.transform.position = origin - rot * b.center;
                float ext = 0f;
                Vector3 e = b.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    Vector3 v = camGo.transform.InverseTransformDirection(rot * corner);
                    ext = Mathf.Max(ext, Mathf.Abs(v.x), Mathf.Abs(v.y));
                }
                cam.orthographicSize = ext * 1.12f;

                Render(cam, rt, Color.black, black);
                Render(cam, rt, Color.white, white);
                sprites[job.key] = Compose(black, white, job.key);
            }

            Object.Destroy(holder);
            cam.targetTexture = null;
            Object.Destroy(camGo);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(black);
            Object.Destroy(white);

            RenderSettings.fog = fog;
            RenderSettings.ambientProbe = probe;
            sun.transform.rotation = sunRot;
            sun.intensity = sunInt;
            sun.color = sunCol;
            sun.shadows = sunShadows;
        }

        static void Render(Camera cam, RenderTexture rt, Color bg, Texture2D into)
        {
            cam.backgroundColor = bg;
            var req = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            into.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false);
            into.Apply(false);
            RenderTexture.active = prev;
        }

        /// <summary>Difference matting: alpha = 1 - (white - black); color = black / alpha.</summary>
        static Sprite Compose(Texture2D black, Texture2D white, string id)
        {
            var b = black.GetPixels32();
            var w = white.GetPixels32();
            var o = new Color32[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                int diff = ((w[i].r - b[i].r) + (w[i].g - b[i].g) + (w[i].b - b[i].b)) / 3;
                int a = Mathf.Clamp(255 - diff, 0, 255);
                if (a < 3) { o[i] = new Color32(0, 0, 0, 0); continue; }
                float inv = 255f / a;
                o[i] = new Color32((byte)Mathf.Min(255, b[i].r * inv), (byte)Mathf.Min(255, b[i].g * inv), (byte)Mathf.Min(255, b[i].b * inv), (byte)a);
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "icon_" + id, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels32(o);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>Procedural 3D models for inventory items (icons). Low-poly, vertex colored.</summary>
    public static class ItemModels
    {
        static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Matrix4x4 M(float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(px, py, pz), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        public static Mesh Build(ItemDef d)
        {
            if (d.category == ItemCategory.Tool || d.category == ItemCategory.Weapon) return HeldItemMesh.Build(d.id);
            if (d.category == ItemCategory.Gear) return GearModels.Icon(d);
            var mb = new MeshBuilder();
            switch (d.id)
            {
                case "wood":
                    Log(mb, 0f, 0f, 0f);
                    Log(mb, 0.36f, 0f, 0.05f);
                    Log(mb, 0.18f, 0.3f, -0.02f);
                    break;
                case "stone":
                    mb.Rock(M(0, 0.3f, 0, 0.8f, 0.6f, 0.7f), C("#8a8a94"), C("#b4b4bc"), 3u, 7, 5);
                    mb.Rock(M(0.45f, 0.15f, 0.2f, 0.4f, 0.3f, 0.35f), C("#7c7c86"), C("#a8a8b0"), 9u, 6, 4);
                    break;
                case "fiber":
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * 51f;
                        mb.Cylinder(M(Mathf.Cos(a * Mathf.Deg2Rad) * 0.06f, 0.5f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.06f, 0.07f, 1f, 0.03f, (i - 3) * 5f, a, (i - 3) * 4f), C("#5f9a36"), C("#c4ea7a"), 4, 0.2f, false);
                    }
                    mb.Cylinder(M(0, 0.42f, 0, 0.2f, 0.08f, 0.2f), C("#b88a4a"), 8, 1f, true);
                    break;
                case "ore":
                    mb.Rock(M(0, 0.32f, 0, 0.85f, 0.65f, 0.75f), C("#6e625c"), C("#8e7e76"), 5u, 7, 5);
                    mb.Rock(M(0.18f, 0.55f, 0.1f, 0.3f, 0.24f, 0.3f), C("#d0843e"), C("#f0a860"), 7u, 5, 4);
                    mb.Rock(M(-0.2f, 0.45f, 0.22f, 0.24f, 0.2f, 0.24f), C("#d0843e"), C("#f0a860"), 11u, 5, 4);
                    break;
                case "ingot":
                    mb.Cylinder(M(0, 0.12f, 0, 0.9f, 0.24f, 0.42f, 0, 0, 0), C("#6c7288"), C("#b4bacc"), 4, 0.78f, true);
                    mb.Cylinder(M(0.08f, 0.36f, 0.02f, 0.9f, 0.24f, 0.42f, 0, 12, 0), C("#747a90"), C("#c4c9d8"), 4, 0.78f, true);
                    break;
                case "crystal":
                    mb.Crystal(M(0, 0, 0, 0.42f, 1f, 0.42f), C("#2fa8e8"), C("#bff0ff"));
                    mb.Crystal(M(0.22f, 0, 0.05f, 0.28f, 0.62f, 0.28f, 0, 0, -22), C("#46c4f0"), C("#d8f8ff"));
                    mb.Crystal(M(-0.2f, 0, -0.04f, 0.24f, 0.5f, 0.24f, 0, 0, 26), C("#5a8ef0"), C("#d0e4ff"));
                    break;
                case "prism_shard":
                    mb.Crystal(M(0, 0, 0, 0.4f, 1f, 0.4f), C("#ff7ad0"), C("#ff4ab8"));
                    mb.Crystal(M(0.22f, 0, 0.05f, 0.28f, 0.66f, 0.28f, 0, 0, -24), C("#7ae8ff"), C("#2ac8ff"));
                    mb.Crystal(M(-0.2f, 0, -0.04f, 0.26f, 0.56f, 0.26f, 0, 0, 26), C("#ffe46a"), C("#ffc81e"));
                    break;
                case "shimmer_prism":
                    mb.Cylinder(M(0, 0.06f, 0, 0.62f, 0.12f, 0.62f), C("#d8b04a"), 10, 0.85f, true);
                    mb.Crystal(M(0, 0.1f, 0, 0.56f, 0.95f, 0.56f), C("#f4f0ff"), C("#ffffff"));
                    mb.Crystal(M(0.26f, 0.1f, 0.06f, 0.2f, 0.5f, 0.2f, 0, 0, -30), C("#ff7ad0"), C("#ff4ab8"));
                    mb.Crystal(M(-0.26f, 0.1f, -0.04f, 0.2f, 0.5f, 0.2f, 0, 0, 30), C("#7ae8ff"), C("#2ac8ff"));
                    mb.Crystal(M(0, 0.1f, 0.26f, 0.18f, 0.44f, 0.18f, -30, 0, 0), C("#ffe46a"), C("#ffc81e"));
                    break;
                case "cinder":
                    mb.Crystal(M(0, 0, 0, 0.5f, 0.9f, 0.5f, 0, 0, 8), C("#3a2e2e"), C("#ff7a2a"));
                    mb.Crystal(M(0.25f, 0, 0.1f, 0.3f, 0.55f, 0.3f, 0, 0, -25), C("#4a3030"), C("#ffb03a"));
                    mb.Sphere(M(0.02f, 0.18f, 0, 0.36f, 0.12f, 0.36f), C("#ff6a1a"), 8, 4);
                    break;
                case "pelt":
                    mb.Sphere(M(0, 0.28f, 0, 0.75f, 0.5f, 0.6f), C("#f6ecd4"), C("#d8c8a8"), 10, 7);
                    mb.Sphere(M(0.3f, 0.42f, 0.05f, 0.45f, 0.4f, 0.42f), C("#fff6e2"), C("#e0d0b0"), 9, 6);
                    mb.Sphere(M(-0.28f, 0.4f, -0.05f, 0.42f, 0.36f, 0.4f), C("#fff2dc"), C("#d8c8a8"), 9, 6);
                    break;
                case "gel":
                    mb.Sphere(M(0, 0.3f, 0, 0.8f, 0.6f, 0.8f), C("#58c8ff"), C("#2a86e0"), 12, 8);
                    mb.Sphere(M(0.16f, 0.5f, 0.16f, 0.16f, 0.1f, 0.16f), C("#ffffff"), 8, 5);
                    break;
                case "berry":
                    mb.Sphere(M(-0.18f, 0.22f, 0, 0.42f, 0.42f, 0.42f), C("#ff4a6a"), C("#c02a4a"), 10, 7);
                    mb.Sphere(M(0.2f, 0.22f, 0.06f, 0.42f, 0.42f, 0.42f), C("#ff5a7a"), C("#c02a4a"), 10, 7);
                    mb.Sphere(M(0f, 0.5f, -0.04f, 0.4f, 0.4f, 0.4f), C("#ff6a8a"), C("#d03a5a"), 10, 7);
                    mb.Sphere(M(0.05f, 0.76f, 0f, 0.3f, 0.08f, 0.16f, 0, 0, -20), C("#4fae3a"), 8, 4);
                    break;
                case "goldgrain":
                    for (int i = -1; i <= 1; i++)
                    {
                        mb.Cylinder(M(i * 0.22f, 0.3f, 0, 0.05f, 0.6f, 0.05f, 0, 0, i * -12), C("#c8a83a"), 5, 1f, true);
                        mb.Sphere(M(i * 0.28f, 0.72f, 0, 0.18f, 0.44f, 0.18f, 0, 0, i * -12), C("#f0c84a"), C("#d8a030"), 8, 6);
                    }
                    break;
                case "dewmelon":
                    mb.Sphere(M(0, 0.36f, 0, 0.9f, 0.72f, 0.9f), C("#7ad0a0"), C("#4aa878"), 12, 8);
                    for (int i = 0; i < 4; i++) mb.Sphere(M(0, 0.36f, 0, 0.92f, 0.74f, 0.1f, 0, i * 45f, 0), C("#3f8a5a"), 10, 6);
                    mb.Cylinder(M(0, 0.76f, 0, 0.06f, 0.12f, 0.06f), C("#6a4a30"), 5, 1f, true);
                    break;
                case "emberpepper":
                    mb.Cylinder(M(0, 0.36f, 0, 0.36f, 0.72f, 0.36f, 180, 0, 12), C("#e84a2a"), C("#ff7a3a"), 8, 0f, true);
                    mb.Cylinder(M(0.02f, 0.78f, 0, 0.1f, 0.14f, 0.1f), C("#4f9a3a"), 5, 1f, true);
                    break;
                case "frostmint":
                    mb.Cylinder(M(0, 0.3f, 0, 0.05f, 0.6f, 0.05f), C("#6ab8a8"), 5, 1f, true);
                    mb.Sphere(M(0.2f, 0.4f, 0, 0.4f, 0.08f, 0.24f, 0, 0, 25), C("#9ae0e8"), C("#6ab8c8"), 8, 4);
                    mb.Sphere(M(-0.2f, 0.52f, 0, 0.4f, 0.08f, 0.24f, 0, 0, -25), C("#9ae0e8"), C("#6ab8c8"), 8, 4);
                    mb.Sphere(M(0, 0.7f, 0, 0.3f, 0.08f, 0.2f), C("#c8f4f8"), 8, 4);
                    break;
                case "silverfin":
                case "sunfin":
                case "reef_snapper":
                case "mirefish":
                case "goldscale":
                    // a fish in the item's colour
                    mb.Sphere(M(0, 0.4f, 0, 0.9f, 0.5f, 0.3f), d.iconColor, Color.Lerp(d.iconColor, Color.white, 0.6f), 10, 6);
                    mb.Cone(M(-0.6f, 0.4f, 0, 0.5f, 0.36f, 0.12f, 0, 0, -90f), Color.Lerp(d.iconColor, Color.black, 0.2f), 4);
                    mb.Cone(M(0.05f, 0.7f, 0, 0.3f, 0.24f, 0.08f), Color.Lerp(d.iconColor, Color.black, 0.2f), 4);
                    mb.Sphere(M(0.3f, 0.46f, 0.14f, 0.09f, 0.09f, 0.05f), C("#1a1a1a"), 5, 4);
                    break;
                case "fishing_rod":
                    mb.Cylinder(M(0, 0.45f, 0, 0.05f, 1f, 0.05f, 0, 0, -32f), C("#8a5a32"), C("#c89a5a"), 5, 0.5f, true);
                    mb.Box(M(0.36f, 0.55f, 0, 0.015f, 0.6f, 0.015f), C("#e8e8e0"));
                    mb.Sphere(M(0.36f, 0.24f, 0, 0.14f, 0.14f, 0.14f), C("#f03a3a"), C("#ffffff"), 6, 5);
                    mb.Cylinder(M(-0.2f, 0.16f, 0.04f, 0.14f, 0.06f, 0.14f, 90f, 0, 0), C("#6a6a70"), 6);
                    break;
                case "grilled_fish":
                    mb.Cylinder(M(0, 0.4f, 0, 0.04f, 1f, 0.04f, 0, 0, 70f), C("#8a5a32"), 4);
                    mb.Sphere(M(-0.14f, 0.36f, 0, 0.5f, 0.26f, 0.2f, 0, 0, -20f), C("#d8a860"), C("#b07a3a"), 8, 5);
                    mb.Sphere(M(0.2f, 0.48f, 0, 0.46f, 0.24f, 0.2f, 0, 0, -20f), C("#e0b470"), C("#b07a3a"), 8, 5);
                    break;
                case "fishers_stew":
                case "reef_platter":
                case "mire_broth":
                case "golden_feast":
                    mb.Cylinder(M(0, 0.2f, 0, 0.9f, 0.4f, 0.9f, 180, 0, 0), C("#b08a60"), C("#8a6a48"), 12, 0.6f, true);
                    mb.Cylinder(M(0, 0.4f, 0, 0.8f, 0.04f, 0.8f), d.iconColor, 12, 1f, true);
                    mb.Sphere(M(0.1f, 0.46f, 0f, 0.4f, 0.12f, 0.16f, 0, 30f, 0), Color.Lerp(d.iconColor, Color.white, 0.5f), 6, 3);
                    break;
                case "honeybloom":
                    mb.Cylinder(M(0, 0.25f, 0, 0.06f, 0.5f, 0.06f), C("#4f9a3a"), 5, 1f, true);
                    for (int i = 0; i < 8; i++)
                        mb.Sphere(M(Mathf.Cos(i * 0.785f) * 0.3f, 0.6f, Mathf.Sin(i * 0.785f) * 0.3f, 0.34f, 0.08f, 0.2f, 0, -i * 45f, 16), C("#ffd23a"), C("#f0a020"), 6, 3);
                    mb.Sphere(M(0, 0.66f, 0, 0.3f, 0.22f, 0.3f), C("#b8701a"), C("#8a4a10"), 8, 5);
                    break;
                case "obsidian":
                    mb.Crystal(M(0, 0.05f, 0, 0.5f, 0.9f, 0.4f, 0, 20, 12), C("#17141f"), C("#6a5a9a"));
                    mb.Crystal(M(0.3f, 0.05f, 0.1f, 0.3f, 0.5f, 0.3f, 0, 0, -28), C("#1c1826"), C("#7a6ab0"));
                    break;
                case "frostcap":
                    mb.Cylinder(M(0, 0.25f, 0, 0.2f, 0.5f, 0.2f), C("#dff4ff"), 6, 0.8f, true);
                    mb.Dome(M(0, 0.48f, 0, 0.8f, 0.5f, 0.8f), C("#7ac8ff"), 10, 4);
                    mb.Sphere(M(0, 0.7f, 0, 0.3f, 0.1f, 0.3f), C("#ffffff"), 6, 3);
                    break;
                case "sea_pearl":
                    mb.Dome(M(0, 0.05f, 0, 0.9f, 0.26f, 0.7f), C("#8a7a8a"), 10, 3);
                    mb.Sphere(M(0, 0.4f, 0, 0.5f, 0.5f, 0.5f), C("#fffaf0"), C("#f0d8e8"), 12, 8);
                    break;
                case "lanternbud":
                    mb.Cylinder(M(0, 0.2f, 0, 0.06f, 0.4f, 0.06f), C("#3f7a4a"), 5, 1f, true);
                    mb.Sphere(M(0, 0.55f, 0, 0.5f, 0.62f, 0.5f), C("#fff09a"), C("#ffb84a"), 10, 7);
                    mb.Cone(M(0, 0.9f, 0, 0.26f, 0.22f, 0.26f), C("#3f7a4a"), 6);
                    break;
                case "sunfig":
                    mb.Sphere(M(0, 0.36f, 0, 0.66f, 0.72f, 0.66f), C("#ffb03a"), C("#c85a2a"), 10, 7);
                    mb.Cylinder(M(0, 0.74f, 0, 0.06f, 0.14f, 0.06f), C("#5a4a2a"), 5, 1f, true);
                    mb.Sphere(M(0.16f, 0.78f, 0, 0.3f, 0.06f, 0.18f, 0, 0, -20), C("#5a8a3a"), 6, 3);
                    break;
                case "sporecap":
                    mb.Cylinder(M(0, 0.22f, 0, 0.2f, 0.44f, 0.2f), C("#d8c8e0"), 6, 0.8f, true);
                    mb.Dome(M(0, 0.42f, 0, 0.84f, 0.5f, 0.84f), C("#c04ad0"), 10, 4);
                    for (int i = 0; i < 5; i++) mb.Sphere(M(Mathf.Cos(i * 1.256f) * 0.22f, 0.6f, Mathf.Sin(i * 1.256f) * 0.22f, 0.13f, 0.09f, 0.13f), C("#f4e8a0"), 5, 3);
                    break;
                case "starmoss":
                    mb.Sphere(M(0, 0.25f, 0, 0.9f, 0.5f, 0.8f), C("#2ad0b0"), C("#1a8a80"), 10, 6);
                    for (int i = 0; i < 6; i++) mb.Sphere(M(Mathf.Cos(i * 1.05f) * 0.26f, 0.46f + (i % 2) * 0.06f, Mathf.Sin(i * 1.05f) * 0.26f, 0.1f, 0.1f, 0.1f), C("#ffffff"), 5, 3);
                    break;
                case "honey_cake":
                    mb.Cylinder(M(0, 0.2f, 0, 0.9f, 0.4f, 0.9f), C("#e0a850"), C("#f4d080"), 12, 1f, true);
                    mb.Cylinder(M(0, 0.42f, 0, 0.94f, 0.06f, 0.94f), C("#ffc83a"), 12, 1f, true);
                    mb.Sphere(M(0, 0.5f, 0, 0.2f, 0.12f, 0.2f), C("#ffd23a"), 6, 4);
                    break;
                case "frostcap_broth":
                case "sunfig_jam":
                case "lantern_tea":
                case "sporecap_draught":
                    // a bowl or a jar in the item's colour
                    mb.Cylinder(M(0, 0.2f, 0, 0.9f, 0.4f, 0.9f, 180, 0, 0), C("#b08a60"), C("#8a6a48"), 12, 0.6f, true);
                    mb.Cylinder(M(0, 0.4f, 0, 0.8f, 0.04f, 0.8f), d.iconColor, 12, 1f, true);
                    mb.Sphere(M(0.12f, 0.44f, 0.05f, 0.2f, 0.08f, 0.2f), Color.Lerp(d.iconColor, Color.white, 0.5f), 6, 3);
                    break;
                case "grain_loaf":
                    mb.Sphere(M(0, 0.26f, 0, 1.0f, 0.5f, 0.6f), C("#d8a860"), C("#b07a3a"), 12, 7);
                    for (int i = -1; i <= 1; i++) mb.Box(M(i * 0.22f, 0.5f, 0, 0.05f, 0.04f, 0.36f, 0, 20, 0), C("#f0d8a0"));
                    break;
                case "trail_mix":
                    mb.Cylinder(M(0, 0.14f, 0, 0.9f, 0.28f, 0.9f), C("#8a6a48"), C("#a88458"), 12, 1.15f, true);
                    mb.Sphere(M(-0.15f, 0.34f, 0.05f, 0.2f, 0.16f, 0.2f), C("#ff5a7a"), 8, 5);
                    mb.Sphere(M(0.18f, 0.34f, -0.08f, 0.2f, 0.16f, 0.2f), C("#f0c84a"), 8, 5);
                    mb.Sphere(M(0.02f, 0.36f, 0.2f, 0.18f, 0.14f, 0.18f), C("#f0c84a"), 8, 5);
                    mb.Sphere(M(0f, 0.36f, -0.22f, 0.18f, 0.14f, 0.18f), C("#ff5a7a"), 8, 5);
                    break;
                case "melon_bowl":
                    mb.Cylinder(M(0, 0.16f, 0, 0.95f, 0.32f, 0.95f), C("#c8a070"), C("#e0c090"), 12, 1.2f, true);
                    mb.Sphere(M(-0.16f, 0.4f, 0, 0.36f, 0.28f, 0.36f), C("#8ae0b0"), 8, 5);
                    mb.Sphere(M(0.2f, 0.4f, 0.08f, 0.3f, 0.24f, 0.3f), C("#ff5a7a"), 8, 5);
                    mb.Sphere(M(0.04f, 0.42f, -0.2f, 0.3f, 0.24f, 0.3f), C("#7ad0a0"), 8, 5);
                    break;
                case "pepper_skewer":
                    mb.Cylinder(M(0, 0.45f, 0, 0.05f, 1.1f, 0.05f, 0, 0, 55), C("#8a6a48"), 5, 1f, true);
                    for (int i = -1; i <= 1; i++) mb.Sphere(M(i * 0.26f, 0.45f - i * 0.18f, 0, 0.3f, 0.3f, 0.3f), i == 0 ? C("#ff8a3a") : C("#e84a2a"), 8, 6);
                    break;
                case "mint_tea":
                    mb.Cylinder(M(0, 0.3f, 0, 0.6f, 0.6f, 0.6f), C("#e8e0d0"), C("#ffffff"), 12, 1.15f, true);
                    mb.Cylinder(M(0, 0.6f, 0, 0.6f, 0.03f, 0.6f), C("#a8e8e8"), 12, 1f, true);
                    mb.Sphere(M(0.1f, 0.64f, 0, 0.26f, 0.04f, 0.16f), C("#6ab8a8"), 6, 3);
                    mb.Cylinder(M(0.42f, 0.32f, 0, 0.2f, 0.06f, 0.2f, 0, 0, 90), C("#e8e0d0"), 8, 1f, true);
                    break;
                case "forager_salad":
                    mb.Cylinder(M(0, 0.16f, 0, 0.95f, 0.32f, 0.95f), C("#c8a070"), C("#e0c090"), 12, 1.2f, true);
                    for (int i = 0; i < 5; i++) mb.Sphere(M(Mathf.Cos(i * 1.26f) * 0.2f, 0.38f + (i % 2) * 0.06f, Mathf.Sin(i * 1.26f) * 0.2f, 0.4f, 0.08f, 0.26f, 0, i * 72f, 15), i % 2 == 0 ? C("#8ad06a") : C("#9ae0e8"), 6, 3);
                    break;
                case "tamers_tart":
                    mb.Cylinder(M(0, 0.12f, 0, 1f, 0.24f, 1f), C("#c8884a"), C("#e8b070"), 14, 1.1f, true);
                    mb.Dome(M(0, 0.24f, 0, 0.86f, 0.22f, 0.86f), C("#f0a0c0"), 14, 4);
                    mb.Sphere(M(0, 0.4f, 0, 0.22f, 0.1f, 0.22f), C("#9ae0e8"), 8, 4);
                    break;
                case "harvest_stew":
                    mb.Cylinder(M(0, 0.26f, 0, 0.9f, 0.52f, 0.9f), C("#4a4448"), C("#6a6468"), 12, 1.1f, true);
                    mb.Cylinder(M(0, 0.52f, 0, 0.84f, 0.04f, 0.84f), C("#c87a3a"), 12, 1f, true);
                    mb.Sphere(M(-0.15f, 0.56f, 0.05f, 0.2f, 0.1f, 0.2f), C("#f0c84a"), 8, 4);
                    mb.Sphere(M(0.18f, 0.56f, -0.1f, 0.2f, 0.1f, 0.2f), C("#e84a2a"), 8, 4);
                    mb.Sphere(M(0.02f, 0.56f, 0.22f, 0.18f, 0.1f, 0.18f), C("#7ad0a0"), 8, 4);
                    break;
                case "berry_tart":
                    mb.Cylinder(M(0, 0.12f, 0, 1f, 0.24f, 1f), C("#c8884a"), C("#e8b070"), 14, 1.1f, true);
                    mb.Dome(M(0, 0.24f, 0, 0.86f, 0.22f, 0.86f), C("#e0305a"), 14, 4);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 72f * Mathf.Deg2Rad;
                        mb.Sphere(M(Mathf.Cos(a) * 0.25f, 0.33f, Mathf.Sin(a) * 0.25f, 0.14f, 0.14f, 0.14f), C("#ff6a8a"), 6, 4);
                    }
                    break;
                case "salve":
                    mb.Cylinder(M(0, 0.3f, 0, 0.6f, 0.6f, 0.6f), C("#4ad88a"), C("#9affc0"), 12, 0.9f, true);
                    mb.Cylinder(M(0, 0.66f, 0, 0.5f, 0.14f, 0.5f), C("#8a5a30"), C("#a87040"), 12, 1f, true);
                    mb.Box(M(0, 0.3f, 0.3f, 0.3f, 0.22f, 0.02f), C("#fff6e0"));
                    break;
                case "kin_treat":
                    mb.Capsule(M(0, 0.2f, 0, 0.26f, 0.9f, 0.26f, 0, 0, 90), C("#e8b04a"), 8);
                    mb.Sphere(M(0.42f, 0.2f, 0.12f, 0.26f, 0.26f, 0.26f), C("#f0c05a"), 8, 5);
                    mb.Sphere(M(0.42f, 0.2f, -0.12f, 0.26f, 0.26f, 0.26f), C("#f0c05a"), 8, 5);
                    mb.Sphere(M(-0.42f, 0.2f, 0.12f, 0.26f, 0.26f, 0.26f), C("#f0c05a"), 8, 5);
                    mb.Sphere(M(-0.42f, 0.2f, -0.12f, 0.26f, 0.26f, 0.26f), C("#f0c05a"), 8, 5);
                    mb.Sphere(M(0, 0.34f, 0, 0.12f, 0.06f, 0.12f), C("#ff5a7a"), 6, 3);
                    break;
                case "satchel":
                    mb.Box(M(0, 0.35f, 0, 0.8f, 0.62f, 0.38f), C("#8a5a30"), C("#a87040"));
                    mb.Box(M(0, 0.58f, 0.12f, 0.82f, 0.3f, 0.2f, 20, 0, 0), C("#a87040"));
                    mb.Box(M(0, 0.5f, 0.23f, 0.12f, 0.12f, 0.03f), C("#e8c05a"));
                    mb.Cylinder(M(0, 0.78f, 0, 0.06f, 0.5f, 0.06f, 0, 0, 90), C("#6a4424"), 5, 1f, true);
                    break;
                case "gale_glider":
                    mb.Sphere(M(-0.45f, 0.55f, 0, 0.9f, 0.06f, 0.5f, 0, 0, 10), C("#6ac050"), C("#c8ec7a"), 10, 4);
                    mb.Sphere(M(0.45f, 0.55f, 0, 0.9f, 0.06f, 0.5f, 0, 0, -10), C("#6ac050"), C("#c8ec7a"), 10, 4);
                    mb.Cylinder(M(0, 0.3f, 0, 0.05f, 0.5f, 0.05f), C("#7a5232"), 5, 1f, true);
                    mb.Cylinder(M(0, 0.08f, 0, 0.05f, 0.6f, 0.05f, 0, 0, 90), C("#7a5232"), 5, 1f, true);
                    break;
                case "vine_grapple":
                    for (int i = 0; i < 5; i++) mb.Cylinder(M(0, 0.12f + i * 0.08f, 0, 0.5f - i * 0.04f, 0.06f, 0.5f - i * 0.04f), C("#4a8a30"), C("#7ac050"), 10, 1f, false);
                    mb.Cone(M(0.35f, 0.75f, 0, 0.12f, 0.35f, 0.12f, 0, 0, -30), C("#d8d8e6"), 6);
                    mb.Cone(M(0.2f, 0.78f, 0.12f, 0.1f, 0.3f, 0.1f, 20, 0, -10), C("#d8d8e6"), 6);
                    break;
                case "provision_pouch":
                    mb.Sphere(M(0, 0.32f, 0, 0.78f, 0.6f, 0.5f), C("#a8703a"), C("#c8904e"), 10, 6);
                    mb.Box(M(0, 0.56f, 0.04f, 0.7f, 0.2f, 0.5f, -12, 0, 0), C("#7a4c26"), C("#94602f"));
                    mb.Cylinder(M(0, 0.44f, 0.27f, 0.12f, 0.05f, 0.12f, 90, 0, 0), C("#e0c060"), 8, 1f, true);
                    mb.Sphere(M(-0.2f, 0.74f, -0.05f, 0.2f, 0.2f, 0.2f), C("#ff5a7a"), C("#ff8aa0"), 8, 5);
                    mb.Sphere(M(0.08f, 0.78f, -0.1f, 0.2f, 0.2f, 0.2f), C("#ff5a7a"), C("#ff8aa0"), 8, 5);
                    break;
                case "kin_harness":
                    mb.Box(M(0, 0.18f, 0, 0.9f, 0.16f, 0.6f), C("#8a5a30"), C("#b07a44"));
                    mb.Box(M(0, 0.3f, -0.22f, 0.9f, 0.16f, 0.12f), C("#6a4424"));
                    mb.Cylinder(M(0.46f, 0.1f, 0, 0.3f, 0.05f, 0.3f, 0, 0, 90), C("#d8b04a"), 10, 0.6f, true);
                    mb.Cylinder(M(-0.46f, 0.1f, 0, 0.3f, 0.05f, 0.3f, 0, 0, 90), C("#d8b04a"), 10, 0.6f, true);
                    mb.Box(M(0, 0.28f, 0.18f, 0.5f, 0.06f, 0.1f), C("#c43a3a"));
                    break;
                case "tether_seed":
                    Seed(mb, C("#8fd96a"), C("#4f9a3a"), C("#7a5530"), C("#5ac04a"));
                    break;
                case "ironbark_seed":
                    Seed(mb, C("#c9a86a"), C("#8a6a3a"), C("#d8dae6"), C("#9ac04a"));
                    break;
                case "cinder_seed":
                    Seed(mb, C("#ff8a4a"), C("#b8401a"), C("#3a3034"), C("#ffc070"));
                    mb.Crystal(M(0.16f, 0.5f, -0.1f, 0.08f, 0.16f, 0.08f), C("#ff6a2a"), C("#ffd080"));
                    break;
                case "rime_seed":
                    Seed(mb, C("#b8f0ff"), C("#4aa0c8"), C("#d8dae6"), C("#e8fbff"));
                    mb.Crystal(M(0, 0.6f, 0, 0.11f, 0.22f, 0.11f), C("#8fe8ff"), C("#ffffff"));
                    break;
                case "sunforged_seed":
                    Seed(mb, C("#d08a3a"), C("#7a4a18"), C("#ffe27a"), C("#ffb84a"));
                    mb.Cylinder(M(0, 0.4f, 0, 0.48f, 0.05f, 0.48f), C("#ffd860"), 12, 1f, true);
                    break;
                case "starbloom_seed":
                    Seed(mb, C("#ffe06a"), C("#e0a02a"), C("#8fe8ff"), C("#ffffff"));
                    mb.Crystal(M(0, 0.62f, 0, 0.14f, 0.28f, 0.14f), C("#8fe8ff"), C("#ffffff"));
                    break;
                // ---- legend and boss seeds: the seed shape in each legend's colours, with the thing it was made from ----
                case "moonpearl_seed":
                    Seed(mb, C("#b4c6ff"), C("#5a70c8"), C("#f6f8ff"), C("#dff0ff"));
                    mb.Sphere(M(0, 0.86f, 0, 0.24f, 0.24f, 0.24f), C("#ffffff"), C("#d8dcf8"), 10, 6);
                    mb.Sphere(M(-0.12f, 0.32f, -0.28f, 0.14f, 0.14f, 0.14f), C("#ffffff"), 8, 5);
                    mb.Sphere(M(0.18f, 0.32f, -0.25f, 0.14f, 0.14f, 0.14f), C("#ffffff"), 8, 5);
                    break;
                case "lanternfruit_seed":
                    Seed(mb, C("#fff09a"), C("#ffb84a"), C("#3f7a4a"), C("#5ac04a"));
                    mb.Cylinder(M(0.2f, 0.82f, 0, 0.03f, 0.2f, 0.03f, 0, 0, -50), C("#3f7a4a"), 5, 1f, true);
                    mb.Sphere(M(0.32f, 0.7f, 0, 0.24f, 0.3f, 0.24f), C("#fff6c0"), C("#ff9a2a"), 8, 6);
                    break;
                case "emberglass_seed":
                    Seed(mb, C("#ff9a4a"), C("#b8301a"), C("#3a2020"), C("#ffd24a"));
                    mb.Crystal(M(0, 0.58f, 0, 0.18f, 0.4f, 0.18f), C("#ff7a2a"), C("#fff0a0"));
                    mb.Crystal(M(0.14f, 0.56f, 0.04f, 0.12f, 0.26f, 0.12f, 0, 0, -32), C("#ff5a1a"), C("#ffd24a"));
                    mb.Crystal(M(-0.14f, 0.56f, -0.02f, 0.12f, 0.24f, 0.12f, 0, 0, 34), C("#ff5a1a"), C("#ffd24a"));
                    break;
                case "blightscale_seed":
                    Seed(mb, C("#8a6ac0"), C("#3a2a5a"), C("#9be04a"), C("#c8ff6a"));
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (i - 2) * 0.5f;
                        mb.Sphere(M(Mathf.Sin(a) * 0.27f, 0.52f - (i % 2) * 0.1f, -Mathf.Cos(a) * 0.27f, 0.2f, 0.2f, 0.06f, -20, -a * Mathf.Rad2Deg, 0), C("#5a4090"), C("#2a1c48"), 6, 4);
                    }
                    break;
                case "starmane_seed":
                    Seed(mb, C("#4a4ab8"), C("#1a1a5a"), C("#ffe9a0"), C("#bff8ff"));
                    for (int i = 0; i < 6; i++)
                        mb.Sphere(M(0.06f + i * 0.07f, 0.78f + Mathf.Sin(i * 0.9f) * 0.09f, 0, 0.13f - i * 0.012f, 0.13f - i * 0.012f, 0.13f - i * 0.012f), C("#9af4ff"), C("#e8ffff"), 6, 4);
                    mb.Crystal(M(-0.16f, 0.5f, -0.22f, 0.08f, 0.18f, 0.08f, -30, 0, 20), C("#fff6c0"), C("#ffffff"));
                    mb.Crystal(M(0.14f, 0.12f, -0.22f, 0.07f, 0.16f, 0.07f, -30, 0, -20), C("#fff6c0"), C("#ffffff"));
                    break;
                case "prismatic_seed":
                    Seed(mb, C("#e6dcff"), C("#9a80d8"), C("#d8b04a"), C("#ffffff"));
                    mb.Crystal(M(0, 0.6f, 0, 0.16f, 0.4f, 0.16f), C("#ff7ad0"), C("#ffd0f0"));
                    mb.Crystal(M(0.15f, 0.58f, 0.03f, 0.12f, 0.28f, 0.12f, 0, 0, -34), C("#7ae8ff"), C("#e0fbff"));
                    mb.Crystal(M(-0.15f, 0.58f, -0.02f, 0.12f, 0.28f, 0.12f, 0, 0, 34), C("#ffe46a"), C("#fff8c8"));
                    break;
                case "deepcall_seed":
                    mb.Sphere(M(0, 0.36f, 0, 0.62f, 0.66f, 0.62f), C("#3a3260"), C("#0e0c1c"), 12, 8);
                    mb.Cylinder(M(0, 0.36f, 0, 0.66f, 0.08f, 0.66f, 0, 0, 32), C("#2f7a5a"), C("#4fae7e"), 12, 1f, true);
                    mb.Cylinder(M(0, 0.36f, 0, 0.66f, 0.08f, 0.66f, 20, 0, -34), C("#256648"), C("#3f9a6c"), 12, 1f, true);
                    mb.Sphere(M(0.2f, 0.72f, 0.04f, 0.3f, 0.06f, 0.14f, 0, 0, -35), C("#4fae7e"), 6, 4);
                    mb.Sphere(M(-0.12f, 0.74f, 0, 0.24f, 0.06f, 0.12f, 0, 0, 40), C("#3f9a6c"), 6, 4);
                    mb.Sphere(M(-0.14f, 0.52f, -0.22f, 0.13f, 0.1f, 0.06f), C("#bfe8ff"), 6, 4);
                    break;
                // ---- the five outer ores (same colours as their veins in PropCatalog) and their bars ----
                case "saltglass":
                    mb.Rock(M(0, 0.2f, 0, 0.85f, 0.4f, 0.75f), C("#c8b890"), C("#e0d4b0"), 61u, 7, 5);
                    mb.Crystal(M(0, 0.22f, 0, 0.36f, 0.72f, 0.36f), C("#3cb8b0"), C("#a4f0e8"));
                    mb.Crystal(M(0.24f, 0.2f, -0.06f, 0.24f, 0.46f, 0.24f, 0, 0, -26), C("#48c4bc"), C("#b4f6ee"));
                    mb.Crystal(M(-0.22f, 0.2f, -0.08f, 0.22f, 0.4f, 0.22f, 0, 0, 28), C("#34a8a0"), C("#98e8e0"));
                    break;
                case "bog_iron":
                    OreChunk(mb, C("#4a4a3a"), C("#5e6048"), C("#3a5a3a"), C("#8ab06a"), 67u);
                    mb.Sphere(M(-0.32f, 0.2f, -0.26f, 0.3f, 0.12f, 0.24f), C("#6a8a3a"), 6, 4);
                    break;
                case "sunsteel_ore":
                    OreChunk(mb, C("#8e6a42"), C("#a88458"), C("#f0a818"), C("#ffe060"), 71u);
                    break;
                case "blight_ore":
                    OreChunk(mb, C("#3a3444"), C("#4a4458"), C("#8a40c8"), C("#b8ff40"), 73u);
                    mb.Sphere(M(-0.1f, 0.2f, -0.37f, 0.1f, 0.22f, 0.08f), C("#b8ff40"), 6, 4);
                    break;
                case "geode_core":
                    // half a geode: a rough grey shell, violet crystal packed inside, facing the camera
                    mb.Sphere(M(0, 0.32f, 0, 0.78f, 0.72f, 0.7f), C("#5a5260"), C("#3e3846"), 9, 6);
                    mb.Sphere(M(-0.06f, 0.34f, -0.24f, 0.58f, 0.54f, 0.24f), C("#2a1a40"), C("#3a2a54"), 9, 6);
                    for (int k = 0; k < 7; k++)
                    {
                        float a = k * 51f * Mathf.Deg2Rad;
                        mb.Crystal(M(-0.06f + Mathf.Cos(a) * 0.15f, 0.34f + Mathf.Sin(a) * 0.15f, -0.26f, 0.09f, 0.22f, 0.09f, -70, 0, k * 51f), C("#9a6aff"), C("#f0e0ff"));
                    }
                    mb.Crystal(M(-0.06f, 0.34f, -0.28f, 0.12f, 0.26f, 0.12f, -90, 0, 0), C("#b48aff"), C("#ffffff"));
                    break;
                case "starcrystal":
                    mb.Rock(M(0, 0.18f, 0, 0.8f, 0.36f, 0.7f), C("#2e2e52"), C("#44447a"), 79u, 7, 5);
                    mb.Crystal(M(0, 0.2f, 0, 0.38f, 0.8f, 0.38f), C("#6a58e8"), C("#c4bcff"));
                    mb.Crystal(M(0.24f, 0.2f, -0.06f, 0.22f, 0.44f, 0.22f, 0, 0, -30), C("#7a6af0"), C("#d0c8ff"));
                    mb.Crystal(M(-0.22f, 0.2f, -0.08f, 0.2f, 0.38f, 0.2f, 0, 0, 30), C("#5a4ad8"), C("#b8b0ff"));
                    mb.Sphere(M(-0.04f, 0.56f, -0.13f, 0.1f, 0.1f, 0.06f), C("#ffffff"), 6, 4);
                    break;
                case "saltglass_bar": Bars(mb, C("#38a8a0"), C("#9aeee6")); break;
                case "bogsteel_bar": Bars(mb, C("#3a4630"), C("#7e9068")); break;
                case "sunsteel_bar": Bars(mb, C("#d08a18"), C("#ffd448")); break;
                case "blightsteel_bar":
                    Bars(mb, C("#3e2c58"), C("#7e62a8"));
                    mb.Box(M(0.08f, 0.485f, 0.02f, 0.5f, 0.012f, 0.05f, 0, 30, 0), C("#b8ff40"));
                    break;
                case "starsteel_bar":
                    Bars(mb, C("#5048c0"), C("#b4acff"));
                    mb.Crystal(M(0.08f, 0.47f, 0.02f, 0.1f, 0.2f, 0.1f), C("#bff8ff"), C("#ffffff"));
                    break;
                // ---- ammunition ----
                case "arrow":
                    Shaft(mb, M(-0.2f, 0.02f, 0.06f, 1, 1, 1, 0, 0, -34), false);
                    Shaft(mb, M(0f, 0f, 0f, 1, 1, 1, 0, 0, -46), false);
                    Shaft(mb, M(0.2f, 0.02f, -0.06f, 1, 1, 1, 0, 0, -58), false);
                    break;
                case "bolt":
                    Shaft(mb, M(-0.18f, 0.02f, 0.06f, 1, 1, 1, 0, 0, -34), true);
                    Shaft(mb, M(0f, 0f, 0f, 1, 1, 1, 0, 0, -46), true);
                    Shaft(mb, M(0.18f, 0.02f, -0.06f, 1, 1, 1, 0, 0, -58), true);
                    break;
                case "kin_egg":
                    // a speckled egg in a nest of straw
                    mb.Sphere(M(0, 0.52f, 0, 0.66f, 0.86f, 0.66f), C("#fff4dc"), C("#e0cfa8"), 14, 10);
                    mb.Sphere(M(-0.2f, 0.66f, -0.27f, 0.13f, 0.11f, 0.06f), C("#c89a5a"), 6, 4);
                    mb.Sphere(M(0.12f, 0.42f, -0.31f, 0.1f, 0.08f, 0.05f), C("#c89a5a"), 6, 4);
                    mb.Sphere(M(-0.06f, 0.86f, -0.18f, 0.08f, 0.07f, 0.05f), C("#c89a5a"), 6, 4);
                    mb.Sphere(M(0.25f, 0.68f, -0.15f, 0.09f, 0.08f, 0.05f), C("#c89a5a"), 6, 4);
                    mb.Cylinder(M(0, 0.1f, 0, 0.9f, 0.18f, 0.9f), C("#c8a050"), C("#9a7430"), 14, 0.8f, true);
                    break;
                case "tonic_vigor":
                case "tonic_might":
                case "tonic_ironhide":
                {
                    // a stoppered flask of coloured tonic
                    string liquid = d.id == "tonic_vigor" ? "#e0506e" : d.id == "tonic_might" ? "#f08a2a" : "#5a86c8";
                    string dark = d.id == "tonic_vigor" ? "#a02848" : d.id == "tonic_might" ? "#b0561a" : "#2e5288";
                    mb.Sphere(M(0, 0.38f, 0, 0.62f, 0.6f, 0.62f), C(liquid), C(dark), 12, 8);
                    mb.Cylinder(M(0, 0.78f, 0, 0.22f, 0.3f, 0.22f), C("#d8eef4"), C("#a8c4cc"), 10, 1f, true);
                    mb.Cylinder(M(0, 0.98f, 0, 0.26f, 0.12f, 0.26f), C("#9a6a3a"), C("#6a4422"), 10, 1f, true);
                    mb.Sphere(M(-0.14f, 0.5f, -0.22f, 0.1f, 0.14f, 0.05f), C("#ffffff"), 6, 4);
                    break;
                }
                case "warden_heart":
                    // a heart cut from crystal on a gold setting
                    {
                        // built flat-on, then leaned back so the camera (above, on the -z side) sees the whole shape
                        var h = new MeshBuilder();
                        h.Sphere(M(-0.19f, 0.5f, 0, 0.48f, 0.46f, 0.36f), C("#ff7aa8"), C("#e8386e"), 10, 7);
                        h.Sphere(M(0.19f, 0.5f, 0, 0.48f, 0.46f, 0.36f), C("#ff7aa8"), C("#e8386e"), 10, 7);
                        h.Cylinder(M(0, 0.2f, 0, 0.8f, 0.54f, 0.36f, 0, 0, 180), C("#ff6a96"), C("#b01850"), 14, 0f, true);
                        h.Sphere(M(-0.22f, 0.6f, -0.15f, 0.16f, 0.1f, 0.05f, 0, 0, 30), C("#ffe0ec"), 6, 4);
                        mb.Append(h, M(0, 0.16f, 0, 1, 1, 1, 30, 49, 0));   // 49 = the camera's bearing in model space
                        mb.Cylinder(M(0, 0.05f, 0, 0.4f, 0.1f, 0.4f), C("#b88a2a"), C("#f0cc5a"), 10, 0.7f, true);
                    }
                    break;
                case "chest_key":
                    // a gold key, built flat-on and leaned back like the Warden Heart so the camera sees its shape
                    {
                        var k = new MeshBuilder();
                        k.Cylinder(M(0, 0.82f, 0, 0.5f, 0.12f, 0.5f, 90, 0, 0), C("#b88a2a"), C("#f0cc5a"), 14, 1f, true);   // the bow
                        k.Cylinder(M(0, 0.82f, -0.02f, 0.24f, 0.16f, 0.24f, 90, 0, 0), C("#3a3020"), 10, 1f, true);        // the hole in it
                        k.Box(M(0, 0.36f, 0, 0.11f, 0.62f, 0.11f), C("#c89a34"), C("#f0cc5a"));                             // the shaft
                        k.Box(M(0.13f, 0.2f, 0, 0.18f, 0.09f, 0.1f), C("#f0cc5a"));                                        // two teeth
                        k.Box(M(0.11f, 0.08f, 0, 0.14f, 0.09f, 0.1f), C("#f0cc5a"));
                        mb.Append(k, M(0, 0.06f, 0, 1, 1, 1, 24, 49, 0));
                    }
                    break;
                case "outpost_kit":
                    mb.Box(M(0, 0.25f, 0, 0.84f, 0.5f, 0.6f), C("#8a5a30"), C("#b88a50"));
                    mb.Box(M(-0.26f, 0.25f, 0, 0.08f, 0.52f, 0.62f), C("#5a3a1e"));
                    mb.Box(M(0.26f, 0.25f, 0, 0.08f, 0.52f, 0.62f), C("#5a3a1e"));
                    mb.Capsule(M(-0.06f, 0.6f, 0.06f, 0.2f, 0.66f, 0.2f, 0, 0, 90), C("#5a9a4a"), 8);
                    mb.Cylinder(M(0.3f, 0.86f, -0.16f, 0.05f, 0.9f, 0.05f), C("#7a5232"), 5, 1f, true);
                    mb.Box(M(0.5f, 1.16f, -0.16f, 0.36f, 0.24f, 0.02f), C("#e86a2a"), C("#ffb04a"));
                    break;
                default:
                    mb.Box(M(0, 0.3f, 0, 0.6f, 0.6f, 0.6f), d.iconColor);
                    break;
            }
            return mb.VertexCount > 0 ? mb.ToMesh("itemicon_" + d.id) : null;
        }

        static void Log(MeshBuilder mb, float x, float y, float z)
        {
            mb.Cylinder(M(x, y + 0.14f, z, 0.28f, 0.95f, 0.28f, 0, 0, 90), C("#8a5a32"), C("#e0b880"), 10, 1f, true);
        }

        static void Seed(MeshBuilder mb, Color32 shell, Color32 shellDark, Color32 band, Color32 sprout)
        {
            mb.Sphere(M(0, 0.32f, 0, 0.56f, 0.64f, 0.56f), shell, shellDark, 12, 8);
            mb.Cylinder(M(0, 0.32f, 0, 0.6f, 0.1f, 0.6f), band, 12, 1f, true);
            mb.Cone(M(0, 0.7f, 0, 0.12f, 0.22f, 0.12f), sprout, 6);
            mb.Sphere(M(0.14f, 0.74f, 0, 0.22f, 0.06f, 0.12f, 0, 0, -35), sprout, 6, 4);
        }

        /// <summary>A lump of rock with nuggets of ore showing (the plain "ore" icon, in any land's colours).</summary>
        static void OreChunk(MeshBuilder mb, Color32 rock, Color32 rockTop, Color32 ore, Color32 oreTop, uint seed)
        {
            mb.Rock(M(0, 0.32f, 0, 0.85f, 0.65f, 0.75f), rock, rockTop, seed, 7, 5);
            mb.Rock(M(0.14f, 0.58f, -0.12f, 0.34f, 0.28f, 0.34f), ore, oreTop, seed + 2u, 5, 4);
            mb.Rock(M(-0.24f, 0.44f, -0.2f, 0.28f, 0.24f, 0.28f), ore, oreTop, seed + 6u, 5, 4);
            mb.Rock(M(0.1f, 0.28f, -0.34f, 0.22f, 0.2f, 0.22f), ore, oreTop, seed + 9u, 5, 4);
        }

        /// <summary>Two stacked bars (the ingot icon, tinted).</summary>
        static void Bars(MeshBuilder mb, Color32 dark, Color32 light)
        {
            mb.Cylinder(M(0, 0.12f, 0, 0.9f, 0.24f, 0.42f), dark, light, 4, 0.78f, true);
            mb.Cylinder(M(0.08f, 0.36f, 0.02f, 0.9f, 0.24f, 0.42f, 0, 12, 0), dark, light, 4, 0.78f, true);
        }

        /// <summary>One arrow or crossbow bolt, built point up and placed by <paramref name="m"/>.</summary>
        static void Shaft(MeshBuilder mb, Matrix4x4 m, bool bolt)
        {
            var a = new MeshBuilder();
            float len = bolt ? 0.62f : 0.96f, thick = bolt ? 0.075f : 0.05f;
            a.Cylinder(M(0, len * 0.5f, 0, thick, len, thick), bolt ? C("#6a4a2a") : C("#c9a26a"), bolt ? C("#8a6238") : C("#e0bc84"), 6, 1f, true);
            if (bolt) a.Cylinder(M(0, len + 0.13f, 0, 0.2f, 0.28f, 0.2f), C("#7a8094"), C("#e4e8f4"), 4, 0f, true);
            else a.Cone(M(0, len + 0.1f, 0, 0.15f, 0.22f, 0.15f), C("#8a8a94"), 4);
            Color32 f = bolt ? C("#b8bcc8") : C("#e0523a"), f2 = bolt ? C("#8a8e9a") : C("#fff0e0");
            float fy = bolt ? 0.1f : 0.14f, fh = bolt ? 0.16f : 0.24f;
            a.Box(M(0, fy, 0, 0.2f, fh, 0.016f), f, f2);
            a.Box(M(0, fy, 0, 0.016f, fh, 0.2f), f, f2);
            mb.Append(a, m);
        }
    }
}
