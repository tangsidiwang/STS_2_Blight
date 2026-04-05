using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

internal static class SneakyGremlinBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 1;
    public const int A3To4HpAdd = 3;
    public const int A5PlusHpAdd = 4;
    public const int MutantHpAdd = 4;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

public sealed class SneakyGremlinBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "SneakyGremlin";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        return;
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

[HarmonyPatch(typeof(SneakyGremlin), "get_MinInitialHp")]
public static class SneakyGremlinMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SneakyGremlinBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SneakyGremlin), "get_MaxInitialHp")]
public static class SneakyGremlinMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SneakyGremlinBlightHpNumbers.GetHpAdd();
    }
}