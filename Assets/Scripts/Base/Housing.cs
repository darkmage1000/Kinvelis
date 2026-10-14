using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    public enum HouseKind { Floor, Wall, Doorway, Window, Roof, RoofSlope, Furniture, Stairs }

    [Serializable] public class HouseSave { public string id; public int cx, cz, side, rot, level; public float x, y, z; }

    public class HousePieceDef
    {
        public string id, name, desc;
        public HouseKind kind;
        public Ingredient[] cost;
        public float radius;          // furniture: collision circle
        public bool stone, bed, wardrobe, lamp;
        /// <summary>Look variant of a wall / roof / floor / window ("bamboo", "adobe", "log", "dark", "thatch", "reed", "sandstone", "crystal").</summary>
        public string style;
        /// <summary>Found as a blueprint in this land's chests (Chests.Blueprints, id "house:" + piece id).</summary>
        public bool blueprint;
        public BiomeId land;
        /// <summary>Kin play piece: idle camp kin go and play on it and come back relaxed (CreatureActor.CampLife).</summary>
        public bool play;
        /// <summary>Furniture that may also stand on open ground in the camp ring (play pieces, planters).</summary>
        public bool ground;
        /// <summary>A self-lit glow (lamps, fires): where, how big, what colour.</summary>
        public Vector3 glowAt; public float glowSize; public Color32 glowCol;
        /// <summary>A camp kin sleeps on it at night (BaseManager.Beds): `bed` is also yours to SLEEP in, `kinBed` is for kin only. `bedSize` = the biggest kin it takes.</summary>
        public bool kinBed; public float bedSize;
        /// <summary>A doorway's opening (0 = the ordinary 1.2 m door): wider ones let bigger kin in. `tall` = it fills two storeys of its edge.</summary>
        public float gap; public bool tall;
    }

    /// <summary>
    /// Your house, piece by piece (user, 2026-10-14: "we need player house building, i just realized i cant sleep
    /// thru the night" - chose snap-together pieces). Pieces snap to a world grid of `Cell` 3 m squares inside the
    /// camp ring: floors fill a square (their top becomes the ground there - `FloorAt` feeds `Ground.Height`), walls,
    /// doorways and window walls sit on the edge between two squares (`side` 0 = the north edge of (cx, cz), 1 = its
    /// east edge) and need a floor beside them, roofs cover the top floor of a square at wall height, furniture snaps
    /// to a 0.5 m grid on a floor. Collision: wall segments (`PushOut`, called from `WorldCollision.PushOut`; a doorway
    /// leaves a 1.2 m gap) and furniture circles (ordinary obstacles, banded to their storey). While you are under a
    /// roof or an upper floor, everything above you nearby and any wall between the camera and you are hidden, and
    /// doors swing open when someone comes near. A bed with a roof over it lets you SLEEP (GameManager.Sleep); camp
    /// kin sleep in beds and Kin Baskets at night (BaseManager.Beds). Saved as `SaveData.house`.
    ///
    /// Storeys (2026-10-14): every piece has a `level` 0-2. A floor laid over a floor (from below or from upstairs)
    /// is an upper floor `LevelH` higher; walls, furniture and stairs go on the storey you stand on (`LevelAt`).
    /// Stairs fill a square, rise one storey toward `rot` and leave a stairwell (no floor above them, a railing
    /// round it). The ground floor is still `FloorAt` for everything; walkers that know their height use
    /// `SurfaceAt` (`Ground.HeightNear`), which adds upper floors and stair ramps within `StepUp` of their feet.
    /// Kin find their way round walls, through doorways and up stairs with `Steer` (Housing.Route.cs).
    ///
    /// Room for big kin (2026-10-14): three door sizes (`gap`: Doorway 1.2 m for small kin, Wide Doorway 1.6 m double
    /// doors for medium, Grand Archway 2.7 m and two storeys tall for large - `MaxRadius`), walls stack on walls with
    /// no floor beside them (tall halls), a roof sits on the highest wall round its square (`RoofY`), and Big / Grand
    /// Kin Beds take medium and large sleepers.
    /// </summary>
    public partial class Housing : MonoBehaviour
    {
        public static Housing I { get; private set; }
        public const float Cell = 3f, WallH = 3f, WallT = 0.22f, DoorGap = 1.2f;
        /// <summary>One storey: a wall and the floor slab on top of it.</summary>
        public const float LevelH = WallH + 0.15f, UpperDepth = 0.12f;
        public const float StairW = 2f, StepUp = 0.7f;
        public const int MaxLevel = 3;   // four storeys of floors, or walls stacked four high

        // ============================================================ PIECES
        static Ingredient[] C(params (string, int)[] c) { var r = new Ingredient[c.Length]; for (int i = 0; i < c.Length; i++) r[i] = new Ingredient(c[i].Item1, c[i].Item2); return r; }

        public static readonly List<HousePieceDef> Pieces = new List<HousePieceDef>
        {
            new HousePieceDef { id = "house_floor", name = "Wooden Floor", kind = HouseKind.Floor, cost = C(("wood", 8), ("stone", 4)), desc = "A 3 m square of planks on a stone footing. Everything else stands on floors. Laid over another floor it makes an upper storey (up to three)." },
            new HousePieceDef { id = "house_wall", name = "Wooden Wall", kind = HouseKind.Wall, cost = C(("wood", 6)), desc = "A plank wall along one edge of a floor. Facing a wall that is already there puts another on top of it - tall rooms for big kin." },
            new HousePieceDef { id = "house_wall_stone", name = "Stone Wall", kind = HouseKind.Wall, stone = true, cost = C(("stone", 10), ("wood", 2)), desc = "A sturdier-looking wall of fitted stone." },
            new HousePieceDef { id = "house_door", name = "Doorway", kind = HouseKind.Doorway, cost = C(("wood", 8)), desc = "A wall with a door that swings open as you come near. Small kin fit through." },
            new HousePieceDef { id = "house_door_wide", name = "Wide Doorway", kind = HouseKind.Doorway, gap = 1.6f, cost = C(("wood", 12), ("fiber", 2)), desc = "Double doors almost a storey tall: medium-sized kin fit through." },
            new HousePieceDef { id = "house_arch_grand", name = "Grand Archway", kind = HouseKind.Doorway, gap = 2.7f, tall = true, cost = C(("wood", 16), ("stone", 16)), desc = "An open arch two storeys tall that fills a whole edge: any kin fits through. Stack walls on walls beside it for a hall big kin can stand up in." },
            new HousePieceDef { id = "house_window", name = "Window Wall", kind = HouseKind.Window, cost = C(("wood", 6), ("fiber", 2)), desc = "A wall with a window in it." },
            new HousePieceDef { id = "house_roof", name = "Flat Roof", kind = HouseKind.Roof, cost = C(("wood", 6), ("fiber", 4)), desc = "A roof over the top floor of one square. A bed needs a roof (or a floor) over it to sleep in." },
            new HousePieceDef { id = "house_ceiling", name = "Flat Ceiling", kind = HouseKind.Roof, style = "ceiling", cost = C(("wood", 6)), desc = "A flat plank ceiling with beams under it. It goes up to the top of the room's walls however high you stacked them, so laid over every square of a room it makes one flat ceiling." },
            new HousePieceDef { id = "house_roof_slope", name = "Sloped Roof", kind = HouseKind.RoofSlope, cost = C(("wood", 6), ("fiber", 4)), desc = "A roof that slopes one way (ROTATE turns it). Two back to back make a ridge." },
            new HousePieceDef { id = "house_stairs", name = "Wooden Stairs", kind = HouseKind.Stairs, cost = C(("wood", 14), ("stone", 2)), desc = "Stairs up one storey, on a floor; they climb the way you face (ROTATE turns them). Lay floors beside the top for an upstairs. Camp kin use them too." },
            new HousePieceDef { id = "house_bed", name = "Bed", kind = HouseKind.Furniture, bed = true, bedSize = 1.2f, radius = 0.75f, cost = C(("wood", 10), ("fiber", 12), ("pelt", 2)), desc = "SLEEP here from evening on to skip to morning (needs a roof over it). The camp keeps working while you sleep. At night a small camp kin curls up on it too." },
            new HousePieceDef { id = "kin_basket", name = "Kin Basket", kind = HouseKind.Furniture, kinBed = true, bedSize = 1.1f, radius = 0.45f, cost = C(("fiber", 8), ("wood", 3), ("pelt", 1)), desc = "A cushioned basket a small camp kin sleeps in at night. Under a roof it sleeps snug and works 5% faster." },
            new HousePieceDef { id = "kin_bed_big", name = "Big Kin Bed", kind = HouseKind.Furniture, kinBed = true, bedSize = 1.7f, radius = 0.8f, cost = C(("fiber", 16), ("wood", 6), ("pelt", 3)), desc = "A deep round cushion for a medium-sized camp kin (in through a Wide Doorway). Under a roof: snug, +5% work." },
            new HousePieceDef { id = "kin_bed_grand", name = "Grand Kin Bed", kind = HouseKind.Furniture, kinBed = true, bedSize = 99f, radius = 1.25f, cost = C(("fiber", 30), ("wood", 12), ("pelt", 6)), desc = "A huge nest of cushions any kin can sleep on - big ones come in through a Grand Archway. Under a roof: snug, +5% work." },
            new HousePieceDef { id = "house_table", name = "Table", kind = HouseKind.Furniture, radius = 0.6f, cost = C(("wood", 6)), desc = "A sturdy table." },
            new HousePieceDef { id = "house_chair", name = "Chair", kind = HouseKind.Furniture, radius = 0.3f, cost = C(("wood", 3)), desc = "A chair." },
            new HousePieceDef { id = "house_lamp", name = "Lamp", kind = HouseKind.Furniture, lamp = true, radius = 0.25f, cost = C(("wood", 2), ("gel", 1)), desc = "A standing lamp that glows.", glowAt = new Vector3(0, 1.48f, 0), glowSize = 0.26f, glowCol = new Color32(255, 235, 160, 0) },
            new HousePieceDef { id = "house_rug", name = "Rug", kind = HouseKind.Furniture, radius = 0f, cost = C(("fiber", 6)), desc = "A woven rug. You can walk over it." },
            new HousePieceDef { id = "house_shelf", name = "Bookshelf", kind = HouseKind.Furniture, radius = 0.45f, cost = C(("wood", 8)), desc = "Shelves of books and keepsakes." },
            new HousePieceDef { id = "house_wardrobe", name = "Wardrobe", kind = HouseKind.Furniture, wardrobe = true, radius = 0.45f, cost = C(("wood", 10), ("fiber", 4)), desc = "CHANGE LOOK here: your explorer's body, hair and outfit." },
            new HousePieceDef { id = "kin_scratch_post", name = "Scratching Post", kind = HouseKind.Furniture, play = true, ground = true, radius = 0.35f, cost = C(("wood", 4), ("fiber", 6)), desc = "Kin play piece: idle camp kin come and play, and go back to work relaxed (+25% work for 5 minutes)." },

            // ---- blueprints from each land's chests: a style, a decoration and a kin play piece
            BP(BiomeId.Meadow, new HousePieceDef { id = "house_roof_thatch", name = "Thatched Roof", kind = HouseKind.RoofSlope, style = "thatch", cost = C(("fiber", 10), ("wood", 3)), desc = "A sloped roof of golden straw." }),
            BP(BiomeId.Meadow, new HousePieceDef { id = "house_flowerbox", name = "Flower Box", kind = HouseKind.Furniture, ground = true, radius = 0.3f, cost = C(("wood", 3), ("berry", 4)), desc = "A planter full of meadow flowers." }),
            BP(BiomeId.Meadow, new HousePieceDef { id = "kin_play_mat", name = "Play Mat", kind = HouseKind.Furniture, play = true, ground = true, radius = 0f, cost = C(("fiber", 10), ("pelt", 2)), desc = "Kin play piece: a patchwork mat with balls to bat about." }),
            BP(BiomeId.Isles, new HousePieceDef { id = "house_wall_bamboo", name = "Bamboo Wall", kind = HouseKind.Wall, style = "bamboo", cost = C(("wood", 5), ("fiber", 3)), desc = "A breezy wall of lashed bamboo poles." }),
            BP(BiomeId.Isles, new HousePieceDef { id = "house_shell_lamp", name = "Shell Lamp", kind = HouseKind.Furniture, lamp = true, radius = 0.25f, cost = C(("wood", 2), ("gel", 2)), desc = "A glowing seashell on a driftwood stand.", glowAt = new Vector3(0, 1.0f, 0), glowSize = 0.3f, glowCol = new Color32(255, 190, 210, 0) }),
            BP(BiomeId.Isles, new HousePieceDef { id = "kin_splash_pool", name = "Splash Pool", kind = HouseKind.Furniture, play = true, ground = true, radius = 0.9f, cost = C(("stone", 10), ("gel", 3)), desc = "Kin play piece: a shallow pool to splash in." }),
            BP(BiomeId.Badlands, new HousePieceDef { id = "house_wall_adobe", name = "Adobe Wall", kind = HouseKind.Wall, style = "adobe", cost = C(("stone", 6), ("wood", 2)), desc = "A smooth clay wall with beam ends." }),
            BP(BiomeId.Badlands, new HousePieceDef { id = "house_fireplace", name = "Fireplace", kind = HouseKind.Furniture, lamp = true, radius = 0.6f, cost = C(("stone", 12), ("cinder", 2)), desc = "A stone hearth with a fire that never goes out.", glowAt = new Vector3(0, 0.38f, 0.12f), glowSize = 0.34f, glowCol = new Color32(255, 140, 50, 0) }),
            BP(BiomeId.Badlands, new HousePieceDef { id = "kin_warm_rocks", name = "Warm Rock Bed", kind = HouseKind.Furniture, play = true, ground = true, radius = 0.8f, cost = C(("stone", 12), ("cinder", 2)), desc = "Kin play piece: sun-warmed stones to bask on." }),
            BP(BiomeId.Peaks, new HousePieceDef { id = "house_wall_log", name = "Log Cabin Wall", kind = HouseKind.Wall, style = "log", cost = C(("wood", 9)), desc = "Round logs stacked the old way." }),
            BP(BiomeId.Peaks, new HousePieceDef { id = "house_fur_rug", name = "Fur Rug", kind = HouseKind.Furniture, radius = 0f, cost = C(("pelt", 4)), desc = "A thick white fur rug." }),
            BP(BiomeId.Peaks, new HousePieceDef { id = "kin_snow_slide", name = "Snow Slide", kind = HouseKind.Furniture, play = true, ground = true, radius = 0.9f, cost = C(("wood", 8), ("crystal", 1)), desc = "Kin play piece: a little slide that is always slippery." }),
            BP(BiomeId.Swamp, new HousePieceDef { id = "house_roof_reed", name = "Reed Roof", kind = HouseKind.RoofSlope, style = "reed", cost = C(("fiber", 8), ("wood", 3)), desc = "A sloped roof of bundled marsh reeds." }),
            BP(BiomeId.Swamp, new HousePieceDef { id = "house_lily_planter", name = "Lily Planter", kind = HouseKind.Furniture, ground = true, radius = 0.4f, cost = C(("wood", 3), ("gel", 2)), desc = "A little tub of water lilies." }),
            BP(BiomeId.Swamp, new HousePieceDef { id = "kin_mud_wallow", name = "Mud Wallow", kind = HouseKind.Furniture, play = true, ground = true, radius = 0f, cost = C(("stone", 6), ("gel", 2)), desc = "Kin play piece: lovely, squelchy mud." }),
            BP(BiomeId.Desert, new HousePieceDef { id = "house_floor_sandstone", name = "Sandstone Floor", kind = HouseKind.Floor, style = "sandstone", cost = C(("stone", 10)), desc = "A floor of warm sandstone tiles." }),
            BP(BiomeId.Desert, new HousePieceDef { id = "house_tapestry", name = "Tapestry Stand", kind = HouseKind.Furniture, radius = 0.25f, cost = C(("wood", 3), ("fiber", 6)), desc = "A woven hanging in desert colours." }),
            BP(BiomeId.Desert, new HousePieceDef { id = "kin_sand_pit", name = "Sand Pit", kind = HouseKind.Furniture, play = true, ground = true, radius = 0f, cost = C(("wood", 6), ("stone", 4)), desc = "Kin play piece: a box of soft sand to dig in." }),
            BP(BiomeId.Blight, new HousePieceDef { id = "house_wall_dark", name = "Dark Timber Wall", kind = HouseKind.Wall, style = "dark", cost = C(("wood", 7)), desc = "Blackened planks with iron straps." }),
            BP(BiomeId.Blight, new HousePieceDef { id = "house_spooky_lantern", name = "Ghost Lantern", kind = HouseKind.Furniture, lamp = true, radius = 0.25f, cost = C(("wood", 2), ("gel", 2)), desc = "A hanging lantern with an eerie green flame.", glowAt = new Vector3(0.32f, 1.25f, 0), glowSize = 0.2f, glowCol = new Color32(150, 255, 140, 0) }),
            BP(BiomeId.Blight, new HousePieceDef { id = "kin_climb_tree", name = "Climbing Tree", kind = HouseKind.Furniture, play = true, ground = true, radius = 0.5f, cost = C(("wood", 12), ("fiber", 4)), desc = "Kin play piece: a twisty tree with platforms to scramble up." }),
            BP(BiomeId.Glow, new HousePieceDef { id = "house_window_crystal", name = "Crystal Window", kind = HouseKind.Window, style = "crystal", cost = C(("wood", 5), ("crystal", 2)), desc = "A window glazed with glowing crystal." }),
            BP(BiomeId.Glow, new HousePieceDef { id = "house_glowshroom", name = "Glowshroom Lamp", kind = HouseKind.Furniture, lamp = true, ground = true, radius = 0.3f, cost = C(("wood", 1), ("gel", 3)), desc = "A cluster of glowing mushrooms in a pot.", glowAt = new Vector3(0, 0.85f, 0), glowSize = 0.34f, glowCol = new Color32(110, 240, 255, 0) }),
            BP(BiomeId.Glow, new HousePieceDef { id = "kin_ball_pit", name = "Glow Ball Pit", kind = HouseKind.Furniture, play = true, ground = true, radius = 0.9f, cost = C(("wood", 8), ("gel", 4)), desc = "Kin play piece: a pit of glowing balls to dive into." }),
        };

        static HousePieceDef BP(BiomeId land, HousePieceDef d) { d.blueprint = true; d.land = land; return d; }

        /// <summary>The id a house blueprint is stored under in PlayerStats.blueprints.</summary>
        public static string BlueprintId(HousePieceDef d) => "house:" + d.id;
        public static bool Locked(HousePieceDef d) => d.blueprint && !Dev.FreeAll && GameManager.I != null && !GameManager.I.Player.Stats.blueprints.Contains(BlueprintId(d));

        /// <summary>The kin play pieces in camp (CreatureActor.CampLife sends idle kin to them).</summary>
        public IEnumerable<Piece> PlayPieces { get { foreach (var f in furniture) if (f.def.play) yield return f; } }
        /// <summary>Beds and baskets camp kin can sleep on (BaseManager.Beds).</summary>
        public IEnumerable<Piece> KinBeds { get { foreach (var f in furniture) if (f.def.bed || f.def.kinBed) yield return f; } }
        /// <summary>How high a sleeper lies on it.</summary>
        public static float SleepTop(HousePieceDef d) => d.bed ? 0.5f : d.bedSize > 2f ? 0.3f : d.bedSize > 1.2f ? 0.25f : 0.2f;
        /// <summary>A doorway's opening (0 for a wall or window).</summary>
        public static float GapOf(HousePieceDef d) => d.kind != HouseKind.Doorway ? 0f : d.gap > 0f ? d.gap : DoorGap;
        /// <summary>The widest walker (its collision radius) that gets through.</summary>
        public static float MaxRadius(HousePieceDef d) => GapOf(d) * 0.5f - WallT * 0.5f - 0.04f;
        /// <summary>The top of an edge piece (a Grand Archway is two storeys).</summary>
        public static float PieceTop(Piece p) => p.Y + (p.def.tall ? LevelH : 0f) + WallH;

        public static HousePieceDef Def(string id) { foreach (var p in Pieces) if (p.id == id) return p; return null; }

        public class Piece
        {
            static int counter;
            public readonly int id = ++counter;
            public HousePieceDef def;
            public int cx, cz, side, rot, level;
            public Vector3 pos;
            public GameObject go;
            public Obstacle obs;
            public Transform door, door2;
            public float doorOpen;
            public Renderer[] rends;
            public Renderer rail;      // stairs: the railing round the stairwell up top
            public bool hidden, railHidden, removed;
            public float Y => pos.y;
        }

        readonly Dictionary<long, Piece> floors = new Dictionary<long, Piece>();      // LKey(square, storey)
        readonly Dictionary<long, Piece> roofs = new Dictionary<long, Piece>();       // Key(square): one roof per square, over its top floor
        readonly Dictionary<long, Piece>[] edges = { new Dictionary<long, Piece>(), new Dictionary<long, Piece>() };   // LKey(square, storey) per side
        readonly Dictionary<long, Piece> stairs = new Dictionary<long, Piece>();      // LKey(square, storey they start on)
        readonly List<Piece> furniture = new List<Piece>();
        public int Count => floors.Count + roofs.Count + edges[0].Count + edges[1].Count + stairs.Count + furniture.Count;
        /// <summary>Goes up whenever anything is built or taken down (route and reachability caches watch it).</summary>
        public int Version { get; private set; }

        public static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
        /// <summary>A square on a storey (storey 0 is the same key as the square).</summary>
        public static long LKey(int cx, int cz, int level) => Key(cx, cz) ^ ((long)level << 58);
        public static int CellOf(float v) => Mathf.FloorToInt(v / Cell);
        public static Vector3 CellCenter(int cx, int cz) => new Vector3((cx + 0.5f) * Cell, 0f, (cz + 0.5f) * Cell);
        static readonly int[] DX = { 0, 1, 0, -1 }, DZ = { 1, 0, -1, 0 };   // north, east, south, west (= stairs rot)

        // ---- floors as ground: published as fresh dictionaries on every change, so any thread can read them
        struct Surf { public float y; public int rot; public bool stair; }
        static volatile Dictionary<long, float> floorSnap = new Dictionary<long, float>();
        static volatile Dictionary<long, Surf[]> upperSnap = new Dictionary<long, Surf[]>();
        /// <summary>The top of a ground-floor floor at this point, or -infinity (Ground.Height takes the higher of this and the terrain).</summary>
        public static float FloorAt(float x, float z) => floorSnap.TryGetValue(Key(CellOf(x), CellOf(z)), out var y) ? y : float.NegativeInfinity;

        /// <summary>
        /// What a walker whose feet are at height y stands on here: the ground floor, or an upper floor or a stair it can
        /// step onto (no more than `StepUp` above its feet - so someone downstairs walks under the floor above). -infinity = nothing built.
        /// </summary>
        public static float SurfaceAt(float x, float z, float y)
        {
            int cx = CellOf(x), cz = CellOf(z);
            long k = Key(cx, cz);
            float h = floorSnap.TryGetValue(k, out var g) ? g : float.NegativeInfinity;
            if (!upperSnap.TryGetValue(k, out var arr)) return h;
            foreach (var s in arr)
            {
                float top = s.y;
                if (s.stair)
                {
                    float u = x - (cx + 0.5f) * Cell, v = z - (cz + 0.5f) * Cell;
                    float along = s.rot == 0 ? v : s.rot == 1 ? u : s.rot == 2 ? -v : -u;
                    float across = s.rot == 0 || s.rot == 2 ? u : v;
                    if (Mathf.Abs(across) > StairW * 0.5f) continue;
                    top = s.y + Mathf.Clamp01(along / Cell + 0.5f) * LevelH;
                }
                if (top <= y + StepUp && top > h) h = top;
            }
            return h;
        }

        void Publish()
        {
            var d = new Dictionary<long, float>();
            var u = new Dictionary<long, List<Surf>>();
            void Up(long k, Surf s) { if (!u.TryGetValue(k, out var l)) u[k] = l = new List<Surf>(); l.Add(s); }
            foreach (var f in floors.Values)
            {
                if (f.level == 0) d[Key(f.cx, f.cz)] = f.Y;
                else Up(Key(f.cx, f.cz), new Surf { y = f.Y });
            }
            foreach (var s in stairs.Values) Up(Key(s.cx, s.cz), new Surf { y = s.Y, rot = s.rot, stair = true });
            var arrs = new Dictionary<long, Surf[]>();
            foreach (var kv in u) arrs[kv.Key] = kv.Value.ToArray();
            floorSnap = d; upperSnap = arrs;
        }

        // ---- lookups by square and storey
        public bool FloorTop(int cx, int cz, int level, out float y)
        {
            if (floors.TryGetValue(LKey(cx, cz, level), out var f)) { y = f.Y; return true; }
            y = 0f; return false;
        }
        Piece StairsAt(int cx, int cz, int level) => stairs.TryGetValue(LKey(cx, cz, level), out var s) ? s : null;
        Piece EdgeAt(int cx, int cz, int dir, int level)
        {
            // the edge on side `dir` (0 north, 1 east, 2 south, 3 west) of a square
            EdgeOf(cx, cz, dir, out int x, out int z, out int side);
            return EdgePiece(x, z, side, level);
        }
        /// <summary>What stands on an edge at a storey: a piece of that storey, or a two-storey archway from the one below.</summary>
        Piece EdgePiece(int ex, int ez, int side, int level)
        {
            if (edges[side].TryGetValue(LKey(ex, ez, level), out var p)) return p;
            if (level > 0 && edges[side].TryGetValue(LKey(ex, ez, level - 1), out p) && p.def.tall) return p;
            return null;
        }
        bool EdgeTaken(int ex, int ez, int side, int level) => EdgePiece(ex, ez, side, level) != null || pending.Contains($"E{level}:{side}:{ex},{ez}");
        /// <summary>An edge piece with no floor beside it at its storey: it stands on the wall below (a tall room).</summary>
        bool Stacked(Piece p) => p.level > 0 && float.IsNegativeInfinity(EdgeBase(p.cx, p.cz, p.side, p.level));
        /// <summary>The edge a direction leads over, as (cx, cz, side) the way walls are keyed.</summary>
        static void EdgeOf(int cx, int cz, int dir, out int ex, out int ez, out int side)
        {
            side = dir == 0 || dir == 2 ? 0 : 1;
            ex = dir == 3 ? cx - 1 : cx; ez = dir == 2 ? cz - 1 : cz;
        }

        /// <summary>
        /// Which storey a point is on: the stairs it is on (the storey they start from), else the highest upper floor
        /// at or just under its feet, else 0 (the ground floor or open ground).
        /// </summary>
        public int LevelAt(Vector3 p)
        {
            int cx = CellOf(p.x), cz = CellOf(p.z);
            for (int l = MaxLevel - 1; l >= 0; l--)
            {
                var st = StairsAt(cx, cz, l);
                if (st != null && p.y > st.Y - 0.6f && p.y < st.Y + LevelH + 0.6f) return l;
            }
            for (int l = MaxLevel; l >= 1; l--)
                if (FloorTop(cx, cz, l, out float y) && y <= p.y + 1f) return l;
            return 0;
        }

        // ---- wall segments, indexed by the squares beside them
        struct Seg { public Vector2 a, b; public float lo, hi; }
        readonly Dictionary<long, List<Seg>> segs = new Dictionary<long, List<Seg>>();

        public void Init() { I = this; floorSnap = new Dictionary<long, float>(); upperSnap = new Dictionary<long, Surf[]>(); }

        // ---- construction sites (BaseManager.Construction) hold their slot until they are built
        readonly HashSet<string> pending = new HashSet<string>();
        readonly Dictionary<long, float> pendingFloors = new Dictionary<long, float>();
        static string SlotKey(HousePieceDef d, Spot s) =>
            d.kind == HouseKind.Floor ? $"F{s.level}:{s.cx},{s.cz}" : d.kind == HouseKind.Roof || d.kind == HouseKind.RoofSlope ? $"R{s.cx},{s.cz}" :
            d.kind == HouseKind.Stairs ? $"S{s.level}:{s.cx},{s.cz}" :
            d.kind == HouseKind.Furniture ? $"U{s.pos.x:0.0},{s.pos.y:0},{s.pos.z:0.0}" : $"E{s.level}:{s.side}:{s.cx},{s.cz}";
        public void Reserve(HousePieceDef d, Spot s) { pending.Add(SlotKey(d, s)); if (d.kind == HouseKind.Floor) pendingFloors[LKey(s.cx, s.cz, s.level)] = s.pos.y; }
        public void Unreserve(HousePieceDef d, Spot s) { pending.Remove(SlotKey(d, s)); if (d.kind == HouseKind.Floor) pendingFloors.Remove(LKey(s.cx, s.cz, s.level)); }
        bool Pending(HousePieceDef d, Spot s) => pending.Contains(SlotKey(d, s));
        bool FloorOrSite(int cx, int cz, int level, out float y)
        {
            if (floors.TryGetValue(LKey(cx, cz, level), out var f)) { y = f.Y; return true; }
            return pendingFloors.TryGetValue(LKey(cx, cz, level), out y);
        }
        bool StairsOrSite(int cx, int cz, int level) => StairsAt(cx, cz, level) != null || pending.Contains($"S{level}:{cx},{cz}");
        bool RoofOrSite(int cx, int cz) => roofs.ContainsKey(Key(cx, cz)) || pending.Contains($"R{cx},{cz}");
        /// <summary>The highest storey with a floor (or a floor being built) on this square, or -1.</summary>
        int TopFloor(int cx, int cz) { int t = -1; for (int l = 0; l <= MaxLevel; l++) if (FloorOrSite(cx, cz, l, out _)) t = l; else break; return t; }

        // ============================================================ PLACING
        public struct Spot { public bool ok; public string reason; public int cx, cz, side, rot, level; public Vector3 pos; public Quaternion q; public float depth; }

        /// <summary>Where a piece would go for a player standing at p facing fwd (turn = ROTATE presses).</summary>
        public Spot Snap(HousePieceDef def, Vector3 p, Vector3 fwd, int turn)
        {
            var s = new Spot { ok = false };
            var bm = BaseManager.I;
            fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            float reach = def.kind == HouseKind.Floor || def.kind == HouseKind.Stairs ? 3.2f : def.kind == HouseKind.Furniture ? 1.7f : def.kind == HouseKind.Roof || def.kind == HouseKind.RoofSlope ? 2.6f : 2.1f;
            Vector3 t = p + fwd * reach;
            int cx = CellOf(t.x), cz = CellOf(t.z);
            int lv = LevelAt(p);
            s.cx = cx; s.cz = cz; s.level = lv;
            s.rot = ((turn % 4) + 4) % 4;
            string Fail(string why) { s.reason = why; return why; }
            if (bm == null || !bm.HasBase) { Fail("Place a Hearthstone first - houses go inside your camp"); }
            switch (def.kind)
            {
                case HouseKind.Floor:
                {
                    var c = CellCenter(cx, cz);
                    // the first storey up from yours with no floor yet: lay it on the ground, or over the floor below
                    int L = lv;
                    while (L <= MaxLevel && FloorOrSite(cx, cz, L, out _)) L++;
                    if (L >= 1)
                    {
                        s.level = Mathf.Min(L, MaxLevel);
                        bool below = FloorOrSite(cx, cz, s.level - 1, out float by);
                        s.pos = new Vector3(c.x, (below ? by : WorldGen.Height(c.x, c.z)) + LevelH, c.z); s.q = Quaternion.identity; s.depth = UpperDepth;
                        if (s.reason != null) break;
                        if (L > MaxLevel) { Fail("A house goes four storeys high at most"); break; }
                        if (!below) { Fail("Upper floors go over a floor below"); break; }
                        if (RoofOrSite(cx, cz)) { Fail("Take the roof off first"); break; }
                        if (StairsOrSite(cx, cz, L - 1)) { Fail("The stairs come up here"); break; }
                        s.ok = true;
                        break;
                    }
                    float hi = float.MinValue, lo = float.MaxValue;
                    for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) { float h = WorldGen.Height(c.x + i * Cell * 0.5f, c.z + j * Cell * 0.5f); hi = Mathf.Max(hi, h); lo = Mathf.Min(lo, h); }
                    float y = hi + 0.3f;
                    // flush with a neighbouring floor when the ground allows it
                    for (int k = 0; k < 4; k++)
                        if (floors.TryGetValue(LKey(cx + DX[k], cz + DZ[k], 0), out var nb) && nb.Y >= hi + 0.12f && nb.Y - hi < 1.6f) y = Mathf.Max(y, nb.Y);
                    s.level = 0;
                    s.pos = new Vector3(c.x, y, c.z); s.q = Quaternion.identity; s.depth = y - lo + 0.5f;
                    if (s.reason != null) break;
                    if (hi - lo > 2.2f) { Fail("Too steep here"); break; }
                    if (lo < WorldGen.WaterLevel + 0.15f) { Fail("Too wet here"); break; }
                    if (!bm.IsInsideBase(c, 1.5f)) { Fail("Inside your camp ring only"); break; }
                    if (WorldCollision.Overlaps(c, 1.45f, true)) { Fail("Something is in the way"); break; }
                    s.ok = true;
                    break;
                }
                case HouseKind.Wall:
                case HouseKind.Doorway:
                case HouseKind.Window:
                {
                    // the nearest edge of the square in front of you; ROTATE steps round its four edges
                    float u = t.x - cx * Cell, v = t.z - cz * Cell;
                    float[] d = { Cell - v, Cell - u, v, u };   // north, east, south, west
                    int best = 0; for (int i = 1; i < 4; i++) if (d[i] < d[best]) best = i;
                    int e = (best + s.rot) % 4;
                    EdgeOf(cx, cz, e, out int ex, out int ez, out int side);
                    s.cx = ex; s.cz = ez; s.side = side;
                    Vector3 mid = side == 0 ? new Vector3((ex + 0.5f) * Cell, 0f, (ez + 1) * Cell) : new Vector3((ex + 1) * Cell, 0f, (ez + 0.5f) * Cell);
                    // your storey, or the first one up where this edge is free: a wall facing a wall goes on top of it (tall rooms for big kin)
                    int L = lv;
                    while (L <= MaxLevel && EdgeTaken(ex, ez, side, L)) L++;
                    s.level = Mathf.Min(L, MaxLevel);
                    float y = EdgeBase(ex, ez, side, s.level);
                    if (float.IsNegativeInfinity(y) && s.level > 0)
                    {
                        var below = EdgePiece(ex, ez, side, s.level - 1);
                        if (below != null) y = below.Y + LevelH * (s.level - below.level);
                    }
                    s.pos = new Vector3(mid.x, float.IsNegativeInfinity(y) ? WorldGen.Height(mid.x, mid.z) : y, mid.z);
                    s.q = Quaternion.Euler(0f, side == 0 ? 0f : 90f, 0f);
                    if (s.reason != null) break;
                    if (L > MaxLevel) { Fail("Walls go four storeys high at most"); break; }
                    if (float.IsNegativeInfinity(y)) { Fail("Walls go on the edge of a floor, or on top of a wall"); break; }
                    if (def.tall && (s.level >= MaxLevel || EdgeTaken(ex, ez, side, s.level + 1))) { Fail("No room above for an archway two storeys tall"); break; }
                    if (s.level == lv && FurnitureNearSegment(mid, side, 0.15f, lv)) { Fail("Furniture is in the way"); break; }
                    string st = StairEdge(ex, ez, side, s.level);
                    if (st != null) { Fail(st); break; }
                    s.ok = true;
                    break;
                }
                case HouseKind.Roof:
                case HouseKind.RoofSlope:
                {
                    var c = CellCenter(cx, cz);
                    int top = TopFloor(cx, cz);
                    s.level = Mathf.Max(0, top);
                    bool hasFloor = FloorOrSite(cx, cz, s.level, out float fy0);
                    s.pos = new Vector3(c.x, hasFloor ? RoomRoofY(cx, cz, s.level, fy0) : WorldGen.Height(c.x, c.z) + WallH, c.z);
                    s.q = Quaternion.Euler(0f, s.rot * 90f, 0f);
                    if (s.reason != null) break;
                    if (!hasFloor) { Fail("Roofs go over a floor"); break; }
                    if (RoofOrSite(cx, cz)) { Fail("There's a roof here already"); break; }
                    if (StairsOrSite(cx, cz, s.level)) { Fail("Stairs go up here - lay a floor above instead"); break; }
                    s.ok = true;
                    break;
                }
                case HouseKind.Stairs:
                {
                    var c = CellCenter(cx, cz);
                    // they climb the way you face; ROTATE turns them
                    int face = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z) ? (fwd.x > 0f ? 1 : 3) : (fwd.z > 0f ? 0 : 2);
                    s.rot = (face + s.rot) % 4;
                    bool hasFloor = FloorOrSite(cx, cz, lv, out float fy);
                    s.pos = new Vector3(c.x, hasFloor ? fy : WorldGen.Height(c.x, c.z), c.z);
                    s.q = Quaternion.Euler(0f, s.rot * 90f, 0f);
                    if (s.reason != null) break;
                    if (!hasFloor) { Fail("Stairs go on a floor"); break; }
                    if (lv >= MaxLevel) { Fail("A house goes four storeys high at most"); break; }
                    if (StairsOrSite(cx, cz, lv)) { Fail("There are stairs here already"); break; }
                    if (FloorOrSite(cx, cz, lv + 1, out _)) { Fail("There's a floor above - stairs need open space over them"); break; }
                    if (RoofOrSite(cx, cz)) { Fail("Take the roof off first"); break; }
                    if (FurnitureInSquare(cx, cz, lv)) { Fail("Furniture is in the way"); break; }
                    var foot = EdgeAt(cx, cz, (s.rot + 2) % 4, lv);
                    if (foot != null && foot.def.kind != HouseKind.Doorway) { Fail("A wall is in the way at the foot"); break; }
                    s.ok = true;
                    break;
                }
                default:
                {
                    Vector3 q = new Vector3(Mathf.Round(t.x * 2f) * 0.5f, 0f, Mathf.Round(t.z * 2f) * 0.5f);
                    int fx = CellOf(q.x), fz = CellOf(q.z);
                    bool onFloor = FloorTop(fx, fz, lv, out float fy);
                    s.cx = fx; s.cz = fz;
                    s.pos = new Vector3(q.x, onFloor ? fy : WorldGen.Height(q.x, q.z), q.z);
                    s.q = Quaternion.Euler(0f, s.rot * 90f, 0f);
                    if (s.reason != null) break;
                    if (!onFloor)
                    {
                        if (!def.ground || lv > 0) { Fail("Furniture goes on a floor"); break; }
                        // out in the open: inside the camp ring, clear of trees, rocks and structures
                        if (!bm.IsInsideBase(s.pos, 0.5f)) { Fail("Inside your camp ring only"); break; }
                        if (WorldCollision.Overlaps(s.pos, Mathf.Max(0.4f, def.radius), true)) { Fail("Something is in the way"); break; }
                    }
                    if (StairsOrSite(fx, fz, lv)) { Fail("The stairs are here"); break; }
                    // keep clear of walls and of other furniture on this storey
                    if (WallClearance(s.pos) < Mathf.Max(0.25f, def.radius * 0.8f)) { Fail("Too close to a wall"); break; }
                    bool hit = false;
                    foreach (var o in furniture)
                    {
                        if (o.level != lv) continue;
                        if (o.def.radius <= 0f || def.radius <= 0f) { if (o.def.id == def.id && (o.pos - s.pos).sqrMagnitude < 0.3f) hit = true; continue; }
                        float m = o.def.radius + def.radius - 0.1f;
                        if (Flat(o.pos - s.pos) < m) hit = true;
                    }
                    if (hit) { Fail("Something is in the way"); break; }
                    s.ok = true;
                    break;
                }
            }
            if (s.ok && Caves.InCave) { s.ok = false; s.reason = "Not underground"; }
            return s;
        }

        /// <summary>The floor height an edge stands on at a storey (the higher floor beside it), or -infinity if neither side has one.</summary>
        float EdgeBase(int cx, int cz, int side, int level)
        {
            float y = float.NegativeInfinity;
            if (FloorOrSite(cx, cz, level, out float a)) y = a;   // a floor still being built counts: walls can go up beside it
            int nx = side == 1 ? cx + 1 : cx, nz = side == 0 ? cz + 1 : cz;
            if (FloorOrSite(nx, nz, level, out float b)) y = Mathf.Max(y, b);
            return y;
        }

        /// <summary>
        /// How high a roof or ceiling sits over a square: on the highest wall stacked round its ROOM - every square of the
        /// same top storey reached from it without crossing a wall, doorway or window - so a tall hall gets one flat
        /// ceiling, middle squares included. A room with no stacked walls: one storey up, as before.
        /// </summary>
        float RoomRoofY(int cx, int cz, int top, float topY)
        {
            float y = topY + WallH;
            var open = new Queue<long>(); var seen = new HashSet<long>();
            open.Enqueue(Key(cx, cz)); seen.Add(Key(cx, cz));
            while (open.Count > 0 && seen.Count < 120)
            {
                long k0 = open.Dequeue();
                int x = (int)(k0 >> 32), z = (int)(uint)k0;
                for (int k = 0; k < 4; k++)
                {
                    for (int l = top; l <= MaxLevel; l++)
                    {
                        var p = EdgeAt(x, z, k, l);
                        if (p != null && (p.level == top || p.def.tall || Stacked(p))) y = Mathf.Max(y, PieceTop(p));
                    }
                    if (EdgeAt(x, z, k, top) != null) continue;   // a wall: the room ends here
                    int nx = x + DX[k], nz = z + DZ[k];
                    long nk = Key(nx, nz);
                    if (seen.Contains(nk) || TopFloor(nx, nz) != top) continue;
                    seen.Add(nk); open.Enqueue(nk);
                }
            }
            return y;
        }

        /// <summary>Why a wall can't go on this edge because of stairs (their foot, or where they arrive up top), or null.</summary>
        string StairEdge(int ex, int ez, int side, int level)
        {
            foreach (var st in stairs.Values)
            {
                if (st.level != level && st.level != level - 1) continue;
                EdgeOf(st.cx, st.cz, st.level == level ? (st.rot + 2) % 4 : st.rot, out int x, out int z, out int sd);
                if (x == ex && z == ez && sd == side) return st.level == level ? "That's the foot of the stairs" : "That's the top of the stairs";
            }
            return null;
        }

        bool FurnitureNearSegment(Vector3 mid, int side, float pad, int level)
        {
            Vector2 dir = side == 0 ? Vector2.right : Vector2.up;
            Vector2 m = new Vector2(mid.x, mid.z), a = m - dir * Cell * 0.5f, b = m + dir * Cell * 0.5f;
            foreach (var o in furniture)
            {
                if (o.def.radius <= 0f || o.level != level) continue;
                if (DistSeg(new Vector2(o.pos.x, o.pos.z), a, b) < o.def.radius + WallT * 0.5f + pad) return true;
            }
            return false;
        }

        bool FurnitureInSquare(int cx, int cz, int level)
        {
            foreach (var f in furniture) if (f.level == level && f.cx == cx && f.cz == cz) return true;
            return false;
        }

        float WallClearance(Vector3 p)
        {
            float best = 99f;
            Vector2 q = new Vector2(p.x, p.z);
            int cx = CellOf(p.x), cz = CellOf(p.z);
            float y = p.y + 0.5f;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (segs.TryGetValue(Key(cx + i, cz + j), out var list))
                        foreach (var s in list) if (y >= s.lo && y <= s.hi) best = Mathf.Min(best, DistSeg(q, s.a, s.b) - WallT * 0.5f);
            return best;
        }

        static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        public Piece Place(HousePieceDef def, Spot s)
        {
            var pc = new Piece { def = def, cx = s.cx, cz = s.cz, side = s.side, rot = s.rot, level = s.level, pos = s.pos };
            Build(pc, s.depth);
            Register(pc);
            return pc;
        }

        void Register(Piece pc)
        {
            switch (pc.def.kind)
            {
                case HouseKind.Floor: floors[LKey(pc.cx, pc.cz, pc.level)] = pc; Publish(); RefreshRails(); break;
                case HouseKind.Roof: case HouseKind.RoofSlope: roofs[Key(pc.cx, pc.cz)] = pc; break;
                case HouseKind.Stairs: stairs[LKey(pc.cx, pc.cz, pc.level)] = pc; Publish(); RebuildSegs(); RefreshRails(); break;
                case HouseKind.Furniture:
                    furniture.Add(pc);
                    if (pc.def.radius > 0f)
                    {
                        pc.obs = WorldCollision.AddObstacle(pc.pos, pc.def.radius, pc.go);
                        // only in the way on its own storey (a table upstairs doesn't stop you downstairs)
                        pc.obs.yLo = pc.level == 0 ? float.NegativeInfinity : pc.Y - 0.8f;
                        pc.obs.yHi = pc.Y + 2.4f;
                    }
                    break;
                default: edges[pc.side][LKey(pc.cx, pc.cz, pc.level)] = pc; RebuildSegs(); break;
            }
            RecalcBounds();
            Version++;
        }

        void RebuildSegs()
        {
            segs.Clear();
            void Add(long k, Vector2 a, Vector2 b, float lo, float hi)
            {
                if (!segs.TryGetValue(k, out var l)) segs[k] = l = new List<Seg>();
                l.Add(new Seg { a = a, b = b, lo = lo, hi = hi });
            }
            for (int side = 0; side < 2; side++)
                foreach (var pc in edges[side].Values)
                {
                    Vector2 dir = side == 0 ? Vector2.right : Vector2.up;
                    Vector2 m = new Vector2(pc.pos.x, pc.pos.z), a = m - dir * Cell * 0.5f, b = m + dir * Cell * 0.5f;
                    // blocks from a little under its floor (the ground floor: well under, for slopes) to just under the floor above
                    float lo = pc.Y - (pc.level == 0 ? 2f : 0.6f), hi = PieceTop(pc) - 0.25f;
                    int nx = side == 1 ? pc.cx + 1 : pc.cx, nz = side == 0 ? pc.cz + 1 : pc.cz;
                    foreach (var k in new[] { Key(pc.cx, pc.cz), Key(nx, nz) })
                    {
                        if (pc.def.kind == HouseKind.Doorway)
                        {
                            float w = (Cell - GapOf(pc.def)) * 0.5f;
                            Add(k, a, a + dir * w, lo, hi);
                            Add(k, b - dir * w, b, lo, hi);
                        }
                        else Add(k, a, b, lo, hi);
                    }
                }
            foreach (var st in stairs.Values)
            {
                // the sides of the flight and the solid space under its top end, then a railing round the stairwell up top
                long k = Key(st.cx, st.cz);
                Vector2 c = new Vector2((st.cx + 0.5f) * Cell, (st.cz + 0.5f) * Cell), up = new Vector2(DX[st.rot], DZ[st.rot]), rt = new Vector2(up.y, -up.x);
                float h = Cell * 0.5f, w = StairW * 0.5f, y = st.Y, top = y + LevelH;
                Add(k, c + rt * w - up * h, c + rt * w + up * h, y - 0.6f, top + 0.9f);
                Add(k, c - rt * w - up * h, c - rt * w + up * h, y - 0.6f, top + 0.9f);
                Add(k, c - rt * w + up * h, c + rt * w + up * h, y - 0.6f, top - 0.7f);
                float rlo = top - 0.6f, rhi = top + 1f;
                Add(k, c - rt * h - up * h, c + rt * h - up * h, rlo, rhi);
                Add(k, c - rt * h - up * h, c - rt * h + up * h, rlo, rhi);
                Add(k, c + rt * h - up * h, c + rt * h + up * h, rlo, rhi);
                Add(k, c - rt * h + up * h, c - rt * w + up * h, rlo, rhi);
                Add(k, c + rt * w + up * h, c + rt * h + up * h, rlo, rhi);
            }
        }

        /// <summary>A stairwell's railing shows once there is an upstairs floor beside it.</summary>
        void RefreshRails()
        {
            foreach (var st in stairs.Values)
            {
                if (st.rail == null) continue;
                bool any = false;
                for (int k = 0; k < 4 && !any; k++) any = floors.ContainsKey(LKey(st.cx + DX[k], st.cz + DZ[k], st.level + 1));
                st.rail.gameObject.SetActive(any);
            }
        }

        /// <summary>The piece nearest a point in front of the player (REMOVE A PIECE): on your storey furniture, walls, stairs; then the roof or a floor above you; then your floor.</summary>
        public Piece PieceAt(Vector3 p, Vector3 fwd)
        {
            fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            Vector3 t = p + fwd * 1.8f;
            int lv = LevelAt(p);
            Piece best = null; float bd = 1.6f;
            foreach (var f in furniture) { if (f.level != lv) continue; float d = Flat(f.pos - t); if (d < bd) { bd = d; best = f; } }
            if (best != null) return best;
            bd = 1.3f;
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values) { if (w.level != lv) continue; float d = Flat(w.pos - t); if (d < bd) { bd = d; best = w; } }
            if (best != null)
            {
                // a stack of walls comes down from the top
                while (true)
                {
                    int up = best.level + (best.def.tall ? 2 : 1);
                    if (up > MaxLevel || !edges[best.side].TryGetValue(LKey(best.cx, best.cz, up), out var above) || !Stacked(above)) break;
                    best = above;
                }
                return best;
            }
            int cx = CellOf(t.x), cz = CellOf(t.z);
            var st = StairsAt(cx, cz, lv);
            if (st != null) return st;
            if (roofs.TryGetValue(Key(cx, cz), out var r) && p.y < r.Y) return r;
            for (int l = lv + 1; l <= MaxLevel; l++) if (floors.TryGetValue(LKey(cx, cz, l), out var up)) { for (int m = MaxLevel; m > l; m--) if (floors.TryGetValue(LKey(cx, cz, m), out var higher)) return higher; return up; }
            return floors.TryGetValue(LKey(cx, cz, lv), out var fl) ? fl : null;
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>Why a piece can't come down yet (null = it can).</summary>
        public string RemoveBlock(Piece pc)
        {
            if (pc == null) return "Nothing here to remove";
            if (pc.def.kind == HouseKind.Wall || pc.def.kind == HouseKind.Doorway || pc.def.kind == HouseKind.Window)
            {
                int up = pc.level + (pc.def.tall ? 2 : 1);
                if (up <= MaxLevel && edges[pc.side].TryGetValue(LKey(pc.cx, pc.cz, up), out var above) && Stacked(above)) return "Take down the wall on top of it first";
                return null;
            }
            if (pc.def.kind != HouseKind.Floor) return null;
            if (floors.ContainsKey(LKey(pc.cx, pc.cz, pc.level + 1))) return "Take down the floor above first";
            if (roofs.TryGetValue(Key(pc.cx, pc.cz), out var rf) && rf.level == pc.level) return "Take the roof off first";
            if (StairsAt(pc.cx, pc.cz, pc.level) != null) return "Take the stairs down first";
            foreach (var f in furniture) if (f.level == pc.level && f.cx == pc.cx && f.cz == pc.cz) return "Move the furniture off first";
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values)
                {
                    if (w.level != pc.level) continue;
                    bool touches = (w.cx == pc.cx && w.cz == pc.cz) || (side == 0 && w.cx == pc.cx && w.cz + 1 == pc.cz) || (side == 1 && w.cx + 1 == pc.cx && w.cz == pc.cz);
                    if (!touches) continue;
                    int ox = side == 1 ? (w.cx == pc.cx ? w.cx + 1 : w.cx) : w.cx, oz = side == 0 ? (w.cz == pc.cz ? w.cz + 1 : w.cz) : w.cz;
                    if (!floors.ContainsKey(LKey(ox, oz, pc.level))) return "Take down the walls that stand on it first";
                }
            return null;
        }

        public void Remove(Piece pc)
        {
            if (pc == null) return;
            switch (pc.def.kind)
            {
                case HouseKind.Floor: floors.Remove(LKey(pc.cx, pc.cz, pc.level)); Publish(); RefreshRails(); break;
                case HouseKind.Roof: case HouseKind.RoofSlope: roofs.Remove(Key(pc.cx, pc.cz)); break;
                case HouseKind.Stairs: stairs.Remove(LKey(pc.cx, pc.cz, pc.level)); Publish(); RebuildSegs(); break;
                case HouseKind.Furniture: furniture.Remove(pc); WorldCollision.RemoveObstacle(pc.obs); break;
                default: edges[pc.side].Remove(LKey(pc.cx, pc.cz, pc.level)); RebuildSegs(); break;
            }
            pc.removed = true;
            if (pc.go != null) Destroy(pc.go);
            RecalcBounds();
            Version++;
        }

        // ============================================================ COLLISION
        /// <summary>Pushes a circle out of every wall, stair side and railing it overlaps at its height (called from WorldCollision.PushOut).</summary>
        public static bool PushOut(ref Vector3 pos, float r)
        {
            var h = I;
            if (h == null || h.segs.Count == 0) return false;
            int cx = CellOf(pos.x), cz = CellOf(pos.z);
            bool hit = false;
            Vector2 p = new Vector2(pos.x, pos.z);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (!h.segs.TryGetValue(Key(cx + i, cz + j), out var list)) continue;
                    foreach (var s in list)
                    {
                        if (pos.y > s.hi || pos.y < s.lo) continue;
                        Vector2 ab = s.b - s.a;
                        float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                        Vector2 q = s.a + ab * t, d = p - q;
                        float min = r + WallT * 0.5f, len = d.magnitude;
                        if (len >= min) continue;
                        if (len < 1e-4f) { d = new Vector2(-ab.y, ab.x).normalized; len = 0f; } else d /= len;
                        p += d * (min - len);
                        hit = true;
                    }
                }
            if (hit) { pos.x = p.x; pos.z = p.y; }
            return hit;
        }

        /// <summary>True if a circle at pos overlaps any house square (camp structures may not be placed on a house).</summary>
        public bool Occupies(Vector3 pos, float r)
        {
            int c0x = CellOf(pos.x - r), c1x = CellOf(pos.x + r), c0z = CellOf(pos.z - r), c1z = CellOf(pos.z + r);
            for (int x = c0x; x <= c1x; x++) for (int z = c0z; z <= c1z; z++) if (floors.ContainsKey(LKey(x, z, 0))) return true;
            return false;
        }

        // ============================================================ SLEEP
        public Piece BedNear(Vector3 p, float reach)
        {
            Piece best = null; float bd = reach;
            foreach (var f in furniture) { if (!f.def.bed || Mathf.Abs(f.Y - p.y) > 1.5f) continue; float d = Flat(f.pos - p) - f.def.radius; if (d < bd) { bd = d; best = f; } }
            return best;
        }

        public Piece WardrobeNear(Vector3 p, float reach)
        {
            Piece best = null; float bd = reach;
            foreach (var f in furniture) { if (!f.def.wardrobe || Mathf.Abs(f.Y - p.y) > 1.5f) continue; float d = Flat(f.pos - p) - f.def.radius; if (d < bd) { bd = d; best = f; } }
            return best;
        }

        /// <summary>Under cover: a roof over this square above you, or a floor of a storey above yours.</summary>
        public bool Indoors(Vector3 p)
        {
            int cx = CellOf(p.x), cz = CellOf(p.z);
            if (roofs.TryGetValue(Key(cx, cz), out var r) && r.Y > p.y + 0.5f) return true;
            for (int l = 1; l <= MaxLevel; l++) if (floors.TryGetValue(LKey(cx, cz, l), out var f) && f.Y > p.y + 1f) return true;
            return false;
        }

        // ============================================================ EVERY FRAME
        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null || Count == 0) return;
            Vector3 pp = gm.Player.Position;
            var cam = Camera.main;
            bool inside = Indoors(pp);
            float cut = pp.y + 1.2f;   // under cover, everything above your storey nearby is hidden so you can see in
            Vector2 a = new Vector2(pp.x, pp.z), b = cam != null ? new Vector2(cam.transform.position.x, cam.transform.position.z) : a;
            foreach (var r in roofs.Values) SetHidden(r, inside && r.Y > cut && Flat(r.pos - pp) < 16f);
            foreach (var f in floors.Values) if (f.level > 0) SetHidden(f, inside && f.Y > cut && Flat(f.pos - pp) < 16f);
            foreach (var f in furniture) if (f.level > 0) SetHidden(f, inside && f.Y > cut && Flat(f.pos - pp) < 16f);
            foreach (var s in stairs.Values)
            {
                bool near = inside && Flat(s.pos - pp) < 16f;
                SetHidden(s, near && s.Y > cut);
                bool rh = near && s.Y + LevelH > cut;
                if (s.rail != null && s.railHidden != rh) { s.railHidden = rh; s.rail.enabled = !rh; }
            }
            for (int side = 0; side < 2; side++)
                foreach (var w in edges[side].Values)
                {
                    bool hide = false;
                    if (inside && Flat(w.pos - pp) < 14f)
                    {
                        if (w.Y > cut) hide = true;
                        else if (Mathf.Abs(w.Y - pp.y) < 1.6f)
                        {
                            Vector2 dir = side == 0 ? Vector2.right : Vector2.up, m = new Vector2(w.pos.x, w.pos.z);
                            hide = SegmentsCross(a, b, m - dir * Cell * 0.5f, m + dir * Cell * 0.5f);
                        }
                    }
                    SetHidden(w, hide);
                    if (w.door != null)
                    {
                        bool near = Close(w.pos, pp, 2.4f);
                        if (!near && BaseManager.I != null) foreach (var k in BaseManager.I.WorkerActors) if (k != null && Close(w.pos, k.Position, 2f)) { near = true; break; }
                        if (!near && gm.Party.Companion != null && Close(w.pos, gm.Party.Companion.Position, 2f)) near = true;
                        float want = near ? 1f : 0f;
                        if (Mathf.Abs(w.doorOpen - want) > 0.001f)
                        {
                            if (want > w.doorOpen && w.doorOpen < 0.02f) SynthAudio.Play(Sfx.Clunk, 1.6f, 0.35f);
                            w.doorOpen = Mathf.MoveTowards(w.doorOpen, want, Time.deltaTime * 3f);
                            float e = w.doorOpen * w.doorOpen * (3f - 2f * w.doorOpen);
                            w.door.localRotation = Quaternion.Euler(0f, -100f * e, 0f);
                            if (w.door2 != null) w.door2.localRotation = Quaternion.Euler(0f, 100f * e, 0f);
                        }
                    }
                }
        }

        static bool Close(Vector3 door, Vector3 who, float r) => Flat(door - who) < r && who.y > door.y - 1f && who.y < door.y + 2.2f;

        static void SetHidden(Piece pc, bool hide)
        {
            if (pc.hidden == hide || pc.rends == null) return;
            pc.hidden = hide;
            foreach (var r in pc.rends) if (r != null) r.enabled = !hide;
        }

        static bool SegmentsCross(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d1 = Cross(q2 - q1, p1 - q1), d2 = Cross(q2 - q1, p2 - q1), d3 = Cross(p2 - p1, q1 - p1), d4 = Cross(p2 - p1, q2 - p1);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // ============================================================ SAVE
        public List<HouseSave> Save()
        {
            var l = new List<HouseSave>();
            void Add(Piece p) => l.Add(new HouseSave { id = p.def.id, cx = p.cx, cz = p.cz, side = p.side, rot = p.rot, level = p.level, x = p.pos.x, y = p.pos.y, z = p.pos.z });
            foreach (var p in floors.Values) Add(p);
            for (int s = 0; s < 2; s++) foreach (var p in edges[s].Values) Add(p);
            foreach (var p in stairs.Values) Add(p);
            foreach (var p in roofs.Values) Add(p);
            foreach (var p in furniture) Add(p);
            return l;
        }

        public void Load(List<HouseSave> list)
        {
            if (list == null) return;
            // floors first, storey by storey: everything else stands on them
            for (int pass = 0; pass <= MaxLevel + 1; pass++)
                foreach (var s in list)
                {
                    var def = Def(s.id);
                    if (def == null) continue;
                    bool floor = def.kind == HouseKind.Floor;
                    if (pass <= MaxLevel ? !(floor && s.level == pass) : floor) continue;
                    var pc = new Piece { def = def, cx = s.cx, cz = s.cz, side = s.side, rot = s.rot, level = s.level, pos = new Vector3(s.x, s.y, s.z) };
                    float depth = UpperDepth;
                    if (floor && s.level == 0)
                    {
                        float lo = s.y;
                        var c = CellCenter(s.cx, s.cz);
                        for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) lo = Mathf.Min(lo, WorldGen.Height(c.x + i * Cell * 0.5f, c.z + j * Cell * 0.5f));
                        depth = s.y - lo + 0.5f;
                    }
                    Build(pc, depth);
                    Register(pc);
                }
        }

        // ============================================================ MESHES
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static Color32 Col(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static readonly Color32 Plank = Col("#b07a48"), PlankDark = Col("#8a5a32"), PlankLight = Col("#c8955c"), Post = Col("#6a4426"),
            StoneC = Col("#9a958c"), StoneDark = Col("#77736c"), Thatch = Col("#b8954a"), Shingle = Col("#8a4a3a"), Cloth = Col("#5a7ab0"), White = Col("#efe8da");

        static Matrix4x4 M(Vector3 p, Vector3 s, float yaw = 0f, float pitch = 0f, float roll = 0f) => Matrix4x4.TRS(p, Quaternion.Euler(pitch, yaw, roll), s);

        /// <summary>The look of a piece in its own space (the ghost uses it too). Floors are drawn with the footing depth given.</summary>
        public static Mesh MeshFor(HousePieceDef def, float depth = 0.8f)
        {
            string key = def.id + (def.kind == HouseKind.Floor ? "|" + Mathf.RoundToInt(depth * 10f) : "");
            if (meshes.TryGetValue(key, out var mesh)) return mesh;
            var mb = new MeshBuilder();
            float h = Cell, half = Cell * 0.5f;
            switch (def.kind)
            {
                case HouseKind.Floor:
                    mb.Box(M(new Vector3(0, -0.12f - depth * 0.5f, 0), new Vector3(h - 0.06f, depth, h - 0.06f)), StoneDark, StoneC);
                    if (def.style == "sandstone")
                        for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++)
                            mb.Box(M(new Vector3(-half + 0.375f + i * 0.75f, -0.06f, -half + 0.375f + j * 0.75f), new Vector3(0.72f, 0.12f, 0.72f)), (i + j) % 2 == 0 ? Col("#e0c088") : Col("#c8a46a"));
                    else
                        for (int i = 0; i < 5; i++)
                            mb.Box(M(new Vector3(-half + 0.3f + i * 0.6f, -0.06f, 0), new Vector3(0.58f, 0.12f, h)), i % 2 == 0 ? Plank : PlankLight);
                    break;
                case HouseKind.Wall when def.style != null:
                    StyledWall(mb, def.style, half);
                    break;
                case HouseKind.Wall:
                    if (def.stone)
                    {
                        for (int row = 0; row < 5; row++)
                            for (int k = 0; k < 3; k++)
                            {
                                float off = row % 2 == 0 ? 0f : 0.5f, w = 1f;
                                float x = -half + (k + off) * w + w * 0.5f;
                                if (x + w * 0.5f > half + 0.01f) x -= (x + w * 0.5f) - half;
                                mb.Box(M(new Vector3(x, 0.3f + row * 0.6f, 0), new Vector3(w - 0.04f, 0.56f, WallT)), (row + k) % 2 == 0 ? StoneC : StoneDark);
                            }
                    }
                    else
                    {
                        for (int row = 0; row < 6; row++) mb.Box(M(new Vector3(0, 0.25f + row * 0.5f, 0), new Vector3(h, 0.48f, WallT)), row % 2 == 0 ? Plank : PlankLight);
                        WallPosts(mb, half);
                    }
                    break;
                case HouseKind.Doorway when def.tall:
                {
                    // two stone pillars two storeys tall, a great beam with brackets and a carved cap
                    float H = LevelH + WallH, pw = (h - GapOf(def)) * 0.5f;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float x = s * (half - pw * 0.5f);
                        mb.Box(M(new Vector3(x, H * 0.5f, 0), new Vector3(pw, H, WallT + 0.14f)), StoneDark, StoneC);
                        mb.Box(M(new Vector3(x, 0.2f, 0), new Vector3(pw + 0.14f, 0.4f, WallT + 0.3f)), StoneC);
                        mb.Box(M(new Vector3(x - s * 0.36f, H - 0.85f, 0), new Vector3(0.14f, 1.0f, 0.16f), 0f, 0f, s * 45f), Post);
                    }
                    mb.Box(M(new Vector3(0, H - 0.22f, 0), new Vector3(h + 0.3f, 0.44f, WallT + 0.18f)), Post);
                    mb.Box(M(new Vector3(0, H + 0.12f, 0), new Vector3(1.3f, 0.26f, WallT + 0.24f)), PlankDark);
                    mb.Box(M(new Vector3(0, H + 0.36f, 0), new Vector3(0.5f, 0.24f, WallT + 0.2f)), Col("#d8b25a"));
                    break;
                }
                case HouseKind.Doorway:
                {
                    // the opening (1.2 m, or 1.6 m double doors almost a storey tall), plank wall either side, a lintel over it
                    float gp = GapOf(def), w = (h - gp) * 0.5f, top = gp > 1.3f ? 2.6f : 2.1f;
                    for (int s = -1; s <= 1; s += 2)
                        for (int row = 0; row < 6; row++)
                            mb.Box(M(new Vector3(s * (half - w * 0.5f), 0.25f + row * 0.5f, 0), new Vector3(w, 0.48f, WallT)), row % 2 == 0 ? Plank : PlankLight);
                    mb.Box(M(new Vector3(0, (top + WallH) * 0.5f, 0), new Vector3(gp, WallH - top, WallT)), PlankDark);
                    mb.Box(M(new Vector3(0, top + 0.02f, 0), new Vector3(gp + 0.1f, 0.1f, WallT + 0.06f)), Post);
                    WallPosts(mb, half);
                    break;
                }
                case HouseKind.Window:
                {
                    float w = (h - 1.2f) * 0.5f;
                    for (int s = -1; s <= 1; s += 2)
                        for (int row = 0; row < 6; row++)
                            mb.Box(M(new Vector3(s * (half - w * 0.5f), 0.25f + row * 0.5f, 0), new Vector3(w, 0.48f, WallT)), row % 2 == 0 ? Plank : PlankLight);
                    mb.Box(M(new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1.0f, WallT)), Plank);
                    mb.Box(M(new Vector3(0, 2.6f, 0), new Vector3(1.2f, 0.8f, WallT)), PlankLight);
                    mb.Box(M(new Vector3(0, 1.0f, 0), new Vector3(1.34f, 0.08f, WallT + 0.12f)), Post);   // sill
                    if (def.style == "crystal")
                    {
                        // glazed with crystal: a pale glowing pane (drawn with the lamp glow below)
                        mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(1.2f, 1.2f, 0.04f)), new Color32(150, 230, 255, 255));
                        mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(1.2f, 0.06f, 0.08f)), Col("#5a7a9a"));
                        mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(0.06f, 1.2f, 0.08f)), Col("#5a7a9a"));
                    }
                    else
                    {
                        mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(1.2f, 0.05f, 0.05f)), Post);            // cross bars
                        mb.Box(M(new Vector3(0, 1.6f, 0), new Vector3(0.05f, 1.2f, 0.05f)), Post);
                    }
                    WallPosts(mb, half);
                    break;
                }
                case HouseKind.Roof when def.style == "ceiling":
                    // flush with its neighbours: planks on top, beams underneath
                    mb.Box(M(new Vector3(0, 0.06f, 0), new Vector3(h + 0.02f, 0.12f, h + 0.02f)), PlankDark, Plank);
                    for (int i = 0; i < 3; i++) mb.Box(M(new Vector3(-h * 0.5f + 0.5f + i, -0.07f, 0), new Vector3(0.16f, 0.16f, h)), Post);
                    mb.Box(M(new Vector3(0, -0.04f, 0), new Vector3(h, 0.1f, 0.12f)), PlankDark);
                    break;
                case HouseKind.Roof:
                    mb.Box(M(new Vector3(0, 0.1f, 0), new Vector3(h + 0.3f, 0.2f, h + 0.3f)), Shingle, Thatch);
                    mb.Box(M(new Vector3(0, -0.04f, 0), new Vector3(h + 0.1f, 0.12f, h + 0.1f)), PlankDark);
                    break;
                case HouseKind.RoofSlope:
                {
                    // rises 1.5 m toward +z (rot turns it); a beam along the high edge, open gables
                    float rise = 1.5f, run = h + 0.3f;
                    float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg, len = Mathf.Sqrt(rise * rise + run * run);
                    Color32 under = def.style == "thatch" ? Col("#c8a050") : def.style == "reed" ? Col("#6a7a3a") : Shingle;
                    Color32 over = def.style == "thatch" ? Col("#e8c870") : def.style == "reed" ? Col("#8a9a4a") : Thatch;
                    mb.Box(M(new Vector3(0, rise * 0.5f + 0.1f, 0), new Vector3(h + 0.3f, def.style != null ? 0.3f : 0.18f, len), 0f, -ang), under, over);
                    if (def.style != null)   // a shaggy fringe along the low edge
                        for (int k = 0; k < 8; k++) mb.Box(M(new Vector3(-half + 0.2f + k * 0.37f, 0.02f, -half - 0.12f), new Vector3(0.3f, 0.22f, 0.12f), 0f, 0f, (k % 2 == 0 ? 6f : -6f)), under);
                    mb.Box(M(new Vector3(0, rise + 0.05f, half + 0.1f), new Vector3(h + 0.4f, 0.16f, 0.16f)), Post);
                    for (int s = -1; s <= 1; s += 2)   // triangular-ish gable ends: stacked boards
                        for (int k = 0; k < 3; k++)
                        {
                            float zz = -half + (k + 0.5f) * (h / 3f), hh = rise * (k + 0.5f) / 3f;
                            mb.Box(M(new Vector3(s * (half - 0.05f), hh * 0.5f, zz), new Vector3(0.1f, hh, h / 3f)), PlankDark);
                        }
                    break;
                }
                case HouseKind.Stairs:
                {
                    // a solid flight of 12 steps climbing one storey toward +z, with stringers and a handrail each side
                    int n = 12; float run = h / n, rise = LevelH / n, w = StairW;
                    for (int i = 0; i < n; i++)
                    {
                        float top = (i + 1) * rise;
                        mb.Box(M(new Vector3(0, top * 0.5f, -half + (i + 0.5f) * run), new Vector3(w - 0.16f, top, run)), PlankDark, i % 2 == 0 ? Plank : PlankLight);
                    }
                    float ang = Mathf.Atan2(LevelH, h) * Mathf.Rad2Deg, len = Mathf.Sqrt(LevelH * LevelH + h * h);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        mb.Box(M(new Vector3(s * (w * 0.5f - 0.04f), LevelH * 0.5f + 0.05f, 0), new Vector3(0.1f, 0.34f, len), 0f, -ang), Post);
                        mb.Box(M(new Vector3(s * (w * 0.5f - 0.04f), LevelH * 0.5f + 0.95f, 0), new Vector3(0.07f, 0.07f, len), 0f, -ang), PlankLight);
                        mb.Box(M(new Vector3(s * (w * 0.5f - 0.04f), 0.5f, -half + 0.06f), new Vector3(0.1f, 1f, 0.1f)), Post);
                        mb.Box(M(new Vector3(s * (w * 0.5f - 0.04f), LevelH + 0.5f, half - 0.06f), new Vector3(0.1f, 1f, 0.1f)), Post);
                        for (int k = 1; k < 4; k++)   // balusters
                        {
                            float zz = -half + k * (h / 4f), yy = (zz + half) / h * LevelH;
                            mb.Box(M(new Vector3(s * (w * 0.5f - 0.04f), yy + 0.5f, zz), new Vector3(0.05f, 0.9f, 0.05f)), PlankDark);
                        }
                    }
                    break;
                }
                default: Furniture(mb, def); break;
            }
            mesh = mb.ToMesh("house_" + key);
            meshes[key] = mesh;
            return mesh;
        }

        /// <summary>The blueprint wall looks: bamboo poles, smooth adobe, round logs, dark timber with iron straps.</summary>
        static void StyledWall(MeshBuilder mb, string style, float half)
        {
            switch (style)
            {
                case "bamboo":
                    for (int i = 0; i < 13; i++)
                    {
                        float x = -half + 0.12f + i * 0.23f;
                        mb.Cylinder(M(new Vector3(x, WallH * 0.5f, 0), new Vector3(0.2f, WallH, 0.2f)), i % 2 == 0 ? Col("#b8b060") : Col("#9aa04a"), 6, 1f, true);
                        for (int k = 1; k < 4; k++) mb.Cylinder(M(new Vector3(x, k * 0.75f, 0), new Vector3(0.22f, 0.05f, 0.22f)), Col("#7a8038"), 6, 1f, true);
                    }
                    mb.Box(M(new Vector3(0, 1.0f, 0.12f), new Vector3(half * 2f, 0.06f, 0.05f)), Col("#6a5030"));
                    mb.Box(M(new Vector3(0, 2.2f, 0.12f), new Vector3(half * 2f, 0.06f, 0.05f)), Col("#6a5030"));
                    break;
                case "adobe":
                    mb.Box(M(new Vector3(0, WallH * 0.5f, 0), new Vector3(half * 2f, WallH, WallT + 0.04f)), Col("#c8784a"), Col("#d89060"));
                    for (int k = 0; k < 3; k++) mb.Cylinder(M(new Vector3(-half + 0.5f + k * 1f, WallH - 0.3f, 0), new Vector3(0.14f, WallT + 0.3f, 0.14f), 0f, 90f), Col("#6a4426"), 6, 1f, true);
                    break;
                case "log":
                    for (int row = 0; row < 7; row++)
                        mb.Cylinder(M(new Vector3(0, 0.21f + row * 0.42f, 0), new Vector3(0.42f, half * 2f + 0.3f, 0.42f), 0f, 0f, 90f), row % 2 == 0 ? Col("#8a5a32") : Col("#9a6a3c"), 8, 1f, true);
                    break;
                default:   // "dark"
                    for (int row = 0; row < 6; row++) mb.Box(M(new Vector3(0, 0.25f + row * 0.5f, 0), new Vector3(half * 2f, 0.48f, WallT)), row % 2 == 0 ? Col("#3a3034") : Col("#48383c"));
                    for (int k = 0; k < 2; k++) mb.Box(M(new Vector3(0, 0.8f + k * 1.4f, 0), new Vector3(half * 2f, 0.1f, WallT + 0.05f)), Col("#5a5a64"));
                    WallPosts(mb, half);
                    break;
            }
        }

        static void WallPosts(MeshBuilder mb, float half)
        {
            for (int s = -1; s <= 1; s += 2) mb.Box(M(new Vector3(s * (half - 0.07f), WallH * 0.5f, 0), new Vector3(0.16f, WallH + 0.02f, WallT + 0.08f)), Post);
            mb.Box(M(new Vector3(0, WallH - 0.05f, 0), new Vector3(half * 2f, 0.12f, WallT + 0.06f)), Post);
        }

        static void Furniture(MeshBuilder mb, HousePieceDef d)
        {
            switch (d.id)
            {
                case "house_bed":
                    mb.Box(M(new Vector3(0, 0.22f, 0), new Vector3(1.1f, 0.3f, 2.1f)), PlankDark);
                    mb.Box(M(new Vector3(0, 0.45f, 0.05f), new Vector3(1.0f, 0.18f, 1.95f)), White);
                    mb.Box(M(new Vector3(0, 0.58f, 0.75f), new Vector3(0.7f, 0.12f, 0.35f)), White);
                    mb.Box(M(new Vector3(0, 0.56f, -0.25f), new Vector3(1.04f, 0.1f, 1.3f)), Cloth);
                    mb.Box(M(new Vector3(0, 0.55f, 1.02f), new Vector3(1.14f, 0.7f, 0.1f)), Post);
                    break;
                case "house_table":
                    mb.Box(M(new Vector3(0, 0.78f, 0), new Vector3(1.3f, 0.08f, 0.85f)), Plank);
                    for (int i = 0; i < 4; i++) mb.Box(M(new Vector3((i % 2 == 0 ? -1 : 1) * 0.55f, 0.38f, (i < 2 ? -1 : 1) * 0.32f), new Vector3(0.08f, 0.76f, 0.08f)), PlankDark);
                    break;
                case "house_chair":
                    mb.Box(M(new Vector3(0, 0.45f, 0), new Vector3(0.48f, 0.07f, 0.48f)), Plank);
                    mb.Box(M(new Vector3(0, 0.8f, -0.21f), new Vector3(0.48f, 0.65f, 0.06f)), PlankDark);
                    for (int i = 0; i < 4; i++) mb.Box(M(new Vector3((i % 2 == 0 ? -1 : 1) * 0.2f, 0.22f, (i < 2 ? -1 : 1) * 0.2f), new Vector3(0.06f, 0.44f, 0.06f)), PlankDark);
                    break;
                case "house_lamp":
                    mb.Cylinder(M(new Vector3(0, 0.06f, 0), new Vector3(0.36f, 0.12f, 0.36f)), Post, 8, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 0.75f, 0), new Vector3(0.06f, 1.4f, 0.06f)), Post, 6, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 1.5f, 0), new Vector3(0.42f, 0.32f, 0.42f)), new Color32(240, 220, 170, 255), new Color32(240, 220, 170, 255), 8, 0.6f, false);
                    break;
                case "house_rug":
                    mb.Box(M(new Vector3(0, 0.015f, 0), new Vector3(1.8f, 0.03f, 1.2f)), Col("#9a3a3a"));
                    mb.Box(M(new Vector3(0, 0.02f, 0), new Vector3(1.4f, 0.03f, 0.8f)), Col("#d8b25a"));
                    mb.Box(M(new Vector3(0, 0.025f, 0), new Vector3(0.9f, 0.03f, 0.4f)), Col("#3a5a8a"));
                    break;
                case "house_shelf":
                    mb.Box(M(new Vector3(0, 1.0f, -0.18f), new Vector3(1.1f, 2.0f, 0.06f)), PlankDark);
                    for (int s = -1; s <= 1; s += 2) mb.Box(M(new Vector3(s * 0.52f, 1.0f, 0), new Vector3(0.06f, 2.0f, 0.42f)), PlankDark);
                    for (int k = 0; k < 4; k++)
                    {
                        mb.Box(M(new Vector3(0, 0.08f + k * 0.6f, 0), new Vector3(1.0f, 0.05f, 0.4f)), Plank);
                        if (k == 3) continue;
                        for (int bk = 0; bk < 6; bk++)
                        {
                            Color32 bc = (bk + k) % 3 == 0 ? Col("#8a3a3a") : (bk + k) % 3 == 1 ? Col("#3a6a5a") : Col("#c8a050");
                            float bh = 0.34f + ((bk * 7 + k * 3) % 4) * 0.04f;
                            mb.Box(M(new Vector3(-0.42f + bk * 0.15f, 0.1f + k * 0.6f + bh * 0.5f, 0), new Vector3(0.12f, bh, 0.3f)), bc);
                        }
                    }
                    break;
                case "kin_scratch_post":
                    mb.Box(M(new Vector3(0, 0.06f, 0), new Vector3(0.6f, 0.12f, 0.6f)), Plank);
                    mb.Cylinder(M(new Vector3(0, 0.6f, 0), new Vector3(0.2f, 1.0f, 0.2f)), Col("#c8a878"), 8, 1f, true);
                    for (int k = 0; k < 4; k++) mb.Cylinder(M(new Vector3(0, 0.25f + k * 0.22f, 0), new Vector3(0.25f, 0.08f, 0.25f)), Col("#a88858"), 8, 1f, true);
                    mb.Box(M(new Vector3(0, 1.12f, 0), new Vector3(0.45f, 0.06f, 0.45f)), Col("#9a3a3a"));
                    mb.Sphere(M(new Vector3(0.22f, 0.95f, 0), Vector3.one * 0.12f), Col("#e8c040"), 6, 4);
                    break;
                case "house_flowerbox":
                    mb.Box(M(new Vector3(0, 0.18f, 0), new Vector3(0.9f, 0.36f, 0.4f)), PlankDark);
                    mb.Box(M(new Vector3(0, 0.36f, 0), new Vector3(0.84f, 0.04f, 0.34f)), Col("#5a3a20"));
                    for (int k = 0; k < 7; k++)
                    {
                        float x = -0.36f + k * 0.12f;
                        mb.Cylinder(M(new Vector3(x, 0.48f, (k % 2) * 0.08f - 0.04f), new Vector3(0.03f, 0.24f, 0.03f)), Col("#4a8a3a"), 4, 1f, false);
                        mb.Sphere(M(new Vector3(x, 0.62f, (k % 2) * 0.08f - 0.04f), Vector3.one * 0.11f), k % 3 == 0 ? Col("#ff8ab0") : k % 3 == 1 ? Col("#ffe060") : Col("#b08aff"), 6, 4);
                    }
                    break;
                case "kin_play_mat":
                    for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
                        mb.Box(M(new Vector3(-0.6f + i * 0.6f, 0.015f, -0.6f + j * 0.6f), new Vector3(0.6f, 0.03f, 0.6f)), (i + j) % 3 == 0 ? Col("#e86a6a") : (i + j) % 3 == 1 ? Col("#6ab0e8") : Col("#f0d060"));
                    mb.Sphere(M(new Vector3(0.4f, 0.15f, 0.3f), Vector3.one * 0.26f), Col("#ff7a4a"), 8, 5);
                    mb.Sphere(M(new Vector3(-0.5f, 0.11f, -0.4f), Vector3.one * 0.2f), Col("#7ae08a"), 8, 5);
                    mb.Sphere(M(new Vector3(-0.2f, 0.09f, 0.55f), Vector3.one * 0.16f), Col("#c08aff"), 8, 5);
                    break;
                case "house_shell_lamp":
                    mb.Cylinder(M(new Vector3(0, 0.05f, 0), new Vector3(0.4f, 0.1f, 0.4f)), Col("#d8c8a8"), 8, 1f, true);
                    mb.Box(M(new Vector3(0, 0.45f, 0), new Vector3(0.08f, 0.8f, 0.08f), 8f), Col("#a89878"));
                    mb.Dome(M(new Vector3(0, 0.88f, 0), new Vector3(0.6f, 0.4f, 0.5f), 0f, -20f), Col("#f0d8d0"), 10, 4);
                    break;
                case "kin_splash_pool":
                    mb.Cylinder(M(new Vector3(0, 0.15f, 0), new Vector3(1.9f, 0.3f, 1.9f)), StoneC, StoneDark, 14, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 0.31f, 0), new Vector3(1.6f, 0.02f, 1.6f)), Col("#4aa8e8"), Col("#7ad0ff"), 14, 1f, true);
                    mb.Sphere(M(new Vector3(0.4f, 0.36f, 0.2f), Vector3.one * 0.18f), Col("#ffd040"), 6, 4);
                    break;
                case "house_fireplace":
                    mb.Box(M(new Vector3(0, 0.6f, -0.15f), new Vector3(1.3f, 1.2f, 0.5f)), StoneC, StoneDark);
                    mb.Box(M(new Vector3(0, 0.35f, 0.08f), new Vector3(0.8f, 0.6f, 0.1f)), Col("#2a2420"));
                    mb.Box(M(new Vector3(0, 1.25f, -0.1f), new Vector3(1.5f, 0.12f, 0.6f)), PlankDark);
                    mb.Box(M(new Vector3(0, 1.9f, -0.25f), new Vector3(0.6f, 1.2f, 0.4f)), StoneDark);
                    break;
                case "kin_warm_rocks":
                    for (int k = 0; k < 7; k++)
                    {
                        float a = k / 7f * Mathf.PI * 2f;
                        mb.Rock(M(new Vector3(Mathf.Cos(a) * 0.65f, 0.12f, Mathf.Sin(a) * 0.65f), new Vector3(0.45f, 0.28f, 0.4f)), Col("#a8603a"), Col("#c87a4a"), (uint)(k * 7 + 3), 6, 4, 0.15f);
                    }
                    mb.Cylinder(M(new Vector3(0, 0.05f, 0), new Vector3(1.0f, 0.1f, 1.0f)), Col("#d8b070"), 12, 1f, true);
                    break;
                case "house_fur_rug":
                    mb.Cylinder(M(new Vector3(0, 0.02f, 0), new Vector3(1.9f, 0.04f, 1.3f)), Col("#f4f0e8"), 14, 1f, true);
                    for (int k = 0; k < 10; k++) { float a = k / 10f * Mathf.PI * 2f; mb.Sphere(M(new Vector3(Mathf.Cos(a) * 0.95f, 0.03f, Mathf.Sin(a) * 0.65f), new Vector3(0.22f, 0.06f, 0.22f)), Col("#e8e0d4"), 6, 3); }
                    break;
                case "kin_snow_slide":
                    mb.Box(M(new Vector3(0, 0.55f, 0.05f), new Vector3(0.7f, 0.08f, 1.9f), 0f, 30f), Col("#d8f0ff"));
                    mb.Box(M(new Vector3(0.38f, 0.6f, 0.05f), new Vector3(0.06f, 0.2f, 1.9f), 0f, 30f), PlankDark);
                    mb.Box(M(new Vector3(-0.38f, 0.6f, 0.05f), new Vector3(0.06f, 0.2f, 1.9f), 0f, 30f), PlankDark);
                    mb.Box(M(new Vector3(0, 0.6f, -0.95f), new Vector3(0.7f, 1.2f, 0.25f)), Plank);
                    for (int k = 0; k < 4; k++) mb.Box(M(new Vector3(0, 0.15f + k * 0.3f, -1.15f), new Vector3(0.6f, 0.06f, 0.2f)), PlankLight);
                    break;
                case "house_lily_planter":
                    mb.Cylinder(M(new Vector3(0, 0.2f, 0), new Vector3(0.75f, 0.4f, 0.75f)), PlankDark, Plank, 10, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 0.41f, 0), new Vector3(0.65f, 0.02f, 0.65f)), Col("#3a6a5a"), 10, 1f, true);
                    for (int k = 0; k < 3; k++) { float a = k * 2.1f; mb.Cylinder(M(new Vector3(Mathf.Cos(a) * 0.18f, 0.43f, Mathf.Sin(a) * 0.18f), new Vector3(0.22f, 0.02f, 0.22f)), Col("#5aa04a"), 8, 1f, true); }
                    mb.Sphere(M(new Vector3(0.05f, 0.5f, 0.05f), new Vector3(0.14f, 0.1f, 0.14f)), Col("#ffb0d0"), 6, 4);
                    break;
                case "kin_mud_wallow":
                    mb.Cylinder(M(new Vector3(0, 0.03f, 0), new Vector3(2.0f, 0.06f, 1.6f)), Col("#5a4030"), Col("#6a4a34"), 14, 1f, true);
                    for (int k = 0; k < 5; k++) { float a = k * 1.3f; mb.Sphere(M(new Vector3(Mathf.Cos(a) * 0.5f, 0.06f, Mathf.Sin(a) * 0.4f), new Vector3(0.18f, 0.06f, 0.18f)), Col("#7a5a40"), 6, 3); }
                    break;
                case "house_tapestry":
                    mb.Box(M(new Vector3(-0.5f, 0.9f, 0), new Vector3(0.06f, 1.8f, 0.06f)), PlankDark);
                    mb.Box(M(new Vector3(0.5f, 0.9f, 0), new Vector3(0.06f, 1.8f, 0.06f)), PlankDark);
                    mb.Box(M(new Vector3(0, 1.75f, 0), new Vector3(1.1f, 0.06f, 0.06f)), PlankDark);
                    for (int k = 0; k < 6; k++) mb.Box(M(new Vector3(0, 1.62f - k * 0.22f, 0), new Vector3(0.9f, 0.22f, 0.03f)), k % 3 == 0 ? Col("#c84a2a") : k % 3 == 1 ? Col("#e8b040") : Col("#2a6a8a"));
                    break;
                case "kin_sand_pit":
                    for (int s = 0; s < 4; s++) mb.Box(M(new Vector3(s < 2 ? (s == 0 ? -0.9f : 0.9f) : 0f, 0.12f, s >= 2 ? (s == 2 ? -0.9f : 0.9f) : 0f), s < 2 ? new Vector3(0.12f, 0.24f, 1.9f) : new Vector3(1.9f, 0.24f, 0.12f)), Plank);
                    mb.Box(M(new Vector3(0, 0.1f, 0), new Vector3(1.7f, 0.12f, 1.7f)), Col("#f0d898"));
                    mb.Cylinder(M(new Vector3(0.4f, 0.28f, 0.4f), new Vector3(0.22f, 0.25f, 0.22f)), Col("#e84a4a"), 8, 0.8f, true);
                    mb.Dome(M(new Vector3(-0.3f, 0.16f, -0.2f), new Vector3(0.6f, 0.3f, 0.5f)), Col("#e0c080"), 8, 3);
                    break;
                case "house_spooky_lantern":
                    mb.Box(M(new Vector3(0, 0.75f, 0), new Vector3(0.08f, 1.5f, 0.08f)), Col("#3a3034"));
                    mb.Box(M(new Vector3(0.16f, 1.48f, 0), new Vector3(0.38f, 0.06f, 0.06f)), Col("#3a3034"));
                    mb.Box(M(new Vector3(0.32f, 1.25f, 0), new Vector3(0.24f, 0.32f, 0.24f)), Col("#4a4a52"));
                    break;
                case "kin_climb_tree":
                    mb.Cylinder(M(new Vector3(0, 1.0f, 0), new Vector3(0.35f, 2.0f, 0.35f)), Col("#4a3a34"), 8, 0.7f, true);
                    mb.Cylinder(M(new Vector3(0, 0.7f, 0), new Vector3(1.1f, 0.08f, 1.1f)), PlankDark, 10, 1f, true);
                    mb.Cylinder(M(new Vector3(0.1f, 1.4f, 0.1f), new Vector3(0.8f, 0.08f, 0.8f)), PlankDark, 10, 1f, true);
                    mb.Sphere(M(new Vector3(0, 2.15f, 0), new Vector3(1.0f, 0.7f, 1.0f)), Col("#5a6a3a"), 8, 5);
                    mb.Cylinder(M(new Vector3(0.45f, 0.35f, 0), new Vector3(0.05f, 0.7f, 0.05f)), Col("#c8a878"), 4, 1f, false);
                    break;
                case "house_glowshroom":
                    mb.Cylinder(M(new Vector3(0, 0.15f, 0), new Vector3(0.45f, 0.3f, 0.45f)), Col("#4a3a5a"), 8, 0.85f, true);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * 2.1f, r2 = k == 0 ? 0f : 0.14f;
                        mb.Cylinder(M(new Vector3(Mathf.Cos(a) * r2, 0.45f + (k == 0 ? 0.15f : 0f), Mathf.Sin(a) * r2), new Vector3(0.06f, 0.3f + (k == 0 ? 0.25f : 0f), 0.06f)), Col("#d8e8f0"), 5, 1f, false);
                    }
                    break;
                case "kin_ball_pit":
                    for (int s = 0; s < 4; s++) mb.Box(M(new Vector3(s < 2 ? (s == 0 ? -0.95f : 0.95f) : 0f, 0.2f, s >= 2 ? (s == 2 ? -0.95f : 0.95f) : 0f), s < 2 ? new Vector3(0.1f, 0.4f, 2.0f) : new Vector3(2.0f, 0.4f, 0.1f)), Col("#3a4a7a"));
                    for (int k = 0; k < 26; k++)
                    {
                        float x = -0.75f + (k % 6) * 0.3f + (k / 6 % 2) * 0.15f, z = -0.75f + (k / 6) * 0.36f;
                        Color32 bc = k % 4 == 0 ? new Color32(120, 240, 255, 0) : k % 4 == 1 ? new Color32(255, 140, 220, 0) : k % 4 == 2 ? new Color32(255, 230, 120, 0) : new Color32(150, 255, 160, 0);
                        mb.Sphere(M(new Vector3(x, 0.32f, Mathf.Clamp(z, -0.8f, 0.8f)), Vector3.one * 0.26f), bc, 6, 4);
                    }
                    break;
                case "kin_bed_big":
                case "kin_bed_grand":
                {
                    float r = d.id == "kin_bed_grand" ? 1.2f : 0.78f;
                    mb.Cylinder(M(new Vector3(0, 0.12f, 0), new Vector3(r * 2f, 0.24f, r * 2f)), PlankDark, Plank, 14, 1f, true);
                    mb.Dome(M(new Vector3(0, 0.22f, 0), new Vector3(r * 1.8f, r * 0.22f, r * 1.8f)), Col("#7a8ad8"), 14, 4);
                    for (int k = 0; k < 6; k++)   // a ring of bolsters
                    {
                        float a = k / 6f * Mathf.PI * 2f;
                        mb.Sphere(M(new Vector3(Mathf.Cos(a) * r * 0.86f, 0.3f, Mathf.Sin(a) * r * 0.86f), new Vector3(r * 0.5f, 0.22f, r * 0.5f)), k % 2 == 0 ? Col("#e8c86a") : Col("#d86a7a"), 8, 4);
                    }
                    break;
                }
                case "kin_basket":
                    mb.Cylinder(M(new Vector3(0, 0.13f, 0), new Vector3(0.95f, 0.26f, 0.75f)), Col("#b08850"), Col("#c8a060"), 12, 1f, true);
                    mb.Cylinder(M(new Vector3(0, 0.27f, 0), new Vector3(1.0f, 0.06f, 0.8f)), Col("#8a6438"), 12, 1f, true);
                    for (int k = 0; k < 6; k++) mb.Box(M(new Vector3(0, 0.08f + k * 0.035f, 0), new Vector3(0.97f, 0.012f, 0.77f)), Col("#9a7444"));   // weave lines
                    mb.Dome(M(new Vector3(0, 0.24f, 0), new Vector3(0.78f, 0.14f, 0.6f)), Col("#d86a7a"), 10, 3);
                    mb.Sphere(M(new Vector3(0.2f, 0.33f, 0.14f), new Vector3(0.24f, 0.1f, 0.17f)), Col("#f0e0c0"), 6, 4);
                    break;
                case "house_wardrobe":
                    mb.Box(M(new Vector3(0, 1.05f, 0), new Vector3(1.1f, 2.1f, 0.6f)), PlankDark, Plank);
                    mb.Box(M(new Vector3(-0.27f, 1.05f, 0.31f), new Vector3(0.5f, 1.9f, 0.03f)), Plank);
                    mb.Box(M(new Vector3(0.27f, 1.05f, 0.31f), new Vector3(0.5f, 1.9f, 0.03f)), Plank);
                    mb.Box(M(new Vector3(-0.06f, 1.1f, 0.34f), new Vector3(0.04f, 0.2f, 0.04f)), Col("#d8b25a"));
                    mb.Box(M(new Vector3(0.06f, 1.1f, 0.34f), new Vector3(0.04f, 0.2f, 0.04f)), Col("#d8b25a"));
                    break;
            }
        }

        static Mesh railMesh;
        static readonly Dictionary<string, Mesh> leafMeshes = new Dictionary<string, Mesh>();

        /// <summary>A plank door leaf hinged at x 0, reaching to x = width.</summary>
        static Mesh DoorLeaf(float width, float height)
        {
            string key = $"{width:0.00}x{height:0.00}";
            if (leafMeshes.TryGetValue(key, out var m)) return m;
            var mb = new MeshBuilder();
            int n = Mathf.Max(2, Mathf.RoundToInt(width / 0.29f));
            float pw = width / n;
            for (int i = 0; i < n; i++) mb.Box(M(new Vector3((i + 0.5f) * pw, height * 0.5f, 0), new Vector3(pw - 0.01f, height, 0.07f)), i % 2 == 0 ? PlankDark : Plank);
            mb.Box(M(new Vector3(width * 0.5f, height * 0.24f, 0.02f), new Vector3(width, 0.1f, 0.08f)), Post);
            mb.Box(M(new Vector3(width * 0.5f, height * 0.76f, 0.02f), new Vector3(width, 0.1f, 0.08f)), Post);
            mb.Sphere(M(new Vector3(width - 0.16f, height * 0.5f, 0.07f), Vector3.one * 0.08f), Col("#d8b25a"), 6, 4);
            m = mb.ToMesh("house_door_leaf_" + key);
            leafMeshes[key] = m;
            return m;
        }
        static readonly Dictionary<string, Mesh> glowMeshes = new Dictionary<string, Mesh>();

        void Build(Piece pc, float depth)
        {
            var go = new GameObject("House_" + pc.def.id);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(pc.pos, Quaternion.Euler(0f, pc.def.kind == HouseKind.Wall || pc.def.kind == HouseKind.Doorway || pc.def.kind == HouseKind.Window ? (pc.side == 0 ? 0f : 90f) : pc.rot * 90f, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = MeshFor(pc.def, depth);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = pc.def.kind == HouseKind.Floor || pc.def.id == "house_rug" ? ShadowCastingMode.Off : ShadowCastingMode.On;
            var rends = new List<Renderer> { mr };
            if (pc.def.kind == HouseKind.Doorway && !pc.def.tall)
            {
                // one door, or a pair for a wide doorway (the second is a mirror image hinged on the other side)
                float gp = GapOf(pc.def);
                bool pair = gp > 1.3f;
                var leaf = DoorLeaf(pair ? gp * 0.5f : gp, pair ? 2.55f : 2.1f);
                Transform Hinge(float x, bool mirror)
                {
                    var t = new GameObject("Door").transform;
                    t.SetParent(go.transform, false);
                    t.localPosition = new Vector3(x, 0f, 0f);
                    if (mirror) t.localScale = new Vector3(-1f, 1f, 1f);
                    t.gameObject.AddComponent<MeshFilter>().sharedMesh = leaf;
                    var dr = t.gameObject.AddComponent<MeshRenderer>(); dr.sharedMaterial = RenderAssets.Building;
                    rends.Add(dr);
                    return t;
                }
                pc.door = Hinge(-gp * 0.5f, false);
                if (pair) pc.door2 = Hinge(gp * 0.5f, true);
            }
            if (pc.def.lamp)
            {
                if (!glowMeshes.TryGetValue(pc.def.id, out var gm))
                {
                    var mb = new MeshBuilder();
                    var gc = pc.def.glowCol; gc.a = 0;   // vertex alpha 0 = self-lit on the creature shader
                    mb.Sphere(M(pc.def.glowAt, Vector3.one * Mathf.Max(0.05f, pc.def.glowSize)), gc, 8, 6);
                    gm = mb.ToMesh("house_glow_" + pc.def.id);
                    glowMeshes[pc.def.id] = gm;
                }
                var g = new GameObject("Glow");
                g.transform.SetParent(go.transform, false);
                g.AddComponent<MeshFilter>().sharedMesh = gm;
                var gr = g.AddComponent<MeshRenderer>(); gr.sharedMaterial = RenderAssets.Creature; gr.shadowCastingMode = ShadowCastingMode.Off;
                rends.Add(gr);
            }
            if (pc.def.kind == HouseKind.Stairs)
            {
                // the railing round the stairwell one storey up (shown once there is a floor up there: RefreshRails)
                if (railMesh == null)
                {
                    var mb = new MeshBuilder();
                    float hc = Cell * 0.5f, w = StairW * 0.5f, y = LevelH;
                    void Rail(Vector3 a, Vector3 b)
                    {
                        Vector3 mid = (a + b) * 0.5f, d = b - a;
                        float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                        mb.Box(M(mid + Vector3.up * (y + 0.95f), new Vector3(0.08f, 0.08f, d.magnitude), yaw), PlankLight);
                        mb.Box(M(mid + Vector3.up * (y + 0.5f), new Vector3(0.05f, 0.05f, d.magnitude), yaw), PlankDark);
                        foreach (var e in new[] { a, b }) mb.Box(M(e + Vector3.up * (y + 0.5f), new Vector3(0.1f, 1f, 0.1f)), Post);
                    }
                    Rail(new Vector3(-hc, 0, -hc), new Vector3(hc, 0, -hc));
                    Rail(new Vector3(-hc, 0, -hc), new Vector3(-hc, 0, hc));
                    Rail(new Vector3(hc, 0, -hc), new Vector3(hc, 0, hc));
                    Rail(new Vector3(-hc, 0, hc), new Vector3(-w, 0, hc));
                    Rail(new Vector3(w, 0, hc), new Vector3(hc, 0, hc));
                    railMesh = mb.ToMesh("house_stair_rail");
                }
                var rg = new GameObject("Rail");
                rg.transform.SetParent(go.transform, false);
                rg.AddComponent<MeshFilter>().sharedMesh = railMesh;
                var rr = rg.AddComponent<MeshRenderer>(); rr.sharedMaterial = RenderAssets.Building;
                pc.rail = rr;
            }
            pc.go = go;
            pc.rends = rends.ToArray();
        }
    }
}
