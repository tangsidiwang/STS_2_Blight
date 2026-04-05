# 荒疫 Mod 遗物实现与注册指南

本文总结 `blight` 工程里新增遗物的完整流程，适用于以后继续给荒疫模式添加专属遗物。

## 1. 基本原则

- **只能修改 `blight/`**：`st-s-2/` 仅用于查看原版实现，不能直接改。
- **优先走模型注册**：新增遗物本体应继承 `RelicModel`，不要试图改原版 `RelicPool` 源码。
- **用官方 mod 扩展点入池**：通过 `ModHelper.AddModelToPool<TPool, TRelic>()` 把遗物加入奖池。
- **模式限制用遗物自身或 Harmony 控制**：如果只想在荒疫模式出现，优先在遗物里覆写 `IsAllowed(IRunState)`；必要时再补额外掉落逻辑。
- **所有改动放在 mod 初始化阶段完成**：注册应在 `Mod.cs` 的 `Init()` 里尽早完成。

## 2. 当前项目中的参考实现

可直接参考：

- `blight/Scripts/Relics/BlightArmorRelic.cs`
- `blight/Scripts/Mod.cs`
- `blight/Scripts/Rewards/ForgeOptions/ForgeOptionUtility.cs`
- `blight/Scripts/Rewards/ForgeOptions/ForgeOptionPool.cs`
- `blight/Scripts/Localization/BlightLocalization.cs`
- `blight/Scripts/Localization/Patches/ModLocalizationPatch.cs`

其中 `BlightArmorRelic` 是一个完整的荒疫专属遗物示例：

- 有独立遗物类
- 在 `Mod.cs` 注册进遗物池
- 只允许荒疫模式使用
- 使用能力图标替代默认遗物图标
- 支持保存层数
- 可被锻造功能奖励增量强化

## 3. 创建新遗物的标准步骤

### 第一步：新增遗物类

建议放在：

- `blight/Scripts/Relics/`

基础模板：

1. 新建一个类，继承 `RelicModel`
2. 定义 `Rarity`
3. 如果有数值，使用 `DynamicVar` / `PowerVar`
4. 如果有运行时持久字段，用 `[SavedProperty(...)]`
5. 如果只在荒疫模式可用，覆写 `IsAllowed(IRunState)`
6. 在合适的回调里写效果，如：
   - `AfterRoomEntered(AbstractRoom room)`
   - `BeforeSideTurnStart(...)`
   - `AfterSideTurnStart(...)`
   - `AfterCombatEnd(CombatRoom room)`

### 第二步：在 `Mod.cs` 注册到奖池

当前做法是在 `Init()` 中注册，例如：

- `ModHelper.AddModelToPool<SharedRelicPool, 你的遗物类>();`

常用池：

- `SharedRelicPool`：公共遗物池
- 角色专属池：如原版对应角色的 `xxxRelicPool`

注意：

- 注册必须发生在内容池冻结前。
- 最稳妥的地方就是 `Mod.cs` 的初始化入口。

### 第三步：补本地化

遗物文本使用的是 `relics` 表，不是 `enchantments`。

要做两件事：

1. 在 `BlightLocalization.cs` 中增加键：
   - `XXX.title`
   - `XXX.description`
   - `XXX.flavor`
2. 在 `ModLocalizationPatch.cs` 中把 `relics` 表也合并进去

如果不补：

- 标题/描述会直接显示 key
- 某些 UI 或 HoverTip 可能出现异常文本

### 第四步：把遗物接入获得来源

如果遗物不是普通随机掉落，而是像荒疫锻造奖励这种特殊来源，有两种方式：

- **直接发遗物**：`RelicCmd.Obtain(...)`
- **已有遗物时增强其内部数值**：先 `player.GetRelic<T>()`，存在则直接改保存字段

`BlightArmorRelic` 当前就是这个思路：

- 没有铠甲遗物：创建后发放
- 已有铠甲遗物：直接增加 `PlatingAmount`

## 4. 图标实现建议

默认情况下，`RelicModel` 会按遗物 id 去找遗物图集资源。

如果没有对应资源，容易出现：

- 图标丢失
- 显示默认占位
- 显示错误资源

当前 `BlightArmorRelic` 的处理方式是直接复用 `PlatingPower` 的图标：

- 覆写 `PackedIconPath`
- 覆写 `BigIconPath`

这是目前最省事也最稳定的方案，适合“遗物效果本质上对应某个能力”的情况。

