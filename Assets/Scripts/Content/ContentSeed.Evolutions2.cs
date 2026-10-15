using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Real evolutions, second wave (2026-10-15, user: "do 2nd wave of evolutions"). 25 more small kin that look like
    /// babies, at least two from every land, plus the three common underground kin: Puffpod, Brookit, Zapfly, Hearthog,
    /// Lumipup, Flickadee (Sunmeadow), Cinderbug, Cactoad (Badlands), Staticub, Needlekit (Peaks), Cocorab, Gullwing,
    /// Palmunk (Isles), Skeeter, Lotuskip (Mirefen), Scarabit, Meerkit (Dunes), Blightrat, Rotgrub (Blightmoor),
    /// Neonewt, Glimworm (Glowdeep), Clayling (every land), Pebblit, Echobat, Dripslug (caves). Same tools and rules as
    /// the first wave (`ContentSeed.Evolutions`): Ascended at Lv 20, Exalted at Lv 35 with its own skill; only
    /// single-element kinds gain a second element (Zapfly +Gale, Lumipup +Ember, Clayling +Ember).
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildEvoSkills2(Bundle b)
        {
            Ab(b, "dandelion_gale", "Dandelion Gale", Element.Gale, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.35f, 5.5f);
            Ab(b, "waterfall_pounce", "Waterfall Pounce", Element.Tide, AbilityKind.Dash, 33, 9f, 6.5f, StatusType.Soaked, 0.45f);
            Ab(b, "arc_swarm", "Arc Swarm", Element.Volt, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Shock, 0.4f, 5.5f);
            Ab(b, "furnace_charge", "Furnace Charge", Element.Ember, AbilityKind.Dash, 34, 9f, 7f, StatusType.Burn, 0.4f);
            Ab(b, "sunflare_howl", "Sunflare Howl", Element.Lumen, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Burn, 0.3f, 5f);
            Ab(b, "ember_swoop", "Ember Swoop", Element.Ember, AbilityKind.Dash, 33, 10f, 6.5f, StatusType.Burn, 0.4f);
            Ab(b, "eruption", "Eruption", Element.Ember, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Burn, 0.45f, 5f);
            Ab(b, "needle_storm", "Needle Storm", Element.Flora, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.None, 0f, 3f, 22f);
            Ab(b, "static_blizzard", "Static Blizzard", Element.Volt, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.Chill, 0.4f, 5.5f);
            Ab(b, "pine_lance", "Pine Lance", Element.Frost, AbilityKind.Projectile, 33, 14f, 6.5f, StatusType.Chill, 0.4f, 3f, 24f);
            Ab(b, "coconut_crush", "Coconut Crush", Element.Stone, AbilityKind.Melee, 34, 2.4f, 7f);
            Ab(b, "squall_dive", "Squall Dive", Element.Gale, AbilityKind.Dash, 33, 10f, 6.5f, StatusType.Soaked, 0.4f);
            Ab(b, "frond_slam", "Frond Slam", Element.Flora, AbilityKind.Melee, 34, 2.6f, 7f, StatusType.Drowsy, 0.25f);
            Ab(b, "bog_sting", "Bog Sting", Element.Shade, AbilityKind.Melee, 33, 2.4f, 6.5f, StatusType.Poison, 0.45f);
            Ab(b, "lotus_bloom", "Lotus Bloom", Element.Flora, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.4f, 5f);
            Ab(b, "solar_roll", "Solar Roll", Element.Lumen, AbilityKind.Dash, 33, 9f, 6.5f, StatusType.Burn, 0.25f);
            Ab(b, "sentry_strike", "Sentry Strike", Element.Normal, AbilityKind.Melee, 33, 2.4f, 6.5f);
            Ab(b, "plague_swarm", "Plague Swarm", Element.Shade, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Poison, 0.5f, 5.5f);
            Ab(b, "rot_burrow", "Rot Burrow", Element.Shade, AbilityKind.Dash, 33, 9f, 7f, StatusType.Poison, 0.4f);
            Ab(b, "neon_spit", "Neon Spit", Element.Lumen, AbilityKind.Projectile, 33, 13f, 6.5f, StatusType.Soaked, 0.35f, 3f, 21f);
            Ab(b, "lure_light", "Lure Light", Element.Lumen, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.45f, 5.5f);
            Ab(b, "kiln_smash", "Kiln Smash", Element.Stone, AbilityKind.Melee, 35, 2.6f, 7.5f, StatusType.Burn, 0.3f);
            Ab(b, "avalanche_roll", "Avalanche Roll", Element.Stone, AbilityKind.Dash, 35, 9f, 7f);
            Ab(b, "sonic_screech", "Sonic Screech", Element.Gale, AbilityKind.Burst, 32, 4.5f, 8f, StatusType.Drowsy, 0.4f, 5.5f);
            Ab(b, "stalactite_rain", "Stalactite Rain", Element.Stone, AbilityKind.Burst, 33, 4.5f, 8f, StatusType.None, 0f, 5f);
        }

        static void BuildEvolutions2(Bundle b)
        {
            const PartShape Sph = PartShape.Sphere, Con = PartShape.Cone, Cry = PartShape.Crystal, Dom = PartShape.Dome, Box = PartShape.Box, Cap = PartShape.Capsule, Cyl = PartShape.Cylinder, Rck = PartShape.Rock;
            const BodyGroup gB = BodyGroup.Body, gH = BodyGroup.Head, gT = BodyGroup.Tail, gAL = BodyGroup.ArmL, gAR = BodyGroup.ArmR, gWL = BodyGroup.WingL, gWR = BodyGroup.WingR;
            const ColorSlot cP = ColorSlot.Primary, cS = ColorSlot.Secondary, cA = ColorSlot.Accent, cG = ColorSlot.Glow, cD = ColorSlot.Dark, cW = ColorSlot.White;
            EvoForm A, X;
            bool Wing(BodyPart q) => q.group == gWL || q.group == gWR;
            // the biggest rounded part of a group (its trunk), so add-ons like a stalactite or a collar don't count
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
                // half its height as it actually stands (a capsule lying along z is short, not tall)
                var m = Main(e, gB); var r = Quaternion.Euler(m.rot);
                float half = 0.5f * (Mathf.Abs((r * Vector3.right).y) * m.scale.x + Mathf.Abs((r * Vector3.up).y) * m.scale.y + Mathf.Abs((r * Vector3.forward).y) * m.scale.z);
                return m.pos.y + half * 0.9f;
            }

            // ---------------------------------------------------------------- Sunmeadow
            {   // Puffpod: the puff swells into a cloud with seeds, then a dandelion crown of parachutes
                var e = EvoOf(b, "puffpod");
                e.Body(1.25f);
                e.With(p => { var bb = e.B(gB); p.Ring(Sph, gB, cW, V(0, bb.center.y - bb.size.y * 0.12f, bb.center.z), bb.size.x * 0.46f, 6, V(bb.size.x * 0.3f, bb.size.y * 0.26f, bb.size.x * 0.3f)); });
                A = e.Form("Puffloft", "A rolling cloud of fluff that rides the warm air all day.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Cyl, gB, cS, V(0, bb.max.y - 0.04f, bb.center.z), 0.14f, 8, V(0.015f, 0.34f, 0.015f), 34f);
                    p.Ring(Sph, gB, cW, V(0, bb.max.y + 0.2f, bb.center.z), 0.28f, 8, V(0.15f, 0.07f, 0.15f));
                    p.Ring(Sph, gB, cG, V(0, bb.center.y, bb.center.z), bb.size.x * 0.5f, 5, V(0.07f, 0.07f, 0.07f), 0f, 36f);
                });
                X = e.Form("Zephyrbloom", "A dandelion clock the size of a cart, crowned with seeds that drift off and put to sleep whatever they land on.", Element.Normal, "dandelion_gale");
                Forms(b, "puffpod", A, X);
            }
            {   // Brookit: the brook pup lengthens into a river otter, then a crested torrent otter
                var e = EvoOf(b, "brookit");
                e.Legs(1.15f).Head(0.9f).Body(new Vector3(1f, 1f, 1.18f)).Scale(gT, 1.4f);
                A = e.Form("Rillotter", "Long and sleek; it swims faster than the stream it was born in.");
                e.Legs(1.1f).Body(new Vector3(1.1f, 1.08f, 1.12f)).Scale(gT, 1.3f).Head(0.94f);
                e.With(p =>
                {
                    var bb = e.B(gB); var t = e.B(gT);
                    p.Spikes(gB, cS, Top(e) - 0.08f, bb.max.z - 0.15f, bb.min.z + 0.1f, 4, 0.08f, 0.2f, 0.02f, -35f);
                    p.Fins(cS, V(0.05f, t.center.y, t.min.z + 0.06f), 0.25f, 0.18f, 8f, 20f);
                    p.Brows(cD);
                });
                X = e.Form("Cascadotter", "A river guardian with a finned crest and a tail like a paddle. It hits like a waterfall.", Element.Normal, "waterfall_pounce");
                Forms(b, "brookit", A, X);
            }
            {   // Zapfly: longer wings, then a second pair and a storm-lit tail
                var e = EvoOf(b, "zapfly");
                e.Wings(1.25f).Body(new Vector3(1f, 1f, 1.25f)).Scale(gT, 1.3f);
                A = e.Form("Zapwing", "Its wings crackle when it turns; the hum carries a mile.");
                e.Wings(1.15f).Body(new Vector3(1.1f, 1.1f, 1.15f));
                e.Dup(Wing, q => { q.pos.z -= 0.16f; q.pos.y -= 0.03f; q.scale *= 0.82f; q.slot = ColorSlot.Glow; return q; });
                e.With(p => { var bb = e.B(gB); var t = e.B(gT); p.Spikes(gT, cG, t.max.y - 0.02f, t.max.z - 0.05f, t.min.z + 0.05f, 4, 0.05f, 0.12f, 0f, -60f, 0f, 0f, Cry); });
                X = e.Form("Thundrifly", "Four wings of light and a tail full of lightning. It rides the storm front. It gains the Gale element.", Element.Gale, "arc_swarm");
                Forms(b, "zapfly", A, X);
            }
            {   // Hearthog: legs, tusks and longer quills, then a forge-hot boar
                var e = EvoOf(b, "hearthog");
                e.Legs(1.25f).Head(0.9f).Body(1.14f).Stretch(q => q.shape == Con && q.group == gB, 1.3f);
                e.With(p => { var s = e.Skull; p.Mirror(Con, gH, cW, V(s.scale.x * 0.28f, s.pos.y - s.scale.y * 0.24f, s.pos.z + s.scale.z * 0.4f), V(0.05f, 0.16f, 0.05f), V(-60, 0, -20)); });
                A = e.Form("Hearthboar", "A stocky young boar with tusks, its quills smouldering.");
                e.Legs(1.1f).Body(1.14f).Stretch(q => q.shape == Con && q.group == gB, 1.25f, 1.1f).Recolor(q => q.shape == Con && q.group == gB, cG);
                e.With(p =>
                {
                    var bb = e.B(gB); var s = e.Skull;
                    p.Mirror(Con, gH, cW, V(s.scale.x * 0.32f, s.pos.y - s.scale.y * 0.2f, s.pos.z + s.scale.z * 0.45f), V(0.07f, 0.26f, 0.07f), V(-55, 0, -25));
                    p.Ring(Rck, gB, cD, V(0, bb.min.y + bb.size.y * 0.35f, bb.center.z), bb.size.x * 0.48f, 6, V(0.14f, 0.1f, 0.14f));
                    p.Brows(cD);
                });
                X = e.Form("Forgeboar", "A boar with iron-dark plates and a back of glowing quills, hot as a smithy's coals.", Element.Normal, "furnace_charge");
                Forms(b, "hearthog", A, X);
            }
            {   // Lumipup: a lanky hound, then a sun hound with a mane of rays
                var e = EvoOf(b, "lumipup");
                e.Legs(1.3f).Head(0.88f).Body(new Vector3(1f, 1f, 1.15f)).Scale(gT, 1.25f);
                A = e.Form("Lumihound", "Long-legged and bright-eyed, its halo now a steady glow.");
                e.Legs(1.1f).Body(1.1f).Head(0.94f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Fan(Con, gH, cG, V(0, s.pos.y, s.pos.z - s.scale.z * 0.3f), s.scale.x * 0.62f, 11, V(0.08f, 0.22f, 0.05f));
                    p.Fan(Con, gH, cA, V(0, s.pos.y, s.pos.z - s.scale.z * 0.36f), s.scale.x * 0.5f, 11, V(0.07f, 0.16f, 0.04f), 16f);
                });
                var pal = e.Pal; pal.glow = Hex("#FFC23A");
                X = e.Form("Solarhound", "A hound wearing the sun as a mane. Its howl is warm enough to burn. It gains the Ember element.", Element.Ember, "sunflare_howl", pal);
                Forms(b, "lumipup", A, X);
            }
            {   // Flickadee: bigger wings and tail, then a fire-crested finch with streaming tail flames
                var e = EvoOf(b, "flickadee");
                e.Wings(1.3f).Body(1.1f).Head(0.92f).Scale(gT, 1.45f);
                A = e.Form("Flickerwing", "A quick little firebird; sparks fall from its tail when it turns.");
                e.Wings(1.25f).Body(1.08f);
                e.With(p =>
                {
                    var s = e.Skull; var t = e.B(gT);
                    p.Fan(Con, gH, cG, V(0, s.pos.y + s.scale.y * 0.4f, s.pos.z - s.scale.z * 0.1f), 0.06f, 5, V(0.05f, 0.18f, 0.04f), -50f, 100f);
                    p.WhipPair(gT, gT, cG, V(0.04f, t.center.y, t.min.z), V(0.015f, -0.01f, -0.09f), V(0.004f, 0.008f, -0.004f), 4, 0.05f, 0.025f);
                });
                X = e.Form("Pyrafinch", "A finch of living flame with a crest of fire and two streamers that trail sparks.", Element.Normal, "ember_swoop");
                Forms(b, "flickadee", A, X);
            }

            // ---------------------------------------------------------------- Badlands
            {   // Cinderbug: a heavier beetle with a real horn, then lava running in its shell
                var e = EvoOf(b, "cinderbug");
                e.Body(new Vector3(1.18f, 1.12f, 1.2f)).Legs(1.15f).Stretch(q => q.shape == Con && q.group == gH, 1.6f, 1.15f);
                A = e.Form("Cinderbeetle", "Its shell has hardened to rock and its horn to a pick.");
                e.Body(1.14f).Stretch(q => q.shape == Con && q.group == gH, 1.25f, 1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, bb.max.y - 0.05f, bb.max.z - 0.12f, bb.min.z + 0.1f, 4, 0.07f, 0.12f, 0f, -10f, 0f, 0f, Cry);
                    p.Mirror(Sph, gB, cG, V(bb.size.x * 0.32f, bb.center.y + bb.size.y * 0.15f, bb.center.z), V(0.08f, 0.05f, bb.size.z * 0.6f));
                });
                X = e.Form("Magmabeetle", "Lava glows in the cracks of its shell. When it is angry, the cracks open.", Element.Normal, "eruption");
                Forms(b, "cinderbug", A, X);
            }
            {   // Cactoad: longer spines, then arms of cactus and a crown of flowers
                var e = EvoOf(b, "cactoad");
                e.Body(1.2f).Stretch(q => q.shape == Con, 1.4f);
                A = e.Form("Spinetoad", "Every spine has doubled. Nothing in the desert picks it up twice.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Mirror(Cap, gB, cP, V(bb.size.x * 0.42f, bb.max.y - 0.02f, bb.center.z - 0.05f), V(0.13f, 0.32f, 0.13f), V(0, 0, -12));
                    p.Mirror(Con, gB, cW, V(bb.size.x * 0.42f, bb.max.y + 0.16f, bb.center.z - 0.05f), V(0.03f, 0.08f, 0.03f), V(0, 0, -30));
                    p.Ring(Sph, gB, cA, V(0, bb.max.y + 0.02f, bb.center.z), 0.14f, 5, V(0.1f, 0.06f, 0.1f));
                });
                X = e.Form("Saguaroad", "A toad grown into a little saguaro, flowering on top. Its spines fly when it sneezes.", Element.Normal, "needle_storm");
                Forms(b, "cactoad", A, X);
            }

            // ---------------------------------------------------------------- Peaks
            {   // Staticub: longer legs and ears, then frost crystals along its back
                var e = EvoOf(b, "staticub");
                e.Legs(1.3f).Head(0.88f).Body(new Vector3(1.05f, 1.05f, 1.15f)).Stretch(e.AboveHead(0.25f), 1.3f);
                A = e.Form("Staticlaw", "A lynx-legged cub whose fur stands on end in any weather.");
                e.Legs(1.12f).Body(1.12f).Head(0.94f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, bb.max.y - 0.04f, bb.max.z - 0.1f, bb.min.z + 0.1f, 5, 0.07f, 0.22f, 0.03f, -15f, 0f, 0f, Cry);
                    p.Brows(cS);
                });
                X = e.Form("Blizzardclaw", "Its back is a ridge of ice that crackles with stored lightning; it hunts in the eye of the storm.", Element.Normal, "static_blizzard");
                Forms(b, "staticub", A, X);
            }
            {   // Needlekit: a taller sapling, then a snow-laden fir with a star on top
                var e = EvoOf(b, "needlekit");
                e.Body(new Vector3(1f, 1.25f, 1f)).Legs(1.2f).Arms(1.2f);
                A = e.Form("Needlequill", "A young fir on legs, its needles sharp enough to throw.");
                e.Body(new Vector3(1.12f, 1.18f, 1.12f)).Arms(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Sph, gB, cW, V(0, bb.center.y + bb.size.y * 0.1f, bb.center.z), bb.size.x * 0.4f, 7, V(0.13f, 0.06f, 0.13f));
                    p.Ring(Sph, gB, cW, V(0, bb.center.y - bb.size.y * 0.18f, bb.center.z), bb.size.x * 0.48f, 8, V(0.14f, 0.06f, 0.14f), 0f, 20f);
                    p.Add(Cry, gB, cG, V(0, bb.max.y + 0.12f, bb.center.z), V(0.1f, 0.24f, 0.1f));
                    p.Add(Cry, gB, cG, V(0, bb.max.y + 0.12f, bb.center.z), V(0.1f, 0.24f, 0.1f), V(180, 0, 0));
                });
                X = e.Form("Frostspine", "A snow-laden fir with a star of ice at its tip. It throws its needles like spears.", Element.Normal, "pine_lance");
                Forms(b, "needlekit", A, X);
            }

            // ---------------------------------------------------------------- Driftkey Isles
            {   // Cocorab: bigger claws, then a shell grown over with coral
                var e = EvoOf(b, "cocorab");
                e.Body(1.18f).Legs(1.12f).Arms(1.35f);
                e.With(p => { var bb = e.B(gB); if (!e.Has(gAL)) p.Mirror(Sph, gAL, gAR, cS, V(bb.size.x * 0.55f, bb.min.y + 0.1f, bb.max.z), V(0.14f, 0.12f, 0.18f)); });
                A = e.Form("Cococlaw", "Its claws have caught up with its coconut; it cracks them for fun.");
                e.Body(1.12f).Arms(1.2f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Ring(Cyl, gB, cA, V(0, bb.max.y - 0.04f, bb.center.z), bb.size.x * 0.22f, 6, V(0.035f, 0.22f, 0.035f), 30f);
                    p.Ring(Sph, gB, cA, V(0, bb.max.y + 0.08f, bb.center.z), bb.size.x * 0.3f, 6, V(0.07f, 0.07f, 0.07f));
                    p.Add(Sph, gB, cG, V(0, bb.max.y + 0.04f, bb.center.z), V(0.1f, 0.1f, 0.1f));
                });
                X = e.Form("Cocoreef", "Its shell is a little reef now, coral and a pearl on top. One squeeze breaks stone.", Element.Normal, "coconut_crush");
                Forms(b, "cocorab", A, X);
            }
            {   // Gullwing: broader wings, then a storm albatross with a crest
                var e = EvoOf(b, "gullwing");
                e.Wings(1.3f).Body(1.1f).Head(0.94f);
                A = e.Form("Galegull", "A broad-winged gull that sleeps on the wind far out at sea.");
                e.Wings(1.35f).Body(1.1f);
                e.With(p =>
                {
                    var s = e.Skull;
                    p.Fan(Con, gH, cS, V(0, s.pos.y + s.scale.y * 0.3f, s.pos.z - s.scale.z * 0.35f), 0.05f, 4, V(0.04f, 0.18f, 0.03f), -110f, 70f);
                });
                X = e.Form("Stormbatross", "Its wings span a boat. It follows storms out to sea and dives out of the squalls.", Element.Normal, "squall_dive");
                Forms(b, "gullwing", A, X);
            }
            {   // Palmunk: taller, then a great grove ape with a mantle of fronds
                var e = EvoOf(b, "palmunk");
                e.Legs(1.2f).Head(0.9f).Arms(1.2f).Stretch(e.AboveHead(0.2f), 1.3f, 1.15f);
                A = e.Form("Frondmunk", "Up on longer legs, its frond grown into a proper sunshade.");
                e.Body(new Vector3(1.25f, 1.15f, 1.2f)).Arms(1.35f).Legs(1.08f).Head(0.92f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fronds(gB, cA, 6, 0.3f, 0.1f, -0.2f, V(0, bb.max.y - 0.04f, bb.center.z), 0f);
                    p.Brows(cD);
                });
                X = e.Form("Grovekong", "A great palm ape with a mantle of fronds. One swing of its arm flattens a hut.", Element.Normal, "frond_slam");
                Forms(b, "palmunk", A, X);
            }

            // ---------------------------------------------------------------- Mirefen
            {   // Skeeter: longer legs and proboscis, then a marsh strider with four wings and a glowing belly
                var e = EvoOf(b, "skeeter");
                e.Wings(1.25f).Legs(1.25f).Stretch(q => q.group == gH && q.shape == Con, 1.4f);
                A = e.Form("Skeetlash", "Longer in the leg and the nose. You hear it before you see it.");
                e.Legs(1.25f).Body(1.1f);
                e.Dup(Wing, q => { q.pos.z -= 0.12f; q.scale *= 0.85f; return q; });
                e.With(p => { var t = e.Has(gT) ? e.B(gT) : e.B(gB); p.Add(Sph, t.size.y > 0 && e.Has(gT) ? gT : gB, cG, V(0, t.center.y, t.min.z + 0.08f), V(0.14f, 0.12f, 0.16f)); });
                X = e.Form("Mirestrider", "It walks on the water of the fen on long stilts, a green light in its belly to lure prey.", Element.Normal, "bog_sting");
                Forms(b, "skeeter", A, X);
            }
            {   // Lotuskip: a bigger lotus, then a sage with a lotus crown and long whiskers
                var e = EvoOf(b, "lotuskip");
                e.Body(1.15f).Legs(1.2f).Stretch(e.AboveHead(0.25f), 1.3f, 1.2f);
                A = e.Form("Lotusleap", "Its lotus has opened; it leaps from pad to pad without a ripple.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var s = e.Skull; float top = s.pos.y + s.scale.y * 0.42f;
                    p.Ring(Sph, gH, cA, V(0, top + 0.04f, s.pos.z), 0.16f, 8, V(0.16f, 0.05f, 0.08f), 70f);
                    p.Add(Sph, gH, cG, V(0, top + 0.06f, s.pos.z), V(0.08f, 0.08f, 0.08f));
                    p.WhipPair(gH, gH, cS, V(s.scale.x * 0.25f, s.pos.y - s.scale.y * 0.2f, s.pos.z + s.scale.z * 0.42f), V(0.03f, -0.04f, 0.01f), V(0f, -0.004f, 0f), 4, 0.025f, 0.012f);
                });
                X = e.Form("Lotusage", "An old frog sage under a full lotus crown. Its croak lulls the whole marsh to sleep.", Element.Normal, "lotus_bloom");
                Forms(b, "lotuskip", A, X);
            }

            // ---------------------------------------------------------------- Sunscour Dunes
            {   // Scarabit: a longer horn, then a sun scarab rolling a disc of light
                var e = EvoOf(b, "scarabit");
                e.Body(new Vector3(1.15f, 1.12f, 1.2f)).Legs(1.15f).Stretch(q => q.shape == Con, 1.5f, 1.1f);
                A = e.Form("Scaraglow", "Its shell has a sheen like dawn on the dunes.");
                e.Body(1.12f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Add(Sph, gB, cG, V(0, bb.max.y + 0.16f, bb.min.z + 0.05f), V(0.36f, 0.36f, 0.08f));
                    p.Fan(Con, gB, cA, V(0, bb.max.y + 0.16f, bb.min.z + 0.02f), 0.24f, 8, V(0.04f, 0.1f, 0.02f));
                });
                X = e.Form("Sunscarab", "It rolls a little sun across the sand, and leaves the path behind it scorched.", Element.Normal, "solar_roll");
                Forms(b, "scarabit", A, X);
            }
            {   // Meerkit: taller lookout, then a chieftain with a feather headdress and sash
                var e = EvoOf(b, "meerkit");
                e.Legs(1.15f).Head(0.92f).Body(new Vector3(1f, 1.1f, 1f));
                A = e.Form("Meerguard", "The tallest of the burrow, always on watch.");
                e.Body(new Vector3(1.06f, 1.08f, 1.06f));
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.Fan(Con, gH, cA, V(0, s.pos.y + s.scale.y * 0.32f, s.pos.z - s.scale.z * 0.12f), 0.12f, 7, V(0.04f, 0.2f, 0.02f), -70f, 140f);
                    p.Add(Cyl, gB, cS, V(0, bb.center.y + bb.size.y * 0.1f, bb.center.z), V(bb.size.x * 1.04f, 0.06f, bb.size.z * 1.04f), V(0, 0, 25));
                });
                X = e.Form("Meerchief", "Chief of the dune burrows, feathered and sashed. It calls the strike and lands it first.", Element.Normal, "sentry_strike");
                Forms(b, "meerkit", A, X);
            }

            // ---------------------------------------------------------------- Blightmoor
            {   // Blightrat: bigger and longer-tailed, then a rat king with a crown and spined back
                var e = EvoOf(b, "blightrat");
                e.Legs(1.2f).Head(0.9f).Body(1.15f).Scale(gT, 1.3f);
                A = e.Form("Plaguerat", "Bigger, bolder, and followed by a faint green haze.");
                e.Body(1.2f).Head(0.94f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.Ring(Cry, gH, cG, V(0, s.pos.y + s.scale.y * 0.42f, s.pos.z), s.scale.x * 0.26f, 5, V(0.05f, 0.12f, 0.05f), 10f);
                    p.Spikes(gB, cD, bb.max.y - 0.04f, bb.max.z - 0.1f, bb.min.z + 0.1f, 5, 0.06f, 0.16f, 0.02f, -30f);
                });
                X = e.Form("Blightking", "King of the rot-rats, crowned in poison crystal. Where it goes, a swarm follows.", Element.Normal, "plague_swarm");
                Forms(b, "blightrat", A, X);
            }
            {   // Rotgrub: longer, then a rot wyrm with mandibles and spines
                var e = EvoOf(b, "rotgrub");
                e.Body(1.18f).Scale(gT, 1.3f);
                A = e.Form("Rotworm", "A long pale worm that tunnels through anything dead.");
                e.Body(1.15f).Scale(gT, 1.15f);
                e.With(p =>
                {
                    var s = e.Skull; var bb = e.B(gB);
                    p.Mirror(Con, gH, cW, V(s.scale.x * 0.25f, s.pos.y - s.scale.y * 0.15f, s.pos.z + s.scale.z * 0.42f), V(0.05f, 0.2f, 0.05f), V(70, 0, -30));
                    p.Spikes(gB, cG, bb.max.y - 0.03f, bb.max.z - 0.05f, bb.min.z + 0.05f, 4, 0.06f, 0.16f, 0f, -20f);
                });
                X = e.Form("Rotwyrm", "A wyrm of the bogs with hooked jaws. It bursts out of the ground beneath its prey.", Element.Normal, "rot_burrow");
                Forms(b, "rotgrub", A, X);
            }

            // ---------------------------------------------------------------- Glowdeep
            {   // Neonewt: longer legs and frills, then a glowing salamander with a lit crest
                var e = EvoOf(b, "neonewt");
                e.Legs(1.2f).Body(new Vector3(1.05f, 1.05f, 1.18f)).Scale(gT, 1.2f);
                A = e.Form("Neonaxl", "Its gill frills have grown out like neon feathers.");
                e.Body(1.14f).Scale(gT, 1.25f);
                e.With(p =>
                {
                    var bb = e.B(gB); var t = e.B(gT);
                    p.Spikes(gB, cG, bb.max.y - 0.03f, bb.max.z - 0.08f, bb.min.z + 0.05f, 5, 0.05f, 0.16f, 0.02f, -25f);
                    p.Spikes(gT, cG, t.max.y - 0.02f, t.max.z - 0.03f, t.min.z + 0.05f, 3, 0.04f, 0.12f, 0f, -35f);
                });
                X = e.Form("Glowmander", "A salamander lit from inside, a crest of light from head to tail. It spits glowing water.", Element.Normal, "neon_spit");
                Forms(b, "neonewt", A, X);
            }
            {   // Glimworm: a bigger crawler, then it grows lantern wings
                var e = EvoOf(b, "glimworm");
                e.Body(1.15f).Scale(gT, 1.2f).Stretch(e.AboveHead(0.2f), 1.3f);
                A = e.Form("Glimcrawler", "A fat glowing caterpillar; the light along its back pulses as it breathes.");
                e.Body(1.1f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Wings(cS, cG, cA, V(0.12f, bb.max.y - 0.04f, bb.center.z), 0.6f, 0.4f, 24f);
                });
                X = e.Form("Lanternwing", "It has spun and hatched: a moth with lantern wings that draw the eye and keep it.", Element.Normal, "lure_light");
                Forms(b, "glimworm", A, X);
            }

            // ---------------------------------------------------------------- every land
            {   // Clayling: a fired golem, then a kiln guard with a glowing core
                var e = EvoOf(b, "clayling");
                e.Body(1.15f).Arms(1.2f).Legs(1.1f);
                e.With(p => { var bb = e.B(gB); p.Add(Cyl, gB, cS, V(0, bb.center.y + bb.size.y * 0.25f, bb.center.z), V(bb.size.x * 1.04f, 0.05f, bb.size.z * 1.04f)); });
                A = e.Form("Kilnling", "Fired hard in a vent's heat; a seam of glaze runs round it.");
                e.Body(1.18f).Arms(1.3f);
                e.With(p =>
                {
                    var bb = e.B(gB); var r = e.B(gAR);
                    var arm = Main(e, gAR);
                    p.Mirror(Dom, gAL, gAR, cS, V(arm.pos.x, arm.pos.y + arm.scale.y * 0.42f, arm.pos.z), V(arm.scale.x * 1.5f, arm.scale.x * 0.8f, arm.scale.z * 1.5f));
                    p.Add(Cyl, gB, cD, V(0, bb.max.y + 0.08f, bb.min.z + 0.1f), V(0.12f, 0.24f, 0.12f));
                    p.Add(Sph, gB, cG, V(0, bb.max.y + 0.22f, bb.min.z + 0.1f), V(0.1f, 0.08f, 0.1f));
                });
                var pal = e.Pal; pal.glow = Hex("#FF8A3A");
                X = e.Form("Kilnguard", "A guardian with a kiln for a heart; smoke curls from the chimney on its back. It gains the Ember element.", Element.Ember, "kiln_smash", pal);
                Forms(b, "clayling", A, X);
            }

            // ---------------------------------------------------------------- underground
            {   // Pebblit: a boulder bug, then a crystal-studded roller
                var e = EvoOf(b, "pebblit");
                e.Body(1.25f).Legs(1.15f);
                A = e.Form("Bouldite", "Rolled up, it is a boulder; uncurled, it is a slightly annoyed boulder.");
                e.Body(1.22f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Spikes(gB, cG, bb.max.y - 0.06f, bb.max.z - 0.15f, bb.min.z + 0.12f, 5, 0.08f, 0.16f, 0.03f, 0f, 0f, 0f, Cry);
                    p.Spikes(gB, cG, bb.max.y - 0.14f, bb.max.z - 0.2f, bb.min.z + 0.18f, 4, 0.06f, 0.12f, 0.02f, 0f, bb.size.x * 0.28f, 40f, Cry);
                });
                X = e.Form("Rollossus", "A roller the size of a cart, studded with crystal. Tunnels in the deep places are the shape of it.", Element.Normal, "avalanche_roll");
                Forms(b, "pebblit", A, X);
            }
            {   // Echobat: bigger wings and ears, then a cloaked lord of the colony
                var e = EvoOf(b, "echobat");
                e.Wings(1.35f).Body(1.12f).Stretch(q => q.shape == Con && q.group == gB && q.pos.y > e.B(gB).center.y, 1.3f);
                A = e.Form("Echowing", "Its wings and ears have grown; it can hear a seed drop across the cavern.");
                e.Wings(1.3f).Body(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    p.Fan(Con, gB, cS, V(0, bb.center.y + bb.size.y * 0.1f, bb.min.z + 0.04f), bb.size.x * 0.42f, 7, V(0.06f, 0.18f, 0.04f), 110f, 140f);
                    p.Mirror(Con, gB, cW, V(0.07f, bb.center.y - bb.size.y * 0.15f, bb.max.z - 0.02f), V(0.035f, 0.1f, 0.035f), V(180, 0, 0));
                });
                X = e.Form("Echolord", "Lord of a thousand bats, a ruff about its shoulders. Its shriek knocks you senseless.", Element.Normal, "sonic_screech");
                Forms(b, "echobat", A, X);
            }
            {   // Dripslug: a longer slug with bigger stalactites, then a walking grotto
                var e = EvoOf(b, "dripslug");
                e.Body(new Vector3(1.05f, 1.05f, 1.25f)).Stretch(q => q.shape == Con && q.group == gB, 1.35f, 1.1f);
                A = e.Form("Stalaslug", "The stalactite on its back has grown a family.");
                e.Body(1.15f);
                e.With(p =>
                {
                    var bb = e.B(gB);
                    var m = Main(e, gB);
                    p.Spikes(gB, cA, Top(e) - 0.12f, m.pos.z + m.scale.z * 0.35f, m.pos.z - m.scale.z * 0.4f, 4, 0.12f, 0.3f, 0.04f, 0f, m.scale.x * 0.28f, 15f);
                    p.Ring(Sph, gB, cG, V(0, bb.min.y + 0.06f, bb.center.z), bb.size.x * 0.48f, 6, V(0.07f, 0.04f, 0.07f));
                });
                X = e.Form("Grottoslug", "A slug carrying a whole grotto: stalagmites, pools that glow, and the drip of a cave behind it.", Element.Normal, "stalactite_rain");
                Forms(b, "dripslug", A, X);
            }
        }
    }
}
