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

public sealed class TrackerRubyRaiderBlightAI : IBlightMonsterAI
{
    private const decimal MutantTrackExtraWeak = 1m;

    public string TargetMonsterId => "TrackerRubyRaider";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not TrackerRubyRaider trackerRubyRaider)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int houndsDamage = (int)AccessTools.Property(typeof(TrackerRubyRaider), "HoundsDamage")!.GetValue(trackerRubyRaider)!;
        int houndsRepeat = (int)AccessTools.Property(typeof(TrackerRubyRaider), "HoundsRepeat")!.GetValue(trackerRubyRaider)!;

        var trackMove = new MoveState("TRACK_MOVE", targets => TrackMove(trackerRubyRaider, targets), new DebuffIntent());
        var houndsMove = new MoveState("HOUNDS_MOVE", targets => InvokeOriginalMove(trackerRubyRaider, "HoundsMove", targets), new MultiAttackIntent(houndsDamage, houndsRepeat));

        trackMove.FollowUpState = houndsMove;
        houndsMove.FollowUpState = houndsMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { trackMove, houndsMove }, trackMove);
    }

    private static async Task TrackMove(TrackerRubyRaider trackerRubyRaider, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(trackerRubyRaider, "TrackMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        await PowerCmd.Apply<WeakPower>(targets, MutantTrackExtraWeak, trackerRubyRaider.Creature, null);
    }

    private static Task InvokeOriginalMove(TrackerRubyRaider trackerRubyRaider, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(TrackerRubyRaider), methodName)!.Invoke(trackerRubyRaider, new object[] { targets })!;
    }
}

internal static class TrackerRubyRaiderBlightHpNumbers
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

[HarmonyPatch(typeof(TrackerRubyRaider), "get_MinInitialHp")]
public static class TrackerRubyRaiderMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TrackerRubyRaiderBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(TrackerRubyRaider), "get_MaxInitialHp")]
public static class TrackerRubyRaiderMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TrackerRubyRaiderBlightHpNumbers.GetHpAdd();
    }
}