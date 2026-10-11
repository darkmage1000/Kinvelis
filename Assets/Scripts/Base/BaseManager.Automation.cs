using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp automation tier 2. A Volt Pylon staffed by a Charging kin powers the camp; powered machines then run
    /// with no workers: the Conveyor Hub moves every producer's output into storage, the Auto-Assembler works
    /// Workbench and Campfire orders, and a Sprinkler (powered, or staffed by an Irrigating kin) boosts farms +40%.
    /// </summary>
    public partial class BaseManager
    {
        float automationTimer;
        const float AutomationStep = 4f;
        const float AssemblerRate = 1.2f;   // craft work per second (about one skilled crafter)

        bool Has(string id) { foreach (var b in Buildings) if (b.Def.id == id) return true; return false; }
        public bool HasBuilding(string id) => Has(id);

        const float GreenhouseRate = 0.5f;   // work per second with nobody tending it
        const float TurretRange = 20f, TurretPeriod = 1.4f;
        float turretTimer;

        /// <summary>Volt Turrets: while the camp is powered, each one shocks the nearest hostile kin in reach.</summary>
        void TickTurrets(float dt, bool near)
        {
            if (!near) return;
            turretTimer -= dt;
            if (turretTimer > 0f) return;
            turretTimer = TurretPeriod;
            if (!Has("volt_turret") || !CampPowered) return;
            foreach (var b in Buildings)
            {
                if (b.Def.id != "volt_turret") continue;
                var foe = FindIntruder(b.transform.position);
                if (foe == null) continue;
                Vector3 d = foe.Position - b.transform.position; d.y = 0;
                if (d.magnitude > TurretRange) continue;
                Vector3 tip = b.transform.position + Vector3.up * 3.4f;
                Vector3 hit = foe.Position + Vector3.up * foe.Radius;
                var col = new Color(1f, 0.95f, 0.4f);
                for (int i = 1; i <= 6; i++) Fx.Mote(Vector3.Lerp(tip, hit, i / 6f) + UnityEngine.Random.insideUnitSphere * 0.25f, Vector3.zero, col, 0.22f, 0.18f, true);
                Fx.Burst(hit, col, 10, 4f, 0.3f, 0.35f, true);
                SynthAudio.Play(Sfx.Shoot, 1.4f, 0.35f);
                foe.ReceiveHit(new HitInfo { attacker = null, damage = 10f + foe.MaxHp * 0.05f, element = Element.Volt, status = StatusType.Shock, statusChance = 0.3f, effectiveness = 1f });
            }
        }

        /// <summary>Any staffed Volt Pylon.</summary>
        public bool CampPowered
        {
            get { foreach (var b in Buildings) if (b.Def.id == "volt_pylon" && b.Powered) return true; return false; }
        }

        /// <summary>Farm work (Cultivating / Foraging) x1.4 while a sprinkler runs.</summary>
        public float IrrigationMul(Building target)
        {
            if (target == null) return 1f;
            var w = target.Def.workType;
            if (w != WorkType.Cultivating && w != WorkType.Foraging) return 1f;
            foreach (var b in Buildings)
                if (b.Def.id == "sprinkler" && (b.Powered || CampPowered)) return 1.4f;
            return 1f;
        }

        /// <summary>Runs near and far (and during offline catch-up, via SimulateAbstract's caller).</summary>
        void TickAutomation(float dt)
        {
            automationTimer -= dt;
            if (automationTimer > 0f) return;
            float step = AutomationStep - automationTimer;
            automationTimer = AutomationStep;
            // greenhouses grow on their own, powered or not
            foreach (var b in Buildings)
                if (b.Def.id == "greenhouse" && b.BufferCount < b.Def.bufferCap) b.AddWork(GreenhouseRate * WorldRules.Current.workSpeed * step);
            if (!CampPowered) return;

            if (Has("conveyor_hub"))
            {
                bool moved = false;
                foreach (var b in Buildings)
                {
                    if (!b.IsProducer || b.BufferCount <= 0) continue;
                    int n = b.TakeFromBuffer(b.BufferCount, out string item);
                    int left = Storage.Add(item, n);
                    if (left > 0) b.ReturnToBuffer(item, left);
                    moved |= n - left > 0;
                }
                if (moved) GameEvents.RaiseInventoryChanged();
            }
            if (Has("auto_assembler"))
            {
                AddCraftWork(CraftStation.Workbench, AssemblerRate * step);
                AddCraftWork(CraftStation.Campfire, AssemblerRate * step);
            }
        }

        /// <summary>Status text for the automation buildings (used by Status()).</summary>
        bool AutomationStatus(Building b, out BuildingHealth h, out string text)
        {
            h = BuildingHealth.Info; text = null;
            switch (b.Def.id)
            {
                case "greenhouse":
                    if (b.BufferCount >= b.Def.bufferCap) { h = BuildingHealth.Blocked; text = "Output full - collect it or add haulers"; }
                    else { h = BuildingHealth.Good; text = $"Growing {ContentDB.ItemName(b.OutputItem)} on its own"; }
                    return true;
                case "kin_clinic": h = BuildingHealth.Good; text = "Fainted kin recover 3x sooner"; return true;
                case "training_yard": h = BuildingHealth.Good; text = "Camp kin learn twice as fast"; return true;
                case "waypad": h = BuildingHealth.Good; text = "Outposts are fast-travel destinations"; return true;
                case "research_bench":
                    if (Tech.Active == null) { h = BuildingHealth.Idle; text = "Nothing being researched"; }
                    else { h = BuildingHealth.Good; text = $"Researching {Tech.Active.name} ({Mathf.RoundToInt(Tech.Progress01 * 100f)}%)"; }
                    return true;
                case "volt_turret":
                    if (CampPowered) { h = BuildingHealth.Good; text = "Guarding the camp"; }
                    else { h = BuildingHealth.Blocked; text = "No power - staff a Volt Pylon"; }
                    return true;
                case "volt_pylon":
                    if (b.Powered) { h = BuildingHealth.Good; text = "Powering the camp"; }
                    else { h = BuildingHealth.Blocked; text = "Needs a Charging kin"; }
                    return true;
                case "conveyor_hub":
                    if (CampPowered) { h = BuildingHealth.Good; text = "Moving goods to storage"; }
                    else { h = BuildingHealth.Blocked; text = "No power - staff a Volt Pylon"; }
                    return true;
                case "auto_assembler":
                    if (!CampPowered) { h = BuildingHealth.Blocked; text = "No power - staff a Volt Pylon"; }
                    else if (HasOrderFor(CraftStation.Workbench) || HasOrderFor(CraftStation.Campfire)) { h = BuildingHealth.Good; text = "Assembling orders"; }
                    else { h = BuildingHealth.Info; text = "Powered - no orders queued"; }
                    return true;
                case "sprinkler":
                    if (b.Powered || CampPowered) { h = BuildingHealth.Good; text = "Farms +40%"; }
                    else { h = BuildingHealth.Blocked; text = "Needs power or an Irrigating kin"; }
                    return true;
            }
            return false;
        }
    }
}
