# 荒疫模式 - 战利品、锻造与篝火机制重构

## 1. 荒疫附魔战利品 (Combat Card Rewards)
> *"战斗获胜的卡牌奖励中每张牌会有随机的附魔（前提是符合附魔规则）"*

*   **实现原理**：战斗结束后，游戏生成 `CardReward`，并在其中调用牌库系统生成选牌列表（如 `GenerateCardOptions`）。
*   **修改切入点**：通过 Harmony Postfix 到 `CardReward.GetCards()` 或是针对 `RewardItem.Update()` 的生成逻辑。
    1.  当 `BlightMod.IsBlightModeActive == true`，在原版卡牌生成完成后，立刻调用 `BlightEnchantmentManager.AttemptEnchantCards(List<Card>)`。
    2.  我们的管理器会根据之前定义的附魔池和几率（参考 `02_Enchantment_System.md`）进行多重随机抽取。
    3.  玩家在挑选卡牌时，可以直接预览它们携带的附魔（UI需保证原样支持）。

## 2. 新奖励类型：锻造奖励 (Forge Reward)
> *"锻造奖励会出现在精英怪和变异个体的战利品中，锻造奖励也会分等级... 三个选项前俩个选项为给卡牌附魔，第三个选项为给个遗物或者敲牌或删牌"*

*   **生成规则与掉落几率**：
    “锻造奖励”本身被划分为不同的**奖励等级**（决定了里面附魔的品质和遗物/删牌的价值）：
    *   **精英怪 (Elite)**：必掉锻造奖励。质量大概率为“普通 (Common)”，小概率出现“罕见 (Uncommon)”或“稀有 (Rare)”。
    *   **变异小怪 (Mutant Minion)**：必掉。质量机制同行，大概率普通，小概率罕见/稀有。
    *   **变异精英 (Mutant Elite)**：必掉。质量经过极大幅度提高：**保底为罕见 (Uncommon)**，大概率出现“稀有 (Rare)”。

*   **展示界面 (基于 KnowledgeDemon UI)**：
    该奖励类型会作为独特条目出现在战斗结算页面中。点击“锻造奖励”后，弹出三选一界面。
    *   **UI复用策略**：我们不从0到1地搓Godot节点，而是利用原生游戏里**知识恶魔 (Knowledge Demon)**的意图/二选一选择界面作为基础框架。拦截其 `ChoiceScreen` 或者相关 UI Prefab，扩展为三个选项。

*   **选单逻辑与选项结构 (永远为三选一)**：
    1.  **选项A（卡牌附魔 1）**：提供一个可视化的特定类别附魔（受锻造奖励等级影响，例如：附加一个“罕见”品质伤害类附魔）。玩家点击后打开卡组选牌附加。
    2.  **选项B（卡牌附魔 2）**：提供另一个方向的附魔选项（例如：附加一个“罕见”品质的保留类或防御类附魔）。
    3.  **选项C（通用收益 / Utility）**：该选项是一个非附魔类的“变数盲盒”，由系统随机从以下3种情况中抽出展示：
        *   **获取遗物**（从荒疫模式专属或常规遗物池盲抽）。
        *   **敲牌 (Smith)**（打开牌库选择一张牌进行常规升级）。
        *   **删牌 (Remove)**（打开牌库将一张牌永久移出卡组）。

## 3. 增强版篝火 (Campfire Buffs)
> *"火堆休息可以恢复更多的生命值。"*

*   **实现原理**：原版游戏使用 `RestSiteOption` 类体系（如 `HealRestSiteOption`）。其具体的回复血量公式在 `GetHealAmount(Player)` 或相关获取属性中动态提供，执行过程彻底**异步化** (`Task<bool> OnSelect`)。
*   **修改切入点**：
    由于随意打断或拦截异步逻辑 (`ExecuteRestSiteHeal`) 非常容易导致软死机（卡死剧情/UI协程），我们应当直接拦截取数值的地方。
    若处于荒疫模式下：
    ```csharp
    [HarmonyPatch(typeof(HealRestSiteOption), "GetHealAmount")]
    public class Patch_Campfire_Heal_Amount {
        public static void Postfix(Player player, ref int __result) {
            if (BlightModeManager.IsBlightModeActive) {
                // 原版默认为 MaxHp * 0.3。我们通过补丁将其提高为 50% + 10 点
                int newAmount = (int)(player.MaxHp * 0.5f) + 10;
                __result = newAmount;
            }
        }
    }
    ```
*   **附加收益与代价**：也可以后缀拦截 `ExecuteRestSiteHeal` 函数，在原本的异步治愈任务执行完成后，顺手把负面效果推入 `RunState`，比如血量虽然回得多但造成最大生命值轻微下降，以此深化这套荒疫主题。