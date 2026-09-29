using System;
using HarmonyLib;
using UnityEngine;

namespace ExtendedFleetCommands
{
    /// <summary>Hooks the fleet ship AI so ships with an extended order follow <see cref="FleetOrders"/>.</summary>
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
            return !FleetOrders.BlocksTargeting(__instance);
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
