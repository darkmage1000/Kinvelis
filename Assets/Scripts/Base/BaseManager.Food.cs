using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Food at camp: how long it keeps, and how much kin eat.
    ///
    /// **Spoiling.** Food in camp storage and in Feed Boxes goes off. Each kind keeps for a number of days
    /// (`KeepDays`); every few seconds a share of each pile is lost so that a pile left alone is mostly gone after
    /// that long (an even decay, so there are no per-stack timers and nothing new in the save). Food stored IN a
    /// Frost Larder that holds cold keeps `ColdKeeps` times as long (BaseManager.Store). The effect is that a camp can never sit on more of
    /// a food than it makes in about its keeping time - no more ten-thousand-berry stores. The player's bag, an
    /// outpost's stock and output still waiting at a structure do not spoil. Runs near, far and while closed.
    ///
    /// **Appetite.** A kin's hunger grows with the best job level it has (`AppetiteMul`): the kin that do the most
    /// work eat the most. A meal is eaten until the kin is full (`EatFood`), so weak food like berries goes by the
    /// dozen and better food (melons, fish, tarts) by the handful.
    /// </summary>
    public partial class BaseManager
    {
        /// <summary>Kin get this many hunger points per point of an item's food value.</summary>
        public const float KinFoodMul = 2.5f;
        /// <summary>Extra hunger per job level above 1 (level 4 = about double).</summary>
        public const float HungerPerWorkLevel = 0.35f;
        /// <summary>Food in a cold Frost Larder keeps this many times as long.</summary>
        public const float ColdKeeps = 3f;
        const float EatUntil = 92f;
        const int MaxBites = 40;
        const float SpoilStep = 5f;

        // ============================================================ APPETITE
        /// <summary>The best job level this kin has (a shimmer's +1 included).</summary>
        public static int BestWork(CreatureData d)
        {
            int m = 0;
            foreach (var w in d.Species.work) m = Mathf.Max(m, d.JobLevel(w.type));   // what it can do, not what is switched on: appetite and beds don't change with the switches
            return m;
        }

        /// <summary>Better workers eat more: x1 at job level 1, about x2 at level 4.</summary>
        public static float AppetiteMul(CreatureData d) => 1f + HungerPerWorkLevel * Mathf.Max(0, BestWork(d) - 1);

        /// <summary>Hunger points a camp kin loses per second right now.</summary>
        public float HungerRate(CreatureData d)
        {
            return hungerPerSecond * d.HungerMul * AppetiteMul(d) * (LarderActive ? 0.6f : 1f) * (Asleep(d) ? 0.5f : 1f)
                 * WorldRules.Current.hungerRate * GameManager.I.Player.Stats.KinHungerMul;
        }

        /// <summary>Hunger points one of this item gives a kin.</summary>
        public static float KinFood(ItemDef d) => d != null ? d.foodValue * KinFoodMul : 0f;

        // ============================================================ SPOILING
        float spoilTimer;
        readonly Dictionary<string, float> spoilCarry = new Dictionary<string, float>();
        readonly Dictionary<string, int> spoilCounts = new Dictionary<string, int>();
        readonly List<string> spoilIds = new List<string>();
        /// <summary>Pieces of food lost since the last morning (for the dawn note).</summary>
        public int SpoiledToday;
        bool wasNight;

        static bool IsRawFish(string id) => id == "silverfin" || id == "sunfin" || id == "reef_snapper" || id == "mirefish" || id == "goldscale";

        /// <summary>How many days this food keeps at camp with no cooling (0 = it is not food and never spoils).</summary>
        public static float KeepDays(ItemDef d)
        {
            if (d == null || d.foodValue <= 0f) return 0f;
            if (d.mealMinutes > 0f || d.id == "berry_tart") return 4f;   // cooked
            if (d.id == "berry" || IsRawFish(d.id)) return 1f;          // soft fruit and raw fish
            return 2f;                                                  // other crops and wild plants
        }

        /// <summary>The same in seconds: cold = it is in a larder that holds cold.</summary>
        public float KeepSeconds(ItemDef d, bool cold) => KeepDays(d) * DaySeconds * (cold ? ColdKeeps : 1f);

        /// <summary>"keeps 1 day at camp, 3 in a cold larder" for item lines.</summary>
        public static string KeepText(ItemDef d)
        {
            float k = KeepDays(d);
            return k <= 0f ? null : $"keeps {k:0.#} day{(k == 1f ? "" : "s")} at camp, {k * ColdKeeps:0.#} in a cold larder";
        }

        void TickSpoil(float dt)
        {
            spoilTimer += dt;
            if (spoilTimer < SpoilStep) return;
            float step = spoilTimer; spoilTimer = 0f;
            int lost = SpoilStore(step);   // box by box: a cold larder's contents go three times slower
            foreach (var fb in FeedBoxes()) lost += SpoilSlots(fb.Feed.slots, fb.Uid, step, false);
            if (lost > 0) { SpoiledToday += lost; GameEvents.RaiseInventoryChanged(); }
        }

        /// <summary>Dev: this much time's spoiling at once. Returns what was lost.</summary>
        public int DevSpoil(float seconds)
        {
            int lost = SpoilStore(seconds);
            foreach (var fb in FeedBoxes()) lost += SpoilSlots(fb.Feed.slots, fb.Uid, seconds, false);
            SpoiledToday += lost;
            GameEvents.RaiseInventoryChanged();
            return lost;
        }

        /// <summary>Pieces of food going off per minute in camp storage and feed boxes at the current rate.</summary>
        public float SpoilPerMinute
        {
            get
            {
                float sum = 0f;
                foreach (var b in Buildings) if (b.BoxLen > 0) { bool cold = IsCold(b); foreach (var s in BoxSlots(b)) sum += SpoilRate(s, cold); }
                foreach (var fb in FeedBoxes()) foreach (var s in fb.Feed.slots) sum += SpoilRate(s, false);
                return sum;
            }
        }

        float SpoilRate(ItemStack s, bool cold)
        {
            if (s.Empty) return 0f;
            float life = KeepSeconds(ContentDB.Item(s.id), cold);
            return life > 0f ? s.count * (1f - Mathf.Exp(-60f / life)) : 0f;
        }

        /// <summary>At dawn, with the player about: one line on what went off since yesterday.</summary>
        void TickSpoilNote(bool near)
        {
            bool night = NightNow;
            if (wasNight && !night)
            {
                if (near && SpoiledToday >= 5)
                    GameEvents.Say($"{SpoiledToday} pieces of food went off at camp since yesterday." + (HasBuilding("frost_larder") ? " Food kept in a cold Frost Larder lasts three times as long." : " Food kept in a Frost Larder lasts three times as long."), new Color(0.85f, 0.8f, 0.6f));
                SpoiledToday = 0;
            }
            wasNight = night;
        }
    }
}
