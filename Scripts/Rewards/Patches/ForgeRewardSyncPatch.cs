using System;
using System.Linq;
using System.Threading.Tasks;
using BlightMod.Rewards.ForgeOptions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Rewards.Patches
{
    public sealed class ForgeRewardStateSyncMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
    {
        public bool ShouldBroadcast => true;

        public NetTransferMode Mode => NetTransferMode.Reliable;

        public LogLevel LogLevel => LogLevel.Debug;

        public RunLocation Location { get; set; }

        public ulong TargetPlayerId { get; set; }

        public SerializablePlayer PlayerState { get; set; } = new SerializablePlayer();

        public void Serialize(PacketWriter writer)
        {
            writer.Write(Location);
            writer.WriteULong(TargetPlayerId);
            writer.Write(PlayerState);
        }

        public void Deserialize(PacketReader reader)
        {
            Location = reader.Read<RunLocation>();
            TargetPlayerId = reader.ReadULong();
            PlayerState = reader.Read<SerializablePlayer>();
        }
    }

    [HarmonyPatch(typeof(ForgeRewardItem), "OnSelect")]
    public static class ForgeRewardSyncPatch
    {
        public static void Postfix(ForgeRewardItem __instance, Task<bool> __result)
        {
            if (__instance?.Player?.RunState == null || __result == null)
            {
                return;
            }

            _ = SyncAfterSelectionAsync(__instance.Player, __result);
        }

        private static async Task SyncAfterSelectionAsync(Player player, Task<bool> selectionTask)
        {
            try
            {
                bool success = await selectionTask;
                if (!success)
                {
                    return;
                }

                TryBroadcastPlayerState(player);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] Forge reward sync postfix failed: {e}");
            }
        }

        internal static void TryBroadcastPlayerState(Player player)
        {
            if (player?.RunState == null || RunManager.Instance?.NetService == null)
            {
                return;
            }

            if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            {
                return;
            }

            var buffer = RunManager.Instance.RunLocationTargetedBuffer;
            if (buffer == null)
            {
                return;
            }

            ForgeRewardStateSyncMessage message = new ForgeRewardStateSyncMessage
            {
                Location = buffer.CurrentLocation,
                TargetPlayerId = player.NetId,
                PlayerState = player.ToSerializable()
            };

            RunManager.Instance.NetService.SendMessage(message);
        }
    }

    [HarmonyPatch(typeof(OneOffSynchronizer), MethodType.Constructor, typeof(RunLocationTargetedMessageBuffer), typeof(MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService), typeof(IPlayerCollection), typeof(ulong))]
    public static class OneOffSynchronizerForgeRewardStateRegisterPatch
    {
        public static void Postfix(OneOffSynchronizer __instance)
        {
            try
            {
                RunManager.Instance?.RunLocationTargetedBuffer?.RegisterMessageHandler<ForgeRewardStateSyncMessage>(HandleForgeRewardStateSyncMessage);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] Failed to register forge reward state sync handler: {e}");
            }
        }

        private static void HandleForgeRewardStateSyncMessage(ForgeRewardStateSyncMessage message, ulong senderId)
        {
            try
            {
                if (message?.PlayerState == null || RunManager.Instance?.DebugOnlyGetState() == null)
                {
                    return;
                }

                if (message.TargetPlayerId != senderId)
                {
                    return;
                }

                Player targetPlayer = RunManager.Instance.DebugOnlyGetState().GetPlayer(senderId);
                if (targetPlayer == null)
                {
                    return;
                }

                if (targetPlayer.NetId != message.PlayerState.NetId)
                {
                    return;
                }

                targetPlayer.SyncWithSerializedPlayer(message.PlayerState);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] Forge reward state sync handler failed: {e}");
            }
        }
    }
}
