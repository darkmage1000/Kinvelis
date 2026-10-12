using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// How a producer makes a batch: a few steps, each either hands-on (a kin, or you, puts work in) or hands-off (it
    /// simply takes time). A Crop Plot is planted, watered, left to grow and then harvested; every other producer is
    /// worked and then left to run (a smelter is stoked, then smelts by itself). The hands-off step is the point:
    /// while a plot grows or a kiln burns, the kin that tended it walks off to do something else (BaseManager.PickTask),
    /// so one good worker keeps several structures going and a camp reads as busy rather than as kin rooted to posts.
    /// Shares are fractions of the batch's whole cost (Building.Batch x WorkPerUnit): work for a hands-on step, seconds
    /// for a hands-off one.
    /// </summary>
    public struct StageDef
    {
        public string doing;        // "Planting" - shown while a kin is on it
        public string waiting;      // "Needs planting" - shown while nobody is
        public bool handsOff;
        public float share;
        public WorkType job, alt;   // who can do it; alt works at altRate
        public float altRate;
    }

    public static class WorkStages
    {
        static StageDef Work(string doing, string waiting, float share, WorkType job, WorkType alt = WorkType.None, float altRate = 0.75f) =>
            new StageDef { doing = doing, waiting = waiting, share = share, job = job, alt = alt, altRate = altRate };
        static StageDef Wait(string doing, float share) => new StageDef { doing = doing, waiting = doing, handsOff = true, share = share };

        static readonly StageDef[] Crop =
        {
            Work("Planting", "Needs planting", 0.2f, WorkType.Cultivating),
            Work("Watering", "Needs water", 0.15f, WorkType.Irrigating, WorkType.Cultivating, 0.5f),   // a farmer can water, half as fast as a kin made for it
            Wait("Growing", 0.65f),
            Work("Harvesting", "Ready to harvest", 0.2f, WorkType.Cultivating, WorkType.Foraging, 1f),
        };

        /// <summary>Index of the watering step of a Crop Plot (a running Sprinkler does it by itself).</summary>
        public const int CropWater = 1, CropGrow = 2, CropHarvest = 3;

        public static StageDef[] For(BuildingDef d)
        {
            if (d.id == Crops.PlotId || d.id == "greenhouse") return Crop;
            string work, idle, run;
            switch (d.id)
            {
                case "lumber_mill": work = "Sawing"; idle = "Logs to saw"; run = "Seasoning the planks"; break;
                case "stone_quarry": work = "Setting wedges"; idle = "Needs wedges set"; run = "Splitting stone"; break;
                case "forage_hut": work = "Foraging"; idle = "Ready to forage"; run = "Sorting and drying"; break;
                case "smelter": work = "Stoking the fire"; idle = "Needs stoking"; run = "Smelting"; break;
                case "fish_pond": work = "Baiting the lines"; idle = "Lines need bait"; run = "Waiting for a bite"; break;
                case "deep_drill": work = "Winding the drill"; idle = "Needs winding"; run = "Drilling"; break;
                default: work = "Working"; idle = "Needs a worker"; run = "Running"; break;
            }
            return new[]
            {
                Work(work, idle, 0.5f, d.workType, d.secondaryWorkType),
                Wait(run, 0.5f),
            };
        }

        /// <summary>Units made per batch: enough that a batch is worth walking over for, whatever one unit costs.</summary>
        public static int Batch(float workPerUnit) => Mathf.Clamp(Mathf.RoundToInt(36f / Mathf.Max(1f, workPerUnit)), 2, 6);

        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
