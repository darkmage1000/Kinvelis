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
        public bool HasFood
        {
            get { foreach (var id in Crops.KinFoods) if (Storage.Count(id) > 0) return true; return false; }
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

        public bool LarderActive
        {
            get { foreach (var x in Buildings) if (x.Def.workType == WorkType.Chilling && x.Powered) return true; return false; }
        }

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
            float lvl = Mathf.Max(d.WorkLevel(b.Def.workType), b.Def.secondaryWorkType != WorkType.None ? d.WorkLevel(b.Def.secondaryWorkType) * 0.75f : 0f);
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
            // refund half the cost into storage
            foreach (var c in b.Def.cost) Storage.Add(c.itemId, c.count / 2);
            if (b.BufferCount > 0) Storage.Add(b.BufferItem, b.BufferCount);
            Buildings.Remove(b);
            b.Remove();
            RecalcStorage();
            GameEvents.RaiseBaseChanged();
            GameEvents.RaiseInventoryChanged();
        }

        void RecalcStorage()
        {
            int slots = HasBase ? (CoreLevel - 1) * StoragePerCoreLevel : 0;
            foreach (var b in Buildings) slots += b.Def.storageSlots;
            Storage.Resize(slots);
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
            // any plain crop will do, cheapest first (tarts last)
            string food = null;
            foreach (var id in Crops.KinFoods) if (Storage.Count(id) > 0) { food = id; break; }
            if (food == null) return false;
            Storage.Remove(food, 1);
            d.hunger = Mathf.Min(100f, d.hunger + ContentDB.Item(food).foodValue * 2.5f);
            return true;
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
            if (!near) SimulateAbstract(dt);
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
            if (block != null)
            {
                GameEvents.Warn(block == "FULL" ? $"{b.Def.displayName} is full - collect it first."
                    : $"{b.Def.displayName} needs {b.Def.inputCount}x {ContentDB.ItemName(b.Def.inputItem)} in camp storage or your bag.");
                return;
            }
            PlayerWorkTarget = b;
            PlayerWorkBlock = null;
            GameEvents.Say($"Working the {b.Def.displayName} by hand - slower than a kin. Tap again or walk away to stop.", new Color(0.7f, 1f, 0.8f));
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
                d.hunger = Mathf.Max(0f, d.hunger - hungerPerSecond * d.HungerMul * larder * WorldRules.Current.hungerRate * GameManager.I.Player.Stats.KinHungerMul * dt);
                if (!actorsOut && d.hunger < 30f && HasFeedBox) EatFood(d);
            }
        }

        /// <summary>Same production math as the actor path, without movement. Used far away and for offline progress.</summary>
        public void SimulateAbstract(float dt, bool xp = true)
        {
            int haulers = 0;
            var party = GameManager.I.Party;
            foreach (var d in party.In(KinLocation.Base))
            {
                var b = FindBuilding(d.assignedBuilding);
                if (b == null) continue;
                if (b.Def.isCore || b.Def.workType == WorkType.Hauling) { haulers += 1 + d.WorkLevel(WorkType.Hauling); if (xp) AccrueWorkXp(d, dt); continue; }
                if (b.CanWork(d)) { b.AddWork(WorkRate(d, b) * dt); if (xp) AccrueWorkXp(d, dt); }
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

        /// <summary>Catch-up simulation for time spent away from the game (capped).</summary>
        public void SimulateOffline(float seconds)
        {
            if (!HasBase || seconds <= 1f || !WorldRules.Current.offlineProgress) return;
            seconds = Mathf.Min(seconds, 2f * 3600f);
            const float step = 5f;
            int steps = Mathf.CeilToInt(seconds / step);
            for (int i = 0; i < steps; i++)
            {
                SimulateNeeds(step);
                SimulateAbstract(step, false);   // production only: kin never level up while the game is closed
                TickAutomation(step);
                // workers keep powering dynamos/larders while offline
                foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                {
                    var b = FindBuilding(d.assignedBuilding);
                    if (b != null && !b.IsProducer && !b.IsStation && b.CanWork(d)) b.PoweredUntil = GameManager.WorldTime + 2.0;
                }
            }
            GameEvents.Say($"Your camp kept working while you were away ({seconds / 60f:0} min).", new Color(0.7f, 1f, 0.8f));
        }

        public void ClearAll()
        {
            DespawnWorkerActors();
            foreach (var b in Buildings) b.Remove();
            Buildings.Clear();
            Core = null;
            CoreLevel = 1;
            Orders.Clear();
            Storage.slots.Clear();
        }
    }
}
