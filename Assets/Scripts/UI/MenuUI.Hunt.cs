using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Maro the Seeker's legend hunts (notebook of clues), the Dex milestone list, and their lines on the Journey page.</summary>
    public partial class MenuUI
    {
        public void OpenHunt() => Open(MenuTab.Hunt);

        void BuildHunt()
        {
            var gm = GameManager.I;
            var stats = gm.Player.Stats;
            Vector3 pp = gm.Player.Position;
            bool atMaro = LegendHunts.I != null && LegendHunts.I.AtHunter(pp);
            LeftRight(out var left, out var right, 0.6f);
            var list = TrackedList(left);
            Header(list, $"{LegendHunts.HunterName}  <size=24>{UIFactory.Col($"Legends found {LegendHunts.Done}/{LegendHunts.All.Count}", Gold)}</size>", 36);
            var h = LegendHunts.Current;
            if (h == null)
            {
                Para(list, "\"That's all of them. Every legend I ever chased, and you walked up to each one. I think I'll just sit here and watch the sky for a while.\"", 24, Color.white);
            }
            else
            {
                var sp = h.Sp;
                bool known = sp != null && stats.DexFor(sp.id, false) != null && stats.DexFor(sp.id, false).seen > 0;
                Para(list, $"<b>Hunt {LegendHunts.Done + 1}: {(known ? sp.displayName : "an unknown legend")}</b>   <size=21>{UIFactory.Col(BiomeInfo.Name(h.land), BiomeInfo.Tint(h.land))}  ·  around Lv {h.level}</size>", 28, Color.white);
                Para(list, $"\"{h.intro}\"", 22);
                if (!LegendHunts.Accepted)
                {
                    Para(list, "A hunt is three clues across that land, then the legend's lair. Legends are high level and resist every seed: even a Starbloom Seed at 10% health is only about a 55% catch. " +
                               $"The last clue gives you {LegendHunts.SeedsPerHunt} seeds made for that legend alone: get it to 10% health or less and they cannot fail. " +
                               "Defeat it by mistake and it is back at its lair ten minutes later. Clues and lairs sometimes only show at a certain time of day.", 21, Color.white);
                    if (atMaro) ActionBtn(list, "ACCEPT THE HUNT", () => { string err = LegendHunts.I.Accept(); if (err != null) GameEvents.Warn(err); Rebuild(); }, new Color(0.6f, 0.45f, 0.2f));
                    else ActionBtn(list, $"TALK TO MARO TO ACCEPT  ({Wardens.Compass(pp, LegendHunts.HunterPos)})", null, new Color(0.3f, 0.3f, 0.32f));
                }
                else
                {
                    Header(list, "Notebook", 28);
                    int found = Mathf.Min(stats.huntStep - 1, h.clues.Length);
                    for (int i = 0; i < found; i++)
                        Para(list, $"{UIFactory.Col($"Clue {i + 1}.", Gold)} {h.clues[i].find}", 22, Color.white);
                    if (found == 0) Para(list, "Nothing found yet.", 21);
                    Header(list, LegendHunts.AtLair ? "The lair" : $"Lead  <size=22>clue {stats.huntStep}/{h.clues.Length}</size>", 28);
                    Para(list, LegendHunts.NextLine(pp), 25, Color.white);
                    if (LegendHunts.AtLair)
                    {
                        int retry = LegendHunts.RetryIn;
                        Para(list, retry > 0 ? UIFactory.Col($"It is resting after your last fight - back in {retry / 60}:{retry % 60:00}.", new Color(1f, 0.8f, 0.5f))
                                 : LegendHunts.TimeOk(h.when) ? UIFactory.Col("The hour is right. Walk into the ring of stones.", new Color(0.6f, 1f, 0.6f))
                                 : $"Wait there until it is {LegendHunts.TimeText(h.when).Replace("at ", "").Replace("around ", "")}.", 22, Color.white);
                        int have = stats.Bag.Count(h.seed);
                        string seedName = ContentDB.ItemName(h.seed);
                        Para(list, $"{seedName}s in your bag: <b>{have}</b>. Readied automatically when it appears. At 10% health or less they cannot fail (a Starbloom is about 55% there); above that they are no better than a Starbloom. " +
                                   "It has far more health than a wild kin, so take your time.", 20, Color.white);
                        int price = LegendHunts.RestockPrice;
                        if (atMaro) ActionBtn(list, $"{LegendHunts.SeedsPerHunt} MORE {seedName.ToUpper()}S  -  {price} G", stats.glimmer >= price
                            ? () => { string err = LegendHunts.I.Restock(); if (err != null) GameEvents.Warn(err); Rebuild(); } : (System.Action)null,
                            stats.glimmer >= price ? new Color(0.3f, 0.45f, 0.4f) : new Color(0.3f, 0.3f, 0.32f));
                        else if (have == 0) Para(list, UIFactory.Col($"Out of them? Maro makes {LegendHunts.SeedsPerHunt} more for {price} Glimmer.", new Color(1f, 0.8f, 0.5f)), 20);
                    }
                    else
                    {
                        var c = LegendHunts.ActiveClue;
                        Para(list, "Within about 90 m a thread of light rises from the clue. Stand on it and tap EXAMINE." +
                                   (c.when != TimeReq.Any && !LegendHunts.TimeOk(c.when) ? UIFactory.Col($"  This one only shows {LegendHunts.TimeText(c.when)}.", new Color(1f, 0.8f, 0.5f)) : ""), 20);
                    }
                    Para(list, $"Reward: {UIFactory.Col($"{h.glimmer} Glimmer", Gold)}, {h.rewardCount}x {ContentDB.ItemName(h.reward)} - and the legend itself.", 22, Color.white);
                }
            }

            // ---------------- the shimmer seed ----------------
            if (h != null)
            {
                Header(list, "Prismatic Seed", 28);
                int own = stats.Bag.Count(LegendHunts.PrismSeed), shards = stats.Bag.Count(Shimmer.ShardItem);
                Para(list, "\"One seed, months of work. Whichever legend you catch with it comes out a shimmer - and a legend is only ever caught once.\"\n" +
                           $"Works on any hunted legend, certain at 10% health or less, and it returns to your hand if a throw fails.  In your bag: <b>{own}</b>", 21, Color.white);
                bool can = stats.glimmer >= LegendHunts.PrismGlimmer && shards >= LegendHunts.PrismShards;
                string cost = $"{UIFactory.Col($"{LegendHunts.PrismGlimmer} Glimmer", stats.glimmer >= LegendHunts.PrismGlimmer ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f))} (you have {stats.glimmer})  +  " +
                              $"{UIFactory.Col($"{LegendHunts.PrismShards} Prism Shards", shards >= LegendHunts.PrismShards ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f))} (you carry {shards})";
                Para(list, cost, 21, Color.white);
                if (!atMaro) ActionBtn(list, "BUY FROM MARO IN PERSON", null, new Color(0.3f, 0.3f, 0.32f));
                else ActionBtn(list, can ? "BUY PRISMATIC SEED" : "NOT ENOUGH YET", can ? () => { string err = LegendHunts.I.BuyPrism(); if (err != null) GameEvents.Warn(err); Rebuild(); } : (System.Action)null,
                    can ? new Color(0.6f, 0.35f, 0.6f) : new Color(0.3f, 0.3f, 0.32f));
            }

            // ---------------- the legends ----------------
            var rl = UIFactory.ScrollList(right, out _, 10f);
            ((RectTransform)rl.parent).Stretch();
            Header(rl, "Legends", 30);
            for (int i = 0; i < LegendHunts.All.Count; i++)
            {
                var hh = LegendHunts.All[i];
                var sp = hh.Sp;
                if (sp == null) continue;
                var e = stats.DexFor(sp.id, false);
                bool seen = e != null && e.seen > 0, done = i < LegendHunts.Done;
                var row = UIFactory.HRow(rl, 96, 12);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + sp.id); pic.preserveAspect = true; pic.raycastTarget = false;
                pic.color = seen ? Color.white : new Color(0f, 0f, 0f, 0.75f);
                UIFactory.Size(pic, 84, 84);
                string state = done ? UIFactory.Col("Caught", new Color(0.6f, 1f, 0.6f)) : i == LegendHunts.Done ? (LegendHunts.Accepted ? UIFactory.Col("On the trail", Gold) : "Next hunt") : UIFactory.Col("Not yet", UIFactory.TextDim);
                var lb = UIFactory.Label(row.transform, $"<b>{(seen ? sp.displayName : "???")}</b>\n<size=20>{UIFactory.Col(BiomeInfo.Name(hh.land), BiomeInfo.Tint(hh.land))}  ·  Lv {hh.level}  ·  {state}</size>", 26, Color.white);
                lb.supportRichText = true; UIFactory.Size(lb, 88, 480);
            }
        }

        // ============================================================ DEX MILESTONES
        void ShowDexMilestones()
        {
            ShowModal("Dex milestones", list =>
            {
                var stats = GameManager.I.Player.Stats;
                int caught = DexRewards.Caught(stats);
                Para(list, $"Different species caught: <b>{caught}/{DexRewards.Total}</b>.  Each milestone also adds +{Mathf.RoundToInt(DexRewards.CatchPerMilestone * 100f)}% catch chance for good " +
                           $"(now +{Mathf.RoundToInt((DexRewards.MilestoneCatchMul(stats) - 1f) * 100f)}%).", 24, Color.white);
                for (int i = 0; i < DexRewards.Milestones.Length; i++)
                {
                    var m = DexRewards.Milestones[i];
                    int ii = i, need = DexRewards.Need(m);
                    bool claimed = DexRewards.Claimed(stats, i), ready = DexRewards.Ready(stats, i);
                    var row = UIFactory.HRow(list, 92, 12);
                    row.childForceExpandHeight = false;
                    var t = UIFactory.Label(row.transform, $"<b>{(m.count == 0 ? "Every species" : need + " species")}</b>  <size=20>{Mathf.Min(caught, need)}/{need}</size>\n<size=20>{DexRewards.RewardText(m)}</size>", 26, claimed ? new Color(1, 1, 1, 0.6f) : Color.white);
                    t.supportRichText = true; UIFactory.Size(t, 86, 760);
                    UIFactory.Size(UIFactory.Button(row.transform, claimed ? "CLAIMED" : ready ? "CLAIM" : "Not yet",
                        ready ? () => { DexRewards.Claim(ii); CloseModal(); Rebuild(); ShowDexMilestones(); } : (System.Action)null,
                        ready ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.3f, 0.3f, 0.32f), 24), 76, 190);
                }
            });
        }

        // ============================================================ JOURNEY LINES
        void JourneyHunts(RectTransform list)
        {
            Header(list, $"Legend hunts  <size=24>{UIFactory.Col($"{LegendHunts.Done}/{LegendHunts.All.Count}", Gold)}</size>", 32);
            var row = UIFactory.HRow(list, 84, 12);
            row.childForceExpandHeight = false;
            var t = UIFactory.Label(row.transform, $"<size=22>{LegendHunts.NextLine(GameManager.I.Player.Position)}</size>", 24, Color.white);
            t.supportRichText = true; UIFactory.Size(t, 78, 1100);
            UIFactory.Size(UIFactory.Button(row.transform, "NOTEBOOK", OpenHunt, new Color(0.32f, 0.34f, 0.4f), 22), 76, 190);
        }

        void JourneyDex(RectTransform list)
        {
            var stats = GameManager.I.Player.Stats;
            int caught = DexRewards.Caught(stats), ready = DexRewards.ReadyCount(stats), next = DexRewards.NextIndex(stats);
            Header(list, $"Kin Dex  <size=24>{UIFactory.Col($"{caught}/{DexRewards.Total} species caught", Gold)}</size>", 32);
            var row = UIFactory.HRow(list, 84, 12);
            row.childForceExpandHeight = false;
            string line = next < 0 ? "Every milestone claimed."
                : $"Next milestone: {DexRewards.Need(DexRewards.Milestones[next])} species - {DexRewards.RewardText(DexRewards.Milestones[next])}.";
            var t = UIFactory.Label(row.transform, $"<size=22>{line}</size>", 24, Color.white);
            t.supportRichText = true; UIFactory.Size(t, 78, 1100);
            UIFactory.Size(UIFactory.Button(row.transform, ready > 0 ? $"CLAIM ({ready})" : "MILESTONES", ShowDexMilestones, ready > 0 ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.32f, 0.34f, 0.4f), 22), 76, 190);
        }
    }
}
