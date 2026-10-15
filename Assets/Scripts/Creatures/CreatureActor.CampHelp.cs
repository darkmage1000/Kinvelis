using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Companion camp help (user request 2026-10-09): while you are inside your camp ring, the kin that is out with
    /// you stops trailing behind and does the camp work it is able to do - it goes to the structure where its job
    /// levels count for most (preferring one nobody is staffing), works there as an extra pair of hands beside the
    /// assigned crew (it takes no worker slot), and with nothing to make it carries finished goods to storage if it
    /// can haul. It earns the same work XP as a camp worker. It drops everything and comes back to your side the
    /// moment you step out of the ring, mount up, or a fight starts (its stance still decides who it fights; an
    /// intruder in the camp counts unless it is Passive). With no job it can do, it follows you as before.
    /// This is the kin acting on its own - the player's own camp work stays an explicit WORK tap.
    /// Switch: PlayerStats.companionRests (Camp > Workers).
    /// </summary>
    public partial class CreatureActor
    {
        Building helpBuilding;
        BaseManager.Site helpSite;
        bool helpHauling;
        float helpPick, helpScan;
        bool helpAnnounced;

        /// <summary>What the companion is doing in camp ("Working: Lumber Mill"), null while it is simply following.</summary>
        public string CampHelpStatus { get; private set; }

        void StopCampHelp()
        {
            if (haulCount > 0 && haulSource != null) haulSource.ReturnToBuffer(haulItem, haulCount);
            haulCount = 0;
            haulSource = null;
            helpBuilding = null;
            helpSite = null;
            helpHauling = false;
            CampHelpStatus = null;
        }

        /// <summary>Returns true while the companion is busy in camp (so it doesn't walk back to its follow slot).</summary>
        bool TickCampHelp(float dt, PlayerController player)
        {
            var bm = BaseManager.I;
            bool busy = CampHelpStatus != null;
            // a few metres of grace on the way out, so walking the edge of the ring doesn't make it dither
            bool inCamp = bm != null && bm.HasBase && !Caves.InCave && player.IsAlive && !player.IsMounted && !player.Stats.companionRests
                          && bm.IsInsideBase(player.Position, busy ? 3f : 0f);
            if (!inCamp)
            {
                if (busy || haulCount > 0) StopCampHelp();
                helpAnnounced = false;
                return false;
            }

            // an intruder is worth more than a plank
            helpScan -= dt;
            if (helpScan <= 0f)
            {
                helpScan = 0.5f;
                if (GameManager.I.Party.Stance != CompanionStance.Passive && Hp > MaxHp * 0.25f)
                {
                    var foe = bm.FindIntruder(transform.position);
                    if (foe != null) { StopCampHelp(); Target = foe; return false; }
                }
            }

            // a kin with the Crafting job builds what you have placed first (user playtest 2026-10-15)
            if (haulCount == 0 && bm.Sites.Count > 0 && BaseManager.CanBuild(Data))
            {
                if (helpSite == null || helpSite.finished || !bm.Sites.Contains(helpSite)) helpSite = bm.SiteFor(transform.position);
                if (helpSite != null) { helpBuilding = null; helpHauling = false; return TickHelpBuild(bm, dt); }
            }
            else helpSite = null;

            // choose (and keep re-checking) the job
            helpPick -= dt;
            bool lost = helpBuilding != null && !helpBuilding.CanWork(Data);
            if (haulCount == 0 && (helpPick <= 0f || lost))
            {
                helpPick = 2.5f;
                PickCampJob(bm);
            }

            if (helpBuilding != null)
            {
                var b = helpBuilding;
                if (FlatDist(transform.position, b.transform.position) > b.Def.footprint + 1.8f)
                {
                    State = AIState.Follow;
                    MoveTo(b.WorkSpot(b.WorkerSlots), RunSpeed * 0.85f);   // the spot after the assigned crew's
                    CampHelpStatus = "To " + b.Def.displayName;
                    return true;
                }
                State = AIState.Working;
                faceTarget = b.transform.position;
                b.AddWork(bm.WorkRate(Data, b) * dt);
                bm.AccrueWorkXp(Data, dt);
                CampHelpStatus = "Working: " + b.Def.displayName;
                emoteTimer -= dt;
                if (emoteTimer <= 0f)
                {
                    emoteTimer = Random.Range(1.2f, 2.2f);
                    Fx.Debris(b.transform.position + Vector3.up * 0.8f, ElementInfo.Color(Data.Element1), 3);
                }
                Announce($"{Data.DisplayName} gets to work at the {b.Def.displayName}.");
                return true;
            }

            if (helpHauling) return TickHelpHaul(bm, dt);

            CampHelpStatus = null;
            return false;
        }

        bool TickHelpBuild(BaseManager bm, float dt)
        {
            var s = helpSite;
            Vector3 away = transform.position - s.pos; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            float reach = s.radius + BodyRadius + 0.7f;
            if (away.magnitude > reach + 0.5f)
            {
                State = AIState.Follow;
                Vector3 spot = s.pos + away.normalized * reach; spot.y = Mathf.Min(transform.position.y, s.pos.y);
                MoveTo(spot, RunSpeed * 0.85f);
                CampHelpStatus = "To build: " + s.Name;
                return true;
            }
            State = AIState.Working;
            faceTarget = s.pos;
            bm.AddSiteWork(s, BaseManager.BuildRate(Data) * dt);
            CampHelpStatus = $"Building: {s.Name} {Mathf.RoundToInt(s.Progress01 * 100f)}%";
            emoteTimer -= dt;
            if (emoteTimer <= 0f)
            {
                emoteTimer = Random.Range(0.6f, 1.0f);
                Fx.Puff(s.pos + Vector3.up * 0.5f + Random.insideUnitSphere * 0.4f, new Color(0.85f, 0.75f, 0.55f), 0.22f, 0.6f);
            }
            Announce($"{Data.DisplayName} pitches in building the {s.Name}.");
            return true;
        }

        void Announce(string msg)
        {
            if (helpAnnounced) return;
            helpAnnounced = true;
            GameEvents.Say(msg, new Color(0.7f, 1f, 0.8f));
        }

        void PickCampJob(BaseManager bm)
        {
            Building best = null; float bestScore = 0f;
            foreach (var b in bm.Buildings)
            {
                if (b == null || b.Def.isCore || b.Def.workType == WorkType.Hauling || !b.CanWork(Data)) continue;
                // where this kin's hands count for most; an unstaffed place first, and don't hop between equals
                float score = bm.WorkRate(Data, b) * (b.AssignedCount == 0 ? 1.6f : 1f) * (b == helpBuilding ? 1.3f : 1f);
                if (score > bestScore) { bestScore = score; best = b; }
            }
            helpBuilding = best;
            helpHauling = best == null && Data.WorkLevel(WorkType.Hauling) > 0 && bm.FindHaulSource() != null;
            if (!helpHauling) haulSource = null;
        }

        bool TickHelpHaul(BaseManager bm, float dt)
        {
            bm.AccrueWorkXp(Data, dt);
            State = AIState.Follow;
            if (haulCount == 0)
            {
                if (haulSource == null || haulSource.BufferCount == 0) haulSource = bm.FindHaulSource();
                if (haulSource == null) { helpHauling = false; helpPick = 0f; CampHelpStatus = null; return false; }
                CampHelpStatus = "Hauling";
                MoveTo(haulSource.WorkSpot(3), RunSpeed * 0.75f);
                if (FlatDist(transform.position, haulSource.transform.position) < haulSource.Def.footprint + 1.6f)
                    haulCount = haulSource.TakeFromBuffer(4 + Data.WorkLevel(WorkType.Hauling) * 3, out haulItem);
                Announce($"{Data.DisplayName} starts carrying goods to storage.");
                return true;
            }
            CampHelpStatus = $"Carrying {haulCount} {ContentDB.ItemName(haulItem)}";
            var dest = bm.StorageFor(haulItem, transform.position);
            Vector3 dpos = dest != null ? dest.transform.position : bm.CorePosition;
            float fp = dest != null ? dest.Def.footprint : 1.6f;
            MoveTo(dpos, RunSpeed * 0.75f);
            if (FlatDist(transform.position, dpos) < fp + 1.6f)
            {
                int left = bm.Deposit(bm.StorageFor(haulItem, transform.position), haulItem, haulCount);
                if (left > 0 && haulSource != null) haulSource.ReturnToBuffer(haulItem, left);
                haulCount = 0;
                helpPick = 0f;   // something to make may have come up meanwhile
                GameEvents.RaiseInventoryChanged();
            }
            return true;
        }
    }
}
