using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Writes everything the player wiki (docs/, published with GitHub Pages) shows about the game straight out of
    /// the running game, so the site never drifts from what is actually in it: docs/data/game.json (kin, skills,
    /// items, recipes, structures, research, Wardens, quests, places, the element chart) and the game's own rendered
    /// pictures as docs/img/kin|item|bld/*.png. Run in Play mode with a world loaded: Wildbond > Export Wiki Data.
    /// The pages themselves (docs/index.html, app.js, guides.js, style.css) are hand-written and read this file.
    /// </summary>
    public static class WikiExport
    {
        [MenuItem("Wildbond/Export Wiki Data")]
        public static void Menu() => Debug.Log("[Wiki] " + Run());

        public static string Run()
        {
            if (!Application.isPlaying || GameManager.I == null || GameManager.I.Player == null) return "Enter Play mode and load a world first.";
            string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs");
            Directory.CreateDirectory(Path.Combine(root, "data"));
            var data = new Dictionary<string, object>
            {
                ["title"] = GameInfo.Title,
                ["exported"] = System.DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["elements"] = Elements(),
                ["lands"] = Lands(),
                ["kin"] = Kin(),
                ["skills"] = Skills(),
                ["items"] = Items(),
                ["recipes"] = Recipes(),
                ["structures"] = Structures(),
                ["research"] = Research(),
                ["wardens"] = WardenList(),
                ["quests"] = QuestLines(),
                ["places"] = Places(),
                ["hunts"] = Hunts(),
                ["crops"] = CropList(),
                ["traits"] = Traits(),
                ["relics"] = RelicList(),
                ["dexMilestones"] = DexMilestones(),
            };
            File.WriteAllText(Path.Combine(root, "data", "game.json"), Json(data), new UTF8Encoding(false));
            int pics = Pictures(root);
            return $"wrote docs/data/game.json ({ContentDB.SpeciesList.Count} kin, {ContentDB.Abilities.Count} skills, {ContentDB.Items.Count} items) and {pics} pictures";
        }

        // ---------------------------------------------------------------- data
        static List<object> L() => new List<object>();
        static Dictionary<string, object> O() => new Dictionary<string, object>();
        static string Name(BiomeId b) => BiomeInfo.Name(b);

        static object Elements()
        {
            var names = L();
            var strong = O();
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
            {
                names.Add(e.ToString());
                var beats = L();
                foreach (Element d in System.Enum.GetValues(typeof(Element))) if (ElementInfo.IsStrong(e, d)) beats.Add(d.ToString());
                strong[e.ToString()] = beats;
            }
            return new Dictionary<string, object> { ["names"] = names, ["strong"] = strong };
        }

        static object Lands()
        {
            var l = L();
            foreach (BiomeId b in System.Enum.GetValues(typeof(BiomeId))) l.Add(new Dictionary<string, object> { ["id"] = b.ToString(), ["name"] = Name(b) });
            return l;
        }

        static object Kin()
        {
            var l = L();
            var all = new List<SpeciesDef>(ContentDB.SpeciesList);
            all.Sort((a, b) => a.dexNumber.CompareTo(b.dexNumber));
            foreach (var sp in all)
            {
                var o = O();
                o["id"] = sp.id; o["dex"] = sp.dexNumber; o["name"] = sp.displayName;
                o["e1"] = sp.element1.ToString(); o["e2"] = sp.element2 != sp.element1 && sp.element2 != Element.Normal ? sp.element2.ToString() : null;
                o["rarity"] = sp.rarity.ToString();
                var lands = L();
                foreach (BiomeId b in System.Enum.GetValues(typeof(BiomeId))) if (sp.LivesIn(b)) lands.Add(b.ToString());
                o["lands"] = lands;
                o["temper"] = sp.temperament.ToString(); o["moves"] = sp.locomotion.ToString(); o["size"] = sp.size;
                o["hp"] = sp.baseHp; o["atk"] = sp.baseAtk; o["def"] = sp.baseDef; o["speed"] = sp.runSpeed;
                o["lore"] = sp.lore; o["personality"] = sp.personality; o["perk"] = sp.overworldRole; o["role"] = sp.role;
                o["skills"] = new List<object>(sp.abilities ?? new string[0]);
                o["rareSkills"] = new List<object>(sp.rareAbilities ?? new string[0]);
                var jobs = L();
                foreach (var w in sp.work) jobs.Add(new Dictionary<string, object> { ["job"] = w.type.ToString(), ["lv"] = w.level });
                o["jobs"] = jobs;
                var drops = L();
                foreach (var d in sp.drops) drops.Add(new Dictionary<string, object> { ["item"] = d.itemId, ["min"] = d.min, ["max"] = d.max, ["chance"] = d.chance });
                o["drops"] = drops;
                o["mount"] = sp.mountable; o["water"] = sp.nearWater;
                o["surf"] = sp.nearWater || sp.element1 == Element.Tide || sp.element2 == Element.Tide;
                o["when"] = sp.spawnCondition != SpawnCondition.None ? EnvironmentSystem.ConditionText(sp.spawnCondition) : sp.spawnTime != SpawnTime.Any ? sp.spawnTime.ToString().ToLowerInvariant() + " only" : null;
                o["starter"] = sp.starter; o["boss"] = sp.bossOnly; o["wild"] = sp.spawnWeight > 0f && !sp.starter && !sp.bossOnly;
                l.Add(o);
            }
            return l;
        }

        static object Skills()
        {
            var l = L();
            var all = new List<AbilityDef>(ContentDB.Abilities.Values);
            all.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
            foreach (var a in all)
                l.Add(new Dictionary<string, object>
                {
                    ["id"] = a.id, ["name"] = a.displayName, ["element"] = a.element.ToString(), ["kind"] = a.kind.ToString(), ["power"] = a.power,
                    ["range"] = a.kind == AbilityKind.Burst ? a.radius : a.range, ["cooldown"] = a.cooldown,
                    ["status"] = a.status != StatusType.None ? a.status.ToString() : null, ["chance"] = a.statusChance,
                    ["special"] = SkillEffects.Describe(a),
                });
            return l;
        }

        static List<object> Cost(Ingredient[] ins)
        {
            var l = L();
            if (ins != null) foreach (var i in ins) l.Add(new Dictionary<string, object> { ["item"] = i.itemId, ["n"] = i.count });
            return l;
        }

        static object Items()
        {
            var l = L();
            var all = new List<ItemDef>(ContentDB.Items.Values);
            all.Sort((a, b) => a.category != b.category ? a.category.CompareTo(b.category) : string.CompareOrdinal(a.displayName, b.displayName));
            foreach (var d in all)
                l.Add(new Dictionary<string, object>
                {
                    ["id"] = d.id, ["name"] = d.displayName, ["type"] = d.category.ToString(), ["about"] = d.description, ["does"] = MenuUI.ItemStatLine(d),
                });
            return l;
        }

        static object Recipes()
        {
            var l = L();
            foreach (var r in ContentDB.RecipeList)
            {
                var t = Tech.ForRecipe(r.outputItem);
                l.Add(new Dictionary<string, object>
                {
                    ["makes"] = r.outputItem, ["n"] = r.outputCount, ["at"] = MenuUI.StationName(r.station), ["level"] = r.unlockLevel, ["needs"] = Cost(r.inputs),
                    ["research"] = t != null ? t.name : null,
                });
            }
            return l;
        }

        static object Structures()
        {
            var l = L();
            foreach (var d in ContentDB.BuildingList)
            {
                var t = Tech.ForBuilding(d.id);
                l.Add(new Dictionary<string, object>
                {
                    ["id"] = d.id, ["name"] = d.displayName, ["about"] = d.description, ["level"] = d.unlockLevel, ["cost"] = Cost(d.cost),
                    ["group"] = MenuUI.BuildCatOf(d).ToString(), ["job"] = d.workType != WorkType.None ? d.workType.ToString() : null, ["workers"] = d.workerSlots,
                    ["makes"] = string.IsNullOrEmpty(d.outputItem) ? null : d.outputItem, ["research"] = t != null ? t.name : null,
                });
            }
            return l;
        }

        static object Research()
        {
            var l = L();
            foreach (var t in Tech.All)
            {
                var w = string.IsNullOrEmpty(t.sigil) ? null : Wardens.Get(t.sigil);
                TechDef pre = null;
                foreach (var x in Tech.All) if (x.id == t.prereq) pre = x;
                l.Add(new Dictionary<string, object>
                {
                    ["name"] = t.name, ["about"] = t.desc, ["cost"] = Cost(t.cost), ["sigil"] = w != null ? w.land : null, ["after"] = pre != null ? pre.name : null,
                    ["recipes"] = new List<object>(t.recipes), ["structures"] = new List<object>(t.buildings), ["metal"] = t.group == 1,
                });
            }
            return l;
        }

        static object WardenList()
        {
            var l = L();
            foreach (var w in Wardens.ByLevel())
                l.Add(new Dictionary<string, object>
                {
                    ["id"] = w.id, ["name"] = w.Name, ["kin"] = w.species, ["place"] = w.place, ["land"] = w.land, ["level"] = w.level, ["optional"] = w.optional, ["final"] = w.final,
                    ["phase2"] = Wardens.Describe(w.phase2, w), ["phase3"] = Wardens.Describe(w.phase3 | w.phase2 & ~(WardenMech.Shield | WardenMech.Adds), w),
                    ["purse"] = Wardens.Purse(w),
                });
            return l;
        }

        static object QuestLines()
        {
            var l = L();
            for (int i = 0; i < Quests.Lines.Length; i++)
            {
                var qs = L();
                foreach (var q in Quests.Lines[i])
                {
                    var objs = L();
                    foreach (var o in q.objs) objs.Add(o.need > 1 ? $"{o.text} ({o.need})" : o.text);
                    qs.Add(new Dictionary<string, object>
                    {
                        ["id"] = q.id, ["title"] = q.title, ["chapter"] = q.chapter, ["story"] = q.story, ["how"] = q.how, ["todo"] = objs, ["reward"] = Quests.RewardText(q),
                    });
                }
                l.Add(new Dictionary<string, object> { ["tab"] = Quests.LineTabs[i], ["name"] = Quests.LineNames[i], ["about"] = Quests.LineBlurb[i], ["quests"] = qs });
            }
            return l;
        }

        static object Places()
        {
            var regions = L();
            foreach (var r in SubRegions.All) regions.Add(new Dictionary<string, object> { ["name"] = r.name, ["land"] = r.biome.ToString(), ["about"] = r.blurb });
            var towns = L();
            foreach (var s in Settlements.All) towns.Add(new Dictionary<string, object> { ["name"] = s.name, ["land"] = s.biome.ToString(), ["keeper"] = s.keeper });
            var caves = L();
            if (Caves.I != null)
                foreach (var c in Caves.I.Entrances)
                {
                    var boss = string.IsNullOrEmpty(c.boss) ? null : ContentDB.Sp(c.boss);
                    caves.Add(new Dictionary<string, object> { ["name"] = c.name, ["kind"] = c.kind.ToString(), ["land"] = c.biome.ToString(), ["min"] = c.minLv, ["max"] = c.maxLv, ["guardian"] = boss != null ? boss.id : null });
                }
            var stones = L();
            if (Waystones.I != null) foreach (var w in Waystones.I.All) stones.Add(new Dictionary<string, object> { ["name"] = w.name, ["land"] = w.biome.ToString() });
            return new Dictionary<string, object> { ["regions"] = regions, ["villages"] = towns, ["underground"] = caves, ["waystones"] = stones };
        }

        static object Hunts()
        {
            var l = L();
            foreach (var h in LegendHunts.All)
                l.Add(new Dictionary<string, object> { ["kin"] = h.species, ["land"] = h.land.ToString(), ["level"] = h.level, ["lair"] = h.lair, ["seed"] = h.seed, ["when"] = h.when.ToString(), ["intro"] = h.intro });
            return l;
        }

        static object CropList()
        {
            var l = L();
            foreach (var c in Crops.All) l.Add(new Dictionary<string, object> { ["item"] = c.id, ["where"] = c.where });
            return l;
        }

        static object Traits()
        {
            var l = L();
            foreach (var t in ContentDB.TraitList)
                l.Add(new Dictionary<string, object> { ["name"] = t.displayName, ["about"] = t.description, ["good"] = t.positive, ["wild"] = t.weight > 0f });
            return l;
        }

        static object RelicList()
        {
            var l = L();
            for (int i = 0; i < (int)RelicType.Count; i++)
                l.Add(new Dictionary<string, object> { ["name"] = Relics.TypeName((RelicType)i), ["each"] = Relics.TypeBonus((RelicType)i), ["count"] = Relics.CountOf((RelicType)i) });
            return l;
        }

        static object DexMilestones()
        {
            var l = L();
            foreach (var m in DexRewards.Milestones)
                l.Add(new Dictionary<string, object> { ["caught"] = DexRewards.Need(m), ["reward"] = DexRewards.RewardText(m) });
            return l;
        }

        // ---------------------------------------------------------------- pictures
        static int Pictures(string root)
        {
            int n = 0;
            foreach (var sp in ContentDB.SpeciesList) n += Save(ItemIcons.Get("kin_" + sp.id), Path.Combine(root, "img", "kin", sp.id + ".png"));
            foreach (var d in ContentDB.Items.Values) n += Save(ItemIcons.Get(d.id), Path.Combine(root, "img", "item", d.id + ".png"));
            foreach (var d in ContentDB.BuildingList) n += Save(ItemIcons.Get("bld_" + d.id), Path.Combine(root, "img", "bld", d.id + ".png"));
            return n;
        }

        /// <summary>Copies a sprite out through a render texture, so it works whatever format or readability the game gave its texture.</summary>
        static int Save(Sprite s, string path)
        {
            if (s == null || s.texture == null) return 0;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var src = s.texture;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            Rect r = s.textureRect;
            var t = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(r.x, r.y, r.width, r.height), 0, 0);
            t.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            return 1;
        }

        // ---------------------------------------------------------------- a small JSON writer (no package needed)
        static string Json(object v) { var sb = new StringBuilder(1 << 18); Write(sb, v); return sb.ToString(); }

        static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string s) { Str(sb, s); return; }
            if (v is bool b) { sb.Append(b ? "true" : "false"); return; }
            if (v is int i) { sb.Append(i.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is float f) { sb.Append(f.ToString("0.###", CultureInfo.InvariantCulture)); return; }
            if (v is double d) { sb.Append(d.ToString("0.###", CultureInfo.InvariantCulture)); return; }
            if (v is IDictionary<string, object> o)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in o) { if (!first) sb.Append(','); first = false; Str(sb, kv.Key); sb.Append(':'); Write(sb, kv.Value); }
                sb.Append('}');
                return;
            }
            if (v is IEnumerable e)
            {
                sb.Append('[');
                bool first = true;
                foreach (var x in e) { if (!first) sb.Append(','); first = false; Write(sb, x); }
                sb.Append(']');
                return;
            }
            Str(sb, v.ToString());
        }

        static void Str(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append(' '); break;
                    default: if (c < 32) sb.Append(' '); else sb.Append(c); break;
                }
            }
            sb.Append('"');
        }
    }
}
