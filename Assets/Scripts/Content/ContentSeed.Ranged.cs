using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Ranged weapons (user request 2026-10-09). Three ways to use them (HandStyle.Sling / Bow / Crossbow, moves in
    /// HandStyles, shots in ProjectileSystem.FireShot) across the same tiers as the melee weapons. Attack power is
    /// about 85% of the melee weapon of the same tier and shots have no combo finisher, so a bow is safer, not
    /// stronger. Range and feel come from the style: sling 20 m and quick, bow 26 m, crossbow 30 m and slow.
    /// Every shot uses one piece of ammunition (HandStyles.Ammo; user, 2026-10-10: "they are op with unlimited"):
    /// a sling throws plain Stones, bows need Arrows (10 by hand), crossbows need Bolts (10 at a Workbench). Out of
    /// ammunition, ATTACK swings the weapon like a club.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildRangedItems(Bundle b)
        {
            ItemDef W(string id, string name, string glyph, string hex, float dmg, HandStyle style, string desc)
            {
                var d = Item(b, id, name, ItemCategory.Weapon, glyph, Hex(hex), desc, 1);
                d.weaponDamage = dmg; d.handStyle = style;
                return d;
            }

            W("sling", "Cord Sling", "Sg", "#b89868", 9f, HandStyle.Sling, "A plaited cord and a pouch. Whirl it once and a pebble does the rest. Each shot throws one Stone from your bag.");
            W("short_bow", "Yew Shortbow", "Bw", "#9a6a3a", 15f, HandStyle.Bow, "A springy hunting bow. Hold ATTACK to keep loosing at the kin you are facing or have locked on to. Each shot uses one Arrow.");
            W("ingot_crossbow", "Ingot Crossbow", "Xb", "#c8ccd8", 23f, HandStyle.Crossbow, "A steel prod on a wooden stock. Slow to wind, but each shot lands like a hammer. Each shot uses one Bolt.");
            W("bogsteel_crossbow", "Bogsteel Arbalest", "Ar", "#5a6a4a", 39f, HandStyle.Crossbow, "Heavy fen steel, wound with both hands. Patient work for a patient hunter. Each shot uses one Bolt.");
            W("sunsteel_longbow", "Sunsteel Longbow", "Lb", "#ffd860", 49f, HandStyle.Bow, "Gold-limbed and warm to hold. Its arrows leave a streak of daylight. Each shot uses one Arrow.");
            W("starsteel_greatbow", "Starsteel Greatbow", "Gb", "#c8c0ff", 75f, HandStyle.Bow, "Taller than its archer, strung with a thread of starcrystal. The last bow anyone needs. Each shot uses one Arrow.");

            // ammunition
            Item(b, "arrow", "Arrow", ItemCategory.Resource, "Aw", Hex("#c9a26a"), "Ammunition for every bow: one is used each shot. A straight stick, a flint and a strip of fiber for the fletching.");
            Item(b, "bolt", "Bolt", ItemCategory.Resource, "Bt", Hex("#b8bcc8"), "Ammunition for every crossbow: one is used each shot. Short, steel-headed and heavy.");
            Recipe(b, "arrow", 10, CraftStation.Hand, 0, 1, ("wood", 2), ("stone", 1), ("fiber", 1));
            Recipe(b, "bolt", 10, CraftStation.Workbench, 8, 7, ("ingot", 1), ("wood", 2));

            Recipe(b, "sling", 1, CraftStation.Hand, 0, 1, ("fiber", 6), ("stone", 2));
            Recipe(b, "short_bow", 1, CraftStation.Workbench, 15, 3, ("wood", 8), ("fiber", 8));
            Recipe(b, "ingot_crossbow", 1, CraftStation.Workbench, 40, 7, ("ingot", 5), ("wood", 6), ("fiber", 6));
            Recipe(b, "bogsteel_crossbow", 1, CraftStation.Workbench, 45, 8, ("bogsteel_bar", 6), ("wood", 6), ("fiber", 8));
            Recipe(b, "sunsteel_longbow", 1, CraftStation.Workbench, 55, 11, ("sunsteel_bar", 6), ("wood", 6), ("pelt", 2));
            Recipe(b, "starsteel_greatbow", 1, CraftStation.Workbench, 80, 17, ("starsteel_bar", 8), ("crystal", 6));
        }
    }
}
