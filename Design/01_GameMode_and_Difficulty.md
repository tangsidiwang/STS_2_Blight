# 荒疫模式 (Blight Mode) - 游戏模式与进阶难度设计

## 1. 游戏模式接入 (GameMode)
《Slay the Spire 2》的底层原版 `GameMode` 是一个枚举 (`enum`)，由于硬编码限制，直接拓展枚举比较困难且容易引起存档数据污染。
> *"在默认模式，每日挑战，自定义模式之外添加新模式，荒疫模式（允许联机）。"*

**实现策略与可行性：**
*   **UI层接入：** 我们通过对 `MainMenuScreen` 的 Patch 增加一个额外的按钮用于进入荒疫模式。
*   **底层映射：** 对于后端的 `GameMode`，我们让其运行在 `GameMode.Standard` 或 `GameMode.Custom` 状态下，但全局使用一个静态的标识符追踪当前状态：
    ```csharp
    public static class BlightModeManager {
        public static bool IsBlightModeActive { get; set; } = false;
        public static int BlightAscensionLevel { get; set; } = 0; // 0-5
    }
    ```
*   **联机支持：** 在创建大厅/加入大厅时，我们将自定义的 `IsBlightModeActive` 与难度级别作为元数据放入网络房间属性中。其他玩家获取该房间时，同步启用本地的 `BlightModeManager`。

## 2. 难度设计 (Blight Ascension 0-5)
由于原版 `AscensionLevel` 同样被定义为枚举，荒疫模式的难度将是一套**完全独立于原版进阶**的计算体系。它的默认强度要达到“原版A10基准”。
我们将通过 Harmony Patch 原本作战/数值公式的代码。

**难度分级设计理念：**
*   **进阶 0 (基准级):** 等同于原版进阶10。敌人血量、攻击力、精英产出等方面均以此定基调。（这是开启荒疫模式的起点）
*   **进阶 1:** 概率出现变异个体，包括小怪和精英不包括boss（变异概率会随着进阶增加而增加）变异个体会直接标识在地图上
*   **进阶 2:** 精英更加致命，在精英战时精英会携带额外的随机强化词条
*   **进阶 3:** 开局会获得随机负面效果（参考自定义模式）。
*   **进阶 4:** 首领 (Boss) 会额外获得特殊荒疫能力（如禁止特定的Debuff或更高额的基础防御）。
*   **进阶 5 (最高阶段):** 概率出现俩波敌人，类似于双boss不过发生在精英或小怪上（会标识在地图上）。

## 3. 怪物意图与强化重构 (Monster AI Overhaul)
> *"所有怪物的意图和强化需要设计更改。"*

*   **策略实现：** 
    原版中怪物意图逻辑通常在 `MonsterModel` 的子类或者某个负责AI判断的组件中执行（如 `GetNextMove` 或者回合结束计算 `TakeTurn`）。
    我们将在这些核心判定方法上布置前置补丁 (Prefix Patch)。
    如果 `BlightModeManager.IsBlightModeActive == true`，拦截原生AI计算逻辑，跳入我们的专属AI类中：

    ```csharp
    [HarmonyPatch(typeof(AbstractMonster), "GetNextMove")]
    public class Patch_Monster_AI {
        public static bool Prefix(AbstractMonster __instance) {
            if (BlightModeManager.IsBlightModeActive) {
                // 执行荒疫模式定制版AI
                BlightMonsterAI.SetBlightIntent(__instance, BlightModeManager.BlightAscensionLevel);
                return false; // 跳过原版逻辑
            }
            return true;
        }
    }
    ```
*   **内容规划：** 为常见的每个怪物单独编写配置或定制类，增加如“附魔剥夺”、“污染牌库”等符合“荒疫”主题的新能力和强力意图。