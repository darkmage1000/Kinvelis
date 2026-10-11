using System.Threading.Tasks;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Generates the whole-island map texture on a worker thread (shaded relief + biome colors + water).</summary>
    public static class MapTexture
    {
        public const int Size = 384;
        public static Texture2D Texture { get; private set; }
        static Color32[] pixels;
        static Task task;

        public static void Begin()
        {
            Texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "worldmap" };
            var fill = new Color32[Size * Size];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(40, 90, 130, 255);
            Texture.SetPixels32(fill);
            Texture.Apply();
            task = Task.Run(() =>
            {
                var px = new Color32[Size * Size];
                float cell = WorldGen.HalfWorld * 2f / Size;
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float wx = -WorldGen.HalfWorld + (x + 0.5f) * cell;
                    float wz = -WorldGen.HalfWorld + (y + 0.5f) * cell;
                    px[y * Size + x] = Shade(wx, wz, cell);
                }
                pixels = px;
            });
        }

        /// <summary>Map color for one world point (thread-safe). cell = sample spacing in meters (for hill shading).</summary>
        public static Color32 Shade(float wx, float wz, float cell)
        {
            WorldGen.Weights(wx, wz, out float m, out float b, out float p);
            float h = WorldGen.HeightW(wx, wz, m, b, p);
            Color c;
            if (h < WorldGen.WaterLevel)
            {
                c = Color.Lerp(new Color(0.35f, 0.7f, 0.85f), new Color(0.12f, 0.3f, 0.55f), Mathf.Clamp01(-h / 20f));
            }
            else
            {
                float e = Mathf.Max(1.5f, cell);
                float hx = WorldGen.Height(wx + e, wz), hz = WorldGen.Height(wx, wz + e);
                float slope = ((h - hx) + (h - hz)) / e;
                float shade = Mathf.Clamp(slope * 0.9f + 1f, 0.68f, 1.3f);
                float ny = 1f / Mathf.Sqrt(1f + ((h - hx) / e) * ((h - hx) / e) + ((h - hz) / e) * ((h - hz) / e));
                Color bc = WorldGen.TerrainColor(wx, wz, h, ny, m, b, p, cell);
                c = bc * shade;
                if (h < 1.2f) c = Color.Lerp(c, new Color(0.95f, 0.88f, 0.66f), 0.6f);
            }
            c.a = 1f;
            return c;
        }

        /// <summary>Call every frame until true.</summary>
        public static bool Poll()
        {
            if (pixels == null) return task == null;
            Texture.SetPixels32(pixels);
            Texture.Apply(false, false);
            pixels = null;
            task = null;
            return true;
        }

        public static Vector2 WorldToUV(Vector3 p) =>
            new Vector2((p.x + WorldGen.HalfWorld) / (WorldGen.HalfWorld * 2f), (p.z + WorldGen.HalfWorld) / (WorldGen.HalfWorld * 2f));
    }
}
