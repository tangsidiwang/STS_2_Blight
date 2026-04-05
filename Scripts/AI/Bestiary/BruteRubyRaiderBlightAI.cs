using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class BruteRubyRaiderBlightAI : IBlightMonsterAI
{
    private const int MutantBeatDamage = 5;
    private const int MutantBeatRepeat = 2;

    public string TargetMonsterId => "BruteRubyRaider";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not BruteRubyRaider bruteRubyRaider)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        var beatMove = new MoveState("BEAT_MOVE", targets => BeatMove(bruteRubyRaider, targets), new MultiAttackIntent(MutantBeatDamage, MutantBeatRepeat));
        var roarMove = new MoveState("ROAR_MOVE", targets => InvokeOriginalMove(bruteRubyRaider, "RoarMove", targets), new BuffIntent());

        beatMove.FollowUpState = roarMove;
        roarMove.FollowUpState = beatMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { beatMove, roarMove }, beatMove);
    }

    private static async Task BeatMove(BruteRubyRaider bruteRubyRaider, IReadOnlyList<Creature> targets)
    {
        string beatAttackSfx = (string)AccessTools.Property(typeof(MonsterModel), "AttackSfx")!.GetValue(bruteRubyRaider)!;

        await DamageCmd.Attack(MutantBeatDamage)
            .WithHitCount(MutantBeatRepeat)
            .FromMonster(bruteRubyRaider)
            .WithAttackerAnim("Attack", 0.6f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, beatAttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private static Task InvokeOriginalMove(BruteRubyRaider bruteRubyRaider, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(BruteRubyRaider), methodName)!.Invoke(bruteRubyRaider, new object[] { targets })!;
    }
}

internal static class BruteRubyRaiderBlightHpNumbers
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

[HarmonyPatch(typeof(BruteRubyRaider), "get_MinInitialHp")]
public static class BruteRubyRaiderMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += BruteRubyRaiderBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(BruteRubyRaider), "get_MaxInitialHp")]
public static class BruteRubyRaiderMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += BruteRubyRaiderBlightHpNumbers.GetHpAdd();
    }
}