using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

internal static class FatGremlinBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 1;
    public const int A3To4HpAdd = 2;
    public const int A5PlusHpAdd = 3;
    public const int MutantHpAdd = 3;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

public sealed class FatGremlinBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "FatGremlin";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        return;
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

[HarmonyPatch(typeof(FatGremlin), "get_MinInitialHp")]
public static class FatGremlinMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FatGremlinBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(FatGremlin), "get_MaxInitialHp")]
public static class FatGremlinMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FatGremlinBlightHpNumbers.GetHpAdd();
    }
}