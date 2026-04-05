using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(NCard), nameof(NCard.OnReturnedFromPool))]
    public static class NCardPoolCleanupPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCard __instance)
        {
            BlightEnchantmentUiRenderer.ClearInjectedTabs(__instance);
        }
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    public static class NCardFreedPoolCleanupPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCard __instance)
        {
            BlightEnchantmentUiRenderer.ClearInjectedTabs(__instance);
        }
    }
}
