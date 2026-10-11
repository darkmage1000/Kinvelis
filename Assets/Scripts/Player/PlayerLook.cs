using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The human body plan: the explorer, and (with other palettes) every villager, tamer, trader and Maro.
    /// Original design: leaf-crested ranger hat and a satchel. It is a real skeleton - hips, a torso that turns at the
    /// waist, a head, two-part arms (elbows) and two-part legs (knees). ModelBuilder.BuildCustom nests the groups
    /// (ModelBuilder.ParentOf), CreatureRig.AnimateHuman walks it, HumanIK reaches hands and plants feet.
    /// All positions are model space with the feet at y = 0.
    /// </summary>
    public static class PlayerLook
    {
        public static readonly Palette Palette = new Palette
        {
            primary = new Color(0.2f, 0.6f, 0.62f),     // jacket
            secondary = new Color(0.32f, 0.26f, 0.22f), // trousers / hair
            accent = new Color(0.86f, 0.62f, 0.32f),    // hat + satchel
            eye = new Color(0.1f, 0.08f, 0.1f),
            glow = new Color(0.98f, 0.8f, 0.66f)        // skin
        };

        // ---- joints ----
        public static readonly Vector3 Waist = new Vector3(0f, 0.88f, 0f), Neck = new Vector3(0f, 1.42f, 0f);
        public static readonly Vector3 ShoulderR = new Vector3(0.27f, 1.33f, 0f), ElbowR = new Vector3(0.29f, 1.08f, 0f);
        public static readonly Vector3 HipR = new Vector3(0.11f, 0.80f, 0f), KneeR = new Vector3(0.11f, 0.43f, 0f);
        /// <summary>Bone lengths: shoulder-elbow, elbow-palm, hip-knee, knee-sole.</summary>
        public const float UpperArm = 0.25f, Forearm = 0.23f, Thigh = 0.37f, Shin = 0.43f;
        /// <summary>Where something held sits, relative to the elbow (the palm).</summary>
        public static readonly Vector3 Grip = new Vector3(0f, -0.23f, 0.04f);

        static Vector3 L(Vector3 v) => new Vector3(-v.x, v.y, v.z);
        public static readonly Vector3 ShoulderL = L(ShoulderR), ElbowL = L(ElbowR), HipL = L(HipR), KneeL = L(KneeR);

        static BodyPart P(PartShape s, BodyGroup g, ColorSlot c, float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            ContentSeed.P(s, g, c, px, py, pz, sx, sy, sz, rx, ry, rz);

        static BodyPart[] Make(bool hat)
        {
            var l = new List<BodyPart>
            {
                // hips
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, 0, 0.82f, 0, 0.43f, 0.28f, 0.3f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Dark, 0, 0.9f, 0, 0.44f, 0.07f, 0.31f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 0.9f, 0.155f, 0.08f, 0.07f, 0.03f),
                // torso: jacket, satchel and bedroll on the back, neck, shoulders
                P(PartShape.Capsule, BodyGroup.Torso, ColorSlot.Primary, 0, 1.14f, 0, 0.44f, 0.54f, 0.3f),
                P(PartShape.Box, BodyGroup.Torso, ColorSlot.Accent, 0, 1.12f, -0.25f, 0.36f, 0.42f, 0.2f),
                P(PartShape.Cylinder, BodyGroup.Torso, ColorSlot.Secondary, 0, 1.37f, -0.27f, 0.14f, 0.42f, 0.14f, 0, 0, 90),
                P(PartShape.Cylinder, BodyGroup.Torso, ColorSlot.Glow, 0, 1.43f, 0, 0.14f, 0.1f, 0.14f),
                P(PartShape.Sphere, BodyGroup.Torso, ColorSlot.Primary, 0.25f, 1.33f, 0, 0.17f, 0.16f, 0.18f),
                P(PartShape.Sphere, BodyGroup.Torso, ColorSlot.Primary, -0.25f, 1.33f, 0, 0.17f, 0.16f, 0.18f),
                // head
                P(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, 0, 1.6f, 0.02f, 0.3f, 0.32f, 0.3f),
                P(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, 0, 1.66f, -0.04f, 0.32f, 0.26f, 0.3f),
                P(PartShape.Sphere, BodyGroup.Head, ColorSlot.Eye, 0.07f, 1.61f, 0.15f, 0.05f, 0.06f, 0.04f),
                P(PartShape.Sphere, BodyGroup.Head, ColorSlot.Eye, -0.07f, 1.61f, 0.15f, 0.05f, 0.06f, 0.04f),
            };
            if (hat)
            {
                l.Add(P(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, 0, 1.75f, 0.01f, 0.5f, 0.03f, 0.5f));
                l.Add(P(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, 0, 1.82f, 0.0f, 0.28f, 0.14f, 0.28f));
                l.Add(P(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, 0.1f, 1.9f, -0.03f, 0.06f, 0.2f, 0.12f, 0, 0, -30));
            }
            for (int s = -1; s <= 1; s += 2)
            {
                bool r = s > 0;
                var arm = r ? BodyGroup.ArmR : BodyGroup.ArmL; var fore = r ? BodyGroup.ForearmR : BodyGroup.ForearmL;
                var leg = r ? BodyGroup.LegFR : BodyGroup.LegFL; var shin = r ? BodyGroup.ShinR : BodyGroup.ShinL;
                // upper arm; forearm with a cuff and a hand
                l.Add(P(PartShape.Capsule, arm, ColorSlot.Primary, s * 0.28f, 1.2f, 0, 0.14f, 0.31f, 0.14f));
                l.Add(P(PartShape.Capsule, fore, ColorSlot.Primary, s * 0.29f, 0.985f, 0, 0.125f, 0.25f, 0.125f));
                l.Add(P(PartShape.Cylinder, fore, ColorSlot.Dark, s * 0.29f, 0.9f, 0, 0.14f, 0.04f, 0.14f));
                l.Add(P(PartShape.Sphere, fore, ColorSlot.Glow, s * 0.29f, 0.85f, 0.01f, 0.12f, 0.13f, 0.12f));
                // thigh; shin with a boot
                l.Add(P(PartShape.Capsule, leg, ColorSlot.Secondary, s * 0.11f, 0.62f, 0, 0.18f, 0.43f, 0.18f));
                l.Add(P(PartShape.Capsule, shin, ColorSlot.Secondary, s * 0.11f, 0.27f, 0, 0.155f, 0.38f, 0.155f));
                l.Add(P(PartShape.Cylinder, shin, ColorSlot.Dark, s * 0.11f, 0.17f, 0, 0.17f, 0.16f, 0.17f));
                l.Add(P(PartShape.Box, shin, ColorSlot.Dark, s * 0.11f, 0.06f, 0.04f, 0.17f, 0.12f, 0.27f));
            }
            return l.ToArray();
        }

        public static readonly BodyPart[] Parts = Make(true);
        /// <summary>Body without the ranger hat (brim, crown, feather) - used while head gear is worn.</summary>
        public static readonly BodyPart[] PartsNoHat = Make(false);

        public static readonly GroupPivot[] Pivots =
        {
            new GroupPivot(BodyGroup.Torso, Waist),
            new GroupPivot(BodyGroup.Head, Neck),
            new GroupPivot(BodyGroup.ArmR, ShoulderR), new GroupPivot(BodyGroup.ArmL, ShoulderL),
            new GroupPivot(BodyGroup.ForearmR, ElbowR), new GroupPivot(BodyGroup.ForearmL, ElbowL),
            new GroupPivot(BodyGroup.LegFR, HipR), new GroupPivot(BodyGroup.LegFL, HipL),
            new GroupPivot(BodyGroup.ShinR, KneeR), new GroupPivot(BodyGroup.ShinL, KneeL),
        };
    }

    /// <summary>
    /// Two-bone reach for the human skeleton: put a hand at a point, or a foot on the ground, and get the rotations of
    /// the upper and lower bone. Directions are in the parent's space (the torso for arms, the model for legs);
    /// a limb at rest hangs straight down (-Y) and bends forward at the elbow, backward at the knee.
    /// </summary>
    public static class HumanIK
    {
        /// <summary>Part of v at right angles to dir (normalised), or the fallback when they are parallel.</summary>
        public static Vector3 Orth(Vector3 v, Vector3 dir, Vector3 fallback)
        {
            Vector3 o = v - dir * Vector3.Dot(v, dir);
            if (o.sqrMagnitude > 1e-5f) return o.normalized;
            o = fallback - dir * Vector3.Dot(fallback, dir);
            return o.sqrMagnitude > 1e-5f ? o.normalized : Vector3.Cross(dir, Vector3.right).normalized;
        }

        /// <summary>
        /// Directions of the two bones that reach 'to' (a vector from the joint's root) with lengths a then b, the
        /// middle joint pushed toward 'pole'. Too far = fully stretched; too near = folded as far as the bones allow.
        /// </summary>
        public static void Bend(Vector3 to, float a, float b, Vector3 pole, out Vector3 upper, out Vector3 lower)
        {
            float d = Mathf.Clamp(to.magnitude, Mathf.Abs(a - b) + 0.02f, a + b - 0.002f);
            Vector3 dir = to.sqrMagnitude > 1e-6f ? to.normalized : Vector3.down;
            float cos = Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f);
            float sin = Mathf.Sqrt(1f - cos * cos);
            upper = (dir * cos + Orth(pole, dir, Vector3.back) * sin).normalized;
            lower = (dir * d - upper * a).normalized;
        }

        /// <summary>Rotation that points a hanging bone along dir, keeping its front toward 'front'.</summary>
        public static Quaternion Aim(Vector3 dir, Vector3 front) => Quaternion.LookRotation(Orth(front, dir, Vector3.up), -dir);

        /// <summary>Local rotations for a leg whose foot should be at 'foot' (from the hip, model space). Knees bend forward.</summary>
        public static void Leg(Vector3 foot, out Quaternion thigh, out Quaternion shin)
        {
            Bend(foot, PlayerLook.Thigh, PlayerLook.Shin, new Vector3(0f, 0.2f, 1f), out var up, out var low);
            thigh = Aim(up, Vector3.forward);
            shin = Quaternion.Inverse(thigh) * Aim(low, Vector3.forward);
        }

        /// <summary>Local rotations for an arm whose palm should be at 'hand' (from the shoulder, torso space). right = which arm (the elbow drops out and back).</summary>
        public static void Arm(Vector3 hand, bool right, out Quaternion upper, out Quaternion fore)
        {
            Bend(hand, PlayerLook.UpperArm, PlayerLook.Forearm, new Vector3(right ? 0.45f : -0.45f, -0.7f, -0.55f), out var up, out var low);
            upper = Aim(up, low);
            fore = Quaternion.Inverse(upper) * Aim(low, Vector3.forward + Vector3.up * 0.3f);
        }

        /// <summary>Eases a bone toward a pose (w = 0 leaves the walk animation alone, 1 = the pose).</summary>
        public static void Blend(CreatureRig rig, BodyGroup g, Quaternion pose, float w)
        {
            var tr = rig.GetGroup(g);
            if (tr != null) tr.localRotation = w >= 0.999f ? pose : Quaternion.Slerp(tr.localRotation, pose, w);
        }

        /// <summary>
        /// Plants both feet for a lowered, stepped-in stance: the hips drop by 'drop' and the body has moved 'lunge'
        /// forward, so the right foot stays behind where it was and the left steps out in front.
        /// </summary>
        public static void Stance(CreatureRig rig, float drop, float lunge, float w)
        {
            if (w <= 0.001f) return;
            float back = -lunge - 0.05f, front = 0.1f + Mathf.Min(0.22f, lunge * 0.5f);
            Leg(new Vector3(0.03f, drop - PlayerLook.HipR.y, back), out var tR, out var sR);
            Leg(new Vector3(-0.03f, drop - PlayerLook.HipR.y, front), out var tL, out var sL);
            Blend(rig, BodyGroup.LegFR, tR, w); Blend(rig, BodyGroup.ShinR, sR, w);
            Blend(rig, BodyGroup.LegFL, tL, w); Blend(rig, BodyGroup.ShinL, sL, w);
        }
    }
}
