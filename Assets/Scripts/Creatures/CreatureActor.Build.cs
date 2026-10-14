using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp kin with the Crafting job put up construction sites (BaseManager.Construction): the nearest site with room
    /// for another builder, with the working animation and a puff of dust now and then. Night sleep, defence, eating
    /// and camp life come first (ThinkWorker runs those before this), and so does a queued order nobody else is
    /// crafting (`BaseManager.CraftFirst`): building never stalls the Workbench queue.
    /// </summary>
    public partial class CreatureActor
    {
        BaseManager.Site buildSite;
        float siteScan, siteFx;
        /// <summary>The construction site this camp kin is building (BaseManager counts builders per site).</summary>
        public BaseManager.Site BuildSite => Mode == CreatureMode.Worker ? buildSite : null;

        bool TickBuildSite(BaseManager bm, float dt)
        {
            if (State == AIState.ToEat || State == AIState.Eating || State == AIState.Haul) return false;
            if (buildSite != null && (buildSite.finished || !bm.Sites.Contains(buildSite))) { buildSite = null; SetState(AIState.Idle, 0.4f); return true; }
            if (bm.Sites.Count == 0 && buildSite == null) return false;
            siteScan -= dt;
            if (siteScan <= 0f)
            {
                siteScan = 1f;
                var site = BaseManager.CanBuild(Data) ? buildSite ?? bm.SiteFor(transform.position, this) : null;
                if (site == null) { if (buildSite != null) { buildSite = null; SetState(AIState.Idle, 0.3f); } return false; }
                // a queued order nobody is crafting comes first: off to that station (or stay at it); the others build
                var station = bm.CraftFirst(Data, this);
                if (station != null)
                {
                    if (buildSite != null) buildSite = null;
                    if (CampTask == null) { task = station; taskCheck = 1f; SetState(AIState.ToWork, 0f); }
                    return false;
                }
                if (buildSite == null) { buildSite = site; task = null; }
            }
            if (buildSite == null) return false;
            var s = buildSite;
            Vector3 away = transform.position - s.pos; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            float reach = s.radius + BodyRadius + 0.7f;
            if (away.magnitude > reach + 0.5f)
            {
                Vector3 spot = s.pos + away.normalized * reach; spot.y = Mathf.Min(transform.position.y, s.pos.y);   // a site a storey up is built from below
                MoveTo(spot, WalkSpeed * 1.5f);
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
