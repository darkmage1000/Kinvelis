using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Rival tamers: two stationed a short walk from each settlement, each with a team of three kin well above the
    /// local wild levels. Challenge one and their kin come out one at a time and go for your companion; beat all three
    /// for Glimmer, items and standing with the settlement. Their kin can't be caught and drop nothing.
    /// </summary>
    public class Tamers : MonoBehaviour
    {
        public static Tamers I { get; private set; }
        public const int RematchSeconds = 20 * 60;
        const float ArenaRadius = 45f;

        public class Tamer
        {
            public string id, name, title;
            public Vector3 face = Vector3.forward;   // toward the road they stand beside
            public Settlements.Settlement home;
            public int tier;                 // 0 = Tamer, 1 = Ace
            public Vector3 pos;
            public string[] team;            // species ids
            public int[] levels;
            public int glimmer;
            public string rewardItem;
            public int rewardCount;
            public GameObject root;
            public Transform body;
            public CreatureRig rig, kinRig;
        }

        public static readonly List<Tamer> All = new List<Tamer>();

        // duel state
        public Tamer Active { get; private set; }
        int index;
        CreatureActor fighter;
        float nextTimer, retargetTimer;

        static readonly Palette[] Looks =
        {
            Look("#2c2c3a", "#1e1e28", "#d83a3a", "#e8b890"),
            Look("#5a2a2a", "#2a2020", "#f0c040", "#c89868"),
        };

        static Palette Look(string coat, string legs, string hat, string skin)
        {
            ColorUtility.TryParseHtmlString(coat, out var a); ColorUtility.TryParseHtmlString(legs, out var b);
            ColorUtility.TryParseHtmlString(hat, out var c); ColorUtility.TryParseHtmlString(skin, out var d);
            return new Palette { primary = a, secondary = b, accent = c, glow = d, eye = new Color(0.1f, 0.08f, 0.1f) };
        }

        // ============================================================ SETUP
        public void Init()
        {
            I = this;
            All.Clear();
            string[][] names = { new[] { "Rook", "Vessa" }, new[] { "Karn", "Idris" }, new[] { "Sylke", "Orwin" } };
            float[] bearing = { 200f, 320f };
            float[] distance = { 95f, 150f };
            float[] roadDistance = { 50f, 105f };   // along the road from the hub outside the gate
            foreach (var s in Settlements.All)
            {
                var lv = BiomeInfo.LevelRange(s.biome, s.pos);
                for (int t = 0; t < 2; t++)
                {
                    // beside a road, a short walk out of the gate (user request 2026-10-09): the Tamer on the road toward the
                    // start / Hearthmere, the Ace a little further out on the next road. No road: the old spot in the hills.
                    Vector3 p = default, face = Vector3.forward;
                    bool placed = false;
                    int toward = t == 0 ? (s.index == 0 ? -1 : 0) : (s.index == 1 ? 2 : 1);
                    var road = Trails.From(s.index, toward);
                    if (road != null && road.length > roadDistance[t] + 20f && Trails.Along(road, s.index, roadDistance[t], out var rp, out var rdir))
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, rdir);
                        for (int k = 0; k < 2 && !placed; k++)
                        {
                            var c = rp + side * ((t == 0) == (k == 0) ? 4.6f : -4.6f);
                            c.y = WorldGen.Height(c.x, c.z);
                            if (c.y < 1.2f || WorldGen.Normal(c.x, c.z, 2f).y < 0.8f || Settlements.Inside(c, 6f)) continue;
                            p = c; face = rp - c; face.y = 0f; placed = true;
                        }
                    }
                    if (!placed)
                    {
                        float a = (bearing[t] + s.index * 47f) * Mathf.Deg2Rad;
                        var want = s.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * distance[t];
                        p = Landmarks.FindLand(want, 0.88f);
                        if (Settlements.Inside(p, 30f)) p = Landmarks.FindLand(want + (want - s.pos).normalized * 40f, 0.88f);
                    }
                    var tm = new Tamer
                    {
                        id = s.id + "_" + t, name = names[s.index % names.Length][t], title = t == 0 ? "Tamer" : "Ace Tamer", home = s, tier = t, pos = p, face = face,
                        glimmer = (t == 0 ? 150 : 300) + s.index * (t == 0 ? 250 : 400),
                        rewardItem = s.index == 0 ? (t == 0 ? "ironbark_seed" : "kin_treat") : s.index == 1 ? (t == 0 ? "kin_treat" : "cinder_seed") : (t == 0 ? "rime_seed" : "crystal"),
                        rewardCount = t == 0 ? 3 : (s.index == 2 ? 6 : 2)
                    };
                    PickTeam(tm, lv);
                    Build(tm);
                    All.Add(tm);
                }
            }
        }

        static void PickTeam(Tamer t, Vector2Int lv)
        {
            var rng = new System.Random(WorldGen.Seed * 53 + t.home.index * 811 + t.tier * 97);
            var common = new List<SpeciesDef>(); var rare = new List<SpeciesDef>();
            foreach (var sp in ContentDB.SpeciesList)
            {
                if (sp.starter || !sp.LivesIn(t.home.biome) || sp.temperament == Temperament.Timid) continue;
                if (sp.rarity <= Rarity.Uncommon) common.Add(sp);
                else if (sp.rarity == Rarity.Rare) rare.Add(sp);
            }
            if (common.Count == 0) foreach (var sp in ContentDB.SpeciesList) if (!sp.starter && sp.rarity <= Rarity.Uncommon) common.Add(sp);
            t.team = new string[3];
            t.levels = new int[3];
            // a clear step above the locals: the first tamer by a few levels, the ace by more, and the last kin is the strongest
            int baseLv = lv.y + (t.tier == 0 ? 2 : 6);
            for (int i = 0; i < 3; i++)
            {
                var pool = i == 2 && t.tier == 1 && rare.Count > 0 ? rare : common;
                SpeciesDef pick = null;
                for (int tries = 0; tries < 8; tries++)
                {
                    pick = pool[rng.Next(pool.Count)];
                    if (System.Array.IndexOf(t.team, pick.id) < 0) break;
                }
                t.team[i] = pick.id;
                t.levels[i] = Mathf.Clamp(baseLv + i + (i == 2 ? 1 : 0), 2, 60);
            }
        }

        void Build(Tamer t)
        {
            t.root = new GameObject("Tamer_" + t.name);
            t.root.transform.SetParent(transform, false);
            Landmarks.List.Add(new Landmarks.Landmark { name = $"{t.title} {t.name}", pos = t.pos, color = new Color(1f, 0.4f, 0.3f), biome = t.home.biome, go = t.root, icon = MapIcon.Tamer });

            var body = new GameObject("Body");
            body.transform.SetParent(t.root.transform, false);
            body.transform.position = t.pos;
            if (t.face.sqrMagnitude > 0.01f) body.transform.rotation = Quaternion.LookRotation(t.face);
            t.body = body.transform;
            t.rig = ModelBuilder.BuildCustom("tamer" + t.tier, PlayerLook.Parts, PlayerLook.Pivots, Looks[t.tier], body.transform, RenderAssets.Creature, Locomotion.Biped, 1.06f);

            // challenge banner: two crossed poles and a red flag
            var mb = new MeshBuilder();
            Color32 wood = new Color(0.35f, 0.25f, 0.18f), red = new Color(0.85f, 0.2f, 0.18f), gold = new Color(0.95f, 0.75f, 0.3f);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 1.9f, 0), Quaternion.Euler(0, 0, 14), new Vector3(0.12f, 3.8f, 0.12f)), wood, 6, 1f, true);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 1.9f, 0), Quaternion.Euler(0, 0, -14), new Vector3(0.12f, 3.8f, 0.12f)), wood, 6, 1f, true);
            mb.Box(Matrix4x4.TRS(new Vector3(0.75f, 3.1f, 0), Quaternion.identity, new Vector3(1.1f, 0.8f, 0.05f)), red, red);
            mb.Sphere(Matrix4x4.TRS(new Vector3(0.75f, 3.1f, 0.04f), Quaternion.identity, new Vector3(0.36f, 0.36f, 0.06f)), gold, 8, 6);
            if (t.tier == 1) mb.Box(Matrix4x4.TRS(new Vector3(-0.75f, 3.1f, 0), Quaternion.identity, new Vector3(1.1f, 0.8f, 0.05f)), gold, gold);
            var banner = new GameObject("Banner");
            banner.transform.SetParent(t.root.transform, false);
            var bp = t.pos + new Vector3(-1.8f, 0, -0.6f); bp.y = WorldGen.Height(bp.x, bp.z);
            banner.transform.position = bp;
            banner.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("tamer_banner" + t.tier);
            var mr = banner.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
            WorldCollision.AddObstacle(bp, 0.3f, this);

            // their lead kin waits at their side
            var lead = ContentDB.Sp(t.team[2]);
            if (lead != null)
            {
                var d = new CreatureData { uid = "tamerlead", speciesId = lead.id, level = t.levels[2], apt = new[] { 50, 50, 50 }, spec = 0 };
                CreatureFactory.EvolveForLevel(d, 1f);   // the one at their side is the one they will send out
                var kin = new GameObject("LeadKin");
                kin.transform.SetParent(t.root.transform, false);
                var kp = t.pos + new Vector3(1.6f + lead.size * 0.4f, 0, 0.3f); kp.y = WorldGen.Height(kp.x, kp.z);
                kin.transform.position = kp;
                t.kinRig = ModelBuilder.Build(d, kin.transform, RenderAssets.Creature);
            }
        }

        // ============================================================ STATE
        static PlayerStats S => GameManager.I.Player.Stats;

        public static long LastWin(Tamer t)
        {
            foreach (var w in S.tamerWins) if (w.id == t.id) return w.unix;
            return 0;
        }

        public static int Wins(Tamer t)
        {
            foreach (var w in S.tamerWins) if (w.id == t.id) return w.wins;
            return 0;
        }

        /// <summary>Seconds until this tamer will fight again (0 = ready).</summary>
        public static int Cooldown(Tamer t) => (int)Mathf.Max(0, LastWin(t) + RematchSeconds - Expeditions.Now);

        /// <summary>First win pays in full; rematches pay 60%.</summary>
        public static int Purse(Tamer t) => Wins(t) == 0 ? t.glimmer : Mathf.RoundToInt(t.glimmer * 0.6f / 5f) * 5;

        public Tamer InReach(Vector3 p)
        {
            foreach (var t in All)
                if (t.root != null && t.root.activeSelf && (p - t.pos).sqrMagnitude < 3.2f * 3.2f) return t;
            return null;
        }

        public string Challenge(Tamer t)
        {
            if (Active != null) return "You're already in a match.";
            if (Cooldown(t) > 0) return $"{t.name} is resting their kin.";
            Active = t;
            index = 0;
            nextTimer = 1.2f;
            fighter = null;
            GameEvents.Warn($"{t.title} {t.name} accepts! Three kin, one at a time. Leave the ring and you forfeit.");
            SynthAudio.Play(Sfx.Summon, 0.8f);
            return null;
        }

        public void Tick(float dt, PlayerController pl)
        {
            Vector3 pp = pl.Position;
            foreach (var t in All)
            {
                if (t.root == null || !t.root.activeSelf || (pp - t.pos).sqrMagnitude > 110f * 110f) continue;
                Vector3 look = pp - t.pos; look.y = 0;
                if (look.sqrMagnitude > 0.1f && look.sqrMagnitude < 30f * 30f)
                    t.body.rotation = Quaternion.RotateTowards(t.body.rotation, Quaternion.LookRotation(look), dt * 240f);
                t.rig.Animate(dt, 0f, false, false);
                if (t.kinRig != null) { t.kinRig.SetVisible(Active != t); t.kinRig.Animate(dt, 0f, false, false); }
            }
            if (Active == null) return;

            // forfeit: knocked out, or walked away
            if (!pl.IsAlive || (pp - Active.pos).sqrMagnitude > ArenaRadius * ArenaRadius || Caves.InCave)
            {
                End(false, pl.IsAlive ? "You left the ring - match forfeited." : $"{Active.name} wins this one.");
                return;
            }
            if (fighter != null && fighter.IsAlive)
            {
                // kin against kin: their fighter goes for your companion whenever one is out
                retargetTimer -= dt;
                if (retargetTimer <= 0f)
                {
                    retargetTimer = 0.6f;
                    var comp = GameManager.I.Party.Companion;
                    ICombatant want = comp != null && comp.IsAlive ? (ICombatant)comp : pl;
                    if (fighter.Target != want) fighter.Target = want;
                    fighter.Aggro = true;
                }
                return;
            }
            // previous fighter is down (or none yet): send the next
            if (fighter != null) { fighter = null; index++; nextTimer = 1.6f; }
            if (index >= Active.team.Length) { End(true, null); return; }
            nextTimer -= dt;
            if (nextTimer > 0f) return;
            SendOut(pl);
        }

        void SendOut(PlayerController pl)
        {
            var t = Active;
            var sp = ContentDB.Sp(t.team[index]);
            var d = CreatureFactory.RollWild(sp, t.levels[index], t.home.biome, false);
            d.shimmer = false; d.traits.Remove("radiant");
            CreatureFactory.EvolveForLevel(d, 1f);   // a tamer's kin is trained: evolved as soon as its level allows
            Vector3 toPlayer = pl.Position - t.pos; toPlayer.y = 0;
            Vector3 p = t.pos + (toPlayer.sqrMagnitude > 0.1f ? toPlayer.normalized : Vector3.forward) * 3.5f;
            fighter = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, p);
            var comp = GameManager.I.Party.Companion;
            fighter.MakeDuelist(t.pos, comp != null && comp.IsAlive ? (ICombatant)comp : pl);
            Fx.Sparkle(p + Vector3.up, new Color(1f, 0.5f, 0.4f), 16, 0.8f);
            SynthAudio.Play(Sfx.Summon, 1f);
            GameEvents.Warn($"{t.name} sends out {sp.displayName} (Lv {t.levels[index]})!  {index + 1}/{t.team.Length}");
        }

        void End(bool won, string msg)
        {
            var t = Active;
            Active = null;
            if (fighter != null && fighter.gameObject.activeSelf && fighter.State != AIState.Dead) fighter.Despawn();
            fighter = null;
            if (!won) { if (msg != null) GameEvents.Warn(msg); return; }

            var s = S;
            int purse = Purse(t);
            bool first = Wins(t) == 0;
            var rec = s.tamerWins.Find(w => w.id == t.id);
            if (rec == null) { rec = new TamerRecord { id = t.id }; s.tamerWins.Add(rec); }
            rec.wins++;
            rec.unix = Expeditions.Now;
            s.glimmer += purse;
            var gm = GameManager.I;
            gm.GiveItem(t.rewardItem, t.rewardCount, gm.Player.Position + Vector3.up);
            if (Random.value < 0.3f) gm.GiveItem(Chests.KeyItem, 1, gm.Player.Position + Vector3.up);   // now and then a tamer hands over a Chest Key
            gm.AddPlayerXp(60 + purse / 4);
            Requests.AddStanding(t.home, first ? 25 : 10);
            Requests.OnTamerBeaten(t.home);
            GameEvents.Good($"You beat {t.title} {t.name}! +{purse} Glimmer, {t.rewardCount}x {ContentDB.ItemName(t.rewardItem)}{(first ? " (first win)" : "")}.");
            SynthAudio.Play(Sfx.LevelUp, 1f, 0.9f);
            Fx.Sparkle(gm.Player.Position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.4f), 40, 1.4f);
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>Called on death / scene teardown so a match never dangles.</summary>
        public void Abort() { if (Active != null) End(false, null); }
    }

    [System.Serializable]
    public class TamerRecord { public string id; public int wins; public long unix; }
}
