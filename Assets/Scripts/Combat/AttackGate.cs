using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Wild kin take turns on you (2026-10-18, user after a mid-game cave: "groups of range enemies make quick work of
    /// me"). Only `Slots` wild kin may use a move on the player at once; each holds its turn for a few seconds, then
    /// steps back for a moment so another can come in. The rest keep their distance and circle. Bosses, alphas, tamers'
    /// kin and Wardens are not gated - those fights are designed one by one. Your kin are never gated.
    /// </summary>
    public static class AttackGate
    {
        public const int Slots = 2;
        static readonly CreatureActor[] holder = new CreatureActor[Slots];
        static readonly float[] until = new float[Slots];
        static readonly CreatureActor[] last = new CreatureActor[Slots];
        static readonly float[] lastEnd = new float[Slots];

        public static bool Gated(CreatureActor a) => a.Mode == CreatureMode.Wild && !a.Boss && !a.Duelist && !a.Data.alpha && a.Target is PlayerController;

        /// <summary>May this kin attack the player now? Takes (or keeps) a turn if one is free.</summary>
        public static bool Allow(CreatureActor a)
        {
            float now = Time.time;
            int free = -1;
            for (int i = 0; i < Slots; i++)
            {
                var h = holder[i];
                if (h == a && now < until[i]) return true;
                bool open = h == null || !h.gameObject.activeSelf || !h.IsAlive || !h.Aggro || now >= until[i];
                if (open && h != null) { last[i] = h; lastEnd[i] = now; holder[i] = null; }
                if (open && free < 0) free = i;
            }
            if (free < 0) return false;
            // the kin whose turn just ended waits a moment before it can take another
            for (int i = 0; i < Slots; i++) if (last[i] == a && now - lastEnd[i] < 1.6f) return false;
            holder[free] = a;
            until[free] = now + Random.Range(3f, 4.5f);
            return true;
        }
    }
}
