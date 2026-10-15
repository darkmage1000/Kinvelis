using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Capture seed "ready" stance: tap SEED to take a seed out (arc, target ring and catch chance shown, aim with the
    /// camera), tap ATTACK/THROW to throw (you stay ready), tap SEED again - or roll, mount, swim - to put it away
    /// without spending anything.
    /// </summary>
    public partial class PlayerController
    {
        const int ArcDots = 14;

        public bool Aiming { get; private set; }
        public CreatureActor AimTarget { get; private set; }
        /// <summary>Catch chance 0..1 for the aimed kin, -1 when nothing is aimed / out of range.</summary>
        public float AimChance { get; private set; } = -1f;
        public bool AimInRange { get; private set; }

        Transform aimRoot, aimRing;
        Transform[] arcDots;
        MeshRenderer ringRenderer;
        static readonly Vector3[] arcPts = new Vector3[ArcDots];
        static Mesh dotMesh, ringMesh;
        MaterialPropertyBlock ringMpb;
        static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        /// <summary>SEED toggles the ready stance; while ready, ATTACK throws. Leaving the stance never spends a seed.</summary>
        void TickThrowInput(float dt)
        {
            // anything that takes your hands puts the seed away
            if (Aiming && (IsSwimming || dead || dodgeTimer > 0f)) { SetAiming(false); return; }   // riding is fine: you throw from the saddle
            var cap = GameManager.I.Capture;
            if (WorldRules.Current.simpleControls)
            {
                // Simple buttons: no ready stance. One tap on SEED throws at the nearest wild kin in reach.
                if (Aiming) SetAiming(false);
                if (!InputHub.Down(Btn.Throw) || IsSwimming || dead) return;
                var seedNow = cap.SelectedSeed(Stats);
                var near = LockTarget != null ? LockTarget : CreatureRegistry.FindNearestWild(transform.position, cap.MaxThrowRange);
                if (seedNow == null) GameEvents.Warn("No capture seeds! Craft Tether Seeds (Menu > Craft).");
                else if (near == null || near.Duelist || Vector3.Distance(near.Position, transform.position) > cap.MaxThrowRange) GameEvents.Say("Walk up to a wild kin, then tap SEED.");
                else
                {
                    Vector3 to = near.Position - transform.position; to.y = 0f;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
                    cap.TryThrow(this, near, cam);
                }
                return;
            }
            if (InputHub.Down(Btn.Throw))
            {
                if (Aiming) { SetAiming(false); SynthAudio.Play(Sfx.UiClick, 0.9f, 0.5f); }
                else if (!IsSwimming && !dead)
                {
                    if (cap.SelectedSeed(Stats) == null) GameEvents.Warn("No capture seeds! Craft Tether Seeds (Menu > Craft).");
                    else SetAiming(true);
                }
            }
            if (!Aiming) return;
            UpdateAim();
            if (InputHub.Down(Btn.Attack))
            {
                if (AimTarget != null && !CaptureSystem.CanBind(AimTarget, cap.SelectedSeed(Stats)))
                {
                    GameEvents.Warn(AimTarget.Boss ? "A Warden can't be bound by an ordinary seed." : "That kin belongs to another tamer - you can't catch it.");
                    return;
                }
                cap.TryThrow(this, AimInRange ? AimTarget : null, cam);
                if (cap.SelectedSeed(Stats) == null) { SetAiming(false); GameEvents.Say("Out of capture seeds."); }
            }
        }

        public void CancelAim()
        {
            if (Aiming) SetAiming(false);
        }

        void SetAiming(bool on)
        {
            Aiming = on;
            if (!on) { AimTarget = null; AimChance = -1f; }
            if (aimRoot == null && on) BuildAimVisuals();
            if (aimRoot != null) aimRoot.gameObject.SetActive(on);
            if (on) SynthAudio.Play(Sfx.UiClick, 1.3f, 0.5f);
        }

        void UpdateAim()
        {
            var cap = GameManager.I.Capture;
            Vector3 pos = transform.position;
            // aim with the camera: best kin near the screen center line
            // a locked kin is who the seed is for; otherwise the best kin near the screen centre line
            AimTarget = LockTarget != null && Vector3.Distance(LockTarget.Position, pos) <= cap.MaxThrowRange + 6f
                ? LockTarget : CreatureRegistry.FindTarget(pos, cam.Forward, cap.MaxThrowRange + 6f, 0.75f);
            AimInRange = AimTarget != null && Vector3.Distance(AimTarget.Position, pos) <= cap.MaxThrowRange;
            var seed = cap.SelectedSeed(Stats);
            AimChance = AimTarget != null && AimInRange && seed != null ? CaptureSystem.Chance(AimTarget, seed, Stats, AimTarget.Unaware) : -1f;

            // face the throw
            Vector3 look = AimTarget != null ? AimTarget.Position - pos : cam.Forward;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 720f * Time.deltaTime);

            // arc
            cap.PredictArc(this, AimInRange ? AimTarget : null, cam, arcPts);
            for (int i = 0; i < ArcDots; i++)
            {
                arcDots[i].position = arcPts[i];
                float k = 1f - i / (float)ArcDots;
                arcDots[i].localScale = Vector3.one * (0.1f + 0.08f * k);
            }

            // ring under the target, tinted by odds
            bool showRing = AimTarget != null;
            if (aimRing.gameObject.activeSelf != showRing) aimRing.gameObject.SetActive(showRing);
            if (showRing)
            {
                Vector3 tp = AimTarget.Position;
                tp.y = Ground.Height(tp.x, tp.z) + 0.08f;
                aimRing.position = tp;
                aimRing.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
                aimRing.localScale = Vector3.one * (AimTarget.Radius * 2.4f + 0.6f);
                if (ringMpb == null) ringMpb = new MaterialPropertyBlock();
                Color rc = AimInRange ? ChanceColor(AimChance) : new Color(0.6f, 0.6f, 0.65f);
                rc.a = 0.75f;
                ringMpb.SetColor(FlashColorId, rc);
                ringRenderer.SetPropertyBlock(ringMpb);
            }
        }

        static readonly System.Collections.Generic.Dictionary<string, Mesh> seedMeshes = new System.Collections.Generic.Dictionary<string, Mesh>();

        /// <summary>The selected seed's icon model, shrunk to palm size (cached).</summary>
        static Mesh SeedHandMesh(ItemDef d)
        {
            if (seedMeshes.TryGetValue(d.id, out var m) && m != null) return m;
            var src = ItemModels.Build(d);
            if (src == null) return null;
            var verts = src.vertices; var cols = src.colors32; var norms = src.normals; var tris = src.triangles;
            // scale + center the model around the hand (icon models sit on y=0, ~1 unit tall)
            var scaled = new Mesh();
            for (int i = 0; i < verts.Length; i++) verts[i] = new Vector3(verts[i].x * 0.32f, (verts[i].y - 0.35f) * 0.32f - 0.06f, verts[i].z * 0.32f);
            scaled.vertices = verts; scaled.normals = norms; scaled.colors32 = cols; scaled.triangles = tris;
            scaled.RecalculateBounds();
            scaled.name = "seedhand_" + d.id;
            Object.Destroy(src);
            seedMeshes[d.id] = scaled;
            return scaled;
        }

        public static Color ChanceColor(float p) =>
            p < 0f ? new Color(0.75f, 0.75f, 0.8f) : p < 0.25f ? new Color(1f, 0.4f, 0.35f) : p < 0.55f ? new Color(1f, 0.82f, 0.3f) : new Color(0.45f, 1f, 0.5f);

        void BuildAimVisuals()
        {
            if (dotMesh == null)
            {
                var mb = new MeshBuilder();
                mb.Sphere(Matrix4x4.identity, new Color(0.75f, 1f, 0.6f), new Color(0.4f, 0.85f, 0.35f), 6, 4);
                dotMesh = mb.ToMesh("aim_dot");
                mb.Clear();
                // flat ring of short boxes (unit diameter)
                for (int i = 0; i < 20; i++)
                {
                    float a = i * 18f;
                    var p = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.5f, 0f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.5f);
                    mb.Box(Matrix4x4.TRS(p, Quaternion.Euler(0f, a, 0f), new Vector3(0.13f, 0.03f, 0.05f)), i % 5 == 0 ? new Color(1f, 1f, 1f) : new Color(0.6f, 1f, 0.55f));
                }
                ringMesh = mb.ToMesh("aim_ring");
            }
            aimRoot = new GameObject("AimVisuals").transform;
            arcDots = new Transform[ArcDots];
            for (int i = 0; i < ArcDots; i++)
            {
                var d = new GameObject("ArcDot");
                d.transform.SetParent(aimRoot, false);
                d.AddComponent<MeshFilter>().sharedMesh = dotMesh;
                var r = d.AddComponent<MeshRenderer>();
                r.sharedMaterial = RenderAssets.Creature;
                r.shadowCastingMode = ShadowCastingMode.Off;
                arcDots[i] = d.transform;
            }
            var ring = new GameObject("AimRing");
            ring.transform.SetParent(aimRoot, false);
            ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            ringRenderer = ring.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = RenderAssets.Creature;
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            aimRing = ring.transform;
        }
    }
}
