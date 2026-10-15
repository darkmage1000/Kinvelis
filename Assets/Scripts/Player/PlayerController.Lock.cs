using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Target lock-on (user request 2026-10-09: pick one kin out of a crowd, and switch easily).
    /// LOCK (L / middle mouse / right stick click) locks the kin the target frame is showing; tapping a kin on the
    /// screen locks that one; the arrows on the target frame ([ and ], d-pad left / right) step to the next kin to the
    /// left or right as you see them. While locked, that kin is the CombatTarget for everything that reads it: your
    /// swings and shots face it, a one-foe weapon picks it out of a crowd, seeds are aimed at it, the companion's
    /// skills go to it, and the camera drifts to keep it in view once you stop steering. The lock passes to whatever is
    /// still hunting you when the kin faints or is caught, and lets go beyond 40 m.
    /// A ring on the ground and an arrow over its head show which one it is.
    /// </summary>
    public partial class PlayerController
    {
        public const float LockRange = 32f;
        const float LockBreak = 40f;

        public CreatureActor LockTarget { get; private set; }
        /// <summary>The kin a shot in flight was loosed at (decided when the draw starts).</summary>
        CreatureActor shotTarget;

        Transform lockRoot, lockRing, lockArrow;
        MeshRenderer lockRingMr, lockArrowMr;
        MaterialPropertyBlock lockMpb;
        float lockCheck;
        static Mesh lockRingMesh, lockArrowMesh;
        static readonly List<CreatureActor> lockBuf = new List<CreatureActor>(24);
        static readonly List<float> lockAngles = new List<float>(24);

        static float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

        void TickLock(float dt, Vector3 pos)
        {
            if (InputHub.Down(Btn.Lock)) ToggleLock();
            if (InputHub.Down(Btn.NextTarget)) CycleLock(1);
            if (InputHub.Down(Btn.PrevTarget)) CycleLock(-1);
            if (InputHub.Tapped) TapLock(InputHub.TapPos);

            var t = LockTarget;
            if (t != null)
            {
                bool gone = t == null || !t.IsAlive || t.Mode != CreatureMode.Wild || !t.gameObject.activeInHierarchy;
                lockCheck -= dt;
                if (!gone && lockCheck <= 0f) { lockCheck = 0.25f; gone = !CreatureRegistry.All.Contains(t); }
                if (gone) SetLock(NextThreat(pos, t), false);   // fainted, caught or gone: on to whatever is still after you
                else if (Flat(t.Position, pos) > LockBreak) { SetLock(null, false); GameEvents.Say("Too far away - lock released."); }
            }
            UpdateLockMark();
            if (LockTarget != null && !Aiming) cam.Assist(LockTarget.Position, dt);
        }

        static bool Lockable(CreatureActor a) => a != null && a.Mode == CreatureMode.Wild && a.IsAlive && a.gameObject.activeInHierarchy;

        CreatureActor NextThreat(Vector3 pos, CreatureActor not)
        {
            CreatureActor best = null; float bd = 16f;
            var all = CreatureRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (a == not || !Lockable(a) || !a.Aggro) continue;
                float d = Flat(a.Position, pos);
                if (d < bd) { bd = d; best = a; }
            }
            return best;
        }

        public void ToggleLock()
        {
            if (LockTarget != null) { SetLock(null, true); return; }
            // what the target frame is showing, else whatever is nearest the middle of the view
            var t = Lockable(CombatTarget) ? CombatTarget : CreatureRegistry.FindTarget(transform.position, cam.Forward, LockRange, 0.5f);
            if (t == null) { GameEvents.Warn("Nothing to lock on to."); return; }
            SetLock(t, true);
        }

        public void ClearLock() { if (LockTarget != null) SetLock(null, false); }

        void SetLock(CreatureActor t, bool sound)
        {
            LockTarget = t;
            lockCheck = 0.25f;
            if (t != null)
            {
                CombatTarget = t;
                if (sound) SynthAudio.Play(Sfx.UiClick, 1.5f, 0.7f);
            }
            else if (sound) SynthAudio.Play(Sfx.UiClick, 0.8f, 0.5f);
            if (t == null) UpdateLockMark();
        }

        /// <summary>Step the lock to the next kin to the right (+1) or left (-1), as seen from the camera.</summary>
        public void CycleLock(int dir)
        {
            Vector3 pos = transform.position, fwd = cam.Forward;
            lockBuf.Clear(); lockAngles.Clear();
            var all = CreatureRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (!Lockable(a)) continue;
                Vector3 d = a.Position - pos; d.y = 0f;
                if (d.magnitude - a.Radius > LockRange) continue;
                float ang = d.sqrMagnitude > 0.01f ? Vector3.SignedAngle(fwd, d, Vector3.up) : 0f;
                // insertion sort by angle, left to right
                int at = 0;
                while (at < lockAngles.Count && lockAngles[at] < ang) at++;
                lockBuf.Insert(at, a); lockAngles.Insert(at, ang);
            }
            if (lockBuf.Count == 0) { GameEvents.Warn("Nothing to lock on to."); return; }
            int cur = lockBuf.IndexOf(LockTarget);
            CreatureActor pick;
            if (cur < 0)
            {
                // nothing locked yet: the one nearest the middle of the view
                int best = 0;
                for (int i = 1; i < lockBuf.Count; i++) if (Mathf.Abs(lockAngles[i]) < Mathf.Abs(lockAngles[best])) best = i;
                pick = lockBuf[best];
            }
            else
            {
                if (lockBuf.Count == 1) { GameEvents.Say("No other kin in range."); return; }
                pick = lockBuf[(cur + dir + lockBuf.Count) % lockBuf.Count];
            }
            SetLock(pick, true);
        }

        /// <summary>A tap on the screen: lock the kin under the finger (a generous circle around each one).</summary>
        void TapLock(Vector2 screen)
        {
            var c = cam.Cam;
            if (c == null || Aiming) return;
            CreatureActor pick = null; float best = float.MaxValue;
            var all = CreatureRegistry.All;
            Vector3 right = c.transform.right;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (!Lockable(a)) continue;
                Vector3 centre = a.Position + Vector3.up * a.Radius;
                Vector3 sp = c.WorldToScreenPoint(centre);
                if (sp.z < 0.5f || sp.z > LockRange + 12f) continue;
                float px = (c.WorldToScreenPoint(centre + right * (a.Radius + 0.35f)) - sp).magnitude;
                float allow = Mathf.Max(px * 1.2f, Screen.height * 0.055f);
                float d = Vector2.Distance(new Vector2(sp.x, sp.y), screen);
                if (d > allow) continue;
                float score = d / allow + sp.z * 0.01f;
                if (score < best) { best = score; pick = a; }
            }
            if (pick != null && pick != LockTarget) SetLock(pick, true);
        }

        // ================================================================ MARKER
        void UpdateLockMark()
        {
            var t = LockTarget;
            if (t == null)
            {
                if (lockRoot != null && lockRoot.gameObject.activeSelf) lockRoot.gameObject.SetActive(false);
                return;
            }
            if (lockRoot == null) BuildLockMark();
            if (!lockRoot.gameObject.activeSelf) lockRoot.gameObject.SetActive(true);

            Vector3 tp = t.Position;
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
            lockRing.position = new Vector3(tp.x, Mathf.Max(Ground.Height(tp.x, tp.z), tp.y - 0.4f) + 0.1f, tp.z);
            lockRing.rotation = Quaternion.Euler(0f, -Time.time * 70f, 0f);
            lockRing.localScale = Vector3.one * ((t.Radius * 2.5f + 0.7f) * pulse);

            float s = Mathf.Clamp(0.6f + t.Radius * 0.4f, 0.7f, 1.25f);
            float top = DangerSkull.ModelTop(t.Sp) * t.Sp.size * t.Data.SizeScale + t.Rig.HoverHeight;
            // clear of the red skull when there is one
            if (DangerSkull.IsDanger(t.Data)) top += Mathf.Clamp(0.4f + t.Radius * 0.55f, 0.5f, 1.1f) * 1.5f;
            lockArrow.position = tp + Vector3.up * (top + s * 0.75f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.16f);
            lockArrow.rotation = Quaternion.Euler(0f, Time.time * 160f, 0f);
            lockArrow.localScale = Vector3.one * s;

            if (lockMpb == null) lockMpb = new MaterialPropertyBlock();
            // self-lit, so it reads at night and in caves; warmer while the kin is hunting you
            Color c = t.Aggro ? new Color(1f, 0.35f, 0.2f, 0.9f) : new Color(1f, 0.82f, 0.25f, 0.9f);
            lockMpb.SetColor(FlashColorId, c);
            lockRingMr.SetPropertyBlock(lockMpb);
            lockArrowMr.SetPropertyBlock(lockMpb);
        }

        void BuildLockMark()
        {
            if (lockRingMesh == null)
            {
                var mb = new MeshBuilder();
                Color32 w = new Color(1f, 1f, 1f);
                // four brackets round the kin, with a tick pointing in from each (unit diameter)
                for (int q = 0; q < 4; q++)
                {
                    for (int k = -3; k <= 3; k++)
                    {
                        float a = q * 90f + k * 9f;
                        var p = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.5f, 0f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.5f);
                        mb.Box(Matrix4x4.TRS(p, Quaternion.Euler(0f, a, 0f), new Vector3(0.085f, 0.05f, 0.07f)), w);
                    }
                    float m = q * 90f;
                    var tp = new Vector3(Mathf.Sin(m * Mathf.Deg2Rad) * 0.41f, 0f, Mathf.Cos(m * Mathf.Deg2Rad) * 0.41f);
                    mb.Box(Matrix4x4.TRS(tp, Quaternion.Euler(0f, m, 0f), new Vector3(0.06f, 0.05f, 0.17f)), w);
                }
                lockRingMesh = mb.ToMesh("lock_ring");
                mb.Clear();
                // a four-sided arrowhead pointing down, with a short stem
                mb.Cone(MeshBuilder.TRS(new Vector3(0f, 0.02f, 0f), new Vector3(180f, 0f, 0f), new Vector3(0.42f, 0.46f, 0.42f)), w, 4);
                mb.Box(MeshBuilder.TRS(new Vector3(0f, 0.42f, 0f), Vector3.zero, new Vector3(0.14f, 0.3f, 0.14f)), w);
                lockArrowMesh = mb.ToMesh("lock_arrow");
            }
            lockRoot = new GameObject("LockMark").transform;
            lockRing = MarkPart("Ring", lockRingMesh, out lockRingMr);
            lockArrow = MarkPart("Arrow", lockArrowMesh, out lockArrowMr);
        }

        Transform MarkPart(string name, Mesh mesh, out MeshRenderer mr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(lockRoot, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        // ================================================================ RANGED
        /// <summary>
        /// With a sling, bow or crossbow in hand: is ATTACK a shot? Yes unless there is something to gather right in
        /// front of you and nothing worth shooting (the kin you locked, one that is hunting you, or one within 7 m).
        /// </summary>
        // ---------------------------------------------------------------- ammunition
        float ammoWarnAt = -99f;

        /// <summary>Endless-seed worlds (Little Explorer) never run out of shots either.</summary>
        static bool FreeAmmo => WorldRules.Current.endlessSeeds;

        /// <summary>The ranged weapon in hand: what it shoots (null if it isn't one) and how many shots are left in the bag.</summary>
        public string AmmoId => HandStyles.Ammo(heldStyle);
        public int AmmoLeft => AmmoId == null ? -1 : FreeAmmo ? 999 : Stats.Bag.Count(AmmoId);

        bool HasAmmoFor(HandStyle s)
        {
            string id = HandStyles.Ammo(s);
            return id == null || FreeAmmo || Stats.Bag.Count(id) > 0;
        }

        /// <summary>ATTACK with an empty quiver: say so (not on every swing), then the weapon is swung like a club.</summary>
        void WarnNoAmmo(HandStyle s)
        {
            if (Time.time - ammoWarnAt < 6f) return;
            ammoWarnAt = Time.time;
            string id = HandStyles.Ammo(s);
            GameEvents.Warn(id == "stone" ? "No Stones left to sling - pick some up off the ground."
                : id == "arrow" ? "Out of Arrows - craft more by hand (Menu > Craft > Weapons)."
                : "Out of Bolts - make more at a Workbench.");
        }

        /// <summary>
        /// A ranged weapon only shoots at a kin you have LOCKED on to (user playtest 2026-10-15: a wild kin at the edge of
        /// range used to win over the rock in front of you). Without a lock ATTACK gathers, or swings the weapon like a club.
        /// </summary>
        bool WantsShot(Vector3 pos)
        {
            // the weapon you are holding, not a tool auto-tool has pulled out for the rock in front
            var st = HandStyles.Of(Stats.HeldDef);
            return HandStyles.IsRanged(st) && Stats.Bag.Count(Stats.held) > 0 && HasAmmoFor(st) && LockTarget != null && LockTarget.IsAlive;
        }

        /// <summary>The release: loose a stone / arrow / bolt at the kin picked when the draw began, or straight ahead.</summary>
        /// <summary>What the weapon in hand adds to a blow or a shot: its element and status (blueprint weapons), or the Venomous ring's poison on a plain one.</summary>
        void WeaponHit(out Element el, out StatusType st, out float chance)
        {
            el = Element.Normal; st = StatusType.None; chance = 0f;
            var wd = Stats.HeldDef;
            if (wd != null && wd.category == ItemCategory.Weapon && Stats.Bag.Count(wd.id) > 0) { el = wd.hitElement; st = wd.hitStatus; chance = wd.hitStatusChance; }
            if (st == StatusType.None && Stats.HasEffect(UniqueEffect.Venomous)) { st = StatusType.Poison; chance = 0.2f; }
        }

        void FireShot()
        {
            // the shot leaves the bag as it leaves the hand
            string ammo = HandStyles.Ammo(swingStyle);
            if (ammo != null && !FreeAmmo)
            {
                if (Stats.Bag.Count(ammo) <= 0) { WarnNoAmmo(swingStyle); shotTarget = null; return; }
                Stats.Bag.Remove(ammo, 1);
                GameEvents.RaiseInventoryChanged();
            }
            var r = HandStyles.Ranged(swingStyle);
            var info = HandStyles.Info(swingStyle, SwingAct.Shoot);
            Vector3 fwd = transform.forward;
            Vector3 origin = transform.position + Vector3.up * (IsMounted ? 1.5f : 1.3f) + fwd * 0.55f;
            var t = Lockable(shotTarget) ? shotTarget : null;
            Vector3 aim;
            if (t != null)
            {
                aim = t.Position + Vector3.up * Mathf.Max(0.3f, t.Radius);
                aim += t.Velocity * (Vector3.Distance(aim, origin) / r.speed * 0.8f);   // lead it
            }
            else
            {
                // nothing to aim at: flat along where you are looking, dipping to meet the ground at full range
                aim = origin + fwd * r.range;
                aim.y = Mathf.Min(origin.y, Ground.Height(aim.x, aim.z) + 0.6f);
            }
            Vector3 dir = aim - origin;
            if (dir.sqrMagnitude < 0.01f) dir = fwd;
            var comp = GameManager.I.Party.Companion;
            float vanguard = comp != null && comp.Sp.perk == CompanionPerk.Vanguard ? 1.15f : 1f;
            // like a blow, a shot is scaled by how long it takes: slow weapons hit harder
            float power = Stats.WeaponDamage * vanguard * (info.time / HandStyles.BaseTime);
            WeaponHit(out var hitEl, out var hitSt, out float hitCh);
            ProjectileSystem.I.FireShot(this, origin, dir.normalized * r.speed, power, r.range + 3f, r.shot, info.knock, t, r.turn, hitEl, hitSt, hitCh);
            SynthAudio.Play(swingStyle == HandStyle.Sling ? Sfx.Throw : Sfx.Shoot, swingStyle == HandStyle.Crossbow ? 0.75f : 1.25f, 0.55f);
            if (swingStyle == HandStyle.Crossbow) cam.Shake(0.18f);
            shotTarget = null;
        }
    }
}
