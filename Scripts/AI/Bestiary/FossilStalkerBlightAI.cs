using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

internal static class FossilStalkerBlightHpNumbers
{
    public const int MutantStartStrength = 2;
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 3;
    public const int A3To4HpAdd = 4;
    public const int A5PlusHpAdd = 5;
    public const int MutantHpAdd = -5;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

public sealed class FossilStalkerBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "FossilStalker";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (!BlightBestiaryHpTemplate.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<StrengthPower>(monster.Creature, FossilStalkerBlightHpNumbers.MutantStartStrength, monster.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

[HarmonyPatch(typeof(FossilStalker), "get_MinInitialHp")]
public static class FossilStalkerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FossilStalkerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(FossilStalker), "get_MaxInitialHp")]
public static class FossilStalkerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FossilStalkerBlightHpNumbers.GetHpAdd();
    }
}