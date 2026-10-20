using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Stored charge. A Volt Pylon is a battery as well as a generator: while a Charging kin works it the camp is
    /// powered directly and the surplus is stored; when the kin sleeps, eats or is called away, the machines run on
    /// what was stored until it is gone. A pylon holds a fifth of a day; each Volt Battery holds another third or so,
    /// so a pylon and one battery filled by day last the night. Charge is kept in seconds of running time, so it is
    /// the same sum near, far and while the game is closed.
    ///
    /// The kin is not tied to the pylon: it charges it to full and is then free for its other jobs. The pylon asks
    /// for a kin again (`PylonWantsKin`, which is what `Building.CanWork` answers for a pylon) when the charge is
    /// down to half, and through the evening so that the camp goes into the night full. Any Charging kin answers,
    /// the one assigned to the pylon first.
    /// </summary>
    public partial class BaseManager
    {
        /// <summary>How much a structure holds, as a share of one day.</summary>
        public const float PylonHolds = 0.2f, BatteryHolds = 0.35f;
        /// <summary>Seconds of charge stored per unit of work at the pylon (a level-1 kin stores about 1.5 s a second).</summary>
        const float ChargeGain = 1.5f;

        /// <summary>The pylon calls a kin back when the charge is down to this share of what it holds.</summary>
        const float RefillBelow = 0.5f;

        /// <summary>Stored power, in seconds the camp's machines can run with nobody at the pylon.</summary>
        public float Charge;
        bool topUp = true;   // latched: charging until full (and all evening), then released until it runs down

        /// <summary>Set by the offline catch-up, which keeps a clock of its own; otherwise the world's.</summary>
        bool? eveningOverride;
        /// <summary>The last stretch of daylight: time to fill up for the night.</summary>
        bool Evening
        {
            get
            {
                if (eveningOverride.HasValue) return eveningOverride.Value;
                var env = EnvironmentSystem.I;
                return env != null && env.timeOfDay >= 0.64f && !env.IsNight;
            }
        }

        /// <summary>Does the pylon need a kin at it right now? (Otherwise its Charging kin is free to do other work.)</summary>
        public bool PylonWantsKin => topUp;

        float DaySeconds => EnvironmentSystem.I != null ? Mathf.Max(60f, EnvironmentSystem.I.DayLength) : 1200f;

        public float ChargeCapacity
        {
            get
            {
                float n = 0f; bool pylon = false;
                foreach (var b in Buildings)
                {
                    if (b.Def.id == "volt_pylon") { n += PylonHolds; pylon = true; }
                    else if (b.Def.id == "volt_battery") n += BatteryHolds;
                }
                return pylon ? n * DaySeconds : 0f;   // batteries are charged through a pylon
            }
        }

        public float Charge01 { get { float c = ChargeCapacity; return c > 0f ? Mathf.Clamp01(Charge / c) : 0f; } }

        /// <summary>A kin is at a pylon right now (the camp runs straight off it and the surplus is stored).</summary>
        public bool PylonStaffed
        {
            get { foreach (var b in Buildings) if (b.Def.id == "volt_pylon" && b.Powered) return true; return false; }
        }

        /// <summary>Stored charge only runs down while something is plugged in.</summary>
        bool DrawsPower => Has("conveyor_hub") || Has("auto_assembler") || Has("sprinkler") || Has("volt_turret") || Has("dynamo");

        /// <summary>Work done at a pylon (Building.AddWork), near, far or offline.</summary>
        // work arrives at camp pace (CampPace); stored power and cold don't slow down, so a pylon still outruns its drain
        public void AddCharge(float work) => Charge = Mathf.Min(ChargeCapacity, Charge + work / CampPace * ChargeGain);

        // ============================================================ COLD
        // The Frost Larder keeps its chill the same way: a Chilling kin fills it with cold, then is free; the food stays
        // fresh (workers get hungry 40% slower) for as long as any cold is left. One larder holds enough for the night.

        /// <summary>How much cold one Frost Larder holds, as a share of one day (night is 0.45).</summary>
        public const float LarderHolds = 0.6f;
        const float ColdGain = 1.5f;

        /// <summary>Stored cold, in seconds the larder keeps food fresh with nobody at it.</summary>
        public float Cold;
        bool coldTopUp = true;

        public static bool IsLarder(Building b) => b != null && b.Def.workType == WorkType.Chilling;

        public float ColdCapacity
        {
            get { int n = 0; foreach (var b in Buildings) if (IsLarder(b)) n++; return n * LarderHolds * DaySeconds; }
        }

        public float Cold01 { get { float c = ColdCapacity; return c > 0f ? Mathf.Clamp01(Cold / c) : 0f; } }

        public bool LarderStaffed
        {
            get { foreach (var b in Buildings) if (IsLarder(b) && b.Powered) return true; return false; }
        }

        /// <summary>Does a larder need a kin at it right now? (Otherwise its Chilling kin is free to do other work.)</summary>
        public bool LarderWantsKin => coldTopUp;

        /// <summary>Night, the cold has run out, and the kin who could chill the larder are in bed.</summary>
        public bool ColdAsleep
        {
            get
            {
                if (!NightNow || LarderActive || ColdCapacity <= 0f) return false;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                    if (!d.Fainted && d.WorkLevel(WorkType.Chilling) > 0 && Asleep(d)) return true;
                return false;
            }
        }

        /// <summary>Work done at a larder (Building.AddWork), near, far or offline.</summary>
        public void AddCold(float work) => Cold = Mathf.Min(ColdCapacity, Cold + work / CampPace * ColdGain);

        void TickCold(float dt)
        {
            float cap = ColdCapacity;
            if (cap <= 0f) { Cold = 0f; return; }
            if (!LarderStaffed) Cold = Mathf.Max(0f, Cold - dt);
            if (Cold > cap) Cold = cap;
            bool evening = Evening;
            if (Cold <= cap * RefillBelow || (evening && Cold < cap * 0.97f)) coldTopUp = true;
            else if (Cold >= cap - 0.5f && !evening) coldTopUp = false;
        }

        void TickCharge(float dt)
        {
            TickCold(dt);
            float cap = ChargeCapacity;
            if (cap <= 0f) { Charge = 0f; return; }
            if (!PylonStaffed && DrawsPower) Charge = Mathf.Max(0f, Charge - dt);
            if (Charge > cap) Charge = cap;
            bool evening = Evening;
            if (Charge <= cap * RefillBelow || (evening && Charge < cap * 0.97f)) topUp = true;
            else if (Charge >= cap - 0.5f && !evening) topUp = false;   // full: the kin can go (in the evening it stays and keeps it brimming)
        }

        /// <summary>"73% · 6:10 stored" for menus and labels.</summary>
        public string ChargeText => $"{Mathf.RoundToInt(Charge01 * 100f)}% · {WorkStages.Clock(Charge)} stored";
    }
}
