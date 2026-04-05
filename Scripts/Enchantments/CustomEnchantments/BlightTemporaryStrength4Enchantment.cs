using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightTemporaryStrength4Enchantment : EnchantmentModel, IBlightEnchantment
    {
        private const decimal StrengthAmount = 4m;

        public override bool ShowAmount => true;

        public override int DisplayAmount => 4;

        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            if (Card?.Owner?.Creature == null)
            {
                return;
            }

            await PowerCmd.Apply<SetupStrikePower>(Card.Owner.Creature, StrengthAmount, Card.Owner.Creature, null);
        }

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType != CardType.None
                && cardType != CardType.Status
                && cardType != CardType.Curse;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;

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