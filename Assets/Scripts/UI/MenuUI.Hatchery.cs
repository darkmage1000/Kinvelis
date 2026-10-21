using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Camp > Hatchery: pick two parents, get an egg that inherits their best quality grades and traits.</summary>
    public partial class MenuUI
    {
        string hatchA, hatchB;

        void CampHatchery(RectTransform body)
        {
            var s = GameManager.I.Player.Stats;
            var party = GameManager.I.Party;
            var list = TrackedList(body);
            var hb0 = BaseManager.I.BuildingHere("hatchery");
            bool here = hb0 != null;   // eggs are laid and hatched at the Hatchery itself
            int places = KinGrowth.Places, warming = KinGrowth.WarmingCount, held = KinGrowth.HeldCount;
            Header(list, $"Hatchery  <size=22>warming {warming}/{places}{(held > 0 ? $"  ·  {held} waiting" : "")}</size>", 32);
            if (!KinGrowth.HasHatchery)
            {
                Para(list, "Build a Hatchery (BUILD menu, Explorer Lv 3) to breed kin. Eggs inherit the best quality grades of both parents and good traits - the way to raise S-grade kin.", 23, Color.white);
                return;
            }
            if (!here)
            {
                Building near = null;
                foreach (var hb1 in BaseManager.I.Buildings) if (hb1.Def.id == "hatchery") near = hb1;
                Para(list, UIFactory.Col($"This is a view. Walk up to the Hatchery ({BaseManager.I.WayTo(near)}) to pair kin and hatch eggs.", Idle), 21, Color.white);
            }

            // places: level 1 warms one egg, level 2 two, level 3 five (KinGrowth.PlacesAt); eggs wait in the bag / camp storage
            int lvl = 1; foreach (var hb2 in BaseManager.I.Buildings) if (hb2.Def.id == "hatchery") lvl = Mathf.Max(lvl, hb2.Level);
            string more = lvl < Building.MaxLevel ? $" Upgrade it (MANAGE > UPGRADE at the Hatchery) to warm {KinGrowth.PlacesAt(lvl + 1)} at once." : "";
            Para(list, $"Eggs only warm inside the Hatchery: {places} place{(places == 1 ? "" : "s")} here.{more} Eggs waiting for a place sit in your bag or camp storage, a slot each; leave them in camp storage and the Hatchery takes the next one by itself when a place frees up.", 20, Color.white);
            if (held > 0)
            {
                int inBag = s.Bag.Count(KinGrowth.EggItem), inStore = BaseManager.I.Storage.Count(KinGrowth.EggItem);
                Para(list, $"Waiting: {inBag} in your bag, {inStore} in camp storage.", 21, new Color(1f, 0.85f, 0.45f));
                if (inBag > 0)
                    UIFactory.Size(UIFactory.Button(list, here ? (KinGrowth.FreePlace ? "PUT AN EGG FROM YOUR BAG IN" : "EVERY PLACE IS WARMING AN EGG") : "PUT EGGS IN AT THE HATCHERY",
                        here && KinGrowth.FreePlace ? () => { var err = KinGrowth.WarmFromBag(); if (err != null) GameEvents.Warn(err); Rebuild(); } : (System.Action)null,
                        here && KinGrowth.FreePlace ? new Color(0.6f, 0.45f, 0.25f) : new Color(0.3f, 0.3f, 0.32f), 24), 70);
            }

            // warming eggs
            foreach (var e in s.eggs)
            {
                if (e.held) continue;
                var ee = e;
                bool ready = KinGrowth.EggReady(e);
                var card = Card(list, 104, ready ? new Color(0.2f, 0.45f, 0.3f, 0.5f) : (Color?)null);
                long left = e.readyUnix - Expeditions.Now;
                var t = UIFactory.Label(card.transform, $"<b>Mystery egg</b>  <size=20>from {e.parents}</size>\n<size=21>{(ready ? UIFactory.Col("It's wobbling - ready to hatch!", Good) : $"hatches in {left / 60}m {left % 60}s")}</size>", 26, Color.white, TextAnchor.UpperLeft);
                t.supportRichText = true;
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(900, 60));
                var bar = UIFactory.Bar(card.transform, ready ? Good : new Color(1f, 0.75f, 0.4f), out var fill);
                bar.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 12), new Vector2(620, 20));
                fill.SetFill(Mathf.Clamp01(1f - left / (KinGrowth.HatchMinutes * 60f)));
                var hb = UIFactory.Button(card.transform, ready ? (here ? "HATCH!" : "AT THE\nHATCHERY") : "WARMING", ready && here ? () => ShowHatch(ee) : (System.Action)null,
                    ready ? new Color(0.3f, 0.6f, 0.4f) : new Color(0.3f, 0.3f, 0.32f), 22);
                ((RectTransform)hb.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(200, 76));
            }

            // parents
            var a = party.Find(hatchA); var b = party.Find(hatchB);
            if (a != null && !KinGrowth.CanParent(a)) { hatchA = null; a = null; }
            if (b != null && !KinGrowth.CanParent(b)) { hatchB = null; b = null; }
            Header(list, "Choose parents", 28);
            var slots = UIFactory.HRow(list, 90, 14);
            foreach (var pr in new[] { a, b })
            {
                var pc = UIFactory.Panel(slots.transform, "Parent", new Color(1, 1, 1, 0.07f)); UIFactory.Size(pc, 86, 520);
                var pt = UIFactory.Label(pc.transform, pr != null ? $"<b>{pr.DisplayName}</b> Lv {pr.level}\n<size=19>{Aptitude.Summary(pr)}</size>" : "<size=22>(empty - pick below)</size>", 22, Color.white);
                pt.supportRichText = true; pt.rectTransform.Stretch(12, 4, 8, 4);
            }
            if (a != null && b != null)
            {
                a.EnsureAptitude(); b.EnsureAptitude();
                var best = new List<string>();
                for (int i = 0; i < 3; i++) { int v = Mathf.Max(a.apt[i], b.apt[i]); best.Add(UIFactory.Col($"{Aptitude.Names[i]} ~{Aptitude.Grade(v)}", Aptitude.GradeColor(v))); }
                Para(list, $"Egg: a {a.Species.displayName}{(a.speciesId != b.speciesId ? " or " + b.Species.displayName : "")}, quality around the best of both: {string.Join("  ", best)}\nCost: {Ingredients(KinGrowth.BreedCost)}  ·  hatches in {KinGrowth.HatchMinutes} min", 21, Color.white);
                var go = UIFactory.Button(list, here ? "BREED" : "BREED AT THE HATCHERY", here ? () =>
                {
                    string err = KinGrowth.Breed(a, b);
                    if (err != null) GameEvents.Warn(err); else { hatchA = null; hatchB = null; }
                    Rebuild();
                } : (System.Action)null, here && (KinGrowth.FreePlace || KinGrowth.EggRoom(out _)) ? new Color(0.6f, 0.4f, 0.25f) : new Color(0.3f, 0.3f, 0.32f), 26);
                UIFactory.Size(go, 76);
            }

            // every kin that can be a parent, through the shared sort / filter bar (user, 2026-10-20: "add it to hatchery as well")
            var parents = new List<CreatureData>();
            foreach (var d in party.All) if (KinGrowth.CanParent(d)) parents.Add(d);
            Header(list, "Your kin", 26);
            KinFilterBar(list, parents, Rebuild);
            var shownParents = KinFiltered(parents);
            if (shownParents.Count < parents.Count) Para(list, UIFactory.Col($"{parents.Count - shownParents.Count} hidden by the filter", UIFactory.TextDim), 18, Color.white);
            foreach (var d in shownParents)
            {
                var dd = d;
                bool picked = d.uid == hatchA || d.uid == hatchB;
                var r = UIFactory.HRow(list, 74, 12);
                var p = Portrait(r.transform, d, 60); UIFactory.Size(p, 60, 60);
                string els = UIFactory.Col(d.Element1.ToString(), ElementInfo.Color(d.Element1)) + (d.Element2 != Element.Normal && d.Element2 != d.Element1 ? "/" + UIFactory.Col(d.Element2.ToString(), ElementInfo.Color(d.Element2)) : "");
                var lt = UIFactory.Label(r.transform, $"<b>{d.DisplayName}</b> Lv {d.level}  <size=19>#{d.Species.dexNumber:000} {els}  ·  {Aptitude.Summary(d)}</size>", 23, Color.white);
                lt.supportRichText = true; UIFactory.Size(lt, 66, 860);
                UIFactory.Size(UIFactory.Button(r.transform, picked ? "REMOVE" : "PICK", () =>
                {
                    if (picked) { if (hatchA == dd.uid) hatchA = null; else hatchB = null; }
                    else if (hatchA == null) hatchA = dd.uid; else hatchB = dd.uid;
                    Rebuild();
                }, picked ? new Color(0.45f, 0.3f, 0.3f) : new Color(0.25f, 0.45f, 0.5f), 20), 60, 150);
            }
        }
    }
}
