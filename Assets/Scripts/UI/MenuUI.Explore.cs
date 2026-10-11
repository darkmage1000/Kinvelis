using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>The Explorer's log page (Journey > LOG): the eight lands, what is left to find in each, milestone rewards, and the lore you have read.</summary>
    public partial class MenuUI
    {
        BiomeId exploreLand = BiomeId.Meadow;
        Pois.Poi lorePoi;

        public void OpenExplore()
        {
            lorePoi = null;
            var env = EnvironmentSystem.I;
            if (env != null) exploreLand = env.CurrentBiome;
            Open(MenuTab.Explore);
        }

        /// <summary>Opened by READ at a tablet, journal or plinth.</summary>
        public void OpenLore(Pois.Poi p)
        {
            lorePoi = p;
            exploreLand = p.biome;
            Open(MenuTab.Explore);
        }

        void BuildExplore()
        {
            var stats = GameManager.I.Player.Stats;
            LeftRight(out var left, out var right, 0.4f);
            var list = TrackedList(left);
            Header(list, $"Explorer's log  <size=24>{UIFactory.Col($"{ExplorerLog.Overall(stats)}% of Rimewild", Gold)}</size>", 36);
            Para(list, "Every land keeps count of what you have found in it. Each quarter of a land pays a reward.", 21);
            foreach (var b in ExplorerLog.Lands)
            {
                var land = b;
                int pct = ExplorerLog.Percent(b, stats), ready = 0;
                foreach (int st in ExplorerLog.Steps) if (ExplorerLog.Ready(stats, b, st)) ready++;
                string label = $"{BiomeInfo.Name(b)}   <b>{pct}%</b>{(ready > 0 ? UIFactory.Col($"   CLAIM {ready}", new Color(0.6f, 1f, 0.6f)) : "")}";
                var btn = UIFactory.Button(list, label, () => { exploreLand = land; lorePoi = null; Rebuild(); },
                    b == exploreLand ? new Color(0.25f, 0.5f, 0.42f) : new Color(0.2f, 0.24f, 0.3f), 26);
                UIFactory.Size(btn, 78);
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) { txt.supportRichText = true; txt.color = Color.Lerp(BiomeInfo.Tint(b), Color.white, 0.55f); }
            }

            var rl = UIFactory.ScrollList(right, out _, 10f);
            ((RectTransform)rl.parent).Stretch();
            if (lorePoi != null)
            {
                Header(rl, lorePoi.title, 32);
                Para(rl, lorePoi.lore, 27, Color.white);
                Para(rl, $"Found at {Pois.KindName(lorePoi.kind)} in {UIFactory.Col(BiomeInfo.Name(lorePoi.biome), BiomeInfo.Tint(lorePoi.biome))}.", 20);
                Para(rl, " ", 12);
            }

            int landPct = ExplorerLog.Percent(exploreLand, stats);
            Header(rl, $"{UIFactory.Col(BiomeInfo.Name(exploreLand), Color.Lerp(BiomeInfo.Tint(exploreLand), Color.white, 0.4f))}  <size=26>{landPct}% explored</size>", 34);
            var bar = UIFactory.Bar(rl, Color.Lerp(BiomeInfo.Tint(exploreLand), Color.white, 0.2f), out var fill);
            UIFactory.Size(bar, 22);
            fill.SetFill(landPct / 100f);
            foreach (var l in ExplorerLog.Lines(exploreLand, stats))
            {
                bool done = l.have >= l.total;
                Para(rl, $"{(done ? UIFactory.Col("[x] ", new Color(0.6f, 1f, 0.6f)) : "[ ] ")}{l.label}   <b>{l.have}/{l.total}</b>", 23, done ? new Color(0.75f, 0.95f, 0.75f) : Color.white);
            }

            Header(rl, "Milestones", 28);
            foreach (int step in ExplorerLog.Steps)
            {
                int st = step;
                bool claimed = ExplorerLog.Claimed(stats, exploreLand, st), ready = ExplorerLog.Ready(stats, exploreLand, st);
                var row = UIFactory.HRow(rl, 80, 12);
                row.childForceExpandHeight = false;
                var t = UIFactory.Label(row.transform, $"<b>{st}%</b>  <size=21>{ExplorerLog.RewardText(exploreLand, st)}</size>", 25, claimed ? UIFactory.TextDim : Color.white);
                t.supportRichText = true; UIFactory.Size(t, 74, 620);
                var land = exploreLand;
                var cb = UIFactory.Button(row.transform, claimed ? "CLAIMED" : ready ? "CLAIM" : $"AT {st}%", ready ? () => { ExplorerLog.Claim(land, st); Rebuild(); } : (System.Action)null,
                    ready ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.26f, 0.27f, 0.3f), 22);
                UIFactory.Size(cb, 70, 170);
            }

            // lore read in this land
            int tablets = 0, read = 0;
            foreach (var p in Pois.All) if (p.biome == exploreLand && p.hasTablet) { tablets++; if (Pois.Read(p)) read++; }
            if (tablets > 0)
            {
                Header(rl, $"Lore  <size=22>{read}/{tablets} read</size>", 28);
                if (read == 0) Para(rl, "Tablets, journals and plinths out in the land. READ one and it is kept here.", 21);
                foreach (var p in Pois.All)
                {
                    if (p.biome != exploreLand || !p.hasTablet || !Pois.Read(p)) continue;
                    var poi = p;
                    var lb = UIFactory.Button(rl, $"{p.title}  <size=20>({p.name})</size>", () => { lorePoi = poi; Rebuild(); }, p == lorePoi ? new Color(0.3f, 0.42f, 0.5f) : new Color(0.2f, 0.24f, 0.3f), 23);
                    UIFactory.Size(lb, 64);
                    var lt = lb.GetComponentInChildren<Text>();
                    if (lt != null) lt.supportRichText = true;
                }
            }
        }

        // ============================================================ JOURNEY LINE
        void JourneyExplore(RectTransform list)
        {
            var stats = GameManager.I.Player.Stats;
            int ready = ExplorerLog.ReadyCount(stats);
            Header(list, $"Explorer's log  <size=24>{UIFactory.Col($"{ExplorerLog.Overall(stats)}% of Rimewild found", Gold)}</size>", 32);
            var row = UIFactory.HRow(list, 84, 12);
            row.childForceExpandHeight = false;
            var env = EnvironmentSystem.I;
            BiomeId here = env != null ? env.CurrentBiome : BiomeId.Meadow;
            string line = $"{BiomeInfo.Name(here)}: {ExplorerLog.Percent(here, stats)}% explored. Named places, caves, lookouts, wrecks and camps, relics, alphas and the Warden all count.";
            var t = UIFactory.Label(row.transform, $"<size=22>{line}</size>", 24, Color.white);
            t.supportRichText = true; UIFactory.Size(t, 78, 1100);
            UIFactory.Size(UIFactory.Button(row.transform, ready > 0 ? $"CLAIM ({ready})" : "LOG", OpenExplore, ready > 0 ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.32f, 0.34f, 0.4f), 22), 76, 190);
        }
    }
}
