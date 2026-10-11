using UnityEngine;

namespace Wildbond
{
    /// <summary>The three specialization paths every individual kin is graded on.</summary>
    public enum KinPath { Combat, Work, Travel }

    /// <summary>
    /// Individual quality. Each kin rolls a score 0..100 per path: one path is its specialty (B..S, sometimes
    /// exceptional) and the other two are only decent/okay (D..B). Duplicates of a species differ, so catching more
    /// finds better individuals. Scores scale combat stats, work speed, and travel (move speed + riding stamina).
    /// </summary>
    public static class Aptitude
    {
        public static readonly string[] Names = { "Combat", "Work", "Travel" };

        public static string Grade(int v) => v >= 90 ? "S" : v >= 75 ? "A" : v >= 58 ? "B" : v >= 40 ? "C" : "D";

        public static Color GradeColor(int v) =>
            v >= 90 ? new Color(1f, 0.82f, 0.3f) : v >= 75 ? new Color(0.8f, 0.55f, 1f) : v >= 58 ? new Color(0.45f, 0.75f, 1f) :
            v >= 40 ? new Color(0.55f, 0.9f, 0.55f) : new Color(0.7f, 0.7f, 0.72f);

        /// <summary>Rolls scores. forcedSpec &lt; 0 picks a specialty weighted by the species' build.</summary>
        public static void Roll(CreatureData d, int forcedSpec = -1)
        {
            var sp = d.Species;
            int spec = forcedSpec;
            if (spec < 0)
            {
                float wc = 1f, ww = 1f, wt = 1f;
                if (sp != null)
                {
                    wc = 0.6f + (sp.baseAtk + sp.baseDef) / 30f;
                    int workSum = 0;
                    foreach (var w in sp.work) workSum += w.level;
                    ww = 0.4f + workSum * 0.4f;
                    wt = 0.5f + (sp.mountable ? 1.3f : 0f) + Mathf.Max(0f, sp.runSpeed - 6f) * 0.2f + (sp.locomotion == Locomotion.Flyer ? 0.4f : 0f);
                }
                float r = Random.value * (wc + ww + wt);
                spec = r < wc ? 0 : (r < wc + ww ? 1 : 2);
            }
            d.spec = spec;
            d.apt = new int[3];
            for (int i = 0; i < 3; i++)
                d.apt[i] = i == spec ? 62 + Mathf.RoundToInt(38f * Mathf.Pow(Random.value, 1.8f))   // specialty 62..100 (S ~15%)
                                     : 35 + Mathf.RoundToInt(33f * Random.value);                    // others 35..68 (decent / okay)
            int bonus = (d.alpha ? 8 : 0) + (d.shimmer ? 4 : 0);
            for (int i = 0; i < 3; i++) d.apt[i] = Mathf.Min(100, d.apt[i] + bonus);
        }

        /// <summary>"Combat S 93 · Work C 44 · Travel B 60" with the specialty marked.</summary>
        public static string Summary(CreatureData d, bool rich = true)
        {
            d.EnsureAptitude();
            var parts = new string[3];
            for (int i = 0; i < 3; i++)
            {
                string s = $"{Names[i]} {Grade(d.apt[i])} {d.apt[i]}" + (i == d.spec ? "*" : "");
                parts[i] = rich ? UIFactory.Col(s, GradeColor(d.apt[i])) : s;
            }
            return string.Join("  ·  ", parts);
        }

        public static string SpecLabel(CreatureData d)
        {
            d.EnsureAptitude();
            return $"{Names[d.spec]} {Grade(d.apt[d.spec])}";
        }
    }
}
