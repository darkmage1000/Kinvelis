using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp work happens at the structure (user, 2026-10-14: "make me walk up to each work station to use them ...
    /// the camp feels unnecessary to build in if you rarely even need to touch any of it"). Menus opened from far
    /// away are views: crafting, keep-in-stock, orders, research, breeding and hatching need you at that station;
    /// taking and storing need you at that box (or any box for the camp-wide store); the Hearthstone holds its own
    /// upgrade, auto-assign and the job list; workers are assigned, crops chosen, output collected and structures
    /// demolished at the structure itself (MANAGE). Dev mode's EVERYTHING FREE lifts this too.
    /// </summary>
    public partial class BaseManager
    {
        /// <summary>How close counts as "at" a structure, past its footprint (the interact prompt shows at 2.2).</summary>
        public const float Reach = 2.8f;

        Vector3 PlayerPos => GameManager.I != null && GameManager.I.Player != null ? GameManager.I.Player.Position : new Vector3(1e6f, 0f, 1e6f);

        public bool PlayerAt(Building b)
        {
            if (b == null) return false;
            if (Dev.FreeAll) return true;
            Vector3 d = b.transform.position - PlayerPos; d.y = 0f;
            return d.magnitude - b.Def.footprint <= Reach;
        }

        /// <summary>The structure of this station kind the player is standing at, or null.</summary>
        public Building StationHere(CraftStation st, int minTier = 1)
        {
            if (st == CraftStation.Hand) return null;
            foreach (var b in Buildings) if (b.Def.station == st && Stations.Tier(b) >= minTier && PlayerAt(b)) return b;
            return null;
        }

        /// <summary>Standing at a station of this kind, at least this tier (Stations).</summary>
        public bool AtStation(CraftStation st, int minTier = 1) => st == CraftStation.Hand || StationHere(st, minTier) != null;

        /// <summary>A storage box (chest, vault, larder, the Hearthstone) the player is standing at, or null.</summary>
        public Building StorageHere()
        {
            foreach (var b in Buildings) if ((b.BoxLen > 0 || b.Def.isCore) && PlayerAt(b)) return b;
            return null;
        }

        public bool AtCore => Core != null && PlayerAt(Core);

        public Building BuildingHere(string id)
        {
            foreach (var b in Buildings) if (b.Def.id == id && PlayerAt(b)) return b;
            return null;
        }

        /// <summary>"12 m north-east" from the player to a structure, for "walk there" hints.</summary>
        public string WayTo(Building b)
        {
            if (b == null) return "";
            Vector3 d = b.transform.position - PlayerPos; d.y = 0f;
            float m = d.magnitude;
            if (m < 4f) return "right here";
            string[] dirs = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
            float ang = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            int i = Mathf.RoundToInt(((ang + 360f) % 360f) / 45f) % 8;
            return $"{m:0} m {dirs[i]}";
        }

        /// <summary>The nearest structure with this station (to point the player at it).</summary>
        public Building NearestStation(CraftStation st, int minTier = 1)
        {
            Building best = null; float bd = float.MaxValue;
            foreach (var b in Buildings)
            {
                if (b.Def.station != st || Stations.Tier(b) < minTier) continue;
                float d = (b.transform.position - PlayerPos).sqrMagnitude;
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }
    }
}
