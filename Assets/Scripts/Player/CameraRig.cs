using UnityEngine;

namespace Wildbond
{
    /// <summary>Third-person orbit camera with smoothing, terrain avoidance, pinch/scroll zoom and screen shake.</summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] float distance = 7f;
        [SerializeField] float minDistance = 3.5f;
        [SerializeField] float maxDistance = 13f;
        [SerializeField] float minPitch = -25f;
        [SerializeField] float maxPitch = 70f;
        [SerializeField] float followSharpness = 14f;
        [SerializeField] float heightOffset = 1.6f;

        public Camera Cam { get; private set; }
        public float Yaw { get; private set; }
        float pitch = 18f;
        Vector3 focus;
        float shake;
        float targetDistance;

        public Vector3 Forward { get { var f = Quaternion.Euler(0, Yaw, 0) * Vector3.forward; return f; } }
        public Vector3 Right => Quaternion.Euler(0, Yaw, 0) * Vector3.right;

        public void Init(Camera cam, Vector3 start, float yaw)
        {
            Cam = cam;
            focus = start;
            Yaw = yaw;
            targetDistance = distance;
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        float lookIdle;

        /// <summary>
        /// Lock-on: once the player has left the camera alone for a moment, drift round until the locked kin is within
        /// 30 degrees of the middle of the view. It never fights the look pad - any look input resets the wait.
        /// </summary>
        public void Assist(Vector3 point, float dt)
        {
            if (hasOverride) return;
            if (InputHub.Look.sqrMagnitude > 0.0004f) { lookIdle = 0f; return; }
            lookIdle += dt;
            if (lookIdle < 0.7f) return;
            Vector3 d = point - focus; d.y = 0f;
            if (d.sqrMagnitude < 4f) return;
            float want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float off = Mathf.DeltaAngle(Yaw, want);
            const float slack = 30f;
            if (Mathf.Abs(off) <= slack) return;
            float excess = Mathf.Abs(off) - slack;
            Yaw += Mathf.Sign(off) * Mathf.Min(excess, (40f + excess * 2.5f) * dt);
        }

        float rideHeight, rideDistance, rideHeightTarget, rideDistanceTarget;

        /// <summary>Raise/pull back the camera while riding.</summary>
        public void SetRideOffset(float height, float extraDistance) { rideHeightTarget = height; rideDistanceTarget = extraDistance; }

        bool hasOverride;
        Vector3 ovPos, ovLook;

        /// <summary>Scripted framing (starter selection). Uses unscaled time so it works while paused.</summary>
        public void SetOverride(Vector3 pos, Vector3 lookAt) { hasOverride = true; ovPos = pos; ovLook = lookAt; }

        public void ClearOverride(float yaw)
        {
            hasOverride = false;
            Yaw = yaw;
            pitch = 18f;
        }

        public void Tick(Vector3 target, float dt)
        {
            if (hasOverride)
            {
                float k = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, ovPos, k);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(ovLook - transform.position), k);
                focus = target + Vector3.up * heightOffset;
                return;
            }
            Yaw += InputHub.Look.x;
            pitch = Mathf.Clamp(pitch - InputHub.Look.y, minPitch, maxPitch);
            targetDistance = Mathf.Clamp(targetDistance - InputHub.Zoom * 4f, minDistance, maxDistance);
            distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-10f * dt));

            rideHeight = Mathf.Lerp(rideHeight, rideHeightTarget, 1f - Mathf.Exp(-6f * dt));
            rideDistance = Mathf.Lerp(rideDistance, rideDistanceTarget, 1f - Mathf.Exp(-6f * dt));
            Vector3 goal = target + Vector3.up * (heightOffset + rideHeight * 0.85f);
            focus = Vector3.Lerp(focus, goal, 1f - Mathf.Exp(-followSharpness * dt));

            var rot = Quaternion.Euler(pitch, Yaw, 0);
            Vector3 pos = focus - rot * Vector3.forward * (distance + rideDistance);

            // keep camera above terrain / water (march along the boom)
            float minClear = 0.6f;
            for (int i = 1; i <= 4; i++)
            {
                Vector3 p = Vector3.Lerp(focus, pos, i / 4f);
                float h = Mathf.Max(Ground.Height(p.x, p.z), Tides.Surface(p.x, p.z) - 0.2f) + minClear;
                if (p.y < h) { pos.y += (h - p.y); }
            }

            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - dt * 2.5f);
                pos += Random.insideUnitSphere * shake * 0.25f;
            }
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(focus - pos);
        }

        public void SnapTo(Vector3 target)
        {
            focus = target + Vector3.up * heightOffset;
        }
    }
}
