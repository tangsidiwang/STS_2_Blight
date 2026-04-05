# 怪物新 Buff（Power）创建流程

本文以 `EffigyWardPower` 为例，说明在 `blight` 模组中给怪物新增一个 Buff 的完整流程。

## 新示例：`HivePower`（蜂巢）

当前工程已额外提供一个完整示例：

- `blight/Scripts/AI/Buffs/HivePower.cs`
- `blight/Scripts/AI/Bestiary/EntomancerHivePatch.cs`

这个 Buff 的设计是：

- 名称：`蜂巢`
- 图标：复用原版 `FreeAttackPower` 图标
- 效果：敌人每受到 `3` 次攻击伤害，向玩家弃牌堆加入 `1` 张 `Wound`
- 显示数值：Buff 角标显示距离下一次触发还差几次命中

它非常适合作为“受击计数型”怪物被动模板。

## 新示例：`MeteorPower`（陨星）

当前工程已额外提供一个“出牌计数型”示例：

- `blight/Scripts/AI/Buffs/MeteorPower.cs`
- `blight/Scripts/AI/Bestiary/DoormakerMeteorPatch.cs`

这个 Buff 的设计是：

- 名称：`陨星`
- 图标：复用原版 `FreeSkillPower` 图标
- 效果：玩家每打出 `3` 张技能牌，向该玩家弃牌堆加入 `1` 张 `Dazed`
- 显示数值：Buff 角标显示距离下一次触发还差几张技能牌

它适合作为“玩家行为触发惩罚”的怪物被动模板。

## 目录约定

- Buff 类放在：`blight/Scripts/AI/Buffs/`
- Buff 相关 Patch 放在：`blight/Scripts/AI/Buffs/Patches/`
- 如果这个 Buff 只服务于某一类怪，也可以继续留在 `AI` 目录下，便于和对应 AI 一起维护。

当前示例文件：

- `blight/Scripts/AI/Buffs/EffigyWardPower.cs`
- `blight/Scripts/AI/Buffs/Patches/BlightPowerIconPatch.cs`

---

## 一、先确认 Buff 的定位

在 STS2 里，怪物/玩家身上的“Buff / Debuff”本质上都是 `PowerModel`。

新增怪物 Buff 时，先明确这 4 件事：

1. **它是 Buff 还是 Debuff**
2. **它是否可叠层**
3. **它在什么时机触发**
4. **它需要什么图标和本地化**

以示例 `EffigyWardPower` 为例：

- 类型：Buff
- 叠层：可叠层计数
- 效果：己方回合结束时，获得等同于层数的格挡
- 本地化 key：`BLIGHT_EFFIGY_WARD.title` / `BLIGHT_EFFIGY_WARD.description`

---

## 二、创建新的 Power 类

新建文件：`blight/Scripts/AI/Buffs/EffigyWardPower.cs`

最小结构如下：

1. 继承 `PowerModel`
2. 覆盖 `Type`
3. 覆盖 `StackType`
4. 根据效果实现对应 hook

示例里使用的是：

- `AfterTurnEnd(...)`

因为它要在回合结束时生效。

常见 hook 可从原版这些文件对照：

- `st-s-2/src/Core/Models/PowerModel.cs`
- `st-s-2/src/Core/Models/AbstractModel.cs`
- `st-s-2/src/Core/Models/Powers/*.cs`

推荐优先找一个“最像你需求”的原版 Power 作为模板，例如：

- 反伤类：`ThornsPower`
- 回合结算类：找带 `AfterTurnEnd` / `BeforeTurnEnd` 的原版 Power
- 伤害修正类：`StrengthPower`

---

## 三、命名规则

建议自定义 Buff 使用统一前缀，避免和原版冲突：

- 类名：`EffigyWardPower`
- `Id.Entry`：默认会按类型名推导成 `BLIGHT_EFFIGY_WARD`

这样有 3 个好处：

1. 本地化 key 好找
2. 图标 Patch 好做
3. 调试时一眼能看出是模组内容

---

## 四、让图标正确显示

### 方案 A：正式资源方案

如果你已经准备好了正式图标，按原版 `PowerModel` 约定放资源：

- 小图：打进 `power_atlas`
- 大图：`res://powers/blight_effigy_ward.png`

这是最终形态，但前期制作成本较高。

### 方案 B：开发期回退方案

当前示例使用的是这个方案。

新增文件：`blight/Scripts/AI/Buffs/Patches/BlightPowerIconPatch.cs`

做法：

1. Patch `PowerModel.get_Icon`
2. 只拦截 `Id.Entry` 以 `BLIGHT_` 开头的自定义 Power
3. 没有正式资源时，回退到一个原版 Power 图标

示例里：

- `BLIGHT_EFFIGY_WARD` 回退到 `PlatingPower` 的图标

这样能保证：

