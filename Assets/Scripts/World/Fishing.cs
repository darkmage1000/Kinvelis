using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Fishing. Spots (rings on the water, close to a bank) are hashed per 56 m cell from the world seed. With a Fishing
    /// Rod in the bag: stand near one and tap FISH, wait for the bobber to dip, then tap REEL! in time. You keep
    /// fishing until you tap again while waiting, walk off, or the spot runs dry (it rests five minutes).
    /// The catch depends on the land; Goldscale are rare and bite more at night.
    /// Kin job: Fishing (WorkType.Fishing). Tide kin and kin that live by water have it (FishJobs, derived from the
    /// species so no asset needs editing). A Fishing companion herds fish to your line (shorter waits, a longer bite,
    /// sometimes a second fish); at camp, Fishing kin work the Fishing Pond.
    /// </summary>
    public class Fishing : MonoBehaviour
    {
        public static Fishing I { get; private set; }
        public const string Rod = "fishing_rod";

        class Spot { public Vector3 pos; public int left; public float restUntil, ring, phase; public GameObject go; public Transform shoal, ringA, ringB; }
        static Mesh shoalMesh, rippleMesh;
        const float RippleLift = 0.22f;

        enum State { Idle, Waiting, Bite }

        const float CellSize = 56f;
        const int Reach = 3;
        readonly Dictionary<long, Spot> spots = new Dictionary<long, Spot>();
        readonly HashSet<long> empty = new HashSet<long>();
        readonly List<long> drop = new List<long>();
        float scanTimer;
        State state;
        Spot cur;
        float timer;
        Vector3 castFrom, bobPos;
        GameObject bobber, line;
        bool toldAssist, toldHow;

        public bool Active => state != State.Idle;

        public void Init()
        {
            I = this;
            state = State.Idle;
        }

        static long Key(int cx, int cz) => ((long)(cx + 4096) << 20) | (long)(cz + 4096);

        /// <summary>A fishing spot near a point (ambient fish like to jump there), or null.</summary>
        public Vector3? SpotNear(Vector3 p, float range)
        {
            foreach (var kv in spots)
            {
                var s = kv.Value;
                float dx = s.pos.x - p.x, dz = s.pos.z - p.z;
                if (dx * dx + dz * dz < range * range && Random.value < 0.5f) return s.pos;
            }
            return null;
        }

        // ================================================================ SPOTS
        void Scan(Vector3 pp)
        {
            int pcx = Mathf.FloorToInt(pp.x / CellSize), pcz = Mathf.FloorToInt(pp.z / CellSize);
            for (int dz = -Reach; dz <= Reach; dz++)
            for (int dx = -Reach; dx <= Reach; dx++)
            {
                int cx = pcx + dx, cz = pcz + dz;
                long k = Key(cx, cz);
                if (spots.ContainsKey(k) || empty.Contains(k)) continue;
                var s = TryMake(cx, cz);
                if (s == null) empty.Add(k); else spots[k] = s;
            }
            if (empty.Count > 4000) empty.Clear();
            drop.Clear();
            foreach (var kv in spots)
            {
                if (kv.Value == cur) continue;
                int cx = Mathf.FloorToInt(kv.Value.pos.x / CellSize), cz = Mathf.FloorToInt(kv.Value.pos.z / CellSize);
                // a spot that is resting is remembered a while longer, so walking off and back does not refill it
                int keep = kv.Value.restUntil > Time.time ? Reach + 4 : Reach + 1;
                if (Mathf.Abs(cx - pcx) > keep || Mathf.Abs(cz - pcz) > keep) drop.Add(kv.Key);
            }
            foreach (var k in drop) { if (spots[k].go != null) Destroy(spots[k].go); spots.Remove(k); }
        }

        /// <summary>Fishable spots near the player (minimap dots).</summary>
        public void Nearby(List<Vector3> into)
        {
            foreach (var kv in spots) if (kv.Value.left > 0 || kv.Value.restUntil <= Time.time) into.Add(kv.Value.pos);
        }

        void BuildMarker(Spot s)
        {
            if (shoalMesh == null)
            {
                // fish shadows, nose to tail in a ring
                var mb = new MeshBuilder();
                Color32 dark = new Color(0.02f, 0.05f, 0.12f), fin = new Color(0.03f, 0.08f, 0.16f);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 90f;
                    var q = Quaternion.Euler(0f, a, 0f);
                    Vector3 c = q * new Vector3(1.4f, RippleLift - 0.03f, 0f);
                    mb.Sphere(Matrix4x4.TRS(c, q, new Vector3(0.42f, 0.03f, 1.25f)), dark, 7, 4);
                    mb.Box(Matrix4x4.TRS(c + q * new Vector3(0f, 0f, -0.8f), q, new Vector3(0.46f, 0.02f, 0.38f)), fin);
                }
                shoalMesh = mb.ToMesh("fish_shoal");
                // a thin bright ring (vertex alpha 0 = self-lit, so it shows at night too)
                mb = new MeshBuilder();
                var glow = new Color(1f, 0.97f, 0.75f, 0f);
                for (int i = 0; i < 24; i++)
                {
                    float a = i * 15f * Mathf.Deg2Rad;
                    mb.Box(Matrix4x4.TRS(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Quaternion.Euler(0f, -i * 15f, 0f), new Vector3(0.13f, 0.04f, 0.275f)), glow);
                }
                rippleMesh = mb.ToMesh("fish_ripple");
            }
            s.go = new GameObject("FishingSpot");
            s.go.transform.SetParent(transform, false);
            s.shoal = Child(s.go.transform, shoalMesh);
            s.ringA = Child(s.go.transform, rippleMesh);
            s.ringB = Child(s.go.transform, rippleMesh);
            s.phase = Mathf.Repeat(s.pos.x * 0.37f + s.pos.z * 0.61f, 1f);
        }

        static Transform Child(Transform parent, Mesh m)
        {
            var go = new GameObject(m.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        static Spot TryMake(int cx, int cz)
        {
            int seed = WorldGen.Seed;
            if (Hash.F(cx, cz, seed + 601) > 0.85f) return null;
            // walk a 6 x 6 grid over the cell (from a hashed starting point) for water with a bank to stand on beside it
            int start = (int)(Hash.F(cx, cz, seed + 602) * 36f) % 36;
            float jx = Hash.F(cx, cz, seed + 603) - 0.5f, jz = Hash.F(cx, cz, seed + 606) - 0.5f;
            for (int n = 0; n < 36; n++)
            {
                int idx = (start + n * 7) % 36;   // 7 and 36 share no factor: every point once, well scattered
                float x = (cx + (idx % 6 + 0.5f + jx * 0.8f) / 6f) * CellSize;
                float z = (cz + (idx / 6 + 0.5f + jz * 0.8f) / 6f) * CellSize;
                if (!WorldGen.InsideWorld(x, z) || Caves.InZone(x, z)) continue;
                float h = WorldGen.Height(x, z);
                if (h > -0.7f || h < -9f) continue;
                bool bank = false;
                for (int k = 0; k < 8 && !bank; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    if (WorldGen.Height(x + Mathf.Cos(a) * 8f, z + Mathf.Sin(a) * 8f) > 0.35f) bank = true;
                }
                if (!bank) continue;
                return new Spot { pos = new Vector3(x, 0f, z), left = 4 + (int)(Hash.F(cx, cz, seed + 604) * 3f), ring = Hash.F(cx, cz, seed + 605) * 2f };
            }
            return null;
        }

        // ================================================================ TICK
        public void Tick(float dt, PlayerController pl)
        {
            if (Caves.InCave)
            {
                if (state != State.Idle) Stop(null);
                foreach (var kv in spots) if (kv.Value.go != null && kv.Value.go.activeSelf) kv.Value.go.SetActive(false);
                return;
            }
            Vector3 pp = pl.Position;
            scanTimer -= dt;
            if (scanTimer <= 0f) { scanTimer = 1.5f; Scan(pp); }

            // rings on the water mark each spot
            foreach (var kv in spots)
            {
                var s = kv.Value;
                float dx = s.pos.x - pp.x, dz = s.pos.z - pp.z;
                if (dx * dx + dz * dz > 110f * 110f) { if (s.go != null && s.go.activeSelf) s.go.SetActive(false); continue; }
                bool resting = s.left <= 0 && s.restUntil > Time.time;
                if (s.left <= 0 && !resting) s.left = 4 + Random.Range(0, 3);
                // a spot you can see from the bank: fish shadows circling under two bright ripples, and a gold glint.
                // A spot that has been fished out shows nothing until it has rested.
                if (resting) { if (s.go != null && s.go.activeSelf) s.go.SetActive(false); continue; }
                if (s.go == null) BuildMarker(s);
                if (!s.go.activeSelf) s.go.SetActive(true);
                float sy = Tides.Surface(s.pos);
                s.go.transform.position = new Vector3(s.pos.x, sy, s.pos.z);
                s.shoal.localRotation = Quaternion.Euler(0f, -(Time.time * 38f + s.phase * 60f), 0f);   // negative yaw: the fish swim head first
                float ta = Mathf.Repeat(Time.time * 0.55f + s.phase, 1f), tb = Mathf.Repeat(ta + 0.5f, 1f);
                s.ringA.localScale = new Vector3(0.5f + ta * 2.6f, 1f, 0.5f + ta * 2.6f);
                s.ringB.localScale = new Vector3(0.5f + tb * 2.6f, 1f, 0.5f + tb * 2.6f);
                // ripples ride just above the wave crests (the water shader lifts the surface by up to 0.18 m), and die away at their widest
                s.ringA.localPosition = new Vector3(0f, ta > 0.85f ? -0.6f : RippleLift, 0f);
                s.ringB.localPosition = new Vector3(0f, tb > 0.85f ? -0.6f : RippleLift, 0f);
                s.ring -= dt;
                if (s.ring > 0f) continue;
                s.ring = Random.Range(0.5f, 1.1f);
                var p = new Vector3(s.pos.x, sy, s.pos.z);
                Vector2 r = Random.insideUnitCircle * 1.3f;
                Fx.Sparkle(p + new Vector3(r.x, 0.35f, r.y), new Color(1f, 0.92f, 0.5f), 2, 0.5f);
                if (Random.value < 0.35f) Fx.Mote(p + new Vector3(r.x, 0.05f, r.y), new Vector3(0f, 1.8f, 0f), new Color(0.9f, 0.97f, 1f), 0.1f, 0.4f, false);
            }

            if (state == State.Idle) return;
            // walking off, swimming, mounting or being knocked out ends it
            Vector3 d = pp - castFrom; d.y = 0f;
            if (d.sqrMagnitude > 2.6f * 2.6f || !pl.IsAlive || pl.IsSwimming || pl.IsMounted) { Stop(pl.IsAlive ? "You reel in." : null); return; }

            float surf = Tides.Surface(bobPos);
            timer -= dt;
            if (state == State.Waiting)
            {
                PlaceBobber(pl, surf + 0.1f + Mathf.Sin(Time.time * 2.2f) * 0.04f);
                if (timer <= 0f)
                {
                    state = State.Bite;
                    timer = 1.3f + 0.2f * AssistLevel();
                    Fx.Burst(new Vector3(bobPos.x, surf + 0.1f, bobPos.z), new Color(0.85f, 0.95f, 1f), 12, 3.5f, 0.14f, 0.5f, false, 9f);
                    SynthAudio.Play(Sfx.Pluck, 1.5f);
                    HUD.DamageNumber(new Vector3(bobPos.x, surf + 1.2f, bobPos.z), "!", new Color(1f, 0.9f, 0.3f), 1.5f);
                }
            }
            else
            {
                PlaceBobber(pl, surf - 0.22f + Mathf.Sin(Time.time * 26f) * 0.05f);
                if (timer <= 0f)
                {
                    GameEvents.Say("It slipped the hook.", new Color(0.8f, 0.9f, 1f));
                    Wait();
                }
            }
        }

        int AssistLevel()
        {
            var comp = GameManager.I.Party.Companion;
            return comp != null && comp.gameObject.activeSelf && comp.Data != null ? comp.Data.WorkLevel(WorkType.Fishing) : 0;
        }

        void Wait()
        {
            state = State.Waiting;
            timer = Random.Range(2.5f, 6.5f) * (1f - 0.15f * Mathf.Min(4, AssistLevel()));
        }

        // ================================================================ PROMPT
        /// <summary>FISH / WAIT... / REEL! (null when there is nothing to fish here).</summary>
        public string Prompt(PlayerController pl, out System.Action act)
        {
            act = null;
            if (state == State.Bite) { act = Reel; return "REEL!"; }
            if (state == State.Waiting) { act = () => Stop("You reel in."); return "WAIT..."; }
            if (pl.IsSwimming || pl.IsMounted || !pl.IsAlive) return null;
            Vector3 pp = pl.Position;
            Spot best = null; float bd = 9.5f * 9.5f;
            foreach (var kv in spots)
            {
                float dx = kv.Value.pos.x - pp.x, dz = kv.Value.pos.z - pp.z, d2 = dx * dx + dz * dz;
                if (d2 < bd) { bd = d2; best = kv.Value; }
            }
            if (best == null) return null;
            var spot = best;
            act = () => Cast(pl, spot);
            return "FISH";
        }

        void Cast(PlayerController pl, Spot s)
        {
            if (state != State.Idle) return;
            if (pl.Stats.Bag.Count(Rod) <= 0) { GameEvents.Warn("You need a Fishing Rod. Craft one by hand: 4 wood, 6 fiber."); return; }
            if (s.left <= 0 && s.restUntil > Time.time) { GameEvents.Say("The fish here have gone quiet. Try another spot, or come back in a few minutes.", new Color(0.8f, 0.9f, 1f)); return; }
            pl.CancelAim();
            cur = s;
            castFrom = pl.Position;
            // the float lands toward the spot, no further than a cast
            Vector3 to = s.pos - castFrom; to.y = 0f;
            float dist = Mathf.Min(to.magnitude, 7.5f);
            bobPos = castFrom + (to.sqrMagnitude > 0.01f ? to.normalized : pl.Forward) * dist;
            if (WorldGen.Height(bobPos.x, bobPos.z) > -0.3f) bobPos = s.pos;
            EnsureBobber();
            bobber.SetActive(true); line.SetActive(true);
            SynthAudio.Play(Sfx.Throw, 1.2f);
            if (!toldHow) { toldHow = true; GameEvents.Say("Watch the float. When it dips, tap REEL! Tap while waiting to stop.", new Color(0.8f, 0.9f, 1f)); }
            int lv = AssistLevel();
            if (lv > 0 && !toldAssist)
            {
                toldAssist = true;
                GameEvents.Say($"{GameManager.I.Party.Companion.Data.DisplayName} slips into the shallows and herds the fish toward your line.", new Color(0.6f, 0.9f, 1f));
            }
            Wait();
        }

        void Reel()
        {
            if (state != State.Bite || cur == null) return;
            var gm = GameManager.I;
            var pl = gm.Player;
            Vector3 at = pl.Position + Vector3.up * 1.2f;
            var env = EnvironmentSystem.I;
            var biome = WorldGen.Biome(cur.pos.x, cur.pos.z);
            int lv = AssistLevel();
            bool night = env != null && env.IsNight;
            float gold = (night ? 0.13f : 0.08f) + 0.02f * lv;
            float r = Random.value;
            string item; int n = 1;
            if (r < 0.06f)
            {
                // something that is not a fish
                int g = Random.Range(12, 40);
                pl.Stats.glimmer += g;
                item = biome == BiomeId.Isles && Random.value < 0.4f ? "sea_pearl" : Random.value < 0.5f ? "tether_seed" : "fiber";
                n = item == "sea_pearl" ? 1 : Random.Range(2, 5);
                GameEvents.Good($"You haul up an old pouch: +{g} Glimmer.");
            }
            else if (r < 0.06f + gold) item = "goldscale";
            else if (r < 0.06f + gold + 0.42f) item = LandFish(biome);
            else item = "silverfin";
            if (item != "goldscale" && Random.value < 0.2f * lv) n++;
            gm.GiveItem(item, n, at);
            Quests.Note("fish");
            gm.AddPlayerXp(item == "goldscale" ? 14 : 6);
            var comp = gm.Party.Companion;
            if (lv > 0 && comp != null) gm.GrantKinXp(comp.Data, 4, true);
            float surf = Tides.Surface(bobPos);
            Fx.Burst(new Vector3(bobPos.x, surf + 0.2f, bobPos.z), new Color(0.85f, 0.95f, 1f), 18, 5f, 0.16f, 0.6f, false, 9f);
            SynthAudio.Play(item == "goldscale" ? Sfx.CaptureSuccess : Sfx.Pickup, 1.2f);
            if (item == "goldscale") GameEvents.Good("A Goldscale! They are rare.");
            GameEvents.RaiseInventoryChanged();
            cur.left--;
            if (cur.left <= 0)
            {
                cur.restUntil = Time.time + 300f;
                Stop("That is the last of them here for now. The spot will rest a few minutes.");
                return;
            }
            Wait();
        }

        /// <summary>The fish that belongs to a land's waters.</summary>
        public static string LandFish(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Isles: case BiomeId.Desert: return "reef_snapper";
                case BiomeId.Swamp: case BiomeId.Blight: case BiomeId.Glow: return "mirefish";
                default: return "sunfin";
            }
        }

        void Stop(string say)
        {
            state = State.Idle;
            cur = null;
            if (bobber != null) { bobber.SetActive(false); line.SetActive(false); }
            if (say != null) GameEvents.Say(say, new Color(0.8f, 0.9f, 1f));
        }

        // ================================================================ BOBBER
        void EnsureBobber()
        {
            if (bobber != null) return;
            var mb = new MeshBuilder();
            mb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.06f, 0), Quaternion.identity, new Vector3(0.4f, 0.4f, 0.4f)), new Color(0.95f, 0.2f, 0.2f), new Color(1f, 1f, 1f), 8, 6);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.26f, 0), Quaternion.identity, new Vector3(0.04f, 0.22f, 0.04f)), new Color(0.2f, 0.2f, 0.22f), 4);
            bobber = Make("Bobber", mb.ToMesh("bobber"));
            mb = new MeshBuilder();
            mb.Box(Matrix4x4.TRS(new Vector3(0, 0, 0.5f), Quaternion.identity, new Vector3(0.025f, 0.025f, 1f)), new Color(0.92f, 0.92f, 0.88f));
            line = Make("Line", mb.ToMesh("fishline"));
        }

        GameObject Make(string name, Mesh m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return go;
        }

        void PlaceBobber(PlayerController pl, float y)
        {
            var p = new Vector3(bobPos.x, y, bobPos.z);
            bobber.transform.position = p;
            Vector3 hand = pl.Position + Vector3.up * 1.7f + pl.Forward * 0.5f;
            Vector3 d = p + Vector3.up * 0.3f - hand;
            line.transform.SetPositionAndRotation(hand, Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : Vector3.forward));
            line.transform.localScale = new Vector3(1f, 1f, d.magnitude);
        }
    }

    /// <summary>
    /// Which kin can fish, and how well: Tide kin (2 if it is their first element, 1 if their second), +1 for kin that
    /// live by the water, +1 for Rare and better; any other kin that lives by the water gets 1. Added to each species'
    /// job list when content loads, so every screen that lists jobs shows it and no asset has to change.
    /// </summary>
    public static class FishJobs
    {
        public static int Level(SpeciesDef sp)
        {
            if (sp == null || sp.bossOnly) return 0;
            int lv = sp.element1 == Element.Tide ? 2 : sp.element2 == Element.Tide ? 1 : 0;
            if (lv == 0) return sp.nearWater ? 1 : 0;
            if (sp.nearWater) lv++;
            if (sp.rarity >= Rarity.Rare) lv++;
            return Mathf.Min(4, lv);
        }

        public static void Apply()
        {
            foreach (var sp in ContentDB.SpeciesList)
            {
                int lv = Level(sp);
                if (lv <= 0 || sp.WorkLevel(WorkType.Fishing) > 0) continue;
                var list = new List<WorkSkill>(sp.work ?? new WorkSkill[0]) { new WorkSkill(WorkType.Fishing, lv) };
                sp.work = list.ToArray();
            }
        }
    }
}
