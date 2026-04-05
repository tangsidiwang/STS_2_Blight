using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;


using MegaCrit.Sts2.Core.HoverTips;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightSoulsPowerEnchantment : EnchantmentModel, IBlightEnchantment
    {
        // 修正反编译错误：使用 yield return 提供额外提示

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
           HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
        };

        public override bool CanEnchant(CardModel card)
        {
            return CanApplyTo(card);
        }

        protected override void OnEnchant()
        {
            Card.RemoveKeyword(CardKeyword.Exhaust);

        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null) return false;

            // 类型限制（根据设计自行调整）
            if (card.Type != CardType.Attack && card.Type != CardType.Skill)
                return false;

            // 必须原本具有 Exhaust 关键字
            if (!card.Keywords.Contains(CardKeyword.Exhaust))
                return false;

            // 禁止附着在不可玩或 X 费卡牌上（与基类 CanEnchant 保持一致）
            if (card.Keywords.Contains(CardKeyword.Unplayable))
                return false;
            if (card.EnergyCost.CostsX)
                return false;

            return true;
        }

        // 禁止多重附魔，因为重复添加没有意义
        public bool AllowDuplicateInstances => false;
        public int? MaxDuplicateInstancesPerCard => 1;
    }
}