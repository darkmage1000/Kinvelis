using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Fast-travel waystones: discovered by coming near (shown on maps), activated by attuning (Interact),
    /// then usable as travel destinations from the Map tab or any attuned waystone. Camp Hearthstone counts too.
    /// </summary>
    public class Waystones : MonoBehaviour
    {
        public static Waystones I { get; private set; }

        [SerializeField] float discoverRange = 70f;
        [SerializeField] float attuneRange = 4.5f;
        [SerializeField] float combatBlockRange = 20f;

        public class Stone
        {
            public string id, name;
            public BiomeId biome;
            public Vector3 pos;
            public GameObject go;
            public Light light;
            public Transform crystal;
            public bool builtActive;
        }

        public readonly List<Stone> All = new List<Stone>();
        public bool Traveling { get; private set; }
        public const string CampId = "camp";

        static Mesh baseMesh, crystalMesh;
        float discoverTimer;
        Image fade;

        PlayerStats Stats => GameManager.I.Player.Stats;
        public PlayerStats StatsRef() => Stats;
        public bool IsFound(Stone s) => Stats.waystonesFound.Contains(s.id);
        public bool IsActive(Stone s) => Stats.waystonesActive.Contains(s.id);

        public void Init(Transform uiRoot)
        {
            I = this;
            Vector3 spawn = WorldGen.FindSpawnPoint();
            Vector3 bloom = Landmarks.List.Count > 0 ? Landmarks.List[0].pos : new Vector3(260, 0, -930);
            Vector3 arch = Landmarks.List.Count > 2 ? Landmarks.List[2].pos : new Vector3(1230, 0, 560);
            Vector3 spire = Landmarks.List.Count > 3 ? Landmarks.List[3].pos : new Vector3(-1100, 0, 700);

            // on the landing shelf below the start plateau: the first thing you reach when you enter the world
            var side = new Vector3(WorldGen.StartFwd.y, 0f, -WorldGen.StartFwd.x);
            AddExact("dawnfield", "Dawnfield Waystone", WorldGen.StartExit + side * 7f);
            Add("bloomtree", "Bloomtree Waystone", bloom + new Vector3(34f, 0, -28f));
            Add("lakeshore", "Lakeshore Waystone", new Vector3(-460f, 0, -900f));
            Add("southcliffs", "Southern Cliffs Waystone", new Vector3(330f, 0, -1380f));
            Add("westmeadow", "Westmeadow Waystone", new Vector3(-720f, 0, -260f));
            Add("crossroads", "Crossroads Waystone", new Vector3(80f, 0, 140f));
            Add("arch", "Sunforge Waystone", arch + new Vector3(-40f, 0, -50f));
            Add("canyonrim", "Canyon Rim Waystone", new Vector3(880f, 0, 140f));
            Add("emberflats", "Ember Flats Waystone", new Vector3(1480f, 0, 900f));
            Add("dustwind", "Dustwind Mesa Waystone", new Vector3(1120f, 0, -220f));
            Add("spire", "Shard Spire Waystone", spire + new Vector3(38f, 0, -38f));
            Add("frostpass", "Frostpass Waystone", new Vector3(-600f, 0, 480f));
            Add("summit", "Summit Watch Waystone", new Vector3(-1300f, 0, 1180f));
            Add("glacier", "Glacier Hollow Waystone", new Vector3(-880f, 0, 1080f));
            // one where you make landfall on each outer land
            string[] outerNames = { "Driftkey Waystone", "Mirefen Waystone", "Sunscour Waystone", "Blightmoor Waystone", "Glowdeep Waystone" };
            for (int i = 0; i < WorldGen.Lands.Length && i < outerNames.Length; i++)
                Add("outer_" + WorldGen.Lands[i].biome.ToString().ToLowerInvariant(), outerNames[i], WorldGen.LandGate(WorldGen.Lands[i]));

            // screen fade used during travel
            fade = UIFactory.Panel(uiRoot, "TravelFade", new Color(0.02f, 0.03f, 0.05f, 0f));
            fade.sprite = null;
            fade.rectTransform.Stretch(-50, -50, -50, -50);
            fade.raycastTarget = false;
            fade.gameObject.SetActive(false);
        }

        void AddExact(string id, string name, Vector3 p)
        {
            p.y = WorldGen.Height(p.x, p.z);
            var s = new Stone { id = id, name = name, pos = p, biome = WorldGen.Biome(p.x, p.z) };
            BuildVisual(s);
            WorldCollision.AddObstacle(p, 1.1f, s);
            All.Add(s);
        }

        void Add(string id, string name, Vector3 p)
        {
            p = Landmarks.FindLand(p, 0.88f);
            var s = new Stone { id = id, name = name, pos = p, biome = WorldGen.Biome(p.x, p.z) };
            BuildVisual(s);
            WorldCollision.AddObstacle(p, 1.1f, s);
            All.Add(s);
        }

        // ============================================================ VISUAL
        static void EnsureMeshes()
        {
            if (baseMesh != null) return;
            var mb = new MeshBuilder();
            Color32 stone = new Color(0.52f, 0.54f, 0.6f), stoneTop = new Color(0.66f, 0.68f, 0.74f), rune = new Color(0.35f, 0.9f, 1f);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.2f, 0), Quaternion.identity, new Vector3(3.2f, 0.4f, 3.2f)), stone, stoneTop, 8, 0.9f, true);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(2.2f, 0.25f, 2.2f)), stone, stoneTop, 8, 0.9f, true);
            // obelisk
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 2.1f, 0), Quaternion.Euler(0, 22.5f, 0), new Vector3(0.9f, 3.2f, 0.9f)), stone, stoneTop, 4, 0.55f, true);
            mb.Cone(Matrix4x4.TRS(new Vector3(0, 4.05f, 0), Quaternion.Euler(0, 22.5f, 0), new Vector3(0.5f, 0.7f, 0.5f)), stoneTop, 4);
            // rune strips
            for (int i = 0; i < 4; i++)
            {
                var r = Quaternion.Euler(0, i * 90f + 22.5f, 0);
                mb.Box(Matrix4x4.TRS(r * new Vector3(0, 2.1f, 0.33f), r, new Vector3(0.1f, 1.6f, 0.04f)), rune);
            }
            // side stones
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                mb.Rock(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 1.8f, 0.3f, Mathf.Sin(a) * 1.8f), Quaternion.identity, new Vector3(0.6f, 0.5f, 0.6f)), stone, stoneTop, (uint)(i + 3), 6, 4);
            }
            baseMesh = mb.ToMesh("waystone");
            mb.Clear();
            mb.Crystal(Matrix4x4.TRS(new Vector3(0, -0.45f, 0), Quaternion.identity, new Vector3(0.45f, 0.9f, 0.45f)), new Color(0.4f, 0.9f, 1f), Color.white);
            crystalMesh = mb.ToMesh("waystone_crystal");
        }

        void BuildVisual(Stone s)
        {
            EnsureMeshes();
            var go = new GameObject("Waystone_" + s.id);
            go.transform.SetParent(transform, false);
            go.transform.position = s.pos - Vector3.up * 0.1f;
            go.AddComponent<MeshFilter>().sharedMesh = baseMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;

            var cr = new GameObject("Crystal");
            cr.transform.SetParent(go.transform, false);
            cr.transform.localPosition = new Vector3(0, 5.2f, 0);
            cr.AddComponent<MeshFilter>().sharedMesh = crystalMesh;
            var cmr = cr.AddComponent<MeshRenderer>();
            cmr.sharedMaterial = RenderAssets.Creature;
            cmr.shadowCastingMode = ShadowCastingMode.Off;
            s.crystal = cr.transform;

            var lg = new GameObject("Glow");
            lg.transform.SetParent(go.transform, false);
            lg.transform.localPosition = new Vector3(0, 4.8f, 0);
            s.light = lg.AddComponent<Light>();
            s.light.type = LightType.Point;
            s.light.range = 14f;
            s.light.color = new Color(0.45f, 0.9f, 1f);
            s.light.shadows = LightShadows.None;
            s.go = go;
        }

        // ============================================================ TICK
        public void Tick(Vector3 playerPos, float dt)
        {
            float cullR = QualityProfile.Current.fogEnd * 1.6f;
            discoverTimer -= dt;
            bool checkDiscover = discoverTimer <= 0f;
            if (checkDiscover) discoverTimer = 0.5f;
            foreach (var s in All)
            {
                float d = Vector2.Distance(new Vector2(s.pos.x, s.pos.z), new Vector2(playerPos.x, playerPos.z));
                bool vis = d < cullR;
                if (s.go.activeSelf != vis) s.go.SetActive(vis);
                if (!vis) continue;
                bool active = IsActive(s);
                // crystal spins + bobs when attuned, rests dim otherwise
                s.crystal.localPosition = new Vector3(0, active ? 5.2f + Mathf.Sin(Time.time * 1.6f) * 0.15f : 4.55f, 0);
                s.crystal.Rotate(0, dt * (active ? 60f : 8f), 0);
                s.light.enabled = active && d < 120f;
                s.light.intensity = 1.8f + Mathf.Sin(Time.time * 2f) * 0.3f;
                if (checkDiscover && !IsFound(s) && d < discoverRange)
                {
                    Stats.waystonesFound.Add(s.id);
                    GameEvents.Say($"Discovered {s.name}! Attune to it to unlock fast travel.", new Color(0.5f, 0.95f, 1f));
                    SynthAudio.Play(Sfx.UiOpen, 0.8f);
                }
            }
        }

        /// <summary>Nearest waystone within attune range (for the Interact button).</summary>
        public Stone NearestInRange(Vector3 p)
        {
            Stone best = null; float bd = attuneRange;
            foreach (var s in All)
            {
                float d = Vector2.Distance(new Vector2(s.pos.x, s.pos.z), new Vector2(p.x, p.z));
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        public void Attune(Stone s)
        {
            if (!IsFound(s)) Stats.waystonesFound.Add(s.id);
            if (IsActive(s)) { MenuUI.I.Open(MenuTab.Map); return; }
            Stats.waystonesActive.Add(s.id);
            Fx.Sparkle(s.pos + Vector3.up * 4f, new Color(0.5f, 0.95f, 1f), 40, 1.5f);
            Fx.Ring(s.pos + Vector3.up * 0.4f, new Color(0.5f, 0.95f, 1f), 4f, 32);
            SynthAudio.Play(Sfx.CaptureSuccess, 0.8f);
            GameManager.I.AddPlayerXp(25);
            GameEvents.Good($"{s.name} attuned! Fast travel from the Map ({Stats.waystonesActive.Count}/{All.Count}).");
            GameEvents.RaiseBaseChanged();
        }

        // ============================================================ TRAVEL
        public bool CanTravel(out string reason)
        {
            reason = null;
            var pl = GameManager.I.Player;
            if (!WorldRules.Current.fastTravel) { reason = "Fast travel is disabled in this world's rules."; return false; }
            if (Traveling) { reason = "Already traveling."; return false; }
            if (Caves.InCave) { reason = "Can't fast travel underground - leave the cave first."; return false; }
            if (!pl.IsAlive) { reason = ""; return false; }
            var hunter = CreatureRegistry.FindHunter(pl.Position, combatBlockRange, pl, GameManager.I.Party.Companion);
            if (hunter != null) { reason = "Can't fast travel while wild kin are hunting you!"; return false; }
            return true;
        }

        public void TravelTo(string id)
        {
            if (!CanTravel(out string reason)) { if (!string.IsNullOrEmpty(reason)) GameEvents.Warn(reason); return; }
            Vector3 dest;
            string name;
            if (id == CampId)
            {
                if (!BaseManager.I.HasBase) return;
                dest = BaseManager.I.CorePosition + new Vector3(3.5f, 0, -3.5f);
                name = "your camp";
            }
            else if (Outposts.ByTravelId(id) != null)
            {
                var o = Outposts.ByTravelId(id);
                if (!BaseManager.I.HasBuilding("waypad")) { GameEvents.Warn("Build a Waypad in camp to travel to outposts."); return; }
                dest = o.Pos + new Vector3(0, 0, 4f);
                name = Outposts.LandName(o) + " outpost";
            }
            else
            {
                var s = All.Find(x => x.id == id);
                if (s == null || !IsActive(s)) { GameEvents.Warn("That waystone isn't attuned yet."); return; }
                dest = s.pos + new Vector3(0, 0, -3.5f);
                name = s.name;
            }
            StartCoroutine(TravelRoutine(dest, name));
        }

        IEnumerator TravelRoutine(Vector3 dest, string name)
        {
            Traveling = true;
            var gm = GameManager.I;
            var pl = gm.Player;
            fade.gameObject.SetActive(true);
            fade.transform.SetAsLastSibling();
            SynthAudio.Play(Sfx.Summon, 0.7f);
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime) { fade.color = new Color(0.02f, 0.03f, 0.05f, t / 0.35f); yield return null; }
            fade.color = new Color(0.02f, 0.03f, 0.05f, 1f);

            bool wasMounted = pl.IsMounted;
            var comp = gm.Party.Companion;
            if (wasMounted) pl.Dismount(true);
            CreatureSpawner.I.DespawnAllWild();
            ProjectileSystem.I.ClearAll();
            pl.Teleport(dest, pl.transform.eulerAngles.y);
            gm.CameraRig.SnapTo(dest);
            if (comp != null && comp.gameObject.activeSelf)
            {
                var cp = dest + pl.Right * 1.8f;
                cp.y = WorldGen.Height(cp.x, cp.z);
                comp.transform.position = cp;
                comp.Home = cp;
            }
            // wait for terrain around the destination (max 4 s)
            for (float t = 0; t < 4f && !ChunkManager.I.IsChunkReady(dest); t += Time.unscaledDeltaTime) yield return null;
            yield return null;
            if (wasMounted && comp != null && comp.IsAlive) pl.ToggleRide();

            for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime) { fade.color = new Color(0.02f, 0.03f, 0.05f, 1f - t / 0.45f); yield return null; }
            fade.gameObject.SetActive(false);
            Fx.Sparkle(dest + Vector3.up, new Color(0.5f, 0.95f, 1f), 24, 1f);
            GameEvents.Say($"Arrived at {name}.", new Color(0.6f, 0.95f, 1f));
            Traveling = false;
        }
    }
}
