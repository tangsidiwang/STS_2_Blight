using System;
using System.Linq;
using BlightMod.Core;
using BlightMod.Enchantments;
using BlightMod.Enchantments.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace BlightMod.Rewards.Patches
{
    [HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
    public static class CombatRewardPatch
    {
        [HarmonyPostfix]
        public static void Postfix(RewardsSet __instance, AbstractRoom room)
        {
            if (!BlightModeManager.IsBlightModeActive || __instance?.Player == null)
            {
                return;
            }

            if (room == null || (room.RoomType != RoomType.Monster && room.RoomType != RoomType.Elite && room.RoomType != RoomType.Boss))
            {
                return;
            }

            try
            {
                bool isBoss = room.RoomType == RoomType.Boss;
                bool isElite = room.RoomType == RoomType.Elite || isBoss;
                var point = __instance.Player.RunState?.CurrentMapPoint;
                string seed = __instance.Player.RunState?.Rng.StringSeed ?? string.Empty;
                bool isMutant = !isBoss
                    && point != null
                    && !string.IsNullOrEmpty(seed)
                    && BlightModeManager.IsNodeMutant(point, seed);

                RewardGenerationContext.Set(__instance.Player, isElite, isMutant);

                if (!isElite && !isMutant)
                {
                    return;
                }

                if (__instance.Rewards.OfType<ForgeRewardItem>().Any())
                {
                    return;
                }

                var runState = __instance.Player.RunState;
                if (runState == null)
                {
                    return;
                }

                var rng = runState.Rng.CombatCardGeneration;
                float roll = rng.NextFloat();
                BlightEnchantmentRarity tier = RollForgeTier(roll, isElite, isMutant, runState.CurrentActIndex);
                Log.Info($"[Blight][Forge] Reward tier roll={roll:0.000}, isElite={isElite}, isMutant={isMutant}, tier={tier}");
                __instance.Rewards.Add(new ForgeRewardItem(tier, __instance.Player));
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] CombatRewardPatch failed: {e}");
            }
        }

        private static BlightEnchantmentRarity RollForgeTier(float roll, bool isElite, bool isMutant, int actIndex)
        {
            float rareBoost = GetRareBoostByAct(actIndex);
            float ultraBoost = GetUltraRareBoostByAct(actIndex);

            if (isElite && isMutant)
            {
                float ultraThreshold = 0.10f + ultraBoost;
                float rareThreshold = 0.60f - rareBoost;

                if (roll < ultraThreshold)
                {
                    return BlightEnchantmentRarity.UltraRare;
                }

                return roll < rareThreshold ? BlightEnchantmentRarity.Rare : BlightEnchantmentRarity.Uncommon;
            }

            if (isElite)
            {
                float ultraThreshold = 0.05f + ultraBoost;
                float rareThreshold = 0.20f + rareBoost;

                if (roll < ultraThreshold)
                {
                    return BlightEnchantmentRarity.UltraRare;
                }

                return roll < rareThreshold ? BlightEnchantmentRarity.Rare : BlightEnchantmentRarity.Uncommon;
            }

            if (isMutant)
            {
                float ultraThreshold = 0.03f + ultraBoost;
                float rareThreshold = 0.05f + ultraBoost + rareBoost;
                float uncommonThreshold = 0.20f + ultraBoost + rareBoost;

                if (roll < ultraThreshold)
                {
                    return BlightEnchantmentRarity.UltraRare;
                }

                if (roll < rareThreshold)
                {
                    return BlightEnchantmentRarity.Rare;
                }

                if (roll < uncommonThreshold)
                {
                    return BlightEnchantmentRarity.Uncommon;
                }

                return BlightEnchantmentRarity.Common;
            }

            return BlightEnchantmentRarity.Common;
        }

        private static float GetRareBoostByAct(int actIndex)
        {
            return actIndex switch
            {
                >= 3 => 0.06f,
                2 => 0.03f,
                _ => 0f,
            };
        }

        private static float GetUltraRareBoostByAct(int actIndex)
        {
            return actIndex switch
            {
                >= 3 => 0.02f,
                2 => 0.01f,
                _ => 0f,
            };
        }
    }
}
