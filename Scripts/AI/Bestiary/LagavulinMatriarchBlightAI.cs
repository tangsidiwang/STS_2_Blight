using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.AI.Bestiary;

public sealed class LagavulinMatriarchBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "LagavulinMatriarch";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        // This monster keeps original intents/move chain; wake-up behavior is adjusted via AsleepPower patch below.
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}

internal static class LagavulinMatriarchBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
    public const int MutantHpAdd = 0;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(LagavulinMatriarch), "get_MinInitialHp")]
public static class LagavulinMatriarchMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += LagavulinMatriarchBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(AsleepPower), nameof(AsleepPower.AfterDamageReceived))]
public static class LagavulinMatriarchAsleepAfterDamagePatch
{
    public static bool Prefix(
        AsleepPower __instance,
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        object? cardSource,
        ref System.Threading.Tasks.Task __result)
    {
        if (!LagavulinMatriarchWakeHelper.ShouldUseBlightWakeRule(__instance)
            || target != __instance.Owner
            || result.UnblockedDamage == 0)
        {
            return true;
        }

        __result = HandleWakeAfterDamage(__instance, target);
        return false;
    }

    private static async System.Threading.Tasks.Task HandleWakeAfterDamage(AsleepPower asleepPower, Creature owner)
    {
        await LagavulinMatriarchWakeHelper.RemovePlatingOnWake(owner, 4m);

        LagavulinMatriarch monster = (LagavulinMatriarch)owner.Monster;
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/lagavulin_matriarch/lagavulin_matriarch_awaken");
        await CreatureCmd.TriggerAnim(owner, "Wake", 0.6f);
        monster.IsAwake = true;
        await CreatureCmd.Stun(owner, monster.WakeUpMove, "SLASH_MOVE");
        await PowerCmd.Remove(asleepPower);
    }
}

[HarmonyPatch(typeof(AsleepPower), nameof(AsleepPower.BeforeTurnEndVeryEarly))]
public static class LagavulinMatriarchAsleepBeforeTurnEndPatch
{
    public static bool Prefix(
        AsleepPower __instance,
        PlayerChoiceContext choiceContext,
        CombatSide side,
        ref System.Threading.Tasks.Task __result)
    {
        if (!LagavulinMatriarchWakeHelper.ShouldUseBlightWakeRule(__instance)
            || side != __instance.Owner.Side
            || __instance.Amount > 1
            || !__instance.Owner.HasPower<PlatingPower>())
        {
            return true;
        }

        __result = LagavulinMatriarchWakeHelper.RemovePlatingOnWake(__instance.Owner, 4m);
        return false;
    }
}

internal static class LagavulinMatriarchWakeHelper
{
    public static bool ShouldUseBlightWakeRule(AsleepPower asleepPower)
    {
        if (!BlightModeManager.IsBlightModeActive || BlightModeManager.BlightAscensionLevel < 5)
        {
            return false;
        }

        if (asleepPower?.Owner?.Monster is not LagavulinMatriarch lagavulinMatriarch)
        {
            return false;
        }

        return BlightAIContext.ShouldOverrideMonsterAi(lagavulinMatriarch);
    }

    public static async System.Threading.Tasks.Task RemovePlatingOnWake(Creature owner, decimal removeAmount)
    {
        if (!owner.HasPower<PlatingPower>())
        {
            return;
        }

        PlatingPower plating = owner.GetPower<PlatingPower>();
        decimal actualRemove = plating.Amount < removeAmount ? plating.Amount : removeAmount;
        if (actualRemove <= 0m)
        {
            return;
        }

        await PowerCmd.Apply<PlatingPower>(owner, -actualRemove, owner, null);
    }
}

[HarmonyPatch(typeof(LagavulinMatriarch), "get_MaxInitialHp")]
public static class LagavulinMatriarchMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }
    }
}