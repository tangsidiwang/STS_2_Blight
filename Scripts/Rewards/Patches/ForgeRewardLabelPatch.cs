using BlightMod.Rewards;
using BlightMod.Localization;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace BlightMod.Rewards.Patches
{
    [HarmonyPatch(typeof(NRewardButton), "Reload")]
    public static class ForgeRewardLabelPatch
    {
        private static readonly AccessTools.FieldRef<NRewardButton, MegaRichTextLabel> LabelRef =
            AccessTools.FieldRefAccess<NRewardButton, MegaRichTextLabel>("_label");

        [HarmonyPostfix]
        public static void Postfix(NRewardButton __instance)
        {
            if (__instance?.Reward is not ForgeRewardItem forge)
            {
                return;
            }

            string rarityText = GetLocalizedRarityText(forge.BaseRewardLevel);
            LabelRef(__instance).Text = BlightLocalization.Format("BLIGHT_FORGE.reward_label", ("Rarity", rarityText));
        }

        private static string GetLocalizedRarityText(Enchantments.BlightEnchantmentRarity rarity)
        {
            string rarityKey = rarity switch
            {
                Enchantments.BlightEnchantmentRarity.Uncommon => "BLIGHT_FORGE.rarity.uncommon",
                Enchantments.BlightEnchantmentRarity.Rare => "BLIGHT_FORGE.rarity.rare",
                Enchantments.BlightEnchantmentRarity.UltraRare => "BLIGHT_FORGE.rarity.ultra_rare",
                Enchantments.BlightEnchantmentRarity.Negative => "BLIGHT_FORGE.rarity.negative",
                _ => "BLIGHT_FORGE.rarity.common",
            };

            string colorTag = rarity switch
            {
                Enchantments.BlightEnchantmentRarity.Common => "#B8B8B8",
                Enchantments.BlightEnchantmentRarity.Uncommon => "#58A6FF",
                Enchantments.BlightEnchantmentRarity.Rare => "#F2C94C",
                Enchantments.BlightEnchantmentRarity.UltraRare => "#FF8A3D",
                Enchantments.BlightEnchantmentRarity.Negative => "#FF5C5C",
                _ => "#B8B8B8",
            };

            string localized = BlightLocalization.GetText(rarityKey);
            return $"[color={colorTag}]{localized}[/color]";
        }
    }
}
