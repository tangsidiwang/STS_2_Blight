using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class FlyconidBlightAI : IBlightMonsterAI
{
    private const decimal MutantExtraWeak = 1m;

    public string TargetMonsterId => "Flyconid";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Flyconid flyconid)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int smashDamage = (int)AccessTools.Property(typeof(Flyconid), "SmashDamage")!.GetValue(flyconid)!;
        int sporeDamage = (int)AccessTools.Property(typeof(Flyconid), "SporeDamage")!.GetValue(flyconid)!;

        var vulnerableSporesMove = new MoveState("VULNERABLE_SPORES_MOVE", targets => VulnerableSporesMove(flyconid, targets), new DebuffIntent());
        var frailSporesMove = new MoveState("FRAIL_SPORES_MOVE", targets => FrailSporesMove(flyconid, targets), new SingleAttackIntent(sporeDamage), new DebuffIntent());
        var smashMove = new MoveState("SMASH_MOVE", targets => InvokeOriginalMove(flyconid, "SmashMove", targets), new SingleAttackIntent(smashDamage));
        var randomMove = new RandomBranchState("RAND");
        var initialMove = new RandomBranchState("INITIAL");

        vulnerableSporesMove.FollowUpState = randomMove;
        frailSporesMove.FollowUpState = randomMove;
        smashMove.FollowUpState = randomMove;

        randomMove.AddBranch(vulnerableSporesMove, 3, MoveRepeatType.CannotRepeat);
        randomMove.AddBranch(frailSporesMove, 2, MoveRepeatType.CannotRepeat);
        randomMove.AddBranch(smashMove, MoveRepeatType.CannotRepeat);

        initialMove.AddBranch(frailSporesMove, 2, MoveRepeatType.CannotRepeat);
        initialMove.AddBranch(smashMove, MoveRepeatType.CannotRepeat);

        return new MonsterMoveStateMachine(
            new List<MonsterState> { vulnerableSporesMove, frailSporesMove, smashMove, randomMove, initialMove },
            initialMove);
    }

    private static async Task VulnerableSporesMove(Flyconid flyconid, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(flyconid, "VulnerableSporesMove", targets);
        await ApplyMutantExtraWeak(flyconid, targets);
    }

    private static async Task FrailSporesMove(Flyconid flyconid, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(flyconid, "FrailSporesMove", targets);
        await ApplyMutantExtraWeak(flyconid, targets);
    }

    private static async Task ApplyMutantExtraWeak(Flyconid flyconid, IReadOnlyList<Creature> targets)
    {
        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<WeakPower>(targets, MutantExtraWeak, flyconid.Creature, null);
    }

    private static Task InvokeOriginalMove(Flyconid flyconid, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(Flyconid), methodName)!.Invoke(flyconid, new object[] { targets })!;
    }
}

internal static class FlyconidBlightHpNumbers
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

[HarmonyPatch(typeof(Flyconid), "get_MinInitialHp")]
public static class FlyconidMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FlyconidBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Flyconid), "get_MaxInitialHp")]
public static class FlyconidMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FlyconidBlightHpNumbers.GetHpAdd();
    }
}