using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// What lives in a cave (cave pass, 2026-10-15). Every chamber has a role from the layout (`Caves.Layout`):
    ///   Pass - a group or two of wild kin, now and then a chest.
    ///   Colony - a nesting colony: a crowd of small kin round their nests; take the egg and they all come for you.
    ///   Hoard - ore veins (the land's ore, crystal, Prism Shards, and Geode Cores in the deepest caves) with territorial
    ///     kin watching them and a chest among them: they wake when you come near (`Watch`).
    ///   Eggs - three or four nests guarded by brooders who wake as you close in and rage if you take an egg. End caves
    ///     have rare eggs (a Rare kind, gold-flecked, better odds of a shimmer).
    ///   Rare - a Rare underground kin, End caves only.
    ///   Mini - a mini-boss (a big non-alpha, x2.2 health) on a sealed chest with a cave-only blueprint.
    ///   Alpha - an alpha guardian on a sealed chest; the one at the end of the main path opens the way out.
    ///   Boss - an Early cave's only guardian: one big common kin.
    /// Underground-only kin (`CaveKin`) mix with the land's own. Guardians show the HUD boss bar while they fight.
    /// Nothing that can be looted (guardians, chests, eggs, veins) comes back until the cave refreshes (`RefreshSeconds`).
    /// </summary>
    public partial class Caves
    {
        public const string CoreItem = "geode_core";

        /// <summary>The underground-only kin and the shallowest tier each first appears in.</summary>
        static readonly (string id, CaveTier tier)[] CaveKinTable =
        {
            ("pebblit", CaveTier.Early), ("echobat", CaveTier.Early), ("dripslug", CaveTier.Early),
            ("glimcrab", CaveTier.Mid), ("mushmaw", CaveTier.Mid), ("stalagrin", CaveTier.Mid),
            ("gemhorn", CaveTier.End), ("deepcoil", CaveTier.End),
        };
        public static IEnumerable<string> CaveKin(CaveTier t) { foreach (var k in CaveKinTable) if (k.tier <= t) yield return k.id; }
        /// <summary>Wiki / Dex: the tier a cave kin starts appearing at (or -1).</summary>
        public static int CaveKinTier(string id) { foreach (var k in CaveKinTable) if (k.id == id) return (int)k.tier; return -1; }

        class Guardian
        {
            public CreatureActor a; public CreatureData d;
            public Role role; public string title; public float wake;
            public Vector3 home;   // the middle of its chamber
            public Chest chest; public bool down, woke, main;
        }

        /// <summary>
        /// Kin that wake together when you come near. 'max' wakes only the nearest few at once (a colony comes at you in
        /// waves, not all at the same time); the rest wake 'gap' seconds later if you are still about.
        /// </summary>
        class Watch { public Vector3 pos; public float radius; public readonly List<CreatureActor> who = new List<CreatureActor>(); public bool woke; public string warn; public int max = 99; public float gap = 7f, rearmAt; public bool warned; }

        class EggNest { public Vector3 pos; public GameObject egg; public SpeciesDef sp; public bool rare, taken; public Watch watch; }

        enum ChestKind { Small, Guarded, Blueprint, Big, Boss, Secret, Vault }

        class Chest { public Vector3 pos; public GameObject go; public Transform lid; public bool opened, sealedUntilDown; public ChestKind kind; public Guardian guard; }

        // ================================================================ SPAWN
        void SpawnContents(Run r, System.Random rng)
        {
            var e = r.e;
            float u() => (float)rng.NextDouble();
            // the land's kin, with stone and shade ones (cave dwellers) favoured, plus the underground ones of this depth
            var pool = new List<SpeciesDef>(); var weights = new List<float>();
            foreach (var s in ContentDB.SpeciesList)
            {
                if (s.starter || s.bossOnly || s.underground || s.custom || !s.LivesIn(e.biome) || s.rarity >= Rarity.Epic) continue;
                float w = 1f;
                if (s.element1 == Element.Stone || s.element1 == Element.Shade || s.element2 == Element.Stone || s.element2 == Element.Shade) w = 2.5f;
                pool.Add(s); weights.Add(w);
            }
            foreach (var id in CaveKin(r.tier))
            {
                var s = ContentDB.Sp(id);
                if (s == null || s.rarity >= Rarity.Rare) continue;
                pool.Add(s); weights.Add(s.rarity == Rarity.Common ? 3f : 2.2f);
            }
            float total = 0f; foreach (var w in weights) total += w;
            SpeciesDef Pick(System.Predicate<SpeciesDef> ok = null)
            {
                for (int tries = 0; tries < 30; tries++)
                {
                    float x = u() * total;
                    for (int i = 0; i < pool.Count; i++)
                    {
                        x -= weights[i];
                        if (x > 0f) continue;
                        if (ok == null || ok(pool[i])) return pool[i];
                        break;
                    }
                }
                return pool.Count > 0 ? pool[rng.Next(pool.Count)] : ContentDB.Sp("pebblit");
            }
            var guardKinds = new HashSet<string>();   // guardians and rare finds: no kind twice in one cave
            SpeciesDef Cave(params string[] ids)
            {
                var ok = new List<SpeciesDef>();
                foreach (var id in ids) { var s = ContentDB.Sp(id); if (s != null && CaveKinTier(id) <= (int)r.tier) ok.Add(s); }
                var fresh = ok.FindAll(s => !guardKinds.Contains(s.id));
                return fresh.Count > 0 ? fresh[rng.Next(fresh.Count)] : ok.Count > 0 ? ok[rng.Next(ok.Count)] : null;
            }
            SpeciesDef LandRare()
            {
                var rares = new List<SpeciesDef>();
                foreach (var s in pool) if (s.rarity == Rarity.Rare && !s.underground && !guardKinds.Contains(s.id)) rares.Add(s);
                if (rares.Count == 0) foreach (var s in ContentDB.SpeciesList) if (s.rarity == Rarity.Rare && s.LivesIn(e.biome) && !s.starter && !s.bossOnly && !guardKinds.Contains(s.id)) rares.Add(s);
                return rares.Count > 0 ? rares[rng.Next(rares.Count)] : null;
            }
            int Lv() => rng.Next(e.minLv, e.maxLv + 1);

            CreatureActor Wild(SpeciesDef sp, int lv, Vector3 at)
            {
                var d = CreatureFactory.RollWildMaybeEvolved(sp, lv, e.biome, false);
                var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, at);
                a.Persistent = true;
                r.kin.Add(a);
                return a;
            }
            void Group(Room rm, SpeciesDef sp, int n)
            {
                var c = OpenSpot(r, rm.c, rm.r * 0.6f, 2f, rng);
                for (int k = 0; k < n; k++) Wild(sp, Lv(), Nudge(r, c + new Vector3((k % 2) * 1.8f, 0f, (k / 2) * 1.6f)));
            }
            Guardian Guard(Room rm, Role role, SpeciesDef sp, bool alpha, int lv, float tough, string title, bool main)
            {
                if (sp == null) return null;
                guardKinds.Add(sp.id);
                var d = CreatureFactory.RollWild(sp, lv, e.biome, alpha);
                if (role == Role.Boss) d.mutation = Mutation.Giant;   // a shallow cave's boss: one of the land's own kind, grown huge
                var at = Nudge(r, new Vector3(rm.c.x, FloorAt2(r, rm.c.x, rm.c.y), rm.c.y));
                var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, at);
                a.Persistent = true;
                if (tough > 1f) a.Toughen(tough);
                if (role == Role.Mini) a.BossAtkMul = 1.1f;
                var g = new Guardian { a = a, d = d, role = role, title = title, wake = Mathf.Min(16f, rm.r * 1.05f), main = main, home = rm.Center };
                r.guards.Add(g);
                return g;
            }

            bool quiet = r.quiet;
            for (int ri = 1; ri < r.rooms.Count; ri++)
            {
                var rm = r.rooms[ri];
                switch (rm.role)
                {
                    case Role.Pass:
                    {
                        int groups = r.tier == CaveTier.Early ? 1 : 1 + (u() < 0.4f ? 1 : 0);
                        for (int g = 0; g < groups; g++) Group(rm, Pick(), u() < 0.55f ? 1 : 2);
                        if (!quiet && u() < (r.tier == CaveTier.Early ? 0.5f : 0.4f)) AddChest(r, EdgeSpot(r, ri, rng), ChestKind.Small, null);
                        break;
                    }
                    case Role.Colony:
                    {
                        // a crowd of one small kind, roosting round their nests
                        var sp = Cave("echobat", "pebblit") ?? Pick(s => s.rarity == Rarity.Common);
                        var w = new Watch { pos = rm.Center, radius = rm.r * 0.45f, warn = $"The {sp.displayName} colony stirs!", max = r.tier == CaveTier.End ? 3 : 2 };
                        int n = r.tier == CaveTier.End ? 6 + rng.Next(0, 2) : 4 + rng.Next(0, 2);
                        for (int k = 0; k < n; k++) w.who.Add(Wild(sp, Mathf.Max(1, e.minLv - 2 + rng.Next(0, 3)), OpenSpot(r, rm.c, rm.r * 0.75f, 1.2f, rng)));
                        r.watches.Add(w);
                        if (!quiet) for (int k = 0; k < 2; k++) AddNest(r, OpenSpot(r, rm.c, rm.r * 0.55f, 2f, rng), sp, false, k == 0 ? w : null, k == 0);
                        break;
                    }
                    case Role.Hoard:
                    {
                        var c = OpenSpot(r, rm.c, rm.r * 0.3f, 3f, rng);
                        var w = new Watch { pos = rm.Center, radius = rm.r * 0.95f + 1f, warn = "Something guards these veins - and it has seen you." };
                        int guards = r.tier == CaveTier.End ? 3 : 2;
                        for (int k = 0; k < guards; k++)
                        {
                            var sp = (k == 0 ? Cave("glimcrab", "stalagrin", "mushmaw") : null) ?? Pick(s => s.temperament == Temperament.Territorial || s.temperament == Temperament.Aggressive);
                            var a = Wild(sp, e.maxLv, OpenSpot(r, new Vector2(c.x, c.z), 4.5f, 1.4f, rng));
                            a.Toughen(1.25f);
                            w.who.Add(a);
                        }
                        r.watches.Add(w);
                        if (!quiet)
                        {
                            int veins = r.tier == CaveTier.End ? 5 : 3 + rng.Next(0, 2);
                            for (int k = 0; k < veins; k++)
                            {
                                PropType t = k == 0 ? LandVein(e.biome) : r.tier == CaveTier.End && k < 3 ? PropType.GeodeVein : k == veins - 1 && u() < 0.5f ? PropType.PrismVein : u() < 0.5f ? PropType.CrystalCluster : LandVein(e.biome);
                                AddVein(r, EdgeSpot(r, ri, rng, 1.6f), t, rng);
                            }
                            AddChest(r, Nudge(r, c + new Vector3(1.4f, 0f, 1f)), ChestKind.Guarded, null);
                        }
                        break;
                    }
                    case Role.Eggs:
                    {
                        if (quiet) { Group(rm, Pick(), 2); break; }
                        bool rareRoom = r.tier == CaveTier.End;
                        var sp = Cave("glimcrab", "mushmaw", "stalagrin", "dripslug") ?? Pick(s => s.rarity == Rarity.Uncommon);
                        var w = new Watch { pos = rm.Center, radius = 7.5f, warn = $"The brooding {sp.displayName} rise to guard their eggs!" };
                        for (int k = 0; k < 2; k++)
                        {
                            var a = Wild(sp, e.maxLv + 1, OpenSpot(r, rm.c, rm.r * 0.5f, 1.6f, rng));
                            a.Toughen(1.4f);
                            w.who.Add(a);
                        }
                        r.watches.Add(w);
                        int nests = rareRoom ? 4 : 3;
                        for (int k = 0; k < nests; k++)
                        {
                            bool rare = rareRoom && k < 2;
                            var esp = rare ? (Cave("gemhorn", "deepcoil") ?? LandRare() ?? sp) : sp;
                            AddNest(r, OpenSpot(r, rm.c, rm.r * 0.65f, 1.8f, rng), esp, rare, w, false);
                        }
                        break;
                    }
                    case Role.Hazard:
                        Group(rm, Pick(), 1 + (u() < 0.5f ? 1 : 0));
                        break;
                    case Role.Descent:
                        Group(rm, Pick(), 1);
                        break;
                    case Role.Secret:
                        if (!quiet) AddChest(r, Nudge(r, rm.Center), ChestKind.Secret, null);
                        break;
                    case Role.Vault:
                        if (!quiet) AddChest(r, Nudge(r, rm.Center), ChestKind.Vault, null);
                        break;
                    case Role.Rare:
                    {
                        var sp = Cave("gemhorn", "deepcoil") ?? LandRare() ?? Pick();
                        var a = Wild(sp, e.maxLv + 1, OpenSpot(r, rm.c, rm.r * 0.4f, 2f, rng));
                        a.Toughen(1.3f);
                        Group(rm, Pick(), 1);
                        break;
                    }
                    case Role.Mini:
                    {
                        if (quiet) { Group(rm, Pick(), 2); break; }
                        // the first mini-boss of a deep cave is an underground Rare, the second one of the land's
                        bool firstMini = !r.guards.Exists(x => x.role == Role.Mini);
                        var sp = r.tier == CaveTier.End ? (firstMini ? Cave("gemhorn", "deepcoil") ?? LandRare() : LandRare() ?? Cave("gemhorn", "deepcoil")) : (Cave("glimcrab", "mushmaw", "stalagrin") ?? LandRare());
                        var g = Guard(rm, Role.Mini, sp ?? Pick(), false, e.maxLv + 2, 2.2f, "Mini-boss", false);
                        if (g != null) g.chest = AddChest(r, ChestSpot(r, rm), ChestKind.Blueprint, g);
                        break;
                    }
                    case Role.Alpha:
                    case Role.Boss:
                    {
                        if (quiet) { Group(rm, Pick(), 2); break; }
                        bool main = rm.branch < 0;
                        SpeciesDef sp;
                        if (rm.role == Role.Boss)
                            sp = u() < 0.7f ? Pick(s => s.rarity == Rarity.Common && s.size >= 0.6f && s.temperament != Temperament.Timid) : (Cave("pebblit", "dripslug") ?? Pick(s => s.rarity == Rarity.Common));
                        else if (main) sp = ContentDB.Sp(e.boss) ?? LandRare() ?? Pick();
                        else sp = Cave("deepcoil", "gemhorn") ?? LandRare() ?? Pick();
                        Guardian g = rm.role == Role.Boss
                            ? Guard(rm, Role.Boss, sp, false, e.maxLv + 2, 1.8f, e.kind == CaveKind.Burrow ? "Den boss" : "Cave boss", true)
                            : Guard(rm, Role.Alpha, sp, true, e.maxLv + (main ? 3 : 4), 1f, main ? "Guardian" : "Deep alpha", main);
                        if (g == null) break;
                        g.chest = AddChest(r, ChestSpot(r, rm), rm.role == Role.Boss ? ChestKind.Boss : ChestKind.Big, g);
                        if (e.kind == CaveKind.Ruin && rm.role == Role.Alpha && main)
                            for (int k = -1; k <= 1; k += 2)
                                Wild(Pick(s => s.rarity <= Rarity.Uncommon), e.maxLv, Nudge(r, rm.Center + new Vector3(k * 5f, 0f, -3f)));
                        break;
                    }
                }
            }
            if (quiet) r.cleared = true;
            // a ruin's ambush: the middle pass chamber looks empty (one chest in plain sight)
            if (e.kind == CaveKind.Ruin)
                for (int ri = 1; ri < r.rooms.Count; ri++)
                {
                    if (r.rooms[ri].role != Role.Pass) continue;
                    r.ambushPos = r.rooms[ri].Center;
                    r.ambushSp = Pick(s => s.rarity <= Rarity.Uncommon);
                    r.ambushSet = true;
                    break;
                }
        }

        /// <summary>Lifts a spot onto the floor and out of any rock it landed in.</summary>
        static Vector3 Nudge(Run r, Vector3 p)
        {
            for (int it = 0; it < 6 && Sample(r.F, p.x, p.z) < 1f; it++)
            {
                var g = Grad(r, p.x, p.z);
                p.x += g.x * 0.8f; p.z += g.y * 0.8f;
            }
            p.y = FloorAt2(r, p.x, p.z);
            return p;
        }

        /// <summary>A spot near the rock face of a chamber, 'inward' metres out from it.</summary>
        Vector3 EdgeSpot(Run r, int ri, System.Random rng, float inward = 1.3f)
        {
            var rm = r.rooms[ri];
            for (int k = 0; k < 60; k++)
            {
                int s = rng.Next(r.segs.Count);
                var sg = r.segs[s];
                var m = new Vector2((sg.x + sg.z) * 0.5f, (sg.y + sg.w) * 0.5f);
                if ((m - rm.c).magnitude > rm.r * 1.3f) continue;
                var n = r.segNormals[s];
                var p = new Vector3(m.x + n.x * inward, 0f, m.y + n.y * inward);
                if (Sample(r.F, p.x, p.z) < inward * 0.6f) continue;
                p.y = FloorAt2(r, p.x, p.z);
                return p;
            }
            return OpenSpot(r, rm.c, rm.r * 0.6f, 1.5f, rng);
        }

        /// <summary>Behind the guardian: toward the far side of the chamber from the way in.</summary>
        static Vector3 ChestSpot(Run r, Room rm)
        {
            var from = rm.parent >= 0 ? r.rooms[rm.parent].c : rm.c - Vector2.right;
            var back = (rm.c - from).normalized;
            var p = rm.c + back * rm.r * 0.45f;
            return Nudge(r, new Vector3(p.x, 0f, p.y));
        }

        static PropType LandVein(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Badlands: return PropType.CinderRock;
                case BiomeId.Peaks: return PropType.CrystalCluster;
                case BiomeId.Isles: return PropType.SaltglassVein;
                case BiomeId.Swamp: return PropType.BogIronVein;
                case BiomeId.Desert: return PropType.SunsteelVein;
                case BiomeId.Blight: return PropType.BlightVein;
                case BiomeId.Glow: return PropType.StarcrystalVein;
                default: return PropType.OreRock;
            }
        }

        // ================================================================ VEINS
        void AddVein(Run r, Vector3 p, PropType t, System.Random rng)
        {
            var n = new ResourceNode { key = -(1 + r.nodes.Count), type = t, pos = p, rotY = (float)rng.NextDouble() * 360f, scale = 1f, alive = true };
            n.hp = n.Info.hp;
            var go = new GameObject("Vein_" + t);
            go.transform.SetParent(r.root.transform, false);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, n.rotY, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = PropCatalog.Mesh(t, false);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Props;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            r.nodes.Add(n);
            r.nodeGo.Add(go);
            r.nodePillar.Add(r.pillars.Count);
            r.pillars.Add(new Vector4(p.x, p.z, n.Radius, 0));
        }

        /// <summary>The vein in front of the player (ChunkManager.FindNode asks here while underground).</summary>
        public ResourceNode FindNode(Vector3 pos, Vector3 forward, float range)
        {
            if (run == null) return null;
            ResourceNode best = null; float bestScore = float.MaxValue;
            foreach (var n in run.nodes)
            {
                if (!n.alive) continue;
                var d = n.pos - pos; d.y = 0f;
                float dist = d.magnitude - n.Radius;
                if (dist > range) continue;
                float score = dist - (dist > 0.01f ? Vector3.Dot(d.normalized, forward) : 1f) * 1.2f;
                if (score < bestScore) { bestScore = score; best = n; }
            }
            return best;
        }

        /// <summary>A vein was mined out: hide it until the cave refreshes.</summary>
        public void NodeGone(ResourceNode n)
        {
            if (run == null) return;
            int i = run.nodes.IndexOf(n);
            if (i < 0) return;
            if (run.nodeGo[i] != null) run.nodeGo[i].SetActive(false);
            run.pillars[run.nodePillar[i]] = new Vector4(1e6f, 1e6f, 0f, 0f);
            run.done.Add(Key(run, "v", i));
        }

        // ================================================================ EGGS
        static Mesh nestMesh, eggMesh, rareEggMesh;

        void AddNest(Run r, Vector3 p, SpeciesDef sp, bool rare, Watch watch, bool withEgg = true)
        {
            if (nestMesh == null)
            {
                var mb = new MeshBuilder();
                Color32 twig = new Color(0.42f, 0.32f, 0.22f), twigLt = new Color(0.55f, 0.43f, 0.3f);
                for (int k = 0; k < 9; k++)
                {
                    float a = k * 40f;
                    mb.Cylinder(Matrix4x4.TRS(Quaternion.Euler(0, a, 0) * new Vector3(0, 0.12f, 0.42f), Quaternion.Euler(0, a + 90f, 80f), new Vector3(0.07f, 0.6f, 0.07f)), k % 2 == 0 ? twig : twigLt, 4);
                }
                mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.05f, 0), Quaternion.identity, new Vector3(0.9f, 0.1f, 0.9f)), new Color(0.3f, 0.24f, 0.18f), 10);
                nestMesh = mb.ToMesh("cave_nest");
                var eb = new MeshBuilder();
                eb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.3f, 0), Quaternion.identity, new Vector3(0.36f, 0.46f, 0.36f)), new Color(0.92f, 0.88f, 0.78f), new Color(0.8f, 0.76f, 0.66f), 10, 7);
                for (int k = 0; k < 7; k++)
                    eb.Sphere(Matrix4x4.TRS(Quaternion.Euler(k * 23f, k * 97f, 0) * new Vector3(0, 0.19f, 0.06f) + new Vector3(0, 0.3f, 0), Quaternion.identity, Vector3.one * 0.07f), new Color(0.5f, 0.42f, 0.6f), 5, 3);
                eggMesh = eb.ToMesh("cave_egg");
                var rb = new MeshBuilder();
                rb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.34f, 0), Quaternion.identity, new Vector3(0.42f, 0.54f, 0.42f)), new Color(0.98f, 0.92f, 0.7f), new Color(0.86f, 0.74f, 0.5f), 10, 7);
                for (int k = 0; k < 9; k++)
                    rb.Sphere(Matrix4x4.TRS(Quaternion.Euler(k * 21f, k * 83f, 0) * new Vector3(0, 0.22f, 0.07f) + new Vector3(0, 0.34f, 0), Quaternion.identity, Vector3.one * 0.08f), new Color32(255, 200, 70, 0), 5, 3);
                rareEggMesh = rb.ToMesh("cave_egg_rare");
            }
            p = Nudge(r, p);
            var go = new GameObject(rare ? "RareNest" : "Nest");
            go.transform.SetParent(r.root.transform, false);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, p.x * 37f % 360f, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = nestMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            GameObject egg = null;
            if (withEgg || watch != null)
            {
                egg = new GameObject("Egg");
                egg.transform.SetParent(go.transform, false);
                egg.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                egg.AddComponent<MeshFilter>().sharedMesh = rare ? rareEggMesh : eggMesh;
                egg.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            }
            r.nests.Add(new EggNest { pos = p, egg = egg, sp = sp, rare = rare, watch = watch, taken = egg == null });
        }

        void TakeEgg(EggNest n)
        {
            if (n.taken) return;
            var gm = GameManager.I;
            var s = gm.Player.Stats;
            if (s.eggs.Count >= KinGrowth.MaxEggs) { GameEvents.Warn($"You are already carrying {KinGrowth.MaxEggs} eggs. Hatch one first (Camp > Hatchery)."); return; }
            var child = CreatureFactory.RollWild(n.sp, 1, run.e.biome, false);
            child.location = KinLocation.Box;
            if (!child.shimmer && Random.value < (n.rare ? 0.15f : 0.06f)) Shimmer.Make(child);
            int lift = n.rare ? 12 : 8;
            for (int i = 0; i < 3; i++) child.apt[i] = Mathf.Min(100, child.apt[i] + lift);
            s.eggs.Add(new KinEgg { child = child, readyUnix = Expeditions.Now + KinGrowth.HatchMinutes * 60L, parents = n.rare ? "a rare cave nest" : "a cave nest" });
            n.taken = true;
            run.done.Add(Key(run, "n", run.nests.IndexOf(n)));
            if (n.egg != null) n.egg.SetActive(false);
            gm.AddPlayerXp(n.rare ? 60 : 25);
            Fx.Burst(n.pos + Vector3.up * 0.6f, n.rare ? new Color(1f, 0.85f, 0.4f) : Color.Lerp(n.sp.palette.glow, Color.white, 0.4f), 16, 4f, 0.25f, 0.6f, true);
            SynthAudio.Play(Sfx.CaptureSuccess, 1.3f);
            string what = n.rare ? $"a rare {n.sp.displayName} egg" : $"the {n.sp.displayName} egg";
            GameEvents.Good(KinGrowth.HasHatchery ? $"You take {what}. It hatches in {KinGrowth.HatchMinutes} minutes (Camp > Hatchery)." : $"You take {what}. Build a Hatchery at camp to hatch it.");
            if (n.watch != null)
            {
                bool any = false;
                foreach (var a in n.watch.who)
                {
                    if (a == null || !a.gameObject.activeSelf || !a.IsAlive || a.Mode != CreatureMode.Wild) continue;
                    if ((a.Position - n.pos).sqrMagnitude > 13f * 13f) continue;   // the ones close enough to see
                    a.BossAtkMul = Mathf.Max(a.BossAtkMul, 1.25f);
                    a.Provoke(gm.Player);
                    any = true;
                }
                if (any) GameEvents.Warn("They saw that. They're furious!");
                n.watch.woke = true;
            }
            GameEvents.RaiseInventoryChanged();
        }

        // ================================================================ CHESTS
        Chest AddChest(Run r, Vector3 p, ChestKind kind, Guardian g)
        {
            bool big = kind == ChestKind.Big || kind == ChestKind.Blueprint || kind == ChestKind.Vault || kind == ChestKind.Secret;
            float s = big ? 1.4f : 1f;
            var mb = new MeshBuilder();
            Color32 wood = new Color(0.45f, 0.3f, 0.18f), band = kind == ChestKind.Blueprint ? new Color(0.55f, 0.85f, 1f) : kind == ChestKind.Secret ? new Color(0.8f, 0.6f, 1f) : big ? new Color(1f, 0.82f, 0.35f) : new Color(0.7f, 0.7f, 0.75f);
            mb.Box(Matrix4x4.TRS(new Vector3(0, 0.3f * s, 0), Quaternion.identity, new Vector3(1.1f * s, 0.6f * s, 0.75f * s)), wood, new Color(0.55f, 0.38f, 0.22f));
            mb.Box(Matrix4x4.TRS(new Vector3(-0.38f * s, 0.3f * s, 0), Quaternion.identity, new Vector3(0.1f * s, 0.62f * s, 0.78f * s)), band);
            mb.Box(Matrix4x4.TRS(new Vector3(0.38f * s, 0.3f * s, 0), Quaternion.identity, new Vector3(0.1f * s, 0.62f * s, 0.78f * s)), band);
            mb.Box(Matrix4x4.TRS(new Vector3(0, 0.3f * s, 0.38f * s), Quaternion.identity, new Vector3(0.18f * s, 0.25f * s, 0.04f)), band);
            var go = new GameObject(kind + "Chest");
            go.transform.SetParent(r.root.transform, false);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, (float)(p.x * 13.7 % 360.0), 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cave_chest");
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            var lmb = new MeshBuilder();
            lmb.Box(Matrix4x4.TRS(new Vector3(0, 0.1f * s, 0.375f * s), Quaternion.identity, new Vector3(1.14f * s, 0.22f * s, 0.78f * s)), new Color(0.5f, 0.34f, 0.2f), band);
            if (kind == ChestKind.Blueprint) lmb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.26f * s, 0.375f * s), Quaternion.identity, Vector3.one * 0.2f * s), new Color32(140, 220, 255, 0), 8, 5);
            var lid = new GameObject("Lid");
            lid.transform.SetParent(go.transform, false);
            lid.transform.localPosition = new Vector3(0, 0.6f * s, -0.375f * s);
            lid.AddComponent<MeshFilter>().sharedMesh = lmb.ToMesh("cave_chest_lid");
            lid.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            var c = new Chest { pos = p, go = go, lid = lid.transform, kind = kind, guard = g, sealedUntilDown = g != null };
            r.chests.Add(c);
            r.pillars.Add(new Vector4(p.x, p.z, 0.75f * s, 0));
            return c;
        }

        void OpenChest(Chest c)
        {
            if (c.opened) return;
            c.opened = true;
            run.done.Add(Key(run, "c", run.chests.IndexOf(c)));
            var gm = GameManager.I;
            var e = run.e;
            var tier = run.tier;
            var drop = c.pos + Vector3.up;
            bool big = c.kind == ChestKind.Big, early = tier == CaveTier.Early;
            bool hidden = c.kind == ChestKind.Secret || c.kind == ChestKind.Vault;   // behind a cracked wall or a puzzle gate
            int mul = big || hidden ? 2 : 1;
            int glim = 0;
            var got = new List<string>();
            void Give(string id, int n) { if (n <= 0 || ContentDB.Item(id) == null) return; gm.GiveItem(id, n, drop); got.Add($"{n} {ContentDB.ItemName(id)}"); }
            // materials by land (an Early cave's chests are lighter)
            int lo = early ? 1 : 2, hi = early ? 3 : 5;
            switch (e.biome)
            {
                case BiomeId.Meadow: Give("ore", Random.Range(lo + 1, hi + 2) * mul); Give(Random.value < 0.5f ? "gel" : "pelt", Random.Range(1, 3) * mul); Give("tether_seed", Random.Range(2, early ? 4 : 6) * mul); glim = 10; break;
                case BiomeId.Badlands: Give("ingot", Random.Range(lo, hi) * mul); Give("cinder", Random.Range(lo, hi) * mul); Give("cinder_seed", Random.Range(1, 4) * mul); glim = 20; break;
                case BiomeId.Peaks: Give("crystal", Random.Range(lo, hi) * mul); Give("ingot", Random.Range(lo, hi) * mul); Give("rime_seed", Random.Range(1, 3) * mul); glim = 30; break;
                case BiomeId.Isles: Give("saltglass", Random.Range(lo, hi) * mul); Give("ironbark_seed", Random.Range(2, 5) * mul); glim = 20; break;
                case BiomeId.Swamp: Give("bog_iron", Random.Range(lo, hi) * mul); Give("cinder_seed", Random.Range(2, 4) * mul); glim = 30; break;
                case BiomeId.Desert: Give("sunsteel_ore", Random.Range(lo, hi) * mul); Give(Random.value < 0.6f ? "rime_seed" : "sunforged_seed", Random.Range(2, 4) * mul); glim = 45; break;
                case BiomeId.Blight: Give("blight_ore", Random.Range(lo, hi) * mul); Give("sunforged_seed", Random.Range(2, 4) * mul); glim = 60; break;
                default: Give("starcrystal", Random.Range(lo, hi) * mul); Give("starbloom_seed", Random.Range(2, 4) * mul); glim = 80; break;
            }
            if (Random.value < 0.35f) Give("salve", Random.Range(1, 3));
            // the deep places' own treasure
            if (tier == CaveTier.End && (big || c.kind == ChestKind.Blueprint || c.kind == ChestKind.Guarded))
                Give(CoreItem, big ? Random.Range(3, 6) : Random.Range(1, 3));
            if (big || c.kind == ChestKind.Secret || (tier == CaveTier.End && c.kind == ChestKind.Blueprint)) Give(Shimmer.ShardItem, Random.Range(2, 5));
            if (hidden && early) Give("tether_seed", Random.Range(3, 6));
            // a blueprint from a mini-boss (and sometimes the deep alpha)
            string bp = null;
            if (c.kind == ChestKind.Blueprint || (big && tier == CaveTier.End && c.guard != null && !c.guard.main && Random.value < 0.5f)
                || (hidden && !early && Random.value < (c.kind == ChestKind.Vault ? 0.5f : 0.35f)))
            {
                bp = PickBlueprint(tier == CaveTier.End ? 2 : 1, e.biome);
                if (bp == null) Give(Shimmer.ShardItem, 4);
            }
            // gear: guaranteed from a guardian's big chest, sometimes otherwise
            if (big || Random.value < (c.kind == ChestKind.Boss ? 0.3f : c.kind == ChestKind.Vault ? 0.4f : c.kind == ChestKind.Secret ? 0.3f : c.kind == ChestKind.Guarded ? 0.25f : 0.12f))
            {
                string g = GearForTier(e.biome);
                if (g != null) { gm.GiveItem(g, 1, drop); GameEvents.Good($"Found gear: {ContentDB.ItemName(g)}!"); }
            }
            float gmul = big ? 3f : hidden ? 2.5f : c.kind == ChestKind.Blueprint ? 2f : c.kind == ChestKind.Guarded ? 1.5f : c.kind == ChestKind.Boss ? 1.5f : 1f;
            glim = Mathf.RoundToInt(glim * gmul * (early ? 0.7f : 1f) * Random.Range(0.8f, 1.25f));
            gm.Player.Stats.glimmer += glim;
            got.Add($"{glim} Glimmer");
            GameEvents.Good(string.Join(", ", got));
            if (bp != null)
            {
                gm.Player.Stats.blueprints.Add(bp);
                GameEvents.Say(bp.StartsWith("house:") ? $"Cave blueprint: {Chests.BlueprintName(bp)}! Build it from BUILD > HOUSE." : $"Cave blueprint: {ContentDB.ItemName(bp)}! Make it at the Workbench.", new Color(0.6f, 0.9f, 1f));
                gm.Confetti(drop);
            }
            gm.AddPlayerXp(big ? 80 : hidden ? 60 : c.kind == ChestKind.Blueprint ? 60 : 25);
            Fx.Burst(drop, big ? new Color(1f, 0.85f, 0.35f) : c.kind == ChestKind.Blueprint ? new Color(0.6f, 0.9f, 1f) : new Color(0.9f, 0.9f, 1f), big ? 30 : 16, 5f, 0.3f, 0.7f, true);
            SynthAudio.Play(Sfx.CaptureSuccess, big ? 0.9f : 1.2f);
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>A cave blueprint of this depth not yet found; failing that, one of the land's own.</summary>
        static string PickBlueprint(int depth, BiomeId land)
        {
            var s = GameManager.I.Player.Stats;
            var open = new List<string>();
            foreach (var b in Chests.Blueprints) if (b.cave == depth && !s.blueprints.Contains(b.recipe)) open.Add(b.recipe);
            if (open.Count == 0) foreach (var b in Chests.Blueprints) if (b.cave == 0 && b.land == land && !s.blueprints.Contains(b.recipe)) open.Add(b.recipe);
            return open.Count > 0 ? open[Random.Range(0, open.Count)] : null;
        }

        static string GearForTier(BiomeId b)
        {
            // the outer lands' own armour is forged from their ore (camp tech), so their chests hold older sets and trinkets
            string[] pool = b == BiomeId.Meadow
                ? new[] { "explorer_hood", "explorer_coat", "explorer_boots", "gatherers_bracelet", "binders_bracelet", "hearty_amulet", "mercy_charm" }
                : b == BiomeId.Badlands || b == BiomeId.Isles
                    ? new[] { "ironbark_helm", "ironbark_mail", "ironbark_greaves", "guard_ring", "windrunner_ring", "keen_ring", "thornweave_vest" }
                    : b == BiomeId.Peaks || b == BiomeId.Swamp
                        ? new[] { "rime_crown", "rimecloak", "rime_treads", "ember_helm", "ember_plate", "ember_greaves", "glowstone_pendant" }
                        : new[] { "guard_ring", "windrunner_ring", "keen_ring", "glowstone_pendant", "mercy_charm", "hearty_amulet", "binders_bracelet", "thornweave_vest" };
            var ok = new List<string>();
            foreach (var id in pool) if (ContentDB.Item(id) != null) ok.Add(id);
            return ok.Count > 0 ? ok[Random.Range(0, ok.Count)] : null;
        }

        // ================================================================ GUARDIANS
        void TickGuardians(Run r, PlayerController pl)
        {
            Guardian show = null; float showD = 38f * 38f;
            foreach (var g in r.guards)
            {
                if (g.down) continue;
                bool gone = g.a == null || g.a.Data != g.d || !g.a.gameObject.activeSelf || g.a.State == AIState.Dead;
                if (gone) { GuardianDown(r, g); continue; }
                float d2 = (g.a.Position - pl.Position).sqrMagnitude;
                if (!g.woke && pl.IsAlive && d2 < g.wake * g.wake)
                {
                    g.woke = true;
                    if (!g.a.Aggro) g.a.Provoke(pl);
                    string who = g.a.Sp.displayName;
                    GameEvents.Warn(g.role == Role.Alpha ? $"The {(g.main ? "guardian" : "deep alpha")}, an alpha {who}, rises!" : g.role == Role.Mini ? $"A huge {who} blocks the way to its hoard!" : $"The {g.title.ToLowerInvariant()}, a big {who}, bristles!");
                    SynthAudio.Play(Sfx.Summon, 0.55f);
                }
                if (g.woke && d2 < showD) { show = g; showD = d2; }
            }
            if (HUD.I == null) return;
            if (show != null)
            {
                string t = show.role == Role.Alpha ? (show.main ? "Guardian" : "Deep alpha") : show.title;
                HUD.I.SetBoss($"{t}: {show.a.Data.DisplayName}  <size=19>Lv {show.a.Data.level}</size>", show.a.HpFrac,
                    show.role == Role.Alpha ? new Color(0.9f, 0.35f, 0.35f) : new Color(0.95f, 0.6f, 0.3f));
                bossBar = true;
            }
            else if (bossBar) { HUD.I.SetBoss(null, 0f, Color.white); bossBar = false; }
        }

        void GuardianDown(Run r, Guardian g)
        {
            g.down = true;
            r.done.Add(Key(r, "g", r.guards.IndexOf(g)));
            if (g.chest != null && g.chest.sealedUntilDown)
            {
                g.chest.sealedUntilDown = false;
                Fx.Sparkle(g.chest.pos + Vector3.up, new Color(1f, 0.9f, 0.5f), 24, 1.2f);
            }
            if (g.main)
            {
                // the way out opens; the cave counts as cleared
                r.cleared = true;
                var rec = Record(r.e.id);
                rec.clearedAt = GameManager.WorldTime;
                Bounties.OnCaveCleared(r.e.id);
                OpenGuardianPortal(r, g);
                GameEvents.Good(g.role == Role.Boss ? "The boss is down! Its chest is open and a way out appears." : "The guardian falls! Its sealed chest opens, and a portal out appears.");
            }
            else GameEvents.Good(g.role == Role.Mini ? "The mini-boss is down - its hoard is yours." : "The deep alpha falls! Its chest is unsealed.");
            SynthAudio.Play(Sfx.LevelUp, 0.9f);
        }

        /// <summary>The way out behind a beaten main guardian: a step from its chest, toward the middle of the chamber.</summary>
        void OpenGuardianPortal(Run r, Guardian g)
        {
            var p = g.chest != null ? g.chest.pos + Vector3.ProjectOnPlane(g.home - g.chest.pos, Vector3.up).normalized * 3f : g.home;
            p = Nudge(r, p);
            r.bossPortal = p;
            BuildPortal(p, r.e.glow);
        }

        void TickWatches(Run r, PlayerController pl)
        {
            if (!pl.IsAlive) return;
            var sleepers = new List<CreatureActor>();
            foreach (var w in r.watches)
            {
                if (w.woke && w.rearmAt > 0f && Time.time > w.rearmAt) { w.woke = false; w.rearmAt = 0f; }
                if (w.woke || (w.pos - pl.Position).sqrMagnitude > w.radius * w.radius) continue;
                w.woke = true;
                sleepers.Clear();
                foreach (var a in w.who)
                    if (a != null && a.gameObject.activeSelf && a.IsAlive && a.Mode == CreatureMode.Wild && !a.Aggro) sleepers.Add(a);
                var pp = pl.Position;
                sleepers.Sort((x, y) => (x.Position - pp).sqrMagnitude.CompareTo((y.Position - pp).sqrMagnitude));
                int n = Mathf.Min(w.max, sleepers.Count);
                for (int k = 0; k < n; k++) sleepers[k].Provoke(pl);
                if (sleepers.Count > n) w.rearmAt = Time.time + w.gap;   // the rest come later, if you stay
                if (n > 0 && !w.warned) { w.warned = true; if (!string.IsNullOrEmpty(w.warn)) GameEvents.Warn(w.warn); }
            }
        }
    }
}
