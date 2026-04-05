using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightSownEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => false;

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

            await PlayerCmd.GainEnergy(1m, Card.Owner);
            Status = EnchantmentStatus.Disabled;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type))
            {
                return false;
            }

            if (BlightEnchantmentRuntimeHelper.HasEnchantment<BlightSownEnchantment>(card))
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