using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public enum BuildingHealth { Good, Idle, Blocked, Info }

    /// <summary>Read-only camp analytics for the Camp menu: production rates, food runway, status + alerts.</summary>
    public partial class BaseManager
    {
        public IEnumerable<CreatureData> WorkersAt(Building b)
        {
            foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (d.assignedBuilding == b.Uid) yield return d;
        }

        public int HaulerCount
        {
            get
            {
                int n = 0;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                {
                    var b = FindBuilding(d.assignedBuilding);
                    if (b != null && (b.Def.isCore || b.Def.workType == WorkType.Hauling)) n++;
                }
                return n;
            }
        }

        /// <summary>Items per minute this building currently produces (0 if it can't work).</summary>
        public float OutputPerMinute(Building b)
        {
            if (!b.IsProducer) return 0f;
            float work = 0f;
            foreach (var d in WorkersAt(b)) if (b.CanWork(d)) work += WorkRate(d, b);
            return work / Mathf.Max(0.01f, b.WorkPerUnit) * b.OutputCount * 60f;
        }

        /// <summary>Total production per minute grouped by item id.</summary>
        public Dictionary<string, float> ProductionPerMinute()
        {
            var map = new Dictionary<string, float>();
            foreach (var b in Buildings)
            {
                float r = OutputPerMinute(b);
                if (r <= 0f) continue;
                map.TryGetValue(b.OutputItem, out float cur);
                map[b.OutputItem] = cur + r;
            }
            return map;
        }

        /// <summary>Hunger points available in storage (what workers get from eating everything).</summary>
        public float FoodPoints
        {
            get
            {
                float pts = 0f;
                foreach (var id in Crops.KinFoods) { var d = ContentDB.Item(id); if (d != null) pts += Storage.Count(id) * d.foodValue * 2.5f; }
                return pts;
            }
        }

        /// <summary>Hunger drained per minute by all camp workers.</summary>
        public float HungerPerMinute
        {
            get
            {
                float larder = LarderActive ? 0.6f : 1f, sum = 0f;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) sum += hungerPerSecond * d.HungerMul * larder * WorldRules.Current.hungerRate * GameManager.I.Player.Stats.KinHungerMul * 60f;
                return sum;
            }
        }

        /// <summary>Minutes until stored food runs out (float.PositiveInfinity if nothing eats).</summary>
        public float FoodMinutes => HungerPerMinute <= 0.001f ? float.PositiveInfinity : FoodPoints / HungerPerMinute;

        public bool StorageFull
        {
            get
            {
                if (Storage.Capacity == 0) return false;
                foreach (var s in Storage.slots) if (s.Empty) return false;
                return true;
            }
        }

        public int BedCount { get { int n = 0; foreach (var b in Buildings) n += b.Def.beds; return n; } }

        /// <summary>Traffic-light status for a structure + short explanation.</summary>
        public BuildingHealth Status(Building b, out string text)
        {
            if (AutomationStatus(b, out var ah, out var at)) { text = at; return ah; }
            int workers = 0; bool anyCan = false;
            foreach (var d in WorkersAt(b)) { workers++; if (b.CanWork(d)) anyCan = true; }
            if (b.Def.isCore)
            {
                text = HaulerCount > 0 ? $"{HaulerCount} hauler(s) moving goods to storage" : "Assign Hauling kin to carry output to storage";
                return HaulerCount > 0 ? BuildingHealth.Good : BuildingHealth.Idle;
            }
            if (b.IsProducer)
            {
                if (workers == 0) { text = $"No workers - needs {b.Def.workType}"; return BuildingHealth.Blocked; }
                if (b.BufferCount >= b.Def.bufferCap) { text = "Output full - collect it or add haulers"; return BuildingHealth.Blocked; }
                if (!string.IsNullOrEmpty(b.Def.inputItem) && b.Progress <= 0.001f && Storage.Count(b.Def.inputItem) < b.Def.inputCount)
                { text = $"Needs {b.Def.inputCount} {ContentDB.ItemName(b.Def.inputItem)} in storage"; return BuildingHealth.Blocked; }
                if (anyCan) { text = $"Working · {OutputPerMinute(b):0.#}/min"; return BuildingHealth.Good; }
                text = "Workers idle"; return BuildingHealth.Idle;
            }
            if (b.IsStation)
            {
                if (!HasOrderFor(b.Def.station)) { text = "No orders queued"; return BuildingHealth.Info; }
                if (workers == 0) { text = "Orders waiting - no workers (you can help nearby)"; return BuildingHealth.Idle; }
                text = "Crafting orders"; return BuildingHealth.Good;
            }
            if (b.Def.boostPercent > 0f || b.Def.workType == WorkType.Chilling)
            {
                if (b.Powered) { text = b.Def.boostPercent > 0 ? $"Active · +{b.Def.boostPercent:0}% production" : "Active · workers get hungry slower"; return BuildingHealth.Good; }
                text = $"Inactive - needs a {b.Def.workType} worker"; return BuildingHealth.Blocked;
            }
            if (b.Def.storageSlots > 0) { text = $"+{b.Def.storageSlots} storage slots"; return BuildingHealth.Info; }
            if (b.Def.feeds) { text = HasFood ? "Stocked" : "Empty - store berries or tarts"; return HasFood ? BuildingHealth.Good : BuildingHealth.Blocked; }
            if (b.Def.beds > 0) { text = $"{b.Def.beds} beds"; return BuildingHealth.Info; }
            text = ""; return BuildingHealth.Info;
        }

        public struct Alert { public string text; public bool severe; }

        public List<Alert> Alerts()
        {
            var list = new List<Alert>();
            var party = GameManager.I.Party;
            int workers = party.CountIn(KinLocation.Base);
            if (StorageFull) list.Add(new Alert { text = "Storage is full - build a Storage Chest or take items", severe = true });
            if (workers > 0 && !HasFeedBox) list.Add(new Alert { text = "No Feed Box - hungry workers can't eat", severe = true });
            else if (workers > 0 && !HasFood) list.Add(new Alert { text = "No food in storage - add berries or tarts", severe = true });
            else if (workers > 0 && FoodMinutes < 10f) list.Add(new Alert { text = $"Food runs out in ~{FoodMinutes:0} min", severe = false });
            int idle = 0;
            foreach (var d in party.In(KinLocation.Base)) if (FindBuilding(d.assignedBuilding) == null) idle++;
            if (idle > 0) list.Add(new Alert { text = $"{idle} worker(s) unassigned - tap AUTO-ASSIGN", severe = false });
            foreach (var b in Buildings)
            {
                var h = Status(b, out string t);
                if (h == BuildingHealth.Blocked && !b.Def.feeds) list.Add(new Alert { text = $"{b.Def.displayName}: {t}", severe = true });
            }
            if (workers > 0 && BedCount < workers && BedCount > 0) list.Add(new Alert { text = $"Only {BedCount} beds for {workers} workers (no rested bonus)", severe = false });
            if (workers == 0) list.Add(new Alert { text = "No workers yet - send kin TO CAMP from the Kin tab", severe = false });
            return list;
        }
    }
}
