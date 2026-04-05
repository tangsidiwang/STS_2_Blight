using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    // Forge demo enchantment: fixed +4 damage on powered attacks.
    public sealed class BlightSharp4Enchantment : EnchantmentModel, IBlightEnchantment
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

            return 2m;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            return card != null && CanEnchantCardType(card.Type);
        }

        public bool AllowDuplicateInstances => true;

        public int? MaxDuplicateInstancesPerCard => null;
    }
}
