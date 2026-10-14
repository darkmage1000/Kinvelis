using UnityEngine;

namespace Wildbond
{
    public partial class CreatureActor
    {
        Building workBuilding;   // the structure it is assigned to (its first choice)
        Building task;           // where it is working right now: that one, or wherever it is lending a hand
        float taskCheck;
        /// <summary>The structure this camp kin is at or heading to (BaseManager counts hands per structure).</summary>
        public Building CampTask => Mode == CreatureMode.Worker && (State == AIState.ToWork || State == AIState.Working) ? task : null;
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
            TickBedLift(dt);

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

            if (TickNight(bm, dt)) return;      // in bed, or on the way there
            if (TickCampLife(bm, dt)) return;   // greeting you, or in the middle of a game

            // eat when hungry and food exists
            if (State != AIState.ToEat && State != AIState.Eating && Data.hunger < 30f && bm.HasFeedBox && bm.HasFood)
            {
                SetState(AIState.ToEat, 20f);
            }

            if (TickBuildSite(bm, dt)) return;   // Crafting kin put up construction sites first (CreatureActor.Build)

            switch (State)
            {
                case AIState.ToEat:
                {
                    var feed = bm.FeedBoxFor(transform.position);
                    if (feed == null || stateTimer <= 0f) { SetState(AIState.Idle, 1f); break; }
                    MoveTo(feed.WorkSpot(0), WalkSpeed * 1.2f);
                    if (FlatDist(transform.position, feed.transform.position) < feed.Def.footprint + 1.4f) SetState(AIState.Eating, 2.5f);
                    break;
                }
                case AIState.Eating:
                    faceTarget = bm.FeedBoxFor(transform.position) != null ? bm.FeedBoxFor(transform.position).transform.position : transform.position;
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
                    if (task == null || !task.CanWork(Data)) { task = null; SetState(AIState.Idle, 0.3f); break; }   // someone got there first, or it started growing
                    MoveTo(task.WorkSpot(task == workBuilding ? task.SlotOf(Data.uid) : 2 + (Data.uid.GetHashCode() & 1)), WalkSpeed * 1.5f);
                    if (FlatDist(transform.position, task.transform.position) < task.Def.footprint + 1.6f) SetState(AIState.Working);
                    break;
                case AIState.Working:
                    if (task == null) { SetState(AIState.Idle, 0.5f); break; }
                    faceTarget = task.transform.position;
                    // the step is done (it is growing now, or the batch can't start): off to find the next thing
                    if (!task.CanWork(Data)) { task = null; SetState(AIState.Idle, 0.4f); break; }
                    // now and then: is there somewhere its hands would count for more? (its own structure needs it again; a step it is better at has come up)
                    taskCheck -= dt;
                    if (taskCheck <= 0f)
                    {
                        taskCheck = 1.5f;
                        var better = bm.PickTask(Data, workBuilding, this, task);
                        if (better != null && better != task) { task = better; SetState(AIState.ToWork, 0f); break; }
                    }
                    task.AddWork(bm.WorkRate(Data, task) * dt);
                    bm.AccrueWorkXp(Data, dt);
                    emoteTimer -= dt;
                    if (emoteTimer <= 0f)
                    {
                        emoteTimer = Random.Range(1.2f, 2.2f);
                        bool watering = task.IsProducer && task.Cur.job == WorkType.Irrigating;
                        if (watering) Fx.Burst(task.transform.position + Vector3.up * 1.1f, new Color(0.5f, 0.8f, 1f), 5, 1.6f, 0.16f, 0.6f);
                        else Fx.Debris(task.transform.position + Vector3.up * 0.8f, ElementInfo.Color(Data.Element1), 3);
                    }
                    break;
                case AIState.Rest:
                    if (stateTimer <= 0f) { SetState(AIState.Idle, 0.5f); break; }
                    // a daytime nap ends when there is something to do (a night's sleep, and a sulk, do not)
                    taskCheck -= dt;
                    if (taskCheck <= 0f)
                    {
                        taskCheck = 1f;
                        bool night = EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight;
                        if (!night && CampMoodLabel == null && Data.hunger > 15f && bm.WantsHands(Data, workBuilding, this)) SetState(AIState.Idle, 0f);
                    }
                    break;
                case AIState.Wander:
                    // strolling about, but a step somewhere may have come up (the plot finished growing, ore arrived)
                    taskCheck -= dt;
                    if (taskCheck <= 0f)
                    {
                        taskCheck = 1f;
                        if (bm.WantsHands(Data, workBuilding, this)) { SetState(AIState.Idle, 0f); break; }
                    }
                    MoveTo(moveTarget, WalkSpeed);
                    if (stateTimer <= 0f || FlatDist(transform.position, moveTarget) < 0.8f) SetState(AIState.Idle, Random.Range(1f, 3f));
                    break;
                default: // Idle: decide what to do next
                    if (stateTimer > 0f) break;
                    bool hauler = workBuilding != null && (workBuilding.Def.workType == WorkType.Hauling || workBuilding.Def.isCore) && Data.JobOn(WorkType.Hauling);
                    // a pylon or larder asking for a top-up outranks carrying (a camp's only Charging kin is often its hauler)
                    var call = bm.StoreCalling(Data, this);
                    if (call != null) { task = call; taskCheck = 1f; SetState(AIState.ToWork, 0f); break; }
                    if (hauler && StartHaul(bm)) break;
                    // its own structure if that has a step for it; otherwise wherever its jobs are wanted
                    task = bm.PickTask(Data, workBuilding, this);
                    if (task != null) { taskCheck = 1f; SetState(AIState.ToWork, 0f); break; }
                    // nothing to tend: anyone who can carry clears the output piles
                    if (!hauler && Data.WorkLevel(WorkType.Hauling) > 0 && StartHaul(bm)) break;
                    if (CampIdleChoice(bm)) break;   // a nap, a game of tag, or a sulk
                    moveTarget = RandomPointNear(bm.CorePosition, 7f);
                    SetState(AIState.Wander, Random.Range(3f, 6f));
                    break;
            }
        }

