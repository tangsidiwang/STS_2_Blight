using HarmonyLib;
using BlightMod.Core;
using BlightMod.AI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(MonsterModel), "SetUpForCombat")]
    public class MonsterAIPatch
    {
        public static void Postfix(MonsterModel __instance)
        {
            // 非荒疫模式或不需要覆写时，保留原版生成的状态机
            if (!BlightModeManager.IsBlightModeActive) return;

            // 进阶1开始（或其他需要高难AI的层级），执行更为强硬和智能的AI逻辑
            if (BlightModeManager.BlightAscensionLevel >= 1)
            {
                var blightStateMachine = BlightMonsterDirector.TryGenerateStateMachine(__instance);
                if (blightStateMachine != null)
                {
                    AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").SetValue(__instance, blightStateMachine);
                    BlightMonsterDirector.TryApplyStartBuffs(__instance);
                }
            }
        }
    }
}