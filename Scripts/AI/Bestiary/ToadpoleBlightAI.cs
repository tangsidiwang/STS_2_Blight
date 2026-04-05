using System.Collections.Generic;
using BlightMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.AI.Bestiary
{
    internal static class ToadpoleBlightNumbers
    {
        private const string SpinAttackSfx = "event:/sfx/enemy/enemy_attacks/toadpole/toadpole_attack_spin";
        private const int MutantSpikeSpitDamage = 5;
        private const int SpikeSpitRepeat = 3;

        public static int GetHpAdd(int ascensionLevel, bool mutant)
        {
            return BlightBestiaryHpTemplate.GetHpAdd("Toadpole", ascensionLevel, mutant);
        }

        public static int GetSpikeSpitDamage(bool mutant, int originalDamage)
        {
            if (mutant)
            {
                return MutantSpikeSpitDamage;
            }

            return originalDamage;
        }

        public static int GetSpikeSpitHits()
        {
            return SpikeSpitRepeat;
        }

        public static string GetAttackSfx()
        {
            return SpinAttackSfx;
        }

        public static int GetSpikenThornsGain(int ascensionLevel, bool mutant)
        {
            if (mutant)
            {
                return ascensionLevel >= 3 ? 20 : 5;
            }

            return ascensionLevel >= 3 ? 3 : 2;
        }

        public static bool IsCurrentNodeMutant()
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

    public sealed class ToadpoleBlightAI : IBlightMonsterAI
    {
        public string TargetMonsterId => "Toadpole";

        public void ApplyBlightStartBuffs(MonsterModel monster, int ascensionLevel)
        {
            return;
        }

        public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
        {
            bool mutant = ToadpoleBlightNumbers.IsCurrentNodeMutant();
            Toadpole toadpole = (Toadpole)monster;
            int attackDamage = mutant ? toadpole.GetType().GetProperty("WhirlDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is { } whirlProp ? (int)whirlProp.GetValue(toadpole)! : 0 : (int)toadpole.GetType().GetProperty("WhirlDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(toadpole)!;
            int originalSpikeSpitDamage = (int)toadpole.GetType().GetProperty("SpikeSpitDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(toadpole)!;
            int multiDamage = ToadpoleBlightNumbers.GetSpikeSpitDamage(mutant, originalSpikeSpitDamage);
            int multiHits = ToadpoleBlightNumbers.GetSpikeSpitHits();
            int thornsGain = ToadpoleBlightNumbers.GetSpikenThornsGain(blightAscensionLevel, mutant);

            var states = new List<MonsterState>();

            var initState = new ConditionalBranchState("INIT_MOVE");

            var buffMove = new MoveState(
                "BLIGHT_SPIKEN",
                async _ =>
                {
                    await CreatureCmd.TriggerAnim(monster.Creature, "Cast", 0.2f);
                    await PowerCmd.Apply<ThornsPower>(monster.Creature, thornsGain, monster.Creature, null);
                },
                new BuffIntent());

            var singleAttackMove = new MoveState(
                "BLIGHT_WHIRL",
                async _ =>
                {
                    await DamageCmd.Attack(attackDamage)
                        .FromMonster(monster)
                        .WithAttackerAnim("AttackSingle", 0.15f)
                        .WithAttackerFx(null, ToadpoleBlightNumbers.GetAttackSfx())
                        .WithHitFx("vfx/vfx_attack_blunt", ToadpoleBlightNumbers.GetAttackSfx())
                        .Execute(null);
                },
                new SingleAttackIntent(attackDamage));

            var multiAttackMove = new MoveState(
                "BLIGHT_SPIKE_SPIT",
                async _ =>
                {
                    await PowerCmd.Apply<ThornsPower>(monster.Creature, -thornsGain, monster.Creature, null);

                    await DamageCmd.Attack(multiDamage)
                        .WithHitCount(multiHits)
                        .FromMonster(monster)
                        .WithAttackerAnim("AttackTriple", 0.3f)
                        .OnlyPlayAnimOnce()
                        .WithAttackerFx(null, ToadpoleBlightNumbers.GetAttackSfx())
                        .WithHitFx("vfx/vfx_attack_blunt", ToadpoleBlightNumbers.GetAttackSfx())
                        .Execute(null);
                },
                new MultiAttackIntent(multiDamage, multiHits));

            initState.AddState(singleAttackMove, () => !toadpole.IsFront);
            initState.AddState(buffMove, () => toadpole.IsFront);

            singleAttackMove.FollowUpState = buffMove;
            buffMove.FollowUpState = multiAttackMove;
            multiAttackMove.FollowUpState = singleAttackMove;

            states.Add(initState);
            states.Add(buffMove);
            states.Add(singleAttackMove);
            states.Add(multiAttackMove);
            return new MonsterMoveStateMachine(states, initState);
        }
    }

    [HarmonyPatch(typeof(Toadpole), "get_MinInitialHp")]
    public static class ToadpoleMinHpPatch
    {
        public static void Postfix(ref int __result)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return;
            }

            bool mutant = ToadpoleBlightNumbers.IsCurrentNodeMutant();
            int before = __result;
            __result += ToadpoleBlightNumbers.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant);
            Log.Info($"[Blight][ToadpoleDebug] MinHpPatch before={before}, add={ToadpoleBlightNumbers.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant)}, mutant={mutant}, after={__result}");
        }
    }

    [HarmonyPatch(typeof(Toadpole), "get_MaxInitialHp")]
    public static class ToadpoleMaxHpPatch
    {
        public static void Postfix(ref int __result)
        {
            if (!BlightModeManager.IsBlightModeActive)
            {
                return;
            }

            bool mutant = ToadpoleBlightNumbers.IsCurrentNodeMutant();
            int before = __result;
            __result += ToadpoleBlightNumbers.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant);
            Log.Info($"[Blight][ToadpoleDebug] MaxHpPatch before={before}, add={ToadpoleBlightNumbers.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant)}, mutant={mutant}, after={__result}");
        }
    }
}
