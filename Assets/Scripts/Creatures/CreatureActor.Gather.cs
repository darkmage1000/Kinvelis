using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Companion gather assist: while the player chops, mines, harvests or picks things up in the wild, a companion
    /// whose species has the matching camp job pitches in - on the same node first, then on the nearest one of the
    /// same kind for as long as the player keeps working. Everything it gathers goes to the player.
    /// Nodes give the same total whoever hits them, so this makes gathering faster, not richer.
    /// </summary>
    public partial class CreatureActor
    {
        /// <summary>Keeps helping this long after the player's last gathering hit.</summary>
        const float GatherAssistWindow = 4f;
        const float GatherSearchRadius = 8f, GatherSwingTime = 1f;

        ResourceNode gatherNode;
        float gatherSwing;
        float gatherHintTime = -999f;
        static readonly List<ResourceNode> gatherBuf = new List<ResourceNode>(32);

        /// <summary>The camp job that covers a wild node: trees = Logging, rocks = Quarrying, plants = Foraging / Cultivating, loose pickups = Hauling / Foraging.</summary>
        public int GatherLevel(PropInfo info)
        {
            if (info.pickup) return Mathf.Max(Data.WorkLevel(WorkType.Hauling), Data.WorkLevel(WorkType.Foraging));
            switch (info.tool)
            {
                case ToolKind.Axe: return Data.WorkLevel(WorkType.Logging);
                case ToolKind.Pick: return Data.WorkLevel(WorkType.Quarrying);
                case ToolKind.Sickle: return Mathf.Max(Data.WorkLevel(WorkType.Foraging), Data.WorkLevel(WorkType.Cultivating));
                default: return 0;
            }
        }

        static bool SameGatherKind(PropInfo a, PropInfo b) => a.pickup == b.pickup && (a.pickup || a.tool == b.tool);

        /// <summary>Hard nodes need a skilled kin: Lv 1 handles what a stone tool can, Lv 2+ handles crystal-hard rock.</summary>
        static bool CanBreak(PropInfo info, int level) => info.pickup || level + 1 >= info.hardness;

        static string GatherVerb(PropInfo info) =>
            info.pickup ? "gather" : info.tool == ToolKind.Axe ? "chop" : info.tool == ToolKind.Pick ? "mine" : "harvest";

        ResourceNode PickGatherNode(PlayerController player, ResourceNode playerNode, PropInfo kind, int level)
        {
            // the node you're on first
            if (playerNode.alive && CanBreak(playerNode.Info, level)) return playerNode;
            var cm = ChunkManager.I;
            if (cm == null) return null;
            gatherBuf.Clear();
            cm.Nodes.Query(player.Position, GatherSearchRadius + 2f, gatherBuf);
            ResourceNode best = null; float bd = float.MaxValue;
            for (int i = 0; i < gatherBuf.Count; i++)
            {
                var n = gatherBuf[i];
                if (!n.alive || !SameGatherKind(n.Info, kind) || !CanBreak(n.Info, level)) continue;
                if (FlatDist(n.pos, player.Position) > GatherSearchRadius) continue;
                float d = FlatDist(n.pos, transform.position);
                if (d < bd) { bd = d; best = n; }
            }
            return best;
        }

        /// <summary>Returns true while the companion is busy helping (so it doesn't walk back to its follow slot).</summary>
        bool TickGatherAssist(float dt, PlayerController player)
        {
            var last = player.LastGatherNode;
            if (last == null || Time.time - player.LastGatherTime > GatherAssistWindow || player.IsMounted || !player.IsAlive)
            {
                gatherNode = null;
                return false;
            }
            var kind = last.Info;
            int level = GatherLevel(kind);
            if (level <= 0) { gatherNode = null; return false; }

            if (gatherNode == null || !gatherNode.alive || !SameGatherKind(gatherNode.Info, kind) || FlatDist(gatherNode.pos, player.Position) > GatherSearchRadius + 4f)
                gatherNode = PickGatherNode(player, last, kind, level);
            if (gatherNode == null) return false;

            if (Time.time - gatherHintTime > 90f)
            {
                gatherHintTime = Time.time;
                GameEvents.Say($"{Data.DisplayName} helps you {GatherVerb(kind)}.", new Color(0.7f, 1f, 0.8f));
            }

            float reach = gatherNode.Radius + BodyRadius + 0.6f;
            if (FlatDist(transform.position, gatherNode.pos) > reach)
            {
                State = AIState.Follow;
                MoveTo(gatherNode.pos, RunSpeed);
                gatherSwing = Mathf.Max(gatherSwing, 0.25f);
                return true;
            }

            // in place: face it and work
            State = AIState.Working;
            faceTarget = gatherNode.pos;
            gatherSwing -= dt;
            if (gatherSwing > 0f) return true;
            gatherSwing = GatherSwingTime / Mathf.Clamp(Data.WorkMul, 0.6f, 1.6f);
            Rig.Lunge();
            var info = gatherNode.Info;
            if (info.pickup) player.KinPickUp(gatherNode);
            else if (info.Plant) player.KinHitNode(gatherNode, level >= 3 ? 2f : 1f);
            else player.KinHitNode(gatherNode, 0.5f + 0.5f * level);   // Lv 1 = 1, Lv 4 = 2.5 (a stone tool hits for 2)
            if (!gatherNode.alive) gatherNode = null;
            return true;
        }
    }
}
