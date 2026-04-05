using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Linq;

namespace BlightMod.Patches
{
    public static class MapPointPatchStyle
    {
        private const string SecondIconName = "BlightSecondIcon";
        private static readonly StringName BaseIconPosMeta = new StringName("blight_base_icon_pos");
        private static readonly StringName BaseIconScaleMeta = new StringName("blight_base_icon_scale");
        private static readonly Color MutantColor = new Color(0.912f, 0f, 0f,1f);
        private static readonly Color DoubleWaveColor = new Color(1f, 0.55f, 0.12f, 1f);
        private static bool _loggedAutoBlightSync;
        private static bool _loggedNoBlightContext;
        // Manual tweak for enemy/elite double-wave icon layout. Adjust this value as needed.
        public static Vector2 DoubleWaveIconOffsetTweak = Vector2.Zero;

        public static void Apply(NNormalMapPoint mapPoint, bool animate)
        {
            try
            {
                if (mapPoint?.Point == null)
                {
                    return;
                }

                if (!IsCombatPoint(mapPoint.Point.PointType))
                {
                    return;
                }

                RunState? state = RunManager.Instance?.DebugOnlyGetState();
                if (!EnsureBlightContext(state))
                {
                    return;
                }

                string? seed = state?.Rng.StringSeed;
                if (string.IsNullOrEmpty(seed))
                {
                    return;
                }

                bool isMutant = BlightModeManager.IsNodeMutant(mapPoint.Point, seed);
                bool isDoubleWave = BlightModeManager.IsNodeDoubleWave(mapPoint.Point, seed);

                TextureRect icon = mapPoint.GetNodeOrNull<TextureRect>("%Icon");
                Control container = mapPoint.GetNodeOrNull<Control>("%IconContainer");
                if (icon == null || container == null)
                {
                    return;
                }

                CacheBaseTransform(icon);
                Vector2 basePos = (Vector2)icon.GetMeta(BaseIconPosMeta);
                Vector2 baseScale = (Vector2)icon.GetMeta(BaseIconScaleMeta);

                Color targetColor = isMutant ? MutantColor : (isDoubleWave ? DoubleWaveColor : icon.SelfModulate);
                TextureRect? secondIcon = EnsureSecondIcon(container, icon, isDoubleWave);

                if (isDoubleWave)
                {
                    Vector2 leftBase = new Vector2(-14f, 0f);
                    Vector2 rightBase = new Vector2(14f, 0f);
                    icon.Position = basePos + leftBase + DoubleWaveIconOffsetTweak;
                    icon.Scale = baseScale * 0.92f;
                    if (secondIcon != null)
                    {
                        secondIcon.Position = basePos + rightBase + DoubleWaveIconOffsetTweak;
                        secondIcon.Scale = baseScale * 0.92f;
                    }
                }
                else
                {
                    // Non-double nodes must stay exactly like vanilla placement.
                    icon.Position = basePos;
                    icon.Scale = baseScale;
                }

                ApplyColor(icon, targetColor, mapPoint, animate);
                if (secondIcon != null)
                {
                    ApplyColor(secondIcon, targetColor, mapPoint, animate);
                }
            }
            catch (Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"Blight MapPointPatch Error: {e}");
            }
        }

