using System;
using System.Collections.Generic;
using BlightMod.Core;
using BlightMod.Modifiers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(NCharacterSelectScreen), "BeginRun")]
    public static class CharacterSelectScreenBeginRunPatch
    {
        public static void Prefix(IReadOnlyList<ModifierModel> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            bool isBlight = false;
            int ascension = 0;

            foreach (ModifierModel modifier in modifiers)
            {
                if (modifier == null || modifier.Id?.Entry == null)
                {
                    continue;
                }

                if (!modifier.Id.Entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                isBlight = true;
                break;
            }

            if (isBlight)
            {
                ascension = ExtractBlightAscension(modifiers);
            }

            if (!isBlight)
            {
                return;
            }

            BlightModeManager.IsBlightModeActive = true;
            BlightModeManager.BlightAscensionLevel = ascension;
            BlightModeManager.ResetRuntimeState();
            BlightRunSaveSlotManager.SetCurrentRunSlotByMode(isBlight: true);
        }

        private static int ExtractBlightAscension(IReadOnlyList<ModifierModel> modifiers)
        {
            foreach (ModifierModel modifier in modifiers)
            {
                if (modifier is not BlightMod.Modifiers.BlightRunTagModifier blightTag)
                {
                    continue;
                }

                return Math.Clamp(blightTag.BlightAscensionLevel, 0, 5);
            }

            return Math.Clamp(BlightModeManager.BlightAscensionLevel, 0, 5);
        }
    }

    [HarmonyPatch(typeof(RunManager), "SetUpNewMultiPlayer")]
    public static class RunManagerSetUpNewMultiPlayerPatch
    {
        public static void Prefix(RunState __0)
        {
            try
            {
                bool isBlight = false;
                int ascension = 0;

                if (__0?.Modifiers != null)
                {
                    foreach (ModifierModel modifier in __0.Modifiers)
                    {
                        if (modifier == null || modifier.Id?.Entry == null)
                        {
                            continue;
                        }

                        if (!modifier.Id.Entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        isBlight = true;
                        if (modifier is BlightMod.Modifiers.BlightRunTagModifier blightTag)
                        {
                            ascension = Math.Clamp(blightTag.BlightAscensionLevel, 0, 5);
                        }
                    }
                }

                if (isBlight && ascension == 0)
                {
                    ascension = Math.Clamp(__0.AscensionLevel, 0, 5);
                }

                BlightModeManager.IsBlightModeActive = isBlight;
                BlightModeManager.BlightAscensionLevel = ascension;

                if (isBlight)
                {
                    BlightModeManager.ResetRuntimeState();
                    BlightRunSaveSlotManager.SetCurrentRunSlotByMode(isBlight: true);
                }
            }
            catch (Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"[Blight] SetUpNewMultiPlayer prefix failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(StartRunLobby), "BeginRun")]
    public static class StartRunLobbyBeginRunPatch
    {
        public static void Prefix(ref List<ModifierModel> modifiers)
        {
            try
            {
                if (!BlightModeManager.IsBlightModeActive)
                {
                    return;
                }

                modifiers ??= new List<ModifierModel>();

                BlightRunTagModifier? runTag = null;
                foreach (ModifierModel modifier in modifiers)
                {
                    if (modifier is BlightRunTagModifier existingTag)
                    {
                        runTag = existingTag;
                        break;
                    }
                }

                if (runTag == null)
                {
                    runTag = (BlightRunTagModifier)ModelDb.Modifier<BlightRunTagModifier>().ToMutable();
                    modifiers.Add(runTag);
                }

                runTag.BlightAscensionLevel = Math.Clamp(BlightModeManager.BlightAscensionLevel, 0, 5);
                runTag.TriggeredDoubleWaveNodeKeys = string.Empty;
            }
            catch (Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"[Blight] StartRunLobby.BeginRun prefix failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.BeginRun))]
    public static class CharacterSelectScreenBeginRunModifiersCompatPatch
    {
        public static bool Prefix(ref IReadOnlyList<ModifierModel> modifiers)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return true;
            }

            if (modifiers == null || modifiers.Count == 0)
            {
                return true;
            }

            bool allBlightModifiers = true;
            foreach (ModifierModel modifier in modifiers)
            {
                string? entry = modifier?.Id?.Entry;
                if (string.IsNullOrEmpty(entry) || !entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase))
                {
                    allBlightModifiers = false;
                    break;
                }
            }

            if (allBlightModifiers)
            {
                modifiers = Array.Empty<ModifierModel>();
            }

            return true;
        }
    }
}
