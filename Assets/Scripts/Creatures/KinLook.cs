using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What makes one kin look different from the next of its kind: overall size, how stocky it is, the size of its
    /// head, tail and wings, a tint over its whole coat and a marking. Rolled when a kin is made (CreatureFactory),
    /// passed down and mixed when two kin have an egg (KinGrowth), and for kin from older saves rolled once from their
    /// uid so they always come out the same. Looks only: none of it changes stats, hit size or riding.
    /// It costs no extra meshes - proportions are transform scales on the rig, the tint is one shader value per kin
    /// (_Coat), and a marking is a small overlay mesh shared by every kin of that kind with that marking.
    /// </summary>
    [Serializable]
    public class KinLook
    {
        public bool rolled;
        public float size = 1f, girth = 1f, head = 1f, tail = 1f, wing = 1f;
        public float hue, sat = 1f, val = 1f;
        public int mark;

        public const int Marks = 5;   // 0 none, 1 dark spots, 2 pale spots, 3 back stripe, 4 forehead star
        static readonly string[] MarkNames = { "", "dark spots", "pale spots", "a stripe down its back", "a star on its forehead" };

        public Vector3 BodyScale => new Vector3(size * girth, size, size * girth);
        /// <summary>The shader's _Coat value: hue turn, saturation, brightness.</summary>
        public Vector4 Coat => new Vector4(hue, sat, val, 0f);
        public bool PlainCoat => Mathf.Abs(hue) < 0.001f && Mathf.Abs(sat - 1f) < 0.001f && Mathf.Abs(val - 1f) < 0.001f;

        // two rolls averaged: most kin are near the middle, a few are striking
        static float Bell(System.Random r, float lo, float hi) => Mathf.Lerp(lo, hi, (float)(r.NextDouble() + r.NextDouble()) * 0.5f);

        public static KinLook Roll(System.Random r)
        {
            var l = new KinLook
            {
                rolled = true,
                size = Bell(r, 0.85f, 1.16f),
                girth = Bell(r, 0.92f, 1.1f),
                head = Bell(r, 0.88f, 1.15f),
                tail = Bell(r, 0.7f, 1.45f),
                wing = Bell(r, 0.82f, 1.25f),
                hue = Bell(r, -0.04f, 0.04f),
                sat = Bell(r, 0.75f, 1.25f),
                val = Bell(r, 0.86f, 1.1f),
            };
            double m = r.NextDouble();
            l.mark = m < 0.42 ? 0 : m < 0.58 ? 1 : m < 0.71 ? 2 : m < 0.86 ? 3 : 4;
            return l;
        }

        public static KinLook Roll() => Roll(new System.Random(UnityEngine.Random.Range(1, int.MaxValue)));

        /// <summary>The same look every time for the same kin (kin caught before looks existed).</summary>
        public static KinLook FromUid(string uid)
        {
            int h = 17;
            if (uid != null) foreach (char c in uid) h = unchecked(h * 31 + c);
            return Roll(new System.Random(h));
        }

        /// <summary>
        /// An egg's look: each feature sits somewhere between the parents' (leaning to one or the other) with a small
        /// change of its own, and the marking is one parent's - or, now and then, something neither has.
        /// </summary>
        public static KinLook Inherit(KinLook a, KinLook b)
        {
            if (a == null || !a.rolled) a = Roll();
            if (b == null || !b.rolled) b = Roll();
            float Mix(float x, float y, float lo, float hi, float drift) =>
                Mathf.Clamp(Mathf.Lerp(x, y, UnityEngine.Random.value) + UnityEngine.Random.Range(-drift, drift), lo, hi);
            var l = new KinLook
            {
                rolled = true,
                size = Mix(a.size, b.size, 0.86f, 1.16f, 0.025f),
                girth = Mix(a.girth, b.girth, 0.9f, 1.12f, 0.02f),
                head = Mix(a.head, b.head, 0.86f, 1.18f, 0.025f),
                tail = Mix(a.tail, b.tail, 0.65f, 1.5f, 0.06f),
                wing = Mix(a.wing, b.wing, 0.8f, 1.3f, 0.04f),
                hue = Mix(a.hue, b.hue, -0.07f, 0.07f, 0.008f),
                sat = Mix(a.sat, b.sat, 0.75f, 1.25f, 0.03f),
                val = Mix(a.val, b.val, 0.82f, 1.12f, 0.02f),
            };
            float m = UnityEngine.Random.value;
            l.mark = m < 0.45f ? a.mark : m < 0.9f ? b.mark : UnityEngine.Random.Range(0, Marks);
            return l;
        }

        /// <summary>A few words for the Kin tab: only what stands out, so most kin get a short line.</summary>
        public string Describe(SpeciesDef sp)
        {
            var w = new List<string>(5);
            if (size > 1.07f) w.Add("big"); else if (size < 0.94f) w.Add("small");
            if (girth > 1.055f) w.Add("stocky"); else if (girth < 0.955f) w.Add("slim");
            if (head > 1.09f) w.Add("big-headed"); else if (head < 0.93f) w.Add("small-headed");
            if (sp != null && Has(sp, BodyGroup.Tail)) { if (tail > 1.24f) w.Add("long-tailed"); else if (tail < 0.86f) w.Add("short-tailed"); }
            if (sp != null && Has(sp, BodyGroup.WingL)) { if (wing > 1.14f) w.Add("broad-winged"); else if (wing < 0.9f) w.Add("short-winged"); }
            if (val < 0.93f) w.Add("dark coat"); else if (sat > 1.11f) w.Add("vivid coat"); else if (sat < 0.89f) w.Add("pale coat"); else if (Mathf.Abs(hue) > 0.028f) w.Add("odd tint");
            if (mark > 0 && mark < MarkNames.Length && ModelBuilder.CanMark(sp, mark)) w.Add(MarkNames[mark]);
            if (w.Count == 0) return "A typical build for its kind";
            string s = string.Join(", ", w);
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        static bool Has(SpeciesDef sp, BodyGroup g)
        {
            if (sp.parts != null) foreach (var p in sp.parts) if (p.group == g) return true;
            return false;
        }
    }
}