        private static bool EnsureBlightContext(RunState? state)
        {
            if (BlightModeManager.IsBlightModeActive)
            {
                return true;
            }

            bool slotSaysBlight = BlightRunSaveSlotManager.CurrentRunSlot == BlightRunSlot.Blight
                || BlightRunSaveSlotManager.LastContinueSlot == BlightRunSlot.Blight;
            bool runHasBlightModifier = state?.Modifiers?.Any(IsBlightModifier) == true;

            if (!slotSaysBlight && !runHasBlightModifier)
            {
                if (BlightRunSaveSlotManager.TryInferBlightFromAnyRunSave(out int inferredAscension))
                {
                    BlightModeManager.IsBlightModeActive = true;
                    BlightModeManager.BlightAscensionLevel = inferredAscension;
                    if (!_loggedAutoBlightSync)
                    {
                        _loggedAutoBlightSync = true;
                        MegaCrit.Sts2.Core.Logging.Log.Info($"[Blight] Auto-synced blight context from raw save json, asc={inferredAscension}.");
                    }
                    return true;
                }

                if (!_loggedNoBlightContext)
                {
                    _loggedNoBlightContext = true;
                    MegaCrit.Sts2.Core.Logging.Log.Info("[Blight] Map marker skipped: no blight context detected from slot or run modifiers.");
                }
                return false;
            }

            BlightModeManager.IsBlightModeActive = true;
            if (BlightModeManager.BlightAscensionLevel <= 0 && state != null)
            {
                BlightModeManager.BlightAscensionLevel = Math.Clamp(state.AscensionLevel, 1, 5);
            }

            if (!_loggedAutoBlightSync)
            {
                _loggedAutoBlightSync = true;
                MegaCrit.Sts2.Core.Logging.Log.Info($"[Blight] Auto-synced blight map context: slotBlight={slotSaysBlight}, modifierBlight={runHasBlightModifier}, asc={BlightModeManager.BlightAscensionLevel}.");
            }

            return true;
        }

        private static bool IsBlightModifier(ModifierModel modifier)
        {
            string? entry = modifier?.Id?.Entry;
            if (string.IsNullOrEmpty(entry))
            {
                return false;
            }

            return entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase);
        }

        private static void CacheBaseTransform(TextureRect icon)
        {
            if (!icon.HasMeta(BaseIconPosMeta))
            {
                icon.SetMeta(BaseIconPosMeta, icon.Position);
            }

            if (!icon.HasMeta(BaseIconScaleMeta))
            {
                icon.SetMeta(BaseIconScaleMeta, icon.Scale);
            }
        }

        private static TextureRect? EnsureSecondIcon(Control container, TextureRect icon, bool needed)
        {
            TextureRect? existing = container.GetNodeOrNull<TextureRect>(SecondIconName);
            if (!needed)
            {
                existing?.QueueFree();
                return null;
            }

            if (existing != null)
            {
                existing.Texture = icon.Texture;
                return existing;
            }

            TextureRect? clone = icon.Duplicate() as TextureRect;
            if (clone == null)
            {
                return null;
            }

            clone.Name = SecondIconName;
            clone.MouseFilter = Control.MouseFilterEnum.Ignore;
            container.AddChild(clone);
            return clone;
        }

        private static bool IsCombatPoint(MapPointType type)
        {
            return type == MapPointType.Monster || type == MapPointType.Elite || type == MapPointType.Boss;
        }

        private static void ApplyColor(TextureRect icon, Color targetColor, Node tweenOwner, bool animate)
        {
            if (!animate)
            {
                icon.SelfModulate = targetColor;
                return;
            }

            Tween tween = tweenOwner.CreateTween();
            tween.TweenProperty(icon, "self_modulate", targetColor, 0.5f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
    }

    [HarmonyPatch(typeof(NNormalMapPoint), "RefreshColorInstantly")]
    public class NNormalMapPointRefreshColorPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NNormalMapPoint __instance)
        {
            MapPointPatchStyle.Apply(__instance, animate: false);
        }
    }

    [HarmonyPatch(typeof(NNormalMapPoint), "RefreshState")]
    public class NNormalMapPointRefreshStatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(NNormalMapPoint __instance)
        {
            MapPointPatchStyle.Apply(__instance, animate: false);
        }
    }

    [HarmonyPatch(typeof(NNormalMapPoint), "AnimUnhover")]
    public class NNormalMapPointAnimUnhoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NNormalMapPoint __instance)
        {
            // Original tween pushes color back to TargetColor; run a follow-up tween to keep custom colors.
            MapPointPatchStyle.Apply(__instance, animate: true);
        }
    }
}
