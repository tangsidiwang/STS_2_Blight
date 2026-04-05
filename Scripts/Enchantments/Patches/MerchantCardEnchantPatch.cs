using System.Linq;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant), new[] { typeof(Player), typeof(System.Collections.Generic.IEnumerable<CardModel>), typeof(CardType) })]
    public static class MerchantCardEnchantByTypePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player player, CardCreationResult __result)
        {
            ApplyMerchantEnchant(player, __result);
        }

        private static void ApplyMerchantEnchant(Player player, CardCreationResult result)
        {
            MerchantCardEnchantLogic.Apply(player, result);
        }
    }

    [HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant), new[] { typeof(Player), typeof(System.Collections.Generic.IEnumerable<CardModel>), typeof(CardRarity) })]
    public static class MerchantCardEnchantByRarityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player player, CardCreationResult __result)
        {
            ApplyMerchantEnchant(player, __result);
        }

        private static void ApplyMerchantEnchant(Player player, CardCreationResult result)
        {
            MerchantCardEnchantLogic.Apply(player, result);
        }
    }

    internal static class MerchantCardEnchantLogic
    {
        private const int TargetPositiveLayerCount = 2;
        private const int MaxAttemptsPerLayer = 8;

        public static void Apply(Player player, CardCreationResult result)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return;
            }

            if (player == null || result?.Card is not CardModel card)
            {
                return;
            }

            var entries = BlightEnchantmentManager.GetEntries(card);
            int positiveCount = entries.Count(e => !e.IsNegative && e.Rarity != BlightEnchantmentRarity.Negative);

            for (int layer = positiveCount; layer < TargetPositiveLayerCount; layer++)
            {
                bool applied = false;
                for (int attempt = 0; attempt < MaxAttemptsPerLayer && !applied; attempt++)
                {
                    applied = BlightEnchantmentManager.TryApplySpecific(card, BlightEnchantmentRarity.Uncommon);
                }

                if (!applied)
                {
                    Log.Warn($"[Blight][MerchantEnchant] Failed to apply uncommon layer: card={card.Id}, layer={layer + 1}/{TargetPositiveLayerCount}");
                    break;
                }
            }

            int finalPositiveCount = BlightEnchantmentManager
                .GetEntries(card)
                .Count(e => !e.IsNegative && e.Rarity != BlightEnchantmentRarity.Negative);
            Log.Info($"[Blight][MerchantEnchant] Merchant card enchanted: card={card.Id}, targetLayers={TargetPositiveLayerCount}, finalPositiveLayers={finalPositiveCount}, rarity={BlightEnchantmentRarity.Uncommon}");
        }
    }
}
