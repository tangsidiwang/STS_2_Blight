using System;
using System.Collections.Generic;
using System.Linq;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Enchantments
{
    public static class BlightEnchantmentManager
    {
        public static bool TryApply(
            CardModel card,
            EnchantmentModel enchantment,
            int amount,
            BlightEnchantmentRarity rarity,
            bool isNegative,
            string source = "unknown")
        {
            if (!BlightModeManager.IsBlightModeActive || card == null || enchantment == null || amount <= 0)
            {
                Log.Info($"[Blight][EnchantTrace] TryApply early reject: source={source}, blightActive={BlightModeManager.IsBlightModeActive}, cardNull={card == null}, enchantNull={enchantment == null}, amount={amount}");
                return false;
            }

            bool suspiciousTarget = card.Type == CardType.Curse
                || card.Type == CardType.Status
                || card.Type == CardType.None
                || card.IsBasicStrikeOrDefend
                || card.Keywords.Contains(CardKeyword.Unplayable);

            if (suspiciousTarget)
            {
                Log.Warn($"[Blight][EnchantTrace] Suspicious target before apply: source={source}, card={card.Id}, type={card.Type}, basicStrikeOrDefend={card.IsBasicStrikeOrDefend}, unplayable={card.Keywords.Contains(CardKeyword.Unplayable)}, enchant={enchantment.Id}, rarity={rarity}, isNegative={isNegative}, amount={amount}");
            }

            if (card.Enchantment is BlightCompositeEnchantment existingComposite && existingComposite.Entries.Count >= BlightCompositeEnchantment.MaxEntries)
            {
                Log.Info($"[Blight][EnchantTrace] TryApply reject: source={source}, existing composite full entries={existingComposite.Entries.Count}");
                return false;
            }

            if (card.Enchantment == null)
            {
                if (!CanApplySingle(card, enchantment))
                {
                    Log.Info($"[Blight][EnchantTrace] TryApply reject: source={source}, CanApplySingle false for card={card.Id}, enchant={enchantment.Id}");
                    return false;
                }

                card.EnchantInternal(enchantment, amount);
                enchantment.ModifyCard();
                card.FinalizeUpgradeInternal();
                Log.Info($"[Blight][EnchantTrace] TryApply success(single): source={source}, card={card.Id}, enchant={enchantment.Id}, amount={amount}, rarity={rarity}");
                return true;
            }

            BlightCompositeEnchantment composite = EnsureComposite(card);
            if (composite.Entries.Count >= BlightCompositeEnchantment.MaxEntries)
            {
                Log.Info($"[Blight][EnchantTrace] TryApply reject: source={source}, ensured composite full entries={composite.Entries.Count}");
                return false;
            }

            if (!CanAddEntry(composite, card, enchantment))
            {
                Log.Info($"[Blight][EnchantTrace] TryApply reject: source={source}, CanAddEntry false for card={card.Id}, enchant={enchantment.Id}");
                return false;
            }

            bool added = composite.TryAddEntry(new BlightEnchantmentEntry
            {
                EnchantmentId = enchantment.Id,
                Amount = amount,
                Rarity = rarity,
                IsNegative = isNegative,
            });

            if (!added)
            {
                Log.Info($"[Blight][EnchantTrace] TryApply reject: source={source}, composite.TryAddEntry returned false for card={card.Id}, enchant={enchantment.Id}");
                return false;
            }

            ApplyCompositeEntryCardMutation(card, enchantment, amount);
            card.FinalizeUpgradeInternal();
            Log.Info($"[Blight][EnchantTrace] TryApply success: source={source}, card={card.Id}, enchant={enchantment.Id}, amount={amount}, rarity={rarity}");
            return true;
        }

        public static bool TryApplyRandomByPool(CardModel card, bool isElite, bool isMutant)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return false;
            }

            IRunState? state = ResolveRunState(card);
            if (state == null)
            {
                return false;
            }

            BlightEnchantmentRarity rarity = BlightEnchantmentPool.RollCardRewardRarity(
                state.Rng.CombatCardGeneration,
                isElite,
                isMutant,
                state.CurrentActIndex);
            foreach (BlightEnchantmentRarity tier in BuildCardRewardFallbackChain(rarity))
            {
                if (TryApplyFromRarity(card, tier, useForgePool: false, source: $"CardReward.Roll(isElite={isElite},isMutant={isMutant},rolled={rarity})"))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryApplySpecific(CardModel card, BlightEnchantmentRarity rarity)
        {
            return TryApplyFromRarity(card, rarity, useForgePool: false, source: "TryApplySpecific");
        }

        public static bool TryApplySpecificFromForge(CardModel card, BlightEnchantmentRarity rarity)
        {
            return TryApplyFromRarity(card, rarity, useForgePool: true, source: "TryApplySpecificFromForge");
        }

        public static bool TryApplyMutantCurse(CardModel card, bool isElite)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return false;
            }

            IRunState? state = ResolveRunState(card);
            if (state == null)
            {
                return false;
            }

            float roll = state.Rng.CombatCardGeneration.NextFloat();

            if (isElite)
            {
                // 变异精英：保底罕见诅咒（灾厄6），有概率升级为稀有诅咒（灾厄10）。
                BlightEnchantmentRarity primaryTier = roll < 0.30f
                    ? BlightEnchantmentRarity.Rare
                    : BlightEnchantmentRarity.Uncommon;

                return TryApplySpecificMutantCurse(card, primaryTier, state.Rng.CombatCardGeneration, "MutantCurse.Elite.Primary")
                    || TryApplySpecificMutantCurse(card, BlightEnchantmentRarity.Uncommon, state.Rng.CombatCardGeneration, "MutantCurse.Elite.FallbackUncommon")
                    || TryApplySpecificMutantCurse(card, BlightEnchantmentRarity.Common, state.Rng.CombatCardGeneration, "MutantCurse.Elite.FallbackCommon");
            }

            // 变异普通：普通诅咒为主（灾厄3），小概率罕见诅咒（灾厄6）。
            BlightEnchantmentRarity normalPrimaryTier = roll < 0.15f
                ? BlightEnchantmentRarity.Uncommon
                : BlightEnchantmentRarity.Common;

            BlightEnchantmentRarity normalFallbackTier = normalPrimaryTier == BlightEnchantmentRarity.Uncommon
                ? BlightEnchantmentRarity.Common
                : BlightEnchantmentRarity.Uncommon;

            return TryApplySpecificMutantCurse(card, normalPrimaryTier, state.Rng.CombatCardGeneration, "MutantCurse.Normal.Primary")
                || TryApplySpecificMutantCurse(card, normalFallbackTier, state.Rng.CombatCardGeneration, "MutantCurse.Normal.Fallback");
        }

        public static IReadOnlyList<BlightEnchantmentEntry> GetEntries(CardModel card)
        {
            if (card?.Enchantment is BlightCompositeEnchantment composite)
            {
                return composite.Entries;
            }

            if (card?.Enchantment == null)
            {
                return Array.Empty<BlightEnchantmentEntry>();
            }

            EnchantmentModel enchantment = card.Enchantment;
            BlightEnchantmentRarity rarity = enchantment is IBlightEnchantment metadata
                ? metadata.Rarity
                : BlightEnchantmentRarity.Common;

            return new[]
            {
                new BlightEnchantmentEntry
                {
                    EnchantmentId = enchantment.Id,
                    Amount = enchantment.Amount,
                    Rarity = rarity,
                    IsNegative = rarity == BlightEnchantmentRarity.Negative,
                },
            };
        }

        private static bool TryApplyFromRarity(CardModel card, BlightEnchantmentRarity rarity, bool useForgePool, string source)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return false;
            }

            IRunState? state = ResolveRunState(card);
            if (state == null)
            {
                return false;
            }

            IReadOnlyList<BlightEnchantmentEntry> existing = GetEntries(card);
            EnchantmentModel? enchantment;
            bool created = useForgePool
                ? BlightEnchantmentPool.TryCreateFromForgeRarity(card, rarity, existing, state.Rng.CombatCardGeneration, out enchantment)
                : BlightEnchantmentPool.TryCreateFromCardRewardRarity(card, rarity, existing, state.Rng.CombatCardGeneration, out enchantment);

            if (!created
                || enchantment == null)
            {
                Log.Info($"[Blight][EnchantTrace] TryApplyFromRarity no-candidate: source={source}, card={card?.Id}, rarity={rarity}, useForgePool={useForgePool}");
                return false;
            }

            IBlightEnchantment? metadata = enchantment as IBlightEnchantment;
            BlightEnchantmentRarity finalRarity = metadata?.Rarity ?? rarity;
            bool isNegative = metadata?.Rarity == BlightEnchantmentRarity.Negative || finalRarity == BlightEnchantmentRarity.Negative;
            int amount = DetermineAmount(enchantment, finalRarity);
            return TryApply(card, enchantment, amount, finalRarity, isNegative, source);
        }

        private static bool CanApplySingle(CardModel card, EnchantmentModel enchantment)
        {
            if (enchantment is IBlightEnchantment metadata)
            {
                return metadata.CanApplyTo(card);
            }

            return enchantment.CanEnchantCardType(card.Type);
        }

        private static bool CanAddEntry(BlightCompositeEnchantment composite, CardModel card, EnchantmentModel enchantment)
        {
            if (enchantment is IBlightEnchantment metadata)
            {
                if (!metadata.CanApplyTo(card))
                {
                    return false;
                }

                int duplicateCount = composite.Entries.Count(e => e.EnchantmentId.Equals(enchantment.Id));
                if (!metadata.AllowDuplicateInstances && duplicateCount > 0)
                {
                    return false;
                }

                if (metadata.MaxDuplicateInstancesPerCard.HasValue && duplicateCount >= metadata.MaxDuplicateInstancesPerCard.Value)
                {
                    return false;
                }

                return true;
            }

            return enchantment.CanEnchantCardType(card.Type);
        }

        private static int DetermineAmount(EnchantmentModel enchantment, BlightEnchantmentRarity rarity)
        {
            int baseAmount = rarity switch
            {
                BlightEnchantmentRarity.Common => 1,
                BlightEnchantmentRarity.Uncommon => 2,
                BlightEnchantmentRarity.Rare => 2,
                BlightEnchantmentRarity.Negative => 1,
                BlightEnchantmentRarity.UltraRare => 2,
                _ => 1,
            };

            if (!enchantment.ShowAmount)
            {
                return 1;
            }

            return baseAmount;
        }

        private static BlightCompositeEnchantment EnsureComposite(CardModel card)
        {
            if (card.Enchantment is BlightCompositeEnchantment composite)
            {
                return composite;
            }

            EnchantmentModel? previous = card.Enchantment;
            if (previous != null)
            {
                card.ClearEnchantmentInternal();
            }

            BlightCompositeEnchantment newComposite = (BlightCompositeEnchantment)ModelDb.Enchantment<BlightCompositeEnchantment>().ToMutable();
            card.EnchantInternal(newComposite, 1m);

            if (previous != null)
            {
                newComposite.TryAddEntry(new BlightEnchantmentEntry
                {
                    EnchantmentId = previous.Id,
                    Amount = previous.Amount,
                    Rarity = previous is IBlightEnchantment metadata ? metadata.Rarity : BlightEnchantmentRarity.Common,
                    IsNegative = previous is IBlightEnchantment negativeMeta && negativeMeta.Rarity == BlightEnchantmentRarity.Negative,
                });
            }

            newComposite.RecalculateValues();
            return newComposite;
        }

        private static IRunState? ResolveRunState(CardModel card)
        {
            return card.RunState
                ?? card.Owner?.RunState
                ?? RunManager.Instance?.DebugOnlyGetState();
        }

        private static bool TryApplySpecificMutantCurse(CardModel card, BlightEnchantmentRarity curseTier, Rng rng, string source)
        {
            IReadOnlyList<BlightEnchantmentEntry> existing = GetEntries(card);
            bool created = BlightEnchantmentPool.TryCreateCurseFromRarity(card, curseTier, existing, rng, out EnchantmentModel? curse);
            if (!created || curse == null)
            {
                Log.Info($"[Blight][EnchantTrace] TryApplyMutantCurse no-candidate: source={source}, card={card?.Id}, tier={curseTier}");
                return false;
            }

            return TryApply(card, curse, amount: 1, rarity: curseTier, isNegative: true, source: source);
        }

        private static void ApplyCompositeEntryCardMutation(CardModel card, EnchantmentModel enchantment, int amount)
        {
            EnchantmentModel runtime = SaveUtil.EnchantmentOrDeprecated(enchantment.Id).ToMutable();
            runtime.ApplyInternal(card, amount);
            runtime.ModifyCard();
        }

        private static IEnumerable<BlightEnchantmentRarity> BuildCardRewardFallbackChain(BlightEnchantmentRarity rolledTier)
        {
            yield return rolledTier;

            BlightEnchantmentRarity[] fallback = rolledTier switch
            {
                BlightEnchantmentRarity.UltraRare => new[] { BlightEnchantmentRarity.Rare, BlightEnchantmentRarity.Uncommon, BlightEnchantmentRarity.Common },
                BlightEnchantmentRarity.Rare => new[] { BlightEnchantmentRarity.Uncommon, BlightEnchantmentRarity.Common, BlightEnchantmentRarity.UltraRare },
                BlightEnchantmentRarity.Uncommon => new[] { BlightEnchantmentRarity.Common, BlightEnchantmentRarity.Rare, BlightEnchantmentRarity.UltraRare },
                _ => new[] { BlightEnchantmentRarity.Uncommon, BlightEnchantmentRarity.Rare, BlightEnchantmentRarity.UltraRare },
            };

            foreach (BlightEnchantmentRarity tier in fallback)
            {
                if (tier != rolledTier)
                {
                    yield return tier;
                }
            }
        }
    }
}
