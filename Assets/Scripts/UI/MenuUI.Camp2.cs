using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Camp sub-tabs: Bounties (rotating tasks for Glimmer) and Expeditions (timed kin trips for loot).</summary>
    public partial class MenuUI
    {
        BiomeId expBiome = BiomeId.Meadow;
        int expDuration = 15;
        readonly List<string> expTeam = new List<string>();

        static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

        // ============================================================ BOUNTIES
        void CampBounties(RectTransform body)
        {
            Bounties.Ensure();
            var s = GameManager.I.Player.Stats;
            var list = TrackedList(body);
            var top = UIFactory.HRow(list, 64, 12);
            var gl = UIFactory.Label(top.transform, $"<b>Bounty Board</b>   {UIFactory.Col($"Glimmer: {s.glimmer}", Gold)}", 28, Color.white);
            gl.supportRichText = true; UIFactory.Size(gl, 60, 760);
            double wait = s.bountyRerollAt - GameManager.WorldTime;
            UIFactory.Size(UIFactory.Button(top.transform, wait > 0 ? $"NEW TASKS IN {Mathf.CeilToInt((float)(wait / 60.0))} MIN" : "NEW TASKS",
                wait > 0 ? (System.Action)null : () => { Bounties.Reroll(); Rebuild(); }, wait > 0 ? new Color(0.3f, 0.3f, 0.32f) : new Color(0.3f, 0.4f, 0.55f), 20), 60, 330);

            foreach (var b in s.bounties)
            {
                var bb = b;
                int prog = Bounties.Progress(b);
                bool ready = Bounties.Ready(b);
                var card = Card(list, 128, ready ? new Color(0.2f, 0.45f, 0.3f, 0.5f) : (Color?)null);
                var t = UIFactory.Label(card.transform, $"<b>{b.title}</b>\n<size=21>Reward: {UIFactory.Col($"{b.glimmer} Glimmer", Gold)}{(string.IsNullOrEmpty(b.rewardItem) ? "" : $" + {b.rewardCount}x {ContentDB.ItemName(b.rewardItem)}")}</size>", 27, Color.white, TextAnchor.UpperLeft);
                t.supportRichText = true;
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(900, 70));
                var bar = UIFactory.Bar(card.transform, ready ? Good : UIFactory.Accent, out var fill);
                bar.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 18), new Vector2(620, 22));
                fill.SetFill(prog / (float)Mathf.Max(1, b.target));
                var pl = UIFactory.Label(card.transform, $"{prog}/{b.target}", 20, Color.white);
                pl.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(650, 16), new Vector2(160, 26));
                var claim = UIFactory.Button(card.transform, ready ? (b.type == BountyType.Deliver ? "DELIVER" : "CLAIM") : "IN PROGRESS",
                    ready ? () => { Bounties.Claim(bb); Rebuild(); } : (System.Action)null,
                    ready ? new Color(0.3f, 0.6f, 0.4f) : new Color(0.3f, 0.3f, 0.32f), 22);
                ((RectTransform)claim.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(240, 90));
            }
            Para(list, "Deliver bounties take the items from your bag and camp storage when you claim them. Spend Glimmer with the Wandering Trader.", 20);
        }

        // ============================================================ EXPEDITIONS
        void CampExpeditions(RectTransform body)
        {
            var s = GameManager.I.Player.Stats;
            var list = TrackedList(body);
            Header(list, $"Expeditions  <size=22>{s.expeditions.Count}/{Expeditions.MaxConcurrent} out  ·  {UIFactory.Col($"Glimmer: {s.glimmer}", Gold)}</size>", 32);

            // active trips
            foreach (var e in s.expeditions)
            {
                var ee = e;
                bool done = Expeditions.Done(e);
                var card = Card(list, 112, done ? new Color(0.2f, 0.45f, 0.3f, 0.5f) : (Color?)null);
                var team = Expeditions.Team(e);
                var names = new List<string>();
                foreach (var d in team) names.Add(d.DisplayName);
                long left = e.endUnix - Expeditions.Now;
                string when = done ? UIFactory.Col("Back! Collect the loot.", Good) : $"returns in {(left >= 3600 ? $"{left / 3600}h {(left % 3600) / 60}m" : $"{left / 60}m {left % 60}s")}";
                var t = UIFactory.Label(card.transform, $"<b>{BiomeInfo.Name(e.biome)}</b> · {Expeditions.Label(e.minutes)}   <size=21>{string.Join(", ", names)}</size>\n<size=21>{when}</size>", 26, Color.white, TextAnchor.UpperLeft);
                t.supportRichText = true;
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(900, 64));
                var bar = UIFactory.Bar(card.transform, done ? Good : UIFactory.Accent, out var fill);
                bar.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 14), new Vector2(620, 20));
                fill.SetFill(Expeditions.Progress01(e));
                var btn = UIFactory.Button(card.transform, done ? "COLLECT" : "RECALL",
                    done ? () => { Expeditions.Collect(ee); Rebuild(); } : (System.Action)(() => Confirm("Recall expedition?", "They'll come home now with no loot.", () => { Expeditions.Recall(ee); Rebuild(); })),
                    done ? new Color(0.3f, 0.6f, 0.4f) : new Color(0.42f, 0.3f, 0.3f), 22);
                ((RectTransform)btn.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(220, 80));
            }

            if (s.expeditions.Count >= Expeditions.MaxConcurrent)
            {
                Para(list, "All expedition slots are busy. Each Hearthstone upgrade adds a slot.", 22);
                return;
            }

            // planner
            Header(list, "Plan a trip", 28);
            var row = UIFactory.HRow(list, 64, 10);
            foreach (BiomeId b in System.Enum.GetValues(typeof(BiomeId)))
            {
                var bb = b;
                bool open = Expeditions.BiomeOpen(b);
                UIFactory.Size(UIFactory.Button(row.transform, open ? BiomeInfo.Name(b) : BiomeInfo.Name(b) + " (find a waystone)",
                    open ? () => { expBiome = bb; Rebuild(); } : (System.Action)null,
                    !open ? new Color(0.25f, 0.25f, 0.27f) : expBiome == b ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 19), 60, 300);
            }
            var drow = UIFactory.HRow(list, 64, 10);
            foreach (int m in Expeditions.Durations)
            {
                int mm = m;
                UIFactory.Size(UIFactory.Button(drow.transform, Expeditions.Label(m), () => { expDuration = mm; Rebuild(); },
                    expDuration == m ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 22), 60, 180);
            }

            Para(list, $"Team ({expTeam.Count}/{Expeditions.MaxTeam}) - idle kin from your party or Hearth Box. Higher level and better Combat/Travel grades bring back more.", 21, Color.white);
            expTeam.RemoveAll(uid => GameManager.I.Party.All.Find(d => d.uid == uid) == null);
            foreach (var d in GameManager.I.Party.All)
            {
                if (!Expeditions.Available(d)) continue;
                var dd = d;
                bool pick = expTeam.Contains(d.uid);
                var r = UIFactory.HRow(list, 78, 12);
                var p = Portrait(r.transform, d, 64); UIFactory.Size(p, 64, 64);
                var lt = UIFactory.Label(r.transform, $"<b>{d.DisplayName}</b> Lv {d.level}  <size=19>{(d.location == KinLocation.Party ? "party" : "box")} · Combat {Aptitude.Grade(d.Apt(KinPath.Combat))} · Travel {Aptitude.Grade(d.Apt(KinPath.Travel))}</size>", 24, Color.white);
                lt.supportRichText = true; UIFactory.Size(lt, 70, 760);
                UIFactory.Size(UIFactory.Button(r.transform, pick ? "REMOVE" : "ADD", () =>
                {
                    if (pick) expTeam.Remove(dd.uid);
                    else if (expTeam.Count < Expeditions.MaxTeam) expTeam.Add(dd.uid);
                    Rebuild();
                }, pick ? new Color(0.45f, 0.3f, 0.3f) : new Color(0.25f, 0.45f, 0.5f), 20), 64, 160);
            }
            var go = UIFactory.Button(list, $"SEND TO {BiomeInfo.Name(expBiome).ToUpperInvariant()} ({Expeditions.Label(expDuration)})", () =>
            {
                var team = new List<CreatureData>();
                foreach (var uid in expTeam) { var d = GameManager.I.Party.All.Find(x => x.uid == uid); if (d != null) team.Add(d); }
                string err = Expeditions.Start(expBiome, expDuration, team);
                if (err != null) GameEvents.Warn(err); else expTeam.Clear();
                Rebuild();
            }, expTeam.Count > 0 ? new Color(0.3f, 0.6f, 0.4f) : new Color(0.3f, 0.3f, 0.32f), 24);
            UIFactory.Size(go, 76);
        }
    }
}
