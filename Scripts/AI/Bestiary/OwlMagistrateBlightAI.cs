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

public sealed class OwlMagistrateBlightAI : IBlightMonsterAI
{
    private const decimal ThornsDelta = 10m;

    public string TargetMonsterId => "OwlMagistrate";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not OwlMagistrate owlMagistrate)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int scrutinyDamage = (int)AccessTools.Property(typeof(OwlMagistrate), "ScrutinyDamage")!.GetValue(owlMagistrate)!;
        int peckAssaultDamage = (int)AccessTools.Property(typeof(OwlMagistrate), "PeckAssaultDamage")!.GetValue(owlMagistrate)!;
        int verdictDamage = (int)AccessTools.Property(typeof(OwlMagistrate), "VerdictDamage")!.GetValue(owlMagistrate)!;

        var magistrateScrutiny = new MoveState(
            "MAGISTRATE_SCRUTINY",
            targets => InvokeOriginalMove(owlMagistrate, "MagistrateScrutinyMove", targets),
            new SingleAttackIntent(scrutinyDamage));
        var peckAssault = new MoveState(
            "PECK_ASSAULT",
            targets => InvokeOriginalMove(owlMagistrate, "PeckAssaultMove", targets),
            new MultiAttackIntent(peckAssaultDamage, 6));
        var judicialFlight = new MoveState(
            "JUDICIAL_FLIGHT",
            targets => JudicialFlightMove(owlMagistrate, targets),
            new BuffIntent());
        var verdict = new MoveState(
            "VERDICT",
            targets => VerdictMove(owlMagistrate, targets),
            new SingleAttackIntent(verdictDamage),
            new DebuffIntent());

        magistrateScrutiny.FollowUpState = peckAssault;
        peckAssault.FollowUpState = judicialFlight;
        judicialFlight.FollowUpState = verdict;
        verdict.FollowUpState = magistrateScrutiny;

        return new MonsterMoveStateMachine(new List<MonsterState> { magistrateScrutiny, peckAssault, judicialFlight, verdict }, magistrateScrutiny);
    }

    private static async Task JudicialFlightMove(OwlMagistrate owlMagistrate, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(owlMagistrate, "JudicialFlightMove", targets);
        await PowerCmd.Apply<ThornsPower>(owlMagistrate.Creature, ThornsDelta, owlMagistrate.Creature, null);
    }

    private static async Task VerdictMove(OwlMagistrate owlMagistrate, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(owlMagistrate, "VerdictMove", targets);
        await PowerCmd.Apply<ThornsPower>(owlMagistrate.Creature, -ThornsDelta, owlMagistrate.Creature, null);
    }

    private static Task InvokeOriginalMove(OwlMagistrate owlMagistrate, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(OwlMagistrate), methodName)!.Invoke(owlMagistrate, new object[] { targets })!;
    }
}

internal static class OwlMagistrateBlightHpNumbers
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

[HarmonyPatch(typeof(OwlMagistrate), "get_MinInitialHp")]
public static class OwlMagistrateMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += OwlMagistrateBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(OwlMagistrate), "get_MaxInitialHp")]
public static class OwlMagistrateMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += OwlMagistrateBlightHpNumbers.GetHpAdd();
    }
}