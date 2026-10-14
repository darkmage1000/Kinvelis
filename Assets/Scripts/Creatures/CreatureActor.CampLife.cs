using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Life at camp beyond work: kin run up their little welcome when you come home, nap in the day when there is
    /// nothing to do (and breathe out Zzz while asleep), play tag with each other, and sulk when they are hungry and
    /// there is no food. None of it costs work: a kin with a job it can do always does the job first, so these only
    /// fill time that used to be aimless wandering (a hungry kin that is working just puffs a grey cloud now and then).
    /// </summary>
    public partial class CreatureActor
    {
        enum CampMood { None, Greet, Play, Sulk, Toy }
        /// <summary>Playing at a kin play piece (Housing): where, and whether it has got there yet.</summary>
        Vector3 toyPos; string toyName; bool toyThere;
        public const float ToyCheer = 300f;
        CampMood mood;
        float moodTimer, moodFx, napFx, sulkFx;
        CreatureActor playMate;
        float playCheck;
        bool playChaser;
        float greetedArrival = -99f;
        /// <summary>Face faceTarget whatever the state (greeting).</summary>
        bool faceAnyState;

        public string CampMoodLabel => mood == CampMood.Greet ? "Welcoming you home" : mood == CampMood.Play ? "Playing" : mood == CampMood.Sulk ? "Hungry and sulking" : mood == CampMood.Toy ? "Playing at the " + toyName : null;

        void EndMood()
        {
            mood = CampMood.None;
            playMate = null;
            faceAnyState = false;
        }

        Vector3 HeadPoint => transform.position + Vector3.up * (BodyRadius * 2f + 0.3f);

        /// <summary>Runs before the worker's ordinary thinking. True = the kin is busy being itself this frame.</summary>
        bool TickCampLife(BaseManager bm, float dt)
        {
            // asleep: a slow stream of Zzz
            if (State == AIState.Rest && mood != CampMood.Sulk)
            {
                napFx -= dt;
                if (napFx <= 0f)
                {
                    napFx = 1.2f;
                    Fx.Mote(HeadPoint, new Vector3(Random.Range(-0.15f, 0.15f), 0.55f, Random.Range(-0.15f, 0.15f)), new Color(0.75f, 0.85f, 1f), 0.17f, 1.5f, false);
                }
            }
            // working on an empty stomach: it shows
            if (State == AIState.Working && Data.hunger < 15f)
            {
                sulkFx -= dt;
                if (sulkFx <= 0f) { sulkFx = 2.4f; Fx.Puff(HeadPoint, new Color(0.45f, 0.45f, 0.5f), 0.3f, 0.9f); }
            }

            // you just came home
            if (mood == CampMood.None && bm.PlayerArrival > greetedArrival && Time.time - bm.PlayerArrival < 1.5f)
            {
                greetedArrival = bm.PlayerArrival;
                bool asleepAtNight = State == AIState.Rest && EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight;
                if (!asleepAtNight && haulCount == 0 && FlatDist(transform.position, GameManager.I.Player.Position) < 26f)
                {
                    mood = CampMood.Greet;
                    moodTimer = Random.Range(1.8f, 2.8f);
                    moodFx = Random.Range(0f, 0.35f);
                }
            }

            switch (mood)
            {
                case CampMood.Greet:
                    moodTimer -= dt;
                    faceAnyState = true;
                    faceTarget = GameManager.I.Player.Position;
                    moodFx -= dt;
                    if (moodFx <= 0f)
                    {
                        moodFx = 0.55f;
                        Rig.Squash();
                        Fx.Burst(HeadPoint, new Color(1f, 0.5f, 0.7f), 3, 1.6f, 0.22f, 0.8f);
                    }
                    if (moodTimer <= 0f) EndMood();
                    return true;

                case CampMood.Play:
                    moodTimer -= dt;
                    // work before play: the game stops the moment either of them is wanted somewhere
                    playCheck -= dt;
                    if (playCheck <= 0f)
                    {
                        playCheck = 1f;
                        if (bm.WantsHands(Data, workBuilding, this)) moodTimer = 0f;
                    }
                    if (playMate == null || !playMate.gameObject.activeInHierarchy || playMate.mood != CampMood.Play || moodTimer <= 0f || State == AIState.Defend)
                    {
                        EndMood();
                        if (State != AIState.Defend) SetState(AIState.Idle, Random.Range(0.5f, 1.5f));
                        return false;
                    }
                    if (playChaser)
                    {
                        if (moodFx > 0f) { moodFx -= dt; return true; }   // counting to three before giving chase
                        MoveTo(playMate.transform.position, RunSpeed * 0.9f);
                        if (FlatDist(transform.position, playMate.transform.position) < BodyRadius + playMate.BodyRadius + 0.45f)
                        {
                            // tag! the other one is "it" now
                            Rig.Squash(); playMate.Rig.Squash();
                            Fx.Sparkle((transform.position + playMate.transform.position) * 0.5f + Vector3.up * 0.6f, new Color(1f, 0.92f, 0.45f), 8, 0.5f);
                            playChaser = false;
                            moveTarget = RandomPointNear(bm.CorePosition, 8f);
                            playMate.playChaser = true;
                            playMate.moodFx = 0.7f;
                        }
                    }
                    else
                    {
                        if (FlatDist(transform.position, moveTarget) < 1f) moveTarget = RandomPointNear(bm.CorePosition, 8f);
                        MoveTo(moveTarget, RunSpeed * 0.8f);
                    }
                    return true;

                case CampMood.Toy:
                    moodTimer -= dt;
                    playCheck -= dt;
                    if (playCheck <= 0f) { playCheck = 1f; if (bm.WantsHands(Data, workBuilding, this)) moodTimer = 0f; }
                    if (moodTimer <= 0f || State == AIState.Defend)
                    {
                        if (toyThere && moodTimer <= 0f && State != AIState.Defend)
                        {
                            // done playing: back to work relaxed
                            Data.cheerUntil = Mathf.Max(Data.cheerUntil, Time.time + ToyCheer);
                            Fx.Sparkle(HeadPoint, new Color(0.7f, 1f, 0.8f), 10, 0.6f);
                        }
                        EndMood();
                        if (State != AIState.Defend) SetState(AIState.Idle, Random.Range(0.5f, 1.2f));
                        return false;
                    }
                    if (!toyThere)
                    {
                        MoveTo(toyPos, RunSpeed * 0.85f);
                        if (FlatDist(transform.position, toyPos) < 1.1f) { toyThere = true; moodTimer = Random.Range(6f, 9f); }
                        return true;
                    }
                    // bounce, roll about, sparkle
                    moodFx -= dt;
                    if (moodFx <= 0f)
                    {
                        moodFx = Random.Range(0.5f, 0.9f);
                        Rig.Squash();
                        if (Random.value < 0.4f) Rig.Lunge();
                        Fx.Burst(HeadPoint, new Color(1f, 0.85f, 0.4f), 3, 1.4f, 0.2f, 0.6f);
                        faceTarget = toyPos + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                        faceAnyState = true;
                    }
                    return true;

                case CampMood.Sulk:
                    if (Data.hunger >= 25f || (bm.HasFeedBox && bm.HasFood) || State != AIState.Rest) { EndMood(); return false; }
                    sulkFx -= dt;
                    if (sulkFx <= 0f) { sulkFx = 1.5f; Fx.Puff(HeadPoint, new Color(0.42f, 0.42f, 0.48f), 0.34f, 1f); }
                    return false;   // it is lying down (Rest); the ordinary brain ends the rest
            }
            return false;
        }

        /// <summary>A worker with nothing to do picks something other than wandering. True = it has.</summary>
        bool CampIdleChoice(BaseManager bm)
        {
            if (mood != CampMood.None) return false;
            // nothing to eat: lie down and sulk about it
            if (Data.hunger < 15f && !(bm.HasFeedBox && bm.HasFood))
            {
                mood = CampMood.Sulk;
                sulkFx = 0.3f;
                SetState(AIState.Rest, Random.Range(6f, 10f));
                return true;
            }
            float roll = Random.value;
            // a kin play piece in camp (Housing): go and play, come back relaxed (not while it is still relaxed from last time)
            if (roll < 0.3f && Data.hunger > 25f && Time.time > Data.cheerUntil && Housing.I != null)
            {
                Housing.Piece toy = null; float bd = bm.Radius * 2f;
                foreach (var t in Housing.I.PlayPieces)
                {
                    if (!Housing.I.ReachableFromCamp(t)) continue;
                    float d = FlatDist(transform.position, t.pos) + Random.Range(0f, 6f);
                    if (d < bd) { bd = d; toy = t; }
                }
                if (toy != null)
                {
                    float a = Random.value * Mathf.PI * 2f, r = toy.def.radius > 0f ? toy.def.radius + BodyRadius + 0.2f : Random.Range(0f, 0.6f);
                    toyPos = toy.pos + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    toyName = toy.def.name; toyThere = false;
                    mood = CampMood.Toy; moodTimer = 25f; moodFx = 0.3f; playCheck = 1f;
                    SetState(AIState.Wander, 30f);
                    return true;
                }
            }
            // an afternoon nap
            if (roll < 0.16f)
            {
                SetState(AIState.Rest, Random.Range(7f, 13f));
                return true;
            }
            // a game of tag with whoever else is free
            if (roll < 0.46f && Data.hunger > 30f)
            {
                CreatureActor mate = null; float best = 14f;
                foreach (var a in bm.WorkerActors)
                {
                    if (a == null || a == this || !a.gameObject.activeInHierarchy || a.mood != CampMood.None) continue;
                    if (a.State != AIState.Idle && a.State != AIState.Wander) continue;
                    if (a.Data == null || a.Data.hunger < 30f || bm.WantsHands(a.Data, a.workBuilding, a)) continue;   // it has something to do
                    float d = FlatDist(transform.position, a.transform.position);
                    if (d < best) { best = d; mate = a; }
                }
                if (mate != null)
                {
                    float t = Random.Range(6f, 9f);
                    mood = CampMood.Play; playMate = mate; playChaser = true; moodTimer = t; moodFx = 0.5f;
                    mate.mood = CampMood.Play; mate.playMate = this; mate.playChaser = false; mate.moodTimer = t;
                    mate.moveTarget = RandomPointNear(bm.CorePosition, 8f);
                    SetState(AIState.Wander, t + 1f);
                    mate.SetState(AIState.Wander, t + 1f);
                    return true;
                }
            }
            return false;
        }
    }
}
