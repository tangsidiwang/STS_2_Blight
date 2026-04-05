using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Patches
{
    internal static class A10BaselineRule
    {
        public static bool ShouldForceA10(AscensionLevel level)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return false;
            }

            // 荒疫模式下将原版 A1-A10 的判定全部视为满足。
            return level >= AscensionLevel.SwarmingElites && level <= AscensionLevel.DoubleBoss;
        }
    }

    [HarmonyPatch(typeof(RunManager), "HasAscension")]
    public static class RunManagerHasAscensionPatch
    {
        public static void Postfix(AscensionLevel level, ref bool __result)
        {
            if (A10BaselineRule.ShouldForceA10(level))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(AscensionManager), "HasLevel")]
    public static class AscensionManagerHasLevelPatch
    {
        public static void Postfix(AscensionLevel level, ref bool __result)
        {
            if (A10BaselineRule.ShouldForceA10(level))
            {
                __result = true;
            }
        }
    }
}
