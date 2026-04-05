# 附魔系统核心系统 - 接口与代码实现规划 (Enchantment System Details)

本文给出可直接实现的技术方案，满足以下需求：

1. 单卡支持多重附魔（最多 5 条）。
2. 卡牌左侧显示多个附魔标签（每条一个，图标+数值）。
3. 标签按稀有度着色。
4. 多重附魔功能仅在荒疫模式启用。

## 1. 文件结构规划

```text
blight/Scripts/Enchantments/
  ├── Enums/
  │   └── BlightEnchantmentRarity.cs
  ├── Data/
  │   └── BlightEnchantmentEntry.cs
  ├── Interfaces/
  │   └── IBlightEnchantment.cs
  ├── Models/
  │   └── BlightCompositeEnchantment.cs
  ├── Core/
  │   ├── BlightEnchantmentManager.cs
  │   ├── BlightEnchantmentPool.cs
  │   └── BlightEnchantmentUiRenderer.cs
  ├── Patches/
  │   ├── CardRewardPopulatePatch.cs
  │   ├── NCardEnchantmentVisualPatch.cs
  │   └── CardCmdEnchantPatch.cs
  └── CustomEnchantments/
      ├── BlightDamageEnchantment.cs
      ├── BlightBlockEnchantment.cs
      ├── BlightDoublePlayEnchantment.cs
      └── ...
```

## 2. 核心数据结构

### 2.1 附魔稀有度枚举

```csharp
namespace BlightMod.Enchantments;

public enum BlightEnchantmentRarity {
    Common,
    Uncommon,
    Rare,
    Negative,
}
```

### 2.2 附魔条目

```csharp
namespace BlightMod.Enchantments;

public sealed class BlightEnchantmentEntry {
    public ModelId EnchantmentId { get; set; }
    public int Amount { get; set; }
    public BlightEnchantmentRarity Rarity { get; set; }
    public bool IsNegative { get; set; }
}
```

### 2.3 扩展接口

```csharp
namespace BlightMod.Enchantments;

public interface IBlightEnchantment {
    BlightEnchantmentRarity Rarity { get; }
    bool CanApplyTo(CardModel card);
    bool AllowDuplicateInstances { get; }
    int? MaxDuplicateInstancesPerCard { get; }
}
```

### 2.4 复合附魔模型

`BlightCompositeEnchantment : EnchantmentModel` 是全系统关键桥接层。

职责：

1. 在原版单槽 `CardModel.Enchantment` 内承载多条附魔。
2. 持有 `List<BlightEnchantmentEntry>`。
3. 在 `EnchantDamageAdditive/Multiplicative`、`EnchantBlock...`、`EnchantPlayCount`、`OnPlay` 聚合子附魔。
4. 通过 `Props` 序列化/反序列化条目，确保存档可恢复。

伪代码：

```csharp
public sealed class BlightCompositeEnchantment : EnchantmentModel {
    public const int MaxEntries = 5;
    private readonly List<BlightEnchantmentEntry> _entries = new();

    public IReadOnlyList<BlightEnchantmentEntry> Entries => _entries;

    public bool TryAddEntry(BlightEnchantmentEntry entry) {
        if (_entries.Count >= MaxEntries) return false;
        _entries.Add(entry);
        RecalculateValues();
        return true;
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props) {
        decimal total = 0m;
        foreach (var sub in BuildRuntimeSubEnchantments()) {
            total += sub.EnchantDamageAdditive(originalDamage + total, props);
        }
        return total;
    }

    // 其余聚合方法同理
}
```

## 3. 核心管理器

`BlightEnchantmentManager` 负责统一入口，避免逻辑散落。

关键方法：

1. `TryApply(CardModel card, EnchantmentModel enchantment, int amount, BlightEnchantmentRarity rarity, bool isNegative)`
2. `TryApplyRandomByPool(CardModel card, bool isElite, bool isMutant)`
3. `GetEntries(CardModel card)`

