using System;
using System.Linq;
using System.Threading.Tasks;
using BlightMod.Enchantments;
using BlightMod.Localization;
using BlightMod.Relics;
using BlightMod.Rewards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;

namespace BlightMod.Rewards.ForgeOptions
{
    public sealed class ForgeOptionUtility : IForgeOption
    {
        private readonly UtilityKind _kind;
        private readonly BlightEnchantmentRarity _rarity;

        public enum UtilityKind
        {
            Relic,
            Armor,
            Smith,
            Remove,
            MaxHpPercent,
        }

        public ForgeOptionUtility(UtilityKind kind, BlightEnchantmentRarity rarity)
        {
            _kind = kind;
            _rarity = rarity;
        }

        public string Title => _kind switch
        {
            UtilityKind.Relic => BlightLocalization.GetText("BLIGHT_FORGE.utility.title.relic"),
            UtilityKind.Armor => BlightLocalization.GetText("BLIGHT_FORGE.utility.title.armor"),
            UtilityKind.Smith => BlightLocalization.GetText("BLIGHT_FORGE.utility.title.smith"),
            UtilityKind.Remove => BlightLocalization.GetText("BLIGHT_FORGE.utility.title.remove"),
            UtilityKind.MaxHpPercent => BlightLocalization.GetText("BLIGHT_FORGE.utility.title.max_hp_percent"),
            _ => BlightLocalization.GetText("BLIGHT_FORGE.utility.title.fallback"),
        };

        public string Description => _kind switch
        {
            UtilityKind.Relic => BlightLocalization.Format("BLIGHT_FORGE.utility.desc.relic", ("Count", GetRelicCount().ToString())),
            UtilityKind.Armor => BlightLocalization.Format("BLIGHT_FORGE.utility.desc.armor", ("Amount", GetArmorPlatingAmount().ToString())),
            UtilityKind.Smith => BlightLocalization.GetText("BLIGHT_FORGE.utility.desc.smith"),
            UtilityKind.Remove => BlightLocalization.GetText("BLIGHT_FORGE.utility.desc.remove"),
            UtilityKind.MaxHpPercent => BlightLocalization.Format("BLIGHT_FORGE.utility.desc.max_hp_percent", ("Percent", GetMaxHpPercentValue().ToString())),
            _ => BlightLocalization.GetText("BLIGHT_FORGE.utility.desc.fallback"),
        };

        public BlightEnchantmentRarity Rarity => _rarity;

        public IEnumerable<IHoverTip> HoverTips => Array.Empty<IHoverTip>();

        public CardRarity DisplayCardRarity => _rarity switch
        {
            BlightEnchantmentRarity.Uncommon => CardRarity.Uncommon,
            BlightEnchantmentRarity.Rare => CardRarity.Rare,
            BlightEnchantmentRarity.UltraRare => CardRarity.Rare,
            BlightEnchantmentRarity.Negative => CardRarity.Curse,
            _ => CardRarity.Common,
        };

        public bool CanExecute(Player player)
        {
            if (player?.Deck == null)
            {
                return false;
            }

            return _kind switch
            {
                UtilityKind.Relic => true,
                UtilityKind.Armor => player.Creature != null,
                UtilityKind.Smith => player.Deck.Cards.Any(c => c.IsUpgradable),
                UtilityKind.Remove => player.Deck.Cards.Any(c => c.IsRemovable && c.Type != CardType.Quest),
                UtilityKind.MaxHpPercent => player.Creature != null && player.Creature.MaxHp > 0,
                _ => false,
            };
        }

        public async Task<bool> ExecuteAsync(Player player, Rng rng)
        {
            Log.Info($"[Blight][Forge] Execute utility option kind={_kind}, rarity={_rarity}");
            switch (_kind)
            {
                case UtilityKind.Relic:
                {
                    int relicCount = GetRelicCount();
                    for (int i = 0; i < relicCount; i++)
                    {
                        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player).ToMutable(), player);
                    }

                    Log.Info($"[Blight][Forge] Utility relic granted count={relicCount}.");
                    return true;
                }
                case UtilityKind.Armor:
                {
                    int platingAmount = GetArmorPlatingAmount();
                    if (platingAmount <= 0)
                    {
                        Log.Info("[Blight][Forge] Utility armor skipped because plating <= 0.");
                        return false;
                    }

                    BlightArmorRelic? armorRelic = player.GetRelic<BlightArmorRelic>();
                    if (armorRelic == null)
                    {
                        armorRelic = (BlightArmorRelic)ModelDb.Relic<BlightArmorRelic>().ToMutable();
                        armorRelic.PlatingAmount = platingAmount;
                        await RelicCmd.Obtain(armorRelic, player);
                        Log.Info($"[Blight][Forge] Utility armor granted new relic with plating={platingAmount}.");
                        return true;
                    }

                    armorRelic.PlatingAmount += platingAmount;
                    Log.Info($"[Blight][Forge] Utility armor increased plating by {platingAmount}, total={armorRelic.PlatingAmount}.");
                    return true;
                }
                case UtilityKind.Smith:
                {
                    var card = await ForgeLocalSelection.SelectDeckForUpgrade(
                        player,
                        new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
                        {
                            Cancelable = true,
                            RequireManualConfirmation = true,
                        });
                    if (card == null)
                    {
                        Log.Info("[Blight][Forge] Utility smith canceled.");
                        return false;
                    }

                    CardCmd.Upgrade(card, CardPreviewStyle.HorizontalLayout);
                    Log.Info($"[Blight][Forge] Utility smith upgraded card={card.Id}");
                    return true;
                }
                case UtilityKind.Remove:
                {
                    var card = await ForgeLocalSelection.SelectDeckForRemoval(
                        player,
                        new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
                        {
                            Cancelable = true,
                            RequireManualConfirmation = true,
                        });
                    if (card == null)
                    {
                        Log.Info("[Blight][Forge] Utility remove canceled.");
                        return false;
                    }

                    await CardPileCmd.RemoveFromDeck(card);
                    Log.Info($"[Blight][Forge] Utility remove removed card={card.Id}");
                    return true;
                }
                case UtilityKind.MaxHpPercent:
                {
                    decimal ratio = GetMaxHpPercentRatio();
                    decimal gain = Math.Ceiling(player.Creature.MaxHp * ratio);
                    if (gain <= 0m)
                    {
                        Log.Info("[Blight][Forge] Utility max hp skipped because computed gain <= 0.");
                        return false;
                    }

                    await CreatureCmd.GainMaxHp(player.Creature, gain);
                    Log.Info($"[Blight][Forge] Utility max hp granted gain={gain}, ratio={ratio}");
                    return true;
                }
                default:
                    return false;
            }
        }

        private int GetMaxHpPercentValue()
        {
            return _rarity switch
            {
                BlightEnchantmentRarity.Uncommon => 10,
                BlightEnchantmentRarity.Rare => 14,
                BlightEnchantmentRarity.UltraRare => 20,
                _ => 7,
            };
        }

        private decimal GetMaxHpPercentRatio()
        {
            return GetMaxHpPercentValue() / 100m;
        }

        private int GetRelicCount()
        {
            return _rarity == BlightEnchantmentRarity.UltraRare ? 2 : 1;
        }

        private int GetArmorPlatingAmount()
        {
            return _rarity switch
            {
                BlightEnchantmentRarity.Uncommon => 2,
                BlightEnchantmentRarity.Rare => 3,
                BlightEnchantmentRarity.UltraRare => 4,
                _ => 1,
            };
        }
    }
}
