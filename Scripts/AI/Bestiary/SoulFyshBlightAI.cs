using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class SoulFyshBlightAI : IBlightMonsterAI
{
    private const int BeckonMoveAmount = 2;
    private const int GazeMoveAmount = 1;
    private const int ScreamMoveAmount = 3;

    public string TargetMonsterId => "SoulFysh";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SoulFysh soulFysh)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI changes only start at A5+.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int deGasDamage = (int)AccessTools.Property(typeof(SoulFysh), "DeGasDamage")!.GetValue(soulFysh)!;
        int screamDamage = (int)AccessTools.Property(typeof(SoulFysh), "ScreamDamage")!.GetValue(soulFysh)!;
        int gazeDamage = (int)AccessTools.Property(typeof(SoulFysh), "GazeDamage")!.GetValue(soulFysh)!;

        var beckonMove = new MoveState("BECKON_MOVE", targets => BeckonMove(soulFysh, targets), new StatusIntent(BeckonMoveAmount));
        var deGasMove = new MoveState("DE_GAS_MOVE", targets => InvokeOriginalMove(soulFysh, "DeGasMove", targets), new SingleAttackIntent(deGasDamage));
        var gazeMove = new MoveState("GAZE_MOVE", targets => InvokeOriginalMove(soulFysh, "GazeMove", targets), new SingleAttackIntent(gazeDamage), new StatusIntent(GazeMoveAmount));
        var fadeMove = new MoveState("FADE_MOVE", targets => InvokeOriginalMove(soulFysh, "FadeMove", targets), new BuffIntent());
        var screamMove = new MoveState("SCREAM_MOVE", targets => InvokeOriginalMove(soulFysh, "ScreamMove", targets), new SingleAttackIntent(screamDamage), new DebuffIntent());

        beckonMove.FollowUpState = deGasMove;
        deGasMove.FollowUpState = gazeMove;
        gazeMove.FollowUpState = fadeMove;
        fadeMove.FollowUpState = screamMove;
        screamMove.FollowUpState = beckonMove;

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState> { beckonMove, deGasMove, gazeMove, screamMove, fadeMove },
            beckonMove);
    }

    private static async System.Threading.Tasks.Task BeckonMove(SoulFysh soulFysh, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/soul_fysh/soul_fysh_beckon");
        await CreatureCmd.TriggerAnim(soulFysh.Creature, "AttackBeckon", 0f);
        await Cmd.Wait(0.3f);
        VfxCmd.PlayOnCreatureCenter(soulFysh.Creature, "vfx/vfx_spooky_scream");
        await Cmd.CustomScaledWait(0f, 0.3f);

        foreach (Creature target in targets)
        {
            Player player = target.Player ?? target.PetOwner;
            CardPileAddResult[] statusCards = new CardPileAddResult[BeckonMoveAmount];

            CardModel card = soulFysh.CombatState.CreateCard<Beckon>(player);
            statusCards[0] = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: false, CardPilePosition.Random);

            CardModel card2 = soulFysh.CombatState.CreateCard<Beckon>(player);
            statusCards[1] = await CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Draw, addedByPlayer: false, CardPilePosition.Random);

            if (LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd(statusCards);
                await Cmd.Wait(1f);
            }
        }
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(SoulFysh soulFysh, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(SoulFysh), methodName)!.Invoke(soulFysh, new object[] { targets })!;
    }
}

internal static class SoulFyshBlightHpNumbers
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

[HarmonyPatch(typeof(SoulFysh), "get_MinInitialHp")]
public static class SoulFyshMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SoulFyshBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SoulFysh), "get_MaxInitialHp")]
public static class SoulFyshMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SoulFyshBlightHpNumbers.GetHpAdd();
    }
}