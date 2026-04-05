using HarmonyLib;
using BlightMod.AI.Buffs;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class EntomancerBlightAI : IBlightMonsterAI
{
    private const decimal EliteStartArtifact = 1m;
    private const decimal MutantStartHive = 1m;

    public string TargetMonsterId => "Entomancer";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Entomancer entomancer || !BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        if (entomancer.Creature == null || !entomancer.Creature.IsAlive)
        {
            return;
        }

        _ = PowerCmd.Apply<ArtifactPower>(entomancer.Creature, EliteStartArtifact, entomancer.Creature, null);

        if (!BlightAIContext.IsCurrentNodeMutant() || entomancer.Creature.HasPower<HivePower>())
        {
            return;
        }

        _ = PowerCmd.Apply<HivePower>(entomancer.Creature, MutantStartHive, entomancer.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class EntomancerBlightHpNumbers
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

[HarmonyPatch(typeof(Entomancer), "get_MinInitialHp")]
public static class EntomancerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += EntomancerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Entomancer), "get_MaxInitialHp")]
public static class EntomancerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}