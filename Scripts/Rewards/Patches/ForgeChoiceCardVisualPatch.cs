using BlightMod.Rewards.ForgeOptions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BlightMod.Rewards.Patches
{
    [HarmonyPatch(typeof(CardModel), "get_Title")]
    public static class ForgeChoiceCardTitlePatch
    {
        private static readonly HashSet<int> LoggedCards = new HashSet<int>();

        [HarmonyPrefix]
        public static bool Prefix(CardModel __instance, ref string __result)
        {
            if (!ForgeChoiceRegistry.TryGet(__instance, out IForgeOption? option))
            {
                return true;
            }

            int hash = RuntimeHelpers.GetHashCode(__instance);
            if (LoggedCards.Add(hash))
            {
                Log.Info($"[Blight][ForgeDebug] Patch.Title HIT card={__instance.Id} hash={hash} option={option.Title} registryCount={ForgeChoiceRegistry.Count}");
            }

            __result = option.Title;
            return false;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), new[] { typeof(PileType), typeof(Creature) })]
    public static class ForgeChoiceCardDescriptionPatch
    {
        private static readonly HashSet<int> LoggedCards = new HashSet<int>();

        [HarmonyPrefix]
        public static bool Prefix(CardModel __instance, PileType pileType, Creature? target, ref string __result)
        {
            if (!ForgeChoiceRegistry.TryGet(__instance, out IForgeOption? option))
            {
                return true;
            }

            int hash = RuntimeHelpers.GetHashCode(__instance);
            if (LoggedCards.Add(hash))
            {
                string enchantType = __instance.Enchantment?.GetType().Name ?? "none";
                Log.Info($"[Blight][ForgeDebug] Patch.Description HIT card={__instance.Id} hash={hash} pile={pileType} targetNull={target == null} enchant={enchantType} option={option.Title} registryCount={ForgeChoiceRegistry.Count}");
            }

            __result = option.Description;
            return false;
        }
    }

    [HarmonyPatch(typeof(CardModel), "get_Rarity")]
    public static class ForgeChoiceCardRarityPatch
    {
        private static readonly HashSet<int> LoggedCards = new HashSet<int>();

        [HarmonyPrefix]
        public static bool Prefix(CardModel __instance, ref CardRarity __result)
        {
            if (!ForgeChoiceRegistry.TryGet(__instance, out IForgeOption? option))
            {
                return true;
            }

            int hash = RuntimeHelpers.GetHashCode(__instance);
            if (LoggedCards.Add(hash))
            {
                Log.Info($"[Blight][ForgeDebug] Patch.Rarity HIT card={__instance.Id} hash={hash} optionRarity={option.DisplayCardRarity} registryCount={ForgeChoiceRegistry.Count}");
            }

            __result = option.DisplayCardRarity;
            return false;
        }
    }

    [HarmonyPatch(typeof(CardModel), "get_HoverTips")]
    public static class ForgeChoiceCardHoverTipsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!ForgeChoiceRegistry.TryGet(__instance, out IForgeOption? option))
            {
                return true;
            }

            __result = option.HoverTips?.ToList() ?? new List<IHoverTip>();
            return false;
        }
    }
}
