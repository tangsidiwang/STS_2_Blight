using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightMomentum2Enchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => true;

        public override int DisplayAmount => 2;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack;
        }

        public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
        {
            if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            {
                return 0m;
            }

            return GetFinishedPlayCount() * 2m;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type))
            {
                return false;
            }

            if (BlightEnchantmentRuntimeHelper.HasEnchantment<BlightMomentum2Enchantment>(card))
            {
                return false;
            }

            return card.Pile == null
                || card.Pile.Type != PileType.Deck
                || !card.Keywords.Contains(CardKeyword.Unplayable);
        }

        public bool AllowDuplicateInstances => false;

        public int? MaxDuplicateInstancesPerCard => 1;

        private int GetFinishedPlayCount()
        {
            if (Card?.CombatState == null)
            {
                return 0;
            }

            return CombatManager.Instance?.History?.CardPlaysFinished.Count(entry => entry.CardPlay.Card == Card) ?? 0;
        }
    }
}