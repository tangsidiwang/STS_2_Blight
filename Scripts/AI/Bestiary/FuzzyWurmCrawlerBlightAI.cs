using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class FuzzyWurmCrawlerBlightAI : IBlightMonsterAI
{
    private const decimal MutantStartCurlUp = 6m;

    public string TargetMonsterId => "FuzzyWurmCrawler";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not FuzzyWurmCrawler fuzzyWurmCrawler || !BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<CurlUpPower>(fuzzyWurmCrawler.Creature, MutantStartCurlUp, fuzzyWurmCrawler.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class FuzzyWurmCrawlerBlightHpNumbers
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

[HarmonyPatch(typeof(FuzzyWurmCrawler), "get_MinInitialHp")]
public static class FuzzyWurmCrawlerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FuzzyWurmCrawlerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(FuzzyWurmCrawler), "get_MaxInitialHp")]
public static class FuzzyWurmCrawlerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += FuzzyWurmCrawlerBlightHpNumbers.GetHpAdd();
    }
}