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

        public static void RenderAll(Light sun)
        {
            // icons survive scene changes (menu <-> world); only render once per app run
            if (sprites.Count > 0)
            {
                bool valid = true;
                foreach (var s in sprites.Values) if (s == null || s.texture == null) { valid = false; break; }
                if (valid) return;
                sprites.Clear();
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
                var m = ItemModels.Build(kv.Value);
                if (m != null) jobs.Add((kv.Key, m, kv.Value.category == ItemCategory.Tool || kv.Value.category == ItemCategory.Weapon));
            }
            foreach (var bd in ContentDB.BuildingList)
                jobs.Add(("bld_" + bd.id, ModelBuilder.BuildStatic(bd.parts, bd.palette, "bldicon_" + bd.id), false));
            foreach (var sp in ContentDB.SpeciesList)
                if (sp.parts != null && sp.parts.Length > 0)
                    jobs.Add(("kin_" + sp.id, ModelBuilder.BuildStatic(sp.parts, sp.palette, "kinicon_" + sp.id), false));

            foreach (var job in jobs)
            {
                var mesh = job.mesh;
                mf.sharedMesh = mesh;
                bool longItem = job.longItem;
                // long items stand diagonally across the icon; others get a gentle turn
                Quaternion rot = longItem
                    ? Quaternion.AngleAxis(-42f, viewDir) * Quaternion.LookRotation(Vector3.up, Vector3.Cross(Vector3.up, viewDir).normalized)
                    : Quaternion.Euler(0f, -20f, 0f);
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
                case "starbloom_seed":
                    Seed(mb, C("#ffe06a"), C("#e0a02a"), C("#8fe8ff"), C("#ffffff"));
                    mb.Crystal(M(0, 0.62f, 0, 0.14f, 0.28f, 0.14f), C("#8fe8ff"), C("#ffffff"));
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
    }
}
