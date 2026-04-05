using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightSlitherEnchantment : EnchantmentModel, IBlightEnchantment
    {
        private int _testEnergyCostOverride = -1;

        public int TestEnergyCostOverride
        {
            get
            {
                return _testEnergyCostOverride;
            }
            set
            {
                if (TestMode.IsOff)
                {
                    throw new InvalidOperationException("Only set this value in test mode.");
                }
                AssertMutable();
                _testEnergyCostOverride = value;
            }
        }

        public override bool CanEnchant(CardModel card)
        {
            if (base.CanEnchant(card) && !card.Keywords.Contains(CardKeyword.Unplayable))
            {
                return !card.EnergyCost.CostsX;
            }
            return false;
        }

        public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card != base.Card)
            {
                return Task.CompletedTask;
            }
            if (base.Card.Pile.Type != PileType.Hand)
            {
                return Task.CompletedTask;
            }
            base.Card.EnergyCost.SetThisCombat(NextEnergyCost());
            NCard.FindOnTable(card)?.PlayRandomizeCostAnim();
            return Task.CompletedTask;
        }

        private int NextEnergyCost()
        {
            if (TestEnergyCostOverride >= 0)
            {
                return TestEnergyCostOverride;
            }
            return base.Card.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            if (card == null)
                return false;

            // 检查卡牌类型是否允许附魔（基类只检查类型，无副作用）
            if (!CanEnchantCardType(card.Type))
                return false;

            // 禁止附魔到不可玩卡牌
            if (card.Keywords.Contains(CardKeyword.Unplayable))
                return false;

            // 禁止附魔到费用为 X 的卡牌
            if (card.EnergyCost.CostsX)
                return false;

            return true;
        }

        public bool AllowDuplicateInstances => false;

        public int? MaxDuplicateInstancesPerCard => 1;
    }
}