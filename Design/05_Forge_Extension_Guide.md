# 05 Forge Extension Guide

本文档基于当前代码实现（2026-03）说明如何：

1. 创建新附魔（含图标、标签数字显示）。
2. 把附魔注册到卡牌奖励池或锻造池（两套池已分离）。
3. 调整附魔稀有度与锻造奖励稀有度概率。
4. 创建新的锻造第三选项（功能奖励）。

路径均为 `blight/` 下相对路径。

---

## 1. 当前系统总览（你改配置前先看）

- 附魔池已分离：
  - 卡牌奖励池：`CardRewardPoolByRarity`
  - 锻造奖励池：`ForgePoolByRarity`
  - 文件：`Scripts/Enchantments/Core/BlightEnchantmentPool.cs`
- 卡牌奖励附魔入口：
  - `Scripts/Enchantments/Patches/CardRewardPopulatePatch.cs`
  - `BlightEnchantmentManager.TryApplyRandomByPool(...)`（走卡牌奖励池）
- 锻造附魔入口：
  - `Scripts/Rewards/ForgeOptions/ForgeOptionAddEnchant.cs`
  - `BlightEnchantmentManager.TryApplySpecificFromForge(...)`（走锻造池）
- 锻造奖励（三选一）来源：
  - `Scripts/Rewards/ForgeOptions/ForgeOptionPool.cs`
  - 前两张附魔，第三张功能奖励（Utility）。

---

## 2. 创建新附魔（含图标与数字）

### 2.1 新建附魔类

在 `Scripts/Enchantments/CustomEnchantments/` 新建类，继承 `EnchantmentModel, IBlightEnchantment`。

