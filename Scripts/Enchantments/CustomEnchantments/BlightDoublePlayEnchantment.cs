using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightDoublePlayEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => false;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack || cardType == CardType.Skill;
        }

        public override int EnchantPlayCount(int originalPlayCount)
        {
            return originalPlayCount + 1;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.UltraRare;

        public bool CanApplyTo(CardModel card)
        {
            return card != null && CanEnchantCardType(card.Type);
        }

        public bool AllowDuplicateInstances => true;

        public int? MaxDuplicateInstancesPerCard => null;
    }
}
