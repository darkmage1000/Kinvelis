using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Kin shown as kin (user, 2026-10-15: "ui for camp kin swapping sucks, i want to see the kin, its info, and have it
    /// all easy to see, atm i just see names and grades ... and that's too much extra for what should be handled on camp
    /// kin swap screen").
    ///   `Portrait` - the kin's own picture (ItemIcons "kin_" + species) in a ring of its element's colour, a gold star
    ///   for a shimmer, greyed when it has fainted. Used by every kin list (it used to be two letters in a circle).
    ///   `KinCard` - one row that says who a kin is at a glance: picture, name (and kind if it has a nickname), level,
    ///   rarity, elements; its camp jobs with levels as stars, the job you are choosing for in gold; work grade and
    ///   traits; where it is and what it is doing, and its hunger. Buttons go in the box on the right.
    ///   `AssignPicker` (tap a worker place at a structure) - who works here (REMOVE / TO BOX), camp kin who can do it
    ///   (ASSIGN, or SWAP IN for the weakest worker when every place is taken), and kin from your party and Hearth Box
    ///   who can do it (BRING HERE - straight into camp and into the job; with the camp full you pick who goes to the box).
    /// </summary>
    public partial class MenuUI
    {
        static readonly Color StarGold = new Color(1f, 0.85f, 0.3f);

        Image Portrait(Transform parent, CreatureData d, float size)
        {
            Color ring = d.Fainted ? new Color(0.35f, 0.35f, 0.38f) : ElementInfo.Color(d.Element1);
            var bg = UIFactory.Panel(parent, "Portrait", Color.Lerp(ring, Color.black, 0.45f), UIFactory.Circle);
            bg.type = Image.Type.Simple;
            bg.raycastTarget = false;
            bg.rectTransform.sizeDelta = new Vector2(size, size);
            var rim = UIFactory.Panel(bg.transform, "Rim", ring, UIFactory.Ring != null ? UIFactory.Ring : UIFactory.Circle);
            rim.type = Image.Type.Simple; rim.raycastTarget = false;
            rim.rectTransform.Stretch();
            if (UIFactory.Ring == null) rim.color = new Color(ring.r, ring.g, ring.b, 0.25f);
            var sprite = ItemIcons.KinPortrait(d);   // an evolved kin shows its new form
            if (sprite != null)
            {
                var pic = UIFactory.Panel(bg.transform, "Pic", d.Fainted ? new Color(0.5f, 0.5f, 0.5f, 0.8f) : Color.white, sprite);
                pic.type = Image.Type.Simple; pic.preserveAspect = true; pic.raycastTarget = false;
                pic.rectTransform.Stretch(size * 0.06f, size * 0.06f, size * 0.06f, size * 0.06f);
            }
            else
            {
                var t = UIFactory.Label(bg.transform, d.Species.displayName.Substring(0, 2), Mathf.RoundToInt(size * 0.4f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch(); t.raycastTarget = false;
            }
            if (d.shimmer)
            {
                var st = UIFactory.Label(bg.transform, "*", Mathf.RoundToInt(size * 0.4f), new Color(1f, 0.86f, 0.35f), TextAnchor.UpperRight, FontStyle.Bold);
                st.rectTransform.Stretch(); st.raycastTarget = false;
            }
            return bg;
        }

        /// <summary>Its camp jobs as "Logging ***", the jobs in `want` picked out in gold, switched-off ones greyed.</summary>
        static string JobLine(CreatureData d, WorkType want, WorkType want2)
        {
            var parts = new List<string>();
            foreach (var w in d.Species.work)
            {
                int lv = d.JobLevel(w.type);
                string s = $"{w.type} {new string('*', lv)}";
                bool key = w.type == want || (want2 != WorkType.None && w.type == want2);
                if (!d.JobOn(w.type)) parts.Add(UIFactory.Col(s + " (off)", new Color(0.55f, 0.56f, 0.6f)));
                else parts.Add(key ? $"<b>{UIFactory.Col(s, StarGold)}</b>" : s);
            }
            return parts.Count > 0 ? string.Join("   ", parts) : UIFactory.Col("No camp jobs", new Color(0.6f, 0.6f, 0.65f));
        }

        static string WhereLine(CreatureData d)
        {
            var bm = BaseManager.I;
            switch (d.location)
            {
                case KinLocation.Base:
                {
                    var b = bm != null ? bm.FindBuilding(d.assignedBuilding) : null;
                    var a = bm != null ? bm.ActorFor(d) : null;
                    string job = b != null ? b.Def.displayName : UIFactory.Col("no job", Idle);
                    return $"Camp: {job}  ·  {(a != null ? a.WorkStatus : b != null ? "working" : "idle")}";
                }
                case KinLocation.Party:
                    var c = GameManager.I.Party.Companion;
                    return c != null && c.Data == d ? UIFactory.Col("Out with you", UIFactory.Accent) : "In your party";
                case KinLocation.Box: return "In the Hearth Box";
                case KinLocation.Expedition: return "On an expedition";
                case KinLocation.Outpost: return "At an outpost";
            }
            return "";
        }

        /// <summary>
        /// One kin as a card in a list: picture, who it is, jobs (want / want2 in gold), grade and traits, where it is.
        /// Returns the box on the right for its buttons (a row, filled by the caller).
        /// </summary>
        RectTransform KinCard(Transform list, CreatureData d, WorkType want = WorkType.None, WorkType want2 = WorkType.None, float buttonsWidth = 380f, Color? tint = null)
        {
            var sp = d.Species;
            var card = UIFactory.Panel(list, "KinCard", tint ?? new Color(1, 1, 1, 0.06f));
            UIFactory.Size(card, 150);
            var p = Portrait(card.transform, d, 124);
            p.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(124, 124));

            string el = UIFactory.Col(d.Element1.ToString(), ElementInfo.Color(d.Element1)) + (d.Element2 != Element.Normal && d.Element2 != d.Element1 ? "/" + UIFactory.Col(d.Element2.ToString(), ElementInfo.Color(d.Element2)) : "");
            string who = d.nickname != null ? $"<b>{d.DisplayName}</b> <size=19>({d.RegionalName})</size>" : $"<b>{d.DisplayName}</b>";
            string rarity = sp.starter ? UIFactory.Col("Partner", new Color(1f, 0.85f, 0.4f)) : UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity));
            int wa = d.Apt(KinPath.Work);
            var traits = new List<string>();
            foreach (var t in d.traits) { var td = ContentDB.Trait(t); if (td != null) traits.Add(UIFactory.Col(td.displayName, td.positive ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.5f))); }
            string grade = UIFactory.Col($"Work {Aptitude.Grade(wa)}", Aptitude.GradeColor(wa)) + $" <size=17>x{d.WorkAptMul:0.00}</size>";
            string tags = (d.alpha ? UIFactory.Col("  Alpha", new Color(1f, 0.55f, 0.4f)) : "") + (d.shimmer ? UIFactory.Col("  Shimmer +1 job", new Color(1f, 0.85f, 0.4f)) : "") + (d.Nocturnal ? UIFactory.Col("  Night owl", new Color(0.75f, 0.8f, 1f)) : "");
            string text = $"{who}  Lv {d.level}  <size=19>{rarity} · {el}</size>\n"
                        + $"<size=21>{JobLine(d, want, want2)}</size>\n"
                        + $"<size=19>{grade}{tags}{(traits.Count > 0 ? "  ·  " + string.Join(", ", traits) : "")}</size>\n"
                        + $"<size=18>{UIFactory.Col(WhereLine(d), UIFactory.TextDim)}{(d.location == KinLocation.Base ? $"  ·  hunger {d.hunger:0}%" : "")}</size>";
            var lbl = UIFactory.Label(card.transform, text, 25, Color.white, TextAnchor.MiddleLeft);
            lbl.supportRichText = true;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 14; lbl.resizeTextMaxSize = 25;
            lbl.rectTransform.Stretch(148, 6, buttonsWidth + 18, 6);
            if (d.location == KinLocation.Base)
            {
                var hb = UIFactory.Bar(card.transform, d.hunger < 25 ? Bad : new Color(1f, 0.7f, 0.3f), out var hf);
                hb.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 8), new Vector2(112, 8));
                hf.SetFill(d.hunger / 100f);
            }
            var btns = UIFactory.Rect(card.transform, "Buttons");
            btns.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(buttonsWidth, 120));
            var h = btns.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            return btns;
        }

        // ============================================================ WHO WORKS HERE
        void AssignPicker(Building b)
        {
            var bm = BaseManager.I;
            var party = GameManager.I.Party;
            var w1 = b.Def.workType; var w2 = b.Def.secondaryWorkType;
            var current = new List<CreatureData>(bm.WorkersAt(b));
            ShowModal($"{b.Def.displayName} - who works here ({current.Count}/{b.WorkerSlots})", list =>
            {
                Para(list, $"Needs <b>{UIFactory.Col(w1.ToString(), StarGold)}</b>{(w2 != WorkType.None ? $" or <b>{UIFactory.Col(w2.ToString(), StarGold)}</b>" : "")}: more stars work faster. Camp {party.CountIn(KinLocation.Base)}/{bm.WorkerCap} kin.", 21, Color.white);
                // the weakest worker here: who a SWAP IN replaces when every place is taken
                CreatureData weakest = null;
                foreach (var d in current) if (weakest == null || b.Suitability(d) < b.Suitability(weakest)) weakest = d;
                bool full = current.Count >= b.WorkerSlots;

                if (current.Count > 0) Header(list, "Working here", 26);
                foreach (var d in current)
                {
                    var dd = d;
                    var btn = KinCard(list, d, w1, w2, 360f, new Color(0.2f, 0.4f, 0.32f, 0.45f));
                    UIFactory.Button(btn, "REMOVE", () => { bm.Assign(dd, null); AssignPicker(b); Rebuild(); }, new Color(0.45f, 0.3f, 0.25f), 20);
                    UIFactory.Button(btn, "TO BOX", () => { party.Move(dd, KinLocation.Box); AssignPicker(b); Rebuild(); }, new Color(0.35f, 0.32f, 0.4f), 20);
                }

                var camp = new List<CreatureData>();
                foreach (var d in party.In(KinLocation.Base)) if (d.assignedBuilding != b.Uid && b.Suitability(d) > 0) camp.Add(d);
                camp.Sort((x, y) => b.Suitability(y).CompareTo(b.Suitability(x)));
                if (camp.Count > 0) Header(list, "Camp kin who can do it", 26);
                foreach (var d in camp)
                {
                    var dd = d;
                    var btn = KinCard(list, d, w1, w2, 360f);
                    if (!full) UIFactory.Button(btn, "ASSIGN", () => { if (bm.Assign(dd, b)) { AssignPicker(b); Rebuild(); } }, new Color(0.25f, 0.5f, 0.42f), 22);
                    else UIFactory.Button(btn, $"SWAP IN\n<size=16>for {weakest.DisplayName}</size>", () => { bm.Assign(weakest, null); if (bm.Assign(dd, b)) { AssignPicker(b); Rebuild(); } }, new Color(0.3f, 0.45f, 0.55f), 20);
                }

                var away = new List<CreatureData>();
                foreach (var d in party.All)
                    if ((d.location == KinLocation.Party || d.location == KinLocation.Box) && b.Suitability(d) > 0) away.Add(d);
                away.Sort((x, y) => b.Suitability(y).CompareTo(b.Suitability(x)));
                if (away.Count > 0) Header(list, "From your party and Hearth Box", 26);
                bool campFull = party.CountIn(KinLocation.Base) >= bm.WorkerCap;
                foreach (var d in away)
                {
                    var dd = d;
                    var btn = KinCard(list, d, w1, w2, 360f);
                    if (!campFull)
                        UIFactory.Button(btn, full ? $"BRING HERE\n<size=16>for {weakest.DisplayName}</size>" : "BRING HERE", () => BringToJob(dd, b, null), new Color(0.25f, 0.5f, 0.42f), 20);
                    else
                        UIFactory.Button(btn, "SWAP IN\n<size=16>camp is full</size>", () => SwapOutPicker(dd, b), new Color(0.3f, 0.45f, 0.55f), 20);
                }

                if (current.Count == 0 && camp.Count == 0 && away.Count == 0)
                    Para(list, $"None of your kin can do {w1} work yet.", 24, Color.white);
                Header(list, "Where to find more", 24);
                Para(list, JobHint(w1), 21, Color.white);
                if (w2 != WorkType.None) Para(list, JobHint(w2), 21, Color.white);
            });
        }

        /// <summary>Into camp and straight into this job (the weakest worker here steps aside if every place is taken); `leaving` goes to the box first.</summary>
        void BringToJob(CreatureData d, Building b, CreatureData leaving)
        {
            var bm = BaseManager.I;
            var party = GameManager.I.Party;
            if (leaving != null && !party.Move(leaving, KinLocation.Box)) return;
            if (!party.Move(d, KinLocation.Base)) return;
            bm.Assign(d, null);   // joining camp auto-assigns it somewhere: clear that so it doesn't count as one of this structure's workers
            var here = new List<CreatureData>(bm.WorkersAt(b));
            if (here.Count >= b.WorkerSlots)
            {
                CreatureData weakest = null;
                foreach (var w in here) if (weakest == null || b.Suitability(w) < b.Suitability(weakest)) weakest = w;
                bm.Assign(weakest, null);
            }
            if (bm.Assign(d, b)) GameEvents.Good($"{d.DisplayName} now works at the {b.Def.displayName}.");
            AssignPicker(b);
            Rebuild();
        }

        /// <summary>The camp is full: pick which camp kin goes to the Hearth Box to make room.</summary>
        void SwapOutPicker(CreatureData incoming, Building b)
        {
            var party = GameManager.I.Party;
            ShowModal($"Make room for {incoming.DisplayName}", list =>
            {
                Para(list, $"Your camp is full. Pick a camp kin to send to the Hearth Box; {incoming.DisplayName} takes its place and goes to work at the {b.Def.displayName}.", 21, Color.white);
                var camp = new List<CreatureData>(party.In(KinLocation.Base));
                // least useful first: no job, then the fewest stars for anything
                camp.Sort((x, y) =>
                {
                    bool xj = x.assignedBuilding != null, yj = y.assignedBuilding != null;
                    if (xj != yj) return xj ? 1 : -1;
                    return BaseManager.BestWork(x).CompareTo(BaseManager.BestWork(y));
                });
                foreach (var d in camp)
                {
                    var dd = d;
                    var btn = KinCard(list, d, b.Def.workType, b.Def.secondaryWorkType, 300f);
                    UIFactory.Button(btn, "SEND TO BOX", () => BringToJob(incoming, b, dd), new Color(0.4f, 0.32f, 0.45f), 20);
                }
                UIFactory.Size(UIFactory.Button(list, "BACK", () => AssignPicker(b), UIFactory.ButtonCol, 24), 70);
            });
        }
    }
}
