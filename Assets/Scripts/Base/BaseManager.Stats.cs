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
            // one batch = every step in turn: the hands-on ones at the pace of the best kin in camp for each, the hands-off ones by the clock
            float seconds = 0f;
            int was = b.Stage;
            for (int i = 0; i < b.Stages.Length; i++)
            {
                if (b.Stages[i].handsOff) { seconds += b.StageNeedAt(i); continue; }
                float best = 0f;
                b.Stage = i;   // WorkRate reads the step the structure is on
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (!d.Fainted && b.StageLevelAt(i, d) > 0f) best = Mathf.Max(best, WorkRate(d, b));
                if (Crops.IsPlot(b) && i == WorkStages.CropWater && SprinklerRunning) best = Mathf.Max(best, SprinklerWater * WorldRules.Current.workSpeed);
                if (b.Def.id == "greenhouse") best += GreenhouseRate * WorldRules.Current.workSpeed;
                if (best <= 0f) { b.Stage = was; return 0f; }
                seconds += b.StageNeedAt(i) / best;
            }
            b.Stage = was;
            return seconds > 0f ? b.BatchOutput / seconds * 60f : 0f;
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
                bool any = false, auto = false;
                foreach (var fb in FeedBoxes())
                {
                    any = true; auto |= fb.FeedAuto;
                    foreach (var s in fb.Feed.slots) { var d = s.Empty ? null : ContentDB.Item(s.id); if (d != null) pts += s.count * KinFood(d); }
                }
                // storage only counts if a box draws on it by itself (or there is no box and you feed them by hand)
                if (auto || !any) foreach (var id in Crops.KinFoods) { var d = ContentDB.Item(id); if (d != null) pts += Storage.Count(id) * KinFood(d); }
                return pts;
            }
        }

        /// <summary>Hunger drained per minute by all camp workers.</summary>
        public float HungerPerMinute
        {
            get
            {
                float sum = 0f;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) sum += HungerRate(d) * 60f;
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
                if (b.AtStart && b.StartBlock() != null) { text = b.StartBlock(); return BuildingHealth.Blocked; }
                float rate = OutputPerMinute(b);
                string pace = rate > 0f ? $" · about {rate:0.#}/min" : "";
                if (b.Waiting) { text = b.StageText(false) + pace; return BuildingHealth.Good; }
                if (!AnyoneCan(b, b.Stage))
                {
                    var st = b.Cur;
                    text = $"{st.waiting} - no {st.job}{(st.alt != WorkType.None ? " or " + st.alt : "")} kin in camp (you can do it by hand)";
                    return BuildingHealth.Blocked;
                }
                bool busy = HandsAt(b) > 0 || !actorsOut;
                if (NightNow && !AnyoneCan(b, b.Stage, true)) { text = $"{b.StageText(false)} - the kin who do this are asleep until dawn{pace}"; return BuildingHealth.Idle; }
                text = $"{b.StageText(busy)} {Mathf.RoundToInt(b.Stage01 * 100f)}%{pace}";
                return BuildingHealth.Good;
            }
            if (b.IsStation)
            {
                if (!HasOrderFor(b.Def.station)) { text = "No orders queued"; return BuildingHealth.Info; }
                bool machine = CampPowered && Has("auto_assembler") && (b.Def.station == CraftStation.Workbench || b.Def.station == CraftStation.Campfire);
                if (HandsAt(b) > 0) { text = machine ? "Kin and the Auto-Assembler are crafting orders" : "Crafting orders"; return BuildingHealth.Good; }
                if (machine) { text = "The Auto-Assembler is crafting orders"; return BuildingHealth.Good; }
                if (!AnyoneSuits(b)) { text = $"Orders waiting - no {b.Def.workType} kin in camp (stand near it to help)"; return BuildingHealth.Idle; }
                if (!AnyoneSuits(b, true)) { text = $"Orders waiting - your {b.Def.workType} kin are asleep until dawn"; return BuildingHealth.Idle; }
                if (!actorsOut) { text = "Crafting orders"; return BuildingHealth.Good; }
                text = $"Orders waiting - a {b.Def.workType} kin will come when it is free"; return BuildingHealth.Idle;
            }
            if (IsLarder(b))
            {
                const string effect = "workers get hungry 40% slower, food keeps 3x longer";
                if (b.Powered) { text = Cold >= ColdCapacity - 0.5f ? $"Full of cold ({WorkStages.Clock(Cold)} stored) · {effect}" : $"Chilling · {Mathf.RoundToInt(Cold01 * 100f)}% cold · {effect}"; return BuildingHealth.Good; }
                if (Cold > 0f) { text = $"Stored cold · {WorkStages.Clock(Cold)} left ({Mathf.RoundToInt(Cold01 * 100f)}%) · {effect}" + (NightNow ? "" : LarderWantsKin ? " · waiting for a Chilling kin" : " · its kin is free until it is half empty"); return BuildingHealth.Good; }
                if (ColdAsleep) { text = "Out of cold and its kin is asleep - food is not kept fresh until dawn"; return BuildingHealth.Idle; }
                text = "No cold - needs a Chilling kin"; return BuildingHealth.Blocked;
            }
            if (b.Def.boostPercent > 0f)
            {
                if (CampPowered) { text = $"Running · everyone works {b.Def.boostPercent:0}% faster"; return BuildingHealth.Good; }
                if (PowerAsleep) { text = "No power until dawn - the charge ran out and the Volt Pylon's kin is asleep"; return BuildingHealth.Idle; }
                text = Has("volt_pylon") ? "No power - the Volt Pylon needs charging" : "No power - build a Volt Pylon"; return BuildingHealth.Blocked;
            }
            if (b.Def.storageSlots > 0) { text = $"+{b.Def.storageSlots} storage slots"; return BuildingHealth.Info; }
            if (b.Def.feeds)
            {
                int n = FeedCount(b);
                bool can = n > 0 || (b.FeedAuto && StorageHasFood);
                text = n > 0 ? $"{n} food in it{(b.FeedAuto ? " · keeps itself stocked" : " · stocked by hand")}" : can ? "Stocking itself from storage" : b.FeedAuto ? "Empty - no food in camp storage" : "Empty - put food in it (OPEN)";
                return can ? BuildingHealth.Good : BuildingHealth.Blocked;
            }
            if (b.Def.beds > 0) { text = BedStatus(b); return BuildingHealth.Info; }
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
            else if (workers > 0 && !HasFood) list.Add(new Alert { text = "The Feed Box is empty - put berries, crops or fish in it", severe = true });
            else if (workers > 0 && FoodMinutes < 10f) list.Add(new Alert { text = $"Food runs out in ~{FoodMinutes:0} min", severe = false });
            int idle = 0;
            foreach (var d in party.In(KinLocation.Base)) if (FindBuilding(d.assignedBuilding) == null) idle++;
            if (idle > 0) list.Add(new Alert { text = $"{idle} worker(s) unassigned - tap AUTO-ASSIGN", severe = false });
            foreach (var b in Buildings)
            {
                var h = Status(b, out string t);
                if (h == BuildingHealth.Blocked && !b.Def.feeds) list.Add(new Alert { text = $"{b.Def.displayName}: {t}", severe = true });
            }
            var noBed = Bedless;
            if (workers > 0 && noBed.Count > 0 && BedCount > 0)
            {
                var want = new List<string>();
                foreach (var d in noBed) { string n = BedNeeded(d); if (!want.Contains(n)) want.Add(n); }
                list.Add(new Alert { text = $"{noBed.Count} kin {(noBed.Count == 1 ? "has" : "have")} no bed that fits (no rested bonus) - build a {string.Join(" / ", want)}", severe = false });
            }
            if (workers == 0) list.Add(new Alert { text = "No workers yet - send kin TO CAMP from the Kin tab", severe = false });
            int asleep = SleeperCount;
            if (asleep > 0) list.Add(new Alert { text = $"Night: {asleep} kin asleep until dawn (night kin and Night Owls keep working)", severe = false });
            return list;
        }
    }
}
