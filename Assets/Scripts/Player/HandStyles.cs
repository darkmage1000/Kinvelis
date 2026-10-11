using UnityEngine;

namespace Wildbond
{
    /// <summary>How something in the hand is used. Auto = work it out from the item (see HandStyles.Of).</summary>
    public enum HandStyle { Auto, Fists, Club, Sword, Spear, Glaive, Dagger, Axe, Pick, Sickle, Sling, Bow, Crossbow }

    /// <summary>What a swing is for: hitting something, an axe biting into a tree, reaching down to pick up / pluck, or throwing a seed.</summary>
    public enum SwingAct { Strike, Chop, Reach, Throw, Shoot }

    /// <summary>A ranged weapon: how far it carries, how fast the shot flies, what flies, and how hard it leans toward its mark.</summary>
    public struct RangedInfo
    {
        public float range, speed, turn;
        public ShotKind shot;
        public RangedInfo(float range, float speed, ShotKind shot, float turn) { this.range = range; this.speed = speed; this.shot = shot; this.turn = turn; }
    }

    public struct StyleInfo
    {
        /// <summary>Seconds per swing, and the point in it (0-1) where the blow lands.</summary>
        public float time, hit;
        /// <summary>Added to melee range; how wide the blow is (minimum facing dot); push on a hit.</summary>
        public float reach, arc, knock;
        /// <summary>Only the nearest foe is hit (jabs, stabs, a hatchet).</summary>
        public bool single;
        /// <summary>The off hand grips the shaft this far along it (0 = one-handed).</summary>
        public float grip;
        public float pitch;
        public StyleInfo(float time, float hit, float reach, float arc, float knock, bool single, float grip, float pitch)
        { this.time = time; this.hit = hit; this.reach = reach; this.arc = arc; this.knock = knock; this.single = single; this.grip = grip; this.pitch = pitch; }
    }

    /// <summary>One moment of a move, as bone rotations for the human skeleton (see PlayerController.ApplySwingPose).</summary>
    public struct HandPose
    {
        public Quaternion armR, foreR, armL, foreL, item, torso;
        public bool useR, useL;
        /// <summary>0-1: how much of this pose to lay over the walk animation (eases in and out at the ends).</summary>
        public float weight;
        /// <summary>The body steps this far forward and the hips sink this far (legs are planted by HumanIK.Stance).</summary>
        public float lunge, drop;
    }

    /// <summary>
    /// A move set for each kind of thing the player holds, so a spear is thrust, a sword slashed, a hatchet chopped
    /// sideways into a trunk, a pick brought down two-handed, a sickle swept low, and bare hands punch or reach down.
    /// A move is three key poses (wind-up, strike, follow-through). Each key says where the hand is - a direction from
    /// the shoulder and how far the arm is stretched (so a thrust really extends and a wind-up really cocks the elbow) -
    /// where the weapon points, and how the torso leans and twists at the waist (torso space: +X right, +Y up, +Z forward).
    /// HumanIK bends the elbow; Solve aims the item in the hand and rolls it so its edge leads; two-handed styles put
    /// the off hand on the shaft; a move's lunge and drop plant the feet. Purely visual plus the few numbers in
    /// StyleInfo: damage per second is unchanged (PlayerController scales a blow by its swing time).
    /// </summary>
    public static class HandStyles
    {
        /// <summary>The old one-size swing time: blows are scaled by time / this, so every style deals the same damage per second.</summary>
        public const float BaseTime = 0.36f;

        public static HandStyle Of(ItemDef d)
        {
            if (d == null) return HandStyle.Fists;
            if (d.handStyle != HandStyle.Auto) return d.handStyle;
            if (d.category == ItemCategory.Tool)
                return d.toolKind == ToolKind.Axe ? HandStyle.Axe : d.toolKind == ToolKind.Pick ? HandStyle.Pick : d.toolKind == ToolKind.Sickle ? HandStyle.Sickle : HandStyle.Club;
            if (d.category != ItemCategory.Weapon) return HandStyle.Fists;
            switch (d.id)
            {
                case "wood_club": return HandStyle.Club;
                case "stone_spear": return HandStyle.Spear;
                case "bogsteel_glaive": return HandStyle.Glaive;
                case "blightsteel_fang": return HandStyle.Dagger;
                default: return HandStyle.Sword;
            }
        }

