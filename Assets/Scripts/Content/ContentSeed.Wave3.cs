using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Wave 3 roster (#104-#178): 15 native kin for each outer land - Driftkey Isles, Mirefen, Sunscour Dunes,
    /// Blightmoor, Glowdeep (5 Common, 6 Uncommon, 3 Rare, 1 Epic each). One file per land (Wave3Isles, ...).
    /// <see cref="K"/> fills stats, spawn weight, drops and role text from rarity + a build letter, and the Plan
    /// "chassis" helpers (Quad / Biped / Blob + Face / Snout / Ears / Horns / Tail) lay down a body in one call.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildWave3(Bundle b)
        {
            // poison: a slower, longer burn (StatusType.Poison)
            Ab(b, "venom_spit", "Venom Spit", Element.Shade, AbilityKind.Projectile, 17, 12, 3f, StatusType.Poison, 0.35f, 3, 16f);
            Ab(b, "acid_bite", "Acid Bite", Element.Shade, AbilityKind.Melee, 25, 2.2f, 3.4f, StatusType.Poison, 0.3f);
            Ab(b, "toxic_cloud", "Toxic Cloud", Element.Shade, AbilityKind.Burst, 20, 4.5f, 6.5f, StatusType.Poison, 0.45f, 5f);
            Ab(b, "sand_blast", "Sand Blast", Element.Stone, AbilityKind.Burst, 22, 4.5f, 6f, StatusType.Drowsy, 0.2f, 5f);
            Ab(b, "wave_crash", "Wave Crash", Element.Tide, AbilityKind.Burst, 25, 4.5f, 6.5f, StatusType.Soaked, 0.4f, 5f);
            Ab(b, "glow_pulse", "Glow Pulse", Element.Lumen, AbilityKind.Burst, 24, 4.5f, 6.5f, StatusType.Drowsy, 0.25f, 5f);
            BuildWave3Isles(b);
            BuildWave3Swamp(b);
            BuildWave3Desert(b);
            BuildWave3Blight(b);
            BuildWave3Glow(b);
        }

        static string PerkText(CompanionPerk p)
        {
            switch (p)
            {
                case CompanionPerk.ForageBoost: return "Companion: +50% berries and fiber.";
                case CompanionPerk.LumberBoost: return "Companion: +50% wood.";
                case CompanionPerk.StoneBoost: return "Companion: +50% stone and ore from rocks.";
                case CompanionPerk.OreSense: return "Companion: +50% ore, cinder and crystal when mining.";
                case CompanionPerk.Swiftness: return "Companion: +12% run speed.";
                case CompanionPerk.Glow: return "Companion: lights up the night around you.";
                case CompanionPerk.Warmth: return "Companion: warmth aura (slower hunger) + light.";
                case CompanionPerk.Mending: return "Companion: slowly restores your HP.";
                default: return "";
            }
        }

        /// <summary>The species' own shimmer coat: its colours swung round the wheel and lifted.</summary>
        static Palette HueShift(Palette p, float shift)
        {
            Color S(Color c, float lift)
            {
                Color.RGBToHSV(c, out float h, out float s, out float v);
                return Color.HSVToRGB(Mathf.Repeat(h + shift, 1f), Mathf.Clamp01(s * 0.9f + 0.08f), Mathf.Clamp01(v + lift));
            }
            return new Palette { primary = S(p.primary, 0.08f), secondary = S(p.secondary, 0.05f), accent = S(p.accent, 0.1f), glow = S(p.glow, 0.05f), eye = p.eye };
        }

        /// <summary>
        /// Compact species definition. build: 'B' balanced, 'T' tank (slow, sturdy), 'F' fast (light), 'G' glass (hits hard).
        /// </summary>
        static SpeciesDef K(Bundle b, int dex, string id, string name, Element e1, Element e2, Rarity r, BiomeMask biome, Temperament temp, Locomotion loco,
            float size, char build, string lore, string personality, string[] abil, string rareAbil, WorkSkill[] work, CompanionPerk perk,
            string dropA, string dropB, Palette pal)
        {
            float hp = r == Rarity.Common ? 46f : r == Rarity.Uncommon ? 60f : r == Rarity.Rare ? 105f : 150f;
            float atk = r == Rarity.Common ? 10f : r == Rarity.Uncommon ? 13f : r == Rarity.Rare ? 20f : 25f;
            float def = r == Rarity.Common ? 9f : r == Rarity.Uncommon ? 12f : r == Rarity.Rare ? 16f : 20f;
            float walk = 2.4f, run = 6.5f;
            switch (build)
            {
                case 'T': hp *= 1.25f; def *= 1.35f; atk *= 0.9f; walk = 1.8f; run = 4.8f; break;
                case 'F': hp *= 0.9f; def *= 0.8f; atk *= 1.05f; walk = 3f; run = 8.2f; break;
                case 'G': atk *= 1.22f; def *= 0.78f; walk = 2.6f; run = 7f; break;
            }
            if (loco == Locomotion.Flyer) { walk += 0.3f; run += 0.8f; }
            float weight = r == Rarity.Common ? 2.2f : r == Rarity.Uncommon ? 1.3f : r == Rarity.Rare ? 0.5f : 0.1f;
            int minG = 1, maxG = r == Rarity.Common ? 3 : r == Rarity.Uncommon ? 2 : 1;
            if (r == Rarity.Common && temp == Temperament.Timid) { minG = 2; maxG = 4; }
            int n = r >= Rarity.Rare ? 2 : 1;
            var drops = string.IsNullOrEmpty(dropB) ? Drops((dropA, n, n + 1, 0.9f)) : Drops((dropA, n, n + 1, 0.9f), (dropB, 1, n, r >= Rarity.Rare ? 0.5f : 0.3f));
            return Kin(b, dex, id, name, e1, e2, r, biome, temp, loco, Mathf.RoundToInt(hp), Mathf.RoundToInt(atk), Mathf.RoundToInt(def), walk, run, size,
                lore, personality, weight, minG, maxG, abil, string.IsNullOrEmpty(rareAbil) ? null : A(rareAbil), work, perk, PerkText(perk), drops, pal, HueShift(pal, 0.42f));
        }

        partial class Plan
        {
            // the current "head": where Face / Snout / Ears / Horns attach
            float hy, hz, hs;
            BodyGroup hg = BodyGroup.Head;

            /// <summary>Four-legged body: an ellipsoid at height y (width, height, length), a round head out front, four legs.</summary>
            public Plan Quad(float y, float bw, float bh, float bl, float head, float legLen, float legW,
                ColorSlot body = ColorSlot.Primary, ColorSlot legs = ColorSlot.Primary, ColorSlot? foot = null, float headUp = 0.14f, float headOut = 0f)
            {
                Add(PartShape.Sphere, BodyGroup.Body, body, V(0, y, 0), V(bw, bh, bl));
                hs = head; hy = y + headUp; hz = bl * 0.5f + head * 0.18f + headOut; hg = BodyGroup.Head;
                Add(PartShape.Sphere, BodyGroup.Head, body, V(0, hy, hz), V(head, head * 0.92f, head * 0.96f));
                Pivot(BodyGroup.Head, V(0, hy - head * 0.2f, hz - head * 0.35f));
                return Legs4(legs, bw * 0.3f, bl * 0.3f, -bl * 0.32f, legLen, legW, foot);
            }

            /// <summary>Upright body: legs to hip height, a torso (width, height) above them, head on top, optional arms.</summary>
            public Plan Biped(float hip, float tw, float th, float head, float legW,
                ColorSlot body = ColorSlot.Primary, ColorSlot legs = ColorSlot.Primary, ColorSlot? foot = null, bool arms = true)
            {
                float ty = hip + th * 0.5f - 0.04f;
                Add(PartShape.Capsule, BodyGroup.Body, body, V(0, ty, 0), V(tw, th, tw * 0.9f));
                hs = head; hy = hip + th + head * 0.34f; hz = 0.02f; hg = BodyGroup.Head;
                Add(PartShape.Sphere, BodyGroup.Head, body, V(0, hy, hz), V(head, head * 0.92f, head * 0.94f));
                Pivot(BodyGroup.Head, V(0, hy - head * 0.4f, 0));
                Legs2(legs, tw * 0.26f, hip, legW, foot);
                if (arms)
                {
                    Mirror(PartShape.Capsule, BodyGroup.ArmL, BodyGroup.ArmR, body, V(tw * 0.58f, ty, 0.02f), V(legW * 0.8f, th * 0.8f, legW * 0.8f), V(0, 0, -12));
                    PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(tw * 0.5f, ty + th * 0.36f, 0));
                }
                return this;
            }

            /// <summary>One round body with the face on it (hoppers, floaters, slimes). y = its center.</summary>
            public Plan Blob(float y, float w, float h, float l, ColorSlot body = ColorSlot.Primary)
            {
                Add(PartShape.Sphere, BodyGroup.Body, body, V(0, y, 0), V(w, h, l));
                hg = BodyGroup.Body; hs = Mathf.Min(w, h); hy = y + h * 0.06f; hz = l * 0.47f - hs * 0.4f;
                return this;
            }

            /// <summary>Points Face / Snout / Ears / Horns at a head you placed yourself.</summary>
            public Plan Head(BodyGroup g, float y, float z, float size) { hg = g; hy = y; hz = z; hs = size; return this; }

            public Plan Face(float eye, ColorSlot slot = ColorSlot.Eye, float up = 0.1f, float spread = 0.24f) =>
                Eyes(hg, V(hs * spread, hy + hs * up, hz + hs * 0.4f), eye, slot);

            public Plan Snout(ColorSlot c, float w, float len = 0.6f, bool nose = true)
            {
                Add(PartShape.Sphere, hg, c, V(0, hy - hs * 0.13f, hz + hs * 0.42f), V(w, w * 0.7f, w * len * 1.3f));
                if (nose) Add(PartShape.Sphere, hg, ColorSlot.Dark, V(0, hy - hs * 0.08f, hz + hs * 0.42f + w * len * 0.62f), V(w * 0.3f, w * 0.24f, w * 0.2f));
                return this;
            }

            public Plan Beak(ColorSlot c, float w, float len) =>
                Add(PartShape.Cone, hg, c, V(0, hy - hs * 0.08f, hz + hs * 0.42f + len * 0.35f), V(w, len, w), V(95, 0, 0));

            public Plan Ears(PartShape shape, ColorSlot c, float w, float h, float tilt = 18f, float back = 0f) =>
                Mirror(shape, hg, c, V(hs * 0.3f, hy + hs * 0.36f + h * 0.4f, hz - hs * 0.05f - back), V(w, h, w * 0.45f), V(-8, 0, -tilt));

            public Plan Horns(ColorSlot c, float w, float h, float tilt = 20f, float lean = -20f) =>
                Mirror(PartShape.Cone, hg, c, V(hs * 0.22f, hy + hs * 0.4f + h * 0.4f, hz - hs * 0.05f), V(w, h, w), V(lean, 0, -tilt));

            public Plan Horn(ColorSlot c, float w, float h, float lean = 30f) =>
                Add(PartShape.Cone, hg, c, V(0, hy + hs * 0.4f + h * 0.36f, hz + hs * 0.2f), V(w, h, w), V(lean, 0, 0));

            /// <summary>Tail from (y, z) heading back, 'up' curls it upward (negative droops), with an optional round tip.</summary>
            public Plan Tail(ColorSlot c, float y, float z, float len, float d0, float d1, float up = 0.03f, ColorSlot? tip = null, float tipSize = 0f, float curl = 0f)
            {
                int n = Mathf.Max(3, Mathf.RoundToInt(len / 0.09f));
                Whip(BodyGroup.Tail, c, V(0, y, z), V(0, up, -0.09f), V(0, curl, curl * 0.4f), n, d0, d1);
                if (tip.HasValue) Tip(PartShape.Sphere, BodyGroup.Tail, tip.Value, V(tipSize, tipSize, tipSize));
                return Pivot(BodyGroup.Tail, V(0, y, z + 0.04f));
            }

            /// <summary>A flat wing / fin each side (flaps with the wing groups).</summary>
            public Plan Fins(ColorSlot c, Vector3 root, float span, float chord, float lift = 12f, float sweep = 0f)
            {
                float co = Mathf.Cos(lift * Mathf.Deg2Rad), si = Mathf.Sin(lift * Mathf.Deg2Rad);
                Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, c, root + new Vector3(span * 0.5f * co, span * 0.5f * si, -sweep * 0.5f), V(span, 0.05f, chord), V(0, -sweep * 40f, lift));
                return PivotPair(BodyGroup.WingL, BodyGroup.WingR, root);
            }

            /// <summary>
            /// Round head with eyes on the end of the last Whip (hydra necks, long-necked birds, eye stalks). pair = also on
            /// the mirrored whip of a WhipPair. For a centered whip, Snout / Horns / Ears then attach to it.
            /// </summary>
            public Plan TipHead(BodyGroup g, ColorSlot c, float size, float eye, ColorSlot eyeSlot = ColorSlot.Eye, bool pair = false, ColorSlot? jaw = null)
            {
                Vector3 tip = last + lastDir * (size * 0.3f);
                for (int side = 0; side < (pair ? 2 : 1); side++)
                {
                    Vector3 o = tip;
                    if (side == 1) o.x = -o.x;
                    Add(PartShape.Sphere, g, c, o, V(size, size * 0.9f, size * 1.1f));
                    if (jaw.HasValue) Add(PartShape.Sphere, g, jaw.Value, o + V(0, -size * 0.16f, size * 0.36f), V(size * 0.6f, size * 0.36f, size * 0.6f));
                    for (int e = -1; e <= 1; e += 2)
                    {
                        Vector3 ep = o + V(e * size * 0.24f, size * 0.12f, size * 0.44f);
                        Add(PartShape.Sphere, g, eyeSlot, ep, V(eye, eye * 1.15f, eye * 0.7f));
                        Add(PartShape.Sphere, g, ColorSlot.White, ep + V(0.015f, eye * 0.25f, eye * 0.3f), Vector3.one * eye * 0.35f);
                    }
                }
                hg = g; hs = size; hy = tip.y; hz = tip.z;
                return this;
            }

            public Plan Cheeks(ColorSlot c) =>
                Mirror(PartShape.Sphere, hg, c, V(hs * 0.33f, hy - hs * 0.07f, hz + hs * 0.31f), V(hs * 0.2f, hs * 0.13f, hs * 0.1f));

            /// <summary>Angry brow bars over the eyes placed by Face.</summary>
            public Plan Brows(ColorSlot c, float up = 0.1f, float spread = 0.24f) =>
                Mirror(PartShape.Box, hg, c, V(hs * spread, hy + hs * (up + 0.16f), hz + hs * 0.4f), V(hs * 0.26f, hs * 0.06f, hs * 0.08f), V(0, 0, 22));

            /// <summary>n flat leaves / petals / feathers radiating from a point (default: the end of the last Whip). rise lifts them.</summary>
            public Plan Fronds(BodyGroup g, ColorSlot c, int n, float len, float w, float rise = 0.35f, Vector3? at = null, float startDeg = 0f)
            {
                Vector3 o = at ?? last;
                for (int i = 0; i < n; i++)
                {
                    float a = (startDeg + i * 360f / n) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(a), rise, Mathf.Cos(a)).normalized;
                    Add(PartShape.Sphere, g, c, o + dir * (len * 0.5f), V(w, 0.04f, len), Quaternion.LookRotation(dir).eulerAngles);
                }
                return this;
            }

            /// <summary>Domed shell / cap on the body. y = its flat base; it rises h / 2 above that.</summary>
            public Plan Shell(ColorSlot c, float y, float w, float h, float l, float z = 0f) => Add(PartShape.Dome, BodyGroup.Body, c, V(0, y, z), V(w, h, l));
        }
    }
}
