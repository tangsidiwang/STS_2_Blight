using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightVigorous6Enchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => true;

        public override int DisplayAmount => 6;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack;
        }

        public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
        {
            if (Status != EnchantmentStatus.Normal)
            {
                return 0m;
            }

            if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            {
                return 0m;
            }

            return 6m;
        }

        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card != Card)
            {
                return Task.CompletedTask;
            }

            Status = EnchantmentStatus.Disabled;
            return Task.CompletedTask;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type))
            {
                return false;
            }

            if (BlightEnchantmentRuntimeHelper.HasEnchantment<BlightVigorous6Enchantment>(card))
            {
                return false;
            }

            return card.Pile == null
                || card.Pile.Type != PileType.Deck
                || !card.Keywords.Contains(CardKeyword.Unplayable);
        }

        public bool AllowDuplicateInstances => true;

        public int? MaxDuplicateInstancesPerCard => null;
    }
}