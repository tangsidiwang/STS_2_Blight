using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;


namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightGlamEnchantment : EnchantmentModel, IBlightEnchantment
    {
        private const string _timesKey = "Times";

        private bool _usedThisCombat;

        protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Times", 1m) };

        // protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, base.DynamicVars["Times"]) };
        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType != CardType.None
                && cardType != CardType.Status
                && cardType != CardType.Curse;
        }
        private bool UsedThisCombat
        {
            get
            {
                return _usedThisCombat;
            }
            set
            {
                AssertMutable();
                _usedThisCombat = value;
            }
        }

        public override int EnchantPlayCount(int originalPlayCount)
        {
            if (UsedThisCombat)
            {
                return originalPlayCount;
            }
            return originalPlayCount + base.DynamicVars["Times"].IntValue;
        }

        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (UsedThisCombat)
            {
                return Task.CompletedTask;
            }
            if (cardPlay.Card != base.Card)
            {
                return Task.CompletedTask;
            }
            UsedThisCombat = true;
            base.Status = EnchantmentStatus.Disabled;
            return Task.CompletedTask;
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