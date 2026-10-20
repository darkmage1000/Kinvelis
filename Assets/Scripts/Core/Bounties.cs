using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public enum BountyType { Catch, CatchElement, Defeat, Deliver, ClearCave, Alpha }

    [Serializable]
    public class Bounty
    {
        public BountyType type;
        public string param;          // element name / item id / cave id
        public int target, progress;
        public int glimmer;
        public string rewardItem;
        public int rewardCount;
        public string title;
        public bool Done => progress >= target;
    }

    /// <summary>
    /// Bounty board (Camp > Bounties): three rotating tasks paid in Glimmer + items. Progress is fed by game hooks
    /// (captures, defeats, cave guardians, alphas); Deliver bounties take the items when claimed.
    /// </summary>
    public static class Bounties
    {
        public const int Slots = 3;
        public const double RerollCooldown = 15 * 60;

        static PlayerStats S => GameManager.I.Player.Stats;

        public static void Ensure()
        {
            var s = S;
            while (s.bounties.Count < Slots) s.bounties.Add(Make(s));
        }

        static Bounty Make(PlayerStats s)
        {
            var rng = new System.Random(Environment.TickCount ^ s.bounties.Count * 7919);
            int lv = s.level;
            var options = new List<BountyType> { BountyType.Catch, BountyType.CatchElement, BountyType.Defeat, BountyType.Deliver, BountyType.Deliver };
            if (s.cavesFound.Count > 0) options.Add(BountyType.ClearCave);
            if (lv >= 4) options.Add(BountyType.Alpha);
            // avoid duplicate kinds on the board
            foreach (var b in s.bounties) if (b.type != BountyType.Deliver) options.Remove(b.type);
            var t = options[rng.Next(options.Count)];
            var bo = new Bounty { type = t };
            switch (t)
            {
                case BountyType.Catch:
                    bo.target = 2 + rng.Next(0, 3);
                    bo.title = $"Catch {bo.target} wild kin";
                    bo.glimmer = 25 + bo.target * 10;
                    break;
                case BountyType.CatchElement:
                {
                    var els = new List<Element>();
                    foreach (var sp in ContentDB.SpeciesList)
                        if (!sp.starter && s.DexFor(sp.id, false) != null && !els.Contains(sp.element1)) els.Add(sp.element1);
                    if (els.Count == 0) els.Add(Element.Flora);
                    var e = els[rng.Next(els.Count)];
                    bo.param = e.ToString();
                    bo.target = 1 + rng.Next(0, 2);
                    bo.title = $"Catch {bo.target} {e} kin";
                    bo.glimmer = 35 + bo.target * 15;
                    break;
                }
                case BountyType.Defeat:
                    bo.target = 5 + rng.Next(0, 6);
                    bo.title = $"Defeat {bo.target} wild kin";
                    bo.glimmer = 20 + bo.target * 5;
                    break;
                case BountyType.Deliver:
                {
                    string[] items = lv < 5 ? new[] { "wood", "stone", "fiber", "berry" } : new[] { "wood", "stone", "fiber", "ore", "ingot", "pelt", "gel" };
                    string id = items[rng.Next(items.Length)];
                    int baseN = id == "ingot" ? 4 : id == "ore" || id == "pelt" || id == "gel" ? 8 : 25;
                    bo.param = id;
                    bo.target = baseN + rng.Next(0, baseN / 2 + 1);
                    bo.title = $"Deliver {bo.target} {ContentDB.ItemName(id)}";
                    bo.glimmer = 15 + bo.target * (id == "ingot" ? 8 : id == "ore" || id == "pelt" || id == "gel" ? 3 : 1);
                    break;
                }
                case BountyType.ClearCave:
                {
                    var id = s.cavesFound[rng.Next(s.cavesFound.Count)];
                    var cave = Caves.I != null ? Caves.I.Entrances.Find(x => x.id == id) : null;
                    bo.param = id;
                    bo.target = 1;
                    bo.title = $"Defeat the guardian of {(cave != null ? cave.name : id)}";
                    bo.glimmer = cave != null ? 80 + cave.maxLv * 6 : 120;
                    break;
                }
                case BountyType.Alpha:
                    bo.target = 1;
                    bo.title = "Defeat or catch an alpha";
                    bo.glimmer = 140;
                    break;
            }
            // bonus item scaled to the player's stage
            string seedR = Seeds.ForLevel(lv + 4);   // the seed rated for kin a little above you
            string[] pool = lv < 5 ? new[] { seedR, "salve", "berry_tart" } : lv < 10 ? new[] { seedR, "salve", "ingot", "kin_treat" } : new[] { seedR, seedR, "kin_treat", "crystal" };
            bo.rewardItem = pool[rng.Next(pool.Length)];
            bo.rewardCount = bo.rewardItem == "tether_seed" ? 5 : bo.rewardItem == seedR ? 3 : 2;
            return bo;
        }

        // ---------------------------------------------------------------- hooks
        public static void OnCapture(CreatureData d)
        {
            foreach (var b in S.bounties)
            {
                if (b.Done) continue;
                if (b.type == BountyType.Catch) Bump(b);
                else if (b.type == BountyType.CatchElement && (d.Element1.ToString() == b.param || d.Element2.ToString() == b.param)) Bump(b);
                else if (b.type == BountyType.Alpha && d.alpha) Bump(b);
            }
        }

        public static void OnDefeat(CreatureData d)
        {
            foreach (var b in S.bounties)
            {
                if (b.Done) continue;
                if (b.type == BountyType.Defeat) Bump(b);
                else if (b.type == BountyType.Alpha && d.alpha) Bump(b);
            }
        }

        public static void OnCaveCleared(string caveId)
        {
            foreach (var b in S.bounties)
                if (!b.Done && b.type == BountyType.ClearCave && b.param == caveId) Bump(b);
        }

        static void Bump(Bounty b)
        {
            b.progress = Mathf.Min(b.target, b.progress + 1);
            if (b.Done) GameEvents.Good($"Bounty complete: {b.title} - claim it at camp (Camp > Bounties).");
        }

        /// <summary>Deliver bounties count what you carry + camp storage.</summary>
        public static int Progress(Bounty b) => b.type == BountyType.Deliver ? Mathf.Min(b.target, ItemSources.Count(b.param)) : b.progress;
        public static bool Ready(Bounty b) => Progress(b) >= b.target;

        public static bool Claim(Bounty b)
        {
            var s = S;
            if (!Ready(b)) return false;
            if (b.type == BountyType.Deliver) ItemSources.Consume(new[] { new Ingredient(b.param, b.target) });
            s.glimmer += b.glimmer;
            var gm = GameManager.I;
            if (!string.IsNullOrEmpty(b.rewardItem)) gm.GiveItem(b.rewardItem, b.rewardCount, gm.Player.Position + Vector3.up);
            gm.AddPlayerXp(30 + b.glimmer / 3);
            GameEvents.Good($"+{b.glimmer} Glimmer{(string.IsNullOrEmpty(b.rewardItem) ? "" : $" and {b.rewardCount}x {ContentDB.ItemName(b.rewardItem)}")}!");
            SynthAudio.Play(Sfx.LevelUp, 1.15f, 0.8f);
            int i = s.bounties.IndexOf(b);
            s.bounties.RemoveAt(i);
            s.bounties.Insert(i, Make(s));
            GameEvents.RaiseInventoryChanged();
            return true;
        }

        public static bool CanReroll => GameManager.WorldTime >= S.bountyRerollAt;

        public static void Reroll()
        {
            var s = S;
            if (!CanReroll) return;
            s.bounties.RemoveAll(b => !Ready(b));
            Ensure();
            s.bountyRerollAt = GameManager.WorldTime + RerollCooldown;
            GameEvents.RaiseInventoryChanged();
        }
    }
}
