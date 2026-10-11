using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>Ghost-preview placement: in front of the player, grid snapped, rotate/confirm/cancel buttons.</summary>
    public class BuildPlacement : MonoBehaviour
    {
        public static BuildPlacement I { get; private set; }

        [SerializeField] float gridSnap = 0.5f;
        [SerializeField] float rotateStep = 45f;

        public bool Active { get; private set; }
        public BuildingDef Current { get; private set; }
        public bool Valid { get; private set; }
        public string Reason { get; private set; }

        GameObject ghost;
        MeshFilter ghostMf;
        MeshRenderer ghostMr;
        MaterialPropertyBlock mpb;
        float rotY;
        Vector3 pos;
        LineRenderer ring;

        public void Init()
        {
            I = this;
            ghost = new GameObject("BuildGhost");
            ghost.transform.SetParent(transform, false);
            ghostMf = ghost.AddComponent<MeshFilter>();
            ghostMr = ghost.AddComponent<MeshRenderer>();
            ghostMr.sharedMaterial = RenderAssets.Ghost;
            ghostMr.shadowCastingMode = ShadowCastingMode.Off;
            ghost.SetActive(false);
            mpb = new MaterialPropertyBlock();

            var rg = new GameObject("CampRing");
            rg.transform.SetParent(transform, false);
            ring = rg.AddComponent<LineRenderer>();
            var ringMat = new Material(RenderAssets.ParticleAdd);
            ringMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            ring.sharedMaterial = ringMat;
            ring.loop = true;
            ring.widthMultiplier = 0.25f;
            ring.positionCount = 64;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.enabled = false;
        }

        public void Begin(BuildingDef def)
        {
            if (def == null) return;
            if (!WorldRules.Current.freeBuild && !ItemSources.Has(def.cost))
            {
                GameEvents.Warn($"Not enough materials for {def.displayName}.");
                return;
            }
            Current = def;
            Active = true;
            if (ghostMf.sharedMesh != null) Destroy(ghostMf.sharedMesh);
            ghostMf.sharedMesh = ModelBuilder.BuildStatic(def.parts, def.palette, "ghost_" + def.id);
            ghost.SetActive(true);
            rotY = GameManager.I.Player.transform.eulerAngles.y + 180f;
            rotY = Mathf.Round(rotY / rotateStep) * rotateStep;
            SynthAudio.Play(Sfx.UiOpen);
        }

        public void Cancel()
        {
            Active = false;
            Current = null;
            ghost.SetActive(false);
            ring.enabled = false;
        }

        public void Tick(float dt)
        {
            var bm = BaseManager.I;
            ring.enabled = bm.HasBase && (Active || bm.IsInsideBase(GameManager.I.Player.Position, 6f) && GameManager.I.ShowCampRing);
            if (ring.enabled) DrawRing(bm.CorePosition, bm.Radius);
            if (!Active) return;

            if (InputHub.Down(Btn.Rotate)) rotY += rotateStep;
            if (InputHub.Down(Btn.Cancel)) { Cancel(); return; }

            var p = GameManager.I.Player;
            Vector3 target = p.Position + p.Forward * (Current.footprint + 2.2f);
            target.x = Mathf.Round(target.x / gridSnap) * gridSnap;
            target.z = Mathf.Round(target.z / gridSnap) * gridSnap;
            target.y = WorldGen.Height(target.x, target.z);
            pos = Vector3.Lerp(pos, target, (pos - target).sqrMagnitude > 25f ? 1f : dt * 18f);
            ghost.transform.SetPositionAndRotation(pos, Quaternion.Slerp(ghost.transform.rotation, Quaternion.Euler(0, rotY, 0), dt * 15f));
            float bob = 1f + Mathf.Sin(Time.time * 6f) * 0.02f;
            ghost.transform.localScale = Vector3.one * bob;

            string reason = null;
            Valid = !Caves.InCave && bm.CanPlace(Current, target, out reason) && (WorldRules.Current.freeBuild || ItemSources.Has(Current.cost));
            if (Caves.InCave) reason = "Can't build underground";
            Reason = reason ?? (Valid ? null : "Missing materials");
            mpb.SetColor("_FlashColor", Valid ? new Color(0.3f, 1f, 0.45f, 0.45f) : new Color(1f, 0.25f, 0.2f, 0.55f));
            ghostMr.SetPropertyBlock(mpb);

            if (InputHub.Down(Btn.Confirm)) Confirm();
        }

        public void Rotate() => rotY += rotateStep;

        public void Confirm()
        {
            if (!Active) return;
            var bm = BaseManager.I;
            Vector3 target = ghost.transform.position;
            if (!bm.CanPlace(Current, target, out string reason)) { GameEvents.Warn(reason); SynthAudio.Play(Sfx.Clunk); return; }
            bool free = WorldRules.Current.freeBuild;   // free-building worlds: nothing is spent
            if (!free && !ItemSources.Has(Current.cost)) { GameEvents.Warn("Missing materials"); SynthAudio.Play(Sfx.Clunk); return; }
            if (!free) ItemSources.Consume(Current.cost);
            var b = bm.Place(Current, target, rotY);
            Fx.Burst(target + Vector3.up * 0.5f, new Color(0.95f, 0.85f, 0.6f), 24, 5f, 0.4f, 0.7f);
            Fx.Ring(target, new Color(1f, 0.95f, 0.7f), Current.footprint + 1f, 24);
            SynthAudio.Play(Sfx.Build);
            GameManager.I.CameraRig.Shake(0.3f);
            GameManager.I.AddPlayerXp(Current.isCore ? 30 : 8);
            if (Current.isCore)
            {
                GameEvents.Good("Camp founded! Build workstations and assign kin as workers (Menu > Camp).");
                Cancel();
                return;
            }
            GameEvents.Good($"Built {Current.displayName}");
            // Auto-staff new buildings with idle base kin
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
            {
                if (b.Def.workerSlots <= 0 || b.AssignedCount >= b.Def.workerSlots) break;
                if (d.assignedBuilding == null && b.Suitability(d) > 0) d.assignedBuilding = b.Uid;
            }
            if (!free && !ItemSources.Has(Current.cost)) Cancel();
        }

        void DrawRing(Vector3 c, float r)
        {
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                float x = c.x + Mathf.Cos(a) * r, z = c.z + Mathf.Sin(a) * r;
                ring.SetPosition(i, new Vector3(x, WorldGen.Height(x, z) + 0.3f, z));
            }
            ring.startColor = ring.endColor = Active ? new Color(0.5f, 1f, 0.7f, 0.8f) : new Color(0.5f, 1f, 0.7f, 0.35f);
        }
    }
}
