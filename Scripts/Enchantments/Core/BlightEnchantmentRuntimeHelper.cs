using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Enchantments
{
    public static class BlightEnchantmentRuntimeHelper
    {
        private static readonly FieldInfo? CardEnchantmentChangedField =
            typeof(CardModel).GetField("EnchantmentChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool HasEnchantment<T>(CardModel card) where T : EnchantmentModel
        {
            return FindEnchantment<T>(card) != null;
        }

        public static T? FindEnchantment<T>(CardModel? card) where T : EnchantmentModel
        {
            if (card == null)
            {
                return null;
            }

            return EnumerateEnchantments(card).OfType<T>().FirstOrDefault();
        }

        public static IEnumerable<EnchantmentModel> EnumerateEnchantments(CardModel? card)
        {
            if (card?.Enchantment == null)
            {
                yield break;
            }

            if (card.Enchantment is BlightCompositeEnchantment composite)
            {
                foreach (BlightEnchantmentEntry entry in composite.Entries)
                {
                    EnchantmentModel? enchantment = TryCreateRuntimeEnchantment(entry, card);
                    if (enchantment != null)
                    {
                        yield return enchantment;
                    }
                }

                yield break;
            }

            yield return card.Enchantment;
        }

        public static void SyncDeckVersionEnchantment<T>(CardModel? card, int amountDelta)
            where T : EnchantmentModel
        {
            if (card == null || amountDelta == 0)
            {
                return;
            }

            CardModel? deckVersion = card.DeckVersion;
            if (deckVersion == null || ReferenceEquals(deckVersion, card))
            {
                return;
            }

            T? existing = FindEnchantment<T>(deckVersion);
            if (existing != null)
            {
                existing.Amount += amountDelta;
                existing.RecalculateValues();
            }

            deckVersion.DynamicVars.RecalculateForUpgradeOrEnchant();
            deckVersion.FinalizeUpgradeInternal();
            TriggerEnchantmentChanged(deckVersion);
        }

        public static void SyncDeckVersionEnchantment(CardModel? card, ModelId enchantmentId, int amountDelta)
        {
            if (card == null || amountDelta == 0)
            {
                return;
            }

            CardModel? deckVersion = card.DeckVersion;
            if (deckVersion == null || ReferenceEquals(deckVersion, card) || enchantmentId == null)
            {
                return;
            }

            bool changed = false;
            if (deckVersion.Enchantment is BlightCompositeEnchantment composite)
            {
                foreach (BlightEnchantmentEntry entry in composite.Entries)
                {
                    if (!entry.EnchantmentId.Equals(enchantmentId))
                    {
                        continue;
                    }

                    entry.Amount += amountDelta;
                    changed = true;
                    break;
                }

                if (changed)
                {
                    composite.RecalculateValues();
                }
            }
            else if (deckVersion.Enchantment != null && deckVersion.Enchantment.Id.Equals(enchantmentId))
            {
                deckVersion.Enchantment.Amount += amountDelta;
                deckVersion.Enchantment.RecalculateValues();
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            deckVersion.DynamicVars.RecalculateForUpgradeOrEnchant();
            deckVersion.FinalizeUpgradeInternal();
            TriggerEnchantmentChanged(deckVersion);
        }

        public static void TriggerEnchantmentChanged(CardModel? card)
        {
            if (card == null)
            {
                return;
            }

            if (CardEnchantmentChangedField?.GetValue(card) is Action action)
            {
                action();
            }
        }

        private static EnchantmentModel? TryCreateRuntimeEnchantment(BlightEnchantmentEntry entry, CardModel card)
        {
            if (entry?.EnchantmentId == null)
            {
                return null;
            }

            EnchantmentModel enchantment;
            try
            {
                enchantment = SaveUtil.EnchantmentOrDeprecated(entry.EnchantmentId).ToMutable();
            }
            catch
            {
                return null;
            }

            if (enchantment is BlightCompositeEnchantment)
            {
                return null;
            }

            try
            {
                enchantment.ApplyInternal(card, entry.Amount);
            }
            catch
            {
                return null;
            }

            return enchantment;
        }
    }
}