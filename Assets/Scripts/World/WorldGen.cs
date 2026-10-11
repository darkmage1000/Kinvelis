using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Pure, thread-safe world function. Height/biome/color for any XZ is computed analytically,
    /// so gameplay (grounding, spawning, placement) never needs chunk data to be loaded.
    /// World: 4096 x 4096 m island, three biomes around warped Voronoi centers.
    /// </summary>
    public static partial class WorldGen
    {
        public const float ChunkSize = 64f;
        public const float HalfWorld = 4096f;      // the original island fills the middle 4 km; the outer lands ring it
        public const int ChunksPerSide = 128;
        public const float WaterLevel = 0f;

        public static readonly Vector2 MeadowCenter = new Vector2(0f, -650f);
        public static readonly Vector2 BadlandsCenter = new Vector2(1250f, 520f);
        public static readonly Vector2 PeaksCenter = new Vector2(-1150f, 720f);

        public static int Seed { get; private set; }
        public static bool Initialized { get; private set; }

        static Noise2D nBase, nDetail, nWarpA, nWarpB, nRidge, nColor, nCanyon, nForest, nIsland;

        public static void Init(int seed)
        {
            Seed = seed;
            nBase = new Noise2D(seed + 11);
            nDetail = new Noise2D(seed + 23);
            nWarpA = new Noise2D(seed + 37);
            nWarpB = new Noise2D(seed + 41);
            nRidge = new Noise2D(seed + 53);
            nColor = new Noise2D(seed + 67);
            nCanyon = new Noise2D(seed + 79);
            nForest = new Noise2D(seed + 97);
            nIsland = new Noise2D(seed + 101);
            // the start plateau is picked on the original island alone, so it never moves when lands are added around it
            startActive = false;
            outerActive = false;
            ChooseStart();
            outerActive = true;
            startActive = true;
            Initialized = true;
            SubRegions.Init();
        }

        /// <summary>Slow -1..1 noise that roughens sub-region edges.</summary>
        public static float EdgeNoise(float x, float z) => nColor.Sample(x * 0.011f + 31.7f, z * 0.011f - 12.3f);

        // ---------------------------------------------------------------- BIOMES
        public static void Weights(float x, float z, out float wM, out float wB, out float wP)
        {
            float wx = x + nWarpA.Fbm(x * 0.0012f, z * 0.0012f, 3) * 320f;
            float wz = z + nWarpB.Fbm(x * 0.0012f, z * 0.0012f, 3) * 320f;
            float dM = Dist(wx, wz, MeadowCenter) * 0.92f; // meadow slightly favoured (start biome)
            float dB = Dist(wx, wz, BadlandsCenter);
            float dP = Dist(wx, wz, PeaksCenter);
            float dMin = Mathf.Min(dM, Mathf.Min(dB, dP));
            const float blend = 70f;
            wM = Mathf.Exp(-(dM - dMin) / blend);
            wB = Mathf.Exp(-(dB - dMin) / blend);
            wP = Mathf.Exp(-(dP - dMin) / blend);
            float s = 1f / (wM + wB + wP);
            wM *= s; wB *= s; wP *= s;
        }

        static float Dist(float x, float z, Vector2 c)
        {
            float dx = x - c.x, dz = z - c.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static BiomeId Biome(float x, float z)
        {
            Weights(x, z, out float m, out float b, out float p);
            if (x * x + z * z >= OuterMinSq && Outer(x, z, out var ob, out float ow, out float oh) && ow > 0.2f && oh >= MainHeightW(x, z, m, b, p) - 0.05f) return ob;
            if (m >= b && m >= p) return BiomeId.Meadow;
            return b >= p ? BiomeId.Badlands : BiomeId.Peaks;
        }

        // ---------------------------------------------------------------- HEIGHT
        public static float Height(float x, float z)
        {
            Weights(x, z, out float wM, out float wB, out float wP);
            return HeightW(x, z, wM, wB, wP);
        }

        public static float HeightW(float x, float z, float wM, float wB, float wP)
        {
            float h = MainHeightW(x, z, wM, wB, wP);
            // outer lands only ever add ground where they stand higher than what was there (open sea)
            if (x * x + z * z >= OuterMinSq && Outer(x, z, out _, out _, out float oh) && oh > h) h = oh;
            return startActive ? ApplyStart(x, z, h) : h;
        }

        /// <summary>The original island on its own (no outer lands, no start plateau).</summary>
        static float MainHeightW(float x, float z, float wM, float wB, float wP)
        {
            float h = 0f;
            if (wM > 0.002f) h += wM * MeadowHeight(x, z);
            if (wB > 0.002f) h += wB * BadlandsHeight(x, z);
            if (wP > 0.002f) h += wP * PeaksHeight(x, z);

            // island falloff into the ocean
            float r = Mathf.Sqrt(x * x + z * z) + nIsland.Fbm(x * 0.002f, z * 0.002f, 2) * 160f;
            float mask = 1f - Smooth(1650f, 1960f, r);
            return Mathf.Lerp(-28f, h, mask);
        }

        // ---------------------------------------------------------------- START PLATEAU
        // New explorers begin on a raised coastal plateau (ocean behind, boulder rim on the sides). The only way off
        // is a 4 m drop at the front onto a landing shelf - too steep to climb back. The Dawnfield Waystone waits
        // on the shelf, right where you truly enter the world.
        public const float StartR = 24f;
        const float StartCliff = 2.5f, StartDrop = 4f, ShelfLen = 18f, ShelfFade = 32f;
        static bool startActive;
        public static Vector2 StartCenter { get; private set; }
        /// <summary>Unit XZ direction from the plateau toward the exit (inland).</summary>
        public static Vector2 StartFwd { get; private set; }
        public static float StartTop { get; private set; }
        /// <summary>Radius around the start where wild kin are gentle (lv 1-3, no packs, no aggressive species).</summary>
        public const float StarterZone = 420f;

        public static float StartDistance(float x, float z) { float dx = x - StartCenter.x, dz = z - StartCenter.y; return Mathf.Sqrt(dx * dx + dz * dz); }
        public static bool OnStartPlateau(Vector3 p) => StartDistance(p.x, p.z) < StartR + StartCliff;

        /// <summary>Player start point: back half of the plateau, facing the exit.</summary>
        public static Vector3 StartSpawn
        {
            get
            {
                var c = StartCenter - StartFwd * 8f;
                return new Vector3(c.x, Height(c.x, c.y), c.y);
            }
        }

        /// <summary>Center of the landing shelf below the exit (Dawnfield Waystone / respawn).</summary>
        public static Vector3 StartExit
        {
            get
            {
                var c = StartCenter + StartFwd * (StartR + 11f);
                return new Vector3(c.x, Height(c.x, c.y), c.y);
            }
        }

        public static float StartYaw => Mathf.Atan2(StartFwd.x, StartFwd.y) * Mathf.Rad2Deg;

        /// <summary>Picks a dry, gentle meadow spot near the south coast with open sea behind it.</summary>
        static void ChooseStart()
        {
            Vector2 best = new Vector2(MeadowCenter.x, MeadowCenter.y - 600f);
            float bestScore = float.MaxValue;
            for (int iz = 0; iz < 24; iz++)
            for (int ix = -8; ix <= 8; ix++)
            {
                float x = MeadowCenter.x + ix * 30f, z = MeadowCenter.y - 450f - iz * 40f;
                float h = Height(x, z);
                if (h < 3f || h > 18f) continue;
                if (Height(x, z - 220f) > -1f) continue;            // needs the sea behind (south)
                float n = Normal(x, z, 4f).y;
                if (n < 0.9f) continue;
                float score = Mathf.Abs(ix) * 12f + (1f - n) * 400f + Mathf.Abs(h - 8f) * 5f;
                if (score < bestScore) { bestScore = score; best = new Vector2(x, z); }
            }
            StartCenter = best;
            var fwd = MeadowCenter - best;
            StartFwd = fwd.sqrMagnitude > 1f ? fwd.normalized : Vector2.up;
            // plateau top stands clear of all the ground around it
            float maxRing = Height(best.x, best.y);
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                for (float r = 0f; r <= StartR + ShelfLen; r += 6f)
                    maxRing = Mathf.Max(maxRing, Height(best.x + Mathf.Cos(a) * r, best.y + Mathf.Sin(a) * r));
            }
            StartTop = maxRing + StartDrop + 3.5f;
        }

        static float ApplyStart(float x, float z, float h)
        {
            float dx = x - StartCenter.x, dz = z - StartCenter.y;
            float d2 = dx * dx + dz * dz;
            float outer = StartR + ShelfLen + ShelfFade;
            if (d2 > outer * outer) return h;
            float d = Mathf.Sqrt(d2);
            if (d <= StartR) return Mathf.Max(h, StartTop);

            // landing shelf in front of the exit
            float cosA = d > 0.01f ? (dx * StartFwd.x + dz * StartFwd.y) / d : 1f;
            float sector = Smooth(0.8f, 0.88f, cosA);
            float shelfTop = StartTop - StartDrop;
            float shelf = d < StartR + ShelfLen ? shelfTop : Mathf.Lerp(shelfTop, h, Smooth(StartR + ShelfLen, outer, d));
            float floor = Mathf.Lerp(h, Mathf.Max(h, shelf), sector);

            // steep cliff from the plateau edge
            if (d < StartR + StartCliff) return Mathf.Lerp(StartTop, floor, Smooth(StartR, StartR + StartCliff, d));
            return floor;
        }

        static float MeadowHeight(float x, float z)
        {
            float broad = nDetail.Fbm(x * 0.0009f, z * 0.0009f, 3);
            float hills = nBase.Fbm(x * 0.0035f, z * 0.0035f, 4);
            return 6f + hills * 9f + broad * 17f;
        }

        static float BadlandsHeight(float x, float z)
        {
            float b = 16f + nBase.Fbm(x * 0.0022f + 50f, z * 0.0022f, 4) * 28f;
            // terraced mesas: sharp cliff between flat steps
            const float step = 9f;
            float f = b / step;
            float fl = Mathf.Floor(f);
            b = (fl + Smooth(0.42f, 0.62f, f - fl)) * step;
            // winding canyons
            float c = Mathf.Abs(nCanyon.Fbm(x * 0.0016f, z * 0.0016f, 2));
            if (c < 0.07f) { float k = 1f - c / 0.07f; b -= k * k * 20f; }
            return b + nDetail.Sample(x * 0.02f, z * 0.02f) * 1.2f;
        }

        static float PeaksHeight(float x, float z)
        {
            float ridge = nRidge.Ridged(x * 0.0018f, z * 0.0018f, 5);
            float lift = Mathf.Clamp01((nBase.Fbm(x * 0.0007f + 9f, z * 0.0007f, 2) + 0.6f));
            return 24f + ridge * 115f * lift + nDetail.Fbm(x * 0.01f, z * 0.01f, 2) * 3f;
        }

        public static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        public static Vector3 Normal(float x, float z, float e = 1f)
        {
            float hl = Height(x - e, z), hr = Height(x + e, z);
            float hd = Height(x, z - e), hu = Height(x, z + e);
            return new Vector3(hl - hr, 2f * e, hd - hu).normalized;
        }

        /// <summary>0..1 forest density (clusters of trees in the meadow).</summary>
        public static float Forest(float x, float z) => Mathf.Clamp01(nForest.Fbm(x * 0.006f, z * 0.006f, 2) * 1.6f + 0.25f);

        public static bool InsideWorld(float x, float z) => Mathf.Abs(x) < HalfWorld - 8f && Mathf.Abs(z) < HalfWorld - 8f;

        // ---------------------------------------------------------------- COLOR
        static readonly Color[] BadBands =
        {
            new Color(0.86f, 0.46f, 0.28f), new Color(0.94f, 0.64f, 0.4f),
            new Color(0.74f, 0.34f, 0.22f), new Color(0.96f, 0.77f, 0.52f), new Color(0.82f, 0.52f, 0.36f)
        };

        /// <summary>Ground colour: the biome's own, then the tint of any named sub-region the point lies in.</summary>
        /// <summary>sample = spacing of the vertices / pixels being coloured, so a trail is drawn wide enough not to fall between them.</summary>
        public static Color32 TerrainColor(float x, float z, float h, float ny, float wM, float wB, float wP, float sample = 2f)
        {
            float cn = nColor.Sample(x * 0.018f, z * 0.018f);
            Color col;
            BiomeId b;
            float bw = 1f;
            if (x * x + z * z >= OuterMinSq && Outer(x, z, out var ob, out float ow, out float oh) && ow > 0.02f && oh >= h - 0.05f)
            {
                col = OuterColor(ob, x, z, h, ny, cn);
                b = ob;
            }
            else
            {
                col = IslandColor(x, z, h, ny, wM, wB, wP, cn);
                if (wM >= wB && wM >= wP) { b = BiomeId.Meadow; bw = wM; }
                else if (wB >= wP) { b = BiomeId.Badlands; bw = wB; }
                else { b = BiomeId.Peaks; bw = wP; }
                bw = Smooth(0.5f, 0.8f, bw);   // fade out across biome borders
            }
            int si = SubRegions.At(x, z, b, out float sw);
            if (si >= 0) col = SubRegions.Tint(si, col, sw * bw, h, ny, cn);
            return Trails.Tint(x, z, col, h, ny, cn, sample);   // worn paths between the settlements
        }

        static Color IslandColor(float x, float z, float h, float ny, float wM, float wB, float wP, float cn)
        {
            Color col = Color.black;

            if (wM > 0.002f)
            {
                Color g = Color.Lerp(new Color(0.36f, 0.68f, 0.28f), new Color(0.56f, 0.82f, 0.34f), cn * 0.5f + 0.5f);
                float fr = Hash.F(Mathf.RoundToInt(x * 0.5f), Mathf.RoundToInt(z * 0.5f), Seed);
                if (fr < 0.03f)
                {
                    // subtle meadow speckle (real flowers are decor props)
                    Color flower = fr < 0.015f ? new Color(1f, 0.7f, 0.82f) : new Color(1f, 0.9f, 0.5f);
                    g = Color.Lerp(g, flower, 0.18f);
                }
                if (ny < 0.8f) g = Color.Lerp(g, new Color(0.55f, 0.43f, 0.3f), Smooth(0.8f, 0.7f, ny));
                if (ny < 0.66f) g = Color.Lerp(g, new Color(0.58f, 0.57f, 0.55f), Smooth(0.66f, 0.55f, ny));
                col += g * wM;
            }
            if (wB > 0.002f)
            {
                float t = h / 9f + cn * 0.15f;
                t -= Mathf.Floor(t);
                Color bc = BadBands[Mathf.Clamp((int)(t * BadBands.Length), 0, BadBands.Length - 1)];
                if (ny > 0.93f) bc = Color.Lerp(bc, new Color(0.92f, 0.7f, 0.48f), 0.6f);
                if (h < 4f) bc = Color.Lerp(bc, new Color(0.95f, 0.83f, 0.62f), Smooth(4f, 1f, h));
                col += bc * wB;
            }
            if (wP > 0.002f)
            {
                Color pc = new Color(0.5f, 0.54f, 0.62f);
                if (h < 34f && ny > 0.8f) pc = Color.Lerp(pc, new Color(0.56f, 0.7f, 0.62f), 0.7f);
                float snowLine = 38f - cn * 10f;
                float snow = Smooth(snowLine - 6f, snowLine + 4f, h) * Smooth(0.62f, 0.8f, ny);
                snow = Mathf.Max(snow, Smooth(0.9f, 0.97f, ny) * 0.8f);
                pc = Color.Lerp(pc, new Color(0.93f, 0.96f, 1f), snow);
                col += pc * wP;
            }

            // beaches & lake beds
            if (h < 1.4f) col = Color.Lerp(col, new Color(0.92f, 0.86f, 0.64f), Smooth(1.4f, 0.4f, h) * 0.85f);
            if (h < -1f) col = Color.Lerp(col, new Color(0.55f, 0.62f, 0.55f), Smooth(-1f, -6f, h));
            return col;
        }

        // ---------------------------------------------------------------- SPAWN
        /// <summary>Where new explorers start (the start plateau). Old-landmark anchoring uses LegacySpawnPoint.</summary>
        public static Vector3 FindSpawnPoint() => StartSpawn;

        /// <summary>Finds dry, gentle ground near the meadow center (the original spawn; landmarks are anchored to it).</summary>
        public static Vector3 LegacySpawnPoint()
        {
            for (int ring = 0; ring < 40; ring++)
            {
                for (int i = 0; i < 16; i++)
                {
                    float a = i / 16f * Mathf.PI * 2f;
                    float x = MeadowCenter.x + Mathf.Cos(a) * ring * 12f;
                    float z = MeadowCenter.y - 40f + Mathf.Sin(a) * ring * 12f;
                    float h = Height(x, z);
                    if (h > 2.5f && Normal(x, z).y > 0.93f) return new Vector3(x, h, z);
                }
            }
            return new Vector3(MeadowCenter.x, Height(MeadowCenter.x, MeadowCenter.y), MeadowCenter.y);
        }
    }
}
