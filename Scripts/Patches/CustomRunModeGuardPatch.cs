using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeSingleplayer))]
    public static class CustomRunScreenInitializeSingleplayerBlightGuardPatch
    {
        public static void Prefix()
        {
            BlightModeManager.IsBlightModeActive = false;
            BlightModeManager.BlightAscensionLevel = 0;
        }
    }

    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsHost))]
    public static class CustomRunScreenInitializeMultiplayerHostBlightGuardPatch
    {
        public static void Prefix()
        {
            BlightModeManager.IsBlightModeActive = false;
            BlightModeManager.BlightAscensionLevel = 0;
        }
    }

    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsClient))]
    public static class CustomRunScreenInitializeMultiplayerClientBlightGuardPatch
    {
        public static void Prefix()
        {
            BlightModeManager.IsBlightModeActive = false;
            BlightModeManager.BlightAscensionLevel = 0;
        }
    }
}
