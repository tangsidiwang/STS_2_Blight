using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class PhantasmalGardenerBlightAI : IBlightMonsterAI
{
    private const decimal EliteStartStrength = 1m;
    private const decimal MutantStartSlippery = 1m;

    public string TargetMonsterId => "PhantasmalGardener";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not PhantasmalGardener phantasmalGardener || !BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        _ = PowerCmd.Apply<StrengthPower>(phantasmalGardener.Creature, EliteStartStrength, phantasmalGardener.Creature, null);

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<SlipperyPower>(phantasmalGardener.Creature, MutantStartSlippery, phantasmalGardener.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class PhantasmalGardenerBlightHpNumbers
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

[HarmonyPatch(typeof(PhantasmalGardener), "get_MinInitialHp")]
public static class PhantasmalGardenerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += PhantasmalGardenerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(PhantasmalGardener), "get_MaxInitialHp")]
public static class PhantasmalGardenerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += PhantasmalGardenerBlightHpNumbers.GetHpAdd();
    }
}