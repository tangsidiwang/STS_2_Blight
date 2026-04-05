using System;
using System.Collections.Generic;
using System.Linq;
using BlightMod.Enchantments.CustomEnchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Enchantments
{
    public static class BlightEnchantmentPool
    {
        private static readonly Dictionary<BlightEnchantmentRarity, List<ModelId>> CardRewardPoolByRarity = CreateEmptyPool();

        private static readonly Dictionary<BlightEnchantmentRarity, List<ModelId>> ForgePoolByRarity = CreateEmptyPool();

        private static readonly Dictionary<BlightEnchantmentRarity, List<ModelId>> CursePoolByRarity = CreateEmptyPool();

        static BlightEnchantmentPool()
        {
            // 显式配置正向/共享附魔池层级，避免依赖附魔类内部 Rarity 决定入池。

            RegisterShared<BlightSwordDance1Enchantment>(BlightEnchantmentRarity.Common);
            RegisterShared<BlightSwordDance2Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightSwordDance3Enchantment>(BlightEnchantmentRarity.Rare);

            RegisterShared<BlightBlock2Enchantment>(BlightEnchantmentRarity.Common);
            RegisterShared<BlightBlock4Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightBlock6Enchantment>(BlightEnchantmentRarity.Rare);

            RegisterShared<BlightSharp2Enchantment>(BlightEnchantmentRarity.Common);
            RegisterShared<BlightSharp4Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightSharp6Enchantment>(BlightEnchantmentRarity.Rare);

            RegisterShared<BlightEnvenom2Enchantment>(BlightEnchantmentRarity.Common);
            RegisterShared<BlightEnvenom3Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightEnvenom4Enchantment>(BlightEnchantmentRarity.Rare);

            RegisterShared<BlightAdroit1Enchantment>(BlightEnchantmentRarity.Common);
            RegisterShared<BlightAdroit2Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightAdroit4Enchantment>(BlightEnchantmentRarity.Rare);

            RegisterShared<BlightInstinctEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterShared<BlightMomentum1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightMomentum2Enchantment>(BlightEnchantmentRarity.Rare);
            RegisterShared<BlightGlamEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterShared<BlightDoublePlayEnchantment>(BlightEnchantmentRarity.UltraRare);
            RegisterShared<BlightPerfectEnchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightSteadyEnchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightSwift1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightSwift2Enchantment>(BlightEnchantmentRarity.Rare);
            RegisterShared<BlightTezcatarasEmberEnchantment>(BlightEnchantmentRarity.UltraRare);
            RegisterShared<BlightVigorous3Enchantment>(BlightEnchantmentRarity.Common);
            RegisterShared<BlightVigorous6Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterShared<BlightVigorous9Enchantment>(BlightEnchantmentRarity.Rare);

            //只进入卡牌附魔池
            RegisterCardReward<BlightImbuedEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterCardReward<BlightCorruptedEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterCardReward<BlightDuplicationEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterCardReward<BlightTemporaryStrength2Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCardReward<BlightTemporaryStrength4Enchantment>(BlightEnchantmentRarity.Rare);
            RegisterCardReward<BlightWound1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCardReward<BlightBrokenBlade1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCardReward<BlightRavage1Enchantment>(BlightEnchantmentRarity.Uncommon);
            //只进锻造附魔池
            RegisterForge<BlightGoopyEnchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterForge<BlightRoyallyApprovedEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterForge<BlightSownEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterForge<BlightSlitherEnchantment>(BlightEnchantmentRarity.Rare);
            RegisterForge<BlightSlumberingEssenceEnchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterForge<BlightFavoredEnchantment>(BlightEnchantmentRarity.UltraRare);
            RegisterForge<BlightSoulsPowerEnchantment>(BlightEnchantmentRarity.Rare);
            // 独立诅咒池：按“普通/罕见/稀有诅咒层级”注册，避免逻辑写死到具体附魔ID。
            RegisterCurse<BlightDoom1Enchantment>(BlightEnchantmentRarity.Common);
            RegisterCurse<BlightDoom6Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCurse<BlightDoom10Enchantment>(BlightEnchantmentRarity.Rare);
            RegisterCurse<BlightPain1Enchantment>(BlightEnchantmentRarity.Common);
            RegisterCurse<BlightPain2Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCurse<BlightPain3Enchantment>(BlightEnchantmentRarity.Rare);
            RegisterCurse<BlightDazed1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCurse<BlightDazed2Enchantment>(BlightEnchantmentRarity.Rare);
            RegisterCurse<BlightWeakSelf1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCurse<BlightVulnerableSelf1Enchantment>(BlightEnchantmentRarity.Uncommon);
            RegisterCurse<BlightFrailSelf1Enchantment>(BlightEnchantmentRarity.Uncommon);
        }

        public static BlightEnchantmentRarity RollCardRewardRarity(Rng rng, bool isElite, bool isMutant, int actIndex)
        {
            float roll = rng.NextFloat();
            float rareBoost = GetRareBoostByAct(actIndex);
            float ultraBoost = GetUltraRareBoostByAct(actIndex);

            // 变异精英：小概率普通，主要在罕见/稀有之间分布。
            if (isElite && isMutant)
            {
                float commonThreshold = Math.Max(0.02f, 0.20f - rareBoost - ultraBoost);
                float ultraThreshold = Math.Max(commonThreshold, 0.70f + ultraBoost);
                float uncommonThreshold = Math.Max(ultraThreshold, 0.90f - rareBoost);

                if (roll < commonThreshold)
                {
                    return BlightEnchantmentRarity.Common;
                }

                if (roll < ultraThreshold)
                {
                    return BlightEnchantmentRarity.Uncommon;
                }

                return roll < uncommonThreshold ? BlightEnchantmentRarity.Rare : BlightEnchantmentRarity.UltraRare;
            }

            // 精英：主要普通，小概率罕见与稀有。
            if (isElite)
            {
                float commonThreshold = Math.Max(0.45f, 0.65f - rareBoost - ultraBoost);
                float ultraThreshold = Math.Max(commonThreshold, 0.90f + ultraBoost);
                float uncommonThreshold = Math.Max(ultraThreshold, 0.97f - rareBoost);

                if (roll < commonThreshold)
                {
                    return BlightEnchantmentRarity.Common;
                }

                if (roll < ultraThreshold)
                {
                    return BlightEnchantmentRarity.Uncommon;
                }

                return roll < uncommonThreshold ? BlightEnchantmentRarity.Rare : BlightEnchantmentRarity.UltraRare;
            }

            // 变异普通：小概率普通，主要罕见，小概率稀有。
            if (isMutant)
            {
                float commonThreshold = Math.Max(0.05f, 0.50f - rareBoost - ultraBoost);
                float ultraThreshold = Math.Max(commonThreshold, 0.80f + ultraBoost);
                float uncommonThreshold = Math.Max(ultraThreshold, 0.99f - rareBoost);

                if (roll < commonThreshold)
                {
                    return BlightEnchantmentRarity.Common;
                }

                if (roll < ultraThreshold)
                {
                    return BlightEnchantmentRarity.Uncommon;
                }

                return roll < uncommonThreshold ? BlightEnchantmentRarity.Rare : BlightEnchantmentRarity.UltraRare;
            }

            float normalCommonThreshold = Math.Max(0.55f, 0.7f - rareBoost - ultraBoost);
            float normalUncommonThreshold = Math.Max(normalCommonThreshold, 0.92f - rareBoost);

            if (roll < normalCommonThreshold)
            {
                return BlightEnchantmentRarity.Common;
            }

            if (roll < normalUncommonThreshold)
            {
                return BlightEnchantmentRarity.Uncommon;
            }

            return BlightEnchantmentRarity.Rare;
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
                >= 3 => 0.1f,
                2 => 0.05f,
                _ => 0f,
            };
        }

        public static bool TryCreateFromCardRewardRarity(
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries,
            Rng rng,
            out EnchantmentModel? enchantment)
        {
            return TryCreateFromPool(CardRewardPoolByRarity, card, rarity, existingEntries, rng, out enchantment);
        }

        public static bool TryCreateFromForgeRarity(
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries,
            Rng rng,
            out EnchantmentModel? enchantment)
        {
            return TryCreateFromPool(ForgePoolByRarity, card, rarity, existingEntries, rng, out enchantment);
        }

        public static bool CanApplyAnyFromCardRewardRarity(
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries)
        {
            return CanApplyAnyFromPool(CardRewardPoolByRarity, card, rarity, existingEntries);
        }

        public static bool CanApplyAnyFromForgeRarity(
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries)
        {
            return CanApplyAnyFromPool(ForgePoolByRarity, card, rarity, existingEntries);
        }

        public static bool CanApplySpecificForCardReward(
            CardModel card,
            ModelId enchantmentId,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries)
        {
            return IsCandidateValid(card, enchantmentId, existingEntries);
        }

        public static bool CanApplySpecificForForge(
            CardModel card,
            ModelId enchantmentId,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries)
        {
            return IsCandidateValid(card, enchantmentId, existingEntries);
        }

        public static bool TryCreateCurseFromRarity(
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries,
            Rng rng,
            out EnchantmentModel? curse)
        {
            return TryCreateFromPool(CursePoolByRarity, card, rarity, existingEntries, rng, out curse);
        }

        public static IReadOnlyList<ModelId> GetCardRewardIdsByRarity(BlightEnchantmentRarity rarity)
        {
            if (!CardRewardPoolByRarity.TryGetValue(rarity, out List<ModelId>? ids))
            {
                return Array.Empty<ModelId>();
            }

            return ids;
        }

        public static IReadOnlyList<ModelId> GetForgeIdsByRarity(BlightEnchantmentRarity rarity)
        {
            if (!ForgePoolByRarity.TryGetValue(rarity, out List<ModelId>? ids))
            {
                return Array.Empty<ModelId>();
            }

            return ids;
        }

        private static bool IsCandidateValid(CardModel card, ModelId id, IReadOnlyList<BlightEnchantmentEntry> existingEntries)
        {
            if (card == null)
            {
                return false;
            }

            EnchantmentModel canonical = SaveUtil.EnchantmentOrDeprecated(id);
            if (canonical is BlightCompositeEnchantment)
            {
                return false;
            }

            if (canonical is IBlightEnchantment blightMeta)
            {
                if (!blightMeta.CanApplyTo(card))
                {
                    return false;
                }

                int duplicateCount = existingEntries.Count(e => e.EnchantmentId.Equals(id));
                if (!blightMeta.AllowDuplicateInstances && duplicateCount > 0)
                {
                    return false;
                }

                if (blightMeta.MaxDuplicateInstancesPerCard.HasValue && duplicateCount >= blightMeta.MaxDuplicateInstancesPerCard.Value)
                {
                    return false;
                }

                return true;
            }

            return canonical.CanEnchantCardType(card.Type);
        }

        private static Dictionary<BlightEnchantmentRarity, List<ModelId>> CreateEmptyPool()
        {
            return new Dictionary<BlightEnchantmentRarity, List<ModelId>>
            {
                [BlightEnchantmentRarity.Common] = new List<ModelId>(),
                [BlightEnchantmentRarity.Uncommon] = new List<ModelId>(),
                [BlightEnchantmentRarity.Rare] = new List<ModelId>(),
                [BlightEnchantmentRarity.Negative] = new List<ModelId>(),
                [BlightEnchantmentRarity.UltraRare] = new List<ModelId>(),
            };
        }

        private static void RegisterShared<T>(BlightEnchantmentRarity tier) where T : EnchantmentModel, IBlightEnchantment
        {
            RegisterCardReward<T>(tier);
            RegisterForge<T>(tier);
        }

        private static void RegisterCardReward<T>(BlightEnchantmentRarity tier) where T : EnchantmentModel, IBlightEnchantment
        {
            T canonical = ModelDb.Enchantment<T>();
            CardRewardPoolByRarity[tier].Add(canonical.Id);
        }

        private static void RegisterForge<T>(BlightEnchantmentRarity tier) where T : EnchantmentModel, IBlightEnchantment
        {
            T canonical = ModelDb.Enchantment<T>();
            ForgePoolByRarity[tier].Add(canonical.Id);
        }

        private static void RegisterCurse<T>(BlightEnchantmentRarity tier) where T : EnchantmentModel, IBlightEnchantment
        {
            T canonical = ModelDb.Enchantment<T>();
            CursePoolByRarity[tier].Add(canonical.Id);
        }

        private static bool TryCreateFromPool(
            Dictionary<BlightEnchantmentRarity, List<ModelId>> pool,
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries,
            Rng rng,
            out EnchantmentModel? enchantment)
        {
            enchantment = null;
            if (!pool.TryGetValue(rarity, out List<ModelId>? ids) || ids.Count == 0)
            {
                return false;
            }

            List<ModelId> candidates = ids
                .Where(id => IsCandidateValid(card, id, existingEntries))
                .ToList();

            if (candidates.Count == 0)
            {
                return false;
            }

            ModelId picked = candidates[rng.NextInt(candidates.Count)];
            enchantment = SaveUtil.EnchantmentOrDeprecated(picked).ToMutable();
            return true;
        }

        private static bool CanApplyAnyFromPool(
            Dictionary<BlightEnchantmentRarity, List<ModelId>> pool,
            CardModel card,
            BlightEnchantmentRarity rarity,
            IReadOnlyList<BlightEnchantmentEntry> existingEntries)
        {
            if (!pool.TryGetValue(rarity, out List<ModelId>? ids) || ids.Count == 0)
            {
                return false;
            }

            return ids.Any(id => IsCandidateValid(card, id, existingEntries));
        }
    }
}
