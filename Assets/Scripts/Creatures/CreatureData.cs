using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Persistent state of one individual kin (wild spawn or owned). Serializable for saves.</summary>
    [Serializable]
    public class CreatureData
    {
        public string uid;
        public string speciesId;
        public string nickname;
        public int level = 1;
        public int xp;
        public float hp = -1f;           // -1 => full
        public float hunger = 100f;      // 0..100 (workers)
        public List<string> traits = new List<string>();
        public bool shimmer;
        /// <summary>Which of the species' three shimmer colours this kin wears (0 = its Radiant coat). See Shimmer.</summary>
        public int shimmerVariant;
        public bool alpha;
        public bool regional;
        public Mutation mutation;
        public string[] abilities = new string[0];
        public KinLocation location = KinLocation.Box;
        public string assignedBuilding;  // building uid when working at base
        public long capturedAt;          // unix seconds
        /// <summary>Individual quality 0..100 per KinPath (Combat, Work, Travel); spec = specialty path. Rolled once.</summary>
        public int[] apt;
        public int spec = -1;
        /// <summary>Evolution stage: 0 base, 1 Ascended (Lv 20), 2 Exalted (Lv 35). Bigger, glowing, stronger.</summary>
        public int stage;
        public float StageMul => 1f + 0.15f * stage;
        public static string StageName(int st) => st == 2 ? "Exalted" : st == 1 ? "Ascended" : "";

        [NonSerialized] SpeciesDef speciesCache;
        [NonSerialized] public float workXpTimer;
        public SpeciesDef Species => speciesCache != null ? speciesCache : (speciesCache = ContentDB.Sp(speciesId));

        public string DisplayName => string.IsNullOrEmpty(nickname) ? RegionalName : nickname;

        public string RegionalName
        {
            get
            {
                var s = Species;
                if (s == null) return speciesId;
                string n = s.NameAt(stage);   // a real form's own name, else "Ascended X"
                return regional && s.regional.enabled ? $"{n} ({s.regional.formName})" : n;
            }
        }

        public Element Element1 => Species != null ? Species.element1 : Element.Normal;
        public Element Element2
        {
            get
            {
                if (Species == null) return Element.Normal;
                var ex = stage >= 2 ? Species.FormAt(2) : null;
                if (ex != null && ex.element2 != Element.Normal && ex.element2 != Species.element1) return ex.element2;   // an Exalted form's new element
                return regional && Species.regional.enabled ? Species.regional.element2 : Species.element2;
            }
        }

        /// <summary>This kin's real evolved form, if its kind has one at its stage.</summary>
        public EvoForm Form => Species != null ? Species.FormAt(stage) : null;
        /// <summary>The body it is built from: its form's parts, or the kind's.</summary>
        public BodyPart[] Parts => Species.PartsAt(stage);
        public GroupPivot[] Pivots => Species.PivotsAt(stage);

        public Palette Palette
        {
            get
            {
                var s = Species;
                var p = shimmer ? Shimmer.PaletteFor(s, shimmerVariant) : (regional && s.regional.enabled ? s.regional.palette : s.palette);
                var form = !shimmer && !(regional && s.regional.enabled) ? Form : null;
                if (form != null && form.ownPalette) p = form.palette;   // a real evolved form's own colours
                if (mutation == Mutation.Glowing)
                {
                    p.primary = Color.Lerp(p.primary, p.glow, 0.25f);
                    p.accent = p.glow;
                }
                if (stage > 0 && Form == null)   // a real evolved form has its own design: no wash
                {
                    // evolved forms glow with their element
                    p.primary = Color.Lerp(p.primary, p.glow, 0.1f * stage);
                    p.accent = Color.Lerp(p.accent, p.glow, 0.45f * stage);
                    p.secondary = Color.Lerp(p.secondary, Color.Lerp(p.glow, Color.white, 0.3f), 0.15f * stage);
                }
                return p;
            }
        }

        /// <summary>This kin's own size, build, coat tint and marking (see KinLook). Null in old saves: read it through <see cref="Look"/>.</summary>
        public KinLook look;
        public KinLook Look
        {
            get { if (look == null || !look.rolled) look = KinLook.FromUid(uid); return look; }
        }

        /// <summary>Up at night: a night species, or one with the Night Owl passive. Every other camp kin sleeps from dusk to dawn.</summary>
        public bool Nocturnal => (Species != null && Species.spawnTime == SpawnTime.Night) || (traits != null && traits.Contains(NightOwl));
        public const string NightOwl = "night_owl";

        public float SizeScale => (alpha ? 1.55f : 1f) * (mutation == Mutation.Giant ? 1.3f : 1f) * (1f + 0.12f * stage);

        public string ModelKey => $"{speciesId}|{(shimmer ? 1 + shimmerVariant : 0)}{(regional ? 1 : 0)}{(alpha ? 1 : 0)}{(int)mutation}{stage}";

        // ---------------------------------------------------------------- STATS
        float TraitMul(Func<TraitDef, float> f)
        {
            float m = 1f;
            for (int i = 0; i < traits.Count; i++)
            {
                var t = ContentDB.Trait(traits[i]);
                if (t != null) m *= f(t);
            }
            return m;
        }

        public void EnsureAptitude() { if (apt == null || apt.Length != 3 || spec < 0 || spec > 2) Aptitude.Roll(this); }
        public int Apt(KinPath p) { EnsureAptitude(); return apt[(int)p]; }
        /// <summary>Combat aptitude: x0.8 (score 0) .. x1.0 (50) .. x1.2 (100) on HP / ATK / DEF.</summary>
        public float CombatAptMul => 0.8f + 0.4f * Apt(KinPath.Combat) / 100f;
        /// <summary>Work aptitude: x0.75 .. x1.25 work speed.</summary>
        public float WorkAptMul => 0.75f + 0.5f * Apt(KinPath.Work) / 100f;
        /// <summary>Travel aptitude: x0.9 .. x1.1 move/mount speed.</summary>
        public float TravelAptMul => 0.9f + 0.2f * Apt(KinPath.Travel) / 100f;
        /// <summary>Rider stamina use while mounted: x1.3 (0) .. x0.7 (100).</summary>
        public float MountStaminaMul => 1.3f - 0.6f * Apt(KinPath.Travel) / 100f;

        public float MaxHp => Species.baseHp * (1f + 0.12f * (level - 1)) * TraitMul(t => t.hpMul) * (alpha ? 1.35f : 1f) * CombatAptMul * StageMul;
        public float Atk => Species.baseAtk * (1f + 0.09f * (level - 1)) * TraitMul(t => t.atkMul) * CombatAptMul * StageMul;
        public float Def => Species.baseDef * (1f + 0.09f * (level - 1)) * TraitMul(t => t.defMul) * CombatAptMul * StageMul;
        public float SpeedMul => TraitMul(t => t.speedMul) * TravelAptMul;
        public float WorkMul => TraitMul(t => t.workMul) * WorkAptMul * (1f + 0.1f * stage);
        public float HungerMul => TraitMul(t => t.hungerMul);

        public float ElementBoost(Element e)
        {
            float m = 1f;
            for (int i = 0; i < traits.Count; i++)
            {
                var t = ContentDB.Trait(traits[i]);
                if (t != null && t.elementBoost == e && e != Element.Normal) m *= t.elementBoostMul;
            }
            return m;
        }

        public float CurrentHp => hp < 0f ? MaxHp : hp;
        public bool Fainted => hp == 0f;

        /// <summary>Jobs this kin has been told not to do (a bit per WorkType). Set on the Kin tab.</summary>
        public int jobsOff;
        public bool JobOn(WorkType w) => (jobsOff & (1 << (int)w)) == 0;
        public void SetJob(WorkType w, bool on) { if (on) jobsOff &= ~(1 << (int)w); else jobsOff |= 1 << (int)w; }

        /// <summary>What it is capable of at a job, whether or not that job is switched on: the species' level, +1 for shimmer kin in every job the species can do at all.</summary>
        public int JobLevel(WorkType w)
        {
            int lvl = Species != null ? Species.WorkLevel(w) : 0;
            return lvl > 0 && shimmer ? lvl + Shimmer.WorkBonus : lvl;
        }

        /// <summary>Camp job level it will actually work at: its JobLevel, or 0 for a job switched off.</summary>
        public int WorkLevel(WorkType w) => JobOn(w) ? JobLevel(w) : 0;

        public static int XpToNext(int level) => 18 + level * level * 5;

        // ---------------------------------------------------------------- BOND
        /// <summary>Bond points: summoned time, fights together, petting, feeding, expeditions.</summary>
        public float bond;
        public static readonly float[] BondThresholds = { 0f, 20f, 60f, 120f, 200f, 300f };
        public const int MaxBond = 5;
        [NonSerialized] public float lastPet = -999f, lastFeed = -999f, lastStand = -999f;
        /// <summary>Camp life (BaseManager.Upgrades): what this camp kin wants from you right now, until when, and how long it stays cheered up.</summary>
        [NonSerialized] public CampWant campWant;
        [NonSerialized] public float campWantUntil, cheerUntil = -999f;

        public int BondLevel
        {
            get { int l = 0; for (int i = 1; i < BondThresholds.Length; i++) if (bond >= BondThresholds[i]) l = i; return l; }
        }

        /// <summary>Companion ATK/DEF bonus: +3% per bond level.</summary>
        public float BondMul => 1f + 0.03f * BondLevel;

        public static string BondPerks(int lv) =>
            lv >= 5 ? "+15% ATK/DEF · skills recharge 10% faster · hangs on at 1 HP once every 10 min" :
            lv >= 3 ? $"+{lv * 3}% ATK/DEF · skills recharge 10% faster (Lv 5: hangs on at 1 HP)" :
            lv >= 1 ? $"+{lv * 3}% ATK/DEF (Lv 3: faster skills)" : "Summon, fight beside, pet and feed to bond";

        // ---------------------------------------------------------------- SKILLS (learned by level)
        public const int MaxSkills = 4;
        /// <summary>Level a skill slot unlocks at: 1, 5, 10, 15.</summary>
        public static int SlotUnlockLevel(int slot) => slot == 0 ? 1 : slot * 5;
        /// <summary>Known skills: 1 at Lv1-4, then a new one every 5 levels, up to 4.</summary>
        public int SkillSlots => Mathf.Clamp(1 + level / 5, 1, MaxSkills);

        [NonSerialized] string[] learnsetCache;
        /// <summary>After evolving (a new element or form skill changes what it learns).</summary>
        public void ResetLearnset() => learnsetCache = null;

        /// <summary>
        /// The 4 skills this individual learns, in unlock order: its own moves first (incl. a rolled rare combo),
        /// then the species' moves, then the strongest moves of its elements, then Tumble Tackle.
        /// </summary>
        public string[] Learnset
        {
            get
            {
                if (learnsetCache != null) return learnsetCache;
                var l = new List<string>(MaxSkills);
                void Add(string id) { if (l.Count < MaxSkills && !string.IsNullOrEmpty(id) && !l.Contains(id) && ContentDB.Ability(id) != null) l.Add(id); }
                if (abilities != null) foreach (var a in abilities) Add(a);
                if (Species != null) foreach (var a in Species.abilities) Add(a);
                if (l.Count < MaxSkills)
                {
                    var pool = new List<AbilityDef>(ContentDB.Abilities.Values);
                    pool.RemoveAll(x => x.effect != AbilityEffect.None || x.kind == AbilityKind.Blink || ContentDB.IsFormSkill(x.id));   // signature and Exalted-form moves stay with their own kin
                    pool.Sort((x, y) => x.power != y.power ? y.power.CompareTo(x.power) : string.CompareOrdinal(x.id, y.id));
                    foreach (var e in new[] { Element1, Element2 })
                    {
                        if (e == Element.Normal) continue;
                        foreach (var a in pool) if (a.element == e) Add(a.id);
                    }
                    Add("tackle");
                    foreach (var a in pool) if (a.element == Element.Normal) Add(a.id);
                }
                // an Exalted form's own skill takes the last slot
                var exf = stage >= 2 && Species != null ? Species.FormAt(2) : null;
                if (exf != null && !string.IsNullOrEmpty(exf.skill) && ContentDB.Ability(exf.skill) != null && !l.Contains(exf.skill))
                {
                    if (l.Count >= MaxSkills) l[MaxSkills - 1] = exf.skill; else l.Add(exf.skill);
                }
                learnsetCache = l.ToArray();
                return learnsetCache;
            }
        }

        /// <summary>Skills usable right now (first SkillSlots of the learnset).</summary>
        public string[] KnownSkills
        {
            get
            {
                var ls = Learnset;
                int n = Mathf.Min(SkillSlots, ls.Length);
                var r = new string[n];
                Array.Copy(ls, r, n);
                return r;
            }
        }

        /// <summary>Adds XP, returns number of levels gained.</summary>
        public int AddXp(int amount)
        {
            int gained = 0;
            xp += amount;
            while (xp >= XpToNext(level) && level < 60)
            {
                xp -= XpToNext(level);
                float hpFrac = CurrentHp / MaxHp;
                level++;
                gained++;
                if (hp >= 0f) hp = MaxHp * hpFrac;
            }
            return gained;
        }

        public string VariantTags()
        {
            var parts = new List<string>(4);
            if (alpha) parts.Add("ALPHA");
            if (shimmer) parts.Add("SHIMMER (" + Shimmer.NameFor(Species, shimmerVariant).ToUpperInvariant() + ")");
            if (regional && Species.regional.enabled) parts.Add(Species.regional.formName.ToUpperInvariant() + " FORM");
            if (mutation != Mutation.None) parts.Add("MUTATED: " + mutation.ToString().ToUpperInvariant());
            return string.Join(" · ", parts);
        }

        public static string NewUid() => Guid.NewGuid().ToString("N").Substring(0, 12);
    }
}
