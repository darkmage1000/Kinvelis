using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// What you see on a Crop Plot or in a Greenhouse as its batch moves along: bare soil, seed mounds appearing as it
    /// is planted, the soil darkening from one end as it is watered, sprouts that grow in four steps, then the ripe
    /// crop in that crop's own shape and colour until it is picked. The structure's own model is drawn without its
    /// built-in bushes (Building.Setup), so the bed really is empty when nothing has been planted.
    /// Meshes are shared by every plot (one per stage look); each plot only swaps which one it shows.
    /// </summary>
    public class CropVisual : MonoBehaviour
    {
        Building b;
        MeshFilter plants;
        Transform wet;
        string shown;
        float soilTop;

        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Vector3[] Spots =
        {
            new Vector3(-0.95f, 0, -0.52f), new Vector3(0f, 0, -0.52f), new Vector3(0.95f, 0, -0.52f),
            new Vector3(-0.95f, 0, 0.52f), new Vector3(0f, 0, 0.52f), new Vector3(0.95f, 0, 0.52f),
        };

        public void Init(Building building, float top)
        {
            b = building;
            soilTop = top;
            var pg = new GameObject("Crop");
            pg.transform.SetParent(transform, false);
            pg.transform.localPosition = new Vector3(0, top, 0);
            plants = pg.AddComponent<MeshFilter>();
            var mr = pg.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            var wg = new GameObject("Wet");
            wg.transform.SetParent(transform, false);
            wg.AddComponent<MeshFilter>().sharedMesh = Get("wet");
            var wr = wg.AddComponent<MeshRenderer>();
            wr.sharedMaterial = RenderAssets.Building;
            wr.shadowCastingMode = ShadowCastingMode.Off;
            wet = wg.transform;
            Sync();
        }

        void LateUpdate() { if (b != null) Sync(); }

        void Sync()
        {
            float p = b.Stage01;
            string want = null;
            float water = 0f;
            switch (b.Stage)
            {
                case 0: want = p > 0.03f ? "seeds" + Mathf.Clamp(Mathf.CeilToInt(p * 3f), 1, 3) : null; break;   // a row at a time
                case WorkStages.CropWater: want = "seeds3"; water = p; break;
                case WorkStages.CropGrow: want = "sprout" + Mathf.Clamp(Mathf.FloorToInt(p * 4f), 0, 3); water = 1f; break;
                default: want = "ripe_" + b.OutputItem; water = 1f; break;
            }
            if (want != shown)
            {
                shown = want;
                plants.sharedMesh = want != null ? Get(want) : null;
            }
            // wet soil spreads across the bed from the left as the watering goes on
            bool anyWater = water > 0.02f;
            if (wet.gameObject.activeSelf != anyWater) wet.gameObject.SetActive(anyWater);
            if (anyWater)
            {
                wet.localScale = new Vector3(2.84f * water, 1f, 2.04f);
                wet.localPosition = new Vector3(-1.42f + 1.42f * water, soilTop + 0.006f, 0f);
            }
        }

        static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Matrix4x4 M(Vector3 at, float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(at + new Vector3(px, py, pz), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        static Mesh Get(string key)
        {
            if (meshes.TryGetValue(key, out var m) && m != null) return m;
            var mb = new MeshBuilder();
            if (key == "wet")
            {
                mb.Box(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.012f, 1f)), C("#3a2618"), C("#3f2a1a"));
            }
            else if (key.StartsWith("seeds"))
            {
                int rows = key[key.Length - 1] - '0';              // 1-3: how many of the three columns are sown
                for (int i = 0; i < Spots.Length; i++)
                {
                    if (i % 3 >= rows) continue;
                    var at = Spots[i];
                    mb.Sphere(M(at, 0, 0.02f, 0, 0.5f, 0.12f, 0.5f), C("#7a5636"), C("#5e4028"), 8, 4);
                    mb.Sphere(M(at, 0.07f, 0.08f, 0.04f, 0.07f, 0.05f, 0.07f), C("#f0e2b0"), 6, 4);
                    mb.Sphere(M(at, -0.08f, 0.08f, -0.05f, 0.06f, 0.045f, 0.06f), C("#f0e2b0"), 6, 4);
                }
            }
            else if (key.StartsWith("sprout"))
            {
                float f = new[] { 0.3f, 0.5f, 0.75f, 1f }[key[key.Length - 1] - '0'];
                foreach (var at in Spots)
                {
                    mb.Sphere(M(at, 0, 0.02f, 0, 0.5f, 0.12f, 0.5f), C("#5e4028"), C("#4a301e"), 8, 4);
                    mb.Cylinder(M(at, 0, 0.2f * f, 0, 0.05f, 0.4f * f, 0.05f), C("#4f9c3a"), 5, 0.8f, true);
                    mb.Sphere(M(at, 0.11f * f, 0.36f * f, 0, 0.3f * f, 0.07f * f, 0.16f * f, 0, 0, -28), C("#6cc24e"), C("#4f9c3a"), 6, 4);
                    mb.Sphere(M(at, -0.11f * f, 0.3f * f, 0, 0.3f * f, 0.07f * f, 0.16f * f, 0, 0, 28), C("#6cc24e"), C("#4f9c3a"), 6, 4);
                    if (f > 0.7f) mb.Sphere(M(at, 0, 0.44f * f, 0.08f, 0.16f * f, 0.07f * f, 0.26f * f, -24, 0, 0), C("#7ad45c"), C("#4f9c3a"), 6, 4);
                }
            }
            else   // ripe_<crop>
            {
                string crop = key.Substring(5);
                var item = ContentDB.Item(crop);
                Color32 fruit = crop == "berry" ? C("#ff5a7a") : crop == "goldgrain" ? C("#f0c040") : crop == "dewmelon" ? C("#8fdc6a")
                              : crop == "emberpepper" ? C("#e8402a") : crop == "frostmint" ? C("#a8e6f4") : (Color32)(item != null ? item.iconColor : Color.white);
                foreach (var at in Spots)
                {
                    mb.Sphere(M(at, 0, 0.02f, 0, 0.5f, 0.12f, 0.5f), C("#5e4028"), C("#4a301e"), 8, 4);
                    if (crop == "goldgrain")
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            float a = i * 1.26f, r = 0.12f;
                            mb.Cylinder(M(at, Mathf.Cos(a) * r, 0.34f, Mathf.Sin(a) * r, 0.04f, 0.68f, 0.04f, Mathf.Sin(a) * 8f, 0, Mathf.Cos(a) * -8f), C("#b89a3a"), C("#e0c050"), 5, 0.8f, true);
                            mb.Sphere(M(at, Mathf.Cos(a) * r * 1.5f, 0.72f, Mathf.Sin(a) * r * 1.5f, 0.09f, 0.22f, 0.09f), fruit, C("#c89a20"), 6, 4);
                        }
                    }
                    else if (crop == "dewmelon")
                    {
                        mb.Sphere(M(at, 0, 0.1f, 0, 0.62f, 0.1f, 0.62f), C("#4f9c3a"), C("#3f7c2e"), 8, 4);
                        mb.Sphere(M(at, 0.04f, 0.24f, 0.02f, 0.44f, 0.38f, 0.4f), fruit, C("#4fa83e"), 10, 6);
                        mb.Sphere(M(at, 0.04f, 0.25f, 0.02f, 0.12f, 0.39f, 0.41f), C("#3f8a34"), 8, 5);
                    }
                    else if (crop == "frostmint")
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i * 1.05f;
                            mb.Sphere(M(at, Mathf.Cos(a) * 0.14f, 0.2f + (i % 2) * 0.14f, Mathf.Sin(a) * 0.14f, 0.3f, 0.08f, 0.17f, 0, -a * Mathf.Rad2Deg, 24), fruit, C("#6cc0d8"), 6, 4);
                        }
                        mb.Cylinder(M(at, 0, 0.2f, 0, 0.05f, 0.4f, 0.05f), C("#4f9c8a"), 5, 0.8f, true);
                    }
                    else
                    {
                        mb.Sphere(M(at, 0, 0.3f, 0, 0.62f, 0.52f, 0.62f), C("#4f9c3a"), C("#3f7c2e"), 9, 6);
                        for (int i = 0; i < 5; i++)
                        {
                            float a = i * 1.26f + at.x;
                            if (crop == "emberpepper") mb.Cone(M(at, Mathf.Cos(a) * 0.25f, 0.3f + (i % 3) * 0.07f, Mathf.Sin(a) * 0.25f, 0.1f, 0.2f, 0.1f, 180, 0, 0), fruit, 6);
                            else mb.Sphere(M(at, Mathf.Cos(a) * 0.26f, 0.32f + (i % 3) * 0.09f, Mathf.Sin(a) * 0.26f, 0.15f, 0.15f, 0.15f), fruit, 6, 5);
                        }
                    }
                }
            }
            m = mb.VertexCount > 0 ? mb.ToMesh("crop_" + key) : null;
            meshes[key] = m;
            return m;
        }
    }
}
