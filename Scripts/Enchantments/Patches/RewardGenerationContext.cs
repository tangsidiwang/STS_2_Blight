using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BlightMod.Enchantments.Patches
{
    internal static class RewardGenerationContext
    {
        private readonly struct Context
        {
            public Context(bool isElite, bool isMutant)
            {
                IsElite = isElite;
                IsMutant = isMutant;
            }

            public bool IsElite { get; }

            public bool IsMutant { get; }
        }

        private static readonly Dictionary<ulong, Context> ContextByPlayerId = new Dictionary<ulong, Context>();

        public static void Set(Player player, bool isElite, bool isMutant)
        {
            if (player == null)
            {
                return;
            }

            ContextByPlayerId[player.NetId] = new Context(isElite, isMutant);
        }

        public static bool TryGet(Player player, out bool isElite, out bool isMutant)
        {
            isElite = false;
            isMutant = false;
            if (player == null)
            {
                return false;
            }

            if (!ContextByPlayerId.TryGetValue(player.NetId, out Context context))
            {
                return false;
            }

            isElite = context.IsElite;
            isMutant = context.IsMutant;
            return true;
        }
    }
}