        public static bool IsRanged(HandStyle s) => s == HandStyle.Sling || s == HandStyle.Bow || s == HandStyle.Crossbow;

        /// <summary>Reach and flight of a ranged style. A sling is quick and short, a bow the all-rounder, a crossbow slow, far and flat.</summary>
        /// <summary>
        /// What a ranged weapon shoots, one per shot (user, 2026-10-10: "they are op with unlimited"): a sling throws the
        /// plain Stones you pick up everywhere, bows need Arrows, crossbows need Bolts. Null for anything else.
        /// </summary>
        public static string Ammo(HandStyle s)
        {
            switch (s)
            {
                case HandStyle.Sling: return "stone";
                case HandStyle.Bow: return "arrow";
                case HandStyle.Crossbow: return "bolt";
                default: return null;
            }
        }

        /// <summary>Crafted ammunition (listed with the weapons in crafting menus).</summary>
        public static bool IsAmmoItem(string id) => id == "arrow" || id == "bolt";

        public static RangedInfo Ranged(HandStyle s)
        {
            switch (s)
            {
                case HandStyle.Sling: return new RangedInfo(20f, 27f, ShotKind.Stone, 70f);
                case HandStyle.Crossbow: return new RangedInfo(30f, 55f, ShotKind.Bolt, 45f);
                default: return new RangedInfo(26f, 42f, ShotKind.Arrow, 60f);
            }
        }

        public static StyleInfo Info(HandStyle s, SwingAct act = SwingAct.Strike)
        {
            if (act == SwingAct.Reach) return new StyleInfo(0.34f, 0.45f, 0f, 0.2f, 0f, true, 0f, 1.2f);
            if (act == SwingAct.Throw) return new StyleInfo(0.36f, 0.3f, 0f, 0.2f, 0f, true, 0f, 1.2f);
            switch (s)
            {
                case HandStyle.Club: return new StyleInfo(0.46f, 0.46f, 0f, 0.2f, 7f, false, 0f, 0.78f);
                case HandStyle.Sword: return new StyleInfo(0.36f, 0.42f, 0.1f, 0.1f, 4f, false, 0f, 1f);
                case HandStyle.Spear: return new StyleInfo(0.38f, 0.45f, 0.9f, 0.7f, 4f, false, 0.42f, 1.15f);
                case HandStyle.Glaive: return new StyleInfo(0.5f, 0.46f, 0.8f, -0.05f, 6f, false, 0.45f, 0.85f);
                case HandStyle.Dagger: return new StyleInfo(0.24f, 0.42f, -0.2f, 0.5f, 2f, true, 0f, 1.4f);
                case HandStyle.Axe: return new StyleInfo(0.36f, 0.44f, 0f, 0.3f, 5f, true, 0f, 0.95f);
                case HandStyle.Pick: return new StyleInfo(0.38f, 0.46f, 0f, 0.45f, 5f, true, 0.24f, 0.85f);
                case HandStyle.Sickle: return new StyleInfo(0.36f, 0.42f, 0.1f, 0f, 3f, false, 0f, 1.1f);
                // ranged: 'hit' is the moment of release. Without a shot to take they are swung like a club.
                case HandStyle.Sling: return act == SwingAct.Shoot ? new StyleInfo(0.56f, 0.5f, 0f, 0.2f, 3f, true, 0f, 1.3f) : new StyleInfo(0.4f, 0.46f, -0.2f, 0.3f, 2f, true, 0f, 1.2f);
                case HandStyle.Bow: return act == SwingAct.Shoot ? new StyleInfo(0.74f, 0.62f, 0f, 0.2f, 4f, true, 0f, 1.5f) : new StyleInfo(0.4f, 0.46f, 0f, 0.3f, 3f, true, 0f, 1.1f);
                case HandStyle.Crossbow: return act == SwingAct.Shoot ? new StyleInfo(1f, 0.3f, 0f, 0.2f, 6f, true, 0.3f, 0.7f) : new StyleInfo(0.46f, 0.46f, 0f, 0.3f, 4f, true, 0f, 0.9f);
                default: return new StyleInfo(0.3f, 0.42f, -0.3f, 0.5f, 3f, true, 0f, 1.25f);   // fists
            }
        }

