using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Enchantments
{
    public interface IBlightEnchantment
    {
        BlightEnchantmentRarity Rarity { get; }

        bool CanApplyTo(CardModel card);

        bool AllowDuplicateInstances { get; }

        int? MaxDuplicateInstancesPerCard { get; }
    }
}
