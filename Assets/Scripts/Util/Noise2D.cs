namespace Wildbond
{
    /// <summary>Seeded 2D simplex noise with fbm / ridged helpers. Immutable after construction => thread-safe.</summary>
    public sealed class Noise2D
    {
        readonly byte[] perm = new byte[512];

        static readonly float[] GX = { 1f, -1f, 1f, -1f, 1f, -1f, 0f, 0f };
        static readonly float[] GY = { 1f, 1f, -1f, -1f, 0f, 0f, 1f, -1f };
        const float F2 = 0.36602540378f;
        const float G2 = 0.21132486540f;

        public Noise2D(int seed)
        {
            var p = new byte[256];
            for (int i = 0; i < 256; i++) p[i] = (byte)i;
            var r = new System.Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }
            for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
        }

        static int FastFloor(float x) { int i = (int)x; return x < i ? i - 1 : i; }

        /// <summary>Simplex noise in roughly [-1, 1].</summary>
        public float Sample(float x, float y)
        {
            float s = (x + y) * F2;
            int i = FastFloor(x + s);
            int j = FastFloor(y + s);
            float t = (i + j) * G2;
            float x0 = x - (i - t);
            float y0 = y - (j - t);
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; } else { i1 = 0; j1 = 1; }
            float x1 = x0 - i1 + G2, y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2, y2 = y0 - 1f + 2f * G2;
            int ii = i & 255, jj = j & 255;

            float n = 0f;
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 > 0f) { int g = perm[ii + perm[jj]] & 7; t0 *= t0; n += t0 * t0 * (GX[g] * x0 + GY[g] * y0); }
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 > 0f) { int g = perm[ii + i1 + perm[jj + j1]] & 7; t1 *= t1; n += t1 * t1 * (GX[g] * x1 + GY[g] * y1); }
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 > 0f) { int g = perm[ii + 1 + perm[jj + 1]] & 7; t2 *= t2; n += t2 * t2 * (GX[g] * x2 + GY[g] * y2); }
            float v = 70f * n;
            return v < -1f ? -1f : (v > 1f ? 1f : v);
        }

        /// <summary>Fractal sum, output roughly [-1, 1].</summary>
        public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f, f = 1f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Sample(x * f + o * 17.13f, y * f - o * 9.71f) * amp;
                norm += amp;
                amp *= gain;
                f *= lacunarity;
            }
            return sum / norm;
        }

        /// <summary>Ridged multifractal, output [0, 1]. Sharp crests - good for mountains.</summary>
        public float Ridged(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f, f = 1f, weight = 1f;
            for (int o = 0; o < octaves; o++)
            {
                float n = 1f - System.Math.Abs(Sample(x * f + o * 31.7f, y * f + o * 11.3f));
                n *= n;
                n *= weight;
                weight = n * 1.5f;
                if (weight > 1f) weight = 1f;
                sum += n * amp;
                norm += amp;
                amp *= gain;
                f *= lacunarity;
            }
            return sum / norm;
        }
    }
}
