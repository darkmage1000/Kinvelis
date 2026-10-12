using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    [Serializable]
    public class CraftOrder
    {
        public string recipeId;
        public int remaining = 1;
        public float progress;
    }

    /// <summary>
    /// The player's camp: Hearthstone core, buildings, shared storage, worker kin and craft orders.
    /// Near the player: workers are real actors walking between stations.
    /// Far away / offline: production is simulated abstractly with the same rates (capped sim range).
    /// </summary>
    public partial class BaseManager : MonoBehaviour
    {
        public static BaseManager I { get; private set; }

        [SerializeField] float actorRange = 120f;
        [SerializeField] float baseRadius = 28f;
        [SerializeField] float radiusPerLevel = 8f;
        [SerializeField] float hungerPerSecond = 100f / (18f * 60f);
        [SerializeField] float playerCraftAssist = 1.5f;
        [Tooltip("Work per second the player adds to a producer (Smelter, Sawmill...) after choosing WORK on it. A Lv1 worker kin is about 1.")]
        [SerializeField] float playerWorkRate = 0.5f;
        const float PlayerWorkReach = 3f;
        /// <summary>Producer the player chose to work by hand (null = none). Ends when they tap again, walk off or run out of inputs.</summary>
        public Building PlayerWorkTarget { get; private set; }
        /// <summary>Why hand-work on PlayerWorkTarget is stalled (null = working).</summary>
        public string PlayerWorkBlock { get; private set; }
        [SerializeField] float abstractHaulPerHauler = 1.5f;

        public bool HasBase => Core != null;
        public Building Core { get; private set; }
        public Vector3 CorePosition => Core != null ? Core.transform.position : Vector3.zero;
        public int CoreLevel = 1;
        public const int MaxCoreLevel = 6;
        const int StoragePerCoreLevel = 8;
        public float Radius => baseRadius + (CoreLevel - 1) * radiusPerLevel;
        public int WorkerCap => Mathf.Max(1, WorldRules.Current.campWorkersBase + (CoreLevel - 1) * WorldRules.Current.campWorkersPerLevel);

        public readonly List<Building> Buildings = new List<Building>();
        public readonly Inventory Storage = new Inventory(0);
        public readonly List<CraftOrder> Orders = new List<CraftOrder>();

        readonly Dictionary<string, CreatureActor> workerActors = new Dictionary<string, CreatureActor>();
        public IEnumerable<CreatureActor> WorkerActors => workerActors.Values;
        /// <summary>Time.time of the player's last return to the camp ring after a while away (-99 = never).</summary>
        public float PlayerArrival { get; private set; } = -99f;
        bool playerInRing;
        float playerLeftAt = -999f;
        bool actorsOut;
        float assistTimer;

        public void Init() { I = this; }

        // ============================================================ QUERIES
        public bool IsInsideBase(Vector3 p, float margin)
        {
            if (!HasBase) return false;
            Vector3 d = p - CorePosition; d.y = 0;
            return d.magnitude <= Radius + margin;
        }

        public Building FindBuilding(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            for (int i = 0; i < Buildings.Count; i++) if (Buildings[i].Uid == uid) return Buildings[i];
            return null;
        }

        public Building FeedBox
        {
            get { foreach (var b in Buildings) if (b.Def.feeds) return b; return null; }
        }

        public bool HasFeedBox => FeedBox != null;
        /// <summary>Is there anything for camp kin to eat: food in a Feed Box, or in storage for a box that stocks itself (or, with no box at all, for hand-feeding)?</summary>
        public bool HasFood
        {
            get
            {
                bool any = false, auto = false;
                foreach (var fb in FeedBoxes()) { any = true; if (!fb.Feed.IsEmpty) return true; auto |= fb.FeedAuto; }
                return (auto || !any) && StorageHasFood;
            }
        }

        public bool HasBeds
        {
            get { int beds = 0; foreach (var b in Buildings) beds += b.Def.beds; return beds > 0; }
        }

        public float RestedMul
        {
            get
            {
                int beds = 0;
                foreach (var b in Buildings) beds += b.Def.beds;
                return beds >= GameManager.I.Party.CountIn(KinLocation.Base) && beds > 0 ? 1.15f : 1f;
            }
        }

        public float DynamoBoost
        {
            get
            {
                float b = 0f;
                foreach (var x in Buildings) if (x.Def.boostPercent > 0f && x.Powered) b += x.Def.boostPercent / 100f;
                return b;
            }
        }

        /// <summary>A kin is chilling a Frost Larder, or there is stored cold left (BaseManager.Power).</summary>
        public bool LarderActive => LarderStaffed || (Cold > 0f && ColdCapacity > 0f);

        public bool HasStation(CraftStation s)
        {
            if (s == CraftStation.Hand) return true;
            foreach (var b in Buildings) if (b.Def.station == s) return true;
            return false;
        }

        public Building NearestStorage(Vector3 p)
        {
            Building best = null; float bd = float.MaxValue;
            foreach (var b in Buildings)
            {
                if (b.Def.storageSlots <= 0) continue;
                float d = (b.transform.position - p).sqrMagnitude;
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }

        public Building FindHaulSource()
        {
            Building best = null; int most = 0;
            foreach (var b in Buildings) if (b.BufferCount > most) { most = b.BufferCount; best = b; }
            return best;
        }

        public Building NearestInteractable(Vector3 p, float extra)
        {
            Building best = null; float bd = float.MaxValue;
            foreach (var b in Buildings)
            {
                Vector3 d = b.transform.position - p; d.y = 0;
                float dist = d.magnitude - b.Def.footprint;
                if (dist < extra && dist < bd) { bd = dist; best = b; }
            }
            return best;
        }

        /// <summary>True if a resource node at pos would overlap a building (keeps trees from regrowing inside camp).</summary>
        public bool BlocksNode(Vector3 pos, float r)
        {
            foreach (var b in Buildings)
            {
                Vector3 d = b.transform.position - pos; d.y = 0;
                if (d.magnitude < b.Def.footprint + r + 0.5f) return true;
            }
            return false;
        }

        // ============================================================ WORK RATES
        public float WorkRate(CreatureData d, Building b)
        {
            float lvl = b.WorkLevelFor(d);   // for a producer: at the step it is on (planting, watering, harvesting...)
            float hungerMul = d.hunger > 0f ? 1f : 0.4f;
            float levelMul = (1f + 0.02f * (d.level - 1)) * WorldRules.Current.workSpeed;   // experienced workers are a bit faster
            return lvl * d.WorkMul * hungerMul * levelMul * (1f + DynamoBoost) * RestedMul * GameManager.I.Player.Stats.WorkBonus * HearthlinkMul * IrrigationMul(b);
        }

        /// <summary>Hearthlink Pendant: +20% work speed while the player is away from camp.</summary>
        public float HearthlinkMul
        {
            get
            {
                var pl = GameManager.I.Player;
                return pl != null && pl.Stats.HasEffect(UniqueEffect.Hearthlink) && !IsInsideBase(pl.Position, 10f) ? 1.2f : 1f;
            }
        }

        /// <summary>Camp workers slowly gain XP while they actually work.</summary>
        public void AccrueWorkXp(CreatureData d, float dt)
        {
            var gm = GameManager.I;
            d.workXpTimer += dt * (HasBuilding("training_yard") ? 2f : 1f);
            while (d.workXpTimer >= gm.WorkXpInterval)
            {
                d.workXpTimer -= gm.WorkXpInterval;
                gm.GrantKinXp(d, gm.WorkXpAmount, true);
            }
        }

        // ============================================================ TASKS
        /// <summary>
        /// What a camp kin should do next: the structure where its hands count for most right now. Its own structure
        /// is favoured (and so is whatever it is already doing, so it doesn't dither), but a farmer who could only
        /// water its own plot slowly will go and plant the empty one next door and leave the watering to a kin made
        /// for it. While its structure is growing, smelting or has nothing for it, it helps wherever its jobs fit and
        /// there is room (a structure takes as many hands at once as it has worker places). Null = nothing needs it.
        /// </summary>
        public Building PickTask(CreatureData d, Building home, CreatureActor self = null, Building current = null)
        {
            // a kin given a post (Dynamo, Frost Larder, Sprinkler) holds it: the machine stops the moment it walks off.
            // A Volt Pylon stores charge, so it only "can be worked" while it wants topping up - then its kin goes straight back.
            if (IsPost(home) && home.CanWork(d) && (home == current || HandsAt(home, self) < home.Def.workerSlots)) return home;
            Building best = null; float bestScore = 0f;
            foreach (var b in Buildings)
            {
                if (b == null || b.Def.isCore || b.Def.workType == WorkType.Hauling || b.Def.workerSlots <= 0) continue;
                if (!b.CanWork(d)) continue;
                if (b != current && HandsAt(b, self) >= b.Def.workerSlots) continue;
                float score = TaskScore(d, b, home) * (b == current ? 1.3f : 1f);
                if (self != null) score /= 1f + Vector3.Distance(self.transform.position, b.transform.position) * 0.03f;
                if (score > bestScore) { bestScore = score; best = b; }
            }
            return best;
        }

        /// <summary>Is there anything at all for this camp kin to do: a step somewhere, or output to carry? (Ends a stroll, a nap or a game.)</summary>
        public bool WantsHands(CreatureData d, Building home, CreatureActor self)
        {
            if (PickTask(d, home, self) != null) return true;
            bool hauler = home != null && (home.Def.isCore || home.Def.workType == WorkType.Hauling);
            return (hauler || d.WorkLevel(WorkType.Hauling) > 0) && FindHaulSource() != null;
        }

        /// <summary>A machine that only runs while a kin stays at it: its assigned kin never leaves for other work (anyone else only minds it when idle).</summary>
        public static bool IsPost(Building b) =>
            b != null && !b.IsProducer && !b.IsStation && !b.Def.isCore && b.Def.workType != WorkType.Hauling && b.Def.workerSlots > 0;

        /// <summary>Night, the camp has no power, and the kin who could run the pylon are in bed.</summary>
        public bool PowerAsleep
        {
            get
            {
                if (!NightNow || CampPowered || !HasBuilding("volt_pylon")) return false;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                    if (!d.Fainted && d.WorkLevel(WorkType.Charging) > 0 && Asleep(d)) return true;
                return false;
            }
        }

        /// <summary>Is anyone in camp (awake, if asked) able to work this station?</summary>
        public bool AnyoneSuits(Building b, bool awakeOnly = false)
        {
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                if (!d.Fainted && b.Suitability(d) > 0 && !(awakeOnly && Asleep(d))) return true;
            return false;
        }

        /// <summary>A producer waiting on a step beats keeping a machine company; then whoever is best at it; its own structure first among equals.</summary>
        float TaskScore(CreatureData d, Building b, Building home) =>
            WorkRate(d, b) * (b.IsProducer ? 1.5f : b.IsStation ? 1.2f : b.Def.id == "volt_pylon" ? 2f : IsLarder(b) ? 1.6f : 0.6f) * (b == home ? 1.35f : 1f);   // a pylon asking for charge comes first: everything powered hangs on it

        /// <summary>How many camp kin are at (or on their way to) a structure right now.</summary>
        public int HandsAt(Building b, CreatureActor except = null)
        {
            int n = 0;
            foreach (var a in workerActors.Values) if (a != null && a != except && a.CampTask == b) n++;
            return n;
        }

        /// <summary>Is anyone in camp able to do the step this producer is waiting on?</summary>
        public bool AnyoneCan(Building b, int stage, bool awakeOnly = false)
        {
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                if (!d.Fainted && b.StageLevelAt(stage, d) > 0f && !(awakeOnly && Asleep(d))) return true;
            return false;
        }

        const float SprinklerWater = 1.5f;   // work per second a running sprinkler puts into a plot that needs water

        /// <summary>A Sprinkler is running (staffed by an Irrigating kin, or the camp is powered).</summary>
        public bool SprinklerRunning
        {
            get { foreach (var b in Buildings) if (b.Def.id == "sprinkler" && (b.Powered || CampPowered)) return true; return false; }
        }

        /// <summary>Hands-off stages tick, sprinklers water, greenhouses tend themselves. Runs near, far and offline.</summary>
        void TickStages(float dt)
        {
            bool sprinkler = SprinklerRunning;
            float speed = WorldRules.Current.workSpeed;
            foreach (var b in Buildings)
            {
                if (!b.IsProducer) continue;
                b.TickStage(dt);
                if (!Crops.IsPlot(b) || b.Waiting) continue;
                if (b.Def.id == "greenhouse") b.AddWork(GreenhouseRate * speed * dt);
                if (sprinkler && b.Stage == WorkStages.CropWater) b.AddWork(SprinklerWater * speed * dt);
            }
        }

        // ============================================================ BUILD
        public bool CanPlace(BuildingDef def, Vector3 pos, out string reason)
        {
            reason = null;
            if (Settlements.Inside(pos, 16f)) { reason = "Too close to a settlement"; return false; }
            if (def.isCore)
            {
                if (HasBase) { reason = "You already have a Hearthstone"; return false; }
            }
            else
            {
                if (!HasBase) { reason = "Place a Hearthstone first"; return false; }
                if (!IsInsideBase(pos, -def.footprint)) { reason = "Must be inside your camp"; return false; }
            }
            float h = WorldGen.Height(pos.x, pos.z);
            if (h < 0.4f) { reason = "Too wet here"; return false; }
            float f = def.footprint;
            float h1 = WorldGen.Height(pos.x + f, pos.z), h2 = WorldGen.Height(pos.x - f, pos.z);
            float h3 = WorldGen.Height(pos.x, pos.z + f), h4 = WorldGen.Height(pos.x, pos.z - f);
            float spread = Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(h3, h4)) - Mathf.Min(Mathf.Min(h1, h2), Mathf.Min(h3, h4));
            if (spread > 1.2f + f * 0.25f) { reason = "Ground too steep"; return false; }
            if (WorldCollision.Overlaps(pos, f * 0.9f, false)) { reason = "Blocked by another structure"; return false; }
            if (Landmarks.NearLandmark(pos, 18f)) { reason = "Too close to a landmark"; return false; }
            if (!WorldRules.Current.freeBuild && ContentDB.Building(def.id).unlockLevel > GameManager.I.Player.Stats.level) { reason = $"Requires level {def.unlockLevel}"; return false; }
            if (!Tech.BuildingUnlocked(def.id)) { reason = $"Research {Tech.ForBuilding(def.id).name} first"; return false; }
            return true;
        }

        public Building Place(BuildingDef def, Vector3 pos, float rotY, string uid = null)
        {
            pos.y = WorldGen.Height(pos.x, pos.z) - 0.05f;
            var go = new GameObject("Bld_" + def.id);
            go.transform.SetParent(transform, false);
            var b = go.AddComponent<Building>();
            b.Setup(def, uid ?? CreatureData.NewUid(), pos, rotY);
            Buildings.Add(b);
            if (def.isCore) Core = b;
            RecalcStorage();
            ClearNodesUnder(pos, def.footprint);
            GameEvents.RaiseBaseChanged();
            return b;
        }

        void ClearNodesUnder(Vector3 pos, float r)
        {
            var cm = ChunkManager.I;
            if (cm == null) return;
            var buf = new List<ResourceNode>();
            cm.Nodes.Query(pos, r + 3f, buf);
            foreach (var n in buf)
            {
                if (!n.alive) continue;
                Vector3 d = n.pos - pos; d.y = 0;
                if (d.magnitude < r + n.Radius + 0.5f) cm.Deplete(n);
            }
        }

        public void Demolish(Building b)
        {
            if (b.Def.isCore) { GameEvents.Warn("The Hearthstone can't be demolished."); return; }
            foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (d.assignedBuilding == b.Uid) d.assignedBuilding = null;
            // what it held (a chest's slots, a feed box's food) and half its cost go back into the camp
            var spill = new List<ItemStack>();
            RemoveBox(b, spill);
            foreach (var c in b.Def.cost) if (c.count / 2 > 0) spill.Add(new ItemStack { id = c.itemId, count = c.count / 2 });
            if (b.BufferCount > 0) spill.Add(new ItemStack { id = b.BufferItem, count = b.BufferCount });
            Buildings.Remove(b);
            b.Remove();
            RecalcStorage();
            Respill(spill);
            GameEvents.RaiseBaseChanged();
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>Levels 2-3 use island materials; 4-6 need outer-land bars and Warden Hearts.</summary>
        public static Ingredient[] UpgradeCost(int toLevel)
        {
            switch (toLevel)
            {
                case 2: return new[] { new Ingredient("stone", 30), new Ingredient("wood", 30), new Ingredient("ingot", 5) };
                case 3: return new[] { new Ingredient("ingot", 15), new Ingredient("crystal", 5), new Ingredient("cinder", 5) };
                case 4: return new[] { new Ingredient("bogsteel_bar", 8), new Ingredient("stone", 60), new Ingredient(Wardens.HeartItem, 2) };
                case 5: return new[] { new Ingredient("sunsteel_bar", 10), new Ingredient("crystal", 15), new Ingredient(Wardens.HeartItem, 4) };
                default: return new[] { new Ingredient("starsteel_bar", 10), new Ingredient("blightsteel_bar", 10), new Ingredient(Wardens.HeartItem, 6) };
            }
        }

        public bool TryUpgradeCore()
        {
            if (!HasBase || CoreLevel >= MaxCoreLevel) return false;
            var cost = UpgradeCost(CoreLevel + 1);
            if (!ItemSources.Has(cost)) { GameEvents.Warn("Not enough materials."); return false; }
            ItemSources.Consume(cost);
            CoreLevel++;
            RecalcStorage();
            Fx.Sparkle(CorePosition + Vector3.up * 2f, new Color(0.6f, 1f, 1f), 40, 2f);
            SynthAudio.Play(Sfx.LevelUp);
            GameEvents.Good($"Hearthstone upgraded to level {CoreLevel}! Camp radius {Radius:0}m, {WorkerCap} workers, +{StoragePerCoreLevel} storage.");
            GameEvents.RaiseBaseChanged();
            return true;
        }

        // ============================================================ WORKERS
        public void AddWorker(CreatureData d)
        {
            d.assignedBuilding = null;
            if (d.hunger <= 0f) d.hunger = 60f;
            AutoAssignOne(d);
            if (actorsOut) SpawnWorkerActor(d);
            GameEvents.RaiseBaseChanged();
        }

        public void RemoveWorker(CreatureData d)
        {
            d.assignedBuilding = null;
            if (workerActors.TryGetValue(d.uid, out var a)) { workerActors.Remove(d.uid); a.Despawn(); }
            GameEvents.RaiseBaseChanged();
        }

        public bool Assign(CreatureData d, Building b)
        {
            if (b == null) { d.assignedBuilding = null; GameEvents.RaiseBaseChanged(); return true; }
            if (b.Suitability(d) <= 0) { GameEvents.Warn($"{d.DisplayName} can't do {b.Def.workType} work."); return false; }
            if (b.AssignedCount >= b.Def.workerSlots && d.assignedBuilding != b.Uid) { GameEvents.Warn($"{b.Def.displayName} is fully staffed."); return false; }
            d.assignedBuilding = b.Uid;
            GameEvents.RaiseBaseChanged();
            return true;
        }

        void AutoAssignOne(CreatureData d)
        {
            Building best = null; int bestScore = 0;
            foreach (var b in Buildings)
            {
                if (b.Def.workerSlots <= 0 || b.AssignedCount >= b.Def.workerSlots) continue;
                int s = b.Suitability(d);
                if (s > bestScore) { bestScore = s; best = b; }
            }
            if (best != null) d.assignedBuilding = best.Uid;
        }

        public void AutoAssignAll()
        {
            foreach (var d in GameManager.I.Party.In(KinLocation.Base)) d.assignedBuilding = null;
            // best specialists first
            var list = new List<CreatureData>(GameManager.I.Party.In(KinLocation.Base));
            list.Sort((a, b) => MaxWork(b).CompareTo(MaxWork(a)));
            foreach (var d in list) AutoAssignOne(d);
            GameEvents.RaiseBaseChanged();
            GameEvents.Good("Workers auto-assigned.");
        }

        static int MaxWork(CreatureData d)
        {
            int m = 0;
            foreach (var w in d.Species.work) m = Mathf.Max(m, w.level);
            return m;
        }

        public bool EatFood(CreatureData d)
        {
            // from a Feed Box: any plain crop will do, cheapest first (tarts last). A box that stocks itself is topped up first.
            bool anyBox = false;
            foreach (var fb in FeedBoxes()) { anyBox = true; if (fb.Feed.IsEmpty) Restock(fb); }
            foreach (var id in Crops.KinFoods)
                foreach (var fb in FeedBoxes())
                {
                    if (fb.Feed.Count(id) <= 0) continue;
                    fb.Feed.Remove(id, 1);
                    d.hunger = Mathf.Min(100f, d.hunger + ContentDB.Item(id).foodValue * 2.5f);
                    return true;
                }
            if (anyBox) return false;
            // no Feed Box yet (the player hand-feeding in camp): straight from storage
            foreach (var id in Crops.KinFoods)
            {
                if (Storage.Count(id) <= 0) continue;
                Storage.Remove(id, 1);
                d.hunger = Mathf.Min(100f, d.hunger + ContentDB.Item(id).foodValue * 2.5f);
                return true;
            }
            return false;
        }

        void SpawnWorkerActor(CreatureData d)
        {
            if (workerActors.ContainsKey(d.uid) || d.Fainted) return;
            var p = CorePosition + new Vector3(UnityEngine.Random.Range(-4f, 4f), 0, UnityEngine.Random.Range(-4f, 4f));
            workerActors[d.uid] = CreatureSpawner.I.SpawnActor(d, CreatureMode.Worker, p);
        }

        void DespawnWorkerActors()
        {
            foreach (var kv in workerActors) if (kv.Value != null) kv.Value.Despawn();
            workerActors.Clear();
            actorsOut = false;
        }

        /// <summary>Rebuilds a worker's body (after evolving: new model).</summary>
        public void RespawnWorker(CreatureData d)
        {
            if (workerActors.TryGetValue(d.uid, out var a) && a != null) a.Despawn();
            workerActors.Remove(d.uid);
            if (actorsOut) SpawnWorkerActor(d);
        }

        public CreatureActor ActorFor(CreatureData d) => workerActors.TryGetValue(d.uid, out var a) ? a : null;

        // ============================================================ CRAFT ORDERS
        public bool HasOrderFor(CraftStation s)
        {
            if (s == CraftStation.Research) return Tech.Active != null;
            foreach (var o in Orders)
            {
                var r = ContentDB.Recipes.TryGetValue(o.recipeId, out var rr) ? rr : null;
                if (r != null && r.station == s) return true;
            }
            return false;
        }

        public void AddCraftWork(CraftStation s, float w)
        {
            if (s == CraftStation.Research) { Tech.AddWork(w); return; }
            for (int i = 0; i < Orders.Count; i++)
            {
                var o = Orders[i];
                if (!ContentDB.Recipes.TryGetValue(o.recipeId, out var r) || r.station != s) continue;
                o.progress += w * GameManager.I.Player.Stats.CraftSpeedMul;
                if (o.progress >= r.work)
                {
                    int left = Storage.Add(r.outputItem, r.outputCount);
                    if (left > 0) left = GameManager.I.Player.Stats.Bag.Add(r.outputItem, left);
                    if (left > 0) { o.progress = r.work; GameEvents.Warn("Storage full - craft order paused."); return; }
                    o.progress = 0f;
                    o.remaining--;
                    GameEvents.Good($"Crafted {r.outputCount}x {ContentDB.ItemName(r.outputItem)} (base storage)");
                    SynthAudio.Play(Sfx.Craft, 1f, 0.6f);
                    if (o.remaining <= 0) Orders.RemoveAt(i);
                    GameEvents.RaiseInventoryChanged();
                }
                return;
            }
        }

        public float OrderProgress01(CraftOrder o) =>
            ContentDB.Recipes.TryGetValue(o.recipeId, out var r) && r.work > 0 ? Mathf.Clamp01(o.progress / r.work) : 0f;

        // ============================================================ TICK
        public void Tick(float dt, Vector3 playerPos)
        {
            if (!HasBase) return;
            float dist = Vector3.Distance(playerPos, CorePosition);
            bool near = dist < actorRange;
            // coming home: after 40 s or more away, the kin at camp welcome you (CreatureActor.CampLife)
            bool inRing = dist < Radius;
            if (inRing && !playerInRing && Time.time - playerLeftAt > 40f)
            {
                PlayerArrival = Time.time + (actorsOut ? 0f : 0.6f);   // give freshly spawned actors a moment to exist
                bool anyone = false;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (!d.Fainted) { anyone = true; break; }
                if (anyone) { SynthAudio.Play(Sfx.Pluck, 1.25f, 0.4f); GameEvents.Say("Your kin are glad to see you home.", new Color(1f, 0.7f, 0.85f)); }
            }
            if (!inRing && playerInRing) playerLeftAt = Time.time;
            playerInRing = inRing;
            if (near && !actorsOut)
            {
                actorsOut = true;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) SpawnWorkerActor(d);
            }
            else if (!near && actorsOut) DespawnWorkerActors();

            SimulateNeeds(dt);
            TickFeed(dt);
            TickStages(dt);
            if (!near) SimulateAbstract(dt);
            TickCharge(dt);
            TickDefense(dt, playerPos, near);
            TickTurrets(dt, near);
            TickAutomation(dt);

            // player helps with craft orders when standing near a station
            assistTimer -= dt;
            foreach (var b in Buildings)
            {
                if (!b.IsStation || !HasOrderFor(b.Def.station)) continue;
                if (Vector3.Distance(playerPos, b.transform.position) < b.Def.footprint + 3f)
                {
                    AddCraftWork(b.Def.station, playerCraftAssist * dt);
                    if (assistTimer <= 0f) { assistTimer = 0.5f; Fx.Debris(b.transform.position + Vector3.up, new Color(0.9f, 0.8f, 0.6f), 2); }
                }
            }
            TickPlayerWork(dt, playerPos, near);
        }

        /// <summary>WORK prompt on a producer: start working it by hand, or stop if already on it.</summary>
        public void TogglePlayerWork(Building b)
        {
            if (b == null || !b.IsProducer) return;
            if (PlayerWorkTarget == b) { StopPlayerWork("You stop working."); return; }
            var pl = GameManager.I.Player;
            string block = b.PlayerWork(0f, pl.Stats.Bag);   // starts the batch (takes the inputs) if it can
            if (block == "WAIT") { GameEvents.Say($"{b.Cur.doing}: it needs no hands for another {WorkStages.Clock(b.SecondsLeft)}."); return; }
            if (block != null)
            {
                GameEvents.Warn(block == "FULL" ? $"{b.Def.displayName} is full - collect it first."
                    : $"{b.Def.displayName} needs {b.BatchInput}x {ContentDB.ItemName(b.Def.inputItem)} in camp storage or your bag.");
                return;
            }
            PlayerWorkTarget = b;
            PlayerWorkBlock = null;
            GameEvents.Say($"{b.Cur.doing} by hand - slower than a kin. Tap again or walk away to stop.", new Color(0.7f, 1f, 0.8f));
            SynthAudio.Play(Sfx.Craft, 1.1f, 0.5f);
        }

        public void StopPlayerWork(string msg = null)
        {
            if (PlayerWorkTarget == null && PlayerWorkBlock == null) return;
            PlayerWorkTarget = null;
            PlayerWorkBlock = null;
            if (msg != null) GameEvents.Say(msg);
        }

        /// <summary>The player can run any producer by hand once they choose to (WORK prompt) - slower than a kin, so no building is locked behind owning the right species.</summary>
        void TickPlayerWork(float dt, Vector3 playerPos, bool near)
        {
            var b = PlayerWorkTarget;
            if (b == null) { PlayerWorkTarget = null; PlayerWorkBlock = null; return; }   // also covers a demolished building
            var pl = GameManager.I.Player;
            Vector3 d = b.transform.position - playerPos; d.y = 0;
            if (!near || !pl.IsAlive || Caves.InCave || d.magnitude - b.Def.footprint > PlayerWorkReach) { StopPlayerWork("You stop working."); return; }
            int before = b.BufferCount;
            PlayerWorkBlock = b.PlayerWork(playerWorkRate * WorldRules.Current.workSpeed * pl.Stats.CraftSpeedMul * dt, pl.Stats.Bag);
            if (PlayerWorkBlock == "WAIT") { StopPlayerWork($"Done. {b.Cur.doing} now - it will be ready in {WorkStages.Clock(b.SecondsLeft)}."); return; }
            if (PlayerWorkBlock != null)
            {
                if (PlayerWorkBlock != "FULL") StopPlayerWork($"Out of {ContentDB.ItemName(b.Def.inputItem)} - you stop working.");
                return;   // full: waits for COLLECT, then carries on
            }
            if (assistTimer <= 0f) { assistTimer = 0.5f; Fx.Debris(b.transform.position + Vector3.up, new Color(0.9f, 0.8f, 0.6f), 2); }
            if (b.BufferCount > before) SynthAudio.Play(Sfx.Craft, 1f, 0.6f);
        }

        void SimulateNeeds(float dt)
        {
            float larder = LarderActive ? 0.6f : 1f;
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
            {
                float asleep = Asleep(d) ? 0.5f : 1f;   // a sleeping kin burns half as much
                d.hunger = Mathf.Max(0f, d.hunger - hungerPerSecond * d.HungerMul * larder * asleep * WorldRules.Current.hungerRate * GameManager.I.Player.Stats.KinHungerMul * dt);
                if (!actorsOut && d.hunger < 30f && HasFeedBox) EatFood(d);
            }
        }

        /// <summary>Same production math as the actor path, without movement. Used far away and for offline progress.</summary>
        public void SimulateAbstract(float dt, bool xp = true)
        {
            float haulers = 0;
            var party = GameManager.I.Party;
            abstractHands.Clear();
            bool piles = FindHaulSource() != null;
            foreach (var d in party.In(KinLocation.Base))
            {
                if (d.Fainted || Asleep(d)) continue;   // at night only the nocturnal ones are up
                var home = FindBuilding(d.assignedBuilding);
                // haulers carry while there is something to carry; with the piles cleared they turn a hand to whatever else they can do
                if (piles && home != null && (home.Def.isCore || home.Def.workType == WorkType.Hauling)) { haulers += 1 + d.WorkLevel(WorkType.Hauling); if (xp) AccrueWorkXp(d, dt); continue; }
                // the same choice the actors make: wherever its hands count for most, its own structure first among equals
                Building t = null;
                float bestScore = 0f;
                if (IsPost(home) && home.CanWork(d)) { t = home; bestScore = float.MaxValue; }   // a kin on a post stays on it
                foreach (var b in Buildings)
                {
                    if (b.Def.isCore || b.Def.workType == WorkType.Hauling || b.Def.workerSlots <= 0 || !b.CanWork(d)) continue;
                    abstractHands.TryGetValue(b, out int there);
                    if (there >= b.Def.workerSlots) continue;
                    float score = TaskScore(d, b, home);
                    if (score > bestScore) { bestScore = score; t = b; }
                }
                if (t != null)
                {
                    abstractHands.TryGetValue(t, out int n);
                    abstractHands[t] = n + 1;
                    t.AddWork(WorkRate(d, t) * dt);
                    if (xp) AccrueWorkXp(d, dt);
                }
                else if (d.WorkLevel(WorkType.Hauling) > 0) haulers += 0.5f * d.WorkLevel(WorkType.Hauling);   // carries when nothing else needs it
            }
            if (haulers > 0)
            {
                float budget = haulers * abstractHaulPerHauler * dt;
                haulBudget += budget;
                while (haulBudget >= 1f)
                {
                    var src = FindHaulSource();
                    if (src == null) { haulBudget = 0f; break; }
                    int n = src.TakeFromBuffer(1, out string item);
                    int left = Storage.Add(item, n);
                    if (left > 0) { src.ReturnToBuffer(item, left); haulBudget = 0f; break; }
                    haulBudget -= 1f;
                }
            }
        }

        float haulBudget;
        readonly Dictionary<Building, int> abstractHands = new Dictionary<Building, int>();

        /// <summary>Catch-up simulation for time spent away from the game (capped).</summary>
        public void SimulateOffline(float seconds)
        {
            if (!HasBase || seconds <= 1f || !WorldRules.Current.offlineProgress) return;
            seconds = Mathf.Min(seconds, 2f * 3600f);
            const float step = 5f;
            int steps = Mathf.CeilToInt(seconds / step);
            // the camp's nights went by too: a clock of its own, started where the world's is now and run backwards over the time away
            var env = EnvironmentSystem.I;
            float dayLen = env != null ? Mathf.Max(60f, env.DayLength) : 1200f;
            float clock = Mathf.Repeat((env != null ? env.timeOfDay : 0.4f) - seconds / dayLen, 1f);
            for (int i = 0; i < steps; i++)
            {
                clock = Mathf.Repeat(clock + step / dayLen, 1f);
                nightOverride = clock < 0.23f || clock > 0.78f;
                eveningOverride = clock >= 0.64f && clock <= 0.78f;
                SimulateNeeds(step);
                TickFeed(step);
                TickStages(step);
                // pylons, dynamos and larders run only while a kin that is awake is at them (SimulateAbstract keeps post kin on their posts)
                foreach (var b in Buildings) if (IsPost(b)) b.PoweredUntil = 0;
                SimulateAbstract(step, false);   // production only: kin never level up while the game is closed
                TickCharge(step);
                TickAutomation(step);
            }
            nightOverride = null;
            eveningOverride = null;
            GameEvents.Say($"Your camp kept working while you were away ({seconds / 60f:0} min).", new Color(0.7f, 1f, 0.8f));
        }

        public void ClearAll()
        {
            DespawnWorkerActors();
            foreach (var b in Buildings) b.Remove();
            Buildings.Clear();
            Core = null;
            CoreLevel = 1;
            Charge = 0f;
            Cold = 0f;
            Orders.Clear();
            Storage.slots.Clear();
        }
    }
}
