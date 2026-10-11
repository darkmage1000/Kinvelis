using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>What kind of slot a gear item fits.</summary>
    public enum EquipSlot { None, Head, Body, Boots, Amulet, Bracelet, Ring }

    /// <summary>Stats gear can modify. Fractions are stored as 0.1 = 10%.</summary>
    public enum StatKind
    {
        Defense, Attack, MaxHp, Crit, MaxStamina, StaminaRegen, MoveSpeed, HungerReduction,
        GatherYield, CaptureChance, ColdResist, HeatResist, ElementResist, PoisonWard, Count
    }

    [Serializable]
    public struct StatMod
    {
        public StatKind stat;
        public Element element;   // only for ElementResist
        public float value;
        public StatMod(StatKind s, float v, Element e = Element.Normal) { stat = s; value = v; element = e; }
    }

    public enum UniqueEffect
    {
        None, Mercy, Tamer, Thornweave, Featherstep, Glowstone, Emberward, Frostward, Rimecloak, Kinbond, Magpie, Hearthlink
    }

    /// <summary>Player equipment positions (index into PlayerStats.gear).</summary>
    public static class GearSlots
    {
        public const int Count = 7;
        public const int Head = 0, Body = 1, Boots = 2, Amulet = 3, Bracelet = 4, Ring1 = 5, Ring2 = 6;
        public static readonly EquipSlot[] Kind = { EquipSlot.Head, EquipSlot.Body, EquipSlot.Boots, EquipSlot.Amulet, EquipSlot.Bracelet, EquipSlot.Ring, EquipSlot.Ring };
        public static readonly string[] Names = { "Head", "Body", "Boots", "Amulet", "Bracelet", "Ring", "Ring" };
        public static bool IsArmor(EquipSlot s) => s == EquipSlot.Head || s == EquipSlot.Body || s == EquipSlot.Boots;
    }

    public static class GearInfo
    {
        static bool IsPercent(StatKind s) => s != StatKind.Defense && s != StatKind.Attack && s != StatKind.MaxHp && s != StatKind.MaxStamina;

        public static string StatName(StatKind s, Element e = Element.Normal)
        {
            switch (s)
            {
                case StatKind.Defense: return "DEF";
                case StatKind.Attack: return "ATK";
                case StatKind.MaxHp: return "Max HP";
                case StatKind.Crit: return "Crit chance";
                case StatKind.MaxStamina: return "Max stamina";
                case StatKind.StaminaRegen: return "Stamina regen";
                case StatKind.MoveSpeed: return "Move speed";
                case StatKind.HungerReduction: return "Hunger drain";
                case StatKind.GatherYield: return "Gather yield";
                case StatKind.CaptureChance: return "Capture chance";
                case StatKind.ColdResist: return "Cold resist";
                case StatKind.HeatResist: return "Heat resist";
                case StatKind.ElementResist: return e + " resist";
                case StatKind.PoisonWard: return "Poison immunity";
            }
            return s.ToString();
        }

        /// <summary>"+12 DEF", "+10% Move speed", "-15% Hunger drain".</summary>
        public static string Format(StatMod m) => m.stat == StatKind.PoisonWard ? StatName(m.stat) : FormatValue(m.stat, m.value) + " " + StatName(m.stat, m.element);

        public static string FormatValue(StatKind s, float v)
        {
            if (s == StatKind.HungerReduction) return (v >= 0 ? "-" : "+") + Mathf.RoundToInt(Mathf.Abs(v) * 100f) + "%";
            if (IsPercent(s)) return (v >= 0 ? "+" : "") + Mathf.RoundToInt(v * 100f) + "%";
            return (v >= 0 ? "+" : "") + Mathf.RoundToInt(v);
        }

        public static string EffectName(UniqueEffect e)
        {
            switch (e)
            {
                case UniqueEffect.Mercy: return "Mercy";
                case UniqueEffect.Tamer: return "Tamer's Grip";
                case UniqueEffect.Thornweave: return "Thornweave";
                case UniqueEffect.Featherstep: return "Featherstep";
                case UniqueEffect.Glowstone: return "Glowsight";
                case UniqueEffect.Emberward: return "Emberward";
                case UniqueEffect.Frostward: return "Frostward";
                case UniqueEffect.Rimecloak: return "Rimecloak";
                case UniqueEffect.Kinbond: return "Kinbond";
                case UniqueEffect.Magpie: return "Magpie";
                case UniqueEffect.Hearthlink: return "Hearthlink";
            }
            return "";
        }

        public static string EffectDesc(UniqueEffect e)
        {
            switch (e)
            {
                case UniqueEffect.Mercy: return "Your hits and your companion's hits never drop wild kin below 1 HP.";
                case UniqueEffect.Tamer: return "+30% capture chance on kin below half HP.";
                case UniqueEffect.Thornweave: return "Reflects 25% of damage taken back at the attacker.";
                case UniqueEffect.Featherstep: return "No fall damage. +20% jump height.";
                case UniqueEffect.Glowstone: return "Nights are much brighter around you.";
                case UniqueEffect.Emberward: return "Immune to Burn and heatwaves.";
                case UniqueEffect.Frostward: return "Immune to Chill and cold weather.";
                case UniqueEffect.Rimecloak: return "Blizzards and snow no longer slow or chill you.";
                case UniqueEffect.Kinbond: return "Companion +20% ATK and DEF. Your ATK -10%.";
                case UniqueEffect.Magpie: return "25% chance for double loot from defeated kin; felled nodes may drop a bonus item.";
                case UniqueEffect.Hearthlink: return "Camp workers are 20% faster while you're away from camp.";
            }
            return "";
        }
    }

    /// <summary>Cached totals from all equipped gear (recomputed when gear changes).</summary>
    public class GearStats
    {
        public readonly float[] stat = new float[(int)StatKind.Count];
        public readonly float[] resist = new float[10];
        int effects;

        public float this[StatKind s] => stat[(int)s];
        public bool Has(UniqueEffect e) => (effects & (1 << (int)e)) != 0;
        public float Resist(Element e) => Mathf.Clamp(resist[(int)e], -0.5f, 0.75f);

        public void Recompute(string[] gear)
        {
            Array.Clear(stat, 0, stat.Length);
            Array.Clear(resist, 0, resist.Length);
            effects = 0;
            if (gear == null) return;
            foreach (var id in gear)
            {
                var d = string.IsNullOrEmpty(id) ? null : ContentDB.Item(id);
                if (d == null) continue;
                if (d.mods != null)
                    foreach (var m in d.mods)
                    {
                        if (m.stat == StatKind.ElementResist) resist[(int)m.element] += m.value;
                        else stat[(int)m.stat] += m.value;
                    }
                if (d.unique != UniqueEffect.None) effects |= 1 << (int)d.unique;
            }
        }

        /// <summary>Adds extra modifiers on top of the equipped gear (the active meal).</summary>
        public void Add(StatMod[] mods)
        {
            if (mods == null) return;
            foreach (var m in mods)
            {
                if (m.stat == StatKind.ElementResist) resist[(int)m.element] += m.value;
                else stat[(int)m.stat] += m.value;
            }
        }

        /// <summary>All non-zero lines, for the stat summary.</summary>
        public List<string> Lines()
        {
            var l = new List<string>();
            for (int i = 0; i < stat.Length; i++)
                if (Mathf.Abs(stat[i]) > 0.0001f && (StatKind)i != StatKind.ElementResist) l.Add(GearInfo.Format(new StatMod((StatKind)i, stat[i])));
            for (int e = 0; e < resist.Length; e++)
                if (Mathf.Abs(resist[e]) > 0.0001f) l.Add(GearInfo.Format(new StatMod(StatKind.ElementResist, resist[e], (Element)e)));
            return l;
        }
    }
}
