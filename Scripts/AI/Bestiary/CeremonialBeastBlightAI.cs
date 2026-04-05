using HarmonyLib;
using BlightMod.Core;
using BlightMod.AI.Buffs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class CeremonialBeastBlightAI : IBlightMonsterAI
{
    private const decimal ExtraConstrictPerMove = 1m;

    public string TargetMonsterId => "CeremonialBeast";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not CeremonialBeast ceremonialBeast)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI changes should only be active at A5+.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int plowDamage = (int)AccessTools.Property(typeof(CeremonialBeast), "PlowDamage")!.GetValue(ceremonialBeast)!;
        int stompDamage = (int)AccessTools.Property(typeof(CeremonialBeast), "StompDamage")!.GetValue(ceremonialBeast)!;
        int crushDamage = (int)AccessTools.Property(typeof(CeremonialBeast), "CrushDamage")!.GetValue(ceremonialBeast)!;

        var stampMove = new MoveState(
            "STAMP_MOVE",
            targets => ExecuteMoveWithConstrict(ceremonialBeast, "StampMove", targets),
            new BuffIntent());

        var plowMove = new MoveState(
            "PLOW_MOVE",
            targets => ExecuteMoveWithConstrict(ceremonialBeast, "PlowMove", targets),
            new SingleAttackIntent(plowDamage),
            new BuffIntent());

        var stunMove = new MoveState(
            "STUN_MOVE",
            targets => ExecuteMoveWithConstrict(ceremonialBeast, "StunnedMove", targets),
            new StunIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };

        var beastCryMove = new MoveState(
            "BEAST_CRY_MOVE",
            targets => ExecuteMoveWithConstrict(ceremonialBeast, "BeastCryMove", targets),
            new DebuffIntent());

        var stompMove = new MoveState(
            "STOMP_MOVE",
            targets => ExecuteMoveWithConstrict(ceremonialBeast, "StompMove", targets),
            new SingleAttackIntent(stompDamage));

        var crushMove = new MoveState(
            "CRUSH_MOVE",
            targets => ExecuteMoveWithConstrict(ceremonialBeast, "CrushMove", targets),
            new SingleAttackIntent(crushDamage),
            new BuffIntent());

        // Keep the original stun flow relying on BeastCryState for PlowPower interactions.
        ceremonialBeast.BeastCryState = beastCryMove;

        stampMove.FollowUpState = plowMove;
        plowMove.FollowUpState = plowMove;
        stunMove.FollowUpState = beastCryMove;
        beastCryMove.FollowUpState = stompMove;
        stompMove.FollowUpState = crushMove;
        crushMove.FollowUpState = beastCryMove;

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState> { plowMove, stampMove, stunMove, beastCryMove, stompMove, crushMove },
            stampMove);
    }

    private static async System.Threading.Tasks.Task ExecuteMoveWithConstrict(CeremonialBeast ceremonialBeast, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(ceremonialBeast, methodName, targets);
        await ApplyConstrictToPlayerTargets(ceremonialBeast, targets);
    }

    private static async System.Threading.Tasks.Task ApplyConstrictToPlayerTargets(CeremonialBeast ceremonialBeast, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        foreach (Creature target in targets)
        {
            if (!target.IsAlive || !target.IsPlayer)
            {
                continue;
            }

            await PowerCmd.Apply<CeremonialBeastConstrictPower>(target, ExtraConstrictPerMove, ceremonialBeast.Creature, null);
        }
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(CeremonialBeast ceremonialBeast, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(CeremonialBeast), methodName)!.Invoke(ceremonialBeast, new object[] { targets })!;
    }
}

internal static class CeremonialBeastBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
    public const int MutantHpAdd = 0;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(
            BlightModeManager.BlightAscensionLevel,
            mutant,
            A0HpAdd,
            A1To2HpAdd,
            A3To4HpAdd,
            A5PlusHpAdd,
            MutantHpAdd);
    }
}

[HarmonyPatch(typeof(CeremonialBeast), "get_MinInitialHp")]
public static class CeremonialBeastMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += CeremonialBeastBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(CeremonialBeast), "get_MaxInitialHp")]
public static class CeremonialBeastMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}