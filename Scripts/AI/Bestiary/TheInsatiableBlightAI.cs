using HarmonyLib;
using BlightMod.Cards;
using BlightMod.Core;
using BlightMod.AI.Buffs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class TheInsatiableBlightAI : IBlightMonsterAI
{
    private const int ThrashRepeat = 2;
    private const int LungingBiteDamage = 15;
    private const int LungingBiteRepeat = 2;
    private const int LiquifyStatusCount = 6;
    private const int CollapseSandSpearCount = 2;
    private const decimal SandShieldAmount = 2m;
    private const int SandShieldSlamDamage = 60;
    internal const string SandShieldMoveId = "SAND_SHIELD_MOVE";

    public string TargetMonsterId => "TheInsatiable";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not TheInsatiable theInsatiable)
        {
            return;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        decimal threshold = theInsatiable.Creature.MaxHp / 2m;
        decimal countdown = theInsatiable.Creature.CurrentHp - threshold;
        _ = PowerCmd.Apply<TheInsatiableShriekPower>(theInsatiable.Creature, countdown, theInsatiable.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not TheInsatiable theInsatiable)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI changes only apply at A5+.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int thrashDamage = (int)AccessTools.Property(typeof(TheInsatiable), "ThrashDamage")!.GetValue(theInsatiable)!;
        var liquifyMove = new MoveState("LIQUIFY_GROUND_MOVE", targets => InvokeOriginalMove(theInsatiable, "LiquifyMove", targets), new BuffIntent(), new StatusIntent(LiquifyStatusCount));
        var thrashMove1 = new MoveState("THRASH_MOVE_1", targets => InvokeOriginalMove(theInsatiable, "ThrashMove", targets), new MultiAttackIntent(thrashDamage, ThrashRepeat));
        var thrashMove2 = new MoveState("THRASH_MOVE_2", targets => InvokeOriginalMove(theInsatiable, "ThrashMove", targets), new MultiAttackIntent(thrashDamage, ThrashRepeat));
        var lungingBiteMove = new MoveState("LUNGING_BITE_MOVE", targets => BiteMove(theInsatiable, targets), new MultiAttackIntent(LungingBiteDamage, LungingBiteRepeat));
        var salivateMove = new MoveState("SALIVATE_MOVE", targets => InvokeOriginalMove(theInsatiable, "SalivateMove", targets), new BuffIntent());
        var sandShieldMove = new MoveState("SAND_SHIELD_MOVE", targets => SandShieldMove(theInsatiable, targets), new BuffIntent());
        var sandShieldSlamMove = new MoveState("SAND_SHIELD_SLAM_MOVE", targets => SandShieldSlamMove(theInsatiable, targets), new SingleAttackIntent(SandShieldSlamDamage));

        liquifyMove.FollowUpState = thrashMove1;
        thrashMove1.FollowUpState = lungingBiteMove;
        lungingBiteMove.FollowUpState = salivateMove;
        salivateMove.FollowUpState = thrashMove2;
        thrashMove2.FollowUpState = thrashMove1;
        sandShieldMove.FollowUpState = sandShieldSlamMove;
        sandShieldSlamMove.FollowUpState = sandShieldMove;

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState>
            {
                liquifyMove,
                lungingBiteMove,
                thrashMove1,
                thrashMove2,
                salivateMove,
                sandShieldMove,
                sandShieldSlamMove
            },
            liquifyMove);
    }

    private static async System.Threading.Tasks.Task SandShieldMove(TheInsatiable theInsatiable, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        decimal stacks = SandShieldAmount + System.Math.Max(0, theInsatiable.CombatState.Players.Count - 1);
        await PowerCmd.Apply<SandShieldPower>(theInsatiable.Creature, stacks, theInsatiable.Creature, null);
    }

    private static async System.Threading.Tasks.Task SandShieldSlamMove(TheInsatiable theInsatiable, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        await DamageCmd.Attack(SandShieldSlamDamage)
            .FromMonster(theInsatiable)
            .WithAttackerAnim("Bite", 0.25f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_bite")
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/the_insatiable/the_insatiable_lunging_bite")
            .Execute(null);

        await PowerCmd.Remove<SandShieldPower>(theInsatiable.Creature);
    }

    internal static async System.Threading.Tasks.Task AddSandSpearsToOpponents(TheInsatiable theInsatiable)
    {
        var targets = theInsatiable.CombatState.GetOpponentsOf(theInsatiable.Creature);
        foreach (Creature target in targets)
        {
            var player = target.Player ?? target.PetOwner;
            if (player == null)
            {
                continue;
            }

            var statusCards = new System.Collections.Generic.List<CardPileAddResult>(CollapseSandSpearCount);
            for (int i = 0; i < CollapseSandSpearCount; i++)
            {
                CardModel card = theInsatiable.CombatState.CreateCard<BlightSandSpear>(player);
                statusCards.Add(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: false, CardPilePosition.Random));
            }

            if (MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd(statusCards);
                await Cmd.Wait(0.8f);
            }
        }
    }

    private static async System.Threading.Tasks.Task BiteMove(TheInsatiable theInsatiable, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        await DamageCmd.Attack(LungingBiteDamage)
            .WithHitCount(LungingBiteRepeat)
            .FromMonster(theInsatiable)
            .WithAttackerAnim("Bite", 0.25f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_bite")
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/the_insatiable/the_insatiable_lunging_bite")
            .Execute(null);
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(TheInsatiable theInsatiable, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(TheInsatiable), methodName)!.Invoke(theInsatiable, new object[] { targets })!;
    }
}

internal static class TheInsatiableBlightHpNumbers
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

[HarmonyPatch(typeof(TheInsatiable), "get_MinInitialHp")]
public static class TheInsatiableMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TheInsatiableBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(TheInsatiable), "get_MaxInitialHp")]
public static class TheInsatiableMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}
