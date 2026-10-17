using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExtendedFleetCommands
{
    /// <summary>
    /// Hooks the fleet ship AI so ships with an extended order follow <see cref="FleetOrders"/>, and
    /// miners follow the <see cref="MiningGear"/> setting.
    /// </summary>
    [HarmonyPatch]
    internal static class FleetAIPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AIMercenary), "SetActions")]
        private static bool SetActions_Prefix(AIMercenary __instance)
        {
            FleetOrder order = FleetOrder.Of(__instance);
            if (order == null)
            {
                return true;
            }
            try
            {
                return FleetOrders.Update(__instance, order);
            }
            catch (Exception e)
            {
                // Hand the ship back to the game's AI rather than leave it stuck.
                Plugin.Log.LogError("Fleet order failed, cancelling it: " + e);
                order.Active = false;
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AIMercenary), "SetNewDestination")]
        private static bool SetNewDestination_Prefix(AIMercenary __instance)
        {
            FleetOrder order = FleetOrder.Of(__instance);
            return order == null || FleetOrders.SetDestination(__instance, order);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.SetNewTarget))]
        private static bool SetNewTarget_Prefix(AIMercenary __instance)
        {
            return !FleetOrders.BlocksTargeting(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.SetRepairTarget))]
        private static bool SetRepairTarget_Prefix(AIMercenary __instance, ref bool __result)
        {
            if (FleetOrders.BlocksTargeting(__instance))
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.SearchForAsteroids))]
        private static bool SearchForAsteroids_Prefix(AIMercenary __instance)
        {
            return !FleetOrders.BlocksTargeting(__instance) && MiningGear.AllowsMining(__instance);
        }

        /// <summary>"Only use mining gear when mining": the ship's own (non-turret) weapons.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AIMercenary), "CanFireWeaponType")]
        private static void CanFireWeaponType_Postfix(AIMercenary __instance, TWeapon weap, ref bool __result)
        {
            if (MiningGear.RestrictedNow(__instance))
            {
                // Mining weapons fire whichever of beams or projectiles the AI is cycling to.
                __result = MiningGear.IsMiningWeapon(weap);
            }
        }

        /// <summary>"Only use mining gear when mining": turret-mounted weapons.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WeaponTurret), "CanFireWeaponType")]
        private static void TurretCanFireWeaponType_Postfix(WeaponTurret __instance, TWeapon weap, ref bool __result)
        {
            if (__result && !MiningGear.TurretMayFire(__instance, weap))
            {
                __result = false;
            }
        }

        /// <summary>"Only use mining gear when mining": drones. AIControl.Update launches them.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AIControl), "Update")]
        private static void AIUpdate_Prefix(AIControl __instance, out List<ActiveEquipment> __state)
        {
            __state = __instance is AIMercenary ship ? MiningGear.LimitDrones(ship) : null;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(AIControl), "Update")]
        private static void AIUpdate_Finalizer(AIControl __instance, List<ActiveEquipment> __state)
        {
            if (__state != null)
            {
                MiningGear.RestoreDrones((AIMercenary)__instance, __state);
            }
        }

        /// <summary>
        /// The AI normally switches its tractor beam on and off by itself in combat. While towing,
        /// the tow order is in charge of it.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AE_TractorBeam), nameof(AE_TractorBeam.ShouldBeActivated))]
        private static void TractorBeamShouldBeActivated_Postfix(AE_TractorBeam __instance, AIControl aiControl, ref bool __result)
        {
            if (aiControl is AIMercenary ship && FleetOrder.Of(ship)?.Kind == OrderKind.Tow)
            {
                __result = __instance.active;
            }
        }
    }
}
