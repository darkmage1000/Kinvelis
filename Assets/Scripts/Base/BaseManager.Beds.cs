using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Beds. A nest has `beds` places and takes kin up to `bedSize` (species size x alpha / giant / evolution):
    /// Kin Nest up to 1.1, Large Nest up to 1.7, Grand Nest any. Each night the kin that sleep are given beds,
    /// biggest kin first, each into the smallest free bed that fits it; those left over curl up on the ground round
    /// the hearth. A kin with a bed walks onto the pad (no pushing out of the nest while it does) and lies on it.
    /// The rested bonus (+15% work) needs a bed that fits for every kin that sleeps.
    ///
    /// The best nest also makes its sleepers work faster (`BuildingDef.bedBonus`, Grand Nest +10%, `BedBonusMul`).
    /// Big kin get those places because they need them; any left over go to the best workers in camp.
    /// </summary>
    public partial class BaseManager
    {
        public const float SmallBed = 1.1f, LargeBed = 1.7f;

        /// <summary>The biggest kin a nest takes.</summary>
        public static float BedSize(BuildingDef d) => d.bedSize > 0f ? d.bedSize : SmallBed;
        /// <summary>How high the top of the pad is, where a sleeper lies.</summary>
        public static float BedTop(BuildingDef d) => d.bedTop > 0f ? d.bedTop : 0.37f;
        /// <summary>A kin's size as far as beds go.</summary>
        public static float SleepSize(CreatureData d) => d.Species.size * d.SizeScale;
        public static string BedNeeded(CreatureData d) { float s = SleepSize(d); return s <= SmallBed ? "Kin Nest" : s <= LargeBed ? "Large Nest" : "Grand Nest"; }
        static string BedTakes(BuildingDef d) { float s = BedSize(d); return s <= SmallBed ? "small kin" : s <= LargeBed ? "small and medium kin" : "kin of any size"; }

        struct BedSlot { public Building nest; public int place; public float size, bonus; }
        readonly Dictionary<string, BedSlot> bedOf = new Dictionary<string, BedSlot>();
        readonly List<CreatureData> bedless = new List<CreatureData>();
        readonly List<CreatureData> planKin = new List<CreatureData>();
        readonly List<BedSlot> planSlots = new List<BedSlot>();
        readonly List<bool> planTaken = new List<bool>();
        int bedPlanFrame = -9999;

        /// <summary>Who sleeps where. Worked out at most every 20 frames (it is read for every work rate).</summary>
        void PlanBeds()
        {
            int f = Time.frameCount;
            if (f - bedPlanFrame < 20 && f >= bedPlanFrame) return;
            bedPlanFrame = f;
            bedOf.Clear(); bedless.Clear(); planKin.Clear(); planSlots.Clear(); planTaken.Clear();
            foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (!d.Fainted && !d.Nocturnal) planKin.Add(d);
            foreach (var b in Buildings)
            {
                if (b == null || b.Def.beds <= 0) continue;
                for (int i = 0; i < b.Def.beds; i++) { planSlots.Add(new BedSlot { nest = b, place = i, size = BedSize(b.Def), bonus = b.Def.bedBonus }); planTaken.Add(false); }
            }
            // biggest kin first, each into the smallest free bed that fits (both sorts are stable enough: ties keep roster / build order)
            for (int i = 1; i < planKin.Count; i++)
            {
                var k = planKin[i]; int j = i - 1;
                while (j >= 0 && SleepSize(planKin[j]) < SleepSize(k)) { planKin[j + 1] = planKin[j]; j--; }
                planKin[j + 1] = k;
            }
            foreach (var d in planKin)
            {
                float need = SleepSize(d); int best = -1;
                for (int i = 0; i < planSlots.Count; i++)
                {
                    if (planTaken[i] || planSlots[i].size < need) continue;
                    if (best < 0 || planSlots[i].size < planSlots[best].size) best = i;
                }
                if (best < 0) { bedless.Add(d); continue; }
                planTaken[best] = true;
                bedOf[d.uid] = planSlots[best];
            }
            // spare places in a nest that gives a bonus go to the best workers who are not already in one as good
            for (int i = 0; i < planSlots.Count; i++)
            {
                if (planTaken[i] || planSlots[i].bonus <= 0f) continue;
                CreatureData pick = null; int pickWork = -1;
                foreach (var d in planKin)
                {
                    if (SleepSize(d) > planSlots[i].size) continue;
                    if (bedOf.TryGetValue(d.uid, out var cur) && cur.bonus >= planSlots[i].bonus) continue;
                    int w = BestWork(d) * 1000 + d.level;
                    if (w > pickWork) { pickWork = w; pick = d; }
                }
                if (pick == null) continue;
                if (bedOf.TryGetValue(pick.uid, out var old))
                {
                    for (int j = 0; j < planSlots.Count; j++) if (planSlots[j].nest == old.nest && planSlots[j].place == old.place) planTaken[j] = false;
                }
                else bedless.Remove(pick);
                planTaken[i] = true;
                bedOf[pick.uid] = planSlots[i];
            }
        }

        /// <summary>A kin that sleeps in the best nest works faster (x1.10 for a Grand Nest). Night kin never sleep, so never get it.</summary>
        public float BedBonusMul(CreatureData d)
        {
            PlanBeds();
            return bedOf.TryGetValue(d.uid, out var s) && s.nest != null ? 1f + s.bonus / 100f : 1f;
        }

        /// <summary>How many kin are getting a nest's work bonus right now.</summary>
        public int WellBedded
        {
            get { PlanBeds(); int n = 0; foreach (var s in bedOf.Values) if (s.bonus > 0f && s.nest != null) n++; return n; }
        }

        /// <summary>Call when nests or the kin at camp change, so the next read plans afresh.</summary>
        public void BedsChanged() => bedPlanFrame = -9999;

        /// <summary>Kin that sleep but have no bed that fits them.</summary>
        public List<CreatureData> Bedless { get { PlanBeds(); return bedless; } }

        public float RestedMul
        {
            get { PlanBeds(); return bedless.Count == 0 && bedOf.Count > 0 ? 1.15f : 1f; }
        }

        /// <summary>Who sleeps in this nest (for its status line).</summary>
        public void SleepersIn(Building nest, List<CreatureData> into)
        {
            PlanBeds();
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                if (bedOf.TryGetValue(d.uid, out var s) && s.nest == nest) into.Add(d);
        }

        public string BedStatus(Building nest)
        {
            var l = new List<CreatureData>();
            SleepersIn(nest, l);
            var names = new List<string>();
            foreach (var d in l) names.Add(d.DisplayName);
            return $"{nest.Def.beds} beds for {BedTakes(nest.Def)}" + (nest.Def.bedBonus > 0f ? $"  ·  sleepers work {nest.Def.bedBonus:0}% faster" : "") + (names.Count > 0 ? "  ·  " + string.Join(", ", names) : "  ·  empty");
        }

        /// <summary>
        /// Where this camp kin beds down. True = on a nest: pos is its place on the pad, lift the pad's height and
        /// pad its radius. False = no bed fits it (or there are none left): a spot on the ground round the hearth.
        /// </summary>
        public bool BedFor(CreatureActor a, out Vector3 pos, out float lift, out float pad)
        {
            PlanBeds();
            if (bedOf.TryGetValue(a.Data.uid, out var s) && s.nest == null) { BedsChanged(); PlanBeds(); }   // that nest was pulled down
            if (bedOf.TryGetValue(a.Data.uid, out s) && s.nest != null)
            {
                var b = s.nest;
                pad = b.Def.footprint * 0.9f;
                // two sleepers lie either side of the middle, one has it to itself
                float side = b.Def.beds > 1 ? (s.place - (b.Def.beds - 1) * 0.5f) * 2f / b.Def.beds : 0f;   // -0.5 / +0.5 for two
                float ang = b.RotY * Mathf.Deg2Rad;
                pos = b.transform.position + new Vector3(Mathf.Cos(ang), 0, -Mathf.Sin(ang)) * (side * pad * 0.84f);
                pos.y = WorldGen.Height(pos.x, pos.z);
                lift = BedTop(b.Def);
                return true;
            }
            int i = Mathf.Max(0, bedless.IndexOf(a.Data));
            float t = i * 2.4f;
            pos = CorePosition + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * (3.2f + (i % 3) * 0.9f);
            pos.y = WorldGen.Height(pos.x, pos.z);
            lift = 0f; pad = 0f;
            return false;
        }
    }
}
