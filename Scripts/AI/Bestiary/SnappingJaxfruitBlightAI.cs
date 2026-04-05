using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace BlightMod.AI.Bestiary;

public sealed class SnappingJaxfruitBlightAI : IBlightMonsterAI
{
    private const int MutantEnergyOrbDamage = 2;
    private const int MutantEnergyOrbRepeat = 2;

    public string TargetMonsterId => "SnappingJaxfruit";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SnappingJaxfruit snappingJaxfruit)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        var energyOrbMove = new MoveState(
            "ENERGY_ORB_MOVE",
            targets => MutantEnergyOrb(snappingJaxfruit, targets),
            new MultiAttackIntent(MutantEnergyOrbDamage, MutantEnergyOrbRepeat),
            new BuffIntent());

        energyOrbMove.FollowUpState = energyOrbMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { energyOrbMove }, energyOrbMove);
    }

    private static async Task MutantEnergyOrb(SnappingJaxfruit snappingJaxfruit, IReadOnlyList<Creature> targets)
    {
        if (TestMode.IsOff)
        {
            NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(snappingJaxfruit.Creature);
            Creature target = LocalContext.GetMe(snappingJaxfruit.CombatState)?.Creature;
            creatureNode.GetSpecialNode<NSnappingJaxfruitVfx>("Visuals/NSnappingJaxfruitVfx")?.SetTarget(target);
        }

        AccessTools.Property(typeof(SnappingJaxfruit), "IsCharged")!.SetValue(snappingJaxfruit, true);
        await DamageCmd.Attack(MutantEnergyOrbDamage)
            .WithHitCount(MutantEnergyOrbRepeat)
            .FromMonster(snappingJaxfruit)
            .WithAttackerAnim("Cast", 0.25f)
            .OnlyPlayAnimOnce()
            .Execute(null);
        AccessTools.Property(typeof(SnappingJaxfruit), "IsCharged")!.SetValue(snappingJaxfruit, false);
        await PowerCmd.Apply<StrengthPower>(snappingJaxfruit.Creature, 2m, snappingJaxfruit.Creature, null);
    }
}

internal static class SnappingJaxfruitBlightHpNumbers
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

[HarmonyPatch(typeof(SnappingJaxfruit), "get_MinInitialHp")]
public static class SnappingJaxfruitMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SnappingJaxfruitBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SnappingJaxfruit), "get_MaxInitialHp")]
public static class SnappingJaxfruitMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SnappingJaxfruitBlightHpNumbers.GetHpAdd();
    }
}