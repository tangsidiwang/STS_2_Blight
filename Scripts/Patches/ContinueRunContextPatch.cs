using System;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(NMainMenu), "RefreshButtons")]
    public static class NMainMenuRefreshButtonsBlightContextPatch
    {
        public static void Postfix(NMainMenu __instance)
        {
            ContinueRunContextPatch.TrySyncBlightContextFromContinueSave(__instance);
        }
    }

    [HarmonyPatch(typeof(NMainMenu), "OnContinueButtonPressed")]
    public static class NMainMenuOnContinuePressedBlightContextPatch
    {
        public static void Prefix(NMainMenu __instance)
        {
            ContinueRunContextPatch.TrySyncBlightContextFromContinueSave(__instance);
        }
    }

    public static class ContinueRunContextPatch
    {
        private static readonly AccessTools.FieldRef<NMainMenu, ReadSaveResult<SerializableRun>?> _readRunSaveResultRef =
            AccessTools.FieldRefAccess<NMainMenu, ReadSaveResult<SerializableRun>?>("_readRunSaveResult");

        private static bool _loggedSync;

        public static void TrySyncBlightContextFromContinueSave(NMainMenu mainMenu)
        {
            if (mainMenu == null)
            {
                return;
            }

            ReadSaveResult<SerializableRun>? readResult;
            try
            {
                readResult = _readRunSaveResultRef(mainMenu);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] Continue context read failed: {e}");
                return;
            }

            if (readResult == null || !readResult.Success || readResult.SaveData == null)
            {
                return;
            }

            SerializableRun save = readResult.SaveData;
            bool isBlight = RunManagerSetUpSavedSinglePlayerPatch.HasAnyBlightModifier(save);
            int ascension = isBlight
                ? RunManagerSetUpSavedSinglePlayerPatch.ReadBlightAscensionFromAnyTag(save, save.Ascension)
                : 0;

            BlightModeManager.IsBlightModeActive = isBlight;
            BlightModeManager.BlightAscensionLevel = ascension;
            if (isBlight)
            {
                BlightRunSaveSlotManager.SetCurrentRunSlotFromLoadedSave(isBlight: true);
            }

            if (!_loggedSync)
            {
                _loggedSync = true;
                Log.Info($"[Blight] Continue context synced from NMainMenu: isBlight={isBlight}, asc={ascension}.");
            }
        }
    }
}
