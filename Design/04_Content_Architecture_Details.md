# 荒疫模式 - 内容扩展架构底层规划 (Content Architecture Details)

> **注：** 本文档旨在为您未来的“遗物、卡牌、附魔”设计提供一套**高扩展性、易于配置的底层注册与构建框架**。具体的物品效果、名称将由您后续亲自设计，本框架只提供“如何把它们安全且规范地加入游戏”的技术范式。

## 1. 文件结构与扩展槽规划

为了保证未来随着您设计出几十乃至上百个新物品时代码不至于混乱，我们将在 `Scripts/Content/` 目录下建立专用的模块化注册表：

```text
blight/Scripts/
  └── Content/
       ├── BlightContentRegistry.cs      // 全局中枢：负责在MOD加载时统一向游戏本体注册所有新内容
       ├── Config/
       │    └── BlightBalanceConfig.cs   // 数值配置：集中管理所有附魔的发生几率、遗物参数（方便随时调整平衡）
       ├── Relics/
       │    ├── BlightRelicBase.cs       // 自定义遗物基类（提供荒疫阶段特有钩子）
       │    └── (未来你编写的具体遗物类).cs
       ├── Cards/
       │    ├── BlightCardBase.cs        // 自定义卡牌基类
       │    └── (未来你编写的具体卡牌类).cs
       └── Enchantments/
            └── (未来你编写的具体附魔类继承，详见02文档)
```

## 2. 核心架构与接口设计

### 2.1 集中注册枢纽 (`BlightContentRegistry.cs`)
原版游戏在启动时会通过特定的工厂或注入方法读取卡牌与遗物。我们需要一个统一的注册器，在 MOD 初始化阶段（如 `OnModLoaded` 或被原版启动回调时）批量派发你的创作。

```csharp
namespace BlightMod.Content {
    public static class BlightContentRegistry {
        
        /// <summary>
        /// Mod主入口在初始化时调用此方法
        /// </summary>
        public static void RegisterAll() {
            RegisterRelics();
            RegisterCards();
            RegisterEnchantmentsToPool();
        }

        private static void RegisterRelics() {
            // 例：伪代码，调用原版API或管理器的注入方法
            // CustomRelicHelper.AddBlightRelic(new YourFutureRelic1());
            // CustomRelicHelper.AddBlightRelic(new YourFutureRelic2());
        }

        private static void RegisterCards() {
            // 例：向游戏所有的牌库或指定的“荒疫卡池”写入卡牌
            // CustomCardHelper.AddBlightCard(new YourFutureCard1());
        }

        private static void RegisterEnchantmentsToPool() {
            // 这里对接 02 文档里的附魔管理器
            // EnchantmentPool.Register(EnchantmentRarity.Common, typeof(YourFutureEnchant1));
            // EnchantmentPool.Register(EnchantmentRarity.Rare, typeof(YourFutureEnchant2));
        }
    }
}
```

### 2.2 荒疫专属遗物基类 (`BlightRelicBase.cs`)
除了继承原版的 `Relic`，我们为荒疫模式提供一个专属的基类。这让你的自定义遗物可以监听“附魔”相关的特有事件，或者限制它只在荒疫模式里生效/掉落。

```csharp
namespace BlightMod.Content.Relics {
    public abstract class BlightRelicBase : MegaCrit.Sts2.Core.Relics.Relic {
        
        protected BlightRelicBase(string id) : base(id) {
            // 确保这些遗物被标记为一种特定的稀有度或“自定义池”
        }

        /// <summary>
        /// 原版可能不支持“当卡牌被附魔时”的回调钩子。
        /// 这里我们预留专属事件，当玩家在03文档的三选一锻造里给卡附魔时，
        /// BlightEnchantmentManager 可以遍历所有玩家遗物并触发该方法。
        /// </summary>
        public virtual void OnCardEnchanted(AbstractCard card, IBlightEnchantment enchantment) {
            // 默认无动作，子类遗物可覆写以实现诸如“每次附魔回5血”的逻辑
        }

        /// <summary>
        /// 控制遗物是否能够掉落：默认只有荒疫模式下才会出现
        /// </summary>
        public override bool CanSpawn() {
            return BlightMod.Core.BlightModeManager.IsBlightModeActive && base.CanSpawn();
        }
    }
}
```

### 2.3 数值参数与平衡管理 (`BlightBalanceConfig.cs`)
未来你会设计大量的附魔和遗物，它们的叠加上限、增加的伤害值如果不统一管理，后期维护会非常痛苦。建议采用统一的静态类或读取外部 Json：

```csharp
namespace BlightMod.Content.Config {
    /// <summary>
    /// 荒疫模式所有常数与几率的中央控制台
    /// 可视需要在后续支持从 JSON 文件读取，方便甚至不改代码直接热更数据。
    /// </summary>
    public static class BlightBalanceConfig {
        
        // --- 附魔系统相关参数 ---
        public static float BaseEnchantChance = 0.70f;     // 单条附魔 70%
        public static float DoubleEnchantChance = 0.25f;   // 第二条附魔 25%
        public static float TripleEnchantChance = 0.05f;   // 第三条附魔 5%
        
        public static int MaxEnchantStackLimit = 5;        // 绝对叠加安全上限上限
        
        // --- 篝火强化数值 ---
        public static float CampfireHealPercent = 0.50f;   // 营地恢复 50%
        public static int CampfireHealFlatBonus = 10;      // 营地额外恢复 10点
        
        // --- 难度挂钩与事件概率 ---
        // (进阶A的倍率, 精英的强化机制配置等)
        public static float Ascension0_HealthMulti = 1.15f; 
        
        // 变异相关 (Ascension 1+)
        public static float BaseMutantChance = 0.15f; // 初始进阶1的变异概率 (15%)
        public static float MutantChancePerAscension = 0.05f; // 此后每级进阶额外增加5%
        
        public static float GetMutantChance() {
            int extraLevels = BlightMod.Core.BlightModeManager.BlightAscensionLevel - 1;
            return BaseMutantChance + (extraLevels > 0 ? extraLevels * MutantChancePerAscension : 0f);
        }

        // 双波战 (Ascension 5)
        public static float DoubleWaveChance = 0.20f; // 必定为20%几率
    }
}
```

## 3. 面向您未来的工作流
当您准备好具体的“遗物”、“卡牌”或“附魔”设计点子时，您的工作流将是非常清爽的：

1. **写配置：** 在 `BlightBalanceConfig.cs` 加一条你要用到的数值（例如：`public static int MyNewEnchantDamage = 4;`）。
2. **建实体：** 在对应的文件夹（例如 `Content/Relics/`）新建一个类继承 `BlightRelicBase`（或卡牌/附魔的 Base 接口）。
3. **注册它：** 到 `BlightContentRegistry` 里加一行 `Register(...)`，进游戏它就自动生效了。

不需要每次设计新东西都去改底层核心框架、找掉落相关的Patch逻辑——它们已经在 01、02、03 中完全解耦！