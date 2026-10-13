using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Procedural animation for part-based models: gait, hop, hover, wing flap, tail wag, lunge, hit flash.</summary>
    public class CreatureRig : MonoBehaviour
    {
        const int GroupCount = (int)BodyGroup.Count;

        /// <summary>A human skeleton (torso, elbows, knees): walks with bending knees and elbows, can crouch and tuck.</summary>
        public bool Human => groups[(int)BodyGroup.Torso] != null && groups[(int)BodyGroup.ShinR] != null && groups[(int)BodyGroup.ForearmR] != null;
        /// <summary>0-1: how low a human is holding itself (sneaking). Eased in Animate.</summary>
        public float Crouch;
        /// <summary>0-1: off the ground (knees tuck, arms lift a little).</summary>
        public float Air;
        float crouchNow, airNow;
        float humanHeadPitch, humanHeadYaw;

        public Transform Model { get; private set; }
        public Locomotion Loco { get; private set; }
        readonly Transform[] groups = new Transform[GroupCount];
        readonly Quaternion[] baseRot = new Quaternion[GroupCount];
        readonly Vector3[] basePos = new Vector3[GroupCount];
        readonly List<Renderer> renderers = new List<Renderer>(10);
        static MaterialPropertyBlock mpb;
        static readonly int FlashId = Shader.PropertyToID("_FlashColor");
        static readonly int CoatId = Shader.PropertyToID("_Coat");
        // this individual's look (KinLook): body proportions, coat tint, marking overlay
        Vector3 lookScale = Vector3.one;
        Vector4 coat = new Vector4(0f, 1f, 1f, 0f);
        bool hasCoat;
        GameObject markGo; MeshFilter markMf; MeshRenderer markMr;

        float scale = 1f;
        float phase, t;
        float lunge, flash, squash;
        // lying down asleep: eased 0-1, and what it takes to lie this model down (measured once per model)
        float sleepNow, restDrop, legLen;
        bool restMeasured;
        Color flashColor = Color.white;
        bool flashing;
        /// <summary>Raises the model off the ground (a kin up on its nest). Reset whenever a model is handed out.</summary>
        public float Lift;
        public float HoverHeight => Loco == Locomotion.Flyer ? 0.35f * scale : 0f;
        /// <summary>How the Extra group moves (set from the species after Setup).</summary>
        public ExtraAnim ExtraMode;

        public void Setup(Transform model, Locomotion loco, float s)
        {
            Model = model;
            Loco = loco;
            scale = s;
            renderers.Clear();
            ExtraMode = ExtraAnim.None;
            for (int i = 0; i < GroupCount; i++) groups[i] = null;
            lookScale = Vector3.one; hasCoat = false; markGo = null; markMf = null; markMr = null;
            phase = Random.value * 10f;
            t = Random.value * 10f;
            sleepNow = 0f; restMeasured = false; Lift = 0f;
        }

        public void Register(BodyGroup g, Transform tr, Renderer r)
        {
            groups[(int)g] = tr;
            baseRot[(int)g] = tr.localRotation;
            basePos[(int)g] = tr.localPosition;
            renderers.Add(r);
        }

        public void SetVisible(bool v)
        {
            for (int i = 0; i < renderers.Count; i++) renderers[i].enabled = v;
        }

        /// <summary>
        /// Makes this model one particular kin: its size and build (model scale), head / tail / wing sizes (group
        /// scales), coat tint (a property block the hit flash shares) and marking (an overlay mesh riding on the body
        /// or head, or none). Called whenever a model is given to a kin, including pooled ones being reused.
        /// </summary>
        public void SetLook(KinLook l, Mesh mark, BodyGroup markGroup, Material mat)
        {
            lookScale = l != null ? l.BodyScale : Vector3.one;
            GroupScale(BodyGroup.Head, l != null ? l.head : 1f);
            GroupScale(BodyGroup.Tail, l != null ? l.tail : 1f);
            GroupScale(BodyGroup.WingL, l != null ? l.wing : 1f);
            GroupScale(BodyGroup.WingR, l != null ? l.wing : 1f);
            hasCoat = l != null && !l.PlainCoat;
            coat = hasCoat ? l.Coat : new Vector4(0f, 1f, 1f, 0f);

            var host = mark != null ? groups[(int)markGroup] : null;
            if (host == null) { if (markGo != null) markGo.SetActive(false); }
            else
            {
                if (markGo == null)
                {
                    markGo = new GameObject("Mark");
                    markMf = markGo.AddComponent<MeshFilter>();
                    markMr = markGo.AddComponent<MeshRenderer>();
                    markMr.sharedMaterial = mat;
                    markMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderers.Add(markMr);
                }
                markGo.layer = host.gameObject.layer;
                markGo.transform.SetParent(host, false);
                markGo.transform.localPosition = Vector3.zero;
                markGo.transform.localRotation = Quaternion.identity;
                markGo.transform.localScale = Vector3.one;
                markMf.sharedMesh = mark;
                markGo.SetActive(true);
            }
            flashing = false;
            ApplyBlock(Color.clear, false);
        }

        void GroupScale(BodyGroup g, float s)
        {
            var tr = groups[(int)g];
            if (tr != null) tr.localScale = Vector3.one * s;
        }

        void ApplyBlock(Color flashCol, bool flashOn)
        {
            if (!flashOn && !hasCoat)
            {
                for (int i = 0; i < renderers.Count; i++) if (renderers[i] != null) renderers[i].SetPropertyBlock(null);
                return;
            }
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mpb.Clear();
            if (flashOn) mpb.SetColor(FlashId, flashCol);
            if (hasCoat) mpb.SetVector(CoatId, coat);
            for (int i = 0; i < renderers.Count; i++) if (renderers[i] != null) renderers[i].SetPropertyBlock(mpb);
        }

        public void Flash(Color c, float duration = 0.12f)
        {
            flashColor = c;
            flash = duration;
        }

        public void Lunge() => lunge = 1f;
        public void Squash() => squash = 1f;

        public Transform GetGroup(BodyGroup g) => groups[(int)g];

        /// <summary>Overrides a group's rotation after Animate (e.g. player sword swing).</summary>
        public void OverrideRot(BodyGroup g, Quaternion r) => SetRot(g, r);

        void SetRot(BodyGroup g, Quaternion r)
        {
            var tr = groups[(int)g];
            if (tr != null) tr.localRotation = baseRot[(int)g] * r;
        }

        /// <summary>speed = current horizontal speed (m/s). working = do a work animation.</summary>
        public void Animate(float dt, float speed, bool working, bool sleeping)
        {
            if (Model == null) return;
            t += dt;
            float norm = Mathf.Clamp01(speed / (3.5f * Mathf.Max(0.6f, scale)));
            float freq = 3f + speed * 1.8f / Mathf.Max(0.6f, scale);
            phase += dt * freq;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);

            Vector3 modelPos = Vector3.zero;
            Vector3 modelScale = Vector3.one * scale;
            Quaternion modelRot = Quaternion.identity;

            switch (Loco)
            {
                case Locomotion.Quadruped:
                    SetRot(BodyGroup.LegFL, Quaternion.Euler(s * 38f * norm, 0, 0));
                    SetRot(BodyGroup.LegBR, Quaternion.Euler(s * 38f * norm, 0, 0));
                    SetRot(BodyGroup.LegFR, Quaternion.Euler(-s * 38f * norm, 0, 0));
                    SetRot(BodyGroup.LegBL, Quaternion.Euler(-s * 38f * norm, 0, 0));
                    modelPos.y = Mathf.Abs(c) * 0.05f * scale * norm;
                    break;
                case Locomotion.Biped:
                    if (Human) { AnimateHuman(dt, s, c, norm, working, ref modelPos, ref modelRot); break; }
                    SetRot(BodyGroup.LegFL, Quaternion.Euler(s * 42f * norm, 0, 0));
                    SetRot(BodyGroup.LegFR, Quaternion.Euler(-s * 42f * norm, 0, 0));
                    SetRot(BodyGroup.ArmL, Quaternion.Euler(-s * 35f * norm, 0, 0));
                    SetRot(BodyGroup.ArmR, Quaternion.Euler(s * 35f * norm, 0, 0));
                    modelPos.y = Mathf.Abs(c) * 0.06f * scale * norm;
                    modelRot = Quaternion.Euler(0, 0, s * 4f * norm);
                    break;
                case Locomotion.Hopper:
                {
                    float hop = norm > 0.05f ? Mathf.Abs(Mathf.Sin(phase * 0.6f)) : 0f;
                    modelPos.y = hop * 0.3f * scale;
                    float sq = 1f + (norm > 0.05f ? (0.5f - hop) * 0.25f : Mathf.Sin(t * 2.5f) * 0.04f);
                    modelScale = new Vector3(scale / Mathf.Sqrt(sq), scale * sq, scale / Mathf.Sqrt(sq));
                    break;
                }
                case Locomotion.Slither:
                {
                    // serpentine: body yaw sway, tail counter-sway, slight head lift
                    float sway = Mathf.Sin(phase * 0.8f) * (6f + 16f * norm);
                    modelRot = Quaternion.Euler(0, sway, 0);
                    SetRot(BodyGroup.Tail, Quaternion.Euler(0, -sway * 2.2f, Mathf.Sin(phase * 0.8f + 1f) * 4f));
                    modelPos.y = Mathf.Abs(Mathf.Sin(phase * 1.6f)) * 0.02f * scale;
                    break;
                }
                case Locomotion.Flyer:
                    modelPos.y = HoverHeight + Mathf.Sin(t * 2.2f) * 0.08f * scale;
                    modelRot = Quaternion.Euler(norm * 12f, 0, 0);
                    break;
            }

            // wings (flyers always, birds when running)
            float beat = 1f / Mathf.Max(1f, scale);   // the bigger the kin, the slower and heavier its wingbeat
            float flap = Loco == Locomotion.Flyer ? Mathf.Sin(t * 13f * beat) * 38f : (norm > 0.7f ? Mathf.Sin(t * 16f * beat) * 30f : Mathf.Sin(t * 2f) * 3f);
            SetRot(BodyGroup.WingL, Quaternion.Euler(0, 0, -flap));
            SetRot(BodyGroup.WingR, Quaternion.Euler(0, 0, flap));

            // species effects living in the Extra group (orbiting shards, propellers, floating puffs)
            var ex = groups[(int)BodyGroup.Extra];
            if (ex != null && ExtraMode != ExtraAnim.None)
            {
                Vector3 exBase = basePos[(int)BodyGroup.Extra];
                switch (ExtraMode)
                {
                    case ExtraAnim.Orbit:
                        ex.localRotation = Quaternion.Euler(0, t * 80f, 0);
                        ex.localPosition = exBase + Vector3.up * (Mathf.Sin(t * 1.7f) * 0.03f);
                        break;
                    case ExtraAnim.Spin: ex.localRotation = Quaternion.Euler(0, t * (300f + 600f * norm), 0); break;
                    case ExtraAnim.Bob:
                        ex.localPosition = exBase + Vector3.up * (0.04f + Mathf.Sin(t * 1.6f) * 0.05f);
                        ex.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.9f) * 20f, 0);
                        break;
                    case ExtraAnim.Pulse: ex.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.12f); break;
                }
            }

            // tail wag + head bob
            float wag = Mathf.Sin(t * (norm > 0.1f ? 9f : 3f)) * (norm > 0.1f ? 12f : 18f);
            if (Loco != Locomotion.Slither) SetRot(BodyGroup.Tail, Quaternion.Euler(Mathf.Sin(t * 2f) * 4f, wag, 0));
            float headPitch = norm * s * 5f + Mathf.Sin(t * 1.7f) * 3f;
            float headYaw = Mathf.Sin(t * 0.7f) * 10f * (1f - norm);

            if (working && Human)
            {
                headPitch += 10f + Mathf.Sin(t * 6f) * 6f;   // AnimateHuman has already set the arms and the lean
            }
            else if (working)
            {
                headPitch += 18f + Mathf.Sin(t * 10f) * 14f;
                SetRot(BodyGroup.ArmL, Quaternion.Euler(-40f + Mathf.Sin(t * 10f) * 40f, 0, 0));
                SetRot(BodyGroup.ArmR, Quaternion.Euler(-40f + Mathf.Sin(t * 10f + 1.5f) * 40f, 0, 0));
                modelPos.y += Mathf.Abs(Mathf.Sin(t * 10f)) * 0.04f * scale;
            }
            if (Human) { headPitch += humanHeadPitch; headYaw += humanHeadYaw; }
            // asleep and still: lie down (eased in and out). Drowsy but on its feet, or a person: only the nod.
            sleepNow = Mathf.MoveTowards(sleepNow, sleeping && norm < 0.05f && !Human ? 1f : 0f, dt * 2.2f);
            if (sleepNow > 0.001f) PoseAsleep(sleepNow * sleepNow * (3f - 2f * sleepNow), ref modelPos, ref modelScale, ref modelRot, ref headPitch, ref headYaw, wag);
            else if (sleeping)
            {
                headPitch = 25f;
                modelScale.y *= 0.92f + Mathf.Sin(t * 1.2f) * 0.03f;
            }

            if (lunge > 0f)
            {
                lunge = Mathf.Max(0f, lunge - dt * 4f);
                float k = Mathf.Sin(lunge * Mathf.PI);
                modelPos.z += k * 0.35f * scale;
                headPitch += k * 25f;
            }
            if (squash > 0f)
            {
                squash = Mathf.Max(0f, squash - dt * 5f);
                float k = Mathf.Sin(squash * Mathf.PI) * 0.25f;
                modelScale = new Vector3(modelScale.x * (1f + k), modelScale.y * (1f - k), modelScale.z * (1f + k));
            }
            SetRot(BodyGroup.Head, Quaternion.Euler(headPitch, headYaw, 0));

            // idle breathing
            float breathe = 1f + Mathf.Sin(t * 2.3f) * 0.015f;
            modelPos.y += Lift;
            Model.localPosition = modelPos;
            Model.localRotation = modelRot;
            Model.localScale = new Vector3(modelScale.x * lookScale.x, modelScale.y * breathe * lookScale.y, modelScale.z * lookScale.z);

            UpdateFlash(dt);
        }

        /// <summary>
        /// The sleeping pose, laid over the standing one by k (0-1). Four-legged kin sprawl with the front legs out
        /// ahead and the back legs behind, belly on the ground; two-legged ones sit with their legs out; flyers land
        /// and fold their wings; hoppers flatten; serpents stop swaying. Everyone tucks the head down and to one
        /// side, curls the tail round and breathes slowly. How far a model sinks comes from its own meshes
        /// (`MeasureRest`), so the body ends up resting on the ground whatever its build.
        /// </summary>
        void PoseAsleep(float k, ref Vector3 pos, ref Vector3 scl, ref Quaternion rot, ref float headPitch, ref float headYaw, float wag)
        {
            if (!restMeasured) MeasureRest();
            float unit = scale * lookScale.y;
            // the angle the legs swing out to when the body has sunk by restDrop (hip height above the ground = leg length)
            float fold = legLen > 0.01f ? Mathf.Acos(Mathf.Clamp(1f - restDrop / legLen, 0.09f, 1f)) * Mathf.Rad2Deg * k : 0f;
            switch (Loco)
            {
                case Locomotion.Quadruped:
                    SetRot(BodyGroup.LegFL, Quaternion.Euler(-fold, 0, 0));
                    SetRot(BodyGroup.LegFR, Quaternion.Euler(-fold, 0, 0));
                    SetRot(BodyGroup.LegBL, Quaternion.Euler(fold, 0, 0));
                    SetRot(BodyGroup.LegBR, Quaternion.Euler(fold, 0, 0));
                    pos.y -= restDrop * unit * k;
                    break;
                case Locomotion.Biped:
                    SetRot(BodyGroup.LegFL, Quaternion.Euler(-fold, 0, 0));
                    SetRot(BodyGroup.LegFR, Quaternion.Euler(-fold, 0, 0));
                    SetRot(BodyGroup.ArmL, Quaternion.Euler(14f * k, 0, 0));
                    SetRot(BodyGroup.ArmR, Quaternion.Euler(14f * k, 0, 0));
                    pos.y -= restDrop * unit * k;
                    break;
                case Locomotion.Hopper:
                    scl = new Vector3(scl.x * (1f + 0.14f * k), scl.y * (1f - 0.3f * k), scl.z * (1f + 0.14f * k));
                    break;
                case Locomotion.Slither:
                    rot = Quaternion.Slerp(rot, Quaternion.identity, k);
                    scl.y *= 1f - 0.12f * k;
                    break;
                case Locomotion.Flyer:
                    pos.y = Mathf.Lerp(pos.y, -restDrop * unit, k);
                    rot = Quaternion.Slerp(rot, Quaternion.identity, k);
                    SetRot(BodyGroup.LegFL, Quaternion.Euler(-fold, 0, 0));
                    SetRot(BodyGroup.LegFR, Quaternion.Euler(-fold, 0, 0));
                    SetRot(BodyGroup.LegBL, Quaternion.Euler(fold, 0, 0));
                    SetRot(BodyGroup.LegBR, Quaternion.Euler(fold, 0, 0));
                    break;
            }
            ToRest(BodyGroup.WingL, k);
            ToRest(BodyGroup.WingR, k);
            SetRot(BodyGroup.Tail, Quaternion.Euler(0, wag * (1f - k) + 32f * k, 0));
            headPitch = Mathf.Lerp(headPitch, 22f, k);
            headYaw = Mathf.Lerp(headYaw, 30f, k);
            scl.y *= 1f + Mathf.Sin(t * 1.3f) * 0.035f * k;   // slow breathing
        }

        void ToRest(BodyGroup g, float k)
        {
            var tr = groups[(int)g];
            if (tr != null) tr.localRotation = Quaternion.Slerp(tr.localRotation, baseRot[(int)g], k);
        }

        /// <summary>Reads the model: how long its legs are and how far its body is off the ground.</summary>
        void MeasureRest()
        {
            restMeasured = true; restDrop = 0f; legLen = 0f;
            int n = 0;
            for (int g = (int)BodyGroup.LegFL; g <= (int)BodyGroup.LegBR; g++)
            {
                var tr = groups[g];
                var mf = tr != null ? tr.GetComponent<MeshFilter>() : null;
                if (mf == null || mf.sharedMesh == null) continue;
                legLen += Mathf.Max(0f, -mf.sharedMesh.bounds.min.y);   // a leg hangs from its pivot at the hip
                n++;
            }
            if (n > 0) legLen /= n;
            var body = groups[(int)BodyGroup.Body];
            var bmf = body != null ? body.GetComponent<MeshFilter>() : null;
            float bottom = bmf != null && bmf.sharedMesh != null ? basePos[(int)BodyGroup.Body].y + bmf.sharedMesh.bounds.min.y : 0f;
            restDrop = Mathf.Max(0f, bottom - 0.02f);
            if (legLen > 0.01f) restDrop = Mathf.Min(restDrop, legLen * 0.97f);   // no further than the legs can fold
            else if (Loco != Locomotion.Flyer) restDrop = 0f;                      // no legs: it is already sitting on the ground
        }

        /// <summary>Leg lengths of the human plan (hip to knee, knee to sole): how far the hips sink when the knees bend.</summary>
        const float Thigh = 0.37f, Shin = 0.43f;

        /// <summary>
        /// Walk and run for the human skeleton: thighs swing and each knee folds as its leg comes forward; arms swing
        /// against the legs with the elbows bent (more the faster you go); the torso twists a little against the hips
        /// and leans into a run. Crouch folds the legs under the hips and tips the torso forward; Air tucks the knees.
        /// Everything here is the base pose: the player lays swings, riding and swimming over it afterwards.
        /// </summary>
        void AnimateHuman(float dt, float s, float c, float norm, bool working, ref Vector3 modelPos, ref Quaternion modelRot)
        {
            crouchNow = Mathf.MoveTowards(crouchNow, Mathf.Clamp01(Crouch), dt * 5f);
            airNow = Mathf.MoveTowards(airNow, Mathf.Clamp01(Air), dt * 7f);
            float cr = crouchNow, air = airNow, gait = norm * (1f - air);

            float stride = 40f * gait * (1f - 0.3f * cr);
            float hipBase = -48f * cr - 24f * air, kneeBase = 6f + 92f * cr + 50f * air;
            // a knee folds while its leg is travelling forward (the foot clears the ground), then straightens to land
            float kneeL = kneeBase + 60f * gait * Mathf.Max(0f, -c), kneeR = kneeBase + 60f * gait * Mathf.Max(0f, c);
            SetRot(BodyGroup.LegFL, Quaternion.Euler(hipBase + s * stride, 0, 0));
            SetRot(BodyGroup.LegFR, Quaternion.Euler(hipBase - s * stride, 0, 0));
            SetRot(BodyGroup.ShinL, Quaternion.Euler(kneeL, 0, 0));
            SetRot(BodyGroup.ShinR, Quaternion.Euler(kneeR, 0, 0));
            // hips sink by what the bent legs lose in height
            float hip = (48f * cr + 24f * air) * Mathf.Deg2Rad, shin = (44f * cr + 26f * air) * Mathf.Deg2Rad;
            modelPos.y += (Mathf.Abs(c) * 0.05f * gait - (Thigh * (1f - Mathf.Cos(hip)) + Shin * (1f - Mathf.Cos(shin))) * (1f - air)) * scale;

            if (working)
            {
                // hammering / kneading: both forearms pump from the elbow, the body nods with it
                float w = Mathf.Sin(t * 6f), w2 = Mathf.Sin(t * 6f + 1.4f);
                SetRot(BodyGroup.ArmL, Quaternion.Euler(-38f + w * 14f, 0, -6f));
                SetRot(BodyGroup.ArmR, Quaternion.Euler(-38f + w2 * 14f, 0, 6f));
                SetRot(BodyGroup.ForearmL, Quaternion.Euler(-58f + w * 30f, 0, 0));
                SetRot(BodyGroup.ForearmR, Quaternion.Euler(-58f + w2 * 30f, 0, 0));
                SetRot(BodyGroup.Torso, Quaternion.Euler(14f + w * 4f, 0, 0));
                humanHeadPitch = 0f; humanHeadYaw = 0f;
                return;
            }
            float swing = 30f * gait;
            float elbow = 12f + 50f * norm + 14f * cr;
            SetRot(BodyGroup.ArmL, Quaternion.Euler(-s * swing - 12f * cr, 0, -3f - 22f * air));
            SetRot(BodyGroup.ArmR, Quaternion.Euler(s * swing - 12f * cr, 0, 3f + 22f * air));
            SetRot(BodyGroup.ForearmL, Quaternion.Euler(-(elbow + Mathf.Max(0f, s) * 16f * gait), 0, 0));
            SetRot(BodyGroup.ForearmR, Quaternion.Euler(-(elbow + Mathf.Max(0f, -s) * 16f * gait), 0, 0));
            float twist = s * 7f * gait, lean = 5f * norm + 26f * cr + Mathf.Sin(t * 2.3f) * 0.8f;
            SetRot(BodyGroup.Torso, Quaternion.Euler(lean, twist, 0));
            humanHeadPitch = -18f * cr;   // look ahead, not at the ground
            humanHeadYaw = -twist;
            modelRot = Quaternion.Euler(0, 0, s * 2.5f * gait);
        }

        void UpdateFlash(float dt)
        {
            if (flash > 0f)
            {
                flash -= dt;
                var col = flashColor; col.a = Mathf.Clamp01(flash / 0.12f) * 0.85f;
                ApplyBlock(col, true);
                flashing = true;
            }
            else if (flashing)
            {
                flashing = false;
                ApplyBlock(Color.clear, false);   // back to the coat tint alone (or nothing)
            }
        }
    }
}
