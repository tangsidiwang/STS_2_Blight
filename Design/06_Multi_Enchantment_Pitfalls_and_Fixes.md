# 06 Multi Enchantment Pitfalls and Fixes

本文档汇总本次对话中在 `Blight` 多重附魔系统里遇到的真实问题、定位过程、根因分析与最终修复方案。

# 06 Multi Enchantment Pitfalls and Fixes

本文档覆写汇总本次完整对话里出现过的多重附魔问题、根因与最终修复方案。

目标：

1. 记录已经验证过的问题与修法。
2. 说明这些问题为什么会发生。
3. 给后续新增附魔与改奖励逻辑提供稳定规则。

路径均为 `blight/` 下相对路径。

---

## 1. 本次涉及的新增附魔

本轮新增并持续调试的附魔：

1. `BlightGoopyEnchantment`（黏糊）
2. `BlightImbuedEnchantment`（注能）
3. `BlightInstinctEnchantment`（本能）
4. `BlightMomentum1Enchantment`（动量1）
5. `BlightMomentum2Enchantment`（动量2）

这些附魔全部在多重附魔系统下运行，因此暴露了大量“原版单附魔假设”和“UI/奖励链路假设”的兼容问题。

---

## 2. 第一类问题：原版附魔默认按“单附魔槽”设计

### 2.1 现象

最初直接参照原版做 `Goopy / Imbued / Instinct / Momentum` 时，出现：

1. 某些附魔不能附到已有其他附魔的卡上。
2. 某些附魔悬浮说明存在，但左侧标签不显示。
3. 某些附魔打出后效果不生效。
4. 某些附魔修改卡面状态后，表现像“附上了但没真正生效”。

### 2.2 根因

原版很多实现默认：

1. `CardModel.Enchantment` 只会有一个对象。
2. 这个附魔对象就是稳定长期存在的唯一实例。
3. `OnEnchant()`、`AfterCardPlayed()`、`RecalculateValues()` 都可以直接围绕这一个对象写。

但在 `BlightCompositeEnchantment` 下：

1. 真正挂在卡上的变成 `BlightCompositeEnchantment`
2. 子附魔只是 runtime 临时构建对象
3. runtime 子附魔可能反复重建

所以不能直接照抄原版实现。

---

## 3. 第二类问题：`CanApplyTo` 和 UI 标签可见性混用

### 3.1 现象

本次出现过：

1. `Goopy` 不能附到已有其他附魔的卡上
2. `Goopy` 标签消失，但悬浮说明还在
3. `Momentum` 标签消失，但悬浮说明还在
4. `Instinct` 在某些牌上标签消失，但悬浮说明还在

### 3.2 根因

`IBlightEnchantment.CanApplyTo(CardModel card)` 被同时拿去做两件事：

1. **新增附魔是否合法**
2. **已有附魔标签是否应该显示**

但这两个语义并不相同。

如果 `CanApplyTo` 直接或间接用了：

```csharp
base.CanEnchant(card)
```

就会被原版单附魔限制影响，在卡已有其他附魔时返回 `false`。

结果就是：

1. 新增附魔失败
2. UI 还会误把已有附魔当成“不合法”，把标签隐藏

### 3.3 修复

修复分两层：

#### A. 业务层

在这些附魔中，`CanApplyTo` 改成多附魔安全写法，不再依赖 `base.CanEnchant(card)`：

1. `BlightGoopyEnchantment`
2. `BlightImbuedEnchantment`
3. `BlightInstinctEnchantment`
4. `BlightMomentum1Enchantment`
5. `BlightMomentum2Enchantment`

#### B. UI 层

在：

- `Scripts/Enchantments/Core/BlightEnchantmentUiRenderer.cs`

中，把“标签是否显示”独立出来。

也就是说：

1. 新增是否合法，看 `CanApplyTo`
2. 标签是否显示，看 `IsModelVisuallyApplicable(...)`

二者不再混用。

### 3.4 `Instinct` 的特殊坑

`Instinct` 还出现过一个额外问题：

1. 某些牌被 `Instinct` 降到 `0` 费后
2. 标签消失
3. 悬浮描述还在

根因是 UI 曾经把“当前是否仍满足原始附魔前提”当成显示条件。

但 `Instinct` 会主动改变费用，因此“费用是否 > 0”不能用来决定一个**已存在的** `Instinct` 标签是否显示。

最终修法：

```csharp
if (model is CustomEnchantments.BlightInstinctEnchantment)
{
    return card.Type == CardType.Attack;
}
```

