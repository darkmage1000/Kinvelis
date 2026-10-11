using System;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Global event hub so UI and systems stay decoupled.</summary>
    public static class GameEvents
    {
        public static event Action QualityChanged;
        public static event Action InventoryChanged;
        public static event Action RosterChanged;
        public static event Action BaseChanged;
        public static event Action<string, Color> Toast;
        public static event Action<CreatureData> KinCaptured;
        public static event Action<int> PlayerLeveledUp;

        public static void RaiseQualityChanged() => QualityChanged?.Invoke();
        public static void RaiseInventoryChanged() => InventoryChanged?.Invoke();
        public static void RaiseRosterChanged() => RosterChanged?.Invoke();
        public static void RaiseBaseChanged() => BaseChanged?.Invoke();
        public static void RaiseKinCaptured(CreatureData d) => KinCaptured?.Invoke(d);
        public static void RaisePlayerLeveledUp(int lvl) => PlayerLeveledUp?.Invoke(lvl);

        public static void Say(string msg) => Toast?.Invoke(msg, Color.white);
        public static void Say(string msg, Color c) => Toast?.Invoke(msg, c);
        public static void Warn(string msg) => Toast?.Invoke(msg, new Color(1f, 0.55f, 0.45f));
        public static void Good(string msg) => Toast?.Invoke(msg, new Color(0.55f, 1f, 0.6f));
    }
}
