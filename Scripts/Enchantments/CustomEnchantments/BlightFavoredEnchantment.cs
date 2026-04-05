using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightFavoredEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack;
        }

        public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
        {
            var method = typeof(ValueProp).Assembly
                .GetType("MegaCrit.Sts2.Core.ValueProps.ValuePropExtensions")?
                .GetMethod("IsPoweredAttack", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            bool isPowered = false;
            if (method != null)
            {
                isPowered = (bool)method.Invoke(null, new object[] { props });
            }
            if (!isPowered)
            {
                return 1m;
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