using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>All owned kin: party (max 5, one active companion), base workers, and the Hearthstone box.</summary>
    public class KinRoster
    {
        public const int PartySize = 5;

        public readonly List<CreatureData> All = new List<CreatureData>();
        public string companionUid;
        public CreatureActor Companion { get; private set; }

        public CompanionStance Stance
        {
            get => (CompanionStance)Mathf.Clamp(GameManager.I.Player.Stats.companionStance, 0, 2);
            set
            {
                GameManager.I.Player.Stats.companionStance = (int)value;
                if (value == CompanionStance.Passive && Companion != null) Companion.Target = null;
                GameEvents.RaiseRosterChanged();
            }
        }

        public void CycleStance()
        {
            Stance = (CompanionStance)(((int)Stance + 1) % 3);
            GameEvents.Say($"Companion: {StanceName(Stance)} - {StanceHint(Stance)}", StanceColor(Stance));
            SynthAudio.Play(Sfx.UiClick, 1.1f);
        }

        public static string StanceName(CompanionStance s) => s.ToString().ToUpperInvariant();

        public static string StanceHint(CompanionStance s)
        {
            switch (s)
            {
                case CompanionStance.Passive: return "never starts fights";
                case CompanionStance.Aggressive: return "attacks nearby wild kin";
                default: return "fights back when you or it is attacked";
            }
        }

        public static Color StanceColor(CompanionStance s)
        {
            switch (s)
            {
                case CompanionStance.Passive: return new Color(0.55f, 0.8f, 1f);
                case CompanionStance.Aggressive: return new Color(1f, 0.45f, 0.35f);
                default: return new Color(1f, 0.82f, 0.35f);
            }
        }

        public List<CreatureData> PartyList()
        {
            var l = new List<CreatureData>(PartySize);
            foreach (var d in All) if (d.location == KinLocation.Party) l.Add(d);
            return l;
        }

        /// <summary>Quick-swap: summon party slot i (recalls the current one). Tapping the active one recalls it.</summary>
        public void SummonSlot(int i)
        {
            var party = PartyList();
            if (i < 0 || i >= party.Count) return;
            var d = party[i];
            if (Companion != null && Companion.Data == d) { Recall(); return; }
            Summon(d);
        }

        public IEnumerable<CreatureData> In(KinLocation loc)
        {
            foreach (var d in All) if (d.location == loc) yield return d;
        }

        public int CountIn(KinLocation loc)
        {
            int n = 0;
            foreach (var d in All) if (d.location == loc) n++;
            return n;
        }

        public CreatureData Find(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            foreach (var d in All) if (d.uid == uid) return d;
            return null;
        }

        public void AddCaptured(CreatureData d)
        {
            d.capturedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            d.hp = -1f;
            d.location = CountIn(KinLocation.Party) < PartySize ? KinLocation.Party : KinLocation.Box;
            All.Add(d);
            GameEvents.RaiseRosterChanged();
        }

        // ============================================================ COMPANION
        public bool Summon(CreatureData d)
        {
            if (d == null || d.location != KinLocation.Party) return false;
            if (d.Fainted) { GameEvents.Warn($"{d.DisplayName} has fainted. Rest or use a Kin Treat."); return false; }
            Recall(false);
            var p = GameManager.I.Player;
            var pos = p.Position + p.Right * 1.5f + p.Forward * 0.5f;
            Companion = CreatureSpawner.I.SpawnActor(d, CreatureMode.Companion, pos);
            companionUid = d.uid;
            Fx.Sparkle(Companion.Position + Vector3.up * 0.5f, ElementInfo.Color(d.Element1), 16, 0.8f);
            SynthAudio.Play(Sfx.Summon);
            GameEvents.RaiseRosterChanged();
            return true;
        }

        public void Recall(bool fx = true)
        {
            var pl = GameManager.I.Player;
            if (pl != null && pl.IsMounted) pl.Dismount(true);
            if (Companion != null)
            {
                if (fx) Fx.Sparkle(Companion.Position + Vector3.up * 0.5f, new Color(0.7f, 1f, 0.8f), 12, 0.6f);
                var c = Companion;
                Companion = null;
                c.Despawn();
                if (fx) SynthAudio.Play(Sfx.Recall);
            }
            companionUid = null;
            GameEvents.RaiseRosterChanged();
        }

        /// <summary>Summon button: toggles; long-press cycles (handled by UI) via SummonNext.</summary>
        public void ToggleCompanion()
        {
            if (Companion != null) { Recall(); return; }
            SummonNext(null);
        }

        public void SummonNext(string afterUid)
        {
            var party = new List<CreatureData>(In(KinLocation.Party));
            if (party.Count == 0) { GameEvents.Warn("No kin in your party. Capture one with a Tether Seed!"); return; }
            int start = 0;
            if (afterUid != null) start = party.FindIndex(x => x.uid == afterUid) + 1;
            for (int i = 0; i < party.Count; i++)
            {
                var d = party[(start + i) % party.Count];
                if (!d.Fainted) { Summon(d); return; }
            }
            GameEvents.Warn("All party kin have fainted.");
        }

        public void OnCompanionFainted(CreatureActor a)
        {
            if (Companion == a)
            {
                Companion = null;
                companionUid = null;
                GameEvents.RaiseRosterChanged();
            }
        }

        /// <summary>True if any party kin is fainted or below full HP (the summoned one is read from its actor).</summary>
        public bool AnyPartyKinHurt()
        {
            if (Companion != null && Companion.IsAlive && Companion.Hp < Companion.MaxHp - 0.5f) return true;
            foreach (var d in In(KinLocation.Party))
            {
                if (Companion != null && Companion.Data == d) continue;
                if (d.Fainted || d.CurrentHp < d.MaxHp - 0.5f) return true;
            }
            return false;
        }

        public void HealParty(float amount, bool revive)
        {
            foreach (var d in In(KinLocation.Party))
            {
                if (d.Fainted && !revive) continue;
                float hp = d.Fainted ? 0f : d.CurrentHp;
                hp = Mathf.Min(d.MaxHp, hp + amount);
                d.hp = hp >= d.MaxHp ? -1f : hp;
            }
            if (Companion != null) Companion.HealAmount(amount);
            GameEvents.RaiseRosterChanged();
        }

        readonly Dictionary<string, float> faintRest = new Dictionary<string, float>();
        public const float PartyReviveSeconds = 60f, CampReviveSeconds = 30f;

        /// <summary>Seconds until a fainted kin is back on its feet (0 if not fainted).</summary>
        public float ReviveIn(CreatureData d)
        {
            if (!d.Fainted) return 0f;
            float need = d.location == KinLocation.Party ? PartyReviveSeconds : CampReviveSeconds;
            return Mathf.Max(0f, need - (faintRest.TryGetValue(d.uid, out var t) ? t : 0f));
        }

        /// <summary>Kin not out slowly recover HP. Fainted kin must rest a while, then wake at 25% HP.</summary>
        public void TickRest(float dt)
        {
            foreach (var d in All)
            {
                if (Companion != null && d == Companion.Data) continue;
                if (d.hp < 0f) continue;
                if (d.Fainted)
                {
                    faintRest.TryGetValue(d.uid, out float t);
                    t += dt;
                    bool clinic = d.location != KinLocation.Party && BaseManager.I != null && BaseManager.I.HasBuilding("kin_clinic");
                    float need = d.location == KinLocation.Party ? PartyReviveSeconds : CampReviveSeconds / (clinic ? 3f : 1f);
                    if (t >= need) { d.hp = d.MaxHp * (clinic ? 0.6f : 0.25f); faintRest.Remove(d.uid); GameEvents.Say($"{d.DisplayName} has recovered.", new Color(0.7f, 1f, 0.8f)); GameEvents.RaiseRosterChanged(); }
                    else faintRest[d.uid] = t;
                    continue;
                }
                float rate = d.location == KinLocation.Party ? 0.01f : 0.04f; // fraction of max per second (faster when resting at base/box)
                if (d.location != KinLocation.Party && BaseManager.I != null && BaseManager.I.HasBuilding("kin_clinic")) rate *= 2f;
                d.hp = Mathf.Min(d.MaxHp, d.hp + d.MaxHp * rate * dt);
                if (d.hp >= d.MaxHp) d.hp = -1f;
            }
        }

        public bool Move(CreatureData d, KinLocation to)
        {
            if (d.location == to) return true;
            if (to == KinLocation.Party && CountIn(KinLocation.Party) >= PartySize) { GameEvents.Warn("Party is full (5)."); return false; }
            if (to == KinLocation.Base)
            {
                var bm = BaseManager.I;
                if (bm == null || !bm.HasBase) { GameEvents.Warn("Build a Hearthstone first."); return false; }
                if (CountIn(KinLocation.Base) >= bm.WorkerCap) { GameEvents.Warn($"Base worker limit reached ({bm.WorkerCap}). Upgrade the Hearthstone."); return false; }
            }
            if (Companion != null && Companion.Data == d) Recall();
            if (d.location == KinLocation.Base) BaseManager.I?.RemoveWorker(d);
            if (d.location == KinLocation.Outpost) Outposts.Forget(d);
            d.location = to;
            if (to == KinLocation.Base) BaseManager.I?.AddWorker(d);
            GameEvents.RaiseRosterChanged();
            return true;
        }

        public void Release(CreatureData d)
        {
            if (Companion != null && Companion.Data == d) Recall();
            if (d.location == KinLocation.Base) BaseManager.I?.RemoveWorker(d);
            if (d.location == KinLocation.Outpost) Outposts.Forget(d);
            All.Remove(d);
            GameEvents.RaiseRosterChanged();
        }
    }
}
