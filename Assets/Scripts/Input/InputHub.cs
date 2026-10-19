using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Wildbond
{
    public enum Btn { Attack, Interact, Throw, Jump, Dodge, Sneak, Sprint, Summon, KinSkill, Build, Menu, Rotate, Confirm, Cancel, CycleSeed, Map, Stance, KinPanel, Hot1, Hot2, Hot3, Hot4, Hot5, Hot6, Ride, SprintToggle, Grapple, Lock, NextTarget, PrevTarget, Count }

    /// <summary>
    /// Unified input: on-screen touch controls (joystick, look pad, buttons) + keyboard/mouse/gamepad fallback.
    /// Gameplay code only reads InputHub. Call BeginFrame() before gameplay and EndFrame() after.
    /// </summary>
    public static class InputHub
    {
        public static Vector2 Move;         // -1..1
        public static Vector2 Look;         // degrees this frame
        public static float Zoom;           // + = zoom in
        public static float LookSensitivity = 1f;
        public static bool InvertY;

        // touch-fed state
        public static Vector2 StickValue;
        static Vector2 lookAccum;
        static float zoomAccum;

        static readonly bool[] held = new bool[(int)Btn.Count];
        static readonly bool[] down = new bool[(int)Btn.Count];
        static readonly bool[] uiHeld = new bool[(int)Btn.Count];

        public static void Press(Btn b) { down[(int)b] = true; uiHeld[(int)b] = true; }
        public static void Release(Btn b) { uiHeld[(int)b] = false; }
        public static void AddLook(Vector2 pixels) => lookAccum += pixels;
        public static void AddZoom(float z) => zoomAccum += z;

        /// <summary>A quick tap on the look area this frame (touch only): used to lock on to the kin under the finger.</summary>
        public static bool Tapped { get; private set; }
        public static Vector2 TapPos { get; private set; }
        static bool tapQueued;
        static Vector2 tapQueuedPos;
        public static void Tap(Vector2 screenPos) { tapQueued = true; tapQueuedPos = screenPos; }

        public static bool Down(Btn b) => down[(int)b];
        public static bool Held(Btn b) => held[(int)b];

        public static void BeginFrame()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            Tapped = tapQueued; TapPos = tapQueuedPos; tapQueued = false;

            Vector2 move = StickValue;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1;
                if (k.sqrMagnitude > 0) move = k.normalized;
            }
            if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f) move = pad.leftStick.ReadValue();
            Move = Vector2.ClampMagnitude(move, 1f);

            // look: touch pad pixels -> degrees (scaled by screen size so it feels the same on every device)
            float pxToDeg = 180f / Mathf.Max(400f, Screen.width * 0.5f);
            Vector2 look = lookAccum * pxToDeg * LookSensitivity;
            lookAccum = Vector2.zero;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null && mouse.rightButton.isPressed) look += mouse.delta.ReadValue() * 0.18f * LookSensitivity;
            if (pad != null) look += pad.rightStick.ReadValue() * 160f * Time.unscaledDeltaTime * LookSensitivity;
            if (InvertY) look.y = -look.y;
            Look = look;

            float zoom = zoomAccum;
            zoomAccum = 0f;
            if (mouse != null && !overUI) zoom += mouse.scroll.ReadValue().y * 0.01f;
            Zoom = zoom;

            for (int i = 0; i < held.Length; i++) held[i] = uiHeld[i];
            if (kb != null)
            {
                Key(kb.spaceKey, Btn.Jump);
                Key(kb.eKey, Btn.Interact);
                Key(kb.fKey, Btn.Attack);
                Key(kb.gKey, Btn.Throw);
                Key(kb.qKey, Btn.Dodge);
                Key(kb.cKey, Btn.Sneak);
                Key(kb.leftShiftKey, Btn.Sprint);
                Key(kb.rKey, Btn.Summon);
                Key(kb.tKey, Btn.KinSkill);
                Key(kb.bKey, Btn.Build);
                Key(kb.tabKey, Btn.Menu);
                Key(kb.iKey, Btn.Menu);
                Key(kb.xKey, Btn.Rotate);
                Key(kb.enterKey, Btn.Confirm);
                Key(kb.escapeKey, Btn.Cancel);
                Key(kb.vKey, Btn.CycleSeed);
                Key(kb.mKey, Btn.Map);
                Key(kb.yKey, Btn.Stance);
                Key(kb.hKey, Btn.Ride);
                Key(kb.kKey, Btn.KinPanel);
                Key(kb.zKey, Btn.Grapple);
                Key(kb.lKey, Btn.Lock);
                Key(kb.rightBracketKey, Btn.NextTarget);
                Key(kb.leftBracketKey, Btn.PrevTarget);
                Key(kb.digit1Key, Btn.Hot1);
                Key(kb.digit2Key, Btn.Hot2);
                Key(kb.digit3Key, Btn.Hot3);
                Key(kb.digit4Key, Btn.Hot4);
                Key(kb.digit5Key, Btn.Hot5);
                Key(kb.digit6Key, Btn.Hot6);
            }
            if (mouse != null && !overUI && mouse.leftButton.wasPressedThisFrame) down[(int)Btn.Attack] = true;
            if (mouse != null && !overUI && mouse.leftButton.isPressed) held[(int)Btn.Attack] = true;
            if (mouse != null && !overUI && mouse.middleButton.wasPressedThisFrame) down[(int)Btn.Lock] = true;
            if (pad != null)
            {
                PadBtn(pad.buttonWest, Btn.Attack);
                PadBtn(pad.buttonSouth, Btn.Jump);
                PadBtn(pad.buttonEast, Btn.Dodge);
                PadBtn(pad.buttonNorth, Btn.Interact);
                PadBtn(pad.rightTrigger, Btn.Throw);
                PadBtn(pad.leftShoulder, Btn.Summon);
                PadBtn(pad.rightShoulder, Btn.KinSkill);
                PadBtn(pad.startButton, Btn.Menu);
                PadBtn(pad.leftStickButton, Btn.SprintToggle);
                PadBtn(pad.rightStickButton, Btn.Lock);
                PadBtn(pad.dpad.right, Btn.NextTarget);
                PadBtn(pad.dpad.left, Btn.PrevTarget);
            }
        }

        static void Key(UnityEngine.InputSystem.Controls.KeyControl k, Btn b)
        {
            if (k.wasPressedThisFrame) down[(int)b] = true;
            if (k.isPressed) held[(int)b] = true;
        }

        static void PadBtn(UnityEngine.InputSystem.Controls.ButtonControl k, Btn b)
        {
            if (k.wasPressedThisFrame) down[(int)b] = true;
            if (k.isPressed) held[(int)b] = true;
        }

        public static void EndFrame()
        {
            for (int i = 0; i < down.Length; i++) down[i] = false;
        }

        public static void ClearAll()
        {
            for (int i = 0; i < down.Length; i++) { down[i] = false; held[i] = false; uiHeld[i] = false; }
            StickValue = Vector2.zero;
            lookAccum = Vector2.zero;
            tapQueued = false; Tapped = false;
        }
    }
}
