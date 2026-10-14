using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp kin with the Crafting job put up construction sites (BaseManager.Construction): the nearest site, before
    /// any other camp work, with the working animation and a puff of dust now and then. Night sleep, defence, eating
    /// and camp life come first (ThinkWorker runs those before this).
    /// </summary>
    public partial class CreatureActor
    {
        BaseManager.Site buildSite;
        float siteScan, siteFx;

        bool TickBuildSite(BaseManager bm, float dt)
        {
            if (State == AIState.ToEat || State == AIState.Eating || State == AIState.Haul) return false;
            if (buildSite != null && (buildSite.finished || !bm.Sites.Contains(buildSite))) { buildSite = null; SetState(AIState.Idle, 0.4f); return true; }
            if (buildSite == null)
            {
                if (bm.Sites.Count == 0 || !BaseManager.CanBuild(Data)) return false;
                siteScan -= dt;
                if (siteScan > 0f) return false;
                siteScan = 1f;
                buildSite = bm.SiteFor(transform.position);
                if (buildSite == null) return false;
                task = null;
            }
            var s = buildSite;
            Vector3 away = transform.position - s.pos; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            float reach = s.radius + BodyRadius + 0.7f;
            if (away.magnitude > reach + 0.5f)
            {
                MoveTo(s.pos + away.normalized * reach, WalkSpeed * 1.5f);
                if (State == AIState.Working) SetState(AIState.ToWork);
                return true;
            }
            faceTarget = s.pos;
            if (State != AIState.Working) SetState(AIState.Working);
            siteFx -= dt;
            if (siteFx <= 0f)
            {
                siteFx = Random.Range(0.6f, 1.0f);
                Fx.Puff(s.pos + Vector3.up * 0.5f + Random.insideUnitSphere * 0.4f, new Color(0.85f, 0.75f, 0.55f), 0.22f, 0.6f);
            }
            bm.AddSiteWork(s, BaseManager.BuildRate(Data) * dt);
            return true;
        }
    }
}
