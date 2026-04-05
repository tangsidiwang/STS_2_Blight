using System.Threading.Tasks;
using BlightMod.Enchantments;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Random;

namespace BlightMod.Rewards.ForgeOptions
{
    public interface IForgeOption
    {
        string Title { get; }

        string Description { get; }

        BlightEnchantmentRarity Rarity { get; }

        CardRarity DisplayCardRarity { get; }

        IEnumerable<IHoverTip> HoverTips { get; }

        bool CanExecute(Player player);

        Task<bool> ExecuteAsync(Player player, Rng rng);
    }
}
