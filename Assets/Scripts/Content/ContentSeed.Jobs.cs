using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp job balance (2026-10-08). Before this pass the Sunmeadow had 21 haulers and 22 foragers but one kin that
    /// could Kindle (so no Campfire or Smelter crew) and one common logger: a camp could not be staffed until you
    /// reached the Badlands. The rule now:
    ///   - every job can be done at level 1-2 by several Common / Uncommon kin of the Sunmeadow;
    ///   - level 3-4 in a job belongs to the land whose element owns it (Kindling: Badlands Ember kin, Chilling: Peaks
    ///     Frost kin, Irrigating / Fishing: the Isles, and so on), so travelling still upgrades the camp;
    ///   - each outer land's natives cover the basics too (for outposts and for players who settle there).
    /// Jobs follow what the kin is: Volt and Lumen kin can spark a fire, woodland Flora and bucktoothed Normal kin log,
    /// cool damp kin chill. Mostly a filler level-1 Hauling / Foraging job was swapped for a scarce one, or a second
    /// job was added; nothing lost a job it was good at. Applied over the species after they are built, like
    /// ApplySpawnConditions; the assets were synced from it (assets override the seed).
    /// </summary>
    public static partial class ContentSeed
    {
        const WorkType Log = WorkType.Logging, Qry = WorkType.Quarrying, Knd = WorkType.Kindling, Irr = WorkType.Irrigating, Clt = WorkType.Cultivating,
                       Crf = WorkType.Crafting, Hau = WorkType.Hauling, Chl = WorkType.Chilling, Chg = WorkType.Charging, For = WorkType.Foraging;

        static void ApplyJobBalance(Bundle b)
        {
            void Jobs(string id, params (WorkType type, int level)[] jobs)
            {
                var sp = b.species.Find(x => x.id == id);
                if (sp == null) { Debug.LogWarning("[ContentSeed] job balance: no species " + id); return; }
                sp.work = new WorkSkill[jobs.Length];
                for (int i = 0; i < jobs.Length; i++) sp.work[i] = new WorkSkill(jobs[i].type, jobs[i].level);
            }

            // ---------------- Sunmeadow: loggers ----------------
            Jobs("acornip", (Log, 2), (For, 2));
            Jobs("mossnout", (For, 2), (Log, 1), (Clt, 1));
            Jobs("bibbit", (Log, 1), (For, 1), (Hau, 1));
            Jobs("bramblebat", (For, 2), (Log, 1));
            Jobs("vinelash", (Log, 2), (Clt, 2), (Crf, 1));
            Jobs("lashtail", (Crf, 2), (Log, 1), (Hau, 1));
            Jobs("bumbleloom", (Clt, 3), (Crf, 1), (For, 1));
            Jobs("twistail", (Clt, 2), (Hau, 2), (For, 1));
            // ---------------- Sunmeadow: fire-starters (Volt sparks, Lumen sunbeams, a Shade/Ember imp) ----------------
            Jobs("zapfly", (Chg, 1), (Knd, 1), (For, 1));
            Jobs("fuzzap", (Chg, 1), (Knd, 1), (For, 1));
            Jobs("plasmoth", (Chg, 2), (Knd, 2), (Crf, 1));
            Jobs("hexhorn", (Knd, 2), (Crf, 2));
            Jobs("raybit", (Knd, 2), (Chg, 2));
            Jobs("lumipup", (Hau, 2), (Chg, 1), (Knd, 1));
            // ---------------- Sunmeadow: something to keep a larder cool ----------------
            Jobs("nimbuff", (Irr, 1), (Chl, 1), (Hau, 1));
            Jobs("gloomtoad", (Irr, 1), (Chl, 1), (For, 1));
            Jobs("dribblet", (Irr, 2), (Chl, 1));
            Jobs("puffspine", (Irr, 2), (Chl, 2));

            // ---------------- Badlands ----------------
            Jobs("tuskaroo", (Hau, 2), (Log, 2), (Qry, 1));
            Jobs("cactoad", (Clt, 2), (Irr, 1), (For, 1));
            Jobs("ashcrow", (For, 2), (Hau, 1), (Log, 1));
            // ---------------- Peaks ----------------
            Jobs("staticub", (Chg, 2), (Knd, 1), (For, 1));
            Jobs("needlekit", (Log, 2), (Clt, 2));
            Jobs("sealance", (Chl, 2), (Irr, 2));
            Jobs("snowmote", (Chl, 1), (Irr, 1), (Hau, 1));
            // ---------------- Driftkey Isles ----------------
            Jobs("palmunk", (Log, 2), (For, 2));
            Jobs("starlet", (Crf, 1), (Irr, 1), (Knd, 1));
            Jobs("jellume", (Chg, 2), (Irr, 1));
            Jobs("coralope", (Irr, 2), (Clt, 2), (Hau, 1));
            Jobs("cocorab", (Qry, 2), (Hau, 1));
            // ---------------- Mirefen ----------------
            Jobs("fenfly", (Chg, 1), (Knd, 1), (For, 1));
            Jobs("murkfin", (Irr, 2), (Chl, 1), (Qry, 1));
            Jobs("mudpup", (Qry, 2), (Hau, 1), (Irr, 1));
            Jobs("rushcat", (Log, 2), (For, 2), (Clt, 1));
            Jobs("tadpoll", (Irr, 2), (Chl, 1));
            // ---------------- Sunscour Dunes ----------------
            Jobs("prickpear", (Clt, 2), (Log, 1), (Irr, 1));
            Jobs("dromble", (Hau, 3), (Irr, 2));
            Jobs("dunelet", (Qry, 2), (Hau, 1));
            Jobs("tumblit", (For, 1), (Hau, 1), (Log, 1));
            // ---------------- Blightmoor ----------------
            Jobs("oozlet", (Irr, 2), (Chl, 1));
            Jobs("stingnat", (For, 2), (Hau, 1));
            Jobs("rotgrub", (Qry, 2), (Clt, 1));
            // ---------------- Glowdeep ----------------
            Jobs("glowmite", (Chg, 1), (Knd, 1));
            Jobs("sporeglide", (Clt, 2), (Log, 1), (For, 1));
        }

        /// <summary>
        /// Build menu tabs for the structures whose purpose can't be read off their fields (BuildCategory.Auto covers
        /// stations, producers, storage, beds and feeders - see MenuUI.BuildCatOf).
        /// </summary>
        static void ApplyBuildCategories(Bundle b)
        {
            foreach (var id in new[] { "kin_clinic", "hatchery", "training_yard" })
            {
                var d = b.buildings.Find(x => x.id == id);
                if (d != null) d.category = BuildCategory.KinCare;
            }
        }

        /// <summary>Bigger places to keep things, each learned at the Research Bench (Core/Tech.cs: chests, vaults, grandvault).</summary>
        static void BuildStorageBuildings(Bundle b)
        {
            var wood = Pal(Hex("#8a5a32"), Hex("#6a4426"), Hex("#c8a060"), Hex("#ffe070"));
            var iron = Pal(Hex("#7c8088"), Hex("#4c5058"), Hex("#c8ccd8"), Hex("#ffd060"));
            var grand = Pal(Hex("#8e8a92"), Hex("#5c6a8a"), Hex("#e0c070"), Hex("#7df2ff"));

            var big = Bld(b, "large_chest", "Large Chest", "+40 camp storage slots: twice a Storage Chest, in a banded trunk.", 1, 1.25f, ("wood", 30), ("fiber", 10), ("stone", 6));
            big.storageSlots = 40; big.palette = wood;
            big.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.5f, 0, 2.0f, 1.0f, 1.2f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 1.0f, 0, 1.2f, 2.0f, 1.2f, 0, 0, 90),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.7f, 0.62f, 0, 0.14f, 1.3f, 1.28f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0.7f, 0.62f, 0, 0.14f, 1.3f, 1.28f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 0.92f, 0.62f, 0.26f, 0.3f, 0.08f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.06f, 0, 2.15f, 0.12f, 1.35f),
            };

            var vault = Bld(b, "iron_vault", "Ironbound Vault", "+100 camp storage slots behind an iron door.", 1, 1.55f, ("wood", 30), ("stone", 30), ("ingot", 12));
            vault.storageSlots = 100; vault.palette = iron;
            vault.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.12f, 0, 2.9f, 0.24f, 2.4f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 1.25f, 0, 2.5f, 2.1f, 2.0f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 2.38f, 0, 2.7f, 0.2f, 2.2f),
                // door, hinges and a wheel lock
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.15f, 1.02f, 1.5f, 1.7f, 0.1f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 1.2f, 1.1f, 0.7f, 0.08f, 0.7f, 90, 0, 0),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 1.2f, 1.16f, 0.2f, 0.2f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.8f, 1.7f, 1.06f, 0.2f, 0.14f, 0.08f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -0.8f, 0.6f, 1.06f, 0.2f, 0.14f, 0.08f),
                // corner straps
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, -1.26f, 1.25f, 1.0f, 0.12f, 2.1f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 1.26f, 1.25f, 1.0f, 0.12f, 2.1f, 0.12f),
            };

            var hall = Bld(b, "grand_vault", "Grand Vault", "+180 camp storage slots: a stone treasury with a crystal lantern. The last storage a camp needs.", 1, 2.0f, ("wood", 60), ("stone", 50), ("ingot", 25), ("crystal", 12));
            hall.storageSlots = 180; hall.palette = grand;
            hall.parts = new[]
            {
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 0.2f, 0, 4.0f, 0.4f, 3.4f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, 0, 1.6f, -0.1f, 3.4f, 2.4f, 2.8f),
                P(PartShape.Dome, BodyGroup.Body, ColorSlot.Secondary, 0, 2.8f, -0.1f, 3.3f, 1.5f, 2.7f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0, 4.2f, -0.1f, 0.4f, 0.9f, 0.4f),
                // pillared front and a banded door
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, -1.35f, 1.5f, 1.5f, 0.36f, 2.2f, 0.36f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 1.35f, 1.5f, 1.5f, 0.36f, 2.2f, 0.36f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 2.7f, 1.5f, 3.4f, 0.3f, 0.6f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, 0, 1.35f, 1.32f, 1.5f, 1.9f, 0.12f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 1.8f, 1.4f, 1.6f, 0.12f, 0.06f),
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.9f, 1.4f, 1.6f, 0.12f, 0.06f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0.5f, 1.35f, 1.42f, 0.18f, 0.18f, 0.1f),
            };
        }
    }
}
