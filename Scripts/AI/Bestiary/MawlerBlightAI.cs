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

public sealed class MawlerBlightAI : IBlightMonsterAI
{
    private const decimal MutantRoarExtraFrail = 3m;

    public string TargetMonsterId => "Mawler";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Mawler mawler)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int ripAndTearDamage = (int)AccessTools.Property(typeof(Mawler), "RipAndTearDamage")!.GetValue(mawler)!;
        int clawDamage = (int)AccessTools.Property(typeof(Mawler), "ClawDamage")!.GetValue(mawler)!;

        var ripAndTearMove = new MoveState("RIP_AND_TEAR_MOVE", targets => InvokeOriginalMove(mawler, "RipAndTearMove", targets), new SingleAttackIntent(ripAndTearDamage));
        var roarMove = new MoveState("ROAR_MOVE", targets => RoarMove(mawler, targets), new DebuffIntent());
        var clawMove = new MoveState("CLAW_MOVE", targets => InvokeOriginalMove(mawler, "ClawMove", targets), new MultiAttackIntent(clawDamage, 2));
        var randomMove = new RandomBranchState("RAND");

        ripAndTearMove.FollowUpState = randomMove;
        roarMove.FollowUpState = randomMove;
        clawMove.FollowUpState = randomMove;

        randomMove.AddBranch(ripAndTearMove, MoveRepeatType.CannotRepeat, 1f);
        randomMove.AddBranch(roarMove, MoveRepeatType.UseOnlyOnce, 1f);
        randomMove.AddBranch(clawMove, MoveRepeatType.CannotRepeat, 1f);

        return new MonsterMoveStateMachine(new List<MonsterState> { ripAndTearMove, roarMove, clawMove, randomMove }, clawMove);
    }

    private static async Task RoarMove(Mawler mawler, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(mawler, "RoarMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<FrailPower>(targets, MutantRoarExtraFrail, mawler.Creature, null);
    }

    private static Task InvokeOriginalMove(Mawler mawler, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(Mawler), methodName)!.Invoke(mawler, new object[] { targets })!;
    }
}

internal static class MawlerBlightHpNumbers
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

[HarmonyPatch(typeof(Mawler), "get_MinInitialHp")]
public static class MawlerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += MawlerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Mawler), "get_MaxInitialHp")]
public static class MawlerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += MawlerBlightHpNumbers.GetHpAdd();
    }
}