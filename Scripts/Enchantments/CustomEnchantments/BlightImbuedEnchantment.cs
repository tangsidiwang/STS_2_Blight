using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightImbuedEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShouldStartAtBottomOfDrawPile => true;

        public override bool ShowAmount => false;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Skill;
        }

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (Card?.Owner == player && Card.CombatState?.RoundNumber == 1)
            {
                await CardCmd.AutoPlay(choiceContext, Card, null);
            }
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type))
            {
                return false;
            }

            if (BlightEnchantmentRuntimeHelper.HasEnchantment<BlightImbuedEnchantment>(card))
            {
                return false;
            }

            return card.Pile == null
                || card.Pile.Type != PileType.Deck
                || !card.Keywords.Contains(CardKeyword.Unplayable);
        }

        public bool AllowDuplicateInstances => false;

        public int? MaxDuplicateInstancesPerCard => 1;
    }
}