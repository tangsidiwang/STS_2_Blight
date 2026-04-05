using HarmonyLib;
using System;
using BlightMod.AI;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(CombatState), nameof(CombatState.CreateCreature))]
    public class DifficultyStatPatch_Health
    {
        public static void Postfix(Creature __result, MonsterModel monster, CombatSide side)
        {
            if (!BlightModeManager.IsBlightModeActive) return;
            if (side != CombatSide.Enemy) return;
            if (__result == null || monster == null) return;

            bool isToadpole = monster.Id.Entry == "Toadpole" || monster.GetType().Name == "Toadpole";

            // 已经有专门覆写数值/AI 的怪物，交给专属逻辑处理，避免重复叠加。
            bool hasOverride = BlightMonsterDirector.HasCustomOverride(monster);
            if (isToadpole)
            {
                Log.Info($"[Blight][ToadpoleDebug] DifficultyStatPatch before skip-check entry={monster.Id.Entry}, type={monster.GetType().Name}, maxHp={__result.MaxHp}, currentHp={__result.CurrentHp}, hasOverride={hasOverride}, asc={BlightModeManager.BlightAscensionLevel}");
            }

            if (hasOverride)
            {
                if (isToadpole)
                {
                    Log.Info("[Blight][ToadpoleDebug] DifficultyStatPatch skipped because custom override exists.");
                }
                return;
            }

            int baseMaxHp = __result.MaxHp;
            if (baseMaxHp <= 0) return;

            decimal ratio = GetBaseHealthRatio(BlightModeManager.BlightAscensionLevel);
            if (IsCurrentNodeMutant())
            {
                ratio += 0.20m;
            }

            if (ratio <= 0m) return;

            int bonusHp = (int)Math.Ceiling(baseMaxHp * ratio);
            if (bonusHp <= 0) return;

            int finalHp = baseMaxHp + bonusHp;
            __result.SetMaxHpInternal(finalHp);
            __result.SetCurrentHpInternal(finalHp);

            if (isToadpole)
            {
                Log.Info($"[Blight][ToadpoleDebug] DifficultyStatPatch applied ratio={ratio}, bonusHp={bonusHp}, finalHp={finalHp}");
            }
        }

        private static decimal GetBaseHealthRatio(int ascensionLevel)
        {
            return ascensionLevel switch
            {
                0 => 0m,
                1 => 0.05m,
                2 => 0.1m,
                3 => 0.15m,
                4 => 0.25m,
                _ => 0.3m
            };
        }

        private static bool IsCurrentNodeMutant()
        {
            if (!BlightModeManager.IsAtLeastAscension(1))
            {
                return false;
            }

            var state = RunManager.Instance?.DebugOnlyGetState();
            var point = state?.CurrentMapPoint;
            string seed = state?.Rng.StringSeed ?? string.Empty;
            if (point == null || string.IsNullOrEmpty(seed))
            {
                return false;
            }

            return BlightModeManager.IsNodeMutant(point, seed);
        }
    }
}
