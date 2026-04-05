using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightRoyallyApprovedEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => false;

        // 需要固定显示数字时覆盖 DisplayAmount（例如显示 3）
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            HoverTipFactory.FromKeyword(CardKeyword.Retain)
        };

        public override bool CanEnchantCardType(CardType cardType)
        {
            if ((uint)(cardType - 1) <= 1u)
            {
                return true;
            }
            return false;
        }

        protected override void OnEnchant()
        {
            base.Card.AddKeyword(CardKeyword.Innate);
            base.Card.AddKeyword(CardKeyword.Retain);
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            return card != null && CanEnchantCardType(card.Type);
        }

        public bool AllowDuplicateInstances => false;

        public int? MaxDuplicateInstancesPerCard => 1;
    }
}