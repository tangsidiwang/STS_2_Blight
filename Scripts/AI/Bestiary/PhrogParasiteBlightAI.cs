using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace BlightMod.AI.Bestiary;

public sealed class PhrogParasiteBlightAI : IBlightMonsterAI
{
    private const decimal OriginalInfestedAmount = 4m;
    private const decimal EliteInfestedAmount = 5m;
    private const int InfectStatusCount = 3;
    private const int LashRepeat = 4;

    public string TargetMonsterId => "PhrogParasite";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not PhrogParasite phrogParasite || !BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        decimal extraInfested = EliteInfestedAmount - OriginalInfestedAmount;
        if (extraInfested <= 0m)
        {
            return;
        }

        _ = PowerCmd.Apply<InfestedPower>(phrogParasite.Creature, extraInfested, phrogParasite.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not PhrogParasite phrogParasite)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int lashDamage = (int)AccessTools.Property(typeof(PhrogParasite), "LashDamage")!.GetValue(phrogParasite)!;

        var infectMove = new MoveState("INFECT_MOVE", targets => InfectMove(phrogParasite, targets), new StatusIntent(InfectStatusCount));
        var lashMove = new MoveState("LASH_MOVE", targets => InvokeOriginalMove(phrogParasite, "LashMove", targets), new MultiAttackIntent(lashDamage, LashRepeat));
        var randomMove = new RandomBranchState("RAND");

        infectMove.FollowUpState = lashMove;
        lashMove.FollowUpState = infectMove;
        randomMove.AddBranch(infectMove, MoveRepeatType.CannotRepeat);
        randomMove.AddBranch(lashMove, MoveRepeatType.CannotRepeat);

        return new MonsterMoveStateMachine(new List<MonsterState> { infectMove, lashMove, randomMove }, infectMove);
    }

    private static async Task InfectMove(PhrogParasite phrogParasite, IReadOnlyList<Creature> targets)
    {
        string castSfx = (string)AccessTools.Property(typeof(MonsterModel), "CastSfx")!.GetValue(phrogParasite)!;
        SfxCmd.Play(castSfx);
        await CreatureCmd.TriggerAnim(phrogParasite.Creature, "Cast", 0.75f);
        foreach (Creature target in targets)
        {
            NWormyImpactVfx? impact = NWormyImpactVfx.Create(target);
            if (impact != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChild(impact);
            }
        }

        await CardPileCmd.AddToCombatAndPreview<Infection>(targets, PileType.Draw, InfectStatusCount, addedByPlayer: false);
    }

    private static Task InvokeOriginalMove(PhrogParasite phrogParasite, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(PhrogParasite), methodName)!.Invoke(phrogParasite, new object[] { targets })!;
    }
}

internal static class PhrogParasiteBlightHpNumbers
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

[HarmonyPatch(typeof(PhrogParasite), "get_MinInitialHp")]
public static class PhrogParasiteMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += PhrogParasiteBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(PhrogParasite), "get_MaxInitialHp")]
public static class PhrogParasiteMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += PhrogParasiteBlightHpNumbers.GetHpAdd();
    }
}