using System;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BlightMod.Patches
{
    internal static class MultiplayerAscensionResyncPatchHelper
    {
        public static void TryResyncToPlayer(NCharacterSelectScreen screen, LobbyPlayer player, string reason)
        {
            try
            {
                if (!BlightModeManager.IsBlightModeActive || screen?.Lobby == null)
                {
                    return;
                }

                if (screen.Lobby.NetService.Type != NetGameType.Host)
                {
                    return;
                }

                if (player.id == screen.Lobby.NetService.NetId)
                {
                    return;
                }

                int ascension = Math.Clamp(BlightModeManager.BlightAscensionLevel, 0, 5);
                screen.Lobby.NetService.SendMessage(new LobbyAscensionChangedMessage
                {
                    ascension = ascension
                }, player.id);

                MegaCrit.Sts2.Core.Logging.Log.Info($"[Blight] Resynced ascension={ascension} to player {player.id} ({reason}).");
            }
            catch (Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"[Blight] Ascension resync failed ({reason}): {e}");
            }
        }
    }

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.PlayerConnected))]
    public static class CharacterSelectScreenPlayerConnectedAscensionResyncPatch
    {
        public static void Postfix(NCharacterSelectScreen __instance, LobbyPlayer player)
        {
            MultiplayerAscensionResyncPatchHelper.TryResyncToPlayer(__instance, player, "player joined");
        }
    }

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.PlayerChanged))]
    public static class CharacterSelectScreenPlayerChangedAscensionResyncPatch
    {
        public static void Postfix(NCharacterSelectScreen __instance, LobbyPlayer player)
        {
            MultiplayerAscensionResyncPatchHelper.TryResyncToPlayer(__instance, player, "player changed");
        }
    }
}