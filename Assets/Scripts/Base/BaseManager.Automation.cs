using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp automation tier 2. A Volt Pylon staffed by a Charging kin powers the camp; powered machines then run
    /// with no workers: the Conveyor Hub moves every producer's output into storage, the Auto-Assembler works
    /// Workbench and Campfire orders, and a Sprinkler waters the plots and boosts farms +40%.
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

        /// <summary>A kin is at a Volt Pylon, or there is stored charge left (BaseManager.Power).</summary>
        public bool CampPowered => PylonStaffed || (Charge > 0f && ChargeCapacity > 0f);

        /// <summary>Farm work (Cultivating / Foraging) x1.4 while a sprinkler runs.</summary>
        public float IrrigationMul(Building target)
        {
            if (target == null) return 1f;
            var w = target.Def.workType;
            if (w != WorkType.Cultivating && w != WorkType.Foraging) return 1f;
            return SprinklerRunning ? 1.4f : 1f;
        }

        /// <summary>Runs near and far (and during offline catch-up, via SimulateAbstract's caller).</summary>
        void TickAutomation(float dt)
        {
            automationTimer -= dt;
            if (automationTimer > 0f) return;
            float step = AutomationStep - automationTimer;
            automationTimer = AutomationStep;
            // (greenhouses tending themselves and sprinklers watering are in TickStages, every frame)
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
                // as good as the best bench of each kind in camp
                AddCraftWork(CraftStation.Workbench, AssemblerRate * Pace * step, CampTier(CraftStation.Workbench));
                AddCraftWork(CraftStation.Campfire, AssemblerRate * Pace * step, CampTier(CraftStation.Campfire));
            }
        }

        const string NoPowerNight = "No power until dawn - the charge ran out and the Volt Pylon's kin is asleep";

        /// <summary>Status text for the automation buildings (used by Status()).</summary>
        bool AutomationStatus(Building b, out BuildingHealth h, out string text)
        {
            h = BuildingHealth.Info; text = null;
            switch (b.Def.id)
            {
                case "greenhouse":
                    if (b.AtStart && b.StartBlock() != null) { h = BuildingHealth.Blocked; text = b.StartBlock(); }
                    else { h = BuildingHealth.Good; text = $"{b.StageText(true)} · {ContentDB.ItemName(b.OutputItem)}, tending itself"; }
                    return true;
                case "kin_clinic": h = BuildingHealth.Good; text = "Fainted kin recover 3x sooner"; return true;
                case GameManager.HealingSpring: h = BuildingHealth.Good; text = "Tap HEAL PARTY here: your whole party back to full"; return true;
                case "training_yard": h = BuildingHealth.Good; text = "Camp kin learn twice as fast"; return true;
                case "waypad": h = BuildingHealth.Good; text = "Outposts are fast-travel destinations"; return true;
                case "research_bench":
                    if (Tech.Active == null) { h = BuildingHealth.Idle; text = "Nothing being researched"; }
                    else { h = BuildingHealth.Good; text = $"Researching {Tech.Active.name} ({Mathf.RoundToInt(Tech.Progress01 * 100f)}%)"; }
                    return true;
                case "volt_turret":
                    if (CampPowered) { h = BuildingHealth.Good; text = "Guarding the camp"; }
                    else if (PowerAsleep) { h = BuildingHealth.Idle; text = NoPowerNight; }
                    else { h = BuildingHealth.Blocked; text = "No power - staff a Volt Pylon"; }
                    return true;
                case "volt_pylon":
                    if (b.Powered) { h = BuildingHealth.Good; text = Charge >= ChargeCapacity - 0.5f ? $"Powering the camp · fully charged ({WorkStages.Clock(Charge)} stored)" : $"Powering the camp · charging, {ChargeText}"; }
                    else if (Charge > 0f) { h = BuildingHealth.Good; text = $"Running on stored charge · {WorkStages.Clock(Charge)} left ({Mathf.RoundToInt(Charge01 * 100f)}%)" + (NightNow ? "" : PylonWantsKin ? " · waiting for a Charging kin" : " · its kin is free until it is half empty"); }
                    else if (PowerAsleep) { h = BuildingHealth.Idle; text = "Out of charge and its kin is asleep - no power until dawn (build a Volt Battery to store more)"; }
                    else { h = BuildingHealth.Blocked; text = "Needs a Charging kin"; }
                    return true;
                case "volt_battery":
                    if (!Has("volt_pylon")) { h = BuildingHealth.Blocked; text = "Needs a Volt Pylon to charge it"; }
                    else { h = Charge > 0f ? BuildingHealth.Good : BuildingHealth.Idle; text = $"Camp charge {ChargeText} (holds {WorkStages.Clock(ChargeCapacity)})"; }
                    return true;
                case "conveyor_hub":
                    if (CampPowered) { h = BuildingHealth.Good; text = "Moving goods to storage"; }
                    else if (PowerAsleep) { h = BuildingHealth.Idle; text = NoPowerNight; }
                    else { h = BuildingHealth.Blocked; text = "No power - staff a Volt Pylon"; }
                    return true;
                case "auto_assembler":
                    if (!CampPowered && PowerAsleep) { h = BuildingHealth.Idle; text = NoPowerNight; }
                    else if (!CampPowered) { h = BuildingHealth.Blocked; text = "No power - staff a Volt Pylon"; }
                    else if (HasOrderFor(CraftStation.Workbench, CampTier(CraftStation.Workbench)) || HasOrderFor(CraftStation.Campfire, CampTier(CraftStation.Campfire))) { h = BuildingHealth.Good; text = "Assembling orders"; }
                    else { h = BuildingHealth.Info; text = "Powered - no orders queued"; }
                    return true;
                case "sprinkler":
                    if (CampPowered) { h = BuildingHealth.Good; text = "Watering the plots · farms +40%"; }
                    else if (PowerAsleep) { h = BuildingHealth.Idle; text = NoPowerNight; }
                    else { h = BuildingHealth.Blocked; text = Has("volt_pylon") ? "No power - the Volt Pylon needs charging" : "No power - build a Volt Pylon"; }
                    return true;
            }
            return false;
        }
    }
}
