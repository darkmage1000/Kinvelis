using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public enum PoiKind { Ruin, Shipwreck, Skeleton, Camp, Statue, Lookout }

    /// <summary>
    /// Small things to find out in the world: broken ruins, shipwrecks on the beaches, great skeletons, abandoned camps
    /// and old statues - each with a chest, a tablet of lore, or both - and lookout towers. A lookout (LOOK OUT at its
    /// foot) marks everything within 650 m on your map: caves, waystones, named places, other finds and a glint for
    /// any relic. Placement is seeded and happens after every other site has chosen its ground. Nothing shows on the
    /// map until you have been near it (lookouts are landmarks, so you can always see where to climb).
    /// </summary>
    public class Pois : MonoBehaviour
    {
        public static Pois I { get; private set; }
        public const float LookRadius = 650f;

        public class Poi
        {
            public string id, name, title, lore;
            public PoiKind kind;
            public BiomeId biome;
            public Vector3 pos, chestPos, tabletPos;
            public float yaw;
            public bool hasChest, hasTablet;
            public GameObject go;
            public Transform lid;
        }

        public static readonly List<Poi> All = new List<Poi>();
        float checkTimer;

        static PlayerStats S => GameManager.I.Player.Stats;
        public static bool Found(Poi p) => S.poisFound.Contains(p.id);
        public static bool Looted(Poi p) => S.poisLooted.Contains(p.id);
        public static bool Read(Poi p) => S.poisRead.Contains(p.id);
        /// <summary>Everything this place offers has been done.</summary>
        public static bool Done(Poi p) => p.kind == PoiKind.Lookout ? S.lookouts.Contains(p.id) : (!p.hasChest || Looted(p)) && (!p.hasTablet || Read(p));

        public static MapIcon Icon(PoiKind k)
        {
            switch (k)
            {
                case PoiKind.Ruin: return MapIcon.Pillars;
                case PoiKind.Shipwreck: return MapIcon.Wreck;
                case PoiKind.Skeleton: return MapIcon.Bones;
                case PoiKind.Camp: return MapIcon.Tent;
                case PoiKind.Statue: return MapIcon.Statue;
                default: return MapIcon.Lookout;
            }
        }

        public static string KindName(PoiKind k)
        {
            switch (k)
            {
                case PoiKind.Ruin: return "a broken ruin";
                case PoiKind.Shipwreck: return "a shipwreck";
                case PoiKind.Skeleton: return "a great skeleton";
                case PoiKind.Camp: return "an abandoned camp";
                case PoiKind.Statue: return "an old statue";
                default: return "a lookout tower";
            }
        }

        // ================================================================ PLACEMENT
        public void Init()
        {
            I = this;
            All.Clear();
            var rng = new System.Random(WorldGen.Seed * 4099 + 7);
            // the old island: five finds and two lookouts per biome
            foreach (var b in new[] { BiomeId.Meadow, BiomeId.Badlands, BiomeId.Peaks })
            {
                Place(rng, b, PoiKind.Camp, 0);
                Place(rng, b, PoiKind.Ruin, 0);
                Place(rng, b, PoiKind.Skeleton, 0);
                Place(rng, b, PoiKind.Statue, 0);
                Place(rng, b, PoiKind.Shipwreck, 0);
                Place(rng, b, PoiKind.Lookout, 0);
                Place(rng, b, PoiKind.Lookout, 1);
            }
            // each outer land: four finds and one lookout
            foreach (var land in WorldGen.Lands)
            {
                Place(rng, land.biome, PoiKind.Shipwreck, 0);
                Place(rng, land.biome, PoiKind.Camp, 0);
                Place(rng, land.biome, land.biome == BiomeId.Isles ? PoiKind.Shipwreck : PoiKind.Ruin, 1);
                Place(rng, land.biome, land.biome == BiomeId.Desert || land.biome == BiomeId.Glow ? PoiKind.Statue : PoiKind.Skeleton, 0);
                Place(rng, land.biome, PoiKind.Lookout, 0);
            }
        }

        void Place(System.Random rng, BiomeId biome, PoiKind kind, int n)
        {
            Vector2 c; float r;
            if (WorldGen.TryLand(biome, out var land)) { c = land.center; r = land.radius * 0.9f; }
            else { c = biome == BiomeId.Meadow ? WorldGen.MeadowCenter : biome == BiomeId.Badlands ? WorldGen.BadlandsCenter : WorldGen.PeaksCenter; r = 620f; }
            Vector3 best = Vector3.zero; float bestScore = -1e9f; float bestYaw = 0f;
            int tries = kind == PoiKind.Shipwreck ? 2400 : 400;
            for (int i = 0; i < tries; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = Mathf.Sqrt((float)rng.NextDouble()) * r;
                float x = c.x + Mathf.Cos(a) * d, z = c.y + Mathf.Sin(a) * d;
                float h = WorldGen.Height(x, z);
                if (h < 1f || WorldGen.Biome(x, z) != biome) continue;
                float yaw = (float)rng.NextDouble() * 360f, score = (float)rng.NextDouble();
                if (kind == PoiKind.Shipwreck)
                {
                    // a beach: low ground with the sea close on one side; the bow points inland
                    if (h > 2.4f || h < 0.9f) continue;
                    bool sea = false;
                    for (int k = 0; k < 8 && !sea; k++)
                    {
                        float sa = k * Mathf.PI / 4f;
                        if (WorldGen.Height(x + Mathf.Cos(sa) * 26f, z + Mathf.Sin(sa) * 26f) < -0.6f) { sea = true; yaw = Mathf.Atan2(-Mathf.Cos(sa), -Mathf.Sin(sa)) * Mathf.Rad2Deg; }
                    }
                    if (!sea) continue;
                }
                else if (kind == PoiKind.Lookout)
                {
                    // climb to the top of the nearest rise: lookouts want height
                    float px = x, pz = z, ph = h;
                    for (int step = 0; step < 30; step++)
                    {
                        float bx = px, bz = pz, bh = ph;
                        for (int k = 0; k < 8; k++)
                        {
                            float qx = px + Mathf.Cos(k * Mathf.PI / 4f) * 8f, qz = pz + Mathf.Sin(k * Mathf.PI / 4f) * 8f, qh = WorldGen.Height(qx, qz);
                            if (qh > bh) { bx = qx; bz = qz; bh = qh; }
                        }
                        if (bh <= ph + 0.05f) break;
                        px = bx; pz = bz; ph = bh;
                    }
                    if (WorldGen.Biome(px, pz) != biome) continue;
                    x = px; z = pz; h = ph;
                    score += h * 0.05f;
                }
                else if (h < 1.7f) continue;
                if (WorldGen.Normal(x, z, 4f).y < (kind == PoiKind.Lookout ? 0.9f : kind == PoiKind.Shipwreck ? 0.94f : 0.965f)) continue;
                var p = new Vector3(x, h, z);
                if (!Clear(p, kind)) continue;
                if (score > bestScore) { bestScore = score; best = p; bestYaw = yaw; if (kind != PoiKind.Lookout) break; }
            }
            if (bestScore < -1e8f) return;
            var poi = new Poi { id = $"{(int)biome}_{kind}_{n}".ToLowerInvariant(), kind = kind, biome = biome, pos = best, yaw = bestYaw };
            Describe(poi, All.Count);
            Build(poi);
            All.Add(poi);
            if (kind == PoiKind.Lookout)
                Landmarks.List.Add(new Landmarks.Landmark { name = poi.name, pos = poi.pos, biome = biome, color = new Color(0.95f, 0.9f, 0.7f), go = poi.go, icon = MapIcon.Lookout });
        }

        static bool Clear(Vector3 p, PoiKind kind)
        {
            float d2;
            foreach (var o in All) { d2 = Flat2(o.pos, p); if (d2 < (kind == PoiKind.Lookout && o.kind == PoiKind.Lookout ? 500f * 500f : 130f * 130f)) return false; }
            foreach (var s in Settlements.All) if (Flat2(s.pos, p) < 190f * 190f) return false;
            foreach (var w in Wardens.All) if (Flat2(w.pos, p) < 95f * 95f) return false;
            if (Caves.I != null) foreach (var e in Caves.I.Entrances) if (Flat2(e.pos, p) < 60f * 60f) return false;
            foreach (var h in LegendHunts.All)
            {
                if (Flat2(h.lairPos, p) < 100f * 100f) return false;
                if (h.clues != null) foreach (var c in h.clues) if (Flat2(c.pos, p) < 45f * 45f) return false;
            }
            if (Flat2(LegendHunts.HunterPos, p) < 50f * 50f) return false;
            if (Landmarks.NearLandmark(p, 50f)) return false;
            if (Waystones.I != null) foreach (var w in Waystones.I.All) if (Flat2(w.pos, p) < 40f * 40f) return false;
            return WorldGen.StartDistance(p.x, p.z) > 200f;
        }

        static float Flat2(Vector3 a, Vector3 b) { float dx = a.x - b.x, dz = a.z - b.z; return dx * dx + dz * dz; }

        public static bool BlocksNode(Vector3 p)
        {
            for (int i = 0; i < All.Count; i++)
            {
                float r = All[i].kind == PoiKind.Shipwreck ? 11f : 8f;
                if (Flat2(All[i].pos, p) < r * r) return true;
            }
            return false;
        }

        // ================================================================ WORDS
        static readonly string[] LookoutNames = { "Hilltop Watch", "Wardens' Watch", "Longview", "Farsight Post" };

        static void Describe(Poi p, int index)
        {
            string land = BiomeInfo.Name(p.biome);
            switch (p.kind)
            {
                case PoiKind.Lookout:
                    p.name = LookoutNames[index % LookoutNames.Length] + " (" + land + ")";
                    return;
                case PoiKind.Ruin: p.name = "Broken ruin"; p.hasChest = true; p.hasTablet = true; p.title = "Carved in the stone"; break;
                case PoiKind.Shipwreck: p.name = "Shipwreck"; p.hasChest = true; p.hasTablet = index % 2 == 0; p.title = "A page from the ship's log"; break;
                case PoiKind.Skeleton: p.name = "Great skeleton"; p.hasTablet = true; p.hasChest = index % 2 == 1; p.title = "Scratched on a rib"; break;
                case PoiKind.Camp: p.name = "Abandoned camp"; p.hasChest = true; p.hasTablet = true; p.title = "A torn journal page"; break;
                default: p.name = "Old statue"; p.hasTablet = true; p.title = "On the plinth"; break;
            }
            p.lore = Lore(p.biome, p.kind, index);
        }

        /// <summary>Lore is also advice: every line points at something real in the game.</summary>
        static string Lore(BiomeId b, PoiKind k, int index)
        {
            switch (b)
            {
                case BiomeId.Meadow:
                    switch (k)
                    {
                        case PoiKind.Ruin: return "\"Eight lands, eight keepers. Wake each in its ring of stones and take its Sigil. When all eight are yours, the storm itself will answer.\"";
                        case PoiKind.Camp: return "\"Day 3. Moved camp nearer the Old Hearth. The kin here are gentle near the fields where we landed, but they grow bolder the further I walk. Note: Honeybloom only grows on the pink hills to the east.\"";
                        case PoiKind.Skeleton: return "\"It lay down here long before the vale had a name. The Flora kin still will not graze inside its ribs.\"";
                        case PoiKind.Statue: return "\"BLOOMWYRM, who sleeps under Mossglow. Leave it sleeping unless your kin are ready.\"";
                        default: return "\"We ran aground chasing lights on the water. The crew swear the sea is lower at midday out by the isles. I should have listened.\"";
                    }
                case BiomeId.Badlands:
                    switch (k)
                    {
                        case PoiKind.Ruin: return "\"The ground breathes here. Count to six after the steam stops, and be standing somewhere else by eight.\"";
                        case PoiKind.Camp: return "\"Ember kin sleep at night. That is when the Shade ones come. I have stopped lighting fires.\"";
                        case PoiKind.Skeleton: return "\"Burned black from the inside. Whatever it fought, it fought it in the Glasswaste, where the sand ran to glass.\"";
                        case PoiKind.Statue: return "\"SOLARION, light of Cinderdeep. Its Sigil opens the forge that works sun-metal.\"";
                        default: return "\"Cargo: red ore, loaded at the canyons. The best veins are where the rock runs darkest.\"";
                    }
                case BiomeId.Peaks:
                    switch (k)
                    {
                        case PoiKind.Ruin: return "\"When the sky ribbons come, train. We have seen a season's growth in one night under them.\"";
                        case PoiKind.Camp: return "\"Lost Henrik to the blue field. The ice looks whole until you stand on it. Keep moving and it holds; stop and it does not.\"";
                        case PoiKind.Skeleton: return "\"The prism veins lie past this, far up, where the air is thin. Bring an ingot pick or do not bother.\"";
                        case PoiKind.Statue: return "\"AUROROX, crowned in light. It guards Rimefrost, and it does not forgive.\"";
                        default: return "\"Iced in. Burning the last of the Sunfig jam to stay warm - the desert traders were right about it.\"";
                    }
                case BiomeId.Isles:
                    return k == PoiKind.Camp ? "\"Low tide is the only time the oysters open. Twice a day, near noon and near midnight. Walk the bars while they are up.\""
                         : index % 2 == 0 ? "\"She broke her back on the bar at high water. At low water you can walk from islet to islet and never wet your knees.\""
                         : "\"Any water kin will carry you between the lands. No harness. Just ask it at the shore.\"";
                case BiomeId.Swamp:
                    return k == PoiKind.Camp ? "\"The lights over the fen are real. Catch one and it leaves a little Glimmer in your hand. They only come at night.\""
                         : k == PoiKind.Ruin ? "\"The Drowned Temple floor will open under you. Watch the dark plates; cross when the spikes drop.\""
                         : "\"The fat pods burst if you come close. Back away while they swell and the cloud misses you.\"";
                case BiomeId.Desert:
                    return k == PoiKind.Camp ? "\"The rings in the sand are not solid ground. If it takes you, roll. Do not stand and think about it.\""
                         : k == PoiKind.Statue ? "\"DJINNADO, the wind that walks. Bring Frostcap broth for the day's heat, or come by night when the sand is cold.\""
                         : "\"We found green ground in the middle of the dunes, and figs on every bush. No rings there. We should never have left.\"";
                case BiomeId.Blight:
                    return k == PoiKind.Camp ? "\"The purple caps are medicine if you brew them. Three and a little gel. Nothing in the spore fields can touch you after.\""
                         : k == PoiKind.Ruin ? "\"After dark the miasma comes down and everything in it is stronger. It also carries more worth taking. Choose.\""
                         : "\"Do not wade here. The water eats at you. Ride across or go around.\"";
                default:
                    return k == PoiKind.Camp ? "\"At night the whole deep blooms, and the rare colours come out. I counted twice as many as by day.\""
                         : k == PoiKind.Statue ? "\"ASTRALISK, the star that fell and stayed. Last of the eight.\""
                         : "\"The moss with lights in it weaves into a band that mends the wearer. It grows only on the fields to the north.\"";
            }
        }

        // ================================================================ MODELS
        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));
        static Color32 C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        void Build(Poi p)
        {
            var mb = new MeshBuilder();
            var rot = Quaternion.Euler(0f, p.yaw, 0f);
            var obs = new List<(Vector3, float)>();
            Vector3 chest = Vector3.zero, tablet = Vector3.zero;
            Color32 stone = Color.Lerp(BiomeInfo.Tint(p.biome), new Color(0.55f, 0.55f, 0.56f), 0.75f), stoneTop = Color.Lerp(stone, Color.white, 0.25f), stoneDark = Color.Lerp(stone, Color.black, 0.3f);
            Color32 wood = C("#6e4a2c"), woodLt = C("#9a7048"), woodDk = C("#4a3020"), cloth = Color.Lerp(BiomeInfo.Tint(p.biome), new Color(0.8f, 0.72f, 0.55f), 0.6f), bone = C("#d8d2bc"), boneDk = C("#a8a088");
            switch (p.kind)
            {
                case PoiKind.Ruin:
                    mb.Box(M(0, 0.1f, 0, 9f, 0.3f, 7f), stoneDark, stone);
                    mb.Box(M(-4f, 1.7f, 0, 0.9f, 3.4f, 6.4f), stone, stoneTop);
                    mb.Box(M(-1.2f, 1.1f, 3f, 4.8f, 2.2f, 0.9f), stone, stoneTop);
                    mb.Box(M(2.6f, 0.6f, 3f, 2.2f, 1.2f, 0.9f), stone, stoneTop);
                    mb.Box(M(-1.8f, 2.6f, -3f, 3.6f, 0.8f, 0.9f, 0, 0, -6f), stone, stoneTop);
                    mb.Cylinder(M(-3.2f, 1.2f, -3f, 0.9f, 2.4f, 0.9f), stone, stoneTop, 8, 0.9f, true);
                    mb.Cylinder(M(0f, 1.6f, -3f, 0.9f, 3.2f, 0.9f), stone, stoneTop, 8, 0.9f, true);
                    mb.Cylinder(M(3.4f, 0.45f, -1.6f, 0.9f, 3.4f, 0.9f, 0, 30f, 90f), stone, stoneTop, 8, 0.9f, true);
                    mb.Rock(M(3.6f, 0.4f, 1.2f, 1.4f, 0.9f, 1.2f), stoneDark, stone, 23u, 6, 4);
                    mb.Rock(M(1.4f, 0.3f, -4.4f, 1f, 0.6f, 0.9f), stoneDark, stone, 29u, 6, 4);
                    obs.Add((new Vector3(-4f, 0, -2f), 1.1f)); obs.Add((new Vector3(-4f, 0, 0f), 1.1f)); obs.Add((new Vector3(-4f, 0, 2f), 1.1f));
                    obs.Add((new Vector3(-2.2f, 0, 3f), 1f)); obs.Add((new Vector3(-0.2f, 0, 3f), 1f)); obs.Add((new Vector3(2.6f, 0, 3f), 1f));
                    obs.Add((new Vector3(-3.2f, 0, -3f), 0.8f)); obs.Add((new Vector3(0f, 0, -3f), 0.8f)); obs.Add((new Vector3(3.4f, 0, -1.6f), 1.3f));
                    chest = new Vector3(-2.4f, 0.25f, 1.4f); tablet = new Vector3(1.2f, 0.25f, 0.6f);
                    break;
                case PoiKind.Shipwreck:
                    // a hull heeled over on the sand, stern broken off
                    for (int i = 0; i < 7; i++)
                    {
                        float z = -5.4f + i * 1.8f, w = 2.6f * Mathf.Sin((i + 1.2f) / 8.4f * Mathf.PI) + 0.5f;
                        for (int k = -1; k <= 1; k += 2)
                            mb.Cylinder(M(k * w * 0.62f, 1.3f, z, 0.3f, 3f, 0.34f, 0, 0, k * -28f - 14f), woodDk, wood, 5, 0.7f, true);
                        mb.Box(M(0.2f, 0.3f, z, w * 1.5f, 0.26f, 1.7f, 0, 0, -14f), wood, woodLt);
                    }
                    mb.Box(M(1.9f, 1.5f, 0f, 0.2f, 1.6f, 11.4f, 0, 0, -42f), wood, woodLt);
                    mb.Box(M(-2.3f, 1.3f, -0.6f, 0.2f, 1.5f, 9f, 0, 0, 14f), woodDk, wood);
                    mb.Box(M(0.3f, 0.12f, 0.4f, 0.5f, 0.4f, 13f, 0, 0, -14f), woodDk);
                    mb.Cylinder(M(-0.2f, 3.2f, 1f, 0.4f, 6.4f, 0.4f, 12f, 0, 22f), wood, woodLt, 7, 0.7f, true);
                    mb.Box(M(-1.5f, 4.6f, 1.6f, 3.4f, 0.14f, 0.14f, 12f, 0, 22f), woodDk);
                    mb.Box(M(-1.7f, 3.6f, 1.5f, 2.6f, 1.8f, 0.06f, 12f, 0, 24f), C("#d8d0b8"), C("#b8ac90"));
                    mb.Box(M(3.6f, 0.3f, -4.2f, 1.1f, 0.6f, 1.1f, 0, 25f, 0), wood, woodLt);
                    mb.Cylinder(M(-3.8f, 0.4f, 3.4f, 0.8f, 1f, 0.8f, 90f, 40f, 0), wood, woodLt, 8, 1f, true);
                    obs.Add((new Vector3(0, 0, -4f), 2f)); obs.Add((new Vector3(0, 0, -1f), 2.4f)); obs.Add((new Vector3(0, 0, 2f), 2.4f)); obs.Add((new Vector3(0, 0, 5f), 1.8f));
                    chest = new Vector3(3.6f, 0f, 1.4f); tablet = new Vector3(-3.6f, 0f, -2.6f);
                    break;
                case PoiKind.Skeleton:
                    // half-buried: a line of ribs, a long spine, a skull with a split jaw
                    for (int i = 0; i < 5; i++)
                    {
                        float z = -4.6f + i * 2.1f, hgt = 2.4f + Mathf.Sin(i / 4f * Mathf.PI) * 1.6f;
                        mb.Cylinder(M(2.2f, hgt * 0.42f, z, 0.5f, hgt, 0.5f, 0, 0, 20f), boneDk, bone, 6, 0.5f, true);
                        mb.Cylinder(M(-2.2f, hgt * 0.3f, z, 0.5f, hgt * 0.7f, 0.5f, 0, 0, -34f), boneDk, bone, 6, 0.5f, true);
                        mb.Sphere(M(0, 0.25f, z, 0.9f, 0.6f, 1.2f), boneDk, 6, 4);
                        if (i % 2 == 0) obs.Add((new Vector3(2.4f, 0, z), 0.5f));
                    }
                    mb.Sphere(M(0.3f, 0.9f, 6.6f, 2f, 1.6f, 2.6f, 0, 12f, 8f), bone, boneDk, 9, 6);
                    mb.Box(M(0.5f, 0.25f, 8.4f, 1.4f, 0.3f, 1.8f, 0, 16f, 0), bone, boneDk);
                    mb.Sphere(M(0.95f, 1.15f, 7.2f, 0.45f, 0.45f, 0.4f), C("#1a1614"), 6, 4);
                    mb.Cone(M(-0.5f, 2.1f, 6f, 0.36f, 1.4f, 0.36f, -30f, 0, 14f), boneDk, 6);
                    for (int i = 0; i < 4; i++) mb.Sphere(M(0, 0.2f, -6.4f - i * 1.3f, 0.7f - i * 0.12f, 0.4f, 1f), boneDk, 6, 4);
                    obs.Add((new Vector3(0.3f, 0, 6.8f), 1.5f));
                    chest = new Vector3(-3.4f, 0f, 3f); tablet = new Vector3(3.6f, 0f, 2.4f);
                    break;
                case PoiKind.Camp:
                    // a sagging tent, a cold fire, a bedroll and crates
                    mb.Box(M(-0.75f, 1.05f, -2.4f, 0.1f, 2.5f, 3.4f, 0, 0, -33f), cloth, Color.Lerp(cloth, Color.white, 0.2f));
                    mb.Box(M(0.75f, 1.0f, -2.4f, 0.1f, 2.4f, 3.4f, 0, 0, 36f), cloth, Color.Lerp(cloth, Color.black, 0.15f));
                    mb.Cylinder(M(0, 1.05f, -0.75f, 0.1f, 2.1f, 0.1f), woodDk, 5);
                    mb.Cylinder(M(0.1f, 0.9f, -4.05f, 0.1f, 1.9f, 0.1f, 0, 0, 8f), woodDk, 5);
                    for (int i = 0; i < 7; i++) mb.Rock(M(Mathf.Cos(i * 0.9f) * 0.9f, 0.1f, 2.2f + Mathf.Sin(i * 0.9f) * 0.9f, 0.45f, 0.3f, 0.4f), C("#6a6a70"), C("#8a8a90"), (uint)(i + 11), 5, 3);
                    mb.Cylinder(M(0, 0.14f, 2.2f, 0.16f, 1.2f, 0.16f, 90f, 30f, 0), C("#2a2420"), 5);
                    mb.Cylinder(M(0, 0.2f, 2.2f, 0.14f, 1.1f, 0.14f, 90f, -50f, 0), C("#1e1a18"), 5);
                    mb.Box(M(2.4f, 0.08f, 0.4f, 0.9f, 0.14f, 2f, 0, 18f, 0), cloth, Color.Lerp(cloth, Color.white, 0.3f));
                    mb.Cylinder(M(2.6f, 0.2f, -0.6f, 0.36f, 0.9f, 0.36f, 90f, 108f, 0), cloth, 7);
                    mb.Box(M(-2.6f, 0.35f, 0.8f, 0.9f, 0.7f, 0.9f, 0, 14f, 0), wood, woodLt);
                    mb.Box(M(-2.9f, 0.3f, 2f, 0.7f, 0.6f, 0.7f, 0, -22f, 0), wood, woodLt);
                    mb.Cylinder(M(-1.8f, 0.5f, 2.6f, 0.07f, 1.1f, 0.07f, 0, 0, 16f), woodDk, 4);
                    obs.Add((new Vector3(0, 0, -2.4f), 1.3f)); obs.Add((new Vector3(-2.7f, 0, 1.3f), 0.9f)); obs.Add((new Vector3(0, 0, 2.2f), 0.9f));
                    chest = new Vector3(1.9f, 0f, 2.6f); tablet = new Vector3(-2.6f, 0.72f, 0.8f);
                    break;
                case PoiKind.Statue:
                    // a kin in stone on a stepped plinth
                    mb.Box(M(0, 0.25f, 0, 5f, 0.5f, 5f), stoneDark, stone);
                    mb.Box(M(0, 0.75f, 0, 3.8f, 0.5f, 3.8f), stone, stoneTop);
                    mb.Box(M(0, 1.6f, 0, 2.6f, 1.2f, 2.6f), stone, stoneTop);
                    mb.Sphere(M(0, 3.3f, -0.2f, 2.2f, 2.4f, 2.6f), stone, stoneDark, 10, 7);
                    mb.Sphere(M(0, 4.9f, 0.9f, 1.6f, 1.5f, 1.6f), stoneTop, stone, 10, 7);
                    mb.Cone(M(0.6f, 5.9f, 0.7f, 0.4f, 1.2f, 0.4f, 0, 0, -16f), stoneTop, 6);
                    mb.Cone(M(-0.6f, 5.9f, 0.7f, 0.4f, 1.2f, 0.4f, 0, 0, 16f), stoneTop, 6);
                    mb.Sphere(M(0.4f, 5f, 1.62f, 0.26f, 0.3f, 0.14f), BiomeInfo.Tint(p.biome), 6, 4);
                    mb.Sphere(M(-0.4f, 5f, 1.62f, 0.26f, 0.3f, 0.14f), BiomeInfo.Tint(p.biome), 6, 4);
                    mb.Cylinder(M(0.9f, 2.6f, 0.9f, 0.6f, 1.6f, 0.6f), stone, stoneTop, 7, 0.8f, true);
                    mb.Cylinder(M(-0.9f, 2.6f, 0.9f, 0.6f, 1.6f, 0.6f), stone, stoneTop, 7, 0.8f, true);
                    mb.Cylinder(M(0, 2.9f, -1.9f, 0.5f, 2.2f, 0.5f, -50f, 0, 0), stone, stoneTop, 7, 0.3f, true);
                    obs.Add((Vector3.zero, 2.6f));
                    tablet = new Vector3(0f, 0.5f, 3.1f);
                    break;
                default:
                    // lookout: four legs, cross-braces, a railed platform under a roof, a ladder and a flag
                    for (int i = 0; i < 4; i++)
                    {
                        float lx = (i % 2 == 0 ? -1 : 1) * 1.8f, lz = (i < 2 ? -1 : 1) * 1.8f;
                        mb.Cylinder(M(lx * 0.92f, 4.5f, lz * 0.92f, 0.36f, 9f, 0.36f, -lz * 1.4f, 0, lx * 1.4f), woodDk, wood, 6, 0.9f, true);
                        obs.Add((new Vector3(lx, 0, lz), 0.45f));
                    }
                    for (int lv = 0; lv < 2; lv++)
                    {
                        float y = 2.4f + lv * 3.4f;
                        mb.Box(M(0, y, -1.75f, 3.9f, 0.14f, 0.14f, 0, 0, 42f), wood); mb.Box(M(0, y, 1.75f, 3.9f, 0.14f, 0.14f, 0, 0, -42f), wood);
                        mb.Box(M(-1.75f, y, 0, 0.14f, 0.14f, 3.9f, 42f, 0, 0), wood); mb.Box(M(1.75f, y, 0, 0.14f, 0.14f, 3.9f, -42f, 0, 0), wood);
                    }
                    mb.Box(M(0, 9f, 0, 4.6f, 0.24f, 4.6f), wood, woodLt);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90f;
                        var q = Quaternion.Euler(0, a, 0);
                        Vector3 e = q * new Vector3(0, 9.9f, 2.2f);
                        mb.Box(Matrix4x4.TRS(e, q, new Vector3(4.6f, 0.12f, 0.12f)), woodLt);
                        mb.Box(Matrix4x4.TRS(q * new Vector3(0, 9.45f, 2.2f), q, new Vector3(4.6f, 0.08f, 0.08f)), wood);
                        mb.Cylinder(Matrix4x4.TRS(q * new Vector3(2.2f, 10.6f, 2.2f), Quaternion.identity, new Vector3(0.16f, 3.2f, 0.16f)), woodDk, 5);
                    }
                    mb.Cylinder(M(0, 12.9f, 0, 6.6f, 1.5f, 6.6f, 0, 45f, 0), Color.Lerp(BiomeInfo.Tint(p.biome), new Color(0.5f, 0.3f, 0.2f), 0.55f), Color.Lerp(BiomeInfo.Tint(p.biome), Color.white, 0.2f), 4, 0.05f, true);
                    mb.Box(M(-0.3f, 4.5f, 2.3f, 0.1f, 9.2f, 0.1f, -4f, 0, 0), woodDk); mb.Box(M(0.3f, 4.5f, 2.3f, 0.1f, 9.2f, 0.1f, -4f, 0, 0), woodDk);
                    for (int i = 0; i < 11; i++) mb.Box(M(0, 0.6f + i * 0.8f, 2.33f - i * 0.055f, 0.7f, 0.08f, 0.08f), woodLt);
                    mb.Cylinder(M(0, 15.1f, 0, 0.1f, 3f, 0.1f), woodDk, 5);
                    mb.Box(M(0.8f, 16f, 0, 1.6f, 0.9f, 0.05f), Color.Lerp(BiomeInfo.Tint(p.biome), Color.white, 0.25f));
                    break;
            }

            p.go = new GameObject("Poi_" + p.id);
            p.go.transform.SetParent(transform, false);
            p.go.transform.SetPositionAndRotation(p.pos, rot);
            p.go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("poi_" + p.id);
            var mr = p.go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            foreach (var o in obs) WorldCollision.AddObstacle(p.pos + rot * o.Item1, o.Item2, p.go);

            if (p.hasChest)
            {
                p.chestPos = OnGround(p.pos + rot * chest, chest.y);
                BuildChest(p, rot * Quaternion.Euler(0, 160f, 0));
            }
            if (p.hasTablet)
            {
                p.tabletPos = OnGround(p.pos + rot * tablet, tablet.y);
                BuildTablet(p, rot);
            }
            if (p.kind == PoiKind.Lookout) { p.chestPos = p.tabletPos = p.pos; }
        }

        /// <summary>Extra height is kept when the thing stands on part of the model (a journal on a crate).</summary>
        static Vector3 OnGround(Vector3 p, float lift)
        {
            p.y = WorldGen.Height(p.x, p.z) + (lift > 0.3f ? lift : 0f);
            return p;
        }

        void BuildChest(Poi p, Quaternion rot)
        {
            var mb = new MeshBuilder();
            Color32 wood = new Color(0.45f, 0.3f, 0.18f), band = new Color(0.86f, 0.74f, 0.4f);
            mb.Box(M(0, 0.3f, 0, 1.1f, 0.6f, 0.75f), wood, new Color(0.55f, 0.38f, 0.22f));
            mb.Box(M(0, 0.3f, 0.38f, 0.18f, 0.25f, 0.04f), band);
            var go = new GameObject("Chest");
            go.transform.SetParent(p.go.transform, false);
            go.transform.SetPositionAndRotation(p.chestPos, rot);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("poi_chest");
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            var lmb = new MeshBuilder();
            lmb.Box(M(0, 0.1f, 0.375f, 1.14f, 0.22f, 0.78f), new Color(0.5f, 0.34f, 0.2f), band);
            var lid = new GameObject("Lid");
            lid.transform.SetParent(go.transform, false);
            lid.transform.localPosition = new Vector3(0, 0.6f, -0.375f);
            lid.AddComponent<MeshFilter>().sharedMesh = lmb.ToMesh("poi_lid");
            lid.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            p.lid = lid.transform;
            WorldCollision.AddObstacle(p.chestPos, 0.7f, p.go);
        }

        void BuildTablet(Poi p, Quaternion rot)
        {
            var mb = new MeshBuilder();
            Color32 glow = Color.Lerp(BiomeInfo.Tint(p.biome), Color.white, 0.3f);
            if (p.kind == PoiKind.Camp || p.kind == PoiKind.Shipwreck)
            {
                // a book
                mb.Box(M(0, 0.06f, 0, 0.6f, 0.1f, 0.44f, 0, 20f, 0), C("#7a3a2a"), C("#9a4a36"));
                mb.Box(M(0, 0.12f, 0, 0.54f, 0.04f, 0.38f, 0, 20f, 0), C("#f0e8d0"));
            }
            else
            {
                // a leaning slab with a glowing line of script
                mb.Box(M(0, 0.55f, 0, 1.1f, 1.2f, 0.22f, -14f, 0, 0), C("#7c7a78"), C("#9a9894"));
                mb.Box(M(0, 0.1f, 0.05f, 1.4f, 0.2f, 0.6f), C("#6a6866"));
                for (int i = 0; i < 3; i++) mb.Box(M(0, 0.82f - i * 0.24f, 0.07f + i * 0.06f, 0.8f - i * 0.12f, 0.07f, 0.03f, -14f, 0, 0), glow);
            }
            var go = new GameObject("Tablet");
            go.transform.SetParent(p.go.transform, false);
            go.transform.SetPositionAndRotation(p.tabletPos, rot * Quaternion.Euler(0, 180f, 0));
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("poi_tablet");
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
        }

        // ================================================================ RUNTIME
        public void Tick(float dt, PlayerController pl)
        {
            checkTimer -= dt;
            if (checkTimer > 0f) return;
            checkTimer = 0.5f;
            if (Caves.InCave) return;
            Vector3 pp = pl.Position;
            var st = pl.Stats;
            foreach (var p in All)
            {
                float d2 = Flat2(p.pos, pp);
                bool show = d2 < 380f * 380f;
                if (p.kind != PoiKind.Lookout && p.go.activeSelf != show) p.go.SetActive(show);   // lookouts are landmarks: Landmarks.Cull owns them
                if (!show) continue;
                if (p.lid != null && Looted(p)) p.lid.localRotation = Quaternion.Euler(-100f, 0, 0);
                if (d2 < 48f * 48f && !st.poisFound.Contains(p.id))
                {
                    st.poisFound.Add(p.id);
                    GameEvents.Good(p.kind == PoiKind.Lookout ? $"A lookout tower. Climb it (LOOK OUT) to mark what lies around." : $"You come across {KindName(p.kind)}.");
                    SynthAudio.Play(Sfx.UiOpen, 1f);
                }
            }
        }

        /// <summary>OPEN / READ / LOOK OUT under the player, or null.</summary>
        public string Prompt(Vector3 pp, out System.Action act)
        {
            act = null;
            foreach (var p in All)
            {
                if (Flat2(p.pos, pp) > 16f * 16f) continue;
                var poi = p;
                if (p.kind == PoiKind.Lookout)
                {
                    if (Flat2(p.pos, pp) < 4.2f * 4.2f) { act = () => LookOut(poi); return "LOOK OUT"; }
                    continue;
                }
                if (p.hasChest && !Looted(p) && Flat2(p.chestPos, pp) < 2.4f * 2.4f) { act = () => Open(poi); return "OPEN"; }
                if (p.hasTablet && Flat2(p.tabletPos, pp) < 2.6f * 2.6f) { act = () => ReadTablet(poi); return "READ"; }
            }
            return null;
        }

        void Open(Poi p)
        {
            if (Looted(p)) return;
            var gm = GameManager.I;
            S.poisLooted.Add(p.id);
            Vector3 drop = p.chestPos + Vector3.up;
            int tier = ExplorerLog.Tier(p.biome);
            int glim = Mathf.RoundToInt((25 + tier * 18) * Random.Range(0.85f, 1.3f));
            S.glimmer += glim;
            gm.GiveItem(ExplorerLog.LandSeed(p.biome), Random.Range(2, 5), drop);
            switch (p.kind)
            {
                case PoiKind.Shipwreck: gm.GiveItem("ingot", Random.Range(1, 4), drop); gm.GiveItem("fiber", Random.Range(3, 7), drop); break;
                case PoiKind.Camp: gm.GiveItem("salve", Random.Range(1, 3), drop); gm.GiveItem("berry", Random.Range(3, 7), drop); break;
                case PoiKind.Skeleton: gm.GiveItem("pelt", Random.Range(2, 5), drop); break;
                default: gm.GiveItem(tier >= 3 ? "crystal" : "ore", Random.Range(2, 5), drop); break;
            }
            string own = ExplorerLog.RegionalItem(p.biome);
            if (own != null && Random.value < 0.6f) gm.GiveItem(own, Random.Range(1, 3), drop);
            gm.AddPlayerXp(30);
            GameEvents.Say($"+{glim} Glimmer", new Color(1f, 0.85f, 0.4f));
            Fx.Burst(drop, new Color(1f, 0.9f, 0.5f), 18, 5f, 0.3f, 0.7f, true);
            SynthAudio.Play(Sfx.CaptureSuccess, 1.2f);
            GameEvents.RaiseInventoryChanged();
        }

        void ReadTablet(Poi p)
        {
            if (!Read(p))
            {
                S.poisRead.Add(p.id);
                GameManager.I.AddPlayerXp(p.kind == PoiKind.Statue ? 40 : 20);
            }
            MenuUI.I.OpenLore(p);
        }

        void LookOut(Poi p)
        {
            var st = S;
            bool first = !st.lookouts.Contains(p.id);
            if (first) st.lookouts.Add(p.id);
            float r2 = LookRadius * LookRadius;
            int caves = 0, stones = 0, places = 0, finds = 0;
            if (Caves.I != null)
                foreach (var e in Caves.I.Entrances)
                    if (Flat2(e.pos, p.pos) < r2 && !st.cavesFound.Contains(e.id)) { st.cavesFound.Add(e.id); caves++; }
            if (Waystones.I != null)
                foreach (var w in Waystones.I.All)
                    if (Flat2(w.pos, p.pos) < r2 && !st.waystonesFound.Contains(w.id)) { st.waystonesFound.Add(w.id); stones++; }
            foreach (var s in SubRegions.All)
                if (s.strength > 0f && Flat2(s.Pos, p.pos) < (LookRadius + s.radius * 0.5f) * (LookRadius + s.radius * 0.5f) && !st.regionsFound.Contains(s.id)) { st.regionsFound.Add(s.id); places++; }
            foreach (var o in All)
                if (o != p && Flat2(o.pos, p.pos) < r2 && !st.poisFound.Contains(o.id)) { st.poisFound.Add(o.id); finds++; }
            // relics are never shown from up here (user, 2026-10-22): the "You sense a ... relic nearby" line close by is the only help
            var parts = new List<string>();
            if (places > 0) parts.Add(places == 1 ? "a named place" : places + " named places");
            if (caves > 0) parts.Add(caves == 1 ? "a way underground" : caves + " ways underground");
            if (stones > 0) parts.Add(stones == 1 ? "a waystone" : stones + " waystones");
            if (finds > 0) parts.Add(finds == 1 ? "something worth a look" : finds + " things worth a look");
            if (first) GameManager.I.AddPlayerXp(40);
            if (parts.Count == 0) GameEvents.Say(first ? "A fine view, but nothing you had not already found." : "Nothing new from up here.", new Color(0.9f, 0.9f, 0.75f));
            else GameEvents.Good("From the top you mark " + string.Join(", ", parts) + ". They are on your map.");
            SynthAudio.Play(Sfx.LevelUp, 1.2f);
            if (parts.Count > 0) MenuUI.I.Open(MenuTab.Map);
        }
    }
}
