using System.Collections.Generic;

namespace BlightMod.AI.Bestiary;

internal readonly record struct BlightBestiaryHpRule(int A0, int A1To2, int A3To4, int A5Plus, int Mutant);

internal static class BlightBestiaryHpRegistry
{
    public static IReadOnlyDictionary<string, BlightBestiaryHpRule> Rules { get; } = new Dictionary<string, BlightBestiaryHpRule>
    {
        //密林
        ["Flyconid"] = new(0, 0, 0, 0, 0), // 飞蕈怪
        ["AxeRubyRaider"] = new(0, 0, 0, 0, 0), // 斧手红袭者
        ["AssassinRubyRaider"] = new(0, 0, 0, 0, 0), // 刺客红袭者
        ["BruteRubyRaider"] = new(0, 0, 0, 0, 0), // 暴徒红袭者
        ["Fogmog"] = new(0, 0, 0, 0, 0), // 雾莫格
        ["Mawler"] = new(0, 0, 0, 0, 0), // 蛮兽
        ["FuzzyWurmCrawler"] = new(0, 0, 0, 0, 0), // 毛绒伏地虫
        ["Inklet"] = new(0, 0, 0, 0, -10), // 墨宝
        ["SnappingJaxfruit"] = new(0, 0, 0, 0, 0), // 闪光贾科斯果
        ["SlitheringStrangler"] = new(0, 0, 0, 0, 0), // 蛇行绞杀者
        ["LeafSlimeM"] = new(0, 0, 0, 0, 0), // 叶史莱姆（中）
        ["LeafSlimeS"] = new(0, 0, 0, 0, 0), // 叶史莱姆（小）
        ["TwigSlimeM"] = new(0, 0, 0, 0, 0), // 枝条史莱姆（中）
        ["TwigSlimeS"] = new(0, 0, 0, 0, 0), // 枝条史莱姆（小）
        ["ShrinkerBeetle"] = new(0, 0, 0, 0, 0), // 缩小甲虫
        ["VineShambler"] = new(0, 0, 0, 0, 0), // 藤蔓蹒跚者
        ["Nibbit"] = new(0, 0, 0, 0, 0), // 小啃兽
        ["CubexConstruct"] = new(0, 0, 0, 0, 0), // 立方构装体
        ["Byrdonis"] = new(0, 0, 0, 0, 0), // 多尼斯异鸟
        ["BygoneEffigy"] = new(0, 0, 0, 0, 0), // 旧日雕像
        ["PhrogParasite"] = new(0, 0, 0, 0, 0), // 异蛙寄生虫
        ["Vantom"] = new(0, 0, 0, 0, 0), // 墨影幻灵
        ["KinPriest"] = new(0, 0, 0, 0, 0), // 同族祭司
        ["CeremonialBeast"] = new(0, 0, 0, 0, 0), // 仪式兽

        //暗港
        ["Toadpole"] = new(0, 4, 4, 5, 10), // 蝌蚪
        ["DampCultist"] = new(0, 3, 4, 4, 0), // 潮湿邪教徒
        ["CalcifiedCultist"] = new(0, 0, 3, 3, 0), // 钙化邪教徒
        ["CorpseSlug"] = new(0, 0, 0, 0, 0), // 尸蛞蝓
        ["GremlinMerc"] = new(0, 2, 3, 4, 6), // 雇佣地精
        ["FatGremlin"] = new(0, 1, 2, 3, 3), // 胖地精
        ["SneakyGremlin"] = new(0, 1, 3, 4, 4), // 鬼祟地精
        ["FossilStalker"] = new(0, 3, 4, 5, -5), // 化石潜猎者
        ["LivingFog"] = new(0, 0, 3, 3, 3), // 活雾
        ["GasBomb"] = new(0, 0, 3, 3, -7), // 毒气炸弹
        ["TwoTailedRat"] = new(0, 0, 0, 0, 0), // 双尾鼠
        ["SewerClam"] = new(0, 0, 0, 0, 0), // 下水道蚌
        ["HauntedShip"] = new(0, 3, 4, 5, 4), //幽灵船
        ["SludgeSpinner"] = new(0, 3, 3, 5, 5), // 淤泥旋螺
        ["Seapunk"] = new(0, 0, 0, 0, 0), // 海洋混混
        ["PunchConstruct"] = new(0, 3, 4, 5, 3), // 重拳构装体
        ["SkulkingColony"] = new(0, 0, 0, 0, 0), // 鬼祟珊瑚群
        ["TerrorEel"] = new(0, 0, 0, 0, 0), // 骇鳗
        ["PhantasmalGardener"] = new(0, 0, 0, 0, 0), // 花园幽灵鳗
        ["LagavulinMatriarch"] = new(0, 0, 0, 0, 0), // 乐加维林祖母
        ["SoulFysh"] = new(0, 0, 0, 0, 0), // 灵魂鱼
        ["WaterfallGiant"] = new(0, 0, 0, 0, 0), // 瀑布巨人
        


        //巢穴
        ["Tunneler"] = new(0, 10, 15, 25, 20), // 地道虫
        ["SpinyToad"] = new(0, 12, 16, 30, 35), // 棘刺蟾蜍
        ["Chomper"] = new(0, 6, 8, 13, 15), // 咬咬怪
        ["HunterKiller"] = new(0, 12, 16, 30, 0), // 猎人杀手
        ["TheObscura"] = new(0, 12, 16, 30, 20), // 膀光怪
        ["Parafright"] = new(0, 0, 0, 0, 0), // 惊麻鬼影
        ["BowlbugEgg"] = new(0, 2, 3, 4, 4), // 碗虫蛋
        ["BowlbugRock"] = new(0, 4, 5, 6, 5), // 岩壳碗虫
        ["BowlbugSilk"] = new(0, 3, 4, 5, 4), // 丝囊碗虫
        ["BowlbugNectar"] = new(0, 3, 4, 5, 4), // 花蜜碗虫
        ["LouseProgenitor"] = new(0, 13, 18, 25, 20), // 虱虫之祖
        ["SlumberingBeetle"] = new(0, 9, 14, 18, 15), // 沉睡甲虫
        ["ThievingHopper"] = new(0, 3, 5, 7, 7), // 偷窃草蜢
        ["Exoskeleton"] = new(0, 2, 5, 6, 5), // 外骨骼
        ["Ovicopter"] = new(0, 12, 15, 18, 10), // 卵翼机
        ["DecimillipedeSegment"] = new(0, 0, 0, 0, 10), // 十足蜈蚣节段
        ["DecimillipedeSegmentBack"] = new(0, 0, 0, 0, 0), // 十足蜈蚣后段
        ["DecimillipedeSegmentMiddle"] = new(0, 0, 0, 0, 0), // 十足蜈蚣中段
        ["DecimillipedeSegmentFront"] = new(0, 0, 0, 0, 0), // 十足蜈蚣前段
        ["Entomancer"] = new(0, 10, 15, 20, 15), // 蜂群术士
        ["InfestedPrism"] = new(0, 10, 20, 30, 30), // 感染棱柱
        ["Crusher"] = new(0, 15, 25, 60, 0), // 碾碎者
        ["Rocket"] = new(0, 15, 20, 30, 0), // 火箭体
        ["TheInsatiable"] = new(0, 20, 30, 60, 0), // 沙虫
        ["KnowledgeDemon"] = new(0, 20, 50, 101, 0), // 知识恶魔



        //荣耀
        ["GlobeHead"] = new(0, 15, 35, 45, 30), // 球头怪
        ["TurretOperator"] = new(0, 6, 12, 20, 25), // 炮塔操作员
        ["LivingShield"] = new(0, 8, 15, 25, 25), // 活体盾牌
        ["OwlMagistrate"] = new(0, 25, 40, 60, 40), // 猫头鹰执政官
        ["DevotedSculptor"] = new(0, 18, 30, 45, 35), // 虔信雕塑家
        ["FrogKnight"] = new(0, 20, 40, 50, 40), // 青蛙骑士
        ["SlimedBerserker"] = new(0, 30, 40, 50, 40), // 黏液狂战士
        ["TheForgotten"] = new(0, 9, 14, 20, 10), // 被遗忘者
        ["TheLost"] = new(0, 9, 14, 20, 10), // 迷失者
        ["ScrollOfBiting"] = new(0, 3, 5, 7, 5), // 啃咬卷轴
        ["Fabricator"] = new(0, 15, 25, 35, 30), // 组装师
        ["MechaKnight"] = new(0, 20, 40, 60, 50), // 机械骑士
        ["SoulNexus"] = new(0, 25, 45, 65, 40), // 灵魂枢纽

        ["FlailKnight"] = new(0, 10, 20, 30, 20), // 链锤骑士
        ["MagiKnight"] = new(0, 8, 16, 25, 20), // 法师骑士
        ["SpectralKnight"] = new(0, 9, 18, 27, 20), // 幽魂骑士
        ["Doormaker"] = new(0, 30, 60, 100, 0), // 造门者
        ["Queen"] = new(0, 30, 60, 100, 0), // 女王
        ["TestSubject"] = new(0, 20, 40, 70, 0), // 实验体



        ["Axebot"] = new(0, 6, 14, 22, 20), // 斧刃机器人
        ["CrossbowRubyRaider"] = new(0, 0, 0, 0, 0), // 弩手红袭者
        ["TrackerRubyRaider"] = new(0, 0, 0, 0, 0), // 追踪者红袭者
    };

    public static BlightBestiaryHpRule GetRule(string monsterId)
    {
        if (Rules.TryGetValue(monsterId, out var rule))
        {
            return rule;
        }

        return new BlightBestiaryHpRule(0, 0, 0, 0, 0);
    }
}