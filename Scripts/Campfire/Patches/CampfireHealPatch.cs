using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BlightMod.Campfire.Patches
{
    [HarmonyPatch(typeof(HealRestSiteOption), nameof(HealRestSiteOption.GetHealAmount))]
    public static class CampfireHealPatch
    {
        private const decimal CampfireHealPercent = 0.5m;

        [HarmonyPostfix]
        public static void Postfix(Player player, ref decimal __result)
        {
            if (!BlightModeManager.IsBlightModeActive || player?.Creature == null)
            {
                return;
            }

            __result = GetBlightCampfireHealAmount(player.Creature);
        }

        internal static decimal GetBlightCampfireHealAmount(Creature creature)
        {
            return (decimal)creature.MaxHp * CampfireHealPercent;
        }

        internal static int GetDisplayedHealPercent()
        {
            return (int)(CampfireHealPercent * 100m);
        }
    }

    [HarmonyPatch(typeof(HealRestSiteOption), nameof(HealRestSiteOption.Description), MethodType.Getter)]
    public static class CampfireHealDescriptionPatch
    {
        [HarmonyPostfix]
        public static void Postfix(HealRestSiteOption __instance, ref LocString __result)
        {
            if (!BlightModeManager.IsBlightModeActive || __instance == null)
            {
                return;
            }

            Player owner = Traverse.Create(__instance).Property("Owner").GetValue<Player>();
            if (owner?.Creature == null)
            {
                return;
            }

            LocString description = new LocString("relics", "BLIGHT_CAMPFIRE_HEAL.description");
            HealVar healVar = new HealVar(CampfireHealPatch.GetBlightCampfireHealAmount(owner.Creature));
            description.Add("HealPercent", CampfireHealPatch.GetDisplayedHealPercent());
            description.Add(healVar);
            description.Add("ExtraText", string.Empty);
            __result = description;
        }
    }
}