如果以后要做独立遗物图，再单独走资源导入。

## 5. 保存字段注意点

如果遗物有可成长数值，例如层数、计数器、累计值：

- 用普通字段保存运行时数据
- 用 `[SavedProperty]` 暴露可序列化属性
- 属性 setter 里同步更新：
  - `DynamicVars` 显示值
  - `DisplayAmount`
  - `Status`
  - UI 刷新（如 `InvokeDisplayAmountChanged()`）

以 `BlightArmorRelic` 为例：

- `PlatingAmount` 是保存字段
- 修改时同步 `base.DynamicVars["PlatingPower"].BaseValue`
- 再调用 `InvokeDisplayAmountChanged()`

## 6. 常见坑

### 6.1 不要直接修改 `st-s-2`

- 那里只是反编译参考。
- 真正实现必须落在 `blight/`。

### 6.2 不要只写遗物类不注册

只创建 `RelicModel` 子类而不调用 `AddModelToPool`，结果通常是：

- 游戏能扫描到模型
- 但不会进入任何奖池
- 随机掉落永远不会出现

### 6.3 遗物文本要走 `relics` 表

不要把遗物文本塞进：

- `enchantments`
- `powers`
- `modifiers`

正确表名是：

- `relics`

### 6.4 动态变量集合不要用反编译生成名

不要手写类似：

- `_003C_003Ez__ReadOnlySingleElementList<>`

这是反编译产物，mod 项目里不一定可用。建议直接写：

- `new DynamicVar[] { ... }`
- `Array.Empty<IHoverTip>()`
- 普通 `List<T>`

### 6.5 需要持久化的字段类型尽量简单

优先使用：

- `int`
- `bool`
- `decimal`
- `string`

复杂对象要非常谨慎，避免存档兼容问题。

### 6.6 注册时机不能太晚

`ModHelper.AddModelToPool(...)` 如果发生在池已冻结之后，会直接报错。

所以统一放在：

- `Mod.cs -> Init()`

### 6.7 仅荒疫模式掉落，不等于只靠入池

即便遗物进了共享池，也最好再加：

- `IsAllowed(IRunState)` 限制

这样即使别的系统扫到了这个遗物，也会被过滤掉。

## 7. 推荐工作流

以后新增遗物，按这个顺序走：

1. 在 `Scripts/Relics/` 新建类
2. 先只实现最小效果
3. 在 `Mod.cs` 注册到池
4. 补 `relics` 本地化
5. 编译确认通过
6. 进游戏验证：
   - 能否正常获得
   - 图标是否正确
   - HoverTip 是否正常
   - 存档读档是否保留状态
   - 是否只在荒疫模式生效
7. 如需特殊来源，再接锻造/事件/奖励补丁

## 8. 当前可直接复用的代码模式

### A. 荒疫模式限定遗物

适合：

- 只想让荒疫模式可获取或可生效的遗物

做法：

- 入池
- 覆写 `IsAllowed(IRunState)`
- 必要时再加掉落来源限制

### B. 可叠层成长遗物

适合：

- 层数类遗物
- 通过锻造反复强化的遗物

做法：

- 保存字段
- 展示计数
- 专门提供“存在则加层，不存在则发放”的辅助逻辑

### C. 复用能力图标的遗物

适合：

- 遗物效果和某个 power 高度一致
- 还没准备单独贴图

做法：

- 覆写 `PackedIconPath`
- 覆写 `BigIconPath`

## 9. 与锻造奖励联动的建议

如果以后再做类似“功能奖励转成遗物成长”的效果，建议统一采用以下范式：

1. 锻造项负责决定数值档位
2. 先检查玩家是否已有目标遗物
3. 没有则 `ToMutable()` 后 `RelicCmd.Obtain(...)`
4. 有则直接修改遗物内部保存字段
5. 记录日志，便于排查

这套模式已经在当前的铠甲遗物上验证通过，后续可以继续复用到：

- 开局获得力量
- 开局获得敏捷
- 每战开始获得格挡
- 额外药水槽
- 每层额外金币收益

## 10. 以后如果继续扩展，建议新增的目录

如果遗物越来越多，建议把辅助逻辑再拆一下：

- `blight/Scripts/Relics/`
- `blight/Scripts/Relics/Helpers/`
- `blight/Scripts/Relics/Registry/`

当前规模还不大，先保持简单即可。