        /// <summary>A few words for item text: how this weapon is used.</summary>
        public static string Describe(HandStyle s)
        {
            switch (s)
            {
                case HandStyle.Club: return "Slow, heavy overhead blows";
                case HandStyle.Sword: return "Slashes that hit everything in front";
                case HandStyle.Spear: return "Two-handed thrusts, long reach";
                case HandStyle.Glaive: return "Wide two-handed sweeps, long reach";
                case HandStyle.Dagger: return "Very fast stabs, one foe at a time";
                case HandStyle.Sling: return "Ranged 20 m: quick shots, throws 1 Stone each";
                case HandStyle.Bow: return "Ranged 26 m: draw and loose, 1 Arrow a shot";
                case HandStyle.Crossbow: return "Ranged 30 m: slow and heavy, 1 Bolt a shot";
                default: return null;
            }
        }

        /// <summary>How the item sits in the hand when nothing is happening (a spear is carried point-up, not levelled).</summary>
        public static Quaternion Carry(HandStyle s)
        {
            switch (s)
            {
                case HandStyle.Spear: case HandStyle.Glaive: return Quaternion.Euler(-58f, 0f, 0f);
                case HandStyle.Sword: case HandStyle.Dagger: return Quaternion.Euler(-10f, 0f, 0f);
                case HandStyle.Pick: case HandStyle.Axe: case HandStyle.Club: return Quaternion.Euler(-20f, 0f, 0f);
                case HandStyle.Crossbow: return Quaternion.Euler(18f, 0f, 0f);   // muzzle down at the side
                default: return Quaternion.identity;
            }
        }

        // ================================================================ MOVES
        struct Key
        {
            public Vector3 a, w;      // hand direction from the shoulder, weapon direction
            public float ext;         // how far the arm is stretched (0.45 folded - 1 straight)
            public float pitch, yaw;  // torso lean / twist at the waist (degrees)
            public Key(float ax, float ay, float az, float ext, float wx, float wy, float wz, float pitch = 0f, float yaw = 0f)
            { a = new Vector3(ax, ay, az).normalized; w = new Vector3(wx, wy, wz).normalized; this.ext = ext; this.pitch = pitch; this.yaw = yaw; }
        }

        struct Move
        {
            public Key wind, strike, follow;
            public float lunge, drop;
            /// <summary>The left hand leads (a jab with the off hand, picking up while the right hand is full).</summary>
            public bool left;
            /// <summary>Roll the item so its edge leads the swing (off for thrusts and blunt things).</summary>
            public bool edge;
            public Move(Key w, Key s, Key f, float lunge = 0f, bool edge = true, bool left = false, float drop = 0f)
            { wind = w; strike = s; follow = f; this.lunge = lunge; this.edge = edge; this.left = left; this.drop = drop; }
        }

        const float ArmLen = PlayerLook.UpperArm + PlayerLook.Forearm;