规则：

1. 先判断 `BlightModeManager.IsBlightModeActive`，否则直接返回 false。
2. 卡无附魔时，先挂载 Composite，再追加条目。
3. 卡已有非 Composite 原版附魔时：
   1. 转换为 Composite（把原附魔迁入第 1 条 entry）。
   2. 再追加新 entry。
4. 总条目数超过 5 时拒绝注入。

## 4. 附魔池与 Roll

`BlightEnchantmentPool`：

1. 维护 `Common/Uncommon/Rare/Negative` 四个池。
2. 按卡类型过滤（攻击牌/技能牌/能力牌兼容性）。
3. Roll 使用 run 内 RNG，推荐：
   1. `RunManager.Instance.RunState.Rng.CombatCardGeneration.NextFloat()`
   2. `NextInt(min, max)`

概率建议：

1. 普通战：`Common 80%` / `Uncommon 15%` / `Rare 5%`
2. 精英战：`Uncommon 70%` / `Rare 30%`
3. 变异战加成：额外 +1 条；并选 1 张卡额外注入 `Rare + Negative`

## 5. Harmony 补丁点（真实入口）

### 5.1 `CardReward.Populate` 后置

用途：奖励卡生成后立即注入附魔。

```csharp
[HarmonyPatch(typeof(CardReward), nameof(CardReward.Populate))]
public static class CardRewardPopulatePatch {
    public static void Postfix(CardReward __instance) {
        if (!BlightModeManager.IsBlightModeActive) return;

        var cards = __instance.Cards?.ToList();
        if (cards == null || cards.Count == 0) return;

        bool isElite = RunManager.Instance?.RunState?.CurrentMapPoint?.PointType == MapPointType.Elite;
        bool isMutant = ResolveCurrentNodeMutant();

        int cursedIndex = isMutant ? RollIndex(cards.Count) : -1;
        for (int i = 0; i < cards.Count; i++) {
            var card = cards[i];
            BlightEnchantmentManager.TryApplyRandomByPool(card, isElite, isMutant);
            if (isMutant) {
                BlightEnchantmentManager.TryApplyRandomByPool(card, isElite, isMutant);
                if (i == cursedIndex) {
                    BlightEnchantmentManager.TryApplySpecific(card, BlightEnchantmentRarity.Rare);
                    BlightEnchantmentManager.TryApplySpecific(card, BlightEnchantmentRarity.Negative);
                }
            }
        }
    }
}
```

### 5.2 `NCard.UpdateEnchantmentVisuals` 后置

用途：在卡牌左侧渲染多个附魔标签。

```csharp
[HarmonyPatch(typeof(NCard), "UpdateEnchantmentVisuals")]
public static class NCardEnchantmentVisualPatch {
    public static void Postfix(NCard __instance) {
        if (!BlightModeManager.IsBlightModeActive) return;
        BlightEnchantmentUiRenderer.RenderTabs(__instance);
    }
}
```

### 5.3 `CardCmd.Enchant` 前置/替换

用途：当其他系统试图对卡附魔时，荒疫模式接管为 Composite 逻辑，避免原版“单附魔冲突异常”。

策略：

1. 仅荒疫模式拦截。
2. 调用 `BlightEnchantmentManager.TryApply(...)`。
3. 成功后跳过原方法。
4. 非荒疫模式继续原版流程。

## 6. 多标签 UI 细节

`BlightEnchantmentUiRenderer`：

1. 从 `NCard` 的原始 `EnchantmentTab` 复制模板节点。
2. 子标签命名：`BlightEnchantTab_0..4`。
3. 布局：左侧纵向堆叠，间距 `36~42`（按实际视觉微调）。
4. 图标：子附魔的 `Icon`。
5. 数值：子附魔 `DisplayAmount`（若 `ShowAmount`）。
6. 稀有度颜色：
   1. Common `#8FA0B0`
   2. Uncommon `#4FB36C`
   3. Rare `#D9A34C`
   4. Negative `#C55555`

