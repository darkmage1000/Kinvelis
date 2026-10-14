using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Dev harness (Play mode, via execute_code): contact sheets of kin at each evolution stage, written straight to a
    /// PNG. EvoSheet.Sheet("wickit,cindling", "Previews/Evolve/sheet.png") = one tile per kind, its base / Ascended /
    /// Exalted forms left to right, seen from the front-left. EvoSheet.Sync() copies the seed's forms onto the loaded
    /// species (assets override the seed) so a new design can be looked at without restarting.
    /// </summary>
    public static class EvoSheet
    {
        const int Layer = 31;

        public static string Sync()
        {
            var seed = ContentSeed.Build();
            int n = 0;
            foreach (var s in seed.species)
            {
                var live = ContentDB.Species.TryGetValue(s.id, out var x) ? x : null;
                if (live == null) continue;
                if ((s.forms == null || s.forms.Length == 0) && (live.forms == null || live.forms.Length == 0)) continue;
                live.forms = s.forms;
                n++;
            }
            foreach (var a in seed.abilities) if (ContentDB.Ability(a.id) == null) ContentDB.Abilities[a.id] = a;
            ModelBuilder.ClearCache();
            return n + " kinds synced";
        }

        public static string Sheet(string ids, string file, int cols = 3, int tileW = 720, int tileH = 300)
        {
            var list = new List<string>();
            foreach (var s in ids.Split(',')) if (s.Trim().Length > 0) list.Add(s.Trim());
            int rows = (list.Count + cols - 1) / cols;
            var sheet = new Texture2D(tileW * cols, tileH * rows, TextureFormat.RGB24, false);
            var rt = new RenderTexture(tileW, tileH, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            rt.Create();
            var camGo = new GameObject("EvoSheetCam");
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; cam.targetTexture = rt; cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.18f, 0.24f);
            cam.fieldOfView = 22f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f; cam.allowHDR = false;
            var extra = cam.GetUniversalAdditionalCameraData(); extra.renderPostProcessing = false; extra.renderShadows = false;
            Vector3 origin = new Vector3(0f, -5000f, 0f);
            var sun = RenderSettings.sun;
            Quaternion sr = Quaternion.identity; float si = 0f; Color sc = Color.white; LightShadows ss = LightShadows.None;
            if (sun != null) { sr = sun.transform.rotation; si = sun.intensity; sc = sun.color; ss = sun.shadows; sun.transform.rotation = Quaternion.Euler(38f, 160f, 0f); sun.intensity = 1.15f; sun.color = Color.white; sun.shadows = LightShadows.None; }
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            var probe = RenderSettings.ambientProbe;
            var sh = new SphericalHarmonicsL2(); sh.AddAmbientLight(new Color(0.55f, 0.55f, 0.62f)); RenderSettings.ambientProbe = sh;
            var missing = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!ContentDB.Species.TryGetValue(list[i], out var sp)) { missing.Add(list[i]); continue; }
                var holder = new GameObject("EvoSheet_" + sp.id);
                holder.transform.position = origin;
                var rigs = new List<Transform>();
                for (int st = 0; st < 3; st++)
                {
                    var d = new CreatureData { uid = "evosheet" + st, speciesId = sp.id, level = 40, stage = st, look = new KinLook { rolled = true } };
                    var h = new GameObject("s" + st).transform;
                    h.SetParent(holder.transform, false);
                    var rig = ModelBuilder.Build(d, h, RenderAssets.Creature);
                    rig.Animate(0.016f, 0f, false, false);
                    rigs.Add(h);
                }
                foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
                // space them by their own widths
                float x = 0f; var widths = new float[3];
                for (int st = 0; st < 3; st++) { var b = BoundsOf(rigs[st]); widths[st] = Mathf.Max(b.size.x, b.size.z * 0.6f, 0.6f); }
                for (int st = 0; st < 3; st++)
                {
                    if (st > 0) x += (widths[st - 1] + widths[st]) * 0.5f + 0.35f;
                    rigs[st].localPosition = new Vector3(-x, 0f, 0f);   // the camera looks along -z, so -x is to the right
                    rigs[st].localRotation = Quaternion.Euler(0f, -28f, 0f);
                }
                var all = BoundsOf(holder.transform);
                float aspect = tileW / (float)tileH;
                float halfV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float dist = Mathf.Max(all.extents.y * 1.15f / halfV, all.extents.x * 1.1f / (halfV * aspect)) + all.extents.z;
                cam.transform.position = all.center + new Vector3(0f, all.extents.y * 0.25f, dist);
                cam.transform.LookAt(all.center);
                var req = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req); else cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                int cx = i % cols, cy = rows - 1 - i / cols;
                sheet.ReadPixels(new Rect(0, 0, tileW, tileH), cx * tileW, cy * tileH);
                RenderTexture.active = prev;
                Object.DestroyImmediate(holder);
            }
            sheet.Apply();
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), file);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
            RenderSettings.fog = fog; RenderSettings.ambientProbe = probe;
            if (sun != null) { sun.transform.rotation = sr; sun.intensity = si; sun.color = sc; sun.shadows = ss; }
            Object.DestroyImmediate(camGo); rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(sheet);
            return path + (missing.Count > 0 ? "  missing: " + string.Join(",", missing) : "");
        }

        static Bounds BoundsOf(Transform t)
        {
            var b = new Bounds(t.position, Vector3.zero); bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
