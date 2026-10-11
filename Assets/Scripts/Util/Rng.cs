using UnityEngine;

namespace Wildbond
{
    /// <summary>Deterministic integer hashing. Thread-safe, allocation-free.</summary>
    public static class Hash
    {
        public static uint U(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u + 0x7F4A7C15u;
                h ^= (uint)x * 0x85EBCA77u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE3Du;
                h *= 0x27D4EB2Fu;
                h ^= h >> 15;
                h *= 0x85EBCA77u;
                h ^= h >> 13;
                h *= 0xC2B2AE3Du;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>0..1 float from three ints.</summary>
        public static float F(int x, int y, int seed) => (U(x, y, seed) & 0xFFFFFF) / 16777216f;
    }

    /// <summary>Small xorshift RNG. Use a fresh instance per thread / per chunk.</summary>
    public sealed class Rng
    {
        uint s;
        public Rng(uint seed) { s = seed == 0 ? 0xA341316Cu : seed; }
        public Rng(int a, int b, int seed) : this(Hash.U(a, b, seed)) { }

        public uint Next()
        {
            unchecked
            {
                s ^= s << 13;
                s ^= s >> 17;
                s ^= s << 5;
                return s;
            }
        }

        public float Value => (Next() & 0xFFFFFF) / 16777216f;
        public float Range(float a, float b) => a + (b - a) * Value;
        /// <summary>Integer in [a, b).</summary>
        public int Range(int a, int b) => b <= a ? a : a + (int)(Next() % (uint)(b - a));
        public bool Chance(float p) => Value < p;
        public Vector2 InsideCircle(float r)
        {
            float a = Value * Mathf.PI * 2f;
            float d = Mathf.Sqrt(Value) * r;
            return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }
    }
}
