using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class CorpseSlugBlightAI : IBlightMonsterAI
{
    private const decimal MutantRavenousBonus = 5m;

    public string TargetMonsterId => "CorpseSlug";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (!BlightBestiaryHpTemplate.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<RavenousPower>(monster.Creature, MutantRavenousBonus, monster.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class CorpseSlugBlightHpNumbers
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

[HarmonyPatch(typeof(CorpseSlug), "get_MinInitialHp")]
public static class CorpseSlugMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += CorpseSlugBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(CorpseSlug), "get_MaxInitialHp")]
public static class CorpseSlugMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += CorpseSlugBlightHpNumbers.GetHpAdd();
    }
}