        static Move Get(HandStyle style, SwingAct act, int combo, bool handFull)
        {
            if (act == SwingAct.Reach)
                // crouch, fold at the waist and take it with the free hand
                return new Move(new Key(0.05f, -0.9f, 0.4f, 0.8f, 0f, 0.2f, 1f, 22f), new Key(0f, -0.82f, 0.55f, 1f, 0f, -0.2f, 1f, 58f), new Key(0.05f, -0.85f, 0.5f, 0.85f, 0f, 0.2f, 1f, 26f),
                                0f, false, handFull, 0.36f);
            if (act == SwingAct.Throw)
                // cocked by the ear, then whipped over the top
                return new Move(new Key(0.35f, 0.55f, -0.6f, 0.6f, 0f, 1f, 0f, -8f, 26f), new Key(0f, 0.55f, 0.85f, 1f, 0f, 0.3f, 1f, 12f, -14f), new Key(-0.25f, -0.45f, 0.8f, 0.9f, 0f, -0.3f, 1f, 18f, -26f), 0.15f, false);
            if (act == SwingAct.Shoot)
            {
                switch (style)
                {
                    case HandStyle.Sling:   // whirled once overhead, let go at the top of the swing
                        return new Move(new Key(0.35f, 0.8f, -0.4f, 0.7f, 0.3f, 0.6f, -0.75f, -6f, 24f), new Key(0.08f, 0.5f, 0.86f, 1f, 0f, 0.35f, 1f, 8f, -12f), new Key(-0.2f, -0.35f, 0.8f, 0.9f, 0f, -0.5f, 1f, 14f, -24f), 0.12f, false);
                    case HandStyle.Crossbow:   // brought up to the shoulder in both hands, fired early, then wound again while it comes down
                        return new Move(new Key(0.18f, -0.55f, 0.6f, 0.75f, 0f, -0.3f, 1f, 0f, 8f), new Key(0.1f, -0.16f, 0.95f, 0.62f, 0f, 0.02f, 1f, 2f, 10f), new Key(0.12f, -0.32f, 0.85f, 0.55f, 0f, 0.3f, 1f, -5f, 10f), 0.05f, false);
                    default:
                        // bow: the body turns side-on (so the arm direction is written 22 degrees right of forward to stay on the
                        // target), the bow arm goes out straight with the bow upright, and Pose draws the string hand to the cheek
                        return new Move(new Key(0.42f, -0.5f, 0.7f, 0.85f, -0.05f, 0.9f, 0.4f, 0f, -8f), new Key(0.37f, 0f, 0.93f, 1f, -0.06f, 1f, 0f, 0f, -22f), new Key(0.37f, -0.04f, 0.93f, 1f, -0.06f, 1f, 0.06f, 0f, -20f), 0.06f, false);
                }
            }
            if (IsRanged(style)) style = HandStyle.Club;   // no shot to take: it is only a stick
            switch (style)
            {
                case HandStyle.Club:
                    if (combo == 1)   // down across the body from the left shoulder
                        return new Move(new Key(-0.3f, 0.75f, -0.2f, 0.7f, -0.6f, 0.5f, -0.6f, -4f, -28f), new Key(0.1f, 0.1f, 1f, 1f, -0.3f, 0.7f, 0.65f, 12f, 6f), new Key(0.55f, -0.6f, 0.5f, 0.9f, 0.5f, -0.1f, 0.85f, 16f, 28f), 0.12f, false);
                    if (combo == 2)   // leaping slam
                        return new Move(new Key(0.1f, 0.9f, -0.4f, 0.75f, 0f, 0.3f, -0.95f, -18f), new Key(0.05f, -0.05f, 1f, 1f, 0f, 0.55f, 0.85f, 26f), new Key(0.05f, -0.75f, 0.65f, 0.9f, 0f, -0.1f, 1f, 32f), 0.3f, false, false, 0.12f);
                    return new Move(new Key(0.2f, 0.75f, -0.55f, 0.7f, 0f, 0.5f, -0.87f, -12f), new Key(0.05f, 0.15f, 1f, 1f, 0f, 0.75f, 0.65f, 16f), new Key(0.05f, -0.65f, 0.75f, 0.9f, 0f, 0.1f, 1f, 24f), 0.15f, false);

                case HandStyle.Sword:
                    if (combo == 1)   // backhand, left to right
                        return new Move(new Key(-0.5f, 0.15f, 0.7f, 0.7f, -1f, 0.2f, 0.1f, 0f, -30f), new Key(0.2f, 0f, 1f, 1f, 0.3f, 0.15f, 1f, 5f, 6f), new Key(0.9f, -0.1f, 0.15f, 0.85f, 0.95f, 0.1f, -0.35f, 5f, 32f), 0.15f);
                    if (combo == 2)   // step in and cut down
                        return new Move(new Key(0.25f, 0.85f, -0.4f, 0.7f, 0.1f, 0.5f, -0.9f, -10f), new Key(0f, 0.1f, 1f, 1f, 0f, 0.55f, 0.85f, 16f), new Key(-0.25f, -0.7f, 0.6f, 0.85f, -0.2f, -0.35f, 0.9f, 24f), 0.3f, true, false, 0.1f);
                    return new Move(new Key(0.85f, 0.2f, -0.2f, 0.75f, 0.9f, 0.45f, -0.5f, 0f, 34f), new Key(0.2f, 0f, 1f, 1f, -0.25f, 0.2f, 1f, 5f, -6f), new Key(-0.6f, -0.15f, 0.7f, 0.8f, -1f, 0f, 0.25f, 5f, -34f), 0.15f);

                case HandStyle.Spear:
                    if (combo == 1)   // high thrust
                        return new Move(new Key(0.4f, -0.1f, -0.8f, 0.6f, 0f, 0.14f, 1f, 0f, 30f), new Key(0.12f, 0.16f, 1f, 1f, 0f, 0.1f, 1f, 10f, 12f), new Key(0.2f, -0.25f, 0.7f, 0.8f, 0f, 0.1f, 1f, 4f, 20f), 0.3f, false);
                    if (combo == 2)   // lunge
                        return new Move(new Key(0.35f, -0.4f, -0.8f, 0.6f, 0f, 0.05f, 1f, -4f, 34f), new Key(0.08f, -0.05f, 1f, 1f, 0f, 0f, 1f, 20f, 8f), new Key(0.2f, -0.4f, 0.65f, 0.8f, 0f, 0.04f, 1f, 6f, 20f), 0.55f, false, false, 0.2f);
                    return new Move(new Key(0.35f, -0.5f, -0.75f, 0.65f, 0f, 0.03f, 1f, 0f, 30f), new Key(0.1f, -0.1f, 1f, 1f, 0f, -0.02f, 1f, 12f, 12f), new Key(0.2f, -0.35f, 0.7f, 0.8f, 0f, 0.02f, 1f, 5f, 20f), 0.3f, false);

                case HandStyle.Glaive:
                    if (combo == 1)   // back the other way
                        return new Move(new Key(-0.45f, -0.25f, 0.75f, 0.8f, -0.95f, 0.1f, 0.3f, 4f, -42f), new Key(0.25f, -0.2f, 1f, 1f, 0.1f, 0.05f, 1f, 8f, 0f), new Key(0.8f, -0.25f, 0.2f, 0.85f, 0.95f, 0f, 0.3f, 6f, 48f), 0.15f);
                    if (combo == 2)   // overhead cleave
                        return new Move(new Key(0.15f, 0.8f, -0.45f, 0.75f, 0f, 0.7f, -0.7f, -14f), new Key(0.05f, 0f, 1f, 1f, 0f, 0.45f, 0.9f, 18f), new Key(0.05f, -0.6f, 0.75f, 0.9f, 0f, -0.15f, 1f, 28f), 0.3f, true, false, 0.12f);
                    return new Move(new Key(0.75f, -0.25f, 0.1f, 0.8f, 0.95f, 0.15f, 0.3f, 4f, 48f), new Key(0.25f, -0.2f, 1f, 1f, -0.1f, 0.05f, 1f, 8f, 0f), new Key(-0.5f, -0.3f, 0.7f, 0.85f, -0.95f, 0f, 0.35f, 6f, -42f), 0.15f);

                case HandStyle.Dagger:
                    if (combo == 1)   // short backhand cut
                        return new Move(new Key(-0.45f, 0.1f, 0.65f, 0.6f, -0.9f, 0.3f, 0.3f, 0f, -22f), new Key(0.2f, 0.05f, 1f, 0.95f, 0.35f, 0.3f, 0.9f, 4f, 6f), new Key(0.8f, -0.1f, 0.3f, 0.8f, 0.9f, 0.2f, -0.2f, 4f, 24f), 0.12f);
                    if (combo == 2)   // lunging stab
                        return new Move(new Key(0.4f, -0.2f, -0.55f, 0.5f, 0f, 0.25f, 1f, 0f, 16f), new Key(0f, 0.1f, 1f, 1f, 0f, 0.3f, 1f, 16f, -14f), new Key(0.15f, -0.3f, 0.7f, 0.7f, 0f, 0.25f, 1f, 6f, -5f), 0.4f, false, false, 0.12f);
                    return new Move(new Key(0.35f, -0.3f, -0.5f, 0.55f, 0f, 0.2f, 1f, 0f, 12f), new Key(0f, 0.05f, 1f, 1f, 0f, 0.3f, 1f, 6f, -14f), new Key(0.15f, -0.3f, 0.7f, 0.7f, 0f, 0.25f, 1f, 3f, -5f), 0.15f, false);

                case HandStyle.Axe:
                    if (act == SwingAct.Chop)   // side-on into the trunk: the hand sweeps across, the haft points at the tree, and it stops where it bites
                        return new Move(new Key(0.92f, 0.2f, 0f, 0.75f, 0.75f, 0.55f, -0.35f, 0f, 40f), new Key(0.4f, -0.1f, 0.9f, 0.95f, 0.05f, 0.15f, 1f, 6f, -6f), new Key(0.26f, -0.12f, 0.96f, 0.9f, -0.1f, 0.12f, 1f, 6f, -10f), 0.1f);
                    if (combo == 1)   // backhand
                        return new Move(new Key(-0.4f, 0.6f, 0.3f, 0.65f, -0.7f, 0.6f, -0.3f, -4f, -22f), new Key(0.15f, 0.1f, 1f, 1f, 0.05f, 0.85f, 0.5f, 10f, 0f), new Key(0.6f, -0.55f, 0.45f, 0.85f, 0.4f, 0.1f, 0.9f, 14f, 24f), 0.12f);
                    if (combo == 2)
                        return new Move(new Key(0.15f, 0.85f, -0.45f, 0.7f, 0f, 0.45f, -0.9f, -12f), new Key(0.05f, 0.05f, 1f, 1f, 0f, 0.8f, 0.6f, 18f), new Key(0.05f, -0.7f, 0.7f, 0.9f, 0f, 0.1f, 1f, 26f), 0.25f, true, false, 0.1f);
                    return new Move(new Key(0.4f, 0.8f, -0.35f, 0.7f, 0.3f, 0.4f, -0.85f, -8f, 16f), new Key(0.05f, 0.1f, 1f, 1f, -0.05f, 0.85f, 0.5f, 12f, -6f), new Key(-0.3f, -0.65f, 0.6f, 0.85f, -0.2f, 0.15f, 1f, 18f, -20f), 0.12f);

                case HandStyle.Pick:   // both hands, up over the head and straight down
                    return new Move(new Key(0.1f, 0.9f, -0.35f, 0.8f, 0f, 0.4f, -0.9f, -14f), new Key(0.05f, -0.15f, 1f, 1f, 0f, 0.85f, 0.5f, 28f), new Key(0.05f, -0.5f, 0.85f, 0.9f, 0f, 0.6f, 0.8f, 32f), combo == 2 ? 0.15f : 0.06f, true, false, 0.1f);

                case HandStyle.Sickle:   // down low, a flat sweep across the stems
                    if (combo == 1)
                        return new Move(new Key(-0.5f, -0.6f, 0.5f, 0.9f, -0.9f, -0.2f, 0.4f, 34f, -24f), new Key(0.2f, -0.62f, 0.8f, 1f, 0.2f, -0.25f, 1f, 40f, 0f), new Key(0.8f, -0.5f, 0.15f, 0.9f, 0.95f, -0.15f, 0.2f, 34f, 26f), 0f, true, false, 0.26f);
                    return new Move(new Key(0.8f, -0.5f, 0.1f, 0.9f, 0.95f, -0.15f, 0.2f, 34f, 26f), new Key(0.2f, -0.62f, 0.8f, 1f, -0.2f, -0.25f, 1f, 40f, 0f), new Key(-0.5f, -0.6f, 0.5f, 0.9f, -0.9f, -0.2f, 0.4f, 34f, -24f), 0f, true, false, 0.26f);

                default:   // fists: right jab, left jab, right hook (keys are written for the right arm; 'left' mirrors them)
                    if (combo == 1)
                        return new Move(new Key(0.3f, -0.3f, -0.3f, 0.45f, 0f, 1f, 0f, 0f, -14f), new Key(-0.05f, 0.08f, 1f, 1f, 0f, 1f, 0f, 5f, 16f), new Key(0.05f, -0.05f, 1f, 0.7f, 0f, 1f, 0f, 2f, 6f), 0.12f, false, true);
                    if (combo == 2)
                        return new Move(new Key(0.5f, -0.7f, -0.1f, 0.5f, 0f, 1f, 0f, -4f, 24f), new Key(-0.15f, 0.45f, 0.9f, 0.75f, 0f, 1f, 0.3f, 8f, -22f), new Key(-0.3f, 0.75f, 0.5f, 0.7f, 0f, 1f, 0.5f, 2f, -28f), 0.2f, false);
                    return new Move(new Key(0.3f, -0.3f, -0.3f, 0.45f, 0f, 1f, 0f, 0f, 14f), new Key(-0.05f, 0.08f, 1f, 1f, 0f, 1f, 0f, 5f, -16f), new Key(0.05f, -0.05f, 1f, 0.7f, 0f, 1f, 0f, 2f, -6f), 0.12f, false);
            }
        }

