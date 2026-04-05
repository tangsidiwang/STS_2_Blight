using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class DampCultistBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "DampCultist";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        return;
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class DampCultistBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 3;
    public const int A3To4HpAdd = 4;
    public const int A5PlusHpAdd = 4;
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

[HarmonyPatch(typeof(DampCultist), "get_MinInitialHp")]
public static class DampCultistMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += DampCultistBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(DampCultist), "get_MaxInitialHp")]
public static class DampCultistMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += DampCultistBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(DampCultist), "get_IncantationAmount")]
public static class DampCultistIncantationAmountPatch
{
    private const int MutantIncantationAmount = 7;

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