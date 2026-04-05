using System.Collections.Generic;
using System.Linq;
using BlightMod.Enchantments;
using BlightMod.Localization;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Rewards.ForgeOptions
{
    public static class ForgeOptionPool
    {
        public static List<IForgeOption> RollOptions(BlightEnchantmentRarity rarity, Player player, Rng rng, int count)
        {
            if (count < 1)
            {
                return new List<IForgeOption>();
            }

            List<IForgeOption> result = new List<IForgeOption>(count);

            // Slot 1-2: enchantments from this rarity tier.
            List<IForgeOption> executableEnchantOptions = GetExecutableEnchantOptions(rarity, player);
            if (executableEnchantOptions.Count == 0)
            {
                result.Add(RollUtilityOption(rarity, player, rng));
            }
            else
            {
                IForgeOption first = executableEnchantOptions[rng.NextInt(executableEnchantOptions.Count)];
                result.Add(first);
            }

            if (count >= 2)
            {
                if (executableEnchantOptions.Count <= 1)
                {
                    result.Add(result[0]);
                }
                else
                {
                    List<IForgeOption> alternates = executableEnchantOptions
                        .Where(o => o.Description != result[0].Description)
                        .ToList();
                    if (alternates.Count == 0)
                    {
                        alternates = executableEnchantOptions;
                    }

                    result.Add(alternates[rng.NextInt(alternates.Count)]);
                }
            }

            // Slot 3+: utility/effect options from this rarity tier.
            for (int i = result.Count; i < count; i++)
            {
                result.Add(RollUtilityOption(rarity, player, rng));
            }

            return result;
        }

        private static List<IForgeOption> GetExecutableEnchantOptions(BlightEnchantmentRarity rarity, Player player)
        {
            foreach (BlightEnchantmentRarity tier in BuildFallbackChain(rarity))
            {
                List<IForgeOption> executable = BuildEnchantmentCandidatesForTier(tier)
                    .Where(o => o.CanExecute(player))
                    .ToList();
                if (executable.Count > 0)
                {
                    return executable;
                }
            }

            return new List<IForgeOption>();
        }

        public static IForgeOption RollOption(BlightEnchantmentRarity rarity, Player player, Rng rng)
        {
            foreach (BlightEnchantmentRarity tier in BuildFallbackChain(rarity))
            {
                List<IForgeOption> candidates = BuildCandidatesForTier(tier);
                List<IForgeOption> executable = candidates.Where(o => o.CanExecute(player)).ToList();
                if (executable.Count > 0)
                {
                    IForgeOption picked = executable[rng.NextInt(executable.Count)];
                    Log.Info($"[Blight][Forge] Rolled option tier={tier}, title={picked.Title}, description={picked.Description}");
                    return picked;
                }

                Log.Info($"[Blight][Forge] No executable options at tier={tier}, fallback.");
            }

            IForgeOption fallback = new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Relic, BlightEnchantmentRarity.Common);
            Log.Warn("[Blight][Forge] Forge option pool fallback to common relic utility.");
            return fallback;
        }

        private static IForgeOption RollEnchantOption(BlightEnchantmentRarity rarity, Player player, Rng rng)
        {
            foreach (BlightEnchantmentRarity tier in BuildFallbackChain(rarity))
            {
                List<IForgeOption> enchantCandidates = BuildEnchantmentCandidatesForTier(tier)
                    .Where(o => o.CanExecute(player))
                    .ToList();
                if (enchantCandidates.Count > 0)
                {
                    IForgeOption picked = enchantCandidates[rng.NextInt(enchantCandidates.Count)];
                    Log.Info($"[Blight][Forge] Rolled enchant option tier={tier}, title={picked.Title}, description={picked.Description}");
                    return picked;
                }
            }

            Log.Warn("[Blight][Forge] No enchant option available, fallback to utility option.");
            return RollUtilityOption(rarity, player, rng);
        }

        private static IForgeOption RollUtilityOption(BlightEnchantmentRarity rarity, Player player, Rng rng)
        {
            foreach (BlightEnchantmentRarity tier in BuildFallbackChain(rarity))
            {
                List<IForgeOption> utilityCandidates = BuildUtilityCandidatesForTier(tier)
                    .Where(o => o.CanExecute(player))
                    .ToList();
                if (utilityCandidates.Count > 0)
                {
                    IForgeOption picked = utilityCandidates[rng.NextInt(utilityCandidates.Count)];
                    Log.Info($"[Blight][Forge] Rolled utility option tier={tier}, title={picked.Title}, description={picked.Description}");
                    return picked;
                }
            }

            IForgeOption fallback = new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Relic, BlightEnchantmentRarity.Common);
            Log.Warn("[Blight][Forge] Utility option fallback to common relic utility.");
            return fallback;
        }

        private static IEnumerable<BlightEnchantmentRarity> BuildFallbackChain(BlightEnchantmentRarity rarity)
        {
            yield return rarity;
            if (rarity == BlightEnchantmentRarity.UltraRare)
            {
                yield return BlightEnchantmentRarity.Rare;
                yield return BlightEnchantmentRarity.Uncommon;
                yield return BlightEnchantmentRarity.Common;
            }
            else if (rarity == BlightEnchantmentRarity.Rare)
            {
                yield return BlightEnchantmentRarity.Uncommon;
                yield return BlightEnchantmentRarity.Common;
            }
            else if (rarity == BlightEnchantmentRarity.Uncommon)
            {
                yield return BlightEnchantmentRarity.Common;
            }
            else if (rarity == BlightEnchantmentRarity.Negative)
            {
                yield return BlightEnchantmentRarity.Common;
            }
        }

        private static List<IForgeOption> BuildCandidatesForTier(BlightEnchantmentRarity rarity)
        {
            List<IForgeOption> result = new List<IForgeOption>();
            result.AddRange(BuildEnchantmentCandidatesForTier(rarity));
            result.AddRange(BuildUtilityCandidatesForTier(rarity));
            return result;
        }

        private static List<IForgeOption> BuildEnchantmentCandidatesForTier(BlightEnchantmentRarity rarity)
        {
            List<IForgeOption> result = new List<IForgeOption>();
            IReadOnlyList<ModelId> enchantIds = BlightEnchantmentPool.GetForgeIdsByRarity(rarity);
            foreach (ModelId id in enchantIds)
            {
                string enchantName = ResolveEnchantDisplayName(id);
                result.Add(new ForgeOptionAddEnchant(rarity, id, enchantName));
            }

            return result;
        }

        private static List<IForgeOption> BuildUtilityCandidatesForTier(BlightEnchantmentRarity rarity)
        {
            List<IForgeOption> result = new List<IForgeOption>();
            switch (rarity)
            {
                case BlightEnchantmentRarity.Common:
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Armor, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Smith, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.MaxHpPercent, rarity));
                    break;
                case BlightEnchantmentRarity.Uncommon:
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Armor, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Remove, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.MaxHpPercent, rarity));
                    break;
                case BlightEnchantmentRarity.Rare:
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Armor, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.MaxHpPercent, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Relic, rarity));
                    break;
                case BlightEnchantmentRarity.UltraRare:
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Armor, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.MaxHpPercent, rarity));
                    result.Add(new ForgeOptionUtility(ForgeOptionUtility.UtilityKind.Relic, rarity));
                    break;
            }

            return result;
        }

        private static string ResolveEnchantDisplayName(ModelId enchantId)
        {
            try
            {
                return SaveUtil.EnchantmentOrDeprecated(enchantId).Title.GetFormattedText();
            }
            catch (LocException)
            {
                // Some modded enchantments do not provide enchantments.<id>.title localization.
                // Fall back to a readable name so forge selection can continue.
                string entry = enchantId.Entry ?? string.Empty;
                string lowered = entry.ToLowerInvariant();

                if (lowered.Contains("sharp2"))
                {
                    return BlightLocalization.GetText("BLIGHT_FORGE.fallback.sharp2");
                }

                if (lowered.Contains("sharp4"))
                {
                    return BlightLocalization.GetText("BLIGHT_FORGE.fallback.sharp4");
                }

                if (lowered.Contains("damage"))
                {
                    return BlightLocalization.GetText("BLIGHT_FORGE.fallback.damage");
                }

                if (lowered.Contains("block"))
                {
                    return BlightLocalization.GetText("BLIGHT_FORGE.fallback.block");
                }

                if (lowered.Contains("double"))
                {
                    return BlightLocalization.GetText("BLIGHT_FORGE.fallback.double");
                }

                if (lowered.Contains("fragile") || lowered.Contains("edge"))
                {
                    return BlightLocalization.GetText("BLIGHT_FORGE.fallback.fragile_edge");
                }

                Log.Warn($"[Blight][Forge] Missing enchant title localization for {enchantId}, using entry fallback.");
                return entry.Replace('_', ' ');
            }
        }
    }
}
