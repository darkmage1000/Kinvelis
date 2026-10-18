using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Real evolutions, fourth wave (2026-10-18, user: "4th wave"). The small Uncommons of the outer lands and the
    /// three cave Uncommons: Bubblefin, Parrokeet, Clampearl, Jellume, Krakling (Isles), Bogsnap, Rushcat, Sporetoad,
    /// Leechlash (Mirefen), Frillizard, Prickpear, Sandjackal, Rattlecoil (Dunes), Silkspin, Fumeskull, Venomoth,
    /// Acidrake (Blightmoor), Prismfox, Noctowlet, Bulbfrog, Shardillo, Sporeglide (Glowdeep), Glimcrab, Mushmaw,
    /// Stalagrin (caves). Same tools and rules as the earlier waves; single-element kinds that gain: Rushcat +Tide,
    /// Sandjackal +Lumen.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildEvoSkills4(Bundle b)
        {
            Ab(b, "bubble_barrage", "Bubble Barrage", Element.Tide, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Soaked, 0.45f, 3f, 20f);
            Ab(b, "copycat_squawk", "Copycat Squawk", Element.Gale, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.35f, 5.5f);
            Ab(b, "pearl_shot", "Pearl Shot", Element.Lumen, AbilityKind.Projectile, 34, 14f, 6.5f, StatusType.None, 0f, 3f, 26f);
            Ab(b, "sting_shower", "Sting Shower", Element.Tide, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Shock, 0.4f, 5.5f);
            Ab(b, "ink_whirlpool", "Ink Whirlpool", Element.Tide, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Soaked, 0.45f, 5.5f);
            Ab(b, "snap_trap", "Snap Trap", Element.Flora, AbilityKind.Melee, 35, 2.6f, 7f, StatusType.Drowsy, 0.25f);
            Ab(b, "reed_ambush", "Reed Ambush", Element.Flora, AbilityKind.Dash, 33, 9f, 6.5f, StatusType.Soaked, 0.25f);
            Ab(b, "mold_burst", "Mold Burst", Element.Shade, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.5f, 5.5f);
            Ab(b, "draining_lash", "Draining Lash", Element.Shade, AbilityKind.Melee, 33, 3f, 6.5f, StatusType.Poison, 0.4f);
            Ab(b, "frill_flash", "Frill Flash", Element.Ember, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Burn, 0.4f, 5f);
            Ab(b, "thorny_hug", "Thorny Hug", Element.Flora, AbilityKind.Melee, 35, 2.4f, 7f);
            Ab(b, "pharaohs_gaze", "Pharaoh's Gaze", Element.Lumen, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Drowsy, 0.35f, 5.5f);
            Ab(b, "rattle_strike", "Rattle Strike", Element.Shade, AbilityKind.Dash, 33, 9f, 6.5f, StatusType.Poison, 0.45f);
            Ab(b, "web_snare", "Web Snare", Element.Shade, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Drowsy, 0.4f, 3f, 20f);
            Ab(b, "skull_cackle", "Skull Cackle", Element.Shade, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Burn, 0.35f, 5.5f);
            Ab(b, "hypno_dust", "Hypno Dust", Element.Shade, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.45f, 5.5f);
            Ab(b, "acid_breath", "Acid Breath", Element.Shade, AbilityKind.Projectile, 34, 13f, 6.5f, StatusType.Poison, 0.45f, 3f, 20f);
            Ab(b, "spectrum_ray", "Spectrum Ray", Element.Lumen, AbilityKind.Projectile, 34, 14f, 6.5f, StatusType.Chill, 0.35f, 3f, 26f);
            Ab(b, "moon_gaze", "Moon Gaze", Element.Shade, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.45f, 5.5f);
            Ab(b, "flash_croak", "Flash Croak", Element.Lumen, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Drowsy, 0.35f, 5.5f);
            Ab(b, "geode_roll", "Geode Roll", Element.Stone, AbilityKind.Dash, 35, 9f, 7f);
            Ab(b, "drift_spores", "Drift Spores", Element.Flora, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.45f, 5.5f);
            Ab(b, "crystal_pinch", "Crystal Pinch", Element.Stone, AbilityKind.Melee, 35, 2.4f, 7f, StatusType.Chill, 0.2f);
            Ab(b, "gnash_bite", "Gnash Bite", Element.Flora, AbilityKind.Melee, 34, 2.4f, 6.5f, StatusType.Poison, 0.4f);
            Ab(b, "stalag_slam", "Stalag Slam", Element.Stone, AbilityKind.Melee, 35, 2.6f, 7f, StatusType.Chill, 0.25f);
        }

        static void BuildEvolutions4(Bundle b)
        {
            const PartShape Sph = PartShape.Sphere, Con = PartShape.Cone, Cry = PartShape.Crystal, Dom = PartShape.Dome, Cyl = PartShape.Cylinder, Box = PartShape.Box;
            const BodyGroup gB = BodyGroup.Body, gH = BodyGroup.Head, gT = BodyGroup.Tail, gX = BodyGroup.Extra, gWL = BodyGroup.WingL, gWR = BodyGroup.WingR, gAL = BodyGroup.ArmL, gAR = BodyGroup.ArmR;
            const ColorSlot cS = ColorSlot.Secondary, cA = ColorSlot.Accent, cG = ColorSlot.Glow, cD = ColorSlot.Dark, cW = ColorSlot.White;
            EvoForm A, X;
            bool Wing(BodyPart q) => q.group == gWL || q.group == gWR;
            BodyPart Main(Evo e, BodyGroup g)
            {
                BodyPart best = default; float vol = -1f;
                foreach (var q in e.p.parts)
                {
                    if (q.group != g || q.shape == Con || q.shape == Cry) continue;
                    float v = q.scale.x * q.scale.y * q.scale.z;
                    if (v > vol) { vol = v; best = q; }
                }
                return best;
            }
            float Top(Evo e)
            {
                var m = Main(e, gB); var r = Quaternion.Euler(m.rot);
                float half = 0.5f * (Mathf.Abs((r * Vector3.right).y) * m.scale.x + Mathf.Abs((r * Vector3.up).y) * m.scale.y + Mathf.Abs((r * Vector3.forward).y) * m.scale.z);
                return m.pos.y + half * 0.9f;
            }
            BodyGroup HeadG(Evo e) => e.Has(gH) ? gH : gB;
            float HeadTop(Evo e) => e.B(HeadG(e)).max.y;

            // ---------------------------------------------------------------- Driftkey Isles
            {   // Bubblefin: a bigger bubble ring, then a bell of coral and a crown of bubbles
                var e = EvoOf(b, "bubblefin");
                e.Body(1.2f).Wings(1.2f).Scale(gX, 1.25f);
                A = e.Form("Bubbleray", "Its bubble ring has grown wide enough to swim through.");
                e.Body(1.1f).Scale(gT, 1.3f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Ring(Cyl, gB, cA, V(0, t - 0.02f, m.pos.z), 0.12f, 6, V(0.035f, 0.2f, 0.035f), 30f);
                    p.Ring(Sph, gB, cW, V(0, t + 0.12f, m.pos.z), 0.16f, 6, V(0.07f, 0.07f, 0.07f));
                });
                X = e.Form("Tidebell", "A reef fish wearing a bell of coral, a crown of bubbles that never pop. It fires them like cannonballs.", Element.Normal, "bubble_barrage");
                Forms(b, "bubblefin", A, X);
            }
            {   // Parrokeet: longer tail and wings, then a macaw with a great crest
                var e = EvoOf(b, "parrokeet");
                e.Wings(1.2f).Body(1.15f).Head(0.92f).Scale(gT, 1.35f);
                A = e.Form("Parrokite", "Its tail streams behind it like a kite's. It repeats whole sentences now.");
                e.Scale(gT, 1.2f).Wings(1.1f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Fan(Con, gH, cA, V(0, s.pos.y + s.scale.y * 0.32f, s.pos.z - s.scale.z * 0.2f), 0.08f, 7, V(0.05f, 0.24f, 0.03f), -70f, 140f);
                    p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.32f, s.pos.z - s.scale.z * 0.24f), 0.06f, 5, V(0.04f, 0.17f, 0.03f), -50f, 100f);
                });
                X = e.Form("Plumecaw", "A great island macaw with a crest like a sunrise. Its squawk copies whatever scared you last.", Element.Normal, "copycat_squawk");
                Forms(b, "parrokeet", A, X);
            }
            {   // Clampearl: a bigger clam, then a fluted shell with a crown of pearls
                var e = EvoOf(b, "clampearl");
                e.Body(1.25f).Scale(gX, 1.2f);
                A = e.Form("Pearlclam", "Its shell has grown fluted and heavy; its pearl, smug.");
                e.Body(1.15f);
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g); var bb = e.B(gB);
                    p.Ring(Sph, g, cW, V(0, hb.max.y + 0.03f, hb.center.z), hb.size.x * 0.32f, 6, V(0.06f, 0.06f, 0.06f));
                });
                X = e.Form("Pearlmonarch", "A great fluted clam whose pearl wears a crown of smaller pearls. It shoots them. Hard.", Element.Normal, "pearl_shot");
                Forms(b, "clampearl", A, X);
            }
            {   // Jellume: a bigger bell, then a moon jelly with a ring of lights and long tentacles
                var e = EvoOf(b, "jellume");
                e.Body(1.2f).Scale(gT, 1.25f);
                A = e.Form("Jellantern", "Its bell glows steady enough to read by; its tentacles reach the ground.");
                e.Body(1.1f).Scale(gT, 1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Sph, gB, cG, V(0, bb.center.y - bb.size.y * 0.15f, bb.center.z), bb.size.x * 0.5f, 9, V(0.06f, 0.05f, 0.06f));
                    p.Add(Sph, gB, cG, V(0, bb.max.y - 0.04f, bb.center.z), V(0.12f, 0.08f, 0.12f));
                });
                X = e.Form("Moonbell", "A great moon jelly ringed with lights. Its tentacles sting like lightning.", Element.Normal, "sting_shower");
                Forms(b, "jellume", A, X);
            }
            {   // Krakling: longer arms, then a young kraken captain in a tricorn
                var e = EvoOf(b, "krakling");
                e.Body(1.18f).Arms(1.3f);
                A = e.Form("Krakwhirl", "Its arms have grown long enough to sink a rowing boat. It practises.");
                e.Body(1.1f).Arms(1.15f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e); float w = m.scale.x;
                    p.Add(Cyl, gB, cD, V(0, t - w * 0.1f, m.pos.z), V(w * 1.15f, 0.05f, w * 1.15f));
                    p.Add(Dom, gB, cD, V(0, t - w * 0.08f, m.pos.z), V(w * 0.75f, w * 0.38f, w * 0.75f));
                    p.Ring(Con, gB, cD, V(0, t - w * 0.06f, m.pos.z), w * 0.5f, 3, V(w * 0.3f, w * 0.3f, 0.06f), 90f);
                    p.Add(Sph, gB, cA, V(0, t + w * 0.1f, m.pos.z + w * 0.38f), V(0.09f, 0.09f, 0.03f));
                });
                X = e.Form("Deepcaptain", "A young kraken in a captain's tricorn, already ordering the reef about. It whips up whirlpools of ink.", Element.Normal, "ink_whirlpool");
                Forms(b, "krakling", A, X);
            }

            // ---------------------------------------------------------------- Mirefen
            {   // Bogsnap: a bigger trap, then a great flytrap with vine whips
                var e = EvoOf(b, "bogsnap");
                e.Body(1.15f).Head(1.1f);
                A = e.Form("Bogjaw", "Its trap has grown big enough to swallow a hat. It has, more than once.");
                e.Body(1.1f).Head(1.08f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.WhipPair(gB, gB, cS, V(bb.size.x * 0.45f, bb.center.y, bb.center.z), V(0.06f, -0.02f, 0.03f), V(0f, 0.006f, 0f), 5, 0.045f, 0.02f);
                    p.Ring(Sph, gB, cA, V(0, bb.min.y + 0.05f, bb.center.z), bb.size.x * 0.5f, 6, V(0.12f, 0.06f, 0.12f));
                });
                X = e.Form("Venusnap", "A great marsh flytrap that lashes out with vines and snaps shut on whatever it catches. It is extremely patient.", Element.Normal, "snap_trap");
                Forms(b, "bogsnap", A, X);
            }
            {   // Rushcat: a lean marsh cat, then a lynx with cattail tufts and reeds down its back
                var e = EvoOf(b, "rushcat");
                e.Legs(1.2f).Head(0.9f).Body(new Vector3(1f, 1f, 1.15f)).Scale(gT, 1.2f);
                A = e.Form("Reedcat", "Long and lean; it stands in the reeds and simply isn't there.");
                e.Legs(1.08f).Body(1.1f).Stretch(e.AboveHead(0.3f), 1.4f).Stretch(q => q.group == gB && q.shape == Con, 1.35f);
                e.With(p => { var bb = e.B(gB); p.Spikes(gB, cS, Top(e) - 0.06f, bb.max.z - 0.08f, bb.min.z + 0.08f, 5, 0.04f, 0.3f, 0.02f, -10f); });
                X = e.Form("Marshlynx", "A lynx of the fen with cattail tufts and a back of reeds. It springs out of the water before you see it. It gains the Tide element.", Element.Tide, "reed_ambush");
                Forms(b, "rushcat", A, X);
            }
            {   // Sporetoad: fatter, then a toad that is mostly toadstools
                var e = EvoOf(b, "sporetoad");
                e.Body(1.22f).Legs(1.1f);
                A = e.Form("Mustytoad", "It has sat still so long that a whole ring of toadstools grows on it.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.57f + 0.4f, r = m.scale.x * 0.25f;
                        var at = V(Mathf.Sin(a) * r, t - 0.02f, m.pos.z + Mathf.Cos(a) * r);
                        p.Add(Cyl, gB, cW, at + V(0, 0.08f, 0), V(0.05f, 0.16f, 0.05f));
                        p.Add(Dom, gB, cA, at + V(0, 0.17f, 0), V(0.2f, 0.12f, 0.2f));
                    }
                    p.Add(Cyl, gB, cW, V(0, t + 0.1f, m.pos.z), V(0.07f, 0.24f, 0.07f));
                    p.Add(Dom, gB, cG, V(0, t + 0.24f, m.pos.z), V(0.3f, 0.16f, 0.3f));
                });
                X = e.Form("Mycotoad", "More mushroom than toad now. One squeeze and a cloud of mould sends the whole clearing to sleep.", Element.Normal, "mold_burst");
                Forms(b, "sporetoad", A, X);
            }
            {   // Leechlash: longer, then a leech with long feelers and a ridge of suckers
                var e = EvoOf(b, "leechlash");
                e.Body(1.15f).Scale(gT, 1.25f).Head(1.05f);
                A = e.Form("Leechwhip", "Its feelers have grown into proper whips. It still only wants a taste.");
                e.Body(1.1f).Scale(gT, 1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, Top(e) - 0.06f, bb.max.z - 0.05f, bb.min.z + 0.05f, 5, 0.14f, 0.1f, 0f, 0f, 0f, 0f, Sph);
                    p.Spikes(gB, cG, Top(e) - 0.12f, bb.max.z - 0.1f, bb.min.z + 0.1f, 4, 0.1f, 0.08f, 0f, 0f, bb.size.x * 0.32f, 50f, Sph);
                    p.Brows(cD);
                });
                X = e.Form("Drainlash", "A fat leech with a ridge of glowing suckers. Its lash drains a little more than a taste.", Element.Normal, "draining_lash");
                Forms(b, "leechlash", A, X);
            }

            // ---------------------------------------------------------------- Sunscour Dunes
            {   // Frillizard: taller, then a frill like the sun
                var e = EvoOf(b, "frillizard");
                e.Legs(1.15f).Head(0.95f).Body(1.1f).Scale(gT, 1.2f);
                A = e.Form("Fanlizard", "Its frill has grown broad and bright; it wins arguments before they start.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Fan(Con, gH, cA, V(0, s.pos.y, s.pos.z - s.scale.z * 0.5f), s.scale.x * 1.25f, 15, V(0.1f, 0.34f, 0.03f), -120f, 240f);
                    p.Fan(Sph, gH, cG, V(0, s.pos.y, s.pos.z - s.scale.z * 0.48f), s.scale.x * 1.05f, 15, V(0.07f, 0.07f, 0.03f), -120f, 240f);
                    p.Brows(cD);
                });
                X = e.Form("Sunfrill", "A lizard with a frill like the rising sun. When it snaps it open, the flash burns.", Element.Normal, "frill_flash");
                Forms(b, "frillizard", A, X);
            }
            {   // Prickpear: a taller cactus, then one in full flower with fruit to share
                var e = EvoOf(b, "prickpear");
                e.Body(1.15f).Legs(1.15f).Arms(1.2f);
                A = e.Form("Pricklypear", "Taller, pricklier, and even keener on hugs.");
                e.Body(1.1f).Arms(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Sph, gB, cA, V(0, bb.max.y - 0.02f, bb.center.z), bb.size.x * 0.28f, 5, V(0.1f, 0.06f, 0.1f));
                    p.Add(Sph, gB, cG, V(0, bb.max.y + 0.02f, bb.center.z), V(0.1f, 0.08f, 0.1f));
                    p.Ring(Sph, gB, cS, V(0, bb.center.y + bb.size.y * 0.15f, bb.center.z), bb.size.x * 0.5f, 4, V(0.09f, 0.11f, 0.09f), 0f, 45f);
                });
                X = e.Form("Bloompear", "A cactus in full flower, hung with fruit for anyone who wants some. Its hugs are legendary, and so are the scratches.", Element.Normal, "thorny_hug");
                Forms(b, "prickpear", A, X);
            }
            {   // Sandjackal: a long-legged tomb hound, then a pharaoh's hound with a striped headdress and sun disc
                var e = EvoOf(b, "sandjackal");
                e.Legs(1.2f).Head(0.92f).Body(new Vector3(1f, 1f, 1.12f)).Stretch(e.AboveHead(0.3f), 1.3f);
                A = e.Form("Tombhound", "A tall black hound that walks the tomb corridors as if it owns them.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var s = e.Skull;
                    for (int i = 0; i < 4; i++)
                        p.Mirror(Box, gH, i % 2 == 0 ? cA : cD, V(s.scale.x * 0.48f, s.pos.y - s.scale.y * 0.05f - i * 0.07f, s.pos.z - s.scale.z * 0.12f), V(0.05f, 0.07f, s.scale.z * 0.7f));
                    p.Add(Sph, gH, cG, V(0, s.pos.y + s.scale.y * 0.62f, s.pos.z - s.scale.z * 0.2f), V(0.2f, 0.2f, 0.04f));
                });
                var pal = e.Pal; pal.accent = Hex("#E8B53A"); pal.glow = Hex("#FFD86A");
                X = e.Form("Pharaohound", "A jackal in a striped headdress beneath a disc of sun. Its stare passes judgement. It gains the Lumen element.", Element.Lumen, "pharaohs_gaze", pal);
                Forms(b, "sandjackal", A, X);
            }
            {   // Rattlecoil: longer, then a hooded viper with a crown of horns
                var e = EvoOf(b, "rattlecoil");
                e.Body(1.15f).Scale(gT, 1.2f).Head(1.05f);
                A = e.Form("Rattlefang", "Longer, heavier, and its rattle can be heard over a sandstorm.");
                e.Body(1.08f).Scale(gT, 1.1f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Mirror(Sph, gH, cS, V(s.scale.x * 0.55f, s.pos.y, s.pos.z - s.scale.z * 0.3f), V(0.06f, s.scale.y * 1.3f, s.scale.z * 0.9f), V(0, 0, -15));
                    p.Ring(Con, gH, cA, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z - 0.02f), s.scale.x * 0.25f, 5, V(0.04f, 0.12f, 0.04f), 15f);
                });
                X = e.Form("Crownrattle", "A hooded viper crowned in horns. It gives exactly one warning, then strikes from across the dune.", Element.Normal, "rattle_strike");
                Forms(b, "rattlecoil", A, X);
            }

            // ---------------------------------------------------------------- Blightmoor
            {   // Silkspin: a bigger spider, then a matriarch with a glowing pattern and a crown of spines
                var e = EvoOf(b, "silkspin");
                e.Body(1.2f).Legs(1.1f).Head(0.95f);
                A = e.Form("Silkweaver", "Its webs now span the gaps between dead trees. It still charges in bugs.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Ring(Sph, gB, cG, V(0, t + 0.01f, m.pos.z), m.scale.x * 0.26f, 6, V(0.12f, 0.06f, 0.12f));
                    p.Add(Sph, gB, cG, V(0, t + 0.03f, m.pos.z), V(0.16f, 0.07f, 0.16f));
                    p.Spikes(gB, cD, t + 0.02f, m.pos.z + m.scale.z * 0.3f, m.pos.z - m.scale.z * 0.42f, 4, 0.06f, 0.18f, 0.02f, -35f, m.scale.x * 0.3f, 30f);
                    var s = e.Skull;
                    p.Ring(Con, gH, cD, V(0, s.pos.y + s.scale.y * 0.4f, s.pos.z), s.scale.x * 0.25f, 5, V(0.03f, 0.1f, 0.03f), 15f);
                });
                X = e.Form("Silkmatriarch", "Mother of the moor's spiders, a glowing sigil on her back. Her webs fly across a clearing and hold anything.", Element.Normal, "web_snare");
                Forms(b, "silkspin", A, X);
            }
            {   // Fumeskull: longer horns, then a crown of green fire
                var e = EvoOf(b, "fumeskull");
                e.Body(1.2f).Stretch(q => q.shape == Con, 1.35f).Scale(gT, 1.2f);
                A = e.Form("Fumehorn", "Its horns have grown back long and curling; the fumes pour thicker.");
                e.Body(1.1f).Scale(gX, 1.2f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Con, gB, cG, V(0, bb.max.y - 0.02f, bb.center.z), bb.size.x * 0.26f, 7, V(0.06f, 0.2f, 0.06f), 12f);
                    p.Add(Sph, gB, cG, V(0, bb.max.y + 0.02f, bb.center.z), V(0.14f, 0.1f, 0.14f));
                });
                X = e.Form("Wraithskull", "A horned skull crowned in fire that does not burn the wood it floats over. Its cackle does.", Element.Normal, "skull_cackle");
                Forms(b, "fumeskull", A, X);
            }
            {   // Venomoth: broader wings, then four eyed wings
                var e = EvoOf(b, "venomoth");
                e.Wings(1.2f).Body(1.1f).Head(0.94f);
                A = e.Form("Gazewing", "The eyes on its wings have grown; you can feel them follow you.");
                e.Wings(1.1f);
                e.Dup(Wing, q => { q.pos.z -= 0.12f; q.pos.y -= 0.04f; q.scale *= 0.8f; return q; });
                e.With(p => { var s = e.Skull; p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.35f, s.pos.z), 0.06f, 4, V(0.03f, 0.16f, 0.03f), -45f, 90f); });
                X = e.Form("Hypnomoth", "Four wings of staring eyes. Its dust puts you to sleep while the poison works.", Element.Normal, "hypno_dust");
                Forms(b, "venomoth", A, X);
            }
            {   // Acidrake: wings that work, then horns and a spined back
                var e = EvoOf(b, "acidrake");
                e.Legs(1.15f).Wings(1.4f).Body(1.12f).Head(0.92f);
                A = e.Form("Acidwyvern", "Its wings have finally caught up. It flies badly, and spits better.");
                e.Wings(1.2f).Body(1.08f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Horns(cD, 0.06f, 0.2f);
                    p.Spikes(gB, cG, Top(e) - 0.03f, bb.max.z - 0.06f, bb.min.z + 0.06f, 4, 0.05f, 0.14f, 0.02f, -30f);
                });
                X = e.Form("Corrodrake", "A horned drake with a glowing spine. Its breath eats through stone and armour alike.", Element.Normal, "acid_breath");
                Forms(b, "acidrake", A, X);
            }

            // ---------------------------------------------------------------- Glowdeep
            {   // Prismfox: longer, then five crystal tails and a crown
                var e = EvoOf(b, "prismfox");
                e.Legs(1.2f).Head(0.9f).Body(new Vector3(1f, 1f, 1.12f)).Scale(gT, 1.2f);
                A = e.Form("Prismtail", "Its three tails throw rainbows the length of the grove.");
                e.Dup(q => q.group == gT && q.pos.x > 0.02f, q => { q.pos.x += 0.08f; q.rot.y += 15f; q.scale *= 0.9f; return q; });
                e.Dup(q => q.group == gT && q.pos.x < -0.02f, q => { q.pos.x -= 0.08f; q.rot.y -= 15f; q.scale *= 0.9f; return q; });
                e.With(p => { var s = e.Skull; p.Ring(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.45f, s.pos.z), s.scale.x * 0.25f, 5, V(0.04f, 0.12f, 0.04f), 12f); });
                X = e.Form("Spectrafox", "Five crystal tails and a crown of prisms. Its beam splits into every colour and freezes where it lands.", Element.Normal, "spectrum_ray");
                Forms(b, "prismfox", A, X);
            }
            {   // Noctowlet: bigger, then an owl with a crescent crown
                var e = EvoOf(b, "noctowlet");
                e.Body(1.2f).Wings(1.25f);
                A = e.Form("Duskhoot", "A round night owl that has grown into its eyes.");
                e.Body(1.1f).Wings(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fan(Sph, gB, cG, V(0, bb.max.y + 0.04f, bb.center.z), 0.18f, 7, V(0.06f, 0.06f, 0.06f), -70f, 140f);
                    p.Mirror(Con, gB, cD, V(bb.size.x * 0.25f, bb.max.y + 0.02f, bb.center.z), V(0.05f, 0.16f, 0.05f), V(0, 0, -15));
                });
                X = e.Form("Lunarowl", "An owl with a crescent moon over its brow. Its gaze sends the whole forest to sleep.", Element.Normal, "moon_gaze");
                Forms(b, "noctowlet", A, X);
            }
            {   // Bulbfrog: a bigger lantern throat, then two lures and a crown of light
                var e = EvoOf(b, "bulbfrog");
                e.Body(1.2f).Legs(1.1f).Scale(gX, 1.2f);
                A = e.Form("Bulbtoad", "Its throat-lantern glows like a porch light.");
                e.Body(1.1f);
                e.Dup(q => q.group == gX, q => { q.pos.x += 0.14f; q.rot.z -= 20f; q.scale *= 0.85f; return q; });
                e.With(p => { var bb = e.B(gB); p.Ring(Sph, gB, cG, V(0, bb.max.y - 0.03f, bb.center.z), bb.size.x * 0.32f, 6, V(0.05f, 0.05f, 0.05f)); });
                X = e.Form("Glowcroaker", "A frog with two lures and a crown of lights. Its croak is a flash that leaves you seeing spots.", Element.Normal, "flash_croak");
                Forms(b, "bulbfrog", A, X);
            }
            {   // Shardillo: bigger, then rows of crystal down its back
                var e = EvoOf(b, "shardillo");
                e.Body(1.18f).Legs(1.1f).Head(0.94f);
                A = e.Form("Geodillo", "Curled up it is a geode the size of a barrel. Miners have learned to knock first.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, Top(e) - 0.06f, bb.max.z - 0.1f, bb.min.z + 0.1f, 5, 0.08f, 0.22f, 0.03f, 0f, 0f, 0f, Cry);
                    p.Spikes(gB, cA, Top(e) - 0.12f, bb.max.z - 0.14f, bb.min.z + 0.14f, 4, 0.06f, 0.16f, 0.02f, 0f, bb.size.x * 0.28f, 40f, Cry);
                });
                X = e.Form("Prismadillo", "An armadillo armoured in rows of crystal. It rolls through a cave like a landslide of gems.", Element.Normal, "geode_roll");
                Forms(b, "shardillo", A, X);
            }
            {   // Sporeglide: a wider parasol, then a canopy with glowing threads
                var e = EvoOf(b, "sporeglide");
                e.Body(1.15f).Scale(gX, 1.3f);
                A = e.Form("Parasporel", "Its parasol has opened wide; it rides warm air all night.");
                e.Scale(gX, 1.15f);
                e.With(p => { var x = e.B(gX); p.Ring(Cyl, gX, cG, V(0, x.min.y - 0.12f, x.center.z), x.size.x * 0.42f, 8, V(0.012f, 0.3f, 0.012f)); p.Ring(Sph, gX, cG, V(0, x.min.y - 0.28f, x.center.z), x.size.x * 0.42f, 8, V(0.04f, 0.04f, 0.04f)); });
                X = e.Form("Canopyglide", "A seed under a canopy of glowing threads. It drifts over a clearing and rains sleepy spores.", Element.Normal, "drift_spores");
                Forms(b, "sporeglide", A, X);
            }

            // ---------------------------------------------------------------- underground
            {   // Glimcrab: bigger claws, then a geode on its back
                var e = EvoOf(b, "glimcrab");
                e.Body(1.18f).Arms(1.25f).Legs(1.08f);
                A = e.Form("Glimshell", "Its claws have caught up with the crystal on its back.");
                e.Body(1.12f).Arms(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Cry, gB, cG, V(0, bb.max.y - 0.04f, bb.center.z), bb.size.x * 0.2f, 6, V(0.06f, 0.2f, 0.06f), 22f);
                    p.Add(Cry, gB, cG, V(0, bb.max.y - 0.02f, bb.center.z), V(0.09f, 0.32f, 0.09f));
                });
                X = e.Form("Geodecrab", "A crab carrying a whole geode, crystals bursting from its shell. Its pinch is cold as the deep rock.", Element.Normal, "crystal_pinch");
                Forms(b, "glimcrab", A, X);
            }
            {   // Mushmaw: a bigger cap and mouth, then a toothy toadstool with glowing spots
                var e = EvoOf(b, "mushmaw");
                e.Body(1.15f).Head(1.15f);
                A = e.Form("Gnashcap", "Its cap has spread and so has its grin.");
                e.Body(1.1f).Head(1.1f);
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g);
                    p.Ring(Sph, g, cG, V(0, hb.max.y - hb.size.y * 0.25f, hb.center.z), hb.size.x * 0.44f, 8, V(0.07f, 0.05f, 0.07f));
                    p.Ring(Con, g, cW, V(0, hb.min.y + 0.03f, hb.center.z), hb.size.x * 0.4f, 8, V(0.04f, 0.08f, 0.04f), 180f);
                });
                X = e.Form("Gnashroom", "A glowing toadstool with a ring of teeth under its cap. It waits among the mushrooms for something to lean on it.", Element.Normal, "gnash_bite");
                Forms(b, "mushmaw", A, X);
            }
            {   // Stalagrin: taller, then a stalagmite king with a crown of icicles
                var e = EvoOf(b, "stalagrin");
                e.Body(new Vector3(1.12f, 1.25f, 1.12f)).Arms(1.2f);
                A = e.Form("Stalagrunt", "A taller, grinning pillar. Count again: there's one more than there was.");
                e.Body(1.1f).Arms(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Cry, gB, cG, V(0, bb.center.y + bb.size.y * 0.15f, bb.center.z), bb.size.x * 0.48f, 6, V(0.06f, 0.16f, 0.06f), 70f);
                    p.Ring(Con, gB, cS, V(0, bb.min.y + 0.1f, bb.center.z), bb.size.x * 0.55f, 7, V(0.12f, 0.3f, 0.12f));
                });
                X = e.Form("Stalagmight", "A stalagmite king ringed with ice crystals and its own little stalagmites. It slams down like a cave-in.", Element.Normal, "stalag_slam");
                Forms(b, "stalagrin", A, X);
            }
        }
    }
}
