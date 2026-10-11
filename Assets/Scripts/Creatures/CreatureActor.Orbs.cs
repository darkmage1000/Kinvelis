using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Legendaries' orbiting orbs are alive: each one shines (a short light trail and the odd glint) and gives off
    /// something of the kin's elements - drips for Tide, falling leaves for Flora, flames for Ember, smoke for Shade,
    /// star-twinkles for Lumen, crackling arcs for Volt, and so on. Two thirds of it follows the first element, a third
    /// the second. Orbs are the small Glow / White spheres (and crystal shards) in the species' Extra part group, so the
    /// effect follows whatever the body plan puts there; big pale spheres in that group are treated as storm clouds.
    /// Only runs while the kin is fully simulated (near the camera), like the species auras.
    /// </summary>
    public partial class CreatureActor
    {
        struct Orb { public Vector3 local; public float size; public bool cloud; }

        Orb[] orbs;
        string orbSpecies;
        float orbTrailTimer, orbFxTimer, orbArcTimer;

        void SetupOrbs()
        {
            orbSpecies = Sp.id;
            Vector3 pivot = Vector3.zero;
            if (Sp.pivots != null)
                foreach (var p in Sp.pivots) if (p.group == BodyGroup.Extra) pivot = p.pivot;
            var list = new List<Orb>();
            foreach (var part in Sp.parts)
            {
                if (part.group != BodyGroup.Extra) continue;
                float big = Mathf.Max(part.scale.x, Mathf.Max(part.scale.y, part.scale.z));
                bool bright = part.slot == ColorSlot.Glow || part.slot == ColorSlot.White;
                if (part.shape == PartShape.Sphere && bright && big <= 0.32f) list.Add(new Orb { local = part.pos - pivot, size = big });
                else if (part.shape == PartShape.Crystal && bright) list.Add(new Orb { local = part.pos - pivot, size = Mathf.Min(0.2f, big * 0.4f) });
                else if (part.shape == PartShape.Sphere && part.slot == ColorSlot.Secondary && big > 0.32f) list.Add(new Orb { local = part.pos - pivot, size = big, cloud = true });
            }
            orbs = list.ToArray();
        }

        void TickOrbs(float dt)
        {
            if (orbSpecies != Sp.id) SetupOrbs();
            if (orbs.Length == 0) return;
            var tr = Rig.GetGroup(BodyGroup.Extra);
            if (tr == null) return;
            float sc = Sp.size * Data.SizeScale;
            bool lean = QualityProfile.Current.particleBudget < 200;
            Color glow = Data.Palette.glow;

            // ---- shine: a short trail behind every orb, and a glint now and then ----
            orbTrailTimer -= dt;
            if (orbTrailTimer <= 0f)
            {
                orbTrailTimer = lean ? 0.12f : 0.06f;
                for (int i = 0; i < orbs.Length; i++)
                {
                    if (orbs[i].cloud) continue;
                    Vector3 p = tr.TransformPoint(orbs[i].local);
                    float s = orbs[i].size * sc;
                    Fx.Mote(p, Vector3.zero, glow, s * 1.5f, 0.34f, true);
                    if (Random.value < 0.035f) Fx.Mote(p, Vector3.zero, Color.white, s * 3.4f, 0.14f, true);
                }
            }

            // ---- what the element gives off ----
            orbFxTimer -= dt;
            if (orbFxTimer <= 0f)
            {
                orbFxTimer = (lean ? 0.16f : 0.08f) * Mathf.Max(1f, 6f / orbs.Length);
                var o = orbs[Random.Range(0, orbs.Length)];
                Vector3 p = tr.TransformPoint(o.local);
                if (o.cloud)
                {
                    // rain under a storm cloud
                    Vector3 r = Random.insideUnitSphere * (o.size * sc * 0.4f);
                    Fx.Mote(p + new Vector3(r.x, -o.size * sc * 0.3f, r.z), Vector3.down * 5f, new Color(0.7f, 0.85f, 1f, 0.8f), 0.07f * sc, 0.45f, false);
                }
                else
                {
                    Element e = Data.Element2 != Element.Normal && Data.Element2 != Data.Element1 && Random.value < 0.34f ? Data.Element2 : Data.Element1;
                    OrbEffect(e, p, o.size * sc, sc, glow);
                }
            }

            // ---- Volt: lightning jumps between two orbs ----
            if (Data.Element1 == Element.Volt || Data.Element2 == Element.Volt)
            {
                orbArcTimer -= dt;
                if (orbArcTimer <= 0f)
                {
                    orbArcTimer = Random.Range(0.35f, 0.9f);
                    int a = Random.Range(0, orbs.Length), b = Random.Range(0, orbs.Length);
                    if (a != b && !orbs[a].cloud && !orbs[b].cloud)
                    {
                        Vector3 pa = tr.TransformPoint(orbs[a].local), pb = tr.TransformPoint(orbs[b].local);
                        float len = Vector3.Distance(pa, pb);
                        int n = Mathf.Clamp(Mathf.RoundToInt(len / (0.22f * sc)), 4, 12);
                        var c = new Color(1f, 0.96f, 0.55f);
                        for (int i = 0; i <= n; i++)
                            Fx.Mote(Vector3.Lerp(pa, pb, i / (float)n) + Random.insideUnitSphere * (0.12f * sc), Vector3.zero, i % 3 == 0 ? Color.white : c, 0.11f * sc, 0.11f, true);
                    }
                }
            }
        }

        static void OrbEffect(Element e, Vector3 p, float s, float sc, Color glow)
        {
            Vector3 j = Random.insideUnitSphere;
            switch (e)
            {
                case Element.Tide:
                    // a drop falls; now and then a bubble drifts up
                    Fx.Mote(p + Vector3.down * (s * 0.4f), new Vector3(j.x * 0.15f, -2.4f, j.z * 0.15f), new Color(0.55f, 0.82f, 1f, 0.9f), 0.09f * sc, 0.7f, false);
                    if (Random.value < 0.25f) Fx.Mote(p, new Vector3(j.x * 0.2f, 0.7f, j.z * 0.2f), new Color(0.85f, 0.96f, 1f), 0.12f * sc, 0.9f, true);
                    break;
                case Element.Flora:
                    // a leaf or a petal spirals down
                    Fx.Mote(p, new Vector3(j.x * 0.6f, -0.7f, j.z * 0.6f), Random.value < 0.6f ? new Color(0.4f, 0.8f, 0.35f, 0.95f) : new Color(1f, 0.6f, 0.75f, 0.95f), 0.13f * sc, 1.3f, false);
                    if (Random.value < 0.3f) Fx.Mote(p, Vector3.up * 0.4f, glow, 0.08f * sc, 0.8f, true);
                    break;
                case Element.Ember:
                    // a tongue of flame, and sparks
                    Fx.Mote(p, new Vector3(j.x * 0.3f, Random.Range(1.2f, 2.2f), j.z * 0.3f), Random.value < 0.5f ? new Color(1f, 0.5f, 0.15f) : new Color(1f, 0.82f, 0.3f), s * 1.3f, 0.45f, true);
                    if (Random.value < 0.35f) Fx.Mote(p, j * 2.2f + Vector3.up * 1.5f, new Color(1f, 0.9f, 0.5f), 0.06f * sc, 0.5f, true);
                    break;
                case Element.Frost:
                    Fx.Mote(p, new Vector3(j.x * 0.3f, -0.8f, j.z * 0.3f), new Color(0.9f, 0.97f, 1f), 0.08f * sc, 1.1f, true);
                    break;
                case Element.Volt:
                    // sparks spit off in every direction
                    for (int i = 0; i < 2; i++)
                        Fx.Mote(p, Random.onUnitSphere * Random.Range(2.5f, 5f), i == 0 ? Color.white : new Color(1f, 0.92f, 0.4f), 0.07f * sc, 0.14f, true);
                    break;
                case Element.Stone:
                    Fx.Mote(p, new Vector3(j.x * 0.4f, -1.4f, j.z * 0.4f), new Color(0.62f, 0.55f, 0.45f, 0.9f), 0.08f * sc, 0.7f, false);
                    break;
                case Element.Gale:
                    // a wisp of wind streams off sideways
                    Fx.Mote(p, new Vector3(j.x, 0.15f, j.z).normalized * 2.4f, new Color(0.85f, 1f, 0.92f, 0.55f), 0.16f * sc, 0.4f, false);
                    break;
                case Element.Shade:
                    // heavy smoke sinks; a sickly wisp rises through it
                    Fx.Mote(p, new Vector3(j.x * 0.25f, -0.45f, j.z * 0.25f), new Color(0.2f, 0.12f, 0.3f, 0.75f), s * 1.9f, 1.1f, false);
                    if (Random.value < 0.4f) Fx.Mote(p, new Vector3(j.x * 0.3f, 0.9f, j.z * 0.3f), glow, 0.08f * sc, 0.8f, true);
                    break;
                case Element.Lumen:
                    // star-twinkles
                    Fx.Sparkle(p, Random.value < 0.5f ? Color.white : glow, 2, s * 0.9f);
                    if (Random.value < 0.3f) Fx.Mote(p, Vector3.zero, Color.white, s * 3f, 0.16f, true);
                    break;
                default:
                    Fx.Mote(p, Vector3.up * 0.6f, glow, 0.08f * sc, 0.8f, true);
                    break;
            }
        }
    }
}