最小模板：

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Enchantments.CustomEnchantments
{
    public sealed class BlightExampleEnchantment : EnchantmentModel, IBlightEnchantment
    {
        public override bool ShowAmount => true;

        // 需要固定显示数字时覆盖 DisplayAmount（例如显示 3）
        public override int DisplayAmount => 3;

        public override bool CanEnchantCardType(CardType cardType)
        {
            return cardType == CardType.Attack;
        }

        public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
        {
            if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            {
                return 0m;
            }

            return 3m;
        }

        public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Uncommon;

        public bool CanApplyTo(CardModel card)
        {
            return card != null && CanEnchantCardType(card.Type);
        }

        public bool AllowDuplicateInstances => true;

        public int? MaxDuplicateInstancesPerCard => null;
    }
}
```

### 2.1.1 多重附魔安全写法（强烈建议先看）

在 `Blight` 里新增附魔时，**不要直接照抄原版附魔实现**。原版很多附魔默认假设：

1. 卡只有一个附魔槽
2. 附魔对象本身是稳定唯一实例
3. `CanEnchant(card)` 可以直接作为所有场景的判定

而 `Blight` 现在是 `BlightCompositeEnchantment` 多条目结构，因此建议遵守以下规则：

#### 规则 A：`CanApplyTo` 不要默认依赖 `base.CanEnchant(card)`

不要默认这样写：

```csharp
return card != null && base.CanEnchant(card);
```

也不要间接这样写：

```csharp
return card != null && CanEnchant(card);
```

如果你的 `CanEnchant(card)` 内部又调用了 `base.CanEnchant(card)`，当卡已有别的附魔时就很容易出问题。

推荐写法是自己明确列出业务条件：

```csharp
public bool CanApplyTo(CardModel card)
{
  if (card == null || !CanEnchantCardType(card.Type))
  {
    return false;
  }

  return card.Pile == null
    || card.Pile.Type != PileType.Deck
    || !card.Keywords.Contains(CardKeyword.Unplayable);
}
```

如果你要禁止重复，再单独补：

```csharp
!BlightEnchantmentRuntimeHelper.HasEnchantment<YourEnchant>(card)
```

#### 规则 B：不要让 UI 显示条件依赖附魔自己会改变的状态

例如：

1. `Instinct` 会把牌降到 `0` 费
2. 所以标签显示条件不能写成“当前费用必须 > 0”

否则会出现：

1. 附魔还在
2. 悬浮说明还在
3. 只有左侧标签消失

#### 规则 C：有 `OnEnchant()` 副作用的附魔要特别小心

如果附魔会：

1. `Card.AddKeyword(...)`
2. `EnergyCost.UpgradeBy(...)`
3. 修改 `DynamicVars`

那么它不只是“数值条目”，而是“会真正改变卡牌状态”的附魔。

这类附魔必须兼容：

1. 新增附魔时的复合附魔路径
2. 读档恢复复合附魔条目路径

否则会出现“条目存在但卡没变”的情况。

#### 规则 D：不要访问不存在的 `DynamicVars`

如果你在 `RecalculateValues()` 里写：

```csharp
base.DynamicVars.Block.BaseValue = 2m;
```

那你必须真的声明了对应的 `CanonicalVars`。

否则在奖励随机到这个附魔、执行 `ModifyCard()` 时，会直接抛异常，中断整条奖励附魔流程。

#### 规则 E：战斗内状态不要只放在 runtime 子附魔字段里

如果你的附魔像 `Momentum` 这类会“打出一次累一次”，不要轻易只写：

```csharp
private int _extraDamage;
```

因为复合附魔子项在运行时可能重建，字段值会丢。

优先考虑：

1. 从 `CombatManager.Instance.History` 推导
2. 从卡稳定状态推导
3. 或者明确持久化

#### 规则 F：永久成长优先回写到复合条目，不要只改 runtime 子附魔

这次 `Goopy` 暴露了一个很典型的问题：

1. 单附魔时，`base.Amount++` 往往就够了
2. 复合附魔时，子附魔是 `BlightCompositeEnchantment` 临时构建出来的 runtime 对象
3. 如果你只改 runtime 子附魔的 `Amount`，下次重建时增长就会丢失

典型症状：

1. 打出当场可能看起来生效
2. 离开战斗或刷新显示后丢失
3. 牌库版本看不到永久成长

因此如果你的附魔会：

1. 打出后永久加伤
2. 打出后永久加格挡
3. 消耗/计数后永久成长

那么必须确保最终状态回写到：

1. `BlightCompositeEnchantment.Entries[i].Amount`
2. 必要时同步 `DeckVersion`

本次修复后，`BlightCompositeEnchantment` 已经会在多个运行时回调后，把子附魔的 `Amount` 回写到真实 entry。新增同类附魔时，**优先复用 `Amount` 作为永久成长值**，不要再单独造一套私有字段。

#### 规则 G：会改卡面关键词/属性的附魔，必须保证“可重复重放”

这次 `SoulsPower` 与 `RoyallyApproved` 的问题本质上是：

1. 附魔条目本身存下来了
2. 但 `SL` 后卡牌属性没有被重新施加

原因是这些附魔依赖 `OnEnchant()` 副作用：

1. `SoulsPower`：移除 `Exhaust`
2. `RoyallyApproved`：增加 `Innate` 和 `Retain`
3. `Goopy`：增加 `Exhaust`

如果你的附魔也会做类似事情，就要满足下面要求：

1. `OnEnchant()` 执行多次时结果应稳定
2. 读档后重新 `ModifyCard()` 不应产生错误累加
3. 不要把“只会执行一次”当成前提

当前 `BlightCompositeEnchantment` 已在外层 `OnEnchant()` 中统一重放所有子附魔的 `ModifyCard()`，所以未来新增此类附魔时，只要你的 `OnEnchant()` 是幂等或至少可安全重放，通常就不会再出现这次 `SL` 后属性消失的问题。

#### 规则 H：涉及牌库显示或跨战斗永久变化时，必须同步 `DeckVersion`

如果你的附魔效果要求：

1. 跨战斗保留
2. 在牌库里能看到变化
3. 存档再读档后仍保持

那你不能只改战斗内那张卡，还要考虑 `DeckVersion`。

这次 `Goopy` 的永久格挡增长就属于此类。

错误写法通常像这样：

```csharp
base.Card.DeckVersion.Enchantment.Amount++;
```

它在单附魔时可能凑巧可用，但在复合附魔时很容易：

1. 加到错误对象
2. 根本找不到对应子附魔
3. 覆盖掉别的附魔状态

当前推荐做法是：

1. 按附魔 `Id` 精确定位
2. 同步到 `DeckVersion` 上对应的单附魔或复合条目
3. 再触发 `FinalizeUpgradeInternal()` 和刷新通知

本次已经把这套逻辑收敛到：

- `Scripts/Enchantments/Core/BlightEnchantmentRuntimeHelper.cs`

新增类似“永久成长型附魔”时，优先复用这里的同步方法，不要手写直改 `DeckVersion.Enchantment`。

#### 规则 I：一次性附魔不能只切 runtime `Status`，必须持久化“已失效”状态

这次又暴露出另一类典型问题：

1. `Swift`
2. `Sown`
3. `Glam`
4. `Vigorous`

这些附魔都属于“本场或本次触发后失效”的一次性附魔。

原版单附魔时，通常只要：

```csharp
base.Status = EnchantmentStatus.Disabled;
```

就够了。

但在 `BlightCompositeEnchantment` 下，如果你只是把 runtime 子附魔切成 `Disabled`，却没有把这个状态存回复合附魔本体，那么：

1. runtime 子附魔一旦重建
2. `Status` 会重新回到 `Normal`
3. 失效过的一次性附魔会再次生效

典型症状：

1. `Vigorous` 第二次打出还会加伤
2. `Swift` 第二次打出还会抽牌
3. `Sown` 第二次打出还会回能
4. `Glam` 已消耗后又重新可用

当前修法是：

1. 在 `BlightCompositeEnchantment` 上持久化“哪些子附魔索引已禁用”
2. runtime 子附魔重建时恢复对应 `Status`
3. 不再只对 `Glam` 特判，而是对所有一次性附魔统一生效

所以以后如果你新增附魔满足下面任一条件：

1. 第一次打出后失效
2. 每场战斗只触发一次
3. 触发一次后图标应变灰

就必须确认它的 `Disabled` 状态能被复合附魔持久保留。

#### 规则 J：一次性附魔失效后，标签视觉也要同步进入 Disabled 风格

原版里附魔失效后，不只是逻辑不再生效，**标签也会变灰**。

如果只修逻辑不修 UI，玩家会看到：

1. 附魔实际上已经失效
2. 但标签仍然是高亮状态
3. 容易误以为它还会继续生效

当前 `Blight` 已经在：

- `Scripts/Enchantments/Core/BlightEnchantmentUiRenderer.cs`

里补上这套行为。现在当子附魔或单附魔为 `EnchantmentStatus.Disabled` 时，会同步：

1. 标签整体变灰
2. 图标跟随父材质降饱和
3. 数字文字变灰

这与原版 `NCard.SetEnchantmentStatus(...)` 的视觉语义保持一致。

所以以后新增一次性附魔时，除了检查逻辑是否失效，还要额外检查：

1. 单附魔时标签是否变灰
2. 复合附魔时对应子标签是否变灰
3. 读档后若已失效，标签是否仍保持灰色

### 2.1.2 本次新增经验：三类最容易踩坑的附魔

下面这三类，是后续新增附魔时最值得优先自查的：

#### A. 永久成长型

例如：

1. 打出后永久加格挡
2. 打出后永久加伤害
3. 每次触发后永久增加 `Amount`

检查点：

1. 是否只改了 runtime 子附魔字段
2. 是否回写到了复合 entry
3. 是否同步了 `DeckVersion`

#### B. 卡面副作用型

例如：

1. 增减 `Keyword`
2. 改费用
3. 改 `Innate / Retain / Exhaust`

检查点：

1. `OnEnchant()` 是否安全可重复执行
2. 复合附魔新增时是否会被执行
3. 读档恢复时是否会被重新执行

#### C. 运行时状态型

例如：

1. 本场累计次数
2. 本场已触发标记
3. 本场动态倍率

检查点：

1. 是否错误存进了 runtime 私有字段
2. 是否会因子附魔重建而丢失
3. 是否应改为从战斗历史或稳定来源推导

#### D. 一次性失效型

例如：

1. 第一次打出后失效
2. 本场战斗只触发一次
3. 触发后标签应变灰

检查点：

1. 是否只修改了 runtime 子附魔的 `Status`
2. 是否把 `Disabled` 状态持久化到复合附魔本体
3. runtime 重建后是否能恢复为 `Disabled`
4. UI 标签是否会同步变灰

### 2.1.3 以后添加类似附魔时的最小自检清单

新增一个附魔前，至少过一遍这 7 项：

1. `CanApplyTo()` 是否避开了 `base.CanEnchant(card)` 的单附魔假设
2. `OnEnchant()` 是否幂等或至少可安全重放
3. 是否存在需要同步到 `DeckVersion` 的永久变化
4. 永久成长是否写进了 `Amount`
5. 运行时状态是否错误放在私有字段里
6. 读档后重新执行 `ModifyCard()` 时结果是否仍正确
7. 一次性附魔的 `Status.Disabled` 是否会在复合附魔重建后保留
8. 失效后的标签是否会像原版一样变灰
9. 多重附魔下与“加关键词/删关键词”类附魔是否会互相冲突

### 2.2 数字显示规则

- 显示数字：`ShowAmount => true`
- 显示哪一个数字：
  - 动态数值用 `Amount`
  - 固定数值（如“灾厄3”）建议覆盖 `DisplayAmount`
- UI 显示逻辑在：`Scripts/Enchantments/Core/BlightEnchantmentUiRenderer.cs`

  补充：

  1. 如果只是固定显示数字，不需要为了显示去额外写 `DynamicVars`
  2. 如果效果本身不依赖 `DynamicVars`，不要硬在 `RecalculateValues()` 里写 `base.DynamicVars[...]`

### 2.3 图标配置

你有两种方式：

1. 直接放自定义图片（最简单）
  - 路径：`images/enchantments/<entry_lowercase>.png`
  - 引擎会通过 `EnchantmentModel.IntendedIconPath` 自动加载

2. 映射到原版 Power 图标
  - 文件：`Scripts/Enchantments/Patches/BlightEnchantmentIconPatch.cs`
  - 推荐复用 `TryLoadPowerIcon<TPower>(...)`，不要直接强转 `Power.Icon`

示例：

```csharp
if (id.Entry.Equals("BLIGHT_EXAMPLE_ENCHANTMENT", StringComparison.Ordinal))
{
    if (TryLoadPowerIcon<DoomPower>(out CompressedTexture2D? icon))
    {
        __result = icon!;
    }
    else
    {
        __result = ModelDb.Enchantment<Sharp>().Icon;
    }

    return false;
}
```

### 2.4 本地化

文件：

- `Scripts/Localization/BlightLocalization.cs`
- `Scripts/Localization/Patches/ModLocalizationPatch.cs`

当前做法不是再去 `ModLocalizationPatch` 里手写一个大字典，而是：

1. 在 `BlightLocalization.cs` 里补文本
2. 由 `ModLocalizationPatch` 在 `LocManager.SetLanguage(...)` 时按当前语言注入到 `enchantments / modifiers` 表

#### 新增附魔时至少补这两项

- `BLIGHT_EXAMPLE_ENCHANTMENT.title`
- `BLIGHT_EXAMPLE_ENCHANTMENT.description`

推荐同时补两套文本：

- `English` 字典：英文
- `Chinese` 字典：中文

示例：

```csharp
["BLIGHT_EXAMPLE_ENCHANTMENT.title"] = "Example",
["BLIGHT_EXAMPLE_ENCHANTMENT.description"] = "This card gains 3 additional damage.",
```

```csharp
["BLIGHT_EXAMPLE_ENCHANTMENT.title"] = "示例",
["BLIGHT_EXAMPLE_ENCHANTMENT.description"] = "这张牌额外造成3点伤害。",
```

#### 当前语言回退规则

当前 `BlightLocalization` 的规则是：

1. 中文语言（如 `zhs / zht`）优先显示中文
2. 英文语言显示英文
3. 其它暂未单独适配的语言统一回退英文

因此：

1. **至少要补英文**，这样其它语言不会报错
2. 如果希望中文正常显示，就要同时补中文

#### 不要再写死到 UI 代码里

如果某个附魔、锻造选项、按钮文本要显示在 UI 上：

1. 不要直接写中文字符串
2. 优先通过 `BlightLocalization.GetText(...)` 或 `Format(...)` 取文本

例如：

```csharp
BlightLocalization.GetText("BLIGHT_EXAMPLE_ENCHANTMENT.title")
```

如果是带变量的描述，使用：

```csharp
BlightLocalization.Format("BLIGHT_FORGE.description", ("Target", target), ("EnchantName", name))
```

如果你未来确定某个附魔要使用额外卡面文案，再单独考虑 `extraCardText`。目前 `Blight` 自定义附魔**不建议默认启用**：

```csharp
public override bool HasExtraCardText => true;
```

因为它在当前多重附魔 UI 下更容易引出兼容问题。

---

## 3. 注册到附魔池（卡牌奖励池 / 锻造池）

文件：`Scripts/Enchantments/Core/BlightEnchantmentPool.cs`

当前有三种注册函数（都在同文件内）：

- `RegisterCardReward<T>()`：只进卡牌奖励池
- `RegisterForge<T>()`：只进锻造池
- `RegisterShared<T>()`：两边都进

在静态构造函数中按需求添加：

```csharp
RegisterCardReward<BlightExampleEnchantment>();
RegisterForge<BlightExampleEnchantment>();
```

或者：

```csharp
RegisterShared<BlightExampleEnchantment>();
```

推荐做法：

- 强力附魔先只进锻造池（`RegisterForge`）
- 普通附魔走共享（`RegisterShared`）

额外建议：

1. 依赖 `OnEnchant()` 改卡面状态的强副作用附魔，先放锻造池单独验证
2. 验证通过后再考虑放进卡牌奖励池

---

## 4. 修改附魔稀有度与概率

### 4.1 修改“某个附魔属于哪档稀有度”

在附魔类里改：

```csharp
public BlightEnchantmentRarity Rarity => BlightEnchantmentRarity.Rare;
```

可选值：`Common / Uncommon / Rare / Negative`

### 4.2 修改卡牌奖励的正面附魔稀有度概率

文件：`Scripts/Enchantments/Core/BlightEnchantmentPool.cs`

函数：`RollCardRewardRarity(Rng rng, bool isElite, bool isMutant)`

这里控制普通战、精英战、变异战、变异精英战每一层正面附魔的概率。

#### 当前正面附魔爆率就是在这里改

也就是说，如果你想改：

1. 普通战更容易出 `Uncommon`
2. 精英战更容易出 `Rare`
3. 变异战更容易出高档附魔

都应该优先改这个函数，而不是去改具体附魔类。

#### 如果想让爆率随层数上升，建议怎么做

当前函数签名只有：

```csharp
RollCardRewardRarity(Rng rng, bool isElite, bool isMutant)
```

如果后续你要做“层数越高，稀有附魔越容易出”，推荐思路是：

1. 在这个函数里拿当前楼层（例如 `RunState.TotalFloor`）
2. 按楼层给 `Uncommon / Rare / UltraRare` 做额外权重
3. 用分段而不是暴力线性拉满

推荐分段思路：

```text
1-15层：基础概率
16-30层：小幅提高 Uncommon
31-45层：继续提高 Rare
46层以后：少量提高 UltraRare
```

不建议：

1. 前几层就明显提高 `Rare`
2. 把所有层数都写成一条线性公式
3. 把楼层逻辑散落到附魔类里

建议把“随层数提高爆率”的逻辑只放在这个 roll 函数里统一处理，这样后期最好平衡。

补充：

1. **当前正面附魔稀有度爆率就在这里改**
2. 如果你想让“爆率随层数上升”，建议就在这个函数里加楼层参数逻辑，而不是去改具体附魔类

推荐做法：

1. 先取当前楼层，例如 `RunState.TotalFloor`
2. 按楼层给 `Rare / Uncommon / UltraRare` 增加权重
3. 最后再 roll 概率

例如思路：

1. 低层：以 `Common` 为主
2. 中层：逐步提高 `Uncommon`
3. 高层：再逐步提高 `Rare`
4. `UltraRare` 只在高层给极小加成

建议不要直接线性拉满，而是做分段：

```text
1-15层：基础概率
16-30层：罕见小幅提高
31层以后：稀有再提高
```

这样更容易平衡，也更容易回调。

### 4.3 修改变异诅咒（负面层）的稀有度概率

文件：`Scripts/Enchantments/Core/BlightEnchantmentManager.cs`

函数：`TryApplyMutantCurse(CardModel card, bool isElite)`

关键参数：

```csharp
float rareChance = isElite ? 0.50f : 0.20f;
```

这行就是变异精英/变异普通“诅咒稀有档”概率。

如果以后你想让负面附魔也随层数增强，推荐同样在这个函数里按楼层统一做：

1. 低层：以普通负面层为主
2. 中层：提高 `Uncommon` 负面层概率
3. 高层：再提高 `Rare` 负面层概率

不要把“楼层影响负面爆率”的逻辑分散到具体负面附魔类里。

如果以后要做“随层数提高负面附魔强度”，同理建议在这个函数里按楼层分段调 `rareChance`，不要把逻辑散到各个诅咒附魔类里。

---

## 5. 修改锻造奖励稀有度概率

文件：`Scripts/Rewards/Patches/CombatRewardPatch.cs`

函数：`RollForgeTier(float roll, bool isElite, bool isMutant)`

这里决定锻造奖励本身是 `Common / Uncommon / Rare`，从而影响三选一质量。

---

## 6. 创建新的锻造第三功能奖励

当前第三项功能奖励由 `ForgeOptionUtility` 承担。

### 6.1 新增功能枚举

文件：`Scripts/Rewards/ForgeOptions/ForgeOptionUtility.cs`

```csharp
public enum UtilityKind
{
    Relic,
    Smith,
    Remove,
    Heal,
}
```

### 6.2 补齐标题、描述、可执行条件、执行逻辑

同文件分别修改：

- `Title` switch
- `Description` switch
- `CanExecute` switch
- `ExecuteAsync` switch

示例（回复10血）：

```csharp
case UtilityKind.Heal:
{
    await CreatureCmd.Heal(player.Creature, 10m);
    return true;
}
```

说明：`10m` 是 decimal，必须带 `m`。

### 6.3 把新功能接到第三选项池

文件：`Scripts/Rewards/ForgeOptions/ForgeOptionPool.cs`

函数：`BuildUtilityCandidatesForTier(BlightEnchantmentRarity rarity)`

把你的新 `UtilityKind` 按稀有度加入候选列表即可。

---

## 7. 快速改动清单（实操版）

新增附魔：

1. 新建 `CustomEnchantments/YourEnchant.cs`
2. 实现 `IBlightEnchantment`
3. 先确认 `CanApplyTo` 没有直接或间接依赖 `base.CanEnchant(card)`
4. 如果附魔会改关键词/费用/动态变量，确认它兼容复合附魔路径
5. 设置 `ShowAmount/DisplayAmount/Rarity`
6. 在 `BlightEnchantmentPool` 里注册到卡牌池/锻造池
7. 补 `BlightLocalization` 文本（至少英文，建议中英都补）
8. 按需补 `BlightEnchantmentIconPatch` 图标映射
9. 重点验证：
  - 已有其他附魔的卡能否继续附上
  - 左侧标签是否正常显示
  - 悬浮说明与标签是否一致
  - 若有 `OnEnchant()` 副作用，卡牌状态是否真的改变
  - 卡牌奖励多层附魔时是否会中断

新增锻造第三功能：

1. 在 `ForgeOptionUtility.UtilityKind` 增枚举
2. 补齐 `Title/Description/CanExecute/ExecuteAsync`
3. 在 `ForgeOptionPool.BuildUtilityCandidatesForTier` 按稀有度加候选

改概率：

1. 卡牌奖励正面附魔概率：`BlightEnchantmentPool.RollCardRewardRarity`
2. 变异诅咒稀有度概率：`BlightEnchantmentManager.TryApplyMutantCurse`
3. 锻造奖励稀有度概率：`CombatRewardPatch.RollForgeTier`

---

## 8. 验证

```powershell
dotnet build .\blight.csproj
```

若 DLL 被游戏占用，请先关闭游戏再构建。

建议最少做以下手测：

1. **单附魔测试**：空白卡正常附上
2. **多附魔测试**：已有其他附魔后仍可附上
3. **标签测试**：左侧标签、悬浮说明一致
4. **奖励测试**：普通 / 精英 / 变异奖励层数正确
5. **锻造测试**：附魔成功后奖励会正常消失
6. **读档测试**：退出重进后标签、卡面状态、效果一致

---

## 9. 常见问题与解决方案

这一节专门写“如果出现问题，优先看哪里”。

### 9.1 问题：附魔存在，但左侧标签消失，悬浮说明还在

优先检查：

1. `Scripts/Enchantments/Core/BlightEnchantmentUiRenderer.cs`
2. `IsModelVisuallyApplicable(...)` 是否把“新增附魔条件”错误当成“标签显示条件”

典型错误：

1. `Instinct` 用“当前费用必须 > 0”当显示条件
2. 但 `Instinct` 自己会把牌降到 `0` 费
3. 于是附魔还在，标签却被隐藏

正确思路：

1. **新增是否合法** 和 **已有标签是否显示** 必须拆开
2. 显示条件只能依赖稳定条件，不能依赖附魔自己会改变的状态

### 9.2 问题：附魔条目在，但卡面效果没真正生效

例如：

1. `Goopy` 有描述，但牌不消耗
2. `Instinct` 有标签，但费用没降

优先检查：

1. 该附魔是否依赖 `OnEnchant()` 改卡状态
2. 复合附魔追加 entry 时，有没有补执行等价 `ModifyCard()`
3. 读档恢复 entry 时，有没有补执行等价 `ModifyCard()`

这类附魔如果只把数据写进 `BlightCompositeEnchantment.Entries`，但不补卡面副作用，就会“看起来附上了，实际上没生效”。

### 9.3 问题：附魔能加上，但不能继续多重附魔

优先检查：

1. `CanApplyTo(CardModel card)` 是否直接或间接使用了 `base.CanEnchant(card)`

错误写法示例：

```csharp
return card != null && base.CanEnchant(card);
```

或者：

```csharp
return card != null && CanEnchant(card);
```

如果 `CanEnchant(card)` 里又走了 `base.CanEnchant(card)`，问题一样存在。

正确思路：

1. 自己检查类型
2. 自己检查 `Unplayable / CostsX / GainsBlock`
3. 自己检查是否允许重复

### 9.4 问题：卡牌奖励里的部分附魔层数丢失

优先检查两件事：

1. 奖励注入逻辑是不是写成了“这张卡只要已有附魔就整张跳过”
2. 某个附魔在 `ModifyCard()` 或 `RecalculateValues()` 里是不是抛了异常，导致整条奖励处理被中断

本次真实踩过的例子：

1. `BlightAdroit2Enchantment`
2. `BlightAdroit4Enchantment`

它们访问了不存在的 `DynamicVars["Block"]`，直接打断整条奖励注入流程。

### 9.5 问题：锻造附魔成功了，但锻造奖励不消失

优先检查：

1. `ForgeOptionAddEnchant.ExecuteAsync(...)` 返回值是否稳定
2. 是否只依赖中间布尔结果，而没有和“条目是否真的增加”绑定

正确思路：

1. 成功判定最好和实际数据变化绑定
2. 例如比较附魔前后 entry 数量是否增加

### 9.6 问题：战斗内叠层效果不生效

例如：

1. `Momentum` 标签正常
2. 但打出后伤害不增加

优先检查：

1. 状态是不是只存在 runtime 子附魔私有字段里
2. `BlightCompositeEnchantment` 是否会导致 runtime 子附魔重建

正确思路：

1. 尽量从 `CombatManager.Instance.History` 推导
2. 或从卡本身稳定状态推导
3. 不要只依赖 runtime 临时字段

### 9.7 问题：`RecalculateValues()` 里报 `KeyNotFoundException`

这通常意味着：

1. 你访问了 `base.DynamicVars["某个键"]`
2. 但你并没有声明对应的 `CanonicalVars`

如果你的附魔其实只是固定数值效果：

1. 显示数字用 `DisplayAmount`
2. 效果逻辑写在 `OnPlay` / `EnchantDamageAdditive` / `EnchantBlockAdditive`
3. 就不要硬访问不存在的 `DynamicVars`

---

## 10. 文档索引建议

如果你以后再新增附魔，建议顺序是：

1. 先看本文件的 `2.1.1 多重附魔安全写法`
2. 再看本文件 `9. 常见问题与解决方案`
3. 如果遇到复杂问题，再看：`Design/06_Multi_Enchantment_Pitfalls_and_Fixes.md`
