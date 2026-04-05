using HarmonyLib;
using BlightMod.Core;
using BlightMod.Modifiers;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Events;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(RunState), "CreateForNewRun")]
    public class RunStartPatch
    {
        public static void Prefix(ref IReadOnlyList<ModifierModel> modifiers, string seed)
        {
            if (!BlightModeManager.IsBlightModeActive) return;

            try {
                BlightModeManager.ResetRuntimeState();
                BlightRunSaveSlotManager.SetCurrentRunSlotByMode(isBlight: true);

                var mutableList = modifiers != null ? new List<ModifierModel>(modifiers) : new List<ModifierModel>();

                BlightRunTagModifier? runTag = mutableList.OfType<BlightRunTagModifier>().FirstOrDefault();
                if (runTag == null)
                {
                    runTag = (BlightRunTagModifier)ModelDb.Modifier<BlightRunTagModifier>().ToMutable();
                    mutableList.Add(runTag);
                }

                runTag.BlightAscensionLevel = Math.Clamp(BlightModeManager.BlightAscensionLevel, 0, 5);
                runTag.TriggeredDoubleWaveNodeKeys = string.Empty;

                int asc = BlightModeManager.BlightAscensionLevel;
                ModifierModel? startModifier = null;
                if (asc >= 5)
                {
                    startModifier = BlightModifierPool.GetRandomA5Modifier(seed);
                }
                else if (asc >= 4)
                {
                    startModifier = BlightModifierPool.GetRandomA4Modifier(seed);
                }
                else if (asc >= 3)
                {
                    startModifier = BlightModifierPool.GetRandomA3Modifier(seed);
                }

                if (startModifier != null && !mutableList.Any(m => m.GetType() == startModifier.GetType()))
                {
                    mutableList.Add(startModifier);
                }

                modifiers = mutableList.AsReadOnly();
            } catch (Exception e) {
                MegaCrit.Sts2.Core.Logging.Log.Error($"Blight RunStartPatch failed: {e}");
            }
        }
    }

    // 兼容修复：Neow 仅在 Modifiers.Count == 0 时才走原版三选项。
    // Blight 需要开局即注入 modifier（显示顶部图标），因此在 Neow 生成选项时临时隐藏这些 modifier。
    [HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
    public static class BlightNeowOptionsCompatPatch
    {
        private static readonly System.Reflection.PropertyInfo? RunStateModifiersProperty =
            AccessTools.Property(typeof(RunState), "Modifiers");

        public static void Prefix(Neow __instance, ref IReadOnlyList<ModifierModel>? __state)
        {
            if (!BlightModeManager.IsBlightModeActive || __instance?.Owner?.RunState == null || RunStateModifiersProperty == null)
            {
                __state = null;
                return;
            }

            if (__instance.Owner.RunState is not RunState state)
            {
                __state = null;
                return;
            }

            __state = state.Modifiers;
            bool hasBlightModifier = __state.Any(m =>
                m?.Id?.Entry != null &&
                m.Id.Entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase));

            bool hasNonBlightModifier = __state.Any(m =>
            {
                string? entry = m?.Id?.Entry;
                if (string.IsNullOrEmpty(entry))
                {
                    // Be conservative: unknown modifier identity should not be hidden.
                    return true;
                }

                return !entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase);
            });

            // Only hide modifiers for pure blight runs so Neow still shows vanilla 3 options.
            // If any non-blight modifier exists (e.g. custom run starter modifiers), keep them visible.
            if (!hasBlightModifier || hasNonBlightModifier)
            {
                __state = null;
                return;
            }

            // 仅在生成 Neow 初始选项时临时清空 modifiers，避免进入 modifier 专用选项流。
            RunStateModifiersProperty.SetValue(state, Array.Empty<ModifierModel>());
        }

        public static void Postfix(Neow __instance, IReadOnlyList<ModifierModel>? __state)
        {
            if (__state == null || __instance?.Owner?.RunState == null || RunStateModifiersProperty == null)
            {
                return;
            }

            if (__instance.Owner.RunState is RunState state)
            {
                RunStateModifiersProperty.SetValue(state, __state);
            }
        }
    }
}
