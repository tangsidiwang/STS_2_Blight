using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class OvicopterBlightAI : IBlightMonsterAI
{
    private const decimal MutantTenderizerExtraWeak = 2m;

    public string TargetMonsterId => "Ovicopter";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Ovicopter ovicopter)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int smashDamage = (int)AccessTools.Property(typeof(Ovicopter), "SmashDamage")!.GetValue(ovicopter)!;
        int tenderizerDamage = (int)AccessTools.Property(typeof(Ovicopter), "TenderizerDamage")!.GetValue(ovicopter)!;

        var layEggsMove = new MoveState("LAY_EGGS_MOVE", targets => InvokeOriginalMove(ovicopter, "LayEggsMove", targets), new SummonIntent());
        var smashMove = new MoveState("SMASH_MOVE", targets => InvokeOriginalMove(ovicopter, "SmashMove", targets), new SingleAttackIntent(smashDamage));
        var tenderizerMove = new MoveState("TENDERIZER_MOVE", targets => TenderizerMove(ovicopter, targets), new SingleAttackIntent(tenderizerDamage), new DebuffIntent());
        var nutritionalPasteMove = new MoveState("NUTRITIONAL_PASTE_MOVE", targets => InvokeOriginalMove(ovicopter, "NutritionalPasteMove", targets), new BuffIntent());
        var summonBranchState = new ConditionalBranchState("SUMMON_BRANCH_STATE");

        layEggsMove.FollowUpState = smashMove;
        nutritionalPasteMove.FollowUpState = smashMove;
        smashMove.FollowUpState = tenderizerMove;
        tenderizerMove.FollowUpState = summonBranchState;
        summonBranchState.AddState(layEggsMove, () => (bool)AccessTools.Property(typeof(Ovicopter), "CanLay")!.GetValue(ovicopter)!);
        summonBranchState.AddState(nutritionalPasteMove, () => !(bool)AccessTools.Property(typeof(Ovicopter), "CanLay")!.GetValue(ovicopter)!);

        return new MonsterMoveStateMachine(new List<MonsterState> { nutritionalPasteMove, layEggsMove, smashMove, tenderizerMove, summonBranchState }, layEggsMove);
    }

    private static async Task TenderizerMove(Ovicopter ovicopter, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(ovicopter, "TenderizerMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<WeakPower>(targets, MutantTenderizerExtraWeak, ovicopter.Creature, null);
    }

    private static Task InvokeOriginalMove(Ovicopter ovicopter, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(Ovicopter), methodName)!.Invoke(ovicopter, new object[] { targets })!;
    }
}

internal static class OvicopterBlightHpNumbers
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

[HarmonyPatch(typeof(Ovicopter), "get_MinInitialHp")]
public static class OvicopterMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += OvicopterBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Ovicopter), "get_MaxInitialHp")]
public static class OvicopterMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += OvicopterBlightHpNumbers.GetHpAdd();
    }
}