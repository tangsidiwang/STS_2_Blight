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

public sealed class AxebotBlightAI : IBlightMonsterAI
{
    private const decimal HammerUppercutVulnerableAmount = 1m;

    public string TargetMonsterId => "Axebot";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Axebot axebot)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int oneTwoDamage = (int)AccessTools.Property(typeof(Axebot), "OneTwoDamage")!.GetValue(axebot)!;
        int hammerUppercutDamage = (int)AccessTools.Property(typeof(Axebot), "HammerUppercutDamage")!.GetValue(axebot)!;
        bool hasStockOverride = AccessTools.Field(typeof(Axebot), "_stockOverrideAmount")!.GetValue(axebot) is not null;

        var bootUpMove = new MoveState("BOOT_UP_MOVE", targets => InvokeOriginalMove(axebot, "BootUpMove", targets), new DefendIntent(), new BuffIntent());
        var oneTwoMove = new MoveState("ONE_TWO_MOVE", targets => InvokeOriginalMove(axebot, "OneTwoMove", targets), new MultiAttackIntent(oneTwoDamage, 2));
        var sharpenMove = new MoveState("SHARPEN_MOVE", targets => InvokeOriginalMove(axebot, "SharpenMove", targets), new BuffIntent());
        var hammerUppercutMove = new MoveState(
            "HAMMER_UPPERCUT_MOVE",
            targets => HammerUppercutMove(axebot, targets),
            new SingleAttackIntent(hammerUppercutDamage),
            new DebuffIntent());
        var randomMove = new RandomBranchState("RAND_MOVE");

        randomMove.AddBranch(oneTwoMove, 2);
        randomMove.AddBranch(sharpenMove, MoveRepeatType.CannotRepeat);
        randomMove.AddBranch(hammerUppercutMove, 2);
        bootUpMove.FollowUpState = randomMove;
        oneTwoMove.FollowUpState = randomMove;
        sharpenMove.FollowUpState = randomMove;
        hammerUppercutMove.FollowUpState = randomMove;

        var states = new List<MonsterState> { bootUpMove, oneTwoMove, sharpenMove, hammerUppercutMove, randomMove };
        return new MonsterMoveStateMachine(states, hasStockOverride ? bootUpMove : randomMove);
    }

    private static async Task HammerUppercutMove(Axebot axebot, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(axebot, "HammerUppercutMove", targets);
        await PowerCmd.Apply<VulnerablePower>(targets, HammerUppercutVulnerableAmount, axebot.Creature, null);
    }

    private static Task InvokeOriginalMove(Axebot axebot, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(Axebot), methodName)!.Invoke(axebot, new object[] { targets })!;
    }
}

internal static class AxebotBlightHpNumbers
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

[HarmonyPatch(typeof(Axebot), "get_MinInitialHp")]
public static class AxebotMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += AxebotBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Axebot), "get_MaxInitialHp")]
public static class AxebotMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += AxebotBlightHpNumbers.GetHpAdd();
    }
}