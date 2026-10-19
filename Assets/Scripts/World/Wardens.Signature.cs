using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>The one arena mechanic that belongs to a single Warden (Wardens.Signature.cs).</summary>
    public enum WardenSig { None, Blooms, Tide, SunBeam, Heads, Tentacles, IcePrison, Vortex, Blight, Starfall, Rods }

    /// <summary>
    /// Signatures (2026-10-19, user: "wardens ive only fought 2, they have the same fight moves? summon, giant aoe
    /// circles, a super dash"). Every Warden used the same six shared pieces (guards, pools, marked strikes, shield,
    /// enrage, regen) and most of them dashed. Now each has one mechanic of its own that runs through the whole fight and
    /// comes faster in later phases (`Every`), and only one shared piece per phase (`Prepare`):
    /// Bloomwyrm grows healing blooms you trample; Reefwarden floods the ring and you stand on coral; Solarion sweeps a sun
    /// beam round itself; Hydrabog's three heads strike three lanes in turn; the Kraken's tentacles slam in from the edge;
    /// Aurorox freezes your kin in ice you thaw by standing beside it; Djinnado pulls you into its whirlwind; Venomarch's
    /// blight fills the ring except close to it; Astralisk drops stars in a spiral; Stormcrest raises lightning rods and
    /// the bolts run between them. Damage here is a share of max health (like the strikes), so it is about dodging.
    /// </summary>
    public partial class Wardens
    {
        public static WardenSig SigFor(string id)
        {
            switch (id)
            {
                case "meadow": return WardenSig.Blooms;
                case "isles": return WardenSig.Tide;
                case "badlands": return WardenSig.SunBeam;
                case "swamp": return WardenSig.Heads;
                case "kraken": return WardenSig.Tentacles;
                case "peaks": return WardenSig.IcePrison;
                case "desert": return WardenSig.Vortex;
                case "blight": return WardenSig.Blight;
                case "glow": return WardenSig.Starfall;
                case "stormcrest": return WardenSig.Rods;
            }
            return WardenSig.None;
        }

        public static string SignatureName(Warden w)
        {
            switch (SigFor(w.id))
            {
                case WardenSig.Blooms: return "Healing Blooms";
                case WardenSig.Tide: return "Rising Tide";
                case WardenSig.SunBeam: return "Sun Beam";
                case WardenSig.Heads: return "Three Heads";
                case WardenSig.Tentacles: return "Tentacles";
                case WardenSig.IcePrison: return "Ice Prison";
                case WardenSig.Vortex: return "Whirlwind";
                case WardenSig.Blight: return "Closing Blight";
                case WardenSig.Starfall: return "Starfall Spiral";
                case WardenSig.Rods: return "Lightning Rods";
            }
            return "";
        }

        /// <summary>What its signature does and what to do about it (Journey page, wiki, the first time it happens).</summary>
        public static string SignatureText(Warden w)
        {
            switch (SigFor(w.id))
            {
                case WardenSig.Blooms: return "Flowers grow round the ring and heal it while they stand - run through them to trample them.";
                case WardenSig.Tide: return "Coral islands rise, then the tide floods the ring - be standing on the coral when the water comes.";
                case WardenSig.SunBeam: return "A line marks where its sun beam starts, then the beam sweeps round it - keep running ahead of it, or get behind it.";
                case WardenSig.Heads: return "Its three heads strike three lanes one after another - watch the order and step into a lane that has already struck.";
                case WardenSig.Tentacles: return "Tentacles rise at the edge of the rock and slam across it - get out of the marked lanes.";
                case WardenSig.IcePrison: return "It freezes your kin in ice - stand beside the ice to thaw it before it shatters and hurts your kin.";
                case WardenSig.Vortex: return "It spins into a whirlwind that drags you in and burns at the centre - run (or RUN) against the wind.";
                case WardenSig.Blight: return "The blight fills the ring - everything far from it is poisoned, so get close to it until the fog clears.";
                case WardenSig.Starfall: return "Stars fall in a spiral out from it, one after another - keep moving round and out of the trail.";
                case WardenSig.Rods: return "Lightning rods rise in a square and bolts run between them - stand inside or outside the square, never on a line.";
            }
            return "";
        }

        // ---- state for the running fight ----
        WardenSig sig;
        float sigTimer, sigClock, sigTick;
        int sigStage;
        bool sigExplained;
        float sigAngle, sigSpin;
        Vector3 sigDir;
        readonly List<Transform> sigProps = new List<Transform>();
        readonly List<float> sigPropAge = new List<float>();
        Transform sigBig;        // water sheet / beam / fog
        Mesh bloomMesh, islandMesh, beamMesh, laneMesh, tentacleMesh, iceMesh, fogMesh, rodMesh, waterMesh, safeMesh, starMesh;
        float thaw;
        CreatureActor frozen;

        float Every(float baseSeconds) => baseSeconds * (phase <= 1 ? 1.15f : phase == 2 ? 1f : 0.75f);

        void SigStart(Warden w)
        {
            sig = SigFor(w.id);
            sigTimer = 7f; sigStage = 0; sigClock = 0f; sigExplained = false; thaw = 0f; frozen = null;
            MakeSigMeshes(w);
        }

        void SigEnd()
        {
            foreach (var t in sigProps) if (t != null) Destroy(t.gameObject);
            sigProps.Clear(); sigPropAge.Clear();
            if (sigBig != null) Destroy(sigBig.gameObject);
            sigBig = null;
            if (frozen != null) frozen.ClearStun();
            frozen = null;
            foreach (var m in new[] { bloomMesh, islandMesh, beamMesh, laneMesh, tentacleMesh, iceMesh, fogMesh, rodMesh, waterMesh, safeMesh, starMesh }) if (m != null) Destroy(m);
            bloomMesh = islandMesh = beamMesh = laneMesh = tentacleMesh = iceMesh = fogMesh = rodMesh = waterMesh = safeMesh = starMesh = null;
            sig = WardenSig.None;
        }

        void Explain(Warden w)
        {
            if (sigExplained) return;
            sigExplained = true;
            GameEvents.Warn($"{w.Name}: {SignatureName(w)}! {SignatureText(w)}");
        }

        /// <summary>The HUD boss bar's extra word while the signature needs something from you.</summary>
        string SigState()
        {
            switch (sig)
            {
                case WardenSig.Blooms: return sigProps.Count > 0 ? UIFactory.Col("  HEALING - trample the blooms", new Color(1f, 0.7f, 0.9f)) : null;
                case WardenSig.Tide: return sigStage == 1 ? UIFactory.Col("  TIDE RISING - get on the coral", new Color(0.5f, 0.85f, 1f)) : sigStage == 2 ? UIFactory.Col("  FLOOD", new Color(0.5f, 0.85f, 1f)) : null;
                case WardenSig.IcePrison: return frozen != null ? UIFactory.Col($"  THAW {frozen.Data.DisplayName} - stand by it {Mathf.RoundToInt(thaw / ThawTime * 100f)}%", new Color(0.7f, 0.95f, 1f)) : null;
                case WardenSig.Vortex: return sigStage == 2 ? UIFactory.Col("  WHIRLWIND - run against it", new Color(1f, 0.85f, 0.5f)) : null;
                case WardenSig.Blight: return sigStage >= 1 ? UIFactory.Col("  BLIGHT - get close to it", new Color(0.75f, 1f, 0.4f)) : null;
            }
            return null;
        }

        // ============================================================ TICK
        void TickSignature(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            if (sig == WardenSig.None || boss == null) return;
            sigTimer -= dt;
            switch (sig)
            {
                case WardenSig.Blooms: TickBlooms(dt, w, pl); break;
                case WardenSig.Tide: TickTide(dt, w, pl, comp, compUp); break;
                case WardenSig.SunBeam: TickBeam(dt, w, pl, comp, compUp); break;
                case WardenSig.Heads: TickHeads(w, pl); break;
                case WardenSig.Tentacles: TickTentacles(dt, w, pl); break;
                case WardenSig.IcePrison: TickIce(dt, w, pl, comp, compUp); break;
                case WardenSig.Vortex: TickVortex(dt, w, pl, comp, compUp); break;
                case WardenSig.Blight: TickBlight(dt, w, pl, comp, compUp); break;
                case WardenSig.Starfall: TickStarfall(w, pl); break;
                case WardenSig.Rods: TickRods(dt, w, pl); break;
            }
        }

        bool Ready => sigTimer <= 0f && !returning;

        Vector3 OnGround(Vector3 p, float lift = 0.06f) { p.y = Ground.Height(p.x, p.z) + lift; return p; }

        Vector3 RingSpot(Warden w, float min, float max)
        {
            float a = Random.value * Mathf.PI * 2f, d = Random.Range(min, max);
            return OnGround(w.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * d);
        }

        Transform Prop(Mesh m, Vector3 at, Material mat = null)
        {
            var go = new GameObject("WardenSig");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat != null ? mat : RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            sigProps.Add(go.transform);
            sigPropAge.Add(0f);
            return go.transform;
        }

        void DropProp(int i)
        {
            if (sigProps[i] != null) Destroy(sigProps[i].gameObject);
            sigProps.RemoveAt(i); sigPropAge.RemoveAt(i);
        }

        void ClearProps() { for (int i = sigProps.Count - 1; i >= 0; i--) DropProp(i); }

        /// <summary>Hurts you and your kin by a share of max health, when the test says they are in it.</summary>
        void SigHit(PlayerController pl, CreatureActor comp, bool compUp, System.Func<Vector3, bool> inside, float pf, float kf, StatusType st, float stChance, Vector3 knockFrom, float knock)
        {
            var el = Active.Sp != null ? Active.Sp.element1 : Element.Normal;
            if (pl.IsAlive && inside(pl.Position))
            {
                Vector3 d = pl.Position - knockFrom; d.y = 0;
                pl.ReceiveHit(new HitInfo { attacker = boss, damage = pl.Stats.MaxHp * pf, element = el, status = st, statusChance = stChance,
                    knockback = knock > 0f ? (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * knock : Vector3.zero, effectiveness = 1f });
            }
            if (compUp && inside(comp.Position))
                comp.ReceiveHit(new HitInfo { attacker = boss, damage = comp.MaxHp * kf, element = el, status = st, statusChance = stChance, effectiveness = 1f });
        }

        static bool InLane(Vector3 p, Vector3 start, Vector3 dir, float len, float halfW)
        {
            Vector3 v = p - start; v.y = 0;
            float along = Vector3.Dot(v, dir);
            if (along < 0f || along > len) return false;
            return (v - dir * along).magnitude <= halfW;
        }

        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        // ============================================================ 1 BLOOMWYRM - healing blooms
        const float BloomHeal = 0.004f;   // of its max health per second, per bloom
        const float BloomSize = 2f;       // a flower you can spot from across the ring

        void TickBlooms(float dt, Warden w, PlayerController pl)
        {
            if (Ready && sigProps.Count < 4)
            {
                sigTimer = Every(15f);
                Explain(w);
                int n = phase >= 3 ? 3 : 2 + (phase >= 2 ? 1 : 0);
                for (int i = 0; i < n && sigProps.Count < 5; i++)
                {
                    var t = Prop(bloomMesh, RingSpot(w, 7f, ArenaRadius - 3f));
                    t.localScale = Vector3.one * 0.05f;
                    Fx.Sparkle(t.position + Vector3.up * 0.5f, new Color(1f, 0.7f, 0.9f), 12, 0.8f);
                }
                SynthAudio.Play(Sfx.Summon, 1.3f, 0.6f);
            }
            sigTick -= dt;
            bool pulse = sigTick <= 0f;
            if (pulse) sigTick = 0.35f;
            for (int i = sigProps.Count - 1; i >= 0; i--)
            {
                var t = sigProps[i];
                sigPropAge[i] += dt;
                float grow = Mathf.Clamp01(sigPropAge[i] / 1.2f);
                t.localScale = Vector3.one * (BloomSize * Mathf.Max(0.05f, grow) * (1f + Mathf.Sin(Time.time * 3f + i) * 0.05f));
                t.Rotate(0f, 20f * dt, 0f);
                if (Flat(pl.Position, t.position) < 1.9f && grow > 0.3f)
                {
                    Fx.Burst(t.position + Vector3.up * 0.6f, new Color(1f, 0.6f, 0.85f), 24, 4f, 0.25f, 0.6f, true);
                    Fx.Burst(t.position + Vector3.up * 0.3f, new Color(0.4f, 0.8f, 0.3f), 10, 3f, 0.2f, 0.5f);
                    SynthAudio.Play(Sfx.Pluck, 1.1f, 0.8f);
                    DropProp(i);
                    continue;
                }
                if (grow >= 1f)
                {
                    boss.BossHeal(boss.MaxHp * BloomHeal * dt);
                    if (pulse)
                    {
                        Vector3 from = t.position + Vector3.up * 1f, to = boss.Position + Vector3.up * boss.Radius;
                        Fx.Mote(from, (to - from) / 0.6f, new Color(1f, 0.75f, 0.9f, 0.9f), 0.2f, 0.6f, true);
                    }
                }
            }
        }

        // ============================================================ 2 REEFWARDEN - rising tide
        const float IslandR = 2.6f, IslandTop = 0.7f, TideWarn = 3.5f, TideFlood = 4.5f;
        float sigLevel;

        void TickTide(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            if (sigStage == 0 && Ready)
            {
                Explain(w);
                sigStage = 1; sigClock = 0f;
                // knee-deep over the ring's average ground; anything standing higher than the water is dry (and safe)
                float sum = Ground.Height(w.pos.x, w.pos.z); int cnt = 1;
                for (int i = 0; i < 16; i++)
                    for (float r = 6f; r <= ArenaRadius; r += 8f)
                    {
                        float a = i / 16f * Mathf.PI * 2f;
                        sum += Ground.Height(w.pos.x + Mathf.Sin(a) * r, w.pos.z + Mathf.Cos(a) * r); cnt++;
                    }
                sigLevel = sum / cnt + 0.5f;
                float a0 = Random.value * 360f;
                for (int i = 0; i < 3; i++)
                {
                    float a = (a0 + i * 120f + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                    var t = Prop(islandMesh, OnGround(w.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * Random.Range(7f, 14f), 0.04f));
                    t.localScale = new Vector3(1f, 0.05f, 1f);
                }
                GameEvents.Warn("The tide is coming in - get onto the coral!");
                SynthAudio.Play(Sfx.Whoosh, 0.5f, 0.8f);
            }
            if (sigStage == 0) return;
            sigClock += dt;
            float rise = sigStage == 2 ? 1f : Mathf.Clamp01(sigClock / 0.8f);
            foreach (var t in sigProps)
            {
                // each island stands a little clear of the flood, whatever the ground under it
                float tall = Mathf.Clamp((sigLevel + 0.25f - t.position.y) / IslandTop, 1f, 1.8f);
                t.localScale = new Vector3(1f, Mathf.Max(0.05f, rise) * tall, 1f) * (sigStage == 1 ? 1f + Mathf.Sin(Time.time * 8f) * 0.04f : 1f);
            }
            if (sigStage == 1 && sigClock >= TideWarn)
            {
                sigStage = 2; sigClock = 0f;
                sigBig = new GameObject("Flood").transform;
                sigBig.SetParent(transform, false);
                sigBig.position = OnGround(w.pos, 0.1f);
                sigBig.gameObject.AddComponent<MeshFilter>().sharedMesh = waterMesh;
                var mr = sigBig.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = RenderAssets.Water;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                SynthAudio.Play(Sfx.Burst, 0.6f, 0.9f);
            }
            if (sigStage != 2) return;
            // the sheet of water rises over the first second and drains over the last
            float level = Mathf.Min(Mathf.Clamp01(sigClock / 0.9f), Mathf.Clamp01((TideFlood - sigClock) / 0.8f));
            sigBig.localScale = new Vector3(ArenaRadius + 3f, 1f, ArenaRadius + 3f);
            float floor = Ground.Height(w.pos.x, w.pos.z) - 0.5f;
            Vector3 sheet = w.pos; sheet.y = Mathf.Lerp(floor, sigLevel, level);
            sigBig.position = sheet;
            sigTick -= dt;
            if (sigTick <= 0f && sigClock > 0.6f && sigClock < TideFlood - 0.5f)
            {
                sigTick = 0.5f;
                System.Func<Vector3, bool> wet = p =>
                {
                    if (Flat(p, w.pos) > ArenaRadius + 2f) return false;
                    foreach (var t in sigProps) if (Flat(p, t.position) < IslandR) return false;
                    return p.y < sheet.y - 0.05f;   // feet under the water (higher ground stays dry)
                };
                SigHit(pl, comp, compUp, wet, 0.05f, 0.06f, StatusType.Soaked, 0.5f, w.pos, 0f);
                for (int i = 0; i < 6; i++) { Vector2 r = Random.insideUnitCircle * ArenaRadius; Fx.Puff(OnGround(w.pos + new Vector3(r.x, 0, r.y), 0.5f), new Color(0.8f, 0.95f, 1f, 0.6f), 0.4f, 0.5f); }
            }
            if (sigClock >= TideFlood)
            {
                Destroy(sigBig.gameObject); sigBig = null;
                ClearProps();
                sigStage = 0; sigTimer = Every(19f);
            }
        }

        // ============================================================ 3 SOLARION - sun beam
        const float BeamLen = 24f, BeamHalf = 0.95f, BeamAim = 1.3f, BeamSweep = 5f;

        void TickBeam(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            Vector3 origin = boss.Position;
            if (sigStage == 0 && Ready)
            {
                Explain(w);
                sigStage = 1; sigClock = 0f;
                Vector3 d = pl.Position - origin; d.y = 0;
                sigAngle = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                sigSpin = (Random.value < 0.5f ? -1f : 1f) * (phase >= 3 ? 52f : 42f);
                sigBig = Prop(laneMesh, origin).transform;
                sigProps.Remove(sigBig); sigPropAge.RemoveAt(sigPropAge.Count - 1);
                SynthAudio.Play(Sfx.Whoosh, 0.6f, 0.7f);
            }
            if (sigStage == 0) return;
            sigClock += dt;
            if (sigStage == 2) sigAngle += sigSpin * dt;
            Vector3 dir = Quaternion.Euler(0, sigAngle, 0) * Vector3.forward;
            sigBig.position = OnGround(origin + dir * (BeamLen * 0.5f), sigStage == 2 ? 0.7f : 0.08f);
            sigBig.rotation = Quaternion.LookRotation(dir);
            sigBig.localScale = sigStage == 2 ? new Vector3(BeamHalf * 2f, 1.1f, BeamLen) : new Vector3(BeamHalf * 2f, 0.05f, BeamLen) * (1f + Mathf.Sin(Time.time * 18f) * 0.03f);
            if (sigStage == 1 && sigClock >= BeamAim)
            {
                sigStage = 2; sigClock = 0f;
                sigBig.GetComponent<MeshFilter>().sharedMesh = beamMesh;
                SynthAudio.Play(Sfx.Burst, 1.2f, 0.9f);
            }
            if (sigStage != 2) return;
            sigTick -= dt;
            if (Random.value < 0.6f) Fx.Mote(origin + dir * Random.Range(2f, BeamLen) + Vector3.up * 0.8f, Vector3.up * 1.5f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.3f, 0.5f, true);
            if (sigTick <= 0f)
            {
                sigTick = 0.3f;
                SigHit(pl, comp, compUp, p => InLane(p, origin, dir, BeamLen, BeamHalf + 0.35f), 0.08f, 0.09f, StatusType.Burn, 0.5f, origin, 3f);
            }
            if (sigClock >= BeamSweep)
            {
                Destroy(sigBig.gameObject); sigBig = null;
                sigStage = 0; sigTimer = Every(14f);
            }
        }

        // ============================================================ 4 HYDRABOG - three heads
        void TickHeads(Warden w, PlayerController pl)
        {
            if (!Ready) return;
            Explain(w);
            sigTimer = Every(9f);
            Vector3 d = pl.Position - boss.Position; d.y = 0;
            float a0 = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            bool leftFirst = Random.value < 0.5f;
            for (int i = 0; i < 3; i++)
            {
                int order = leftFirst ? i : 2 - i;
                Vector3 dir = Quaternion.Euler(0, a0 + (i - 1) * 38f, 0) * Vector3.forward;
                AddLane(boss.Position + dir * boss.Radius * 0.5f, dir, 17f, 3.4f, 1.0f + order * 0.5f, 0.13f, 0.12f, StatusType.Poison, 0.5f, 4f);
            }
            SynthAudio.Play(Sfx.Whoosh, 0.8f, 0.7f);
        }

        // ============================================================ 5 KRAKEN - tentacles from the rim
        void TickTentacles(float dt, Warden w, PlayerController pl)
        {
            if (Ready)
            {
                Explain(w);
                sigTimer = Every(9f);
                int n = phase >= 3 ? 3 : 2;
                float a0 = Mathf.Atan2(pl.Position.x - w.pos.x, pl.Position.z - w.pos.z) * Mathf.Rad2Deg + Random.Range(-40f, 40f);
                for (int i = 0; i < n; i++)
                {
                    float a = (a0 + i * (360f / n)) * Mathf.Deg2Rad;
                    Vector3 rim = w.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (ArenaRadius - 2f);
                    Vector3 dir = (w.pos - rim); dir.y = 0; dir.Normalize();
                    AddLane(rim, dir, (ArenaRadius - 2f) * 1.7f, 2.8f, 1.9f, 0.15f, 0.13f, StatusType.Soaked, 0.35f, 7f);
                    var t = Prop(tentacleMesh, OnGround(rim, -0.2f));
                    t.rotation = Quaternion.LookRotation(dir);
                    t.localScale = new Vector3(1f, 0.05f, 1f);
                    Fx.Burst(t.position + Vector3.up * 0.3f, new Color(0.6f, 0.75f, 0.9f), 14, 4f, 0.3f, 0.5f);
                }
                SynthAudio.Play(Sfx.Whoosh, 0.5f, 0.8f);
            }
            // a tentacle rises while its lane is marked, slams down along it, then sinks
            for (int i = sigProps.Count - 1; i >= 0; i--)
            {
                var t = sigProps[i];
                float age = sigPropAge[i] += dt;
                Vector3 flat = t.forward; flat.y = 0;
                flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
                if (age < 1.9f)
                {
                    // rises, leaning back a little before the slam
                    t.rotation = Quaternion.LookRotation(flat) * Quaternion.Euler(-Mathf.Lerp(0f, 10f, age / 1.9f), 0, 0);
                    t.localScale = new Vector3(1f, Mathf.Max(0.05f, Mathf.Clamp01(age / 1.2f)), 1f);
                    continue;
                }
                float slam = Mathf.Clamp01((age - 1.9f) / 0.18f);
                t.rotation = Quaternion.LookRotation(flat) * Quaternion.Euler(Mathf.Lerp(-10f, 84f, slam), 0, 0);
                if (age > 2.6f) t.localScale = new Vector3(1f, Mathf.Max(0.05f, 1f - (age - 2.6f) / 0.6f), 1f);
                if (age > 3.2f) DropProp(i);
            }
        }

        // ============================================================ 6 AURUROX - ice prison
        const float ThawTime = 2.2f, IceShatter = 8f;

        void TickIce(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            if (frozen == null && Ready)
            {
                sigTimer = Every(17f);
                Explain(w);
                if (compUp)
                {
                    frozen = comp; thaw = 0f; sigClock = 0f;
                    comp.Stun(IceShatter + 1f);
                    var t = Prop(iceMesh, comp.Position);
                    t.localScale = Vector3.one * Mathf.Max(1f, comp.Radius * 1.5f);
                    Fx.Burst(comp.Position + Vector3.up, new Color(0.8f, 0.95f, 1f), 30, 5f, 0.3f, 0.6f, true);
                    SynthAudio.Play(Sfx.Clunk, 1.4f, 0.9f);
                    GameEvents.Warn($"{w.Name} freezes {comp.Data.DisplayName} in ice - stand beside it to thaw it!");
                }
                else
                {
                    // no kin out: a ring of ice spikes round you instead
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        AddZone(pl.Position + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 3.2f, 1.8f, 1.4f, false);
                    }
                    AddZone(pl.Position, 1.8f, 2.2f, false);
                }
            }
            if (frozen == null) return;
            if (!frozen.IsAlive || frozen != comp) { ClearProps(); frozen = null; return; }
            sigClock += dt;
            if (sigProps.Count > 0) sigProps[0].position = frozen.Position;
            if (Flat(pl.Position, frozen.Position) < frozen.Radius + 2.6f)
            {
                thaw += dt;
                if (Random.value < 0.4f) Fx.Mote(frozen.Position + Vector3.up * Random.Range(0.3f, 1.5f) + Random.insideUnitSphere * 0.6f, Vector3.down * 0.8f, new Color(0.7f, 0.9f, 1f, 0.9f), 0.12f, 0.6f, true);
            }
            if (thaw >= ThawTime)
            {
                Fx.Burst(frozen.Position + Vector3.up, new Color(0.85f, 0.97f, 1f), 26, 4f, 0.25f, 0.5f, true);
                SynthAudio.Play(Sfx.Mine, 1.4f, 0.7f);
                GameEvents.Good($"{frozen.Data.DisplayName} is free!");
                frozen.ClearStun();
                ClearProps(); frozen = null;
            }
            else if (sigClock >= IceShatter)
            {
                Fx.Burst(frozen.Position + Vector3.up, new Color(0.7f, 0.9f, 1f), 40, 7f, 0.35f, 0.6f, true);
                SynthAudio.Play(Sfx.HitHeavy, 1.2f, 0.9f);
                frozen.ClearStun();
                frozen.ReceiveHit(new HitInfo { attacker = boss, damage = frozen.MaxHp * 0.25f, element = Element.Frost, status = StatusType.Chill, statusChance = 1f, effectiveness = 1f });
                GameEvents.Warn("The ice shatters!");
                ClearProps(); frozen = null;
            }
        }

        // ============================================================ 7 DJINNADO - whirlwind
        const float VortexWarn = 1.5f, VortexTime = 6f, VortexCore = 4.2f, VortexPull = 3.4f;

        void TickVortex(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            if (sigStage == 0 && Ready)
            {
                Explain(w);
                sigStage = 1; sigClock = 0f;
                sigBig = Prop(laneMesh, OnGround(boss.Position)).transform;
                sigProps.Remove(sigBig); sigPropAge.RemoveAt(sigPropAge.Count - 1);
                sigBig.GetComponent<MeshFilter>().sharedMesh = warnMesh;
                SynthAudio.Play(Sfx.Whoosh, 0.4f, 0.9f);
            }
            if (sigStage == 0) return;
            sigClock += dt;
            Vector3 c = boss.Position;
            sigBig.position = OnGround(c);
            sigBig.localScale = new Vector3(VortexCore * 2f, 1f, VortexCore * 2f);
            if (sigStage == 1 && sigClock >= VortexWarn) { sigStage = 2; sigClock = 0f; }
            if (sigStage != 2) return;
            // dust spiralling in
            for (int i = 0; i < 3; i++)
            {
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(3f, ArenaRadius);
                Vector3 p = OnGround(c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r, 0.5f + Random.value * 2f);
                Vector3 inward = (c - p); inward.y = 0; inward.Normalize();
                Vector3 tangent = Vector3.Cross(Vector3.up, inward);
                Fx.Mote(p, inward * 5f + tangent * 8f + Vector3.up * 0.6f, new Color(0.95f, 0.8f, 0.55f, 0.75f), 0.3f, 0.7f, false);
            }
            // the pull: you can walk out slowly and run out easily
            Vector3 toC = c - pl.Position; toC.y = 0;
            if (toC.magnitude > 0.8f && toC.magnitude < ArenaRadius + 8f) pl.Drift = toC.normalized * VortexPull;
            if (compUp)
            {
                Vector3 kc = c - comp.Position; kc.y = 0;
                if (kc.magnitude > comp.Radius + boss.Radius + 0.6f) comp.transform.position += kc.normalized * VortexPull * 0.7f * dt;
            }
            sigTick -= dt;
            if (sigTick <= 0f)
            {
                sigTick = 0.5f;
                SigHit(pl, comp, compUp, p => Flat(p, c) < VortexCore, 0.07f, 0.07f, StatusType.Burn, 0.4f, c, 6f);
            }
            if (sigClock >= VortexTime)
            {
                Destroy(sigBig.gameObject); sigBig = null;
                sigStage = 0; sigTimer = Every(16f);
            }
        }

        // ============================================================ 8 VENOMARCH - closing blight
        const float BlightSafe = 7.5f, BlightWarn = 2.2f, BlightTime = 6.5f;

        void TickBlight(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            if (sigStage == 0 && Ready)
            {
                Explain(w);
                sigStage = 1; sigClock = 0f;
                var safe = Prop(safeMesh, OnGround(boss.Position));
                safe.localScale = new Vector3(BlightSafe * 2f, 1f, BlightSafe * 2f);
                GameEvents.Warn($"The blight closes in - get close to {w.Name}!");
                SynthAudio.Play(Sfx.Whoosh, 0.45f, 0.8f);
            }
            if (sigStage == 0) return;
            sigClock += dt;
            Vector3 c = boss.Position;
            if (sigProps.Count > 0) sigProps[0].position = OnGround(c, 0.08f);
            if (sigStage == 1 && sigClock >= BlightWarn)
            {
                sigStage = 2; sigClock = 0f;
                sigBig = new GameObject("Blight").transform;
                sigBig.SetParent(transform, false);
                sigBig.gameObject.AddComponent<MeshFilter>().sharedMesh = fogMesh;
                var mr = sigBig.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = RenderAssets.Building;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            if (sigStage != 2) return;
            sigBig.position = OnGround(c, 0.12f);
            for (int i = 0; i < 3; i++)
            {
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(BlightSafe + 0.5f, ArenaRadius + 4f);
                Vector3 p = OnGround(c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r, 0.3f);
                Fx.Mote(p, Vector3.up * 0.9f, new Color(0.55f, 0.75f, 0.2f, 0.7f), 0.5f, 1.2f, false);
            }
            sigTick -= dt;
            if (sigTick <= 0f)
            {
                sigTick = 0.6f;
                SigHit(pl, comp, compUp, p => Flat(p, c) > BlightSafe && Flat(p, w.pos) < LeaveRadius, 0.04f, 0.05f, StatusType.Poison, 0.6f, c, 0f);
            }
            if (sigClock >= BlightTime)
            {
                Destroy(sigBig.gameObject); sigBig = null;
                ClearProps();
                sigStage = 0; sigTimer = Every(17f);
            }
        }

        // ============================================================ 9 ASTRALISK - starfall spiral
        void TickStarfall(Warden w, PlayerController pl)
        {
            if (!Ready) return;
            Explain(w);
            sigTimer = Every(13f);
            Vector3 c = boss.Position;
            float a0 = Mathf.Atan2(pl.Position.x - c.x, pl.Position.z - c.z);
            int arms = phase >= 3 ? 2 : 1;
            float turn = Random.value < 0.5f ? 0.55f : -0.55f;
            for (int arm = 0; arm < arms; arm++)
                for (int i = 0; i < 14; i++)
                {
                    float a = a0 + arm * Mathf.PI + i * turn, r = 2.5f + i * 1.25f;
                    var z = AddZone(c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r, 2.1f, 1.1f + i * 0.17f, false, starMesh);
                    z.pf = 0.1f; z.kf = 0.09f; z.knock = 3f; z.st = StatusType.Shock; z.stChance = 0.3f;   // a trail can catch you twice: lighter per star
                }
            SynthAudio.Play(Sfx.Whoosh, 1.3f, 0.7f);
        }

        // ============================================================ 10 STORMCREST - lightning rods
        const float RodCharge = 2.6f, RodRadius = 11f;

        void TickRods(float dt, Warden w, PlayerController pl)
        {
            if (Ready)
            {
                Explain(w);
                sigTimer = Every(15f);
                float a0 = Random.value * 90f;
                var pts = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    float a = (a0 + i * 90f) * Mathf.Deg2Rad;
                    pts[i] = OnGround(w.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * RodRadius, 0f);
                    var t = Prop(rodMesh, pts[i]);
                    t.localScale = new Vector3(1f, 0.05f, 1f);
                }
                for (int i = 0; i < 4; i++) Bolt(pts[i], pts[(i + 1) % 4]);
                if (phase >= 3) { Bolt(pts[0], pts[2]); Bolt(pts[1], pts[3]); }
                SynthAudio.Play(Sfx.Summon, 0.7f, 0.8f);
            }
            for (int i = sigProps.Count - 1; i >= 0; i--)
            {
                var t = sigProps[i];
                float age = sigPropAge[i] += dt;
                t.localScale = new Vector3(1f, Mathf.Clamp(age / 0.8f, 0.05f, 1f), 1f);
                if (age < RodCharge && Random.value < 0.25f) Fx.Mote(t.position + Vector3.up * 6.2f, Random.insideUnitSphere * 1.5f, new Color(1f, 0.95f, 0.5f, 1f), 0.25f, 0.3f, true);
                if (age > RodCharge + 0.9f) t.localScale = new Vector3(1f, Mathf.Max(0.05f, 1f - (age - RodCharge - 0.9f) / 0.5f), 1f);
                if (age > RodCharge + 1.4f) DropProp(i);
            }
        }

        void Bolt(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a; d.y = 0;
            AddLane(a, d.normalized, d.magnitude, 2.4f, RodCharge, 0.15f, 0.13f, StatusType.Shock, 0.5f, 3f);
        }

        // ============================================================ LANES (used by heads, tentacles, rods)
        void AddLane(Vector3 start, Vector3 dir, float len, float width, float fuse, float pf, float kf, StatusType st, float stChance, float knock)
        {
            dir.y = 0; dir.Normalize();
            Vector3 mid = start + dir * (len * 0.5f);
            var z = new Zone
            {
                pos = OnGround(mid), r = width * 0.5f, fuse = fuse, life = 0.25f, pool = false, lane = true,
                start = start, dir = dir, len = len, pf = pf, kf = kf, st = st, stChance = stChance, knock = knock,
                disc = Disc(laneMesh)
            };
            z.disc.position = z.pos;
            z.disc.rotation = Quaternion.LookRotation(dir);
            z.disc.localScale = new Vector3(width, 1f, len);
            zones.Add(z);
        }

        // ============================================================ MESHES
        void MakeSigMeshes(Warden w)
        {
            var mb = new MeshBuilder();
            // every signature marks the ground in its own colours, never the yellow-and-red of the shared strikes
            Color warn = new Color(1f, 0.82f, 0.3f), warnRim = new Color(1f, 0.3f, 0.2f);
            switch (sig)
            {
                case WardenSig.Heads: warn = new Color(0.55f, 0.85f, 0.3f); warnRim = new Color(0.3f, 0.15f, 0.4f); break;      // venom green, bruise stripe
                case WardenSig.Tentacles: warn = new Color(0.35f, 0.55f, 0.85f); warnRim = new Color(0.45f, 0.2f, 0.55f); break; // deep water, ink
                case WardenSig.Rods: warn = new Color(1f, 1f, 0.7f); warnRim = new Color(0.4f, 0.6f, 1f); break;                 // white-gold bolt, blue core
                case WardenSig.SunBeam: warn = new Color(1f, 0.9f, 0.5f); warnRim = new Color(1f, 0.55f, 0.1f); break;
                case WardenSig.Vortex: warn = new Color(0.95f, 0.75f, 0.45f); warnRim = new Color(0.85f, 0.35f, 0.1f); break;
            }
            mb.Box(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.05f, 1f)), warnRim, warn);
            mb.Box(Matrix4x4.TRS(new Vector3(0, 0.02f, 0), Quaternion.identity, new Vector3(0.35f, 0.05f, 1f)), warnRim, warnRim);
            laneMesh = mb.ToMesh("warden_lane");
            if (sig == WardenSig.Starfall)
            {
                // falling-star marks: violet ring, white heart, four points
                mb.Clear();
                Color32 vio = new Color(0.55f, 0.4f, 1f), white = new Color(0.95f, 0.95f, 1f);
                mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.05f, 1f)), vio, vio, 20, 1f, true);
                mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.03f, 0), Quaternion.identity, new Vector3(0.45f, 0.05f, 0.45f)), white, white, 16, 1f, true);
                for (int i = 0; i < 4; i++)
                    mb.Box(Matrix4x4.TRS(new Vector3(0, 0.035f, 0), Quaternion.Euler(0, i * 45f, 0), new Vector3(0.12f, 0.05f, 0.95f)), white, white);
                starMesh = mb.ToMesh("warden_star");
            }
            switch (sig)
            {
                case WardenSig.Blooms:
                    mb.Clear();
                    Color32 stem = new Color(0.3f, 0.6f, 0.25f), petal = new Color(1f, 0.55f, 0.8f), petal2 = new Color(1f, 0.8f, 0.92f), heart = new Color(1f, 0.9f, 0.35f);
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(0.14f, 1f, 0.14f)), stem, 6);
                    mb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.12f, 0), Quaternion.identity, new Vector3(1.2f, 0.25f, 1.2f)), stem, 8, 4);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f;
                        Vector3 o = Quaternion.Euler(0, a, 0) * new Vector3(0, 1.05f, 0.42f);
                        mb.Sphere(Matrix4x4.TRS(o, Quaternion.Euler(-35f, a, 0), new Vector3(0.45f, 0.14f, 0.7f)), i % 2 == 0 ? petal : petal2, 8, 5);
                    }
                    mb.Sphere(Matrix4x4.TRS(new Vector3(0, 1.12f, 0), Quaternion.identity, Vector3.one * 0.38f), heart, 8, 6);
                    bloomMesh = mb.ToMesh("warden_bloom");
                    break;
                case WardenSig.Tide:
                    mb.Clear();
                    Color32 sand = new Color(0.86f, 0.8f, 0.62f), coral = new Color(1f, 0.45f, 0.5f), coral2 = new Color(1f, 0.7f, 0.4f), rim = new Color(0.5f, 1f, 0.85f);
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.35f, 0), Quaternion.identity, new Vector3(IslandR * 2f, 0.7f, IslandR * 2f)), sand, rim, 16, 0.92f, true);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 o = Quaternion.Euler(0, i * 72f + 20f, 0) * new Vector3(0, 0, IslandR * 0.7f);
                        mb.Cylinder(Matrix4x4.TRS(o + new Vector3(0, 1.0f, 0), Quaternion.Euler(i * 7f - 14f, 0, i * 9f - 18f), new Vector3(0.22f, 1.3f, 0.22f)), i % 2 == 0 ? coral : coral2, 6, 0.6f, true);
                    }
                    islandMesh = mb.ToMesh("warden_island");
                    mb.Clear();
                    mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(2f, 0.04f, 2f)), new Color(0.3f, 0.6f, 0.8f), 32, 1f, true);
                    waterMesh = mb.ToMesh("warden_flood");
                    break;
                case WardenSig.SunBeam:
                    mb.Clear();
                    mb.Box(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.95f, 0.55f));
                    mb.Box(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.45f, 1.08f, 1f)), new Color(1f, 0.98f, 0.8f), new Color(1f, 1f, 0.9f));
                    beamMesh = mb.ToMesh("warden_beam");
                    break;
                case WardenSig.Tentacles:
                    mb.Clear();
                    Color32 skin = new Color(0.55f, 0.35f, 0.6f), belly = new Color(0.85f, 0.7f, 0.75f);
                    for (int i = 0; i < 6; i++)
                    {
                        float y = i * 1.25f, s = 1.25f - i * 0.17f;
                        mb.Cylinder(Matrix4x4.TRS(new Vector3(0, y + 0.62f, 0), Quaternion.identity, new Vector3(s, 1.3f, s)), skin, skin, 8, 0.86f, true);
                        mb.Sphere(Matrix4x4.TRS(new Vector3(0, y + 0.6f, s * 0.42f), Quaternion.identity, new Vector3(s * 0.45f, 0.3f, 0.2f)), belly, 6, 4);
                    }
                    mb.Cone(Matrix4x4.TRS(new Vector3(0, 7.9f, 0), Quaternion.identity, new Vector3(0.3f, 0.9f, 0.3f)), skin, 6);
                    tentacleMesh = mb.ToMesh("warden_tentacle");
                    break;
                case WardenSig.IcePrison:
                    mb.Clear();
                    Color32 ice = new Color(0.7f, 0.9f, 1f), iceTip = new Color(0.95f, 1f, 1f);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * 51f;
                        Vector3 o = Quaternion.Euler(0, a, 0) * new Vector3(0, 0, 0.75f);
                        mb.Crystal(Matrix4x4.TRS(o, Quaternion.Euler(0, a, 18f + (i % 3) * 6f), new Vector3(0.6f, 1.6f + (i % 3) * 0.4f, 0.6f)), ice, iceTip);
                    }
                    mb.Crystal(Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(0.9f, 2.4f, 0.9f)), ice, iceTip);
                    iceMesh = mb.ToMesh("warden_ice");
                    break;
                case WardenSig.Blight:
                    mb.Clear();
                    mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.05f, 1f)), new Color(0.4f, 1f, 0.6f), new Color(0.2f, 0.5f, 0.3f), 24, 1f, true);
                    safeMesh = mb.ToMesh("warden_safe");
                    fogMesh = Annulus(BlightSafe, ArenaRadius + 6f, 48, new Color32(62, 78, 30, 255), new Color32(44, 30, 52, 255));   // murky green fading to bruise purple
                    break;
                case WardenSig.Rods:
                    mb.Clear();
                    Color32 rodC = new Color(0.55f, 0.55f, 0.62f), gold = new Color(1f, 0.85f, 0.3f);
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.3f, 0), Quaternion.identity, new Vector3(1.4f, 0.6f, 1.4f)), rodC, 8);
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 3.2f, 0), Quaternion.identity, new Vector3(0.45f, 5.6f, 0.45f)), rodC, gold, 8, 0.7f, true);
                    mb.Sphere(Matrix4x4.TRS(new Vector3(0, 6.2f, 0), Quaternion.identity, Vector3.one * 0.6f), gold, 8, 6);
                    rodMesh = mb.ToMesh("warden_rod");
                    break;
            }
        }

        /// <summary>A flat ring between two radii (the blight's fog, with a clear hole in the middle).</summary>
        static Mesh Annulus(float rIn, float rOut, int segs, Color32 inner, Color32 outer)
        {
            var v = new Vector3[segs * 2];
            var c = new Color32[segs * 2];
            var n = new Vector3[segs * 2];
            var t = new int[segs * 6];
            for (int i = 0; i < segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                v[i * 2] = d * rIn; v[i * 2 + 1] = d * rOut;
                c[i * 2] = inner; c[i * 2 + 1] = outer;
                n[i * 2] = n[i * 2 + 1] = Vector3.up;
                int j = (i + 1) % segs;
                t[i * 6] = i * 2; t[i * 6 + 1] = i * 2 + 1; t[i * 6 + 2] = j * 2;
                t[i * 6 + 3] = j * 2; t[i * 6 + 4] = i * 2 + 1; t[i * 6 + 5] = j * 2 + 1;
            }
            var m = new Mesh { name = "warden_fog", vertices = v, colors32 = c, normals = n, triangles = t };
            m.RecalculateBounds();
            return m;
        }
    }
}
