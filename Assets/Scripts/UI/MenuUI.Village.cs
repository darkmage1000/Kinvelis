using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Settlement request board (jobs + standing) and the rival tamer challenge screen.</summary>
    public partial class MenuUI
    {
        Settlements.Settlement requestsAt;
        Tamers.Tamer tamerAt;

        public void OpenRequests(Settlements.Settlement s)
        {
            if (s != requestsAt) savedScroll = 1f;
            requestsAt = s;
            Open(MenuTab.Requests);
        }

        public void OpenTamer(Tamers.Tamer t)
        {
            tamerAt = t;
            Open(MenuTab.Tamer);
        }

        void BuildRequests()
        {
            SaveScroll();
            var s = requestsAt ?? (Settlements.All.Count > 0 ? Settlements.All[0] : null);
            if (s == null) { Header(content, "No board here"); return; }
            var stats = GameManager.I.Player.Stats;
            var gold = new Color(1f, 0.85f, 0.35f);
            LeftRight(out var left, out var right, 0.62f);

            // ---------------- requests ----------------
            var bl = TrackedList(left);
            Header(bl, $"{s.name} requests  <size=24>{UIFactory.Col($"{stats.glimmer} G", gold)}</size>", 34);
            Para(bl, "Three jobs stay open. Finish one and another takes its place. Deliveries are taken from your bag.", 20);
            foreach (var r in Requests.For(s))
            {
                var rr = r;
                int prog = Requests.Progress(r);
                bool ready = Requests.Ready(r);
                var row = UIFactory.HRow(bl, 118, 12);
                row.childForceExpandHeight = false;
                if (r.type == RequestType.Deliver)
                {
                    var ic = UIFactory.ItemIcon(row.transform, ContentDB.Item(r.param), 84); UIFactory.Size(ic, 84, 84);
                }
                else
                {
                    var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                    pic.sprite = r.type == RequestType.CatchSpecies && !string.IsNullOrEmpty(r.param) ? ItemIcons.Get("kin_" + r.param) : null;
                    pic.preserveAspect = true; pic.raycastTarget = false;
                    if (pic.sprite == null) pic.color = r.type == RequestType.BeatTamer ? new Color(1f, 0.4f, 0.3f, 0.5f) : new Color(1, 1, 1, 0.12f);
                    UIFactory.Size(pic, 84, 84);
                }
                string reward = UIFactory.Col($"{r.glimmer} G", gold) + (string.IsNullOrEmpty(r.rewardItem) ? "" : $" + {r.rewardCount}x {ContentDB.ItemName(r.rewardItem)}") + $"  ·  standing +{r.standing}";
                var t = UIFactory.Label(row.transform, $"<b>{r.title}</b>\n<size=20>{UIFactory.Col($"{prog}/{r.target}", ready ? new Color(0.6f, 1f, 0.6f) : Color.white)}   {reward}</size>", 25, Color.white);
                t.supportRichText = true; UIFactory.Size(t, 104, 560);
                if (ready)
                    UIFactory.Size(UIFactory.Button(row.transform, "CLAIM", () => { Requests.Claim(s, rr); Rebuild(); }, new Color(0.25f, 0.55f, 0.4f), 24), 84, 170);
                else
                    UIFactory.Size(UIFactory.Button(row.transform, $"SWAP\n{Requests.SwapCost} G", stats.glimmer >= Requests.SwapCost
                        ? () => { string err = Requests.Swap(s, rr); if (err != null) GameEvents.Warn(err); Rebuild(); } : (System.Action)null, new Color(0.32f, 0.34f, 0.4f), 20), 84, 170);
            }
            RestoreScroll();

            // ---------------- standing ----------------
            var sl = UIFactory.ScrollList(right, out _);
            ((RectTransform)sl.parent).Stretch();
            int rank = Requests.Rank(s), pts = Requests.Points(s);
            Header(sl, "Standing", 30);
            Para(sl, $"<b>{UIFactory.Col(Requests.RankName(rank), s.color)}</b>  <size=20>rank {rank}/{Requests.MaxRank}</size>", 28, Color.white);
            var bar = UIFactory.Bar(sl, s.color, out var fill);
            UIFactory.Size(bar, 22);
            int floor = Requests.RankFloor(rank), next = Requests.NextRankAt(rank);
            fill.SetFill(rank >= Requests.MaxRank ? 1f : (pts - floor) / (float)Mathf.Max(1, next - floor));
            Para(sl, rank >= Requests.MaxRank ? $"{pts} standing - as high as it goes." : $"{pts}/{next} to {Requests.RankName(rank + 1)}", 20);
            Header(sl, "What it gets you", 26);
            Para(sl, $"Kinkeeper prices: {(rank > 0 ? UIFactory.Col($"-{Mathf.RoundToInt(Requests.DiscountPerRank * rank * 100)}%", new Color(0.6f, 1f, 0.6f)) : "full price")}  <size=18>(-{Mathf.RoundToInt(Requests.DiscountPerRank * 100)}% per rank)</size>", 22, Color.white);
            Para(sl, $"Kin on offer: {4 + Requests.ExtraOffers(s)}  <size=18>(+1 at Friend, +1 at Honored)</size>", 22, Color.white);
            Para(sl, "Standing comes from requests here and from beating the tamers stationed near this settlement.", 19);
        }

        void BuildTamer()
        {
            var t = tamerAt;
            if (t == null || Tamers.I == null) { Header(content, "No one here"); return; }
            var gold = new Color(1f, 0.85f, 0.35f);
            var list = TrackedList(content);
            Header(list, $"{t.title} {t.name}  <size=24>{UIFactory.Col("near " + t.home.name, t.home.color)}</size>", 36);
            int wins = Tamers.Wins(t), cd = Tamers.Cooldown(t);
            Para(list, wins == 0 ? "\"Think your kin are ready? Mine have been waiting all day.\"" : $"\"Back for more? You've beaten me {wins} time{(wins == 1 ? "" : "s")}.\"", 22);
            Para(list, "A match is three kin, sent out one at a time. They go for your companion, so keep one out and swap when it tires. You may fight too. " +
                       "Their kin can't be caught. Walk out of the ring (45 m) or get knocked out and you forfeit - nothing is lost but the time.", 21, Color.white);
            Header(list, "Their team", 28);
            int myLv = GameManager.I.Player.Stats.level;
            for (int i = 0; i < t.team.Length; i++)
            {
                var sp = ContentDB.Sp(t.team[i]);
                if (sp == null) continue;
                var row = UIFactory.HRow(list, 96, 12);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + sp.id); pic.preserveAspect = true; pic.raycastTarget = false;
                UIFactory.Size(pic, 80, 80);
                string el = UIFactory.Col(sp.element1.ToString(), ElementInfo.Color(sp.element1)) + (sp.element2 != Element.Normal && sp.element2 != sp.element1 ? "/" + UIFactory.Col(sp.element2.ToString(), ElementInfo.Color(sp.element2)) : "");
                bool skull = t.levels[i] >= myLv + DangerSkull.LevelGap;
                var lb = UIFactory.Label(row.transform, $"<b>{sp.displayName}</b>  {UIFactory.Col($"Lv {t.levels[i]}", skull ? new Color(1f, 0.3f, 0.25f) : Color.white)}{(skull ? UIFactory.Col("  10+ levels over you", new Color(1f, 0.3f, 0.25f)) : "")}\n<size=20>{el}  ·  {UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity))}</size>", 26, Color.white);
                lb.supportRichText = true; UIFactory.Size(lb, 88, 800);
            }
            Header(list, "Prize", 28);
            Para(list, $"{UIFactory.Col($"{Tamers.Purse(t)} Glimmer", gold)}, {t.rewardCount}x {ContentDB.ItemName(t.rewardItem)}, standing with {t.home.name}{(wins == 0 ? "  <size=19>(first win pays in full; rematches 60%)</size>" : "")}", 24, Color.white);
            if (Tamers.I.Active != null) ActionBtn(list, "MATCH IN PROGRESS", null, new Color(0.3f, 0.3f, 0.32f));
            else if (cd > 0) ActionBtn(list, $"RESTING THEIR KIN  ({cd / 60}:{cd % 60:00})", null, new Color(0.3f, 0.3f, 0.32f));
            else ActionBtn(list, "CHALLENGE", () => { string err = Tamers.I.Challenge(t); if (err != null) GameEvents.Warn(err); Close(); }, new Color(0.7f, 0.28f, 0.22f));
        }
    }
}
