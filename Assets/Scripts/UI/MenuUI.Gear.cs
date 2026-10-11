using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Gear tab: live 3D preview of the explorer, 7 equipment slots, stat totals, and compare-before-equip.</summary>
    public partial class MenuUI
    {
        int selectedGearSlot = -1;
        GearPreview gearPreview;

        static Color RarityCol(Rarity r) => RarityInfo.Color(r);

        void BuildGear()
        {
            SaveScroll();
            var stats = GameManager.I.Player.Stats;
            if (gearPreview == null) gearPreview = new GearPreview();
            gearPreview.Sync(stats);

            // ---------------- left: preview + slots + totals ----------------
            var left = UIFactory.Rect(content, "Left");
            left.Anchors(new Vector2(0, 0), new Vector2(0.56f, 1), new Vector2(16, 16), new Vector2(-8, -16));
            var title = UIFactory.Label(left, "Equipment", 40, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            title.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -56), new Vector2(0, 0));

            var view = UIFactory.Panel(left, "Preview", new Color(0, 0, 0, 0.25f));
            view.rectTransform.Anchors(new Vector2(0.2f, 0.2f), new Vector2(0.8f, 1f), new Vector2(0, 0), new Vector2(0, -60));
            var raw = UIFactory.Rect(view.transform, "Raw").gameObject.AddComponent<RawImage>();
            raw.rectTransform.Stretch();
            raw.texture = gearPreview.Texture;
            raw.raycastTarget = false;
            var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = GearPreview.Aspect;

            // armor down the left, accessories down the right
            for (int i = 0; i < GearSlots.Count; i++)
            {
                bool leftCol = i < 3;
                int row = leftCol ? i : i - 3;
                GearSlotButton(left, i, stats, leftCol, (leftCol ? -150f : -70f) - row * 165f);
            }

            // totals
            var tot = UIFactory.Label(left, StatSummary(stats), 22, Color.white, TextAnchor.UpperLeft);
            tot.supportRichText = true;
            tot.rectTransform.Anchors(new Vector2(0, 0), new Vector2(1, 0.2f), new Vector2(8, 0), new Vector2(-8, -8));

            // ---------------- right: slot detail / compare ----------------
            var right = UIFactory.Rect(content, "Right");
            right.Anchors(new Vector2(0.56f, 0), new Vector2(1, 1), new Vector2(8, 16), new Vector2(-16, -16));
            var list = TrackedList(right);
            if (selectedGearSlot < 0) { GearOverview(list, stats); RestoreScroll(); return; }

            int slot = selectedGearSlot;
            var kind = GearSlots.Kind[slot];
            var cur = ContentDB.Item(stats.gear[slot]);
            Header(list, GearSlots.Names[slot] + (kind == EquipSlot.Ring ? (slot == GearSlots.Ring1 ? " 1" : " 2") : ""), 34);
            if (cur != null)
            {
                GearCard(list, cur, null, true);
                var un = UIFactory.Button(list, "UNEQUIP", () =>
                {
                    if (!stats.Unequip(slot)) GameEvents.Warn("Bag is full.");
                    SynthAudio.Play(Sfx.UiClick);
                    GameEvents.RaiseInventoryChanged();
                }, new Color(0.42f, 0.3f, 0.3f), 26);
                UIFactory.Size(un, 70);
            }
            else Para(list, "Empty slot.", 24);

            var cands = new List<ItemDef>();
            var seen = new HashSet<string>();
            foreach (var s in stats.Bag.slots)
            {
                if (s.Empty || seen.Contains(s.id)) continue;
                var d = ContentDB.Item(s.id);
                if (PlayerStats.IsGear(d) && d.equipSlot == kind) { cands.Add(d); seen.Add(s.id); }
            }
            Header(list, cands.Count > 0 ? "In your bag" : "Nothing to equip here", 28);
            if (cands.Count == 0) Para(list, HowToGet(kind), 22);
            foreach (var d in cands)
            {
                var dd = d;
                GearCard(list, d, cur, false);
                var eq = UIFactory.Button(list, cur != null ? "SWAP IN" : "EQUIP", () =>
                {
                    stats.Equip(dd.id, slot);
                    SynthAudio.Play(Sfx.Craft, 1.3f, 0.6f);
                    GameEvents.RaiseInventoryChanged();
                }, new Color(0.25f, 0.5f, 0.42f), 26);
                UIFactory.Size(eq, 70);
            }
            RestoreScroll();
        }

        void GearSlotButton(RectTransform parent, int i, PlayerStats stats, bool leftCol, float y)
        {
            var d = ContentDB.Item(stats.gear[i]);
            bool sel = i == selectedGearSlot;
            var cell = UIFactory.Panel(parent, "Slot" + i, sel ? new Color(0.3f, 0.6f, 0.5f, 1f) : new Color(1, 1, 1, 0.08f));
            cell.rectTransform.Place(new Vector2(leftCol ? 0f : 1f, 1f), new Vector2(leftCol ? 0f : 1f, 1f), new Vector2(0f, y), new Vector2(150, 150));
            int idx = i;
            cell.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                selectedGearSlot = selectedGearSlot == idx ? -1 : idx;
                SynthAudio.Play(Sfx.UiClick);
                Rebuild();
            });
            if (d != null)
            {
                var rim = UIFactory.Panel(cell.transform, "Rim", RarityCol(d.rarity));
                rim.rectTransform.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 6));
                var ic = UIFactory.ItemIcon(cell.transform, d, 118);
                ic.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(118, 118));
                ic.raycastTarget = false;
            }
            else
            {
                var l = UIFactory.Label(cell.transform, GearSlots.Names[i].ToUpperInvariant(), 22, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
                l.rectTransform.Stretch();
            }
        }

        string StatSummary(PlayerStats s)
        {
            var g = s.Gear;
            string line1 = $"HP {Mathf.RoundToInt(s.MaxHp)}   ATK {s.Attack:0}   DEF {s.Defense:0}   Crit {s.CritChance * 100f:0}%   Stamina {Mathf.RoundToInt(s.MaxStamina)}";
            var fx = new List<string>();
            foreach (var id in s.gear)
            {
                var d = ContentDB.Item(id);
                if (d != null && d.unique != UniqueEffect.None) fx.Add(UIFactory.Col(GearInfo.EffectName(d.unique), new Color(1f, 0.8f, 0.4f)));
            }
            var lines = g.Lines();
            return line1 + (lines.Count > 0 ? "\n" + UIFactory.Col("Gear: " + string.Join(" · ", lines), UIFactory.Accent) : "") + (fx.Count > 0 ? "\nEffects: " + string.Join(", ", fx) : "");
        }

        void GearOverview(RectTransform list, PlayerStats stats)
        {
            Header(list, "Tap a slot", 34);
            Para(list, "Armor (head, body, boots) shows on your explorer. Accessories (amulet, bracelet, 2 rings) add stats and unique effects. Gear never wears out.", 22);
            bool any = false;
            foreach (var id in stats.gear)
            {
                var d = ContentDB.Item(id);
                if (d == null || d.unique == UniqueEffect.None) continue;
                if (!any) { Header(list, "Active effects", 28); any = true; }
                Para(list, $"{UIFactory.Col(GearInfo.EffectName(d.unique), new Color(1f, 0.8f, 0.4f))} ({d.displayName})\n{GearInfo.EffectDesc(d.unique)}", 22, Color.white);
            }
            Header(list, "Where to get gear", 28);
            Para(list, "Workbench: Explorer set (Lv 2, fluff + fiber), Ironbark set (Lv 5, ingots), Rimecrystal & Emberforge sets (Lv 8, crystal / cinder), plus rings, bracelets and amulets.\n" +
                       "Alphas drop unique trophies (Tamer's Band, Kinbond Ring, Magpie Bracelet, wards...). First defeat of each alpha species is guaranteed.", 22);
        }

        static string HowToGet(EquipSlot k)
        {
            switch (k)
            {
                case EquipSlot.Head: case EquipSlot.Body: case EquipSlot.Boots: return "Craft armor at a Workbench (its own menu: tap CRAFT at the bench).";
                default: return "Craft accessories at a Workbench, or defeat alphas for unique trophies.";
            }
        }

        /// <summary>Item card: icon, name (rarity color), stat lines. With 'compare', lines show the change vs the equipped piece.</summary>
        void GearCard(RectTransform list, ItemDef d, ItemDef compare, bool equipped)
        {
            var card = UIFactory.Panel(list, "Card", equipped ? new Color(0.3f, 0.6f, 0.5f, 0.18f) : new Color(1, 1, 1, 0.06f));
            var ic = UIFactory.ItemIcon(card.transform, d, 100);
            ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(100, 100));
            var body = new List<string>();
            body.Add($"<b>{UIFactory.Col(d.displayName, RarityCol(d.rarity))}</b>  <size=18>{d.rarity}{(d.alphaDrop ? " · Alpha trophy" : "")}</size>");
            body.Add($"<size=19>{d.description}</size>");
            body.AddRange(compare != null && !equipped ? CompareLines(d, compare) : StatLines(d));
            if (d.unique != UniqueEffect.None) body.Add(UIFactory.Col($"* {GearInfo.EffectName(d.unique)}: {GearInfo.EffectDesc(d.unique)}", new Color(1f, 0.8f, 0.4f)));
            if (compare != null && !equipped && compare.unique != UniqueEffect.None && compare.unique != d.unique)
                body.Add(UIFactory.Col($"loses {GearInfo.EffectName(compare.unique)}", new Color(1f, 0.45f, 0.4f)));
            var t = UIFactory.Label(card.transform, string.Join("\n", body), 22, Color.white, TextAnchor.UpperLeft);
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.rectTransform.Stretch(122, 10, 10, 10);
            // estimate wrapped height (~46 visible chars per line at this width)
            int rows = 0;
            foreach (var line in body) rows += Mathf.Max(1, Mathf.CeilToInt(System.Text.RegularExpressions.Regex.Replace(line, "<[^>]+>", "").Length / 46f));
            UIFactory.Size(card, Mathf.Max(124, 26 + rows * 29));
        }

        static List<string> StatLines(ItemDef d)
        {
            var l = new List<string>();
            if (d.mods != null) foreach (var m in d.mods) l.Add(UIFactory.Col(GearInfo.Format(m), UIFactory.Accent));
            return l;
        }

        /// <summary>Per-stat difference: green = better than equipped, red = worse.</summary>
        static List<string> CompareLines(ItemDef d, ItemDef cur)
        {
            var map = new Dictionary<(StatKind, Element), float>();
            if (d.mods != null) foreach (var m in d.mods) { map.TryGetValue((m.stat, m.element), out float v); map[(m.stat, m.element)] = v + m.value; }
            if (cur.mods != null) foreach (var m in cur.mods) { map.TryGetValue((m.stat, m.element), out float v); map[(m.stat, m.element)] = v - m.value; }
            var l = new List<string>();
            foreach (var kv in map)
            {
                if (Mathf.Abs(kv.Value) < 0.0001f) continue;
                var c = kv.Value > 0 ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
                l.Add(UIFactory.Col(GearInfo.Format(new StatMod(kv.Key.Item1, kv.Value, kv.Key.Item2)), c));
            }
            if (l.Count == 0) l.Add(UIFactory.Col("Same stats as equipped", UIFactory.TextDim));
            return l;
        }

        void TickGearPreview()
        {
            if (gearPreview == null) return;
            if (IsOpen && Tab == MenuTab.Gear && modal == null) gearPreview.Render(Time.unscaledDeltaTime);
        }
    }

    /// <summary>Off-screen explorer model rendered into a RenderTexture with fixed studio lighting.</summary>
    public class GearPreview
    {
        const int Layer = 30;
        public const float Aspect = 0.72f;
        static readonly Vector3 Origin = new Vector3(0f, -3000f, 0f);

        public RenderTexture Texture { get; private set; }
        readonly GameObject root;
        readonly Camera cam;
        CreatureRig rig;
        readonly List<GameObject> gear = new List<GameObject>();
        int version = -1;
        bool hat = true;
        float yaw = 200f;

        public GearPreview()
        {
            root = new GameObject("GearPreview");
            root.transform.position = Origin;
            var camGo = new GameObject("GearPreviewCam");
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.13f, 0.18f, 1f);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
            cam.allowHDR = false;
            var extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = false;
            extra.renderShadows = false;
            camGo.transform.position = Origin + new Vector3(0f, 1.25f, 6.2f);
            camGo.transform.LookAt(Origin + new Vector3(0f, 0.98f, 0f));
            Texture = new RenderTexture(360, 500, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "GearPreviewRT" };
            Texture.Create();
            cam.targetTexture = Texture;
        }

        public void Sync(PlayerStats stats)
        {
            if (stats.GearVersion == version && rig != null) return;
            version = stats.GearVersion;
            bool wantHat = string.IsNullOrEmpty(stats.gear[GearSlots.Head]);
            if (rig == null || wantHat != hat)
            {
                hat = wantHat;
                if (rig != null && rig.Model != null) Object.Destroy(rig.Model.gameObject);
                gear.Clear();
                rig = ModelBuilder.BuildCustom(hat ? "player" : "player_nohat", hat ? PlayerLook.Parts : PlayerLook.PartsNoHat, PlayerLook.Pivots, PlayerLook.Palette, root.transform, RenderAssets.Creature, Locomotion.Biped, 1f);
                SetLayer(rig.Model);
            }
            ArmorVisuals.Apply(rig, stats, gear);
            foreach (var g in gear) g.layer = Layer;
            // show the held weapon/tool too
            var held = rig.GetGroup(BodyGroup.ForearmR) ?? rig.GetGroup(BodyGroup.ArmR);
            if (held != null)
            {
                var old = held.Find("PreviewHeld");
                if (old != null) Object.Destroy(old.gameObject);
                var mesh = string.IsNullOrEmpty(stats.held) ? null : HeldItemMesh.Build(stats.held);
                if (mesh != null)
                {
                    var go = new GameObject("PreviewHeld") { layer = Layer };
                    go.transform.SetParent(held, false);
                    go.transform.localPosition = PlayerLook.Grip;
                    go.transform.localRotation = HandStyles.Carry(HandStyles.Of(ContentDB.Item(stats.held)));
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
                }
            }
        }

        static void SetLayer(Transform t)
        {
            t.gameObject.layer = Layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i));
        }

        public void Render(float dt)
        {
            if (rig == null) return;
            yaw += dt * 22f;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            rig.Animate(dt, 0f, false, false);

            // studio lighting, restored afterwards (same approach as the icon pass)
            var sun = RenderSettings.sun;
            bool fog = RenderSettings.fog;
            var probe = RenderSettings.ambientProbe;
            Quaternion sr = Quaternion.identity; float si = 0f; Color sc = Color.white; LightShadows ss = LightShadows.None;
            if (sun != null) { sr = sun.transform.rotation; si = sun.intensity; sc = sun.color; ss = sun.shadows; sun.transform.rotation = Quaternion.Euler(40f, 160f, 0f); sun.intensity = 1.1f; sun.color = Color.white; sun.shadows = LightShadows.None; }
            RenderSettings.fog = false;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(0.5f, 0.52f, 0.58f));
            sh.AddDirectionalLight(Vector3.up, new Color(0.25f, 0.25f, 0.3f), 1f);
            RenderSettings.ambientProbe = sh;

            var req = new UniversalRenderPipeline.SingleCameraRequest { destination = Texture };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else cam.Render();

            RenderSettings.fog = fog;
            RenderSettings.ambientProbe = probe;
            if (sun != null) { sun.transform.rotation = sr; sun.intensity = si; sun.color = sc; sun.shadows = ss; }
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root);
            if (cam != null) Object.Destroy(cam.gameObject);
            if (Texture != null) { Texture.Release(); Object.Destroy(Texture); }
        }
    }
}
