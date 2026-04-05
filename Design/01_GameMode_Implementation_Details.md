# 荒疫模式 - 游戏模式与难度系统实现详情 (Implementation Details)

基于 `01_GameMode_and_Difficulty.md` 的设计，本文档详细规划了为了实现“荒疫模式”的基础架构、难度分级和怪物AI系统所需要创建的C#文件、类结构、核心接口及挂载逻辑。

## 1. 文件结构规划 (Directory Structure)
在 `blight/Scripts/` 目录下，推荐以下文件组织方式：

```text
blight/Scripts/
  ├── Core/
  │    └── BlightModeManager.cs        // 核心管理器：状态控制、网络同步、全局难度参数
  ├── Patches/
  │    ├── MainMenuPatch.cs            // UI补丁：在主菜单添加“荒疫模式”入口
  │    ├── GameStatePatch.cs           // 状态补丁：拦截游戏开始、存档加载、网络房间属性
  │    ├── MonsterAIPatch.cs           // 战斗补丁：拦截怪物意图生成 (GetNextMove 等)
  │    ├── DifficultyStatPatch.cs      // 数值补丁：根据荒疫进阶等级修改怪物血量、伤害计算
  │    ├── MapGenerationPatch.cs       // 地图补丁：生成爬塔路线时，标记变异个体/双波次节点 (进阶1, 5)
  │    └── RunStartPatch.cs            // 开局补丁：拦截游戏开始时逻辑注入开局诅咒 (进阶3)
  └── AI/
       ├── IBlightMonsterAI.cs         // 接口：定义荒疫模式专用的怪物意图覆盖规范
       ├── BlightMonsterDirector.cs    // 调度器：负责为不同的怪物实例分发对应的AI逻辑
       └── Bestiary/                   // 具体怪物的AI覆写实现目录
            ├── BlightJawWorm.cs
            └── BlightCultist.cs
```

## 2. 核心类与接口详细设计

### 2.1 核心管理器 `BlightModeManager.cs`
该类作为单例或静态工具类，掌控全局荒疫模式的状态。

**代码规范提纲：**
```csharp
namespace BlightMod.Core {
    public static class BlightModeManager {
        // 当前是否处于荒疫模式
        public static bool IsBlightModeActive { get; set; } = false;
        
        // 荒疫进阶等级 (0 - 5)
        public static int BlightAscensionLevel { get; set; } = 0;

        // 当前所处节点的特殊状态（由地图生成时决定的该节点属性）
        public static bool CurrentNodeIsMutant { get; set; } = false;
        public static bool CurrentNodeIsDoubleWave { get; set; } = false;

        /// <summary>
        /// 游戏/战斗初始化时调用，重置状态或加载存档
        /// </summary>
        public static void InitializeFromSave(RunSaveData data) { ... }

        /// <summary>
        /// 用于联机时同步模式状态。将状态写入NetworkRoomProperties。
        /// </summary>
        public static void SyncToNetwork() { ... }

        /// <summary>
        /// 检查当前是否满足某个荒疫进阶的条件
        /// </summary>
        public static bool IsAtLeastAscension(int level) {
            return IsBlightModeActive && BlightAscensionLevel >= level;
        }
    }
}
```

### 2.2 难度数值动态补丁 `DifficultyStatPatch.cs`
原版的数据读取通常依赖 `AscensionLevel`，我们将通过 Harmony 的 Postfix 或者 Prefix 修改获取血量、伤害的方法。

**机制设计：**
*   拦截 `MegaCrit.Sts2.Core.Entities.Creatures.Creature` 的构造函数或 `SetUniqueMonsterHpValue`。
*   根据 `BlightModeManager.BlightAscensionLevel` 乘上修改后的血量倍率。

**核心思想伪代码：**
```csharp
[HarmonyPatch(typeof(Creature), MethodType.Constructor, typeof(MonsterModel), typeof(CombatSide), typeof(string))]
public class DifficultyStatPatch_Health {
    public static void Postfix(Creature __instance, MonsterModel monster) {
        if (!BlightModeManager.IsBlightModeActive) return;

        // A0基准：所有怪物血量以原版A10为基准提升(例如+15%)
        float multiplier = 1.15f; 
        
        // 荒疫高发难度额外加成
        if (BlightModeManager.IsAtLeastAscension(4) && monster.IsBoss) {
            multiplier += 0.20f; // 进阶4首领额外20%血量
        }
        
        // 需利用反射或直接修改公开属性（如可用）去设置提升后的最大生命值 _maxHp / _currentHp
        int modifiedHp = (int)(__instance.MaxHp * multiplier);
        // ReflectionHelper.SetField(__instance, "_maxHp", modifiedHp);
        // ReflectionHelper.SetField(__instance, "_currentHp", modifiedHp);
    }
}
```

