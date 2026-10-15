using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Cave hazards (cave pass, 2026-10-15). None in shallow caves, a few in mid caves, lots in the deep ones (above all
    /// their Hazard chambers). Every hazard warns before it hurts:
    ///   Rockfall - a loose stretch of roof (a scatter of fallen stones marks it). While you are under it, every few
    ///     seconds a shadow grows under you and a rock drops 1.1 s later: 9% health and a stagger if it lands on you.
    ///   Vent - a crack in the floor that sputters for a second, then blasts a column for 1.8 s: 5% health every half
    ///     second you stand in it, and the land's status (steam Burn, frost Chill, gas Poison, spores Drowsy, jets Soaked).
    ///   Pool - lava, ooze, freezing water or spore mud, glowing: 4% health every 0.6 s while you stand in it, + status.
    /// Ruins also keep their spike plates (`Trap`).
    /// </summary>
    public partial class Caves
    {
        enum HazKind { Rockfall, Vent, Pool }

        class Hazard
        {
            public HazKind kind; public Vector3 pos; public float radius, phase, cool, timer;
            public StatusType status; public Color col; public Element el;
            public Transform shadow, rock; public Vector3 dropAt; public int state;   // rockfall: 0 idle, 1 shadow, 2 falling
            public bool erupting;
        }

        static Mesh discMesh, ventMesh, rockMesh;

        void PlaceHazards(Run r, System.Random rng)
        {
            if (r.tier == CaveTier.Early) return;
            var e = r.e;
            float u() => (float)rng.NextDouble();
            int count(Role role) => r.tier == CaveTier.Mid ? (role == Role.Pass || role == Role.Hoard || role == Role.Colony || role == Role.Mini ? 1 : 0)
                                  : role == Role.Hazard ? 4 + rng.Next(0, 2) : role == Role.Entrance || role == Role.Eggs ? 0 : 1 + (u() < 0.4f ? 1 : 0);
            Element el; StatusType st; Color col; bool pools;
            switch (e.biome)
            {
                case BiomeId.Badlands: el = Element.Ember; st = StatusType.Burn; col = new Color(1f, 0.45f, 0.12f); pools = true; break;
                case BiomeId.Desert: el = Element.Ember; st = StatusType.Burn; col = new Color(1f, 0.6f, 0.2f); pools = false; break;
                case BiomeId.Peaks: el = Element.Frost; st = StatusType.Chill; col = new Color(0.55f, 0.85f, 1f); pools = true; break;
                case BiomeId.Swamp: el = Element.Shade; st = StatusType.Poison; col = new Color(0.6f, 0.9f, 0.3f); pools = true; break;
                case BiomeId.Blight: el = Element.Shade; st = StatusType.Poison; col = new Color(0.75f, 0.35f, 1f); pools = true; break;
                case BiomeId.Isles: el = Element.Tide; st = StatusType.Soaked; col = new Color(0.35f, 0.8f, 1f); pools = false; break;
                case BiomeId.Glow: el = Element.Lumen; st = StatusType.Drowsy; col = new Color(0.5f, 1f, 0.95f); pools = true; break;
                default: el = Element.Flora; st = StatusType.Drowsy; col = new Color(0.75f, 0.95f, 0.45f); pools = false; break;
            }
            bool TooClose(Vector3 p)
            {
                if (Flat2(p, r.startPos) < 10f * 10f) return true;
                foreach (var c in r.chests) if (Flat2(c.pos, p) < 4f * 4f) return true;
                foreach (var n in r.nests) if (Flat2(n.pos, p) < 3.5f * 3.5f) return true;
                foreach (var n in r.nodes) if (Flat2(n.pos, p) < 3f * 3f) return true;
                foreach (var h in r.hazards) if (Flat2(h.pos, p) < 6f * 6f) return true;
                return false;
            }
            for (int ri = 1; ri < r.rooms.Count; ri++)
            {
                var rm = r.rooms[ri];
                int n = count(rm.role);
                for (int k = 0; k < n; k++)
                {
                    var p = OpenSpot(r, rm.c, rm.r * 0.75f, 2f, rng);
                    for (int tries = 0; tries < 5 && TooClose(p); tries++) p = OpenSpot(r, rm.c, rm.r * 0.8f, 2f, rng);
                    if (TooClose(p)) continue;
                    float pick = u();
                    var kind = pick < 0.34f ? HazKind.Rockfall : pick < 0.7f || !pools ? HazKind.Vent : HazKind.Pool;
                    AddHazard(r, kind, p, el, st, col, rng);
                }
            }
            // the tunnels between chambers get a few too
            int inTunnels = r.tier == CaveTier.End ? 4 + rng.Next(0, 3) : 1 + rng.Next(0, 2);
            for (int k = 0; k < inTunnels && r.tunnels.Count > 0; k++)
            {
                var t = r.tunnels[rng.Next(r.tunnels.Count)];
                var q = t.pts[1 + rng.Next(0, t.pts.Length - 2)];
                var p = new Vector3(q.x, FloorAt2(r, q.x, q.y), q.y);
                if (Sample(r.F, p.x, p.z) < 1f || TooClose(p)) continue;
                AddHazard(r, u() < 0.5f ? HazKind.Rockfall : HazKind.Vent, p, el, st, col, rng);
            }
        }

        void AddHazard(Run r, HazKind kind, Vector3 p, Element el, StatusType st, Color col, System.Random rng)
        {
            EnsureHazardMeshes();
            float u() => (float)rng.NextDouble();
            var h = new Hazard { kind = kind, pos = p, el = el, status = st, col = col, phase = u() * 7f, radius = kind == HazKind.Rockfall ? 5.5f : kind == HazKind.Vent ? 1.6f : 1.4f + u() * 1.1f };
            var mb = new MeshBuilder();
            Color rock = r.e.rock, top = r.e.rockTop;
            switch (kind)
            {
                case HazKind.Rockfall:
                    // fallen stones mark the stretch, and a ring of cracked roof hangs over it
                    for (int k = 0; k < 7; k++)
                    {
                        float a = u() * 6.28f, d = u() * h.radius;
                        var o = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                        o.y = FloorAt2(r, p.x + o.x, p.z + o.z) - p.y;
                        float s = 0.25f + u() * 0.45f;
                        mb.Rock(Matrix4x4.TRS(o + Vector3.up * s * 0.25f, Quaternion.Euler(0, u() * 360f, 0), new Vector3(s * 1.2f, s * 0.7f, s)), rock, top, (uint)rng.Next(), 5, 4, 0.3f);
                    }
                    {
                        float c = CeilAt(r, p.x, p.z) - p.y;
                        for (int k = 0; k < 5; k++)
                        {
                            float a = k * 1.25f + u(), d = 1f + u() * 2.5f;
                            mb.Rock(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * d, c - 0.25f, Mathf.Sin(a) * d), Quaternion.Euler(0, u() * 360f, 0), new Vector3(0.9f, 0.6f, 0.8f)), top, rock, (uint)rng.Next(), 5, 4, 0.3f);
                        }
                    }
                    break;
                case HazKind.Vent:
                    mb.Append(MeshFromVent(), Matrix4x4.identity);
                    mb.Sphere(Matrix4x4.TRS(new Vector3(0, 0.02f, 0), Quaternion.identity, new Vector3(0.7f, 0.08f, 0.7f)), new Color(col.r, col.g, col.b, 0f), 10, 4);
                    break;
                case HazKind.Pool:
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.04f, 0), Quaternion.identity, new Vector3(h.radius * 2f + 0.5f, 0.08f, h.radius * 2f + 0.5f)), rock * 0.7f, 14, 1f, true);
                    mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.07f, 0), Quaternion.identity, new Vector3(h.radius * 2f, 0.04f, h.radius * 2f)), new Color(col.r * 0.8f, col.g * 0.8f, col.b * 0.8f, 0f), new Color(col.r, col.g, col.b, 0f), 14, 1f, true);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = u() * 6.28f, d = u() * h.radius * 0.7f;
                        mb.Sphere(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * d, 0.1f, Mathf.Sin(a) * d), Quaternion.identity, Vector3.one * (0.15f + u() * 0.2f)), new Color(Mathf.Lerp(col.r, 1f, 0.5f), Mathf.Lerp(col.g, 1f, 0.5f), Mathf.Lerp(col.b, 1f, 0.5f), 0f), 6, 4);
                    }
                    break;
            }
            var go = new GameObject("Hazard_" + kind);
            go.transform.SetParent(r.root.transform, false);
            go.transform.position = p;
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("cave_hazard");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            if (kind == HazKind.Rockfall)
            {
                h.shadow = Child(r, discMesh, RenderAssets.Creature);
                h.rock = Child(r, rockMesh, RenderAssets.Creature);
            }
            r.hazards.Add(h);
        }

        static Transform Child(Run r, Mesh m, Material mat)
        {
            var go = new GameObject("HazardPart");
            go.transform.SetParent(r.root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.SetActive(false);
            return go.transform;
        }

        static MeshBuilder ventBuilder;
        static MeshBuilder MeshFromVent()
        {
            if (ventBuilder != null) return ventBuilder;
            var mb = new MeshBuilder();
            Color32 dark = new Color(0.12f, 0.1f, 0.1f), rim = new Color(0.32f, 0.28f, 0.26f);
            for (int k = 0; k < 7; k++)
            {
                float a = k * 51.4f * Mathf.Deg2Rad;
                mb.Rock(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 0.85f, 0.12f, Mathf.Sin(a) * 0.85f), Quaternion.Euler(0, k * 40f, 0), new Vector3(0.6f, 0.35f, 0.5f)), rim, rim, (uint)(301 + k), 5, 4, 0.3f);
            }
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.03f, 0), Quaternion.identity, new Vector3(1.2f, 0.04f, 1.2f)), dark, 10, 1f, true);
            ventBuilder = mb;
            return mb;
        }

        static void EnsureHazardMeshes()
        {
            if (discMesh != null) return;
            var mb = new MeshBuilder();
            mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.02f, 1f)), new Color32(20, 12, 10, 255), 16, 1f, true);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.005f, 0), Quaternion.identity, new Vector3(1.08f, 0.015f, 1.08f)), new Color32(255, 110, 70, 0), 16, 1f, true);
            discMesh = mb.ToMesh("cave_rock_shadow");
            var rb = new MeshBuilder();
            rb.Rock(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1.1f, 0.9f, 1f)), new Color(0.42f, 0.4f, 0.38f), new Color(0.55f, 0.52f, 0.5f), 77u, 6, 5, 0.25f);
            rockMesh = rb.ToMesh("cave_falling_rock");
        }

        // ================================================================ TICK
        void TickHazards(Run r, float dt, PlayerController pl)
        {
            if (r.hazards.Count == 0) return;
            var pp = pl.Position;
            float maxHp = pl.Stats.MaxHp;
            foreach (var h in r.hazards)
            {
                h.cool -= dt;
                float d2 = Flat2(h.pos, pp);
                switch (h.kind)
                {
                    case HazKind.Rockfall:
                    {
                        h.timer -= dt;
                        if (h.state == 0)
                        {
                            if (d2 > (h.radius + 1.5f) * (h.radius + 1.5f) || !pl.IsAlive || h.timer > 0f) break;
                            // a shadow under you (a step ahead if you are moving)
                            var at = pp + pl.Velocity * 0.55f + new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f));
                            at = Nudge(r, at);
                            h.dropAt = at; h.state = 1; h.timer = 1.1f;
                            h.shadow.gameObject.SetActive(true);
                            h.shadow.position = at + Vector3.up * 0.03f;
                            h.shadow.localScale = Vector3.one * 0.3f;
                            Fx.Debris(new Vector3(at.x, CeilAt(r, at.x, at.z) - 0.3f, at.z), r.e.rock, 5);
                            SynthAudio.Play(Sfx.Clunk, 0.6f, 0.5f);
                        }
                        else if (h.state == 1)
                        {
                            float k = 1f - Mathf.Clamp01(h.timer / 1.1f);
                            h.shadow.localScale = new Vector3(0.3f + 1.5f * k, 1f, 0.3f + 1.5f * k);
                            if (h.timer <= 0f)
                            {
                                h.state = 2; h.timer = 0.28f;
                                h.rock.gameObject.SetActive(true);
                                h.rock.position = new Vector3(h.dropAt.x, CeilAt(r, h.dropAt.x, h.dropAt.z) - 0.4f, h.dropAt.z);
                                h.rock.localScale = Vector3.one * 1.1f;
                            }
                        }
                        else
                        {
                            float top = CeilAt(r, h.dropAt.x, h.dropAt.z) - 0.4f;
                            h.rock.position = new Vector3(h.dropAt.x, Mathf.Lerp(h.dropAt.y + 0.45f, top, Mathf.Clamp01(h.timer / 0.28f)), h.dropAt.z);
                            h.rock.Rotate(400f * dt, 230f * dt, 0f);
                            if (h.timer <= 0f)
                            {
                                h.state = 0; h.timer = Random.Range(2.4f, 4.2f);
                                h.shadow.gameObject.SetActive(false);
                                h.rock.gameObject.SetActive(false);
                                Fx.Burst(h.dropAt + Vector3.up * 0.4f, r.e.rock, 18, 4f, 0.3f, 0.7f, false, 6f);
                                SynthAudio.Play(Sfx.Mine, 0.7f, 0.9f);
                                GameManager.I.CameraRig.Shake(0.25f);
                                if (pl.IsAlive && Flat2(h.dropAt, pl.Position) < 1.55f * 1.55f)
                                {
                                    pl.ReceiveHit(new HitInfo { damage = Mathf.Max(5f, maxHp * 0.09f), element = Element.Stone, effectiveness = 1f, knockback = (pl.Position - h.dropAt).normalized * 4f });
                                    HUD.DamageNumber(pl.Position + Vector3.up * 2.2f, "ROCKFALL", new Color(1f, 0.75f, 0.5f), 0.9f);
                                }
                            }
                        }
                        break;
                    }
                    case HazKind.Vent:
                    {
                        float c = (Time.time + h.phase) % 7f;
                        bool warn = c > 4.2f && c <= 5.2f, burst = c > 5.2f;
                        if (d2 < 30f * 30f)
                        {
                            if (warn && Random.value < dt * 10f) Fx.Puff(h.pos + Vector3.up * 0.3f + Random.insideUnitSphere * 0.3f, h.col * 0.8f, 0.25f, 0.5f);
                            if (burst)
                                for (int q = 0; q < 2; q++)
                                    Fx.Mote(h.pos + new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, Random.Range(-0.5f, 0.5f)), new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(6f, 9f), Random.Range(-0.6f, 0.6f)), h.col, Random.Range(0.35f, 0.6f), 0.7f, true);
                        }
                        if (burst && !h.erupting && d2 < 20f * 20f) SynthAudio.Play(Sfx.Whoosh, 0.6f, 0.5f);
                        h.erupting = burst;
                        if (burst && h.cool <= 0f && pl.IsAlive && d2 < h.radius * h.radius && pp.y - h.pos.y < 3.5f)
                        {
                            h.cool = 0.5f;
                            pl.ReceiveHit(new HitInfo { damage = Mathf.Max(3f, maxHp * 0.05f), element = h.el, effectiveness = 1f, status = h.status, statusChance = 0.6f });
                        }
                        break;
                    }
                    case HazKind.Pool:
                    {
                        if (d2 < 25f * 25f && Random.value < dt * 2.5f)
                            Fx.Mote(h.pos + new Vector3(Random.Range(-h.radius, h.radius) * 0.7f, 0.15f, Random.Range(-h.radius, h.radius) * 0.7f), Vector3.up * 0.8f, h.col, 0.22f, 1f, true);
                        if (h.cool <= 0f && pl.IsAlive && !pl.IsMounted && d2 < h.radius * h.radius && pp.y - h.pos.y < 0.6f)
                        {
                            h.cool = 0.6f;
                            pl.ReceiveHit(new HitInfo { damage = Mathf.Max(2f, maxHp * 0.04f), element = h.el, effectiveness = 1f, status = h.status, statusChance = 0.5f });
                        }
                        break;
                    }
                }
            }
        }
    }
}
