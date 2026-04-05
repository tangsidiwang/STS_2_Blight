using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightGoopyEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => true;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Skill || cardType == CardType.Attack;
        }

        public override bool CanEnchant(CardModel card)
        {
            return base.CanEnchant(card) && card.GainsBlock;
        }

        protected override void OnEnchant()
        {
            Card.AddKeyword(CardKeyword.Exhaust);
        }

        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card != base.Card)
            {
                return Task.CompletedTask;
            }

            base.Amount++;
            BlightEnchantmentRuntimeHelper.SyncDeckVersionEnchantment(base.Card, Id, 1);
            return Task.CompletedTask;
        }

        public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props)
        {
            var method = typeof(ValueProp).Assembly
                .GetType("MegaCrit.Sts2.Core.ValueProps.ValuePropExtensions")?
                .GetMethod("IsPoweredCardOrMonsterMoveBlock", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            bool isPoweredBlock = false;
            if (method != null)
            {
                isPoweredBlock = method.Invoke(null, new object[] { props }) as bool? ?? false;
            }

            if (!isPoweredBlock)
            {
                return 0m;
            }

            return Amount - 1;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

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