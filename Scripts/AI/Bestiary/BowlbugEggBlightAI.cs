using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class BowlbugEggBlightAI : IBlightMonsterAI
{
    private const decimal MutantStartCurlUp = 6m;

    public string TargetMonsterId => "BowlbugEgg";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not BowlbugEgg bowlbugEgg || !BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<CurlUpPower>(bowlbugEgg.Creature, MutantStartCurlUp, bowlbugEgg.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class BowlbugEggBlightHpNumbers
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

[HarmonyPatch(typeof(BowlbugEgg), "get_MinInitialHp")]
public static class BowlbugEggMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += BowlbugEggBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(BowlbugEgg), "get_MaxInitialHp")]
public static class BowlbugEggMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += BowlbugEggBlightHpNumbers.GetHpAdd();
    }
}