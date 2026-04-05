using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments
{
    public sealed class BlightEnchantmentEntry
    {
        public ModelId EnchantmentId { get; set; } = ModelId.none;

        public int Amount { get; set; }

        public BlightEnchantmentRarity Rarity { get; set; }

        public bool IsNegative { get; set; }
    }
}
