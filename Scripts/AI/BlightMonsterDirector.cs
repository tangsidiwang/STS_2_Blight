using System.Collections.Generic;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Logging;

namespace BlightMod.AI
{
    public static class BlightMonsterDirector
    {
        // 维护“原版怪物ID -> 荒疫AI策略实现”的映射表。
        private static Dictionary<string, IBlightMonsterAI> _registry = new();
        private static bool _builtInsRegistered;

        static BlightMonsterDirector()
        {
            EnsureBuiltInsRegistered();
            Register(new Bestiary.SlitheringStranglerBlightAI());
        }

        private static void EnsureBuiltInsRegistered()
        {
            if (_builtInsRegistered)
            {
                return;
            }

            _builtInsRegistered = true;
            Register(new Bestiary.ToadpoleBlightAI());
            Register(new Bestiary.DampCultistBlightAI());
            Register(new Bestiary.CalcifiedCultistBlightAI());
            Register(new Bestiary.GremlinMercBlightAI());
            Register(new Bestiary.FatGremlinBlightAI());
            Register(new Bestiary.SneakyGremlinBlightAI());
            Register(new Bestiary.SeapunkBlightAI());
            Register(new Bestiary.ExoskeletonBlightAI());
            Register(new Bestiary.FossilStalkerBlightAI());
            Register(new Bestiary.HunterKillerBlightAI());
            Register(new Bestiary.LivingFogBlightAI());
            Register(new Bestiary.TwoTailedRatBlightAI());
            Register(new Bestiary.SewerClamBlightAI());
            Register(new Bestiary.HauntedShipBlightAI());
            Register(new Bestiary.SludgeSpinnerBlightAI());
            Register(new Bestiary.PunchConstructBlightAI());
            Register(new Bestiary.CubexConstructBlightAI());
            Register(new Bestiary.CorpseSlugBlightAI());
            Register(new Bestiary.TerrorEelBlightAI());
            Register(new Bestiary.ByrdonisBlightAI());
            Register(new Bestiary.BygoneEffigyBlightAI());
            Register(new Bestiary.PhantasmalGardenerBlightAI());
            Register(new Bestiary.SkulkingColonyBlightAI());
            Register(new Bestiary.ChomperBlightAI());
            Register(new Bestiary.BowlbugEggBlightAI());
            Register(new Bestiary.BowlbugNectarBlightAI());
            Register(new Bestiary.BowlbugRockBlightAI());
            Register(new Bestiary.BowlbugSilkBlightAI());
            Register(new Bestiary.FlyconidBlightAI());
            Register(new Bestiary.BruteRubyRaiderBlightAI());
            Register(new Bestiary.AssassinRubyRaiderBlightAI());
            Register(new Bestiary.AxeRubyRaiderBlightAI());
            Register(new Bestiary.CrossbowRubyRaiderBlightAI());
            Register(new Bestiary.TrackerRubyRaiderBlightAI());
            Register(new Bestiary.ThievingHopperBlightAI());
            Register(new Bestiary.FogmogBlightAI());
            Register(new Bestiary.MawlerBlightAI());
            Register(new Bestiary.LouseProgenitorBlightAI());
            Register(new Bestiary.FuzzyWurmCrawlerBlightAI());
            Register(new Bestiary.InkletBlightAI());
            Register(new Bestiary.NibbitBlightAI());
            Register(new Bestiary.InfestedPrismBlightAI());
            Register(new Bestiary.OvicopterBlightAI());
            Register(new Bestiary.EntomancerBlightAI());
            Register(new Bestiary.GlobeHeadBlightAI());
            Register(new Bestiary.LivingShieldBlightAI());
            Register(new Bestiary.TurretOperatorBlightAI());
            Register(new Bestiary.AxebotBlightAI());
            Register(new Bestiary.OwlMagistrateBlightAI());
            Register(new Bestiary.DevotedSculptorBlightAI());
            Register(new Bestiary.FrogKnightBlightAI());
            Register(new Bestiary.SlimedBerserkerBlightAI());
            Register(new Bestiary.TheForgottenBlightAI());
            Register(new Bestiary.TheLostBlightAI());
            Register(new Bestiary.ScrollOfBitingBlightAI());
            Register(new Bestiary.FabricatorBlightAI());
            Register(new Bestiary.MechaKnightBlightAI());
            Register(new Bestiary.SoulNexusBlightAI());
            Register(new Bestiary.FlailKnightBlightAI());
            Register(new Bestiary.SpectralKnightBlightAI());
            Register(new Bestiary.MagiKnightBlightAI());
            Register(new Bestiary.SnappingJaxfruitBlightAI());
            Register(new Bestiary.ShrinkerBeetleBlightAI());
            Register(new Bestiary.SlumberingBeetleBlightAI());
            Register(new Bestiary.ParafrightBlightAI());
            Register(new Bestiary.VineShamblerBlightAI());
            Register(new Bestiary.PhrogParasiteBlightAI());
            Register(new Bestiary.TunnelerBlightAI());
            Register(new Bestiary.SpinyToadBlightAI());
            Register(new Bestiary.TheObscuraBlightAI());
            Register(new Bestiary.VantomBlightAI());
            Register(new Bestiary.KinPriestBlightAI());
            Register(new Bestiary.CeremonialBeastBlightAI());
            Register(new Bestiary.LagavulinMatriarchBlightAI());
            Register(new Bestiary.SoulFyshBlightAI());
            Register(new Bestiary.WaterfallGiantBlightAI());
            Register(new Bestiary.CrusherBlightAI());
            Register(new Bestiary.RocketBlightAI());
            Register(new Bestiary.TheInsatiableBlightAI());
            Register(new Bestiary.DoormakerBlightAI());
            Register(new Bestiary.QueenBlightAI());
            Register(new Bestiary.TestSubjectBlightAI());
            Register(new Bestiary.KnowledgeDemonBlightAI());
            Register(new Bestiary.LeafSlimeSBlightAI());
            Register(new Bestiary.LeafSlimeMBlightAI());
            Register(new Bestiary.TwigSlimeSBlightAI());
            Register(new Bestiary.TwigSlimeMBlightAI());
            Register(new Bestiary.DecimillipedeSegmentBlightAI());
            Register(new Bestiary.DecimillipedeSegmentFrontBlightAI());
            Register(new Bestiary.DecimillipedeSegmentMiddleBlightAI());
            Register(new Bestiary.DecimillipedeSegmentBackBlightAI());
        }

