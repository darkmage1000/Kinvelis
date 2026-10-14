using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// One box's own screen (hidden tab MenuTab.Box): opened with OPEN at a Storage Chest or vault, FEED BOX at a
    /// Feed Box, or from Camp > Storage. Left: that box's slots as a grid. Right: what you tapped (take 1 / 10 / all of
    /// it), then your bag with buttons to put things in this box, and the bulk buttons. A Feed Box only takes kin
    /// food, shows what is in camp storage too, and has the switch for keeping itself stocked.
    /// The arrows step through every box in camp, so you can sort the lot without walking between them.
    /// </summary>
    public partial class MenuUI
    {
        Building boxTarget;
        int boxSel = -1;

        public void OpenBox(Building b)
        {
            boxTarget = b;
            boxSel = -1;
            Open(MenuTab.Box);
        }

        List<Building> AllBoxes()
        {
            var bm = BaseManager.I;
            var l = bm.Boxes();
            foreach (var fb in bm.FeedBoxes()) l.Add(fb);
            return l;
        }

        void BuildBox()
        {
            SaveScroll();
            var bm = BaseManager.I;
            var gm = GameManager.I;
            if (boxTarget == null || bm == null || !bm.Buildings.Contains(boxTarget)) { boxTarget = null; Open(MenuTab.Camp); return; }
            var b = boxTarget;
            bool feed = b.Def.feeds;
            bool larder = BaseManager.IsLarder(b);
            var filter = BaseManager.FilterOf(b);
            bool inCamp = bm.PlayerAt(b);   // a box is filled and emptied standing at it
            var bag = gm.Player.Stats.Bag;
            var slots = bm.BoxSlots(b);
            if (boxSel >= slots.Count || (boxSel >= 0 && slots[boxSel].Empty)) boxSel = -1;

            LeftRight(out var left, out var right, 0.56f);

            // ---------------- left: the box ----------------
            var head = UIFactory.Rect(left, "Head").Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1000, 76));
            var hh = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            hh.spacing = 10; hh.childControlWidth = true; hh.childControlHeight = true; hh.childForceExpandWidth = false; hh.childAlignment = TextAnchor.MiddleLeft;
            var all = AllBoxes();
            int at = all.IndexOf(b);
            if (all.Count > 1)
            {
                UIFactory.Size(UIFactory.Button(head, "<", () => { boxTarget = all[(at - 1 + all.Count) % all.Count]; boxSel = -1; Rebuild(); }, UIFactory.ButtonCol, 30), 70, 76);
                UIFactory.Size(UIFactory.Button(head, ">", () => { boxTarget = all[(at + 1) % all.Count]; boxSel = -1; Rebuild(); }, UIFactory.ButtonCol, 30), 70, 76);
            }
            var icon = UIFactory.Rect(head, "Icon").gameObject.AddComponent<Image>();
            icon.sprite = ItemIcons.Get("bld_" + b.Def.id); icon.preserveAspect = true; icon.raycastTarget = false;
            UIFactory.Size(icon, 70, 70);
            int used = bm.BoxUsed(b), cap = bm.BoxCapacity(b);
            var title = UIFactory.Label(head, $"<b>{bm.BoxName(b)}</b>  <size=24>{used}/{cap} slots{(feed ? $"  ·  {bm.FeedCount(b)} food" : "")}</size>", 36, Color.white);
            title.supportRichText = true;
            UIFactory.Size(title, 70, 640);

            var gridHost = UIFactory.Rect(left, "GridHost");
            gridHost.Anchors(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -90));
            var list = TrackedList(gridHost);
            DestroyImmediate(list.GetComponent<VerticalLayoutGroup>());
            var grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(118, 118); grid.spacing = new Vector2(8, 8); grid.padding = new RectOffset(6, 6, 6, 6);
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                int idx = i;
                var cell = UIFactory.Panel(list, "Slot", i == boxSel ? new Color(0.3f, 0.6f, 0.5f, 1f) : new Color(1, 1, 1, s.Empty ? 0.04f : 0.09f));
                if (s.Empty) continue;
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { boxSel = idx; SynthAudio.Play(Sfx.UiClick); Rebuild(); });
                var d = ContentDB.Item(s.id);
                var ic = UIFactory.ItemIcon(cell.transform, d, 92);
                ic.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(92, 92));
                var cnt = UIFactory.Label(cell.transform, "x" + s.count, 22, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                cnt.rectTransform.Stretch(4, 2, 8, 2);
                cnt.raycastTarget = false;
            }

            // ---------------- right: take out, put in ----------------
            var rlist = UIFactory.ScrollList(right, out _, 8f);
            ((RectTransform)rlist.parent).Stretch();
            if (!inCamp) Para(rlist, UIFactory.Col($"This is a view. Walk up to this box ({bm.WayTo(b)}) to put things in or take them out.", Idle), 22);

            if (feed)
            {
                UIFactory.Size(UIFactory.Button(rlist, "KEEP STOCKED FROM STORAGE: " + (b.FeedAuto ? "ON" : "OFF"), () => { b.FeedAuto = !b.FeedAuto; Rebuild(); },
                    b.FeedAuto ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 26), 76);
                Para(rlist, b.FeedAuto
                    ? $"This box tops itself up to about {BaseManager.FeedKeep} pieces of food from camp storage, cheapest first. Switch it off to choose exactly what your kin eat."
                    : "Your kin eat only what you put in here. Nothing is taken from camp storage.", 20);
                Para(rlist, "Kin eat until they are full: a dozen berries a meal, or a couple of melons, fish or tarts. Food left here goes off as it does in a chest; only food kept in a cold Frost Larder lasts longer.", 20);
                float mins = bm.FoodMinutes;
                Para(rlist, $"{gm.Party.CountIn(KinLocation.Base)} kin at camp  ·  food for " + (float.IsInfinity(mins) ? "as long as you like" : mins > 90f ? $"about {mins / 60f:0.#} h" : $"about {mins:0} min"), 20, Color.white);
            }

            if (larder)
                Para(rlist, bm.LarderActive
                    ? UIFactory.Col($"Cold  ·  food in here keeps three times as long  ·  {WorkStages.Clock(bm.Cold)} of cold stored", new Color(0.6f, 0.9f, 1f))
                    : UIFactory.Col("No cold: food in here goes off as fast as anywhere. It needs a Chilling kin.", Idle), 21);
            else if (!feed && !b.Def.isCore && b.BoxLen > 0)
            {
                // what this chest holds: tap one and the whole camp's storage is dealt back out to match
                Para(rlist, $"This chest holds: <b>{BaseManager.FilterNames[(int)filter]}</b>", 22, Color.white);
                for (int row = 0; row < 2; row++)
                {
                    var fr = UIFactory.HRow(rlist, 62, 8);
                    for (int i = row * 3; i < row * 3 + 3 && i < BaseManager.FilterShort.Length; i++)
                    {
                        var f = (BoxFilter)i;
                        UIFactory.Size(UIFactory.Button(fr.transform, BaseManager.FilterShort[i], inCamp ? () => { bm.SetFilter(b, f); boxSel = -1; GameEvents.Good($"{bm.BoxName(b)} now holds: {BaseManager.FilterNames[(int)f]}. Storage sorted."); Rebuild(); } : (System.Action)null,
                            filter == f ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 20), 58, 190);
                    }
                }
            }

            if (boxSel >= 0)
            {
                var s = slots[boxSel];
                var d = ContentDB.Item(s.id);
                string sid = s.id;
                var row = UIFactory.HRow(rlist, 96, 10);
                UIFactory.Size(UIFactory.ItemIcon(row.transform, d, 88), 88, 88);
                var nm = UIFactory.Label(row.transform, $"<b>{ContentDB.ItemName(sid)}</b>\n<size=22>x{bm.BoxCount(b, sid)} in this box</size>", 28, Color.white);
                nm.supportRichText = true;
                UIFactory.Size(nm, 88, 420);
                if (inCamp)
                {
                    var tr = UIFactory.HRow(rlist, 76, 10);
                    UIFactory.Size(UIFactory.Button(tr.transform, "TAKE 1", () => BoxTake(b, sid, 1), new Color(0.25f, 0.5f, 0.42f), 24), 72, 190);
                    UIFactory.Size(UIFactory.Button(tr.transform, "TAKE 10", () => BoxTake(b, sid, 10), new Color(0.25f, 0.5f, 0.42f), 24), 72, 190);
                    UIFactory.Size(UIFactory.Button(tr.transform, "TAKE ALL", () => BoxTake(b, sid, int.MaxValue), new Color(0.25f, 0.5f, 0.42f), 24), 72, 190);
                }
            }
            else Para(rlist, used > 0 ? "Tap something in the box to take it out." : "This box is empty.", 22);

            if (inCamp)
            {
                var bulk = UIFactory.HRow(rlist, 76, 10);
                UIFactory.Size(UIFactory.Button(bulk.transform, "EMPTY THE BOX", () => { foreach (var s in bm.BoxSlots(b)) if (!s.Empty) { int left = bag.Add(s.id, s.count); s.count = left; if (left <= 0) s.id = ""; } BoxChanged(); },
                    new Color(0.3f, 0.4f, 0.55f), 22), 72, 280);
                if (!feed)
                    UIFactory.Size(UIFactory.Button(bulk.transform, filter == BoxFilter.Any ? "STORE ALL MATERIALS" : "STORE ALL THAT FITS", () =>
                    {
                        foreach (var s in bag.slots)
                        {
                            if (s.Empty) continue;
                            var d = ContentDB.Item(s.id);
                            if (d == null || (filter == BoxFilter.Any && d.category != ItemCategory.Resource && d.category != ItemCategory.Food)) continue;
                            int left = bm.BoxAdd(b, s.id, s.count);
                            s.count = left; if (left <= 0) s.id = "";
                        }
                        BoxChanged();
                    }, new Color(0.3f, 0.4f, 0.55f), 22), 72, 300);
            }

            // your bag: put things in this box
            Header(rlist, feed || filter == BoxFilter.Food ? "Food in your bag" : filter != BoxFilter.Any ? "In your bag, for this chest" : "Your bag", 28);
            int shown = PutRows(rlist, b, bag, feed, inCamp, false);
            if (shown == 0) Para(rlist, feed ? "No kin food in your bag (berries, crops, fish, tarts)." : filter != BoxFilter.Any ? "Nothing in your bag that this box holds." : "Your bag is empty.", 20);
            if (feed)
            {
                Header(rlist, "Food in camp storage", 28);
                if (PutRows(rlist, b, bm.Storage, true, inCamp, true) == 0) Para(rlist, "No kin food in camp storage.", 20);
            }
            RestoreScroll();
        }

        /// <summary>Rows of "icon, name x N, +1 / +10 / ALL" for everything in an inventory that this box will take.</summary>
        int PutRows(RectTransform list, Building b, Inventory from, bool foodOnly, bool inCamp, bool fromStorage)
        {
            var bm = BaseManager.I;
            var totals = new Dictionary<string, int>();
            foreach (var s in from.slots) if (!s.Empty) { totals.TryGetValue(s.id, out int c); totals[s.id] = c + s.count; }
            var ids = new List<string>(totals.Keys);
            ids.Sort((x, y) =>
            {
                var dx = ContentDB.Item(x); var dy = ContentDB.Item(y);
                int cx = dx != null ? (int)dx.category : 99, cy = dy != null ? (int)dy.category : 99;
                return cx != cy ? cx.CompareTo(cy) : string.CompareOrdinal(x, y);
            });
            int n = 0;
            foreach (var id in ids)
            {
                if (foodOnly ? !BaseManager.IsKinFood(id) : !BaseManager.BoxTakes(b, id)) continue;
                n++;
                string iid = id;
                var row = UIFactory.HRow(list, 72, 8);
                UIFactory.Size(UIFactory.ItemIcon(row.transform, ContentDB.Item(id), 64), 64, 64);
                var nm = UIFactory.Label(row.transform, $"{ContentDB.ItemName(id)}  <size=20>x{totals[id]}</size>", 24, Color.white);
                nm.supportRichText = true;
                UIFactory.Size(nm, 64, 250);
                if (!inCamp) continue;
                UIFactory.Size(UIFactory.Button(row.transform, "+1", () => BoxPut(b, from, iid, 1, fromStorage), UIFactory.ButtonCol, 22), 64, 84);
                UIFactory.Size(UIFactory.Button(row.transform, "+10", () => BoxPut(b, from, iid, 10, fromStorage), UIFactory.ButtonCol, 22), 64, 92);
                UIFactory.Size(UIFactory.Button(row.transform, "ALL", () => BoxPut(b, from, iid, int.MaxValue, fromStorage), UIFactory.ButtonCol, 22), 64, 92);
            }
            return n;
        }

        void BoxTake(Building b, string id, int amount)
        {
            var bm = BaseManager.I;
            var bag = GameManager.I.Player.Stats.Bag;
            int want = Mathf.Min(amount, bm.BoxCount(b, id));
            int fits = Mathf.Min(want, bag.SpaceFor(id));
            if (fits <= 0) { GameEvents.Warn("Bag is full."); return; }
            bm.BoxRemove(b, id, fits);
            bag.Add(id, fits);
            if (fits < want) GameEvents.Warn("Bag is full - took what fits.");
            SynthAudio.Play(Sfx.Pickup);
            BoxChanged();
        }

        void BoxPut(Building b, Inventory from, string id, int amount, bool fromStorage)
        {
            var bm = BaseManager.I;
            int have = from.Count(id);
            int want = Mathf.Min(amount, have);
            if (want <= 0) return;
            from.Remove(id, want);                       // out first, so a move within camp storage frees its own slot
            int left = bm.BoxAdd(b, id, want);
            if (left > 0) from.Add(id, left);
            if (left >= want) { GameEvents.Warn(b.Def.feeds && !BaseManager.IsKinFood(id) ? "Kin won't eat that." : !b.Def.feeds && !BaseManager.BoxTakes(b, id) ? $"This box holds {BaseManager.FilterNames[(int)BaseManager.FilterOf(b)].ToLower()} only." : "That box is full."); return; }
            if (left > 0) GameEvents.Warn("The box is full - put in what fits.");
            SynthAudio.Play(Sfx.Pickup, 0.9f);
            BoxChanged();
        }

        void BoxChanged()
        {
            GameEvents.RaiseInventoryChanged();
            Rebuild();
        }
    }
}
