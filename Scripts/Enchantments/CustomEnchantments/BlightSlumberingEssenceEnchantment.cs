using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightSlumberingEssenceEnchantment : EnchantmentModel, IBlightEnchantment
    {

        public override Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
        {
            Log.Info($"000");

            if (player != base.Card.Owner)
            {
                return Task.CompletedTask;
            }
            Log.Info($"1111");

            CardPile? pile = base.Card.Pile;
            if (pile == null || pile.Type != PileType.Hand)
            {
                return Task.CompletedTask;
            }
            Log.Info($"3333");
            base.Card.EnergyCost.AddUntilPlayed(-1);
            return Task.CompletedTask;
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