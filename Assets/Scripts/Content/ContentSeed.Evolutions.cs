using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Real evolutions (user, 2026-10-14: "give some kin real evolutions and grow or change in some meaningful way
    /// physically, and some are already perfect designed and just get stronger and maybe bigger"). First wave: the
    /// three partners and 25 small Common kin that look like babies, two or more of every element and every land.
    /// Each line has an Ascended form (Lv 20) and an Exalted form (Lv 35) with their own names and bodies, grown from
    /// the base body with the Evo tools below (longer legs, a smaller baby head, bigger features, new parts), and an
    /// Exalted skill; thirteen of them gain a second element at Exalted. Every other kind is a "perfect form".
    /// To add a line: one block in BuildEvolutions (an Evo, two Form calls) and its skill in BuildEvoSkills.
    /// </summary>
    public static partial class ContentSeed
    {
        // ================================================================ THE TOOLS
        /// <summary>Grows a kind's body into an evolved form, one change at a time; Form() snapshots it.</summary>
        class Evo
        {
            public readonly Plan p = new Plan();
            readonly SpeciesDef sp;
            int shifted;

            /// <summary>The kind's own colours, to start a form's palette from.</summary>
            public Palette Pal => sp.palette;

            public Evo(SpeciesDef sp)
            {
                this.sp = sp;
                p.parts.AddRange(sp.parts);
                p.pivots.AddRange(sp.pivots);
                shifted = p.parts.Count;
                Aim();
            }

            static bool IsLeg(BodyGroup g) => g == BodyGroup.LegFL || g == BodyGroup.LegFR || g == BodyGroup.LegBL || g == BodyGroup.LegBR;

            public bool Has(BodyGroup g) { foreach (var q in p.parts) if (q.group == g) return true; return false; }

            public Bounds B(BodyGroup g)
            {
                var b = new Bounds(); bool any = false;
                foreach (var q in p.parts)
                {
                    if (q.group != g) continue;
                    var e = new Bounds(q.pos, q.scale);
                    if (!any) { b = e; any = true; } else b.Encapsulate(e);
                }
                return b;
            }

            public Vector3 PivotOf(BodyGroup g)
            {
                foreach (var v in p.pivots) if (v.group == g) return v.pivot;
                return B(g).center;
            }

            BodyGroup HeadGroup => Has(BodyGroup.Head) ? BodyGroup.Head : BodyGroup.Body;

            /// <summary>The head's main sphere: where Plan's Face / Ears / Horns / Brows attach.</summary>
            public BodyPart Skull
            {
                get
                {
                    var g = HeadGroup; BodyPart best = default; float vol = -1f;
                    foreach (var q in p.parts)
                    {
                        if (q.group != g || (q.shape != PartShape.Sphere && q.shape != PartShape.Capsule && q.shape != PartShape.Rock && q.shape != PartShape.Dome)) continue;
                        float v = q.scale.x * q.scale.y * q.scale.z;
                        if (v > vol) { vol = v; best = q; }
                    }
                    return best;
                }
            }

            public Evo Aim()
            {
                var s = Skull;
                p.Head(HeadGroup, s.pos.y, s.pos.z, Mathf.Max(0.05f, s.scale.x));
                return this;
            }

            float Hip
            {
                get
                {
                    float h = 0f; int n = 0;
                    foreach (var v in p.pivots) if (IsLeg(v.group)) { h += v.pivot.y; n++; }
                    return n > 0 ? h / n : 0f;
                }
            }

            /// <summary>Longer legs: everything above them rises by what they gained (feet stay on the ground, feet keep their size).</summary>
            public Evo Legs(float k)
            {
                float hip = Hip;
                if (hip <= 0f) return this;
                float lift = hip * (k - 1f);
                for (int i = 0; i < p.parts.Count; i++)
                {
                    var q = p.parts[i];
                    if (IsLeg(q.group)) { q.pos.y *= k; if (q.scale.y > hip * 0.45f) q.scale.y *= k; }
                    else q.pos.y += lift;
                    p.parts[i] = q;
                }
                for (int i = 0; i < p.pivots.Count; i++)
                {
                    var v = p.pivots[i];
                    v.pivot.y = IsLeg(v.group) ? v.pivot.y * k : v.pivot.y + lift;
                    p.pivots[i] = v;
                }
                return Aim();
            }

            /// <summary>A whole group bigger or smaller about its pivot (or a point you give).</summary>
            public Evo Scale(BodyGroup g, Vector3 k, Vector3? about = null)
            {
                if (!Has(g)) return this;
                Vector3 c = about ?? PivotOf(g);
                for (int i = 0; i < p.parts.Count; i++)
                {
                    var q = p.parts[i];
                    if (q.group != g) continue;
                    q.pos = c + Vector3.Scale(q.pos - c, k);
                    q.scale = Vector3.Scale(q.scale, k);
                    p.parts[i] = q;
                }
                return Aim();
            }

            public Evo Scale(BodyGroup g, float k, Vector3? about = null) => Scale(g, Vector3.one * k, about);

            /// <summary>The head relative to the body (babies have big heads: below 1 grows the kin up).</summary>
            public Evo Head(float k) => Has(BodyGroup.Head) ? Scale(BodyGroup.Head, k) : this;

            /// <summary>Both wings / both arms.</summary>
            public Evo Wings(float k) { Scale(BodyGroup.WingL, k); return Scale(BodyGroup.WingR, k); }
            public Evo Arms(float k) { Scale(BodyGroup.ArmL, k); return Scale(BodyGroup.ArmR, k); }

            /// <summary>
            /// The body taller / wider / longer about its base; what sits on it (head, arms, wings, tail, extras) moves
            /// with the body's surface so nothing floats or sinks.
            /// </summary>
            public Evo Body(Vector3 k)
            {
                var bb = B(BodyGroup.Body);
                Vector3 c = new Vector3(bb.center.x, bb.min.y, bb.center.z);
                Vector3 Move(Vector3 v) => c + Vector3.Scale(v - c, k);
                // where each attached group is carried to, measured before anything moves
                var carry = new Dictionary<BodyGroup, Vector3>();
                foreach (var q in p.parts)
                    if (!IsLeg(q.group) && q.group != BodyGroup.Body && !carry.ContainsKey(q.group)) { Vector3 piv = PivotOf(q.group); carry[q.group] = Move(piv) - piv; }
                for (int i = 0; i < p.parts.Count; i++)
                {
                    var q = p.parts[i];
                    if (IsLeg(q.group)) continue;
                    if (q.group == BodyGroup.Body) { q.pos = Move(q.pos); q.scale = Vector3.Scale(q.scale, k); }
                    else q.pos += carry[q.group];
                    p.parts[i] = q;
                }
                for (int i = 0; i < p.pivots.Count; i++)
                {
                    var v = p.pivots[i];
                    if (IsLeg(v.group) || v.group == BodyGroup.Body) continue;
                    v.pivot = Move(v.pivot);
                    p.pivots[i] = v;
                }
                return Aim();
            }

            public Evo Body(float k) => Body(Vector3.one * k);

            /// <summary>Parts that match grow longer along their own length, keeping their base where it was (ears, horns, spikes, crests).</summary>
            public Evo Stretch(Func<BodyPart, bool> which, float k, float widen = 1f)
            {
                for (int i = 0; i < p.parts.Count; i++)
                {
                    var q = p.parts[i];
                    if (!which(q)) continue;
                    Vector3 up = Quaternion.Euler(q.rot) * Vector3.up;
                    q.pos += up * (q.scale.y * (k - 1f) * 0.5f);
                    q.scale = new Vector3(q.scale.x * widen, q.scale.y * k, q.scale.z * widen);
                    p.parts[i] = q;
                }
                return this;
            }

            public Evo Recolor(Func<BodyPart, bool> which, ColorSlot to)
            {
                for (int i = 0; i < p.parts.Count; i++) { var q = p.parts[i]; if (which(q)) { q.slot = to; p.parts[i] = q; } }
                return this;
            }

            public Evo Remove(Func<BodyPart, bool> which) { p.parts.RemoveAll(q => which(q)); return Aim(); }

            /// <summary>A copy of each matching part, changed by f (a second tail, a second pair of wings).</summary>
            public Evo Dup(Func<BodyPart, bool> which, Func<BodyPart, BodyPart> f)
            {
                var add = new List<BodyPart>();
                foreach (var q in p.parts) if (which(q)) add.Add(f(q));
                p.parts.AddRange(add);
                return this;
            }

            public Evo With(Action<Plan> add) { add(p); return this; }

            /// <summary>A snapshot of the body as it is now, as a named form. The Evo can keep growing into the next one.</summary>
            public EvoForm Form(string name, string blurb, Element el2 = Element.Normal, string skill = null, Palette? pal = null)
            {
                // parts added since the last snapshot were placed by their centres: crystals grow up from their base
                for (int i = shifted; i < p.parts.Count; i++)
                {
                    var q = p.parts[i];
                    if (q.shape != PartShape.Crystal) continue;
                    q.pos -= Quaternion.Euler(q.rot) * Vector3.up * (q.scale.y * 0.5f);
                    p.parts[i] = q;
                }
                shifted = p.parts.Count;
                return new EvoForm
                {
                    name = name, blurb = blurb, parts = p.parts.ToArray(), pivots = p.pivots.ToArray(), element2 = el2, skill = skill,
                    ownPalette = pal.HasValue, palette = pal ?? default
                };
            }

            // handy picks
            public Func<BodyPart, bool> Group(BodyGroup g) => q => q.group == g;
            public Func<BodyPart, bool> AboveHead(float frac = 0.3f) { var s = Skull; float y = s.pos.y + s.scale.y * frac; var g = HeadGroup; return q => q.group == g && q.pos.y > y; }
        }

        static Evo EvoOf(Bundle b, string id)
        {
            var sp = b.species.Find(s => s.id == id);
            if (sp == null) throw new Exception("BuildEvolutions: no species " + id);
            return new Evo(sp);
        }

        static void Forms(Bundle b, string id, EvoForm asc, EvoForm ex) => b.species.Find(s => s.id == id).forms = new[] { asc, ex };

        // ================================================================ EXALTED SKILLS
        static void BuildEvoSkills(Bundle b)
        {
            Ab(b, "meteor_fist", "Meteor Fist", Element.Ember, AbilityKind.Melee, 35, 2.6f, 7f, StatusType.Burn, 0.35f);
            Ab(b, "pile_driver", "Pile Driver", Element.Stone, AbilityKind.Melee, 36, 2.4f, 7.5f);
            Ab(b, "tailwind_kick", "Tailwind Kick", Element.Gale, AbilityKind.Dash, 33, 9f, 6.5f);
            Ab(b, "grove_bloom", "Grove Bloom", Element.Flora, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.3f, 5f);
            Ab(b, "riptide_surge", "Riptide Surge", Element.Tide, AbilityKind.Dash, 33, 9f, 7f, StatusType.Soaked, 0.5f);
            Ab(b, "fox_fire", "Fox Fire", Element.Ember, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Burn, 0.4f, 3f, 20f);
            Ab(b, "acorn_barrage", "Acorn Barrage", Element.Flora, AbilityKind.Projectile, 32, 12f, 6f, StatusType.None, 0f, 3f, 22f);
            Ab(b, "moon_hop", "Moon Hop", Element.Lumen, AbilityKind.Dash, 32, 9f, 6.5f, StatusType.Drowsy, 0.3f);
            Ab(b, "ram_bolt", "Ram Bolt", Element.Volt, AbilityKind.Dash, 34, 9f, 7f, StatusType.Shock, 0.35f);
            Ab(b, "umbral_pounce", "Umbral Pounce", Element.Shade, AbilityKind.Dash, 34, 9f, 6.5f, StatusType.Drowsy, 0.25f);
            Ab(b, "starfall", "Starfall", Element.Lumen, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.None, 0f, 5f);
            Ab(b, "candle_blaze", "Candle Blaze", Element.Ember, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Burn, 0.5f, 5f);
            Ab(b, "storm_dive", "Storm Dive", Element.Gale, AbilityKind.Dash, 34, 10f, 7f, StatusType.Shock, 0.3f);
            Ab(b, "dynamo_burst", "Dynamo Burst", Element.Volt, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Shock, 0.4f, 5f);
            Ab(b, "drill_rush", "Drill Rush", Element.Stone, AbilityKind.Dash, 35, 9f, 7f);
            Ab(b, "avalanche", "Avalanche", Element.Frost, AbilityKind.Burst, 33, 4.5f, 8.5f, StatusType.Chill, 0.45f, 5.5f);
            Ab(b, "glacier_slide", "Glacier Slide", Element.Frost, AbilityKind.Dash, 33, 10f, 7f, StatusType.Chill, 0.4f);
            Ab(b, "prism_quake", "Prism Quake", Element.Stone, AbilityKind.Burst, 34, 4.5f, 8f, StatusType.None, 0f, 5f);
            Ab(b, "shell_crash", "Shell Crash", Element.Tide, AbilityKind.Dash, 34, 8f, 7f, StatusType.Soaked, 0.4f);
            Ab(b, "tidal_starburst", "Tidal Starburst", Element.Lumen, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Soaked, 0.4f, 5f);
            Ab(b, "bog_croak", "Bog Croak", Element.Tide, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.35f, 5.5f);
            Ab(b, "mudslide", "Mudslide", Element.Stone, AbilityKind.Dash, 34, 9f, 7f, StatusType.Soaked, 0.4f);
            Ab(b, "mirage_flare", "Mirage Flare", Element.Ember, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Burn, 0.35f, 3f, 19f);
            Ab(b, "sandstorm", "Sandstorm", Element.Gale, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.None, 0f, 6f);
            Ab(b, "ooze_flood", "Ooze Flood", Element.Shade, AbilityKind.Burst, 31, 4.5f, 8f, StatusType.Poison, 0.5f, 5f);
            Ab(b, "spore_bloom", "Spore Bloom", Element.Flora, AbilityKind.Burst, 31, 4.5f, 8f, StatusType.Poison, 0.45f, 5f);
            Ab(b, "lamp_beam", "Lamp Beam", Element.Lumen, AbilityKind.Projectile, 34, 14f, 6.5f, StatusType.None, 0f, 3f, 26f);
            Ab(b, "fae_dust", "Fae Dust", Element.Lumen, AbilityKind.Burst, 30, 4.5f, 8f, StatusType.Drowsy, 0.5f, 5f);
        }

        // ================================================================ THE LINES
        static void BuildEvolutions(Bundle b)
        {
            const PartShape Sph = PartShape.Sphere, Con = PartShape.Cone, Cry = PartShape.Crystal, Dom = PartShape.Dome, Box = PartShape.Box, Cap = PartShape.Capsule, Cyl = PartShape.Cylinder, Rck = PartShape.Rock;
            const BodyGroup gB = BodyGroup.Body, gH = BodyGroup.Head, gT = BodyGroup.Tail, gX = BodyGroup.Extra, gAL = BodyGroup.ArmL, gAR = BodyGroup.ArmR, gWL = BodyGroup.WingL, gWR = BodyGroup.WingR;
            const ColorSlot cP = ColorSlot.Primary, cS = ColorSlot.Secondary, cA = ColorSlot.Accent, cG = ColorSlot.Glow, cD = ColorSlot.Dark, cW = ColorSlot.White;
            EvoForm A, X;

            // ---------------------------------------------------------------- the partners
            {   // Knuckub: the scrappy cub becomes a heavyweight bear, fists of fire at the end
                var e = EvoOf(b, "knuckub");
                e.Legs(1.12f).Head(0.9f).Arms(1.25f).Body(new Vector3(1.1f, 1.05f, 1.08f));
                e.With(p => { var r = e.B(gAR); p.Mirror(Cyl, gAL, gAR, cA, V(r.center.x, r.min.y + r.size.y * 0.32f, r.center.z), V(r.size.x * 1.05f, 0.06f, r.size.z * 1.05f)); p.Brows(cD); });
                A = e.Form("Brawlbear", "Taller and heavier, with taped wrists and a scowl to match.");
                e.Legs(1.1f).Head(0.9f).Arms(1.25f).Body(new Vector3(1.12f, 1.06f, 1.1f));
                e.With(p =>
                {
                    var bb = e.B(gB); var r = e.B(gAR);
                    p.Mirror(Dom, gAL, gAR, cA, V(r.center.x, r.max.y - 0.04f, r.center.z), V(r.size.x * 0.95f, 0.12f, r.size.z * 1.0f), V(0, 0, -14));
                    p.Mirror(Sph, gAL, gAR, cG, V(r.center.x, r.min.y + 0.02f, r.center.z + 0.02f), V(r.size.x * 0.95f, r.size.x * 0.95f, r.size.x * 0.95f));
                    p.Fan(Con, gB, cS, V(0, bb.max.y - 0.04f, bb.min.z + 0.08f), 0.16f, 5, V(0.07f, 0.18f, 0.07f), -70f, 140f);
                });
                var pal = e.Pal; pal.glow = Hex("#FF8A2A"); pal.accent = Hex("#B8322A");
                X = e.Form("Ursmasher", "A champion brawler. Its fists burn hot enough to glow - it gains the Ember element.", Element.Ember, "meteor_fist", pal);
                Forms(b, "knuckub", A, X);
            }
            {   // Chiskit: the apprentice builder becomes a master mason with a great flat tail
                var e = EvoOf(b, "chiskit");
                e.Legs(1.12f).Head(0.92f).Scale(gT, 1.35f).Body(new Vector3(1.06f, 1.05f, 1.06f));
                e.With(p => { var bb = e.B(gB); p.Add(Cyl, gB, cS, V(0, bb.min.y + bb.size.y * 0.3f, 0), V(bb.size.x * 1.04f, 0.07f, bb.size.z * 1.04f)); });
                A = e.Form("Chiselwright", "A journeyman: a tool belt, a broader tail for slapping mortar flat.");
                e.Legs(1.1f).Head(0.9f).Scale(gT, new Vector3(1.4f, 1.1f, 1.3f)).Body(new Vector3(1.1f, 1.06f, 1.1f)).Arms(1.2f);
                e.With(p =>
                {
                    var bb = e.B(gB); var r = e.B(gAR);
                    p.Mirror(Rck, gAL, gAR, cD, V(r.center.x, r.max.y, r.center.z), V(r.size.x * 1.3f, 0.12f, r.size.x * 1.3f));
                    p.Add(Box, gB, cS, V(0, bb.center.y + 0.04f, bb.min.z - 0.06f), V(bb.size.x * 0.7f, bb.size.y * 0.55f, 0.12f));
                    p.Add(Box, gB, cA, V(0, bb.center.y + bb.size.y * 0.32f, bb.min.z - 0.08f), V(bb.size.x * 0.78f, 0.05f, 0.16f));
                });
                X = e.Form("Grandwright", "Master of the craft, stone pauldrons and a pack of planks on its back. It gains the Stone element.", Element.Stone, "pile_driver");
                Forms(b, "chiskit", A, X);
            }
            {   // Hopling: the scout's ears and scarf grow into a wind-runner's streamers
                var e = EvoOf(b, "hopling");
                e.Head(0.95f).Stretch(e.AboveHead(0.35f), 1.2f).Body(new Vector3(1f, 1.12f, 1f));
                e.With(p => { var s = e.Skull; p.WhipPair(BodyGroup.Tail, BodyGroup.Tail, cA, V(0.08f, s.pos.y - s.scale.y * 0.45f, s.pos.z - s.scale.z * 0.4f), V(0.03f, -0.01f, -0.09f), V(0.01f, 0.01f, -0.01f), 4, 0.07f, 0.04f); });
                A = e.Form("Hopscout", "Longer ears that hear a twig snap a hill away; its scarf streams behind it.");
                e.Stretch(e.AboveHead(0.35f), 1.15f).Body(new Vector3(1.04f, 1.08f, 1.04f));
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fins(cA, V(0.12f, bb.center.y + 0.1f, bb.min.z + 0.12f), 0.6f, 0.24f, 28f, 0.5f);
                    p.Brows(cS);
                });
                X = e.Form("Galehare", "Rides the wind it kicks up; the scarf has grown into wings of cloth. It gains the Gale element.", Element.Gale, "tailwind_kick");
                Forms(b, "hopling", A, X);
            }

            // ---------------------------------------------------------------- Sunmeadow
            {   // Sproutle
                var e = EvoOf(b, "sproutle");
                e.Legs(1.25f).Head(0.94f).Stretch(e.AboveHead(0.3f), 1.3f, 1.15f);
                e.With(p => { var r = e.B(gAR); p.Fronds(gAR, cA, 3, 0.16f, 0.07f, 0.1f, V(r.max.x, r.min.y + 0.02f, r.center.z)); p.Fronds(gAL, cA, 3, 0.16f, 0.07f, 0.1f, V(-r.max.x, r.min.y + 0.02f, r.center.z)); });
                A = e.Form("Blossomle", "Its petals open wide and leaves sprout from its hands.");
                e.Legs(1.15f).Body(new Vector3(1.08f, 1.12f, 1.08f)).Recolor(e.Group(BodyGroup.LegFL), cD).Recolor(e.Group(BodyGroup.LegFR), cD);
                e.With(p =>
                {
                    var s = e.Skull; float top = s.pos.y + s.scale.y * 0.45f;
                    p.Add(Sph, gH, cA, V(0, top + 0.12f, s.pos.z - 0.04f), V(0.42f, 0.26f, 0.38f));
                    p.Add(Sph, gH, cA, V(0.16f, top + 0.06f, s.pos.z - 0.08f), V(0.26f, 0.2f, 0.24f));
                    p.Add(Sph, gH, cA, V(-0.16f, top + 0.06f, s.pos.z - 0.08f), V(0.26f, 0.2f, 0.24f));
                    p.Ring(Sph, gH, cG, V(0, top + 0.16f, s.pos.z - 0.04f), 0.2f, 6, V(0.07f, 0.07f, 0.07f));
                });
                X = e.Form("Arborle", "A walking sapling with a blossoming crown and bark-brown legs.", Element.Normal, "grove_bloom");
                Forms(b, "sproutle", A, X);
            }
            {   // Driplet
                var e = EvoOf(b, "driplet");
                e.Body(new Vector3(1.05f, 1.2f, 1.05f)).Stretch(e.AboveHead(0.3f), 1.3f, 1.1f);
                e.With(p => { var bb = e.B(gB); p.Spikes(gB, cS, bb.max.y - 0.1f, 0.05f, -0.25f, 3, 0.1f, 0.16f, 0.02f, -30f); });
                A = e.Form("Puddlop", "Taller and wobblier, with a crest that sloshes when it hops.");
                e.Body(new Vector3(1.1f, 1.15f, 1.1f));
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fan(Con, gB, cS, V(0, bb.max.y - 0.12f, bb.min.z + 0.12f), 0.22f, 5, V(0.1f, 0.3f, 0.06f), -60f, 120f);
                    p.Ring(Sph, gB, cG, V(0, bb.center.y + 0.1f, 0), bb.size.x * 0.62f, 6, V(0.09f, 0.11f, 0.09f));
                });
                X = e.Form("Torrentide", "A walking wave with a curling crest, ringed by drops that never fall.", Element.Normal, "riptide_surge");
                Forms(b, "driplet", A, X);
            }
            {   // Cindling
                var e = EvoOf(b, "cindling");
                e.Legs(1.3f).Head(0.86f).Body(new Vector3(1f, 1.05f, 1.12f)).Scale(gT, 1.3f);
                A = e.Form("Cinderfox", "Leggy and quick now, its tail a proper brush of flame.");
                e.Legs(1.15f).Head(0.9f).Body(new Vector3(1.04f, 1.04f, 1.12f)).Scale(gT, 1.25f).Stretch(e.AboveHead(0.2f), 1.25f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fan(Con, gB, cG, V(0, bb.max.y - 0.02f, bb.max.z - 0.08f), 0.14f, 7, V(0.06f, 0.16f, 0.06f), -80f, 160f);
                    var t = e.B(gT);
                    p.Spikes(gT, cG, t.max.y - 0.08f, t.min.z + 0.05f, t.max.z - 0.05f, 3, 0.06f, 0.14f, 0f, -40f);
                });
                X = e.Form("Pyrevix", "A slender fire fox with a mane of flame running down its neck.", Element.Normal, "fox_fire");
                Forms(b, "cindling", A, X);
            }
            {   // Acornip
                var e = EvoOf(b, "acornip");
                e.Legs(1.6f).Arms(1.2f).Head(0.95f);
                e.With(p => { var s = e.B(gH); p.Fronds(gH, cG, 3, 0.18f, 0.08f, 0.6f, V(0, s.max.y + 0.02f, s.center.z)); });
                A = e.Form("Oakernip", "Up on proper legs; its cap has put out its first leaves.");
                e.Legs(1.25f).Body(new Vector3(1.12f, 1.1f, 1.1f)).Arms(1.2f);
                e.With(p =>
                {
                    var s = e.B(gH); var r = e.B(gAL);
                    p.Fronds(gH, cG, 6, 0.24f, 0.1f, 0.35f, V(0, s.max.y + 0.03f, s.center.z), 30f);
                    p.Add(Dom, gAL, cS, V(r.min.x - 0.04f, r.center.y, r.center.z), V(0.08f, 0.28f, 0.28f), V(0, 0, 90));
                });
                X = e.Form("Oakenward", "An oak guardian with a leafy crown and an acorn-cap shield. It gains the Stone element.", Element.Stone, "acorn_barrage");
                Forms(b, "acornip", A, X);
            }
            {   // Bibbit
                var e = EvoOf(b, "bibbit");
                e.Body(new Vector3(1f, 1.12f, 1f)).Stretch(e.AboveHead(0.2f), 1.25f).Scale(gT, 1.35f);
                A = e.Form("Bibbun", "Longer ears and a much bigger puff of a tail.");
                e.Body(new Vector3(1.06f, 1.08f, 1.06f)).Stretch(e.AboveHead(0.2f), 1.2f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.Ring(Sph, gB, cW, V(0, bb.max.y - 0.06f, bb.center.z), bb.size.x * 0.36f, 8, V(0.11f, 0.09f, 0.11f));
                    p.Add(Sph, gH, cG, V(0, s.pos.y + s.scale.y * 0.3f, s.pos.z + s.scale.z * 0.42f), V(0.1f, 0.1f, 0.03f));
                });
                X = e.Form("Bunnoble", "A moon rabbit with a lace ruff and a glowing mark on its brow. It gains the Lumen element.", Element.Lumen, "moon_hop");
                Forms(b, "bibbit", A, X);
            }
            {   // Fuzzap
                var e = EvoOf(b, "fuzzap");
                e.Legs(1.45f).Head(0.9f);
                e.With(p => { var s = e.Skull; p.WhipPair(gH, gH, cA, V(s.scale.x * 0.32f, s.pos.y + s.scale.y * 0.3f, s.pos.z - 0.02f), V(0.05f, 0.03f, -0.03f), V(0f, -0.03f, 0.02f), 3, 0.08f, 0.05f); });
                A = e.Form("Fuzzvolt", "Its legs have caught up with its wool, and little horns are curling in.");
                e.Legs(1.2f).Body(1.12f).Head(0.92f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.WhipPair(gH, gH, cA, V(s.scale.x * 0.34f, s.pos.y + s.scale.y * 0.32f, s.pos.z - 0.03f), V(0.07f, 0.04f, -0.04f), V(0f, -0.06f, 0.04f), 5, 0.12f, 0.06f);
                    p.Spikes(gB, cG, bb.max.y - 0.08f, bb.max.z - 0.15f, bb.min.z + 0.15f, 3, 0.06f, 0.22f, 0.02f, -10f, 0f, 0f, Cry);
                });
                X = e.Form("Thunderam", "A storm ram with great curled horns and lightning rods in its fleece. It gains the Stone element.", Element.Stone, "ram_bolt");
                Forms(b, "fuzzap", A, X);
            }
            {   // Inkit
                var e = EvoOf(b, "inkit");
                e.Legs(1.35f).Head(0.86f).Body(new Vector3(1f, 1f, 1.15f)).Scale(gT, 1.2f);
                A = e.Form("Inkat", "Grown long and sleek, a shadow with a tail like a question mark.");
                e.Legs(1.15f).Head(0.92f).Body(new Vector3(1.06f, 1.04f, 1.12f)).Scale(gT, 1.15f).Stretch(e.AboveHead(0.2f), 1.2f);
                e.With(p =>
                {
                    var bb = e.B(gB); var s = e.Skull;
                    p.Spikes(gB, cD, bb.max.y - 0.05f, bb.max.z - 0.08f, bb.min.z + 0.06f, 5, 0.06f, 0.16f, 0.02f, -35f);
                    p.Add(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z + s.scale.z * 0.25f), V(0.05f, 0.16f, 0.05f), V(25, 0, 0));
                });
                X = e.Form("Nightprowl", "A panther of the dark with a ridge of shadow down its back and a single glowing horn.", Element.Normal, "umbral_pounce");
                Forms(b, "inkit", A, X);
            }
            {   // Twinklet
                var e = EvoOf(b, "twinklet");
                e.Body(new Vector3(1.12f, 1.12f, 1.05f)).Scale(gX, 1.3f);
                A = e.Form("Glintstar", "Its points have grown sharp and its sparkles swing wider.");
                e.Body(1.08f).Scale(gX, 1.2f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Sph, gX, cG, V(0, bb.max.y + 0.08f, 0), 0.22f, 8, V(0.06f, 0.06f, 0.06f));
                    p.Fan(Con, gB, cA, V(0, bb.center.y + 0.05f, bb.min.z + 0.02f), bb.size.y * 0.55f, 5, V(0.06f, 0.2f, 0.04f), 36f);
                });
                X = e.Form("Astrelle", "A full star with a halo of lights and a second set of points behind. It gains the Gale element.", Element.Gale, "starfall");
                Forms(b, "twinklet", A, X);
            }
            {   // Wickit
                var e = EvoOf(b, "wickit");
                e.Body(new Vector3(1f, 1.25f, 1f)).Scale(gX, 1.3f);
                e.With(p => { var bb = e.B(gB); p.Mirror(Sph, gB, cS, V(bb.max.x - 0.04f, bb.max.y - 0.12f, 0.06f), V(0.07f, 0.16f, 0.07f)); p.Add(Sph, gB, cS, V(-0.05f, bb.max.y - 0.1f, -bb.size.z * 0.4f), V(0.07f, 0.2f, 0.07f)); });
                A = e.Form("Waxwick", "Taller, with drips of wax down its sides and a brighter flame.");
                e.Body(new Vector3(1.08f, 1.12f, 1.08f));
                e.With(p =>
                {
                    var bb = e.B(gB); float top = bb.max.y;
                    p.Mirror(Cyl, gB, cS, V(bb.size.x * 0.5f + 0.06f, top - 0.05f, -0.02f), V(0.06f, 0.24f, 0.06f));
                    p.Mirror(Con, gB, cG, V(bb.size.x * 0.5f + 0.06f, top + 0.15f, -0.02f), V(0.1f, 0.2f, 0.1f));
                    p.Ring(Con, gB, cA, V(0, top - 0.02f, 0), bb.size.x * 0.34f, 6, V(0.05f, 0.1f, 0.05f), 10f);
                });
                X = e.Form("Candelord", "A living candelabra crowned in wax, three flames instead of one. It gains the Lumen element.", Element.Lumen, "candle_blaze");
                Forms(b, "wickit", A, X);
            }
            {   // Zephyrkit
                var e = EvoOf(b, "zephyrkit");
                e.Legs(1.3f).Head(0.88f).Wings(1.45f).Body(new Vector3(1f, 1f, 1.12f));
                A = e.Form("Skylynx", "Long legs and wide wings - it can glide from tree to tree.");
                e.Legs(1.15f).Head(0.92f).Wings(1.4f).Body(new Vector3(1.05f, 1.05f, 1.1f)).Scale(gT, 1.2f).Stretch(e.AboveHead(0.2f), 1.15f);
                e.With(p => { var s = e.Skull; p.Ears(Con, cG, 0.04f, 0.12f, 10f, 0.02f); p.Brows(cS); });
                X = e.Form("Tempestrix", "A storm lynx with great wings and tufted, crackling ears. It gains the Volt element.", Element.Volt, "storm_dive");
                Forms(b, "zephyrkit", A, X);
            }
            {   // Fennick (Sunscour Dunes)
                var e = EvoOf(b, "fennick");
                e.Legs(1.3f).Head(0.9f).Stretch(e.AboveHead(0.2f), 1.2f).Scale(gT, 1.15f);
                A = e.Form("Sandfennec", "Leggier, with even bigger ears for hearing beetles under the sand.");
                e.Legs(1.12f).Head(0.92f).Stretch(e.AboveHead(0.2f), 1.2f);
                e.Dup(e.Group(gT), q => { q.pos.x = q.pos.x * 0.6f + 0.06f; q.rot.y += 16f; return q; });
                e.Recolor(q => q.group == gT && q.pos.x > 0.04f, cG);
                e.With(p => { var s = e.Skull; p.Add(Sph, gH, cG, V(0, s.pos.y + s.scale.y * 0.3f, s.pos.z + s.scale.z * 0.44f), V(0.07f, 0.07f, 0.03f)); });
                X = e.Form("Sunfennec", "A desert fox of the noon sun with twin tails, one of them burning.", Element.Normal, "mirage_flare");
                Forms(b, "fennick", A, X);
            }

            // ---------------------------------------------------------------- Badlands, Peaks
            {   // Sparkrat
                var e = EvoOf(b, "sparkrat");
                e.Head(0.92f).Body(new Vector3(1.05f, 1.15f, 1.05f)).Scale(gT, 1.2f);
                e.With(p => { var t = e.B(gT); p.Add(Cry, gT, cG, V(0, t.max.y + 0.02f, t.min.z + 0.04f), V(0.08f, 0.22f, 0.04f), V(0, 0, 25)); });
                A = e.Form("Zaprat", "A bigger rat with a bolt of crystal on the tip of its tail.");
                e.Body(new Vector3(1.08f, 1.1f, 1.08f)).Head(0.95f).Stretch(e.AboveHead(0.2f), 1.25f);
                e.With(p => { p.Cheeks(cG); var t = e.B(gT); p.Spikes(gT, cG, t.center.y, t.max.z - 0.05f, t.min.z + 0.1f, 3, 0.05f, 0.12f, 0f, -60f, 0f, 0f, Cry); });
                X = e.Form("Dynamouse", "Its cheeks hold a charge you can see, and its tail is a lightning rod.", Element.Normal, "dynamo_burst");
                Forms(b, "sparkrat", A, X);
            }
            {   // Burrowby
                var e = EvoOf(b, "burrowby");
                e.Body(new Vector3(1.08f, 1.08f, 1.1f)).Scale(BodyGroup.LegFL, 1.35f).Scale(BodyGroup.LegFR, 1.35f);
                A = e.Form("Delvemole", "Broad digging claws that can tunnel through packed clay.");
                e.Body(new Vector3(1.1f, 1.1f, 1.08f));
                e.With(p =>
                {
                    var h = e.B(gH); var bb = e.B(gB);
                    p.Add(Con, gH, cD, V(0, h.center.y, h.max.z + 0.1f), V(0.12f, 0.3f, 0.12f), V(90, 0, 0));
                    p.Ring(Rck, gB, cD, V(0, bb.max.y - 0.06f, bb.center.z - 0.04f), bb.size.x * 0.28f, 4, V(0.14f, 0.1f, 0.14f));
                });
                X = e.Form("Bormolith", "A drill-nosed digger plated in stone; it bores straight through rock.", Element.Normal, "drill_rush");
                Forms(b, "burrowby", A, X);
            }
            {   // Snowmote
                var e = EvoOf(b, "snowmote");
                e.Body(new Vector3(1.12f, 1.15f, 1.12f)).Stretch(q => q.shape == Con, 1.45f);
                A = e.Form("Frostmote", "A bigger snowball with a long icicle horn.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Cry, gB, cS, V(0, bb.max.y - 0.1f, -0.02f), bb.size.x * 0.26f, 7, V(0.09f, 0.3f, 0.09f), 20f);
                    p.Mirror(Sph, BodyGroup.ArmL, BodyGroup.ArmR, cS, V(bb.max.x - 0.02f, bb.center.y - 0.04f, 0.04f), V(0.16f, 0.14f, 0.16f));
                    p.Brows(cS, 0.12f, 0.2f);
                });
                X = e.Form("Avalord", "A blizzard with a face, crowned in icicles. It gains the Gale element.", Element.Gale, "avalanche");
                Forms(b, "snowmote", A, X);
            }
            {   // Chillpip
                var e = EvoOf(b, "chillpip");
                e.Legs(1.6f).Head(0.9f).Body(new Vector3(1f, 1.15f, 1f)).Stretch(e.AboveHead(0.3f), 1.25f);
                A = e.Form("Frostpip", "A taller penguin with a proud crest.");
                e.Body(new Vector3(1.08f, 1.1f, 1.06f)).Head(0.95f).Arms(1.2f);
                e.With(p =>
                {
                    var s = e.Skull; var r = e.B(gAR);
                    p.Ring(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z - 0.02f), s.scale.x * 0.26f, 5, V(0.05f, 0.14f, 0.05f), 14f);
                    p.Mirror(Con, gAL, gAR, cG, V(r.max.x, r.min.y + 0.04f, r.center.z), V(0.05f, 0.2f, 0.12f), V(0, 0, 160));
                });
                X = e.Form("Emperice", "An emperor of the ice with a crystal crown and flippers like blades. It gains the Tide element.", Element.Tide, "glacier_slide");
                Forms(b, "chillpip", A, X);
            }
            {   // Geodeling
                var e = EvoOf(b, "geodeling");
                e.Body(new Vector3(1.1f, 1.08f, 1.1f)).Arms(1.3f).Stretch(q => q.shape == Cry, 1.4f, 1.1f);
                A = e.Form("Geodite", "Its crystals have grown through the rock, and its arms are heavier.");
                e.Body(new Vector3(1.12f, 1.12f, 1.1f)).Arms(1.2f).Stretch(q => q.shape == Cry, 1.2f);
                e.With(p => { var r = e.B(gAR); p.Mirror(Cry, gAL, gAR, cG, V(r.center.x, r.max.y - 0.02f, r.center.z), V(0.08f, 0.24f, 0.08f), V(0, 0, -20)); });
                X = e.Form("Geocolossus", "A walking geode, crystal bursting from its shoulders.", Element.Normal, "prism_quake");
                Forms(b, "geodeling", A, X);
            }

            // ---------------------------------------------------------------- outer lands
            {   // Shellby (Driftkey Isles)
                var e = EvoOf(b, "shellby");
                e.Legs(1.3f).Head(0.92f).Body(new Vector3(1.1f, 1.08f, 1.1f));
                e.With(p => { var bb = e.B(gB); p.Spikes(gB, cS, bb.max.y - 0.06f, bb.max.z - 0.12f, bb.min.z + 0.12f, 3, 0.07f, 0.12f, 0.02f, -20f); });
                A = e.Form("Shellguard", "A bigger shell with a ridge of studs along it.");
                e.Body(1.12f).Legs(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fronds(gB, cA, 5, 0.18f, 0.06f, 1.2f, V(0.1f, bb.max.y - 0.02f, -0.05f));
                    p.Fronds(gB, cA, 4, 0.14f, 0.05f, 1.2f, V(-0.12f, bb.max.y - 0.04f, -0.12f), 20f);
                    p.Ring(Rck, gB, cD, V(0, bb.min.y + bb.size.y * 0.42f, 0), bb.size.x * 0.48f, 6, V(0.1f, 0.08f, 0.1f));
                });
                X = e.Form("Reefback", "A sea turtle that carries a coral reef on its shell. It gains the Stone element.", Element.Stone, "shell_crash");
                Forms(b, "shellby", A, X);
            }
            {   // Starlet (Driftkey Isles)
                var e = EvoOf(b, "starlet");
                var c0 = e.B(gB).center;
                e.Scale(gB, new Vector3(1.15f, 1.15f, 1.1f), new Vector3(c0.x, 0f, c0.z));
                A = e.Form("Starlette", "Longer points and a brighter middle.");
                e.Scale(gB, new Vector3(1.1f, 1.1f, 1.1f), new Vector3(c0.x, 0f, c0.z));
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fan(Con, gB, cG, V(0, bb.max.y + 0.02f, 0.02f), 0.08f, 5, V(0.04f, 0.12f, 0.04f), -60f, 120f);
                    p.Ring(Sph, gB, cW, V(0, bb.center.y, 0.02f), bb.size.x * 0.32f, 5, V(0.07f, 0.07f, 0.07f), 0f, 36f);
                });
                X = e.Form("Stellamare", "A sea star queen with a tiara of light and a string of pearls.", Element.Normal, "tidal_starburst");
                Forms(b, "starlet", A, X);
            }
            {   // Tadpoll (Mirefen): the one that really changes - tadpole to frog
                var e = EvoOf(b, "tadpoll");
                e.Scale(gT, 0.7f).Body(new Vector3(1.08f, 1.08f, 1.08f));
                e.With(p => { var bb = e.B(gB); p.Mirror(Sph, gB, cP, V(bb.size.x * 0.42f, 0.08f, bb.min.z + 0.12f), V(0.14f, 0.14f, 0.22f), V(0, 20, 0)); });
                A = e.Form("Froglet", "Halfway there: back legs, and a tail that is shrinking.");
                e.Remove(e.Group(gT)).Body(new Vector3(1.12f, 1.05f, 1.12f));
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Mirror(Sph, gB, cP, V(bb.size.x * 0.46f, 0.1f, bb.min.z + 0.16f), V(0.18f, 0.2f, 0.32f), V(0, 20, 0));
                    p.Add(Sph, gB, cA, V(0, bb.min.y + bb.size.y * 0.25f, bb.max.z - 0.06f), V(bb.size.x * 0.45f, bb.size.y * 0.32f, 0.12f));
                    p.Mirror(Sph, gB, cS, V(bb.size.x * 0.2f, bb.max.y - 0.02f, bb.center.z + 0.04f), V(0.12f, 0.1f, 0.12f));
                });
                X = e.Form("Bullmire", "A full-grown bullfrog of the fen with a booming throat sac. It gains the Shade element.", Element.Shade, "bog_croak");
                Forms(b, "tadpoll", A, X);
            }
            {   // Mudpup (Mirefen)
                var e = EvoOf(b, "mudpup");
                e.Legs(1.3f).Head(0.88f).Body(new Vector3(1.08f, 1.06f, 1.12f));
                A = e.Form("Mudhound", "A sturdy young hound that loves a wallow.");
                e.Legs(1.12f).Head(0.92f).Body(new Vector3(1.12f, 1.1f, 1.08f));
                e.With(p =>
                {
                    var bb = e.B(gB); var h = e.B(gH);
                    p.Spikes(gB, cD, bb.max.y - 0.06f, bb.max.z - 0.1f, bb.min.z + 0.1f, 4, 0.12f, 0.1f, 0.02f, -15f, 0f, 0f, Rck);
                    p.Ring(Con, gH, cS, V(0, h.min.y + 0.06f, PivotZ(e, gH)), h.size.x * 0.32f, 6, V(0.04f, 0.1f, 0.04f), 90f);
                });
                X = e.Form("Bogmastiff", "A swamp mastiff armoured in caked mud, with a spiked collar.", Element.Normal, "mudslide");
                Forms(b, "mudpup", A, X);
            }
            {   // Dunelet (Sunscour Dunes)
                var e = EvoOf(b, "dunelet");
                e.Body(new Vector3(1.08f, 1.15f, 1.08f)).Scale(gX, 1.3f);
                A = e.Form("Dunespin", "Taller, and its ring of sand whirls wider.");
                e.Body(1.08f).Scale(gX, 1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB); var x = e.B(gX);
                    p.Ring(Sph, gX, cS, V(0, x.center.y + 0.32f, 0), x.size.x * 0.4f, 6, V(0.09f, 0.07f, 0.09f), 0f, 30f);
                    p.Horns(cD, 0.06f, 0.14f, 25f, -10f);
                });
                X = e.Form("Siroccor", "A spirit of the hot wind, two rings of sand spinning round it.", Element.Normal, "sandstorm");
                Forms(b, "dunelet", A, X);
            }
            {   // Oozlet (Blightmoor)
                var e = EvoOf(b, "oozlet");
                e.Body(new Vector3(1.05f, 1.2f, 1.05f)).Scale(gX, 1.3f);
                e.With(p => { var bb = e.B(gB); p.Mirror(Sph, gB, cS, V(bb.max.x - 0.05f, bb.min.y + 0.15f, 0.05f), V(0.08f, 0.18f, 0.08f)); });
                A = e.Form("Oozeling", "A taller slime with slow drips running down it.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Con, gB, cA, V(0, bb.max.y - 0.05f, 0), bb.size.x * 0.22f, 5, V(0.07f, 0.18f, 0.07f), 12f);
                    p.Ring(Sph, gB, cS, V(0, bb.min.y + 0.03f, 0), bb.size.x * 0.5f, 7, V(0.14f, 0.06f, 0.14f));
                });
                X = e.Form("Oozelord", "A crowned king of the bog slime, sitting in its own puddle.", Element.Normal, "ooze_flood");
                Forms(b, "oozlet", A, X);
            }
            {   // Sporeling (Blightmoor)
                var e = EvoOf(b, "sporeling");
                e.Legs(1.5f).Body(new Vector3(1.1f, 1.3f, 1.1f)).Scale(gH, new Vector3(1.15f, 1.05f, 1.15f));
                A = e.Form("Capsporel", "Up on longer legs under a wider cap.");
                e.Legs(1.15f).Body(new Vector3(1.08f, 1.15f, 1.08f)).Scale(gH, new Vector3(1.2f, 1.08f, 1.2f));
                e.With(p =>
                {
                    var h = e.B(gH);
                    p.Ring(Sph, gH, cG, V(0, h.max.y - h.size.y * 0.2f, h.center.z), h.size.x * 0.34f, 7, V(0.07f, 0.05f, 0.07f));
                    p.Ring(Con, gH, cS, V(0, h.min.y + 0.02f, h.center.z), h.size.x * 0.42f, 8, V(0.03f, 0.1f, 0.03f), 180f);
                });
                X = e.Form("Myconarch", "A great toadstool lord with glowing spots and a fringe of gills.", Element.Normal, "spore_bloom");
                Forms(b, "sporeling", A, X);
            }
            {   // Glowmite (Glowdeep)
                var e = EvoOf(b, "glowmite");
                e.Body(new Vector3(1.1f, 1.08f, 1.15f));
                e.With(p => { var bb = e.B(gB); p.Add(Con, gB, cA, V(0, bb.max.y - 0.04f, bb.max.z - 0.08f), V(0.06f, 0.2f, 0.06f), V(40, 0, 0)); });
                A = e.Form("Glimbeetle", "A real beetle now, with a small horn.");
                e.Body(new Vector3(1.1f, 1.1f, 1.12f));
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Add(Con, gB, cG, V(0, bb.max.y + 0.02f, bb.max.z - 0.02f), V(0.08f, 0.3f, 0.08f), V(35, 0, 0));
                    p.Fins(cS, V(0.08f, bb.max.y - 0.06f, bb.center.z - 0.02f), 0.28f, 0.34f, 18f, 0.2f);
                });
                X = e.Form("Lampscarab", "A shining scarab with a lamp of a horn and wing cases that open to fly. It gains the Stone element.", Element.Stone, "lamp_beam");
                Forms(b, "glowmite", A, X);
            }
            {   // Pixiwing (Glowdeep)
                var e = EvoOf(b, "pixiwing");
                e.Wings(1.3f).Stretch(e.AboveHead(0.3f), 1.3f);
                A = e.Form("Pixielle", "Longer antennae and broader wings.");
                e.Wings(1.2f).Body(new Vector3(1f, 1.15f, 1f));
                e.Dup(q => q.group == gWL || q.group == gWR, q => { q.pos.y -= 0.14f; q.pos.x *= 0.85f; q.scale *= 0.7f; q.rot.z -= 25f * Mathf.Sign(q.pos.x); return q; });
                e.With(p => { var s = e.Skull; p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.48f, s.pos.z), 0.08f, 5, V(0.03f, 0.1f, 0.03f), -50f, 100f); });
                X = e.Form("Faequeen", "A fairy queen with four wings and a crown of light.", Element.Normal, "fae_dust");
                Forms(b, "pixiwing", A, X);
            }
        }

        static float PivotZ(Evo e, BodyGroup g) => e.PivotOf(g).z;
    }
}
