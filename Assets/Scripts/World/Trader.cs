using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Wayfarer Odo, the wandering trader: camps beside one of the waystones you've found, moves on every ~25 minutes
    /// of play, buys your surplus for Glimmer and sells seeds, treats, upgrades, gear and mystery eggs (stock rotates
    /// with each stop).
    /// </summary>
    public class Trader : MonoBehaviour
    {
        public static Trader I { get; private set; }
        public const string Name = "Wayfarer Odo";
        const double StaySeconds = 25 * 60;

        public Vector3 Position { get; private set; }
        public bool Present { get; private set; }
        int stopIndex = -1;
        GameObject visual;
        CreatureRig rig;
        Obstacle cartObstacle;
        float checkTimer;

        public struct Offer { public string id; public int count, price; public bool egg; }
        public readonly List<Offer> Stock = new List<Offer>();

        static readonly Palette Look = new Palette
        {
            primary = new Color(0.55f, 0.32f, 0.45f),    // coat
            secondary = new Color(0.3f, 0.24f, 0.2f),
            accent = new Color(0.95f, 0.75f, 0.3f),      // hat + pack
            eye = new Color(0.1f, 0.08f, 0.1f),
            glow = new Color(0.86f, 0.66f, 0.52f)
        };

        public void Init() { I = this; }

        public static int SellPrice(ItemDef d)
        {
            if (d == null) return 0;
            switch (d.id)
            {
                case "wood": case "stone": case "fiber": case "berry": return 1;
                case "ore": return 3;
                case "pelt": case "gel": return 4;
                case "ingot": return 10;
                case "crystal": case "cinder": return 15;
                case "berry_tart": return 6;
                case "salve": return 6;
                case "tether_seed": return 2;
                case "ironbark_seed": return 6;
                case "cinder_seed": return 8;
                case "rime_seed": return 10;
                case "sunforged_seed": return 12;
                case "starbloom_seed": return 14;
            }
            if (d.category == ItemCategory.Gear) return d.rarity == Rarity.Common ? 30 : d.rarity == Rarity.Uncommon ? 60 : d.rarity == Rarity.Rare ? 120 : 220;
            return 0;
        }

        public void Tick(float dt, PlayerController pl)
        {
            checkTimer -= dt;
            if (checkTimer > 0f) return;
            checkTimer = 1f;
            var ws = Waystones.I;
            var found = new List<Waystones.Stone>();
            foreach (var s in ws.All) if (ws.IsFound(s)) found.Add(s);
            if (found.Count == 0 || Caves.InCave) { Hide(); return; }
            int idx = (int)(GameManager.WorldTime / StaySeconds);
            var stone = found[idx % found.Count];
            var pos = stone.pos + new Vector3(6.5f, 0f, 5f);
            pos.y = WorldGen.Height(pos.x, pos.z);
            if (idx != stopIndex || (Position - pos).sqrMagnitude > 1f)
            {
                bool moved = stopIndex >= 0 && idx != stopIndex;
                stopIndex = idx;
                Position = pos;
                RollStock(idx);
                Hide();
                if (moved) GameEvents.Say($"{Name} the trader has moved on to {stone.name}.", new Color(1f, 0.85f, 0.5f));
            }
            Present = true;
            bool near = (pl.Position - Position).sqrMagnitude < 140f * 140f;
            if (near && visual == null) Show();
            else if (!near && visual != null) Hide();
            if (rig != null) { var to = pl.Position - Position; to.y = 0; if (to.sqrMagnitude > 0.1f && to.sqrMagnitude < 400f) visual.transform.rotation = Quaternion.Slerp(visual.transform.rotation, Quaternion.LookRotation(to), 0.3f); }
        }

        void RollStock(int seed)
        {
            Stock.Clear();
            var rng = new System.Random(seed * 977 + WorldGen.Seed);
            Stock.Add(new Offer { id = "tether_seed", count = 5, price = 15 });
            Stock.Add(new Offer { id = "ironbark_seed", count = 3, price = 45 });
            Stock.Add(new Offer { id = "salve", count = 2, price = 20 });
            Stock.Add(new Offer { id = "kin_treat", count = 1, price = 35 });
            // a few of the seeds a tier above where you are (Capture/Seeds.cs)
            string up = Seeds.ForLevel((GameManager.I != null ? GameManager.I.Player.Stats.level : 1) + 8);
            if (up != "ironbark_seed" && up != "tether_seed" && rng.NextDouble() < 0.6)
                Stock.Add(new Offer { id = up, count = 2, price = 40 + 15 * Seeds.Rank(ContentDB.Item(up)) });
            if (rng.NextDouble() < 0.35) Stock.Add(new Offer { id = "satchel", count = 1, price = 250 });
            string[] gear = { "explorer_coat", "explorer_boots", "guard_ring", "windrunner_ring", "hearty_amulet", "binders_bracelet", "ironbark_helm", "keen_ring", "featherstep_boots", "glowstone_pendant" };
            for (int i = 0; i < 2; i++)
            {
                var g = ContentDB.Item(gear[rng.Next(gear.Length)]);
                if (g != null && !Stock.Exists(o => o.id == g.id)) Stock.Add(new Offer { id = g.id, count = 1, price = SellPrice(g) * 3 });
            }
            Stock.Add(new Offer { id = "egg", count = 1, price = 300, egg = true });
        }

        public string Buy(Offer o)
        {
            var s = GameManager.I.Player.Stats;
            if (s.glimmer < o.price) return "Not enough Glimmer.";
            if (o.egg)
            {
                if (!KinGrowth.HasHatchery) return "You'll need a Hatchery at camp to keep an egg warm.";
                if (s.eggs.Count >= KinGrowth.MaxEggs) return "Your Hatchery is full.";
                s.glimmer -= o.price;
                s.eggs.Add(KinGrowth.MysteryEgg());
                GameEvents.Good("A mystery egg! It's warming in your Hatchery.");
            }
            else
            {
                if (s.Bag.SpaceFor(o.id) < o.count) return "Bag is full.";
                s.glimmer -= o.price;
                s.Bag.Add(o.id, o.count);
                GameEvents.Good($"Bought {o.count}x {ContentDB.ItemName(o.id)}.");
            }
            SynthAudio.Play(Sfx.Pickup, 1.1f);
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        public void Sell(string id, int count)
        {
            var s = GameManager.I.Player.Stats;
            int have = s.Bag.Count(id);
            count = Mathf.Min(count, have);
            int price = SellPrice(ContentDB.Item(id));
            if (count <= 0 || price <= 0) return;
            s.Bag.Remove(id, count);
            s.glimmer += price * count;
            SynthAudio.Play(Sfx.Pickup, 0.9f);
            GameEvents.RaiseInventoryChanged();
        }

        public bool InReach(Vector3 p) => visual != null && (p - Position).sqrMagnitude < 3.6f * 3.6f;

        void Show()
        {
            visual = new GameObject("Trader");
            visual.transform.SetParent(transform, false);
            visual.transform.position = Position;
            var body = new GameObject("Body");
            body.transform.SetParent(visual.transform, false);
            rig = ModelBuilder.BuildCustom("trader", PlayerLook.Parts, PlayerLook.Pivots, Look, body.transform, RenderAssets.Creature, Locomotion.Biped, 1.05f);
            // cart with goods
            var mb = new MeshBuilder();
            Color32 wood = new Color(0.5f, 0.34f, 0.2f), cloth = new Color(0.85f, 0.35f, 0.3f), crate = new Color(0.62f, 0.45f, 0.26f);
            mb.Box(Matrix4x4.TRS(new Vector3(-2.2f, 0.9f, 0f), Quaternion.identity, new Vector3(1.6f, 0.5f, 2.6f)), wood, crate);
            for (int s = -1; s <= 1; s += 2)
                mb.Cylinder(Matrix4x4.TRS(new Vector3(-2.2f + s * 0.9f, 0.55f, 0.7f), Quaternion.Euler(0, 0, 90), new Vector3(1.1f, 0.15f, 1.1f)), wood, 10, 1f, true);
            mb.Box(Matrix4x4.TRS(new Vector3(-2.2f, 1.45f, -0.6f), Quaternion.identity, new Vector3(0.7f, 0.6f, 0.6f)), crate);
            mb.Sphere(Matrix4x4.TRS(new Vector3(-2.0f, 1.4f, 0.5f), Quaternion.identity, new Vector3(0.7f, 0.5f, 0.7f)), new Color(0.95f, 0.85f, 0.5f), 8, 5);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(-2.2f, 2.3f, 0f), Quaternion.Euler(90, 0, 0), new Vector3(1.9f, 2.8f, 0.5f)), cloth, cloth, 10, 1f, false);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(-1.2f, 2.0f, 1.1f), Quaternion.identity, new Vector3(0.08f, 2.4f, 0.08f)), wood, 5, 1f, true);
            mb.Sphere(Matrix4x4.TRS(new Vector3(-1.2f, 3.25f, 1.1f), Quaternion.identity, new Vector3(0.3f, 0.36f, 0.3f)), new Color(1f, 0.85f, 0.4f), 6, 5);
            var cart = new GameObject("Cart");
            cart.transform.SetParent(visual.transform, false);
            cart.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("trader_cart");
            var mr = cart.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.On;
            cartObstacle = WorldCollision.AddObstacle(Position + new Vector3(-2.2f, 0, 0), 1.5f, this);
        }

        void Hide()
        {
            if (visual != null) Destroy(visual);
            visual = null;
            rig = null;
            if (cartObstacle != null) { WorldCollision.RemoveObstacle(cartObstacle); cartObstacle = null; }
        }
    }
}
