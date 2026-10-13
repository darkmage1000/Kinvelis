using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Tether Seeds: thrown on a ballistic arc; on hitting a wild kin the seed sprouts vines,
    /// pulls it in and shakes up to 3 times. Each shake passes with p^(1/3), so the overall
    /// success chance equals p. p depends on HP, rarity, status, seed tier, Binding skill,
    /// level gap, alpha/shimmer, and a sneak-throw bonus for unaware targets.
    /// </summary>
    public class CaptureSystem : MonoBehaviour
    {
        [Header("Tuning")]
        [SerializeField] float flightTime = 0.55f;
        [SerializeField] float seedGravity = 20f;
        [SerializeField] float shakeInterval = 0.75f;
        [SerializeField] float maxThrowRange = 20f;
        [SerializeField] float throwCooldown = 0.6f;

        enum Phase { Idle, Flying, Binding, Shaking, Result }

        Phase phase;
        Transform seed;
        Vector3 pos, vel;
        float timer;
        int shakes;
        float perShake;
        bool willSucceed;
        CreatureActor victim;
        ItemDef seedDef;
        float cooldown;
        float groundTimer;
        bool sneakBonus;
        CreatureActor homing;

        public bool Busy => phase != Phase.Idle;

        public void Init()
        {
            var go = new GameObject("TetherSeed");
            go.transform.SetParent(transform, false);
            var mb = new MeshBuilder();
            mb.Sphere(Matrix4x4.Scale(new Vector3(0.34f, 0.4f, 0.34f)), new Color(0.55f, 0.85f, 0.35f), new Color(0.3f, 0.6f, 0.25f), 10, 7);
            mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.36f, 0.06f, 0.36f)), new Color(0.45f, 0.32f, 0.2f), 10, 1f, true);
            mb.Cone(Matrix4x4.TRS(new Vector3(0, 0.25f, 0), Quaternion.identity, new Vector3(0.08f, 0.14f, 0.08f)), new Color(0.35f, 0.75f, 0.3f), 6);
            mb.Sphere(Matrix4x4.TRS(new Vector3(0.1f, 0.3f, 0), Quaternion.Euler(0, 0, -40), new Vector3(0.14f, 0.05f, 0.08f)), new Color(0.4f, 0.85f, 0.4f), 6, 4);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("seed");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            seed = go.transform;
            seed.gameObject.SetActive(false);
        }

        // ============================================================ PROBABILITY
        /// <summary>Highest kin level a seed is rated for; above it the chance falls off fast (Tether 20, Ironbark 40, Starbloom any).</summary>
        /// <summary>Catch multiplier of a seed tier: Tether x1, Ironbark x1.15, Starbloom x1.35.</summary>
        public static float SeedBonus(ItemDef seed) => seed == null ? 1f : 1f + (Mathf.Max(1f, seed.captureMult) - 1f) * 0.25f;

        public static int SeedLevelCap(ItemDef seed) => seed == null ? 0 : seed.captureMult >= 2.2f ? 99 : seed.captureMult >= 1.5f ? 40 : 20;

        /// <summary>
        /// Final capture chance 0..1. Health is what matters: ~20% at full HP rising to 90% at 5% HP or less.
        /// Rarity and shimmer don't affect it. Better seeds give a modest boost and are rated for higher-level kin;
        /// each level above the seed's rating costs 6% (down to x0.15).
        /// </summary>
        public static float Chance(CreatureActor c, ItemDef seedItem, PlayerStats stats, bool sneak)
        {
            if (c == null || seedItem == null) return 0f;
            if (Dev.SureCatch) return 1f;
            float hpFrac = Mathf.Clamp01(c.Hp / Mathf.Max(1f, c.MaxHp));
            if (IsLegendSeed(seedItem))
            {
                if (!SeedFits(seedItem, c.Sp)) return 0f;          // made for one legend: nothing else
                // a boss's own seed (the Kraken's): no hold at all until it is nearly beaten, then good odds that climb as it weakens
                if (c.Boss) return hpFrac > BossSeedHp || c.Invulnerable ? 0f : Mathf.Lerp(BossSeedMax, BossSeedMin, hpFrac / BossSeedHp);
                if (hpFrac <= LegendSeedHp) return 1f;             // ...and at 10% health or less it cannot fail
            }
            float p = HpCurve(hpFrac);                                 // full 20% · 50% 45% · 25% 65% · 10% 90% · 5% or less 99%
            p *= c.Sp.captureMod;                                      // per-species designer knob (1 by default)
            if (c.Status.Active) p *= StatusState.CaptureBonus(c.Status.type);
            p *= stats.CaptureMult;                                    // relics + gear
            p *= Nightfall.CatchMul;                                   // drowsy kin on a meadow night
            p *= DexRewards.MilestoneCatchMul(stats) * DexRewards.CatchMul(stats, c.Sp.id);   // Dex milestones + knowing the species
            if (stats.HasEffect(UniqueEffect.Tamer) && hpFrac < 0.5f) p *= 1.3f;
            if (stats.HasEffect(UniqueEffect.SeaFriend) && (c.Sp.element1 == Element.Tide || c.Sp.element2 == Element.Tide)) p *= 1.15f;
            if (stats.HasEffect(UniqueEffect.NightsFavour) && EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight) p *= 1.12f;
            if (sneak) p *= 1.35f;
            p *= SeedBonus(seedItem);                                  // better seeds: a modest boost (their real job is higher-level kin)
            int over = c.Level - SeedLevelCap(seedItem);
            if (over > 0) p *= Mathf.Clamp(1f - over * 0.06f, 0.15f, 1f);
            // only all 7 Tetherbloom relics unlock a true 100%; otherwise 99% is the ceiling
            float cap = stats.RelicCount(RelicType.Tetherbloom) >= Relics.PerType ? 1f : 0.99f;
            // overkill: a seed tier well above the kin's level is a near-sure catch (Starbloom on a low-level kin ~ cap)
            int under = SeedTierFloor(seedItem) - c.Level;
            if (under > 0) p = Mathf.Lerp(p, cap, Mathf.Clamp01(under / 20f));
            if (c.Data.alpha) p *= 0.5f;
            if (c.Sp.rarity == Rarity.Legendary) p *= LegendMul;   // a Starbloom at 10% health lands at 55%: good odds, never a sure thing
            p *= WorldRules.Current.captureRate;
            return Mathf.Clamp(p, 0.01f, cap);
        }

        /// <summary>Lowest kin level a seed tier is meant for (Tether 0, Ironbark 20, Starbloom 40); kin below it are overkill.</summary>
        public static int SeedTierFloor(ItemDef seed) => seed == null ? 0 : seed.captureMult >= 2.2f ? 40 : seed.captureMult >= 1.5f ? 20 : 0;

        // catch chance by remaining HP fraction (linear between points)
        static readonly float[] CurveHp = { 0.05f, 0.10f, 0.25f, 0.50f, 1.00f };
        static readonly float[] CurveP  = { 0.99f, 0.90f, 0.65f, 0.45f, 0.20f };

        static float HpCurve(float hp)
        {
            if (hp <= CurveHp[0]) return CurveP[0];
            for (int i = 1; i < CurveHp.Length; i++)
                if (hp <= CurveHp[i]) return Mathf.Lerp(CurveP[i - 1], CurveP[i], (hp - CurveHp[i - 1]) / (CurveHp[i] - CurveHp[i - 1]));
            return CurveP[CurveP.Length - 1];
        }

        /// <summary>Legendaries resist every seed: tuned so a Starbloom Seed at 10% health is a 55% catch (0.90 x 1.35 x this).</summary>
        public const float LegendMul = 0.453f;

        /// <summary>Health fraction at or below which a legend seed is a certain catch.</summary>
        public const float LegendSeedHp = 0.10f;
        /// <summary>
        /// The Kraken's Deepcall Seed (user, 2026-10-10: "high odds of catching if you get it low enough but impossible to
        /// catch before"): nothing above 15% health, 75% at 15% rising to 95% as its health runs out.
        /// </summary>
        public const float BossSeedHp = 0.15f, BossSeedMin = 0.75f, BossSeedMax = 0.95f;

        /// <summary>A boss can only ever be bound by the seed made for its kind.</summary>
        public static bool BossCatch(CreatureActor c, ItemDef seed) => c != null && c.Boss && IsLegendSeed(seed) && seed.legendFor == c.Sp.id;
        /// <summary>Tamers' kin and Wardens shrug every seed off - except a boss facing its own.</summary>
        public static bool CanBind(CreatureActor c, ItemDef seed) => c != null && (!c.Duelist || BossCatch(c, seed));

        public static bool IsLegendSeed(ItemDef seed) => seed != null && !string.IsNullOrEmpty(seed.legendFor);

        /// <summary>Ordinary seeds fit anything; a legend seed fits its own legend ("*" = any legendary).</summary>
        public static bool SeedFits(ItemDef seed, SpeciesDef sp) =>
            !IsLegendSeed(seed) || (seed.legendFor == "*" ? sp.rarity == Rarity.Legendary : seed.legendFor == sp.id);

        public ItemDef SelectedSeed(PlayerStats stats)
        {
            var d = ContentDB.Item(stats.selectedSeed);
            // a legend seed is only ever in hand while a hunted legend is out (LegendHunts readies it and puts it away again)
            if (d != null && stats.Bag.Count(d.id) > 0 && (!IsLegendSeed(d) || LegendHunts.LegendOut || Wardens.BossOut(d.legendFor))) return d;
            // fall back to any seed we own (best tier first)
            ItemDef best = null;
            foreach (var s in stats.Bag.slots)
            {
                if (s.Empty) continue;
                var it = ContentDB.Item(s.id);
                if (it != null && it.category == ItemCategory.CaptureSeed && !IsLegendSeed(it) && (best == null || it.captureMult > best.captureMult)) best = it;
            }
            return best;
        }

        public void CycleSeed(PlayerStats stats)
        {
            string[] ids = { "tether_seed", "ironbark_seed", "starbloom_seed" };
            int cur = System.Array.IndexOf(ids, stats.selectedSeed);
            for (int i = 1; i <= ids.Length; i++)
            {
                string next = ids[(cur + i + ids.Length) % ids.Length];
                if (stats.Bag.Count(next) > 0) { stats.selectedSeed = next; GameEvents.Say("Seed: " + ContentDB.ItemName(next)); return; }
            }
        }

        // ============================================================ THROW
        public float MaxThrowRange => maxThrowRange;

        /// <summary>Launch parameters shared by the real throw and the aim preview.</summary>
        void ComputeThrow(PlayerController player, CreatureActor target, CameraRig cam, out Vector3 origin, out Vector3 v, out float t, out CreatureActor home)
        {
            origin = player.Position + Vector3.up * 1.6f + player.Forward * 0.3f;
            Vector3 aim;
            home = null;
            if (target != null && Vector3.Distance(target.Position, player.Position) <= maxThrowRange)
            {
                // lead the target so fleeing kin can still be hit
                float tEst = flightTime * Mathf.Clamp(Vector3.Distance(origin, target.Position) / 10f, 0.6f, 1.4f);
                Vector3 lead = target.Velocity; lead.y = 0;
                aim = target.Position + lead * tEst + Vector3.up * (target.Radius + target.Rig.HoverHeight);
                aim.y = Mathf.Max(aim.y, Ground.Height(aim.x, aim.z) + target.Radius);
                home = target;
            }
            else
            {
                aim = player.Position + cam.Forward * 11f;
                aim.y = Ground.Height(aim.x, aim.z) + 0.5f;
            }
            t = flightTime * Mathf.Clamp(Vector3.Distance(origin, aim) / 10f, 0.6f, 1.4f);
            v = (aim - origin) / t + Vector3.up * 0.5f * seedGravity * t;
        }

        /// <summary>Fills pts with the predicted seed path (aim mode dotted arc).</summary>
        public void PredictArc(PlayerController player, CreatureActor target, CameraRig cam, Vector3[] pts)
        {
            ComputeThrow(player, target, cam, out Vector3 o, out Vector3 v, out float t, out _);
            for (int i = 0; i < pts.Length; i++)
            {
                float tt = t * i / (pts.Length - 1);
                pts[i] = o + v * tt + Vector3.down * (0.5f * seedGravity * tt * tt);
            }
        }

        public void TryThrow(PlayerController player, CreatureActor target, CameraRig cam)
        {
            if (phase != Phase.Idle || cooldown > 0f) return;
            var stats = player.Stats;
            seedDef = SelectedSeed(stats);
            if (seedDef == null) { GameEvents.Warn("No capture seeds! Craft Tether Seeds (Menu > Craft)."); return; }
            stats.Bag.Remove(seedDef.id, 1);
            GameEvents.RaiseInventoryChanged();

            ComputeThrow(player, target, cam, out Vector3 origin, out vel, out _, out homing);
            pos = origin;
            seed.position = pos;
            seed.localScale = Vector3.one;
            seed.gameObject.SetActive(true);
            phase = Phase.Flying;
            timer = 3f;
            victim = null;
            sneakBonus = target != null && target.Unaware;
            player.PlayThrow();
            SynthAudio.Play(Sfx.Throw);
            cooldown = throwCooldown;
        }

        public void Tick(float dt)
        {
            cooldown -= dt;
            switch (phase)
            {
                case Phase.Flying:
                {
                    // small steps: one long frame (a hitch while a chunk loads) used to carry the seed straight past a kin
                    float left = dt;
                    while (left > 0f && phase == Phase.Flying)
                    {
                        float step = Mathf.Min(left, 0.02f);
                        TickFlight(step);
                        left -= step;
                    }
                    break;
                }
                case Phase.Binding: TickBinding(dt); break;
                case Phase.Shaking: TickShaking(dt); break;
                case Phase.Result:
                    timer -= dt;
                    if (timer <= 0f) { seed.gameObject.SetActive(false); phase = Phase.Idle; }
                    break;
            }
        }

        void TickFlight(float dt)
        {
            timer -= dt;
            vel += Vector3.down * seedGravity * dt;
            // gentle homing (mobile-friendly aim assist)
            if (homing != null && homing.IsAlive)
            {
                Vector3 to = homing.Position + Vector3.up * homing.Radius - pos;
                Vector3 flat = new Vector3(to.x, 0, to.z);
                Vector3 hv = new Vector3(vel.x, 0, vel.z);
                if (flat.sqrMagnitude > 0.01f)
                {
                    Vector3 want = flat.normalized * hv.magnitude;
                    hv = Vector3.MoveTowards(hv, want, dt * 18f);
                    vel.x = hv.x; vel.z = hv.z;
                }
            }
            pos += vel * dt;
            seed.position = pos;
            seed.Rotate(400f * dt, 0, 200f * dt);
            Fx.Puff(pos, new Color(0.5f, 1f, 0.4f, 0.6f), 0.15f, 0.25f);

            // hit test vs wild kin
            var all = CreatureRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (a.Mode != CreatureMode.Wild || !a.IsAlive || !CanBind(a, seedDef)) continue;   // another tamer's kin can't be bound; a boss only by its own seed
                Vector3 c = a.Position + Vector3.up * (a.Radius + a.Rig.HoverHeight);
                float r = a.Radius + 0.75f;
                if ((c - pos).sqrMagnitude < r * r) { BeginBind(a); return; }
            }

            float h = Ground.Height(pos.x, pos.z);
            if (pos.y <= h + 0.15f || timer <= 0f)
            {
                // missed: seed breaks
                pos.y = h + 0.15f;
                seed.position = pos;
                ReturnIfPrismatic();
                Fx.Burst(pos, new Color(0.55f, 0.85f, 0.35f), 10, 2.5f, 0.25f, 0.5f);
                SynthAudio.Play(Sfx.Clunk, 1.2f, 0.5f);
                phase = Phase.Result;
                timer = 0.3f;
            }
        }

        /// <summary>The Prismatic Seed is too dear to lose: it comes back after a miss or a broken bind.</summary>
        void ReturnIfPrismatic()
        {
            if (seedDef == null || !seedDef.shimmerSeed) return;
            GameManager.I.Player.Stats.Bag.Add(seedDef.id, 1);
            GameEvents.Say($"The {seedDef.displayName} returns to your hand.", new Color(1f, 0.8f, 1f));
            GameEvents.RaiseInventoryChanged();
        }

        void BeginBind(CreatureActor a)
        {
            var stats = GameManager.I.Player.Stats;
            if (!SeedFits(seedDef, a.Sp))
            {
                // a legend seed thrown at anything else just bounces off and is picked back up
                stats.Bag.Add(seedDef.id, 1);
                GameEvents.Warn($"The {seedDef.displayName} only answers to {(seedDef.legendFor == "*" ? "a legend" : ContentDB.Sp(seedDef.legendFor) != null ? ContentDB.Sp(seedDef.legendFor).displayName : "its legend")}. You pick it back up.");
                GameEvents.RaiseInventoryChanged();
                SynthAudio.Play(Sfx.Clunk, 1.2f, 0.5f);
                phase = Phase.Result;
                timer = 0.3f;
                return;
            }
            if (a.Boss && Chance(a, seedDef, stats, false) <= 0f)
            {
                // still too strong (or shielded): the seed slides off and comes back to the bag
                stats.Bag.Add(seedDef.id, 1);
                GameEvents.Warn($"{a.Data.DisplayName} is still too strong - the {seedDef.displayName} slides off and you catch it. Get it to {BossSeedHp * 100f:0}% health or less.");
                GameEvents.RaiseInventoryChanged();
                SynthAudio.Play(Sfx.Clunk, 1.2f, 0.5f);
                phase = Phase.Result;
                timer = 0.3f;
                return;
            }
            victim = a;
            float p = Chance(a, seedDef, stats, sneakBonus);
            perShake = Mathf.Pow(p, 1f / 3f);
            shakes = 0;
            if (sneakBonus) HUD.DamageNumber(a.Position + Vector3.up * 2f, "Surprise throw!", new Color(0.6f, 1f, 0.6f), 0.9f);
            Fx.Burst(a.Position + Vector3.up * a.Radius, new Color(0.45f, 1f, 0.45f), 18, 4f, 0.3f, 0.5f, true);
            SynthAudio.Play(Sfx.Bind);
            a.BeginCapture();
            phase = Phase.Binding;
            timer = 0.45f;
            // seed drops to the ground where the kin was
            pos = a.Position;
            pos.y = Mathf.Max(Ground.Height(pos.x, pos.z), Tides.Surface(pos.x, pos.z) - 0.3f) + 0.2f;
        }

        void TickBinding(float dt)
        {
            timer -= dt;
            seed.position = Vector3.Lerp(seed.position, pos, dt * 12f);
            seed.rotation = Quaternion.Slerp(seed.rotation, Quaternion.identity, dt * 10f);
            if (timer <= 0f) { phase = Phase.Shaking; timer = shakeInterval; }
        }

        void TickShaking(float dt)
        {
            timer -= dt;
            float k = 1f - timer / shakeInterval;
            float wob = Mathf.Sin(k * Mathf.PI * 3f) * 28f * (1f - k);
            seed.rotation = Quaternion.Euler(0, 0, wob);
            if (timer > 0f) return;

            if (victim == null || victim.Data == null)
            {
                phase = Phase.Result; timer = 0.2f; return;
            }
            if (Random.value <= perShake)
            {
                shakes++;
                SynthAudio.Play(Sfx.Shake, 1f + shakes * 0.12f);
                Fx.Sparkle(seed.position, new Color(0.7f, 1f, 0.6f), 5, 0.3f);
                if (shakes >= 3) { Succeed(); return; }
                timer = shakeInterval;
            }
            else Fail();
        }

        void Succeed()
        {
            var a = victim;
            var data = a.Data;
            Fx.Sparkle(seed.position + Vector3.up * 0.3f, new Color(1f, 0.95f, 0.5f), 30, 0.8f);
            Fx.Ring(seed.position, new Color(0.6f, 1f, 0.5f), 2.5f, 26);
            SynthAudio.Play(Sfx.CaptureSuccess);
            if (data.alpha) CreatureSpawner.I.OnAlphaGone(a);
            if (seedDef != null && seedDef.shimmerSeed && !data.shimmer) Shimmer.Make(data);   // Prismatic Seed: it comes out a shimmer
            a.Despawn();
            data.hp = -1f;
            GameManager.I.OnKinCaptured(data);
            if (Wardens.I != null) Wardens.I.BossCaught(a);   // binding a boss wins its fight
            victim = null;
            phase = Phase.Result;
            timer = 0.8f;
        }

        void Fail()
        {
            var a = victim;
            a.transform.position = new Vector3(pos.x, a.transform.position.y, pos.z);
            a.EndCaptureFailed();
            Fx.Burst(seed.position, new Color(0.6f, 0.45f, 0.3f), 14, 3.5f, 0.25f, 0.5f);
            SynthAudio.Play(Sfx.CaptureFail);
            string[] lines = { "Argh! It broke free!", "So close!", "It slipped out!" };
            GameEvents.Warn(shakes >= 2 ? lines[1] : lines[Random.Range(0, 3)]);
            ReturnIfPrismatic();
            victim = null;
            phase = Phase.Result;
            timer = 0.05f;
        }

        public void Cancel()
        {
            if (victim != null && phase != Phase.Idle && phase != Phase.Result) victim.EndCaptureFailed();
            victim = null;
            seed.gameObject.SetActive(false);
            phase = Phase.Idle;
        }
    }
}
