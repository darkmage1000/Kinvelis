using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Trade screen with Wayfarer Odo: buy on the left, sell your surplus on the right.</summary>
    public partial class MenuUI
    {
        void BuildTrader()
        {
            SaveScroll();
            var tr = Trader.I;
            var s = GameManager.I.Player.Stats;
            LeftRight(out var left, out var right, 0.5f);
            var gold = new Color(1f, 0.85f, 0.35f);

            var bl = TrackedList(left);
            Header(bl, Trader.Name, 34);
            Para(bl, UIFactory.Col($"Your Glimmer: <b>{s.glimmer} G</b>", gold), 32, Color.white);
            Para(bl, "\"Fresh stock at every waystone, friend. I'm off again before long!\"", 21);
            foreach (var o in tr.Stock)
            {
                var oo = o;
                var row = UIFactory.HRow(bl, 92, 12);
                if (o.egg)
                {
                    var ic = UIFactory.Icon(row.transform, new Color(1f, 0.92f, 0.75f), "?", 72, true); UIFactory.Size(ic, 72, 72);
                }
                else { var ic = UIFactory.ItemIcon(row.transform, ContentDB.Item(o.id), 72); UIFactory.Size(ic, 72, 72); }
                string name = o.egg ? "Mystery Egg" : $"{ContentDB.ItemName(o.id)}{(o.count > 1 ? $" x{o.count}" : "")}";
                if (!o.egg) { int own = s.Bag.Count(o.id); if (own > 0) name += $"</b>  <size=18>{UIFactory.Col($"have {own}", UIFactory.TextDim)}</size><b>"; }
                string desc = o.egg ? "Hatches a kin you've seen, with a good quality roll (needs a Hatchery)." : ContentDB.Item(o.id)?.description ?? "";
                var t = UIFactory.Label(row.transform, $"<b>{name}</b>\n<size=18>{desc}</size>", 24, Color.white);
                t.supportRichText = true; UIFactory.Size(t, 86, 470);
                bool afford = s.glimmer >= o.price;
                UIFactory.Size(UIFactory.Button(row.transform, afford ? $"{o.price} G" : $"{o.price} G\nneed {o.price - s.glimmer}", afford ? () => { string err = tr.Buy(oo); if (err != null) GameEvents.Warn(err); Rebuild(); } : (System.Action)null,
                    afford ? new Color(0.55f, 0.45f, 0.2f) : new Color(0.3f, 0.3f, 0.32f), afford ? 22 : 19), 80, 150);
            }
            RestoreScroll();

            var sl = UIFactory.ScrollList(right, out _);
            ((RectTransform)sl.parent).Stretch();
            Header(sl, $"Sell  <size=24>{UIFactory.Col($"you have {s.glimmer} G", gold)}</size>", 32);
            bool any = false;
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var st in s.Bag.slots)
            {
                if (st.Empty || seen.Contains(st.id)) continue;
                seen.Add(st.id);
                var d = ContentDB.Item(st.id);
                int price = Trader.SellPrice(d);
                if (price <= 0) continue;
                any = true;
                string id = st.id;
                int have = s.Bag.Count(id);
                var row = UIFactory.HRow(sl, 84, 10);
                var ic = UIFactory.ItemIcon(row.transform, d, 64); UIFactory.Size(ic, 64, 64);
                var t = UIFactory.Label(row.transform, $"<b>{d.displayName}</b> x{have}\n<size=19>{price} G each</size>", 23, Color.white);
                t.supportRichText = true; UIFactory.Size(t, 76, 300);
                UIFactory.Size(UIFactory.Button(row.transform, "SELL 1", () => { tr.Sell(id, 1); Rebuild(); }, new Color(0.3f, 0.42f, 0.5f), 20), 70, 130);
                if (have > 1) UIFactory.Size(UIFactory.Button(row.transform, $"ALL +{price * have}", () => Confirm("Sell all?", $"Sell {have}x {d.displayName} for {price * have} Glimmer?", () => { tr.Sell(id, have); Rebuild(); }), new Color(0.5f, 0.42f, 0.25f), 20), 70, 170);
            }
            if (!any) Para(sl, "Nothing to sell. Odo buys materials, food, seeds and gear.", 22);
        }
    }
}
