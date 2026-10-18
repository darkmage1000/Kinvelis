using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Real evolutions, fifth wave (2026-10-18, user: "knock out any pal still needing a evolution"). Every Common and
    /// Uncommon left without one, except the mounts and the styled characters (finished designs): Voltbeak, Kilnback,
    /// Sandsnare, Quartzclaw, Yetikin, Shiverwisp, Tuskaroo, Vinelash, Scorchorn, Magmite, Whipsquid, Sealance, Lashvolt,
    /// Hornolith, Scorpite, Windchime, Hexhorn, Lanternjaw, Prismite, Coralope, Seastride, Reedstalk, Murkfin, Vulturn,
    /// Blightbloom, Rustcrawler. Same tools and rules as the earlier waves; single-element kinds that gain: Kilnback
    /// +Stone, Yetikin +Gale, Seastride +Lumen.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildEvoSkills5(Bundle b)
        {
            Ab(b, "crackle_dance", "Crackle Dance", Element.Volt, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Shock, 0.4f, 5.5f);
            Ab(b, "kiln_roll", "Kiln Roll", Element.Ember, AbilityKind.Dash, 34, 9f, 7f, StatusType.Burn, 0.35f);
            Ab(b, "moonlight_strike", "Moonlight Strike", Element.Shade, AbilityKind.Dash, 33, 9f, 6.5f, StatusType.Drowsy, 0.35f);
            Ab(b, "rime_pinch", "Rime Pinch", Element.Frost, AbilityKind.Melee, 35, 2.4f, 7f, StatusType.Chill, 0.4f);
            Ab(b, "blizzard_hug", "Blizzard Hug", Element.Frost, AbilityKind.Melee, 35, 2.6f, 7f, StatusType.Chill, 0.35f);
            Ab(b, "frost_flowers", "Frost Flowers", Element.Frost, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Chill, 0.45f, 5.5f);
            Ab(b, "knot_swing", "Knot Swing", Element.Stone, AbilityKind.Melee, 35, 2.8f, 7f);
            Ab(b, "thorn_lash", "Thorn Lash", Element.Flora, AbilityKind.Melee, 33, 3f, 6.5f, StatusType.Poison, 0.35f);
            Ab(b, "molten_ram", "Molten Ram", Element.Ember, AbilityKind.Dash, 34, 9f, 7f, StatusType.Burn, 0.4f);
            Ab(b, "lava_spout", "Lava Spout", Element.Ember, AbilityKind.Projectile, 34, 13f, 6.5f, StatusType.Burn, 0.45f, 3f, 18f);
            Ab(b, "towel_snap", "Towel Snap", Element.Tide, AbilityKind.Melee, 33, 3f, 6.5f, StatusType.Soaked, 0.4f);
            Ab(b, "ice_drill", "Ice Drill", Element.Frost, AbilityKind.Dash, 34, 9f, 7f, StatusType.Chill, 0.35f);
            Ab(b, "live_wire", "Live Wire", Element.Volt, AbilityKind.Melee, 34, 3f, 6.5f, StatusType.Shock, 0.45f);
            Ab(b, "frill_quake", "Frill Quake", Element.Stone, AbilityKind.Burst, 34, 4.5f, 8f, StatusType.None, 0f, 5.5f);
            Ab(b, "sleep_sting", "Sleep Sting", Element.Shade, AbilityKind.Melee, 33, 2.6f, 6.5f, StatusType.Drowsy, 0.5f);
            Ab(b, "resonant_toll", "Resonant Toll", Element.Gale, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Drowsy, 0.4f, 6f);
            Ab(b, "dare_flame", "Dare Flame", Element.Ember, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Burn, 0.4f, 3f, 22f);
            Ab(b, "lure_bite", "Lure Bite", Element.Shade, AbilityKind.Melee, 34, 2.6f, 6.5f, StatusType.Drowsy, 0.3f);
            Ab(b, "rainbow_pinch", "Rainbow Pinch", Element.Lumen, AbilityKind.Melee, 35, 2.4f, 7f, StatusType.Drowsy, 0.2f);
            Ab(b, "tide_charge", "Tide Charge", Element.Tide, AbilityKind.Dash, 34, 9f, 7f, StatusType.Soaked, 0.4f);
            Ab(b, "surf_joust", "Surf Joust", Element.Tide, AbilityKind.Dash, 33, 10f, 6.5f, StatusType.Soaked, 0.35f);
            Ab(b, "heron_strike", "Heron Strike", Element.Gale, AbilityKind.Melee, 36, 2.8f, 7.5f);
            Ab(b, "mud_whiskers", "Mud Whiskers", Element.Tide, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Soaked, 0.45f, 5.5f);
            Ab(b, "grave_dive", "Grave Dive", Element.Shade, AbilityKind.Dash, 34, 10f, 7f, StatusType.Drowsy, 0.25f);
            Ab(b, "stench_cloud", "Stench Cloud", Element.Shade, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Poison, 0.5f, 5.5f);
            Ab(b, "rust_bite", "Rust Bite", Element.Stone, AbilityKind.Melee, 35, 2.4f, 7f, StatusType.Poison, 0.2f);
        }

        static void BuildEvolutions5(Bundle b)
        {
            const PartShape Sph = PartShape.Sphere, Con = PartShape.Cone, Cry = PartShape.Crystal, Dom = PartShape.Dome, Cyl = PartShape.Cylinder, Rck = PartShape.Rock;
            const BodyGroup gB = BodyGroup.Body, gH = BodyGroup.Head, gT = BodyGroup.Tail, gX = BodyGroup.Extra, gWL = BodyGroup.WingL, gWR = BodyGroup.WingR, gAL = BodyGroup.ArmL, gAR = BodyGroup.ArmR;
            const ColorSlot cS = ColorSlot.Secondary, cA = ColorSlot.Accent, cG = ColorSlot.Glow, cD = ColorSlot.Dark, cW = ColorSlot.White;
            EvoForm A, X;
            bool Wing(BodyPart q) => q.group == gWL || q.group == gWR;
            bool Arm(BodyPart q) => q.group == gAL || q.group == gAR;
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

            {   // Voltbeak: a taller strutter, then a fan of crackling tail plumes
                var e = EvoOf(b, "voltbeak");
                e.Legs(1.15f).Body(1.12f).Head(0.92f).Stretch(e.AboveHead(0.3f), 1.35f).Scale(gT, 1.3f);
                A = e.Form("Voltcrest", "Its zig-zag crest has doubled; it struts with sparks dripping off it.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var t = e.B(gT);
                    var bb = e.B(gB);
                    var c = V(0, bb.center.y + bb.size.y * 0.15f, bb.min.z - 0.04f);
                    p.Fan(Con, gT, cG, c, 0.3f, 11, V(0.1f, 0.52f, 0.03f), -80f, 160f);
                    p.Fan(Sph, gT, cW, c + V(0, 0, -0.02f), 0.52f, 11, V(0.09f, 0.09f, 0.03f), -80f, 160f);
                    p.Fan(Sph, gT, cS, c + V(0, 0, -0.03f), 0.52f, 11, V(0.05f, 0.05f, 0.03f), -80f, 160f);
                });
                X = e.Form("Peacrackle", "It spreads a fan of plumes humming with lightning and dances. Rivals who watch get shocked.", Element.Normal, "crackle_dance");
                Forms(b, "voltbeak", A, X);
            }
            {   // Kilnback: taller chimneys, then a furnace armadillo with glowing flues
                var e = EvoOf(b, "kilnback");
                e.Legs(1.12f).Body(1.18f).Head(0.92f).Stretch(q => q.group == gB && q.shape == Cyl, 1.35f);
                A = e.Form("Kilnshell", "Its chimneys stand tall and it bakes bricks in its shell.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var m = Main(e, gB); float t = Top(e);
                    p.Spikes(gB, cD, t - 0.04f, m.pos.z + m.scale.z * 0.3f, m.pos.z - m.scale.z * 0.35f, 4, 0.1f, 0.26f, 0.04f, 0f, m.scale.x * 0.26f, 25f, Cyl);
                    p.Spikes(gB, cG, t + 0.15f, m.pos.z + m.scale.z * 0.3f, m.pos.z - m.scale.z * 0.35f, 4, 0.08f, 0.05f, 0.04f, 0f, m.scale.x * 0.3f, 25f, Sph);
                });
                X = e.Form("Furnadillo", "An armadillo with a furnace for a shell, flues glowing down both sides. It rolls through you red-hot. It gains the Stone element.", Element.Stone, "kiln_roll");
                Forms(b, "kilnback", A, X);
            }
            {   // Sandsnare: longer, then a hooded moon serpent
                var e = EvoOf(b, "sandsnare");
                e.Body(1.15f).Scale(gT, 1.2f).Head(1.05f);
                A = e.Form("Duneshroud", "Its hood has spread wide; only its eyes show above the sand.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Mirror(Sph, gH, cS, V(s.scale.x * 0.6f, s.pos.y, s.pos.z - s.scale.z * 0.3f), V(0.06f, s.scale.y * 1.5f, s.scale.z), V(0, 0, -15));
                    p.Fan(Sph, gH, cG, V(0, s.pos.y + s.scale.y * 0.5f, s.pos.z - s.scale.z * 0.3f), 0.14f, 7, V(0.05f, 0.05f, 0.04f), -60f, 120f);
                });
                X = e.Form("Moonhood", "A hooded serpent with a crescent of light over its brow. It strikes by moonlight and you sleep through the rest.", Element.Normal, "moonlight_strike");
                Forms(b, "sandsnare", A, X);
            }
            {   // Quartzclaw: bigger claws, then spires of rime crystal
                var e = EvoOf(b, "quartzclaw");
                e.Body(1.18f).Arms(1.25f).Legs(1.08f);
                A = e.Form("Quartzshell", "Its shell has thickened into clear quartz; you can see the lake through it.");
                e.Body(1.1f).Arms(1.12f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, Top(e) - 0.05f, bb.max.z - 0.1f, bb.min.z + 0.1f, 4, 0.08f, 0.3f, 0.03f, 0f, 0f, 0f, Cry);
                    p.Spikes(gB, cW, Top(e) - 0.1f, bb.max.z - 0.12f, bb.min.z + 0.12f, 3, 0.06f, 0.2f, 0.02f, 0f, bb.size.x * 0.3f, 35f, Cry);
                });
                X = e.Form("Rimespire", "A crab crowned with spires of rime crystal. Its pinch freezes whatever it holds.", Element.Normal, "rime_pinch");
                Forms(b, "quartzclaw", A, X);
            }
            {   // Yetikin: a bigger yeti, then a titan with icicle shoulders
                var e = EvoOf(b, "yetikin");
                e.Legs(1.15f).Body(1.18f).Head(0.9f).Arms(1.2f);
                A = e.Form("Yetibrawn", "Big enough to be properly scary now. It still carries your bags.");
                e.Body(1.1f).Arms(1.12f);
                e.With(p =>
                {
                    var bb = e.B(gB); var s = e.Skull;
                    p.Mirror(Cry, gB, cG, V(bb.size.x * 0.42f, bb.max.y - 0.04f, bb.center.z), V(0.07f, 0.24f, 0.07f), V(0, 0, -25));
                    p.Mirror(Cry, gB, cW, V(bb.size.x * 0.32f, bb.max.y - 0.02f, bb.center.z - 0.06f), V(0.05f, 0.16f, 0.05f), V(0, 0, -15));
                    p.Brows(cW, 0.12f, 0.3f);
                });
                X = e.Form("Yetitan", "A yeti titan with icicles on its shoulders. It hugs, and the hug is a blizzard. It gains the Gale element.", Element.Gale, "blizzard_hug");
                Forms(b, "yetikin", A, X);
            }
            {   // Shiverwisp: more shards, then a wraith crowned in frost flowers
                var e = EvoOf(b, "shiverwisp");
                e.Body(1.2f).Scale(gX, 1.25f).Scale(gT, 1.2f);
                A = e.Form("Shiverwraith", "A colder breath; its shards circle wider and faster.");
                e.Body(1.08f);
                e.Dup(q => q.group == gX, q => { q.pos = new Vector3(-q.pos.x, q.pos.y + 0.12f, -q.pos.z); q.scale *= 0.8f; return q; });
                e.With(p => { var bb = e.B(gB); p.Ring(Cry, gB, cW, V(0, bb.max.y - 0.04f, bb.center.z), bb.size.x * 0.22f, 6, V(0.05f, 0.16f, 0.05f), 30f); });
                X = e.Form("Rimewraith", "A wraith of frost with a crown of ice flowers and a dozen shards orbiting it. It leaves frost flowers on you, too.", Element.Normal, "frost_flowers");
                Forms(b, "shiverwisp", A, X);
            }
            {   // Tuskaroo: longer tusks, then plated with a boulder tail
                var e = EvoOf(b, "tuskaroo");
                e.Legs(1.15f).Body(1.12f).Head(0.92f).Scale(gT, 1.25f).Stretch(q => q.group == gH && q.shape == Con, 1.4f);
                A = e.Form("Tuskbounder", "Longer tusks, heavier tail, and it bounds twice as far.");
                e.Body(1.1f).Scale(gT, 1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB); var t = e.B(gT);
                    p.Ring(Rck, gB, cS, V(0, bb.max.y - 0.06f, bb.center.z), bb.size.x * 0.35f, 5, V(0.14f, 0.1f, 0.14f));
                    p.Add(Rck, gT, cS, V(0, t.center.y, t.min.z + 0.05f), V(0.26f, 0.24f, 0.26f));
                });
                X = e.Form("Boulderoo", "A plated desert bruiser with a boulder knotted into its tail. It swings first, and it swings hard.", Element.Normal, "knot_swing");
                Forms(b, "tuskaroo", A, X);
            }
            {   // Vinelash: longer vines, then a pod crowned in thorny blooms
                var e = EvoOf(b, "vinelash");
                e.Body(1.15f).Head(1.08f).Arms(1.25f).Legs(1.1f);
                A = e.Form("Vinewhip", "Its vines reach twice as far and the thorns have hooks.");
                e.Body(1.08f).Arms(1.12f);
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g);
                    p.Ring(Sph, g, cA, V(0, hb.max.y - 0.02f, hb.center.z), hb.size.x * 0.32f, 6, V(0.1f, 0.07f, 0.1f));
                    p.Ring(Con, gB, cD, V(0, e.B(gB).center.y, e.B(gB).center.z), e.B(gB).size.x * 0.5f, 7, V(0.04f, 0.12f, 0.04f), 90f);
                });
                X = e.Form("Vinemaw", "A great snapping pod crowned in thorny flowers, its vines laced with barbs that sting.", Element.Normal, "thorn_lash");
                Forms(b, "vinelash", A, X);
            }
            {   // Scorchorn: longer horns, then horns glowing like iron in the forge
                var e = EvoOf(b, "scorchorn");
                e.Legs(1.15f).Body(1.12f).Head(0.92f).Stretch(e.AboveHead(0.2f), 1.35f, 1.1f);
                A = e.Form("Scorchram", "Its horns have curled into a ram's; they still never cool.");
                e.Body(1.08f).Stretch(e.AboveHead(0.2f), 1.2f).Recolor(e.AboveHead(0.35f), cG);
                e.With(p => { var bb = e.B(gB); p.Spikes(gB, cD, Top(e) - 0.03f, bb.max.z - 0.1f, bb.min.z + 0.1f, 4, 0.06f, 0.12f, 0.02f, -30f); });
                X = e.Form("Infernohorn", "A goat whose horns glow like iron in the forge. It rams boulders to light them, and you to make a point.", Element.Normal, "molten_ram");
                Forms(b, "scorchorn", A, X);
            }
            {   // Magmite: a bigger volcano, then a snail carrying a lava peak
                var e = EvoOf(b, "magmite");
                e.Body(1.18f).Scale(gX, 1.2f);
                A = e.Form("Magmasnail", "Its volcano has grown; it rumbles when it naps.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Add(Con, gB, cD, V(0, bb.max.y + 0.08f, bb.center.z - 0.05f), V(0.32f, 0.36f, 0.32f));
                    p.Add(Sph, gB, cG, V(0, bb.max.y + 0.26f, bb.center.z - 0.05f), V(0.12f, 0.08f, 0.12f));
                    p.Ring(Sph, gB, cG, V(0, bb.max.y + 0.02f, bb.center.z - 0.05f), 0.14f, 4, V(0.04f, 0.16f, 0.04f), 25f);
                });
                X = e.Form("Volcanail", "A snail with a real volcano on its back, lava running down the sides. Once it gets going, it spouts.", Element.Normal, "lava_spout");
                Forms(b, "magmite", A, X);
            }
            {   // Whipsquid: longer arms, then a masked squid bandit
                var e = EvoOf(b, "whipsquid");
                e.Body(1.12f).Legs(1.12f).Arms(1.3f);
                A = e.Form("Snapsquid", "Its arms snap so fast you hear them before you feel them.");
                e.Body(1.08f).Arms(1.12f);
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g);
                    p.Add(Cyl, g, cD, V(0, hb.center.y - hb.size.y * 0.05f, hb.center.z), V(hb.size.x * 1.06f, 0.07f, hb.size.z * 1.06f));
                    p.Mirror(Sph, g, cD, V(hb.size.x * 0.52f, hb.center.y - hb.size.y * 0.05f, hb.center.z - hb.size.z * 0.3f), V(0.04f, 0.04f, 0.14f), V(0, 25, 0));
                });
                X = e.Form("Squidbandit", "A squid in a bandit's mask with arms like wet whips. It snaps you, then steals whatever you dropped.", Element.Normal, "towel_snap");
                Forms(b, "whipsquid", A, X);
            }
            {   // Sealance: a longer horn, then a narwhal-seal with an ice drill
                var e = EvoOf(b, "sealance");
                e.Body(new Vector3(1.1f, 1.1f, 1.2f)).Head(0.92f).Stretch(q => q.group == gH && (q.shape == Con || q.shape == Cry), 1.3f);
                A = e.Form("Icelance", "Its spiral horn is longer than ever, and sharper.");
                e.Body(1.1f).Stretch(q => q.group == gH && (q.shape == Con || q.shape == Cry), 1.15f, 1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, Top(e) - 0.06f, bb.max.z - 0.15f, bb.min.z + 0.15f, 4, 0.06f, 0.16f, 0.02f, -20f, 0f, 0f, Cry);
                    p.Ring(Sph, gB, cW, V(0, bb.min.y + 0.05f, bb.center.z), bb.size.x * 0.48f, 6, V(0.1f, 0.05f, 0.1f));
                });
                X = e.Form("Glaciwhal", "A narwhal-seal with a horn like an ice drill and frost along its back. It bores straight through you.", Element.Normal, "ice_drill");
                Forms(b, "sealance", A, X);
            }
            {   // Lashvolt: longer, then a storm eel with crackling fins
                var e = EvoOf(b, "lashvolt");
                e.Body(1.15f).Scale(gT, 1.25f).Head(1.05f);
                A = e.Form("Lashcoil", "Longer and thicker; its bait-bulbs glow brighter.");
                e.Body(1.08f).Scale(gT, 1.12f);
                e.With(p =>
                {
                    var bb = e.B(gB); var t = e.B(gT);
                    p.Spikes(gB, cG, Top(e) - 0.03f, bb.max.z - 0.05f, bb.min.z + 0.05f, 4, 0.04f, 0.16f, 0f, -40f, 0f, 0f, Cry);
                    p.Spikes(gT, cG, t.max.y - 0.03f, t.max.z - 0.05f, t.min.z + 0.05f, 4, 0.035f, 0.13f, 0f, -45f, 0f, 0f, Cry);
                });
                X = e.Form("Stormeel", "An eel with a ridge of lightning down its back. Its feelers are live wires.", Element.Normal, "live_wire");
                Forms(b, "lashvolt", A, X);
            }
            {   // Hornolith: bigger horns and frill, then a wall of standing stones
                var e = EvoOf(b, "hornolith");
                e.Legs(1.12f).Body(1.12f).Head(0.95f).Stretch(q => q.group == gH && q.shape == Con, 1.3f);
                A = e.Form("Horncrag", "Its three horns are longer and its frill stones heavier.");
                e.Body(1.08f).Stretch(q => q.group == gH && q.shape == Con, 1.15f, 1.1f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Fan(Rck, gH, cS, V(0, s.pos.y + s.scale.y * 0.1f, s.pos.z - s.scale.z * 0.45f), s.scale.x * 0.95f, 7, V(0.14f, 0.3f, 0.1f), -80f, 160f);
                    p.Fan(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.1f, s.pos.z - s.scale.z * 0.5f), s.scale.x * 1.2f, 5, V(0.04f, 0.12f, 0.04f), -60f, 120f);
                });
                X = e.Form("Megalhorn", "Three ivory horns and a frill like a ring of standing stones, rune-lit. It stamps and the canyon shakes.", Element.Normal, "frill_quake");
                Forms(b, "hornolith", A, X);
            }
            {   // Scorpite: a bigger stinger, then a crystal-backed scorpion
                var e = EvoOf(b, "scorpite");
                e.Body(1.15f).Arms(1.2f).Legs(1.08f).Scale(gT, 1.2f).Stretch(q => q.group == gT && q.shape == Cry, 1.4f, 1.2f);
                A = e.Form("Scorpshard", "Its violet stinger has grown into a dagger of crystal.");
                e.Body(1.08f);
                e.With(p => { var bb = e.B(gB); p.Spikes(gB, cG, Top(e) - 0.05f, bb.max.z - 0.08f, bb.min.z + 0.08f, 4, 0.07f, 0.18f, 0.02f, 0f, bb.size.x * 0.25f, 30f, Cry); });
                X = e.Form("Amethorax", "A scorpion armoured in amethyst. One sting and you are asleep; you wake with your pockets lighter.", Element.Normal, "sleep_sting");
                Forms(b, "scorpite", A, X);
            }
            {   // Windchime: a bigger bell, then three bells ringing together
                var e = EvoOf(b, "windchime");
                e.Body(1.2f).Scale(gT, 1.2f);
                A = e.Form("Chimebell", "A bigger bell with a deeper note; its ribbons stream on the wind.");
                e.Body(1.05f);
                e.With(p =>
                {
                    // two smaller bells hung either side on a bar, each with its clapper
                    var m = Main(e, gB);
                    float w = Mathf.Max(m.scale.x, m.scale.z);
                    p.Add(Cyl, gB, cD, V(0, m.pos.y + m.scale.y * 0.45f, m.pos.z), V(0.03f, w * 1.6f, 0.03f), V(0, 0, 90));
                    p.Mirror(Dom, gB, ColorSlot.Primary, V(w * 0.72f, m.pos.y - m.scale.y * 0.05f, m.pos.z), V(w * 0.5f, w * 0.55f, w * 0.5f));
                    p.Mirror(Cyl, gB, cA, V(w * 0.72f, m.pos.y - m.scale.y * 0.06f, m.pos.z), V(w * 0.52f, 0.03f, w * 0.52f));
                    p.Mirror(Sph, gB, cG, V(w * 0.72f, m.pos.y - m.scale.y * 0.12f, m.pos.z), V(0.07f, 0.07f, 0.07f));
                    p.Mirror(Cyl, gB, cD, V(w * 0.72f, m.pos.y + m.scale.y * 0.32f, m.pos.z), V(0.015f, 0.14f, 0.015f));
                });
                X = e.Form("Carillon", "Three bells of blown glass that ring in chords. The whole valley stops to listen, and some of it falls asleep.", Element.Normal, "resonant_toll");
                Forms(b, "windchime", A, X);
            }
            {   // Hexhorn: bigger horns, then an arch-imp with a glowing sigil and flames on its horns
                var e = EvoOf(b, "hexhorn");
                e.Legs(1.15f).Body(1.12f).Head(0.92f).Wings(1.25f);
                A = e.Form("Hexbrute", "It has grown into its horns at last, and grown new ones.");
                e.Body(1.08f).Wings(1.12f).Stretch(e.AboveHead(0.3f), 1.15f).Recolor(e.AboveHead(0.55f), cG);
                e.With(p => { var bb = e.B(gB); p.Add(Cyl, gB, cG, V(0, bb.center.y, bb.max.z - 0.01f), V(0.16f, 0.02f, 0.16f), V(90, 0, 0)); p.Brows(cD); });
                X = e.Form("Archimp", "An imp with burning horn-tips and a glowing sigil on its belly. It dares you, then throws the fire anyway.", Element.Normal, "dare_flame");
                Forms(b, "hexhorn", A, X);
            }
            {   // Lanternjaw: a bigger lure, then a jaw ringed with lights
                var e = EvoOf(b, "lanternjaw");
                e.Body(1.18f).Head(1.08f).Wings(1.2f).Scale(gT, 1.2f);
                A = e.Form("Lanternmaw", "Its little light dangles further out now. The teeth are further back.");
                e.Body(1.08f).Head(1.05f);
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g);
                    p.Ring(Sph, g, cG, V(0, hb.center.y, hb.center.z), hb.size.x * 0.52f, 8, V(0.06f, 0.06f, 0.06f));
                    p.Ring(Con, g, cW, V(0, hb.min.y + hb.size.y * 0.3f, hb.max.z - 0.05f), hb.size.x * 0.32f, 6, V(0.04f, 0.1f, 0.04f), 180f);
                });
                X = e.Form("Beaconjaw", "An angler ringed with lights like a ship at night. By the time you see the teeth, you're asleep.", Element.Normal, "lure_bite");
                Forms(b, "lanternjaw", A, X);
            }
            {   // Prismite: bigger prisms, then a crab with a rainbow crown
                var e = EvoOf(b, "prismite");
                e.Body(1.18f).Arms(1.22f).Legs(1.08f).Stretch(q => q.shape == Cry, 1.3f);
                A = e.Form("Prismshell", "Its prisms have grown long; at noon the canyon is all rainbows.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fan(Cry, gB, cA, V(0, Top(e) - 0.04f, bb.center.z - 0.05f), 0.05f, 5, V(0.06f, 0.28f, 0.06f), -60f, 120f);
                    p.Fan(Sph, gB, cG, V(0, Top(e) + 0.02f, bb.center.z - 0.08f), bb.size.x * 0.5f, 7, V(0.06f, 0.06f, 0.06f), -80f, 160f);
                });
                X = e.Form("Rainclaw", "A crab under an arch of rainbow light. It clacks happily at each colour, then pinches you with one.", Element.Normal, "rainbow_pinch");
                Forms(b, "prismite", A, X);
            }
            {   // Coralope: a taller antelope, then a reef stag with great coral antlers
                var e = EvoOf(b, "coralope");
                e.Legs(1.15f).Body(1.1f).Head(0.94f).Stretch(e.AboveHead(0.3f), 1.35f, 1.15f);
                A = e.Form("Coralstag", "Its coral antlers have branched; little fish nibble them.");
                e.Body(1.08f).Stretch(e.AboveHead(0.3f), 1.2f, 1.1f);
                e.With(p => { var s = e.Skull; p.Fan(Cyl, gH, cA, V(0, s.pos.y + s.scale.y * 0.5f, s.pos.z - 0.04f), s.scale.x * 0.55f, 6, V(0.04f, 0.22f, 0.04f), -70f, 140f); });
                X = e.Form("Reefstag", "A stag with antlers like a whole reef. It charges through the shallows in a burst of spray.", Element.Normal, "tide_charge");
                Forms(b, "coralope", A, X);
            }
            {   // Seastride: bigger, then a crowned pearl steed
                var e = EvoOf(b, "seastride");
                e.Body(1.18f).Head(0.94f).Scale(gT, 1.2f).Wings(1.2f);
                A = e.Form("Seasteed", "A tall sea-horse that rides the crests of the waves.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Ring(Cry, gH, cW, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z), s.scale.x * 0.28f, 5, V(0.04f, 0.12f, 0.04f), 10f);
                    p.Add(Sph, gH, cG, V(0, s.pos.y + s.scale.y * 0.5f, s.pos.z + 0.02f), V(0.08f, 0.08f, 0.08f));
                    p.Spikes(gB, cS, Top(e) - 0.05f, e.B(gB).max.z - 0.05f, e.B(gB).min.z + 0.05f, 4, 0.05f, 0.14f, 0f, -40f);
                });
                X = e.Form("Pearlsteed", "A sea-horse crowned with a pearl and a crest of fins. It jousts across the waves. It gains the Lumen element.", Element.Lumen, "surf_joust");
                Forms(b, "seastride", A, X);
            }
            {   // Reedstalk: taller, then a storm heron with long plumes
                var e = EvoOf(b, "reedstalk");
                e.Legs(1.2f).Body(1.1f).Head(0.94f).Wings(1.2f);
                A = e.Form("Reedwader", "Taller than the reeds now; the fish still never see it.");
                e.Body(1.05f).Wings(1.1f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.3f, s.pos.z - s.scale.z * 0.4f), 0.05f, 3, V(0.025f, 0.32f, 0.02f), -120f, 50f);
                    p.Fan(Con, gB, cW, V(0, bb.center.y, bb.min.z + 0.02f), bb.size.x * 0.3f, 5, V(0.04f, 0.3f, 0.02f), 150f, 60f);
                });
                X = e.Form("Stormheron", "A grey heron trailing long plumes. It moves exactly once, and that once hits like a thunderclap.", Element.Normal, "heron_strike");
                Forms(b, "reedstalk", A, X);
            }
            {   // Murkfin: bigger, then a catfish baron with long whiskers and a crown
                var e = EvoOf(b, "murkfin");
                e.Legs(1.12f).Body(1.15f).Head(1.02f).Scale(gT, 1.2f);
                A = e.Form("Murkwhisker", "Its whiskers trail on the ground behind it, tasting everything.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.WhipPair(gH, gH, cS, V(s.scale.x * 0.35f, s.pos.y - s.scale.y * 0.15f, s.pos.z + s.scale.z * 0.42f), V(0.05f, -0.03f, 0.01f), V(0.002f, -0.004f, -0.01f), 6, 0.035f, 0.015f);
                    p.Ring(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z), s.scale.x * 0.22f, 5, V(0.04f, 0.1f, 0.04f), 10f);
                });
                X = e.Form("Mudbaron", "Baron of the mud flats, crowned and whiskered. A shake of its whiskers brings the swamp up with it.", Element.Normal, "mud_whiskers");
                Forms(b, "murkfin", A, X);
            }
            {   // Vulturn: broader wings, then a grave circler with a ruff of bone
                var e = EvoOf(b, "vulturn");
                e.Wings(1.2f).Body(1.12f).Head(0.94f);
                A = e.Form("Vulturoost", "Its wings span a cart. It waits more patiently than ever.");
                e.Wings(1.12f).Body(1.06f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Con, gB, cW, V(0, bb.max.y - 0.04f, bb.max.z - 0.1f), bb.size.x * 0.3f, 8, V(0.04f, 0.14f, 0.04f), 60f);
                    p.Ring(Sph, gB, cD, V(0, bb.max.y - 0.08f, bb.max.z - 0.1f), bb.size.x * 0.28f, 8, V(0.1f, 0.08f, 0.1f));
                });
                X = e.Form("Gravecircler", "A great vulture with a ruff of bone. Seeing three of these is very bad luck indeed.", Element.Normal, "grave_dive");
                Forms(b, "vulturn", A, X);
            }
            {   // Blightbloom: a bigger bloom, then a corpse flower queen
                var e = EvoOf(b, "blightbloom");
                e.Body(1.12f).Head(1.1f).Legs(1.1f);
                A = e.Form("Rotbloom", "Bigger, redder, and it smells even worse. It has fewer friends than ever.");
                e.Head(1.08f).Body(1.06f);
                e.With(p =>
                {
                    var g = HeadG(e); var hb = e.B(g);
                    p.Add(Cyl, g, cD, V(0, hb.center.y + hb.size.y * 0.1f, hb.center.z), V(hb.size.x * 0.3f, hb.size.y * 0.6f, hb.size.z * 0.3f));
                    p.Ring(Sph, g, cG, V(0, hb.max.y + 0.04f, hb.center.z), hb.size.x * 0.25f, 6, V(0.06f, 0.06f, 0.06f));
                });
                X = e.Form("Stenchqueen", "A corpse flower as big as a door, wafting a cloud that drops birds out of the sky. She is very sensitive about it.", Element.Normal, "stench_cloud");
                Forms(b, "blightbloom", A, X);
            }
            {   // Rustcrawler: heavier plates, then an iron maw with spikes
                var e = EvoOf(b, "rustcrawler");
                e.Body(1.15f).Legs(1.1f).Head(1.02f).Scale(gT, 1.2f);
                A = e.Form("Rustplate", "Its plates have thickened; it rattles like a cart full of scrap.");
                e.Body(1.08f);
                e.With(p =>
                {
                    var bb = e.B(gB); var s = e.Skull;
                    p.Spikes(gB, cD, Top(e) - 0.04f, bb.max.z - 0.08f, bb.min.z + 0.08f, 5, 0.07f, 0.2f, 0.02f, -25f);
                    p.Mirror(Con, gH, cW, V(s.scale.x * 0.3f, s.pos.y - s.scale.y * 0.3f, s.pos.z + s.scale.z * 0.42f), V(0.05f, 0.16f, 0.05f), V(120, 0, 0));
                });
                X = e.Form("Ironmaw", "A spiked crawler with iron jaws. It bites through a pick and leaves the rust behind.", Element.Normal, "rust_bite");
                Forms(b, "rustcrawler", A, X);
            }
        }
    }
}
