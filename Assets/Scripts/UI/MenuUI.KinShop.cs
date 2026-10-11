using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>A settlement Kinkeeper's stall: kin for sale on the left, your own kin to sell on the right.</summary>
    public partial class MenuUI
    {
        Settlements.Settlement kinShop;

        public void OpenKinShop(Settlements.Settlement s)
        {
            if (s != kinShop) savedScroll = 1f;
            kinShop = s;
            Open(MenuTab.KinShop);
        }

        static string KinLine(CreatureData d)
        {
            var sp = d.Species;
            string el = UIFactory.Col(d.Element1.ToString(), ElementInfo.Color(d.Element1)) + (d.Element2 != Element.Normal ? "/" + UIFactory.Col(d.Element2.ToString(), ElementInfo.Color(d.Element2)) : "");
            return $"{el}  ·  {UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity))}";
        }

        static string JobLine(CreatureData d)
        {
            var jobs = new List<string>();
            foreach (var w in d.Species.work) jobs.Add($"{w.type} {d.WorkLevel(w.type)}");
            return jobs.Count > 0 ? string.Join(", ", jobs) : "no camp jobs";
        }

        void BuildKinShop()
        {
            SaveScroll();
            var shops = Settlements.I;
            var s = kinShop ?? (Settlements.All.Count > 0 ? Settlements.All[0] : null);
            if (shops == null || s == null) { Header(content, "No Kinkeeper here"); return; }
            var stats = GameManager.I.Player.Stats;
            var gold = new Color(1f, 0.85f, 0.35f);
            LeftRight(out var left, out var right, 0.54f);

            // ---------------- for sale ----------------
            var bl = TrackedList(left);
            Header(bl, $"{s.keeper}  <size=24>{UIFactory.Col(s.name, s.color)}</size>", 34);
            Para(bl, UIFactory.Col($"Your Glimmer: <b>{stats.glimmer} G</b>", gold), 30, Color.white);
            int left2 = Settlements.RestockInSeconds;
            int rank = Requests.Rank(s);
            Para(bl, $"\"Raised right here, every one of them.\"   New kin in {left2 / 60}:{left2 % 60:00}." +
                     (rank > 0 ? UIFactory.Col($"   {Requests.RankName(rank)}: -{Mathf.RoundToInt(Requests.DiscountPerRank * rank * 100)}%", new Color(0.6f, 1f, 0.6f)) : ""), 20);
            foreach (var o in shops.VisibleOffers(s))
            {
                int cost = Settlements.PriceFor(s, o);
                var oo = o;
                var d = o.kin;
                bool sold = shops.IsSold(o);
                var row = UIFactory.HRow(bl, 132, 12);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + d.speciesId);
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                if (sold) pic.color = new Color(1, 1, 1, 0.3f);
                UIFactory.Size(pic, 104, 104);
                string tags = d.VariantTags();
                var t = UIFactory.Label(row.transform,
                    $"<b>{d.RegionalName}</b>  Lv {d.level}{(tags.Length > 0 ? "  " + UIFactory.Col(tags, gold) : "")}\n<size=19>{KinLine(d)}</size>\n<size=19>{Aptitude.Summary(d)}</size>\n<size=18>{UIFactory.Col(JobLine(d), UIFactory.TextDim)}</size>",
                    25, sold ? UIFactory.TextDim : Color.white);
                t.supportRichText = true;
                UIFactory.Size(t, 124, 470);
                bool afford = stats.glimmer >= cost;
                string label = sold ? "SOLD" : afford ? $"BUY\n{cost} G" : $"{cost} G\nneed {cost - stats.glimmer}";
                System.Action act = sold || !afford ? (System.Action)null : () =>
                    Confirm($"Buy {d.RegionalName}?", $"Lv {d.level} {d.RegionalName} for {cost} Glimmer.", () => { string err = shops.Buy(s, oo); if (err != null) GameEvents.Warn(err); Rebuild(); });
                UIFactory.Size(UIFactory.Button(row.transform, label, act, !sold && afford ? new Color(0.55f, 0.45f, 0.2f) : new Color(0.3f, 0.3f, 0.32f), 22), 96, 160);
            }
            RestoreScroll();

            // ---------------- sell ----------------
            var sl = UIFactory.ScrollList(right, out _);
            ((RectTransform)sl.parent).Stretch();
            Header(sl, "Sell your kin", 30);
            Para(sl, "The village takes them in for good. Partners aren't for sale.", 19);
            var mine = new List<CreatureData>(GameManager.I.Party.All);
            mine.Sort((a, b) => Settlements.SellPrice(b).CompareTo(Settlements.SellPrice(a)));
            foreach (var d in mine)
            {
                var dd = d;
                string why = Settlements.WhyNotSell(d);
                int price = Settlements.SellPrice(d);
                string where = d.location == KinLocation.Party ? "Party" : d.location == KinLocation.Base ? "Camp" : d.location == KinLocation.Expedition ? "Expedition" : "Box";
                var row = UIFactory.HRow(sl, 92, 10);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + d.speciesId);
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                UIFactory.Size(pic, 72, 72);
                var t = UIFactory.Label(row.transform, $"<b>{d.DisplayName}</b>  Lv {d.level}  <size=18>{where}</size>\n<size=18>{Aptitude.Summary(d)}</size>", 22, Color.white);
                t.supportRichText = true;
                UIFactory.Size(t, 84, 330);
                if (why != null) UIFactory.Size(UIFactory.Button(row.transform, why, null, new Color(0.3f, 0.3f, 0.32f), 20), 70, 150);
                else UIFactory.Size(UIFactory.Button(row.transform, $"SELL\n+{price} G",
                    () => Confirm($"Sell {dd.DisplayName}?", $"{dd.DisplayName} (Lv {dd.level}) stays in {s.name} for good. You get {price} Glimmer.", () => { string err = shops.Sell(dd); if (err != null) GameEvents.Warn(err); Rebuild(); }),
                    new Color(0.3f, 0.42f, 0.5f), 20), 70, 150);
            }
        }
    }
}
