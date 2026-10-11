using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Wave 2 roster (#048-#097): 50 more kin, five per element - a small cute one, wacky ones (horns, spikes, whips,
    /// orbiting / floating effects), big cool ones (eight new mounts) and Floressa, the humanoid bloom.
    /// Split by element: Wave2A (Normal, Flora, Ember), Wave2B (Tide, Frost, Volt), Wave2C (Stone, Gale, Shade, Lumen).
    /// Shared abilities, shorthand and the whip / spike / ring plan helpers live here.
    /// </summary>
    public static partial class ContentSeed
    {
        // shorthand for body plans
        const PartShape Sph = PartShape.Sphere, Bx = PartShape.Box, Cyl = PartShape.Cylinder, Con = PartShape.Cone,
            Cap = PartShape.Capsule, Cry = PartShape.Crystal, Rok = PartShape.Rock, Dom = PartShape.Dome;
        const BodyGroup gB = BodyGroup.Body, gH = BodyGroup.Head, gT = BodyGroup.Tail, gX = BodyGroup.Extra,
            gAL = BodyGroup.ArmL, gAR = BodyGroup.ArmR, gFL = BodyGroup.LegFL, gFR = BodyGroup.LegFR,
            gWL = BodyGroup.WingL, gWR = BodyGroup.WingR;
        const ColorSlot cP = ColorSlot.Primary, cS = ColorSlot.Secondary, cA = ColorSlot.Accent, cG = ColorSlot.Glow,
            cW = ColorSlot.White, cD = ColorSlot.Dark;

        static void BuildWave2(Bundle b)
        {
            BuildWave2Abilities(b);
            BuildWave2A(b);
            BuildWave2B(b);
            BuildWave2C(b);
        }

        static void BuildWave2Abilities(Bundle b)
        {
            // whips reach far, horns and jaws hit hard
            Ab(b, "whip_crack", "Whip Crack", Element.Normal, AbilityKind.Melee, 17, 3.4f, 2.6f);
            Ab(b, "mace_swing", "Mace Swing", Element.Normal, AbilityKind.Melee, 24, 2.4f, 3.4f);
            Ab(b, "horn_toss", "Horn Toss", Element.Normal, AbilityKind.Melee, 27, 2.3f, 3.8f);
            Ab(b, "stampede", "Stampede", Element.Normal, AbilityKind.Dash, 28, 10, 5.5f, StatusType.None, 0, 8f);
            Ab(b, "war_cry", "War Cry", Element.Normal, AbilityKind.Burst, 22, 4.5f, 6.5f, StatusType.Drowsy, 0.2f, 5f);
            Ab(b, "vine_whip", "Vine Whip", Element.Flora, AbilityKind.Melee, 19, 3.4f, 2.6f);
            Ab(b, "flare_horn", "Flare Horn", Element.Ember, AbilityKind.Melee, 26, 2.2f, 3.6f, StatusType.Burn, 0.35f);
            Ab(b, "lava_plume", "Lava Plume", Element.Ember, AbilityKind.Burst, 26, 4.5f, 6.5f, StatusType.Burn, 0.35f, 4.5f);
            Ab(b, "tidal_lash", "Tidal Lash", Element.Tide, AbilityKind.Melee, 19, 3.4f, 2.6f, StatusType.Soaked, 0.3f);
            Ab(b, "riptide", "Riptide", Element.Tide, AbilityKind.Dash, 26, 9, 5f, StatusType.Soaked, 0.3f, 7f);
            Ab(b, "icicle_spear", "Icicle Spear", Element.Frost, AbilityKind.Projectile, 21, 13, 3.2f, StatusType.Chill, 0.2f, 3, 20f);
            Ab(b, "hail_burst", "Hail Burst", Element.Frost, AbilityKind.Burst, 24, 4.5f, 6.5f, StatusType.Chill, 0.35f, 5f);
            Ab(b, "volt_whip", "Volt Whip", Element.Volt, AbilityKind.Melee, 19, 3.4f, 2.6f, StatusType.Shock, 0.25f);
            Ab(b, "thunder_charge", "Thunder Charge", Element.Volt, AbilityKind.Dash, 27, 10, 5.2f, StatusType.Shock, 0.25f, 8f);
            Ab(b, "spike_shot", "Spike Shot", Element.Stone, AbilityKind.Projectile, 19, 12, 2.8f, StatusType.None, 0, 3, 17f);
            Ab(b, "quake_stomp", "Quake Stomp", Element.Stone, AbilityKind.Burst, 27, 4.5f, 6.5f, StatusType.None, 0, 5f);
            Ab(b, "rock_jaw", "Rock Jaw", Element.Stone, AbilityKind.Melee, 31, 2.4f, 4.4f);
            Ab(b, "wind_ribbon", "Wind Ribbon", Element.Gale, AbilityKind.Melee, 18, 3.4f, 2.5f);
            Ab(b, "sky_dive", "Sky Dive", Element.Gale, AbilityKind.Dash, 26, 10, 5f, StatusType.None, 0, 8f);
            Ab(b, "sting_tail", "Sting Tail", Element.Shade, AbilityKind.Melee, 22, 2.6f, 3.2f, StatusType.Drowsy, 0.3f);
            Ab(b, "night_slash", "Night Slash", Element.Shade, AbilityKind.Melee, 27, 2.3f, 3.4f);
            Ab(b, "gloom_pulse", "Gloom Pulse", Element.Shade, AbilityKind.Burst, 24, 4.5f, 6.5f, StatusType.Drowsy, 0.3f, 5f);
            Ab(b, "radiant_charge", "Radiant Charge", Element.Lumen, AbilityKind.Dash, 26, 10, 5f, StatusType.Drowsy, 0.15f, 8f);
        }

        /// <summary>Species ambience: particle aura + how the Extra part group moves.</summary>
        static void Effects(SpeciesDef d, AuraFx aura, ExtraAnim anim = ExtraAnim.None) { d.aura = aura; d.extraAnim = anim; }

        static Vector3 FlipX(Vector3 v) => new Vector3(-v.x, v.y, v.z);

        partial class Plan
        {
            /// <summary>End of the last Whip (just past its final link) and the direction it was heading. Used by Tip.</summary>
            Vector3 last, lastDir = Vector3.up;

            /// <summary>
            /// Chain of stretched links: whips, tendrils, curled horns, long hair, tusks, tails. Starts at 'start', each link
            /// advances |step| along the current direction, and 'bend' is added to the direction every link (so it curls).
            /// Thickness tapers d0 -> d1. 'band' alternates a second color.
            /// </summary>
            public Plan Whip(BodyGroup g, ColorSlot c, Vector3 start, Vector3 step, Vector3 bend, int n, float d0, float d1, ColorSlot? band = null)
            {
                float len = step.magnitude;
                Vector3 p = start, s = step;
                for (int i = 0; i < n; i++)
                {
                    float d = Mathf.Lerp(d0, d1, n == 1 ? 0f : i / (float)(n - 1));
                    Add(PartShape.Sphere, g, band.HasValue && i % 2 == 1 ? band.Value : c, p, new Vector3(d, d, Mathf.Max(d, len * 1.4f)),
                        Quaternion.FromToRotation(Vector3.forward, s).eulerAngles);
                    lastDir = s.normalized;
                    last = p + lastDir * (len * 0.5f);
                    p += s;
                    s = (s + bend).normalized * len;
                }
                return this;
            }

            /// <summary>Mirrored pair of whips (right side as given, left side flipped in X). Tip/TipPair refer to the right one.</summary>
            public Plan WhipPair(BodyGroup left, BodyGroup right, ColorSlot c, Vector3 start, Vector3 step, Vector3 bend, int n, float d0, float d1, ColorSlot? band = null)
            {
                Whip(left, c, FlipX(start), FlipX(step), FlipX(bend), n, d0, d1, band);
                return Whip(right, c, start, step, bend, n, d0, d1, band);
            }

            /// <summary>Caps the last whip: a part at its end, pointing along it (stinger, bulb, mace head, blade, tuft).</summary>
            public Plan Tip(PartShape s, BodyGroup g, ColorSlot c, Vector3 size)
            {
                return Add(s, g, c, last + lastDir * (size.y * 0.35f), size, Quaternion.FromToRotation(Vector3.up, lastDir).eulerAngles);
            }

            public Plan TipPair(PartShape s, BodyGroup left, BodyGroup right, ColorSlot c, Vector3 size)
            {
                Vector3 d = FlipX(lastDir);
                Add(s, left, c, FlipX(last) + d * (size.y * 0.35f), size, Quaternion.FromToRotation(Vector3.up, d).eulerAngles);
                return Tip(s, right, c, size);
            }

            /// <summary>
            /// Row of n spikes along the spine from z0 to z1 sitting on height y (arc lifts the middle). lean tilts them
            /// back (negative) or forward. With x != 0 it becomes two mirrored side rows, rolled outward by 'roll'.
            /// </summary>
            public Plan Spikes(BodyGroup g, ColorSlot c, float y, float z0, float z1, int n, float w, float h, float arc = 0f, float lean = -15f,
                float x = 0f, float roll = 0f, PartShape shape = PartShape.Cone)
            {
                for (int i = 0; i < n; i++)
                {
                    float k = n == 1 ? 0.5f : i / (float)(n - 1);
                    float s = 0.75f + 0.25f * Mathf.Sin(k * Mathf.PI);
                    var pos = new Vector3(x, y + Mathf.Sin(k * Mathf.PI) * arc + h * s * 0.4f, Mathf.Lerp(z0, z1, k));
                    var size = new Vector3(w * s, h * s, w * s);
                    if (x == 0f) Add(shape, g, c, pos, size, new Vector3(lean, 0, 0));
                    else Mirror(shape, g, c, pos, size, new Vector3(lean, 0, roll));
                }
                return this;
            }

            /// <summary>
            /// Wing pair made of three overlapping flat lobes reaching 'span' out from the shoulder 'root', raised by 'lift'
            /// degrees, finished with three pointed feathers / claws at the outer edge.
            /// </summary>
            public Plan Wings(ColorSlot inner, ColorSlot outer, ColorSlot tips, Vector3 root, float span, float chord, float lift = 14f)
            {
                float c = Mathf.Cos(lift * Mathf.Deg2Rad), s = Mathf.Sin(lift * Mathf.Deg2Rad);
                for (int i = 0; i < 3; i++)
                {
                    float dist = span * (i + 0.5f) / 3f;
                    float ch = chord * (1f - 0.22f * i);
                    Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, i < 2 ? inner : outer,
                        root + new Vector3(dist * c, dist * s, -0.05f * i), new Vector3(span * 0.5f, 0.05f, ch), new Vector3(0, 0, lift));
                }
                for (int i = 0; i < 3; i++)
                {
                    float dist = span * (0.98f - 0.2f * i);
                    Mirror(PartShape.Cone, BodyGroup.WingL, BodyGroup.WingR, tips,
                        root + new Vector3(dist * c, dist * s, -0.1f - chord * 0.3f * i), new Vector3(0.04f, chord * 0.75f, chord * 0.32f),
                        new Vector3(0, 28f * i, -(90f - lift)));
                }
                return this;
            }

            /// <summary>
            /// Finishes a wave-2 plan. Crystal meshes grow upward from their base, but these plans place every part by its
            /// center, so crystals are shifted down half their length along their own axis first.
            /// </summary>
            public void Done(SpeciesDef d)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    var p = parts[i];
                    if (p.shape != PartShape.Crystal) continue;
                    p.pos -= Quaternion.Euler(p.rot) * Vector3.up * (p.scale.y * 0.5f);
                    parts[i] = p;
                }
                ApplyTo(d);
            }

            /// <summary>n parts on a horizontal circle; 'tilt' leans each part's up-axis outward (90 = pointing straight out).</summary>
            public Plan Ring(PartShape s, BodyGroup g, ColorSlot c, Vector3 center, float radius, int n, Vector3 size, float tilt = 0f, float startDeg = 0f)
            {
                for (int i = 0; i < n; i++)
                {
                    float a = startDeg + i * 360f / n;
                    float r = a * Mathf.Deg2Rad;
                    Add(s, g, c, center + new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r)) * radius, size, new Vector3(tilt, a, 0));
                }
                return this;
            }

            /// <summary>n parts on a vertical circle facing forward, each pointing away from the center (manes, frills, star points).</summary>
            public Plan Fan(PartShape s, BodyGroup g, ColorSlot c, Vector3 center, float radius, int n, Vector3 size, float startDeg = 0f, float arcDeg = 360f)
            {
                for (int i = 0; i < n; i++)
                {
                    float a = arcDeg >= 360f ? startDeg + i * 360f / n : startDeg + arcDeg * (n == 1 ? 0.5f : i / (float)(n - 1));
                    float r = a * Mathf.Deg2Rad;
                    Add(s, g, c, center + new Vector3(Mathf.Sin(r), Mathf.Cos(r), 0) * radius, size, new Vector3(0, 0, -a));
                }
                return this;
            }
        }
    }
}
