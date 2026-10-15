using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Traversal gear. Gale Glider: press JUMP again in mid-air to glide (slow fall, ~9 m/s forward, stamina drain,
    /// no fall damage). Vine Grapple (redone 2026-10-15 after the playtest: "it needs be targeted and not just a slight
    /// hop forward and the button position is awkward"): `TickHookTarget` keeps looking for something to hook where the
    /// camera points - a tree top within 14 degrees of the centre of the view, or the ground / a ledge the view lands
    /// on that is worth it (above you, or 10 m+ away), 5-34 m off - and marks it with a ring; the HOOK button only shows
    /// while there is one. HOOK reels you straight there at `ReelSpeed`: onto a ledge you land, at a tree you are flung
    /// up and on past it (open the glider for a swing-and-glide). Swimming mounts (Tide / water kin) carry you across
    /// water instead of throwing you off.
    /// </summary>
    public partial class PlayerController
    {
        public const string GliderId = "gale_glider", GrappleId = "vine_grapple";
        const float GlideSpeed = 9f, GlideFall = 2.2f, GlideDrain = 5f;
        const float GrappleRange = 34f, GrappleCost = 10f, ReelSpeed = 26f;

        public bool Gliding { get; private set; }
        float airTime;
        Transform gliderVisual;

        public bool HasGlider => Stats.Bag.Count(GliderId) > 0;
        public bool HasGrapple => Stats.Bag.Count(GrappleId) > 0;
        /// <summary>JUMP would open the glider right now.</summary>
        public bool CanGlideNow => HasGlider && !Gliding && !grounded && !IsSwimming && !IsMounted && airTime > 0.2f && Stats.stamina > 4f;

        /// <summary>The current mount carries you over water.</summary>
        bool MountSwims => mount != null && (mount.Sp.nearWater || mount.Data.Element1 == Element.Tide || mount.Data.Element2 == Element.Tide);

        void ToggleGlide()
        {
            if (Gliding) { SetGliding(false); return; }
            if (!CanGlideNow) return;
            SetGliding(true);
            if (vVel < -GlideFall) vVel = -GlideFall;
            SynthAudio.Play(Sfx.Whoosh, 0.8f, 0.6f);
        }

        void SetGliding(bool on)
        {
            Gliding = on;
            if (on && gliderVisual == null) BuildGliderVisual();
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(on);
        }

        void BuildGliderVisual()
        {
            var mb = new MeshBuilder();
            Color32 leaf = new Color(0.45f, 0.78f, 0.4f), vein = new Color(0.8f, 0.92f, 0.55f), wood = new Color(0.5f, 0.34f, 0.2f);
            for (int s = -1; s <= 1; s += 2)
            {
                mb.Sphere(Matrix4x4.TRS(new Vector3(s * 0.95f, 0f, 0f), Quaternion.Euler(0, 0, s * 8f), new Vector3(1.9f, 0.06f, 0.9f)), leaf, vein, 10, 4);
                mb.Box(Matrix4x4.TRS(new Vector3(s * 0.95f, 0.03f, 0f), Quaternion.identity, new Vector3(1.7f, 0.04f, 0.05f)), vein);
            }
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, -0.45f, 0), Quaternion.identity, new Vector3(0.05f, 0.9f, 0.05f)), wood, 5, 1f, true);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, -0.85f, 0), Quaternion.Euler(0, 0, 90), new Vector3(0.05f, 0.9f, 0.05f)), wood, 5, 1f, true);
            var go = new GameObject("Glider");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 2.6f, -0.1f);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("glider");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.On;
            gliderVisual = go.transform;
        }

        // ---------------------------------------------------------------- grapple
        bool grappling;
        Vector3 grappleFrom, grappleTo;
        float grappleT, grappleDur, grappleCd;
        Transform vine;

        // ---- what the grapple has in its sights
        public bool HasHookTarget { get; private set; }
        Vector3 hookPoint;
        bool hookTree;
        float hookScan;
        Transform hookMark;
        readonly System.Collections.Generic.List<ResourceNode> hookBuf = new System.Collections.Generic.List<ResourceNode>(64);

        void TickHookTarget(float dt)
        {
            if (hookMark != null && HasHookTarget)
            {
                hookMark.position = hookPoint + Vector3.up * 0.1f;
                if (cam != null) hookMark.rotation = Quaternion.LookRotation(hookMark.position - cam.Cam.transform.position);
                hookMark.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.time * 8f));
            }
            hookScan -= dt;
            if (hookScan > 0f) return;
            hookScan = 0.12f;
            bool was = HasHookTarget;
            HasHookTarget = false;
            if (HasGrapple && !grappling && !IsSwimming && !IsMounted && !dead && !Caves.InCave && cam != null && cam.Cam != null)
            {
                var camT = cam.Cam.transform;
                Vector3 o = camT.position, d = camT.forward, me = transform.position;
                float best = float.MaxValue;
                // a tree top close to the middle of the view
                if (ChunkManager.I != null)
                {
                    hookBuf.Clear();
                    ChunkManager.I.Nodes.Query(me, GrappleRange, hookBuf);
                    foreach (var n in hookBuf)
                    {
                        if (!n.alive || !PropCatalog.IsTree(n.type)) continue;
                        Vector3 top = n.pos + Vector3.up * (3.2f * Mathf.Max(0.6f, n.scale));
                        float dist = Vector3.Distance(top, me);
                        if (dist < 5f || dist > GrappleRange) continue;
                        float ang = Vector3.Angle(d, top - o);
                        if (ang > 14f) continue;
                        float score = ang + dist * 0.05f;
                        if (score < best) { best = score; hookPoint = top; hookTree = true; }
                    }
                }
                // or the ground / a ledge where the view lands, if it is above you or a good way off
                for (float t = 1f; t < GrappleRange + 14f; t += 0.5f)
                {
                    var p = o + d * t;
                    float g = Ground.Height(p.x, p.z);
                    if (p.y > g) continue;
                    Vector3 hit = new Vector3(p.x, g, p.z);
                    float dist = Vector3.Distance(hit, me);
                    if (dist >= 5f && dist <= GrappleRange && (hit.y > me.y + 1.5f || dist > 10f))
                    {
                        float score = 3.5f + dist * 0.05f;   // a tree right in the middle of the view still wins
                        if (score < best) { best = score; hookPoint = hit; hookTree = false; }
                    }
                    break;
                }
                HasHookTarget = best < float.MaxValue;
            }
            if (HasHookTarget && hookMark == null) BuildHookMark();
            if (hookMark != null && hookMark.gameObject.activeSelf != HasHookTarget) hookMark.gameObject.SetActive(HasHookTarget);
            if (HasHookTarget && !was) SynthAudio.Play(Sfx.UiClick, 1.6f, 0.15f);
        }

        void BuildHookMark()
        {
            var mb = new MeshBuilder();
            Color32 c = new Color32(150, 255, 120, 0);   // alpha 0: self-lit, seen in the dark
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                mb.Box(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a) * 0.55f, 0f), Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), new Vector3(0.08f, 0.24f, 0.04f)), c);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                mb.Box(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 0.85f, Mathf.Sin(a) * 0.85f, 0f), Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), new Vector3(0.3f, 0.07f, 0.04f)), c);
            }
            var go = new GameObject("HookMark");
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("hook_mark");
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = RenderAssets.Creature; mr.shadowCastingMode = ShadowCastingMode.Off;
            hookMark = go.transform;
        }

        void TryGrapple()
        {
            if (!HasGrapple || grappling || grappleCd > 0f || IsSwimming || IsMounted || dead) return;
            if (!HasHookTarget) { GameEvents.Say("Nothing to hook - look at a tree or a ledge (a ring shows on it)."); return; }
            if (Stats.stamina < GrappleCost) { GameEvents.Warn("Too tired to grapple."); return; }
            Vector3 hit = hookPoint;
            float dist = Vector3.Distance(hit, transform.position);
            Vector3 back = transform.position - hit; back.y = 0f;
            grappleTo = hit + (back.sqrMagnitude > 0.01f ? back.normalized * (hookTree ? 0.9f : 0.6f) : Vector3.zero) + Vector3.up * (hookTree ? -0.6f : 0.15f);
            if (!hookTree) grappleTo.y = Mathf.Max(grappleTo.y, Ground.Height(grappleTo.x, grappleTo.z));
            grappleSwing = hookTree;
            grappleFrom = transform.position;
            grappleT = 0f;
            grappleDur = Mathf.Clamp(dist / ReelSpeed, 0.25f, 1.4f);
            HasHookTarget = false;
            if (hookMark != null) hookMark.gameObject.SetActive(false);
            grappling = true;
            SetGliding(false);
            Stats.stamina -= GrappleCost;
            staminaDelay = 0.8f;
            transform.rotation = Quaternion.LookRotation(new Vector3(-back.x, 0f, -back.z).normalized + Vector3.forward * 0.0001f);
            SynthAudio.Play(Sfx.Throw, 0.8f);
            ShowVine(true);
        }

        bool grappleSwing;

        Vector3 GrappleStep(float dt)
        {
            grappleT += dt / grappleDur;
            float k = Mathf.Clamp01(grappleT);
            float e = k * k * (3f - 2f * k);   // reel in hard, ease at the end
            float lift = grappleSwing ? 0.6f : Mathf.Min(2f, Vector3.Distance(grappleFrom, grappleTo) * 0.06f);
            var p = Vector3.Lerp(grappleFrom, grappleTo, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * lift;
            UpdateVine(p);
            if (k >= 1f)
            {
                grappling = false;
                grappleCd = 0.5f;
                ShowVine(false);
                Fx.Burst(grappleTo + Vector3.up * 0.3f, new Color(0.6f, 0.85f, 0.4f), 10, 3f, 0.3f, 0.4f);
                if (grappleSwing)
                {
                    // flung up and on past the tree: open the glider (JUMP) to keep going
                    Vector3 on = grappleTo - grappleFrom; on.y = 0f;
                    hVel = (on.sqrMagnitude > 0.01f ? on.normalized : transform.forward) * 7f;
                    vVel = 7f;
                    grounded = false;
                    airTime = 0.3f;
                }
            }
            return p;
        }

        void ShowVine(bool on)
        {
            if (vine == null)
            {
                var mb = new MeshBuilder();
                mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0, 0.5f), Quaternion.Euler(90, 0, 0), new Vector3(0.06f, 1f, 0.06f)), new Color(0.35f, 0.6f, 0.25f), 5, 1f, false);
                var go = new GameObject("Vine");
                go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("vine");
                go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
                vine = go.transform;
            }
            vine.gameObject.SetActive(on);
        }

        void UpdateVine(Vector3 playerPos)
        {
            if (vine == null) return;
            Vector3 hand = playerPos + Vector3.up * 1.3f;
            Vector3 to = grappleTo - hand;
            if (to.sqrMagnitude < 0.01f) return;
            vine.position = hand;
            vine.rotation = Quaternion.LookRotation(to);
            vine.localScale = new Vector3(1f, 1f, to.magnitude);
        }
    }
}