注意：

1. 每次刷新先清理旧的 `BlightEnchantTab_*`，防止重复堆叠。
2. 若不是 Composite，清理并退出。

## 7. 荒疫模式限定原则

必须在以下三层都判定 `IsBlightModeActive`：

1. 注入层（CardReward/事件/控制台附魔）。
2. 数据层（TryApply 入口）。
3. UI 层（NCard 渲染扩展）。

确保普通模式零影响。

## 8. 开发顺序建议

1. 实现 `BlightCompositeEnchantment` + `BlightEnchantmentEntry` + 序列化。
2. 实现 `BlightEnchantmentManager.TryApply`（先无 UI）。
3. 补丁接入 `CardReward.Populate`，验证奖励注入。
4. 实现 `BlightEnchantmentUiRenderer` + `NCard` 后置补丁。
5. 增加 `CardCmd.Enchant` 接管补丁，覆盖外部附魔来源。
6. 做存档/读档回归测试。

## 9. 验收清单

1. 荒疫模式下，奖励卡可出现 2~5 条附魔。
2. 同名附魔可重复存在，效果按多条目聚合。
3. 左侧出现多个标签，且颜色能反映稀有度。
4. 退出荒疫模式后，原版附魔行为与显示完全不变。
5. 读档后条目数量、效果、UI 标签一致。

## 10. 如何新增一个附魔（实操）

本节用于指导后续快速新增自定义附魔，并接入本多重附魔系统。

### 10.1 新增步骤

1. 在 `blight/Scripts/Enchantments/CustomEnchantments/` 下创建新类，继承 `EnchantmentModel`。
2. 实现基础规则：
   1. `CanEnchant(CardModel)` 或 `CanEnchantCardType(CardType)`。
   2. 至少一个效果方法：`EnchantDamageAdditive` / `EnchantBlockAdditive` / `EnchantPlayCount` / `OnPlay`。
3. 实现 `IBlightEnchantment`，声明：
   1. `Rarity`
   2. `AllowDuplicateInstances`
   3. `MaxDuplicateInstancesPerCard`
4. 在 `BlightEnchantmentPool` 把该类型加入对应池。
5. 增加本地化文案：
   1. `enchantments.<id>.title`
   2. `enchantments.<id>.description`
6. 添加图标资源，命名与 `id` 保持一致。
7. 运行并验证：荒疫模式下可掉落、可叠加、左侧多标签显示正确。

### 10.2 最小模板（可复制）

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments;

public sealed class BlightBleedEdgeEnchantment : EnchantmentModel, IBlightEnchantment
{
    public override bool ShowAmount => true;

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack())
        {
            return 0m;
        }
        return Amount;
    }

    // ===== IBlightEnchantment =====
    public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

    public bool CanApplyTo(CardModel card)
    {
        return CanEnchant(card);
    }

    public bool AllowDuplicateInstances => true;

    public int? MaxDuplicateInstancesPerCard => 3;
}
```

### 10.3 常见坑位

1. 忘记加入池：类写好了但永远不会掉落。
2. 忘记本地化键：卡面或悬浮提示显示异常文本。
3. 图标缺失：会回退到 missing icon。
4. 未做模式门禁：普通模式也被注入附魔。
5. `Amount` 与数值类型不一致：涉及 `decimal` 时注意显式处理。
6. 特殊附魔未做兼容：若无法安全聚合，先放黑名单。

### 10.4 新附魔接入完成定义

满足以下 6 条视为接入完成：

1. 编译通过。
2. 荒疫模式下可在奖励中出现。
3. 与其他附魔可共存（不冲掉）。
4. 重复获得时遵循 `AllowDuplicateInstances` 与上限规则。
5. 左侧标签正确显示图标、数量、稀有度颜色。
6. 存档后读档仍然保留条目与表现。