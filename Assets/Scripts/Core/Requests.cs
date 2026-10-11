using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public enum RequestType { Deliver, CatchSpecies, DefeatElement, BeatTamer }

    [Serializable]
    public class SettleRequest
    {
        public string settlement;
        public RequestType type;
        public string param;      // item id / species id / element name
        public int target, progress;
        public int glimmer, standing;
        public string rewardItem;
        public int rewardCount;
        public string title;
    }

    [Serializable]
    public class StandingEntry { public string id; public int points; }

    /// <summary>
    /// Settlement request boards: each settlement keeps three jobs open (deliveries, a local kin to catch, kin to
    /// thin out, a rival tamer to beat). Finishing them pays Glimmer and raises your standing there, which cuts the
    /// Kinkeeper's prices and adds to what they offer.
    /// </summary>
    public static class Requests
    {
        public const int Slots = 3;
        public const int SwapCost = 20;
        static readonly int[] RankAt = { 0, 30, 80, 160, 280, 450 };
        static readonly string[] RankNames = { "Stranger", "Visitor", "Friend", "Trusted", "Honored", "Kin of the village" };
        public const float DiscountPerRank = 0.03f;

        static PlayerStats S => GameManager.I.Player.Stats;

        // ---------------------------------------------------------------- standing
        public static int Points(Settlements.Settlement s)
        {
            foreach (var e in S.standing) if (e.id == s.id) return e.points;
            return 0;
        }

        public static int Rank(Settlements.Settlement s)
        {
            int p = Points(s), r = 0;
            for (int i = 0; i < RankAt.Length; i++) if (p >= RankAt[i]) r = i;
            return r;
        }

        public static int MaxRank => RankAt.Length - 1;
        public static string RankName(int rank) => RankNames[Mathf.Clamp(rank, 0, RankNames.Length - 1)];
        public static int NextRankAt(int rank) => rank >= MaxRank ? RankAt[MaxRank] : RankAt[rank + 1];
        public static int RankFloor(int rank) => RankAt[Mathf.Clamp(rank, 0, MaxRank)];

        /// <summary>Kinkeeper price multiplier at this settlement.</summary>
        public static float PriceMul(Settlements.Settlement s) => 1f - DiscountPerRank * Rank(s);
        /// <summary>Extra kin the Kinkeeper shows you (Friend +1, Honored +1).</summary>
        public static int ExtraOffers(Settlements.Settlement s) { int r = Rank(s); return (r >= 2 ? 1 : 0) + (r >= 4 ? 1 : 0); }

        public static void AddStanding(Settlements.Settlement s, int points)
        {
            if (s == null || points <= 0) return;
            int before = Rank(s);
            var e = S.standing.Find(x => x.id == s.id);
            if (e == null) { e = new StandingEntry { id = s.id }; S.standing.Add(e); }
            e.points += points;
            int after = Rank(s);
            if (after > before)
                GameEvents.Good($"{s.name} now sees you as: {RankName(after)}. Kinkeeper prices -{Mathf.RoundToInt(DiscountPerRank * after * 100)}%{(ExtraOffers(s) > 0 ? $", +{ExtraOffers(s)} kin on offer" : "")}.");
        }

        // ---------------------------------------------------------------- board
        public static List<SettleRequest> For(Settlements.Settlement s)
        {
            var list = new List<SettleRequest>();
            foreach (var r in S.requests) if (r.settlement == s.id) list.Add(r);
            while (list.Count < Slots)
            {
                var r = Make(s, list);
                S.requests.Add(r);
                list.Add(r);
            }
            return list;
        }

        static string[] DeliverPool(Settlements.Settlement s) =>
            s.biome == BiomeId.Meadow ? new[] { "wood", "fiber", "berry", "goldgrain", "dewmelon", "trail_mix", "grain_loaf" }
            : s.biome == BiomeId.Badlands ? new[] { "stone", "ore", "ingot", "cinder", "emberpepper", "pepper_skewer", "pelt" }
            : new[] { "wood", "crystal", "pelt", "frostmint", "mint_tea", "ingot", "gel" };

        static SettleRequest Make(Settlements.Settlement s, List<SettleRequest> existing)
        {
            var rng = new System.Random(Environment.TickCount ^ (S.requests.Count * 7919 + s.index * 131));
            float tier = 1f + s.index * 0.8f;
            var types = new List<RequestType> { RequestType.Deliver, RequestType.Deliver, RequestType.CatchSpecies, RequestType.DefeatElement, RequestType.BeatTamer };
            foreach (var e in existing) if (e.type != RequestType.Deliver) types.Remove(e.type);
            var t = types[rng.Next(types.Count)];
            var r = new SettleRequest { settlement = s.id, type = t };
            switch (t)
            {
                case RequestType.Deliver:
                {
                    var pool = new List<string>(DeliverPool(s));
                    foreach (var e in existing) if (e.type == RequestType.Deliver) pool.Remove(e.param);
                    string id = pool[rng.Next(pool.Count)];
                    var d = ContentDB.Item(id);
                    bool meal = d != null && d.mealMods != null && d.mealMods.Length > 0;
                    bool crop = Crops.TryGet(id, out _);
                    int baseN = meal ? 2 : crop ? 6 : id == "ingot" || id == "crystal" ? 4 : id == "ore" || id == "pelt" || id == "gel" || id == "cinder" ? 8 : 20;
                    r.param = id;
                    r.target = baseN + rng.Next(0, baseN / 2 + 1);
                    r.title = $"Bring {r.target} {ContentDB.ItemName(id)}";
                    int each = meal ? 30 : crop ? 6 : id == "ingot" ? 9 : id == "crystal" ? 14 : id == "ore" || id == "pelt" || id == "gel" || id == "cinder" ? 4 : 1;
                    r.glimmer = Mathf.RoundToInt((15 + r.target * each) * (0.8f + tier * 0.2f) / 5f) * 5;
                    r.standing = meal ? 18 : 12;
                    break;
                }
                case RequestType.CatchSpecies:
                {
                    var pool = new List<SpeciesDef>();
                    foreach (var sp in ContentDB.SpeciesList)
                        if (!sp.starter && sp.rarity <= Rarity.Uncommon && sp.LivesIn(s.biome) && sp.spawnCondition == SpawnCondition.None && sp.spawnTime != SpawnTime.Night) pool.Add(sp);
                    var pick = pool.Count > 0 ? pool[rng.Next(pool.Count)] : null;
                    r.param = pick != null ? pick.id : "";
                    r.target = 1;
                    r.title = pick != null ? $"Catch a {pick.displayName} (you keep it)" : "Catch any wild kin";
                    r.glimmer = Mathf.RoundToInt((40 + (pick != null && pick.rarity == Rarity.Uncommon ? 30 : 0)) * tier / 5f) * 5;
                    r.standing = 16;
                    break;
                }
                case RequestType.DefeatElement:
                {
                    var els = new List<Element>();
                    foreach (var sp in ContentDB.SpeciesList)
                        if (!sp.starter && sp.LivesIn(s.biome) && sp.element1 != Element.Normal && !els.Contains(sp.element1)) els.Add(sp.element1);
                    var e = els.Count > 0 ? els[rng.Next(els.Count)] : Element.Flora;
                    r.param = e.ToString();
                    r.target = 4 + rng.Next(0, 4);
                    r.title = $"Drive off {r.target} {e} kin";
                    r.glimmer = Mathf.RoundToInt((20 + r.target * 8) * tier / 5f) * 5;
                    r.standing = 14;
                    break;
                }
                case RequestType.BeatTamer:
                    r.target = 1;
                    r.title = "Win a match against a tamer stationed nearby";
                    r.glimmer = Mathf.RoundToInt(60 * tier / 5f) * 5;
                    r.standing = 22;
                    break;
            }
            // a little of the local crop on top, so every settlement can seed your plots
            if (rng.NextDouble() < 0.55)
            {
                r.rewardItem = s.biome == BiomeId.Meadow ? (rng.Next(2) == 0 ? "goldgrain" : "dewmelon") : s.biome == BiomeId.Badlands ? "emberpepper" : "frostmint";
                r.rewardCount = 3;
            }
            return r;
        }

        public static int Progress(SettleRequest r) =>
            r.type == RequestType.Deliver ? Mathf.Min(r.target, GameManager.I.Player.Stats.Bag.Count(r.param)) : Mathf.Min(r.target, r.progress);
        public static bool Ready(SettleRequest r) => Progress(r) >= r.target;

        public static bool Claim(Settlements.Settlement s, SettleRequest r)
        {
            if (!Ready(r)) return false;
            var st = S;
            var gm = GameManager.I;
            if (r.type == RequestType.Deliver) st.Bag.Remove(r.param, r.target);
            st.glimmer += r.glimmer;
            if (!string.IsNullOrEmpty(r.rewardItem)) gm.GiveItem(r.rewardItem, r.rewardCount, gm.Player.Position + Vector3.up);
            gm.AddPlayerXp(25 + r.glimmer / 3);
            GameEvents.Good($"{s.name} thanks you: +{r.glimmer} Glimmer{(string.IsNullOrEmpty(r.rewardItem) ? "" : $", {r.rewardCount}x {ContentDB.ItemName(r.rewardItem)}")}.");
            AddStanding(s, r.standing);
            SynthAudio.Play(Sfx.LevelUp, 1.15f, 0.8f);
            int i = st.requests.IndexOf(r);
            st.requests.RemoveAt(i);
            var mine = new List<SettleRequest>();
            foreach (var x in st.requests) if (x.settlement == s.id) mine.Add(x);
            st.requests.Insert(Mathf.Min(i, st.requests.Count), Make(s, mine));
            GameEvents.RaiseInventoryChanged();
            return true;
        }

        /// <summary>Trade a request you don't fancy for another, for a small fee.</summary>
        public static string Swap(Settlements.Settlement s, SettleRequest r)
        {
            var st = S;
            if (st.glimmer < SwapCost) return "Not enough Glimmer.";
            st.glimmer -= SwapCost;
            int i = st.requests.IndexOf(r);
            st.requests.RemoveAt(i);
            var mine = new List<SettleRequest>();
            foreach (var x in st.requests) if (x.settlement == s.id) mine.Add(x);
            mine.Add(r);   // don't hand back the same kind
            st.requests.Insert(Mathf.Min(i, st.requests.Count), Make(s, mine));
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        // ---------------------------------------------------------------- hooks
        static void Bump(SettleRequest r)
        {
            if (r.progress >= r.target) return;
            r.progress++;
            if (r.progress >= r.target)
            {
                var s = Settlements.All.Find(x => x.id == r.settlement);
                GameEvents.Good($"Request done: {r.title}. Claim it at the board in {(s != null ? s.name : "the settlement")}.");
            }
        }

        public static void OnCapture(CreatureData d)
        {
            foreach (var r in S.requests)
                if (r.type == RequestType.CatchSpecies && (string.IsNullOrEmpty(r.param) || r.param == d.speciesId)) Bump(r);
        }

        public static void OnDefeat(CreatureData d)
        {
            foreach (var r in S.requests)
                if (r.type == RequestType.DefeatElement && (d.Element1.ToString() == r.param || d.Element2.ToString() == r.param)) Bump(r);
        }

        public static void OnTamerBeaten(Settlements.Settlement s)
        {
            if (s == null) return;
            foreach (var r in S.requests)
                if (r.type == RequestType.BeatTamer && r.settlement == s.id) Bump(r);
        }
    }
}
