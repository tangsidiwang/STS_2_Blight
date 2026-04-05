using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlightMod.AI.Bestiary;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetUpForCombat))]
public static class GasBombStartBuffPatch
{
    public static void Postfix(MonsterModel __instance)
    {
        if (!BlightModeManager.IsBlightModeActive || !BlightBestiaryHpTemplate.IsCurrentNodeMutant())
        {
            return;
        }

        if (__instance is not GasBomb)
        {
            return;
        }

        _ = PowerCmd.Apply<SlipperyPower>(__instance.Creature, 1m, __instance.Creature, null);
    }
}