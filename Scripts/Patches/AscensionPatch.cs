using HarmonyLib;
using Godot;
using BlightMod.Core;
using BlightMod.Localization;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.addons.mega_text;

namespace BlightMod.Patches
{
    internal static class BlightAscensionUiHelper
    {
        public static bool ShouldUseBlightText(int level)
        {
            if (level < 0 || level > 5)
            {
                return false;
            }

            // In-run tooltip replacement must follow actual run modifiers, not stale menu state.
            RunState? state = RunManager.Instance?.DebugOnlyGetState();
            if (state != null)
            {
                return HasBlightRunModifiers(state.Modifiers);
            }

            // Outside a run (main menu / character select), only use explicit blight mode flag.
            return BlightModeManager.IsBlightModeActive;
        }

        public static bool ShouldUseBlightTextForPanel(NAscensionPanel panel)
        {
            if (panel == null)
            {
                return false;
            }

            // Never override ascension text on custom run screen.
            if (FindAncestor<NCustomRunScreen>(panel) != null)
            {
                return false;
            }

            // Character select screen should only use blight text when blight mode is active.
            if (FindAncestor<NCharacterSelectScreen>(panel) != null)
            {
                return BlightModeManager.IsBlightModeActive;
            }

            return ShouldUseBlightText(panel.Ascension);
        }

        private static bool HasBlightRunModifiers(IReadOnlyList<ModifierModel>? modifiers)
        {
            if (modifiers == null)
            {
                return false;
            }

            foreach (ModifierModel modifier in modifiers)
            {
                string? entry = modifier?.Id?.Entry;
                if (!string.IsNullOrEmpty(entry) && entry.StartsWith("BLIGHT_", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static T? FindAncestor<T>(Godot.Node node) where T : class
        {
            Godot.Node? current = node;
            while (current != null)
            {
                if (current is T matched)
                {
                    return matched;
                }

                current = current.GetParent();
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(NAscensionPanel), "SetMaxAscension")]
    public class AscensionPanelMaxPatch
    {
        public static void Prefix(NAscensionPanel __instance, ref int maxAscension)
        {
            if (BlightAscensionUiHelper.ShouldUseBlightTextForPanel(__instance))
            {
                maxAscension = 5;
            }
        }
    }

    [HarmonyPatch(typeof(NAscensionPanel), "SetAscensionLevel")]
    public class AscensionPanelSetLevelPatch
    {
        public static void Prefix(NAscensionPanel __instance, ref int ascension)
        {
            if (BlightAscensionUiHelper.ShouldUseBlightTextForPanel(__instance))
            {
                if (ascension > 5) ascension = 5;
                if (ascension < 0) ascension = 0;
            }
        }
    }

    [HarmonyPatch(typeof(NCharacterSelectScreen), "OnAscensionPanelLevelChanged")]
    public class CharacterSelectScreenAscensionChangedPatch
    {
        private static readonly AccessTools.FieldRef<NCharacterSelectScreen, object> LobbyRef =
            AccessTools.FieldRefAccess<NCharacterSelectScreen, object>("_lobby");

        public static bool Prefix(NCharacterSelectScreen __instance)
        {
            return LobbyRef(__instance) != null;
        }

        public static void Postfix(NCharacterSelectScreen __instance)
        {
            if (BlightModeManager.IsBlightModeActive)
            {
                var panel = __instance.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
                if (panel != null)
                {
                    BlightModeManager.BlightAscensionLevel = panel.Ascension;
                }
            }
        }
    }

    [HarmonyPatch(typeof(NCharacterSelectScreen), "_Ready")]
    public class CharacterSelectScreenReadyPatch
    {
        public static void Postfix(NCharacterSelectScreen __instance)
        {
            if (BlightModeManager.IsBlightModeActive)
            {
                var panel = __instance.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
                if (panel != null)
                {
                    AccessTools.Method(typeof(NAscensionPanel), "SetMaxAscension")?.Invoke(panel, new object[] { 5 });
                    AccessTools.Method(typeof(NAscensionPanel), "SetAscensionLevel")?.Invoke(panel, new object[] { BlightModeManager.BlightAscensionLevel });
                }
            }
        }
    }

    [HarmonyPatch(typeof(NAscensionPanel), "RefreshAscensionText")]
    public class AscensionPanelRefreshTextPatch
    {
        public static void Postfix(NAscensionPanel __instance)
        {
            if (BlightAscensionUiHelper.ShouldUseBlightTextForPanel(__instance))
            {
                var info = __instance.GetNodeOrNull<MegaRichTextLabel>("HBoxContainer/AscensionDescription/Description");
                
                if (info != null)
                {
                    string title = BlightLocalization.GetText($"BLIGHT_ASCENSION.{__instance.Ascension}.title");
                    string desc = BlightLocalization.GetText($"BLIGHT_ASCENSION.{__instance.Ascension}.description");

                    info.Text = $"[b][color=#8b00ff]{title}[/color][/b]\n{desc}";
                }
            }
        }
    }

    [HarmonyPatch(typeof(AscensionHelper), nameof(AscensionHelper.GetHoverTip))]
    public class AscensionHoverTipPatch
    {
        public static void Postfix(CharacterModel character, int level, ref HoverTip __result)
        {
            RunState? state = RunManager.Instance?.DebugOnlyGetState();
            bool runHasBlight = state?.Modifiers?.Any(m =>
                !string.IsNullOrEmpty(m?.Id?.Entry)
                && m.Id.Entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase)) == true;

            int displayLevel = level;
            if (runHasBlight && level > 5)
            {
                displayLevel = Math.Clamp(BlightModeManager.BlightAscensionLevel, 0, 5);
            }

            if (!runHasBlight && !BlightAscensionUiHelper.ShouldUseBlightText(level))
            {
                return;
            }

            LocString title = new LocString("ascension", "PORTRAIT_TITLE");
            title.Add("character", character.Title);
            title.Add("ascension", displayLevel);

            __result = new HoverTip(title, BuildBlightAscensionDescription(displayLevel));
        }

        private static string BuildBlightAscensionDescription(int level)
        {
            if (level <= 0)
            {
                return BlightLocalization.GetText("BLIGHT_ASCENSION.0.description");
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int ascension = 1; ascension <= level; ascension++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                string ascensionTitle = BlightLocalization.GetText($"BLIGHT_ASCENSION.{ascension}.title");
                string ascensionDescription = BlightLocalization.GetText($"BLIGHT_ASCENSION.{ascension}.description");
                builder.Append("[b][color=#8b00ff]");
                builder.Append(ascensionTitle);
                builder.Append("[/color][/b]：");
                builder.Append(ascensionDescription);
            }

            return builder.ToString();
        }
    }
}
