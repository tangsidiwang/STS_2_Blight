using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class KnowledgeDemonBlightAI : IBlightMonsterAI
{
    private const int KnowledgeOverwhelmingRepeat = 3;
    private const int SlapDamage = 6;
    private const int SlapRepeat = 3;

    public string TargetMonsterId => "KnowledgeDemon";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not KnowledgeDemon knowledgeDemon)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI changes only apply at A5+.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int ponderDamage = (int)AccessTools.Property(typeof(KnowledgeDemon), "PonderDamage")!.GetValue(knowledgeDemon)!;
        int knowledgeOverwhelmingDamage = (int)AccessTools.Property(typeof(KnowledgeDemon), "KnowledgeOverwhelmingDamage")!.GetValue(knowledgeDemon)!;

        var curseOfKnowledgeMove = new MoveState("CURSE_OF_KNOWLEDGE_MOVE", targets => InvokeOriginalMove(knowledgeDemon, "CurseOfKnowledge", targets), new DebuffIntent());
        var slapMove = new MoveState("SLAP_MOVE", targets => SlapMove(knowledgeDemon, targets), new MultiAttackIntent(SlapDamage, SlapRepeat));
        var knowledgeOverwhelmingMove = new MoveState("KNOWLEDGE_OVERWHELMING_MOVE", targets => InvokeOriginalMove(knowledgeDemon, "KnowledgeOverwhelmingMove", targets), new MultiAttackIntent(knowledgeOverwhelmingDamage, KnowledgeOverwhelmingRepeat));
        var ponderMove = new MoveState("PONDER_MOVE", targets => InvokeOriginalMove(knowledgeDemon, "PonderMove", targets), new SingleAttackIntent(ponderDamage), new HealIntent(), new BuffIntent());
        var curseOfKnowledgeBranch = new ConditionalBranchState("CurseOfKnowledgeBranch");

        curseOfKnowledgeMove.FollowUpState = slapMove;
        slapMove.FollowUpState = knowledgeOverwhelmingMove;
        knowledgeOverwhelmingMove.FollowUpState = ponderMove;
        ponderMove.FollowUpState = curseOfKnowledgeBranch;
        curseOfKnowledgeBranch.AddState(curseOfKnowledgeMove, () =>
            (int)AccessTools.Field(typeof(KnowledgeDemon), "_curseOfKnowledgeCounter")!.GetValue(knowledgeDemon)! < 3);
        curseOfKnowledgeBranch.AddState(slapMove, () =>
            (int)AccessTools.Field(typeof(KnowledgeDemon), "_curseOfKnowledgeCounter")!.GetValue(knowledgeDemon)! >= 3);

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState>
            {
                curseOfKnowledgeBranch,
                curseOfKnowledgeMove,
                slapMove,
                ponderMove,
                knowledgeOverwhelmingMove
            },
            curseOfKnowledgeMove);
    }

    private static async System.Threading.Tasks.Task SlapMove(KnowledgeDemon knowledgeDemon, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        await DamageCmd.Attack(SlapDamage)
            .WithHitCount(SlapRepeat)
            .FromMonster(knowledgeDemon)
            .WithAttackerAnim("MediumAttackTrigger", 0.5f)
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_slap")
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .OnlyPlayAnimOnce()
            .Execute(null);
    }

    private static System.Threading.Tasks.Task InvokeOriginalMove(KnowledgeDemon knowledgeDemon, string methodName, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        return (System.Threading.Tasks.Task)AccessTools.Method(typeof(KnowledgeDemon), methodName)!.Invoke(knowledgeDemon, new object[] { targets })!;
    }
}

internal static class KnowledgeDemonBlightHpNumbers
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

[HarmonyPatch(typeof(KnowledgeDemon), "get_MinInitialHp")]
public static class KnowledgeDemonMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += KnowledgeDemonBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(KnowledgeDemon), "get_MaxInitialHp")]
public static class KnowledgeDemonMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}