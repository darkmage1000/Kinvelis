using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What a Crop Plot can grow. A plot starts on Sunberries; planting something else costs a few of that crop
    /// (found wild in its biome, or as request rewards) and the plot then yields it for good.
    /// </summary>
    public static class Crops
    {
        public struct Crop
        {
            public string id;
            public int count;       // per harvest
            public float work;      // work per harvest
            public string where;    // where it grows wild
        }

        public static readonly Crop[] All =
        {
            new Crop { id = "berry", count = 2, work = 12f, where = "Sunberry bushes, everywhere in the meadow" },
            new Crop { id = "goldgrain", count = 2, work = 14f, where = "golden stalks in the Sunmeadow" },
            new Crop { id = "dewmelon", count = 1, work = 18f, where = "striped melons in the Sunmeadow" },
            new Crop { id = "emberpepper", count = 1, work = 16f, where = "red pepper shrubs in the Cinderreach Badlands" },
            new Crop { id = "frostmint", count = 2, work = 16f, where = "pale blue sprigs in the Rimefang Peaks" },
        };

        /// <summary>How many of the crop it takes to replant a plot.</summary>
        public const int PlantCost = 3;
        public const string PlotId = "berry_plot";

        /// <summary>Plain foods camp workers will eat from storage, cheapest first.</summary>
        public static readonly string[] KinFoods = { "berry", "frostmint", "goldgrain", "emberpepper", "dewmelon", "berry_tart", "silverfin" };

        public static bool IsPlot(Building b) => b != null && (b.Def.id == PlotId || b.Def.id == "greenhouse");

        public static bool TryGet(string id, out Crop c)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].id == id) { c = All[i]; return true; }
            c = default;
            return false;
        }

        /// <summary>Items per minute for one work point per second (for comparing crops in the picker).</summary>
        public static float PerMinute(Crop c) => c.count / Mathf.Max(0.01f, c.work) * 60f;

        /// <summary>Replants a plot. Returns why it can't, or null when done.</summary>
        public static string Plant(Building b, string cropId)
        {
            if (!IsPlot(b)) return "Only Crop Plots and Greenhouses can be replanted.";
            if (!TryGet(cropId, out _)) return "Unknown crop.";
            if (b.OutputItem == cropId) return "Already growing that.";
            if (b.BufferCount > 0) return "Collect the plot's harvest first.";
            var cost = new[] { new Ingredient(cropId, PlantCost) };
            if (!ItemSources.Has(cost)) return $"Needs {PlantCost}x {ContentDB.ItemName(cropId)} to plant.";
            ItemSources.Consume(cost);
            b.Crop = cropId;
            b.Progress = 0f;
            b.Stage = 0;   // dig it over and start again
            GameEvents.Good($"Planted {ContentDB.ItemName(cropId)}.");
            SynthAudio.Play(Sfx.Craft, 1.1f, 0.6f);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseBaseChanged();
            return null;
        }
    }
}