### 2.3 怪物AI重构接口与调度器 (AI Override System)

为了避免 `MonsterAIPatch` 变得极其臃肿，我们引入策略模式 (Strategy Pattern) 来管理每个怪物的不同行为。

#### 接口 `IBlightMonsterAI.cs`
控制怪物特定行为重写的接口。
```csharp
namespace BlightMod.AI {
    public interface IBlightMonsterAI {
        /// <summary>
        /// 怪物ID，用于与原版怪物进行匹配绑定
        /// </summary>
        string TargetMonsterId { get; }

        /// <summary>
        /// 提供荒疫模式下重构的怪物意图状态机图谱（用于完全覆写原版状态机）
        /// </summary>
        MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel);

        /// <summary>
        /// 进阶强化回调：战斗开始时给予额外的 Buff（如进阶1的精英强化）。
        /// </summary>
        void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel);
    }
}
```

#### 调度器 `BlightMonsterDirector.cs`
负责维护怪物ID到 `IBlightMonsterAI` 实例的字典字典注册表。

```csharp
namespace BlightMod.AI {
    public static class BlightMonsterDirector {
        private static Dictionary<string, IBlightMonsterAI> _registry = new();

        public static void Register(IBlightMonsterAI ai) {
            _registry[ai.TargetMonsterId] = ai;
        }

        public static MonsterMoveStateMachine TryGenerateStateMachine(MonsterModel monster) {
            if (_registry.TryGetValue(monster.Id, out var ai)) {
                return ai.GenerateBlightStateMachine(monster, BlightModeManager.BlightAscensionLevel);
            }
            return null;
        }
    }
}
```

### 2.4 怪物动作切入点 `MonsterAIPatch.cs`
负责在原版核心代码上“剪秋水”，把逻辑导向上述的调度器中。

```csharp
namespace BlightMod.Patches {
    [HarmonyPatch(typeof(MonsterModel), "GenerateMoveStateMachine")] // STS2改为了基于状态机生成意图
    public class MonsterAIPatch {
        public static void Postfix(MonsterModel __instance, ref MonsterMoveStateMachine __result) {
            // 非荒疫模式或不需要覆写时，保留原版生成的状态机
            if (!BlightModeManager.IsBlightModeActive) return;

            // 进阶3开始（或其他需要高难AI的层级），执行更为强硬和智能的AI逻辑
            if (BlightModeManager.BlightAscensionLevel >= 3) {
                var blightStateMachine = BlightMonsterDirector.TryGenerateStateMachine(__instance);
                if (blightStateMachine != null) {
                    __result = blightStateMachine; // 直接覆写状态机图谱，让怪物走荒疫的全新连招路线
                }
            }
        }
    }
}
```

### 2.5 高阶机制拦截：变异标识与开局惩罚 (A1, A3, A5)
对应您最新设计的进阶 1（变异个体地图标识）、进阶 3（开局负面）、进阶 5（双波战地图标识）。