即：

- UI 对 `Instinct` 只看稳定身份条件
- 不再看它自己会改变的当前费用状态

---

## 4. 第三类问题：复合附魔追加条目时，没有执行 `ModifyCard()`

### 4.1 现象

本次出现过：

1. `Goopy` 附魔后卡牌不消耗
2. `Goopy` 看起来像附上了，但卡本身没有真正拿到 `Exhaust`

### 4.2 根因

原版 `CardCmd.Enchant(...)` 会做：

```csharp
card.EnchantInternal(enchantment, amount);
enchantment.ModifyCard();
card.FinalizeUpgradeInternal();
```

而复合附魔路径里，最早只做了：

1. 给 `BlightCompositeEnchantment` 加 entry
2. `FinalizeUpgradeInternal()`

没有对新 entry 对应的子附魔执行一次等价 `ModifyCard()`。

于是像：

1. `Goopy.OnEnchant()` 加 `Exhaust`
2. `Instinct.OnEnchant()` 改费用

这种依赖 `OnEnchant()` 的副作用都不会真正落到卡上。

### 4.3 修复

在：

- `Scripts/Enchantments/Core/BlightEnchantmentManager.cs`

中加入对子附魔的等价落卡动作：

```csharp
private static void ApplyCompositeEntryCardMutation(CardModel card, EnchantmentModel enchantment, int amount)
{
    EnchantmentModel runtime = SaveUtil.EnchantmentOrDeprecated(enchantment.Id).ToMutable();
    runtime.ApplyInternal(card, amount);
    runtime.ModifyCard();
}
```

复合附魔成功追加 entry 后立即调用。

---

## 5. 第四类问题：读档恢复后卡面副作用丢失

### 5.1 现象

风险表现为：

1. 条目恢复了
2. HoverTip 恢复了
3. 标签恢复了
4. 但费用 / 关键词等卡面状态可能没有恢复

### 5.2 根因

如果新增附魔时补了 `ModifyCard()`，但读档恢复 entry 时不补，那读档后就会丢失依赖 `OnEnchant()` 的卡面副作用。

### 5.3 修复

在：

- `Scripts/Enchantments/Core/BlightCompositeEnchantmentSaveExtension.cs`

中恢复 entry 时，也补执行一次等价 `ModifyCard()`。

这样保证：

1. 新附魔时正确
2. 读档恢复后也正确

---

## 6. 第五类问题：`Momentum` 战斗内加成丢失

### 6.1 现象

曾经出现：

1. `Momentum1/2` 标签能显示
2. 可以多重附魔
3. 但打出后伤害不增加

### 6.2 根因

最早模仿原版写成：

```csharp
private int _extraDamage;
```

再在 `OnPlay()` 中累加。

但在 `BlightCompositeEnchantment` 下，runtime 子附魔是会被重建的。

一旦重建：

1. `_extraDamage` 清零
2. 累计状态全部丢失

### 6.3 修复

改成从稳定来源推导：

- `CombatManager.Instance.History.CardPlaysFinished`

按“这张卡本场已打出次数”计算加成。

优点：

1. 不依赖 runtime 子附魔实例稳定存在
2. 不需要额外存储状态
3. 更适合复合附魔体系

---

## 7. 第六类问题：`HasExtraCardText` 在当前环境不适合作为默认写法

### 7.1 现象

新附魔上如果默认写：

```csharp
public override bool HasExtraCardText => true;
```

在当前 UI 和多重附魔环境下容易引出兼容问题。

### 7.2 处理

本次最终把新加附魔上的该行都移除了。

当前结论：

1. `Blight` 自定义附魔不要默认开 `HasExtraCardText`
2. 除非你明确验证过该附魔需要额外卡面文本，而且 UI 正常

---

## 8. 第七类问题：卡牌奖励附魔层数丢失

### 8.1 现象

卡牌奖励中本应满足：

1. 普通战：每张卡 `1` 个正面附魔
2. 精英战：每张卡 `2` 个正面附魔
3. 变异：在原本基础上再 `+1` 个正面，并额外 `+1` 个负面

但实际曾经出现部分卡牌层数不够。

### 8.2 第一层根因：奖励补层逻辑写成“有附魔就跳过”

最早在：

- `Scripts/Enchantments/Patches/CardRewardPopulatePatch.cs`

里用了类似：

1. 这张卡已有任意 `Blight` 附魔
2. 那就整张卡跳过，不再继续补层

