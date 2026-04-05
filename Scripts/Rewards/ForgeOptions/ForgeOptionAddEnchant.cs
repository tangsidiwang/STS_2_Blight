using System.Linq;
using System.Threading.Tasks;
using BlightMod.Enchantments;
using BlightMod.Rewards;
using BlightMod.Localization;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Rewards.ForgeOptions
{
    public sealed class ForgeOptionAddEnchant : IForgeOption
    {
        private readonly BlightEnchantmentRarity _rarity;
        private readonly ModelId? _enchantmentId;
        private readonly string? _displayName;

        public ForgeOptionAddEnchant(BlightEnchantmentRarity rarity, ModelId? enchantmentId = null, string? displayName = null)
        {
            _rarity = rarity;
            _enchantmentId = enchantmentId;
            _displayName = displayName;
        }

        public string Title => BlightLocalization.GetText("BLIGHT_FORGE.title");

        public string Description => BlightLocalization.Format(
            "BLIGHT_FORGE.description",
            ("Target", GetTargetCardDescription()),
            ("EnchantName", GetDisplayEnchantName()));

        public BlightEnchantmentRarity Rarity => _rarity;

        public IEnumerable<IHoverTip> HoverTips
        {
            get
            {
                if (_enchantmentId == null)
                {
                    return System.Array.Empty<IHoverTip>();
                }

                try
                {
                    return SaveUtil.EnchantmentOrDeprecated(_enchantmentId).HoverTips;
                }
                catch
                {
                    return System.Array.Empty<IHoverTip>();
                }
            }
        }

        public CardRarity DisplayCardRarity => _rarity switch
        {
            BlightEnchantmentRarity.Uncommon => CardRarity.Uncommon,
            BlightEnchantmentRarity.Rare => CardRarity.Rare,
            BlightEnchantmentRarity.UltraRare => CardRarity.Rare,
            BlightEnchantmentRarity.Negative => CardRarity.Curse,
            _ => CardRarity.Common,
        };

        public bool CanExecute(Player player)
        {
            if (player?.Deck == null)
            {
                return false;
            }

            return player.Deck.Cards.Any(c =>
                c.IsRemovable
                && CanApplyToCard(c));
        }

        public async Task<bool> ExecuteAsync(Player player, Rng rng)
        {
            CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
            {
                Cancelable = true,
                RequireManualConfirmation = true,
            };

            var target = await ForgeLocalSelection.SelectDeckGeneric(
                player,
                prefs,
                c => c.IsRemovable
                    && CanApplyToCard(c));
            if (target == null)
            {
                Log.Info($"[Blight][Forge] Enchant option canceled or no valid card: rarity={_rarity}, enchantId={_enchantmentId}");
                return false;
            }

            Log.Info($"[Blight][EnchantTrace] Forge target selected: card={target.Id}, type={target.Type}, basicStrikeOrDefend={target.IsBasicStrikeOrDefend}, hasEnchant={target.Enchantment != null}, rarity={_rarity}, enchantId={_enchantmentId}");

            int beforeTotalEntries = BlightEnchantmentManager.GetEntries(target).Count;
            int beforeSpecificEntries = _enchantmentId != null
                ? BlightEnchantmentManager.GetEntries(target).Count(e => e.EnchantmentId.Equals(_enchantmentId))
                : 0;

            if (_enchantmentId != null)
            {
                var enchantment = SaveUtil.EnchantmentOrDeprecated(_enchantmentId).ToMutable();
                bool isNegative = _rarity == BlightEnchantmentRarity.Negative;
                bool applied = BlightEnchantmentManager.TryApply(target, enchantment, 1, _rarity, isNegative, source: "ForgeOptionAddEnchant.Specific");
                int afterSpecificEntries = BlightEnchantmentManager.GetEntries(target).Count(e => e.EnchantmentId.Equals(enchantment.Id));
                bool effectiveSuccess = applied || afterSpecificEntries > beforeSpecificEntries;
                Log.Info($"[Blight][Forge] Apply specific enchant result={applied}, effectiveSuccess={effectiveSuccess}, card={target.Id}, enchant={enchantment.Id}, rarity={_rarity}");
                return effectiveSuccess;
            }

            bool randomApplied = BlightEnchantmentManager.TryApplySpecificFromForge(target, _rarity);
            int afterTotalEntries = BlightEnchantmentManager.GetEntries(target).Count;
            bool randomEffectiveSuccess = randomApplied || afterTotalEntries > beforeTotalEntries;
            Log.Info($"[Blight][Forge] Apply random-by-rarity result={randomApplied}, effectiveSuccess={randomEffectiveSuccess}, card={target.Id}, rarity={_rarity}");
            return randomEffectiveSuccess;
        }

        private string GetDisplayEnchantName()
        {
            if (!string.IsNullOrWhiteSpace(_displayName))
            {
                return _displayName;
            }

            return GetRarityName(_rarity);
        }

        private string GetTargetCardDescription()
        {
            if (_enchantmentId == null)
            {
                return BlightLocalization.GetText("BLIGHT_FORGE.target.playable");
            }

            string entry = _enchantmentId.Entry?.ToUpperInvariant() ?? string.Empty;

            if (entry.Contains("SWORD_DANCE"))
            {
                return BlightLocalization.GetText("BLIGHT_FORGE.target.playable");
            }

            if (entry.Contains("SHARP") || entry.Contains("DAMAGE") || entry.Contains("FRAGILE_EDGE"))
            {
                return BlightLocalization.GetText("BLIGHT_FORGE.target.attack");
            }

            if (entry.Contains("BLOCK"))
            {
                return BlightLocalization.GetText("BLIGHT_FORGE.target.skill");
            }

            if (entry.Contains("DOUBLE_PLAY"))
            {
                return BlightLocalization.GetText("BLIGHT_FORGE.target.attack_or_skill");
            }

            return BlightLocalization.GetText("BLIGHT_FORGE.target.playable");
        }

        private bool CanApplyToCard(CardModel card)
        {
            var existingEntries = BlightEnchantmentManager.GetEntries(card);
            if (_enchantmentId != null)
            {
                return BlightEnchantmentPool.CanApplySpecificForForge(card, _enchantmentId, existingEntries);
            }

            return BlightEnchantmentPool.CanApplyAnyFromForgeRarity(card, _rarity, existingEntries);
        }

        private static string GetRarityName(BlightEnchantmentRarity rarity)
        {
            return rarity switch
            {
                BlightEnchantmentRarity.Common => BlightLocalization.GetText("BLIGHT_FORGE.rarity.common"),
                BlightEnchantmentRarity.Uncommon => BlightLocalization.GetText("BLIGHT_FORGE.rarity.uncommon"),
                BlightEnchantmentRarity.Rare => BlightLocalization.GetText("BLIGHT_FORGE.rarity.rare"),
                BlightEnchantmentRarity.UltraRare => BlightLocalization.GetText("BLIGHT_FORGE.rarity.ultra_rare"),
                BlightEnchantmentRarity.Negative => BlightLocalization.GetText("BLIGHT_FORGE.rarity.negative"),
                _ => BlightLocalization.GetText("BLIGHT_FORGE.rarity.common"),
            };
        }
    }
}
