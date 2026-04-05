using HarmonyLib;
using BlightMod.AI.Buffs;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class DoormakerBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "Doormaker";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Doormaker doormaker)
        {
            return;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        if (doormaker.Creature == null || !doormaker.Creature.IsAlive || doormaker.Creature.HasPower<MeteorPower>())
        {
            return;
        }

        _ = PowerCmd.Apply<MeteorPower>(doormaker.Creature, 1m, doormaker.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class DoormakerBlightHpNumbers
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

[HarmonyPatch(typeof(Doormaker), "get_MinInitialHp")]
public static class DoormakerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += DoormakerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(Doormaker), "get_MaxInitialHp")]
public static class DoormakerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}