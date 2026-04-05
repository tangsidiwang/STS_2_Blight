using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace BlightMod.AI.Bestiary;

internal static class WrigglerBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
    public const int MutantHpAdd = 0;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(Wriggler), "get_MinInitialHp")]
public static class WrigglerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += WrigglerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Wriggler), "get_MaxInitialHp")]
public static class WrigglerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += WrigglerBlightHpNumbers.GetHpAdd();
    }
}