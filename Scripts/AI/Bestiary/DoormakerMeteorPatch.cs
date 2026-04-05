using BlightMod.AI;
using BlightMod.AI.Buffs;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace BlightMod.AI.Bestiary;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.AfterAddedToRoom))]
public static class DoormakerMeteorPatch
{
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance)
    {
        if (__instance is not Doormaker doormaker)
        {
            return;
        }

        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        // Boss AI changes should only apply on A5+, and Doormaker AI handles Meteor itself.
        if (!BlightAIContext.ShouldOverrideMonsterAi(doormaker) || BlightMonsterDirector.HasCustomOverride(doormaker))
        {
            return;
        }

        if (doormaker.Creature == null || !doormaker.Creature.IsAlive)
        {
            return;
        }

        if (doormaker.Creature.HasPower<MeteorPower>())
        {
            return;
        }

        _ = PowerCmd.Apply<MeteorPower>(doormaker.Creature, 1m, doormaker.Creature, null);
    }
}
