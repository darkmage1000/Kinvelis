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

        /// <summary>Where this structure's storage slots sit in the camp's pool (BaseManager.Boxes). BoxLen 0 = it holds none.</summary>
        public int BoxStart, BoxLen;
        /// <summary>A Feed Box's own contents (null on anything else).</summary>
        public Inventory Feed;
        /// <summary>A Feed Box keeps itself stocked from camp storage unless the player switches that off.</summary>
        public bool FeedAuto = true;

        /// <summary>Crop Plots: what was planted (empty = the building's own output).</summary>
        public string Crop;
        public string OutputItem => !string.IsNullOrEmpty(Crop) ? Crop : Def.outputItem;
        public int OutputCount => !string.IsNullOrEmpty(Crop) && Crops.TryGet(Crop, out var c) ? c.count : Def.outputCount;
        public float WorkPerUnit => !string.IsNullOrEmpty(Crop) && Crops.TryGet(Crop, out var c) ? c.work : Def.workPerUnit;

        public bool IsStation => Def.station != CraftStation.Hand;
        public bool IsProducer => !string.IsNullOrEmpty(Def.outputItem);

        // ---- a producer makes a batch in stages (WorkStages): Progress is the work or the seconds put into the current one ----
        /// <summary>Which step of the batch this producer is on (index into Stages).</summary>
        public int Stage;
        public StageDef[] Stages { get; private set; }
        public StageDef Cur => Stages[Mathf.Clamp(Stage, 0, Stages.Length - 1)];
        /// <summary>Hands-off right now: growing, smelting, seasoning. Nobody can hurry it.</summary>
        public bool Waiting => IsProducer && Cur.handsOff;
        public int Batch => WorkStages.Batch(WorkPerUnit);
        public int BatchOutput => Batch * OutputCount;
        public int BatchInput => Batch * Def.inputCount;
        /// <summary>Work (hands-on) or seconds (hands-off) the current stage takes.</summary>
        public float StageNeed => StageNeedAt(Stage);
        public float StageNeedAt(int i)
        {
            var st = Stages[Mathf.Clamp(i, 0, Stages.Length - 1)];
            float whole = Batch * WorkPerUnit * st.share;
            return st.handsOff ? whole / Mathf.Max(0.25f, WorldRules.Current.workSpeed) : whole;
        }
        public float Stage01 => IsProducer ? Mathf.Clamp01(Progress / Mathf.Max(0.01f, StageNeed)) : 0f;
        public float SecondsLeft => Waiting ? Mathf.Max(0f, StageNeed - Progress) : 0f;
        /// <summary>A fresh batch has not been started (nothing sown, no ore loaded).</summary>
        public bool AtStart => Stage == 0 && Progress <= 0.001f;

        /// <summary>How good this kin is at a stage of this producer (0 = it can't do that step).</summary>
        public float StageLevelAt(int i, CreatureData d)
        {
            var st = Stages[Mathf.Clamp(i, 0, Stages.Length - 1)];
            if (st.handsOff) return 0f;
            float a = st.job != WorkType.None ? d.WorkLevel(st.job) : 0f;
            float b2 = st.alt != WorkType.None ? d.WorkLevel(st.alt) * st.altRate : 0f;
            return Mathf.Max(a, b2);
        }

        /// <summary>Work level this kin brings to whatever needs doing here now.</summary>
        public float WorkLevelFor(CreatureData d)
        {
            if (IsProducer) return StageLevelAt(Stage, d);
            return Mathf.Max(d.WorkLevel(Def.workType), Def.secondaryWorkType != WorkType.None ? d.WorkLevel(Def.secondaryWorkType) * 0.75f : 0f);
        }

        /// <summary>Why a new batch can't start, or null. (Room for the output, and the inputs in storage.)</summary>
        public string StartBlock()
        {
            if (BufferCount + BatchOutput > Def.bufferCap) return "Output full - collect it or add haulers";
            if (!string.IsNullOrEmpty(Def.inputItem) && BaseManager.I.Storage.Count(Def.inputItem) < BatchInput)
                return $"Needs {BatchInput} {ContentDB.ItemName(Def.inputItem)} in storage";
            return null;
        }

        /// <summary>What it is doing, for labels: "Growing 0:14", "Watering", "Needs planting".</summary>
        public string StageText(bool beingWorked)
        {
            if (!IsProducer) return "";
            if (Waiting) return $"{Cur.doing} {WorkStages.Clock(SecondsLeft)}";
            return beingWorked ? Cur.doing : Cur.waiting;
        }

        /// <summary>The top of the model, for a label floating over it.</summary>
        public float TopY { get; private set; } = 2f;

        public void Setup(BuildingDef def, string uid, Vector3 pos, float rotY)
        {
            Def = def;
            Uid = uid;
            RotY = rotY;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
            Stages = WorkStages.For(def);
            if (def.feeds) Feed = new Inventory(BaseManager.FeedSlots);
            bool plot = def.id == Crops.PlotId || def.id == "greenhouse";
            if (!meshCache.TryGetValue(def.id, out var mesh))
            {
                var parts = def.parts;
                // a plot's bed starts empty: its bushes are drawn by CropVisual as the crop really grows
                if (plot) parts = System.Array.FindAll(def.parts, p => p.shape != PartShape.Sphere);
                mesh = ModelBuilder.BuildStatic(parts, def.palette, "bld_" + def.id);
                meshCache[def.id] = mesh;
            }
            TopY = mesh.bounds.max.y;
            if (plot)
            {
                float soil = 0.25f;
                foreach (var p in def.parts) if (p.shape == PartShape.Box && p.pos.y < 0.3f && p.scale.x > 2f) soil = p.pos.y + p.scale.y * 0.5f;
                gameObject.AddComponent<CropVisual>().Init(this, soil);
                TopY = Mathf.Max(TopY, 1.1f);
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

        /// <summary>Is there something for this worker to do here right now? (For a producer: the current step is hands-on and this kin can do it.)</summary>
        public bool CanWork(CreatureData d)
        {
            var bm = BaseManager.I;
            if (IsProducer)
            {
                if (WorkLevelFor(d) <= 0f) return false;
                if (AtStart && StartBlock() != null) return false;
                return true;
            }
            if (Suitability(d) <= 0) return false;
            if (IsStation) return bm.HasOrderFor(Def.station);
            if (Def.id == "volt_pylon") return bm.PylonWantsKin;   // charged up: its kin is free until it runs down (BaseManager.Power)
            return Def.workType == WorkType.Charging || Def.workType == WorkType.Chilling || Def.workType == WorkType.Irrigating;
        }

        /// <summary>Work put into the current hands-on stage (by a kin, the player or a machine). Nothing happens during a hands-off one.</summary>
        public void AddWork(float w)
        {
            var bm = BaseManager.I;
            if (IsProducer)
            {
                if (Cur.handsOff) return;
                if (AtStart)
                {
                    if (StartBlock() != null) return;
                    if (!string.IsNullOrEmpty(Def.inputItem)) bm.Storage.Remove(Def.inputItem, BatchInput);  // the whole batch's inputs go in up front
                    Progress = 0.002f;
                }
                Progress += w;
                if (Progress >= StageNeed) NextStage();
            }
            else if (IsStation) bm.AddCraftWork(Def.station, w);
            else
            {
                PoweredUntil = GameManager.WorldTime + 2.0;
                if (Def.id == "volt_pylon") bm.AddCharge(w);   // the surplus is stored (BaseManager.Power)
            }
        }

        /// <summary>Time passing: hands-off stages run by themselves (near, far and while the game is closed).</summary>
        public void TickStage(float dt)
        {
            if (!IsProducer || !Cur.handsOff) return;
            Progress += dt;
            if (Progress >= StageNeed) NextStage();
        }

        void NextStage()
        {
            Progress = 0f;
            Stage++;
            pulse = 1f;
            if (Stage < Stages.Length) return;
            // the batch is done
            Stage = 0;
            BufferItem = OutputItem;
            BufferCount = Mathf.Min(Def.bufferCap, BufferCount + BatchOutput);
            if (Def.id == "forage_hut")
                for (int i = 0; i < Batch; i++) if (Random.value < 0.25f) BaseManager.I.Storage.Add("berry", 1);
        }

        /// <summary>The player working this producer by hand; inputs come from camp storage, then the bag. Returns why nothing happened (null = working).</summary>
        public string PlayerWork(float w, Inventory bag)
        {
            if (!IsProducer) return "INFO";
            if (Waiting) return "WAIT";                      // growing, smelting...: nothing for hands to do
            var bm = BaseManager.I;
            if (AtStart)
            {
                if (BufferCount + BatchOutput > Def.bufferCap) return "FULL";
                if (!string.IsNullOrEmpty(Def.inputItem))
                {
                    int need = BatchInput, stored = bm.Storage.Count(Def.inputItem);
                    if (stored + bag.Count(Def.inputItem) < need) return "NEED " + ContentDB.ItemName(Def.inputItem).ToUpper();
                    int fromStore = Mathf.Min(stored, need);
                    if (fromStore > 0) bm.Storage.Remove(Def.inputItem, fromStore);
                    if (fromStore < need) bag.Remove(Def.inputItem, need - fromStore);
                    GameEvents.RaiseInventoryChanged();
                }
                Progress = 0.002f;
            }
            if (w <= 0f) return null;
            Progress += w;                                   // (not AddWork: the inputs are already paid for, from the bag if need be)
            if (Progress >= StageNeed) NextStage();
            return null;
        }

        public float Progress01 => Stage01;

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
            // running by itself: a wisp of smoke, dust or steam now and then so you can see it is busy
            if (Def != null && IsProducer && Cur.handsOff && Stages.Length == 2)
            {
                puff -= Time.deltaTime;
                if (puff <= 0f)
                {
                    puff = Random.Range(0.9f, 1.6f);
                    var cam = Camera.main;
                    if (cam != null && (cam.transform.position - transform.position).sqrMagnitude < 45f * 45f)
                    {
                        Color c = Def.id == "smelter" ? new Color(0.5f, 0.42f, 0.4f) : Def.id == "fish_pond" ? new Color(0.6f, 0.85f, 1f)
                                : Def.id == "forage_hut" ? new Color(0.6f, 0.8f, 0.45f) : new Color(0.78f, 0.74f, 0.66f);
                        Fx.Puff(transform.position + new Vector3(Random.Range(-0.3f, 0.3f), TopY * 0.85f, Random.Range(-0.3f, 0.3f)), c, 0.3f, 1.1f);
                    }
                }
            }
        }

        float puff;
    }
}
