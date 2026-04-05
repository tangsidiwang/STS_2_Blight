using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightPerfectEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => false;



        public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
	{
		if (!isInitialShuffle && cards.Contains(base.Card))
		{
			cards.Remove(base.Card);
			cards.Insert(0, base.Card);
		}
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