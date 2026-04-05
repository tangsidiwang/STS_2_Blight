using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace BlightMod.AI.Bestiary;

internal static class GasBombBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 3;
    public const int A5PlusHpAdd = 3;
    public const int MutantHpAdd = -7;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(GasBomb), "get_MinInitialHp")]
public static class GasBombMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += GasBombBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(GasBomb), "get_MaxInitialHp")]
public static class GasBombMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}