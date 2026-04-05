using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightAdroit1Enchantment : EnchantmentModel, IBlightEnchantment
    {
        private const decimal BlockAmount = 1m;
        public override int DisplayAmount => 1;
        public override bool ShowAmount => true;
        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType != CardType.None
                && cardType != CardType.Status
                && cardType != CardType.Curse;
        }
        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            if (Card?.Owner?.Creature == null)
            {
                return;
            }
            await CreatureCmd.GainBlock(Card.Owner.Creature, BlockAmount, ValueProp.Move, cardPlay);
        }

        public override void RecalculateValues()
        {
            // 不再依赖DynamicVars
        }
        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Common;

        public bool CanApplyTo(CardModel card)
        {
            return card != null
                && CanEnchantCardType(card.Type)
                && !card.Keywords.Contains(CardKeyword.Unplayable);
        }

            public bool AllowDuplicateInstances => true;

            public int? MaxDuplicateInstancesPerCard => null;
    }
}