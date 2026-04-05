using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class VantomBlightAI : IBlightMonsterAI
{
    private const int InkyLanceRepeat = 2;
    private const int DismemberWounds = 3;

    public string TargetMonsterId => "Vantom";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Vantom vantom)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss only starts using blight AI rules at A5.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int inkBlotDamage = (int)AccessTools.Property(typeof(Vantom), "InkBlotDamage")!.GetValue(vantom)!;
        int inkyLanceDamage = (int)AccessTools.Property(typeof(Vantom), "InkyLanceDamage")!.GetValue(vantom)!;
        int dismemberDamage = (int)AccessTools.Property(typeof(Vantom), "DismemberDamage")!.GetValue(vantom)!;

        var inkBlotMove = new MoveState(
            "INK_BLOT_MOVE",
            targets => InvokeOriginalMove(vantom, "InkBlotMove", targets),
            new SingleAttackIntent(inkBlotDamage));

        var inkyLanceMove = new MoveState(
            "INKY_LANCE_MOVE",
            targets => InvokeOriginalMove(vantom, "InkyLanceMove", targets),
            new MultiAttackIntent(inkyLanceDamage, InkyLanceRepeat));

        var dismemberMove = new MoveState(
            "DISMEMBER_MOVE",
            targets => DismemberMove(vantom, dismemberDamage, targets),
            new SingleAttackIntent(dismemberDamage),
            new StatusIntent(DismemberWounds));

        var prepareMove = new MoveState(
            "PREPARE_MOVE",
            targets => InvokeOriginalMove(vantom, "PrepareMove", targets),
            new BuffIntent());

        inkBlotMove.FollowUpState = inkyLanceMove;
        inkyLanceMove.FollowUpState = dismemberMove;
        dismemberMove.FollowUpState = prepareMove;
        prepareMove.FollowUpState = inkBlotMove;

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState> { inkBlotMove, inkyLanceMove, dismemberMove, prepareMove },
            inkBlotMove);
    }

    private static async System.Threading.Tasks.Task DismemberMove(Vantom vantom, int dismemberDamage, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(dismemberDamage)
            .FromMonster(vantom)
            .Execute(null);

        await CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Draw, DismemberWounds, addedByPlayer: false);
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(Vantom vantom, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(Vantom), methodName)!.Invoke(vantom, new object[] { targets })!;
    }
}


internal static class VantomBlightHpNumbers
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

[HarmonyPatch(typeof(Vantom), "get_MinInitialHp")]
public static class VantomMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += VantomBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Vantom), "get_MaxInitialHp")]
public static class VantomMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += VantomBlightHpNumbers.GetHpAdd();
    }
}