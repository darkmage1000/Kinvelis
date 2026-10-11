using UnityEngine;

namespace Wildbond
{
    public partial class CreatureActor
    {
        const float LeashRange = 48f;

        // flee stamina: 1 = fresh. Running away drains it (5-10 s by Travel quality); at 0 the kin is exhausted
        // and cowers for a few seconds (catch window), then slowly recovers.
        float fleeStamina = 1f, tiredTimer, puffTimer;
        public bool Tired => tiredTimer > 0f;

        void ResetFleeStamina() { fleeStamina = 1f; tiredTimer = 0f; }

        void TickFleeStamina(float dt)
        {
            if (tiredTimer > 0f)
            {
                tiredTimer -= dt;
                puffTimer -= dt;
                if (puffTimer <= 0f) { puffTimer = 0.45f; Fx.Puff(transform.position + Vector3.up * (BodyRadius * 1.8f), new Color(0.9f, 0.93f, 1f, 0.7f), 0.3f, 0.6f); }
                return;
            }
            if (State == AIState.Flee)
            {
                float duration = 5f + 5f * Data.Apt(KinPath.Travel) / 100f;
                fleeStamina -= dt / duration;
                if (fleeStamina <= 0f)
                {
                    fleeStamina = 0f;
                    tiredTimer = 5f;
                    SetState(AIState.Alert, tiredTimer);
                    HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2f + 1f), "TIRED", new Color(0.75f, 0.85f, 1f), 1.1f);
                }
            }
            else fleeStamina = Mathf.Min(1f, fleeStamina + dt / 12f);
        }

        void ThinkWild(float dt)
        {
            var player = GameManager.I.Player;
            float dPlayer = DistToPlayer;
            TickFleeStamina(dt);

            // leash: give up and heal when dragged too far from home (raiders fight to the end)
            if (Aggro && !Raider && !Duelist && FlatDist(transform.position, Home) > LeashRange && State != AIState.Return)
            {
                Aggro = false;
                Target = null;
                SetState(AIState.Return);
            }

            switch (State)
            {
                case AIState.Return:
                    MoveTo(Home, RunSpeed);
                    if (!Boss) Hp = Mathf.Min(MaxHp, Hp + MaxHp * 0.2f * dt);   // a Warden walking back to its ring keeps its wounds
                    if (FlatDist(transform.position, Home) < 3f) SetState(AIState.Idle, 1f);
                    return;

                case AIState.Recover:
                    if (stateTimer <= 0f) SetState(Aggro ? AIState.Chase : AIState.Idle, 0.5f);
                    return;

                case AIState.Flee:
                {
                    Vector3 from = Target != null ? Target.Position : player.Position;
                    if (Tired) { faceTarget = from; SetState(AIState.Alert, tiredTimer); return; }   // too winded to run: cower
                    Vector3 away = transform.position - from; away.y = 0;
                    if (away.sqrMagnitude < 0.01f) away = transform.forward;
                    float winded = fleeStamina < 0.3f ? Mathf.Lerp(0.75f, 1f, fleeStamina / 0.3f) : 1f;   // slows as it tires
                    MoveTo(transform.position + away.normalized * 6f, RunSpeed * 1.05f * winded);
                    if (stateTimer <= 0f)
                    {
                        // cornered or recovered => fight back if aggro'd
                        SetState(Aggro && Hp > MaxHp * 0.25f ? AIState.Chase : AIState.Wander, 2f);
                    }
                    return;
                }
            }

            if (Aggro)
            {
                if (Target == null || !Target.IsAlive)
                {
                    // raiders pick the next camp worker, then the player
                    var w = Raider && BaseManager.I != null ? BaseManager.I.NearestWorkerActor(transform.position) : null;
                    Target = w != null ? (ICombatant)w : (player.IsAlive ? (ICombatant)player : null);
                }
                if (Target == null) { Aggro = false; SetState(AIState.Idle, 1f); return; }
                State = AIState.Chase;
                FightTarget(dt);
                return;
            }

            // ---------------- temperament reactions when calm ----------------
            bool night = EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight;
            // friendly-wild worlds: nothing starts a fight (kin still answer back when hit, and timid ones still bolt)
            switch (WorldRules.Current.friendlyWild && Sp.temperament >= Temperament.Territorial ? Temperament.Docile : Sp.temperament)
            {
                case Temperament.Timid:
                {
                    float scare = player.IsSprinting ? 12f : 7f;
                    if (dPlayer < scare && player.IsAlive && !player.IsCrouching && !Tired) { SetState(AIState.Flee, 2.5f); Target = null; return; }
                    break;
                }
                case Temperament.Curious:
                    if (dPlayer < 14f && dPlayer > 4f && State != AIState.Alert && Random.value < dt * 0.3f)
                    {
                        SetState(AIState.Alert, 3f);
                        return;
                    }
                    break;
                case Temperament.Territorial:
                    if (dPlayer < (Data.alpha ? 11f : 7.5f) && player.IsAlive)
                    {
                        if (State != AIState.Alert) { SetState(AIState.Alert, 1.6f); HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2 + 1f), "!", new Color(1f, 0.7f, 0.2f), 1.4f); return; }
                        if (stateTimer <= 0f) { Aggro = true; Target = player; return; }
                    }
                    break;
                case Temperament.Aggressive:
                    if (dPlayer < (night ? 17f : 13f) + (Data.alpha ? 6f : 0f) && player.IsAlive && !player.IsCrouching)
                    {
                        Aggro = true;
                        Target = player;
                        HUD.DamageNumber(transform.position + Vector3.up * (BodyRadius * 2 + 1f), "!", new Color(1f, 0.3f, 0.2f), 1.4f);
                        return;
                    }
                    break;
            }

            switch (State)
            {
                case AIState.Alert:
                    faceTarget = player.Position;
                    if (Sp.temperament == Temperament.Curious && dPlayer > 4f) MoveTo(player.Position, WalkSpeed * 0.8f);
                    if (stateTimer <= 0f) SetState(AIState.Wander, Random.Range(3f, 6f));
                    break;
                case AIState.Idle:
                    if (stateTimer <= 0f)
                    {
                        moveTarget = RandomPointNear(Home, Data.alpha ? 8f : 14f);
                        SetState(AIState.Wander, Random.Range(4f, 9f));
                    }
                    break;
                case AIState.Wander:
                    MoveTo(moveTarget, WalkSpeed);
                    if (stateTimer <= 0f || FlatDist(transform.position, moveTarget) < 0.8f) SetState(AIState.Idle, Random.Range(1.5f, 5f));
                    break;
                default:
                    SetState(AIState.Idle, 1f);
                    break;
            }
        }
    }
}
