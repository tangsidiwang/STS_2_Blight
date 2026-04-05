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

public sealed class TheForgottenBlightAI : IBlightMonsterAI
{
    private const int MutantDreadBaseDamage = 13;

    public string TargetMonsterId => "TheForgotten";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not TheForgotten forgotten)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        var miasmaMove = new MoveState(
            "MIASMA",
            targets => InvokeOriginalMove(forgotten, "MiasmaMove", targets),
            new DebuffIntent(),
            new DefendIntent(),
            new BuffIntent());
        var dreadMove = new MoveState(
            "DREAD",
            targets => DreadMove(forgotten, targets),
            new SingleAttackIntent(() => GetMutantDreadDamage(forgotten)));

        miasmaMove.FollowUpState = dreadMove;
        dreadMove.FollowUpState = miasmaMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { miasmaMove, dreadMove }, miasmaMove);
    }

    private static async Task DreadMove(TheForgotten forgotten, IReadOnlyList<Creature> targets)
    {
        string attackSfx = (string)AccessTools.Property(typeof(MonsterModel), "AttackSfx")!.GetValue(forgotten)!;
        await DamageCmd.Attack(GetMutantDreadDamage(forgotten)).FromMonster(forgotten).WithAttackerAnim("Attack", 0.15f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, attackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private static int GetMutantDreadDamage(TheForgotten forgotten)
    {
        int dexterity = forgotten.Creature?.GetPower<DexterityPower>()?.Amount ?? 0;
        return MutantDreadBaseDamage + dexterity * 2;
    }

    private static Task InvokeOriginalMove(TheForgotten forgotten, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(TheForgotten), methodName)!.Invoke(forgotten, new object[] { targets })!;
    }
}

internal static class TheForgottenBlightHpNumbers
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

[HarmonyPatch(typeof(TheForgotten), "get_MinInitialHp")]
public static class TheForgottenMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TheForgottenBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(TheForgotten), "get_MaxInitialHp")]
public static class TheForgottenMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}