        // 向注册表添加或覆盖一个怪物AI策略。
        public static void Register(IBlightMonsterAI ai)
        {
            _registry[ai.TargetMonsterId] = ai;
        }

        public static bool HasCustomOverride(MonsterModel monster)
        {
            EnsureBuiltInsRegistered();
            if (monster == null)
            {
                return false;
            }

            string entry = monster.Id.Entry;
            string typeName = monster.GetType().Name;
            bool matched = _registry.ContainsKey(entry) || _registry.ContainsKey(typeName);

            if (entry == "Toadpole" || typeName == "Toadpole")
            {
                Log.Info($"[Blight][ToadpoleDebug] Director.HasCustomOverride entry={entry}, type={typeName}, entryRegistered={_registry.ContainsKey(entry)}, typeRegistered={_registry.ContainsKey(typeName)}, registryCount={_registry.Count}");
            }

            return matched;
        }

        // 若该怪物已注册荒疫AI，则返回替换状态机；否则返回 null。
        public static MonsterMoveStateMachine? TryGenerateStateMachine(MonsterModel monster)
        {
            EnsureBuiltInsRegistered();
            if (_registry.TryGetValue(monster.Id.Entry, out var ai) || _registry.TryGetValue(monster.GetType().Name, out ai))
            {
                return ai.GenerateBlightStateMachine(monster, BlightModeManager.BlightAscensionLevel);
            }
            return null;
        }

        // 对已注册怪物应用可选的战斗开场强化。
        public static void TryApplyStartBuffs(MonsterModel monster)
        {
            EnsureBuiltInsRegistered();
            if (_registry.TryGetValue(monster.Id.Entry, out var ai) || _registry.TryGetValue(monster.GetType().Name, out ai))
            {
                ai.ApplyBlightStartBuffs(monster, BlightModeManager.BlightAscensionLevel);
            }
        }
    }
}