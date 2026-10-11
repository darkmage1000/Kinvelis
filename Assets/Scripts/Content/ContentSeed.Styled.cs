using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The "styled" kin (user request 2026-10-09: "I like unique styled kin, like flora girl, ninjas" - then "knock
    /// them all out"): nineteen kin that are each a character you recognise at a glance, each with a signature skill
    /// no other kin has (AbilityDef.effect, rules in SkillEffects). #191-#209:
    ///   StyledA  warriors and casters   Kabutaro, Bulwarden, Jabaroo, Rapieron, Hexpurr, Prestofox, Marionoir, Morbeak
    ///   StyledB  performers and night   Lilute, Encorine, Flambear, Forgemole, Mortamoth, Vespertine, Armadraw, Cutlotter
    ///   StyledC  the three ladies       Sirelle (mermaid), Sekhara (lioness of the sun), Cindra (fire hound)
    /// A signature that deals damage is the kin's first move (known at Lv 1). One that only sets something up (a guard,
    /// a buff, a copy) is its second, learned at Lv 5, so a low-level wild one can still fight.
    /// </summary>
    public static partial class ContentSeed
    {
        static void Sig(Bundle b, string id, string name, Element e, AbilityKind k, AbilityEffect fx, float power, float range, float cd,
            float windup = -1f, StatusType st = StatusType.None, float stChance = 0f, float radius = 3f, float projSpeed = 16f)
        {
            Ab(b, id, name, e, k, power, range, cd, st, stChance, radius, projSpeed);
            var a = b.abilities[b.abilities.Count - 1];
            a.effect = fx;
            if (windup >= 0f) a.windup = windup;
        }

        static void BuildStyledAbilities(Bundle b)
        {
            // warriors
            Sig(b, "iaido_cut", "Iaido", Element.Stone, AbilityKind.Melee, AbilityEffect.Iaido, 24, 5f, 8f, 0.9f);
            Sig(b, "shield_wall", "Shield Wall", Element.Lumen, AbilityKind.Burst, AbilityEffect.Guard, 0, 9f, 12f, 0.25f);
            Sig(b, "one_two", "One-Two", Element.Normal, AbilityKind.Melee, AbilityEffect.Flurry, 9, 2f, 5f, 0.2f);
            Sig(b, "riposte", "Riposte", Element.Tide, AbilityKind.Melee, AbilityEffect.Riposte, 30, 3f, 7f, 0.1f);
            // casters
            Sig(b, "hex_mark", "Hex", Element.Shade, AbilityKind.Projectile, AbilityEffect.Hex, 14, 13f, 9f, 0.35f, StatusType.None, 0f, 3f, 18f);
            Sig(b, "vanishing_act", "Vanishing Act", Element.Lumen, AbilityKind.Burst, AbilityEffect.Decoy, 0, 9f, 14f, 0.2f);
            Sig(b, "puppet_strings", "Strings", Element.Shade, AbilityKind.Projectile, AbilityEffect.Tether, 12, 12f, 9f, 0.35f, StatusType.None, 0f, 3f, 22f);
            Sig(b, "tonic_toxin", "Tonic or Toxin", Element.Flora, AbilityKind.Projectile, AbilityEffect.Tonic, 15, 12f, 8f, 0.4f, StatusType.Poison, 1f, 3f, 15f);
            // performers
            Sig(b, "anthem", "Anthem", Element.Lumen, AbilityKind.Burst, AbilityEffect.Anthem, 0, 9f, 18f, 0.4f);
            Sig(b, "encore", "Encore", Element.Gale, AbilityKind.Projectile, AbilityEffect.Encore, 12, 11f, 4.5f, 0.3f, StatusType.None, 0f, 3f, 18f);
            Sig(b, "flambe", "Flambé", Element.Ember, AbilityKind.Burst, AbilityEffect.Sear, 22, 4f, 6f, 0.45f, StatusType.Burn, 0.6f, 4.5f);
            Sig(b, "temper", "Temper", Element.Ember, AbilityKind.Burst, AbilityEffect.Temper, 0, 9f, 14f, 0.3f);
            // night
            Sig(b, "reap", "Reap", Element.Shade, AbilityKind.Melee, AbilityEffect.Reap, 20, 2.8f, 5f, 0.4f);
            Sig(b, "blood_drain", "Drain", Element.Shade, AbilityKind.Melee, AbilityEffect.Drain, 19, 2.2f, 4.5f);
            Sig(b, "quickdraw", "Quickdraw", Element.Volt, AbilityKind.Projectile, AbilityEffect.Quickdraw, 17, 15f, 4f, 0.06f, StatusType.Shock, 0.1f, 3f, 46f);
            Sig(b, "plunder", "Plunder", Element.Tide, AbilityKind.Melee, AbilityEffect.Plunder, 20, 2.3f, 4f);
            // the three ladies
            Sig(b, "siren_song", "Siren Song", Element.Tide, AbilityKind.Burst, AbilityEffect.Pull, 18, 5f, 9f, 0.5f, StatusType.Soaked, 0.4f, 6.5f);
            Sig(b, "sun_wrath", "Wrath of the Sun", Element.Shade, AbilityKind.Burst, AbilityEffect.Wrath, 0, 9f, 16f, 0.3f);
            Sig(b, "heat_howl", "Heat Howl", Element.Ember, AbilityKind.Burst, AbilityEffect.Rally, 0, 9f, 18f, 0.35f);
        }

        static void BuildStyled(Bundle b)
        {
            BuildStyledA(b);
            BuildStyledB(b);
            BuildStyledC(b);
        }

        partial class Plan
        {
            /// <summary>
            /// Upright woman's figure for the humanoid kin: legs to hip height (or none - a mermaid brings her own tail),
            /// hips, a narrow waist, chest, shoulders, neck, head and arms. Points Face / Snout / Ears at the head.
            /// </summary>
            public Plan Lady(float hip, ColorSlot skin, ColorSlot torso, ColorSlot bust, ColorSlot hips, bool legs = true, float head = 0.26f)
            {
                if (legs)
                {
                    Legs2(skin, 0.085f, hip, 0.085f);
                    Mirror(Sph, gFL, gFR, skin, V(0.09f, hip - 0.17f, 0f), V(0.15f, 0.36f, 0.16f));           // thighs
                    Mirror(Sph, gFL, gFR, skin, V(0.085f, hip * 0.36f, -0.012f), V(0.105f, 0.3f, 0.115f));    // calves
                    Mirror(Sph, gFL, gFR, skin, V(0.085f, 0.035f, 0.045f), V(0.09f, 0.07f, 0.19f));           // feet
                }
                Add(Sph, gB, hips, V(0, hip + 0.03f, -0.008f), V(0.34f, 0.21f, 0.26f));
                Add(Sph, gB, skin, V(0, hip + 0.17f, 0f), V(0.2f, 0.22f, 0.165f));                            // waist
                Add(Sph, gB, torso, V(0, hip + 0.31f, -0.005f), V(0.27f, 0.23f, 0.19f));                      // ribs
                Mirror(Sph, gB, bust, V(0.066f, hip + 0.325f, 0.078f), V(0.135f, 0.125f, 0.125f));
                Mirror(Sph, gB, skin, V(0.145f, hip + 0.405f, -0.005f), V(0.1f, 0.085f, 0.1f));               // shoulders
                Add(Cyl, gB, skin, V(0, hip + 0.47f, 0f), V(0.068f, 0.11f, 0.068f));                          // neck
                hs = head; hy = hip + 0.52f + head * 0.42f; hz = 0.012f; hg = gH;
                Add(Sph, gH, skin, V(0, hy, hz), V(head * 0.94f, head, head * 0.96f));
                Pivot(gH, V(0, hip + 0.5f, 0));
                Mirror(Cap, gAL, gAR, skin, V(0.2f, hip + 0.2f, 0f), V(0.066f, 0.46f, 0.066f), V(0, 0, -9));
                Mirror(Sph, gAL, gAR, skin, V(0.236f, hip - 0.035f, 0.005f), V(0.075f, 0.09f, 0.075f));
                return PivotPair(gAL, gAR, V(0.15f, hip + 0.4f, 0f));
            }

            /// <summary>Eyes with a lash line over each, and lips, on the head Lady (or Head) set up.</summary>
            public Plan LadyFace(ColorSlot lips, float eye = 0.05f, ColorSlot iris = ColorSlot.Eye)
            {
                Eyes(hg, V(hs * 0.22f, hy + hs * 0.02f, hz + hs * 0.4f), eye, iris);
                Mirror(Bx, hg, ColorSlot.Dark, V(hs * 0.25f, hy + hs * 0.15f, hz + hs * 0.39f), V(hs * 0.27f, hs * 0.035f, hs * 0.07f), V(0, -14, 14));
                return Add(Sph, hg, lips, V(0, hy - hs * 0.27f, hz + hs * 0.41f), V(hs * 0.2f, hs * 0.08f, hs * 0.09f));
            }

            /// <summary>Hair: a cap over the skull, a swept fringe, and a long fall down the back with a lock over each shoulder.</summary>
            public Plan LongHair(ColorSlot c, int links, float width = 0.24f, ColorSlot? tip = null)
            {
                Add(Sph, hg, c, V(0, hy + hs * 0.14f, hz - hs * 0.16f), V(hs * 1.16f, hs * 1.02f, hs * 1.14f));
                Mirror(Sph, hg, c, V(hs * 0.3f, hy + hs * 0.36f, hz + hs * 0.32f), V(hs * 0.52f, hs * 0.3f, hs * 0.36f), V(0, 0, -22));
                Whip(hg, c, V(0, hy - hs * 0.05f, hz - hs * 0.52f), V(0, -0.05f, -0.012f), V(0, 0, 0.0016f), links, width, width * 0.42f);
                if (tip.HasValue) Tip(Sph, hg, tip.Value, V(width * 0.4f, width * 0.4f, width * 0.4f));
                return WhipPair(hg, hg, c, V(hs * 0.46f, hy - hs * 0.1f, hz - hs * 0.2f), V(0.006f, -0.05f, 0.002f), V(0, 0, 0.001f), Mathf.Max(4, links - 3), width * 0.6f, width * 0.3f);
            }
        }
    }
}
