using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The hatching show: HATCH at the Hatchery (or Camp > Hatchery) takes over the screen. The egg (HatchStage)
    /// rocks and knocks, bursts in a flash, and the kin pops out on a turntable; then its page slides in - name it,
    /// its kind, rarity and elements, quality grades, traits, looks, skills and when it learns the next ones, camp
    /// jobs, who laid the egg - and where it goes (party / camp / box). Tap the picture to hurry the egg along.
    /// The kin joins the roster the moment the show starts, so closing early loses nothing.
    /// </summary>
    public partial class MenuUI
    {
        HatchStage hatchStage;
        CreatureData hatchKin;
        bool hatchNew, hatchFanfare, hatchRevealed;
        float hatchFanfareAt;
        Text hatchTitle, hatchSub;
        Image hatchFlash;
        CanvasGroup hatchPage;
        RectTransform hatchWhere;

        /// <summary>From the Hatchery building or anywhere else: the first ready egg hatches, else the Hatchery page opens.</summary>
        public void OpenHatchery()
        {
            var e = KinGrowth.FirstReady();
            if (WorldRules.Current.simpleControls)
            {
                // the Camp tab is hidden for Little Explorers: the show is all they get
                if (e == null) { GameEvents.Say(KinGrowth.EggLine()); return; }
                Open(MenuTab.Stickers);
            }
            else OpenCamp(CampTab.Hatchery);
            if (e != null) ShowHatch(e);
        }

        void ShowHatch(KinEgg e)
        {
            var s = GameManager.I.Player.Stats;
            bool isNew = e.child != null && !s.registeredSpecies.Contains(e.child.speciesId);
            var d = KinGrowth.Hatch(e, true);
            if (d == null) return;
            CloseModal();
            hatchKin = d; hatchNew = isNew; hatchFanfare = false; hatchRevealed = false;
            if (hatchStage == null) hatchStage = new HatchStage();
            hatchStage.Begin(d);
            var sp = d.Species;

            var bg = UIFactory.Panel(panel.transform, "Hatch", new Color(0.02f, 0.03f, 0.07f, 1f));
            bg.rectTransform.Stretch();
            modal = bg.gameObject;

            // the stage picture; tapping it hurries the egg
            var frame = UIFactory.Panel(bg.transform, "Frame", new Color(1f, 0.86f, 0.45f, 0.9f));
            frame.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-405, 0), new Vector2(1008, 868));
            var raw = UIFactory.Rect(frame.transform, "Picture").gameObject.AddComponent<RawImage>();
            raw.rectTransform.Stretch(4, 4, 4, 4);
            raw.texture = hatchStage.Texture;
            var tap = raw.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => hatchStage.Skip());

            hatchTitle = UIFactory.Label(frame.transform, "Something stirs inside...", 54, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            hatchTitle.supportRichText = true;
            hatchTitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(980, 80));
            hatchTitle.raycastTarget = false;
            var sh = hatchTitle.gameObject.AddComponent<Outline>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(3, -3);
            hatchSub = UIFactory.Label(frame.transform, $"An egg from {e.parents}  ·  tap to hurry it along", 26, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter);
            hatchSub.supportRichText = true;
            hatchSub.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(980, 90));
            hatchSub.raycastTarget = false;
            var sh2 = hatchSub.gameObject.AddComponent<Outline>(); sh2.effectColor = new Color(0, 0, 0, 0.8f); sh2.effectDistance = new Vector2(2, -2);

            // its page, faded in after the burst
            var pageBg = UIFactory.Panel(bg.transform, "Page", UIFactory.PanelBg2);
            pageBg.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520, 0), new Vector2(780, 1000));
            hatchPage = pageBg.gameObject.AddComponent<CanvasGroup>();
            hatchPage.alpha = 0f; hatchPage.interactable = false; hatchPage.blocksRaycasts = false;
            var list = UIFactory.ScrollList(pageBg.transform, out _);
            ((RectTransform)list.parent).Anchors(Vector2.zero, Vector2.one, new Vector2(16, 210), new Vector2(-16, -16));
            FillHatchPage(list, d, e.parents);

            hatchWhere = UIFactory.Rect(pageBg.transform, "Where");
            hatchWhere.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 112), new Vector2(-16, 196));
            var hl = hatchWhere.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            FillHatchWhere();

            var end = UIFactory.Rect(pageBg.transform, "End");
            end.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 16), new Vector2(-16, 100));
            var el = end.gameObject.AddComponent<HorizontalLayoutGroup>();
            el.spacing = 10; el.childControlHeight = true; el.childControlWidth = true; el.childForceExpandWidth = true; el.childForceExpandHeight = true;
            UIFactory.Button(end, "KIN CARD", () => ShowKinCard(d), new Color(0.3f, 0.42f, 0.7f), 26);
            var next = KinGrowth.FirstReady();
            if (next != null) UIFactory.Button(end, "NEXT EGG", () => ShowHatch(KinGrowth.FirstReady() ?? next), new Color(0.65f, 0.45f, 0.2f), 26);
            UIFactory.Button(end, "DONE", () => { CloseModal(); Rebuild(); }, new Color(0.2f, 0.55f, 0.42f), 26);

            hatchFlash = UIFactory.Panel(bg.transform, "Flash", new Color(1f, 1f, 1f, 0f));
            hatchFlash.rectTransform.Stretch();
            hatchFlash.raycastTarget = false;
            SynthAudio.Play(Sfx.Summon, 0.7f, 0.8f);
        }

        void FillHatchPage(RectTransform list, CreatureData d, string parents)
        {
            var sp = d.Species;
            // name it
            var nrow = UIFactory.HRow(list, 64, 10);
            var nl = UIFactory.Label(nrow.transform, "Name", 26, UIFactory.TextDim); UIFactory.Size(nl, 60, 90);
            var nameIn = UIFactory.Input(nrow.transform, d.nickname ?? "", "Give it a name", 28);
            nameIn.characterLimit = 14;
            UIFactory.Size(nameIn, 60, 600);
            nameIn.onEndEdit.AddListener(v => { d.nickname = string.IsNullOrWhiteSpace(v) ? null : v.Trim(); GameEvents.RaiseRosterChanged(); });

            string els = UIFactory.Col(sp.element1.ToString(), ElementInfo.Color(sp.element1)) + (sp.element2 != sp.element1 && sp.element2 != Element.Normal ? " / " + UIFactory.Col(sp.element2.ToString(), ElementInfo.Color(sp.element2)) : "");
            Header(list, $"#{sp.dexNumber:000} {sp.displayName}  <size=26>Lv {d.level}</size>", 38);
            Para(list, $"{els}   ·   {UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity))}   ·   {sp.temperament}", 25, Color.white);

            var banners = new List<string>();
            if (hatchNew) banners.Add(UIFactory.Col("NEW KIN FOR YOUR DEX!", new Color(0.5f, 1f, 0.6f)));
            if (d.shimmer) banners.Add(UIFactory.Col($"SHIMMER - {Shimmer.NameFor(sp, d.shimmerVariant)} coat (+1 in every camp job)", new Color(1f, 0.85f, 0.35f)));
            if (d.mutation != Mutation.None) banners.Add(UIFactory.Col($"MUTATED: {d.mutation}", new Color(0.85f, 0.6f, 1f)));
            if (d.regional && sp.regional.enabled) banners.Add(UIFactory.Col(sp.regional.formName + " form", new Color(0.6f, 0.85f, 1f)));
            if (banners.Count > 0) Para(list, string.Join("\n", banners), 25, Color.white);

            // quality: three big grades
            d.EnsureAptitude();
            var grades = UIFactory.HRow(list, 128, 10);
            for (int i = 0; i < 3; i++)
            {
                int v = d.apt[i];
                var gc = UIFactory.Panel(grades.transform, "Grade", new Color(1f, 1f, 1f, i == d.spec ? 0.13f : 0.06f));
                UIFactory.Size(gc, 128, 232);
                var gt = UIFactory.Label(gc.transform, $"<size=20>{Aptitude.Names[i].ToUpperInvariant()}{(i == d.spec ? "  (best)" : "")}</size>\n<size=56><b>{UIFactory.Col(Aptitude.Grade(v), Aptitude.GradeColor(v))}</b></size>  <size=22>{v}</size>", 24, Color.white, TextAnchor.MiddleCenter);
                gt.supportRichText = true; gt.rectTransform.Stretch(4, 4, 4, 4);
            }
            Para(list, $"<size=19>Combat x{d.CombatAptMul:0.00} stats  ·  Work x{d.WorkAptMul:0.00} speed  ·  Travel x{d.TravelAptMul:0.00} speed</size>", 19, UIFactory.TextDim);

            var traits = new List<string>();
            foreach (var t in d.traits) { var td = ContentDB.Trait(t); if (td != null) traits.Add(UIFactory.Col(td.displayName, td.positive ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.5f)) + $"<size=19>: {td.description}</size>"); }
            Para(list, "<b>Traits</b>\n  " + (traits.Count > 0 ? string.Join("\n  ", traits) : "none"), 22, Color.white);
            Para(list, "<b>Looks</b>  " + d.Look.Describe(sp) + "  <size=18>(eggs take after their parents)</size>", 22, Color.white);

            var abil = new List<string>();
            var learnset = d.Learnset;
            for (int si = 0; si < learnset.Length; si++)
            {
                var ad = ContentDB.Ability(learnset[si]);
                if (ad == null) continue;
                bool known = si < d.SkillSlots;
                string line = UIFactory.Col(ad.displayName, known ? ElementInfo.Color(ad.element) : new Color(0.6f, 0.62f, 0.68f)) + $" <size=18>{ad.kind} · pow {ad.power}{(SkillEffects.Describe(ad).Length > 0 ? " · " + SkillEffects.Describe(ad) : "")}</size>";
                line += known ? UIFactory.Col("  knows it", new Color(0.55f, 1f, 0.6f)) : UIFactory.Col($"  at Lv {CreatureData.SlotUnlockLevel(si)}", new Color(1f, 0.8f, 0.4f));
                abil.Add(line);
            }
            Para(list, "<b>Skills</b>\n  " + string.Join("\n  ", abil), 22, Color.white);

            var work = new List<string>();
            foreach (var w in sp.work) work.Add($"{w.type} {new string('*', d.JobLevel(w.type))}");
            Para(list, "<b>Camp work</b>  " + (work.Count > 0 ? string.Join(",  ", work) : "none") + (d.Nocturnal ? UIFactory.Col("\nUp all night: keeps working after dark.", new Color(0.75f, 0.8f, 1f)) : ""), 22, Color.white);
            if (!string.IsNullOrEmpty(sp.lore)) Para(list, $"<i>{sp.lore}</i>", 21);
            if (!string.IsNullOrEmpty(parents)) Para(list, $"Hatched from an egg from {parents}.", 20);
        }

        void FillHatchWhere()
        {
            if (hatchWhere == null || hatchKin == null) return;
            for (int i = hatchWhere.childCount - 1; i >= 0; i--) Destroy(hatchWhere.GetChild(i).gameObject);
            var d = hatchKin;
            var party = GameManager.I.Party;
            foreach (var (loc, label) in new[] { (KinLocation.Party, "PARTY"), (KinLocation.Base, "CAMP"), (KinLocation.Box, "BOX") })
            {
                var l = loc;
                bool here = d.location == l;
                UIFactory.Button(hatchWhere, here ? $"IN {label}" : $"TO {label}", here ? (System.Action)null : () =>
                {
                    if (party.Move(d, l)) { SynthAudio.Play(Sfx.UiClick); FillHatchWhere(); }
                }, here ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 24);
            }
        }

        void TickHatch()
        {
            if (hatchStage == null || modal == null || modal.name != "Hatch") return;
            float dt = Time.unscaledDeltaTime;
            hatchStage.Tick(dt);
            hatchStage.Render();
            var d = hatchKin;
            int ev = hatchStage.Event;
            if (ev >= 0 && ev < 3)
            {
                SynthAudio.Play(Sfx.Shake, 0.85f + 0.15f * ev, 0.9f);
                hatchTitle.text = ev == 0 ? "It moved!" : ev == 1 ? "It's cracking..." : "Here it comes!";
            }
            if (hatchStage.Burst && !hatchRevealed)   // not keyed to the burst frame, so a long frame can never skip it
            {
                hatchRevealed = true;
                SynthAudio.Play(Sfx.Burst, 1.1f);
                SynthAudio.Play(Sfx.LevelUp, d.shimmer ? 1.25f : 1f);
                hatchFanfare = true; hatchFanfareAt = hatchStage.T + 0.55f;
                var sp = d.Species;
                hatchTitle.text = $"It's {(StartsWithVowel(sp.displayName) ? "an" : "a")} {UIFactory.Col(sp.displayName, Color.Lerp(RarityInfo.Color(sp.rarity), Color.white, 0.25f))}!";
                var tags = new List<string>();
                if (d.shimmer) tags.Add(UIFactory.Col($"A SHIMMER! {Shimmer.NameFor(sp, d.shimmerVariant)} coat", new Color(1f, 0.85f, 0.35f)));
                if (hatchNew) tags.Add(UIFactory.Col("New for your Dex!", new Color(0.5f, 1f, 0.6f)));
                tags.Add($"{UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity))}  ·  best at {Aptitude.Names[d.spec]} ({Aptitude.Grade(d.apt[d.spec])})");
                hatchSub.text = string.Join("\n", tags);
                hatchSub.fontSize = 30;
            }
            if (hatchFanfare && hatchStage.T >= hatchFanfareAt)
            {
                hatchFanfare = false;
                SynthAudio.Play(Sfx.CaptureSuccess, d.shimmer ? 1.5f : 1.2f);
                if (d.shimmer) SynthAudio.Play(Sfx.LevelUp, 1.6f, 0.7f);
            }
            // the flash at the burst, and the page fading in after it
            float since = hatchStage.T - HatchStage.BurstAt;
            hatchFlash.color = new Color(1f, 1f, 1f, since < 0f ? Mathf.Clamp01((since + 0.25f) / 0.25f) * 0.35f : Mathf.Clamp01(1f - since / 0.6f) * 0.95f);
            float pa = Mathf.Clamp01((since - 0.8f) / 0.45f);
            hatchPage.alpha = pa;
            bool on = pa > 0.5f;
            if (hatchPage.interactable != on) { hatchPage.interactable = on; hatchPage.blocksRaycasts = on; }
        }

        static bool StartsWithVowel(string s) => !string.IsNullOrEmpty(s) && "AEIOUaeiou".IndexOf(s[0]) >= 0;
    }
}
