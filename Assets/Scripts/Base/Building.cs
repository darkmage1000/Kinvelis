using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>A placed base structure. Produces into a small output buffer that haulers carry to storage.</summary>
    public class Building : MonoBehaviour
    {
        public string Uid { get; private set; }
        public BuildingDef Def { get; private set; }
        public float Progress;
        public string BufferItem;
        public int BufferCount;
        public double PoweredUntil;   // dynamo / larder: world time while staffed
        public float RotY { get; private set; }

        Obstacle obstacle;
        static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
        Transform spin;
        float pulse;

        /// <summary>Crop Plots: what was planted (empty = the building's own output).</summary>
        public string Crop;
        public string OutputItem => !string.IsNullOrEmpty(Crop) ? Crop : Def.outputItem;
        public int OutputCount => !string.IsNullOrEmpty(Crop) && Crops.TryGet(Crop, out var c) ? c.count : Def.outputCount;
        public float WorkPerUnit => !string.IsNullOrEmpty(Crop) && Crops.TryGet(Crop, out var c) ? c.work : Def.workPerUnit;

        public bool IsStation => Def.station != CraftStation.Hand;
        public bool IsProducer => !string.IsNullOrEmpty(Def.outputItem);

        public void Setup(BuildingDef def, string uid, Vector3 pos, float rotY)
        {
            Def = def;
            Uid = uid;
            RotY = rotY;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
            if (!meshCache.TryGetValue(def.id, out var mesh))
            {
                mesh = ModelBuilder.BuildStatic(def.parts, def.palette, "bld_" + def.id);
                meshCache[def.id] = mesh;
            }
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = QualityProfile.Current.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            obstacle = WorldCollision.AddObstacle(pos, def.footprint, this);
            if (def.isCore)
            {
                var lg = new GameObject("CoreLight");
                lg.transform.SetParent(transform, false);
                lg.transform.localPosition = Vector3.up * 2.2f;
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point; l.range = 12f; l.intensity = 1.6f; l.color = def.palette.glow; l.shadows = LightShadows.None;
            }
        }

        public void Remove()
        {
            WorldCollision.RemoveObstacle(obstacle);
            Destroy(gameObject);
        }

        public Vector3 WorkSpot(int slot)
        {
            float a = (RotY + 180f + slot * 70f - 35f) * Mathf.Deg2Rad;
            float r = Def.footprint + 0.9f;
            var p = transform.position + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r;
            p.y = WorldGen.Height(p.x, p.z);
            return p;
        }

        public int SlotOf(string uid)
        {
            int i = 0;
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
            {
                if (d.assignedBuilding != Uid) continue;
                if (d.uid == uid) return i;
                i++;
            }
            return 0;
        }

        public int AssignedCount
        {
            get
            {
                int n = 0;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (d.assignedBuilding == Uid) n++;
                return n;
            }
        }

        public int Suitability(CreatureData d)
        {
            if (Def.workType == WorkType.None) return 0;
            int a = d.WorkLevel(Def.workType);
            int b = Def.secondaryWorkType != WorkType.None ? d.WorkLevel(Def.secondaryWorkType) : 0;
            return Mathf.Max(a * 4, b * 3);
        }

        /// <summary>Is there something for this worker to do here right now?</summary>
        public bool CanWork(CreatureData d)
        {
            if (Suitability(d) <= 0) return false;
            var bm = BaseManager.I;
            if (IsProducer)
            {
                if (BufferCount >= Def.bufferCap) return false;
                if (!string.IsNullOrEmpty(Def.inputItem) && Progress <= 0.001f && bm.Storage.Count(Def.inputItem) < Def.inputCount) return false;
                return true;
            }
            if (IsStation) return bm.HasOrderFor(Def.station);
            return Def.workType == WorkType.Charging || Def.workType == WorkType.Chilling;
        }

        public void AddWork(float w)
        {
            var bm = BaseManager.I;
            if (IsProducer)
            {
                if (!string.IsNullOrEmpty(Def.inputItem) && Progress <= 0.001f)
                {
                    if (bm.Storage.Count(Def.inputItem) < Def.inputCount) return;
                    bm.Storage.Remove(Def.inputItem, Def.inputCount);  // consume up-front when a batch starts
                    Progress = 0.002f;
                }
                Progress += w;
                if (Progress >= WorkPerUnit)
                {
                    Progress = 0f;
                    BufferItem = OutputItem;
                    BufferCount = Mathf.Min(Def.bufferCap, BufferCount + OutputCount);
                    if (Def.id == "forage_hut" && Random.value < 0.25f) bm.Storage.Add("berry", 1);
                    pulse = 1f;
                }
            }
            else if (IsStation) bm.AddCraftWork(Def.station, w);
            else PoweredUntil = GameManager.WorldTime + 2.0;
        }

        /// <summary>The player working this producer by hand; inputs come from camp storage, then the bag. Returns why nothing happened (null = working).</summary>
        public string PlayerWork(float w, Inventory bag)
        {
            if (!IsProducer) return "INFO";
            if (BufferCount >= Def.bufferCap) return "FULL";
            var bm = BaseManager.I;
            if (!string.IsNullOrEmpty(Def.inputItem) && Progress <= 0.001f)
            {
                int stored = bm.Storage.Count(Def.inputItem);
                if (stored + bag.Count(Def.inputItem) < Def.inputCount) return "NEED " + ContentDB.ItemName(Def.inputItem).ToUpper();
                int fromStore = Mathf.Min(stored, Def.inputCount);
                if (fromStore > 0) bm.Storage.Remove(Def.inputItem, fromStore);
                if (fromStore < Def.inputCount) bag.Remove(Def.inputItem, Def.inputCount - fromStore);
                Progress = 0.002f;
                GameEvents.RaiseInventoryChanged();
            }
            AddWork(w);
            return null;
        }

        public float Progress01 => WorkPerUnit > 0f ? Mathf.Clamp01(Progress / WorkPerUnit) : 0f;

        public int TakeFromBuffer(int max, out string item)
        {
            item = BufferItem;
            int n = Mathf.Min(max, BufferCount);
            BufferCount -= n;
            return n;
        }

        public void ReturnToBuffer(string item, int n)
        {
            BufferItem = item;
            BufferCount += n;
        }

        public bool Powered => GameManager.WorldTime < PoweredUntil;

        int tierShown = 1;
        GameObject tierGo;
        static readonly Dictionary<int, Mesh> tierMeshes = new Dictionary<int, Mesh>();

        /// <summary>Hearthstone only: rings of crystal, a halo and a taller spire as the camp levels up.</summary>
        void ShowTier(int level)
        {
            tierShown = level;
            if (tierGo != null) Destroy(tierGo);
            tierGo = null;
            if (level <= 1) return;
            if (!tierMeshes.TryGetValue(level, out var mesh))
            {
                var mb = new MeshBuilder();
                Color32 glow = Def.palette.glow, stone = Def.palette.primary, gold = new Color(1f, 0.82f, 0.35f);
                Color32 tip = Color.Lerp(Def.palette.glow, Color.white, 0.6f);
                for (int ring = 1; ring < level; ring++)
                {
                    int n = 3 + ring;
                    float r = 1.25f + ring * 0.3f, h = 0.7f + ring * 0.28f;
                    for (int i = 0; i < n; i++)
                    {
                        float a = (i + ring * 0.5f) / n * Mathf.PI * 2f;
                        var p = new Vector3(Mathf.Cos(a) * r, 0.25f, Mathf.Sin(a) * r);
                        mb.Crystal(Matrix4x4.TRS(p, Quaternion.Euler(Mathf.Sin(a) * -14f, 0, Mathf.Cos(a) * 14f), new Vector3(0.3f, h, 0.3f)), glow, tip);
                    }
                }
                if (level >= 3) mb.Crystal(Matrix4x4.TRS(new Vector3(0, 2.2f, 0), Quaternion.identity, new Vector3(0.7f, 1.2f + level * 0.5f, 0.7f)), glow, tip);
                if (level >= 4)
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i / 4f * Mathf.PI * 2f + 0.4f;
                        mb.Cylinder(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 2.5f, 1.1f, Mathf.Sin(a) * 2.5f), Quaternion.identity, new Vector3(0.45f, 2.2f, 0.45f)), stone, stone, 6, 0.8f, true);
                        mb.Sphere(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 2.5f, 2.45f, Mathf.Sin(a) * 2.5f), Quaternion.identity, Vector3.one * 0.5f), level >= 6 ? gold : glow, 6, 5);
                    }
                if (level >= 5)
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        mb.Sphere(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 1.1f, 4.4f + level * 0.5f, Mathf.Sin(a) * 1.1f), Quaternion.identity, Vector3.one * 0.22f), level >= 6 ? gold : glow, 6, 4);
                    }
                mesh = mb.ToMesh("hearth_tier_" + level);
                tierMeshes[level] = mesh;
            }
            tierGo = new GameObject("Tier");
            tierGo.transform.SetParent(transform, false);
            tierGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = tierGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            var l = GetComponentInChildren<Light>();
            if (l != null) { l.range = 12f + level * 3f; l.intensity = 1.6f + level * 0.25f; }
        }

        void Update()
        {
            if (Def != null && Def.isCore && BaseManager.I != null && BaseManager.I.CoreLevel != tierShown) ShowTier(BaseManager.I.CoreLevel);
            if (pulse > 0f)
            {
                pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
                float s = 1f + Mathf.Sin(pulse * Mathf.PI) * 0.06f;
                transform.localScale = new Vector3(s, 1f / s, s);
            }
        }
    }
}