```csharp
namespace BlightMod.Patches {
    
    // ---- 拦截地图生成（满足 A1 和 A5） ----
    [HarmonyPatch(typeof(MapGenerator), "GenerateDungeon")] // 原版地图生成函数
    public class MapGenerationPatch {
        public static void Postfix(MapGenerator __instance) {
            if (!BlightMod.Core.BlightModeManager.IsBlightModeActive) return;

            // 遍历生成的节点，利用游戏内的 RNG 进行概率判定
            foreach (var node in __instance.Nodes) {
                if (node.Room is MonsterRoom || node.Room is EliteRoom) {
                    
                    // A1：变异个体标识
                    if (BlightMod.Core.BlightModeManager.BlightAscensionLevel >= 1) {
                        // 例如通过 BlightBalanceConfig 获取随进阶提升的概率
                        float mutantChance = BlightMod.Content.Config.BlightBalanceConfig.GetMutantChance();
                        if (RunState.MiscRng.RandomFloat() < mutantChance) {
                            node.AddMapModifier("Blight_Mutant"); // 假设原版有 Modifier 标签，或者我们自己维护一个 Dictionary
                        }
                    }

                    // A5：双波次精英/小怪标识
                    if (BlightMod.Core.BlightModeManager.BlightAscensionLevel >= 5) {
                        float waveChance = BlightMod.Content.Config.BlightBalanceConfig.DoubleWaveChance;
                        if (RunState.MiscRng.RandomFloat() < waveChance) {
                            node.AddMapModifier("Blight_DoubleWave");
                        }
                    }
                }
            }
        }
    }

    // ---- 拦截开局（满足 A3） ----
    [HarmonyPatch(typeof(RunState), "InitializeRun")] 
    public class RunStartPatch {
        public static void Postfix() {
            if (!BlightMod.Core.BlightModeManager.IsBlightModeActive) return;

            if (BlightMod.Core.BlightModeManager.BlightAscensionLevel >= 3) {
                // 执行 A3 的开局惩罚（塞入特定遗物或放入诅咒牌等）
                BlightMod.Core.RunState.Player.Deck.AddCard(new RandomCurseCard());
            }
        }
    }
}
```

## 3. UI 与启动切面 (UI & Initialization)
荒疫模式的起点在于UI，在玩家点击主菜单时触发。

*   **`MainMenuPatch.cs`**:
    *   在原版 `MainMenuScreen.Show()` 中加入按钮（通过实例化原版的 Button Prefab 并挂载事件）。
    *   点击后弹出“荒疫进阶选择面板”（可复用原版的 Ascension 选取滑块，只不过把文案改掉，范围锁住在 0-5）。
    *   启动游戏时，强行设置 `GameMode = GameMode.Standard`，但设置 `BlightModeManager.IsBlightModeActive = true` 且传入选择的 `BlightAscensionLevel`。

## 4. 下一步开发建议
1.  **打通主干：** 首先实现 `BlightModeManager` 与 `MainMenuPatch`，验证能否顺利附加一个“状态钩子”启动一局标准游戏而不崩溃。
2.  **数值渗透：** 编写 `DifficultyStatPatch`，加倍一个最弱小鸡的血量进游戏测试，如果生效，说明底层拦截成功。
3.  **铺开AI库：** 根据 `IBlightMonsterAI` 疯狂实现具体的怪物变态设计。

---

## 5. Toadpole 实战落地模板（AI + 数值 + 变异）

你当前要做的“01文档实现”可以直接套这个模板。下面示例以 `Toadpole` 为目标，强调两件事：

1. 不修改原版文件，只通过 `Harmony` Patch。
2. 所有 mod 代码写在 `blight/Scripts/` 下。

### 5.1 先确认原版锚点（只读参考）

原版 `Toadpole` 位于：

- `st-s-2/src/Core/Models/Monsters/Toadpole.cs`

本体里已经给出了你要覆写的关键点：

- 血量：`MinInitialHp` / `MaxInitialHp`
- 伤害：`SpikeSpitDamage` / `WhirlDamage`（私有 getter）
- AI：`GenerateMoveStateMachine()`

所以我们的做法是：

- 用数值 Patch 改 HP 与伤害相关输出。
- 用 AI Patch 或 AI Director 改状态机。
- 在战斗开始时根据“当前节点是否变异”追加变异 Buff 与行为分支。

### 5.2 进阶与变异的目标配置（Toadpole 示例）

建议先做一张可调参数表，后续统一从配置读取。

| 条件 | HP加值 | 伤害加成 | AI变化 |
|---|---:|---:|---|
| 荒疫 A0 | +2 | +1 | 保持原循环 |
| 荒疫 A1-A2 | +4 | +2 | 首回合优先 Buff |
| 荒疫 A3-A4 | +6 | +3 | Buff/攻击循环更激进 |
| 荒疫 A5 | +9 | +4 | 允许高压开场（直接攻击或双连） |
| 变异个体（额外叠加） | +5 | +2 | 战斗开始先获得额外荆棘，并切换到变异状态机 |

说明：

- “变异”是节点属性，不是怪物 ID；在 `RunManager.Instance.DebugOnlyGetState()?.CurrentMapPoint` 上判定。
- 你当前 `BlightModeManager.IsNodeMutant(point, seed)` 已经是确定性算法，可直接复用。

### 5.3 推荐文件拆分

建议在 `blight/Scripts/` 增加或完善以下文件（命名可按你项目风格微调）：

