using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// New-game partner choice: the three starters stand in front of the player (live procedural models),
    /// with a card per kin. World is paused; animation uses unscaled time.
    /// </summary>
    public class StarterSelect : MonoBehaviour
    {
        public static StarterSelect I { get; private set; }
        public bool Active { get; private set; }

        class Option
        {
            public SpeciesDef sp;
            public CreatureData data;
            public GameObject go;
            public CreatureRig rig;
            public Image card;
            public float hop;
        }

        readonly List<Option> options = new List<Option>();
        GameObject ui;
        Text confirmLabel;
        Image confirmBg;
        int selected = -1;
        float idleHop;

        static readonly Dictionary<string, string> SignatureTrait = new Dictionary<string, string>
        {
            { "knuckub", "brawny" }, { "chiskit", "diligent" }, { "hopling", "swift" }
        };

        public void Begin(Canvas canvas)
        {
            I = this;
            var starters = ContentDB.Starters;
            if (starters.Count == 0) { Debug.LogWarning("[Starter] no starter species defined"); return; }
            Active = true;
            var gm = GameManager.I;
            gm.SetPaused(true);
            HUD.I.SetVisible(false);
            gm.SetGoalVisible(false);

            var pl = gm.Player;
            Vector3 fwd = pl.Forward, right = pl.Right;
            Vector3 center = pl.Position + fwd * 5.5f;
            for (int i = 0; i < starters.Count && i < 3; i++)
            {
                var sp = starters[i];
                var o = new Option { sp = sp, data = MakeData(sp) };
                var p = center + right * (i - 1) * 2.6f;
                p.y = Mathf.Max(WorldGen.Height(p.x, p.z), WorldGen.WaterLevel);
                o.go = new GameObject("Starter_" + sp.id);
                o.go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(-fwd));
                o.rig = ModelBuilder.Build(o.data, o.go.transform, RenderAssets.Creature);
                o.hop = i * 0.7f;
                options.Add(o);
            }
            gm.CameraRig.SetOverride(pl.Position + fwd * 0.3f + Vector3.up * 2.1f, center + Vector3.down * 0.5f);
            BuildUI(canvas);
            SynthAudio.Play(Sfx.UiOpen);
        }

        static CreatureData MakeData(SpeciesDef sp)
        {
            var d = new CreatureData
            {
                uid = CreatureData.NewUid(), speciesId = sp.id, level = 3, abilities = (string[])sp.abilities.Clone(), location = KinLocation.Party
            };
            if (SignatureTrait.TryGetValue(sp.id, out var t) && ContentDB.Trait(t) != null) d.traits.Add(t);
            // partners always specialise in their role, with a solid (A-grade) score
            string role = (sp.role ?? "").ToLowerInvariant();
            int path = role.Contains("fight") ? 0 : role.Contains("build") ? 1 : 2;   // Fighter / Builder / Scout
            Aptitude.Roll(d, path);
            d.apt[path] = Mathf.Max(d.apt[path], 80);
            return d;
        }

        // ============================================================ UI
        void BuildUI(Canvas canvas)
        {
            var root = UIFactory.Panel(canvas.transform, "StarterSelect", new Color(0, 0, 0, 0));
            root.rectTransform.Stretch();
            ui = root.gameObject;

            var title = UIFactory.Label(root.transform, "Choose your partner", 56, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(1400, 72));
            var sub = UIFactory.Label(root.transform, "Every explorer on Rimewild Isle starts with a bonded kin. All three are Normal type and stay strong all game.", 26, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
            sub.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(1500, 40));

            // stat maxima across the three for relative bars
            float mHp = 1, mAtk = 1, mDef = 1, mSpd = 1;
            foreach (var o in options) { mHp = Mathf.Max(mHp, o.sp.baseHp); mAtk = Mathf.Max(mAtk, o.sp.baseAtk); mDef = Mathf.Max(mDef, o.sp.baseDef); mSpd = Mathf.Max(mSpd, o.sp.runSpeed); }

            const float cw = 580f, ch = 440f, gap = 24f;
            for (int i = 0; i < options.Count; i++)
            {
                int idx = i;
                var o = options[i];
                var sp = o.sp;
                var card = UIFactory.Panel(root.transform, "Card_" + sp.id, new Color(0.07f, 0.09f, 0.13f, 0.9f));
                card.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1) * (cw + gap), 130), new Vector2(cw, ch));
                card.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(idx));
                o.card = card;

                Color roleCol = RoleColor(sp.role);
                var name = UIFactory.Label(card.transform, sp.displayName, 40, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                name.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -14), new Vector2(330, 52));
                var pill = UIFactory.Panel(card.transform, "Role", roleCol);
                pill.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -18), new Vector2(170, 44));
                pill.raycastTarget = false;
                var pt = UIFactory.Label(pill.transform, sp.role.ToUpperInvariant(), 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                pt.rectTransform.Stretch();

                var line = UIFactory.Label(card.transform, $"Normal type  ·  {sp.personality}", 20, UIFactory.TextDim, TextAnchor.UpperLeft);
                line.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -66), new Vector2(540, 30));

                StatBar(card.transform, "HP", sp.baseHp / mHp, 0, new Color(0.95f, 0.4f, 0.4f));
                StatBar(card.transform, "ATK", sp.baseAtk / mAtk, 1, new Color(1f, 0.6f, 0.3f));
                StatBar(card.transform, "DEF", sp.baseDef / mDef, 2, new Color(0.5f, 0.7f, 1f));
                StatBar(card.transform, "SPD", sp.runSpeed / mSpd, 3, new Color(0.5f, 1f, 0.7f));

                var work = new List<string>();
                foreach (var w in sp.work) work.Add($"{w.type} {new string('*', w.level)}");
                var abil = new List<string>();
                foreach (var a in sp.abilities) { var ad = ContentDB.Ability(a); if (ad != null) abil.Add(ad.displayName); }
                string traitName = SignatureTrait.TryGetValue(sp.id, out var tr) && ContentDB.Trait(tr) != null ? ContentDB.Trait(tr).displayName : "-";
                var body = UIFactory.Label(card.transform,
                    $"{UIFactory.Col(sp.overworldRole, new Color(0.7f, 1f, 0.8f))}\n" +
                    $"<size=19>Moves: {string.Join(", ", abil)}   ·   Trait: {traitName}\nCamp work: {string.Join(", ", work)}</size>",
                    21, Color.white, TextAnchor.UpperLeft);
                body.supportRichText = true;
                body.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -262), new Vector2(540, 170));
            }

            confirmBg = UIFactory.Panel(root.transform, "Confirm", new Color(0.3f, 0.3f, 0.32f, 0.95f));
            confirmBg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(620, 92));
            confirmBg.gameObject.AddComponent<Button>().onClick.AddListener(Confirm);
            confirmLabel = UIFactory.Label(confirmBg.transform, "Tap a partner", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            confirmLabel.rectTransform.Stretch();
        }

        void StatBar(Transform card, string label, float v, int row, Color c)
        {
            float y = -108 - row * 36;
            var l = UIFactory.Label(card, label, 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            l.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, y), new Vector2(70, 30));
            var bar = UIFactory.Bar(card, c, out var fill);
            bar.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, y - 4), new Vector2(450, 22));
            bar.raycastTarget = false;
            fill.SetFill(Mathf.Clamp01(v));
        }

        static Color RoleColor(string role)
        {
            switch (role)
            {
                case "Fighter": return new Color(0.85f, 0.32f, 0.28f);
                case "Builder": return new Color(0.85f, 0.62f, 0.15f);
                default: return new Color(0.15f, 0.65f, 0.62f);
            }
        }

        void Select(int i)
        {
            selected = i;
            for (int k = 0; k < options.Count; k++)
                options[k].card.color = k == i ? new Color(0.16f, 0.32f, 0.28f, 0.96f) : new Color(0.07f, 0.09f, 0.13f, 0.9f);
            var o = options[i];
            o.rig.Squash();
            o.rig.Lunge();
            confirmLabel.text = $"CHOOSE {o.sp.displayName.ToUpperInvariant()}";
            confirmBg.color = RoleColor(o.sp.role);
            SynthAudio.Play(Sfx.Summon, 0.9f + i * 0.1f, 0.7f);
        }

        void Confirm()
        {
            if (!Active) return;
            if (selected < 0) { SynthAudio.Play(Sfx.Clunk); return; }
            var gm = GameManager.I;
            var chosen = options[selected];
            var d = chosen.data;
            var s = gm.Player.Stats;
            if (!s.registeredSpecies.Contains(d.speciesId)) s.registeredSpecies.Add(d.speciesId);
            s.starterChosen = true;
            gm.Party.AddCaptured(d);
            d.location = KinLocation.Party;
            GameManager.DexCaught(s, d, true);

            foreach (var o in options) Destroy(o.go);
            options.Clear();
            Destroy(ui);
            Active = false;
            HUD.I.SetVisible(true);
            gm.SetGoalVisible(true);
            gm.CameraRig.ClearOverride(gm.Player.transform.eulerAngles.y);
            gm.SetPaused(false);
            gm.Party.Summon(d);
            gm.MarkGoal("summon");
            SynthAudio.Play(Sfx.CaptureSuccess);
            GameEvents.Good($"{chosen.sp.displayName} is your partner! Tap its portrait (KIN) to swap kin later.");
            SaveSystem.Save();
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.unscaledDeltaTime;
            idleHop += dt;
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                o.rig.Animate(dt, i == selected ? 1.5f + Mathf.Sin(idleHop * 3f) : 0f, false, false);
                // excited selected kin hops in place every so often
                if (i == selected && Mathf.Repeat(idleHop + o.hop, 1.6f) < dt) { o.rig.Squash(); o.rig.Lunge(); }
            }
        }
    }
}
