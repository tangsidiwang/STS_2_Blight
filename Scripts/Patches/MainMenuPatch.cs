using HarmonyLib;
using Godot;
using BlightMod.Core;
using BlightMod.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using System;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(NSingleplayerSubmenu), "RefreshButtons")]
    public class MainMenuPatch
    {
        public static void Postfix(NSingleplayerSubmenu __instance)
        {
            try {
                var standardButton = __instance.GetNodeOrNull<NSubmenuButton>("StandardButton");
                if (standardButton == null) return;
                var parent = standardButton.GetParent();
                if (parent.HasNode("BlightModeButton")) return;

                // Instantiate the shared button scene resource instead of duplicating StandardButton.
                // This avoids inheriting STANDARD localization state from the original button node.
                var buttonScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/submenu_button.tscn");
                if (buttonScene == null) return;

                var blightButton = buttonScene.Instantiate<NSubmenuButton>(PackedScene.GenEditState.Disabled);
                if (blightButton == null) return;

                blightButton.Name = "BlightModeButton";
                blightButton.FocusMode = Control.FocusModeEnum.None;
                blightButton.AnchorLeft = standardButton.AnchorLeft;
                blightButton.AnchorRight = standardButton.AnchorRight;
                blightButton.AnchorTop = standardButton.AnchorTop;
                blightButton.AnchorBottom = standardButton.AnchorBottom;
                blightButton.OffsetTop = standardButton.OffsetTop;
                blightButton.OffsetBottom = standardButton.OffsetBottom;
                blightButton.PivotOffset = standardButton.PivotOffset;

                // Duplicate material
                var bgPanel = blightButton.GetNodeOrNull<Control>("BgPanel");
                if (bgPanel != null && bgPanel.Material != null) {
                    bgPanel.Material = (Material)bgPanel.Material.Duplicate();
                    bgPanel.Material.Set("shader_parameter/h", 0.75f);
                    bgPanel.Material.Set("shader_parameter/s", 0.8f);
                    bgPanel.Material.Set("shader_parameter/v", 0.8f);
                }

                parent.AddChild(blightButton);

                // RECONNECT SIGNAL MANUALLY
                blightButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>((btn) => {
                    BlightModeManager.IsBlightModeActive = true;
                    BlightModeManager.BlightAscensionLevel = 1;
                    AccessTools.Method(typeof(NSingleplayerSubmenu), "OpenCharacterSelect").Invoke(__instance, new object[] { btn });
                }));

                // FIX LAYOUT
                var dailyButton = __instance.GetNodeOrNull<NSubmenuButton>("DailyButton");
                var customButton = __instance.GetNodeOrNull<NSubmenuButton>("CustomRunButton");

                if (dailyButton != null && customButton != null) {
                    standardButton.OffsetLeft = -735f; standardButton.OffsetRight = -405f;
                    blightButton.OffsetLeft = -355f;   blightButton.OffsetRight = -25f;
                    dailyButton.OffsetLeft = 25f;      dailyButton.OffsetRight = 355f;
                    customButton.OffsetLeft = 405f;    customButton.OffsetRight = 735f;
                }

                // Force UI update
                ApplyBlightModeLabels(blightButton);

            } catch (Exception e) {
                MegaCrit.Sts2.Core.Logging.Log.Error($"BlightModeButton setup failed: {e}");
            }
        }

        internal static void ApplyBlightModeLabels(NSubmenuButton button)
        {
            string titleKey = "BLIGHT_BUTTON.title";
            string descriptionKey = "BLIGHT_BUTTON.description";

            if (string.Equals(button.Name.ToString(), "BlightMultiplayerModeButton", StringComparison.Ordinal))
            {
                titleKey = "BLIGHT_MP_BUTTON.title";
                descriptionKey = "BLIGHT_MP_BUTTON.description";
            }

            // Runtime-duplicated nodes may not resolve %UniqueName paths correctly, so use direct child names.
            var title = button.GetNodeOrNull<MegaLabel>("Title") ?? button.GetNodeOrNull<MegaLabel>("%Title");
            var desc = button.GetNodeOrNull<MegaRichTextLabel>("Description") ?? button.GetNodeOrNull<MegaRichTextLabel>("%Description");

            if (title != null)
            {
                title.SetTextAutoSize(BlightLocalization.GetText(titleKey));
            }

            if (desc != null)
            {
                desc.Text = BlightLocalization.GetText(descriptionKey);
            }
        }
    }

    [HarmonyPatch(typeof(NMultiplayerHostSubmenu), "OnSubmenuOpened")]
    public class MultiplayerHostSubmenuPatch
    {
        public static void Prefix()
        {
            BlightModeManager.IsBlightModeActive = false;
            BlightModeManager.BlightAscensionLevel = 0;
        }

        public static void Postfix(NMultiplayerHostSubmenu __instance)
        {
            try
            {
                var standardButton = __instance.GetNodeOrNull<NSubmenuButton>("StandardButton");
                if (standardButton == null)
                {
                    return;
                }

                var parent = standardButton.GetParent();
                if (parent == null || parent.HasNode("BlightMultiplayerModeButton"))
                {
                    if (parent != null)
                    {
                        var existingButton = parent.GetNodeOrNull<NSubmenuButton>("BlightMultiplayerModeButton");
                        if (existingButton != null)
                        {
                            MainMenuPatch.ApplyBlightModeLabels(existingButton);
                        }
                    }

                    return;
                }

                var buttonScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/submenu_button.tscn");
                if (buttonScene == null)
                {
                    return;
                }

                var blightButton = buttonScene.Instantiate<NSubmenuButton>(PackedScene.GenEditState.Disabled);
                if (blightButton == null)
                {
                    return;
                }

                blightButton.Name = "BlightMultiplayerModeButton";
                blightButton.FocusMode = Control.FocusModeEnum.None;
                blightButton.AnchorLeft = standardButton.AnchorLeft;
                blightButton.AnchorRight = standardButton.AnchorRight;
                blightButton.AnchorTop = standardButton.AnchorTop;
                blightButton.AnchorBottom = standardButton.AnchorBottom;
                blightButton.OffsetTop = standardButton.OffsetTop;
                blightButton.OffsetBottom = standardButton.OffsetBottom;
                blightButton.PivotOffset = standardButton.PivotOffset;

                var bgPanel = blightButton.GetNodeOrNull<Control>("BgPanel");
                if (bgPanel != null && bgPanel.Material != null)
                {
                    bgPanel.Material = (Material)bgPanel.Material.Duplicate();
                    bgPanel.Material.Set("shader_parameter/h", 0.75f);
                    bgPanel.Material.Set("shader_parameter/s", 0.8f);
                    bgPanel.Material.Set("shader_parameter/v", 0.8f);
                }

                parent.AddChild(blightButton);
                blightButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>((btn) =>
                {
                    BlightModeManager.IsBlightModeActive = true;
                    BlightModeManager.BlightAscensionLevel = Math.Max(BlightModeManager.BlightAscensionLevel, 1);
                    AccessTools.Method(typeof(NMultiplayerHostSubmenu), "StartHost")?.Invoke(__instance, new object[] { GameMode.Standard });
                }));

                var dailyButton = __instance.GetNodeOrNull<NSubmenuButton>("DailyButton");
                var customButton = __instance.GetNodeOrNull<NSubmenuButton>("CustomRunButton");

                if (dailyButton != null && customButton != null)
                {
                    standardButton.OffsetLeft = -735f; standardButton.OffsetRight = -405f;
                    blightButton.OffsetLeft = -355f;   blightButton.OffsetRight = -25f;
                    dailyButton.OffsetLeft = 25f;      dailyButton.OffsetRight = 355f;
                    customButton.OffsetLeft = 405f;    customButton.OffsetRight = 735f;
                }

                MainMenuPatch.ApplyBlightModeLabels(blightButton);
            }
            catch (Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"BlightMultiplayerModeButton setup failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(NSubmenuButton), "RefreshLabels")]
    public class NSubmenuButtonRefreshLabelsPatch
    {
        public static bool Prefix(NSubmenuButton __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            string buttonName = __instance.Name.ToString();
            if (!string.Equals(buttonName, "BlightModeButton", StringComparison.Ordinal)
                && !string.Equals(buttonName, "BlightMultiplayerModeButton", StringComparison.Ordinal))
            {
                return true;
            }

            MainMenuPatch.ApplyBlightModeLabels(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(NSingleplayerSubmenu), "OpenCharacterSelect")]
    public class SingleplayerOpenCharacterSelectPatch
    {
        public static void Prefix(NButton __0)
        {
            if (__0 != null && __0.Name == "BlightModeButton")
            {
                BlightModeManager.IsBlightModeActive = true;
            }
            else
            {
                BlightModeManager.IsBlightModeActive = false;
            }
        }
    }

    [HarmonyPatch(typeof(NSingleplayerSubmenu), "OnSubmenuOpened")]
    public class SingleplayerSubmenuInitPatch
    {
        public static void Prefix()
        {
            BlightModeManager.IsBlightModeActive = false;
            BlightModeManager.BlightAscensionLevel = 0;
        }
    }
}