- 战斗内 Buff 图标正常显示
- HoverTip 图标正常显示
- 不需要改原版资源文件

如果你后面补了正式资源，只要让原版逻辑能加载到你的图，就可以删掉对应回退分支。

---

## 五、让本地化正确显示

原版 `PowerModel` 默认从 `powers` 表读取：

- `<PowerId>.title`
- `<PowerId>.description`

所以你必须做两件事：

### 1）在 `BlightLocalization` 中加入 `powers` 文本

本次已经加入：

- `powers.BLIGHT_EFFIGY_WARD.title`
- `powers.BLIGHT_EFFIGY_WARD.description`

### 2）在 `ModLocalizationPatch` 中合并 `powers` 表

否则文本不会进入游戏的 `LocManager`。

结论：

- **自定义 Power 仅创建类还不够**
- **必须同时补 `powers` 本地化表**

---

## 六、在怪物 AI 中使用这个 Buff

你不能改原版怪物代码，所以必须在 `blight` 里通过 Harmony 接管后的 AI 或开场 Buff 来施加它。

最常见的两个位置：

### 1）开场施加

放进某个怪的 `ApplyBlightStartBuffs(...)`：

- `await/调用 PowerCmd.Apply<EffigyWardPower>(monster.Creature, 3m, monster.Creature, null)`

适合：

- 开局就有的被动
- 变异额外光环
- 精英 / Boss 开场强化

### 2）在 `MoveState` 行为中施加

放进某个招式的执行函数里：

- 先攻击
- 再 `PowerCmd.Apply<EffigyWardPower>(...)`

适合：

- 攻击后给自己加防护
- 给队友或自己叠层
- 作为某个专属技能的一部分

---

## 七、推荐接线步骤

如果你要给一个怪物正式加新 Buff，建议按下面顺序做：

1. 在 `st-s-2/src/Core/Models/Powers/` 找一个最像的原版 Power 参考
2. 在 `blight/Scripts/AI/Buffs/` 新建你的 `PowerModel`
3. 在 `blight/Scripts/Localization/BlightLocalization.cs` 增加 `powers` 文本
4. 在 `blight/Scripts/Localization/Patches/ModLocalizationPatch.cs` 确保 merge 了 `powers`
5. 在 `blight/Scripts/AI/Buffs/Patches/BlightPowerIconPatch.cs` 给开发期图标回退
6. 在对应怪物 AI 的 `ApplyBlightStartBuffs(...)` 或 `GenerateBlightStateMachine(...)` 中调用 `PowerCmd.Apply<T>()`
7. 构建并验证图标、描述、层数、实际效果是否一致

---

## 八、示例接入方式

如果你想给 `BygoneEffigy` 做一个新 Buff，可以直接在它的 AI 文件里这样接：

### 开场施加

- 精英或变异时，在 `ApplyBlightStartBuffs(...)` 中给它加 `EffigyWardPower`

### 招式施加

- 新建一个 `MoveState`
- 行为里调用 `PowerCmd.Apply<EffigyWardPower>(bygoneEffigy.Creature, amount, bygoneEffigy.Creature, null)`
- 意图一般配 `new BuffIntent()`，如果同时攻击，就再加 `SingleAttackIntent` 或 `MultiAttackIntent`

---

## 九、验证清单

进战后至少检查下面几项：

1. Buff 图标是否显示
2. HoverTip 标题是否正确
3. HoverTip 描述是否正确
4. 层数是否正确
5. 实际效果是否与描述一致
6. 普通怪 / 精英 / Boss / 变异的触发条件是否符合你的 AI 判定规则

---

## 十、常见坑

### 1）只写了 Power 类，没有本地化

表现：

- Buff 名称显示 key
- 描述为空或显示 key

### 2）只写了大图，没有小图回退

表现：

- 战斗条上的 Buff 图标缺失

### 3）直接 `new EffigyWardPower()`

不推荐。

虽然 `PowerCmd.Apply<T>()` 内部会正确通过 `ModelDb.Power<T>().ToMutable()` 创建运行时实例，但如果你手写别的创建流程，仍应遵循项目里已有习惯：

- 优先让 `ModelDb` 管 canonical
- 运行时使用 mutable 实例

### 4）描述和实际数值不一致

如果描述写“回合结束获得 3 格挡”，那实际逻辑就必须和层数 / 公式一致。

---

## 十一、建议的后续拆分

如果后面 Buff 变多，建议继续按下面结构整理：

- `blight/Scripts/AI/Buffs/`：Buff 类
- `blight/Scripts/AI/Buffs/Patches/`：图标或兼容性 Patch
- `blight/Scripts/AI/Bestiary/`：怪物 AI 中的接线逻辑

这样职责最清晰：

- Buff 自己管效果
- AI 自己管谁什么时候上 Buff
- Localization 自己管文本