using System;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Level-up skills. Binding/Foraging/Command/Packmule are retired (refunded on load; Packmule ranks became bag slots).</summary>
    public enum PlayerSkill { Vitality, Endurance, Binding, Foraging, Command, Packmule, Toughness, Warding, Craftsmanship }

    /// <summary>Per-species Kin Dex record.</summary>
    [Serializable]
    public class DexEntry
    {
        public string id;
        public int seen, defeated, caught;
        /// <summary>Best specialty score among individuals caught (-1 none) and its path.</summary>
        public int bestScore = -1;
        public int bestPath;
        /// <summary>Where it has been spotted (world XZ), newest last.</summary>
        public System.Collections.Generic.List<Vector2> sightings = new System.Collections.Generic.List<Vector2>();
    }

    /// <summary>Player progression + survival stats + bag. Serializable (lives inside the save file).</summary>
    [Serializable]
    public class PlayerStats
    {
        public const int MaxSkillRank = 10;
        /// <summary>What the explorer looks like (character creator; changeable from the Gear tab).</summary>
        public Appearance look = new Appearance();
        /// <summary>Kin designed in the kin maker: the designs are saved, the species are rebuilt on load.</summary>
        public System.Collections.Generic.List<CustomKin> customKin = new System.Collections.Generic.List<CustomKin>();
        public const int SkillCount = 9;
        /// <summary>Skills offered on level-up (in display order).</summary>
        public static readonly PlayerSkill[] ActiveSkills = { PlayerSkill.Vitality, PlayerSkill.Endurance, PlayerSkill.Toughness, PlayerSkill.Warding, PlayerSkill.Craftsmanship };

        public int level = 1;
        public int xp;
        public int skillPoints;
        public int[] skills = new int[SkillCount];
        public int bonusSlots;

        public float hp = -1f;
        public float stamina = -1f;
        public float hunger = 100f;

        public const int HotbarSize = 4;

        [Tooltip("Legacy (pre-hotbar saves). Migrated into 'held' on load.")]
        public string weapon = "";
        /// <summary>Item currently in hand (weapon or tool), "" = bare hands.</summary>
        public string held = "";
        public string[] hotbar = new string[HotbarSize];
        /// <summary>Gathering swaps to the best valid tool automatically, then back to what you held.</summary>
        public bool autoTool = true;
        public bool autoFeed = true, autoTreat = true;   // Provision Pouch switches
        /// <summary>Off (the default): the companion works in camp while you are there. On: it just follows you (Camp > Workers).</summary>
        public bool companionRests;
        public int companionStance = (int)CompanionStance.Defensive;
        public bool starterChosen;
        public System.Collections.Generic.List<string> waystonesFound = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> waystonesActive = new System.Collections.Generic.List<string>();
        public string selectedSeed = "tether_seed";
        public Inventory Bag = new Inventory(30);
        public System.Collections.Generic.List<string> registeredSpecies = new System.Collections.Generic.List<string>();
        /// <summary>Equipped armor/accessories by GearSlots index (equipped items leave the bag). No durability.</summary>
        public string[] gear = new string[GearSlots.Count];
        /// <summary>Alpha species that already dropped their trophy accessory (first defeat is guaranteed).</summary>
        public System.Collections.Generic.List<string> alphaTrophies = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<DexEntry> dex = new System.Collections.Generic.List<DexEntry>();
        /// <summary>Hidden relics picked up (ids "type_index").</summary>
        public System.Collections.Generic.List<string> relicsFound = new System.Collections.Generic.List<string>();
        public bool skillsMigrated;
        public System.Collections.Generic.List<string> cavesFound = new System.Collections.Generic.List<string>();
        /// <summary>Named sub-regions walked into (SubRegions ids).</summary>
        public System.Collections.Generic.List<string> regionsFound = new System.Collections.Generic.List<string>();
        /// <summary>Points of interest (Pois ids): come across / chest opened / tablet read; lookouts climbed; relics a lookout showed you.</summary>
        public System.Collections.Generic.List<string> poisFound = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> poisLooted = new System.Collections.Generic.List<string>();
        // treasure chests (World/Chests): squares whose chest has been opened, blueprints learned, and a running count
        public System.Collections.Generic.List<ChestState> chests = new System.Collections.Generic.List<ChestState>();
        public System.Collections.Generic.List<string> blueprints = new System.Collections.Generic.List<string>();
        public int chestsOpened;
        public System.Collections.Generic.List<string> poisRead = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> lookouts = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> relicHints = new System.Collections.Generic.List<string>();
        /// <summary>Region-only items picked up at least once, and Explorer's log milestones claimed ("biome:step").</summary>
        public System.Collections.Generic.List<string> regionalFound = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> explorerClaimed = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<CaveRecord> caveRecords = new System.Collections.Generic.List<CaveRecord>();
        /// <summary>Currency from bounties, expeditions and selling to the trader.</summary>
        public int glimmer;
        /// <summary>Kinkeeper offers already bought this restock ("settlement|bucket|slot").</summary>
        public System.Collections.Generic.List<string> kinShopSold = new System.Collections.Generic.List<string>();
        /// <summary>Settlement request boards, standing per settlement, and rival tamers beaten.</summary>
        public System.Collections.Generic.List<SettleRequest> requests = new System.Collections.Generic.List<SettleRequest>();
        public System.Collections.Generic.List<StandingEntry> standing = new System.Collections.Generic.List<StandingEntry>();
        public System.Collections.Generic.List<TamerRecord> tamerWins = new System.Collections.Generic.List<TamerRecord>();
        public System.Collections.Generic.List<WardenRecord> wardenWins = new System.Collections.Generic.List<WardenRecord>();
        // research (Core/Tech.cs)
        public System.Collections.Generic.List<string> techDone = new System.Collections.Generic.List<string>();
        public string researchId;
        public float researchProgress;
        public System.Collections.Generic.List<OutpostSave> outposts = new System.Collections.Generic.List<OutpostSave>();
        // legend hunts (World/LegendHunts.cs): which hunt, and how far along (0 = not accepted, 1-3 = that clue, 4 = lair)
        public int huntIndex, huntStep;
        public long huntRetryUnix;
        /// <summary>Dex milestone indices already claimed (Core/DexRewards.cs).</summary>
        public System.Collections.Generic.List<int> dexClaimed = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<Bounty> bounties = new System.Collections.Generic.List<Bounty>();
        public double bountyRerollAt;
        public System.Collections.Generic.List<Expedition> expeditions = new System.Collections.Generic.List<Expedition>();
        public System.Collections.Generic.List<KinEgg> eggs = new System.Collections.Generic.List<KinEgg>();
        // quest log (Core/Quests.cs): ids finished, event counters, which line the HUD follows, and whether an old save has been caught up
        public System.Collections.Generic.List<string> questsDone = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<QuestCount> questCounts = new System.Collections.Generic.List<QuestCount>();
        public int questTrack;
        public bool questsStarted;

        [NonSerialized] System.Collections.Generic.Dictionary<string, DexEntry> dexIndex;
        [NonSerialized] int[] relicCounts;

        public DexEntry DexFor(string speciesId, bool create)
        {
            if (dexIndex == null)
            {
                dexIndex = new System.Collections.Generic.Dictionary<string, DexEntry>();
                foreach (var e in dex) if (e != null && !string.IsNullOrEmpty(e.id)) dexIndex[e.id] = e;
            }
            if (dexIndex.TryGetValue(speciesId, out var x)) return x;
            if (!create) return null;
            x = new DexEntry { id = speciesId };
            dex.Add(x);
            dexIndex[speciesId] = x;
            return x;
        }

        public int RelicCount(RelicType t)
        {
            if (relicCounts == null)
            {
                relicCounts = new int[(int)RelicType.Count];
                foreach (var id in relicsFound)
                {
                    int u = id.IndexOf('_');
                    if (u > 0 && int.TryParse(id.Substring(0, u), out int ti) && ti >= 0 && ti < relicCounts.Length) relicCounts[ti]++;
                }
            }
            return relicCounts[(int)t];
        }

        public void AddRelic(string id) { if (!relicsFound.Contains(id)) relicsFound.Add(id); relicCounts = null; }

        // relic bonuses (see Relics.cs)
        public float SwimMul => 1f + 0.06f * RelicCount(RelicType.Tidepearl);
        public float SwimStaminaMul => Mathf.Max(0.3f, 1f - 0.08f * RelicCount(RelicType.Tidepearl));
        public float ClimbBonus => 0.12f * RelicCount(RelicType.CragClaw);
        public float KinStaminaMul => Mathf.Max(0.4f, 1f - 0.06f * RelicCount(RelicType.Kinspring));
        public float DamageTakenMul => Mathf.Max(0.5f, 1f - 0.03f * RelicCount(RelicType.Wardstone));
        public float KinXpMul => (1f + 0.04f * RelicCount(RelicType.Kinheart)) * (HasEffect(UniqueEffect.Mentor) ? 1.15f : 1f);
        public float ShimmerMul => (1f + 0.08f * RelicCount(RelicType.Glintstone)) * (HasEffect(UniqueEffect.Shimmerlure) ? 1.5f : 1f);
        /// <summary>A Sporecap Draught is working: no Poison.</summary>
        public bool PoisonWarded => Gear[StatKind.PoisonWard] > 0f;
        public float KinHungerMul => Mathf.Max(0.5f, 1f - 0.04f * RelicCount(RelicType.Hearthmoss)) * (HasEffect(UniqueEffect.FenLantern) ? 0.8f : 1f);
        public float CraftSpeedMul => 1f + Rank(PlayerSkill.Craftsmanship) * 0.08f;

        // ---- meal buff: one at a time, counted in world time ----
        public string mealItem;
        public double mealUntil;
        /// <summary>Slept in a bed (GameManager.Sleep): +10% explorer XP until this world time.</summary>
        public double restedUntil;
        public bool Rested => GameManager.WorldTime < restedUntil;
        public bool MealActive => !string.IsNullOrEmpty(mealItem) && GameManager.WorldTime < mealUntil;
        public float MealSecondsLeft => MealActive ? (float)(mealUntil - GameManager.WorldTime) : 0f;
        public void SetMeal(ItemDef d) { mealItem = d.id; mealUntil = GameManager.WorldTime + d.mealMinutes * 60.0; GearChanged(); }
        public void ClearMeal() { mealItem = null; GearChanged(); }
        void ApplyMeal()
        {
            if (!MealActive) return;
            var d = ContentDB.Item(mealItem);
            if (d != null) gearCache.Add(d.mealMods);
        }

        [NonSerialized] GearStats gearCache;
        [NonSerialized] public int GearVersion;
        public GearStats Gear
        {
            get
            {
                if (gearCache == null) { gearCache = new GearStats(); gearCache.Recompute(gear); ApplyMeal(); }
                return gearCache;
            }
        }
        public bool HasEffect(UniqueEffect e) => Gear.Has(e);

        public int Rank(PlayerSkill s) => skills != null && (int)s < skills.Length ? skills[(int)s] : 0;

        public float MaxHp => 100f + Rank(PlayerSkill.Vitality) * 15f + (level - 1) * 4f + Gear[StatKind.MaxHp];
        public float MaxStamina => 100f + Rank(PlayerSkill.Endurance) * 12f + Gear[StatKind.MaxStamina];
        public float CaptureMult => (1f + 0.03f * RelicCount(RelicType.Tetherbloom)) * (1f + Gear[StatKind.CaptureChance]);
        public float GatherBonus => Gear[StatKind.GatherYield];
        public float WorkBonus => 1f;
        public int BagSize => 30 + bonusSlots;
        public float BaseAttack => 10f + (level - 1) * 1.6f;
        public float BaseDefense => 8f + (level - 1) * 1.5f;
        public float Attack => (BaseAttack + Gear[StatKind.Attack]) * (HasEffect(UniqueEffect.Kinbond) ? 0.9f : 1f)
                             * (HasEffect(UniqueEffect.LastEmber) && hp >= 0f && hp < MaxHp * 0.5f ? 1.25f : 1f);
        public float Defense => BaseDefense + Rank(PlayerSkill.Toughness) * 3f + Gear[StatKind.Defense];
        public float CritChance => 0.08f + Gear[StatKind.Crit];
        public float MoveMul => Mathf.Max(0.5f, 1f + Gear[StatKind.MoveSpeed] + 0.02f * RelicCount(RelicType.Windfeather));
        public float RegenMul => Mathf.Max(0.2f, 1f + Gear[StatKind.StaminaRegen]);
        public float HungerMul => Mathf.Clamp(1f - Gear[StatKind.HungerReduction], 0.3f, 2f);
        public float ColdResist => HasEffect(UniqueEffect.Frostward) || HasEffect(UniqueEffect.Rimecloak) ? 1f : Mathf.Clamp01(Gear[StatKind.ColdResist]);
        public float HeatResist => HasEffect(UniqueEffect.Emberward) ? 1f : Mathf.Clamp01(Gear[StatKind.HeatResist]);
        public float Resist(Element e) => Mathf.Min(0.8f, Gear.Resist(e) + Rank(PlayerSkill.Warding) * 0.03f);

        public static int XpToNext(int level) => 40 + level * level * 12;

        public static string SkillName(PlayerSkill s)
        {
            switch (s)
            {
                case PlayerSkill.Vitality: return "Health";
                case PlayerSkill.Endurance: return "Stamina";
                case PlayerSkill.Toughness: return "Defense";
                case PlayerSkill.Warding: return "Element Resist";
                case PlayerSkill.Craftsmanship: return "Craft Speed";
                default: return s.ToString();
            }
        }

        public static string SkillDesc(PlayerSkill s)
        {
            switch (s)
            {
                case PlayerSkill.Vitality: return "+15 max HP per rank.";
                case PlayerSkill.Endurance: return "+12 max stamina per rank.";
                case PlayerSkill.Toughness: return "+3 DEF per rank.";
                case PlayerSkill.Warding: return "+3% resistance to every element per rank.";
                case PlayerSkill.Craftsmanship: return "+8% crafting speed at stations (your help and your kin crafters) per rank.";
                default: return "Retired skill.";
            }
        }

        public void EnsureValid()
        {
            if (skills == null || skills.Length != SkillCount) { var n = new int[SkillCount]; if (skills != null) Array.Copy(skills, n, Mathf.Min(SkillCount, skills.Length)); skills = n; }
            if (!skillsMigrated)
            {
                // old skill tree -> refund retired skills; Packmule ranks become permanent bag slots
                skillsMigrated = true;
                bonusSlots += skills[(int)PlayerSkill.Packmule] * 4;
                foreach (var old in new[] { PlayerSkill.Binding, PlayerSkill.Foraging, PlayerSkill.Command, PlayerSkill.Packmule })
                {
                    skillPoints += skills[(int)old];
                    skills[(int)old] = 0;
                }
            }
            if (dex == null) dex = new System.Collections.Generic.List<DexEntry>();
            if (cavesFound == null) cavesFound = new System.Collections.Generic.List<string>();
            if (regionsFound == null) regionsFound = new System.Collections.Generic.List<string>();
            if (poisFound == null) poisFound = new System.Collections.Generic.List<string>();
            if (poisLooted == null) poisLooted = new System.Collections.Generic.List<string>();
            if (chests == null) chests = new System.Collections.Generic.List<ChestState>();
            if (blueprints == null) blueprints = new System.Collections.Generic.List<string>();
            if (poisRead == null) poisRead = new System.Collections.Generic.List<string>();
            if (lookouts == null) lookouts = new System.Collections.Generic.List<string>();
            if (relicHints == null) relicHints = new System.Collections.Generic.List<string>();
            if (regionalFound == null) regionalFound = new System.Collections.Generic.List<string>();
            if (explorerClaimed == null) explorerClaimed = new System.Collections.Generic.List<string>();
            if (caveRecords == null) caveRecords = new System.Collections.Generic.List<CaveRecord>();
            if (bounties == null) bounties = new System.Collections.Generic.List<Bounty>();
            if (expeditions == null) expeditions = new System.Collections.Generic.List<Expedition>();
            if (eggs == null) eggs = new System.Collections.Generic.List<KinEgg>();
            if (kinShopSold == null) kinShopSold = new System.Collections.Generic.List<string>();
            if (requests == null) requests = new System.Collections.Generic.List<SettleRequest>();
            if (standing == null) standing = new System.Collections.Generic.List<StandingEntry>();
            if (tamerWins == null) tamerWins = new System.Collections.Generic.List<TamerRecord>();
            if (wardenWins == null) wardenWins = new System.Collections.Generic.List<WardenRecord>();
            if (techDone == null) techDone = new System.Collections.Generic.List<string>();
            if (outposts == null) outposts = new System.Collections.Generic.List<OutpostSave>();
            if (dexClaimed == null) dexClaimed = new System.Collections.Generic.List<int>();
            if (relicsFound == null) relicsFound = new System.Collections.Generic.List<string>();
            dexIndex = null; relicCounts = null;
            if (Bag == null) Bag = new Inventory(30);
            Bag.Resize(BagSize);
            if (hp < 0f) hp = MaxHp;
            if (stamina < 0f) stamina = MaxStamina;
            if (registeredSpecies == null) registeredSpecies = new System.Collections.Generic.List<string>();
            if (hotbar == null || hotbar.Length != HotbarSize) { var h = new string[HotbarSize]; if (hotbar != null) Array.Copy(hotbar, h, Mathf.Min(HotbarSize, hotbar.Length)); hotbar = h; }
            if (held == null) held = "";
            if (waystonesFound == null) waystonesFound = new System.Collections.Generic.List<string>();
            if (waystonesActive == null) waystonesActive = new System.Collections.Generic.List<string>();
            if (gear == null || gear.Length != GearSlots.Count) { var g = new string[GearSlots.Count]; if (gear != null) Array.Copy(gear, g, Mathf.Min(GearSlots.Count, gear.Length)); gear = g; }
            for (int i = 0; i < gear.Length; i++) if (!string.IsNullOrEmpty(gear[i]) && ContentDB.Item(gear[i]) == null) gear[i] = null;
            if (alphaTrophies == null) alphaTrophies = new System.Collections.Generic.List<string>();
            GearChanged();
            if (!string.IsNullOrEmpty(weapon) && string.IsNullOrEmpty(held)) held = weapon;   // old save migration
            weapon = "";
            ValidateEquipment();
            foreach (var s in Bag.slots) if (!s.Empty) AutoHotbar(s.id);
        }

        // ------------------------------------------------------------------ GEAR (armor / accessories)
        public static bool IsGear(ItemDef d) => d != null && d.category == ItemCategory.Gear && d.equipSlot != EquipSlot.None;

        public void GearChanged()
        {
            if (gearCache == null) gearCache = new GearStats();
            gearCache.Recompute(gear);
            ApplyMeal();
            GearVersion++;
            hp = Mathf.Min(hp, MaxHp);
            stamina = Mathf.Min(stamina, MaxStamina);
        }

        /// <summary>First slot index this item can go in (empty ring slot preferred), or -1.</summary>
        public int SlotFor(ItemDef d)
        {
            if (!IsGear(d)) return -1;
            int first = -1;
            for (int i = 0; i < GearSlots.Count; i++)
            {
                if (GearSlots.Kind[i] != d.equipSlot) continue;
                if (string.IsNullOrEmpty(gear[i])) return i;
                if (first < 0) first = i;
            }
            return first;
        }

        /// <summary>Equip from the bag into a slot (swapping the old piece back into the bag).</summary>
        public bool Equip(string id, int slot = -1)
        {
            var d = ContentDB.Item(id);
            if (!IsGear(d) || Bag.Count(id) <= 0) return false;
            if (slot < 0) slot = SlotFor(d);
            if (slot < 0 || slot >= GearSlots.Count || GearSlots.Kind[slot] != d.equipSlot) return false;
            Bag.Remove(id, 1);
            string old = gear[slot];
            gear[slot] = id;
            if (!string.IsNullOrEmpty(old)) Bag.Add(old, 1);
            GearChanged();
            return true;
        }

        public bool Unequip(int slot)
        {
            if (slot < 0 || slot >= GearSlots.Count || string.IsNullOrEmpty(gear[slot])) return false;
            if (Bag.SpaceFor(gear[slot]) < 1) return false;
            Bag.Add(gear[slot], 1);
            gear[slot] = null;
            GearChanged();
            return true;
        }

        public bool Owns(string id)
        {
            if (Bag.Count(id) > 0) return true;
            foreach (var g in gear) if (g == id) return true;
            return false;
        }

        // ------------------------------------------------------------------ EQUIPMENT
        public static bool IsEquippable(ItemDef d) => d != null && (d.category == ItemCategory.Tool || d.category == ItemCategory.Weapon);

        public ItemDef HeldDef => string.IsNullOrEmpty(held) ? null : ContentDB.Item(held);

        /// <summary>Drops references to items no longer in the bag.</summary>
        public void ValidateEquipment()
        {
            if (!string.IsNullOrEmpty(held) && Bag.Count(held) <= 0) held = "";
            for (int i = 0; i < hotbar.Length; i++)
                if (!string.IsNullOrEmpty(hotbar[i]) && Bag.Count(hotbar[i]) <= 0) hotbar[i] = null;
        }

        /// <summary>Puts a newly acquired tool/weapon on a free hotbar slot. Returns true if added.</summary>
        public bool AutoHotbar(string id)
        {
            if (!IsEquippable(ContentDB.Item(id)) || HotbarIndex(id) >= 0) return false;
            for (int i = 0; i < hotbar.Length; i++)
                if (string.IsNullOrEmpty(hotbar[i])) { hotbar[i] = id; return true; }
            return false;
        }

        public int HotbarIndex(string id)
        {
            for (int i = 0; i < hotbar.Length; i++) if (hotbar[i] == id) return i;
            return -1;
        }

        public void SetHotbar(int slot, string id)
        {
            if (slot < 0 || slot >= hotbar.Length) return;
            int existing = HotbarIndex(id);
            if (existing >= 0) hotbar[existing] = null;
            hotbar[slot] = id;
        }

        /// <summary>Hold an item (or "" for hands). Returns false if it isn't equippable/owned.</summary>
        public bool Hold(string id)
        {
            if (string.IsNullOrEmpty(id)) { held = ""; return true; }
            if (!IsEquippable(ContentDB.Item(id)) || Bag.Count(id) <= 0) return false;
            held = id;
            return true;
        }

        /// <summary>Power of the held tool for this kind (1 = hands / wrong tool).</summary>
        public int HeldToolPower(ToolKind kind)
        {
            var d = HeldDef;
            return d != null && d.category == ItemCategory.Tool && d.toolKind == kind ? Mathf.Max(1, d.toolPower) : 1;
        }

        /// <summary>Best owned tool of this kind (power, id), or (1, null) for bare hands.</summary>
        public int BestToolPower(ToolKind kind, out string toolId)
        {
            toolId = null;
            int best = 1;
            foreach (var s in Bag.slots)
            {
                if (s.Empty) continue;
                var d = ContentDB.Item(s.id);
                if (d != null && d.category == ItemCategory.Tool && d.toolKind == kind && d.toolPower > best)
                {
                    best = d.toolPower;
                    toolId = d.id;
                }
            }
            return best;
        }

        /// <summary>Returns levels gained.</summary>
        public int AddXp(int amount)
        {
            int gained = 0;
            xp += amount;
            while (xp >= XpToNext(level) && level < 50)
            {
                xp -= XpToNext(level);
                level++;
                skillPoints++;
                gained++;
                hp = MaxHp;
            }
            return gained;
        }

        public bool TrySpend(PlayerSkill s)
        {
            if (skillPoints <= 0 || Rank(s) >= MaxSkillRank || Array.IndexOf(ActiveSkills, s) < 0) return false;
            skills[(int)s]++;
            skillPoints--;
            return true;
        }

        /// <summary>Melee damage of what's in hand: weapon damage, tools are light weapons, fists 6.</summary>
        public float WeaponDamage
        {
            get
            {
                var d = HeldDef;
                if (d == null || Bag.Count(d.id) <= 0) return 6f;
                if (d.category == ItemCategory.Weapon) return d.weaponDamage;
                return 6f + d.toolPower * 2f; // stone tools 10, ingot tools 12
            }
        }
    }
}
