using UnityEngine;

namespace Wildbond
{
    /// <summary>What a map marker stands for. Dot is the plain circle (kin sightings, place names).</summary>
    public enum MapIcon
    {
        Dot, Camp, Village, Warden, Tamer, Waystone, Cave, Burrow, Dungeon, Lookout, Fish, Nest, Lair, Trader, Relic,
        Wreck, Bones, Tent, Statue, Pillars, Hunter, Legend, Outpost, Crater, Tree, Stones, Arch, Crystal, Quest, Chest, Count
    }

    /// <summary>
    /// Small pictures for the minimap and the map, drawn in code (no art files): each is a white shape with a dark
    /// outline and dark inner detail, so the marker's colour still tints it and it reads on any ground colour.
    /// Shapes are signed-distance functions in a -1..1 square (y up), rasterised once per icon at 64 px with mipmaps.
    /// </summary>
    public static class MapIcons
    {
        const int Size = 64;
        const float Outline = 0.12f;
        static readonly Sprite[] cache = new Sprite[(int)MapIcon.Count];

        public static Sprite Get(MapIcon icon)
        {
            int i = (int)icon;
            if (i < 0 || i >= cache.Length) i = 0;
            if (cache[i] == null) cache[i] = Build((MapIcon)i);
            return cache[i];
        }

        static Sprite Build(MapIcon icon)
        {
            var px = new Color32[Size * Size];
            float pixel = 2f / Size;
            var dark = new Color(0.07f, 0.08f, 0.11f);
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2((x + 0.5f) * pixel - 1f, (y + 0.5f) * pixel - 1f);
                float f, d;
                Shape(icon, p, out f, out d);
                float aOut = Mathf.Clamp01(0.5f - (f - Outline) / pixel);
                float aFill = Mathf.Clamp01(0.5f - f / pixel);
                float aDet = Mathf.Clamp01(0.5f - Mathf.Max(d, f) / pixel);   // detail only inside the shape
                Color c = Color.Lerp(Color.Lerp(dark, Color.white, aFill), dark, aDet);
                c.a = aOut;
                px[y * Size + x] = c;
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "mapicon_" + icon, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            var sp = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sp.name = "mapicon_" + icon;
            return sp;
        }

        // ================================================================ SHAPE TOOLS
        const float None = 1e9f;
        static float Circle(Vector2 p, float cx, float cy, float r) { float dx = p.x - cx, dy = p.y - cy; return Mathf.Sqrt(dx * dx + dy * dy) - r; }
        static float Ell(Vector2 p, float cx, float cy, float rx, float ry) { float dx = (p.x - cx) / rx, dy = (p.y - cy) / ry; return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry); }

