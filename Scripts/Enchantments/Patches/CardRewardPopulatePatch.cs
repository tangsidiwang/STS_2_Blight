using System;
using System.Linq;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(CardReward), nameof(CardReward.Populate))]
    public static class CardRewardPopulatePatch
    {
        private const int MaxRollAttemptsPerLayer = 8;

        [HarmonyPostfix]
        public static void Postfix(CardReward __instance)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return;
            }

            if (!ShouldEnchantReward(__instance))
            {
                return;
            }

            try
            {
                var cards = __instance.Cards?.ToList();
                if (cards == null || cards.Count == 0)
                {
                    return;
                }

                if (!RewardGenerationContext.TryGet(__instance.Player, out bool isElite, out bool isMutant))
                {
                    ResolveRewardContext(__instance, out isElite, out isMutant);
                }

                int targetPositiveLayerCount = (isElite ? 2 : 1) + (isMutant ? 1 : 0);
                int targetNegativeLayerCount = isMutant ? 1 : 0;

                for (int i = 0; i < cards.Count; i++)
                {
                    var existingEntries = BlightEnchantmentManager.GetEntries(cards[i]);
                    int existingPositiveCount = existingEntries.Count(e => !e.IsNegative && e.Rarity != BlightEnchantmentRarity.Negative);
                    int existingNegativeCount = existingEntries.Count(e => e.IsNegative || e.Rarity == BlightEnchantmentRarity.Negative);

                    for (int layer = existingPositiveCount; layer < targetPositiveLayerCount; layer++)
                    {
                        bool applied = false;
                        for (int attempt = 0; attempt < MaxRollAttemptsPerLayer && !applied; attempt++)
                        {
                            applied = BlightEnchantmentManager.TryApplyRandomByPool(cards[i], isElite, isMutant);
                        }

                        if (!applied)
                        {
                            Log.Warn($"[Blight][Enchant] Failed to apply positive layer after retries: card={cards[i].Id}, layer={layer + 1}/{targetPositiveLayerCount}, existingPositive={existingPositiveCount}, isElite={isElite}, isMutant={isMutant}");
                        }
                    }

                    for (int layer = existingNegativeCount; layer < targetNegativeLayerCount; layer++)
                    {
                        bool negativeApplied = BlightEnchantmentManager.TryApplyMutantCurse(cards[i], isElite);
                        if (!negativeApplied)
                        {
                            Log.Warn($"[Blight][Enchant] Failed to apply negative layer: card={cards[i].Id}, layer={layer + 1}/{targetNegativeLayerCount}, existingNegative={existingNegativeCount}, isElite={isElite}, isMutant={isMutant}");
                        }
                    }
                }

                RefreshRewardScreenIfOpen(__instance);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] CardRewardPopulatePatch failed: {e}");
            }
        }

        private static bool ShouldEnchantReward(CardReward reward)
        {
            if (reward == null)
            {
                return false;
            }

            var optionsField = AccessTools.Field(typeof(CardReward), "<Options>k__BackingField");
            if (optionsField?.GetValue(reward) is not CardCreationOptions options)
            {
                return false;
            }

            if (options.Source != CardCreationSource.Encounter)
            {
                return false;
            }

            RoomType? roomType = reward.Player?.RunState?.CurrentRoom?.RoomType;
            return roomType == RoomType.Monster
                || roomType == RoomType.Elite
                || roomType == RoomType.Boss;
        }

        private static void ResolveRewardContext(CardReward reward, out bool isElite, out bool isMutant)
        {
            isElite = false;
            isMutant = false;

            RoomType? currentRoomType = reward?.Player?.RunState?.CurrentRoom?.RoomType;
            if (currentRoomType == RoomType.Boss)
            {
                // Boss reward enchantment follows normal elite rules.
                isElite = true;
                isMutant = false;
                return;
            }

            var runState = reward?.Player?.RunState;
            var point = runState?.CurrentMapPoint;
            if (point == null)
            {
                return;
            }

            isElite = point.PointType == MapPointType.Elite;

            string seed = runState?.Rng.StringSeed ?? string.Empty;
            if (!string.IsNullOrEmpty(seed))
            {
                isMutant = BlightModeManager.IsNodeMutant(point, seed);
            }
        }

        private static void RefreshRewardScreenIfOpen(CardReward reward)
        {
            try
            {
                var screenField = AccessTools.Field(typeof(CardReward), "_currentlyShownScreen");
                var cardsField = AccessTools.Field(typeof(CardReward), "_cards");

                if (screenField?.GetValue(reward) is not NCardRewardSelectionScreen screen)
                {
                    return;
                }

                if (cardsField?.GetValue(reward) is not System.Collections.IEnumerable rawCards)
                {
                    return;
                }

                var cardResults = rawCards.Cast<object>()
                    .OfType<CardCreationResult>()
                    .ToList();

                var alternatives = CardRewardAlternative.Generate(reward);
                screen.RefreshOptions(cardResults, alternatives);
            }
            catch (Exception e)
            {
                Log.Warn($"[Blight][Enchant] RefreshRewardScreenIfOpen failed: {e.Message}");
            }
        }
    }
}
