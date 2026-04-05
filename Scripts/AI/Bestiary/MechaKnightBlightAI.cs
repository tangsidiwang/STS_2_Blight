using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class MechaKnightBlightAI : IBlightMonsterAI
{
    private const int FlamethrowerCardCount = 4;

    public string TargetMonsterId => "MechaKnight";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not MechaKnight mechaKnight)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int chargeDamage = (int)AccessTools.Property(typeof(MechaKnight), "ChargeDamage")!.GetValue(null)!;
        int heavyCleaveDamage = (int)AccessTools.Property(typeof(MechaKnight), "HeavyCleaveDamage")!.GetValue(null)!;

        var chargeMove = new MoveState(
            "CHARGE_MOVE",
            targets => InvokeOriginalMove(mechaKnight, "ChargeMove", targets),
            new SingleAttackIntent(chargeDamage));
        var flamethrowerMove = new MoveState(
            "FLAMETHROWER_MOVE",
            targets => FlamethrowerMove(mechaKnight, targets),
            new StatusIntent(FlamethrowerCardCount));
        var windupMove = new MoveState(
            "WINDUP_MOVE",
            targets => InvokeOriginalMove(mechaKnight, "WindupMove", targets),
            new DefendIntent(),
            new BuffIntent());
        var heavyCleaveMove = new MoveState(
            "HEAVY_CLEAVE_MOVE",
            targets => InvokeOriginalMove(mechaKnight, "HeavyCleaveMove", targets),
            new SingleAttackIntent(heavyCleaveDamage));

        chargeMove.FollowUpState = flamethrowerMove;
        flamethrowerMove.FollowUpState = windupMove;
        windupMove.FollowUpState = heavyCleaveMove;
        heavyCleaveMove.FollowUpState = flamethrowerMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { chargeMove, heavyCleaveMove, windupMove, flamethrowerMove }, chargeMove);
    }

    private static async Task FlamethrowerMove(MechaKnight mechaKnight, IReadOnlyList<Creature> targets)
    {
        string flameSfx = "event:/sfx/enemy/enemy_attacks/mechaknight/mechaknight_flamethrower";
        SfxCmd.Play(flameSfx);
        await CreatureCmd.TriggerAnim(mechaKnight.Creature, "flamethrower", 1.5f);

        foreach (Creature target in targets)
        {
            for (int index = 0; index < FlamethrowerCardCount; index++)
            {
                CardModel burn = mechaKnight.CombatState.CreateCard<Burn>(target.Player!);
                CardCmd.Upgrade(burn);
                await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Hand, addedByPlayer: false);
            }
        }
    }

    private static Task InvokeOriginalMove(MechaKnight mechaKnight, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(MechaKnight), methodName)!.Invoke(mechaKnight, new object[] { targets })!;
    }
}

internal static class MechaKnightBlightHpNumbers
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

[HarmonyPatch(typeof(MechaKnight), "get_MinInitialHp")]
public static class MechaKnightMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += MechaKnightBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(MechaKnight), "get_MaxInitialHp")]
public static class MechaKnightMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += MechaKnightBlightHpNumbers.GetHpAdd();
    }
}