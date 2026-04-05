using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class TestSubjectBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "TestSubject";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class TestSubjectBlightHpNumbers
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

[HarmonyPatch(typeof(TestSubject), "get_MinInitialHp")]
public static class TestSubjectMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TestSubjectBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(TestSubject), "RespawnMove")]
public static class TestSubjectRespawnMovePatch
{
    [HarmonyPostfix]
    public static async System.Threading.Tasks.Task Postfix(System.Threading.Tasks.Task __result, TestSubject __instance)
    {
        await __result;

        if (!BlightModeManager.IsBlightModeActive || !BlightAIContext.ShouldOverrideMonsterAi(__instance))
        {
            return;
        }

        int respawns = (int)AccessTools.Field(typeof(TestSubject), "_respawns")!.GetValue(__instance)!;
        if (respawns != 1)
        {
            return;
        }

        if (__instance.Creature == null || !__instance.Creature.IsAlive)
        {
            return;
        }

        await PowerCmd.Apply<IntangiblePower>(__instance.Creature, 2m, __instance.Creature, null);
    }
}

[HarmonyPatch(typeof(TestSubject), "get_MaxInitialHp")]
public static class TestSubjectMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TestSubjectBlightHpNumbers.GetHpAdd();
    }
}