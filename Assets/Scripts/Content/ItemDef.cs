using UnityEngine;

namespace Wildbond
{
    [CreateAssetMenu(menuName = "Wildbond/Item", fileName = "Item_")]
    public class ItemDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(1, 3)] public string description;
        public ItemCategory category;
        public int maxStack = 99;
        public Color iconColor = Color.white;
        public string iconGlyph = "?";

        [Header("Food / Consumable")]
        public float foodValue;
        public float healValue;
        public float kinHealValue;
        public int bonusInventorySlots;

        [Header("Meal buff (food): timed bonus, the newest meal replaces the last")]
        public StatMod[] mealMods = new StatMod[0];
        public float mealMinutes;

        [Header("In the hand")]
        [Tooltip("How it is swung (thrust, slash, chop...). Auto works it out from the item: tools by kind, weapons as swords unless listed in HandStyles.Of.")]
        public HandStyle handStyle;

        [Header("Tool")]
        public ToolKind toolKind;
        public int toolPower;

        [Header("Weapon")]
        public float weaponDamage;

        [Header("Capture Seed")]
        public float captureMult = 1f;
        [Tooltip("Legend seed: the species id it is made for (\"*\" = any legendary). It only binds that kin, and cannot fail once the kin is at 10% health or less.")]
        public string legendFor;
        [Tooltip("The legend caught with this seed comes out a shimmer. Returns to the bag if a throw fails.")]
        public bool shimmerSeed;

        [Header("Gear (armor / accessory)")]
        public EquipSlot equipSlot;
        public Rarity rarity;
        public StatMod[] mods = new StatMod[0];
        public UniqueEffect unique;
        [Tooltip("Visual style on the player: 0 cloth, 1 metal, 2 rime crystal, 3 ember, 4 thorn/vine")]
        public int gearStyle;
        public Color trimColor = Color.gray;
        [Tooltip("Dropped by alphas (not craftable).")]
        public bool alphaDrop;
    }
}