这样只适合“只做一次注入”的情况。

一旦奖励生成流程里有多次刷新、重建或局部重投，就会导致：

1. 某张卡只拿到部分层数
2. 后面再跑到这张卡时又因“已有附魔”被跳过
3. 最终层数不达标

### 8.3 第二层根因：个别附魔在奖励流程里直接抛异常

后续日志里还出现过：

```text
KeyNotFoundException: The given key 'Block' was not present in the dictionary.
```

这个异常来自：

1. `BlightAdroit2Enchantment`
2. `BlightAdroit4Enchantment`

它们在 `RecalculateValues()` 中访问：

```csharp
base.DynamicVars.Block.BaseValue
```

但自身并没有声明对应 `CanonicalVars`，于是：

1. 奖励流程随机到它们
2. `ModifyCard()` 调用 `RecalculateValues()`
3. 直接抛异常
4. `CardRewardPopulatePatch` 整体中断
5. 后续卡牌 / 后续层全部没注完

### 8.4 修复

修复分两步：

#### A. 卡牌奖励改成“补齐到目标层数”

不再看“有没有附魔”，而是：

1. 先算当前已有多少正面层
2. 再补到目标正面层数
3. 再单独补负面层

#### B. 清掉会中断奖励流程的附魔异常

在：

1. `BlightAdroit2Enchantment.cs`
2. `BlightAdroit4Enchantment.cs`

中移除无效的 `DynamicVars.Block` 访问。

它们的实现改为：

1. 显示数字走 `DisplayAmount`
2. 实际效果走 `OnPlay` 固定加格挡
3. 不再依赖不存在的 `DynamicVars`

---

## 9. 第八类问题：锻造奖励附魔后奖励不消失

### 9.1 现象

出现过：

1. 锻造选到 `伶俐` 附魔后，卡确实被附魔了
2. 但锻造奖励按钮没消失
3. 于是可以继续点同一个奖励，一直附魔到换别的选项为止

### 9.2 根因

`Reward` 是否被收起取决于 `OnSelectWrapper()` 的返回值。

也就是说：

1. 不是“看起来附上了就算成功”
2. 而是 `ForgeOptionAddEnchant.ExecuteAsync(...)` 必须返回 `true`

之前的问题是：

1. 某些路径里附魔数据已变化
2. 但方法返回值不稳定
3. UI 于是认为“这次选择失败”，重新启用按钮

### 9.3 修复

在：

- `Scripts/Rewards/ForgeOptions/ForgeOptionAddEnchant.cs`

中把成功判定改成：

1. 不只信 `TryApply(...)` / `TryApplySpecificFromForge(...)` 的布尔返回值
2. 还比较附魔前后 entry 数是否实际增加

即：

1. 指定附魔：比较该 `EnchantmentId` 数量是否变多
2. 随机附魔：比较总 entry 数是否变多

只要数据真的增加，就视为成功，奖励就会正确消失。

---

## 10. 第九类问题：卡牌奖励打开时/重刷后标签显示不同步

### 10.1 现象

对话里还遇到过奖励界面里：

1. 数据上已经是复合附魔
2. 但画面上只看到总的“多重附魔”标签
3. 没有正常展开子标签

### 10.2 根因

奖励牌选界面在某些刷新链路里，卡牌数据改完后 UI 不一定自动重新跑一遍我们自己的多标签渲染。

### 10.3 处理

在 `CardRewardPopulatePatch` 末尾增加“如果奖励选牌界面已经打开，则主动刷新一次当前奖励选项”的逻辑，尽量让界面与数据同步。

这不是多重附魔本身的核心问题，而是“奖励 UI 刷新时机”和“附魔注入时机”不完全一致造成的。

---

## 11. 本次实际修改过的关键文件

### 11.1 核心逻辑

- `Scripts/Enchantments/Core/BlightEnchantmentManager.cs`
- `Scripts/Enchantments/Core/BlightCompositeEnchantmentSaveExtension.cs`
- `Scripts/Enchantments/Core/BlightEnchantmentRuntimeHelper.cs`
- `Scripts/Enchantments/Models/BlightCompositeEnchantment.cs`

### 11.2 奖励与锻造

- `Scripts/Enchantments/Patches/CardRewardPopulatePatch.cs`
- `Scripts/Rewards/ForgeOptions/ForgeOptionAddEnchant.cs`

### 11.3 UI

