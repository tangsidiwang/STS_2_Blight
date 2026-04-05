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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class ParafrightBlightAI : IBlightMonsterAI
{
    private const int MutantSlamDamage = 6;
    private const int MutantSlamRepeat = 2;
    private const int MutantSlamDazedCount = 2;

    public string TargetMonsterId => "Parafright";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Parafright parafright)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        var slamMove = new MoveState("SLAM_MOVE", targets => SlamMove(parafright, targets), new MultiAttackIntent(MutantSlamDamage, MutantSlamRepeat), new StatusIntent(MutantSlamDazedCount));
        slamMove.FollowUpState = slamMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { slamMove }, slamMove);
    }

    private static async Task SlamMove(Parafright parafright, IReadOnlyList<Creature> targets)
    {
        string attackSfx = (string)AccessTools.Property(typeof(Parafright), "AttackSfx")!.GetValue(parafright)!;
        await DamageCmd.Attack(MutantSlamDamage)
            .WithHitCount(MutantSlamRepeat)
            .FromMonster(parafright)
            .WithAttackerAnim("Attack", 0.15f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, attackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, MutantSlamDazedCount, addedByPlayer: false);
    }
}

internal static class ParafrightBlightHpNumbers
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

[HarmonyPatch(typeof(Parafright), "get_MinInitialHp")]
public static class ParafrightMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += ParafrightBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Parafright), "get_MaxInitialHp")]
public static class ParafrightMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += ParafrightBlightHpNumbers.GetHpAdd();
    }
}