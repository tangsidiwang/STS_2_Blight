using System;
using System.Linq;
using System.Threading.Tasks;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(SaveManager), "LoadRunSave")]
    public static class SaveManagerLoadRunSavePatch
    {
        public static void Prefix()
        {
            BlightRunSaveSlotManager.PrepareCurrentRunForContinue();
        }

        public static void Postfix(ReadSaveResult<SerializableRun> __result)
        {
            try
            {
                if (__result == null || !__result.Success || __result.SaveData == null)
                {
                    return;
                }

                SerializableRun save = __result.SaveData;
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

                BlightSaveLoadWindow.OnRunSaveLoaded(save);
                string modifierIds = string.Join(",", save.Modifiers?.Select(m => m?.Id?.Entry).Where(s => !string.IsNullOrEmpty(s)).Take(8) ?? Array.Empty<string>());
                Log.Info($"[Blight] LoadRunSave detected isBlight={isBlight}, asc={ascension}, slot={BlightRunSaveSlotManager.CurrentRunSlot}, modifiers={modifierIds}");
            }
            catch (Exception e)
            {
                // Keep load flow resilient; downstream patches still have fallbacks.
                Log.Error($"[Blight] LoadRunSave postfix failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager), "LoadAndCanonicalizeMultiplayerRunSave")]
    public static class SaveManagerLoadMultiplayerRunSavePatch
    {
        public static void Postfix(ReadSaveResult<SerializableRun> __result)
        {
            try
            {
                if (__result == null || !__result.Success || __result.SaveData == null)
                {
                    return;
                }

                SerializableRun save = __result.SaveData;
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

                BlightSaveLoadWindow.OnRunSaveLoaded(save);
                string modifierIds = string.Join(",", save.Modifiers?.Select(m => m?.Id?.Entry).Where(s => !string.IsNullOrEmpty(s)).Take(8) ?? Array.Empty<string>());
                Log.Info($"[Blight] LoadMultiplayerRunSave detected isBlight={isBlight}, asc={ascension}, modifiers={modifierIds}");
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] LoadMultiplayerRunSave postfix failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager), "get_HasRunSave")]
    public static class SaveManagerHasRunSavePatch
    {
        public static void Postfix(ref bool __result)
        {
            __result = __result || BlightRunSaveSlotManager.HasAnyRunSave();
        }
    }

    [HarmonyPatch(typeof(SaveManager), "SaveRun")]
    public static class SaveManagerSaveRunPatch
    {
        public static void Postfix(ref Task __result)
        {
            __result = MirrorAfterSave(__result);
        }

        private static async Task MirrorAfterSave(Task original)
        {
            await original;
            BlightRunSaveSlotManager.MirrorCurrentRunToModeSlot();
        }
    }

    [HarmonyPatch(typeof(SaveManager), "DeleteCurrentRun")]
    public static class SaveManagerDeleteCurrentRunPatch
    {
        public static void Prefix()
        {
            BlightRunSaveSlotManager.DeleteAllSingleplayerRunSavesForCurrentProfile();
        }
    }

    [HarmonyPatch(typeof(RunManager), "SetUpSavedSinglePlayer")]
    public static class RunManagerSetUpSavedSinglePlayerPatch
    {
        private const string BlightTagEntry = "BLIGHT_RUN_TAG";

        private const string BlightAscensionPropertyName = "BlightAscensionLevel";

        private const string TriggeredDoubleWaveNodeKeysPropertyName = "TriggeredDoubleWaveNodeKeys";

        public static void Prefix(SerializableRun __1)
        {
            try
            {
                SerializableRun save = __1;
                bool isBlight = TryGetBlightTagModifier(save, out SerializableModifier? blightTag);
                if (!isBlight)
                {
                    // Fallback: older saves or parsing issues may miss the explicit run tag.
                    // If the selected continue slot is the blight slot, treat this as a blight run.
                    isBlight = BlightRunSaveSlotManager.LastContinueSlot == BlightRunSlot.Blight || HasAnyBlightModifier(save);
                }

                int blightAscension = isBlight
                    ? ReadBlightAscensionFromTag(blightTag, save.Ascension)
                    : 0;
                string triggeredDoubleWaveNodeKeys = isBlight
                    ? ReadTriggeredDoubleWaveNodeKeysFromTag(blightTag)
                    : string.Empty;

                BlightModeManager.IsBlightModeActive = isBlight;
                BlightModeManager.BlightAscensionLevel = blightAscension;
                BlightModeManager.ResetRuntimeState();
                BlightModeManager.RestoreTriggeredDoubleWaveNodeKeys(triggeredDoubleWaveNodeKeys);
                if (isBlight)
                {
                    BlightRunSaveSlotManager.SetCurrentRunSlotFromLoadedSave(isBlight: true);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] SetUpSavedSinglePlayer prefix failed: {e}");
                BlightModeManager.IsBlightModeActive = false;
                BlightModeManager.BlightAscensionLevel = 0;
            }
        }

        public static void Postfix(RunState __0, SerializableRun __1)
        {
            try
            {
                if (__0 == null || __1 == null)
                {
                    return;
                }

                BlightSaveLoadWindow.OnRunStateReadyForLoadedSave(__0, __1);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] SetUpSavedSinglePlayer postfix failed: {e}");
            }
        }

        public static bool HasAnyBlightModifier(SerializableRun save)
        {
            if (save.Modifiers == null)
            {
                return false;
            }

            return save.Modifiers.Any(m =>
                m?.Id?.Entry != null &&
                m.Id.Entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase));
        }

        public static bool TryGetBlightTagModifier(SerializableRun save, out SerializableModifier? tag)
        {
            tag = save.Modifiers?.FirstOrDefault(m =>
                m?.Id != null &&
            m.Id.Entry != null &&
            m.Id.Entry.IndexOf(BlightTagEntry, StringComparison.OrdinalIgnoreCase) >= 0);

            return tag != null;
        }

        private static int ReadBlightAscensionFromTag(SerializableModifier? tag, int fallback)
        {
            int fallbackClamped = Math.Clamp(fallback, 0, 5);
            if (tag?.Props?.ints == null)
            {
                return fallbackClamped;
            }

            SavedProperties.SavedProperty<int>? levelProp = tag.Props.ints.FirstOrDefault(i =>
                string.Equals(i.name, BlightAscensionPropertyName, StringComparison.Ordinal));

            if (!levelProp.HasValue)
            {
                return fallbackClamped;
            }

            return Math.Clamp(levelProp.Value.value, 0, 5);
        }

        public static int ReadBlightAscensionFromAnyTag(SerializableRun save, int fallback)
        {
            if (TryGetBlightTagModifier(save, out SerializableModifier? blightTag))
            {
                return ReadBlightAscensionFromTag(blightTag, fallback);
            }

            return Math.Clamp(fallback, 0, 5);
        }

        public static string ReadTriggeredDoubleWaveNodeKeysFromTag(SerializableModifier? tag)
        {
            if (tag?.Props?.strings == null)
            {
                return string.Empty;
            }

            SavedProperties.SavedProperty<string>? keysProp = tag.Props.strings.FirstOrDefault(i =>
                string.Equals(i.name, TriggeredDoubleWaveNodeKeysPropertyName, StringComparison.Ordinal));

            return keysProp?.value ?? string.Empty;
        }
    }

    [HarmonyPatch(typeof(RunManager), "SetUpSavedMultiPlayer")]
    public static class RunManagerSetUpSavedMultiPlayerPatch
    {
        public static void Prefix(SerializableRun __1)
        {
            try
            {
                SerializableRun save = __1;
                bool isBlight = RunManagerSetUpSavedSinglePlayerPatch.HasAnyBlightModifier(save);
                int blightAscension = isBlight
                    ? RunManagerSetUpSavedSinglePlayerPatch.ReadBlightAscensionFromAnyTag(save, save.Ascension)
                    : 0;
                string triggeredDoubleWaveNodeKeys = isBlight && RunManagerSetUpSavedSinglePlayerPatch.TryGetBlightTagModifier(save, out SerializableModifier? blightTag)
                    ? RunManagerSetUpSavedSinglePlayerPatch.ReadTriggeredDoubleWaveNodeKeysFromTag(blightTag)
                    : string.Empty;

                BlightModeManager.IsBlightModeActive = isBlight;
                BlightModeManager.BlightAscensionLevel = blightAscension;
                BlightModeManager.ResetRuntimeState();
                BlightModeManager.RestoreTriggeredDoubleWaveNodeKeys(triggeredDoubleWaveNodeKeys);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] SetUpSavedMultiPlayer prefix failed: {e}");
                BlightModeManager.IsBlightModeActive = false;
                BlightModeManager.BlightAscensionLevel = 0;
            }
        }

        public static void Postfix(RunState __0, SerializableRun __1)
        {
            try
            {
                if (__0 == null || __1 == null)
                {
                    return;
                }

                BlightSaveLoadWindow.OnRunStateReadyForLoadedSave(__0, __1);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] SetUpSavedMultiPlayer postfix failed: {e}");
            }
        }
    }
}