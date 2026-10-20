using System;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The Toy Box (user request 2026-10-10: a nephew who "loves the settings for games, messing with them"): on worlds
    /// created with it switched on, Settings gains a panel where every rule of the world can be changed while playing -
    /// the time of day, the weather, how big kin are, how high you jump, plus all the sliders from the New World screen.
    /// Everything writes straight to WorldRules.Current (saved with the world) and takes effect at once.
    /// </summary>
    public partial class MenuUI
    {
        static readonly Color ToyCol = new Color(0.55f, 0.35f, 0.75f);

        void ToyToggle(Transform row, string label, bool on, Action flip, float width = 400f)
        {
            UIFactory.Size(UIFactory.Button(row, label + ": " + (on ? "ON" : "OFF"), () => { flip(); Rebuild(); },
                on ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 24), 80, width);
        }

        void ToyBoxSection(RectTransform list)
        {
            var w = WorldRules.Current;
            if (!w.toyBox) return;
            var env = EnvironmentSystem.I;
            Header(list, UIFactory.Col("TOY BOX", new Color(0.85f, 0.7f, 1f)) + "  <size=22>change the rules of this world while you play</size>", 34);

            // ---- sky
            var sky = UIFactory.HRow(list, 84, 10);
            UIFactory.Size(UIFactory.Label(sky.transform, "Time", 26, Color.white), 80, 150);
            void TimeBtn(string label, float t) =>
                UIFactory.Size(UIFactory.Button(sky.transform, label, () => { if (env != null) env.timeOfDay = t; GameEvents.Say("It is " + label.ToLowerInvariant() + "."); }, ToyCol, 24), 80, 230);
            TimeBtn("MORNING", 0.28f); TimeBtn("NOON", 0.5f); TimeBtn("EVENING", 0.72f); TimeBtn("NIGHT", 0.92f);
            var wx = UIFactory.HRow(list, 84, 10);
            UIFactory.Size(UIFactory.Label(wx.transform, "Weather", 26, Color.white), 80, 150);
            void WxBtn(string label, WeatherType t) =>
                UIFactory.Size(UIFactory.Button(wx.transform, label, () => { if (env != null) env.SetWeather(t, 600f); Rebuild(); },
                    env != null && env.Weather == t ? new Color(0.25f, 0.55f, 0.45f) : ToyCol, 24), 80, 180);
            WxBtn("CLEAR", WeatherType.Clear); WxBtn("CLOUDY", WeatherType.Cloudy); WxBtn("RAIN", WeatherType.Rain); WxBtn("SNOW", WeatherType.Snow); WxBtn("BLIZZARD", WeatherType.Blizzard);

            // ---- silly sliders
            Func<float, string> x = v => $"x{v:0.##}";
            UIFactory.SliderRow(list, "Kin size", 0.4f, 3f, w.kinSize, false, x, v => { w.kinSize = v; foreach (var a in CreatureRegistry.All) a.ApplyWorldSize(); });
            UIFactory.SliderRow(list, "Jump height", 1f, 4f, w.jumpPower, false, x, v => w.jumpPower = v);
            UIFactory.SliderRow(list, "Your speed", 0.5f, 3f, w.moveSpeed, false, x, v => w.moveSpeed = v);
            UIFactory.SliderRow(list, "Shimmer (rare colour) odds", 1f, 2000f, w.shimmerRate, false, v => v >= 1990f ? "every kin" : $"x{v:0}", v => w.shimmerRate = v);   // base odds are 1 in 2000

            // ---- switches
            var t1 = UIFactory.HRow(list, 84, 10);
            ToyToggle(t1.transform, "Can't faint", w.cantFaint, () => w.cantFaint = !w.cantFaint);
            ToyToggle(t1.transform, "Safe travel", w.safeTravel, () => w.safeTravel = !w.safeTravel);
            ToyToggle(t1.transform, "Endless seeds & ammo", w.endlessSeeds, () => w.endlessSeeds = !w.endlessSeeds);
            var t2 = UIFactory.HRow(list, 84, 10);
            ToyToggle(t2.transform, "Friendly wild kin", w.friendlyWild, () => w.friendlyWild = !w.friendlyWild);
            ToyToggle(t2.transform, "Wild kin at my level", w.levelMatched, () => w.levelMatched = !w.levelMatched);
            ToyToggle(t2.transform, "Simple buttons", w.simpleControls, () => w.simpleControls = !w.simpleControls);
            var t3 = UIFactory.HRow(list, 84, 10);
            ToyToggle(t3.transform, "Free building", w.freeBuild, () => w.freeBuild = !w.freeBuild);
            ToyToggle(t3.transform, "Ride any kin", w.rideAnything, () => w.rideAnything = !w.rideAnything);
            ToyToggle(t3.transform, "Parade", w.parade, () => w.parade = !w.parade);
            Para(list, "Free building: camp pieces cost nothing. Ride any kin: no harness needed. Parade: your whole party walks behind you.", 20);
            Para(list, "Can't faint: you and your kin get back up. Safe travel: no fall damage, no drowning. Friendly: wild kin never start a fight. " +
                       "At my level: new wild kin are never stronger than your best kin. Simple buttons: fewer buttons, SEED throws in one tap, you always run.", 20);

            // ---- the world's rules
            UIFactory.SliderRow(list, "Wild spawn rate", 0.25f, 3f, w.wildSpawnRate, false, x, v => w.wildSpawnRate = v);
            UIFactory.SliderRow(list, "Capture chance", 0.25f, 5f, w.captureRate, false, x, v => w.captureRate = v);
            UIFactory.SliderRow(list, "XP gain", 0.25f, 10f, w.expRate, false, x, v => w.expRate = v);
            UIFactory.SliderRow(list, "Gathering yield", 0.25f, 5f, w.gatherRate, false, x, v => w.gatherRate = v);
            UIFactory.SliderRow(list, "Resource regrowth speed", 0.25f, 4f, w.resourceRespawnRate, false, x, v => w.resourceRespawnRate = v);
            UIFactory.SliderRow(list, "Damage taken", 0f, 3f, w.wildDamage, false, x, v => w.wildDamage = v);
            UIFactory.SliderRow(list, "Damage dealt", 0.25f, 10f, w.playerDamage, false, x, v => w.playerDamage = v);
            UIFactory.SliderRow(list, "Hunger drain", 0f, 3f, w.hungerRate, false, x, v => w.hungerRate = v);
            UIFactory.SliderRow(list, "Stamina drain", 0f, 2f, w.staminaDrain, false, x, v => w.staminaDrain = v);
            UIFactory.SliderRow(list, "Day length", 2f, 60f, w.dayLengthMinutes, true, v => $"{v:0} min", v => { w.dayLengthMinutes = v; if (env != null) env.SetDayLength(v * 60f); });
            UIFactory.SliderRow(list, "Camp work speed", 0.25f, 10f, w.workSpeed, false, x, v => w.workSpeed = v);
            UIFactory.SliderRow(list, "Camp raids", 0f, 3f, w.raidFrequency, false, v => v < 0.05f ? "off" : $"x{v:0.##}", v => w.raidFrequency = v);

            var reset = UIFactory.HRow(list, 84, 10);
            UIFactory.Size(UIFactory.Button(reset.transform, "PUT EVERYTHING BACK TO NORMAL", () =>
            {
                var n = WorldSettings.FromPreset(WorldPreset.Normal);
                n.worldName = w.worldName; n.seed = w.seed; n.toyBox = true; n.preset = WorldPreset.Custom;
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(n), w);   // in place: the save and every system keep the same object
                if (env != null) env.SetDayLength(n.dayLengthMinutes * 60f);
                foreach (var a in CreatureRegistry.All) a.ApplyWorldSize();
                GameEvents.Say("Every rule is back to normal.");
                Rebuild();
            }, new Color(0.5f, 0.3f, 0.3f), 24), 80, 620);
        }
    }
}
