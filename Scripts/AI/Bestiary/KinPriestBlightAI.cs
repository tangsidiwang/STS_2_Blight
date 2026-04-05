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

public sealed class KinPriestBlightAI : IBlightMonsterAI
{
    private const int BeamRepeat = 3;
    private const decimal RitualRegenAmount = 3m;

    public string TargetMonsterId => "KinPriest";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not KinPriest kinPriest)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI modifications should only apply on A5+.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int orbOfFrailtyDamage = (int)AccessTools.Property(typeof(KinPriest), "OrbOfFrailtyDamage")!.GetValue(kinPriest)!;
        int orbOfWeaknessDamage = (int)AccessTools.Property(typeof(KinPriest), "OrbOfWeaknessDamage")!.GetValue(kinPriest)!;
        int beamDamage = (int)AccessTools.Property(typeof(KinPriest), "BeamDamage")!.GetValue(kinPriest)!;

        var orbOfFrailtyMove = new MoveState(
            "ORB_OF_FRAILTY_MOVE",
            targets => InvokeOriginalMove(kinPriest, "OrbOfFrailtyMove", targets),
            new SingleAttackIntent(orbOfFrailtyDamage),
            new DebuffIntent());

        var orbOfWeaknessMove = new MoveState(
            "ORB_OF_WEAKNESS_MOVE",
            targets => InvokeOriginalMove(kinPriest, "OrbOfWeaknessMove", targets),
            new SingleAttackIntent(orbOfWeaknessDamage),
            new DebuffIntent());

        var beamMove = new MoveState(
            "BEAM_MOVE",
            targets => InvokeOriginalMove(kinPriest, "BeamMove", targets),
            new MultiAttackIntent(beamDamage, BeamRepeat));

        var ritualMove = new MoveState(
            "RITUAL_MOVE",
            targets => RitualMove(kinPriest, targets),
            new BuffIntent());

        orbOfFrailtyMove.FollowUpState = orbOfWeaknessMove;
        orbOfWeaknessMove.FollowUpState = beamMove;
        beamMove.FollowUpState = ritualMove;
        ritualMove.FollowUpState = orbOfFrailtyMove;

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState> { orbOfFrailtyMove, orbOfWeaknessMove, beamMove, ritualMove },
            orbOfFrailtyMove);
    }

    private static async System.Threading.Tasks.Task RitualMove(KinPriest kinPriest, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(kinPriest, "RitualMove", targets);

        var enemySideCreatures = kinPriest.CombatState?.GetTeammatesOf(kinPriest.Creature);
        if (enemySideCreatures == null)
        {
            return;
        }

        foreach (Creature enemyCreature in enemySideCreatures)
        {
            if (!enemyCreature.IsAlive)
            {
                continue;
            }

            await PowerCmd.Apply<RegenPower>(enemyCreature, RitualRegenAmount, kinPriest.Creature, null);
        }
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(KinPriest kinPriest, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(KinPriest), methodName)!.Invoke(kinPriest, new object[] { targets })!;
    }
}

internal static class KinPriestBlightHpNumbers
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

[HarmonyPatch(typeof(KinPriest), "get_MinInitialHp")]
public static class KinPriestMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += KinPriestBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(KinPriest), "get_MaxInitialHp")]
public static class KinPriestMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}