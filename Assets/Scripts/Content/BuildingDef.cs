using UnityEngine;

namespace Wildbond
{
    [CreateAssetMenu(menuName = "Wildbond/Building", fileName = "Building_")]
    public class BuildingDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(1, 3)] public string description;
        public Ingredient[] cost = new Ingredient[0];
        public int unlockLevel = 1;
        public float footprint = 1.5f;
        public bool isCore;
        [Tooltip("Build menu tab. Auto: stations = Crafting, anything with an output = Production, storage = Storage, beds / feeding = Kin Care, the rest = Utility.")]
        public BuildCategory category;

        [Header("Work / Production")]
        public WorkType workType;
        [Tooltip("Optional helper job that also progresses this building at 75% efficiency.")]
        public WorkType secondaryWorkType;
        public int workerSlots;
        public string outputItem;
        public int outputCount = 1;
        [Tooltip("Work points per output. A level-1 worker produces 1 work point per second.")]
        public float workPerUnit = 10f;
        public string inputItem;
        public int inputCount;
        public int bufferCap = 30;

        [Header("Utility")]
        public int storageSlots;
        public CraftStation station = CraftStation.Hand;
        [Tooltip("Base-wide production bonus while staffed (Dynamo).")]
        public float boostPercent;
        public bool feeds;
        public int beds;

        [Header("Look")]
        public Palette palette;
        public BodyPart[] parts = new BodyPart[0];
    }
}
