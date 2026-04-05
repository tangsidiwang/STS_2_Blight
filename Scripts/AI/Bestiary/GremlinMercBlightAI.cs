using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class GremlinMercBlightAI : IBlightMonsterAI
{
    private const string AttackBuffSfx = "event:/sfx/enemy/enemy_attacks/gremlin_merc/gremlin_merc_attack_buff";
    private const string AttackSfx = "event:/sfx/enemy/enemy_attacks/gremlin_merc/gremlin_merc_attack";
    private bool _hasSpoken;

    public string TargetMonsterId => "GremlinMerc";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        return;
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        var gremlinMerc = (GremlinMerc)monster;
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        int gimmeDamage = (int)AccessTools.Property(typeof(GremlinMerc), "GimmeDamage")!.GetValue(gremlinMerc)!;
        int gimmeRepeat = (int)AccessTools.Property(typeof(GremlinMerc), "GimmeRepeat")!.GetValue(gremlinMerc)!;
        int doubleSmashDamage = (int)AccessTools.Property(typeof(GremlinMerc), "DoubleSmashDamage")!.GetValue(gremlinMerc)!;
        int doubleSmashRepeat = (int)AccessTools.Property(typeof(GremlinMerc), "DoubleSmashRepeat")!.GetValue(gremlinMerc)!;
        int heheDamage = gremlinMerc.HeheDamage;

        var states = new List<MonsterState>();

        MoveState gimmeMove = mutant
            ? new MoveState(
                "GIMME_MOVE",
                async targets =>
                {
                    if (!_hasSpoken)
                    {
                        _hasSpoken = true;
                        LocString line = MonsterModel.L10NMonsterLookup("GREMLIN_MERC.moves.GIMME.banter");
                        TalkCmd.Play(line, gremlinMerc.Creature);
                    }

                    VfxCmd.PlayOnCreatureCenters(targets, "vfx/vfx_coin_explosion_regular");
                    await DamageCmd.Attack(gimmeDamage).WithHitCount(gimmeRepeat).FromMonster(gremlinMerc)
                        .WithAttackerAnim("AttackDouble", 0.15f)
                        .OnlyPlayAnimOnce()
                        .WithAttackerFx(null, AttackSfx)
                        .WithHitFx("vfx/vfx_attack_slash")
                        .Execute(null);

                    foreach (ThieveryPower powerInstance in gremlinMerc.Creature.GetPowerInstances<ThieveryPower>())
                    {
                        await powerInstance.Steal();
                    }

                    await PowerCmd.Apply<WeakPower>(targets, 2m, gremlinMerc.Creature, null);
                    await PowerCmd.Apply<FrailPower>(targets, 1m, gremlinMerc.Creature, null);
                },
                new MultiAttackIntent(gimmeDamage, gimmeRepeat),
                new DebuffIntent())
            : new MoveState(
                "GIMME_MOVE",
                async targets =>
                {
                    if (!_hasSpoken)
                    {
                        _hasSpoken = true;
                        LocString line = MonsterModel.L10NMonsterLookup("GREMLIN_MERC.moves.GIMME.banter");
                        TalkCmd.Play(line, gremlinMerc.Creature);
                    }

                    VfxCmd.PlayOnCreatureCenters(targets, "vfx/vfx_coin_explosion_regular");
                    await DamageCmd.Attack(gimmeDamage).WithHitCount(gimmeRepeat).FromMonster(gremlinMerc)
                        .WithAttackerAnim("AttackDouble", 0.15f)
                        .OnlyPlayAnimOnce()
                        .WithAttackerFx(null, AttackSfx)
                        .WithHitFx("vfx/vfx_attack_slash")
                        .Execute(null);

                    foreach (ThieveryPower powerInstance in gremlinMerc.Creature.GetPowerInstances<ThieveryPower>())
                    {
                        await powerInstance.Steal();
                    }
                },
                new MultiAttackIntent(gimmeDamage, gimmeRepeat));

        var doubleSmashMove = new MoveState(
            "DOUBLE_SMASH_MOVE",
            async targets =>
            {
                SfxCmd.Play(AttackSfx);
                VfxCmd.PlayOnCreatureCenters(targets, "vfx/vfx_coin_explosion_regular");
                await DamageCmd.Attack(doubleSmashDamage).WithHitCount(doubleSmashRepeat).FromMonster(gremlinMerc)
                    .WithAttackerAnim("AttackDouble", 0.15f)
                    .OnlyPlayAnimOnce()
                    .WithAttackerFx(null, AttackSfx)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(null);

                foreach (ThieveryPower powerInstance in gremlinMerc.Creature.GetPowerInstances<ThieveryPower>())
                {
                    await powerInstance.Steal();
                }

                await PowerCmd.Apply<WeakPower>(targets, 2m, gremlinMerc.Creature, null);
            },
            new MultiAttackIntent(doubleSmashDamage, doubleSmashRepeat),
            new DebuffIntent());

        var heheMove = new MoveState(
            "HEHE_MOVE",
            async _ =>
            {
                await DamageCmd.Attack(heheDamage).FromMonster(gremlinMerc).WithAttackerAnim("Attack", 0.15f)
                    .WithAttackerFx(null, AttackBuffSfx)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(null);

                foreach (ThieveryPower powerInstance in gremlinMerc.Creature.GetPowerInstances<ThieveryPower>())
                {
                    await powerInstance.Steal();
                }

                await PowerCmd.Apply<StrengthPower>(gremlinMerc.Creature, 2m, gremlinMerc.Creature, null);
            },
            new SingleAttackIntent(heheDamage),
            new BuffIntent());

        gimmeMove.FollowUpState = doubleSmashMove;
        doubleSmashMove.FollowUpState = heheMove;
        heheMove.FollowUpState = gimmeMove;

        states.Add(gimmeMove);
        states.Add(doubleSmashMove);
        states.Add(heheMove);
        return new MonsterMoveStateMachine(states, gimmeMove);
    }
}

internal static class GremlinMercBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 2;
    public const int A3To4HpAdd = 3;
    public const int A5PlusHpAdd = 4;
    public const int MutantHpAdd = 6;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(GremlinMerc), "get_MinInitialHp")]
public static class GremlinMercMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += GremlinMercBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(GremlinMerc), "get_MaxInitialHp")]
public static class GremlinMercMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += GremlinMercBlightHpNumbers.GetHpAdd();
    }
}