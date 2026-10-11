using UnityEngine;

namespace Wildbond
{
    public partial class CreatureActor
    {
        Building workBuilding;
        Building haulSource;
        string haulItem;
        int haulCount;
        float emoteTimer;
        float defendScan;

        /// <summary>Drop the job (keeping any carried goods safe) and fight an intruder.</summary>
        void BeginDefend(ICombatant foe)
        {
            if (haulCount > 0 && haulSource != null) { haulSource.ReturnToBuffer(haulItem, haulCount); haulCount = 0; }
            Target = foe;
            SetState(AIState.Defend);
            HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 0.9f), "!", new Color(1f, 0.6f, 0.2f), 1.2f);
            BaseManager.I?.OnDefenderEngaged(this);
        }

        void ThinkDefend(BaseManager bm, float dt)
        {
            // badly hurt: fall back to the hearth
            if (Hp < MaxHp * 0.2f)
            {
                Target = null;
                moveTarget = RandomPointNear(bm.CorePosition, 3f);
                SetState(AIState.Wander, 4f);
                return;
            }
            if (Target == null || !Target.IsAlive || !bm.IsInsideBase(Target.Position, 22f))
            {
                Target = bm.FindIntruder(transform.position);
                if (Target == null)
                {
                    // camp is clear: back to the job
                    SetState(AIState.Idle, 0.4f);
                    HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 0.9f), "back to work", new Color(0.7f, 1f, 0.7f), 0.6f);
                    return;
                }
            }
            FightTarget(dt);
        }

        partial void OnWorkerDespawn()
        {
            workBuilding = null;
            haulSource = null;
            haulCount = 0;
        }

        /// <summary>Worker brain while the player is near the base (far away, BaseManager simulates abstractly).</summary>
        void ThinkWorker(float dt)
        {
            var bm = BaseManager.I;
            if (bm == null || !bm.HasBase) { SetState(AIState.Idle, 1f); return; }

            // hunger drains in BaseManager.SimulateWorkers so near/far stay consistent
            workBuilding = bm.FindBuilding(Data.assignedBuilding);

            // camp defense: intruders take priority over work
            if (State == AIState.Recover) { if (stateTimer <= 0f) SetState(Target != null && Target.IsAlive ? AIState.Defend : AIState.Idle, 0f); return; }
            if (State == AIState.Defend) { ThinkDefend(bm, dt); return; }
            defendScan -= dt;
            if (defendScan <= 0f)
            {
                defendScan = 0.4f;
                if (Hp > MaxHp * 0.3f && !Data.Fainted)
                {
                    var foe = bm.FindIntruder(transform.position);
                    if (foe != null) { BeginDefend(foe); return; }
                }
            }

            // eat when hungry and food exists
            if (State != AIState.ToEat && State != AIState.Eating && Data.hunger < 30f && bm.HasFeedBox && bm.HasFood)
            {
                SetState(AIState.ToEat, 20f);
            }

            switch (State)
            {
                case AIState.ToEat:
                {
                    var feed = bm.FeedBox;
                    if (feed == null || stateTimer <= 0f) { SetState(AIState.Idle, 1f); break; }
                    MoveTo(feed.WorkSpot(0), WalkSpeed * 1.2f);
                    if (FlatDist(transform.position, feed.transform.position) < feed.Def.footprint + 1.4f) SetState(AIState.Eating, 2.5f);
                    break;
                }
                case AIState.Eating:
                    faceTarget = bm.FeedBox != null ? bm.FeedBox.transform.position : transform.position;
                    if (stateTimer <= 0f)
                    {
                        if (bm.EatFood(Data)) Fx.Sparkle(transform.position + Vector3.up, new Color(1f, 0.6f, 0.7f), 6, 0.3f);
                        SetState(AIState.Idle, 0.5f);
                    }
                    break;
                case AIState.Haul:
                    TickHaul(bm);
                    break;
                case AIState.ToWork:
                    if (workBuilding == null) { SetState(AIState.Idle, 1f); break; }
                    MoveTo(workBuilding.WorkSpot(workBuilding.SlotOf(Data.uid)), WalkSpeed * 1.3f);
                    if (FlatDist(transform.position, workBuilding.transform.position) < workBuilding.Def.footprint + 1.6f) SetState(AIState.Working);
                    break;
                case AIState.Working:
                    if (workBuilding == null) { SetState(AIState.Idle, 1f); break; }
                    faceTarget = workBuilding.transform.position;
                    if (!workBuilding.CanWork(Data)) { SetState(AIState.Idle, 2f); break; }
                    workBuilding.AddWork(bm.WorkRate(Data, workBuilding) * dt);
                    bm.AccrueWorkXp(Data, dt);
                    emoteTimer -= dt;
                    if (emoteTimer <= 0f)
                    {
                        emoteTimer = Random.Range(1.2f, 2.2f);
                        Fx.Debris(workBuilding.transform.position + Vector3.up * 0.8f, ElementInfo.Color(Data.Element1), 3);
                    }
                    break;
                case AIState.Rest:
                    if (stateTimer <= 0f) SetState(AIState.Idle, 0.5f);
                    break;
                case AIState.Wander:
                    MoveTo(moveTarget, WalkSpeed);
                    if (stateTimer <= 0f || FlatDist(transform.position, moveTarget) < 0.8f) SetState(AIState.Idle, Random.Range(1f, 3f));
                    break;
                default: // Idle: decide what to do next
                    if (stateTimer > 0f) break;
                    if (workBuilding != null)
                    {
                        if (workBuilding.Def.workType == WorkType.Hauling || workBuilding.Def.isCore)
                        {
                            if (StartHaul(bm)) break;
                        }
                        else if (workBuilding.CanWork(Data)) { SetState(AIState.ToWork, 0f); break; }
                    }
                    if (EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight && Sp.spawnTime != SpawnTime.Night && bm.HasBeds && Random.value < 0.5f)
                    {
                        SetState(AIState.Rest, Random.Range(6f, 12f));
                        break;
                    }
                    moveTarget = RandomPointNear(bm.CorePosition, 7f);
                    SetState(AIState.Wander, Random.Range(3f, 6f));
                    break;
            }
        }

        bool StartHaul(BaseManager bm)
        {
            var src = bm.FindHaulSource();
            if (src == null) return false;
            haulSource = src;
            haulCount = 0;
            SetState(AIState.Haul, 30f);
            return true;
        }

        void TickHaul(BaseManager bm)
        {
            bm.AccrueWorkXp(Data, Time.deltaTime);
            if (stateTimer <= 0f) { haulCount = 0; SetState(AIState.Idle, 1f); return; }
            if (haulCount == 0)
            {
                if (haulSource == null || haulSource.BufferCount == 0) { SetState(AIState.Idle, 0.5f); return; }
                MoveTo(haulSource.WorkSpot(3), RunSpeed * 0.75f);
                if (FlatDist(transform.position, haulSource.transform.position) < haulSource.Def.footprint + 1.6f)
                {
                    int cap = 4 + Data.WorkLevel(WorkType.Hauling) * 3;
                    haulCount = haulSource.TakeFromBuffer(cap, out haulItem);
                    if (haulCount == 0) SetState(AIState.Idle, 0.5f);
                }
            }
            else
            {
                var dest = bm.NearestStorage(transform.position);
                Vector3 dpos = dest != null ? dest.transform.position : bm.CorePosition;
                float fp = dest != null ? dest.Def.footprint : 1.6f;
                MoveTo(dpos, RunSpeed * 0.75f);
                if (FlatDist(transform.position, dpos) < fp + 1.6f)
                {
                    int left = bm.Storage.Add(haulItem, haulCount);
                    if (left > 0 && haulSource != null) haulSource.ReturnToBuffer(haulItem, left);
                    haulCount = 0;
                    GameEvents.RaiseInventoryChanged();
                    SetState(AIState.Idle, 0.3f);
                }
            }
        }

        public bool IsCarrying => haulCount > 0;
        public string WorkStatus
        {
            get
            {
                switch (State)
                {
                    case AIState.Working: return "Working";
                    case AIState.ToWork: return "Heading to work";
                    case AIState.Haul: return haulCount > 0 ? $"Carrying {haulCount} {ContentDB.ItemName(haulItem)}" : "Hauling";
                    case AIState.ToEat: case AIState.Eating: return "Eating";
                    case AIState.Rest: return "Sleeping";
                    case AIState.Defend: case AIState.Recover: return "Defending the camp!";
                    default: return Data.assignedBuilding == null ? "Idle (unassigned)" : "Idle";
                }
            }
        }
    }
}
