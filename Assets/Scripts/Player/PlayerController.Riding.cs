using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Mounts: ride your active companion if its species is mountable and you own a Kin Harness.
    /// The player keeps driving movement (same collision/ground code); the mount just mirrors the pose
    /// and animates its gait. Rider sits on the species' seat point.
    /// </summary>
    public partial class PlayerController
    {
        CreatureActor mount;
        public bool IsMounted => mount != null;
        public CreatureActor Mount => mount;

        // riders sit on the species' saddle point; kin that are only surfed carry you on their back
        Vector3 Seat => mount.Sp.mountable && mount.Sp.mountSeat != Vector3.zero ? mount.Sp.mountSeat : new Vector3(0f, DangerSkull.ModelTop(mount.Sp) * 0.82f, 0f);   // zero = worked out from the model (newer mounts)

        // ---- flying mounts (user playtest 2026-10-15): every Flyer you ride flies. Hold JUMP to climb (stamina),
        // let go to glide down slowly; a landing never hurts. Up to FlyCeiling above the ground.
        public const float FlyClimb = 6.5f, FlySink = 2.6f, FlyDrain = 7f, FlyCeiling = 55f;
        /// <summary>You are riding a kin that flies.</summary>
        public bool FlyingMount => IsMounted && mount.Sp.locomotion == Locomotion.Flyer && (mount.Sp.mountable || RideAny);

        /// <summary>Vertical speed for a flying mount this frame (called instead of plain gravity while airborne on one).</summary>
        void FlyStep(float dt, float groundY, float staMul)
        {
            var q = Stats;
            bool climb = InputHub.Held(Btn.Jump) && q.stamina > 1f && transform.position.y < groundY + FlyCeiling;
            if (climb)
            {
                vVel = Mathf.MoveTowards(vVel, FlyClimb, dt * 22f);
                q.stamina -= FlyDrain * staMul * dt; staminaDelay = 0.6f;
            }
            else vVel = Mathf.Max(vVel - gravity * dt * 0.35f, -FlySink);   // a long glide down
            if (transform.position.y >= groundY + FlyCeiling) vVel = Mathf.Min(vVel, 0f);
        }
        // flying mounts hover above the ground: the saddle rides up with them
        float SeatY => Seat.y * mount.Sp.size * mount.Data.SizeScale + mount.Rig.HoverHeight;
        float SeatZ => Seat.z * mount.Sp.size * mount.Data.SizeScale;
        /// <summary>How much higher the rider sits than someone standing (seeds are thrown from there).</summary>
        public float RiderLift => IsMounted ? Mathf.Max(0f, SeatY - 0.8f) : 0f;
        /// <summary>Reach for gathering: a big mount keeps you further from the tree.</summary>
        float GatherReach => gatherRange + (IsMounted ? mount.BodyRadius : 0f);

        /// <summary>
        /// Riding a kin that can log, quarry or forage (user playtest 2026-10-15): your swing at a node is its blow too -
        /// its job level on top of your tool (at least what the job could manage alone), and it works nodes your own
        /// tool is too weak for if its level is up to them (`GatherLevel` + 1 >= hardness, as for gather assist).
        /// False = the mount can't help with this one: the rider gathers alone, as usual.
        /// </summary>
        bool MountGather(ResourceNode n)
        {
            var info = n.Info;
            int lvl = mount.GatherLevel(info);
            if (lvl <= 0) return false;
            if (info.pickup) { PickUp(n); return true; }
            if (!info.Plant && lvl + 1 < info.hardness) return false;
            float dmg = info.Plant ? 2f + lvl : 1f + lvl;
            if (!info.Plant && info.tool != ToolKind.None)
            {
                int tool = Stats.BestToolPower(info.tool, out _);
                if (tool >= info.hardness) dmg += tool;
            }
            HitNode(n, dmg, 0.15f * lvl);
            mount.Rig.Lunge();
            Fx.Debris(n.pos + Vector3.up * 0.8f, ElementInfo.Color(mount.Data.Element1), 4);
            return true;
        }

        /// <summary>Any Tide kin, and any kin that lives by the water, can carry you across open water - no harness needed.</summary>
        public static bool CanSurf(CreatureActor c) =>
            c != null && (c.Sp.nearWater || c.Data.Element1 == Element.Tide || c.Data.Element2 == Element.Tide);

        /// <summary>In the water, or standing right at its edge.</summary>
        bool AtWater
        {
            get
            {
                if (IsSwimming) return true;
                Vector3 p = transform.position, a = p + transform.forward * 3f;
                float sea = Tides.Surface(p);
                return Ground.Height(p.x, p.z) < sea - 0.5f || Ground.Height(a.x, a.z) < sea - 1.1f;
            }
        }

        bool HasHarness => Stats.Bag.Count("kin_harness") > 0;
        /// <summary>Ride-anything worlds: every kin big enough is a mount, and nobody needs a harness.</summary>
        static bool RideAny => WorldRules.Current.rideAnything;
        static bool BigEnough(CreatureActor c) => c.Sp.size * c.Data.SizeScale * WorldRules.Current.kinSize >= 0.55f;
        /// <summary>Riding on water only (not a land mount, or no harness): hops you off at the shore.</summary>
        bool SurfOnly => mount != null && !RideAny && (!mount.Sp.mountable || !HasHarness);
        float ashoreTimer;

        /// <summary>Whether RIDE / SURF is possible right now (reason explains why not).</summary>
        public bool CanRide(out string reason)
        {
            reason = null;
            var c = GameManager.I.Party.Companion;
            if (dead) { reason = ""; return false; }
            if (c == null || !c.IsAlive) { reason = "Summon a kin first: a rideable one for land, a water kin to surf."; return false; }
            bool surf = CanSurf(c) && AtWater;
            if (surf) return true;
            if (IsSwimming) { reason = $"{c.Data.DisplayName} can't swim with you on its back - summon a Tide kin to surf."; return false; }
            if (RideAny)
            {
                if (BigEnough(c)) return true;
                reason = $"{c.Data.DisplayName} is too small to ride.";
                return false;
            }
            if (!c.Sp.mountable) { reason = CanSurf(c) ? $"{c.Data.DisplayName} will carry you over water - wade in first." : $"{c.Data.DisplayName} can't be ridden."; return false; }
            if (!HasHarness) { reason = "Craft a Kin Harness at a Workbench to ride on land."; return false; }
            return true;
        }

        public bool CompanionRideable
        {
            get { var c = GameManager.I.Party.Companion; return c != null && c.IsAlive && (c.Sp.mountable || (RideAny && BigEnough(c)) || (CanSurf(c) && AtWater)); }
        }

        /// <summary>RIDE on land, SURF when a water kin can take you out.</summary>
        public string RideLabel
        {
            get { var c = GameManager.I.Party.Companion; return c != null && CanSurf(c) && AtWater ? "SURF" : "RIDE"; }
        }

        /// <summary>Surf-only kin set you down once you're properly back on land.</summary>
        void TickSurf(float dt)
        {
            if (!IsMounted || !SurfOnly) { ashoreTimer = 0f; return; }
            Vector3 p = transform.position;
            bool ashore = !IsSwimming && Ground.Height(p.x, p.z) > Tides.Surface(p) - 0.3f;
            ashoreTimer = ashore ? ashoreTimer + dt : 0f;
            if (ashoreTimer > 0.5f)
            {
                string who = mount.Data.DisplayName;
                Dismount(false);
                GameEvents.Say($"{who} sets you down on the shore.", new Color(0.7f, 1f, 0.8f));
            }
        }

        public void ToggleRide()
        {
            if (IsMounted) { Dismount(false); return; }
            if (!CanRide(out string reason)) { if (!string.IsNullOrEmpty(reason)) GameEvents.Warn(reason); return; }
            MountUp(GameManager.I.Party.Companion);
        }

        void MountUp(CreatureActor c)
        {
            mount = c;
            c.SetMounted(true);
            Quests.Note("ride");
            IsCrouching = false;
            swingTimer = 0f;
            dodgeTimer = 0f;
            // hop onto it: the mount's spot becomes ours
            bool onWater = IsSwimming;
            Vector3 p = onWater ? transform.position : c.Position;
            if (!onWater) p.y = Mathf.Max(GroundHeight(p), Tides.Surface(p) - 1.05f);
            transform.position = p;
            transform.rotation = Quaternion.Euler(0, onWater ? transform.eulerAngles.y : c.transform.eulerAngles.y, 0);
            cam.SetRideOffset(SeatY, 2.5f);
            ashoreTimer = 0f;
            Fx.Burst(p + Vector3.up * SeatY, new Color(0.9f, 0.85f, 0.7f), 12, 3f, 0.3f, 0.5f);
            SynthAudio.Play(Sfx.Jump, 0.8f);
            GameEvents.Say(SurfOnly ? $"Surfing on {c.Data.DisplayName}. Sprint to go faster - it sets you down when you reach land."
                                    : $"Riding {c.Data.DisplayName}. Sprint to gallop - tap DISMOUNT to hop off.", new Color(0.7f, 1f, 0.8f));
        }

        /// <summary>Hop off (also called automatically when the mount faints, is recalled, or you swim/die).</summary>
        public void Dismount(bool quiet)
        {
            if (mount == null) return;
            var c = mount;
            mount = null;
            if (c != null && c.gameObject.activeSelf)
            {
                c.SetMounted(false);
                Vector3 side = transform.position + transform.right * 1.6f;
                side.y = GroundHeight(side);
                c.transform.position = side;
            }
            Vector3 p = transform.position + transform.right * -0.6f;
            p.y = GroundHeight(p);
            transform.position = p;
            if (Rig.Model != null) Rig.Model.localPosition = Vector3.zero;
            cam.SetRideOffset(0f, 0f);
            if (!quiet) SynthAudio.Play(Sfx.Jump, 1.1f, 0.5f);
        }

        float MountTargetSpeed()
        {
            // a kin that was never meant as a mount (ride-anything worlds, or a surfer) carries you a little faster than it runs
            float baseSpeed = mount.Sp.mountable ? mount.Sp.mountSpeed : Mathf.Max(7.5f, mount.Sp.runSpeed * 1.2f);
            float run = baseSpeed * mount.Data.SpeedMul * mount.Status.SpeedMul;
            return IsSprinting ? run : run * 0.55f;
        }

        void UpdateMountFollow()
        {
            if (!IsMounted) return;
            if (!mount.IsAlive || !mount.gameObject.activeSelf || mount.Mode != CreatureMode.Companion)
            {
                Dismount(true);
                return;
            }
            mount.SetMountPose(transform.position, transform.eulerAngles.y, Speed);
        }

        void AnimateRider(float dt)
        {
            Rig.Animate(dt, 0f, false, false);
            if (Rig.Model == null) return;
            // saddle bob follows the mount's gait
            float bob = Mathf.Abs(Mathf.Sin(Time.time * (4f + Speed * 0.9f))) * Mathf.Clamp01(Speed / 6f) * 0.08f;
            Rig.Model.localPosition = new Vector3(0f, SeatY - 0.8f + bob, SeatZ);
            Rig.Model.localRotation = Quaternion.Euler(Mathf.Clamp(Speed * 1.2f, 0f, 12f), 0f, 0f);
            // astride: thighs forward and out around the mount, knees bent so the shins hang down its sides
            Rig.OverrideRot(BodyGroup.LegFL, Quaternion.Euler(-72f, 0f, 24f));
            Rig.OverrideRot(BodyGroup.LegFR, Quaternion.Euler(-72f, 0f, -24f));
            Rig.OverrideRot(BodyGroup.ShinL, Quaternion.Euler(78f, 0f, 0f));
            Rig.OverrideRot(BodyGroup.ShinR, Quaternion.Euler(78f, 0f, 0f));
            Rig.OverrideRot(BodyGroup.Torso, Quaternion.Euler(4f + Mathf.Clamp(Speed * 0.6f, 0f, 8f), 0f, 0f));
            // hands forward and low, elbows bent, as if on the reins (a swing takes the arms over)
            Rig.OverrideRot(BodyGroup.ArmL, Quaternion.Euler(-28f, 0f, 8f));
            Rig.OverrideRot(BodyGroup.ArmR, Quaternion.Euler(-28f, 0f, -8f));
            Rig.OverrideRot(BodyGroup.ForearmL, Quaternion.Euler(-62f, 14f, 0f));
            Rig.OverrideRot(BodyGroup.ForearmR, Quaternion.Euler(-62f, -14f, 0f));
        }

        // ============================================================ SICKLE SWEEP
        readonly List<ResourceNode> sweepBuf = new List<ResourceNode>(16);

        /// <summary>Sickle: harvest every plant node in a frontal arc in one swing.</summary>
        void SweepHarvest(ResourceNode first, int power, float toolYield, int tier)
        {
            float range = tier >= 3 ? 3.4f : 2.8f;
            int maxHits = tier >= 3 ? 6 : 4;
            Vector3 pos = transform.position, fwd = transform.forward;
            sweepBuf.Clear();
            ChunkManager.I.Nodes.Query(pos, range + 1f, sweepBuf);
            sweepBuf.Sort((a, b) => (a.pos - pos).sqrMagnitude.CompareTo((b.pos - pos).sqrMagnitude));
            int hits = 0;
            HitNode(first, power, toolYield);
            hits++;
            foreach (var n in sweepBuf)
            {
                if (hits >= maxHits) break;
                if (n == first || !n.alive || !n.Info.Plant) continue;
                Vector3 d = n.pos - pos; d.y = 0;
                if (d.magnitude - n.Radius > range) continue;
                if (d.sqrMagnitude > 0.2f && Vector3.Dot(d.normalized, fwd) < 0.05f) continue;
                HitNode(n, power, toolYield);
                hits++;
            }
            // visible sweep arc
            for (int i = -3; i <= 3; i++)
            {
                Vector3 dir = Quaternion.Euler(0, i * 20f, 0) * fwd;
                Fx.Puff(pos + dir * (range * 0.7f) + Vector3.up * 0.5f, new Color(0.75f, 1f, 0.55f, 0.8f), 0.35f, 0.3f);
            }
        }
    }
}
