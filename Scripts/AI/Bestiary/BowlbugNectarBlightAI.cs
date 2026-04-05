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

public sealed class BowlbugNectarBlightAI : IBlightMonsterAI
{
    private const decimal MutantBuffStrengthGain = 20m;

    public string TargetMonsterId => "BowlbugNectar";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not BowlbugNectar bowlbugNectar)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int thrashDamage = (int)AccessTools.Property(typeof(BowlbugNectar), "ThrashDamage")!.GetValue(bowlbugNectar)!;

        var thrashMove = new MoveState("THRASH_MOVE", targets => InvokeOriginalMove(bowlbugNectar, "ThrashMove", targets), new SingleAttackIntent(thrashDamage));
        var buffMove = new MoveState("BUFF_MOVE", targets => BuffMove(bowlbugNectar, targets), new BuffIntent());
        var thrash2Move = new MoveState("THRASH2_MOVE", targets => InvokeOriginalMove(bowlbugNectar, "ThrashMove", targets), new SingleAttackIntent(thrashDamage));

        thrashMove.FollowUpState = buffMove;
        buffMove.FollowUpState = thrash2Move;
        thrash2Move.FollowUpState = thrash2Move;

        return new MonsterMoveStateMachine(new List<MonsterState> { buffMove, thrash2Move, thrashMove }, thrashMove);
    }

    private static async Task BuffMove(BowlbugNectar bowlbugNectar, IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(bowlbugNectar.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<StrengthPower>(bowlbugNectar.Creature, MutantBuffStrengthGain, bowlbugNectar.Creature, null);
    }

    private static Task InvokeOriginalMove(BowlbugNectar bowlbugNectar, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(BowlbugNectar), methodName)!.Invoke(bowlbugNectar, new object[] { targets })!;
    }
}

internal static class BowlbugNectarBlightHpNumbers
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

[HarmonyPatch(typeof(BowlbugNectar), "get_MinInitialHp")]
public static class BowlbugNectarMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += BowlbugNectarBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(BowlbugNectar), "get_MaxInitialHp")]
public static class BowlbugNectarMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += BowlbugNectarBlightHpNumbers.GetHpAdd();
    }
}