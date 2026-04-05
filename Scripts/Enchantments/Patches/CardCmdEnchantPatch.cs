using System;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Enchant), new[] { typeof(EnchantmentModel), typeof(CardModel), typeof(decimal) })]
    public static class CardCmdEnchantPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(EnchantmentModel enchantment, CardModel card, decimal amount, ref EnchantmentModel? __result)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return true;
            }

            if (enchantment == null || card == null || amount <= 0m)
            {
                return true;
            }

            // Only intercept blight enchantments. Let vanilla flow handle all other enchant types.
            if (enchantment is not IBlightEnchantment metadata)
            {
                return true;
            }

            var rarity = metadata?.Rarity ?? BlightEnchantmentRarity.Common;
            bool isNegative = rarity == BlightEnchantmentRarity.Negative;

            bool applied = BlightEnchantmentManager.TryApply(
                card,
                enchantment,
                Math.Max(1, (int)Math.Round(amount)),
                rarity,
                isNegative);

            if (!applied)
            {
                return true;
            }

            __result = card.Enchantment;
            return false;
        }
    }
}