- `Scripts/Enchantments/Core/BlightEnchantmentUiRenderer.cs`
- `Scripts/Enchantments/Patches/BlightEnchantmentIconPatch.cs`

### 11.4 新附魔实现

- `Scripts/Enchantments/CustomEnchantments/BlightGoopyEnchantment.cs`
- `Scripts/Enchantments/CustomEnchantments/BlightImbuedEnchantment.cs`
- `Scripts/Enchantments/CustomEnchantments/BlightInstinctEnchantment.cs`
- `Scripts/Enchantments/CustomEnchantments/BlightMomentum1Enchantment.cs`
- `Scripts/Enchantments/CustomEnchantments/BlightMomentum2Enchantment.cs`
- `Scripts/Enchantments/CustomEnchantments/BlightAdroit2Enchantment.cs`
- `Scripts/Enchantments/CustomEnchantments/BlightAdroit4Enchantment.cs`

---

## 12. 后续新增附魔时的稳定规则

### 12.1 `CanApplyTo` 不要默认走 `base.CanEnchant(card)`

除非你明确就是要复用原版单附魔限制，否则不要这么写。

优先自己写：

1. 类型判定
2. `Unplayable / CostsX / GainsBlock` 等业务限制
3. 重复规则

### 12.2 UI 显示条件不能依赖“附魔自己会改变的状态”

例如：

1. `Instinct` 会改费用
2. 所以 UI 不应该用“当前费用 > 0”决定标签是否显示

显示条件只看稳定身份条件。

### 12.3 有 `OnEnchant()` 副作用的附魔，必须考虑复合附魔路径

只要涉及：

1. 改关键词
2. 改费用
3. 改动态变量

就必须保证：

1. 新增 entry 时补一次等价 `ModifyCard()`
2. 读档恢复 entry 时也补一次等价 `ModifyCard()`

### 12.4 关键战斗状态不要只存在 runtime 子附魔字段里

如果子附魔会被 runtime 重建，就不要只把核心状态放在私有字段里。

优先：

1. 从战斗历史推导
2. 从卡稳定状态推导
3. 或者显式做可恢复的持久化

### 12.5 奖励逻辑不要写“有附魔就整卡跳过”

奖励注入必须按“目标层数补齐”的思路写，否则在重刷/再处理场景下很容易丢层。

### 12.6 所有会批量注入附魔的流程，都要假设某个附魔可能抛异常

任何单个附魔的 `RecalculateValues()` / `ModifyCard()` 崩溃，都可能打断整条奖励链。

因此新增附魔时一定要检查：

1. 是否访问了不存在的 `DynamicVars`
2. 是否依赖了只在原版附魔里才有的内部状态
3. 是否会因当前卡状态变化而让 UI 判定失真

---

## 13. 速查表

### 问题 A：悬浮说明还在，但左侧标签没了

先查：

1. `BlightEnchantmentUiRenderer.IsModelVisuallyApplicable(...)`
2. 是否把“新增合法性”误用了到“显示合法性”
3. 是否依赖了附魔自己会改变的状态

### 问题 B：附魔存在，但关键词/费用/卡面效果没落到牌上

先查：

1. 复合附魔追加 entry 时是否补了等价 `ModifyCard()`
2. 读档恢复时是否补了等价 `ModifyCard()`

### 问题 C：战斗内叠层/累计效果不生效

先查：

1. 状态是否只存在 runtime 子附魔私有字段
2. runtime 子附魔是否会重建

### 问题 D：卡牌奖励附魔层数不够

先查：

1. 注入逻辑是不是写成“已有附魔就跳过”
2. 是否某个附魔在 `ModifyCard()` 过程中抛异常，打断整个奖励处理

### 问题 E：锻造奖励点完没消失

先查：

1. `ExecuteAsync()` 是否真的返回 `true`
2. 成功判定是否与“实际条目增加”绑定

---

## 14. 当前结论

经过本轮完整修复后，`Blight` 多重附魔系统已经形成以下规则：

1. **新增附魔判定** 与 **已有标签显示** 必须拆开
2. **复合附魔 entry** 不只是存数据，还要补执行必要的卡面副作用
3. **奖励注入** 必须按目标层数补齐，不能按“有无附魔”粗暴跳过
4. **成功判定** 应尽量与实际数据变化绑定，而不是只信单个布尔返回值
5. **新增附魔实现** 不要直接照搬原版；先判断它是否依赖单附魔稳定实例假设

后续新增附魔时，优先参考本文件，而不是直接照抄原版附魔实现。
