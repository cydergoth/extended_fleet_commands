using System;
using HarmonyLib;

namespace ExtendedFleetCommands
{
    /// <summary>Non-public game members the mod needs, bound once through Harmony's AccessTools.</summary>
    internal static class GameAccess
    {
        public static readonly AccessTools.FieldRef<AIControl, int> BroadcastCooldown =
            AccessTools.FieldRefAccess<AIControl, int>("broadcastCooldown");

        public static readonly Action<AIControl> ReduceNotAttackingTime =
            AccessTools.MethodDelegate<Action<AIControl>>(AccessTools.Method(typeof(AIControl), "ReduceNotAttackingTime"));

        public static readonly Action<AIControl> VerifyTargetStatus =
            AccessTools.MethodDelegate<Action<AIControl>>(AccessTools.Method(typeof(AIControl), "VerifyTargetStatus"));

        public static readonly Action<AIMercenary> CheckDamageControl =
            AccessTools.MethodDelegate<Action<AIMercenary>>(AccessTools.Method(typeof(AIMercenary), "CheckDamageControl"));

        /// <summary>How close the AI has to get to its destination before it stops.</summary>
        public static readonly Func<AIControl, float> DestCheckRange =
            AccessTools.MethodDelegate<Func<AIControl, float>>(AccessTools.PropertyGetter(typeof(AIControl), "destCheckRange"));

        public static readonly AccessTools.FieldRef<AE_TractorBeam, float> TractorMaxDistance =
            AccessTools.FieldRefAccess<AE_TractorBeam, float>("maxDistance");

        /// <summary>The ship a weapon turret belongs to, if it is an AI ship.</summary>
        public static readonly AccessTools.FieldRef<WeaponTurret, AIControl> TurretOwner =
            AccessTools.FieldRefAccess<WeaponTurret, AIControl>("aiControl");

        /// <summary>The fleet ship the behaviour panel is showing.</summary>
        public static readonly AccessTools.FieldRef<FleetBehaviorControl, AIMercenaryCharacter> BehaviorShip =
            AccessTools.FieldRefAccess<FleetBehaviorControl, AIMercenaryCharacter>("aiMercChar");
    }
}
