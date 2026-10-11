using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The QUESTS tab (Core/Quests.cs): three lines - Story, Camp, Kin - each a chain of quests. Left: the lines and the
    /// chain of the one picked (done, current, still to come). Right: the picked quest's story, objectives with live
    /// counts, how-to, where to go, rewards, and which line the HUD follows.
    /// </summary>
    public partial class MenuUI
    {
        QuestLine questLine;
        string questSel;
        bool questLineSet;

        public void OpenQuests(QuestLine line)
        {
            questLine = line;
            questLineSet = true;
            questSel = null;
            Open(MenuTab.Quests);
        }

        void BuildQuests()
        {
            if (!questLineSet) { questLine = Quests.TrackedLine; questLineSet = true; }
            SaveScroll();
            var pl = GameManager.I.Player;
            LeftRight(out var left, out var right, 0.38f);

            // ---------------------------------------------------------------- left: lines, then the chain
            var list = TrackedList(left);
            // the three lines: a strip fixed above the scrolling chain
            const float strip = 96f;
            var view = (RectTransform)list.parent;
            view.offsetMax = new Vector2(view.offsetMax.x, view.offsetMax.y - strip - 8f);
            var bar = UIFactory.Rect(view.parent, "QuestLines");
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
            bar.offsetMin = new Vector2(view.offsetMin.x, -strip); bar.offsetMax = new Vector2(view.offsetMax.x, 0);
            var tabs = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabs.spacing = 8;
            tabs.childControlHeight = true; tabs.childControlWidth = true;
            tabs.childForceExpandHeight = true; tabs.childForceExpandWidth = true;
            for (int i = 0; i < Quests.Lines.Length; i++)
            {
                var line = (QuestLine)i;
                if (!Quests.LineOn(line)) continue;
                bool tracked = Quests.TrackedLine == line;
                string label = $"{Quests.LineTabs[i]}\n<size=19>{Quests.DoneCount(line)}/{Quests.Total(line)}{(tracked ? "  ·  on HUD" : "")}</size>";
                var b = UIFactory.Button(bar, label, () => { questLine = line; questSel = null; Rebuild(); },
                    line == questLine ? new Color(0.25f, 0.55f, 0.45f) : new Color(0.2f, 0.24f, 0.3f), 28);
                var bt = b.GetComponentInChildren<Text>();
                if (bt != null) bt.supportRichText = true;
            }
            Para(list, $"<b>{Quests.LineNames[(int)questLine]}</b>\n<size=20>{Quests.LineBlurb[(int)questLine]}</size>", 24, Color.white);

            if (!Quests.LineOn(questLine)) questLine = QuestLine.Story;
            var chain = Quests.Visible(questLine);   // (partner quests that belong to the other starting choices are not listed)
            var current = Quests.Current(questLine);
            QuestDef sel = null;
            foreach (var q in chain) if (q.id == questSel) sel = q;
            if (sel == null) sel = current ?? chain[chain.Count - 1];

            string chapter = null;
            bool ahead = false;
            foreach (var q in chain)
            {
                if (q.chapter != chapter)
                {
                    chapter = q.chapter;
                    Para(list, UIFactory.Col(chapter, Quests.Gold), 21);
                }
                var quest = q;
                bool done = Quests.IsDone(q), now = q == current;
                string mark = done ? UIFactory.Col("[x] ", new Color(0.6f, 1f, 0.6f)) : now ? UIFactory.Col(">> ", Quests.Gold) : "      ";
                Color bg = q == sel ? new Color(0.3f, 0.42f, 0.5f) : now ? new Color(0.28f, 0.3f, 0.2f) : new Color(0.17f, 0.2f, 0.25f);
                var row = UIFactory.Button(list, mark + q.title, () => { questSel = quest.id; Rebuild(); }, bg, 24);
                UIFactory.Size(row, 58);
                var rt = row.GetComponentInChildren<Text>();
                if (rt != null)
                {
                    rt.supportRichText = true;
                    rt.alignment = TextAnchor.MiddleLeft;
                    rt.color = done ? new Color(0.72f, 0.8f, 0.74f) : ahead ? new Color(0.6f, 0.62f, 0.68f) : Color.white;
                    rt.rectTransform.Stretch(16, 2, 8, 2);
                }
                if (now) ahead = true;
            }
            // nothing picked yet (just opened, or the line was switched): bring the current quest into view
            if (questSel == null && current != null)
                savedScroll = Mathf.Clamp01(1f - (chain.IndexOf(current) - 3) / (float)Mathf.Max(1, chain.Count - 8));
            RestoreScroll();

            // ---------------------------------------------------------------- right: the picked quest
            var rl = UIFactory.ScrollList(right, out _, 10f);
            ((RectTransform)rl.parent).Stretch();
            bool selDone = Quests.IsDone(sel), selNow = sel == current || Quests.IsFree(sel.line);   // stars can be earned in any order
            Para(rl, UIFactory.Col(sel.chapter, Quests.Gold), 22);
            Header(rl, sel.title + (selDone ? UIFactory.Col("   <size=24>complete</size>", new Color(0.6f, 1f, 0.6f)) : ""), 38);
            if (!selDone && !selNow)
            {
                Para(rl, $"Finish \"{current.title}\" and the quests before this one first.", 24, Color.white);
                Para(rl, "Reward: " + UIFactory.Col(Quests.RewardText(sel), Quests.Gold), 22, Color.white);
                return;
            }

            Para(rl, $"<i>{sel.story}</i>", 25, new Color(0.9f, 0.9f, 0.84f));
            Header(rl, "To do", 28);
            foreach (var o in sel.objs)
            {
                bool od = selDone || o.Done;
                string count = o.need > 1 ? $"   <b>{(selDone ? o.need : o.Have)}/{o.need}</b>" : "";
                Para(rl, $"{(od ? UIFactory.Col("[x] ", new Color(0.6f, 1f, 0.6f)) : "[ ] ")}{o.text}{count}", 26, od ? new Color(0.75f, 0.95f, 0.75f) : Color.white);
            }
            if (!selDone)
            {
                string adv = sel.advice != null ? sel.advice() : null;
                if (!string.IsNullOrEmpty(adv)) Para(rl, UIFactory.Col(adv, new Color(1f, 0.8f, 0.55f)), 22);
                Vector3? at = sel.where != null ? sel.where() : null;
                if (at.HasValue) Para(rl, $"Where: {UIFactory.Col(sel.whereName, Quests.Gold)}, {Wardens.Compass(pl.Position, at.Value)} of you. A gold marker shows it on the minimap and the Map while this line is on your HUD.", 22, Color.white);
                Header(rl, "How", 28);
                Para(rl, sel.how, 23, Color.white);
            }
            Header(rl, selDone ? "Reward (received)" : "Reward", 28);
            Para(rl, UIFactory.Col(Quests.RewardText(sel), selDone ? UIFactory.TextDim : Quests.Gold), 24);
            if (selDone) return;

            Para(rl, " ", 8);
            var line2 = sel.line;
            if (Quests.TrackedLine == line2) ActionBtn(rl, "THIS LINE IS ON YOUR HUD", null, new Color(0.26f, 0.3f, 0.28f));
            else ActionBtn(rl, "SHOW THIS LINE ON MY HUD", () => { Quests.TrackedLine = line2; Rebuild(); }, new Color(0.25f, 0.5f, 0.42f));
            if (sel.warden != null)
            {
                var w = Wardens.Get(sel.warden);
                if (w != null) ActionBtn(rl, "ABOUT THIS WARDEN", () => OpenWarden(w), new Color(0.45f, 0.3f, 0.26f));
            }
            if (sel.where != null) ActionBtn(rl, "OPEN THE MAP", () => Open(MenuTab.Map), new Color(0.26f, 0.34f, 0.46f));
        }
    }
}
