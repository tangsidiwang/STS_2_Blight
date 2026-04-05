using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class HauntedShipBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "HauntedShip";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class HauntedShipBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 3;
    public const int A3To4HpAdd = 4;
    public const int A5PlusHpAdd = 5;
    public const int MutantHpAdd = 4;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(HauntedShip), "get_MinInitialHp")]
public static class HauntedShipMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += HauntedShipBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(HauntedShip), "get_MaxInitialHp")]
public static class HauntedShipMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += HauntedShipBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(HauntedShip), "RammingSpeedMove")]
public static class HauntedShipRammingSpeedMovePatch
{
    public static async Task Postfix(Task __result, HauntedShip __instance, IReadOnlyList<Creature> targets)
    {
        await __result;

        if (!BlightModeManager.IsBlightModeActive || !BlightBestiaryHpTemplate.IsCurrentNodeMutant())
        {
            return;
        }

        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, 2, addedByPlayer: false);
    }
}