        static float Box(Vector2 p, float cx, float cy, float hx, float hy, float deg = 0f)
        {
            var q = new Vector2(p.x - cx, p.y - cy);
            if (deg != 0f) q = Rot(q, deg);
            float qx = Mathf.Abs(q.x) - hx, qy = Mathf.Abs(q.y) - hy;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f);
        }

        static float Seg(Vector2 p, float ax, float ay, float bx, float by, float r)
        {
            float ex = bx - ax, ey = by - ay, wx = p.x - ax, wy = p.y - ay;
            float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
            float dx = wx - ex * t, dy = wy - ey * t;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>Polygon from x,y pairs (any winding, may be concave).</summary>
        static float Poly(Vector2 p, params float[] v)
        {
            int n = v.Length / 2;
            float d = (p.x - v[0]) * (p.x - v[0]) + (p.y - v[1]) * (p.y - v[1]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float ix = v[i * 2], iy = v[i * 2 + 1], jx = v[j * 2], jy = v[j * 2 + 1];
                float ex = jx - ix, ey = jy - iy, wx = p.x - ix, wy = p.y - iy;
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * t, by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = p.y >= iy, c2 = p.y < jy, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        /// <summary>Rotates the sample point, so the shape drawn after it appears turned by 'deg' (counter-clockwise).</summary>
        static Vector2 Rot(Vector2 p, float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(p.x * c + p.y * s, -p.x * s + p.y * c);
        }

        static float U(float a, float b) => Mathf.Min(a, b);
        static float U(float a, float b, float c) => Mathf.Min(a, Mathf.Min(b, c));
        static float U(float a, float b, float c, float d) => Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
        static float U(float a, float b, float c, float d, float e) => Mathf.Min(U(a, b, c, d), e);
        static float Cut(float a, float b) => Mathf.Max(a, -b);
        static float Both(float a, float b) => Mathf.Max(a, b);

        static float Sword(Vector2 p, float deg)
        {
            var q = Rot(p, deg);
            return U(Poly(q, -0.09f, -0.3f, -0.09f, 0.62f, 0f, 0.84f, 0.09f, 0.62f, 0.09f, -0.3f),
                     Box(q, 0f, -0.34f, 0.28f, 0.06f), Box(q, 0f, -0.56f, 0.06f, 0.18f), Circle(q, 0f, -0.76f, 0.1f));
        }

        // ================================================================ THE ICONS
        /// <summary>f: distance to the white shape (negative inside). d: distance to its dark inner detail.</summary>
        static void Shape(MapIcon icon, Vector2 p, out float f, out float d)
        {
            d = None;
            switch (icon)
            {
                case MapIcon.Camp:   // home: a house with a door
                    f = U(Poly(p, -0.82f, 0.02f, 0f, 0.82f, 0.82f, 0.02f), Box(p, 0f, -0.38f, 0.56f, 0.42f), Box(p, 0.42f, 0.5f, 0.1f, 0.24f));
                    d = Box(p, 0f, -0.52f, 0.17f, 0.28f);
                    break;
                case MapIcon.Village:   // a row of roofs
                    f = U(U(Box(p, -0.44f, -0.42f, 0.3f, 0.34f), Poly(p, -0.84f, -0.12f, -0.44f, 0.36f, -0.04f, -0.12f)),
                          U(Box(p, 0.32f, -0.28f, 0.36f, 0.48f), Poly(p, -0.16f, 0.18f, 0.32f, 0.8f, 0.8f, 0.18f)));
                    d = U(Box(p, 0.32f, -0.52f, 0.12f, 0.24f), Box(p, -0.44f, -0.4f, 0.1f, 0.1f), Box(p, 0.32f, 0.02f, 0.1f, 0.09f));
                    break;
                case MapIcon.Warden:   // a crown
                    f = U(Poly(p, -0.76f, -0.55f, -0.76f, 0.3f, -0.38f, -0.06f, 0f, 0.58f, 0.38f, -0.06f, 0.76f, 0.3f, 0.76f, -0.55f),
                          Circle(p, -0.74f, 0.38f, 0.12f), Circle(p, 0f, 0.66f, 0.14f), Circle(p, 0.74f, 0.38f, 0.12f));
                    d = U(Box(p, 0f, -0.34f, 0.8f, 0.05f), Circle(p, 0f, -0.08f, 0.1f));
                    break;
                case MapIcon.Tamer:   // crossed swords
                    f = U(Sword(p, 42f), Sword(p, -42f));
                    break;
                case MapIcon.Waystone:   // a standing stone with a rune
                    f = U(Poly(p, -0.3f, -0.7f, -0.4f, 0.34f, 0f, 0.84f, 0.4f, 0.34f, 0.3f, -0.7f), Box(p, 0f, -0.72f, 0.58f, 0.1f));
                    d = U(Poly(p, 0f, 0.42f, 0.17f, 0.16f, 0f, -0.1f, -0.17f, 0.16f), Box(p, 0f, -0.34f, 0.14f, 0.04f));
                    break;
                case MapIcon.Cave:   // a mound with a dark mouth
                    f = Both(Ell(p, 0f, -0.62f, 0.84f, 1.3f), -(p.y + 0.62f));
                    d = Both(Ell(p, 0f, -0.62f, 0.42f, 0.84f), -(p.y + 0.62f));
                    break;
                case MapIcon.Burrow:   // a low bank, a round hole, grass on top
                    f = U(Both(Ell(p, 0f, -0.5f, 0.84f, 0.74f), -(p.y + 0.5f)),
                          Poly(p, -0.34f, 0.16f, -0.24f, 0.66f, -0.06f, 0.2f), Poly(p, -0.02f, 0.2f, 0.16f, 0.74f, 0.26f, 0.18f), Poly(p, 0.3f, 0.1f, 0.5f, 0.52f, 0.52f, 0.02f));
                    d = Ell(p, 0f, -0.22f, 0.32f, 0.26f);
                    break;
                case MapIcon.Dungeon:   // a pillared doorway
                    f = U(Box(p, 0f, -0.08f, 0.62f, 0.62f), Box(p, 0f, 0.62f, 0.82f, 0.14f), Box(p, 0f, -0.74f, 0.82f, 0.08f));
                    d = U(Box(p, 0f, -0.2f, 0.3f, 0.5f), Box(p, 0f, 0.44f, 0.62f, 0.035f));
                    break;
                case MapIcon.Lookout:   // a watchtower
                    f = U(U(Poly(p, -0.56f, -0.82f, -0.3f, 0.12f, -0.16f, 0.12f, -0.36f, -0.82f), Poly(p, 0.56f, -0.82f, 0.3f, 0.12f, 0.16f, 0.12f, 0.36f, -0.82f), Box(p, 0f, -0.36f, 0.4f, 0.05f)),
                          Box(p, 0f, 0.14f, 0.54f, 0.08f), Box(p, 0f, 0.34f, 0.4f, 0.16f), Poly(p, -0.66f, 0.46f, 0f, 0.86f, 0.66f, 0.46f));
                    d = Box(p, 0f, 0.34f, 0.24f, 0.08f);
                    break;
                case MapIcon.Fish:
                    f = U(Ell(p, -0.14f, 0f, 0.6f, 0.38f), Poly(p, 0.3f, 0f, 0.82f, 0.44f, 0.82f, -0.44f), Poly(p, -0.3f, 0.3f, 0.04f, 0.62f, 0.14f, 0.26f));
                    d = U(Circle(p, -0.44f, 0.08f, 0.08f), Seg(p, -0.16f, 0.24f, -0.08f, -0.24f, 0.03f));
                    break;
                case MapIcon.Nest:   // an egg in a nest
                    f = U(Ell(p, 0f, 0.2f, 0.36f, 0.5f), Both(Ell(p, 0f, -0.2f, 0.84f, 0.56f), p.y + 0.2f));
                    d = U(U(Box(p, -0.34f, -0.44f, 0.26f, 0.035f, 20f), Box(p, 0.34f, -0.42f, 0.26f, 0.035f, -24f)), Circle(p, 0.12f, 0.42f, 0.07f), Circle(p, -0.14f, 0.2f, 0.06f), Circle(p, 0.1f, 0.02f, 0.05f));
                    break;
                case MapIcon.Lair:   // a paw print
                    f = U(Ell(p, 0f, -0.34f, 0.44f, 0.38f), Ell(p, -0.58f, 0.08f, 0.17f, 0.23f), Ell(p, -0.21f, 0.48f, 0.18f, 0.25f), Ell(p, 0.21f, 0.48f, 0.18f, 0.25f), Ell(p, 0.58f, 0.08f, 0.17f, 0.23f));
                    break;
                case MapIcon.Trader:   // a coin purse
                    f = U(Circle(p, 0f, -0.22f, 0.6f), Poly(p, -0.2f, 0.3f, -0.4f, 0.8f, 0.4f, 0.8f, 0.2f, 0.3f));
                    d = U(Box(p, 0f, 0.36f, 0.3f, 0.05f), Mathf.Abs(Circle(p, 0f, -0.24f, 0.24f)) - 0.05f);
                    break;
                case MapIcon.Relic:   // a four-point glint
                    f = Poly(p, 0f, 0.86f, 0.17f, 0.17f, 0.86f, 0f, 0.17f, -0.17f, 0f, -0.86f, -0.17f, -0.17f, -0.86f, 0f, -0.17f, 0.17f);
                    break;
                case MapIcon.Wreck:   // a heeled-over hull with a torn sail
                {
                    var q = Rot(p, -14f);
                    f = U(Poly(q, -0.84f, -0.2f, 0.84f, -0.2f, 0.52f, -0.72f, -0.52f, -0.72f), Box(q, 0f, 0.26f, 0.05f, 0.5f), Poly(q, 0.1f, 0.74f, 0.1f, 0f, 0.62f, 0f));
                    d = Box(q, 0f, -0.36f, 0.6f, 0.035f);
                    break;
                }
                case MapIcon.Bones:   // a bone
                {
                    var q = Rot(p, -40f);
                    f = U(Seg(q, -0.5f, 0f, 0.5f, 0f, 0.14f), Circle(q, -0.62f, 0.17f, 0.2f), Circle(q, -0.62f, -0.17f, 0.2f), Circle(q, 0.62f, 0.17f, 0.2f), Circle(q, 0.62f, -0.17f, 0.2f));
                    break;
                }
                case MapIcon.Tent:
                    f = U(Poly(p, -0.84f, -0.72f, 0f, 0.66f, 0.84f, -0.72f), Seg(p, 0f, 0.66f, -0.16f, 0.86f, 0.04f), Seg(p, 0f, 0.66f, 0.16f, 0.86f, 0.04f));
                    d = Poly(p, -0.22f, -0.72f, 0f, 0.04f, 0.22f, -0.72f);
                    break;
                case MapIcon.Statue:   // a figure on a plinth
                    f = U(Circle(p, 0f, 0.52f, 0.27f), Poly(p, -0.42f, -0.36f, -0.3f, 0.22f, 0.3f, 0.22f, 0.42f, -0.36f), Box(p, 0f, -0.58f, 0.62f, 0.22f));
                    d = Box(p, 0f, -0.4f, 0.62f, 0.035f);
                    break;
                case MapIcon.Pillars:   // broken columns
                    f = U(U(Box(p, -0.56f, -0.12f, 0.15f, 0.58f), Box(p, -0.56f, 0.5f, 0.22f, 0.07f)), Poly(p, -0.15f, -0.7f, -0.15f, -0.05f, 0.03f, 0.12f, 0.15f, -0.12f, 0.15f, -0.7f),
                          U(Box(p, 0.56f, 0.02f, 0.15f, 0.72f), Box(p, 0.56f, 0.76f, 0.22f, 0.07f)), Box(p, 0f, -0.76f, 0.84f, 0.07f));
                    break;
                case MapIcon.Hunter:   // a seeker's glass
                    f = U(Circle(p, -0.16f, 0.2f, 0.54f), Seg(p, 0.26f, -0.24f, 0.7f, -0.7f, 0.14f));
                    d = Circle(p, -0.16f, 0.2f, 0.34f);
                    break;
                case MapIcon.Legend:   // a five-point star
                {
                    var v = new float[20];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = (90f + i * 36f) * Mathf.Deg2Rad, r = i % 2 == 0 ? 0.86f : 0.37f;
                        v[i * 2] = Mathf.Cos(a) * r; v[i * 2 + 1] = Mathf.Sin(a) * r - 0.04f;
                    }
                    f = Poly(p, v);
                    break;
                }
                case MapIcon.Chest:   // a treasure chest: a box with a rounded lid, a band and a lock
                    f = U(Box(p, 0f, -0.3f, 0.74f, 0.36f), Both(Ell(p, 0f, 0.06f, 0.74f, 0.56f), 0.06f - p.y));
                    d = U(Box(p, 0f, 0.06f, 0.78f, 0.05f), Box(p, 0f, -0.16f, 0.12f, 0.16f));
                    break;
                case MapIcon.Quest:   // a diamond with an exclamation mark
                    f = Poly(p, 0f, 0.9f, 0.72f, 0f, 0f, -0.9f, -0.72f, 0f);
                    d = U(Box(p, 0f, 0.14f, 0.09f, 0.3f), Circle(p, 0f, -0.42f, 0.11f));
                    break;
                case MapIcon.Outpost:   // a flag on a pole
                    f = U(Box(p, -0.46f, 0f, 0.06f, 0.82f), Poly(p, -0.4f, 0.8f, 0.8f, 0.44f, -0.4f, 0.08f), Box(p, -0.46f, -0.76f, 0.3f, 0.07f));
                    break;
                case MapIcon.Crater:   // a falling star
                    f = U(Circle(p, -0.32f, -0.32f, 0.42f), Poly(p, -0.66f, -0.06f, 0.82f, 0.82f, -0.06f, -0.66f));
                    d = Circle(p, -0.32f, -0.32f, 0.17f);
                    break;
                case MapIcon.Tree:
                    f = U(Circle(p, 0f, 0.3f, 0.54f), Circle(p, -0.38f, 0.04f, 0.36f), Circle(p, 0.38f, 0.04f, 0.36f), Box(p, 0f, -0.56f, 0.13f, 0.3f));
                    break;
                case MapIcon.Stones:   // standing stones
                    f = U(Seg(p, -0.56f, -0.6f, -0.56f, 0f, 0.19f), Seg(p, 0f, -0.6f, 0f, 0.42f, 0.21f), Seg(p, 0.56f, -0.6f, 0.56f, -0.12f, 0.19f), Box(p, 0f, -0.76f, 0.84f, 0.07f));
                    break;
                case MapIcon.Arch:   // a rock arch you can see through
                    f = Cut(U(Box(p, 0f, 0.06f, 0.84f, 0.66f), Box(p, 0f, -0.72f, 0.84f, 0.1f)), Both(Ell(p, 0f, -0.62f, 0.44f, 0.94f), -(p.y + 0.62f)));
                    break;
                case MapIcon.Crystal:   // a cluster of shards
                    f = U(Poly(p, -0.14f, -0.76f, -0.3f, 0.3f, 0f, 0.86f, 0.3f, 0.3f, 0.14f, -0.76f),
                          Poly(p, -0.2f, -0.76f, -0.78f, 0f, -0.62f, 0.3f, -0.36f, 0.1f, -0.1f, -0.76f), Poly(p, 0.2f, -0.76f, 0.78f, -0.1f, 0.64f, 0.2f, 0.38f, 0f, 0.1f, -0.76f));
                    d = U(Seg(p, 0f, 0.8f, 0f, -0.3f, 0.025f), Seg(p, -0.6f, 0.24f, -0.3f, -0.4f, 0.02f));
                    break;
                default:
                    f = Circle(p, 0f, 0f, 0.52f);
                    break;
            }
        }
    }
}
