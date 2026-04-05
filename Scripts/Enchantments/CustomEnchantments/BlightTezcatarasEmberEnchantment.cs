using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightTezcatarasEmberEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => false;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Eternal)
        };

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack || cardType == CardType.Skill || cardType == CardType.Power;
        }

        protected override void OnEnchant()
        {
            Card.EnergyCost.UpgradeBy(-Card.EnergyCost.GetWithModifiers(CostModifiers.None));
            Card.AddKeyword(CardKeyword.Eternal);
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.UltraRare;

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

            if (BlightEnchantmentRuntimeHelper.HasEnchantment<BlightTezcatarasEmberEnchantment>(card))
            {
                return false;
            }

            return card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0;
        }

        public bool AllowDuplicateInstances => false;

        public int? MaxDuplicateInstancesPerCard => 1;
    }
}