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

public sealed class SpinyToadBlightAI : IBlightMonsterAI
{
    private const decimal MutantSpikeThorns = 20m;

    public string TargetMonsterId => "SpinyToad";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SpinyToad spinyToad)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int lashDamage = (int)AccessTools.Property(typeof(SpinyToad), "LashDamage")!.GetValue(spinyToad)!;
        int explosionDamage = (int)AccessTools.Property(typeof(SpinyToad), "ExplosionDamage")!.GetValue(spinyToad)!;

        var spikesMove = new MoveState("PROTRUDING_SPIKES_MOVE", targets => SpikesMove(spinyToad, targets), new BuffIntent());
        var explosionMove = new MoveState("SPIKE_EXPLOSION_MOVE", targets => ExplosionMove(spinyToad, targets), new SingleAttackIntent(explosionDamage));
        var lashMove = new MoveState("TONGUE_LASH_MOVE", targets => InvokeOriginalMove(spinyToad, "LashMove", targets), new SingleAttackIntent(lashDamage));

        spikesMove.FollowUpState = explosionMove;
        explosionMove.FollowUpState = lashMove;
        lashMove.FollowUpState = spikesMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { spikesMove, explosionMove, lashMove }, spikesMove);
    }

    private static async Task SpikesMove(SpinyToad spinyToad, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(spinyToad, "SpikesMove", targets);
        await PowerCmd.Apply<ThornsPower>(spinyToad.Creature, MutantSpikeThorns - 5m, spinyToad.Creature, null);
    }

    private static async Task ExplosionMove(SpinyToad spinyToad, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(spinyToad, "ExplosionMove", targets);
        await PowerCmd.Apply<ThornsPower>(spinyToad.Creature, -(MutantSpikeThorns - 5m), spinyToad.Creature, null);
    }

    private static Task InvokeOriginalMove(SpinyToad spinyToad, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(SpinyToad), methodName)!.Invoke(spinyToad, new object[] { targets })!;
    }
}

internal static class SpinyToadBlightHpNumbers
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

[HarmonyPatch(typeof(SpinyToad), "get_MinInitialHp")]
public static class SpinyToadMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SpinyToadBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SpinyToad), "get_MaxInitialHp")]
public static class SpinyToadMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SpinyToadBlightHpNumbers.GetHpAdd();
    }
}