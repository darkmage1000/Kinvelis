using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The outer lands: five regions in the sea around the original island, added without touching it.
    /// Nothing here is evaluated inside <see cref="OuterMinR"/> of the origin, and outer ground only ever wins by being
    /// higher than what was already there (which, off the old coast, is open sea) - so every existing height, landmark,
    /// relic, waystone and camp stays exactly where it was. There are no land bridges: you cross the strait by
    /// surfing on a water kin (PlayerController.Riding), then attune the land's waystone to come back quickly.
    /// </summary>
    public static partial class WorldGen
    {
        public struct OuterLand
        {
            public BiomeId biome;
            public Vector2 center;
            public float radius;
            /// <summary>The crossing: 'from' on the old coast, 'to' just inside the new land's near shore.</summary>
            public Vector2 from, to;
        }

        public const float OuterMinR = 1600f;
        const float OuterMinSq = OuterMinR * OuterMinR;
        const float Sea = -28f;
        static bool outerActive = true;

        public static readonly OuterLand[] Lands =
        {
            Land(BiomeId.Isles, -1700f, -2500f, 1000f),
            Land(BiomeId.Swamp, 2650f, -900f, 800f),
            Land(BiomeId.Desert, 2300f, 2150f, 900f),
            Land(BiomeId.Blight, 250f, 3050f, 850f),
            Land(BiomeId.Glow, -2750f, 1250f, 850f),
        };

        /// <summary>A lone flat-topped islet in the open sea south-east of the old island: the Kraken's arena.</summary>
        public static readonly Vector2 KrakenIsle = new Vector2(1250f, -3050f);
        public const float KrakenIsleR = 95f;

        static OuterLand Land(BiomeId b, float cx, float cz, float radius)
        {
            var c = new Vector2(cx, cz);
            var dir = c.normalized;
            return new OuterLand { biome = b, center = c, radius = radius, from = dir * 1700f, to = c - dir * (radius - 300f) };
        }

        public static bool TryLand(BiomeId b, out OuterLand land)
        {
            for (int i = 0; i < Lands.Length; i++) if (Lands[i].biome == b) { land = Lands[i]; return true; }
            land = default;
            return false;
        }

        /// <summary>Where you make landfall coming from the old island (its waystone stands here).</summary>
        public static Vector3 LandGate(OuterLand l)
        {
            var p = l.to + (l.center - l.to).normalized * 60f;
            return new Vector3(p.x, 0f, p.y);
        }

        /// <summary>
        /// Outer land at this point, if any: which biome, how strongly (0..1, 1 = well inside), and its ground height.
        /// False (and sea level) anywhere the outer lands don't reach.
        /// </summary>
        public static bool Outer(float x, float z, out BiomeId biome, out float w, out float h)
        {
            biome = BiomeId.Meadow; w = 0f; h = Sea;
            if (!outerActive || x * x + z * z < OuterMinSq) return false;
            bool any = false;
            for (int i = 0; i < Lands.Length; i++)
            {
                var L = Lands[i];
                float dx = x - L.center.x, dz = z - L.center.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > L.radius + 200f) continue;
                float edge = L.radius + nIsland.Fbm(x * 0.003f + 31f, z * 0.003f, 2) * 150f;
                float m = 1f - Smooth(edge - 230f, edge, d);
                if (m <= 0f) continue;
                float hh = Mathf.Lerp(Sea, LandHeight(L, x, z), m);
                if (hh > h)
                {
                    h = hh; biome = L.biome; w = m; any = true;
                }
            }
            {
                float dx = x - KrakenIsle.x, dz = z - KrakenIsle.y;
                float d2 = dx * dx + dz * dz;
                if (d2 < (KrakenIsleR + 40f) * (KrakenIsleR + 40f))
                {
                    float d = Mathf.Sqrt(d2) + nDetail.Sample(x * 0.03f, z * 0.03f) * 6f;
                    float m = 1f - Smooth(KrakenIsleR - 55f, KrakenIsleR, d);
                    float hh = Mathf.Lerp(Sea, 3.4f + nDetail.Sample(x * 0.05f + 9f, z * 0.05f) * 0.25f, m);
                    if (m > 0f && hh > h) { h = hh; biome = BiomeId.Isles; w = m; any = true; }
                }
            }
            return any && w > 0f;
        }

        /// <summary>True when an outer land (not the old island) is the ground at this point, given the final height there.</summary>
        public static bool OuterBiome(float x, float z, float h, out BiomeId biome)
        {
            if (Outer(x, z, out biome, out float w, out float oh) && w > 0.2f && oh >= h - 0.05f) return true;
            biome = BiomeId.Meadow;
            return false;
        }

        // ---------------------------------------------------------------- HEIGHTS
        static float LandHeight(OuterLand L, float x, float z)
        {
            switch (L.biome)
            {
                case BiomeId.Isles: return IslesHeight(x, z);
                case BiomeId.Swamp: return SwampHeight(x, z);
                case BiomeId.Desert: return DesertHeight(x, z, L);
                case BiomeId.Blight: return BlightHeight(x, z);
                default: return GlowHeight(x, z);
            }
        }

        /// <summary>A warm shallow lagoon you can wade through, dotted with islets on a jittered grid.</summary>
        static float IslesHeight(float x, float z)
        {
            float floor = -0.75f + nBase.Fbm(x * 0.006f + 120f, z * 0.006f, 2) * 1.2f;
            const float cell = 230f;
            int cx = Mathf.FloorToInt(x / cell), cz = Mathf.FloorToInt(z / cell);
            float best = floor;
            for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int gx = cx + i, gz = cz + j;
                if (Hash.F(gx, gz, Seed + 501) > 0.74f) continue;
                float ix = (gx + 0.5f + (Hash.F(gx, gz, Seed + 502) - 0.5f) * 0.6f) * cell;
                float iz = (gz + 0.5f + (Hash.F(gx, gz, Seed + 503) - 0.5f) * 0.6f) * cell;
                float r = 46f + Hash.F(gx, gz, Seed + 504) * 62f;
                float peak = 4.5f + Hash.F(gx, gz, Seed + 505) * 9f;
                float dx = x - ix, dz = z - iz;
                float d = Mathf.Sqrt(dx * dx + dz * dz) + nDetail.Sample(x * 0.03f, z * 0.03f) * 7f;
                if (d >= r) continue;
                float k = 1f - d / r;
                float dome = floor + (peak - floor) * k * k * (3f - 2f * k);
                if (dome > best) best = dome;
            }
            return best;
        }

        /// <summary>Low, flat and wet: mud flats barely above the water, pools and slow channels everywhere.</summary>
        static float SwampHeight(float x, float z)
        {
            float h = 0.55f + nBase.Fbm(x * 0.005f + 200f, z * 0.005f, 3) * 3.2f + nDetail.Fbm(x * 0.02f, z * 0.02f, 2) * 0.45f;
            float c = Mathf.Abs(nCanyon.Fbm(x * 0.0025f + 70f, z * 0.0025f, 2));
            if (c < 0.07f) { float k = 1f - c / 0.07f; h -= k * 2.4f; }
            return Mathf.Max(h, -1.6f);
        }

        /// <summary>Long ridged dunes over a broad swell, with one oasis.</summary>
        static float DesertHeight(float x, float z, OuterLand L)
        {
            float dunes = nRidge.Ridged(x * 0.0042f + 300f, z * 0.0023f, 3);
            float h = 6f + dunes * 15f + nBase.Fbm(x * 0.0012f + 90f, z * 0.0012f, 2) * 8f;
            float ox = x - (L.center.x + 170f), oz = z - (L.center.y + 110f);
            float od = Mathf.Sqrt(ox * ox + oz * oz);
            if (od < 150f) h = Mathf.Lerp(-1.7f, h, Smooth(55f, 150f, od));
            return Mathf.Max(h, -1.7f);
        }

        /// <summary>Sickly hills pocked with sink-pits that fill with toxic water.</summary>
        static float BlightHeight(float x, float z)
        {
            float h = 9f + nBase.Fbm(x * 0.003f + 400f, z * 0.003f, 4) * 14f;
            float p = nCanyon.Fbm(x * 0.006f + 33f, z * 0.006f, 2);
            if (p > 0.16f) h -= Smooth(0.16f, 0.42f, p) * 22f;
            return Mathf.Max(h, -1.8f) + nDetail.Sample(x * 0.025f, z * 0.025f) * 0.6f;
        }

        /// <summary>Rolling forest floor under glowing spires, cut by a few bright streams.</summary>
        static float GlowHeight(float x, float z)
        {
            float h = 9f + nBase.Fbm(x * 0.0028f + 700f, z * 0.0028f, 4) * 12f;
            float sp = nRidge.Ridged(x * 0.006f + 50f, z * 0.006f, 3);
            h += sp * sp * sp * 34f;
            float c = Mathf.Abs(nCanyon.Fbm(x * 0.002f + 210f, z * 0.002f, 2));
            if (c < 0.035f) { float k = 1f - c / 0.035f; h = Mathf.Lerp(h, -1.2f, k * k); }
            return h;
        }

        // ---------------------------------------------------------------- COLORS
        static Color Hx(float r, float g, float b) => new Color(r, g, b);

        static Color32 OuterColor(BiomeId b, float x, float z, float h, float ny, float cn)
        {
            float t = cn * 0.5f + 0.5f;
            Color col, bed;
            switch (b)
            {
                case BiomeId.Isles:
                    col = Color.Lerp(Hx(0.38f, 0.74f, 0.36f), Hx(0.56f, 0.84f, 0.4f), t);
                    if (h < 3.2f) col = Color.Lerp(Hx(0.96f, 0.9f, 0.68f), col, Smooth(1.6f, 3.2f, h));
                    if (ny < 0.75f) col = Color.Lerp(col, Hx(0.62f, 0.58f, 0.5f), Smooth(0.75f, 0.6f, ny));
                    bed = Hx(0.86f, 0.9f, 0.72f);
                    break;
                case BiomeId.Swamp:
                    col = Color.Lerp(Hx(0.25f, 0.4f, 0.22f), Hx(0.38f, 0.48f, 0.24f), t);
                    if (h < 1.1f) col = Color.Lerp(Hx(0.3f, 0.26f, 0.18f), col, Smooth(0.2f, 1.1f, h));
                    if (ny < 0.8f) col = Color.Lerp(col, Hx(0.34f, 0.3f, 0.22f), Smooth(0.8f, 0.65f, ny));
                    bed = Hx(0.2f, 0.26f, 0.16f);
                    break;
                case BiomeId.Desert:
                    col = Color.Lerp(Hx(0.82f, 0.67f, 0.42f), Hx(0.9f, 0.78f, 0.52f), t);
                    col = Color.Lerp(col * 0.78f, col, Smooth(0.8f, 0.985f, ny));        // shaded dune faces
                    if (ny < 0.7f) col = Color.Lerp(col, Hx(0.72f, 0.52f, 0.34f), Smooth(0.7f, 0.55f, ny));
                    if (h < 1.5f) col = Color.Lerp(Hx(0.5f, 0.66f, 0.36f), col, Smooth(0.2f, 1.5f, h));   // oasis green
                    bed = Hx(0.8f, 0.78f, 0.56f);
                    break;
                case BiomeId.Blight:
                {
                    col = Color.Lerp(Hx(0.32f, 0.26f, 0.38f), Hx(0.42f, 0.36f, 0.46f), t);
                    float patch = nColor.Sample(x * 0.006f + 40f, z * 0.006f);
                    if (patch > 0.15f) col = Color.Lerp(col, Hx(0.46f, 0.58f, 0.22f), Smooth(0.15f, 0.5f, patch) * 0.7f);
                    if (ny < 0.8f) col = Color.Lerp(col, Hx(0.24f, 0.2f, 0.3f), Smooth(0.8f, 0.62f, ny));
                    if (h < 1.2f) col = Color.Lerp(Hx(0.5f, 0.7f, 0.18f), col, Smooth(0f, 1.2f, h));     // slime-ringed pits
                    bed = Hx(0.48f, 0.72f, 0.16f);
                    break;
                }
                default: // Glow
                {
                    col = Color.Lerp(Hx(0.1f, 0.26f, 0.32f), Hx(0.14f, 0.36f, 0.38f), t);
                    float fr = Hash.F(Mathf.RoundToInt(x * 0.5f), Mathf.RoundToInt(z * 0.5f), Seed + 77);
                    if (fr < 0.06f) col = Color.Lerp(col, fr < 0.03f ? Hx(0.45f, 1f, 0.9f) : Hx(0.9f, 0.5f, 1f), 0.3f);   // glowing moss
                    if (ny < 0.75f) col = Color.Lerp(col, Hx(0.2f, 0.24f, 0.36f), Smooth(0.75f, 0.58f, ny));
                    if (h < 1f) col = Color.Lerp(Hx(0.3f, 0.9f, 0.85f), col, Smooth(0f, 1f, h));
                    bed = Hx(0.25f, 0.85f, 0.8f);
                    break;
                }
            }
            if (h < 0f) col = Color.Lerp(col, bed, Smooth(0f, -1.2f, h));
            if (h < -3f) col = Color.Lerp(col, Hx(0.55f, 0.62f, 0.55f), Smooth(-3f, -9f, h));
            return col;
        }
    }
}
