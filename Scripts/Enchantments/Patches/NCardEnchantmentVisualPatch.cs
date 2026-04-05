using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(NCard), "UpdateEnchantmentVisuals")]
    public static class NCardEnchantmentVisualPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCard __instance)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return;
            }

            BlightEnchantmentUiRenderer.RenderTabs(__instance);
        }
    }
}
