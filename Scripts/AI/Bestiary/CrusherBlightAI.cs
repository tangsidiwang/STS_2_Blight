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

public sealed class CrusherBlightAI : IBlightMonsterAI
{
    private const int BugStingTimes = 2;
    private const decimal AdaptExtraPlating = 5m;

    public string TargetMonsterId => "Crusher";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Crusher crusher)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI changes only apply at A5+.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int thrashDamage = (int)AccessTools.Property(typeof(Crusher), "ThrashDamage")!.GetValue(crusher)!;
        int enlargingStrikeDamage = (int)AccessTools.Property(typeof(Crusher), "EnlargingStrikeDamage")!.GetValue(crusher)!;
        int bugStingDamage = (int)AccessTools.Property(typeof(Crusher), "BugStingDamage")!.GetValue(crusher)!;
        int guardedStrikeDamage = (int)AccessTools.Property(typeof(Crusher), "GuardedStrikeDamage")!.GetValue(crusher)!;

        var thrashMove = new MoveState("THRASH_MOVE", targets => InvokeOriginalMove(crusher, "ThrashMove", targets), new SingleAttackIntent(thrashDamage));
        var enlargingStrikeMove = new MoveState("ENLARGING_STRIKE_MOVE", targets => InvokeOriginalMove(crusher, "EnlargingStrikeMove", targets), new SingleAttackIntent(enlargingStrikeDamage));
        var bugStingMove = new MoveState("BUG_STING_MOVE", targets => InvokeOriginalMove(crusher, "BugStingMove", targets), new MultiAttackIntent(bugStingDamage, BugStingTimes), new DebuffIntent());
        var adaptMove = new MoveState("ADAPT_MOVE", targets => AdaptMove(crusher, targets), new BuffIntent());
        var guardedStrikeMove = new MoveState("GUARDED_STRIKE_MOVE", targets => InvokeOriginalMove(crusher, "GuardedStrikeMove", targets), new SingleAttackIntent(guardedStrikeDamage), new DefendIntent());

        thrashMove.FollowUpState = enlargingStrikeMove;
        enlargingStrikeMove.FollowUpState = bugStingMove;
        bugStingMove.FollowUpState = adaptMove;
        adaptMove.FollowUpState = guardedStrikeMove;
        guardedStrikeMove.FollowUpState = thrashMove;

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState> { thrashMove, enlargingStrikeMove, bugStingMove, adaptMove, guardedStrikeMove },
            thrashMove);
    }

    private static async System.Threading.Tasks.Task AdaptMove(Crusher crusher, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(crusher, "AdaptMove", targets);
        await PowerCmd.Apply<PlatingPower>(crusher.Creature, AdaptExtraPlating, crusher.Creature, null);
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(Crusher crusher, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(Crusher), methodName)!.Invoke(crusher, new object[] { targets })!;
    }
}

internal static class CrusherBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
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

[HarmonyPatch(typeof(Crusher), "get_MinInitialHp")]
public static class CrusherMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += CrusherBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Crusher), "get_MaxInitialHp")]
public static class CrusherMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}