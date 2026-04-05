using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Patches
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ToSerializable))]
    public static class CardModelToSerializableBlightSavePatch
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref SerializableCard __result)
        {
            if (__instance == null || __result == null)
            {
                return;
            }

            BlightSaveLoadWindow.OnCardSerializing(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
    public static class CardModelFromSerializableBlightSavePatch
    {
        [HarmonyPostfix]
        public static void Postfix(SerializableCard save, ref CardModel __result)
        {
            if (save == null || __result == null)
            {
                return;
            }

            BlightSaveLoadWindow.OnCardDeserialized(save, __result);
        }
    }
}
