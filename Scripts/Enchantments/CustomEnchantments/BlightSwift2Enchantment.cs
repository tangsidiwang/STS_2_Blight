using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightSwift2Enchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => true;

        public override int DisplayAmount => 2;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack || cardType == CardType.Skill || cardType == CardType.Power;
        }

        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            if (cardPlay?.Card != Card || Card?.Owner == null)
            {
                return;
            }

            if (Status != EnchantmentStatus.Normal)
            {
                return;
            }

            await CardPileCmd.Draw(choiceContext, 2m, Card.Owner);
            Status = EnchantmentStatus.Disabled;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type))
            {
                return false;
            }

            if (BlightEnchantmentRuntimeHelper.HasEnchantment<BlightSwift2Enchantment>(card))
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