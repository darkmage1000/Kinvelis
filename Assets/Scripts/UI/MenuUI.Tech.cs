using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Research Bench screen (the tech tree), outposts (Camp > Outposts and the screen at a tent), and the camp lines on the Journey page.</summary>
    public partial class MenuUI
    {
        // ============================================================ RESEARCH
        void BuildResearch()
        {
            SaveScroll();
            var bm = BaseManager.I;
            var stats = GameManager.I.Player.Stats;
            bool built = bm.HasStation(CraftStation.Research);
            bool inCamp = bm.HasBase && bm.IsInsideBase(GameManager.I.Player.Position, 0f);
            var list = TrackedList(content);
            Header(list, $"Research Bench  <size=24>{UIFactory.Col($"{Tech.DoneCount}/{Tech.All.Length} researched", UIFactory.Accent)}   " +
                         $"{UIFactory.Col($"Sigils {Wardens.Sigils}/{Wardens.SigilsNeeded}", Gold)}   Warden Hearts {ItemSources.Count(Wardens.HeartItem)}</size>", 36);
            Para(list, !built ? "You haven't built a Research Bench yet (Build tab, level 3). This is what it can teach you."
                     : !inCamp ? "You're away from camp, so this is view-only. Go to your Research Bench to start something."
                     : "Pick one thing to study. Materials are taken when you start; then a Crafting kin on the bench works on it, or you can stand at the bench yourself. " +
                       "Each project opens at an explorer level; a land's Sigil comes from beating its Warden (see JOURNEY).", 21);

            var active = Tech.Active;
            if (active != null)
            {
                var card = Card(list, 104, new Color(0.2f, 0.4f, 0.5f, 0.35f));
                var t = UIFactory.Label(card.transform, $"<b>Researching: {active.name}</b>  <size=21>{Mathf.RoundToInt(Tech.Progress01 * 100f)}%</size>", 28, Color.white);
                t.supportRichText = true;
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(900, 40));
                var bar = UIFactory.Bar(card.transform, UIFactory.Accent, out var fill);
                bar.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 16), new Vector2(1000, 26));
                fill.SetFill(Tech.Progress01);
                var cancel = UIFactory.Button(card.transform, "CANCEL", () => Confirm("Stop this research?", "Progress is lost. The materials come back.", () => { Tech.Cancel(); Rebuild(); }), new Color(0.45f, 0.25f, 0.25f), 22);
                ((RectTransform)cancel.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(180, 66));
                int crew = 0;
                foreach (var b in bm.Buildings) if (b.Def.station == CraftStation.Research) crew += b.AssignedCount;
                if (crew == 0) Para(list, "No kin on the bench - it only moves while you stand next to it. Assign a Crafting kin in Camp > Workers.", 20, new Color(1f, 0.8f, 0.5f));
            }

            for (int g = 0; g < 2; g++)
            {
                Header(list, g == 0 ? "Camp know-how" : "Metals of the outer lands", 28);
                foreach (var t in Tech.ByLevel)
                {
                    if (t.group != g) continue;
                    string why = Tech.Blocker(t);
                    bool done = why == "Done", busy = why == "Researching";
                    var parts = new List<string>();
                    foreach (var b in t.buildings) { var d = ContentDB.Building(b); if (d != null) parts.Add(d.displayName); }
                    foreach (var r in t.recipes) parts.Add(ContentDB.ItemName(r));
                    string needs = "";
                    if (!done)
                    {
                        var req = new List<string>();
                        if (t.level > 1) req.Add(UIFactory.Col($"Explorer Lv {t.level}", stats.level >= t.level ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f)));
                        if (t.sigil != null)
                        {
                            var w = Wardens.Get(t.sigil);
                            bool have = Wardens.Beaten(t.sigil);
                            req.Add(UIFactory.Col($"Sigil of {(w != null ? w.land : t.sigil)}{(have ? "" : $" (beat {(w != null ? w.Name : "its Warden")})")}", have ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f)));
                        }
                        if (t.prereq != null) req.Add(UIFactory.Col(Tech.Get(t.prereq).name, Tech.Has(t.prereq) ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f)));
                        needs = (req.Count > 0 ? "Needs: " + string.Join("  +  ", req) + "\n" : "") + Ingredients(t.cost);
                    }
                    float rowH = done ? 96 : 150;
                    var row = UIFactory.HRow(list, rowH, 14);
                    row.childForceExpandHeight = false;
                    var tx = UIFactory.Label(row.transform,
                        $"<b>{t.name}</b>{(done ? UIFactory.Col("   researched", new Color(0.6f, 1f, 0.6f)) : "")}\n<size=20>{UIFactory.Col(t.desc, UIFactory.TextDim)}\n" +
                        $"{UIFactory.Col("Unlocks: " + string.Join(", ", parts), UIFactory.Accent)}{(done ? "" : "\n" + needs)}</size>", 26, done ? new Color(1, 1, 1, 0.7f) : Color.white);
                    tx.supportRichText = true;
                    UIFactory.Size(tx, rowH - 6, 1180);
                    if (done) continue;
                    string label = busy ? "IN PROGRESS" : !built ? "Not built" : !inCamp ? "Go to camp" : why ?? "RESEARCH";
                    var tt = t;
                    bool can = why == null && built && inCamp;
                    var btn = UIFactory.Button(row.transform, label, can ? () => { string err = Tech.Start(tt); if (err != null) GameEvents.Warn(err); Rebuild(); } : (System.Action)null,
                        can ? new Color(0.25f, 0.55f, 0.4f) : busy ? new Color(0.2f, 0.4f, 0.5f) : new Color(0.3f, 0.3f, 0.32f), 22);
                    UIFactory.Size(btn, 84, 250);
                }
            }
            RestoreScroll();
        }

        // ============================================================ OUTPOSTS
        OutpostSave outpostAt;

        public void OpenOutpost(OutpostSave o)
        {
            outpostAt = o;
            Open(MenuTab.Outpost);
        }

        void CampOutposts(RectTransform body)
        {
            var list = TrackedList(body);
            var all = Outposts.All;
            Header(list, $"Outposts  <size=24>{all.Count}/5</size>", 32);
            Para(list, "A tent on an outer land with two kin stationed there. They gather that land's goods while you play or while you're away, and a caravan brings it to camp storage.", 21);
            if (all.Count == 0)
            {
                Para(list, Tech.Has("outposts")
                    ? "Craft an Outpost Kit at the Workbench, carry it to an outer land, and USE it from your bag on dry ground."
                    : "Research Outpost Logistics at a Research Bench (it needs the Sigil of the Driftkey Isles), then craft an Outpost Kit.", 23, Color.white);
                return;
            }
            bool pad = BaseManager.I.HasBuilding("waypad");
            foreach (var o in all)
            {
                var oo = o;
                var card = Card(list, 124);
                int cin = Outposts.CaravanIn(o);
                string caravan = cin < 0 ? (o.kin.Count == 0 ? UIFactory.Col("No crew - nothing is being gathered", new Color(1f, 0.8f, 0.5f)) : "Caravan waiting for goods") : $"Next caravan in {cin / 60}:{cin % 60:00}";
                var crew = new List<string>();
                foreach (var d in Outposts.Crew(o)) crew.Add($"{d.DisplayName} Lv{d.level}");
                var t = UIFactory.Label(card.transform, $"<b>{UIFactory.Col(Outposts.LandName(o), BiomeInfo.Tint(o.Biome))}</b>  <size=21>crew {o.kin.Count}/{Outposts.Slots}   ·   {Outposts.Stock(o)} items waiting   ·   {caravan}</size>\n" +
                    $"<size=21>{(crew.Count > 0 ? string.Join(", ", crew) : "Nobody stationed")}{(pad ? "" : UIFactory.Col("   ·   build a Waypad to fast-travel here", UIFactory.TextDim))}</size>", 28, Color.white);
                t.supportRichText = true;
                t.rectTransform.Stretch(18, 8, 230, 8);
                var btn = UIFactory.Button(card.transform, "MANAGE", () => OpenOutpost(oo), new Color(0.3f, 0.4f, 0.55f), 24);
                ((RectTransform)btn.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(200, 76));
            }
        }

        void BuildOutpost()
        {
            var o = outpostAt;
            if (o == null || !Outposts.All.Contains(o)) { Header(content, "No outpost here"); return; }
            var op = Outposts.I;
            bool here = op.PlayerAt(o);
            LeftRight(out var left, out var right, 0.58f);

            // ---------------- crew ----------------
            var list = TrackedList(left);
            Header(list, $"Outpost  <size=26>{UIFactory.Col(Outposts.LandName(o), BiomeInfo.Tint(o.Biome))}</size>", 36);
            Para(list, "Kin stationed here gather on their own, even while the game is closed (up to 8 hours). What each one brings depends on its best gathering job. " +
                       "A Hauling kin on the crew halves the caravan wait.", 21);
            Header(list, $"Crew  <size=22>{o.kin.Count}/{Outposts.Slots}</size>", 28);
            foreach (var d in Outposts.Crew(o))
            {
                var dd = d;
                var row = UIFactory.HRow(list, 96, 12);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + d.Species.id); pic.preserveAspect = true; pic.raycastTarget = false;
                UIFactory.Size(pic, 80, 80);
                var lb = UIFactory.Label(row.transform, $"<b>{d.DisplayName}</b>  Lv {d.level}\n<size=20>{Outposts.JobText(o, d)}</size>", 26, Color.white);
                lb.supportRichText = true; UIFactory.Size(lb, 88, 620);
                UIFactory.Size(UIFactory.Button(row.transform, "RECALL", () => { op.Recall(o, dd); Rebuild(); }, new Color(0.42f, 0.3f, 0.24f), 22), 76, 170);
            }
            if (o.kin.Count < Outposts.Slots)
                ActionBtn(list, "STATION A KIN", () => OutpostPicker(o), new Color(0.25f, 0.5f, 0.42f));
            int hard; string ore = Outposts.OreOf(o.Biome, out hard);
            Para(list, $"This land's ore is {ContentDB.ItemName(ore)}: a kin needs Quarrying Lv {hard - 2} to mine it here.", 20);
            Para(list, "", 8);
            UIFactory.Size(UIFactory.Button(list, here ? "PACK UP OUTPOST" : "PACK UP  (go to the outpost)",
                here ? () => Confirm("Pack up?", "The kit goes back in your bag, its stock comes with you and its kin return to the Hearth Box.", () => { op.PackUp(o); Close(); }) : (System.Action)null,
                here ? new Color(0.42f, 0.24f, 0.24f) : new Color(0.3f, 0.3f, 0.32f), 22), 64);

            // ---------------- stock ----------------
            var rl = UIFactory.ScrollList(right, out _, 10f);
            ((RectTransform)rl.parent).Stretch();
            Header(rl, $"Waiting here  <size=22>{Outposts.Stock(o)} items</size>", 30);
            int cin = Outposts.CaravanIn(o);
            Para(rl, !BaseManager.I.HasBase ? "No camp to deliver to - collect it yourself." : o.kin.Count == 0 ? "No crew, so no caravan." :
                     cin < 0 ? "The caravan leaves once there is something to carry." : $"Next caravan to camp in {cin / 60}:{cin % 60:00}.", 21, Color.white);
            for (int i = 0; i < o.items.Count; i++)
            {
                var def = ContentDB.Item(o.items[i]);
                if (def == null) continue;
                var row = UIFactory.HRow(rl, 70, 12);
                row.childForceExpandHeight = false;
                var ic = UIFactory.ItemIcon(row.transform, def, 58); UIFactory.Size(ic, 58, 58);
                var t = UIFactory.Label(row.transform, $"<b>{def.displayName}</b>  x{o.counts[i]}{(o.counts[i] >= Outposts.StockCap ? UIFactory.Col("  full", new Color(1f, 0.6f, 0.5f)) : "")}", 24, Color.white);
                t.supportRichText = true; UIFactory.Size(t, 62, 400);
            }
            if (o.items.Count == 0) Para(rl, "Nothing yet.", 21);
            else UIFactory.Size(UIFactory.Button(rl, here ? "COLLECT INTO BAG" : "COLLECT  (go to the outpost)",
                here ? () => { int n = op.Collect(o); GameEvents.Say(n > 0 ? $"Collected {n} items." : "Your bag is full."); Rebuild(); } : (System.Action)null,
                here ? new Color(0.25f, 0.5f, 0.42f) : new Color(0.3f, 0.3f, 0.32f), 22), 70);
        }

        void OutpostPicker(OutpostSave o)
        {
            ShowModal($"Station a kin in {Outposts.LandName(o)}", list =>
            {
                var party = GameManager.I.Party;
                bool any = false;
                foreach (var d in party.All)
                {
                    if (!Outposts.CanStation(d)) continue;
                    any = true;
                    var dd = d;
                    var row = UIFactory.HRow(list, 90, 12);
                    row.childForceExpandHeight = false;
                    var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                    pic.sprite = ItemIcons.Get("kin_" + d.Species.id); pic.preserveAspect = true; pic.raycastTarget = false;
                    UIFactory.Size(pic, 76, 76);
                    var lb = UIFactory.Label(row.transform, $"<b>{d.DisplayName}</b>  Lv {d.level}  <size=19>{(d.location == KinLocation.Party ? "Party" : "Hearth Box")}</size>\n<size=20>{Outposts.JobText(o, d)}</size>", 25, Color.white);
                    lb.supportRichText = true; UIFactory.Size(lb, 84, 700);
                    UIFactory.Size(UIFactory.Button(row.transform, "SEND", () => { string err = Outposts.I.Station(o, dd); if (err != null) GameEvents.Warn(err); CloseModal(); Rebuild(); }, new Color(0.25f, 0.5f, 0.42f), 24), 76, 170);
                }
                if (!any) Para(list, "No kin free. Kin in your party (not the one that's out) or the Hearth Box can go.", 24, Color.white);
            });
        }

        // ============================================================ JOURNEY EXTRAS
        partial void JourneyExtras(RectTransform list)
        {
            JourneyHunts(list);
            JourneyDex(list);
            JourneyExplore(list);
            var bm = BaseManager.I;
            Header(list, "Your camp", 32);
            var row = UIFactory.HRow(list, 96, 12);
            row.childForceExpandHeight = false;
            string hearth = !bm.HasBase ? "No camp yet - place a Hearthstone (Build)."
                : bm.CoreLevel >= BaseManager.MaxCoreLevel ? $"Hearthstone Lv {bm.CoreLevel} (max)"
                : $"Hearthstone Lv {bm.CoreLevel}/{BaseManager.MaxCoreLevel}  ·  next: {Ingredients(BaseManager.UpgradeCost(bm.CoreLevel + 1))}";
            string research = Tech.Active != null ? $"researching {Tech.Active.name} ({Mathf.RoundToInt(Tech.Progress01 * 100f)}%)" : bm.HasStation(CraftStation.Research) ? "bench idle" : "build a Research Bench (Lv 3)";
            var t = UIFactory.Label(row.transform, $"<size=22>{hearth}\nResearch {Tech.DoneCount}/{Tech.All.Length}  ·  {research}\nOutposts {Outposts.All.Count}/5</size>", 24, Color.white);
            t.supportRichText = true; UIFactory.Size(t, 90, 1100);
            UIFactory.Size(UIFactory.Button(row.transform, "RESEARCH", () => OpenStation(CraftStation.Research), new Color(0.32f, 0.34f, 0.4f), 22), 80, 190);
        }
    }
}
