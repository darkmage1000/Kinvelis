using UnityEngine;

namespace Wildbond
{
    [CreateAssetMenu(menuName = "Wildbond/Species", fileName = "Species_")]
    public partial class SpeciesDef : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public int dexNumber;
        public string displayName;
        [TextArea(2, 4)] public string lore;
        public string personality;
        public Element element1 = Element.Normal;
        public Element element2 = Element.Normal;
        public Rarity rarity;
        [Tooltip("Offered at new-game partner selection; never spawns wild.")]
        public bool starter;
        [Tooltip("Only ever met as a boss: can't be caught, so it doesn't count toward Dex completion.")]
        public bool bossOnly;
        [Tooltip("Lives only underground: no land of its own, met only in caves (Caves.CaveKin).")]
        public bool underground;
        /// <summary>Made by the player in the kin maker (ContentSeed.BuildCustom): never wild, never sold, not counted by the Dex.</summary>
        [System.NonSerialized] public bool custom;
        public string role;

        [Header("Habitat")]
        public BiomeMask biomes = BiomeMask.Meadow;
        public SpawnTime spawnTime = SpawnTime.Any;
        public bool nearWater;
        [Tooltip("Only spawns in this weather / at this time of day (and is 4x more likely while it lasts).")]
        public SpawnCondition spawnCondition;
        public float spawnWeight = 1f;
        public int minGroup = 1;
        public int maxGroup = 2;
        public Temperament temperament = Temperament.Docile;

        [Header("Stats (level 1)")]
        public int baseHp = 60;
        public int baseAtk = 10;
        public int baseDef = 10;
        public float walkSpeed = 2.5f;
        public float runSpeed = 6f;
        public float size = 1f;
        public Locomotion locomotion = Locomotion.Quadruped;
        [Range(0.2f, 2f)] public float captureMod = 1f;

        [Header("Abilities")]
        public string[] abilities = new string[0];
        [Tooltip("Uncommon ability combos: a wild spawn has a small chance to know one of these instead of its 2nd ability.")]
        public string[] rareAbilities = new string[0];

        [Header("Base Work")]
        public WorkSkill[] work = new WorkSkill[0];
        public CompanionPerk perk;
        [TextArea(1, 3)] public string overworldRole;

        [Header("Mount")]
        [Tooltip("Can be ridden while it's your active companion (needs a Kin Harness).")]
        public bool mountable;
        [Tooltip("Top riding speed (m/s) while sprinting.")]
        public float mountSpeed = 11f;
        public float mountJump = 8f;
        [Tooltip("Rider seat position in model space (before size scaling).")]
        public Vector3 mountSeat = new Vector3(0, 1f, 0);

        [Header("Drops")]
        public DropEntry[] drops = new DropEntry[0];

        [Header("Look")]
        public Palette palette;
        public Palette shimmerPalette;
        [Tooltip("Optional hand-made 2nd / 3rd shimmer colours. Left empty, Shimmer picks two themed coats for the species.")]
        public Palette shimmerPalette2;
        public Palette shimmerPalette3;
        public BodyPart[] parts = new BodyPart[0];
        public GroupPivot[] pivots = new GroupPivot[0];
        [Tooltip("Real evolved forms: [0] Ascended (Lv 20), [1] Exalted (Lv 35). Empty = perfect form (it only grows and glows). See EvoForm.")]
        public EvoForm[] forms = new EvoForm[0];
        public RegionalForm regional;
        [Tooltip("Motion of the Extra part group (orbiting bits, propeller, floating puffs).")]
        public ExtraAnim extraAnim;
        [Tooltip("Ambient particles around the kin (embers, sparks, snow...). Uses the palette glow color.")]
        public AuraFx aura;

        public int WorkLevel(WorkType t)
        {
            for (int i = 0; i < work.Length; i++) if (work[i].type == t) return work[i].level;
            return 0;
        }

        public bool LivesIn(BiomeId b) => (biomes & (BiomeMask)(1 << (int)b)) != 0;
    }
}
