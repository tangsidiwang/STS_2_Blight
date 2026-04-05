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

public sealed class TunnelerBlightAI : IBlightMonsterAI
{
    private const decimal MutantBurrowBonusStrength = 4m;

    public string TargetMonsterId => "Tunneler";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Tunneler tunneler)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int biteDamage = (int)AccessTools.Property(typeof(Tunneler), "BiteDamage")!.GetValue(tunneler)!;
        int belowDamage = (int)AccessTools.Property(typeof(Tunneler), "BelowDamage")!.GetValue(tunneler)!;

        var biteMove = new MoveState("BITE_MOVE", targets => InvokeOriginalMove(tunneler, "BiteMove", targets), new SingleAttackIntent(biteDamage));
        var burrowMove = new MoveState("BURROW_MOVE", targets => BurrowMove(tunneler, targets), new BuffIntent(), new DefendIntent());
        var belowMove = new MoveState("BELOW_MOVE_1", targets => InvokeOriginalMove(tunneler, "BelowMove", targets), new SingleAttackIntent(belowDamage));
        var dizzyMove = new MoveState("DIZZY_MOVE", targets => InvokeOriginalMove(tunneler, "StillDizzyMove", targets), new StunIntent());

        biteMove.FollowUpState = burrowMove;
        burrowMove.FollowUpState = belowMove;
        belowMove.FollowUpState = belowMove;
        dizzyMove.FollowUpState = biteMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { biteMove, burrowMove, belowMove, dizzyMove }, biteMove);
    }

    private static async Task BurrowMove(Tunneler tunneler, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(tunneler, "BurrowMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(tunneler.Creature, MutantBurrowBonusStrength, tunneler.Creature, null);
    }

    private static Task InvokeOriginalMove(Tunneler tunneler, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(Tunneler), methodName)!.Invoke(tunneler, new object[] { targets })!;
    }
}

internal static class TunnelerBlightHpNumbers
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

[HarmonyPatch(typeof(Tunneler), "get_MinInitialHp")]
public static class TunnelerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TunnelerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Tunneler), "get_MaxInitialHp")]
public static class TunnelerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TunnelerBlightHpNumbers.GetHpAdd();
    }
}