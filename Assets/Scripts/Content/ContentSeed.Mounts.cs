using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Mount balance (user playtest 2026-10-15: "i want galewing to be a early flying mount, and we need more mounts,
    /// barkhorn should not be our fastest as he is a early game mount, now he can roll super good ability on travel
    /// and compete but base he shouldn't be"). Base gallop speed now follows where the mount lives and how rare it is:
    ///   early (Sunmeadow, Driftkey Isles)            10.5-11.5
    ///   middle (Badlands, Peaks, Mirefen)            11.5-13
    ///   late (Sunscour Dunes, Blightmoor, Glowdeep)  13-13.5
    ///   Epic 13.5-14, Legendary 14.5-15
    /// A kin's Travel quality (`CreatureData.SpeedMul`, ~x0.9-1.15) still moves it inside that, so a well-rolled
    /// early mount can keep up with an average later one - but no early mount starts out the fastest. Heavy kin (shells,
    /// tusks, camels) sit a step under their tier; they make it up in toughness.
    /// Every mountable Flyer flies when ridden (PlayerController.Flight). Galewing is the first one: Rare, Sunmeadow.
    /// 14 more kin became mounts. A seat of zero means "work it out from the model" (PlayerController.Seat).
    /// Applied over the species after they are built; the assets were synced from it (assets override the seed).
    /// </summary>
    public static partial class ContentSeed
    {
        static void ApplyMountBalance(Bundle b)
        {
            void Ride(string id, float speed, float jump = 0f)
            {
                var sp = b.species.Find(x => x.id == id);
                if (sp == null) { Debug.LogWarning("[ContentSeed] mount balance: no species " + id); return; }
                if (!sp.mountable) { sp.mountable = true; sp.mountSeat = Vector3.zero; if (jump <= 0f) jump = 8.5f; }
                sp.mountSpeed = speed;
                if (jump > 0f) sp.mountJump = jump;
            }

            // ---------------- early ----------------
            Ride("barkhorn", 11f);          // was 13.5 - the fastest mount in the game, from the first meadow
            Ride("ramparth", 11f);
            Ride("frostuft", 10.5f);
            Ride("tidalith", 9.5f);
            Ride("tortide", 9.5f);
            Ride("wavecrest", 11.5f);       // flies and surfs
            Ride("galewing", 11f, 9f);      // NEW - the early flying mount (Rare, Sunmeadow and Peaks)
            Ride("thornback", 10.5f, 8f);   // NEW
            Ride("bouldermaw", 10.5f, 7.5f);// NEW
            Ride("tidewing", 11.5f, 8.5f);  // NEW - flies, and a Tide kin surfs

            // ---------------- middle ----------------
            Ride("raijolt", 13f);           // was 15
            Ride("duskfang", 13f);          // was 14.5
            Ride("emberhoof", 12.5f);       // was 14
            Ride("galegryph", 12.5f);
            Ride("glaciermane", 12.5f);
            Ride("lumicorn", 12f);
            Ride("dunestrider", 12f);
            Ride("rustmaw", 11.5f);
            Ride("stormhorn", 11.5f);
            Ride("tundrusk", 10.5f);
            Ride("mirejaw", 10.5f);
            Ride("kingmane", 12.5f, 9f);    // NEW
            Ride("cindermaw", 12f, 8.5f);   // NEW
            Ride("mangrowl", 12f, 8.5f);    // NEW
            Ride("zephyrion", 12.5f, 9f);   // NEW - flies
            Ride("pyrovern", 12.5f, 9f);    // NEW - flies

            // ---------------- late ----------------
            Ride("miasmare", 13f);
            Ride("rothound", 13f);
            Ride("lunelk", 13f);
            Ride("starwhal", 13f);          // flies
            Ride("sunsphinx", 13f);
            Ride("dromble", 11.5f);
            Ride("glowmaw", 13.5f, 9f);     // NEW
            Ride("mirageon", 13.5f, 9f);    // NEW - flies

            // ---------------- Epic / Legendary ----------------
            Ride("solarion", 14f);
            Ride("aurorox", 13.5f);
            Ride("hydrabog", 13.5f, 8f);    // NEW
            Ride("halcyrie", 14f, 9.5f);    // NEW - flies
            Ride("djinnado", 14f, 9.5f);    // NEW - flies
            Ride("marelune", 14.5f);        // flies and surfs
            Ride("kiralux", 15f);
        }
    }
}
