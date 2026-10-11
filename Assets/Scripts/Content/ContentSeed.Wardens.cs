using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Boss-only kin and the items the Warden journey hands out.</summary>
        static void BuildWardenContent(Bundle b)
        {
            Item(b, "warden_heart", "Warden Heart", ItemCategory.Resource, "Wh", Hex("#ff7ab0"),
                "A warm, slow-beating stone left behind by a beaten Warden. Camp upgrades and research need them. Three from a first win, one from each rematch.");

            // ---------------------------------------------------------------- 179 KRAKOLOS (the Kraken, boss only)
            {
                var d = K(b, 179, "krakolos", "Krakolos", Element.Tide, Element.Shade, Rarity.Epic, BiomeMask.None, Temperament.Aggressive, Locomotion.Slither, 2.1f, 'T',
                    "The Kraken. It sleeps under a lone rock in the south-east sea with a wrecked fleet for a nest, and Kraklings are what it leaves behind.",
                    "Vast, patient, takes everything personally.", A("tidal_lash", "wave_crash"), "gloom_pulse",
                    Work((WorkType.Hauling, 3), (WorkType.Irrigating, 3)), CompanionPerk.None, "gel", "crystal",
                    Pal(Hex("#5a2a5a"), Hex("#c890b8"), Hex("#ff7a4a"), Hex("#ffe070")));
                d.bossOnly = true;
                d.spawnWeight = 0f;
                d.nearWater = true;
                Effects(d, AuraFx.Bubbles);
                var p = new Plan()
                    .Add(Sph, gB, cP, V(0, 0.95f, -0.2f), V(0.9f, 1.3f, 1.0f), V(-18, 0, 0))
                    .Add(Sph, gB, cS, V(0, 1.1f, -0.36f), V(0.6f, 0.9f, 0.5f), V(-18, 0, 0))
                    .Blob(0.52f, 0.86f, 0.62f, 0.8f)
                    .Face(0.13f, cG, 0.16f, 0.25f).Brows(cD, 0.16f, 0.25f)
                    .Add(Con, gB, cW, V(0, 0.3f, 0.36f), V(0.16f, 0.22f, 0.16f), V(160, 0, 0))
                    .Spikes(gB, cA, 1.5f, 0f, -0.6f, 4, 0.12f, 0.3f, 0.05f, -20)
                    .Mirror(Sph, gB, cP, V(0.42f, 1.3f, -0.4f), V(0.5f, 0.08f, 0.6f), V(0, 0, -30));
                for (int i = 0; i < 8; i++)
                {
                    float a = (22.5f + i * 45f) * Mathf.Deg2Rad;
                    p.Whip(i >= 3 && i <= 4 ? gT : gB, cP, V(Mathf.Sin(a) * 0.3f, 0.22f, Mathf.Cos(a) * 0.3f), V(Mathf.Sin(a) * 0.13f, -0.03f, Mathf.Cos(a) * 0.13f), V(0, 0.01f, 0), 7, 0.2f, 0.07f, cS);
                }
                p.WhipPair(gAL, gAR, cP, V(0.4f, 0.6f, 0.1f), V(0.1f, 0.1f, 0.05f), V(0.002f, -0.02f, 0.006f), 11, 0.16f, 0.08f, cS)
                    .TipPair(Sph, gAL, gAR, cA, V(0.3f, 0.16f, 0.3f))
                    .PivotPair(gAL, gAR, V(0.36f, 0.6f, 0.06f)).Pivot(gT, V(0, 0.22f, -0.3f))
                    .Done(d);
            }
        }
    }
}
