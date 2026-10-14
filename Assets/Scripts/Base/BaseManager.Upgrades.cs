using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>The kind of thing a camp kin wants from you (a bubble over it; walk up and answer it).</summary>
    public enum CampWant { None, Pat, Snack, Gift }

    /// <summary>
    /// Reasons to walk round camp (user, 2026-10-14: "more reasons to visit camp").
    ///
    /// **Structure levels.** Producers and stations go to level 2 and 3, upgraded standing at them (MANAGE >
    /// UPGRADE): each level is +25% work there (`Building.LevelMul`, in `WorkRate`), one more worker place
    /// (`Building.WorkerSlots`) and a bigger output shelf (`Building.BufferCap`), shown by gold pennants. Level 2 costs
    /// the structure's own cost again + 3 ingots; level 3 twice its cost + 6 ingots + 3 crystal.
    ///
    /// **Kin wants.** While you are in camp, every few minutes one awake camp kin wants something, for a few minutes:
    /// a pat, a snack from your bag, or it found a gift for you (from its job; now and then a rare one). Walk up and
    /// answer (PET / GIVE SNACK / TAKE GIFT): a pat or a snack cheers it up (+25% work for 10 minutes,
    /// `CheerMul`) and builds bond. Ignored, it simply passes - there is no penalty.
    /// </summary>
    public partial class BaseManager
    {
        // ============================================================ STRUCTURE LEVELS
        public static Ingredient[] StructureUpgradeCost(Building b)
        {
            int to = b.Level + 1;
            var list = new List<Ingredient>();
            foreach (var c in b.Def.cost) list.Add(new Ingredient(c.itemId, c.count * (to - 1)));
            void Add(string id, int n)
            {
                for (int i = 0; i < list.Count; i++) if (list[i].itemId == id) { list[i] = new Ingredient(id, list[i].count + n); return; }
                list.Add(new Ingredient(id, n));
            }
            Add("ingot", to == 2 ? 3 : 6);
            if (to >= 3) Add("crystal", 3);
            return list.ToArray();
        }

        public static string UpgradeText(Building b) =>
            $"Level {b.Level + 1}: +25% work speed here (x{1f + 0.25f * b.Level:0.00} in all)" + (b.Def.workerSlots > 0 ? ", one more worker place" : "") + (b.Def.bufferCap > 0 ? ", a bigger output shelf" : "") + ".";

        public bool TryUpgradeStructure(Building b)
        {
            if (b == null || !b.Upgradable || b.Level >= Building.MaxLevel) return false;
            if (!PlayerAt(b)) { GameEvents.Warn($"Walk up to the {b.Def.displayName} to upgrade it."); return false; }
            var cost = StructureUpgradeCost(b);
            if (!Dev.FreeAll && !ItemSources.Has(cost)) { GameEvents.Warn("Not enough materials."); return false; }
            if (!Dev.FreeAll) ItemSources.Consume(cost);
            b.Level++;
            Fx.Sparkle(b.transform.position + Vector3.up * (b.TopY + 0.4f), new Color(1f, 0.85f, 0.4f), 30, 1.4f);
            SynthAudio.Play(Sfx.Build, 1.1f);
            SynthAudio.Play(Sfx.LevelUp, 1.3f, 0.6f);
            GameEvents.Good($"{b.Def.displayName} is now level {b.Level}: x{b.LevelMul:0.00} work speed{(b.Def.workerSlots > 0 ? $", {b.WorkerSlots} worker places" : "")}.");
            GameEvents.RaiseBaseChanged();
            GameEvents.RaiseInventoryChanged();
            return true;
        }

        // ============================================================ KIN WANTS
        public const float CheerSeconds = 600f, CheerBonus = 0.25f, WantSeconds = 240f;
        float wantTimer = 90f;

        /// <summary>+25% work for a kin you have just patted or fed.</summary>
        public static float CheerMul(CreatureData d) => d != null && Time.time < d.cheerUntil ? 1f + CheerBonus : 1f;

        void TickWants(float dt, bool playerHere)
        {
            // wants that ran out
            foreach (var a in workerActors.Values)
                if (a != null && a.Data != null && a.Data.campWant != CampWant.None && Time.time > a.Data.campWantUntil) a.Data.campWant = CampWant.None;
            if (!playerHere) return;
            wantTimer -= dt;
            if (wantTimer > 0f) return;
            wantTimer = Random.Range(150f, 300f);
            int active = 0;
            var pick = new List<CreatureActor>();
            foreach (var a in workerActors.Values)
            {
                if (a == null || !a.IsAlive || a.Data == null) continue;
                if (a.Data.campWant != CampWant.None) { active++; continue; }
                if (Asleep(a.Data) || a.Data.Fainted) continue;
                pick.Add(a);
            }
            if (active >= 2 || pick.Count == 0) return;
            var who = pick[Random.Range(0, pick.Count)];
            float r = Random.value;
            who.Data.campWant = r < 0.45f ? CampWant.Pat : r < 0.72f ? CampWant.Snack : CampWant.Gift;
            who.Data.campWantUntil = Time.time + WantSeconds;
            Fx.Sparkle(who.Position + Vector3.up * (who.Radius * 2.2f), new Color(1f, 0.6f, 0.8f), 10, 0.5f);
            SynthAudio.Play(Sfx.Pluck, 1.6f, 0.5f);
            GameEvents.Say($"{who.Data.DisplayName} {WantText(who.Data.campWant)}.", new Color(1f, 0.75f, 0.85f));
        }

        public static string WantText(CampWant w) =>
            w == CampWant.Pat ? "wants a pat" : w == CampWant.Snack ? "is after a snack" : w == CampWant.Gift ? "found something for you" : "";

        public static string WantPrompt(CampWant w) =>
            w == CampWant.Pat ? "PET" : w == CampWant.Snack ? "GIVE SNACK" : "TAKE GIFT";

        /// <summary>A camp kin with a want within reach of the player, nearest first.</summary>
        public CreatureActor WantNear(Vector3 p, float reach)
        {
            CreatureActor best = null; float bd = reach * reach;
            foreach (var a in workerActors.Values)
            {
                if (a == null || !a.IsAlive || a.Data == null || a.Data.campWant == CampWant.None) continue;
                Vector3 d = a.Position - p; d.y = 0f;
                float r = d.sqrMagnitude - a.Radius * a.Radius;
                if (r < bd) { bd = r; best = a; }
            }
            return best;
        }

        public void AnswerWant(CreatureActor a)
        {
            if (a == null || a.Data == null) return;
            var d = a.Data; var gm = GameManager.I;
            switch (d.campWant)
            {
                case CampWant.Pat:
                    gm.AddBond(d, 4f);
                    Quests.Note("pet");
                    Cheer(a, $"{d.DisplayName} loves that and gets back to work with a spring in its step (+25% work for 10 min).");
                    break;
                case CampWant.Snack:
                {
                    string food = CheapestFoodInBag();
                    if (food == null) { GameEvents.Warn("You have no food in your bag for it."); return; }
                    gm.Player.Stats.Bag.Remove(food, 1);
                    d.hunger = 100f;
                    gm.AddBond(d, 3f);
                    Cheer(a, $"{d.DisplayName} munches the {ContentDB.ItemName(food)} happily (+25% work for 10 min).");
                    GameEvents.RaiseInventoryChanged();
                    break;
                }
                case CampWant.Gift:
                {
                    var (id, n) = GiftFor(d);
                    gm.GiveItem(id, n, a.Position + Vector3.up);
                    gm.AddBond(d, 2f);
                    a.Rig.Squash();
                    GameEvents.Good($"{d.DisplayName} found {n} {ContentDB.ItemName(id)} for you!");
                    SynthAudio.Play(Sfx.Pickup, 1.2f);
                    break;
                }
                default: return;
            }
            d.campWant = CampWant.None;
        }

        void Cheer(CreatureActor a, string msg)
        {
            a.Data.cheerUntil = Time.time + CheerSeconds;
            a.Rig.Squash(); a.Rig.Lunge();
            Fx.Sparkle(a.Position + Vector3.up * (a.Radius * 2f), new Color(1f, 0.55f, 0.75f), 18, 0.8f);
            Fx.Ring(a.Position, new Color(1f, 0.6f, 0.8f), a.Radius + 1f, 16);
            SynthAudio.Play(Sfx.Pluck, 1.4f, 0.6f);
            GameEvents.Say(msg, new Color(1f, 0.7f, 0.85f));
        }

        static string CheapestFoodInBag()
        {
            string best = null; float bv = float.MaxValue;
            foreach (var s in GameManager.I.Player.Stats.Bag.slots)
            {
                if (s.Empty) continue;
                var def = ContentDB.Item(s.id);
                if (def == null || def.foodValue <= 0f || def.mealMinutes > 0f) continue;   // never a meal with a buff
                if (def.foodValue < bv) { bv = def.foodValue; best = s.id; }
            }
            return best;
        }

        /// <summary>What a kin brings you: something from its best job, now and then something rare.</summary>
        static (string, int) GiftFor(CreatureData d)
        {
            float r = Random.value;
            if (r < 0.06f) return ("prism_shard", 1);
            if (r < 0.12f) return ("tether_seed", 2);
            WorkType best = WorkType.None; int lv = 0;
            foreach (var w in d.Species.work) { int l = d.JobLevel(w.type); if (l > lv) { lv = l; best = w.type; } }
            int k = Mathf.Max(1, lv);
            switch (best)
            {
                case WorkType.Logging: return ("wood", 4 + 2 * k);
                case WorkType.Quarrying: return (Random.value < 0.3f ? "ore" : "stone", 3 + 2 * k);
                case WorkType.Foraging: return (Random.value < 0.5f ? "fiber" : "berry", 4 + 2 * k);
                case WorkType.Cultivating: return ("goldgrain", 2 + k);
                case WorkType.Irrigating: return ("gel", 1 + k);
                case WorkType.Kindling: return ("cinder", 1 + k);
                case WorkType.Crafting: return ("ingot", k);
                case WorkType.Chilling: return ("crystal", 1 + k / 2);
                case WorkType.Fishing: return ("silverfin", 1 + k);
                default: return (Random.value < 0.5f ? "pelt" : "fiber", 2 + k);
            }
        }
    }
}
