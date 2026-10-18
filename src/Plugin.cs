using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExtendedFleetCommands
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "starvalor.extendedfleetcommands";
        public const string Name = "Extended Fleet Commands";
        public const string Version = "1.1.1";

        internal static ManualLogSource Log;
        internal static ConfigEntry<float> DefendRadius;
        internal static ConfigEntry<float> LoadSearchRange;
        internal static ConfigEntry<float> EscortCollectRadius;

        private void Awake()
        {
            Log = Logger;
            DefendRadius = Config.Bind("General", "DefendRadius", 300f,
                "How far from the defended ship or station a defender will engage hostiles.");
            LoadSearchRange = Config.Bind("General", "LoadSearchRange", 600f,
                "How far from the fleet ship 'Load nearest' looks for loot.");
            EscortCollectRadius = Config.Bind("General", "EscortCollectRadius", 200f,
                "How far from the escorted ship an escort with 'Collect loot' on picks up loot.");

            try
            {
                // Bind the game's non-public members now, so a game update that renames one
                // disables the mod at startup instead of failing in the middle of a fight.
                RuntimeHelpers.RunClassConstructor(typeof(GameAccess).TypeHandle);
            }
            catch (Exception e)
            {
                Log.LogError($"{Name} is not compatible with this version of the game and is disabled: {e}");
                return;
            }

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(OrdersPanelPatches));
            harmony.PatchAll(typeof(FleetAIPatches));
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
