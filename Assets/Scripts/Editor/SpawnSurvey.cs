using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Play mode: carries the explorer in a straight line at `speed` m/s for `seconds` and counts every wild kin that
    /// appears (unique uids) and how many were shimmer - how fast a ride turns over the wild population.
    /// Start with `Begin(speed, seconds)`, read with `Report()`.
    /// </summary>
    public static class SpawnSurvey
    {
        static readonly HashSet<string> seen = new HashSet<string>();
        static int shimmer;
        static float speed, left, total;
        static Vector3 dir;

        public static string Begin(float metresPerSecond, float seconds)
        {
            seen.Clear(); shimmer = 0;
            speed = metresPerSecond; left = total = seconds;
            var pl = GameManager.I.Player;
            dir = pl.Forward; dir.y = 0f; dir.Normalize();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return "survey started at " + pl.Position;
        }

        static float last;
        static void Tick()
        {
            if (!Application.isPlaying || GameManager.I == null) { EditorApplication.update -= Tick; return; }
            float dt = Time.deltaTime;
            if (Time.time == last) return;
            last = Time.time;
            var pl = GameManager.I.Player;
            if (left > 0f)
            {
                left -= dt;
                Vector3 p = pl.transform.position + dir * speed * dt;
                p.y = Ground.Height(p.x, p.z);
                // keep to land: turn round at the sea
                if (p.y < WorldGen.WaterLevel + 0.5f) { dir = -dir; return; }
                pl.transform.position = p;
            }
            foreach (var a in CreatureRegistry.All)
            {
                if (a == null || a.Mode != CreatureMode.Wild || a.Data == null) continue;
                if (seen.Add(a.Data.uid) && a.Data.shimmer) shimmer++;
            }
        }

        public static string Report()
        {
            float t = total - Mathf.Max(0f, left);
            return $"{t:0} s: {seen.Count} wild kin seen ({seen.Count / Mathf.Max(1f, t) * 60f:0} a minute), {shimmer} shimmer, live now {CreatureRegistry.WildCount}";
        }
    }
}
