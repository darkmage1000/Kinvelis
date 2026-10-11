using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Developer harness for kin skills, driven from the editor in Play mode (MCP execute_code). Not part of the game.
    ///   SkillTest.Begin()                   pick the first starter, make the player unhurtable
    ///   SkillTest.Run(species, ability, p)  report on the previous test, then: summon a Lv 20 kin of that species,
    ///                                       stand a stunned 40x-health Normal dummy 6 m ahead, and order the skill.
    ///                                       p: 1 dummy burning, 2 dummy at 10% health, 3 kin at 40% health, 4 player at 30%
    ///   SkillTest.HitPlayer / HitCompanion  land a plain hit from the dummy (Guard, Riposte)
    ///   SkillTest.Lineup(from, to)          static models of those Dex numbers on a grid 300 m up, for screenshots
    /// </summary>
    public static class SkillTest
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly System.Type T = typeof(CreatureActor);

        static CreatureActor Dummy()
        {
            foreach (var a in CreatureRegistry.All) if (a.gameObject.name.StartsWith("TESTDUMMY")) return a;
            return null;
        }

        static float F(object o, string field) => (float)T.GetField(field, BF).GetValue(o);
        static void SetHp(CreatureActor a, float v) => T.GetProperty("Hp").GetSetMethod(true).Invoke(a, new object[] { v });

        public static string Begin()
        {
            var ss = Object.FindFirstObjectByType<StarterSelect>();
            if (ss != null)
            {
                var f = typeof(StarterSelect).GetField("selected", BF);
                if (f != null) f.SetValue(ss, 0);
                typeof(StarterSelect).GetMethod("Confirm", BF).Invoke(ss, null);
            }
            var p = GameManager.I.Player;
            typeof(PlayerController).GetField("invuln", BF).SetValue(p, 99999f);
            return (ss != null ? "starter confirmed" : "no starter screen") + ", player at " + p.Position;
        }

        public static string Report()
        {
            var gm = GameManager.I; var pl = gm.Player; var comp = gm.Party.Companion; var dummy = Dummy();
            if (dummy == null || comp == null) return "(nothing to report)";
            float before = float.Parse(dummy.gameObject.name.Split('|')[1], CultureInfo.InvariantCulture);
            float now = Time.time;
            return comp.Sp.id + ": dmg=" + (before - dummy.Hp).ToString("0.0") + " dummyHp=" + dummy.HpFrac.ToString("0.000") + " hex=" + dummy.Hexed + " teth=" + dummy.Tethered
                + " status=" + dummy.Status.type + " compHp=" + comp.HpFrac.ToString("0.00") + " plHp=" + (pl.Stats.hp / pl.Stats.MaxHp).ToString("0.00")
                + " tempered=" + pl.Tempered + " anthem=" + CreatureActor.AnthemMul(Team.Player, pl.Position) + " rally=" + CreatureActor.RallyMul(Team.Player, pl.Position)
                + " out=" + comp.OutMul + " in=" + comp.InMul + " decoys=" + Decoy.Active.Count
                + " guard=" + (F(comp, "guardUntil") - now).ToString("0.0") + " brace=" + (F(comp, "braceUntil") - now).ToString("0.0")
                + " dist=" + Vector3.Distance(comp.Position, dummy.Position).ToString("0.0")
                + " cds=" + comp.SkillCooldownLeft(0).ToString("0.0") + "," + comp.SkillCooldownLeft(1).ToString("0.0") + "," + comp.SkillCooldownLeft(2).ToString("0.0");
        }

        public static string Run(string id, string abil, int prep)
        {
            var gm = GameManager.I; var pl = gm.Player; var party = gm.Party;
            string prev = "PREV " + Report() + "\n";
            var dummy = Dummy();
            if (dummy != null) { dummy.gameObject.name = "Creature"; dummy.Despawn(); }
            var comp = party.Companion;
            if (comp != null && comp.Sp.dexNumber >= 188) { var old = comp.Data; party.Recall(false); party.Release(old); }
            Decoy.ClearAll();
            if (string.IsNullOrEmpty(id)) return prev;

            var d = CreatureFactory.RollWild(ContentDB.Sp(id), 20, BiomeId.Meadow, false);
            party.AddCaptured(d);
            bool ok = party.Summon(d);
            comp = party.Companion;
            pl.Stats.hp = pl.Stats.MaxHp;
            var dd = CreatureFactory.RollWild(ContentDB.Sp("knuckub"), 20, BiomeId.Meadow, false);
            dummy = CreatureSpawner.I.SpawnActor(dd, CreatureMode.Wild, pl.Position + pl.Forward * 6f);
            dummy.Persistent = true; dummy.Toughen(40f); dummy.Stun(600f);
            if (prep == 1) dummy.ApplyStatus(StatusType.Burn);
            if (prep == 2) SetHp(dummy, dummy.MaxHp * 0.1f);
            if (prep == 3) SetHp(comp, comp.MaxHp * 0.4f);
            if (prep == 4) pl.Stats.hp = pl.Stats.MaxHp * 0.3f;
            dummy.gameObject.name = "TESTDUMMY|" + dummy.Hp.ToString(CultureInfo.InvariantCulture);
            return prev + "SET " + id + " summon=" + ok + " " + Order(abil);
        }

        /// <summary>Orders one skill of the current companion at the dummy; every other skill is put on a long cooldown.</summary>
        public static string Order(string abil)
        {
            var comp = GameManager.I.Party.Companion; var dummy = Dummy();
            int slot = -1;
            string names = "";
            for (int i = 0; i < comp.SkillCount; i++)
            {
                names += comp.Skill(i).id + " ";
                if (comp.Skill(i).id == abil) slot = i;
            }
            var cds = (float[])T.GetField("cooldowns", BF).GetValue(comp);
            for (int i = 0; i < cds.Length; i++) cds[i] = i == slot ? 0f : 99f;
            string why = slot >= 0 ? comp.CommandSkill(slot, dummy) : "NOT KNOWN";
            return "order " + abil + " slot=" + slot + " refused=" + (why ?? "no") + " skills=" + names;
        }

        public static string HitPlayer(float dmg)
        {
            var pl = GameManager.I.Player;
            var inv = typeof(PlayerController).GetField("invuln", BF);
            inv.SetValue(pl, 0f);
            float before = pl.Stats.hp;
            pl.ReceiveHit(new HitInfo { attacker = Dummy(), damage = dmg, element = Element.Normal, effectiveness = 1f });
            inv.SetValue(pl, 99999f);
            return "player took " + (before - pl.Stats.hp).ToString("0.0") + " of " + dmg + "; " + Report();
        }

        public static string HitCompanion(float dmg)
        {
            var comp = GameManager.I.Party.Companion;
            float before = comp.Hp;
            comp.ReceiveHit(new HitInfo { attacker = Dummy(), damage = dmg, element = Element.Normal, effectiveness = 1f });
            return "companion took " + (before - comp.Hp).ToString("0.0") + " of " + dmg + "; " + Report();
        }

        public static string Lineup(int from, int to)
        {
            var old = GameObject.Find("ModelLineup");
            if (old != null) Object.Destroy(old);
            var root = new GameObject("ModelLineup");
            Vector3 basePos = GameManager.I.Player.Position + new Vector3(0, 300f, 0);
            root.transform.position = basePos;
            int n = 0;
            for (int dex = from; dex <= to; dex++)
            {
                SpeciesDef sp = null;
                foreach (var s in ContentDB.Species.Values) if (s.dexNumber == dex) sp = s;
                if (sp == null) continue;
                var go = new GameObject("m_" + sp.id);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3((n % 4) * 2.2f, -(n / 4) * 3f, 0);
                go.transform.localScale = Vector3.one * sp.size;
                go.AddComponent<MeshFilter>().sharedMesh = ModelBuilder.BuildStatic(sp.parts, sp.palette, "lineup_" + sp.id);
                go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
                n++;
            }
            return n + " models, 4 per row (2.2 m apart, rows 3 m down), first at " + basePos.x.ToString("0.00") + "," + basePos.y.ToString("0.00") + "," + basePos.z.ToString("0.00") + " facing +Z";
        }
    }
}
