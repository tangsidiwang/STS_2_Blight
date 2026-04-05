using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightInstinctEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack;
        }

        public override bool CanEnchant(CardModel card)
        {
            if (base.CanEnchant(card) && !card.Keywords.Contains(CardKeyword.Unplayable) && !card.EnergyCost.CostsX)
            {
                return card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0;
            }

            return false;
        }

        protected override void OnEnchant()
        {
            Card.EnergyCost.UpgradeBy(-1);
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type))
            {
                return false;
            }

            if (card.Keywords.Contains(CardKeyword.Unplayable) || card.EnergyCost.CostsX)
            {
                return false;
            }

            return card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0;
        }

        public bool AllowDuplicateInstances => true;

        public int? MaxDuplicateInstancesPerCard => null;
    }
}