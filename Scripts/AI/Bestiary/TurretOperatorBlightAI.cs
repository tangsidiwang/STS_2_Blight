using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class TurretOperatorBlightAI : IBlightMonsterAI
{
    private const decimal MutantStartSlippery = 1m;

    public string TargetMonsterId => "TurretOperator";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not TurretOperator turretOperator || !BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<SlipperyPower>(turretOperator.Creature, MutantStartSlippery, turretOperator.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class TurretOperatorBlightHpNumbers
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

[HarmonyPatch(typeof(TurretOperator), "get_MinInitialHp")]
public static class TurretOperatorMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TurretOperatorBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(TurretOperator), "get_MaxInitialHp")]
public static class TurretOperatorMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += TurretOperatorBlightHpNumbers.GetHpAdd();
    }
}