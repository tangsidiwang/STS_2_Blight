using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(RunManager), "ProceedFromTerminalRewardsScreen")]
    public static class DoubleWaveProceedPatch
    {
        public static bool Prefix(RunManager __instance, ref Task __result)
        {
            try
            {
                var state = __instance.DebugOnlyGetState();
                var currentCoord = state?.CurrentMapCoord;
                var currentPoint = state?.CurrentMapPoint;

                if (state == null || currentCoord == null || currentPoint == null)
                {
                    return true;
                }

                if (!BlightModeManager.TryStartSecondWave(state, currentPoint))
                {
                    return true;
                }

                __result = TaskHelper.RunSafely(__instance.EnterMapPointInternal(
                    currentCoord.Value.row + 1,
                    currentPoint.PointType,
                    currentCoord,
                    preFinishedRoom: null,
                    saveGame: true));
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"Blight DoubleWaveProceedPatch failed: {e}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapPointInternal))]
    public static class DoubleWaveEnterMapPointPatch
    {
        public static void Prefix(RunManager __instance)
        {
            if (!BlightModeManager.ConsumePendingSecondWaveTransition())
            {
                return;
            }

            DoubleWaveActChangeSynchronizerPatch.ResetPlayerChoiceSynchronization(__instance.DebugOnlyGetState());
        }
    }

    [HarmonyPatch(typeof(NRewardsScreen), "OnProceedButtonPressed")]
    public static class DoubleWaveRewardsProceedSyncPatch
    {
        private static readonly AccessTools.FieldRef<NRewardsScreen, bool> IsTerminalRef =
            AccessTools.FieldRefAccess<NRewardsScreen, bool>("_isTerminal");

        private static readonly AccessTools.FieldRef<NRewardsScreen, IRunState> RunStateRef =
            AccessTools.FieldRefAccess<NRewardsScreen, IRunState>("_runState");

        private static readonly AccessTools.FieldRef<NRewardsScreen, NProceedButton> ProceedButtonRef =
            AccessTools.FieldRefAccess<NRewardsScreen, NProceedButton>("_proceedButton");

        private static readonly AccessTools.FieldRef<NRewardsScreen, Control> WaitingOverlayRef =
            AccessTools.FieldRefAccess<NRewardsScreen, Control>("_waitingForOtherPlayersOverlay");

        private static readonly System.Reflection.MethodInfo? TryEnableProceedButtonMethod =
            AccessTools.Method(typeof(NRewardsScreen), "TryEnableProceedButton");

        private static readonly System.Reflection.FieldInfo? IsCompleteBackingField =
            AccessTools.Field(typeof(NRewardsScreen), "<IsComplete>k__BackingField");

        public static bool Prefix(NRewardsScreen __instance)
        {
            try
            {
                if (!IsTerminalRef(__instance))
                {
                    return true;
                }

                if (RunStateRef(__instance) is not RunState state)
                {
                    return true;
                }

                if (!ShouldSyncSecondWaveProceed(state))
                {
                    return true;
                }

                NProceedButton? proceedButton = ProceedButtonRef(__instance);
                proceedButton?.Disable();

                IsCompleteBackingField?.SetValue(__instance, true);
                __instance.EmitSignal(NRewardsScreen.SignalName.Completed);

                if (RunManager.Instance.ActChangeSynchronizer.IsWaitingForOtherPlayers())
                {
                    Control? waitingOverlay = WaitingOverlayRef(__instance);
                    if (waitingOverlay != null)
                    {
                        waitingOverlay.Visible = true;
                    }
                }

                RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"Blight DoubleWaveRewardsProceedSyncPatch failed: {e}");
                return true;
            }
        }

        internal static bool ShouldSyncSecondWaveProceed(RunState? state)
        {
            if (state == null || RunManager.Instance?.NetService == null)
            {
                return false;
            }

            if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            {
                return false;
            }

            return BlightModeManager.CanStartSecondWave(state, state.CurrentMapPoint);
        }

        internal static void CloseRewardsScreenAfterSync(NRewardsScreen screen)
        {
            if (screen == null || !GodotObject.IsInstanceValid(screen))
            {
                return;
            }

            screen.HideWaitingForPlayersScreen();
            TryEnableProceedButtonMethod?.Invoke(screen, null);
            NOverlayStack.Instance?.Remove(screen);
        }
    }

    [HarmonyPatch(typeof(ActChangeSynchronizer), nameof(ActChangeSynchronizer.OnPlayerReady))]
    public static class DoubleWaveActChangeSynchronizerPatch
    {
        private static readonly AccessTools.FieldRef<ActChangeSynchronizer, RunState> RunStateRef =
            AccessTools.FieldRefAccess<ActChangeSynchronizer, RunState>("_runState");

        private static readonly AccessTools.FieldRef<ActChangeSynchronizer, List<bool>> ReadyPlayersRef =
            AccessTools.FieldRefAccess<ActChangeSynchronizer, List<bool>>("_readyPlayers");

        internal static readonly System.Reflection.FieldInfo? ChoiceIdsField =
            AccessTools.Field(typeof(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer), "_choiceIds");

        internal static readonly System.Reflection.FieldInfo? ReceivedChoicesField =
            AccessTools.Field(typeof(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer), "_receivedChoices");

        public static bool Prefix(ActChangeSynchronizer __instance, Player player)
        {
            try
            {
                RunState state = RunStateRef(__instance);
                if (!DoubleWaveRewardsProceedSyncPatch.ShouldSyncSecondWaveProceed(state))
                {
                    return true;
                }

                List<bool> readyPlayers = ReadyPlayersRef(__instance);
                int playerSlotIndex = state.GetPlayerSlotIndex(player);
                readyPlayers[playerSlotIndex] = true;

                foreach (bool isReady in readyPlayers)
                {
                    if (!isReady)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < readyPlayers.Count; i++)
                {
                    readyPlayers[i] = false;
                }

                var currentPoint = state.CurrentMapPoint;
                if (currentPoint == null || !BlightModeManager.TryStartSecondWave(state, currentPoint))
                {
                    return false;
                }

                MapCoord? currentCoord = state.CurrentMapCoord;
                if (!currentCoord.HasValue)
                {
                    return false;
                }

                if (NOverlayStack.Instance?.Peek() is NRewardsScreen rewardsScreen)
                {
                    DoubleWaveRewardsProceedSyncPatch.CloseRewardsScreenAfterSync(rewardsScreen);
                }

                TaskHelper.RunSafely(BeginSecondWaveTransitionAsync(currentCoord.Value, currentPoint.PointType));
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"Blight DoubleWaveActChangeSynchronizerPatch failed: {e}");
                return true;
            }
        }

        internal static void ResetPlayerChoiceSynchronization(RunState? state)
        {
            if (state == null)
            {
                return;
            }

            var synchronizer = RunManager.Instance?.PlayerChoiceSynchronizer;
            if (synchronizer == null)
            {
                return;
            }

            if (ChoiceIdsField?.GetValue(synchronizer) is System.Collections.IList choiceIds)
            {
                choiceIds.Clear();
                for (int i = 0; i < state.Players.Count; i++)
                {
                    choiceIds.Add(0u);
                }
            }

            if (ReceivedChoicesField?.GetValue(synchronizer) is System.Collections.IList receivedChoices)
            {
                receivedChoices.Clear();
            }
        }

        private static async Task BeginSecondWaveTransitionAsync(MapCoord currentCoord, MapPointType pointType)
        {
            try
            {
                if (RunManager.Instance?.ActionQueueSet != null)
                {
                    await RunManager.Instance.ActionQueueSet.BecameEmpty();
                }

                await Task.Yield();

                await RunManager.Instance.EnterMapPointInternal(
                    currentCoord.row + 1,
                    pointType,
                    currentCoord,
                    preFinishedRoom: null,
                    saveGame: true);
            }
            catch (Exception e)
            {
                Log.Error($"Blight BeginSecondWaveTransitionAsync failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(ScreenStateTracker), "OnOverlayStackChanged")]
    public static class ScreenStateTrackerOverlayPatch
    {
        private static readonly AccessTools.FieldRef<ScreenStateTracker, NetScreenType> OverlayScreenRef =
            AccessTools.FieldRefAccess<ScreenStateTracker, NetScreenType>("_overlayScreen");

        private static readonly AccessTools.FieldRef<ScreenStateTracker, NRewardsScreen> ConnectedRewardsScreenRef =
            AccessTools.FieldRefAccess<ScreenStateTracker, NRewardsScreen>("_connectedRewardsScreen");

        public static bool Prefix(ScreenStateTracker __instance)
        {
            if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
            {
                return false;
            }

            IOverlayScreen overlayScreen = NOverlayStack.Instance.Peek();
            if (overlayScreen is NRewardsScreen rewardsScreen)
            {
                ConnectedRewardsScreenRef(__instance) = rewardsScreen;
                Callable completedCallback = Callable.From(new Action(SyncLocalScreen));
                if (!rewardsScreen.IsConnected(NRewardsScreen.SignalName.Completed, completedCallback))
                {
                    rewardsScreen.Connect(NRewardsScreen.SignalName.Completed, completedCallback);
                }
            }
            else
            {
                ConnectedRewardsScreenRef(__instance) = null;
            }

            OverlayScreenRef(__instance) = overlayScreen?.ScreenType ?? NetScreenType.None;
            SyncLocalScreen();
            return false;

            void SyncLocalScreen()
            {
                RunManager.Instance.InputSynchronizer.SyncLocalScreen(GetCurrentScreen());
            }

            NetScreenType GetCurrentScreen()
            {
                if (overlayScreen is NRewardsScreen { IsComplete: false })
                {
                    return NetScreenType.Rewards;
                }

                return overlayScreen?.ScreenType ?? NetScreenType.None;
            }
        }
    }
}
