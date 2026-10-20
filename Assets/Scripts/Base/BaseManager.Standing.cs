using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>"Keep N of this in stock": a recipe the camp re-queues by itself.</summary>
    [Serializable]
    public class StandingOrder
    {
        public string recipeId;
        public int keep;
    }

    /// <summary>
    /// Standing orders. Set on a recipe at its station ("KEEP 10"). Every couple of seconds each one compares what the
    /// camp holds (camp storage, plus what is already queued) with what it should, and if it is short and the
    /// ingredients are in camp storage it takes them and queues the difference as ordinary craft orders (marked
    /// `auto`), a few at a time. From there everything is the existing order system: kin with the station's job, the
    /// Auto-Assembler or you standing at the station do the work. Runs near, far and while the game is closed.
    /// Ingredients come from camp storage only (never your bag), and nothing is queued for a recipe that is still
    /// locked or a station that is not built.
    /// </summary>
    public partial class BaseManager
    {
        public readonly List<StandingOrder> Standing = new List<StandingOrder>();
        /// <summary>The steps a KEEP button cycles through.</summary>
        public static readonly int[] KeepSteps = { 0, 5, 10, 20, 50 };
        /// <summary>How many crafts one standing order puts in the queue at a time (so it never ties up all the ingredients).</summary>
        const int StandingBatch = 4;
        float standingTimer;

        public int KeepOf(string recipeId)
        {
            foreach (var s in Standing) if (s.recipeId == recipeId) return s.keep;
            return 0;
        }

        public void SetKeep(string recipeId, int keep)
        {
            Standing.RemoveAll(s => s.recipeId == recipeId);
            if (keep > 0) Standing.Add(new StandingOrder { recipeId = recipeId, keep = keep });
            standingTimer = 0f;
        }

        /// <summary>The next step up from what is set now (wraps to off).</summary>
        public int NextKeep(string recipeId)
        {
            int cur = KeepOf(recipeId);
            for (int i = 0; i < KeepSteps.Length; i++) if (KeepSteps[i] > cur) return KeepSteps[i];
            return 0;
        }

        /// <summary>In camp storage now, plus what is already on order.</summary>
        public int StockOf(RecipeDef r)
        {
            int n = Storage.Count(r.outputItem);
            foreach (var o in Orders) if (o.recipeId == r.id) n += o.remaining * r.outputCount;
            return n;
        }

        bool StorageHas(Ingredient[] list)
        {
            foreach (var ing in list) if (Storage.Count(ing.itemId) < ing.count) return false;
            return true;
        }

        /// <summary>Why a standing order is not moving (null = it is, or it is satisfied).</summary>
        public string StandingBlock(RecipeDef r)
        {
            if (!HasStation(r.station, Stations.Tier(r))) return $"needs a {Stations.BenchName(r.station, Stations.Tier(r))}";
            if (!Tech.RecipeUnlocked(r.id)) return "needs research";
            if (Chests.Locked(r)) return "needs its blueprint";
            if (r.unlockLevel > GameManager.I.Player.Stats.level) return $"needs Lv {r.unlockLevel}";
            if (StockOf(r) >= KeepOf(r.id)) return null;
            foreach (var o in Orders) if (o.recipeId == r.id) return null;   // some are on the way
            return StorageHas(r.inputs) ? null : "missing ingredients in camp storage";
        }

        void TickStanding(float dt)
        {
            if (Standing.Count == 0) return;
            standingTimer -= dt;
            if (standingTimer > 0f) return;
            standingTimer = 2f;
            bool changed = false;
            foreach (var s in Standing)
            {
                if (!ContentDB.Recipes.TryGetValue(s.recipeId, out var r) || r.station == CraftStation.Hand) continue;
                if (!HasStation(r.station, Stations.Tier(r)) || !Tech.RecipeUnlocked(r.id) || Chests.Locked(r) || r.unlockLevel > GameManager.I.Player.Stats.level) continue;
                int queued = 0;
                CraftOrder mine = null;
                foreach (var o in Orders) if (o.recipeId == r.id) { mine = o; queued += o.remaining; }
                int have = Storage.Count(r.outputItem) + queued * r.outputCount;
                while (have < s.keep && queued < StandingBatch && StorageHas(r.inputs))
                {
                    foreach (var ing in r.inputs) Storage.Remove(ing.itemId, ing.count);
                    if (mine == null) { mine = new CraftOrder { recipeId = r.id, remaining = 0, auto = true }; Orders.Add(mine); }
                    mine.remaining++;
                    queued++;
                    have += r.outputCount;
                    changed = true;
                }
            }
            if (changed) GameEvents.RaiseInventoryChanged();
        }
    }
}
