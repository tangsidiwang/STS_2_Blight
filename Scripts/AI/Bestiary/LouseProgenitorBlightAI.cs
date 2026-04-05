using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class LouseProgenitorBlightAI : IBlightMonsterAI
{
    private const int MutantPounceDamage = 8;
    private const int MutantPounceRepeat = 2;

    public string TargetMonsterId => "LouseProgenitor";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not LouseProgenitor louseProgenitor)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int webDamage = (int)AccessTools.Property(typeof(LouseProgenitor), "WebDamage")!.GetValue(louseProgenitor)!;

        var webMove = new MoveState("WEB_CANNON_MOVE", targets => InvokeOriginalMove(louseProgenitor, "WebMove", targets), new SingleAttackIntent(webDamage), new DebuffIntent());
        var pounceMove = new MoveState("POUNCE_MOVE", targets => PounceMove(louseProgenitor, targets), new MultiAttackIntent(MutantPounceDamage, MutantPounceRepeat));
        var curlAndGrowMove = new MoveState("CURL_AND_GROW_MOVE", targets => InvokeOriginalMove(louseProgenitor, "CurlAndGrowMove", targets), new DefendIntent(), new BuffIntent());

        webMove.FollowUpState = curlAndGrowMove;
        curlAndGrowMove.FollowUpState = pounceMove;
        pounceMove.FollowUpState = webMove;

        return new MonsterMoveStateMachine(new List<MonsterState> { curlAndGrowMove, webMove, pounceMove }, webMove);
    }

    private static async Task PounceMove(LouseProgenitor louseProgenitor, IReadOnlyList<Creature> targets)
    {
        if (louseProgenitor.Curled)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(louseProgenitor.Creature, "Uncurl", 0.9f);
            louseProgenitor.Curled = false;
        }

        string attackSfx = (string)AccessTools.Property(typeof(LouseProgenitor), "AttackSfx")!.GetValue(louseProgenitor)!;
        await DamageCmd.Attack(MutantPounceDamage)
            .WithHitCount(MutantPounceRepeat)
            .FromMonster(louseProgenitor)
            .WithAttackerAnim("Attack", 0.2f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, attackSfx)
            .Execute(null);
    }

    private static Task InvokeOriginalMove(LouseProgenitor louseProgenitor, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(LouseProgenitor), methodName)!.Invoke(louseProgenitor, new object[] { targets })!;
    }
}

internal static class LouseProgenitorBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
    public const int MutantHpAdd = 5;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(LouseProgenitor), "get_MinInitialHp")]
public static class LouseProgenitorMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += LouseProgenitorBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(LouseProgenitor), "get_MaxInitialHp")]
public static class LouseProgenitorMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += LouseProgenitorBlightHpNumbers.GetHpAdd();
    }
}