- `Scripts/Patches/ToadpoleStatPatch.cs`
- `Scripts/AI/ToadpoleBlightAI.cs`（你已存在，可扩展）
- `Scripts/Patches/MonsterAIPatch.cs`（你已存在，可补战前变异注入）

### 5.4 Toadpole 数值 Patch（Harmony）

下面示例展示“按荒疫进阶 + 变异”改基础血量。注意这是文档模板，你可以把倍率提取到配置类。

```csharp
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.Patches
{
    internal static class ToadpoleBlightStatHelper
    {
        public static int ScaleHp(int baseValue)
        {
            if (!BlightModeManager.IsBlightModeActive) return baseValue;

            int hpAdd = BlightModeManager.BlightAscensionLevel switch
            {
                <= 0 => 2,
                <= 2 => 4,
                <= 4 => 6,
                _ => 9
            };

            var state = RunManager.Instance?.DebugOnlyGetState();
            var point = state?.CurrentMapPoint;
            var seed = state?.Rng.StringSeed;
            bool mutant = point != null && !string.IsNullOrEmpty(seed) && BlightModeManager.IsNodeMutant(point, seed);
            if (mutant)
            {
                hpAdd += 5;
            }

            return baseValue + hpAdd;
        }
    }

    [HarmonyPatch(typeof(Toadpole), "get_MinInitialHp")]
    public static class ToadpoleMinHpPatch
    {
        public static void Postfix(ref int __result)
        {
            __result = ToadpoleBlightStatHelper.ScaleHp(__result);
        }
    }

    [HarmonyPatch(typeof(Toadpole), "get_MaxInitialHp")]
    public static class ToadpoleMaxHpPatch
    {
        public static void Postfix(ref int __result)
        {
            __result = ToadpoleBlightStatHelper.ScaleHp(__result);
        }
    }
}
```

实战里建议把 `ApplyToadpoleHpScaling` 抽成共享静态方法，避免 `Min/Max` 两份重复。

伤害也同理处理：

- 直接 Patch `Toadpole` 私有伤害 getter（`SpikeSpitDamage` / `WhirlDamage`）并在 `Postfix` 增量。
- 或者在命令层做统一伤害乘算（更通用，但需要更谨慎防止影响全局）。

### 5.5 Toadpole AI Patch（Harmony + Director）

你当前已经有：

- `Scripts/AI/BlightMonsterDirector.cs`
- `Scripts/AI/ToadpoleBlightAI.cs`
- `Scripts/Patches/MonsterAIPatch.cs`

这套结构可以继续扩展为“普通荒疫状态机 + 变异状态机”双轨：

```csharp
public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int ascensionLevel)
{
    bool mutant = ResolveCurrentNodeMutant();
    return mutant
        ? BuildMutantStateMachine(monster, ascensionLevel)
        : BuildStandardBlightStateMachine(monster, ascensionLevel);
}

public void ApplyBlightStartBuffs(MonsterModel monster, int ascensionLevel)
{
    bool mutant = ResolveCurrentNodeMutant();
    if (!mutant) return;

    // 例：变异个体开场额外荆棘/力量
    // await PowerCmd.Apply<ThornsPower>(monster.Creature, 2m, monster.Creature, null);
    // await PowerCmd.Apply<StrengthPower>(monster.Creature, 1m, monster.Creature, null);
}
```

建议行为差异（Toadpole）：

- 普通荒疫：`Buff -> Attack -> Buff -> Attack`。
- 变异个体：`HeavyAttack -> Buff -> MultiAttack`，并提高首回合压制。
- A5 可在变异状态机里提高 `MultiAttack` hitCount 或增加一次条件分支。

### 5.6 最关键的流程顺序

要避免“文档写了但实际不触发”，请确保流程顺序如下：

1. 地图层已用 `BlightModeManager.IsNodeMutant` 判定该战斗节点（你已完成）。
2. 进入战斗后，`MonsterAIPatch` 在 `SetUpForCombat` 后把 `Toadpole` 的状态机替换为荒疫版。
3. 同时调用 `TryApplyStartBuffs`，让变异个体在战斗开始时拿到额外能力。
4. 数值 Patch 对 HP/伤害 getter 生效，确保同一只怪同时具备“新面板 + 新 AI”。

做到这四步，Toadpole 就能完整体现“不同进阶 + 变异个体”的荒疫逻辑。