        // ================================================================ POSE
        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        static float EaseIn(float t) => t * t;

        static Key Mix(Key a, Key b, float t)
        {
            Key k;
            k.a = Vector3.Slerp(a.a, b.a, t).normalized;
            k.w = Vector3.Slerp(a.w, b.w, t).normalized;
            k.ext = Mathf.Lerp(a.ext, b.ext, t);
            k.pitch = Mathf.Lerp(a.pitch, b.pitch, t);
            k.yaw = Mathf.Lerp(a.yaw, b.yaw, t);
            return k;
        }

        /// <summary>Rotation of the bone that holds the item (pointing along 'bone') and of the item in the hand, so the item points along 'w' with its edge (-Y) turned toward 'edge'.</summary>
        static void Solve(Vector3 bone, Vector3 w, Vector3 edge, float edgeAmount, out Quaternion rot, out Quaternion item)
        {
            float s = Mathf.Clamp(Vector3.Dot(w, bone), -1f, 1f);
            Vector3 wo = HumanIK.Orth(w, bone, Vector3.up);
            rot = Quaternion.LookRotation(wo, -bone);
            item = Quaternion.Euler(Mathf.Asin(s) * Mathf.Rad2Deg, 0f, 0f);
            if (edgeAmount <= 0.001f) return;
            // where the item's up (+Y, the back of the blade) is now, and where it should be: opposite the swing
            Vector3 up = rot * (item * Vector3.up);
            Vector3 want = -(edge - w * Vector3.Dot(edge, w));
            if (want.sqrMagnitude < 1e-4f) return;
            float roll = Vector3.SignedAngle(up, want.normalized, w);
            item = item * Quaternion.AngleAxis(roll * edgeAmount, Vector3.forward);
        }