        bool sleeping, bedSet, bedOn, ignoreObstacles;
        Vector3 bedPos;
        float bedFx, bedLift, bedPad;

        /// <summary>A kin with a bed steps up onto the pad as it reaches it (and is not pushed out of the nest), and back down when it gets up.</summary>
        void TickBedLift(float dt)
        {
            float dist = sleeping && bedSet && bedOn ? FlatDist(transform.position, bedPos) : 999f;
            ignoreObstacles = dist < bedPad + 1.2f;
            Rig.Lift = Mathf.MoveTowards(Rig.Lift, dist < bedPad ? bedLift : 0f, dt * 1.6f);
        }

        /// <summary>
        /// Night at camp: every kin that is not nocturnal puts down what it is doing, walks to its bed (a place by a
        /// Kin Nest, or a spot round the hearth) and sleeps until dawn. A raid still wakes it (the defence check runs
        /// first), and one that is starving gets up to eat. True = it is in bed or on its way.
        /// </summary>
        bool TickNight(BaseManager bm, float dt)
        {
            if (!bm.Asleep(Data))
            {
                if (sleeping)
                {
                    sleeping = false; bedSet = false;
                    Rig.Squash();   // a stretch
                    SetState(AIState.Idle, Random.Range(0.2f, 1.6f));
                }
                return false;
            }
            if (State == AIState.ToEat || State == AIState.Eating) return false;                       // let it finish its supper
            if (Data.hunger < 15f && bm.HasFeedBox && bm.HasFood) { if (State == AIState.Rest) SetState(AIState.Idle, 0f); bedSet = false; return false; }
            if (!sleeping)
            {
                sleeping = true; bedSet = false;
                if (haulCount > 0 && haulSource != null) { haulSource.ReturnToBuffer(haulItem, haulCount); haulCount = 0; }
                task = null;
                EndMood();
            }
            if (!bedSet) { bedSet = true; bedOn = bm.BedFor(this, out bedPos, out bedLift, out bedPad); SetState(AIState.Wander, 25f); }
            if (State != AIState.Rest)
            {
                if (State != AIState.Wander) SetState(AIState.Wander, 25f);   // back to bed after a fight or a meal
                MoveTo(bedPos, WalkSpeed * 1.25f);
                bool there = FlatDist(transform.position, bedPos) < (bedOn ? 0.22f : 0.8f) && Mathf.Abs(transform.position.y - bedPos.y) < 1.2f;   // (not under a bed upstairs)
                if (there || stateTimer <= 0f) SetState(AIState.Rest, 99999f);
                return true;
            }
            bedFx -= dt;
            if (bedFx <= 0f)
            {
                bedFx = 1.4f;
                Fx.Mote(transform.position + Vector3.up * (BodyRadius * 2f + 0.3f), new Vector3(Random.Range(-0.15f, 0.15f), 0.5f, Random.Range(-0.15f, 0.15f)), new Color(0.75f, 0.85f, 1f), 0.17f, 1.5f, false);
            }
            return true;
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
                var dest = bm.StorageFor(haulItem, transform.position);   // a box that takes what it carries
                Vector3 dpos = dest != null ? dest.transform.position : bm.CorePosition;
                float fp = dest != null ? dest.Def.footprint : 1.6f;
                MoveTo(dpos, RunSpeed * 0.75f);
                if (FlatDist(transform.position, dpos) < fp + 1.6f)
                {
                    int left = bm.Deposit(dest, haulItem, haulCount);
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
                if (sleeping) return State == AIState.Rest ? "Asleep until dawn" : "Off to bed";
                if (CampMoodLabel != null) return CampMoodLabel;
                if (buildSite != null) return State == AIState.Working ? $"Building the {buildSite.Name}" : $"Off to build the {buildSite.Name}";
                switch (State)
                {
                    case AIState.Working: return task == null ? "Working" : task.IsProducer ? $"{task.Cur.doing} ({task.Def.displayName})" : $"Working at the {task.Def.displayName}";
                    case AIState.ToWork: return task != null ? $"Off to the {task.Def.displayName}" : "Heading to work";
                    case AIState.Haul: return haulCount > 0 ? $"Carrying {haulCount} {ContentDB.ItemName(haulItem)}" : "Hauling";
                    case AIState.ToEat: case AIState.Eating: return "Eating";
                    case AIState.Rest: return "Sleeping";
                    case AIState.Defend: case AIState.Recover: return "Defending the camp!";
                    default: return "Nothing to do just now";
                }
            }
        }
    }
}
