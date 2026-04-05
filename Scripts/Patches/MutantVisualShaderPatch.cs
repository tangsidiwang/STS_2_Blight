using BlightMod.Visuals;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlightMod.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class MutantVisualShaderReadyPatch
{
	public static void Postfix(NCreature __instance)
	{
		MutantVisualShaderController.TryApply(__instance);
	}
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetScaleAndHue))]
public static class MutantVisualShaderScalePatch
{
	public static void Postfix(NCreature __instance)
	{
		MutantVisualShaderController.TryApply(__instance);
	}
}