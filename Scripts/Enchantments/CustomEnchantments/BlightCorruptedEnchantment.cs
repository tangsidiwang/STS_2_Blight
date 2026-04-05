using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightCorruptedEnchantment : EnchantmentModel, IBlightEnchantment
    {
        private const decimal _damageAmount = 2m;

        public override bool HasExtraCardText => true;

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
            return 1.5m;
        }

        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            await CreatureCmd.Damage(choiceContext, base.Card.Owner.Creature, 2m, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, base.Card);
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
