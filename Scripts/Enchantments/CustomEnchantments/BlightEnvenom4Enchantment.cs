using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightEnvenom4Enchantment : EnchantmentModel, IBlightEnchantment
    {
        private const decimal PoisonAmount = 4m;

        public override bool ShowAmount => true;

        public override int DisplayAmount => 4;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack;
        }

        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            if (cardPlay?.Target == null || Card?.Owner?.Creature == null)
            {
                return;
            }

            await PowerCmd.Apply<PoisonPower>(cardPlay.Target, PoisonAmount, Card.Owner.Creature, null);
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;

        public bool CanApplyTo(CardModel card)
        {
            return card != null
                && CanEnchantCardType(card.Type)
                && card.TargetType == TargetType.AnyEnemy
                && !card.Keywords.Contains(CardKeyword.Unplayable);
        }

        public bool AllowDuplicateInstances => true;

        public int? MaxDuplicateInstancesPerCard => null;
    }
}