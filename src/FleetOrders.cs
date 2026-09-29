using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedFleetCommands
{
    /// <summary>
    /// The AI rules for the extended orders. They hook into <see cref="AIMercenary"/>, the AI that
    /// player fleet ships and hired escorts run:
    ///
    /// <list type="bullet">
    /// <item><b>Tow</b>: fly to the object, lock the ship's own Tractor Beam on it, then follow the
    /// player as usual with the object in tow. The ship doesn't pick fights while towing.</item>
    /// <item><b>Load</b>: fly to the loot and pull it into the ship's own cargo hold, the same way
    /// fleet ships with "Collect loot" on pick things up. The ship doesn't pick fights on the way.</item>
    /// <item><b>Defend</b>: stay with a friendly ship or station instead of the player, attack whatever
    /// it is fighting or whatever threatens it, and (repair role) keep it repaired.</item>
    /// <item><b>Escort</b>: follow a ship instead of the player, trailing behind it, without picking
    /// fights. With "Collect loot" on, also pick up loot that drops near it (ore from a miner, say).</item>
    /// </list>
    ///
    /// Any direct attack order from the fleet controls, docking, or an emergency warp ends the
    /// extended order and the ship goes back to its normal behaviour.
    /// </summary>
    internal static class FleetOrders
    {
        private const float TowRangeMargin = 12f;
        private const float LoadReach = 25f;
        private const float GuardDistance = 30f;

        public static void Issue(AIMercenary ship, OrderKind kind, Transform target)
        {
            Cancel(ship);
            FleetOrder order = FleetOrder.GetOrAdd(ship);
            order.Active = true;
            order.Kind = kind;
            order.Target = target;
            order.TargetName = OrderTargets.DisplayName(target);
            order.Attached = false;
            float clearance = OrderTargets.Radius(target) + OrderTargets.Radius(ship.transform);
            order.StandOff = clearance + 5f;
            order.GuardOffset = kind == OrderKind.Escort
                // Behind and off to one side, relative to the escorted ship's heading.
                ? Quaternion.Euler(0f, 180f + Random.Range(25f, 50f) * (Random.value < 0.5f ? -1f : 1f), 0f) * Vector3.forward * (clearance + GuardDistance)
                // Anywhere around the defended ship or station.
                : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * (clearance + GuardDistance);
            order.Loot = null;
            order.SkippedLoot.Clear();
            order.HoldFull = false;

            // Drop whatever it was doing.
            ship.mainTarget = null;
            ship.ForgetTarget(checkTargetLastPosition: false);
            ship.mining = false;
            ship.repairingTarget = false;
            ship.returningToBoss = false;
            ship.destination = Vector3.zero;
            if (kind == OrderKind.Tow)
            {
                ReleaseTractorBeam(ship, onlyFrom: null);
            }
        }

        public static void Cancel(AIMercenary ship)
        {
            FleetOrder order = FleetOrder.Of(ship);
            if (order == null)
            {
                return;
            }
            if (order.Kind == OrderKind.Tow)
            {
                ReleaseTractorBeam(ship, order.Target);
            }
            order.Active = false;
            order.Target = null;
        }

        public static AE_TractorBeam FindTractorBeam(AIMercenary ship)
        {
            List<ActiveEquipment> equipment = ship.GetSS().activeEquips;
            return equipment?.OfType<AE_TractorBeam>().FirstOrDefault();
        }

        /// <summary>Runs before AIMercenary.SetActions. Returns false to skip the game's own AI update.</summary>
        public static bool Update(AIMercenary ship, FleetOrder order)
        {
            // Docking, emergency warps and direct attack orders take over from extended orders.
            // mainTarget is only set for fleet ships by the fleet controls' attack orders.
            if (ship.carrierDocking || ship.targetDocking != null || ship.eWarping || !ship.isGuardingPlayer || ship.mainTarget != null)
            {
                Cancel(ship);
                return true;
            }
            switch (order.Kind)
            {
                case OrderKind.Tow:
                    UpdateTow(ship, order);
                    return true;
                case OrderKind.Load:
                    UpdateLoad(ship, order);
                    return true;
                case OrderKind.Escort:
                    UpdateEscort(ship, order);
                    return true;
                default:
                    return !UpdateDefend(ship, order);
            }
        }

        /// <summary>Runs before AIMercenary.SetNewDestination. Returns false when it set the destination.</summary>
        public static bool SetDestination(AIMercenary ship, FleetOrder order)
        {
            if (ship.carrierDocking || ship.targetDocking != null || order.Target == null)
            {
                return true;
            }
            Vector3 targetPosition = order.Target.position;
            switch (order.Kind)
            {
                case OrderKind.Tow:
                    if (order.Attached)
                    {
                        // Keep station with the player; the tow line drags the object along.
                        return true;
                    }
                    AE_TractorBeam beam = FindTractorBeam(ship);
                    float standOff = beam != null ? Mathf.Min(order.StandOff, TowRange(beam) - 5f) : order.StandOff;
                    ship.destination = ApproachPoint(ship, targetPosition, standOff);
                    return false;
                case OrderKind.Load:
                    ship.destination = ApproachPoint(ship, targetPosition, order.StandOff);
                    return false;
                case OrderKind.Escort:
                    if (order.Loot != null)
                    {
                        ship.destination = ApproachPoint(ship, order.Loot.position, order.StandOff);
                        return false;
                    }
                    Vector3 trail = order.Target.rotation * order.GuardOffset;
                    trail.y = 0f;
                    ship.destination = targetPosition + trail;
                    return false;
                default:
                    ship.destination = targetPosition + order.GuardOffset;
                    return false;
            }
        }

        /// <summary>Tow and load orders keep the ship out of fights and off asteroids.</summary>
        public static bool BlocksTargeting(AIMercenary ship)
        {
            FleetOrder order = FleetOrder.Of(ship);
            return order != null && order.Kind != OrderKind.Defend;
        }

        private static void UpdateTow(AIMercenary ship, FleetOrder order)
        {
            if (order.Target == null)
            {
                Report(ship, Loc.MsgTowEnded, order.TargetName);
                Cancel(ship);
                return;
            }
            AE_TractorBeam beam = FindTractorBeam(ship);
            if (beam == null)
            {
                Report(ship, Loc.MsgNoTractorBeam, order.TargetName);
                Cancel(ship);
                return;
            }
            if (beam.active && BeamTarget(beam) != order.Target)
            {
                // Busy with something else, such as the enemy it was fighting before the order.
                ReleaseTractorBeam(ship, onlyFrom: null);
            }
            if (beam.active)
            {
                return;
            }
            // Not attached yet, or the line broke (out of range or out of energy): close in and try again.
            order.Attached = false;
            if (Vector3.Distance(ship.transform.position, order.Target.position) > TowRange(beam))
            {
                return;
            }
            beam.ActivateDeactivate(shiftPressed: false, order.Target);
            if (beam.active)
            {
                order.Attached = true;
                Report(ship, Loc.MsgTowAttached, order.TargetName);
            }
        }

        private static void UpdateLoad(AIMercenary ship, FleetOrder order)
        {
            Collectible collectible = order.Target != null ? order.Target.GetComponent<Collectible>() : null;
            if (collectible == null)
            {
                Report(ship, Loc.MsgLoadTargetGone, order.TargetName);
                Cancel(ship);
                return;
            }
            if (!OrderTargets.FitsInHold(collectible, ship))
            {
                Report(ship, Loc.MsgTooLargeToLoad, order.TargetName);
                Cancel(ship);
                return;
            }
            // Loot can't be picked up for a moment after it drops, or while a beam is pulling it.
            if (!collectible.collectEnabled || collectible.beingTowed)
            {
                return;
            }
            if (Vector3.Distance(ship.transform.position, order.Target.position) > order.StandOff + LoadReach)
            {
                return;
            }
            switch (Collect(ship, collectible))
            {
                case CollectResult.All:
                    Report(ship, Loc.MsgLoaded, order.TargetName);
                    break;
                case CollectResult.Part:
                    Report(ship, Loc.MsgLoadedPartly, order.TargetName);
                    break;
                default:
                    Report(ship, Loc.MsgNoCargoSpace, order.TargetName);
                    break;
            }
            Cancel(ship);
        }

        private static void UpdateEscort(AIMercenary ship, FleetOrder order)
        {
            Entity escorted = order.Target != null ? order.Target.GetComponent<Entity>() : null;
            if (escorted == null || !escorted.gameObject.activeInHierarchy)
            {
                Report(ship, Loc.MsgEscortTargetGone, order.TargetName);
                Cancel(ship);
                return;
            }
            if (OrderTargets.IsHostile(ship, escorted))
            {
                Report(ship, Loc.MsgEscortTargetHostile, order.TargetName);
                Cancel(ship);
                return;
            }
            // Picking up loot follows the ship's own "Collect loot" setting.
            if (!ship.aiMercChar.behavior.collectLoot || order.HoldFull)
            {
                order.Loot = null;
                return;
            }

            Collectible loot = order.Loot != null ? order.Loot.GetComponent<Collectible>() : null;
            if (loot == null)
            {
                loot = OrderTargets.FindNearestLoadable(ship, escorted.transform.position, Plugin.EscortCollectRadius.Value, order.SkippedLoot);
                order.Loot = loot != null ? loot.transform : null;
                if (loot == null)
                {
                    return;
                }
                order.StandOff = OrderTargets.Radius(loot.transform) + OrderTargets.Radius(ship.transform) + 5f;
            }
            if (!loot.collectEnabled || loot.beingTowed
                || Vector3.Distance(ship.transform.position, loot.transform.position) > order.StandOff + LoadReach)
            {
                return;
            }
            CollectResult result = Collect(ship, loot);
            order.Loot = null;
            if (result == CollectResult.All)
            {
                return;
            }
            if (result == CollectResult.Part || ship.GetComponent<CargoSystem>().FreeSpace(passengers: false) < 1f)
            {
                // Keep escorting, but stop collecting until a new order.
                order.HoldFull = true;
                Report(ship, Loc.MsgEscortHoldFull, order.TargetName);
            }
            else
            {
                // The hold has room, so a loot filter or item rule turned it down; don't retry it.
                order.SkippedLoot.Add(loot.GetInstanceID());
            }
        }

        private enum CollectResult
        {
            All,
            Part,
            None,
        }

        private static CollectResult Collect(AIMercenary ship, Collectible collectible)
        {
            int before = collectible.qnt;
            ship.GetComponent<CargoSystem>().CollectItem(collectible.gameObject);
            // CollectItem clears collectEnabled when it takes everything, lowers qnt when it takes
            // part, and changes nothing when the hold is full.
            if (!collectible.collectEnabled)
            {
                return CollectResult.All;
            }
            return collectible.qnt < before ? CollectResult.Part : CollectResult.None;
        }

        /// <summary>
        /// Replaces AIMercenary.SetActions while defending. The game's version keeps the ship near the
        /// player and only answers threats to the player, so it can't be reused here.
        /// Returns false if the order ended and the game's AI should run instead.
        /// </summary>
        private static bool UpdateDefend(AIMercenary ship, FleetOrder order)
        {
            Entity defended = order.Target != null ? order.Target.GetComponent<Entity>() : null;
            if (defended == null || !defended.gameObject.activeInHierarchy || defended.IsDestroyedOrWreckedStation)
            {
                Report(ship, Loc.MsgDefendTargetGone, order.TargetName);
                Cancel(ship);
                return false;
            }
            if (OrderTargets.IsHostile(ship, defended))
            {
                Report(ship, Loc.MsgDefendTargetHostile, order.TargetName);
                Cancel(ship);
                return false;
            }

            // The upkeep AIMercenary.SetActions does every update.
            SpaceShip ss = ship.GetSS();
            ref int broadcastCooldown = ref GameAccess.BroadcastCooldown(ship);
            if (broadcastCooldown > 0)
            {
                broadcastCooldown--;
            }
            if (ship.changeTargetTime > 0)
            {
                ship.changeTargetTime--;
            }
            GameAccess.ReduceNotAttackingTime(ship);
            GameAccess.CheckDamageControl(ship);
            if (ship.carrierDocking || ship.eWarping || !ship.isGuardingPlayer)
            {
                // Low hull: it's heading for the carrier hangar or has warped out.
                Cancel(ship);
                return true;
            }
            if (!ship.aiMercChar.emergencyWarpReady && ss.FullHP)
            {
                ship.aiMercChar.emergencyWarpReady = true;
            }
            if (ss.ffSys == null || ss.ffSys.ss == null)
            {
                ss.RefreshFriendlyFireSystem();
            }
            // Being far from the player is the point of the order, so never head back.
            ship.returningToBoss = false;

            if (ship.target == null)
            {
                if (ship.aiMercChar.behavior.role == 1)
                {
                    if (ship.canRepair && defended.NeedRepairs)
                    {
                        ship.SetRepairTarget(defended);
                    }
                }
                else if (ship.canDealDmg && ship.NotAttackingTime <= 0)
                {
                    Transform threat = FindThreat(ship, defended);
                    if (threat != null)
                    {
                        ship.SetNewTarget(threat, startedFight: true);
                    }
                }
                return true;
            }

            GameAccess.VerifyTargetStatus(ship);
            // Don't chase attackers far away from the ship it's meant to protect.
            if (ship.target != null && !ship.repairingTarget
                && Vector3.Distance(ship.target.position, defended.transform.position) > Plugin.DefendRadius.Value * 2f)
            {
                ship.ForgetTarget(checkTargetLastPosition: false);
            }
            if (ship.target != null)
            {
                ship.VerifyChangeTactic(20);
            }
            return true;
        }

        /// <summary>
        /// First whatever the defended ship or station is fighting, then anything attacking it,
        /// then the nearest hostile within <see cref="Plugin.DefendRadius"/> of it.
        /// </summary>
        private static Transform FindThreat(AIMercenary ship, Entity defended)
        {
            Transform defendedTransform = defended.transform;
            Transform fighting = null;
            if (defended.IsAI && defended.AIControl != null && !defended.AIControl.repairingTarget)
            {
                fighting = defended.AIControl.target;
            }
            else if (defended.IsStation && defended.GetAIStationControl != null)
            {
                fighting = defended.GetAIStationControl.target;
            }
            if (fighting != null && fighting != ship.transform)
            {
                Entity entity = fighting.GetComponent<Entity>();
                if (entity != null && !entity.IsPlayer && OrderTargets.IsHostile(ship, entity))
                {
                    return fighting;
                }
            }

            List<ScanObject> hostiles = ship.objectsScan?.hostileObjects;
            if (hostiles == null)
            {
                return null;
            }
            Transform nearest = null;
            float nearestDistance = Plugin.DefendRadius.Value;
            foreach (ScanObject scanObject in hostiles)
            {
                if (scanObject == null || !scanObject.IsActive)
                {
                    continue;
                }
                Entity entity = scanObject.GetEntity();
                if (entity == null || entity.IsPlayer)
                {
                    continue;
                }
                bool attacking = (entity.IsAI && entity.AIControl.target == defendedTransform && !entity.AIControl.repairingTarget)
                    || (entity.IsStation && entity.GetAIStationControl.target == defendedTransform);
                if (attacking)
                {
                    return scanObject.trans;
                }
                float distance = Vector3.Distance(scanObject.trans.position, defendedTransform.position);
                if (distance < nearestDistance && OrderTargets.IsHostile(ship, entity))
                {
                    nearest = scanObject.trans;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        /// <summary>
        /// The AI stops once it is destCheckRange from its destination, so aim that far beyond the
        /// point <paramref name="standOff"/> short of the target to stop right there.
        /// </summary>
        private static Vector3 ApproachPoint(AIMercenary ship, Vector3 targetPosition, float standOff)
        {
            Vector3 direction = targetPosition - ship.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return targetPosition;
            }
            return targetPosition + direction.normalized * (GameAccess.DestCheckRange(ship) - standOff);
        }

        /// <summary>How close the ship must be for the tractor beam to lock on.</summary>
        private static float TowRange(AE_TractorBeam beam)
        {
            return GameAccess.TractorMaxDistance(beam) - TowRangeMargin;
        }

        private static Transform BeamTarget(AE_TractorBeam beam)
        {
            GameObject buff = beam.GetCurrBuffGO;
            return buff != null ? buff.transform.parent : null;
        }

        /// <summary>Switches the tractor beam off, if it is on (and, with onlyFrom, attached to that object).</summary>
        private static void ReleaseTractorBeam(AIMercenary ship, Transform onlyFrom)
        {
            AE_TractorBeam beam = FindTractorBeam(ship);
            if (beam == null || !beam.active || (onlyFrom != null && BeamTarget(beam) != onlyFrom))
            {
                return;
            }
            beam.ActivateDeactivate(shiftPressed: false, null);
        }

        public static void Report(AIMercenary ship, int code, string targetName)
        {
            SideInfo.AddMsg(Loc.Get(code, ship.aiMercChar.CommanderName(12), targetName));
        }
    }
}
