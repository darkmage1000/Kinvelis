using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wildbond
{
    [Serializable]
    public class GameSettings
    {
        public int quality = -1;
        public float lookSensitivity = 1f;
        public float musicVolume = 0.45f;
        public float sfxVolume = 0.8f;
        public bool invertY;
        public bool showFps;
        public bool devMode;   // the tester's menu (Core/Dev.cs)

        const string Key = "wildbond_settings";

        public static GameSettings Load()
        {
            try
            {
                var json = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(json)) return JsonUtility.FromJson<GameSettings>(json);
            }
            catch (Exception e) { Debug.LogWarning("[Settings] " + e.Message); }
            return new GameSettings();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }

    [Serializable] public class BuildingSave { public string uid, defId, bufferItem, crop; public float x, y, z, rotY, progress; public int bufferCount, stage, filter; public List<ItemStack> feed = new List<ItemStack>(); public bool feedByHand; }
    [Serializable] public class NodeSave { public long key; public double t; }
    [Serializable] public class AlphaSave { public string id; public double t; }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int seed;
        public long savedAtUnix;
        public double worldTime;
        public float timeOfDay;
        public int weather;
        public float weatherRemaining;
        public float px, py, pz, yaw, camYaw;
        public PlayerStats player;
        public List<CreatureData> kin = new List<CreatureData>();
        public string companionUid;
        public bool hasBase;
        public int coreLevel = 1;
        public float campCharge;   // seconds of stored pylon / battery power
        public float campCold;     // seconds of stored larder cold
        public int storageLayout;  // which structures held storage slots when this was written (BaseManager.SaveLayout; 0 = before larders did)
        public List<BuildingSave> buildings = new List<BuildingSave>();
        public Inventory storage = new Inventory();
        public List<CraftOrder> orders = new List<CraftOrder>();
        public List<StandingOrder> standing = new List<StandingOrder>();
        public List<NodeSave> depleted = new List<NodeSave>();
        public List<AlphaSave> alphaCooldowns = new List<AlphaSave>();
        public List<string> goalsDone = new List<string>();
        public WorldSettings world;
        public long createdAtUnix;
        public double playSeconds;
    }

    /// <summary>Lightweight info for a save-slot card.</summary>
    public class SlotSummary
    {
        public int slot;
        public bool exists;
        public string worldName;
        public int level;
        public int kin;
        public double playSeconds;
        public long savedAtUnix;
        public WorldSettings world;
        public string partner;
    }

    /// <summary>JSON save to persistentDataPath with atomic write. Load happens at scene start (GameManager).</summary>
    public static class SaveSystem
    {
        static string LegacyPath => Path.Combine(Application.persistentDataPath, "wildbond_save.json");
        public static string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"wildbond_slot{slot}.json");
        /// <summary>Save file of the world currently being played.</summary>
        public static string FilePath => SlotPath(GameSession.Slot);
        public static bool HasSave => File.Exists(FilePath);
        public static bool SlotExists(int slot) => File.Exists(SlotPath(slot));
        const string LastSlotKey = "wildbond_last_slot";

        /// <summary>Moves a pre-slots save (wildbond_save.json) into slot 0 once.</summary>
        public static void MigrateLegacy()
        {
            try
            {
                if (File.Exists(LegacyPath) && !File.Exists(SlotPath(0))) File.Move(LegacyPath, SlotPath(0));
            }
            catch (Exception e) { Debug.LogWarning("[Save] legacy migration failed: " + e.Message); }
        }

        public static int LastSlot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(LastSlotKey, -1), -1, GameSession.SlotCount - 1);
            set { PlayerPrefs.SetInt(LastSlotKey, value); PlayerPrefs.Save(); }
        }

        public static SlotSummary Summary(int slot)
        {
            var s = new SlotSummary { slot = slot };
            try
            {
                string p = SlotPath(slot);
                if (!File.Exists(p)) return s;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
                if (d == null || d.player == null) return s;
                s.exists = true;
                s.world = d.world ?? new WorldSettings();
                s.worldName = string.IsNullOrEmpty(s.world.worldName) ? "Rimewild Isle" : s.world.worldName;
                s.level = d.player.level;
                s.kin = d.kin != null ? d.kin.Count : 0;
                s.playSeconds = d.playSeconds;
                s.savedAtUnix = d.savedAtUnix;
                if (d.kin != null)
                    foreach (var k in d.kin) { var sp = ContentDB.Sp(k.speciesId); if (sp != null && sp.starter) { s.partner = sp.displayName; break; } }
            }
            catch (Exception e) { Debug.LogWarning($"[Save] slot {slot} unreadable: {e.Message}"); }
            return s;
        }

        public static void DeleteSlot(int slot)
        {
            try { if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot)); } catch (Exception e) { Debug.LogWarning(e); }
            if (LastSlot == slot) LastSlot = -1;
        }

        public static void Save()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Started) return;
            try
            {
                var d = Capture(gm);
                string json = JsonUtility.ToJson(d);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e) { Debug.LogError("[Save] failed: " + e); GameEvents.Warn("Save failed!"); }
        }

        public static SaveData Load()
        {
            try
            {
                if (!HasSave) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return d != null && d.player != null ? d : null;
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] corrupt save, starting fresh: " + e);
                return null;
            }
        }

        public static void Delete()
        {
            try { if (HasSave) File.Delete(FilePath); } catch (Exception e) { Debug.LogWarning(e); }
        }

        /// <summary>Reload the scene; GameManager applies the save on start.</summary>
        public static void LoadAndApply()
        {
            GameManager.SkipAutosaveOnce = true;
            GameSession.NewWorld = null;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Save and return to the main menu.</summary>
        public static void QuitToMenu()
        {
            Save();
            GameManager.SkipAutosaveOnce = true;
            SceneManager.LoadScene(GameSession.MenuScene);
        }

        static SaveData Capture(GameManager gm)
        {
            var pl = gm.Player;
            var env = EnvironmentSystem.I;
            var bm = BaseManager.I;
            var d = new SaveData
            {
                seed = WorldGen.Seed,
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                worldTime = GameManager.WorldTime,
                timeOfDay = env.timeOfDay,
                weather = (int)env.Weather,
                weatherRemaining = env.WeatherRemaining,
                // caves aren't saved: underground you load back in at the cave mouth
                px = (Caves.InCave ? Caves.I.SafeOutside : pl.Position).x,
                py = (Caves.InCave ? Caves.I.SafeOutside : pl.Position).y,
                pz = (Caves.InCave ? Caves.I.SafeOutside : pl.Position).z,
                yaw = pl.transform.eulerAngles.y,
                camYaw = gm.CameraRig.Yaw,
                player = pl.Stats,
                companionUid = gm.Party.Companion != null ? gm.Party.Companion.Data.uid : null,
                hasBase = bm.HasBase,
                coreLevel = bm.CoreLevel,
                campCharge = bm.Charge,
                campCold = bm.Cold,
                storageLayout = BaseManager.SaveLayout,
                goalsDone = new List<string>(gm.GoalsDone),
                world = WorldRules.Current,
                createdAtUnix = gm.CreatedAtUnix,
                playSeconds = gm.PlaySeconds
            };
            // live companion/worker HP back into data
            if (gm.Party.Companion != null) gm.Party.Companion.Data.hp = gm.Party.Companion.Hp >= gm.Party.Companion.MaxHp ? -1f : gm.Party.Companion.Hp;
            d.kin.AddRange(gm.Party.All);
            foreach (var b in bm.Buildings)
            {
                var p = b.transform.position;
                d.buildings.Add(new BuildingSave { uid = b.Uid, defId = b.Def.id, x = p.x, y = p.y, z = p.z, rotY = b.RotY, progress = b.Progress, stage = b.Stage, filter = (int)b.Filter, feed = b.Feed != null ? b.Feed.slots : new List<ItemStack>(), feedByHand = !b.FeedAuto, bufferItem = b.BufferItem, bufferCount = b.BufferCount, crop = b.Crop });
            }
            d.storage.CopyFrom(bm.Storage);
            d.orders.AddRange(bm.Orders);
            d.standing.AddRange(bm.Standing);
            double now = GameManager.WorldTime;
            foreach (var kv in ChunkManager.I.Depleted) if (kv.Value > now) d.depleted.Add(new NodeSave { key = kv.Key, t = kv.Value });
            foreach (var kv in CreatureSpawner.I.AlphaCooldowns) if (kv.Value > now) d.alphaCooldowns.Add(new AlphaSave { id = kv.Key, t = kv.Value });
            return d;
        }
    }
}
