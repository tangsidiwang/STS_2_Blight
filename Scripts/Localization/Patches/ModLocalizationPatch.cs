using System;
using System.Collections.Generic;
using BlightMod.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;

namespace BlightMod.Localization.Patches
{
    [HarmonyPatch(typeof(LocManager), nameof(LocManager.SetLanguage))]
    public static class ModLocalizationPatch
    {
        [HarmonyPostfix]
        public static void Postfix(LocManager __instance, string language)
        {
            MergeTable(__instance, "relics", BlightLocalization.GetRelics(language));
            MergeTable(__instance, "enchantments", BlightLocalization.GetEnchantments(language));
            MergeTable(__instance, "modifiers", BlightLocalization.GetModifiers(language));
            MergeTable(__instance, "powers", BlightLocalization.GetPowers(language));
            MergeTable(__instance, "cards", BlightLocalization.GetCards(language));
        }

        private static void MergeTable(LocManager locManager, string tableName, IReadOnlyDictionary<string, string> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            try
            {
                LocTable table = locManager.GetTable(tableName);
                table.MergeWith(new Dictionary<string, string>(entries));
            }
            catch (Exception ex)
            {
                Log.Warn($"[Blight][Loc] Failed to merge table={tableName}: {ex.Message}");
            }
        }
    }
}
