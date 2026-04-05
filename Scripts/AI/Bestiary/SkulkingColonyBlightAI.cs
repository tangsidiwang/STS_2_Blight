using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

internal static class SkulkingColonyBlightNumbers
{
    public const int OriginalHardenedShell = 20;
    public const int EliteA2PlusHardenedShell = 25;
    public const int EliteA2PlusHardToKill = 8;
    public const int MutantHardToKill = 6;
    public const string InertiaMoveName = "INERTIA_MOVE";
    public const string ZoomMoveName = "ZOOM_MOVE";
    public const string SuperCrabMoveName = "SUPER_CRAB_MOVE";
    public const string SmashMoveName = "SMASH_MOVE";
}

public sealed class SkulkingColonyBlightAI : IBlightMonsterAI
{
    private static readonly FieldInfo? MoveStateMachineField = AccessTools.Field(typeof(MonsterModel), "_moveStateMachine");

    public string TargetMonsterId => "SkulkingColony";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SkulkingColony skulkingColony || !BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        if (BlightAIContext.IsCurrentNodeMutant())
        {
            _ = PowerCmd.Apply<HardToKillPower>(skulkingColony.Creature, SkulkingColonyBlightNumbers.MutantHardToKill, skulkingColony.Creature, null);
            return;
        }

        int extraShell = SkulkingColonyBlightNumbers.EliteA2PlusHardenedShell - SkulkingColonyBlightNumbers.OriginalHardenedShell;
        if (extraShell > 0)
        {
            _ = PowerCmd.Apply<HardenedShellPower>(skulkingColony.Creature, extraShell, skulkingColony.Creature, null);
        }

        _ = PowerCmd.Apply<HardToKillPower>(skulkingColony.Creature, SkulkingColonyBlightNumbers.EliteA2PlusHardToKill, skulkingColony.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SkulkingColony skulkingColony)
        {
            return (MonsterMoveStateMachine)MoveStateMachineField!.GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)MoveStateMachineField!.GetValue(monster)!;
        }

        var inertiaMove = new MoveState(
            SkulkingColonyBlightNumbers.InertiaMoveName,
            targets => InvokeOriginalMoveAsync(skulkingColony, "InertiaMove", targets),
            new DefendIntent(),
            new BuffIntent());

        var zoomMove = new MoveState(
            SkulkingColonyBlightNumbers.ZoomMoveName,
            targets => InvokeOriginalMoveAsync(skulkingColony, "ZoomMove", targets),
            new SingleAttackIntent(GetPrivateIntProperty(skulkingColony, "ZoomDamage")));

        var superCrabMove = new MoveState(
            SkulkingColonyBlightNumbers.SuperCrabMoveName,
            targets => InvokeOriginalMoveAsync(skulkingColony, "SuperCrabMove", targets),
            new MultiAttackIntent(
                GetPrivateIntProperty(skulkingColony, "SuperCrabDamage"),
                GetPrivateIntProperty(skulkingColony, "SuperCrabRepeat")));

        var smashMove = new MoveState(
            SkulkingColonyBlightNumbers.SmashMoveName,
            targets => InvokeOriginalMoveAsync(skulkingColony, "SmashMove", targets),
            new SingleAttackIntent(GetPrivateIntProperty(skulkingColony, "SmashDamage")),
            new StatusIntent(4));

        smashMove.FollowUpState = zoomMove;
        zoomMove.FollowUpState = inertiaMove;
        inertiaMove.FollowUpState = superCrabMove;
        superCrabMove.FollowUpState = smashMove;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { inertiaMove, zoomMove, superCrabMove, smashMove },
            smashMove);
    }

    private static int GetPrivateIntProperty(object target, string propertyName)
    {
        return (int)(AccessTools.Property(target.GetType(), propertyName)?.GetValue(target)
            ?? throw new MissingMemberException(target.GetType().FullName, propertyName));
    }

    private static Task InvokeOriginalMoveAsync(SkulkingColony skulkingColony, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)(AccessTools.Method(typeof(SkulkingColony), methodName)?.Invoke(skulkingColony, new object[] { targets })
            ?? throw new MissingMethodException(typeof(SkulkingColony).FullName, methodName));
    }
}