using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Real evolutions, third wave (2026-10-18, user: "start the 3rd wave"). The small Commons and early Uncommons that
    /// still looked like babies: Pebblump, Mossnout, Ribbloom, Gloomtoad, Nimbuff, Dribblet, Fenfly, Tumblit, Stingnat,
    /// Capglow (Commons), Emberkit, Duskit, Thistlehog, Bumbleloom, Ashcrow, Steamkit, Hooten, Wispling, Bramblebat,
    /// Lashtail, Capshroom, Puffspine, Raybit, Twistail, Plasmoth (Uncommons). Same tools and rules as the first two
    /// waves (`ContentSeed.Evolutions`): Ascended at Lv 20, Exalted at Lv 35 with its own skill; only single-element
    /// kinds gain a second element (Pebblump +Flora, Nimbuff +Lumen, Dribblet +Gale).
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildEvoSkills3(Bundle b)
        {
            Ab(b, "cairn_quake", "Cairn Quake", Element.Stone, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.None, 0f, 5.5f);
            Ab(b, "truffle_rush", "Truffle Rush", Element.Flora, AbilityKind.Dash, 34, 9f, 7f, StatusType.Drowsy, 0.2f);
            Ab(b, "lily_torrent", "Lily Torrent", Element.Tide, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Soaked, 0.4f, 3f, 21f);
            Ab(b, "lure_glare", "Lure Glare", Element.Shade, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.45f, 5.5f);
            Ab(b, "rainbow_squall", "Rainbow Squall", Element.Gale, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Soaked, 0.4f, 5.5f);
            Ab(b, "downpour", "Downpour", Element.Tide, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Soaked, 0.5f, 5.5f);
            Ab(b, "beacon_flare", "Beacon Flare", Element.Lumen, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Burn, 0.3f, 3f, 22f);
            Ab(b, "bramble_roll", "Bramble Roll", Element.Flora, AbilityKind.Dash, 33, 9f, 6.5f);
            Ab(b, "barb_volley", "Barb Volley", Element.Shade, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Poison, 0.45f, 3f, 24f);
            Ab(b, "spire_light", "Spire Light", Element.Lumen, AbilityKind.Projectile, 33, 14f, 6.5f, StatusType.Drowsy, 0.25f, 3f, 24f);
            Ab(b, "triple_flare", "Triple Flare", Element.Ember, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Burn, 0.4f, 3f, 22f);
            Ab(b, "pilfer_pounce", "Pilfer Pounce", Element.Shade, AbilityKind.Dash, 33, 9f, 6.5f);
            Ab(b, "thistle_volley", "Thistle Volley", Element.Flora, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.None, 0f, 3f, 22f);
            Ab(b, "pollen_storm", "Pollen Storm", Element.Flora, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.4f, 5.5f);
            Ab(b, "ashfall_dive", "Ashfall Dive", Element.Shade, AbilityKind.Dash, 33, 10f, 6.5f, StatusType.Burn, 0.35f);
            Ab(b, "geyser_burst", "Geyser Burst", Element.Tide, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Burn, 0.3f, 5f);
            Ab(b, "whiteout", "Whiteout", Element.Frost, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Chill, 0.45f, 5.5f);
            Ab(b, "guiding_flame", "Guiding Flame", Element.Lumen, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Burn, 0.3f, 3f, 20f);
            Ab(b, "rosethorn_dive", "Rosethorn Dive", Element.Shade, AbilityKind.Dash, 33, 10f, 6.5f, StatusType.Poison, 0.4f);
            Ab(b, "ringmaster_crack", "Ringmaster Crack", Element.Normal, AbilityKind.Melee, 34, 2.8f, 6.5f);
            Ab(b, "dream_spores", "Dream Spores", Element.Shade, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.5f, 5.5f);
            Ab(b, "spine_burst", "Spine Burst", Element.Stone, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.None, 0f, 5.5f);
            Ab(b, "prism_beam", "Prism Beam", Element.Lumen, AbilityKind.Projectile, 34, 14f, 6.5f, StatusType.None, 0f, 3f, 26f);
            Ab(b, "rotor_gust", "Rotor Gust", Element.Gale, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.None, 0f, 5.5f);
            Ab(b, "ball_lightning", "Ball Lightning", Element.Volt, AbilityKind.Projectile, 34, 13f, 6.5f, StatusType.Shock, 0.4f, 3f, 18f);
        }

        static void BuildEvolutions3(Bundle b)
        {
            const PartShape Sph = PartShape.Sphere, Con = PartShape.Cone, Cry = PartShape.Crystal, Dom = PartShape.Dome, Cyl = PartShape.Cylinder, Rck = PartShape.Rock;
            const BodyGroup gB = BodyGroup.Body, gH = BodyGroup.Head, gT = BodyGroup.Tail, gX = BodyGroup.Extra, gWL = BodyGroup.WingL, gWR = BodyGroup.WingR;
            const ColorSlot cP = ColorSlot.Primary, cS = ColorSlot.Secondary, cA = ColorSlot.Accent, cG = ColorSlot.Glow, cD = ColorSlot.Dark, cW = ColorSlot.White;
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
            // the body's real standing top (a capsule lying along z is short, not tall)
            float Top(Evo e)
            {
                var m = Main(e, gB); var r = Quaternion.Euler(m.rot);
                float half = 0.5f * (Mathf.Abs((r * Vector3.right).y) * m.scale.x + Mathf.Abs((r * Vector3.up).y) * m.scale.y + Mathf.Abs((r * Vector3.forward).y) * m.scale.z);
                return m.pos.y + half * 0.9f;
            }
            // the top of the head (ears, caps and all) or of the body for kin that are all body
            float HeadTop(Evo e) => e.Has(gH) ? e.B(gH).max.y : e.B(gB).max.y;
            BodyGroup HeadG(Evo e) => e.Has(gH) ? gH : gB;

            // ---------------------------------------------------------------- Commons
            {   // Pebblump: a cairn of river stones, then a mossy standing stone
                var e = EvoOf(b, "pebblump");
                e.Legs(1.15f).Body(1.18f).Head(0.92f);
                e.With(p => { var m = Main(e, gB); p.Add(Rck, gB, cS, V(0, Top(e) + 0.1f, m.pos.z), V(0.34f, 0.22f, 0.3f)); });
                A = e.Form("Cairnback", "Its stones have stacked themselves into a cairn. Travellers add a pebble for luck.");
                e.Body(1.12f).Legs(1.06f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Add(Rck, gB, cS, V(0, t + 0.42f, m.pos.z - 0.04f), V(0.26f, 0.72f, 0.22f));
                    p.Add(PartShape.Box, gB, cG, V(0, t + 0.48f, m.pos.z + 0.08f), V(0.05f, 0.3f, 0.02f));
                    p.Ring(Dom, gB, cA, V(0, t - 0.04f, m.pos.z), m.scale.x * 0.38f, 6, V(0.16f, 0.07f, 0.16f));
                });
                var pal = e.Pal; pal.accent = Hex("#5E9E48");
                X = e.Form("Monolithe", "A tortoise that carries a standing stone grown over with moss. When it stamps, the ground answers. It gains the Flora element.", Element.Flora, "cairn_quake", pal);
                Forms(b, "pebblump", A, X);
            }
            {   // Mossnout: a tusked young boar, then a boar with a little tree on its back
                var e = EvoOf(b, "mossnout");
                e.Legs(1.2f).Head(0.9f).Body(1.14f);
                e.With(p => { var s = e.Skull; p.Mirror(Con, gH, cW, V(s.scale.x * 0.28f, s.pos.y - s.scale.y * 0.24f, s.pos.z + s.scale.z * 0.4f), V(0.05f, 0.15f, 0.05f), V(-60, 0, -20)); });
                A = e.Form("Mossboar", "A stout boar with a thick green coat and tusks for digging truffles.");
                e.Body(1.12f).Legs(1.08f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Add(Cyl, gB, cD, V(0, t + 0.16f, m.pos.z - 0.05f), V(0.07f, 0.34f, 0.07f));
                    p.Add(Sph, gB, cA, V(0, t + 0.42f, m.pos.z - 0.05f), V(0.44f, 0.32f, 0.44f));
                    p.Ring(Sph, gB, cG, V(0, t + 0.4f, m.pos.z - 0.05f), 0.2f, 5, V(0.06f, 0.06f, 0.06f));
                    p.Brows(cD);
                });
                X = e.Form("Grovehog", "A boar with a little fruit tree growing out of its back. It charges through the undergrowth and leaves a trail of truffles.", Element.Normal, "truffle_rush");
                Forms(b, "mossnout", A, X);
            }
            {   // Ribbloom: a wider lily hat, then a bullfrog crowned in water lilies
                var e = EvoOf(b, "ribbloom");
                e.Body(1.18f).Head(0.92f).Stretch(e.AboveHead(0.3f), 1f, 1.25f);
                A = e.Form("Lilyrib", "Its lily pad has grown into a parasol; dragonflies rest on it.");
                e.Body(1.15f);
                e.With(p =>
                {
                    var s = e.Skull; float top = HeadTop(e);
                    p.Ring(Sph, gH, cW, V(0, top + 0.04f, s.pos.z), 0.14f, 7, V(0.1f, 0.05f, 0.06f), 60f);
                    p.Add(Sph, gH, cG, V(0, top + 0.06f, s.pos.z), V(0.08f, 0.08f, 0.08f));
                    p.Add(Sph, gH, cS, V(0, s.pos.y - s.scale.y * 0.32f, s.pos.z + s.scale.z * 0.3f), V(0.22f, 0.16f, 0.18f));
                    p.Brows(cD);
                });
                X = e.Form("Lilylord", "A great bullfrog crowned in water lilies. Its throat swells and it spits a jet that could knock down a door.", Element.Normal, "lily_torrent");
                Forms(b, "ribbloom", A, X);
            }
            {   // Gloomtoad: fatter, then an angler-toad with a lantern lure
                var e = EvoOf(b, "gloomtoad");
                e.Body(1.22f).Head(0.92f);
                A = e.Form("Gloombelly", "A fat night toad whose spots glow brighter the hungrier it gets.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB); float top = HeadTop(e);
                    p.Add(Cyl, gH, cD, V(0, top + 0.12f, s.pos.z + 0.08f), V(0.03f, 0.28f, 0.03f), V(35, 0, 0));
                    p.Add(Sph, gH, cG, V(0, top + 0.24f, s.pos.z + 0.2f), V(0.12f, 0.12f, 0.12f));
                    p.Ring(Sph, gB, cG, V(0, bb.center.y + bb.size.y * 0.2f, bb.center.z), bb.size.x * 0.45f, 7, V(0.06f, 0.06f, 0.06f));
                });
                X = e.Form("Lanterntoad", "It dangles a glowing lure over the dark water. Whatever stares into it falls asleep where it stands.", Element.Normal, "lure_glare");
                Forms(b, "gloomtoad", A, X);
            }
            {   // Nimbuff: a horned cloud ram, then a great cloud with a rainbow over its back
                var e = EvoOf(b, "nimbuff");
                e.Body(1.25f).Head(0.9f);
                e.With(p => p.Horns(cS, 0.07f, 0.16f));
                A = e.Form("Cloudbuff", "A billowing ram of cloud with curling horns. When it sulks, it thunders a little.");
                e.Body(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB); float t = Top(e);
                    p.Fan(Sph, gB, cA, V(0, t - 0.06f, bb.center.z), bb.size.x * 0.58f, 9, V(0.08f, 0.08f, 0.08f), -80f, 160f);
                    p.Fan(Sph, gB, cG, V(0, t - 0.06f, bb.center.z), bb.size.x * 0.5f, 9, V(0.07f, 0.07f, 0.07f), -80f, 160f);
                    p.Fan(Sph, gB, cW, V(0, t - 0.06f, bb.center.z), bb.size.x * 0.42f, 9, V(0.06f, 0.06f, 0.06f), -80f, 160f);
                });
                var pal = e.Pal; pal.accent = Hex("#FF8A9A"); pal.glow = Hex("#FFE15A");
                X = e.Form("Stratowool", "A whole weather front shaped like a ram, a rainbow over its back. Its squall leaves everything soaked. It gains the Lumen element.", Element.Lumen, "rainbow_squall", pal);
                Forms(b, "nimbuff", A, X);
            }
            {   // Dribblet: a fuller drop, then a drop under its own little rain cloud
                var e = EvoOf(b, "dribblet");
                e.Body(1.25f).Legs(1.15f);
                A = e.Form("Dropple", "A plump raindrop that bounces higher than it falls.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Ring(Sph, gB, cW, V(0, t + 0.34f, m.pos.z), 0.2f, 6, V(0.2f, 0.15f, 0.2f));
                    p.Add(Sph, gB, cW, V(0, t + 0.4f, m.pos.z), V(0.3f, 0.2f, 0.3f));
                    p.Ring(Sph, gB, cG, V(0, t + 0.16f, m.pos.z), 0.12f, 4, V(0.03f, 0.06f, 0.03f), 0f, 45f);
                });
                X = e.Form("Raincrown", "It carries its own rain cloud wherever it goes, and opens it on anyone who annoys it. It gains the Gale element.", Element.Gale, "downpour");
                Forms(b, "dribblet", A, X);
            }
            {   // Fenfly: bigger lantern, then four wings and a caged beacon
                var e = EvoOf(b, "fenfly");
                e.Wings(1.25f).Scale(gT, 1.35f).Body(1.1f);
                A = e.Form("Fenglow", "Its lantern is bright enough to read by. Marsh folk keep one in a jar.");
                e.Scale(gT, 1.2f);
                e.Dup(Wing, q => { q.pos.z -= 0.12f; q.pos.y -= 0.03f; q.scale *= 0.82f; return q; });
                e.With(p => { var t = e.B(gT); p.Ring(Cyl, gT, cD, t.center, Mathf.Max(t.size.x, t.size.z) * 0.5f, 4, V(0.02f, t.size.y * 0.95f, 0.02f), 0f, 45f); });
                X = e.Form("Marshbeacon", "A firefly with a lantern like a lighthouse, caged in dark wire. Its flare burns.", Element.Normal, "beacon_flare");
                Forms(b, "fenfly", A, X);
            }
            {   // Tumblit: a bigger weave, then a thorny whirl in bloom
                var e = EvoOf(b, "tumblit");
                e.Body(1.3f);
                A = e.Form("Tumbleweave", "A tumbleweed as big as a barrel, woven tight by the wind.");
                e.Body(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Con, gB, cD, V(0, bb.center.y, bb.center.z), bb.size.x * 0.48f, 10, V(0.05f, 0.16f, 0.05f), 90f);
                    p.Ring(Con, gB, cD, V(0, bb.center.y + bb.size.y * 0.3f, bb.center.z), bb.size.x * 0.36f, 8, V(0.05f, 0.14f, 0.05f), 55f, 20f);
                    p.Ring(Sph, gB, cA, V(0, bb.max.y - 0.04f, bb.center.z), bb.size.x * 0.2f, 4, V(0.08f, 0.06f, 0.08f));
                });
                X = e.Form("Thornwhirl", "A whirl of thorny briar with flowers caught in it. It rolls at you faster than the wind that carries it.", Element.Normal, "bramble_roll");
                Forms(b, "tumblit", A, X);
            }
            {   // Stingnat: a longer barb, then an armoured hive marshal with four wings
                var e = EvoOf(b, "stingnat");
                e.Wings(1.2f).Body(1.1f).Scale(gT, 1.25f).Stretch(q => q.group == gT && q.shape == Con, 1.4f);
                A = e.Form("Barbwasp", "Its sting has grown into a barb you can see from across the moor.");
                e.Dup(Wing, q => { q.pos.z -= 0.1f; q.scale *= 0.85f; return q; });
                e.With(p =>
                {
                    var bb = e.B(gB); var s = e.Skull;
                    p.Mirror(Sph, gB, cA, V(bb.size.x * 0.42f, bb.center.y, bb.center.z), V(0.07f, bb.size.y * 0.8f, bb.size.z * 0.8f));
                    p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z), 0.06f, 5, V(0.03f, 0.12f, 0.03f), -50f, 100f);
                });
                X = e.Form("Hivemarshal", "A wasp in armour of chewed paper, four wings and a crown of barbs. It fires its stings like arrows.", Element.Normal, "barb_volley");
                Forms(b, "stingnat", A, X);
            }
            {   // Capglow: a taller cap, then a spire ringed with lights
                var e = EvoOf(b, "capglow");
                e.Legs(1.2f).Arms(1.1f).Stretch(e.AboveHead(0.2f), 1.25f, 1.1f);
                A = e.Form("Lampcap", "Its cap has grown tall and its glow steady; it walks the paths at night on purpose.");
                e.With(p =>
                {
                    var hb = e.B(gH); var s = e.Skull;
                    p.Ring(Sph, gH, cG, V(0, hb.center.y, s.pos.z), hb.size.x * 0.58f, 9, V(0.08f, 0.06f, 0.08f));
                    p.Ring(Sph, gH, cG, V(0, hb.center.y + hb.size.y * 0.28f, s.pos.z), hb.size.x * 0.47f, 7, V(0.07f, 0.05f, 0.07f), 0f, 25f);
                    p.Add(Sph, gH, cG, V(0, hb.max.y + 0.04f, s.pos.z), V(0.1f, 0.1f, 0.1f));
                });
                X = e.Form("Lumenspire", "A tall spire of a mushroom ringed with lights, like a lamppost that went for a walk. Its beam sends you to sleep.", Element.Normal, "spire_light");
                Forms(b, "capglow", A, X);
            }

            // ---------------------------------------------------------------- Uncommons
            {   // Emberkit: a long-legged fox with a brush of flame, then three flaming tails and a mane
                var e = EvoOf(b, "emberkit");
                e.Legs(1.2f).Head(0.9f).Body(new Vector3(1f, 1f, 1.15f)).Scale(gT, 1.35f);
                A = e.Form("Brushfox", "Its tail is a brush of flame as long as it is.");
                e.Dup(q => q.group == gT, q => { q.pos.x += 0.13f; q.rot.y += 22f; q.scale *= 0.9f; return q; });
                e.Dup(q => q.group == gT && q.pos.x < 0.05f, q => { q.pos.x -= 0.13f; q.rot.y -= 22f; q.scale *= 0.9f; return q; });
                e.With(p => { var s = e.Skull; p.Fan(Con, gH, cG, V(0, s.pos.y, s.pos.z - s.scale.z * 0.35f), s.scale.x * 0.55f, 9, V(0.07f, 0.18f, 0.04f), -110f, 220f); });
                X = e.Form("Blazetail", "Three tails of fire and a mane of flame. Each tail throws its own fireball.", Element.Normal, "triple_flare");
                Forms(b, "emberkit", A, X);
            }
            {   // Duskit: a lanky imp, then a caped shadow baron with a little crown
                var e = EvoOf(b, "duskit");
                e.Legs(1.2f).Head(0.9f).Stretch(e.AboveHead(0.25f), 1.3f);
                A = e.Form("Duskimp", "Long ears, longer shadow. Anything shiny within reach goes missing.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB); var s = e.Skull;
                    p.Add(Sph, gB, cA, V(0, Top(e) - 0.02f, bb.center.z - 0.08f), V(bb.size.x * 1.25f, 0.07f, bb.size.z * 1.05f), V(-8, 0, 0));
                    p.Mirror(Sph, gB, cA, V(bb.size.x * 0.5f, bb.center.y, bb.center.z - 0.06f), V(0.05f, bb.size.y * 0.7f, bb.size.z * 0.9f));
                    p.Ring(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.48f, s.pos.z), s.scale.x * 0.2f, 5, V(0.05f, 0.15f, 0.05f), 8f);
                    p.Add(Cyl, gH, cG, V(0, s.pos.y + s.scale.y * 0.47f, s.pos.z), V(s.scale.x * 0.42f, 0.04f, s.scale.x * 0.42f));
                    p.Brows(cD);
                });
                var dpal = e.Pal; dpal.accent = Hex("#8E1F3A"); dpal.glow = Hex("#FFD34A");
                X = e.Form("Duskbaron", "A shadow cat in a cape and a stolen crown. It pounces from the dark and runs off with your things.", Element.Normal, "pilfer_pounce", dpal);
                Forms(b, "duskit", A, X);
            }
            {   // Thistlehog: longer quills, then a monarch crowned in thistle blooms
                var e = EvoOf(b, "thistlehog");
                e.Legs(1.15f).Body(1.15f).Stretch(q => q.group == gB && q.shape == Con, 1.3f);
                A = e.Form("Briarhog", "Every quill a thistle stem; it bristles at the slightest sound.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Ring(Sph, gB, cA, V(0, t + 0.06f, m.pos.z), 0.2f, 6, V(0.1f, 0.08f, 0.1f));
                    p.Add(Sph, gB, cG, V(0, t + 0.08f, m.pos.z), V(0.1f, 0.08f, 0.1f));
                    p.Brows(cD);
                });
                X = e.Form("Thornmonarch", "Monarch of the bramble patch, crowned in purple thistle blooms. It fires its quills in volleys.", Element.Normal, "thistle_volley");
                Forms(b, "thistlehog", A, X);
            }
            {   // Bumbleloom: a bigger bee, then a queen with a flower crown and four wings
                var e = EvoOf(b, "bumbleloom");
                e.Wings(1.2f).Body(1.2f).Head(0.92f);
                A = e.Form("Bumblewing", "A plump, busy bee; whole meadows bloom behind it.");
                e.Body(1.1f).Scale(gT, 1.2f);
                e.Dup(Wing, q => { q.pos.z -= 0.1f; q.scale *= 0.85f; return q; });
                e.With(p =>
                {
                    var s = e.Skull; float top = HeadTop(e);
                    p.Ring(Sph, gH, cA, V(0, top, s.pos.z), s.scale.x * 0.3f, 6, V(0.07f, 0.05f, 0.07f));
                    p.Add(Sph, gH, cG, V(0, top + 0.02f, s.pos.z), V(0.07f, 0.07f, 0.07f));
                });
                X = e.Form("Bloomqueen", "Queen of the flower meadows, crowned in blossoms. A storm of her pollen puts anything to sleep.", Element.Normal, "pollen_storm");
                Forms(b, "bumbleloom", A, X);
            }
            {   // Ashcrow: a raven, then a pyre-raven with a crest of embers
                var e = EvoOf(b, "ashcrow");
                e.Legs(1.15f).Wings(1.08f).Body(1.12f).Head(0.92f);
                A = e.Form("Ashraven", "A big black raven; the ash never seems to settle on it.");
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.35f, s.pos.z - s.scale.z * 0.2f), 0.05f, 5, V(0.04f, 0.18f, 0.03f), -60f, 120f);
                    p.Brows(cD);
                });
                X = e.Form("Pyreraven", "A raven crested with embers. It dives out of the smoke of wildfires and leaves burns where it strikes.", Element.Normal, "ashfall_dive");
                Forms(b, "ashcrow", A, X);
            }
            {   // Steamkit: a seal, then a walrus with a geyser on its head
                var e = EvoOf(b, "steamkit");
                e.Body(new Vector3(1.1f, 1.1f, 1.25f)).Head(0.9f).Scale(gT, 1.25f);
                A = e.Form("Steamseal", "A sleek hot-spring seal. The water around it is always warm.");
                e.Body(1.15f);
                e.With(p =>
                {
                    var s = e.Skull; float top = HeadTop(e);
                    p.Mirror(Con, gH, cW, V(s.scale.x * 0.18f, s.pos.y - s.scale.y * 0.32f, s.pos.z + s.scale.z * 0.35f), V(0.05f, 0.22f, 0.05f), V(160, 0, 0));
                    p.Ring(Sph, gH, cW, V(0, top + 0.04f, s.pos.z), 0.09f, 5, V(0.13f, 0.1f, 0.13f));
                    p.Add(Sph, gH, cW, V(0, top + 0.12f, s.pos.z), V(0.18f, 0.16f, 0.18f));
                    p.Ring(Sph, gH, cW, V(0, top + 0.2f, s.pos.z), 0.06f, 4, V(0.09f, 0.08f, 0.09f), 0f, 45f);
                });
                X = e.Form("Geysirus", "A tusked walrus with a geyser for a blowhole. When it blows, the spray scalds.", Element.Normal, "geyser_burst");
                Forms(b, "steamkit", A, X);
            }
            {   // Hooten: longer ear tufts, then an owl crowned in ice
                var e = EvoOf(b, "hooten");
                e.Wings(1.25f).Body(1.15f).Head(0.92f).Stretch(e.AboveHead(0.3f), 1.4f);
                A = e.Form("Snowhoot", "A great snowy owl with long ear tufts. It hears ore humming in the rock.");
                e.Body(1.1f).Wings(1.15f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.Ring(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.38f, s.pos.z), s.scale.x * 0.3f, 5, V(0.04f, 0.12f, 0.04f), 10f);
                    p.Add(Cry, gB, cG, V(0, bb.center.y, bb.max.z - 0.02f), V(0.06f, 0.14f, 0.06f), V(70, 0, 0));
                });
                X = e.Form("Glacierowl", "An owl crowned in ice with a crystal at its breast. One beat of its wings and the air turns to snow.", Element.Normal, "whiteout");
                Forms(b, "hooten", A, X);
            }
            {   // Wispling: a bigger flame, then a cloaked spirit carrying a lantern
                var e = EvoOf(b, "wispling");
                e.Body(1.2f).Scale(gT, 1.3f);
                A = e.Form("Wispflame", "A bigger, steadier light. It no longer fades at dawn - only dims.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Con, gB, cD, V(0, bb.min.y + 0.06f, bb.center.z), bb.size.x * 0.42f, 7, V(0.09f, 0.24f, 0.09f), 165f);
                    p.Add(Sph, gB, cG, V(bb.size.x * 0.58f, bb.center.y - 0.06f, bb.max.z - 0.04f), V(0.12f, 0.14f, 0.12f));
                    p.Add(Cyl, gB, cD, V(bb.size.x * 0.58f, bb.center.y + 0.06f, bb.max.z - 0.04f), V(0.02f, 0.1f, 0.02f));
                });
                X = e.Form("Lanternwraith", "A cloaked spirit with a lantern in hand. Lost travellers follow its light home - and its flame burns anything that won't.", Element.Normal, "guiding_flame");
                Forms(b, "wispling", A, X);
            }
            {   // Bramblebat: bigger thorn wings, then a briar bat with a rose and a spined back
                var e = EvoOf(b, "bramblebat");
                e.Wings(1.3f).Body(1.15f);
                A = e.Form("Thornbat", "Its leaf-wings have toughened into thorn; berry bushes fear it.");
                e.Wings(1.2f).Stretch(e.AboveHead(0.25f), 1.3f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB); float top = HeadTop(e);
                    p.Add(Sph, gH, cA, V(s.scale.x * 0.3f, top - 0.02f, s.pos.z), V(0.1f, 0.08f, 0.1f));
                    p.Spikes(gB, cD, Top(e) - 0.04f, bb.max.z - 0.06f, bb.min.z + 0.06f, 4, 0.05f, 0.14f, 0.02f, -30f);
                });
                X = e.Form("Briarwing", "A great briar bat with a rose behind its ear. It dives through a thicket and comes out bleeding nothing.", Element.Normal, "rosethorn_dive");
                Forms(b, "bramblebat", A, X);
            }
            {   // Lashtail: a taller lemur, then a ringmaster with a tall hat, a bow tie and two tails
                var e = EvoOf(b, "lashtail");
                e.Legs(1.2f).Head(0.9f).Arms(1.15f).Scale(gT, 1.2f);
                A = e.Form("Lashlemur", "Taller, quicker, and its tail cracks like a coachman's whip.");
                e.Body(1.08f);
                e.Dup(q => q.group == gT, q => { q.pos.x += 0.1f; q.rot.y += 18f; q.scale *= 0.9f; return q; });
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB); float top = s.pos.y + s.scale.y * 0.42f;
                    p.Add(Cyl, gH, cD, V(0, top + 0.12f, s.pos.z), V(0.15f, 0.22f, 0.15f));
                    p.Add(Cyl, gH, cD, V(0, top + 0.02f, s.pos.z), V(0.26f, 0.02f, 0.26f));
                    p.Add(Cyl, gH, cA, V(0, top + 0.05f, s.pos.z), V(0.155f, 0.04f, 0.155f));
                    p.Mirror(Con, gB, cA, V(0.05f, bb.max.y - 0.04f, bb.max.z - 0.02f), V(0.05f, 0.08f, 0.05f), V(0, 0, 90));
                });
                X = e.Form("Ringmaster", "A lemur ringmaster in a tall hat with two tails for whips. Its crack makes a whole crowd jump.", Element.Normal, "ringmaster_crack");
                Forms(b, "lashtail", A, X);
            }
            {   // Capshroom: a bigger cap, then a sleepy toadstool in a nightcap under a halo of spores
                var e = EvoOf(b, "capshroom");
                e.Body(1.15f).Stretch(e.AboveHead(0.2f), 1.15f, 1.2f);
                A = e.Form("Nightcap", "Its cap has spread wide; its spores glow brighter after midnight.");
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g); var s = e.Skull;
                    p.Add(Con, g, cS, V(0, hb.max.y + 0.14f, s.pos.z - 0.05f), V(0.18f, 0.34f, 0.18f), V(-25, 0, 0));
                    p.Add(Sph, g, cW, V(0, hb.max.y + 0.27f, s.pos.z - 0.18f), V(0.08f, 0.08f, 0.08f));
                    p.Ring(Sph, g, cG, V(0, hb.max.y - 0.06f, s.pos.z), hb.size.x * 0.62f, 8, V(0.05f, 0.05f, 0.05f));
                });
                X = e.Form("Dreamcap", "A sleepy old toadstool in a nightcap, a halo of spores drifting round it. Breathe them and you dream.", Element.Normal, "dream_spores");
                Forms(b, "capshroom", A, X);
            }
            {   // Puffspine: a bigger puffer, then an airship of spines and fins
                var e = EvoOf(b, "puffspine");
                e.Body(1.25f).Stretch(q => q.group == gB && q.shape == Con, 1.3f);
                A = e.Form("Puffpike", "It floats higher and bristles faster. Nobody pokes it twice.");
                e.Body(1.15f).Wings(1.3f).Scale(gT, 1.4f).Stretch(q => q.group == gB && q.shape == Con, 1.15f).Recolor(q => q.group == gB && q.shape == Con, cA);
                X = e.Form("Zeppuff", "A puffer the size of an airship, every spine tipped in colour. When it bursts out, its spines fly.", Element.Normal, "spine_burst");
                Forms(b, "puffspine", A, X);
            }
            {   // Raybit: a taller hare, then a hare crowned with prisms and a wider ring of lights
                var e = EvoOf(b, "raybit");
                e.Legs(1.2f).Head(0.9f).Body(1.12f).Stretch(e.AboveHead(0.3f), 1.2f);
                A = e.Form("Raylop", "Its lamp-ears now droop with their own weight of light.");
                e.Body(1.1f).Scale(gX, 1.3f);
                e.With(p => { var s = e.Skull; p.Ring(Cry, gH, cW, V(0, s.pos.y + s.scale.y * 0.44f, s.pos.z), s.scale.x * 0.34f, 6, V(0.07f, 0.2f, 0.07f), 18f); p.Add(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.48f, s.pos.z), V(0.08f, 0.26f, 0.08f)); });
                X = e.Form("Prismhare", "A hare crowned with prisms that split its light into a beam. It is even more pleased with itself.", Element.Normal, "prism_beam");
                Forms(b, "raybit", A, X);
            }
            {   // Twistail: a bigger rotor, then a double-rotored gyro seed with a glowing core
                var e = EvoOf(b, "twistail");
                e.Body(1.2f).Scale(gX, 1.3f);
                A = e.Form("Whirligig", "Its propeller has grown; it can hover a whole minute without falling over.");
                e.Scale(gX, 1.15f);
                e.Dup(q => q.group == gX, q => { q.pos.y -= 0.16f; q.scale *= 0.85f; q.rot.y += 36f; return q; });
                e.With(p => { var bb = e.B(gB); p.Add(Sph, gB, cG, bb.center + V(0, 0, bb.size.z * 0.3f), V(0.12f, 0.12f, 0.12f)); });
                X = e.Form("Gyroseed", "Two rotors and a glowing seed for a heart. It whips up gusts that knock you flat.", Element.Normal, "rotor_gust");
                Forms(b, "twistail", A, X);
            }
            {   // Plasmoth: bigger wings and orbs, then a storm moth with six orbs of ball lightning
                var e = EvoOf(b, "plasmoth");
                e.Wings(1.25f).Body(1.12f).Scale(gX, 1.2f);
                A = e.Form("Arcmoth", "Its wings flicker like distant lightning; its orbs hum louder.");
                e.Wings(1.2f).Body(1.15f).Scale(gX, 1.15f);
                e.Dup(q => q.group == gX, q => { q.pos = new Vector3(-q.pos.x, q.pos.y + 0.12f, q.pos.z - 0.05f); return q; });
                X = e.Form("Stormmoth", "A moth that rides the thunderhead with six orbs of ball lightning circling it. It throws them.", Element.Normal, "ball_lightning");
                Forms(b, "plasmoth", A, X);
            }
        }
    }
}
