# 附魔系统核心 (Enchantment System & Stacking)

## 1. 目标与硬约束

本系统的目标是：

1. 支持单卡多重附魔（最多 5 条）。
2. 左侧显示多个附魔标签（每条一个标签，带图标）。
3. 标签按附魔稀有度显示不同颜色。
4. 该功能仅在荒疫模式生效，普通模式完全不受影响。

必须遵守的原版约束：

1. 原版 `CardModel` 只有一个 `Enchantment` 字段，不是 `List`。
2. 原版渲染 `NCard` 只有一个 `Enchantment` UI 节点。
3. 原版存档 `SerializableCard` 也只有一个 `enchantment` 字段。

因此不能直接把卡改造成 `card.Enchantments` 列表，而要走兼容方案。

## 2. 兼容实现总思路

### 2.1 数据层：复合附魔桥接

引入 `BlightCompositeEnchantment : EnchantmentModel`，让卡牌表面上仍只有 1 个 `Enchantment`，但内部持有多条子附魔。

核心规则：

1. 当卡牌没有附魔时，首次注入就设置为 `BlightCompositeEnchantment`。
2. 以后每次新增附魔，都追加到 Composite 的 `Entries` 里。
3. 每个条目保留独立身份，不把同类条目强行合并到单一 `Amount`。
4. 总条目数上限固定为 `5`。

推荐条目结构：

```csharp
public sealed class BlightEnchantmentEntry {
    public ModelId EnchantmentId { get; set; }
    public int Amount { get; set; }
    public BlightEnchantmentRarity Rarity { get; set; }
    public bool IsNegative { get; set; }
}
```

说明：

1. 复合附魔内部按 `ModelId` + `Amount` 还原子附魔实例（运行时可缓存）。
2. `BlightCompositeEnchantment` 负责在 `EnchantDamageAdditive/Multiplicative`、`EnchantBlock...`、`EnchantPlayCount`、`OnPlay` 等入口聚合所有子附魔效果。

### 2.2 UI 层：左侧多标签

原版只有一个左侧标签节点，所以要在 `NCard` 渲染后动态生成额外标签：

1. 保留原版 `Enchantment` 标签作为“第一个标签”或模板来源。
2. 通过 Patch 在 `NCard.UpdateEnchantmentVisuals()` 后执行 `BlightEnchantmentUiRenderer.RenderTabs(...)`。
3. 若当前卡不是 `BlightCompositeEnchantment`，清理并退出。
4. 若是 Composite，则按条目数量在左侧纵向堆叠多个标签。
5. 每个标签显示：图标 + 数值（若该附魔 `ShowAmount` 为 true）。
6. 标签底色/描边按稀有度映射。

建议颜色映射（可后续美术微调）：

1. `Common`: `#8FA0B0`
2. `Uncommon`: `#4FB36C`
3. `Rare`: `#D9A34C`
4. `Negative`: `#C55555`

### 2.3 模式限定：仅荒疫生效

所有入口统一先判断：

```csharp
if (!BlightModeManager.IsBlightModeActive) return;
```

要求：

1. 附魔注入逻辑只在荒疫模式执行。
2. 多标签渲染只在荒疫模式执行。
3. 非荒疫模式下保持原版单标签、单附魔逻辑。

## 3. 附魔池与掉落规则

### 3.1 池分类

1. `Common`
2. `Uncommon`
3. `Rare`
4. `Negative`

并按卡牌类型过滤（如攻击牌不能抽只对技能生效的附魔）。

### 3.2 掉落规则

1. 普通战：默认 1 条，主要 `Common`，少量 `Uncommon/Rare`。
2. 精英/首领：默认 1 条，保底 `Uncommon`，更高概率出 `Rare`。
3. 变异节点：
   1. 所有奖励卡额外 +1 条。
   2. 指定 1 张为“风险收益牌”：`Rare + Negative`。

### 3.3 RNG 规范

必须使用 run 内 RNG，避免 SL 刷变：

1. 使用 `RunManager.Instance.RunState.Rng` 对应流（建议 `CombatCardGeneration` 或专用流）。
2. 禁止使用 `new Random()` 临时随机。

## 4. 接入点（以真实 API 为准）

1. 奖励卡附魔注入：Patch `CardReward.Populate()` 后置。
2. 手动附魔/事件附魔注入：统一走 `BlightEnchantmentManager.TryApply(...)`，不要散落直接写卡模型。
3. 卡面标签更新：Patch `NCard.UpdateEnchantmentVisuals()` 后置。

## 5. 稳定性与兼容性

1. 不修改原版源码，只使用 Harmony Patch + blight 自己的类。
2. 不破坏原版单附魔字段结构，避免存档/网络结构全面重写。
3. Composite 需要可序列化（至少可通过 `Props` 保存条目数据）。
4. 对于无法聚合的特殊附魔，先列入黑名单，避免出现行为错误。

## 6. 验收标准

1. 荒疫模式：同一张卡可稳定叠到最多 5 条附魔。
2. 荒疫模式：卡牌左侧显示多个标签，数量与条目一致。
3. 荒疫模式：不同稀有度标签颜色不同，负面附魔可一眼识别。
4. 非荒疫模式：一切表现与原版一致。
5. 存档读档后，多重附魔条目与显示保持一致。