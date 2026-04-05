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

public sealed class ShrinkerBeetleBlightAI : IBlightMonsterAI
{
    private const decimal MutantShrinkerExtraFrail = 99m;

    public string TargetMonsterId => "ShrinkerBeetle";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not ShrinkerBeetle shrinkerBeetle)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int chompDamage = (int)AccessTools.Property(typeof(ShrinkerBeetle), "ChompDamage")!.GetValue(shrinkerBeetle)!;
        int stompDamage = (int)AccessTools.Property(typeof(ShrinkerBeetle), "StompDamage")!.GetValue(shrinkerBeetle)!;

        var shrinkerMove = new MoveState("SHRINKER_MOVE", targets => ShrinkerMove(shrinkerBeetle, targets), new DebuffIntent(strong: true));
        var chompMove = new MoveState("CHOMP_MOVE", targets => InvokeOriginalMove(shrinkerBeetle, "ChompMove", targets), new SingleAttackIntent(chompDamage));
        var stompMove = new MoveState("STOMP_MOVE", targets => InvokeOriginalMove(shrinkerBeetle, "StompMove", targets), new SingleAttackIntent(stompDamage));

        shrinkerMove.FollowUpState = chompMove;
        chompMove.FollowUpState = stompMove;
        stompMove.FollowUpState = chompMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { shrinkerMove, chompMove, stompMove }, shrinkerMove);
    }

    private static async Task ShrinkerMove(ShrinkerBeetle shrinkerBeetle, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(shrinkerBeetle, "ShrinkMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<FrailPower>(targets, MutantShrinkerExtraFrail, shrinkerBeetle.Creature, null);
    }

    private static Task InvokeOriginalMove(ShrinkerBeetle shrinkerBeetle, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(ShrinkerBeetle), methodName)!.Invoke(shrinkerBeetle, new object[] { targets })!;
    }
}

internal static class ShrinkerBeetleBlightHpNumbers
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

[HarmonyPatch(typeof(ShrinkerBeetle), "get_MinInitialHp")]
public static class ShrinkerBeetleMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += ShrinkerBeetleBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(ShrinkerBeetle), "get_MaxInitialHp")]
public static class ShrinkerBeetleMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += ShrinkerBeetleBlightHpNumbers.GetHpAdd();
    }
}