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
        /// <summary>House building (Housing): the piece being placed, or the REMOVE tool.</summary>
        public HousePieceDef Piece { get; private set; }
        public bool Removing { get; private set; }
        Housing.Spot spot;
        Housing.Piece removeTarget;
        BaseManager.Site removeSite;
        int turns;
        public string CurrentName => Removing ? (removeSite != null ? "Cancel " + removeSite.Name + " (all back)" : removeTarget != null ? "Remove " + removeTarget.def.name + " (half back)" : "Remove") : Piece != null ? Piece.name : Current != null ? Current.displayName : "";
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

        public void BeginPiece(HousePieceDef def)
        {
            if (def == null) return;
            if (!WorldRules.Current.freeBuild && !ItemSources.Has(def.cost)) { GameEvents.Warn($"Not enough materials for {def.name}."); return; }
            Current = null; Removing = false; Piece = def; Active = true;
            ghostMf.sharedMesh = Housing.MeshFor(def);
            ghost.SetActive(true);
            turns = 0;
            SynthAudio.Play(Sfx.UiOpen);
        }

        public void BeginRemove()
        {
            Current = null; Piece = null; Removing = true; Active = true; removeTarget = null; turns = 0;
            ghost.SetActive(false);
            SynthAudio.Play(Sfx.UiOpen);
        }

        void TickHouse(float dt)
        {
            var p = GameManager.I.Player;
            if (Removing)
            {
                var site = BaseManager.I.SiteNear(p.Position + p.Forward * 1.8f, 1.6f);
                if (site != null)
                {
                    removeSite = site; removeTarget = null;
                    ghost.SetActive(false);
                    Valid = true; Reason = null;
                    if (InputHub.Down(Btn.Confirm)) Confirm();
                    return;
                }
                removeSite = null;
                var t = Housing.I.PieceAt(p.Position, p.Forward);
                if (t != removeTarget)
                {
                    removeTarget = t;
                    if (t != null) ghostMf.sharedMesh = t.go.GetComponent<MeshFilter>().sharedMesh;
                }
                ghost.SetActive(t != null);
                if (t != null) { ghost.transform.SetPositionAndRotation(t.go.transform.position, t.go.transform.rotation); ghost.transform.localScale = Vector3.one * 1.02f; }
                string why = Housing.I.RemoveBlock(t);
                Valid = why == null; Reason = why;
                mpb.SetColor("_FlashColor", new Color(1f, 0.25f, 0.2f, Valid ? 0.6f : 0.3f));
                ghostMr.SetPropertyBlock(mpb);
                if (InputHub.Down(Btn.Confirm)) Confirm();
                return;
            }
            spot = Housing.I.Snap(Piece, p.Position, p.Forward, turns);
            Vector3 target = spot.pos;
            pos = Vector3.Lerp(pos, target, (pos - target).sqrMagnitude > 25f ? 1f : dt * 18f);
            ghost.transform.SetPositionAndRotation(pos, Quaternion.Slerp(ghost.transform.rotation, spot.q, dt * 15f));
            ghost.transform.localScale = Vector3.one;
            bool afford = WorldRules.Current.freeBuild || ItemSources.Has(Piece.cost);
            Valid = spot.ok && afford;
            Reason = !spot.ok ? spot.reason : afford ? null : "Missing materials";
            mpb.SetColor("_FlashColor", Valid ? new Color(0.3f, 1f, 0.45f, 0.45f) : new Color(1f, 0.25f, 0.2f, 0.55f));
            ghostMr.SetPropertyBlock(mpb);
            if (InputHub.Down(Btn.Confirm)) Confirm();
        }

        void ConfirmHouse()
        {
            var h = Housing.I;
            bool free = WorldRules.Current.freeBuild;
            if (Removing)
            {
                if (removeSite != null) { BaseManager.I.CancelSite(removeSite); removeSite = null; SynthAudio.Play(Sfx.Chop, 0.8f); return; }
                var t = removeTarget;
                string why = h.RemoveBlock(t);
                if (why != null) { GameEvents.Warn(why); SynthAudio.Play(Sfx.Clunk); return; }
                if (!free) foreach (var c in t.def.cost) { int n = c.count / 2; if (n > 0) GameManager.I.Player.Stats.Bag.Add(c.itemId, n); }
                Fx.Burst(t.go.transform.position + Vector3.up, new Color(0.8f, 0.65f, 0.45f), 18, 4f, 0.35f, 0.7f);
                SynthAudio.Play(Sfx.Chop, 0.8f);
                h.Remove(t);
                removeTarget = null;
                GameEvents.RaiseInventoryChanged();
                return;
            }
            var p = GameManager.I.Player;
            var s = h.Snap(Piece, p.Position, p.Forward, turns);
            if (!s.ok) { GameEvents.Warn(s.reason); SynthAudio.Play(Sfx.Clunk); return; }
            if (!free && !ItemSources.Has(Piece.cost)) { GameEvents.Warn("Missing materials"); SynthAudio.Play(Sfx.Clunk); return; }
            if (!free) ItemSources.Consume(Piece.cost);
            h.LevelWith(Piece, s);   // a ground floor on higher ground lifts the house beside it to stay level (Housing.Level)
            if (BaseManager.PieceBuildsOverTime) BaseManager.I.StartSite(Piece, s);   // a site Crafting kin (or you) build
            else h.Place(Piece, s);
            Fx.Burst(s.pos + Vector3.up * 0.5f, new Color(0.95f, 0.85f, 0.6f), 16, 4f, 0.35f, 0.6f);
            SynthAudio.Play(Sfx.Build, 1.15f);
            GameManager.I.AddPlayerXp(2);
            if (!free && !ItemSources.Has(Piece.cost)) { GameEvents.Say($"Out of materials for another {Piece.name}."); Cancel(); }
        }

        public void Begin(BuildingDef def)
        {
            if (def == null) return;
            Piece = null; Removing = false;
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
            Piece = null; Removing = false; removeTarget = null;
            ghost.SetActive(false);
            ring.enabled = false;
        }

        public void Tick(float dt)
        {
            var bm = BaseManager.I;
            ring.enabled = bm.HasBase && (Active || bm.IsInsideBase(GameManager.I.Player.Position, 6f) && GameManager.I.ShowCampRing);
            if (ring.enabled) DrawRing(bm.CorePosition, bm.Radius);
            if (!Active) return;

            if (InputHub.Down(Btn.Rotate)) Rotate();
            if (InputHub.Down(Btn.Cancel)) { Cancel(); return; }
            if (Piece != null || Removing) { TickHouse(dt); return; }

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

        public void Rotate() { rotY += rotateStep; turns++; }

        public void Confirm()
        {
            if (!Active) return;
            if (Piece != null || Removing) { ConfirmHouse(); return; }
            var bm = BaseManager.I;
            Vector3 target = ghost.transform.position;
            if (!bm.CanPlace(Current, target, out string reason)) { GameEvents.Warn(reason); SynthAudio.Play(Sfx.Clunk); return; }
            bool free = WorldRules.Current.freeBuild;   // free-building worlds: nothing is spent
            if (!free && !ItemSources.Has(Current.cost)) { GameEvents.Warn("Missing materials"); SynthAudio.Play(Sfx.Clunk); return; }
            if (!free) ItemSources.Consume(Current.cost);
            // most structures go up as a construction site that Crafting kin (or you) build (BaseManager.Construction)
            if (BaseManager.BuildsOverTime(Current))
            {
                bm.StartSite(Current, target, rotY);
                Fx.Ring(target, new Color(1f, 0.95f, 0.7f), Current.footprint + 1f, 24);
                SynthAudio.Play(Sfx.Build, 0.8f, 0.7f);
                GameManager.I.AddPlayerXp(8);
                GameEvents.Say($"{Current.displayName}: construction started - Crafting kin will build it, or tap BUILD there to help.");
                if (!free && !ItemSources.Has(Current.cost)) Cancel();
                return;
            }
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
            bm.StaffNew(b);   // idle camp kin that suit it start there
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
