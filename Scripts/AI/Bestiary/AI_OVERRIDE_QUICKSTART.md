# 荒疫 AI 覆写速查（本轮对话经验浓缩）

本文件用于在新对话中快速让 Agent 进入状态。

---

## 1. 工作边界

- 只修改 `blight\` 目录下的内容。
- 不能直接改原版代码。
- 所有行为改动都通过 Harmony + `IBlightMonsterAI` 接线完成。
- 原版怪物实现优先查看：`st-s-2\src\Core\Models\Monsters\*.cs`
- 当前项目的 AI 接线入口：
  - `blight\Scripts\AI\IBlightMonsterAI.cs`
  - `blight\Scripts\AI\BlightMonsterDirector.cs`
  - `blight\Scripts\Patches\MonsterAIPatch.cs`
  - `blight\Scripts\AI\BlightAIContext.cs`

---

## 2. 这轮对话总结出的固定做法

### 2.1 只改 Buff，不改行动链

如果需求只是：

- 开局加 `Strength`
- 开局加 `Slippery`
- 开局加 `CurlUp`
- 开局加 `Artifact`
- 开局补原版已有 Buff 的层数差值

最稳妥写法：

1. 新增 `public sealed class XxxBlightAI : IBlightMonsterAI`
2. `ApplyBlightStartBuffs()` 里加效果
3. `GenerateBlightStateMachine()` 直接返回原版 `_moveStateMachine`
4. 在 `BlightMonsterDirector` 注册
5. HP 补丁保持全 0

适合：

- `AssassinRubyRaider`
- `AxeRubyRaider`
- `CrossbowRubyRaider`
- `BowlbugEgg`
- `BowlbugSilk`
- `Fogmog`
- `Inklet`
- `Byrdonis`
- `PhantasmalGardener`

### 2.2 改单个招式，但尽量保留原版链路

如果需求只是：

- 某个 move 改伤害
- 某个 move 多一层 Debuff
- 某个 move 改塞牌堆位置
- 某个 move 改 hit count

最稳妥写法：

1. 仅在 `BlightAIContext.ShouldOverrideMonsterAi(monster)` 条件下接管
2. 若只有变异分支需要改，则再加 `BlightAIContext.IsCurrentNodeMutant()`
3. 用 `AccessTools.Property(...)` 读原版私有数值
4. 用 `AccessTools.Method(...)` 调原版私有 move
5. 按原版 `FollowUpState` 重建状态链
6. 只替换目标 move，其余 move 尽量走原版逻辑

适合：

- `Flyconid`
- `TrackerRubyRaider`
- `Mawler`
- `SlitheringStrangler`
- `HunterKiller`
- `Chomper`
- `Parafright`
- `SpinyToad`
- `LouseProgenitor`
- `Tunneler`
- `PhrogParasite`

---

## 3. 本轮最常见坑点

### 3.1 别把“总层数”写成“补充层数”反了

如果原版 `AfterAddedToRoom()` 已经给了 Buff：

- `Inklet` 原版已有 `Slippery 1`
- `CubexConstruct` 原版已有 `Artifact 1`
- `PhrogParasite` 原版已有 `Infested 4`

此时应补“差值”，不是直接再给目标总量。

例如：

- 目标 `99` 层，原版已有 `1` 层 => 补 `98`
- 目标 `5` 层，原版已有 `4` 层 => 补 `1`

### 3.2 原版根本没有的 Buff，不要误以为“原版已有”

例如：

- `BygoneEffigy` 原版开局只有 `SlowPower`
- 原版没有 `Artifact`

所以如果需求说“A2+ 额外获得 1 层 Artifact”，那就是直接补 1，不存在“原版已有 1 层”的前提。

### 3.3 只要不想吃生命值加成，就把 HP 模板全清零

目标是“确保不被应用全局生命值加成”时：

- `A0HpAdd = 0`
- `A1To2HpAdd = 0`
- `A3To4HpAdd = 0`
- `A5PlusHpAdd = 0`
- `MutantHpAdd = 0`

本轮还修过几个遗漏：

- `LeafSlimeS`
- `LeafSlimeM`
- `TwigSlimeS`
- `TwigSlimeM`
- `LouseProgenitor`

### 3.4 只写 HP Patch 不够，很多怪还没注册 AI 类

如果一个文件里只有 `MinInitialHp` / `MaxInitialHp` Patch：

- 它并不会参与 `ApplyBlightStartBuffs`
- 也不会参与状态机覆写

需要：

1. 新增 `XxxBlightAI : IBlightMonsterAI`
2. 注册到 `BlightMonsterDirector`

### 3.5 注册表要先看当前内容再改

`BlightMonsterDirector.cs` 在这轮对话里被多次手工调整过。

修改前先读当前文件，避免：

- 补丁上下文失配
- 重复注册
- 手工新增内容被覆盖

### 3.6 受保护成员不要直接访问

例如：

- `MonsterModel.CastSfx`
- 某些 `AttackSfx`

如果访问级别不够，用：

`AccessTools.Property(typeof(MonsterModel), "CastSfx")!.GetValue(instance)`

或

`AccessTools.Property(typeof(TargetType), "AttackSfx")!.GetValue(instance)`

### 3.7 私有 move / 私有属性统一用 AccessTools

常见模式：

- 读私有属性：`AccessTools.Property(typeof(Xxx), "SomeDamage")`
- 调私有方法：`AccessTools.Method(typeof(Xxx), "SomeMove")`

如果只要保留原版实现，优先直接调用原版私有 move，而不是重写一大段动画和特效。

### 3.8 `AddChildSafely` 不是所有容器都有

本轮遇到过：

### 3.9 `MaxInitialHp => MinInitialHp` 时不要双补丁叠加

有些原版怪物的实现不是独立的最大血量，而是直接写成：

`public override int MaxInitialHp => MinInitialHp;`

这种情况下，如果 mod 同时对：

- `get_MinInitialHp`
- `get_MaxInitialHp`

都做同样的 `Postfix` 加值，就会发生二次叠加：

1. `MinInitialHp` 先被加一次
2. `MaxInitialHp` 取值时先走已经修改后的 `MinInitialHp`
3. `MaxInitialHp` 的 `Postfix` 又再加一次

最终可能出现：

- `min > max`
- 进入战斗时报 `System.InvalidOperationException`

这轮实际踩到的例子：

- `HunterKiller`
- `BygoneEffigy`
- `TheForgotten`
- `TheLost`

安全做法：

- 如果原版 `MaxInitialHp` 直接依赖 `MinInitialHp`
- 只给 `get_MinInitialHp` 加 HP 修正
- `get_MaxInitialHp` 保留空 `Postfix` 或不再追加同样的加值

排查时先去原版 `st-s-2\src\Core\Models\Monsters\Xxx.cs` 看：

- `MaxInitialHp` 是否 `=> MinInitialHp`
- 再决定能不能同时 patch 两边

- 某些容器类型上 `AddChildSafely` 不可用
- 可直接改为 `AddChild`

### 3.9 变异条件和精英条件要分清

- 普通怪：一般只有变异时覆写
- 精英：A2+ 就可能需要覆写或开场强化
- 精英变异：通常是在 A2+ 基础上进一步增强

因此不要把“精英 A2+ 基础效果”和“变异额外效果”混成一个条件。

例如：

- `Byrdonis`：A2+ +1 Strength；变异改为 +2 Strength
- `PhrogParasite`：A2+ 开局 Infested 从 4 变 5；变异另外改 `Infect` 的塞牌位置

### 3.10 只改目标 move 时，也要保证意图正确

如果招式从：

- 单段攻击 -> 多段攻击
- 纯攻击 -> 攻击 + 状态

要同步改 `Intent`：

- `SingleAttackIntent`
- `MultiAttackIntent`
- `StatusIntent`
- `DebuffIntent`
- `BuffIntent`
- `DefendIntent`

不然头顶显示会错。

---

## 4. 推荐工作流

1. 读 `AI_OVERRIDE_TUTORIAL.md`
2. 读原版怪物文件
3. 判断需求属于：
   - 开场 Buff
   - 单 move 改动
   - 整链改动
4. 检查当前 `BlightMonsterDirector.cs`
5. 在 `blight\Scripts\AI\Bestiary\XxxBlightAI.cs` 实现
6. 保证 HP 全为 0（若需求是不要全局 HP 加成）
7. 注册到 Director
8. `dotnet build ./blight`

---

## 5. 新对话可直接用的提示词模板

### 模板 A：只改开场 Buff

```text
我们在 STS2 的 mod 工程里工作，只修改 blight 目录，不改原版代码。
请先阅读 blight\Scripts\AI\Bestiary\AI_OVERRIDE_TUTORIAL.md。
原版怪物代码去 st-s-2\src\Core\Models\Monsters 里看。
根据教程修改 XxxBlightAI.cs。
需求：变异个体开局获得 1 层 Slippery。
要求：
1. 通过 Harmony / IBlightMonsterAI 实现
2. 不要应用全局生命值加成
3. 保持原版意图正常
4. 改完后注册到 BlightMonsterDirector
5. 最后运行 dotnet build ./blight 验证
```

### 模板 B：只改一个意图

```text
我们在 STS2 的 mod 工程里工作，只修改 blight 目录，不改原版代码。
请先阅读 blight\Scripts\AI\Bestiary\AI_OVERRIDE_TUTORIAL.md。
原版怪物代码去 st-s-2\src\Core\Models\Monsters 里看。
根据教程修改 XxxBlightAI.cs。
需求：变异个体的 YyyMove 改为 6*2，并向弃牌堆塞 2 张 Dazed。
要求：
1. 只覆写需要改的 move，尽量保留原版链路
2. 不要应用全局生命值加成
3. 意图显示必须正确
4. 改完后注册到 BlightMonsterDirector
5. 最后运行 dotnet build ./blight 验证
```

### 模板 C：原版已有 Buff，需要补差值

```text
我们在 STS2 的 mod 工程里工作，只修改 blight 目录，不改原版代码。
请先阅读 blight\Scripts\AI\Bestiary\AI_OVERRIDE_TUTORIAL.md。
原版怪物代码去 st-s-2\src\Core\Models\Monsters 里看。
根据教程修改 XxxBlightAI.cs。
需求：变异个体开局总共应有 99 层 Artifact，注意原版 AfterAddedToRoom 已经给了 1 层。
要求：
1. 只补差值，不要重复给总量
2. 不要应用全局生命值加成
3. 保持原版意图正常
4. 改完后注册到 BlightMonsterDirector
5. 最后运行 dotnet build ./blight 验证
```

---

## 6. 本轮已验证有效的构建命令

```text
dotnet build ./blight
```

---

## 7. 当前仍存在但不属于单怪改动的问题

- `SnappingJaxfruitBlightAI.cs` 仍有若干空引用警告
- 仓库里有一批既有 nullable warning
- 文档类请求完成后，优先不要顺手改这些无关问题，除非明确要求
