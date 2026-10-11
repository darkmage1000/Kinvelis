using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Tracks active creature actors for queries (targeting, AoE, threats).</summary>
    public static class CreatureRegistry
    {
        public static readonly List<CreatureActor> All = new List<CreatureActor>(128);
        public static int WildCount { get; private set; }

        public static void Add(CreatureActor a)
        {
            if (!All.Contains(a)) All.Add(a);
            Recount();
        }

        public static void Remove(CreatureActor a)
        {
            All.Remove(a);
            Recount();
        }

        static void Recount()
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i].Mode == CreatureMode.Wild) n++;
            WildCount = n;
        }

        /// <summary>Best wild target for the player: nearest in a forward cone, else nearest overall.</summary>
        public static CreatureActor FindTarget(Vector3 pos, Vector3 forward, float range, float coneDot = 0.35f)
        {
            CreatureActor best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var a = All[i];
                if (a.Mode != CreatureMode.Wild || !a.IsAlive) continue;
                Vector3 d = a.Position - pos; d.y = 0;
                float dist = d.magnitude - a.Radius;
                if (dist > range) continue;
                float dot = dist > 0.01f ? Vector3.Dot(d.normalized, forward) : 1f;
                float score = dist * (dot > coneDot ? 1f : 2.5f) - dot * 2f;
                if (score < bestScore) { bestScore = score; best = a; }
            }
            return best;
        }

        /// <summary>Nearest aggro'd wild creature near a point (companion defence).</summary>
        public static CreatureActor FindThreat(Vector3 pos, float range)
        {
            CreatureActor best = null;
            float bestD = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var a = All[i];
                if (a.Mode != CreatureMode.Wild || !a.IsAlive || !a.Aggro) continue;
                float d = (a.Position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        /// <summary>Wild kin currently targeting the player or the given companion.</summary>
        public static CreatureActor FindHunter(Vector3 pos, float range, ICombatant player, ICombatant companion)
        {
            CreatureActor best = null;
            float bestD = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var a = All[i];
                if (a.Mode != CreatureMode.Wild || !a.IsAlive || !a.Aggro) continue;
                if (a.Target != player && a.Target != companion) continue;
                float d = (a.Position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        /// <summary>Nearest living wild kin (Aggressive stance).</summary>
        public static CreatureActor FindNearestWild(Vector3 pos, float range)
        {
            CreatureActor best = null;
            float bestD = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var a = All[i];
                if (a.Mode != CreatureMode.Wild || !a.IsAlive) continue;
                float d = (a.Position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        /// <summary>Invokes fn for every enemy of 'source' within radius (creatures and the player).</summary>
        public static void ForEachEnemyInRange(ICombatant source, Vector3 center, float radius, Action<ICombatant> fn)
        {
            float r2 = radius * radius;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                if (i >= All.Count) continue;
                var a = All[i];
                if (!a.IsAlive || a.Team == source.Team) continue;
                if (a.Mode == CreatureMode.Worker && a.State != AIState.Defend && a.State != AIState.Recover) continue;
                if ((a.Position - center).sqrMagnitude <= r2 + a.Radius * a.Radius) fn(a);
            }
            var p = GameManager.I.Player;
            if (source.Team == Team.Wild && p.IsAlive && (p.Position - center).sqrMagnitude <= r2) fn(p);
        }

        /// <summary>Pack behaviour: nearby members of the same species join the fight.</summary>
        public static void AlertPack(CreatureActor victim, ICombatant attacker)
        {
            if (victim.Sp.temperament == Temperament.Timid) return;
            for (int i = 0; i < All.Count; i++)
            {
                var a = All[i];
                if (a == victim || a.Mode != CreatureMode.Wild || a.Aggro || !a.IsAlive) continue;
                if (a.Data.speciesId != victim.Data.speciesId) continue;
                if ((a.Position - victim.Position).sqrMagnitude > 18f * 18f) continue;
                a.Aggro = true;
                a.Target = attacker;
            }
        }
    }
}
