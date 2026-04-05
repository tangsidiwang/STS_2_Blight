using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlightMod.AI.Buffs.Patches;

[HarmonyPatch(typeof(PowerModel), "get_Icon")]
public static class BlightPowerIconPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PowerModel __instance, ref Texture2D __result)
    {
        ModelId id = __instance.Id;
        if (id.Category != "POWER" || string.IsNullOrEmpty(id.Entry))
        {
            return true;
        }

        bool isCustomBlightPower = id.Entry.StartsWith("BLIGHT_", StringComparison.Ordinal)
            || id.Entry.Equals("CEREMONIAL_BEAST_CONSTRICT_POWER", StringComparison.Ordinal)
            || id.Entry.Equals("HIVE_POWER", StringComparison.Ordinal)
            || id.Entry.Equals("METEOR_POWER", StringComparison.Ordinal)
            || id.Entry.Equals("COIN_SCATTER_POWER", StringComparison.Ordinal)
            || id.Entry.Equals("SAND_SHIELD_POWER", StringComparison.Ordinal)
            || id.Entry.Equals("THE_INSATIABLE_SHRIEK_POWER", StringComparison.Ordinal);

        if (!isCustomBlightPower)
        {
            return true;
        }

        if (TryGetFallbackIcon(id.Entry, out Texture2D? icon))
        {
            __result = icon;
            return false;
        }

        return true;
    }

    private static bool TryGetFallbackIcon(string entry, out Texture2D? icon)
    {
        icon = null;

        if (entry.Equals("BLIGHT_EFFIGY_WARD", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<PlatingPower>().Icon;
            return true;
        }

        if (entry.Equals("CEREMONIAL_BEAST_CONSTRICT_POWER", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<ConstrictPower>().Icon;
            return true;
        }

        if (entry.Equals("HIVE_POWER", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<FreeAttackPower>().Icon;
            return true;
        }

        if (entry.Equals("METEOR_POWER", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<FreeSkillPower>().Icon;
            return true;
        }
        if (entry.Equals("SAND_SHIELD_POWER", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<BlackHolePower>().Icon;
            return true;
        }

        if (entry.Equals("COIN_SCATTER_POWER", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<HeistPower>().Icon;
            return true;
        }

        if (entry.Equals("THE_INSATIABLE_SHRIEK_POWER", StringComparison.Ordinal))
        {
            icon = ModelDb.Power<ShriekPower>().Icon;
            return true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_ResolvedBigIconPath")]
public static class BlightPowerBigIconPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PowerModel __instance, ref string __result)
    {
        ModelId id = __instance.Id;
        if (id.Category != "POWER" || string.IsNullOrEmpty(id.Entry))
        {
            return true;
        }

        if (id.Entry.Equals("CEREMONIAL_BEAST_CONSTRICT_POWER", StringComparison.Ordinal))
        {
            __result = ModelDb.Power<ConstrictPower>().ResolvedBigIconPath;
            return false;
        }

        if (id.Entry.Equals("SAND_SHIELD_POWER", StringComparison.Ordinal))
        {
            __result = ModelDb.Power<BlackHolePower>().ResolvedBigIconPath;
            return false;
        }

        if (id.Entry.Equals("THE_INSATIABLE_SHRIEK_POWER", StringComparison.Ordinal))
        {
            __result = ModelDb.Power<ShriekPower>().ResolvedBigIconPath;
            return false;
        }

        return true;
    }
}