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
using Godot;

namespace BlightMod.AI.Bestiary;

public sealed class SlumberingBeetleBlightAI : IBlightMonsterAI
{
    private const int MutantRolloutDamage = 7;
    private const int MutantRolloutRepeat = 2;

    public string TargetMonsterId => "SlumberingBeetle";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SlumberingBeetle slumberingBeetle)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        var snoreMove = new MoveState("SNORE_MOVE", targets => InvokeOriginalMove(slumberingBeetle, "SnoreMove", targets), new SleepIntent());
        var rollOutMove = new MoveState("ROLL_OUT_MOVE", targets => RolloutMove(slumberingBeetle, targets), new MultiAttackIntent(MutantRolloutDamage, MutantRolloutRepeat), new BuffIntent());
        var snoreNext = new ConditionalBranchState("SNORE_NEXT");

        snoreMove.FollowUpState = snoreNext;
        snoreNext.AddState(snoreMove, () => slumberingBeetle.Creature.HasPower<SlumberPower>());
        snoreNext.AddState(rollOutMove, () => !slumberingBeetle.Creature.HasPower<SlumberPower>());
        rollOutMove.FollowUpState = rollOutMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { snoreMove, snoreNext, rollOutMove }, snoreMove);
    }

    private static async Task RolloutMove(SlumberingBeetle slumberingBeetle, IReadOnlyList<Creature> targets)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(slumberingBeetle.Creature);
        if (node != null)
        {
            NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(LocalContext.GetMe(slumberingBeetle.CombatState).Creature);
            Node2D? specialNode = node.GetSpecialNode<Node2D>("Visuals/SpineBoneNode");
            if (specialNode != null)
            {
                specialNode.Position = Vector2.Left * (node.GlobalPosition.X - creatureNode.GlobalPosition.X);
            }
        }

        await DamageCmd.Attack(MutantRolloutDamage)
            .WithHitCount(MutantRolloutRepeat)
            .FromMonster(slumberingBeetle)
            .WithAttackerAnim("Rollout", 0.5f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/slumbering_beetle/slumbering_beetle_roll")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<StrengthPower>(slumberingBeetle.Creature, 2m, slumberingBeetle.Creature, null);
    }

    private static Task InvokeOriginalMove(SlumberingBeetle slumberingBeetle, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(SlumberingBeetle), methodName)!.Invoke(slumberingBeetle, new object[] { targets })!;
    }
}

internal static class SlumberingBeetleBlightHpNumbers
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

[HarmonyPatch(typeof(SlumberingBeetle), "get_MinInitialHp")]
public static class SlumberingBeetleMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SlumberingBeetleBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SlumberingBeetle), "get_MaxInitialHp")]
public static class SlumberingBeetleMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SlumberingBeetleBlightHpNumbers.GetHpAdd();
    }
}