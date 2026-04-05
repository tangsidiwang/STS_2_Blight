using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class GlobeHeadBlightAI : IBlightMonsterAI
{
    private const decimal ShockingSlapWeakAmount = 2m;

    public string TargetMonsterId => "GlobeHead";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not GlobeHead globeHead)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int thunderStrikeDamage = (int)AccessTools.Property(typeof(GlobeHead), "ThunderStrikeDamage")!.GetValue(globeHead)!;
        int shockingSlapDamage = (int)AccessTools.Property(typeof(GlobeHead), "ShockingSlapDamage")!.GetValue(globeHead)!;
        int galvanicBurstDamage = (int)AccessTools.Property(typeof(GlobeHead), "GalvanicBurstDamage")!.GetValue(globeHead)!;

        var shockingSlap = new MoveState(
            "SHOCKING_SLAP",
            targets => ShockingSlapMove(globeHead, targets),
            new SingleAttackIntent(shockingSlapDamage),
            new DebuffIntent());
        var thunderStrike = new MoveState(
            "THUNDER_STRIKE",
            targets => InvokeOriginalMove(globeHead, "ThunderStrike", targets),
            new MultiAttackIntent(thunderStrikeDamage, 3));
        var galvanicBurst = new MoveState(
            "GALVANIC_BURST",
            targets => InvokeOriginalMove(globeHead, "GalvanicBurstMove", targets),
            new SingleAttackIntent(galvanicBurstDamage),
            new BuffIntent());

        shockingSlap.FollowUpState = thunderStrike;
        thunderStrike.FollowUpState = galvanicBurst;
        galvanicBurst.FollowUpState = shockingSlap;

        return new MonsterMoveStateMachine(new List<MonsterState> { shockingSlap, thunderStrike, galvanicBurst }, shockingSlap);
    }

    private static async Task ShockingSlapMove(GlobeHead globeHead, IReadOnlyList<Creature> targets)
    {
        await InvokeOriginalMove(globeHead, "ShockingSlap", targets);
        await PowerCmd.Apply<WeakPower>(targets, ShockingSlapWeakAmount, globeHead.Creature, null);
    }

    private static Task InvokeOriginalMove(GlobeHead globeHead, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(GlobeHead), methodName)!.Invoke(globeHead, new object[] { targets })!;
    }
}

internal static class GlobeHeadBlightHpNumbers
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

[HarmonyPatch(typeof(GlobeHead), "get_MinInitialHp")]
public static class GlobeHeadMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += GlobeHeadBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(GlobeHead), "get_MaxInitialHp")]
public static class GlobeHeadMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}