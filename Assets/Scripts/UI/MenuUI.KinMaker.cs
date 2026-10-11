using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The kin maker (Kin tab > DREAM UP A KIN): pick a body, ears, horns, tail, wings, a face, three colours, an
    /// element and a size, name it, and it joins you as a real kin - it fights, levels, works at camp and gets a kin
    /// card like any other. The picture on the left is the kin itself (KinStage) rebuilt on every choice. Limited to
    /// a few per world (more with the Toy Box on) so the roster you catch still matters.
    /// </summary>
    public partial class MenuUI
    {
        CustomKin makerDraft;

        int CustomKinLimit => WorldRules.Current.toyBox ? CustomKinParts.MaxToyBox : CustomKinParts.MaxPerWorld;

        void ShowKinMaker()
        {
            var stats = GameManager.I.Player.Stats;
            if (stats.customKin.Count >= CustomKinLimit) { GameEvents.Warn($"You can dream up {CustomKinLimit} kin in this world, and you have."); return; }
            CloseModal();
            if (makerDraft == null) makerDraft = new CustomKin();
            if (kinStage == null) kinStage = new KinStage();

            var bg = UIFactory.Panel(panel.transform, "KinCard", new Color(0.05f, 0.06f, 0.12f, 1f));   // named so TickKinCard keeps the picture turning
            bg.rectTransform.Stretch();
            modal = bg.gameObject;

            var title = UIFactory.Label(bg.transform, $"Dream up a kin  <size=24>({stats.customKin.Count}/{CustomKinLimit} made in this world)</size>", 40, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -16), new Vector2(1100, 60));

            // left: the kin
            var frame = UIFactory.Panel(bg.transform, "Frame", new Color(0, 0, 0, 0.3f));
            frame.rectTransform.Anchors(new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(40, 150), new Vector2(-10, -90));
            var raw = UIFactory.Rect(frame.transform, "Picture").gameObject.AddComponent<RawImage>();
            raw.rectTransform.Stretch(4, 4, 4, 4);
            raw.texture = kinStage.Texture;
            raw.raycastTarget = false;
            var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 664f / 500f;

            var nameRow = UIFactory.Rect(bg.transform, "Name");
            nameRow.Anchors(new Vector2(0, 0), new Vector2(0.42f, 0), new Vector2(40, 84), new Vector2(-10, 144));
            var nameIn = UIFactory.Input(nameRow, makerDraft.name, "Name it", 30);
            ((RectTransform)nameIn.transform).Stretch();
            nameIn.characterLimit = 14;
            nameIn.onEndEdit.AddListener(v => makerDraft.name = string.IsNullOrWhiteSpace(v) ? "Dreamling" : v.Trim());

            var btns = UIFactory.Rect(bg.transform, "Buttons");
            btns.Anchors(new Vector2(0, 0), new Vector2(0.42f, 0), new Vector2(40, 12), new Vector2(-10, 76));
            var hl = btns.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = true;
            UIFactory.Button(btns, "BACK", CloseModal, new Color(0.3f, 0.3f, 0.34f), 26);
            UIFactory.Button(btns, "SURPRISE ME", () => { makerDraft = RandomKin(makerDraft.name); ShowKinMaker(); }, new Color(0.5f, 0.35f, 0.7f), 26);
            UIFactory.Button(btns, "BRING IT TO LIFE", () =>
            {
                var made = GameManager.I.CreateCustomKin(makerDraft);
                makerDraft = null;
                CloseModal();
                Rebuild();
                if (made != null) ShowKinCard(made);
            }, new Color(0.2f, 0.55f, 0.42f), 26);

            // right: the choices
            var right = UIFactory.Rect(bg.transform, "Right");
            right.Anchors(new Vector2(0.42f, 0), new Vector2(1, 1), new Vector2(10, 12), new Vector2(-30, -90));
            var list = UIFactory.ScrollList(right, out _, 6f);
            ((RectTransform)list.parent).Stretch();
            var d = makerDraft;
            var rows = LookEditor.Begin(list, RefreshMakerPreview);
            rows.Names("Body", CustomKinParts.Bodies, 4, () => d.body, v => d.body = v);
            rows.Names("Size", CustomKinParts.Sizes, 3, () => d.size, v => d.size = v);
            rows.Names("Ears", CustomKinParts.Ears, 4, () => d.ears, v => d.ears = v);
            rows.Names("Horns", CustomKinParts.Horns, 3, () => d.horns, v => d.horns = v);
            rows.Names("Tail", CustomKinParts.Tails, 4, () => d.tail, v => d.tail = v);
            rows.Names("Wings", CustomKinParts.Wings, 3, () => d.wings, v => d.wings = v);
            rows.Names("Face", CustomKinParts.Faces, 4, () => d.face, v => d.face = v);
            rows.Swatches("Main colour", AvatarLook.Cloth, () => d.colorA, v => d.colorA = v);
            rows.Swatches("Belly and wings", AvatarLook.Cloth, () => d.colorB, v => d.colorB = v);
            rows.Swatches("Horns and trim", AvatarLook.Cloth, () => d.colorC, v => d.colorC = v);
            var els = new List<string>();
            foreach (Element e in System.Enum.GetValues(typeof(Element))) els.Add(e.ToString());
            rows.Names("Element  (decides its moves and its camp job)", els.ToArray(), 5, () => d.element, v => d.element = v);
            rows.Done();
            RefreshMakerPreview();
        }

        string makerKey;
        void RefreshMakerPreview()
        {
            if (makerDraft == null || kinStage == null) return;
            var sp = ContentSeed.BuildCustom(makerDraft, CustomKinParts.PreviewId);
            ContentDB.SetPreview(sp);
            var d = new CreatureData { uid = "maker_preview", speciesId = sp.id, level = 1, abilities = sp.abilities, look = new KinLook { rolled = true } };
            ModelBuilder.DropCustom(d.ModelKey);   // the last preview's meshes had the same key
            kinStage.Show(d);
        }

        static CustomKin RandomKin(string keepName)
        {
            return new CustomKin
            {
                name = keepName,
                body = Random.Range(0, CustomKinParts.Bodies.Length), size = Random.Range(0, CustomKinParts.Sizes.Length),
                ears = Random.Range(0, CustomKinParts.Ears.Length), horns = Random.Range(0, CustomKinParts.Horns.Length),
                tail = Random.Range(0, CustomKinParts.Tails.Length), wings = Random.Range(0, CustomKinParts.Wings.Length),
                face = Random.Range(0, CustomKinParts.Faces.Length), element = Random.Range(0, System.Enum.GetValues(typeof(Element)).Length),
                colorA = Random.Range(0, AvatarLook.Cloth.Length), colorB = Random.Range(0, AvatarLook.Cloth.Length), colorC = Random.Range(0, AvatarLook.Cloth.Length),
            };
        }
    }
}
