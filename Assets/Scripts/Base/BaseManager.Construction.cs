using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    [Serializable] public class SiteSave { public string defId, pieceId; public float x, y, z, rotY, done, depth; public int cx, cz, side, rot; }

    /// <summary>
    /// Construction (user, 2026-10-14: "crafting kin could be used to help build placed pieces like palworld"). Placing a
    /// camp structure or a house piece spends its materials and puts up a site: the model rises out of the ground
    /// inside a scaffold as work goes in, and only when it is finished does it become the real structure
    /// (`Place`) or piece (`Housing.Place`), so nothing else has to know about half-built things. Camp kin with the
    /// Crafting job (switched on) go and build (CreatureActor.Build); you can help by standing at a site and tapping
    /// BUILD (a toggle, like WORK; walking away stops it). Far away and while the game is closed, Crafting kin who
    /// are awake build too (`SimulateAbstract`). The REMOVE tool cancels a site and gives everything back. The
    /// Hearthstone, and worlds with free building, build at once. Saved as `SaveData.sites`.
    /// </summary>
    public partial class BaseManager
    {
        public class Site
        {
            public BuildingDef def;          // a camp structure ...
            public HousePieceDef piece;      // ... or a house piece
            public Housing.Spot spot;
            public Vector3 pos;
            public float rotY, need, done, radius;
            public GameObject go;
            public Transform model;
            public Obstacle obs;
            public bool finished;
            public string Name => def != null ? def.displayName : piece.name;
            public float Progress01 => need > 0f ? Mathf.Clamp01(done / need) : 1f;
        }

        public readonly List<Site> Sites = new List<Site>();
        /// <summary>The site you are building at (BUILD toggles it), or null.</summary>
        public Site PlayerBuildSite { get; private set; }
        public const float PlayerBuildRate = 1.2f;

        /// <summary>Work a structure needs: more for bigger costs.</summary>
        public static float BuildWork(Ingredient[] cost, bool piece)
        {
            int n = 0; if (cost != null) foreach (var c in cost) n += c.count;
            return piece ? Mathf.Clamp(2f + 0.25f * n, 2f, 14f) : Mathf.Clamp(5f + 0.35f * n, 5f, 45f);
        }

        /// <summary>Building happens over time (false = it goes up at once: the Hearthstone, free-build worlds).</summary>
        public static bool BuildsOverTime(BuildingDef def) => def == null || (!def.isCore && !WorldRules.Current.freeBuild);
        public static bool PieceBuildsOverTime => !WorldRules.Current.freeBuild;

        public Site StartSite(BuildingDef def, Vector3 pos, float rotY)
        {
            var s = new Site { def = def, pos = pos, rotY = rotY, need = BuildWork(def.cost, false), radius = def.footprint };
            MakeSiteVisual(s, ModelBuilder.BuildStatic(def.parts, def.palette, "site_" + def.id), RenderAssets.Building);
            s.obs = WorldCollision.AddObstacle(pos, def.footprint, s.go);
            Sites.Add(s);
            return s;
        }

        public Site StartSite(HousePieceDef piece, Housing.Spot spot)
        {
            var s = new Site { piece = piece, spot = spot, pos = spot.pos, rotY = spot.q.eulerAngles.y, need = BuildWork(piece.cost, true), radius = piece.kind == HouseKind.Furniture ? Mathf.Max(0.3f, piece.radius) : 1.4f };
            MakeSiteVisual(s, Housing.MeshFor(piece, Mathf.Max(0.3f, spot.depth)), RenderAssets.Building);
            Housing.I.Reserve(piece, spot);
            Sites.Add(s);
            return s;
        }

        static Mesh scaffold;

        void MakeSiteVisual(Site s, Mesh mesh, Material mat)
        {
            s.go = new GameObject("Site_" + s.Name);
            s.go.transform.SetParent(transform, false);
            s.go.transform.SetPositionAndRotation(s.pos, Quaternion.Euler(0f, s.rotY, 0f));
            var m = new GameObject("Model");
            m.transform.SetParent(s.go.transform, false);
            m.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = m.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off;
            s.model = m.transform;
            // a scaffold of poles round it
            if (scaffold == null)
            {
                var mb = new MeshBuilder();
                Color32 pole = new Color(0.62f, 0.48f, 0.3f), rope = new Color(0.85f, 0.75f, 0.5f);
                for (int i = 0; i < 4; i++)
                {
                    float x = i % 2 == 0 ? -0.5f : 0.5f, z = i < 2 ? -0.5f : 0.5f;
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(x, 0.5f, z), Quaternion.identity, new Vector3(0.05f, 1f, 0.05f)), pole, pole, 5, 1f, true);
                }
                for (int k = 0; k < 2; k++)
                {
                    float y = 0.35f + k * 0.45f;
                    mb.Box(Matrix4x4.TRS(new Vector3(0, y, -0.5f), Quaternion.identity, new Vector3(1f, 0.03f, 0.03f)), rope);
                    mb.Box(Matrix4x4.TRS(new Vector3(0, y, 0.5f), Quaternion.identity, new Vector3(1f, 0.03f, 0.03f)), rope);
                    mb.Box(Matrix4x4.TRS(new Vector3(-0.5f, y, 0), Quaternion.identity, new Vector3(0.03f, 0.03f, 1f)), rope);
                    mb.Box(Matrix4x4.TRS(new Vector3(0.5f, y, 0), Quaternion.identity, new Vector3(0.03f, 0.03f, 1f)), rope);
                }
                scaffold = mb.ToMesh("site_scaffold");
            }
            var b = mesh.bounds;
            var sc = new GameObject("Scaffold");
            sc.transform.SetParent(s.go.transform, false);
            sc.transform.localPosition = new Vector3(b.center.x, Mathf.Min(0f, b.min.y), b.center.z);
            sc.transform.localScale = new Vector3(Mathf.Max(0.8f, b.size.x + 0.4f), Mathf.Max(1.2f, b.max.y + 0.3f), Mathf.Max(0.8f, b.size.z + 0.4f));
            sc.AddComponent<MeshFilter>().sharedMesh = scaffold;
            var sr = sc.AddComponent<MeshRenderer>(); sr.sharedMaterial = RenderAssets.Building; sr.shadowCastingMode = ShadowCastingMode.Off;
            ShowProgress(s);
        }

        static void ShowProgress(Site s)
        {
            if (s.model == null) return;
            float k = 0.08f + 0.92f * s.Progress01;
            s.model.localScale = new Vector3(1f, k, 1f);   // it rises out of the ground as it is built
        }

        /// <summary>Work on a site (from a kin, the player or the camp's far simulation). Finishes it when done.</summary>
        public void AddSiteWork(Site s, float work)
        {
            if (s == null || s.finished) return;
            s.done += work;
            ShowProgress(s);
            if (s.done >= s.need) FinishSite(s);
        }

        void FinishSite(Site s)
        {
            s.finished = true;
            Sites.Remove(s);
            if (PlayerBuildSite == s) PlayerBuildSite = null;
            WorldCollision.RemoveObstacle(s.obs);
            if (s.go != null) Destroy(s.go);
            if (s.def != null)
            {
                var b = Place(s.def, s.pos, s.rotY);
                StaffNew(b);
                GameEvents.Good($"{s.def.displayName} is built!");
            }
            else
            {
                Housing.I.Unreserve(s.piece, s.spot);
                Housing.I.Place(s.piece, s.spot);
            }
            Fx.Burst(s.pos + Vector3.up * 0.6f, new Color(0.95f, 0.85f, 0.6f), 20, 4.5f, 0.35f, 0.7f);
            SynthAudio.Play(Sfx.Build, 1.05f, 0.8f);
        }

        /// <summary>A new structure takes idle camp kin that suit it.</summary>
        public void StaffNew(Building b)
        {
            if (b == null) return;
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
            {
                if (b.WorkerSlots <= 0 || b.AssignedCount >= b.WorkerSlots) break;
                if (d.assignedBuilding == null && b.Suitability(d) > 0) d.assignedBuilding = b.Uid;
            }
        }

        /// <summary>REMOVE on a site: it comes down and every material goes back to your bag.</summary>
        public void CancelSite(Site s)
        {
            if (s == null || s.finished) return;
            s.finished = true;
            Sites.Remove(s);
            if (PlayerBuildSite == s) PlayerBuildSite = null;
            WorldCollision.RemoveObstacle(s.obs);
            if (s.piece != null) Housing.I.Unreserve(s.piece, s.spot);
            if (s.go != null) Destroy(s.go);
            var cost = s.def != null ? s.def.cost : s.piece.cost;
            if (!WorldRules.Current.freeBuild) foreach (var c in cost) { int left = GameManager.I.Player.Stats.Bag.Add(c.itemId, c.count); if (left > 0) Storage.Add(c.itemId, left); }
            GameEvents.Say($"{s.Name} taken down - materials back.");
            GameEvents.RaiseInventoryChanged();
        }

        public Site SiteNear(Vector3 p, float reach)
        {
            Site best = null; float bd = reach;
            foreach (var s in Sites)
            {
                Vector3 d = s.pos - p; d.y = 0f;
                float dist = d.magnitude - s.radius;
                if (dist < bd) { bd = dist; best = s; }
            }
            return best;
        }

        /// <summary>The site a Crafting kin should go and build: the nearest.</summary>
        public Site SiteFor(Vector3 p)
        {
            Site best = null; float bd = float.MaxValue;
            foreach (var s in Sites) { float d = (s.pos - p).sqrMagnitude; if (d < bd) { bd = d; best = s; } }
            return best;
        }

        /// <summary>How fast a kin builds: its Crafting level (at least 1) and its work multipliers.</summary>
        public static float BuildRate(CreatureData d) => Mathf.Max(1, d.JobLevel(WorkType.Crafting)) * d.WorkMul * Dev.WorkMul * WorldRules.Current.workSpeed * CheerMul(d);

        public static bool CanBuild(CreatureData d) => d != null && d.JobOn(WorkType.Crafting) && !d.Fainted;

        /// <summary>BUILD at a site: start or stop helping.</summary>
        public void TogglePlayerBuild(Site s)
        {
            PlayerBuildSite = PlayerBuildSite == s ? null : s;
            if (PlayerBuildSite != null) SynthAudio.Play(Sfx.Chop, 1.1f, 0.6f);
        }

        float playerBuildFx;
        void TickPlayerBuild(float dt)
        {
            var s = PlayerBuildSite;
            if (s == null) return;
            var pl = GameManager.I.Player;
            Vector3 d = s.pos - pl.Position; d.y = 0f;
            if (s.finished || d.magnitude - s.radius > 3f || !pl.IsAlive) { PlayerBuildSite = null; return; }
            playerBuildFx -= dt;
            if (playerBuildFx <= 0f) { playerBuildFx = 0.45f; SynthAudio.Play(Sfx.Chop, UnityEngine.Random.Range(1.0f, 1.25f), 0.5f); Fx.Puff(s.pos + Vector3.up * 0.4f, new Color(0.85f, 0.75f, 0.55f), 0.25f, 0.6f); }
            AddSiteWork(s, PlayerBuildRate * Dev.WorkMul * dt);
        }

        /// <summary>Far away or closed: awake Crafting kin build the nearest site (called from SimulateAbstract).</summary>
        void SimulateSites(float dt)
        {
            if (Sites.Count == 0) return;
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
            {
                if (Sites.Count == 0) return;
                if (!CanBuild(d) || Asleep(d)) continue;
                AddSiteWork(Sites[0], BuildRate(d) * dt);
            }
        }

        // ============================================================ SAVE
        public List<SiteSave> SaveSites()
        {
            var l = new List<SiteSave>();
            foreach (var s in Sites)
                l.Add(new SiteSave { defId = s.def != null ? s.def.id : null, pieceId = s.piece != null ? s.piece.id : null, x = s.pos.x, y = s.pos.y, z = s.pos.z, rotY = s.rotY, done = s.done,
                    cx = s.spot.cx, cz = s.spot.cz, side = s.spot.side, rot = s.spot.rot, depth = s.spot.depth });
            return l;
        }

        public void LoadSites(List<SiteSave> list)
        {
            if (list == null) return;
            foreach (var v in list)
            {
                Site s = null;
                if (!string.IsNullOrEmpty(v.defId)) { var def = ContentDB.Building(v.defId); if (def != null) s = StartSite(def, new Vector3(v.x, v.y, v.z), v.rotY); }
                else
                {
                    var piece = Housing.Def(v.pieceId);
                    if (piece == null) continue;
                    var spot = new Housing.Spot { ok = true, cx = v.cx, cz = v.cz, side = v.side, rot = v.rot, pos = new Vector3(v.x, v.y, v.z), depth = v.depth,
                        q = Quaternion.Euler(0f, v.rotY, 0f) };
                    s = StartSite(piece, spot);
                }
                if (s != null) { s.done = Mathf.Min(v.done, s.need * 0.99f); ShowProgress(s); }
            }
        }
    }
}
