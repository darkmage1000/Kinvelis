using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Stars = the Little Explorer's goals: any order, each with a picture. Only on in worlds with simple buttons.</summary>
    /// <summary>Partners = how to earn the two starter partners you did not choose (any order; which village offers which depends on your choice).</summary>
    public enum QuestLine { Story, Camp, Kin, Stars, Partners }

    [Serializable]
    public class QuestCount { public string id; public int n; }

    /// <summary>One thing to do in a quest. Progress is read from the world's state, so it can never get out of step with it.</summary>
    public class QuestObj
    {
        public string text;
        public int need = 1;
        public Func<int> have;
        public int Have => Mathf.Clamp(have(), 0, need);
        public bool Done => have() >= need;
    }

    public class QuestDef
    {
        public string id, title, chapter, story, how;
        public QuestLine line;
        public QuestObj[] objs;
        public int glimmer, xp;
        public (string id, int n)[] items = new (string, int)[0];
        /// <summary>Where to go, when the quest is about a place (drives the HUD / map marker and the compass line).</summary>
        public Func<Vector3?> where;
        public string whereName;
        /// <summary>A recommendation shown with the quest. Never blocks it.</summary>
        public Func<string> advice;
        /// <summary>Id of the Warden this quest is about (the page gets a WARDEN button).</summary>
        public string warden;
        /// <summary>Picture for the HUD goal box: an ItemIcons key (item id, "bld_x", "kin_x") or "map:<MapIcon>".</summary>
        public string icon;
        /// <summary>Only offered while this is true (null = always): the partner quests depend on which starter was chosen.</summary>
        public Func<bool> when;
        /// <summary>Something the quest hands over that is not an item - a kin joining the roster - run once when it is finished.</summary>
        public Action onFinish;
        /// <summary>What onFinish hands over, in words, for the reward line ("Hopling joins you").</summary>
        public string gift;

        public QuestDef Pay(int glimmer, int xp, params (string, int)[] items) { this.glimmer = glimmer; this.xp = xp; this.items = items; return this; }
        public QuestDef At(string name, Func<Vector3?> where) { whereName = name; this.where = where; return this; }
        public QuestDef Advice(Func<string> f) { advice = f; return this; }
        public QuestDef Pic(string key) { icon = key; return this; }
        public QuestDef When(Func<bool> f) { when = f; return this; }
        public QuestDef Then(Action a, string gift = null) { onFinish = a; this.gift = gift; return this; }
        public bool Complete { get { foreach (var o in objs) if (!o.Done) return false; return true; } }
    }

    /// <summary>
    /// The quest log (user request 2026-10-09: "a main story line, teaches basics then sends you on a journey to the
    /// boss fights... its own tab [with] main camp line quests, boss progressing quests, and pal training quests").
    /// Three lines run side by side, each a fixed chain where the next quest opens when the one before is done:
    ///   Story - "The Sealed Storm": the first steps ashore, then land by land to each Warden and on to Stormcrest.
    ///   Camp  - "Hearth and Home": from the first worker to a Lv 6 Hearthstone that runs itself.
    ///   Kin   - "Bonds": ordering skills, levels, bond, tamers, ascending, hatching, a full Dex.
    /// Objectives are functions of the save (levels, buildings, Sigils, the Dex...) plus a few counters (Note), so
    /// things done before a quest opened still count and an old save catches up by itself the first time it loads.
    /// Finishing a quest pays out at once and opens the next; nothing has to be handed in. State: PlayerStats
    /// questsDone / questCounts / questTrack / questsStarted. To add a quest: one Q(...) line in the right place.
    /// </summary>
    public static class Quests
    {
        public static readonly List<QuestDef>[] Lines = { new List<QuestDef>(), new List<QuestDef>(), new List<QuestDef>(), new List<QuestDef>(), new List<QuestDef>() };
        public static readonly string[] LineNames = { "The Sealed Storm", "Hearth and Home", "Bonds", "Explorer Stars", "The Other Partners" };
        public static readonly string[] LineTabs = { "STORY", "CAMP", "KIN", "STARS", "PARTNERS" };
        public static readonly string[] LineBlurb =
        {
            "Why the storm stands round Rimewild, and the road to the eight Wardens who hold its locks.",
            "Your camp, from one fire and one worker to a home that runs itself.",
            "Raising, bonding with and understanding the kin who travel with you.",
            "Stars for doing fun things, in any order you like.",
            "The two partners you did not choose are waiting with the Kinkeepers of Cinderhold and Frostgate.",
        };

        /// <summary>A line whose quests can be finished in any order (the stars).</summary>
        public static bool IsFree(QuestLine line) => line == QuestLine.Stars || line == QuestLine.Partners;
        /// <summary>The stars only exist in worlds with simple buttons; the other lines are always there.</summary>
        public static bool LineOn(QuestLine line) =>
            line == QuestLine.Stars ? WorldRules.Current.simpleControls : line != QuestLine.Partners || Visible(line).Count > 0;

        /// <summary>Is this quest offered in this save at all?</summary>
        public static bool On(QuestDef q) => q.when == null || q.when();

        /// <summary>A line's quests as this save sees them (the partner quests for the other choices are left out).</summary>
        public static List<QuestDef> Visible(QuestLine line)
        {
            var l = new List<QuestDef>();
            if (G == null || G.Player == null) return l;
            foreach (var q in Lines[(int)line]) if (On(q)) l.Add(q);
            return l;
        }

        // ---- the three starter partners: the one chosen at the start, and where the other two wait ----
        public static readonly string[] PartnerIds = { "knuckub", "chiskit", "hopling" };

        /// <summary>Which partner this save started with (the first one it ever registered); -1 before the choice.</summary>
        public static int StarterIndex
        {
            get
            {
                if (G == null || G.Player == null) return -1;
                foreach (var id in S.registeredSpecies) { int i = Array.IndexOf(PartnerIds, id); if (i >= 0) return i; }
                return -1;
            }
        }

        /// <summary>The partner a village's Kinkeeper is keeping: the next one round from yours in Cinderhold (offset 1), the one after in Frostgate (offset 2).</summary>
        public static string PartnerAt(int offset) { int c = StarterIndex; return c < 0 ? null : PartnerIds[(c + offset) % PartnerIds.Length]; }

        static bool NearTown(string id)
        {
            var t = Town(id);
            if (t == null) return false;
            Vector3 d = t.pos - G.Player.Position; d.y = 0f;
            return d.sqrMagnitude < 55f * 55f;
        }

        static bool TamerBeatenAt(string town)
        {
            foreach (var w in S.tamerWins) if (w.wins > 0 && w.id.StartsWith(town + "_")) return true;
            return false;
        }

        static int TownRank(string id) { var t = Town(id); return t != null ? Requests.Rank(t) : 0; }

        /// <summary>The partner joins: rolled like any kin of its kind, a couple of levels under your strongest so it can keep up.</summary>
        static void GivePartner(string id, string keeper)
        {
            var sp = ContentDB.Sp(id);
            if (sp == null) return;
            var d = CreatureFactory.RollWild(sp, Mathf.Max(8, KinLevel - 2), BiomeId.Meadow, false);
            G.OnKinBought(d, keeper);
            G.Confetti(G.Player.Position + Vector3.up * 1.4f);
        }

        public static Sprite IconFor(QuestDef q)
        {
            if (q == null || string.IsNullOrEmpty(q.icon)) return null;
            if (q.icon.StartsWith("map:")) return MapIcons.Get((MapIcon)Enum.Parse(typeof(MapIcon), q.icon.Substring(4)));
            return ItemIcons.Get(q.icon);
        }

        /// <summary>Simple-button worlds: where the companion should run ahead to (a few steps toward the tracked goal), or null.</summary>
        public static Vector3? LeadPoint(Vector3 from)
        {
            if (!WorldRules.Current.simpleControls) return null;
            var m = Marker;
            if (!m.HasValue) return null;
            Vector3 d = m.Value - from; d.y = 0f;
            if (d.sqrMagnitude < 14f * 14f) return null;   // nearly there: back to heel
            Vector3 p = from + d.normalized * 5.5f;
            p.y = Ground.Height(p.x, p.z);
            return p;
        }
        public static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

        // ============================================================ READING THE WORLD
        static GameManager G => GameManager.I;
        static PlayerStats S => G.Player.Stats;
        static int B(bool b) => b ? 1 : 0;

        static bool Built(string id)
        {
            var bm = BaseManager.I;
            return bm != null && bm.HasBase && bm.Buildings.Exists(b => b.Def.id == id);
        }

        static int HearthLevel => BaseManager.I != null && BaseManager.I.HasBase ? BaseManager.I.CoreLevel : 0;
        static int Workers => G.Party.CountIn(KinLocation.Base);

        /// <summary>In the bag or in camp storage, wherever you are standing.</summary>
        static int Own(string item)
        {
            int n = S.Bag.Count(item);
            var bm = BaseManager.I;
            if (bm != null && bm.HasBase && bm.Storage != null) n += bm.Storage.Count(item);
            return n;
        }

        static int MaxKin(Func<CreatureData, int> f) { int m = 0; foreach (var d in G.Party.All) m = Mathf.Max(m, f(d)); return m; }
        static int KinLevel => MaxKin(d => d.level);
        static int KinBond => MaxKin(d => d.BondLevel);
        static int KinStage => MaxKin(d => d.stage);
        static int Owned => G.Party.All.Count;
        static int Species => S.registeredSpecies.Count;

        static int Elements
        {
            get
            {
                int mask = 0, n = 0;
                foreach (var d in G.Party.All) mask |= 1 << (int)d.Element1;
                for (int i = 0; i < 16; i++) if ((mask & (1 << i)) != 0) n++;
                return n;
            }
        }

        static int Defeated { get { int n = 0; foreach (var e in S.dex) n += e.defeated; return n; } }
        static int Tamers { get { int n = 0; foreach (var t in S.tamerWins) if (t.wins > 0) n++; return n; } }
        static bool CanSurf { get { foreach (var d in G.Party.All) { var sp = d.Species; if (sp != null && (sp.nearWater || d.Element1 == Element.Tide || d.Element2 == Element.Tide)) return true; } return false; } }
        static bool HasShimmer { get { foreach (var d in G.Party.All) if (d.shimmer) return true; return false; } }

        /// <summary>Owns a kin whose species has a signature skill (a Blink, or any AbilityDef.effect).</summary>
        static bool HasSignature
        {
            get
            {
                foreach (var d in G.Party.All)
                {
                    var sp = d.Species;
                    if (sp == null || sp.abilities == null) continue;
                    foreach (var id in sp.abilities)
                    {
                        var a = ContentDB.Ability(id);
                        if (a != null && (a.effect != AbilityEffect.None || a.kind == AbilityKind.Blink)) return true;
                    }
                }
                return false;
            }
        }

        static int OuterMetals { get { int n = 0; foreach (var t in Tech.All) if (t.group == 1 && S.techDone.Contains(t.id)) n++; return n; } }

        static Settlements.Settlement Town(string id) { foreach (var s in Settlements.All) if (s.id == id) return s; return null; }
        static Vector3? TownPos(string id) { var s = Town(id); return s != null ? s.pos : (Vector3?)null; }
        static Vector3? WardenPos(string id) { var w = Wardens.Get(id); return w != null ? w.pos : (Vector3?)null; }

        /// <summary>Has walked into this village (or has clearly been there: standing with it, or its land's Sigil).</summary>
        static bool Visited(string town, string warden)
        {
            if (Count("town:" + town) > 0 || Wardens.Beaten(warden)) return true;
            foreach (var e in S.standing) if (e.id == town && e.points > 0) return true;
            return false;
        }

        static bool BeenTo(BiomeId b, string warden) =>
            Count("land:" + b) > 0 || Wardens.Beaten(warden) || (b != BiomeId.Meadow && ExplorerLog.Percent(b, S) > 0);

        static Vector3? NearestSleepingWaystone()
        {
            var ws = Waystones.I;
            if (ws == null) return null;
            Vector3 me = G.Player.Position;
            Vector3? best = null; float bd = float.MaxValue;
            foreach (var w in ws.All)
            {
                if (ws.IsActive(w)) continue;
                float d = (w.pos - me).sqrMagnitude;
                if (d < bd) { bd = d; best = w.pos; }
            }
            return best;
        }

        // ============================================================ COUNTERS
        public static int Count(string key)
        {
            foreach (var c in S.questCounts) if (c.id == key) return c.n;
            return 0;
        }

        /// <summary>Something happened that a quest may be counting (ordering a skill, landing a fish, hatching an egg, reaching a place).</summary>
        public static void Note(string key, int n = 1)
        {
            if (G == null || G.Player == null) return;
            foreach (var c in S.questCounts) if (c.id == key) { c.n += n; return; }
            S.questCounts.Add(new QuestCount { id = key, n = n });
        }

        static void Mark(string key) { if (Count(key) == 0) Note(key); }

        // ============================================================ BUILDING THE TABLE
        static string chapter;

        static QuestDef Q(QuestLine line, string id, string title, string story, string how, params QuestObj[] objs)
        {
            var q = new QuestDef { line = line, id = id, title = title, story = story, how = how, objs = objs, chapter = chapter };
            Lines[(int)line].Add(q);
            return q;
        }

        static QuestObj O(string text, Func<bool> done) => new QuestObj { text = text, need = 1, have = () => B(done()) };
        static QuestObj O(string text, int need, Func<int> have) => new QuestObj { text = text, need = need, have = have };

        const string ChallengeHow = "Stand at the shrine inside the ring of standing stones, tap WARDEN, then CHALLENGE. It fights you and your companion together. " +
                                    "Leave the ring or faint and it goes back to sleep - nothing is lost, so a failed try costs you nothing.";

        static QuestDef WardenQuest(string id, string wid, string boss, string title, string story, int glimmer, params (string, int)[] items)
        {
            var q = Q(QuestLine.Story, id, title, story, ChallengeHow, O($"Defeat {boss}, the Warden", () => Wardens.Beaten(wid)));
            q.warden = wid;
            q.icon = "map:Warden";
            q.Pay(glimmer, glimmer / 2, items);
            q.where = () => WardenPos(wid);
            q.whereName = "the Warden's ring";
            q.advice = () =>
            {
                var w = Wardens.Get(wid);
                if (w == null) return null;
                int best = KinLevel;
                return best >= w.level - 2
                    ? $"The Warden is Lv {w.level}. Your strongest kin is Lv {best} - you are ready."
                    : $"The Warden is Lv {w.level} and your strongest kin is Lv {best}. Train to about Lv {w.level - 2} first: a few levels count for a lot.";
            };
            return q;
        }

        static Quests()
        {
            const QuestLine ST = QuestLine.Story, CA = QuestLine.Camp, KI = QuestLine.Kin;

            // ================================================================ STORY: THE SEALED STORM
            chapter = "Chapter 1 - Washed Ashore";
            Q(ST, "s_wake", "Washed Ashore",
                "The storm that stands round Rimewild broke your boat and threw you onto a ledge above the Dawnfield. Everything you owned is at the bottom of the sea. Start with what the ground gives you.",
                "Walk up to sticks, stones and fiber bundles and tap PICK UP. Then MENU > CRAFT and make a Stone Hatchet and a Stone Pick.",
                O("Make a hatchet", () => S.BestToolPower(ToolKind.Axe, out _) > 1),
                O("Make a pick", () => S.BestToolPower(ToolKind.Pick, out _) > 1))
                .Pay(20, 20, ("berry", 6));
            Q(ST, "s_seeds", "Something to Throw",
                "Your partner found you before you found it, and has not left your heel since. The islanders have a word for that - kin - and a way of asking for it: a Tether Seed, which takes root in a tired creature's trust.",
                "Fiber comes from bushes and fiberleaf (your hatchet shakes bark fiber out of trees too). MENU > CRAFT > Tether Seed.",
                O("Have 4 Tether Seeds", 4, () => G.GoalsDone.Contains("capture") ? 4 : S.Bag.Count("tether_seed")))
                .Pay(20, 20, ("tether_seed", 3));
            Q(ST, "s_catch", "A Second Friend",
                "One kin is company. Two is the start of a camp.",
                "Fight a wild kin until its health is low, tap SEED so the seed is in your hand, then THROW. The lower its health, the surer the catch - the ring shows the odds.",
                O("Capture a wild kin", () => G.GoalsDone.Contains("capture")))
                .Pay(30, 40, ("kin_treat", 2));
            Q(ST, "s_fight", "Side by Side",
                "Not everything on the Dawnfield wants to be friends. Learn to fight as a pair before you leave it.",
                "KIN calls your companion out. LOCK holds one target, and the four chips above the action buttons order its skills. Hold ATTACK to keep swinging.",
                O("Defeat wild kin", 3, () => Defeated))
                .Pay(30, 40, ("salve", 2));
            Q(ST, "s_camp", "A Fire of Your Own",
                "The wind off the sea has teeth after dark. You need a fire with walls round it, and somewhere to put what you carry.",
                "Tap BUILD and place a Hearthstone on flat ground. Everything inside its ring is your camp: kin work there, and you wake there if you fall.",
                O("Place a Hearthstone", () => HearthLevel > 0))
                .Pay(40, 40, ("berry_tart", 2));
            Q(ST, "s_town", "The Dawnfield Path",
                "A worn path runs north from the Dawnfield gate, hung with lanterns somebody keeps lit. There is smoke at the far end of it.",
                "Follow the pale trail with the lantern posts. The gold marker on your minimap points the way.",
                O("Reach Hearthmere", () => Visited("hearthmere", "meadow")))
                .Pay(40, 50, ("tether_seed", 4)).At("Hearthmere", () => TownPos("hearthmere"));
            Q(ST, "s_stone", "The Standing Stone",
                "Hearthmere's Kinkeeper, Juno, looks you over. \"Off a wreck? Then you have met the storm. It has stood round this island since Stormcrest shut itself in its Aerie, and no boat leaves until it lifts. Start small. Wake a waystone - the island answers those who do.\"",
                "Waystones are tall glowing stones. Walk up to one and tap ATTUNE. Awake stones are fast-travel points on the Map.",
                O("Attune a waystone", () => S.waystonesActive.Count > 0))
                .Pay(50, 50, ("salve", 2)).At("the nearest sleeping waystone", NearestSleepingWaystone);

            chapter = "Chapter 2 - The First Lock";
            Q(ST, "s_ready", "Eight Locks",
                "\"Eight Wardens hold eight Sigils,\" Juno says. \"They are the locks on the Aerie door, set there by the first keepers so that nobody would wake the storm-bird lightly. Each one tests whoever comes asking. The nearest sleeps in Bloomwyrm Grove, west of here. Do not go as you are.\"",
                "Kin gain levels by fighting beside you (the active one most, the rest of the party half). Catching new kinds is the fastest way to fill a party.",
                O("Raise a kin to Lv 8", 8, () => KinLevel),
                O("Own 4 kin", 4, () => Owned))
                .Pay(60, 60, ("ironbark_seed", 3));
            WardenQuest("s_w_meadow", "meadow", "Bloomwyrm", "The Bloomwyrm",
                "The grove is too quiet and the flowers are too large. Somewhere under the petals something very old is pretending to be asleep.",
                150, ("ironbark_seed", 3), ("kin_treat", 3));

            chapter = "Chapter 3 - Across the Water";
            Q(ST, "s_go_isles", "Across the Water",
                "The first Sigil hums in your pack like a struck bell. Juno turns it over twice. \"That is one. The next is over open sea, on the Driftkey Isles to the south-west. Nothing on two feet crosses that - but a water kin will carry you, and gladly.\"",
                "Any Tide kin (or one that lives by water) will surf: summon it, wade in and tap SURF. No harness needed. The lagoon between the Driftkeys is shallow enough to wade.",
                O("Own a kin that can surf", () => CanSurf),
                O("Set foot on the Driftkey Isles", () => BeenTo(BiomeId.Isles, "isles")))
                .Pay(100, 80, ("ironbark_seed", 3)).At("the Driftkey Isles", () => WardenPos("isles"));
            WardenQuest("s_w_isles", "isles", "Reefwarden", "The Coral Throne",
                "The islanders say the reef grew a guardian the way a shell grows a pearl: slowly, around something that hurt.",
                200, ("ironbark_seed", 4), ("salve", 3));

            chapter = "Chapter 4 - Fire Country";
            Q(ST, "s_go_badlands", "Fire Country",
                "East of the Sunmeadow the grass gives out and the ground starts to smoke. The people of Cinderhold live there on purpose, which tells you most of what you need to know about them.",
                "The road east from Hearthmere runs all the way to Cinderhold. Watch for geysers on the Ashfall Flats: the ground rumbles before one blows.",
                O("Reach Cinderhold", () => Visited("cinderhold", "badlands")))
                .Pay(120, 100, ("salve", 3)).At("Cinderhold", () => TownPos("cinderhold"));
            WardenQuest("s_w_badlands", "badlands", "Solarion", "Solarion's Caldera",
                "Kinkeeper Brask points with his chin. \"Up in the caldera. It does not like visitors and it does not like shade. Bring something wet.\"",
                280, ("ironbark_seed", 5), ("kin_treat", 3));

            chapter = "Chapter 5 - The Drowned Heart";
            Q(ST, "s_go_swamp", "The Drowned Heart",
                "The third Sigil lies east across the water in Mirefen, where the trees stand on their own roots and the lights between them are not lanterns.",
                "Surf east from the Badlands coast. In the fen, spore pods swell before they burst - step back and the poison cloud misses you.",
                O("Set foot in Mirefen", () => BeenTo(BiomeId.Swamp, "swamp")))
                .Pay(140, 110, ("salve", 3)).At("Mirefen", () => WardenPos("swamp"));
            WardenQuest("s_w_swamp", "swamp", "Hydrabog", "Three Heads",
                "Something in the Sunken Heart has been counting your footsteps since the shore. It has three heads to count with.",
                300, ("starbloom_seed", 2), ("salve", 4));

            chapter = "Chapter 6 - The Glacier's Light";
            Q(ST, "s_go_peaks", "The Glacier's Light",
                "Back on the old island the road climbs north-west into the Rimefang Peaks. Frostgate sits where the pines stop, with every door facing away from the wind.",
                "Snow and blizzards chill you: cook warm food, wear furs, or keep a warm kin out. The road from Hearthmere reaches Frostgate.",
                O("Reach Frostgate", () => Visited("frostgate", "peaks")))
                .Pay(180, 130, ("salve", 3)).At("Frostgate", () => TownPos("frostgate"));
            WardenQuest("s_w_peaks", "peaks", "Aurorox", "Aurora Glacier",
                "Kinkeeper Sella does not look up from her fire. \"You will know the place. The sky comes down to look at it.\"",
                380, ("starbloom_seed", 3), ("kin_treat", 4));

            chapter = "Chapter 7 - The Laughing Wind";
            Q(ST, "s_go_desert", "The Laughing Wind",
                "North-east over the sea the Sunscour Dunes lie under a sun that takes it personally. The wind there has a voice, and the voice has a temple.",
                "The dunes bake you by day: travel at night or carry cooling food. Quicksand drags at your feet - keep moving and save your stamina.",
                O("Set foot on the Sunscour Dunes", () => BeenTo(BiomeId.Desert, "desert")))
                .Pay(200, 140, ("starbloom_seed", 2)).At("the Sunscour Dunes", () => WardenPos("desert"));
            WardenQuest("s_w_desert", "desert", "Djinnado", "The Sun Temple",
                "It has been laughing at travellers for a thousand years. It would like very much to be given a reason to stop.",
                420, ("starbloom_seed", 3), ("salve", 4));

            chapter = "Chapter 8 - What the Crater Keeps";
            Q(ST, "s_go_blight", "What the Crater Keeps",
                "North, past where the charts stop, the Blightmoor rots quietly round a hole in the world. The first keepers put a Warden there for a reason they did not write down.",
                "Its pools are poison to wade - ride a kin through them. A Sporecap Draught blocks Poison outright. Wild kin there are a little stronger after dark.",
                O("Set foot on the Blightmoor", () => BeenTo(BiomeId.Blight, "blight")))
                .Pay(260, 170, ("starbloom_seed", 2)).At("the Blightmoor", () => WardenPos("blight"));
            WardenQuest("s_w_blight", "blight", "Venomarch", "The Blight Crater",
                "Something heavy uncoils when you come to the rim. It has been patient for longer than the villages have had names.",
                520, ("starbloom_seed", 4), ("salve", 5));

            chapter = "Chapter 9 - Every Light Looks Back";
            Q(ST, "s_go_glow", "Every Light Looks Back",
                "The last Sigil is far to the west, in Glowdeep, where night never quite ends and the ground gives off its own stars.",
                "Glowdeep's kin are the strongest on the map. Shimmer kin turn up twice as often there at night.",
                O("Set foot in Glowdeep", () => BeenTo(BiomeId.Glow, "glow")))
                .Pay(300, 190, ("starbloom_seed", 3)).At("Glowdeep", () => WardenPos("glow"));
            WardenQuest("s_w_glow", "glow", "Astralisk", "Star Hollow",
                "Every light in the hollow turns to look at you. Then the largest one blinks.",
                600, ("starbloom_seed", 5), ("kin_treat", 5));

            chapter = "Chapter 10 - The Aerie";
            Q(ST, "s_sigils", "The Eighth Lock",
                "Eight Sigils, and each one is singing a different note. Juno listens for a long time. \"It can hear them too, you know. It has been listening to you come this whole way.\"",
                "Any Warden you passed over still holds its Sigil - the JOURNEY tab lists them all.",
                O("Hold all eight Sigils", Wardens.SigilsNeeded, () => Wardens.Sigils))
                .Pay(400, 250, ("starbloom_seed", 4));
            WardenQuest("s_w_storm", "stormcrest", "Stormcrest", "The Sealed Storm",
                "The Aerie door stands open at the heart of the island. Above it the sky is perfectly clear, and thunder answers from it anyway. Go and ask the storm-bird to let the sea go.",
                1000, ("starbloom_seed", 6), ("kin_treat", 6));

            // ================================================================ CAMP: HEARTH AND HOME
            chapter = "First Fire";
            Q(CA, "c_work", "Hands for the Work",
                "A fire keeps you warm. A camp keeps you fed while you are somewhere else.",
                "BUILD > Production: a Lumber Mill, Stone Quarry, Crop Plot or Forage Hut. Then MENU > KIN, pick a kin and send it TO CAMP - it takes the job it is best at.",
                O("Build something that produces", () => Built("lumber_mill") || Built("stone_quarry") || Built("berry_plot") || Built("forage_hut")),
                O("Send a kin to work at camp", () => Workers > 0))
                .Pay(40, 40, ("berry", 10));
            Q(CA, "c_bench", "A Place to Make Things",
                "Your knees are tired of being a table.",
                "BUILD > Crafting > Workbench, and BUILD > Storage > Storage Chest. Anything in camp storage counts as yours when you craft inside the ring.",
                O("Build a Workbench", () => Built("workbench")),
                O("Build a Storage Chest", () => Built("storage_chest") || Built("large_chest")))
                .Pay(50, 50, ("tether_seed", 4));
            Q(CA, "c_cook", "Supper",
                "Kin work on a full stomach or not at all, and so do you.",
                "A Campfire cooks: walk up and tap COOK. A Feed Box holds food for your workers, so they stop coming to find you when they are hungry.",
                O("Build a Campfire", () => Built("campfire")),
                O("Build a Feed Box", () => Built("feed_box")))
                .Pay(50, 50, ("berry_tart", 3));
            Q(CA, "c_crew", "A Proper Crew",
                "One pair of hands is a favour. Three is a camp.",
                "The Hearthstone itself can be upgraded: MENU > CAMP > Overview > UPGRADE. Each level lets more kin work and adds storage. A Kin Nest gives them somewhere to sleep.",
                O("Have 3 kin working at camp", 3, () => Workers),
                O("Raise the Hearthstone to Lv 2", 2, () => HearthLevel))
                .Pay(80, 70, ("kin_treat", 3));

            chapter = "Fire and Stone";
            Q(CA, "c_smelt", "Fire and Stone",
                "Stone tools only go so far, and you have found where.",
                "A Smelter turns ore and cinder into ingots; it needs a kin that can Kindle (Ember kin, and most Volt and Lumen ones). Ore comes from the darker, speckled rocks.",
                O("Build a Smelter", () => Built("smelter")),
                O("Have 5 ingots", 5, () => Own("ingot")))
                .Pay(100, 80, ("salve", 3));
            Q(CA, "c_tools", "Better Edges",
                "An ingot axe goes through a tree in half the swings, and an ingot pick opens rocks a stone one only scratches.",
                "Workbench > Tools. Harder rocks, and the prism veins high in the Peaks, need the Ingot Pick.",
                O("Own an Ingot Axe", () => Own("iron_axe") > 0 || S.BestToolPower(ToolKind.Axe, out _) > 2),
                O("Own an Ingot Pick", () => Own("iron_pick") > 0 || S.BestToolPower(ToolKind.Pick, out _) > 2))
                .Pay(100, 80, ("ironbark_seed", 3));
            Q(CA, "c_fish", "Rod and Line",
                "The water round here is full of supper that nobody is using.",
                "Craft a Fishing Rod by hand. Fishing spots are rings on the water near a bank: tap FISH, wait, then REEL when it bites. Fish feed your workers too.",
                O("Land 3 fish", 3, () => Count("fish")))
                .Pay(80, 60, ("berry_tart", 3));
            Q(CA, "c_hearth3", "Room to Grow",
                "The ring of firelight is getting crowded.",
                "CAMP > Overview shows what the next Hearthstone level costs. Camp > Workers shows who is doing what, and Camp > Structures where more hands would help.",
                O("Raise the Hearthstone to Lv 3", 3, () => HearthLevel),
                O("Have 5 kin working at camp", 5, () => Workers))
                .Pay(150, 100, ("kin_treat", 4));

            chapter = "Bright Ideas";
            Q(CA, "c_research", "Bright Ideas",
                "You keep having ideas for things you do not know how to build yet.",
                "A Research Bench studies blueprints: pay the materials, then a Crafting kin (or you, with WORK) puts in the hours. Many blueprints need a land's Sigil first.",
                O("Build a Research Bench", () => Built("research_bench")),
                O("Finish any research", () => S.techDone.Count > 0))
                .Pay(150, 100, ("ironbark_seed", 4));
            Q(CA, "c_forge", "A Hotter Fire",
                "The metals of the outer lands laugh at your Smelter.",
                "Research Alloy Forging, then build the Alloy Forge. Every outer-land metal starts there.",
                O("Research Alloy Forging", () => S.techDone.Contains("forge")),
                O("Build an Alloy Forge", () => Built("alloy_forge")))
                .Pay(200, 120, ("salve", 4));
            Q(CA, "c_outpost", "Far Hands",
                "There is ore on the far shores, and nobody there to dig it.",
                "Research Outpost Logistics (it needs the Driftkey Sigil), craft an Outpost Kit and USE it on an outer land. Station two kin there and a caravan brings what they gather home.",
                O("Found an outpost", () => S.outposts.Count > 0))
                .Pay(250, 150, ("starbloom_seed", 2));
            Q(CA, "c_metal", "Metals of the Outer Lands",
                "Saltglass, bogsteel, sunsteel, blightsteel, starsteel: five lands, five metals, and each one opens tools and armour the last could not.",
                "Each metal's research needs its land's Sigil and some of its ore. Research > Metals.",
                O("Research an outer-land metal", () => OuterMetals > 0))
                .Pay(300, 180, ("starbloom_seed", 2));

            chapter = "A Home Worth the Storm";
            Q(CA, "c_hearth4", "The Hearth Grows",
                "Travellers have started asking whether this is a village.",
                "Hearthstone levels 4 to 6 cost outer-land bars and Warden Hearts. Each adds storage and room for more workers.",
                O("Raise the Hearthstone to Lv 4", 4, () => HearthLevel))
                .Pay(350, 200, ("kin_treat", 5));
            Q(CA, "c_power", "Sparks",
                "Some of your kin crackle when they are bored. Put that to use.",
                "A Volt Pylon charged by a Charging kin powers the camp: sprinklers, turrets and the machines that come after.",
                O("Build a Volt Pylon", () => Built("volt_pylon")),
                O("Have the camp powered", () => BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.CampPowered))
                .Pay(400, 220, ("starbloom_seed", 3));
            Q(CA, "c_auto", "It Runs Itself",
                "The best camp is the one that does not notice you are gone.",
                "A Conveyor Hub moves goods to storage without haulers. An Auto-Assembler works a queue of orders by itself while the camp is powered.",
                O("Build a Conveyor Hub", () => Built("conveyor_hub")),
                O("Build an Auto-Assembler", () => Built("auto_assembler")))
                .Pay(500, 260, ("starbloom_seed", 3));
            Q(CA, "c_hearth6", "A Home Worth the Storm",
                "One fire, once. Look at it now.",
                "The last Hearthstone level wants the rarest bars and a good many Warden Hearts.",
                O("Raise the Hearthstone to Lv 6", 6, () => HearthLevel))
                .Pay(1000, 400, ("starbloom_seed", 6));

            // ================================================================ KIN: BONDS
            chapter = "Getting to Know Each Other";
            Q(KI, "k_skill", "Call the Shot",
                "Your kin will fight without being asked. It fights much better when it is.",
                "With a wild kin in front of you (or locked), tap one of the four skill chips above the action buttons. Your companion runs in and uses that skill.",
                O("Order a skill from the skill bar", () => Count("skill") > 0 || S.level >= 8))
                .Pay(30, 30, ("kin_treat", 2));
            Q(KI, "k_team", "Strength in Numbers",
                "Different kin are good at different things, and the island has a lot of different things.",
                "Up to five kin travel in your party; the KIN bar swaps which one is out. The rest wait in the Hearth Box at camp.",
                O("Own 3 kin", 3, () => Owned))
                .Pay(40, 40, ("tether_seed", 4));
            Q(KI, "k_lv5", "A Second Trick",
                "Kin learn a new skill at levels 5, 10 and 15.",
                "The kin that is out earns full experience and the rest of the party half. MENU > KIN shows what each one will learn next.",
                O("Raise a kin to Lv 5", 5, () => KinLevel))
                .Pay(50, 50, ("kin_treat", 2));
            Q(KI, "k_bond", "Trust",
                "A kin that trusts you hits harder, guards better and, in the end, refuses to fall.",
                "Bond grows from fighting together, from PET when the prompt shows, and from FEED in the Kin tab. Each bond level adds 3% to its attack and defence.",
                O("Reach bond level 1 with a kin", 1, () => KinBond))
                .Pay(60, 50, ("kin_treat", 3));
            Q(KI, "k_elements", "Know Your Elements",
                "Water puts out fire, fire eats leaves, leaves drink water - and seven more besides. The right kin makes a hard fight a short one.",
                "A hit that says \"Super effective\" did half again as much. The Dex shows each kin's elements; a target's are on its frame.",
                O("Own kin of 4 different elements", 4, () => Elements))
                .Pay(80, 60, ("ironbark_seed", 2));

            chapter = "Training";
            Q(KI, "k_tamer", "A Friendly Match",
                "Other people keep kin too, and some of them stand by the road looking for someone to prove it to.",
                "Rival tamers wait along the roads out of each village (crossed swords on the map). Tap CHALLENGE: their kin come out one at a time and cannot be caught.",
                O("Beat a rival tamer", 1, () => Tamers))
                .Pay(100, 80, ("ironbark_seed", 3)).At("the road out of Hearthmere", () => TownPos("hearthmere"));
            Q(KI, "k_lv10", "Coming Along",
                "Ten kinds caught, and you are starting to tell the tracks apart.",
                "A kin's third skill comes at Lv 10. New kinds are worth double experience the first time you catch them.",
                O("Raise a kin to Lv 10", 10, () => KinLevel),
                O("Register 10 kinds of kin", 10, () => Species))
                .Pay(120, 100, ("ironbark_seed", 3));
            Q(KI, "k_sig", "One of a Kind",
                "A few kin know a trick nobody else does: a step through shadow, a shield planted in front of you, a song that speeds the whole camp.",
                "Look for the characters: Kagekit and Hexpurr in the Sunmeadow, Bulwarden in the meadow and Peaks, Lilute by ponds, Cindra where the grass meets the Badlands. The Kin tab explains each signature skill.",
                O("Own a kin with a signature skill", () => HasSignature))
                .Pay(150, 100, ("kin_treat", 4));
            Q(KI, "k_alpha", "The Big One",
                "Every land has a few kin that grew too big and too bold. They keep lairs, and they keep trophies.",
                "Alphas are larger, glow at the edges and hit hard. The first of each kind you beat always drops a trophy accessory.",
                O("Defeat an alpha", () => S.alphaTrophies.Count > 0))
                .Pay(180, 120, ("ironbark_seed", 4));
            Q(KI, "k_ascend", "Ascension",
                "At twenty levels something in a kin changes. It stands taller. It starts to shine.",
                "MENU > KIN, pick a kin of Lv 20 or more and tap EVOLVE: +15% to every stat, a little more work at camp, and a new look.",
                O("Ascend a kin", 1, () => KinStage))
                .Pay(250, 150, ("starbloom_seed", 2));

            chapter = "Family";
            Q(KI, "k_egg", "New Life",
                "Two kin, a warm nest and ten minutes of patience.",
                "Build a Hatchery and pair two kin there. The egg hatches into one of their kinds with the better parent's quality. Wild nests sometimes hold an egg too, if you can get past the mother.",
                O("Hatch an egg", () => Count("hatch") > 0))
                .Pay(250, 150, ("kin_treat", 4));
            Q(KI, "k_bond3", "Close as Kin",
                "It has started finishing your sentences, which is impressive for something that cannot talk.",
                "At bond level 3 a companion's skills recharge 10% faster.",
                O("Reach bond level 3 with a kin", 3, () => KinBond))
                .Pay(300, 180, ("kin_treat", 5));
            Q(KI, "k_tamers3", "Known on the Road",
                "Word gets round. The tamers have started practising before you arrive.",
                "Each village has a Tamer and an Ace on its roads. Wins also raise your standing with the village.",
                O("Beat 3 different tamers", 3, () => Tamers))
                .Pay(350, 200, ("starbloom_seed", 2));
            Q(KI, "k_dex25", "Field Notes",
                "Twenty-five kinds in the book. The Kinkeepers have started asking you questions.",
                "The Dex shows where each kin lives and when. Catching one of a kind makes every later catch of it 10% easier.",
                O("Register 25 kinds of kin", 25, () => Species))
                .Pay(400, 220, ("starbloom_seed", 3));

            chapter = "Mastery";
            Q(KI, "k_shimmer", "A Different Coat",
                "Once in a long while a kin is born the wrong colour, and it is the best thing that ever happened to it.",
                "Shimmer kin sparkle and work one level better at every camp job. They are rare everywhere, less rare on shimmer nights and in Glowdeep after dark.",
                O("Own a shimmer kin", () => HasShimmer))
                .Pay(500, 250, ("starbloom_seed", 3));
            Q(KI, "k_exalt", "Exalted",
                "There is a second change, later, for kin that have come a long way with somebody.",
                "MENU > KIN, the same EVOLVE button again at Lv 35.",
                O("Exalt a kin", 2, () => KinStage))
                .Pay(600, 300, ("starbloom_seed", 4));
            Q(KI, "k_bond5", "Unbreakable",
                "There is a point past which a kin simply will not leave you, whatever is standing over it.",
                "At bond level 5 a companion survives a blow that would have dropped it, once every ten minutes.",
                O("Reach bond level 5 with a kin", 5, () => KinBond))
                .Pay(700, 350, ("kin_treat", 8));
            Q(KI, "k_master", "Keeper of Kin",
                "Sixty kinds, and one of yours could stand in a Warden's ring by itself.",
                "The Dex milestones pay out along the way - MILESTONES in the Dex tab.",
                O("Register 60 kinds of kin", 60, () => Species),
                O("Raise a kin to Lv 50", 50, () => KinLevel))
                .Pay(1500, 500, ("starbloom_seed", 8));

            // ================================================================ STARS (Little Explorer worlds, any order)
            const QuestLine SR = QuestLine.Stars;
            chapter = "Stars";
            Q(SR, "x_pet", "Best Friends", "Your kin loves a scratch behind the ears.", "Stand next to your kin and tap PET.",
                O("Pet your kin", () => Count("pet") > 0)).Pay(20, 10, ("kin_treat", 2)).Pic("kin_treat");
            Q(SR, "x_catch", "A New Friend", "There are kin everywhere. Any of them could come with you.", "Walk up to a wild kin and tap SEED.",
                O("Catch a kin", () => Owned >= 2)).Pay(20, 10, ("kin_treat", 2)).Pic("tether_seed");
            Q(SR, "x_town", "The Village", "Somebody lives at the end of the lantern path.", "Follow the arrow, or follow your kin - it knows the way.",
                O("Find the village", () => Visited("hearthmere", "meadow"))).Pay(30, 10, ("berry_tart", 2)).Pic("map:Village").At("Hearthmere", () => TownPos("hearthmere"));
            Q(SR, "x_stone", "The Glowing Stone", "Tall stones hum when you come close.", "Follow the arrow to a tall stone and tap ATTUNE.",
                O("Wake a waystone", () => S.waystonesActive.Count > 0)).Pay(30, 10, ("berry_tart", 2)).Pic("map:Waystone").At("the nearest sleeping waystone", NearestSleepingWaystone);
            Q(SR, "x_swim", "Splash!", "The water is lovely.", "Walk into the sea or a pond. You can't sink.",
                O("Go for a swim", () => Count("swim") > 0)).Pay(20, 10, ("kin_treat", 1)).Pic("map:Fish");
            Q(SR, "x_ride", "Giddy Up", "Your kin would love to carry you.", "With a bigger kin out, tap RIDE.",
                O("Ride a kin", () => Count("ride") > 0)).Pay(30, 10, ("kin_treat", 2)).Pic("kin_harness");
            Q(SR, "x_five", "Five Friends", "A whole parade of them.", "Catch more kin. Up to five walk with you.",
                O("Have five kin", 5, () => Owned)).Pay(40, 20, ("kin_treat", 3)).Pic("ironbark_seed");
            Q(SR, "x_brave", "Brave", "Some kin like a scrap. So do yours.", "Tap ATTACK near a wild kin. Your kin helps.",
                O("Win fights", 10, () => Defeated)).Pay(40, 20, ("kin_treat", 2)).Pic("wood_club");
            Q(SR, "x_camp", "My Camp", "A place that is yours.", "Tap BUILD and put down a Hearthstone. HOME brings you back to it.",
                O("Build a Hearthstone", () => HearthLevel > 0)).Pay(40, 20, ("berry_tart", 3)).Pic("bld_hearthstone");
            Q(SR, "x_build", "Builder", "A camp needs things in it.", "Tap BUILD and put down anything you like. It is all free.",
                O("Build three more things", 3, () => BaseManager.I != null ? BaseManager.I.Buildings.Count - 1 : 0)).Pay(50, 20, ("kin_treat", 3)).Pic("bld_workbench");
            Q(SR, "x_ten", "Sticker Collector", "Ten different kinds!", "Every new kind you catch is a sticker in your book.",
                O("Collect stickers", 10, () => Species)).Pay(60, 30, ("kin_treat", 4)).Pic("starbloom_seed");
            Q(SR, "x_look", "Top of the World", "You can see everything from up there.", "Find a lookout tower and tap LOOK OUT.",
                O("Climb a lookout", () => S.lookouts.Count > 0)).Pay(50, 20, ("berry_tart", 2)).Pic("map:Lookout");
            Q(SR, "x_chest", "Treasure!", "Chests stand all over the land under a beam of light.", "Walk to a beam of light and tap OPEN.",
                O("Open a treasure chest", () => S.poisLooted.Count > 0 || S.chestsOpened > 0)).Pay(50, 20, ("kin_treat", 2)).Pic("map:Wreck");
            Q(SR, "x_cave", "Into the Dark", "There are holes in the hills.", "Walk around until you find a cave mouth.",
                O("Find a cave", () => S.cavesFound.Count > 0)).Pay(50, 20, ("kin_treat", 2)).Pic("map:Cave");
            Q(SR, "x_tamer", "Champion", "Somebody by the road wants a match.", "Find a tamer near the village and tap CHALLENGE.",
                O("Beat a tamer", 1, () => Tamers)).Pay(60, 30, ("kin_treat", 3)).Pic("map:Tamer").At("the road out of Hearthmere", () => TownPos("hearthmere"));
            Q(SR, "x_land", "Across the Sea", "There is more land out there.", "Ride a water kin out over the sea.",
                O("Reach a new land", () => OuterLandsSeen > 0)).Pay(80, 40, ("starbloom_seed", 2)).Pic("map:Outpost");
            Q(SR, "x_shimmer", "Sparkles", "Some kin are a different colour, and they glitter.", "Keep catching kin. The sparkly ones are special.",
                O("Have a shimmer kin", () => HasShimmer)).Pay(80, 40, ("kin_treat", 4)).Pic("shimmer_prism");
            Q(SR, "x_warden", "The Big Boss", "Something huge sleeps in the grove.", "Follow the arrow to the ring of stones and tap WARDEN.",
                O("Beat a Warden", 1, () => Wardens.Sigils)).Pay(150, 60, ("starbloom_seed", 3)).Pic("map:Warden").At("Bloomwyrm Grove", () => WardenPos("meadow"));
            Q(SR, "x_many", "Super Collector", "Twenty-five different kinds!", "Look in new places for new kin.",
                O("Collect stickers", 25, () => Species)).Pay(150, 60, ("starbloom_seed", 3)).Pic("starbloom_seed");

            // ================================================================ PARTNERS (user, 2026-10-10: "we need a way to get the
            // other starters... quests, one in each major region depending on what you chose"). You start with one of three in
            // the Sunmeadow; the other two wait in the Badlands and the Peaks, in an order set by your choice (PartnerAt).
            // Six quests are defined - each village with each partner - and a save only ever sees its own two (When).
            const QuestLine PA = QuestLine.Partners;
            chapter = "Partners";
            string[] pNames = { "Knuckub", "Chiskit", "Hopling" };
            for (int p = 0; p < PartnerIds.Length; p++)
            {
                string pid = PartnerIds[p], pn = pNames[p];
                // ---- Cinderhold (Badlands): prove yourself on its roads, earn the village's trust, come back
                {
                    var pre = new[]
                    {
                        O("Reach Cinderhold", () => Visited("cinderhold", "badlands")),
                        O("Beat a tamer on Cinderhold's roads", () => TamerBeatenAt("cinderhold")),
                        O("Become a Visitor in Cinderhold (finish requests on its board)", () => TownRank("cinderhold") >= 1),
                    };
                    var objs = new List<QuestObj>(pre);
                    objs.Add(O("Go back to Kinkeeper Brask in Cinderhold", () => { foreach (var o in pre) if (!o.Done) return false; return NearTown("cinderhold"); }));
                    Q(PA, "p_cinderhold_" + pid, pn + " of Cinderhold",
                        $"Kinkeeper Brask keeps a young {pn} behind his stall that has chewed through three leads. \"Walked in off the Dawnfield road looking for somebody,\" he says. \"Not me, evidently. Show me you can hold your own on these roads, and that the village will vouch for you, and we will see whether it was you.\"",
                        "Tamers stand along the roads out of the village (crossed swords on the map): tap CHALLENGE. The notice board by the gate has requests, and finishing them raises your standing. Then walk back into Cinderhold.",
                        objs.ToArray())
                        .Pay(200, 150, ("kin_treat", 4)).Pic("kin_" + pid).At("Cinderhold", () => TownPos("cinderhold"))
                        .When(() => PartnerAt(1) == pid).Then(() => GivePartner(pid, "Kinkeeper Brask"), pn + " joins you");
                }
                // ---- Frostgate (Peaks): the same, and Sella also wants to see that your own kin trust you
                {
                    var pre = new[]
                    {
                        O("Reach Frostgate", () => Visited("frostgate", "peaks")),
                        O("Beat a tamer on Frostgate's roads", () => TamerBeatenAt("frostgate")),
                        O("Become a Visitor in Frostgate (finish requests on its board)", () => TownRank("frostgate") >= 1),
                        O("Reach bond level 2 with one of your kin", 2, () => KinBond),
                    };
                    var objs = new List<QuestObj>(pre);
                    objs.Add(O("Go back to Kinkeeper Sella in Frostgate", () => { foreach (var o in pre) if (!o.Done) return false; return NearTown("frostgate"); }));
                    Q(PA, "p_frostgate_" + pid, pn + " of Frostgate",
                        $"A {pn} has slept by Kinkeeper Sella's fire all winter with one eye on the gate. \"It is waiting for someone,\" she says. \"I will not hand it to a stranger my neighbours have never heard of, or to anybody whose own kin do not trust them.\"",
                        "Tamers stand along the roads out of the village: tap CHALLENGE. Requests on the board by the gate raise your standing. Bond grows from fighting beside a kin, PET and FEED. Then walk back into Frostgate.",
                        objs.ToArray())
                        .Pay(350, 250, ("kin_treat", 6)).Pic("kin_" + pid).At("Frostgate", () => TownPos("frostgate"))
                        .When(() => PartnerAt(2) == pid).Then(() => GivePartner(pid, "Kinkeeper Sella"), pn + " joins you");
                }
            }
        }

        static int OuterLandsSeen
        {
            get
            {
                int n = 0;
                foreach (var b in new[] { BiomeId.Isles, BiomeId.Swamp, BiomeId.Desert, BiomeId.Blight, BiomeId.Glow }) if (Count("land:" + b) > 0) n++;
                return n;
            }
        }

        // ============================================================ PROGRESS
        public static bool IsDone(QuestDef q) => S.questsDone.Contains(q.id);

        /// <summary>The quest a line is on (null once the whole line is finished).</summary>
        public static QuestDef Current(QuestLine line)
        {
            foreach (var q in Lines[(int)line]) if (!IsDone(q) && On(q)) return q;
            return null;
        }

        public static int DoneCount(QuestLine line) { int n = 0; foreach (var q in Lines[(int)line]) if (IsDone(q) && On(q)) n++; return n; }
        public static int Total(QuestLine line) { int n = 0; foreach (var q in Lines[(int)line]) if (On(q)) n++; return n; }

        public static QuestLine TrackedLine
        {
            get => (QuestLine)Mathf.Clamp(S.questTrack, 0, Lines.Length - 1);
            set => S.questTrack = (int)value;
        }

        /// <summary>The quest shown on the HUD: the tracked line's, or the first unfinished line's once that one is done.</summary>
        public static QuestDef Tracked
        {
            get
            {
                if (G == null || G.Player == null) return null;
                var q = LineOn(TrackedLine) ? Current(TrackedLine) : null;
                for (int i = 0; q == null && i < Lines.Length; i++) if (LineOn((QuestLine)i)) q = Current((QuestLine)i);
                return q;
            }
        }

        public static string HudLine()
        {
            var q = Tracked;
            if (q == null) return null;
            QuestObj o = null;
            foreach (var x in q.objs) if (!x.Done) { o = x; break; }
            if (o == null) return $"<b>{q.title}</b>";
            return $"<b>{q.title}</b>\n<size=20>{o.text}{(o.need > 1 ? $"  {o.Have}/{o.need}" : "")}</size>";
        }

        /// <summary>Where the tracked quest wants you to go (gold marker on the minimap and the map).</summary>
        static Vector3? markerCache;
        static int markerFrame = -1;

        public static Vector3? Marker
        {
            get
            {
                if (markerFrame < 0 || Time.frameCount - markerFrame > 15)
                {
                    markerFrame = Time.frameCount;
                    var q = Tracked;
                    markerCache = q != null && q.where != null ? q.where() : null;
                }
                return markerCache;
            }
        }

        public static string RewardText(QuestDef q)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(q.gift)) parts.Add(q.gift);
            if (q.glimmer > 0) parts.Add($"{q.glimmer} Glimmer");
            foreach (var it in q.items) parts.Add($"{it.n}x {ContentDB.ItemName(it.id)}");
            if (q.xp > 0) parts.Add($"{q.xp} XP");
            return string.Join(", ", parts);
        }

        static void Finish(QuestDef q, bool loud)
        {
            S.questsDone.Add(q.id);
            S.glimmer += q.glimmer;
            foreach (var it in q.items) G.GiveItem(it.id, it.n, G.Player.Position + Vector3.up);
            if (q.xp > 0) G.AddPlayerXp(q.xp);
            if (q.onFinish != null) q.onFinish();
            if (!loud) return;
            if (q.line == QuestLine.Stars)
            {
                GameEvents.Good($"STAR!  {q.title}  ({DoneCount(QuestLine.Stars)}/{Total(QuestLine.Stars)})");
                G.Confetti(G.Player.Position + Vector3.up * 1.4f);
                SynthAudio.Play(Sfx.CaptureSuccess, 1.2f, 0.8f);
                return;
            }
            GameEvents.Good($"Quest complete: {q.title}");
            GameEvents.Say("Reward: " + RewardText(q), Gold);
            SynthAudio.Play(Sfx.LevelUp, 1.15f, 0.7f);
        }

        /// <summary>Says what a line moved on to after 'from' was finished.</summary>
        static void Announce(QuestDef from)
        {
            var next = Current(from.line);
            if (next == null) { GameEvents.Good($"{LineNames[(int)from.line]} - every quest complete!"); return; }
            if (next.chapter != from.chapter) GameEvents.Say(next.chapter, Gold);
            GameEvents.Say($"New quest: {next.title}  (QUESTS)", new Color(0.75f, 0.9f, 1f));
        }

        /// <summary>Finishes, without fanfare, every quest of a line from its current one on that is already done. Returns how many.</summary>
        static int Sweep(QuestLine line, ref int glimmer)
        {
            int n = 0;
            if (IsFree(line))
            {
                foreach (var s in Lines[(int)line])
                    if (!IsDone(s) && On(s) && s.Complete) { glimmer += s.glimmer; Finish(s, false); n++; }
                return n;
            }
            for (int guard = 0; guard < 200; guard++)
            {
                var q = Current(line);
                if (q == null || !q.Complete) break;
                glimmer += q.glimmer;
                Finish(q, false);
                n++;
            }
            return n;
        }

        /// <summary>A save from before the quest log: everything it has already done is ticked off in one go.</summary>
        static void CatchUp()
        {
            int n = 0, glim = 0;
            for (int l = 0; l < Lines.Length; l++) if (LineOn((QuestLine)l)) n += Sweep((QuestLine)l, ref glim);
            if (n > 0) GameEvents.Good($"Quest log: {n} quests you had already done are ticked off (+{glim} Glimmer and their rewards). Open QUESTS for what comes next.");
        }

        /// <summary>Called twice a second while the game runs: notes where you are, then finishes any quest whose objectives are all met.</summary>
        public static void Tick()
        {
            if (G == null || G.Player == null || !G.Player.IsAlive) return;
            var env = EnvironmentSystem.I;
            if (env != null && !Caves.InCave) Mark("land:" + env.CurrentBiome);
            if (G.Player.IsSwimming) Mark("swim");
            Vector3 me = G.Player.Position;
            foreach (var t in Settlements.All)
            {
                Vector3 d = t.pos - me; d.y = 0f;
                if (d.sqrMagnitude < 50f * 50f) Mark("town:" + t.id);
            }
            if (!S.questsStarted)
            {
                S.questsStarted = true;
                if (WorldRules.Current.simpleControls)
                {
                    // a Little Explorer starts with the stars on the HUD and tools already in hand (no crafting menus to learn)
                    S.questTrack = (int)QuestLine.Stars;
                    if (S.BestToolPower(ToolKind.Axe, out _) <= 1) G.GiveItem("stone_axe", 1, G.Player.Position + Vector3.up);
                    if (S.BestToolPower(ToolKind.Pick, out _) <= 1) G.GiveItem("stone_pick", 1, G.Player.Position + Vector3.up);
                }
                CatchUp();
                return;
            }
            for (int l = 0; l < Lines.Length; l++)
            {
                if (!LineOn((QuestLine)l)) continue;
                if (IsFree((QuestLine)l))
                {
                    // stars: whichever one has just been earned (one a tick, so each gets its moment)
                    foreach (var s in Lines[l]) if (!IsDone(s) && On(s) && s.Complete) { Finish(s, true); break; }
                    continue;
                }
                var q = Current((QuestLine)l);
                if (q == null || !q.Complete) continue;
                Finish(q, true);
                // anything further along that was already done goes with it, in one line instead of a wall of toasts
                int glim = 0, more = Sweep((QuestLine)l, ref glim);
                if (more > 0) GameEvents.Good($"...and {more} more you had already done (+{glim} Glimmer and their rewards).");
                Announce(q);
            }
        }
    }
}
