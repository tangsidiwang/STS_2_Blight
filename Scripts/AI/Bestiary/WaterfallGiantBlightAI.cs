using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace BlightMod.AI.Bestiary;

public sealed class WaterfallGiantBlightAI : IBlightMonsterAI
{
    private const decimal SteamBonusPerIntent = 2m;
    private const decimal DefaultSteamGain = 3m;
    private const int SiphonHeal = 15;
    private const int PressureGunIncrease = 5;

    public string TargetMonsterId => "WaterfallGiant";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not WaterfallGiant waterfallGiant)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        // Boss AI changes only apply from A5.
        if (!BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int pressurizeAmount = (int)AccessTools.Property(typeof(WaterfallGiant), "PressurizeAmount")!.GetValue(waterfallGiant)!;

        var pressurizeMove = new MoveState("PRESSURIZE_MOVE", targets => PressurizeMove(waterfallGiant, pressurizeAmount, targets), new BuffIntent());
        var stompMove = new MoveState("STOMP_MOVE", targets => StompMove(waterfallGiant, targets), new DebuffIntent(), new BuffIntent());
        var ramMove = new MoveState("RAM_MOVE", targets => RamMove(waterfallGiant, targets), new BuffIntent());
        var siphonMove = new MoveState("SIPHON_MOVE", targets => SiphonMove(waterfallGiant, targets), new HealIntent(), new BuffIntent());
        var pressureGunMove = new MoveState("PRESSURE_GUN_MOVE", targets => PressureGunMove(waterfallGiant, targets), new BuffIntent());
        var pressureUpMove = new MoveState("PRESSURE_UP_MOVE", targets => PressureUpMove(waterfallGiant, targets), new BuffIntent());
        var aboutToBlowMove = new MoveState("ABOUT_TO_BLOW_MOVE", targets => AboutToBlowMove(waterfallGiant, targets), new StunIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        var explodeMove = new MoveState("EXPLODE_MOVE", targets => ExplodeMove(waterfallGiant, targets), new DeathBlowIntent(() => GetStoredSteamDamage(waterfallGiant)));

        pressurizeMove.FollowUpState = stompMove;
        stompMove.FollowUpState = ramMove;
        ramMove.FollowUpState = siphonMove;
        siphonMove.FollowUpState = pressureGunMove;
        pressureGunMove.FollowUpState = pressureUpMove;
        pressureUpMove.FollowUpState = stompMove;
        aboutToBlowMove.FollowUpState = explodeMove;
        explodeMove.FollowUpState = explodeMove;

        AccessTools.Property(typeof(WaterfallGiant), "AboutToBlowState")!.SetValue(waterfallGiant, aboutToBlowMove);

        return new MonsterMoveStateMachine(
            new System.Collections.Generic.List<MonsterState>
            {
                pressurizeMove,
                stompMove,
                ramMove,
                siphonMove,
                pressureGunMove,
                pressureUpMove,
                explodeMove,
                aboutToBlowMove
            },
            pressurizeMove);
    }

    private static async System.Threading.Tasks.Task PressurizeMove(WaterfallGiant waterfallGiant, int pressurizeAmount, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/waterfall_giant/waterfall_giant_eruption");
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "Heal", 0.8f);
        await PowerCmd.Apply<SteamEruptionPower>(waterfallGiant.Creature, pressurizeAmount + SteamBonusPerIntent, waterfallGiant.Creature, null);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task PressureUpMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "AttackBuff", 0.15f);
        await PowerCmd.Apply<SteamEruptionPower>(waterfallGiant.Creature, DefaultSteamGain + SteamBonusPerIntent, waterfallGiant.Creature, null);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task StompMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "AttackDebuff", 0.3f);
        await PowerCmd.Apply<WeakPower>(targets, 1m, waterfallGiant.Creature, null);
        await PowerCmd.Apply<SteamEruptionPower>(waterfallGiant.Creature, DefaultSteamGain + SteamBonusPerIntent, waterfallGiant.Creature, null);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task RamMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "Attack", 0.3f);
        await PowerCmd.Apply<SteamEruptionPower>(waterfallGiant.Creature, DefaultSteamGain + SteamBonusPerIntent, waterfallGiant.Creature, null);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task SiphonMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/waterfall_giant/waterfall_giant_eruption");
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "Heal", 0.8f);
        await CreatureCmd.Heal(waterfallGiant.Creature, SiphonHeal * waterfallGiant.Creature.CombatState.Players.Count);
        await PowerCmd.Apply<SteamEruptionPower>(waterfallGiant.Creature, DefaultSteamGain + SteamBonusPerIntent, waterfallGiant.Creature, null);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task PressureGunMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "Attack", 0.3f);

        int currentPressureGunDamage = (int)AccessTools.Property(typeof(WaterfallGiant), "CurrentPressureGunDamage")!.GetValue(waterfallGiant)!;
        AccessTools.Property(typeof(WaterfallGiant), "CurrentPressureGunDamage")!.SetValue(waterfallGiant, currentPressureGunDamage + PressureGunIncrease);

        await PowerCmd.Apply<SteamEruptionPower>(waterfallGiant.Creature, DefaultSteamGain + SteamBonusPerIntent, waterfallGiant.Creature, null);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task AboutToBlowMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        int steamEruptionDamage = waterfallGiant.Creature.GetPowerAmount<SteamEruptionPower>();
        AccessTools.Property(typeof(WaterfallGiant), "SteamEruptionDamage")!.SetValue(waterfallGiant, steamEruptionDamage);
        await PowerCmd.Remove<SteamEruptionPower>(waterfallGiant.Creature);
        AccessTools.Property(typeof(WaterfallGiant), "PressureBuildupIdx")!.SetValue(waterfallGiant, 6);
        InvokeOriginalNoArg(waterfallGiant, "IncrementBuildUpAnimationTrack");
    }

    private static async System.Threading.Tasks.Task ExplodeMove(WaterfallGiant waterfallGiant, System.Collections.Generic.IReadOnlyList<Creature> _)
    {
        int steamDamage = GetStoredSteamDamage(waterfallGiant);

        InvokeOriginalNoArg(waterfallGiant, "StopAmbientSfx");
        await CreatureCmd.TriggerAnim(waterfallGiant.Creature, "Erupt", 0.1f);

        // Match vanilla behavior: convert accumulated steam into explosion damage before self-kill.
        await DamageCmd.Attack(steamDamage)
            .FromMonster(waterfallGiant)
            .WithAttackerAnim("Erupt", 0.1f)
            .WithAttackerFx(null, waterfallGiant.DeathSfx)
            .Execute(null);

        await CreatureCmd.Kill(waterfallGiant.Creature);
        NRunMusicController.Instance?.UpdateMusicParameter("waterfall_giant_progress", 5f);
    }

    private static int GetStoredSteamDamage(WaterfallGiant waterfallGiant)
    {
        return (int)AccessTools.Property(typeof(WaterfallGiant), "SteamEruptionDamage")!.GetValue(waterfallGiant)!;
    }

    private static void InvokeOriginalNoArg(WaterfallGiant waterfallGiant, string methodName)
    {
        AccessTools.Method(typeof(WaterfallGiant), methodName)!.Invoke(waterfallGiant, null);
    }
}

internal static class WaterfallGiantBlightHpNumbers
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

[HarmonyPatch(typeof(WaterfallGiant), "get_MinInitialHp")]
public static class WaterfallGiantMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += WaterfallGiantBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(WaterfallGiant), "get_MaxInitialHp")]
public static class WaterfallGiantMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += WaterfallGiantBlightHpNumbers.GetHpAdd();
    }
}