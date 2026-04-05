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

public sealed class SlitheringStranglerBlightAI : IBlightMonsterAI
{
    private const decimal MutantConstrictExtraFrail = 1m;

    public string TargetMonsterId => "SlitheringStrangler";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SlitheringStrangler slitheringStrangler)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int thwackDamage = (int)AccessTools.Property(typeof(SlitheringStrangler), "ThwackDamage")!.GetValue(slitheringStrangler)!;
        int lashDamage = (int)AccessTools.Property(typeof(SlitheringStrangler), "LashDamage")!.GetValue(slitheringStrangler)!;

        var constrictMove = new MoveState("CONSTRICT", targets => ConstrictMove(slitheringStrangler, targets), new DebuffIntent());
        var thwackMove = new MoveState("TWACK", targets => InvokeOriginalMove(slitheringStrangler, "ThwackMove", targets), new SingleAttackIntent(thwackDamage), new DefendIntent());
        var lashMove = new MoveState("LASH", targets => InvokeOriginalMove(slitheringStrangler, "LashMove", targets), new SingleAttackIntent(lashDamage));
        var randomMove = new RandomBranchState("rand");

        constrictMove.FollowUpState = randomMove;
        thwackMove.FollowUpState = constrictMove;
        lashMove.FollowUpState = constrictMove;

        randomMove.AddBranch(thwackMove, MoveRepeatType.CanRepeatForever);
        randomMove.AddBranch(lashMove, MoveRepeatType.CanRepeatForever);

        return new MonsterMoveStateMachine(new List<MonsterState> { randomMove, thwackMove, constrictMove, lashMove }, constrictMove);
    }

    private static async Task ConstrictMove(SlitheringStrangler slitheringStrangler, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(slitheringStrangler, "ConstrictMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<FrailPower>(targets, MutantConstrictExtraFrail, slitheringStrangler.Creature, null);
    }

    private static Task InvokeOriginalMove(SlitheringStrangler slitheringStrangler, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(SlitheringStrangler), methodName)!.Invoke(slitheringStrangler, new object[] { targets })!;
    }
}

internal static class SlitheringStranglerBlightHpNumbers
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

[HarmonyPatch(typeof(SlitheringStrangler), "get_MinInitialHp")]
public static class SlitheringStranglerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SlitheringStranglerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SlitheringStrangler), "get_MaxInitialHp")]
public static class SlitheringStranglerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SlitheringStranglerBlightHpNumbers.GetHpAdd();
    }
}