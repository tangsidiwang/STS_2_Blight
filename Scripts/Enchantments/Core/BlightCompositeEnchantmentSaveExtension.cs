using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Enchantments
{
    public sealed class BlightCompositeEnchantmentSaveExtension : IBlightSaveLoadExtension
    {
        private const string SavePropertyName = "BLIGHT_MULTI_ENCHANT_V1";

        public int Priority => -100;

        public void OnCardSerializing(CardModel card, SerializableCard save)
        {
            // Do not write custom key-value entries into SerializableCard.Props.
            // Packet serialization requires SavedProperty names to be pre-mapped in SavedPropertiesTypeCache,
            // and arbitrary names can hard-fail turn checksums in combat.
            RemoveSavedString(save?.Props, SavePropertyName);
        }

        public void OnCardDeserialized(SerializableCard save, CardModel card)
        {
            if (save == null || card == null)
            {
                return;
            }

            if (!TryGetSavedString(save.Props, SavePropertyName, out string payloadText) || string.IsNullOrWhiteSpace(payloadText))
            {
                return;
            }

            CompositePayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<CompositePayload>(payloadText);
            }
            catch (Exception e)
            {
                Log.Warn($"[Blight][SaveLoad] Invalid composite payload on card={card.Id}: {e.Message}");
                RemoveSavedString(save.Props, SavePropertyName);
                return;
            }

            List<BlightEnchantmentEntry> entries = ConvertEntries(payload);
            if (entries.Count == 0)
            {
                RemoveSavedString(save.Props, SavePropertyName);
                return;
            }

            if (card.Enchantment is BlightCompositeEnchantment existing && HasSameEntries(existing.Entries, entries))
            {
                RemoveSavedString(save.Props, SavePropertyName);
                return;
            }

            card.ClearEnchantmentInternal();
            BlightCompositeEnchantment restored = (BlightCompositeEnchantment)ModelDb.Enchantment<BlightCompositeEnchantment>().ToMutable();
            card.EnchantInternal(restored, 1m);
            foreach (BlightEnchantmentEntry entry in entries)
            {
                restored.TryAddEntry(entry);
                ApplyRestoredEntryCardMutation(card, entry);
            }

            restored.RecalculateValues();
            card.FinalizeUpgradeInternal();
            RemoveSavedString(save.Props, SavePropertyName);
            Log.Info($"[Blight][SaveLoad] Rehydrated composite enchantment for card={card.Id}, entries={entries.Count}.");
        }

        private static void ApplyRestoredEntryCardMutation(CardModel card, BlightEnchantmentEntry entry)
        {
            EnchantmentModel runtime = SaveUtil.EnchantmentOrDeprecated(entry.EnchantmentId).ToMutable();
            runtime.ApplyInternal(card, entry.Amount);
            runtime.ModifyCard();
        }

        private static List<BlightEnchantmentEntry> ConvertEntries(CompositePayload? payload)
        {
            var result = new List<BlightEnchantmentEntry>();
            if (payload?.Entries == null)
            {
                return result;
            }

            foreach (EntryPayload entry in payload.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.EnchantmentId))
                {
                    continue;
                }

                ModelId id;
                try
                {
                    id = ModelId.Deserialize(entry.EnchantmentId);
                }
                catch
                {
                    continue;
                }

                result.Add(new BlightEnchantmentEntry
                {
                    EnchantmentId = id,
                    Amount = entry.Amount,
                    Rarity = Enum.IsDefined(typeof(BlightEnchantmentRarity), entry.Rarity)
                        ? (BlightEnchantmentRarity)entry.Rarity
                        : BlightEnchantmentRarity.Common,
                    IsNegative = entry.IsNegative,
                });

                if (result.Count >= BlightCompositeEnchantment.MaxEntries)
                {
                    break;
                }
            }

            return result;
        }

        private static bool HasSameEntries(IReadOnlyList<BlightEnchantmentEntry> left, IReadOnlyList<BlightEnchantmentEntry> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                BlightEnchantmentEntry a = left[i];
                BlightEnchantmentEntry b = right[i];
                if (a.EnchantmentId != b.EnchantmentId
                    || a.Amount != b.Amount
                    || a.Rarity != b.Rarity
                    || a.IsNegative != b.IsNegative)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetSavedString(SavedProperties props, string key, out string value)
        {
            value = string.Empty;
            if (props?.strings == null)
            {
                return false;
            }

            foreach (SavedProperties.SavedProperty<string> item in props.strings)
            {
                if (!string.Equals(item.name, key, StringComparison.Ordinal))
                {
                    continue;
                }

                value = item.value ?? string.Empty;
                return true;
            }

            return false;
        }

        private static void RemoveSavedString(SavedProperties props, string key)
        {
            if (props?.strings == null)
            {
                return;
            }

            props.strings.RemoveAll(s => string.Equals(s.name, key, StringComparison.Ordinal));
        }

        private sealed class CompositePayload
        {
            public List<EntryPayload> Entries { get; set; } = new List<EntryPayload>();
        }

        private sealed class EntryPayload
        {
            public string EnchantmentId { get; set; } = string.Empty;

            public int Amount { get; set; }

            public int Rarity { get; set; }

            public bool IsNegative { get; set; }
        }
    }
}
