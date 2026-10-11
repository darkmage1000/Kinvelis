using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Traversal gear. Gale Glider: press JUMP again in mid-air to glide (slow fall, ~9 m/s forward, stamina drain,
    /// no fall damage). Vine Grapple: the HOOK button pulls you to the ground point the camera is aiming at (up to 30 m,
    /// cliffs included). Swimming mounts (Tide / water kin) carry you across water instead of throwing you off.
    /// </summary>
    public partial class PlayerController
    {
        public const string GliderId = "gale_glider", GrappleId = "vine_grapple";
        const float GlideSpeed = 9f, GlideFall = 2.2f, GlideDrain = 5f;
        const float GrappleRange = 30f, GrappleCost = 12f;

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

        void TryGrapple()
        {
            if (!HasGrapple || grappling || grappleCd > 0f || IsSwimming || IsMounted || dead) return;
            if (Stats.stamina < GrappleCost) { GameEvents.Warn("Too tired to grapple."); return; }
            // march along the camera's aim until it meets the ground
            var camT = cam.Cam.transform;
            Vector3 o = camT.position, d = camT.forward;
            Vector3 hit = Vector3.zero;
            bool found = false;
            for (float t = 1f; t < GrappleRange + 12f; t += 0.5f)
            {
                var p = o + d * t;
                if (p.y <= Ground.Height(p.x, p.z)) { hit = new Vector3(p.x, Ground.Height(p.x, p.z), p.z); found = true; break; }
            }
            float dist = found ? Vector3.Distance(hit, transform.position) : 0f;
            if (!found || dist > GrappleRange || dist < 3f) { GameEvents.Say(found && dist < 3f ? "Too close to hook." : "Nothing in reach to hook onto - aim at ground or a cliff within 30 m."); return; }
            Vector3 back = transform.position - hit; back.y = 0f;
            grappleTo = hit + (back.sqrMagnitude > 0.01f ? back.normalized * 0.6f : Vector3.zero) + Vector3.up * 0.15f;
            grappleTo.y = Mathf.Max(grappleTo.y, Ground.Height(grappleTo.x, grappleTo.z));
            grappleFrom = transform.position;
            grappleT = 0f;
            grappleDur = Mathf.Clamp(dist / 24f, 0.3f, 1.1f);
            grappling = true;
            SetGliding(false);
            Stats.stamina -= GrappleCost;
            staminaDelay = 0.8f;
            transform.rotation = Quaternion.LookRotation(new Vector3(-back.x, 0f, -back.z).normalized + Vector3.forward * 0.0001f);
            SynthAudio.Play(Sfx.Throw, 0.8f);
            ShowVine(true);
        }

        Vector3 GrappleStep(float dt)
        {
            grappleT += dt / grappleDur;
            float k = Mathf.Clamp01(grappleT);
            float e = 1f - (1f - k) * (1f - k);
            var p = Vector3.Lerp(grappleFrom, grappleTo, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
            UpdateVine(p);
            if (k >= 1f)
            {
                grappling = false;
                grappleCd = 0.8f;
                ShowVine(false);
                Fx.Burst(grappleTo + Vector3.up * 0.3f, new Color(0.6f, 0.85f, 0.4f), 10, 3f, 0.3f, 0.4f);
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
