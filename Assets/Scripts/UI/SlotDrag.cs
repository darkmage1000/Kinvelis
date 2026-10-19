using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Drag and drop for an inventory grid slot (user, 2026-10-19: "let me move items in my bag with drag and drop").
    /// The grid sits in a scroll list, so a quick swipe still scrolls: only a finger held still on a filled slot for
    /// `HoldTime` picks the item up (the slot lights up to say so). Then a copy of the icon follows the finger, and
    /// letting go over another slot of the same grid calls `onMove(from, to)` (Inventory.MoveSlot: move, stack or swap).
    /// A plain tap is still the slot's Button click.
    /// </summary>
    public class SlotDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float HoldTime = 0.28f;

        public int index;
        public bool filled;
        public Sprite icon;
        public Color iconColor = Color.white;
        public string group;                       // slots only swap with slots of the same grid
        public System.Action<int, int> onMove;

        ScrollRect scroll;
        Image bg;
        Color bgColor;
        float downAt = -1f;
        bool pressed, grabbed, dragging;
        RectTransform ghost;
        Canvas root;

        void Awake()
        {
            scroll = GetComponentInParent<ScrollRect>();
            bg = GetComponent<Image>();
            if (bg != null) bgColor = bg.color;
        }

        void Update()
        {
            if (!pressed || grabbed || !filled) return;
            if (Time.unscaledTime - downAt >= HoldTime)
            {
                grabbed = true;
                if (bg != null) bg.color = new Color(1f, 0.85f, 0.35f, 0.9f);
                SynthAudio.Play(Sfx.UiClick, 1.4f, 0.6f);
            }
        }

        public void OnPointerDown(PointerEventData e) { pressed = true; grabbed = false; downAt = Time.unscaledTime; }

        public void OnPointerUp(PointerEventData e)
        {
            pressed = false;
            if (!dragging && grabbed && bg != null) bg.color = bgColor;
            if (!dragging) grabbed = false;
        }

        public void OnInitializePotentialDrag(PointerEventData e)
        {
            if (scroll != null) scroll.OnInitializePotentialDrag(e);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!grabbed)
            {
                // not held long enough: this is a scroll
                pressed = false;
                if (scroll != null) scroll.OnBeginDrag(e);
                return;
            }
            dragging = true;
            e.useDragThreshold = false;
            root = GetComponentInParent<Canvas>().rootCanvas;
            var go = new GameObject("DragIcon", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            ghost = (RectTransform)go.transform;
            ghost.SetParent(root.transform, false);
            ghost.SetAsLastSibling();
            ghost.sizeDelta = new Vector2(110, 110);
            var img = go.GetComponent<Image>();
            img.sprite = icon;
            img.color = icon != null ? Color.white : iconColor;
            img.preserveAspect = true;
            img.raycastTarget = false;
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;   // so the slot under the finger can be found
            Move(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging) { if (scroll != null) scroll.OnDrag(e); return; }
            Move(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) { if (scroll != null) scroll.OnEndDrag(e); return; }
            dragging = false; grabbed = false; pressed = false;
            if (ghost != null) Destroy(ghost.gameObject);
            if (bg != null) bg.color = bgColor;
            var hit = e.pointerCurrentRaycast.gameObject;
            var target = hit != null ? hit.GetComponentInParent<SlotDrag>() : null;
            if (target != null && target != this && target.group == group) onMove?.Invoke(index, target.index);
        }

        void Move(PointerEventData e)
        {
            if (ghost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)root.transform, e.position,
                root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera, out var local);
            ghost.anchoredPosition = local;
        }

        void OnDisable()
        {
            if (ghost != null) Destroy(ghost.gameObject);
            dragging = grabbed = pressed = false;
        }
    }
}
