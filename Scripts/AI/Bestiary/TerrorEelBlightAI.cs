using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class TerrorEelBlightAI : IBlightMonsterAI
{
    private const decimal ThrashBonusStrength = 1m;
    private const decimal MutantExtraWeak = 99m;

    public string TargetMonsterId => "TerrorEel";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not TerrorEel terrorEel)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int crashDamage = (int)AccessTools.Property(typeof(TerrorEel), "CrashDamage")!.GetValue(terrorEel)!;
        int thrashDamage = (int)AccessTools.Property(typeof(TerrorEel), "ThrashDamage")!.GetValue(terrorEel)!;
        int thrashRepeat = (int)AccessTools.Property(typeof(TerrorEel), "ThrashRepeat")!.GetValue(terrorEel)!;

        var states = new List<MonsterState>();
        var crashMove = new MoveState("CRASH_MOVE", targets => InvokeOriginalMove(terrorEel, "CrashMove", targets), new SingleAttackIntent(crashDamage));
        var thrashMove = new MoveState("ThrashMove", targets => ThrashMove(terrorEel, targets), new MultiAttackIntent(thrashDamage, thrashRepeat), new BuffIntent());
        var stunMove = new MoveState("STUN_MOVE", targets => InvokeOriginalMove(terrorEel, "StunMove", targets), new StunIntent());
        var terrorMove = new MoveState("TERROR_MOVE", targets => TerrorMove(terrorEel, targets), new DebuffIntent());

        crashMove.FollowUpState = thrashMove;
        thrashMove.FollowUpState = crashMove;
        stunMove.FollowUpState = terrorMove;
        terrorMove.FollowUpState = crashMove;

        states.Add(crashMove);
        states.Add(thrashMove);
        states.Add(stunMove);
        states.Add(terrorMove);

        return new MonsterMoveStateMachine(states, crashMove);
    }

    private static async Task ThrashMove(TerrorEel terrorEel, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(terrorEel, "ThrashMove", targets);
        await PowerCmd.Apply<StrengthPower>(terrorEel.Creature, ThrashBonusStrength, terrorEel.Creature, null);
    }

    private static async Task TerrorMove(TerrorEel terrorEel, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(terrorEel, "TerrorMove", targets);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/terror_eel/terror_eel_debuff");
        await PowerCmd.Apply<WeakPower>(targets, MutantExtraWeak, terrorEel.Creature, null);
    }

    private static Task InvokeOriginalMove(TerrorEel terrorEel, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(TerrorEel), methodName)!.Invoke(terrorEel, new object[] { targets })!;
    }
}

internal static class TerrorEelBlightHpNumbers
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

[HarmonyPatch(typeof(TerrorEel), "get_MinInitialHp")]
public static class TerrorEelMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TerrorEelBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(TerrorEel), "get_MaxInitialHp")]
public static class TerrorEelMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TerrorEelBlightHpNumbers.GetHpAdd();
    }
}