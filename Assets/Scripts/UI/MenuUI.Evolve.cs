using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The evolution show (same stage and layout as hatching, MenuUI.Hatch): the kin glows white and turns faster and
    /// faster, flickering between its old and new forms, then bursts into the new one; its page shows what changed
    /// (stats before -> after, size, work speed), its skills and what the next stage needs.
    /// </summary>
    public partial class MenuUI
    {
        struct EvolveBefore { public float hp, atk, def, size, work; public string name; }
        EvolveBefore evolveBefore;

        /// <summary>EVOLVE from the Kin tab (free = dev mode: no level or cost).</summary>
        public void EvolveWithShow(CreatureData d, bool free)
        {
            var before = new EvolveBefore { hp = d.MaxHp, atk = d.Atk, def = d.Def, size = d.SizeScale, work = d.WorkMul, name = d.DisplayName };
            if (!KinGrowth.Evolve(d, true, free)) { GameEvents.Warn(KinGrowth.WhyNot(d) ?? "It can't evolve any further."); return; }
            if (!IsOpen) Open(MenuTab.Kin);
            evolveBefore = before;
            ShowEvolve(d);
        }

        void ShowEvolve(CreatureData d)
        {
            CloseModal();
            hatchKin = d; hatchNew = false; hatchFanfare = false; hatchRevealed = false;
            if (hatchStage == null) hatchStage = new HatchStage();
            hatchStage.BeginEvolve(d);
            var pageBg = ShowFrame($"{evolveBefore.name} is evolving!", $"Becoming {CreatureData.StageName(d.stage)}  ·  tap to hurry it along", 112, out var list);
            FillEvolvePage(list, d);

            var end = UIFactory.Rect(pageBg, "End");
            end.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 16), new Vector2(-16, 100));
            var el = end.gameObject.AddComponent<HorizontalLayoutGroup>();
            el.spacing = 10; el.childControlHeight = true; el.childControlWidth = true; el.childForceExpandWidth = true; el.childForceExpandHeight = true;
            UIFactory.Button(end, "KIN CARD", () => ShowKinCard(d), new Color(0.3f, 0.42f, 0.7f), 26);
            UIFactory.Button(end, "DONE", () => { CloseModal(); Rebuild(); }, new Color(0.2f, 0.55f, 0.42f), 26);
            hatchFlash.transform.SetAsLastSibling();
            SynthAudio.Play(Sfx.Summon, 0.6f, 0.8f);
        }

        /// <summary>The words over the picture at the burst.</summary>
        void RevealEvolve(CreatureData d)
        {
            Color stageCol = d.stage >= 2 ? new Color(1f, 0.8f, 0.4f) : new Color(0.75f, 0.65f, 1f);
            hatchTitle.text = d.Form != null ? $"{evolveBefore.name} evolved into {UIFactory.Col(d.Form.name, stageCol)}!" : $"{evolveBefore.name} is now {UIFactory.Col(CreatureData.StageName(d.stage), stageCol)}!";
            float stat = (d.MaxHp / Mathf.Max(1f, evolveBefore.hp) - 1f) * 100f, work = (d.WorkMul / Mathf.Max(0.01f, evolveBefore.work) - 1f) * 100f;
            hatchSub.text = UIFactory.Col($"+{stat:0}% health, attack and defence", new Color(0.6f, 1f, 0.65f)) + (d.Form != null ? $"\na whole new shape, and it works {work:0}% faster" : $"\nbigger, glowing, and works {work:0}% faster");
            hatchSub.fontSize = 30;
        }

        /// <summary>"gains Lumen, learns Candle Blaze" for an Exalted form (empty for an Ascended one).</summary>
        static string EvoChangeText(EvoForm f)
        {
            var bits = new List<string>();
            if (f.element2 != Element.Normal) bits.Add("gains " + UIFactory.Col(f.element2.ToString(), ElementInfo.Color(f.element2)));
            var a = ContentDB.Ability(f.skill);
            if (a != null) bits.Add("learns " + UIFactory.Col(a.displayName, ElementInfo.Color(a.element)));
            return bits.Count > 0 ? string.Join(", ", bits) + "." : "";
        }

        /// <summary>The Dex line: a line of forms (names shown once you have caught one), or "perfect form".</summary>
        static string EvolutionLine(SpeciesDef sp, bool known)
        {
            if (!sp.RealEvolution) return "Evolves at Lv 20 and 35 " + UIFactory.Col("(perfect form: it keeps its shape, grows bigger and glows)", new Color(0.8f, 0.85f, 1f));
            if (!known) return "Evolves at Lv 20 and 35 " + UIFactory.Col("into new forms - catch one to see them", new Color(0.75f, 0.65f, 1f));
            var a = sp.FormAt(1); var x = sp.FormAt(2);
            string s = $"Evolves: {sp.displayName} -> {UIFactory.Col(a.name, new Color(0.75f, 0.65f, 1f))} (Lv 20)";
            if (x != null) s += $" -> {UIFactory.Col(x.name, new Color(1f, 0.8f, 0.4f))} (Lv 35)" + (EvoChangeText(x).Length > 0 ? "\n<size=19>At Exalted it " + EvoChangeText(x) + "</size>" : "");
            return s;
        }

        void FillEvolvePage(RectTransform list, CreatureData d)
        {
            var sp = d.Species;
            var b = evolveBefore;
            Color stageCol = d.stage >= 2 ? new Color(1f, 0.8f, 0.4f) : new Color(0.75f, 0.65f, 1f);
            Header(list, $"{d.DisplayName}  <size=26>Lv {d.level}</size>", 38);
            Para(list, $"{UIFactory.Col(CreatureData.StageName(d.stage) + " form", stageCol)}   ·   #{sp.dexNumber:000} {sp.displayName}   ·   {UIFactory.Col(sp.element1.ToString(), ElementInfo.Color(sp.element1))}", 25, Color.white);

            var form = d.Form;
            if (form != null)
            {
                if (!string.IsNullOrEmpty(form.blurb)) Para(list, $"<i>{form.blurb}</i>", 22, Color.white);
                if (form.element2 != Element.Normal) Para(list, $"New element: {UIFactory.Col(form.element2.ToString(), ElementInfo.Color(form.element2))}  <size=19>(its skills and weaknesses change with it)</size>", 23, Color.white);
                var fs = ContentDB.Ability(form.skill);
                if (fs != null) Para(list, $"New skill: {UIFactory.Col(fs.displayName, ElementInfo.Color(fs.element))}  <size=19>{fs.kind} · pow {fs.power}{(fs.status != StatusType.None ? $" · {fs.status} {fs.statusChance * 100:0}%" : "")} - in its fourth slot</size>", 23, Color.white);
            }
            else if (!d.Species.RealEvolution) Para(list, UIFactory.Col("Perfect form: this kind keeps its shape and grows in power.", new Color(0.8f, 0.85f, 1f)), 21, Color.white);
            // before -> after, in big rows
            Header(list, "What changed", 28);
            Row("Health", b.hp, d.MaxHp, "0");
            Row("Attack", b.atk, d.Atk, "0");
            Row("Defence", b.def, d.Def, "0");
            Row("Size", b.size, d.SizeScale, "0.00", "x");
            Row("Work speed", b.work, d.WorkMul, "0.00", "x");

            var known = new List<string>();
            var learnset = d.Learnset;
            for (int si = 0; si < learnset.Length && si < d.SkillSlots; si++)
            {
                var ad = ContentDB.Ability(learnset[si]);
                if (ad != null) known.Add(UIFactory.Col(ad.displayName, ElementInfo.Color(ad.element)) + $" <size=18>{ad.kind} · pow {ad.power}</size>");
            }
            Para(list, "<b>Skills</b>  <size=19>(hit harder now: attack is up)</size>\n  " + string.Join("\n  ", known), 22, Color.white);
            if (d.BondLevel >= 3) Para(list, "Its bond still holds: skills recharge 10% faster.", 20);

            if (KinGrowth.CanEvolveStage(d))
            {
                int g = KinGrowth.EvolveGlimmer(d);
                Para(list, $"<b>Next:</b> {UIFactory.Col(CreatureData.StageName(d.stage + 1), new Color(1f, 0.8f, 0.4f))} at Lv {KinGrowth.EvolveLevel(d)} - {Ingredients(KinGrowth.EvolveCost(d))}{(g > 0 ? $", {g} Glimmer" : "")}", 22, Color.white);
            }
            else Para(list, UIFactory.Col("Fully evolved: this is its greatest form.", new Color(1f, 0.8f, 0.4f)), 22, Color.white);
            if (!string.IsNullOrEmpty(sp.lore)) Para(list, $"<i>{sp.lore}</i>", 21);

            void Row(string label, float from, float to, string fmt, string pre = "")
            {
                var r = UIFactory.HRow(list, 52, 10);
                UIFactory.Size(UIFactory.Label(r.transform, label, 26, UIFactory.TextDim), 52, 200);
                UIFactory.Size(UIFactory.Label(r.transform, pre + from.ToString(fmt), 28, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleRight), 52, 140);
                UIFactory.Size(UIFactory.Label(r.transform, "->", 26, UIFactory.TextDim, TextAnchor.MiddleCenter), 52, 60);
                var t = UIFactory.Label(r.transform, $"<b>{pre}{to.ToString(fmt)}</b>  <size=20>{UIFactory.Col($"+{(to / Mathf.Max(0.0001f, from) - 1f) * 100f:0}%", new Color(0.55f, 1f, 0.6f))}</size>", 30, Color.white);
                t.supportRichText = true;
                UIFactory.Size(t, 52, 300);
            }
        }
    }
}
