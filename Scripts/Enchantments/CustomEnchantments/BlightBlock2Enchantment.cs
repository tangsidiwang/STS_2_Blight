using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightBlock2Enchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => true;
        public override int DisplayAmount => 1;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Skill||cardType==CardType.Attack;
        }
        public override bool CanEnchant(CardModel card)
        {
            if (base.CanEnchant(card))
            {
                return card.GainsBlock;
            }
            return false;
        }
        public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props)
        {
            var method = typeof(ValueProp).Assembly
                .GetType("MegaCrit.Sts2.Core.ValueProps.ValuePropExtensions")?
                .GetMethod("IsPoweredCardOrMonsterMoveBlock", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            bool isPowered = false;
            if (method != null)
            {
                isPowered = (bool)method.Invoke(null, new object[] { props });
            }
            if (!isPowered)
            {
                return 0m;
            }
            return 1m;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Common;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null || !CanEnchantCardType(card.Type) || !card.GainsBlock)
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
