using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What the explorer looks like: chosen in the character creator when a world is made (MainMenu) and changeable
    /// later from the Gear tab. Saved with the player (PlayerStats.look). Every field is an index into a list in
    /// <see cref="AvatarLook"/>, so old saves keep working when a list grows (indices are clamped when read).
    /// The defaults are the original explorer: teal jacket, brown hair, ranger hat, satchel.
    /// </summary>
    [Serializable]
    public class Appearance
    {
        public int body;            // 0 male, 1 female
        public int skin = 1;
        public int hair;
        public int hairColor = 2;
        public int eyes;
        public int outfit;
        public int colorA;          // the outfit's main colour
        public int colorB;          // its second colour
        public int accent;          // hat, satchel, trim
        public int hat = 1;
        public bool pack = true;

        public Appearance Clone() => (Appearance)MemberwiseClone();

        /// <summary>Mesh cache key: two explorers with the same key are the same model.</summary>
        public string Key(bool headGear) =>
            $"av_{body}_{skin}_{hair}_{hairColor}_{eyes}_{outfit}_{colorA}_{colorB}_{accent}_{hat}_{(pack ? 1 : 0)}_{(headGear ? 1 : 0)}";

        public static Appearance Random()
        {
            var a = new Appearance
            {
                body = UnityEngine.Random.Range(0, 2),
                skin = UnityEngine.Random.Range(0, AvatarLook.Skin.Length),
                hair = UnityEngine.Random.Range(0, AvatarLook.HairNames.Length),
                hairColor = UnityEngine.Random.Range(0, AvatarLook.HairColors.Length),
                eyes = UnityEngine.Random.Range(0, AvatarLook.EyeColors.Length),
                outfit = UnityEngine.Random.Range(0, AvatarLook.OutfitNames.Length),
                colorA = UnityEngine.Random.Range(0, AvatarLook.Cloth.Length),
                colorB = UnityEngine.Random.Range(0, AvatarLook.Cloth.Length),
                accent = UnityEngine.Random.Range(0, AvatarLook.Cloth.Length),
                hat = UnityEngine.Random.Range(0, AvatarLook.HatNames.Length),
                pack = UnityEngine.Random.value < 0.7f,
            };
            return a;
        }
    }

    /// <summary>
    /// Builds the explorer's body from an <see cref="Appearance"/>: the same skeleton as <see cref="PlayerLook"/> (same
    /// joints, so every animation, IK pose and piece of armour fits) with a male or female build, ten hair styles,
    /// seven starter outfits, six things to wear on the head and free colours. Outfits are looks only - they give no
    /// stats; armour from the Gear tab is drawn over them. Unlike a kin's five-colour palette, each part here carries
    /// its own colour (ModelBuilder.BuildCustom's colour list).
    /// </summary>
    public static class AvatarLook
    {
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static readonly string[] BodyNames = { "Male", "Female" };
        public static readonly Color[] Skin = { H("#ffe2cf"), new Color(0.98f, 0.8f, 0.66f), H("#e8b98f"), H("#c68e62"), H("#96633f"), H("#5e3b26") };
        public static readonly string[] HairNames = { "Short", "Buzz cut", "Spiky", "Mohawk", "Curly", "Bob", "Long", "Ponytail", "Bun", "Pigtails" };
        public static readonly Color[] HairColors =
        {
            H("#1c1a1e"), H("#3a2a20"), new Color(0.32f, 0.26f, 0.22f), H("#7a4a26"), H("#b5651d"), H("#e8c26a"),
            H("#f2ead8"), H("#c4372e"), H("#e86aa8"), H("#4a8ee8"), H("#3fae6a"), H("#8a5ad8"),
        };
        public static readonly Color[] EyeColors = { new Color(0.1f, 0.08f, 0.1f), H("#4a2c14"), H("#2a5ab8"), H("#2a8a4a"), H("#7a3ab8"), H("#b8421e") };
        public static readonly string[] OutfitNames = { "Explorer", "Trail tunic", "Vest", "Dress", "Overalls", "Cloak", "Hoodie" };
        /// <summary>Clothing colours. The first three are the original explorer's jacket, trousers and leather, so index 0 / 0 / 0 is the classic look.</summary>
        public static readonly Color[] Cloth =
        {
            new Color(0.2f, 0.6f, 0.62f), new Color(0.32f, 0.26f, 0.22f), new Color(0.86f, 0.62f, 0.32f),
            H("#c8372e"), H("#e8842a"), H("#f0c83a"), H("#5aa83a"), H("#2f6e4a"), H("#3a78d8"), H("#2a3a7a"),
            H("#8a4ad0"), H("#e86aa0"), H("#f4f0e6"), H("#8a8e98"), H("#2a2a32"), H("#7a5232"),
        };
        public static readonly string[] HatNames = { "Nothing", "Ranger hat", "Cap", "Headband", "Bow", "Goggles" };

        // which colour of Cloth each slot starts on (the classic explorer)
        public const int DefaultA = 0, DefaultB = 1, DefaultAccent = 2;

        static readonly Color Dark = new Color(0.12f, 0.1f, 0.12f), White = new Color(0.97f, 0.97f, 0.97f);
        /// <summary>A darker tone of a cloth colour (cuffs, hems, the inside of a hood).</summary>
        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        static T At<T>(T[] list, int i) => list[Mathf.Clamp(i, 0, list.Length - 1)];
        /// <summary>The three clothing colour fields are offsets from the classic colours, so a new Appearance() is the original explorer.</summary>
        static int Wrap(int v) => ((v % Cloth.Length) + Cloth.Length) % Cloth.Length;
        public static int IndexA(Appearance a) => Wrap(a.colorA + DefaultA);
        public static int IndexB(Appearance a) => Wrap(a.colorB + DefaultB);
        public static int IndexAccent(Appearance a) => Wrap(a.accent + DefaultAccent);
        public static void SetA(Appearance a, int swatch) => a.colorA = swatch - DefaultA;
        public static void SetB(Appearance a, int swatch) => a.colorB = swatch - DefaultB;
        public static void SetAccent(Appearance a, int swatch) => a.accent = swatch - DefaultAccent;
        public static Color ClothA(Appearance a) => Cloth[IndexA(a)];
        public static Color ClothB(Appearance a) => Cloth[IndexB(a)];
        public static Color Accent(Appearance a) => Cloth[IndexAccent(a)];

        /// <summary>True when what is worn on the head covers the top of the hair (so tall hair is flattened under it).</summary>
        static bool Covers(int hat) => hat == 1 || hat == 2;

        struct Out
        {
            public List<BodyPart> parts; public List<Color> cols;
            public void Add(PartShape s, BodyGroup g, Color c, float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0)
            {
                parts.Add(new BodyPart(s, g, ColorSlot.Primary, new Vector3(px, py, pz), new Vector3(sx, sy, sz), new Vector3(rx, ry, rz)));
                cols.Add(c);
            }
        }

        /// <summary>The explorer's parts and a colour for each. headGear = a helmet or hood from the Gear tab is worn, which replaces anything on the head.</summary>
        public static void Build(Appearance a, bool headGear, out BodyPart[] parts, out Color[] colors)
        {
            if (a == null) a = new Appearance();
            var o = new Out { parts = new List<BodyPart>(96), cols = new List<Color>(96) };
            bool fem = a.body == 1;
            Color skin = At(Skin, a.skin), hair = At(HairColors, a.hairColor), eye = At(EyeColors, a.eyes);
            Color A = ClothA(a), B = ClothB(a), C = Accent(a);
            int outfit = Mathf.Clamp(a.outfit, 0, OutfitNames.Length - 1);
            int hat = headGear ? 0 : Mathf.Clamp(a.hat, 0, HatNames.Length - 1);
            bool covered = headGear || Covers(hat);

            // ---- build: the same joints for both, a different silhouette ----
            float tw = fem ? 0.37f : 0.44f, td = fem ? 0.26f : 0.3f;       // torso width / depth
            float hipW = fem ? 0.46f : 0.43f;
            float sx = fem ? 0.225f : 0.25f, sr = fem ? 0.14f : 0.17f;     // shoulder ball
            float ax = fem ? 0.262f : 0.28f, at = fem ? 0.115f : 0.14f, ft = fem ? 0.1f : 0.125f;   // arm x, upper / fore thickness
            float lt = fem ? 0.17f : 0.18f, st = fem ? 0.14f : 0.155f;     // thigh / shin thickness
            float hand = fem ? 0.105f : 0.12f;

            // ---- what the outfit covers ----
            // sleeves: 0 none, 1 short, 2 long.  legs: 0 shorts, 1 trousers.  shoes: 0 boots, 1 trainers
            int sleeves = 2, legs = 1, shoes = 0;
            Color top = A, sleeve = A, legCol = B, hipCol = B;
            switch (outfit)
            {
                case 1: sleeves = 1; legs = 0; break;                       // trail tunic
                case 2: sleeves = 1; top = White; sleeve = White; break;    // vest over a shirt
                case 3: sleeves = 1; hipCol = A; break;                     // dress (leggings in B)
                case 4: break;                                              // overalls: shirt A, the rest B
                case 5: top = B; sleeve = B; legCol = Shade(B, 0.72f); hipCol = legCol; break;   // cloak (A) over a tunic (B)
                case 6: legs = 0; shoes = 1; break;                         // hoodie and shorts
            }
            float bulk = outfit == 6 ? 0.022f : 0f;                         // a hoodie's sleeves are roomy

            // ---- hips, belt ----
            o.Add(PartShape.Sphere, BodyGroup.Body, hipCol, 0, 0.82f, 0, hipW, 0.28f, 0.3f);
            if (outfit == 0 || outfit == 2 || outfit == 5)
            {
                o.Add(PartShape.Cylinder, BodyGroup.Body, Dark, 0, 0.9f, 0, tw, 0.07f, td + 0.01f);
                o.Add(PartShape.Box, BodyGroup.Body, C, 0, 0.9f, td * 0.5f + 0.005f, 0.08f, 0.07f, 0.03f);
            }

            // ---- torso ----
            o.Add(PartShape.Capsule, BodyGroup.Torso, top, 0, 1.14f, 0, tw, 0.54f, td);
            if (fem) o.Add(PartShape.Sphere, BodyGroup.Torso, outfit == 2 ? A : top, 0, 1.25f, 0.07f, tw * 0.74f, 0.15f, 0.15f);   // (the waistcoat closes over it)
            o.Add(PartShape.Cylinder, BodyGroup.Torso, skin, 0, 1.43f, 0, fem ? 0.12f : 0.14f, 0.1f, fem ? 0.12f : 0.14f);
            Color shoulder = sleeves == 0 ? skin : sleeve;
            o.Add(PartShape.Sphere, BodyGroup.Torso, shoulder, sx, 1.33f, 0, sr, sr * 0.94f, sr * 1.06f);
            o.Add(PartShape.Sphere, BodyGroup.Torso, shoulder, -sx, 1.33f, 0, sr, sr * 0.94f, sr * 1.06f);

            switch (outfit)
            {
                case 1:   // tunic: a flared hem over the shorts and a rope belt
                    o.Add(PartShape.Dome, BodyGroup.Body, A, 0, 0.7f, 0, hipW + 0.1f, 0.5f, 0.42f);
                    o.Add(PartShape.Cylinder, BodyGroup.Body, C, 0, 0.92f, 0, tw + 0.02f, 0.05f, td + 0.03f);
                    break;
                case 2:   // waistcoat: two rounded halves that wrap the sides and leave the shirt showing down the front, three buttons
                    o.Add(PartShape.Capsule, BodyGroup.Torso, A, tw * 0.31f, 1.125f, 0, tw * 0.54f, 0.5f, td * 1.12f);
                    o.Add(PartShape.Capsule, BodyGroup.Torso, A, -tw * 0.31f, 1.125f, 0, tw * 0.54f, 0.5f, td * 1.12f);
                    o.Add(PartShape.Capsule, BodyGroup.Torso, A, 0, 1.125f, -td * 0.2f, tw * 0.94f, 0.5f, td * 0.76f);
                    for (int i = 0; i < 3; i++)
                        o.Add(PartShape.Sphere, BodyGroup.Torso, C, tw * 0.1f, 1.22f - i * 0.1f, td * 0.56f, 0.035f, 0.035f, 0.025f);
                    break;
                case 3:   // dress: one colour from the collar to a bell skirt at the knee, trimmed at the neck and the hem
                    o.Add(PartShape.Dome, BodyGroup.Body, A, 0, 0.5f, 0, hipW + 0.24f, 0.82f, 0.56f);
                    o.Add(PartShape.Cylinder, BodyGroup.Body, C, 0, 0.52f, 0, hipW + 0.285f, 0.05f, 0.605f);
                    o.Add(PartShape.Cylinder, BodyGroup.Torso, C, 0, 1.385f, 0, tw * 0.52f, 0.035f, td * 0.82f);
                    break;
                case 4:   // overalls: bib, two straps, a pocket
                    o.Add(PartShape.Box, BodyGroup.Torso, B, 0, 1.08f, td * 0.5f - 0.005f, tw * 0.62f, 0.3f, 0.05f);
                    o.Add(PartShape.Box, BodyGroup.Torso, B, 0, 1.0f, 0, tw * 1.02f, 0.2f, td * 1.03f);
                    o.Add(PartShape.Box, BodyGroup.Torso, B, tw * 0.25f, 1.27f, 0, 0.06f, 0.2f, td * 1.06f);
                    o.Add(PartShape.Box, BodyGroup.Torso, B, -tw * 0.25f, 1.27f, 0, 0.06f, 0.2f, td * 1.06f);
                    o.Add(PartShape.Box, BodyGroup.Torso, C, 0, 1.1f, td * 0.5f + 0.02f, tw * 0.3f, 0.1f, 0.02f);
                    break;
                case 5:   // travelling cloak: a mantle over the shoulders, a long cape to the knee, the hood down, a clasp at the throat
                    o.Add(PartShape.Dome, BodyGroup.Torso, A, 0, 1.17f, -0.01f, tw + 0.3f, 0.52f, td + 0.26f);
                    // the cape hangs in five folds that fan out toward the hem and wrap a little round the sides
                    for (int i = -2; i <= 2; i++)
                    {
                        float side = Mathf.Abs(i) * 0.5f;                       // 0 in the middle, 1 at the edges
                        o.Add(PartShape.Capsule, BodyGroup.Torso, Shade(A, (i & 1) == 0 ? 0.92f : 0.8f),
                            i * (tw + 0.16f) * 0.22f, 0.84f, -td * 0.5f - 0.085f + side * 0.07f,
                            (tw + 0.16f) * 0.3f, 0.92f, 0.1f, 6f, i * -11f, i * 5.5f);
                    }
                    // the hood lies flat between the shoulders, a cowl stands round the neck
                    o.Add(PartShape.Sphere, BodyGroup.Torso, Shade(A, 0.9f), 0, 1.36f, -td * 0.5f - 0.07f, tw * 0.74f, 0.2f, 0.16f, -18);
                    o.Add(PartShape.Cylinder, BodyGroup.Torso, A, 0, 1.415f, -0.005f, tw * 0.62f, 0.08f, td * 1.02f);
                    o.Add(PartShape.Sphere, BodyGroup.Torso, C, 0, 1.345f, (td + 0.26f) * 0.36f, 0.07f, 0.07f, 0.05f);
                    break;
                case 6:   // hoodie: a roomy body with a ribbed hem, the hood down, a raised collar, a pouch pocket and drawstrings
                    o.Add(PartShape.Capsule, BodyGroup.Torso, A, 0, 1.13f, 0, tw + 0.05f, 0.52f, td + 0.05f);
                    o.Add(PartShape.Cylinder, BodyGroup.Torso, Shade(A, 0.78f), 0, 0.9f, 0, tw + 0.05f, 0.08f, td + 0.06f);
                    o.Add(PartShape.Sphere, BodyGroup.Torso, A, 0, 1.345f, -td * 0.5f - 0.06f, tw * 0.8f, 0.24f, 0.17f, -14);
                    o.Add(PartShape.Cylinder, BodyGroup.Torso, A, 0, 1.395f, 0, tw * 0.6f, 0.07f, td * 0.95f);
                    o.Add(PartShape.Capsule, BodyGroup.Torso, Shade(A, 0.84f), 0, 1.01f, td * 0.5f + 0.022f, 0.15f, tw * 0.7f, 0.06f, 0, 0, 90);
                    o.Add(PartShape.Box, BodyGroup.Torso, White, 0.04f, 1.29f, td * 0.5f + 0.035f, 0.016f, 0.13f, 0.014f, 0, 0, 5);
                    o.Add(PartShape.Box, BodyGroup.Torso, White, -0.04f, 1.29f, td * 0.5f + 0.035f, 0.016f, 0.13f, 0.014f, 0, 0, -5);
                    break;
            }

            if (a.pack)
            {
                o.Add(PartShape.Box, BodyGroup.Torso, C, 0, 1.12f, -td * 0.5f - 0.11f, 0.36f, 0.42f, 0.2f);
                o.Add(PartShape.Cylinder, BodyGroup.Torso, outfit == 5 ? A : B, 0, 1.37f, -td * 0.5f - 0.12f, 0.14f, 0.42f, 0.14f, 0, 0, 90);
            }

            // ---- head ----
            o.Add(PartShape.Sphere, BodyGroup.Head, skin, 0, 1.6f, 0.02f, 0.3f, 0.32f, 0.3f);
            float eyeH = fem ? 0.07f : 0.06f;
            o.Add(PartShape.Sphere, BodyGroup.Head, eye, 0.07f, 1.61f, 0.15f, 0.05f, eyeH, 0.04f);
            o.Add(PartShape.Sphere, BodyGroup.Head, eye, -0.07f, 1.61f, 0.15f, 0.05f, eyeH, 0.04f);
            // brows: a negative tilt on the +x side drops the outer end, which reads as friendly (the other way looks cross)
            float browW = fem ? 0.055f : 0.065f, browH = fem ? 0.011f : 0.016f;
            o.Add(PartShape.Box, BodyGroup.Head, hair, 0.07f, fem ? 1.672f : 1.665f, 0.155f, browW, browH, 0.02f, 0, 0, -9);
            o.Add(PartShape.Box, BodyGroup.Head, hair, -0.07f, fem ? 1.672f : 1.665f, 0.155f, browW, browH, 0.02f, 0, 0, 9);
            if (fem)
            {
                // a lash flicking up from the outer corner of each eye
                o.Add(PartShape.Box, BodyGroup.Head, Dark, 0.103f, 1.632f, 0.15f, 0.04f, 0.012f, 0.016f, 0, 0, 38);
                o.Add(PartShape.Box, BodyGroup.Head, Dark, -0.103f, 1.632f, 0.15f, 0.04f, 0.012f, 0.016f, 0, 0, -38);
            }
            Hair(ref o, Mathf.Clamp(a.hair, 0, HairNames.Length - 1), hair, skin, C, covered);
            Hat(ref o, hat, A, C);

            // ---- arms and legs ----
            for (int s = -1; s <= 1; s += 2)
            {
                bool r = s > 0;
                var arm = r ? BodyGroup.ArmR : BodyGroup.ArmL; var fore = r ? BodyGroup.ForearmR : BodyGroup.ForearmL;
                var leg = r ? BodyGroup.LegFR : BodyGroup.LegFL; var shin = r ? BodyGroup.ShinR : BodyGroup.ShinL;
                float fx = s * (ax + 0.01f);
                o.Add(PartShape.Capsule, arm, sleeves >= 1 ? sleeve : skin, s * ax, 1.2f, 0, at + bulk, 0.31f, at + bulk);
                o.Add(PartShape.Capsule, fore, sleeves == 2 ? sleeve : skin, fx, 0.985f, 0, ft + bulk, 0.25f, ft + bulk);
                if (sleeves == 2) o.Add(PartShape.Cylinder, fore, outfit == 6 ? Shade(A, 0.78f) : Dark, fx, 0.9f, 0, ft + bulk + 0.015f, outfit == 6 ? 0.06f : 0.04f, ft + bulk + 0.015f);
                else if (outfit == 1 || outfit == 2) o.Add(PartShape.Cylinder, fore, C, fx, 0.93f, 0, ft + 0.02f, 0.08f, ft + 0.02f);   // bracers / wristbands
                o.Add(PartShape.Sphere, fore, skin, fx, 0.85f, 0.01f, hand, hand * 1.08f, hand);

                o.Add(PartShape.Capsule, leg, legCol, s * 0.11f, 0.62f, 0, lt, 0.43f, lt);
                o.Add(PartShape.Capsule, shin, legs == 1 ? legCol : skin, s * 0.11f, 0.27f, 0, st, 0.38f, st);
                if (shoes == 0)
                {
                    o.Add(PartShape.Cylinder, shin, Dark, s * 0.11f, 0.17f, 0, st + 0.015f, 0.16f, st + 0.015f);
                    o.Add(PartShape.Box, shin, Dark, s * 0.11f, 0.06f, 0.04f, st + 0.015f, 0.12f, 0.27f);
                    if (legs == 0) o.Add(PartShape.Cylinder, shin, White, s * 0.11f, 0.27f, 0, st + 0.02f, 0.05f, st + 0.02f);   // sock top
                }
                else
                {
                    o.Add(PartShape.Cylinder, shin, White, s * 0.11f, 0.15f, 0, st + 0.02f, 0.08f, st + 0.02f);
                    o.Add(PartShape.Box, shin, C, s * 0.11f, 0.075f, 0.04f, st + 0.02f, 0.09f, 0.27f);
                    o.Add(PartShape.Box, shin, White, s * 0.11f, 0.02f, 0.04f, st + 0.03f, 0.04f, 0.28f);
                }
            }
            parts = o.parts.ToArray();
            colors = o.cols.ToArray();
        }

        // The head is a ball from y 1.44 to 1.76; the hair cap covers its top and back and leaves the face open.
        static void Hair(ref Out o, int style, Color h, Color skin, Color band, bool covered)
        {
            const BodyGroup G = BodyGroup.Head;
            bool cap = style != 1 && style != 3 && style != 4;
            if (cap) o.Add(PartShape.Sphere, G, h, 0, 1.66f, -0.04f, 0.32f, 0.26f, 0.3f);
            switch (style)
            {
                case 0:   // short, with a swept fringe
                    if (!covered) o.Add(PartShape.Sphere, G, h, 0.05f, 1.725f, 0.09f, 0.2f, 0.09f, 0.12f, 0, 0, -8);
                    break;
                case 1:   // buzz cut
                    o.Add(PartShape.Sphere, G, Color.Lerp(h, skin, 0.2f), 0, 1.662f, -0.035f, 0.318f, 0.25f, 0.302f);
                    break;
                case 2:   // spiky
                    if (!covered)
                    {
                        o.Add(PartShape.Cone, G, h, 0, 1.81f, 0.02f, 0.11f, 0.17f, 0.11f, 18);
                        o.Add(PartShape.Cone, G, h, 0.1f, 1.79f, -0.02f, 0.1f, 0.16f, 0.1f, 0, 0, -28);
                        o.Add(PartShape.Cone, G, h, -0.1f, 1.79f, -0.02f, 0.1f, 0.16f, 0.1f, 0, 0, 28);
                        o.Add(PartShape.Cone, G, h, 0, 1.78f, -0.11f, 0.11f, 0.17f, 0.11f, -32);
                        o.Add(PartShape.Cone, G, h, 0.06f, 1.75f, 0.1f, 0.08f, 0.13f, 0.08f, 52, 0, -10);
                        o.Add(PartShape.Cone, G, h, -0.06f, 1.75f, 0.1f, 0.08f, 0.13f, 0.08f, 52, 0, 10);
                    }
                    break;
                case 3:   // mohawk over shaved sides
                    o.Add(PartShape.Sphere, G, Color.Lerp(h, skin, 0.45f), 0, 1.66f, -0.035f, 0.316f, 0.245f, 0.3f);
                    if (!covered)
                        for (int i = 0; i < 5; i++)
                            o.Add(PartShape.Sphere, G, h, 0, 1.79f - Mathf.Abs(i - 1.5f) * 0.018f, 0.1f - i * 0.065f, 0.075f, 0.19f - i * 0.012f, 0.11f);
                    else o.Add(PartShape.Sphere, G, h, 0, 1.6f, -0.17f, 0.075f, 0.16f, 0.08f);
                    break;
                case 4:   // curly
                    if (!covered)
                    {
                        o.Add(PartShape.Sphere, G, h, 0, 1.7f, -0.06f, 0.42f, 0.34f, 0.38f);
                        o.Add(PartShape.Sphere, G, h, 0.15f, 1.76f, 0.03f, 0.16f, 0.15f, 0.16f);
                        o.Add(PartShape.Sphere, G, h, -0.15f, 1.76f, 0.03f, 0.16f, 0.15f, 0.16f);
                        o.Add(PartShape.Sphere, G, h, 0, 1.8f, 0.07f, 0.18f, 0.14f, 0.16f);
                    }
                    else
                    {
                        o.Add(PartShape.Sphere, G, h, 0, 1.63f, -0.05f, 0.4f, 0.26f, 0.36f);
                    }
                    break;
                case 5:   // bob with a straight fringe
                    o.Add(PartShape.Sphere, G, h, 0, 1.575f, -0.055f, 0.38f, 0.3f, 0.31f);
                    if (!covered) o.Add(PartShape.Box, G, h, 0, 1.712f, 0.115f, 0.25f, 0.06f, 0.07f);
                    break;
                case 6:   // long, down the back
                    o.Add(PartShape.Capsule, G, h, 0, 1.4f, -0.135f, 0.31f, 0.52f, 0.13f);
                    o.Add(PartShape.Capsule, G, h, 0.155f, 1.5f, -0.02f, 0.07f, 0.3f, 0.11f);
                    o.Add(PartShape.Capsule, G, h, -0.155f, 1.5f, -0.02f, 0.07f, 0.3f, 0.11f);
                    if (!covered) o.Add(PartShape.Sphere, G, h, -0.04f, 1.725f, 0.09f, 0.22f, 0.09f, 0.12f, 0, 0, 8);
                    break;
                case 7:   // ponytail
                    o.Add(PartShape.Sphere, G, band, 0, 1.69f, -0.19f, 0.09f, 0.09f, 0.07f);
                    o.Add(PartShape.Capsule, G, h, 0, 1.52f, -0.245f, 0.11f, 0.38f, 0.1f, -14);
                    if (!covered) o.Add(PartShape.Sphere, G, h, 0.05f, 1.725f, 0.09f, 0.2f, 0.09f, 0.12f, 0, 0, -8);
                    break;
                case 8:   // bun
                    if (!covered) o.Add(PartShape.Sphere, G, h, 0, 1.83f, -0.09f, 0.17f, 0.16f, 0.17f);
                    else o.Add(PartShape.Sphere, G, h, 0, 1.6f, -0.2f, 0.16f, 0.15f, 0.14f);
                    break;
                case 9:   // pigtails
                    for (int s = -1; s <= 1; s += 2)
                    {
                        o.Add(PartShape.Sphere, G, band, s * 0.17f, 1.63f, -0.05f, 0.08f, 0.08f, 0.08f);
                        o.Add(PartShape.Capsule, G, h, s * 0.215f, 1.48f, -0.05f, 0.1f, 0.3f, 0.1f, 0, 0, s * 10);
                    }
                    if (!covered) o.Add(PartShape.Box, G, h, 0, 1.712f, 0.115f, 0.25f, 0.06f, 0.07f);
                    break;
            }
        }

        static void Hat(ref Out o, int hat, Color A, Color C)
        {
            const BodyGroup G = BodyGroup.Head;
            switch (hat)
            {
                case 1:   // the ranger hat: brim, crown, a feather in the outfit's colour
                    o.Add(PartShape.Cylinder, G, C, 0, 1.75f, 0.01f, 0.5f, 0.03f, 0.5f);
                    o.Add(PartShape.Cylinder, G, C, 0, 1.82f, 0, 0.28f, 0.14f, 0.28f);
                    o.Add(PartShape.Sphere, G, A, 0.1f, 1.9f, -0.03f, 0.06f, 0.2f, 0.12f, 0, 0, -30);
                    break;
                case 2:   // cap with a peak
                    o.Add(PartShape.Sphere, G, C, 0, 1.73f, -0.015f, 0.345f, 0.2f, 0.335f);
                    o.Add(PartShape.Box, G, C, 0, 1.715f, 0.2f, 0.24f, 0.025f, 0.17f, 10);
                    o.Add(PartShape.Sphere, G, A, 0, 1.83f, -0.015f, 0.05f, 0.03f, 0.05f);
                    break;
                case 3:   // headband with two tails
                    o.Add(PartShape.Cylinder, G, C, 0, 1.685f, 0.005f, 0.335f, 0.05f, 0.325f);
                    o.Add(PartShape.Box, G, C, 0.04f, 1.6f, -0.19f, 0.05f, 0.17f, 0.02f, 12, 0, 14);
                    o.Add(PartShape.Box, G, C, -0.04f, 1.6f, -0.19f, 0.05f, 0.14f, 0.02f, 12, 0, -14);
                    break;
                case 4:   // bow
                    o.Add(PartShape.Sphere, G, C, 0.17f, 1.8f, 0.02f, 0.13f, 0.1f, 0.06f, 0, 0, 25);
                    o.Add(PartShape.Sphere, G, C, 0.06f, 1.83f, 0.02f, 0.13f, 0.1f, 0.06f, 0, 0, -25);
                    o.Add(PartShape.Sphere, G, A, 0.115f, 1.805f, 0.03f, 0.05f, 0.05f, 0.05f);
                    break;
                case 5:   // goggles pushed up on the forehead
                    o.Add(PartShape.Cylinder, G, Dark, 0, 1.7f, 0.005f, 0.335f, 0.04f, 0.325f);
                    o.Add(PartShape.Cylinder, G, C, 0.065f, 1.705f, 0.155f, 0.1f, 0.04f, 0.1f, 80);
                    o.Add(PartShape.Cylinder, G, C, -0.065f, 1.705f, 0.155f, 0.1f, 0.04f, 0.1f, 80);
                    o.Add(PartShape.Cylinder, G, new Color(0.6f, 0.9f, 1f), 0.065f, 1.708f, 0.172f, 0.07f, 0.02f, 0.07f, 80);
                    o.Add(PartShape.Cylinder, G, new Color(0.6f, 0.9f, 1f), -0.065f, 1.708f, 0.172f, 0.07f, 0.02f, 0.07f, 80);
                    break;
            }
        }

        /// <summary>Builds (or rebuilds) the explorer's animated model under parent.</summary>
        public static CreatureRig BuildRig(Appearance a, bool headGear, Transform parent)
        {
            if (a == null) a = new Appearance();
            Build(a, headGear, out var parts, out var cols);
            return ModelBuilder.BuildCustom(a.Key(headGear), parts, PlayerLook.Pivots, PlayerLook.Palette, parent, RenderAssets.Creature, Locomotion.Biped, 1f, cols);
        }
    }
}
