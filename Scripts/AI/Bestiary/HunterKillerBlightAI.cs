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

public sealed class HunterKillerBlightAI : IBlightMonsterAI
{
    private const decimal MutantGoopExtraPainfulStabs = 1m;
    private const int PunctureRepeat = 3;

    public string TargetMonsterId => "HunterKiller";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not HunterKiller hunterKiller)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int biteDamage = (int)AccessTools.Property(typeof(HunterKiller), "BiteDamage")!.GetValue(hunterKiller)!;
        int punctureDamage = (int)AccessTools.Property(typeof(HunterKiller), "PunctureDamage")!.GetValue(hunterKiller)!;

        var goopMove = new MoveState("TENDERIZING_GOOP_MOVE", targets => GoopMove(hunterKiller, targets), new DebuffIntent());
        var biteMove = new MoveState("BITE_MOVE", targets => InvokeOriginalMove(hunterKiller, "BiteMove", targets), new SingleAttackIntent(biteDamage));
        var punctureMove = new MoveState("PUNCTURE_MOVE", targets => InvokeOriginalMove(hunterKiller, "PunctureMove", targets), new MultiAttackIntent(punctureDamage, PunctureRepeat));
        var randomMove = new RandomBranchState("RAND");

        goopMove.FollowUpState = randomMove;
        biteMove.FollowUpState = randomMove;
        punctureMove.FollowUpState = randomMove;
        randomMove.AddBranch(biteMove, MoveRepeatType.CannotRepeat);
        randomMove.AddBranch(punctureMove, 2);

        return new MonsterMoveStateMachine(new List<MonsterState> { goopMove, biteMove, punctureMove, randomMove }, goopMove);
    }

    private static async Task GoopMove(HunterKiller hunterKiller, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(hunterKiller, "GoopMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<PainfulStabsPower>(targets, MutantGoopExtraPainfulStabs, hunterKiller.Creature, null);
    }

    private static Task InvokeOriginalMove(HunterKiller hunterKiller, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(HunterKiller), methodName)!.Invoke(hunterKiller, new object[] { targets })!;
    }
}

internal static class HunterKillerBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
    public const int MutantHpAdd = -10;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(HunterKiller), "get_MinInitialHp")]
public static class HunterKillerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += HunterKillerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(HunterKiller), "get_MaxInitialHp")]
public static class HunterKillerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}