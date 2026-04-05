using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class CalcifiedCultistBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "CalcifiedCultist";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        return;
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class CalcifiedCultistBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 3;
    public const int A5PlusHpAdd = 3;
    public const int MutantHpAdd = 0;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(
            BlightModeManager.BlightAscensionLevel,
            mutant,
            A0HpAdd,
            A1To2HpAdd,
            A3To4HpAdd,
            A5PlusHpAdd,
            MutantHpAdd);
    }
}

[HarmonyPatch(typeof(CalcifiedCultist), "get_MinInitialHp")]
public static class CalcifiedCultistMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += CalcifiedCultistBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(CalcifiedCultist), "get_MaxInitialHp")]
public static class CalcifiedCultistMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += CalcifiedCultistBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(CalcifiedCultist), "get_IncantationAmount")]
public static class CalcifiedCultistIncantationAmountPatch
{
    private const int MutantIncantationAmount = 3;

    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        if (!BlightBestiaryHpTemplate.IsCurrentNodeMutant())
        {
            return;
        }

        __result = MutantIncantationAmount;
    }
}