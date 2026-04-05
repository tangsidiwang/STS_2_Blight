using System.Collections.Generic;
using System.Linq;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade), new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) })]
    public static class CardCmdUpgradePreserveCompositePatch
    {
        [HarmonyPrefix]
        public static void Prefix(IEnumerable<CardModel> cards, ref Dictionary<CardModel, List<BlightEnchantmentEntry>>? __state)
        {
            __state = null;
            if (!BlightModeManager.IsBlightModeActive || cards == null)
            {
                return;
            }

            List<CardModel> snapshot = cards.Where(c => c != null).ToList();
            Dictionary<CardModel, List<BlightEnchantmentEntry>> map = new Dictionary<CardModel, List<BlightEnchantmentEntry>>();
            foreach (CardModel card in snapshot)
            {
                if (card.Enchantment is not BlightCompositeEnchantment composite || composite.Entries.Count == 0)
                {
                    continue;
                }

                map[card] = composite.Entries
                    .Select(e => new BlightEnchantmentEntry
                    {
                        EnchantmentId = e.EnchantmentId,
                        Amount = e.Amount,
                        Rarity = e.Rarity,
                        IsNegative = e.IsNegative,
                    })
                    .ToList();
            }

            if (map.Count > 0)
            {
                __state = map;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Dictionary<CardModel, List<BlightEnchantmentEntry>>? __state)
        {
            if (!BlightModeManager.IsBlightModeActive || __state == null || __state.Count == 0)
            {
                return;
            }

            foreach ((CardModel card, List<BlightEnchantmentEntry> entries) in __state)
            {
                if (card == null || entries == null || entries.Count == 0 || card.HasBeenRemovedFromState)
                {
                    continue;
                }

                if (card.Enchantment is BlightCompositeEnchantment existing && existing.Entries.Count == entries.Count)
                {
                    continue;
                }

                card.ClearEnchantmentInternal();
                BlightCompositeEnchantment restored = (BlightCompositeEnchantment)ModelDb.Enchantment<BlightCompositeEnchantment>().ToMutable();
                card.EnchantInternal(restored, 1m);

                foreach (BlightEnchantmentEntry entry in entries)
                {
                    restored.TryAddEntry(new BlightEnchantmentEntry
                    {
                        EnchantmentId = entry.EnchantmentId,
                        Amount = entry.Amount,
                        Rarity = entry.Rarity,
                        IsNegative = entry.IsNegative,
                    });
                }

                restored.RecalculateValues();
                card.FinalizeUpgradeInternal();
            }
        }
    }
}
