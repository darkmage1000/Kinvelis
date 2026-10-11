using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The parade (world rule WorldSettings.parade, on in Little Explorer worlds): every kin in the party that is not
    /// the active companion walks behind the player in a line, along the path the player took. They are scenery -
    /// animated models, not CreatureActors: they don't fight, can't be hit and cost nothing to the simulation. The
    /// companion is still the one real kin out; swapping it changes who is in the line.
    /// </summary>
    public static class Parade
    {
        class Walker
        {
            public string uid, key;
            public GameObject go;
            public CreatureRig rig;
            public Vector3 pos;
            public float yaw;
        }

        const float Step = 0.6f, Gap = 1.9f, First = 3.6f;
        static readonly List<Walker> walkers = new List<Walker>();
        static readonly List<Vector3> trail = new List<Vector3>();   // where the player has been, newest first
        static readonly List<CreatureData> want = new List<CreatureData>();

        public static void Clear()
        {
            foreach (var w in walkers) if (w.go != null) Object.Destroy(w.go);
            walkers.Clear();
            trail.Clear();
        }

        public static void Tick(float dt)
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null) return;
            if (!WorldRules.Current.parade || Caves.InCave || !gm.Player.IsAlive) { if (walkers.Count > 0) Clear(); return; }

            Vector3 me = gm.Player.Position;
            if (trail.Count == 0 || (trail[0] - me).sqrMagnitude > Step * Step)
            {
                trail.Insert(0, me);
                if (trail.Count > 90) trail.RemoveAt(trail.Count - 1);
            }
            Sync(gm);
            float size = Mathf.Clamp(WorldRules.Current.kinSize, 0.2f, 4f);
            for (int i = 0; i < walkers.Count; i++)
            {
                var w = walkers[i];
                Vector3 to = PointBack(First + i * Gap, me) - w.pos; to.y = 0f;
                float dist = to.magnitude;
                float speed = 0f;
                if (dist > 30f) w.pos = me - gm.Player.Forward * (First + i * Gap);   // left far behind (fast travel): catch up at once
                else if (dist > 0.35f)
                {
                    speed = Mathf.Min(dist * 3f, 13f);
                    w.pos += to / dist * Mathf.Min(speed * dt, dist);
                    w.yaw = Mathf.MoveTowardsAngle(w.yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 540f * dt);
                }
                w.pos.y = Mathf.Max(Ground.Height(w.pos.x, w.pos.z), Tides.Surface(w.pos.x, w.pos.z) - 0.4f);
                w.go.transform.SetPositionAndRotation(w.pos, Quaternion.Euler(0f, w.yaw, 0f));
                w.go.transform.localScale = Vector3.one * size;
                w.rig.Animate(dt, speed, false, false);
            }
        }

        /// <summary>The point 'dist' metres back along the player's path (straight behind them if the path is still too short).</summary>
        static Vector3 PointBack(float dist, Vector3 me)
        {
            Vector3 prev = me;
            float left = dist;
            for (int i = 0; i < trail.Count; i++)
            {
                float seg = Vector3.Distance(prev, trail[i]);
                if (seg >= left && seg > 0.001f) return Vector3.Lerp(prev, trail[i], left / seg);
                left -= seg;
                prev = trail[i];
            }
            return prev - GameManager.I.Player.Forward * left;
        }

        /// <summary>Keeps one walker per party kin that is not out as the companion; rebuilt only when the line-up changes.</summary>
        static void Sync(GameManager gm)
        {
            want.Clear();
            var comp = gm.Party.Companion;
            foreach (var d in gm.Party.All)
            {
                if (d.location != KinLocation.Party || d.Species == null) continue;
                if (comp != null && comp.Data == d) continue;
                want.Add(d);
            }
            bool same = want.Count == walkers.Count;
            for (int i = 0; same && i < want.Count; i++)
                same = walkers[i].go != null && walkers[i].uid == want[i].uid && walkers[i].key == want[i].ModelKey;
            if (same) return;

            foreach (var w in walkers) if (w.go != null) Object.Destroy(w.go);
            walkers.Clear();
            Vector3 me = gm.Player.Position;
            for (int i = 0; i < want.Count; i++)
            {
                var d = want[i];
                var go = new GameObject("Parade_" + d.Species.id);
                var w = new Walker { uid = d.uid, key = d.ModelKey, go = go, pos = me - gm.Player.Forward * (First + i * Gap), yaw = gm.Player.transform.eulerAngles.y };
                w.rig = ModelBuilder.Build(d, go.transform, RenderAssets.Creature);
                walkers.Add(w);
            }
        }
    }
}
