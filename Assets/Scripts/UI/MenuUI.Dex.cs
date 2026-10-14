using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Kin Dex tab: every species with Seen / Defeated / Caught counts, best caught quality, habitat and map view.</summary>
    public partial class MenuUI
    {
        string dexSel;
        int dexFilter;   // 0 all, 1..8 a biome (DexBiomes order), 9 partners
        static readonly BiomeId[] DexBiomes = { BiomeId.Meadow, BiomeId.Badlands, BiomeId.Peaks, BiomeId.Isles, BiomeId.Swamp, BiomeId.Desert, BiomeId.Blight, BiomeId.Glow };

        void BuildDex()
        {
            SaveScroll();
            MapView.PrepareHabitat();
            var stats = GameManager.I.Player.Stats;
            var all = ContentDB.SpeciesList;
            int seen = 0, caught = 0, total = DexRewards.Total;
            foreach (var sp in all)
            {
                if (!DexRewards.Counts(sp)) continue;
                var e = stats.DexFor(sp.id, false);
                if (e != null && e.seen > 0) seen++;
                if (e != null && e.caught > 0) caught++;
            }

            LeftRight(out var left, out var right, 0.6f);
            var head = Header(left, $"Kin Dex  <size=26>Seen {seen}/{total}  ·  Caught {caught}/{total}</size>", 38);
            head.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -56), Vector2.zero);
            int ready = DexRewards.ReadyCount(stats);
            var mile = UIFactory.Button(left, ready > 0 ? $"MILESTONES ({ready}!)" : "MILESTONES", ShowDexMilestones, ready > 0 ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.32f, 0.34f, 0.4f), 20);
            ((RectTransform)mile.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-4, -4), new Vector2(230, 50));

            var bar = UIFactory.Rect(left, "Filters").Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -64), new Vector2(900, 56));
            var bh = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 4; bh.childControlWidth = true; bh.childControlHeight = true; bh.childForceExpandWidth = false;
            string[] names = { "ALL", "MEADOW", "BADLAND", "PEAKS", "ISLES", "SWAMP", "DESERT", "BLIGHT", "GLOW", "PARTNER" };
            for (int i = 0; i < names.Length; i++)
            {
                int fi = i;
                UIFactory.Size(UIFactory.Button(bar, names[i], () => { dexFilter = fi; Rebuild(); }, dexFilter == i ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 16), 56, 86);
            }

            var gridHost = UIFactory.Rect(left, "GridHost");
            gridHost.Anchors(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -128));
            var list = TrackedList(gridHost);
            DestroyImmediate(list.GetComponent<VerticalLayoutGroup>());
            var grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150, 176);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(6, 6, 6, 6);

            foreach (var sp in all)
            {
                if (!DexFilterMatch(sp)) continue;
                var e = stats.DexFor(sp.id, false);
                bool known = e != null && e.seen > 0;
                if (sp.bossOnly && !known) continue;   // Wardens you can't catch only appear once met
                bool got = e != null && e.caught > 0;
                string sid = sp.id;
                var cell = UIFactory.Panel(list, "Dex", sid == dexSel ? new Color(0.3f, 0.6f, 0.5f, 1f) : new Color(1, 1, 1, got ? 0.1f : 0.05f));
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { dexSel = sid; SynthAudio.Play(Sfx.UiClick); Rebuild(); });
                var pic = UIFactory.Rect(cell.transform, "Pic").Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(118, 118)).gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + sp.id);
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                pic.color = known ? Color.white : new Color(0f, 0f, 0f, 0.75f);   // unseen = silhouette
                if (pic.sprite == null) pic.color = new Color(1, 1, 1, 0.08f);
                var lbl = UIFactory.Label(cell.transform, $"#{sp.dexNumber:000}\n{(known ? sp.displayName : "???")}", 18, known ? Color.white : UIFactory.TextDim, TextAnchor.LowerCenter, FontStyle.Bold);
                lbl.rectTransform.Stretch(4, 4, 4, 4);
                if (got)
                {
                    var dot = UIFactory.Panel(cell.transform, "Caught", Aptitude.GradeColor(e.bestScore), UIFactory.Circle);
                    dot.type = Image.Type.Simple;
                    dot.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -6), new Vector2(30, 30));
                    var g = UIFactory.Label(dot.transform, e.bestScore >= 0 ? Aptitude.Grade(e.bestScore) : "", 18, new Color(0.1f, 0.1f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    g.rectTransform.Stretch();
                }
            }
            RestoreScroll();

            // ---------------- detail ----------------
            var rl = UIFactory.ScrollList(right, out _, 10f);
            ((RectTransform)rl.parent).Stretch();
            right = rl;
            var sel = dexSel != null ? ContentDB.Sp(dexSel) : null;
            if (sel == null)
            {
                Header(right, "Select a kin", 32);
                Para(right, "Kin are added when you spot them nearby, defeat them, or catch them.\n\n" +
                            "Every individual rolls its own quality in three paths - Combat, Work and Travel. Each kin is exceptional in one path " +
                            "(its specialty, marked *) and only decent in the other two, so catching duplicates can find you a better one. " +
                            "The badge on a caught entry is the best grade you own.", 22);
                return;
            }
            DexDetail(right, sel, stats.DexFor(sel.id, false));
        }

        bool DexFilterMatch(SpeciesDef sp)
        {
            switch (dexFilter)
            {
                case 0: return true;
                case 9: return sp.starter;
                default: return !sp.starter && sp.LivesIn(DexBiomes[Mathf.Clamp(dexFilter - 1, 0, DexBiomes.Length - 1)]);
            }
        }

        void DexDetail(RectTransform right, SpeciesDef sp, DexEntry e)
        {
            bool known = e != null && e.seen > 0;
            bool got = e != null && e.caught > 0;
            var pic = UIFactory.Rect(right, "Big").gameObject.AddComponent<Image>();
            pic.sprite = ItemIcons.Get("kin_" + sp.id);
            pic.preserveAspect = true;
            pic.color = known ? Color.white : new Color(0, 0, 0, 0.75f);
            UIFactory.Size(pic, 170);
            Header(right, $"#{sp.dexNumber:000} {(known ? sp.displayName : "???")}", 34);
            if (!known)
            {
                Para(right, sp.starter ? "A partner kin. One sets out with you; the other two wait with the Kinkeepers of Cinderhold and Frostgate (QUESTS > PARTNERS)." : "Not seen yet. Explore more of Rimewild Isle.", 22);
                return;
            }
            string el = UIFactory.Col(sp.element1.ToString(), ElementInfo.Color(sp.element1)) + (sp.element2 != Element.Normal && sp.element2 != sp.element1 ? " / " + UIFactory.Col(sp.element2.ToString(), ElementInfo.Color(sp.element2)) : "");
            Para(right, $"{el}   ·   {UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity))}   ·   {sp.temperament}", 24, Color.white);
            Para(right, $"Seen {e.seen}   ·   Defeated {e.defeated}   ·   Caught {e.caught}", 26, Color.white);
            if (sp.bossOnly) Para(right, UIFactory.Col("The Kraken: it rises only at its rock in the south-east sea. No ordinary seed holds it - a Deepcall Seed does, once it is at 15% health or less.", new Color(1f, 0.8f, 0.5f)), 21);
            else
            {
                int rank = DexRewards.Rank(e);
                Para(right, $"Research: <b>{UIFactory.Col(DexRewards.RankName(rank), DexRewards.RankColor(rank))}</b>  <size=19>{DexRewards.RankDetail(e)}</size>", 23, Color.white);
            }
            if (got && e.bestScore >= 0)
                Para(right, "Best caught: " + UIFactory.Col($"{Aptitude.Names[e.bestPath]} {Aptitude.Grade(e.bestScore)} ({e.bestScore})", Aptitude.GradeColor(e.bestScore)), 24, Color.white);
            Para(right, got ? $"<i>{sp.lore}</i>" : "<i>Catch one to learn more about it.</i>", 21);
            if (!sp.bossOnly) Para(right, EvolutionLine(sp, got), 21, Color.white);
            {
                var coats = new List<string>();
                for (int v = 0; v < Shimmer.Variants; v++) coats.Add(UIFactory.Col(Shimmer.NameFor(sp, v), Color.Lerp(Shimmer.PaletteFor(sp, v).primary, Color.white, 0.25f)));
                Para(right, "Shimmer colours: " + string.Join("  ·  ", coats) + "  <size=17>(rare; +1 to every camp job)</size>", 20, Color.white);
            }

            // habitat
            var biomes = new List<string>();
            foreach (BiomeId b in System.Enum.GetValues(typeof(BiomeId))) if (sp.LivesIn(b)) biomes.Add(BiomeInfo.Name(b));
            string when = sp.spawnTime == SpawnTime.Night ? "at night" : sp.spawnTime == SpawnTime.Day ? "by day" : "day and night";
            bool hasAlpha = false;
            foreach (var a in Landmarks.AlphaSites) if (a.species == sp.id) hasAlpha = true;
            if (!sp.starter)
            {
                Para(right, $"Habitat: {string.Join(", ", biomes)}{(sp.nearWater ? " (near water)" : "")}, {(sp.spawnCondition != SpawnCondition.None ? UIFactory.Col("only " + EnvironmentSystem.ConditionText(sp.spawnCondition), new Color(0.6f, 0.95f, 1f)) : when)}.{(hasAlpha ? UIFactory.Col("  An alpha lairs on the island.", new Color(1f, 0.7f, 0.3f)) : "")}", 22, Color.white);
                var id = sp;
                ActionBtn(right, "SHOW WHERE IT LIVES ON MAP", () => { MapView.HabitatFocus = id; Open(MenuTab.Map); }, new Color(0.25f, 0.42f, 0.55f));
            }
            else Para(right, "Partner kin don't live in the wild.", 22);

            // camp jobs: what it can be assigned to and how good the species is at it
            Header(right, "Camp jobs", 26);
            if (sp.work == null || sp.work.Length == 0) Para(right, "None - this kin is no camp worker. Keep it for the party.", 21);
            else
            {
                var jobs = new List<WorkSkill>(sp.work);
                jobs.Sort((a, b) => b.level.CompareTo(a.level));
                foreach (var w in jobs)
                {
                    if (w.type == WorkType.None || w.level <= 0) continue;
                    var where = new List<string>();
                    foreach (var bd in ContentDB.BuildingList)
                        if (bd.workType == w.type || bd.secondaryWorkType == w.type || (w.type == WorkType.Hauling && bd.isCore)
                            || (w.type == WorkType.Irrigating && (bd.id == "berry_plot" || bd.id == "greenhouse"))) where.Add(bd.displayName);   // watering is a step at the plots
                    Color jc = w.level >= 3 ? new Color(1f, 0.8f, 0.35f) : w.level == 2 ? new Color(0.6f, 0.9f, 1f) : new Color(0.8f, 0.85f, 0.8f);
                    string rank = w.level >= 4 ? "Master" : w.level == 3 ? "Expert" : w.level == 2 ? "Skilled" : "Basic";
                    Para(right, $"<b>{UIFactory.Col($"{w.type}  Lv {w.level}", jc)}</b>  <size=19>{rank} · x{w.level} work speed</size>" +
                                (where.Count > 0 ? $"\n<size=18>{UIFactory.Col(string.Join(", ", where), UIFactory.TextDim)}</size>" : ""), 23, Color.white);
                }
                Para(right, "Each individual's Work grade speeds this up or slows it down.", 18);
            }

            // owned individuals of this species, best first
            var owned = new List<CreatureData>();
            foreach (var d in GameManager.I.Party.All) if (d.speciesId == sp.id) owned.Add(d);
            if (owned.Count > 0)
            {
                owned.Sort((a, b) => b.Apt((KinPath)b.spec).CompareTo(a.Apt((KinPath)a.spec)));
                Header(right, $"Yours ({owned.Count})", 26);
                int shown = 0;
                foreach (var d in owned)
                {
                    if (shown++ >= 6) break;
                    Para(right, $"<b>{d.DisplayName}</b> Lv{d.level}  {Aptitude.Summary(d)}", 20, Color.white);
                }
            }
        }
    }
}
