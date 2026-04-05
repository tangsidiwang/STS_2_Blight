using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class ChomperBlightAI : IBlightMonsterAI
{
    private const int ScreechStatusCount = 3;
    private const int ClampRepeat = 2;

    public string TargetMonsterId => "Chomper";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Chomper chomper)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int clampDamage = (int)AccessTools.Property(typeof(Chomper), "ClampDamage")!.GetValue(null)!;
        bool screamFirst = (bool)AccessTools.Property(typeof(Chomper), "ScreamFirst")!.GetValue(chomper)!;

        var clampMove = new MoveState("CLAMP_MOVE", targets => InvokeOriginalMove(chomper, "ClampMove", targets), new MultiAttackIntent(clampDamage, ClampRepeat));
        var screechMove = new MoveState("SCREECH_MOVE", targets => ScreechMove(chomper, targets), new StatusIntent(ScreechStatusCount));

        clampMove.FollowUpState = screechMove;
        screechMove.FollowUpState = clampMove;

        MonsterState initialState = screamFirst ? screechMove : clampMove;
        return new MonsterMoveStateMachine(new List<MonsterState> { clampMove, screechMove }, initialState);
    }

    private static async Task ScreechMove(Chomper chomper, IReadOnlyList<Creature> targets)
    {
        LocString line = MonsterModel.L10NMonsterLookup("CHOMPER.moves.SCREECH.title");
        TalkCmd.Play(line, chomper.Creature);
        string castSfx = (string)AccessTools.Property(typeof(MonsterModel), "CastSfx")!.GetValue(chomper)!;
        SfxCmd.Play(castSfx);
        await CreatureCmd.TriggerAnim(chomper.Creature, "Cast", 1f);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, ScreechStatusCount, addedByPlayer: false);
    }

    private static Task InvokeOriginalMove(Chomper chomper, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(Chomper), methodName)!.Invoke(chomper, new object[] { targets })!;
    }
}

internal static class ChomperBlightHpNumbers
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

[HarmonyPatch(typeof(Chomper), "get_MinInitialHp")]
public static class ChomperMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += ChomperBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Chomper), "get_MaxInitialHp")]
public static class ChomperMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += ChomperBlightHpNumbers.GetHpAdd();
    }
}