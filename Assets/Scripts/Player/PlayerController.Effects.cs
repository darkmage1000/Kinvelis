using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What kin signature skills (SkillEffects) can leave on the player: a Hex mark (the next hit taken is super
    /// effective), a Tether (pinned to a small circle for a few seconds; a roll's safe frames dodge it), and Temper
    /// from your own blacksmith kin (the next weapon hit is a certain crit).
    /// </summary>
    public partial class PlayerController
    {
        float hexUntil, temperUntil, tetherUntil, tetherRadius, markFx;
        Vector3 tetherPos;

        public bool Hexed => Time.time < hexUntil;
        public bool Tethered => Time.time < tetherUntil;
        public bool Tempered => Time.time < temperUntil;

        public void Hex(float seconds)
        {
            if (dead || invuln > 0f) return;
            hexUntil = Time.time + seconds;
            HUD.DamageNumber(transform.position + Vector3.up * 2.4f, "HEXED", new Color(0.75f, 0.5f, 1f), 0.8f);
        }

        public bool TakeHex() { if (!Hexed) return false; hexUntil = 0f; return true; }

        public void Temper(float seconds) => temperUntil = Time.time + seconds;
        public bool TakeTemper() { if (!Tempered) return false; temperUntil = 0f; return true; }

        public void TetherTo(Vector3 anchor, float radius, float seconds)
        {
            if (dead || invuln > 0f) return;
            tetherPos = anchor;
            tetherRadius = radius;
            tetherUntil = Time.time + seconds;
            HUD.DamageNumber(transform.position + Vector3.up * 2.4f, "TETHERED", new Color(0.85f, 0.85f, 0.95f), 0.8f);
        }

        void ClearMarks() { hexUntil = temperUntil = tetherUntil = 0f; }

        /// <summary>Keeps a tethered player inside the circle (called on the step about to be taken).</summary>
        void ClampTether(ref Vector3 next)
        {
            Vector3 off = next - tetherPos; off.y = 0f;
            if (off.sqrMagnitude <= tetherRadius * tetherRadius) return;
            off = off.normalized * tetherRadius;
            next.x = tetherPos.x + off.x;
            next.z = tetherPos.z + off.z;
        }

        void TickMarks(float dt)
        {
            float now = Time.time;
            if (now >= hexUntil && now >= tetherUntil && now >= temperUntil) return;
            markFx -= dt;
            if (markFx > 0f) return;
            markFx = 0.16f;
            Vector3 pos = transform.position;
            if (now < hexUntil)
            {
                float a = now * 4f;
                Fx.Mote(pos + new Vector3(Mathf.Cos(a) * 0.3f, 2.2f, Mathf.Sin(a) * 0.3f), Vector3.zero, new Color(0.75f, 0.4f, 1f), 0.16f, 0.4f, true);
            }
            if (now < tetherUntil)
            {
                Vector3 a0 = tetherPos + Vector3.up * 0.1f, b = pos + Vector3.up * 0.9f;
                for (int i = 0; i <= 5; i++) Fx.Mote(Vector3.Lerp(a0, b, i / 5f), Vector3.zero, new Color(0.9f, 0.9f, 1f, 0.8f), 0.07f, 0.2f, true);
            }
            if (now < temperUntil) Fx.Mote(pos + Vector3.up * 1.1f + Random.insideUnitSphere * 0.4f, Vector3.up, new Color(1f, 0.6f, 0.25f), 0.12f, 0.4f, true);
        }
    }
}
