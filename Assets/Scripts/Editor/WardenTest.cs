#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Play-mode harness for Warden signatures (via execute_code): `Wake(id)` puts you in the ring with your kin and
    /// wakes it with its signature due at once; `Step(seconds)` freezes the game and runs only the Warden fight for that
    /// long (so a screenshot catches a moment); `Run()` lets the game run again; `State()` reports.
    /// </summary>
    public static class WardenTest
    {
        const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        static object Get(string f) => typeof(Wardens).GetField(f, F).GetValue(Wardens.I);
        static void Set(string f, object v) => typeof(Wardens).GetField(f, F).SetValue(Wardens.I, v);

        public static string Wake(string id)
        {
            var gm = GameManager.I;
            gm.enabled = true;
            Wardens.I.Abort();
            var w = Wardens.Get(id);
            Vector3 p = w.pos + new Vector3(0, 0, -8f); p.y = Ground.Height(p.x, p.z);
            gm.Player.transform.position = p;
            var c = gm.Party.Companion;
            if (c != null) c.transform.position = p + Vector3.right * 2f;
            gm.Player.Stats.hp = gm.Player.Stats.MaxHp;
            string why = Wardens.I.Challenge(w);
            Set("sigTimer", 0f);
            var boss = (CreatureActor)Get("boss");
            string moves = "";
            if (boss != null) for (int i = 0; i < boss.SkillCount; i++) moves += boss.Skill(i).displayName + (i < boss.SkillCount - 1 ? ", " : "");
            return $"{(why ?? "awake")}: {w.Name} at {w.pos.x:0},{w.pos.y:0.0},{w.pos.z:0}; sig {Get("sig")}; moves {moves}; phases {w.phase2} / {w.phase3}";
        }

        /// <summary>Freeze the world and advance only the fight (zones + signature) by `seconds`.</summary>
        public static string Step(float seconds)
        {
            var gm = GameManager.I;
            gm.enabled = false;
            var pl = gm.Player;
            var w = Wardens.I.Active;
            if (w == null) return "no fight";
            var comp = gm.Party.Companion;
            bool up = comp != null && comp.IsAlive;
            var sigTick = typeof(Wardens).GetMethod("TickSignature", F);
            var zoneTick = typeof(Wardens).GetMethod("TickZones", F);
            for (float t = 0; t < seconds; t += 0.02f)
            {
                zoneTick.Invoke(Wardens.I, new object[] { 0.02f, w, pl, comp, up });
                sigTick.Invoke(Wardens.I, new object[] { 0.02f, w, pl, comp, up });
            }
            pl.Stats.hp = pl.Stats.MaxHp;
            return State();
        }

        public static void Run() { GameManager.I.enabled = true; }

        public static string State()
        {
            var zones = (System.Collections.IList)Get("zones");
            var props = (System.Collections.IList)Get("sigProps");
            return $"stage {Get("sigStage")} clock {(float)Get("sigClock"):0.0} props {props.Count} zones {zones.Count} big {(Get("sigBig") != null)} frozen {(Get("frozen") != null)} thaw {(float)Get("thaw"):0.0}";
        }
    }
}
#endif
