using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Floating virtual joystick: appears where the thumb lands inside its zone.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform baseRing;
        public RectTransform knob;
        public float radius = 110f;
        Vector2 origin;
        Vector2 restPos;
        RectTransform zone;
        int pointerId = -99;

        void Start()
        {
            zone = (RectTransform)transform;
            restPos = baseRing.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != -99) return;
            pointerId = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out origin);
            baseRing.anchoredPosition = origin;
            knob.anchoredPosition = origin;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out var p);
            Vector2 d = Vector2.ClampMagnitude(p - origin, radius);
            knob.anchoredPosition = origin + d;
            InputHub.StickValue = d / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            pointerId = -99;
            InputHub.StickValue = Vector2.zero;
            baseRing.anchoredPosition = restPos;
            knob.anchoredPosition = restPos;
        }

        void OnDisable()
        {
            pointerId = -99;
            InputHub.StickValue = Vector2.zero;
        }
    }

    /// <summary>Right-side drag area for camera look; two-finger pinch zooms.</summary>
    public class LookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        int primary = -99, secondary = -99;
        Vector2 pPos, sPos;
        // a quick touch that barely moves is a tap: it locks on to the kin under the finger (InputHub.Tap)
        Vector2 downPos;
        float downTime, moved;
        bool tapOk;

        public void OnPointerDown(PointerEventData e)
        {
            if (primary == -99) { primary = e.pointerId; pPos = e.position; downPos = e.position; downTime = Time.unscaledTime; moved = 0f; tapOk = e.pointerId >= 0; }
            else if (secondary == -99) { secondary = e.pointerId; sPos = e.position; tapOk = false; }
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == primary)
            {
                moved += e.delta.magnitude;
                if (secondary == -99) InputHub.AddLook(e.delta);
                else PinchUpdate(e.position, sPos);
                pPos = e.position;
            }
            else if (e.pointerId == secondary)
            {
                PinchUpdate(pPos, e.position);
                sPos = e.position;
            }
        }

        void PinchUpdate(Vector2 a, Vector2 b)
        {
            float before = Vector2.Distance(pPos, sPos);
            float after = Vector2.Distance(a, b);
            InputHub.AddZoom((after - before) * 0.004f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == primary)
            {
                if (tapOk && Time.unscaledTime - downTime < 0.3f && moved < Screen.height * 0.025f && (e.position - downPos).magnitude < Screen.height * 0.025f) InputHub.Tap(e.position);
                tapOk = false;
                primary = secondary; pPos = sPos; secondary = -99;
            }
            else if (e.pointerId == secondary) secondary = -99;
        }

        void OnDisable() { primary = secondary = -99; }
    }

    /// <summary>Hold/press button that feeds InputHub.</summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Btn button;
        public Image image;
        public Color normal = new Color(1f, 1f, 1f, 0.22f);
        public Color pressed = new Color(1f, 1f, 1f, 0.5f);
        bool isDown;

        public void OnPointerDown(PointerEventData e)
        {
            isDown = true;
            InputHub.Press(button);
            if (image != null) image.color = pressed;
            transform.localScale = Vector3.one * 0.92f;
        }

        public void OnPointerUp(PointerEventData e) => Up();
        public void OnPointerExit(PointerEventData e) { if (isDown) Up(); }

        void Up()
        {
            isDown = false;
            InputHub.Release(button);
            if (image != null) image.color = normal;
            transform.localScale = Vector3.one;
        }

        void OnDisable() { if (isDown) Up(); }
    }
}
