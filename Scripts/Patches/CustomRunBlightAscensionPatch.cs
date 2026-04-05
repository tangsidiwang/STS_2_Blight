using BlightMod.Core;
using BlightMod.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.addons.mega_text;

namespace BlightMod.Patches
{
    internal static class CustomRunBlightAscensionHelper
    {
        public const int StandardAscensionMax = 10;
        public const int BlightAscensionMin = 0;
        public const int BlightAscensionMax = 5;
        public const int CustomPanelMaxAscension = StandardAscensionMax + BlightAscensionMax + 1; // 16
        public const int FirstBlightUiAscension = StandardAscensionMax + 1; // 11 => blight 0

        public static bool IsCustomPanel(NAscensionPanel panel)
        {
            return FindAncestor<NCustomRunScreen>(panel) != null;
        }

        public static bool IsCustomBlightTier(int uiAscension)
        {
            return uiAscension >= FirstBlightUiAscension && uiAscension <= CustomPanelMaxAscension;
        }

        public static int UiToBlightAscension(int uiAscension)
        {
            return uiAscension - FirstBlightUiAscension;
        }

        public static int BlightToUiAscension(int blightAscension)
        {
            return FirstBlightUiAscension + System.Math.Clamp(blightAscension, BlightAscensionMin, BlightAscensionMax);
        }

        public static T? FindAncestor<T>(Godot.Node node) where T : class
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
    public static class CustomRunAscensionPanelMaxPatch
    {
        public static void Prefix(NAscensionPanel __instance, ref int maxAscension)
        {
            if (CustomRunBlightAscensionHelper.IsCustomPanel(__instance))
            {
                maxAscension = CustomRunBlightAscensionHelper.CustomPanelMaxAscension;
            }
        }
    }

    [HarmonyPatch(typeof(NAscensionPanel), "RefreshAscensionText")]
    public static class CustomRunAscensionPanelTextPatch
    {
        public static bool Prefix(NAscensionPanel __instance)
        {
            if (!CustomRunBlightAscensionHelper.IsCustomPanel(__instance))
            {
                return true;
            }

            if (!CustomRunBlightAscensionHelper.IsCustomBlightTier(__instance.Ascension))
            {
                return true;
            }

            int blightAscension = CustomRunBlightAscensionHelper.UiToBlightAscension(__instance.Ascension);
            var level = __instance.GetNodeOrNull<MegaLabel>("HBoxContainer/AscensionIconContainer/AscensionIcon/AscensionLevel");
            var info = __instance.GetNodeOrNull<MegaRichTextLabel>("HBoxContainer/AscensionDescription/Description");

            if (level != null)
            {
                level.SetTextAutoSize(blightAscension.ToString());
            }

            if (info != null)
            {
                string title = BlightLocalization.GetText($"BLIGHT_ASCENSION.{blightAscension}.title");
                string desc = BlightLocalization.GetText($"BLIGHT_ASCENSION.{blightAscension}.description");
                info.Text = $"[b][color=#8b00ff]{title}[/color][/b]\n{desc}";
            }

            // Skip vanilla text lookup (ascension.LEVEL_11+) which does not exist.
            return false;
        }
    }

    [HarmonyPatch(typeof(NCustomRunScreen), "OnAscensionPanelLevelChanged")]
    public static class CustomRunAscensionChangedPatch
    {
        public static void Postfix(NCustomRunScreen __instance)
        {
            CustomRunBlightStateSync.SyncCustomRunBlightState(__instance);
        }
    }

    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.OnSubmenuOpened))]
    public static class CustomRunSubmenuOpenedPatch
    {
        public static void Postfix(NCustomRunScreen __instance)
        {
            CustomRunBlightStateSync.SyncCustomRunBlightState(__instance);
        }
    }

    [HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
    public static class NGameCustomSingleplayerAscensionPatch
    {
        public static void Prefix(ref int ascensionLevel)
        {
            if (BlightModeManager.IsBlightModeActive
                && BlightModeManager.BlightAscensionLevel >= CustomRunBlightAscensionHelper.BlightAscensionMin
                && BlightModeManager.BlightAscensionLevel <= CustomRunBlightAscensionHelper.BlightAscensionMax
                && ascensionLevel > CustomRunBlightAscensionHelper.StandardAscensionMax)
            {
                // Custom blight tiers are an extension after A10; the underlying vanilla ascension stays at A10.
                ascensionLevel = CustomRunBlightAscensionHelper.StandardAscensionMax;
            }
        }
    }

    [HarmonyPatch(typeof(NGame), nameof(NGame.StartNewMultiplayerRun))]
    public static class NGameCustomMultiplayerAscensionPatch
    {
        public static void Prefix(ref int ascensionLevel)
        {
            if (BlightModeManager.IsBlightModeActive
                && BlightModeManager.BlightAscensionLevel >= CustomRunBlightAscensionHelper.BlightAscensionMin
                && BlightModeManager.BlightAscensionLevel <= CustomRunBlightAscensionHelper.BlightAscensionMax
                && ascensionLevel > CustomRunBlightAscensionHelper.StandardAscensionMax)
            {
                ascensionLevel = CustomRunBlightAscensionHelper.StandardAscensionMax;
            }
        }
    }

    internal static class CustomRunBlightStateSync
    {
        public static void SyncCustomRunBlightState(NCustomRunScreen screen)
        {
            var panel = screen?.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
            if (panel == null)
            {
                return;
            }

            if (!CustomRunBlightAscensionHelper.IsCustomBlightTier(panel.Ascension))
            {
                BlightModeManager.IsBlightModeActive = false;
                BlightModeManager.BlightAscensionLevel = 0;
                BlightRunSaveSlotManager.SetCurrentRunSlotByMode(isBlight: false);
                return;
            }

            BlightModeManager.IsBlightModeActive = true;
            BlightModeManager.BlightAscensionLevel = CustomRunBlightAscensionHelper.UiToBlightAscension(panel.Ascension);
            BlightRunSaveSlotManager.SetCurrentRunSlotByMode(isBlight: true);
        }
    }

}
