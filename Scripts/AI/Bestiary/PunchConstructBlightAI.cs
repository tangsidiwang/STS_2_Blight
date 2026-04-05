using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class PunchConstructBlightAI : IBlightMonsterAI
{
    private const decimal MutantArtifactBonus = 98m;

    public string TargetMonsterId => "PunchConstruct";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (!BlightBestiaryHpTemplate.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<ArtifactPower>(monster.Creature, MutantArtifactBonus, monster.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class PunchConstructBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 3;
    public const int A3To4HpAdd = 4;
    public const int A5PlusHpAdd = 5;
    public const int MutantHpAdd = 3;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(PunchConstruct), "get_MinInitialHp")]
public static class PunchConstructMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += PunchConstructBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(PunchConstruct), "get_MaxInitialHp")]
public static class PunchConstructMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += PunchConstructBlightHpNumbers.GetHpAdd();
    }
}