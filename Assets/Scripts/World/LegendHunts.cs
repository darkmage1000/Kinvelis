using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    public enum TimeReq { Any, Night, Day, Dawn, Dusk }

    /// <summary>
    /// Legend hunts. Maro the Seeker camps outside Hearthmere and hands out one hunt at a time: three clues laid across
    /// an outer land (each one says what you found and where the trail goes next), then the legend's lair, where it
    /// only shows at the right time of day. Catch it and the hunt is done; defeat it and it returns to the lair ten
    /// minutes later. The five hunted legendaries never spawn any other way. State: PlayerStats.huntIndex (which hunt),
    /// huntStep (0 = not accepted, 1..3 = looking for that clue, 4 = at the lair), huntRetryUnix.
    /// </summary>
    public class LegendHunts : MonoBehaviour
    {
        public static LegendHunts I { get; private set; }
        public const int RetrySeconds = 600;
        public const string PrismSeed = "prismatic_seed";
        public const int SeedsPerHunt = 3, PrismGlimmer = 6000, PrismShards = 30;

        /// <summary>A hunted legend is out of its lair right now (legend seeds are only in hand while this is true).</summary>
        public static bool LegendOut => I != null && I.legend != null;
        public static int RestockPrice => 200 + 100 * Mathf.Clamp(S.huntIndex, 0, 10);

        /// <summary>Maro makes three more of the current hunt's seed (only once the trail has reached the lair).</summary>
        public string Restock()
        {
            var h = Current;
            if (h == null || !AtLair) return "Find its lair first.";
            if (S.glimmer < RestockPrice) return $"That takes {RestockPrice} Glimmer.";
            S.glimmer -= RestockPrice;
            GameManager.I.GiveItem(h.seed, SeedsPerHunt, GameManager.I.Player.Position + Vector3.up);
            GameEvents.Good($"Maro binds {SeedsPerHunt} more {ContentDB.ItemName(h.seed)}s.");
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        public string BuyPrism()
        {
            var bag = S.Bag;
            if (S.glimmer < PrismGlimmer) return $"That takes {PrismGlimmer} Glimmer.";
            if (bag.Count(Shimmer.ShardItem) < PrismShards) return $"That takes {PrismShards} Prism Shards in your bag.";
            S.glimmer -= PrismGlimmer;
            bag.Remove(Shimmer.ShardItem, PrismShards);
            GameManager.I.GiveItem(PrismSeed, 1, GameManager.I.Player.Position + Vector3.up);
            SynthAudio.Play(Sfx.LevelUp, 1.1f, 0.8f);
            GameEvents.Good("Maro hands you a Prismatic Seed. The next legend you catch with it will be a shimmer.");
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        string seedBefore;

        /// <summary>Puts the right seed in hand when the legend appears: the Prismatic Seed if you carry one, else this hunt's own.</summary>
        void ReadySeed(Hunt h)
        {
            var st = S;
            string pick = st.Bag.Count(PrismSeed) > 0 ? PrismSeed : st.Bag.Count(h.seed) > 0 ? h.seed : null;
            if (pick == null) { GameEvents.Say($"You have no {ContentDB.ItemName(h.seed)}s left - a Starbloom is about a 55% catch at 10% health, or Maro can make more.", new Color(1f, 0.8f, 0.5f)); return; }
            seedBefore = st.selectedSeed;
            st.selectedSeed = pick;
            GameEvents.Say($"{ContentDB.ItemName(pick)} readied. Get it to 10% health or less and the catch cannot fail.", new Color(0.7f, 1f, 0.8f));
        }

        void Gone()
        {
            legend = null;
            if (seedBefore != null) { S.selectedSeed = seedBefore; seedBefore = null; }
        }
        const float ClueReach = 3.6f, HunterReach = 3.4f, WakeRange = 45f, LoseRange = 150f, HpMul = 1.6f;

        public class Clue
        {
            public string find, lead;
            public TimeReq when;
            public Vector3 pos;
            public GameObject go;
        }

        public class Hunt
        {
            public string species, lair, intro, arrive, reward;
            /// <summary>The seed made for this legend (handed over with the last clue).</summary>
            public string seed;
            public BiomeId land;
            public int level, glimmer, rewardCount;
            public TimeReq when;
            public Clue[] clues;
            public Vector3 lairPos;
            public GameObject lairGo;
            public bool lairMarked;
            public SpeciesDef Sp => ContentDB.Sp(species);
        }

        public static readonly List<Hunt> All = new List<Hunt>();
        public static Vector3 HunterPos;
        public const string HunterName = "Maro the Seeker";

        static Clue C(string find, string lead, TimeReq when = TimeReq.Any) => new Clue { find = find, lead = lead, when = when };

        static void Add(string species, BiomeId land, int level, string lair, TimeReq when, int glimmer, string reward, int count, string intro, string arrive, params Clue[] clues)
        {
            All.Add(new Hunt { species = species, land = land, level = level, lair = lair, when = when, glimmer = glimmer, reward = reward, rewardCount = count, intro = intro, arrive = arrive, clues = clues });
        }

        /// <summary>Builds the hunts and picks every site from the world seed. Runs before chunks build.</summary>
        public static void Prepare()
        {
            All.Clear();
            Add("marelune", BiomeId.Isles, 40, "the Moonwell", TimeReq.Night, 800, "starbloom_seed", 5,
                "Fisherfolk on the Driftkeys swear the lagoon glows some nights. Something big is making that light. Start where you make landfall and look for shells that shine.",
                "The pool brightens from below. Marelune rises from the Moonwell!",
                C("A ring of shells, each glowing faintly from inside. Whatever left them was wet, heavy, and heading further across the shallows.", "A trail of glowing shells leads on"),
                C("A palm bent flat, its trunk rubbed smooth and slick with something that shines like moonlight.", "Something heavy dragged itself this way"),
                C("Pearls, dozens, laid in a crescent on the sand. The open side of the crescent faces a still pool.", "The crescent of pearls points to a pool. Whatever it is, it comes up after dark"));
            Add("oldroot", BiomeId.Swamp, 45, "the Standing Grove", TimeReq.Dawn, 1200, "kin_treat", 6,
                "The Mirefen's trees stand in rows where nobody planted them. Something walks through that fen and leaves a forest behind it. Find where it walked.",
                "One of the trees turns its head. Oldroot wakes!",
                C("Footprints the size of a cart, each one already full of seedlings.", "The footprints head deeper into the fen"),
                C("A lantern fruit, still warm and glowing, fallen from far too high to be a mangrove.", "More fallen fruit glimmers further on"),
                C("Bark rubbed off a whole stand of trees at antler height. If the antlers were as wide as a house.", "The rubbed trees ring a grove. It stirs at first light"));
            Add("solphyre", BiomeId.Desert, 50, "the Ashen Nest", TimeReq.Dawn, 1800, "starbloom_seed", 8,
                "Travellers in the Sunscour keep finding glass in the sand. Still hot. In the shape of wings. I want to know what lands there.",
                "The ember catches. Solphyre is reborn in front of you!",
                C("A patch of dune fused to glass: the print of a wing, feather by feather, twelve paces across.", "Glass prints run on across the dunes"),
                C("A single feather, too bright to look at straight on. It has scorched the rock it lies on.", "Scorch marks lead toward higher ground"),
                C("A cold ember the size of your head in a nest of ash, facing east. It is waiting for the sun.", "The nest is close. It burns out at dusk and is reborn at dawn", TimeReq.Dusk));
            Add("noxaris", BiomeId.Blight, 55, "the Sleeper's Hollow", TimeReq.Night, 2400, Wardens.HeartItem, 1,
                "Something on the Blightmoor is making new pools faster than the old ones dry. I want to know what sleeps there. Bring a good mount and better medicine.",
                "The fog pulls back. Noxaris opens one eye.",
                C("A fresh pool, still bubbling, in a hollow shaped as if something enormous lay down here.", "A line of fresh pools trails away"),
                C("Claw marks across bare rock, every groove half melted.", "The claw marks climb toward a crater"),
                C("Shed scales that glow a sick green, scattered downwind of a crater's rim.", "The scales lead to a hollow. It only wakes at night", TimeReq.Night));
            Add("kiralux", BiomeId.Glow, 60, "the Starfall Glade", TimeReq.Night, 3200, Wardens.HeartItem, 2,
                "Every night the Glowdeep's mushrooms light up in a line, as if something ran past them. Nothing I own is fast enough to see what. Maybe you are.",
                "A streak of light stops dead in the glade. Kiralux looks straight at you.",
                C("A row of glowcaps burning far brighter than the rest, in a dead straight line.", "The bright line runs deeper in"),
                C("Hoofprints in the moss with a star of light still fading in each one.", "The glowing prints bound away"),
                C("A strand of mane caught on a crystal. It crackles when you touch it.", "The trail ends at a glade. It runs only at night", TimeReq.Night));

            string[] seeds = { "moonpearl_seed", "lanternfruit_seed", "emberglass_seed", "blightscale_seed", "starmane_seed" };
            for (int i = 0; i < All.Count; i++) { All[i].seed = seeds[i]; Place(All[i], i); }
            var s = Settlements.All.Count > 0 ? Settlements.All[0] : null;
            Vector3 origin = s != null ? s.pos : WorldGen.StartExit;
            float a = 140f * Mathf.Deg2Rad;
            HunterPos = Landmarks.FindLand(origin + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (Settlements.Radius + 18f), 0.9f);
        }

        static Vector3 Dry(Vector3 p, BiomeId land)
        {
            Vector3 q = Landmarks.FindLand(p, 0.86f);
            if (q.y < 1.5f) q = Settlements.FindFlat(p, land);
            return q;
        }

        static void Place(Hunt h, int index)
        {
            if (!WorldGen.TryLand(h.land, out var land)) return;
            Vector3 center = new Vector3(land.center.x, 0, land.center.y);
            Vector3 gate = WorldGen.LandGate(land);
            // the lair sits on the far side of the land: past the Warden's arena at its heart, a long walk from the landing
            Vector3 away = center - gate; away.y = 0;
            float bearing = Mathf.Atan2(away.x, away.z) + (index % 2 == 0 ? 0.9f : -0.9f);
            Vector3 lair = Vector3.zero;
            for (int tries = 0; tries < 8; tries++)
            {
                float b = bearing + (tries % 2 == 0 ? 1f : -1f) * (tries / 2) * 0.45f;
                lair = Dry(center + new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b)) * land.radius * (0.5f - tries * 0.02f), h.land);
                if (lair.y >= 1.5f && !Wardens.Inside(lair, 160f) && (lair - gate).sqrMagnitude > 550f * 550f) break;
            }
            h.lairPos = lair;
            Vector3 along = lair - gate; along.y = 0;
            Vector3 side = new Vector3(-along.z, 0, along.x).normalized;
            for (int k = 0; k < h.clues.Length; k++)
            {
                float t = (k + 0.6f) / (h.clues.Length + 0.6f);
                Vector3 p = gate + along * t + side * ((k % 2 == 0 ? 1f : -1f) * 80f);
                p = Dry(p, h.land);
                if (Wardens.Inside(p, 60f)) p = Dry(p + side * 140f, h.land);
                h.clues[k].pos = p;
            }
        }

        // ============================================================ STATE
        static PlayerStats S => GameManager.I.Player.Stats;
        public static int Done => Mathf.Clamp(S.huntIndex, 0, All.Count);
        public static Hunt Current => S.huntIndex >= 0 && S.huntIndex < All.Count ? All[S.huntIndex] : null;
        public static bool Accepted => Current != null && S.huntStep > 0;
        public static bool AtLair => Current != null && S.huntStep > Current.clues.Length;
        public static Clue ActiveClue => Accepted && !AtLair ? Current.clues[S.huntStep - 1] : null;
        public static int RetryIn => (int)Mathf.Max(0, S.huntRetryUnix - Expeditions.Now);

        public static bool TimeOk(TimeReq t)
        {
            var env = EnvironmentSystem.I;
            if (env == null) return true;
            switch (t)
            {
                case TimeReq.Night: return env.IsNight;
                case TimeReq.Day: return !env.IsNight;
                case TimeReq.Dawn: return env.IsDawn;
                case TimeReq.Dusk: return env.IsDusk;
                default: return true;
            }
        }

        public static string TimeText(TimeReq t) =>
            t == TimeReq.Night ? "at night" : t == TimeReq.Day ? "by day" : t == TimeReq.Dawn ? "around dawn" : t == TimeReq.Dusk ? "around dusk" : "at any hour";

        /// <summary>One line for the Journey page / HUD: what to do next on the hunts.</summary>
        public static string NextLine(Vector3 from)
        {
            var h = Current;
            if (h == null) return "Every legend has been found.";
            if (!Accepted) return $"Talk to {HunterName} outside Hearthmere ({Wardens.Compass(from, HunterPos)}) for a new hunt in {BiomeInfo.Name(h.land)}.";
            if (AtLair) return $"Its lair, {h.lair}, is {Wardens.Compass(from, h.lairPos)}. It shows {TimeText(h.when)}.";
            var c = ActiveClue;
            string lead = S.huntStep == 1 ? $"Search {BiomeInfo.Name(h.land)} near where you make landfall" : h.clues[S.huntStep - 2].lead;
            return $"{lead}: about {Wardens.Compass(from, c.pos)}{(c.when != TimeReq.Any ? $" (shows {TimeText(c.when)})" : "")}.";
        }

        public string Accept()
        {
            var h = Current;
            if (h == null) return "There is nothing left to hunt.";
            if (Accepted) return "You're already on this hunt.";
            S.huntStep = 1;
            S.huntRetryUnix = 0;
            GameEvents.Good($"Hunt accepted. {NextLine(GameManager.I.Player.Position)}");
            SynthAudio.Play(Sfx.Craft, 0.9f, 0.7f);
            return null;
        }

        void Examine(Clue c)
        {
            var h = Current;
            S.huntStep++;
            c.go.SetActive(false);
            var gm = GameManager.I;
            gm.AddPlayerXp(30 + h.level * 4);
            Fx.Sparkle(c.pos + Vector3.up, GlowOf(h), 30, 1.2f);
            SynthAudio.Play(Sfx.Pickup, 0.8f, 0.9f);
            GameEvents.Good(c.find);
            if (AtLair)
            {
                MarkLair(h);
                gm.GiveItem(h.seed, SeedsPerHunt, c.pos + Vector3.up);
                GameEvents.Say($"{c.lead}. {h.lair} is marked on your map: {Wardens.Compass(gm.Player.Position, h.lairPos)}.", new Color(1f, 0.9f, 0.6f));
                GameEvents.Good($"From what you found here, Maro's notes make {SeedsPerHunt}x {ContentDB.ItemName(h.seed)}: made for this legend alone, and certain once it is at 10% health or less.");
            }
            else GameEvents.Say($"{c.lead}: about {Wardens.Compass(gm.Player.Position, ActiveClue.pos)}.", new Color(1f, 0.9f, 0.6f));
        }

        static Color GlowOf(Hunt h) => h.Sp != null ? h.Sp.palette.glow : Color.white;

        // ============================================================ WORLD
        CreatureRig hunterRig;
        Transform hunterBody;
        CreatureActor legend;
        float moteTimer;

        public void Init()
        {
            I = this;
            BuildHunter();
            foreach (var h in All)
            {
                BuildLair(h);
                foreach (var c in h.clues) BuildClue(h, c);
            }
        }

        static Color32 Hx(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }
        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        void BuildHunter()
        {
            var root = new GameObject("Seeker");
            root.transform.SetParent(transform, false);
            root.transform.position = HunterPos;
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.transform.position = HunterPos;
            hunterBody = body.transform;
            var look = new Palette { primary = Hx("#3a5a6a"), secondary = Hx("#2a3a44"), accent = Hx("#e8c060"), glow = Hx("#d8a878"), eye = new Color(0.1f, 0.08f, 0.1f) };
            hunterRig = ModelBuilder.BuildCustom("seeker", PlayerLook.Parts, PlayerLook.Pivots, look, body.transform, RenderAssets.Creature, Locomotion.Biped, 1.04f);
            // her camp: a lean-to, a spyglass on a tripod and a map table
            var mb = new MeshBuilder();
            Color32 wood = Hx("#6a4a2e"), cloth = Hx("#4a6a7a"), brass = Hx("#e8c060"), paper = Hx("#f0e4c0");
            mb.Box(M(-2.4f, 1.2f, -1.2f, 2.6f, 0.08f, 2.4f, 0, 0, 24), cloth, cloth);
            mb.Cylinder(M(-3.5f, 0.85f, -0.1f, 0.1f, 1.7f, 0.1f), wood, wood, 5, 1f, true);
            mb.Cylinder(M(-3.5f, 0.85f, -2.3f, 0.1f, 1.7f, 0.1f), wood, wood, 5, 1f, true);
            mb.Cylinder(M(-1.3f, 0.35f, -0.1f, 0.1f, 0.7f, 0.1f), wood, wood, 5, 1f, true);
            mb.Cylinder(M(-1.3f, 0.35f, -2.3f, 0.1f, 0.7f, 0.1f), wood, wood, 5, 1f, true);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                mb.Cylinder(M(2.0f + Mathf.Sin(a * Mathf.Deg2Rad) * 0.3f, 0.6f, -0.6f + Mathf.Cos(a * Mathf.Deg2Rad) * 0.3f, 0.06f, 1.3f, 0.06f, Mathf.Cos(a * Mathf.Deg2Rad) * 26f, 0, -Mathf.Sin(a * Mathf.Deg2Rad) * 26f), wood, wood, 4, 1f, true);
            }
            mb.Cylinder(M(2.0f, 1.3f, -0.5f, 0.16f, 0.9f, 0.16f, 65, 20, 0), brass, brass, 6, 0.7f, true);
            mb.Box(M(1.2f, 0.7f, -2.2f, 1.3f, 0.08f, 0.9f), wood, wood);
            mb.Box(M(1.2f, 0.36f, -2.2f, 0.12f, 0.7f, 0.12f), wood, wood);
            mb.Box(M(1.2f, 0.76f, -2.2f, 0.9f, 0.02f, 0.6f, 0, 12, 0), paper, paper);
            root.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("seeker_camp");
            var mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
            WorldCollision.AddObstacle(HunterPos + new Vector3(-2.4f, 0, -1.2f), 1.2f, this);
            Landmarks.List.Add(new Landmarks.Landmark { name = HunterName, pos = HunterPos, color = new Color(1f, 0.82f, 0.4f), biome = WorldGen.Biome(HunterPos.x, HunterPos.z), go = root, icon = MapIcon.Hunter });
        }

        void BuildLair(Hunt h)
        {
            var go = new GameObject("Lair_" + h.species);
            go.transform.SetParent(transform, false);
            go.transform.position = h.lairPos;
            h.lairGo = go;
            Color32 glow = GlowOf(h), stone = Hx("#6a6a74"), dark = Hx("#4a4a54");
            var mb = new MeshBuilder();
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f, r = 7f;
                Vector3 p = new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                p.y = WorldGen.Height(h.lairPos.x + p.x, h.lairPos.z + p.z) - h.lairPos.y;
                mb.Rock(Matrix4x4.TRS(p + Vector3.up * 0.5f, Quaternion.Euler(0, i * 40f, 0), new Vector3(1.2f, 1.3f + (i % 3) * 0.4f, 1.1f)), dark, stone, (uint)(i * 5 + 7), 6, 5, 0.2f);
                mb.Crystal(Matrix4x4.TRS(p + Vector3.up * (1.0f + (i % 3) * 0.3f), Quaternion.Euler(0, 0, i % 2 == 0 ? 8 : -8), new Vector3(0.3f, 0.9f, 0.3f)), glow, glow);
            }
            mb.Cylinder(M(0, 0.1f, 0, 4.4f, 0.2f, 4.4f), dark, stone, 12, 1f, true);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("lair_" + h.species);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
        }

        void MarkLair(Hunt h)
        {
            if (h.lairMarked) return;
            h.lairMarked = true;
            string name = char.ToUpper(h.lair[4]) + h.lair.Substring(5);   // "the Moonwell" -> "Moonwell"
            Landmarks.List.Add(new Landmarks.Landmark { name = name, pos = h.lairPos, color = GlowOf(h), biome = h.land, go = h.lairGo, icon = MapIcon.Legend });
        }

        void BuildClue(Hunt h, Clue c)
        {
            var go = new GameObject("Clue_" + h.species);
            go.transform.SetParent(transform, false);
            go.transform.position = c.pos;
            c.go = go;
            Color32 glow = GlowOf(h), accent = h.Sp != null ? (Color32)h.Sp.palette.accent : (Color32)Color.white;
            var mb = new MeshBuilder();
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f, r = 0.9f;
                mb.Sphere(M(Mathf.Sin(a) * r, 0.12f, Mathf.Cos(a) * r, 0.3f, 0.2f, 0.3f), glow, 6, 4);
            }
            mb.Cone(M(0, 0.5f, 0, 0.3f, 1.0f, 0.12f, 0, 30, 22), accent, 6);
            mb.Sphere(M(0.2f, 0.95f, 0.1f, 0.22f, 0.22f, 0.22f), glow, 6, 5);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("clue_" + h.species);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.SetActive(false);
        }

        // ============================================================ TICK
        /// <summary>HUNTS at Maro, EXAMINE at the active clue.</summary>
        public string Prompt(Vector3 p, out System.Action action)
        {
            action = null;
            Vector3 d = p - HunterPos; d.y = 0;
            if (d.sqrMagnitude < HunterReach * HunterReach) { action = () => MenuUI.I.OpenHunt(); return "HUNTS"; }
            var c = ActiveClue;
            if (c != null && c.go.activeSelf)
            {
                d = p - c.pos; d.y = 0;
                if (d.sqrMagnitude < ClueReach * ClueReach) { action = () => Examine(c); return "EXAMINE"; }
            }
            return null;
        }

        public bool AtHunter(Vector3 p) { Vector3 d = p - HunterPos; d.y = 0; return d.sqrMagnitude < 8f * 8f; }

        public void Tick(float dt, PlayerController pl)
        {
            Vector3 pp = pl.Position;
            Vector3 look = pp - HunterPos; look.y = 0;
            if (look.sqrMagnitude < 90f * 90f)
            {
                if (look.sqrMagnitude > 0.1f && look.sqrMagnitude < 25f * 25f)
                    hunterBody.rotation = Quaternion.RotateTowards(hunterBody.rotation, Quaternion.LookRotation(look), dt * 240f);
                hunterRig.Animate(dt, 0f, false, false);
            }
            var h = Current;
            if (h == null || !Accepted) return;
            moteTimer -= dt;
            bool mote = moteTimer <= 0f;
            if (mote) moteTimer = 0.2f;
            Color glow = GlowOf(h);

            var c = ActiveClue;
            if (c != null)
            {
                Vector3 d = pp - c.pos; d.y = 0;
                bool show = d.sqrMagnitude < 220f * 220f && TimeOk(c.when);
                if (c.go.activeSelf != show) c.go.SetActive(show);
                // a rising thread of light so the last stretch is findable
                if (show && mote && d.sqrMagnitude < 90f * 90f)
                    Fx.Mote(c.pos + new Vector3(Random.Range(-0.4f, 0.4f), 0.3f, Random.Range(-0.4f, 0.4f)), Vector3.up * 2.6f, glow, 0.2f, 2.2f, true);
                return;
            }

            // ---- at the lair ----
            MarkLair(h);
            Vector3 toLair = pp - h.lairPos; toLair.y = 0;
            if (legend != null)
            {
                if (!legend.gameObject.activeSelf || legend.Data == null || legend.Sp == null || legend.Sp.id != h.species || legend.State == AIState.Dead) { Gone(); return; }
                if (toLair.sqrMagnitude > LoseRange * LoseRange && (legend.Position - pp).sqrMagnitude > 90f * 90f)
                {
                    legend.Despawn();
                    Gone();
                    GameEvents.Say($"{h.Sp.displayName} slips away. It will be back at {h.lair}.", new Color(1f, 0.9f, 0.6f));
                }
                return;
            }
            if (toLair.sqrMagnitude > WakeRange * WakeRange || Caves.InCave || !pl.IsAlive) return;
            if (RetryIn > 0 || !TimeOk(h.when))
            {
                if (mote && Random.value < 0.3f) Fx.Mote(h.lairPos + new Vector3(Random.Range(-3f, 3f), 0.3f, Random.Range(-3f, 3f)), Vector3.up * 1.2f, glow, 0.16f, 1.6f, true);
                return;
            }
            var sp = h.Sp;
            if (sp == null) return;
            var data = CreatureFactory.RollWild(sp, h.level, h.land, false);
            Vector3 at = h.lairPos; at.y = Ground.Height(at.x, at.z);
            legend = CreatureSpawner.I.SpawnActor(data, CreatureMode.Wild, at);
            legend.Persistent = true;
            legend.Toughen(HpMul);
            Fx.Burst(at + Vector3.up * 2f, glow, 70, 9f, 0.5f, 1.4f, true);
            SynthAudio.Play(Sfx.Summon, 0.6f);
            SynthAudio.Play(Sfx.LevelUp, 0.7f, 0.6f);
            GameEvents.Warn($"{h.arrive}  Lv {h.level} - wear it down, then throw.");
            ReadySeed(h);
        }

        // ============================================================ OUTCOMES
        public static void OnCapture(CreatureData d)
        {
            var h = Current;
            if (I == null || h == null || d.speciesId != h.species || !AtLair) return;
            I.Gone();
            var gm = GameManager.I;
            S.glimmer += h.glimmer;
            gm.GiveItem(h.reward, h.rewardCount, gm.Player.Position + Vector3.up);
            gm.AddPlayerXp(200 + h.level * 15);
            S.huntIndex++;
            S.huntStep = 0;
            S.huntRetryUnix = 0;
            SynthAudio.Play(Sfx.LevelUp, 0.8f, 1f);
            GameEvents.Good($"Legend caught: {h.Sp.displayName}!  +{h.glimmer} Glimmer, {h.rewardCount}x {ContentDB.ItemName(h.reward)}." +
                            (Current != null ? $"  {HunterName} has another lead for you." : "  That was the last of them - every legend is found."));
            GameEvents.RaiseInventoryChanged();
        }

        public static void OnDefeat(CreatureActor a)
        {
            if (I == null || a != I.legend) return;
            var h = Current;
            I.Gone();
            S.huntRetryUnix = Expeditions.Now + RetrySeconds;
            GameEvents.Say($"{h.Sp.displayName} fades back into {h.lair}. It will return in {RetrySeconds / 60} minutes. Stop at 10% health next time and throw its seed.", new Color(1f, 0.9f, 0.6f));
        }

        /// <summary>Death / respawn: the legend goes back to sleep.</summary>
        public void Abort()
        {
            if (legend != null && legend.gameObject.activeSelf && legend.State != AIState.Dead) legend.Despawn();
            Gone();
        }
    }
}
