using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Journey page (the long-term goals: Wardens and Sigils) and the screen at a Warden's shrine.</summary>
    public partial class MenuUI
    {
        Wardens.Warden wardenAt;

        public void OpenWarden(Wardens.Warden w)
        {
            wardenAt = w;
            Open(MenuTab.Warden);
        }


        void BuildJourney()
        {
            SaveScroll();
            var list = TrackedList(content);
            var pl = GameManager.I.Player;
            int sig = Wardens.Sigils;
            Header(list, $"Journey  <size=26>{UIFactory.Col($"Sigils {sig}/{Wardens.SigilsNeeded}", Gold)}</size>", 38);
            Para(list, "Every land has a Warden waiting at a landmark you can see from far off. Stand at its shrine, tap WARDEN, and challenge it. " +
                       "A first win earns that land's Sigil and Warden Hearts. All eight Sigils open Stormcrest's Aerie.", 21);
            int myLv = pl.Stats.level;
            foreach (var w in Wardens.ByLevel())
            {
                var sp = w.Sp;
                int wins = Wardens.Wins(w);
                bool locked = w.final && sig < Wardens.SigilsNeeded;
                var row = UIFactory.HRow(list, 112, 12);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = sp != null ? ItemIcons.Get("kin_" + sp.id) : null; pic.preserveAspect = true; pic.raycastTarget = false;
                if (locked) pic.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                UIFactory.Size(pic, 92, 92);
                bool skull = w.level >= myLv + DangerSkull.LevelGap;
                string lv = UIFactory.Col($"Lv {w.level}", skull ? new Color(1f, 0.35f, 0.3f) : Color.white);
                string kind = w.final ? UIFactory.Col("  FINAL", Gold) : w.optional ? UIFactory.Col("  optional", UIFactory.TextDim) : "";
                string where = $"{UIFactory.Col(w.place, w.color)} in {w.land}  ·  {Wardens.Compass(pl.Position, w.pos)}";
                string status = locked ? UIFactory.Col($"Sealed until you hold all {Wardens.SigilsNeeded} Sigils.", UIFactory.TextDim)
                              : wins > 0 ? UIFactory.Col(w.final ? "Journey complete" : w.optional ? "Beaten" : "Sigil earned", new Color(0.6f, 1f, 0.6f)) + (wins > 1 ? $"  ×{wins}" : "")
                              : skull ? UIFactory.Col("10+ levels over you", new Color(1f, 0.35f, 0.3f)) : "Not yet beaten";
                var t = UIFactory.Label(row.transform, $"<b>{(locked ? "???" : w.Name)}</b>  {lv}{kind}\n<size=20>{where}\n{status}</size>", 26, Color.white);
                t.supportRichText = true; UIFactory.Size(t, 104, 900);
                var ww = w;
                UIFactory.Size(UIFactory.Button(row.transform, "DETAILS", () => OpenWarden(ww), new Color(0.32f, 0.34f, 0.4f), 22), 80, 170);
            }
            JourneyExtras(list);
            RestoreScroll();
        }

        /// <summary>More long-term goals are appended here by later systems (research, Dex milestones, legend hunts).</summary>
        partial void JourneyExtras(RectTransform list);

        void BuildWarden()
        {
            var w = wardenAt;
            if (w == null || Wardens.I == null) { Header(content, "Nothing here"); return; }
            var sp = w.Sp;
            var list = TrackedList(content);
            var pl = GameManager.I.Player;
            int wins = Wardens.Wins(w), cd = Wardens.Cooldown(w), sig = Wardens.Sigils;
            bool locked = w.final && sig < Wardens.SigilsNeeded;
            bool here = Wardens.I.InReach(pl.Position) == w;
            Header(list, $"{(locked ? "Sealed shrine" : w.Name)}  <size=24>{UIFactory.Col(w.place, w.color)}  ·  Lv {w.level}</size>", 36);
            if (sp != null && !locked)
            {
                string el = UIFactory.Col(sp.element1.ToString(), ElementInfo.Color(sp.element1)) + (sp.element2 != Element.Normal && sp.element2 != sp.element1 ? " / " + UIFactory.Col(sp.element2.ToString(), ElementInfo.Color(sp.element2)) : "");
                var row = UIFactory.HRow(list, 150, 16);
                row.childForceExpandHeight = false;
                var pic = UIFactory.Rect(row.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + sp.id); pic.preserveAspect = true; pic.raycastTarget = false;
                UIFactory.Size(pic, 140, 140);
                var lb = UIFactory.Label(row.transform, $"{el}\n<size=21>{sp.lore}</size>", 26, Color.white);
                lb.supportRichText = true; UIFactory.Size(lb, 140, 1000);
            }
            if (locked)
            {
                Para(list, $"The Aerie stays shut until you carry all {Wardens.SigilsNeeded} Sigils. You hold {sig}.", 24, Color.white);
                return;
            }
            Header(list, "The fight", 28);
            Para(list, "It wakes in the middle of the ring and fights you and your companion together. It can't be caught. " +
                       "Step outside the standing stones for long (about 60 m) or get knocked out and it goes back to sleep - nothing is lost.", 21, Color.white);
            if (w.optional) Para(list, UIFactory.Col($"The Kraken alone can be bound. A {ContentDB.ItemName(Wardens.KrakenSeed)} (Workbench, and it leaves some behind when beaten) takes hold once it is at {CaptureSystem.BossSeedHp * 100f:0}% health or less: SEED, then THROW.", Gold), 21);
            Para(list, $"<b>Above 66% health:</b> fights like a very strong {(sp != null ? sp.displayName : "kin")}.", 22, Color.white);
            Para(list, $"<b>Below 66%:</b> {Wardens.Describe(w.phase2, w)}", 22, Color.white);
            Para(list, $"<b>Below 33%:</b> {Wardens.Describe(w.phase3 | w.phase2 & ~(WardenMech.Shield | WardenMech.Adds), w)}", 22, Color.white);
            Header(list, "Reward", 28);
            Para(list, $"{UIFactory.Col($"{Wardens.Purse(w)} Glimmer", Gold)}, {Wardens.Hearts(w)}x Warden Heart, Starbloom Seeds and Kin Treats" +
                       (wins == 0 ? (w.final ? ", and Stormcrest itself." : w.optional ? "." : $", and the {UIFactory.Col("Sigil of " + w.land, w.color)}.") : "  <size=19>(rematch)</size>"), 24, Color.white);
            if (Wardens.I.Active != null) ActionBtn(list, "A WARDEN IS AWAKE", null, new Color(0.3f, 0.3f, 0.32f));
            else if (cd > 0) ActionBtn(list, $"RESTING  ({cd / 60}:{cd % 60:00})", null, new Color(0.3f, 0.3f, 0.32f));
            else if (!here) ActionBtn(list, $"GO TO ITS SHRINE  ({Wardens.Compass(pl.Position, w.pos)})", null, new Color(0.3f, 0.3f, 0.32f));
            else ActionBtn(list, "CHALLENGE", () => { string err = Wardens.I.Challenge(w); if (err != null) GameEvents.Warn(err); Close(); }, new Color(0.7f, 0.28f, 0.22f));
        }
    }
}
