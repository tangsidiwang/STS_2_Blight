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

public sealed class FrogKnightBlightAI : IBlightMonsterAI
{
    private const decimal TongueLashVulnerableAmount = 2m;

    public string TargetMonsterId => "FrogKnight";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not FrogKnight frogKnight)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int strikeDownEvilDamage = (int)AccessTools.Property(typeof(FrogKnight), "StrikeDownEvilDamage")!.GetValue(frogKnight)!;
        int tongueLashDamage = (int)AccessTools.Property(typeof(FrogKnight), "TongueLashDamage")!.GetValue(frogKnight)!;
        int beetleChargeDamage = (int)AccessTools.Property(typeof(FrogKnight), "BeetleChargeDamage")!.GetValue(frogKnight)!;

        var forTheQueen = new MoveState("FOR_THE_QUEEN", targets => InvokeOriginalMove(frogKnight, "ForTheQueenMove", targets), new BuffIntent());
        var strikeDownEvil = new MoveState("STRIKE_DOWN_EVIL", targets => InvokeOriginalMove(frogKnight, "StrikeDownEvilMove", targets), new SingleAttackIntent(strikeDownEvilDamage));
        var tongueLash = new MoveState(
            "TONGUE_LASH",
            targets => TongueLashMove(frogKnight, targets),
            new SingleAttackIntent(tongueLashDamage),
            new DebuffIntent());
        var beetleCharge = new MoveState("BEETLE_CHARGE", targets => InvokeOriginalMove(frogKnight, "BeetleChargeMove", targets), new SingleAttackIntent(beetleChargeDamage));
        var halfHealth = new ConditionalBranchState("HALF_HEALTH");

        halfHealth.AddState(tongueLash, () => HasBeetleCharged(frogKnight) || frogKnight.Creature.CurrentHp >= frogKnight.Creature.MaxHp / 2);
        halfHealth.AddState(beetleCharge, () => !HasBeetleCharged(frogKnight) && frogKnight.Creature.CurrentHp < frogKnight.Creature.MaxHp / 2);
        forTheQueen.FollowUpState = halfHealth;
        strikeDownEvil.FollowUpState = forTheQueen;
        tongueLash.FollowUpState = strikeDownEvil;
        beetleCharge.FollowUpState = tongueLash;

        return new MonsterMoveStateMachine(new List<MonsterState> { halfHealth, forTheQueen, strikeDownEvil, tongueLash, beetleCharge }, tongueLash);
    }

    private static async Task TongueLashMove(FrogKnight frogKnight, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(frogKnight, "TongueLashMove", targets);
        await PowerCmd.Apply<VulnerablePower>(targets, TongueLashVulnerableAmount, frogKnight.Creature, null);
    }

    private static bool HasBeetleCharged(FrogKnight frogKnight)
    {
        return (bool)AccessTools.Property(typeof(FrogKnight), "HasBeetleCharged")!.GetValue(frogKnight)!;
    }

    private static Task InvokeOriginalMove(FrogKnight frogKnight, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(FrogKnight), methodName)!.Invoke(frogKnight, new object[] { targets })!;
    }
}

internal static class FrogKnightBlightHpNumbers
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

[HarmonyPatch(typeof(FrogKnight), "get_MinInitialHp")]
public static class FrogKnightMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FrogKnightBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(FrogKnight), "get_MaxInitialHp")]
public static class FrogKnightMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}