        /// <summary>
        /// The pose at k (0-1) through a swing: into the wind-up by 14%, the blow at the style's hit point,
        /// follow-through to 78%, then eased back out. handFull = the right hand holds something (so the left picks up).
        /// </summary>
        public static void Pose(HandStyle style, SwingAct act, int combo, float k, bool handFull, out HandPose p)
        {
            var info = Info(style, act);
            var m = Get(style, act, combo, handFull);
            const float w0 = 0.14f, rec = 0.78f;
            float h = Mathf.Max(info.hit, w0 + 0.05f);
            Key key; float back = 0f;
            if (k < w0) key = m.wind;
            else if (k < h) key = Mix(m.wind, m.strike, EaseIn((k - w0) / (h - w0)));
            else if (k < rec) key = Mix(m.strike, m.follow, EaseOut((k - h) / (rec - h)));
            else { back = (k - rec) / (1f - rec); key = m.follow; }
            float into = k < w0 ? EaseOut(k / w0) : 1f - back * back * (3f - 2f * back);

            p = new HandPose { weight = into, torso = Quaternion.Euler(key.pitch, key.yaw, 0f) };
            float bell = Mathf.Sin(Mathf.Clamp01(k < h ? k / h * 0.5f : 0.5f + (k - h) / (1f - h) * 0.5f) * Mathf.PI);   // 0 -> 1 at the blow -> 0
            p.lunge = m.lunge * bell;
            p.drop = m.drop * bell;

            Vector3 hand = key.a * (Mathf.Clamp(key.ext, 0.4f, 1f) * ArmLen);
            if (m.left)
            {
                // mirrored onto the left arm; the right keeps whatever it was doing
                HumanIK.Arm(new Vector3(-hand.x, hand.y, hand.z), false, out p.armL, out p.foreL);
                p.useL = true;
                p.item = Carry(style);
                return;
            }
            // elbow, then the forearm turned so the item points where the move wants it
            HumanIK.Bend(hand, PlayerLook.UpperArm, PlayerLook.Forearm, new Vector3(0.45f, -0.7f, -0.55f), out var upper, out var fore);
            Vector3 sweep = m.follow.a - m.wind.a;
            bool level = act == SwingAct.Shoot && style == HandStyle.Crossbow;   // a crossbow is held flat: its top stays up whatever the forearm does
            Solve(fore, key.w, level || sweep.sqrMagnitude <= 0.01f ? Vector3.down : sweep.normalized, m.edge || level ? 1f : 0f, out var foreRot, out p.item);
            p.armR = HumanIK.Aim(upper, fore);
            p.foreR = Quaternion.Inverse(p.armR) * foreRot;
            p.item = Quaternion.Slerp(Carry(style), p.item, into);
            p.useR = true;
            if (act == SwingAct.Shoot && style == HandStyle.Bow)
            {
                // the string hand: on the bow as it comes up, drawn back to the cheek by the release, then let go
                Vector3 palm = PlayerLook.ShoulderR + upper * PlayerLook.UpperArm + fore * PlayerLook.Forearm;
                Vector3 cheek = PlayerLook.Neck + new Vector3(-0.02f, 0.07f, 0.1f);
                float draw = k < w0 ? 0.12f : k < h ? Mathf.Lerp(0.12f, 1f, EaseOut((k - w0) / (h - w0))) : Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((k - h) / 0.12f));
                HumanIK.Arm(Vector3.Lerp(palm + new Vector3(-0.04f, 0f, -0.04f), cheek, draw) - PlayerLook.ShoulderL, false, out p.armL, out p.foreL);
                p.useL = true;
            }
            else if (info.grip > 0f && (act == SwingAct.Strike || act == SwingAct.Shoot))
            {
                // off hand on the shaft, a little way along it from the right hand
                Vector3 palm = PlayerLook.ShoulderR + upper * PlayerLook.UpperArm + fore * PlayerLook.Forearm;
                HumanIK.Arm(palm + key.w * info.grip - PlayerLook.ShoulderL, false, out p.armL, out p.foreL);
                p.useL = true;
            }
        }
    }
}
