# 荒疫敌人 AI 覆写完整教程

本文档面向当前 `blight` 工程，目标是指导你为 **所有普通敌人 / 精英 / Boss** 编写荒疫 AI，同时满足以下规则：

- **普通敌人**：
    - 普通个体在荒疫中默认保持**原版 A10 基线**行为与意图。
    - 只有 **变异个体** 才切换到你定义的荒疫 AI。
- **精英敌人**：
  - 在 **荒疫 A2 及以上** 才允许切换到荒疫 AI。
    - 在未达到 A2 时，即使是荒疫模式，也保持**原版 A10 基线**行为与意图。
  - 变异精英可以有更激进/不同难度分层的 AI。
- **Boss**：
  - 在 **荒疫 A5 及以上** 才允许切换到荒疫 AI。
    - 在未达到 A5 时保持**原版 A10 基线**行为与意图。
  - Boss 的变异/荒疫能力可以与普通个体完全不同。
- **不同难度下**：
  - 同一个怪物的 AI 链、意图显示、伤害、Buff 量都可以随荒疫难度变化。
- **扩展内容**：
  - 可以创建新的 `Intent`。
  - 可以创建新的 `Power/Buff`。
  - 可以新增新的行动（行为函数 / 状态机状态）。

这份文档会尽量把每个关键函数、接线点、原版对照方式都讲清楚。

---

## 1. 先理解：当前工程是怎么接管怪物 AI 的

### 1.1 入口总览

当前 `blight` 中和怪物 AI 直接相关的文件：

- `blight/Scripts/AI/IBlightMonsterAI.cs`
- `blight/Scripts/AI/BlightMonsterDirector.cs`
- `blight/Scripts/Patches/MonsterAIPatch.cs`
- `blight/Scripts/Core/BlightModeManager.cs`
- `blight/Scripts/AI/Bestiary/*.cs`

它们的职责分别是：

#### `IBlightMonsterAI`
定义“一个怪物的荒疫 AI 策略”需要实现什么。

当前接口：

- `string TargetMonsterId { get; }`
- `MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)`
- `void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)`

含义：

- `TargetMonsterId`：告诉 Director，这个 AI 对应哪个原版怪物。
- `GenerateBlightStateMachine(...)`：返回你自定义的状态机，用来 **完全替换原版 AI**。
- `ApplyBlightStartBuffs(...)`：在战斗开始时额外上 Buff、初始化状态、做一次性强化。

#### `BlightMonsterDirector`
它是总路由：

- 维护 `原版怪物ID -> IBlightMonsterAI实现` 的映射。
- 根据当前怪物找到是否有荒疫 AI。
- 让补丁层调用对应 AI。

关键函数：

- `Register(IBlightMonsterAI ai)`：注册一个怪物 AI。
- `HasCustomOverride(MonsterModel monster)`：查询是否存在自定义 AI。
- `TryGenerateStateMachine(MonsterModel monster)`：尝试生成替换状态机。
- `TryApplyStartBuffs(MonsterModel monster)`：尝试调用开场强化。

#### `MonsterAIPatch`
它 patch 了原版：

- `MonsterModel.SetUpForCombat`

原版流程中，每个怪物会在 `SetUpForCombat()` 内生成自己的 `MoveStateMachine`。
你的 mod 在这里插入逻辑：

1. 原版先生成原版状态机。
2. 如果当前属于荒疫模式，且满足覆写条件，则改写成你的状态机。
3. 然后调用 `ApplyBlightStartBuffs()`。

也就是说：

- **不动原版文件。**
- **只在战斗初始化后替换状态机。**
- **荒疫模式的默认基线是原版 A10。** 如果你手写状态机，不要回退到低难度数值/节奏。

#### `BlightModeManager`
它控制荒疫难度与变异判定，当前已经包含：

- `IsBlightModeActive`
- `BlightAscensionLevel`
- `IsAtLeastAscension(int level)`
- `IsNodeMutant(MapPoint point, string seed)`

它是你决定“当前要不要切 AI / 用哪套 AI”的核心依据。

---

## 2. 你真正要实现的行为规则

你当前想要的规则可以总结成一个 **AI 生效门槛矩阵**。

### 2.1 普通敌人

| 条件 | 是否改 AI |
|---|---|
| 荒疫关闭 | 否 |
| 荒疫开启，普通个体 | 否，保持原版 A10 基线 |
| 荒疫开启，变异个体 | 是 |

结论：

- 普通战的小怪，**默认不该被接管**。
- 只有变异节点中的目标怪物，才切成荒疫 AI。

### 2.2 精英敌人

| 条件 | 是否改 AI |
|---|---|
| 荒疫 A0-A1 | 否，保持原版 A10 基线 |
| 荒疫 A2+，普通精英 | 是 |
| 荒疫 A2+，变异精英 | 是，可使用更强分支 |

结论：

- 精英从 **A2** 开始进入“荒疫 AI 池”。
- 精英的变异分支可以更强。

### 2.3 Boss

| 条件 | 是否改 AI |
|---|---|
| 荒疫 A0-A4 | 否，保持原版 A10 基线 |
| 荒疫 A5+，普通 Boss | 是 |
| 荒疫 A5+，变异 Boss（如果你定义） | 是，可更强 |

结论：

- Boss 从 **A5** 才进入荒疫 AI。

---

## 3. 推荐先补一个“AI 是否生效”的总判断层

当前 `MonsterAIPatch.cs` 里是：

- 荒疫开启
- `BlightAscensionLevel >= 1`
- 就尝试替换状态机

这不够细，因为你要区分：普通怪 / 精英 / Boss / 变异 / 难度门槛。

### 3.1 推荐思路

不要把所有判断都塞进单个怪物文件里，而是分成两层：

#### 第一层：全局生效判断
在 Director 或新的辅助类中决定：

- 当前怪物是否允许覆写 AI。
- 当前该怪物应该走哪种模式：
  - 原版
  - 普通荒疫
  - 变异荒疫

#### 第二层：怪物文件内部做具体状态机分支
例如：

- `GenerateBlightStateMachine` 中再根据：
  - 是否变异
  - 当前荒疫等级
  - 是否精英/Boss

来切换状态链。

### 3.2 推荐增加一个 AI 判定辅助类

建议新增一个统一帮助类，例如：

- `blight/Scripts/AI/BlightAIContext.cs`

它可以提供类似下面的函数：

- `bool IsCurrentNodeMutant()`
- `bool IsEliteFight()`
- `bool IsBossFight()`
- `bool ShouldOverrideNormalMonsterAi(MonsterModel monster)`
- `bool ShouldOverrideEliteAi(MonsterModel monster)`
- `bool ShouldOverrideBossAi(MonsterModel monster)`
- `bool ShouldOverrideMonsterAi(MonsterModel monster)`

推荐规则：

```text
普通怪：仅变异时覆写
精英：A2+ 覆写
Boss：A5+ 覆写
```

### 3.3 如何判断当前战斗是普通 / 精英 / Boss

当前工程里你已经大量使用：

- `RunManager.Instance?.DebugOnlyGetState()?.CurrentMapPoint`
- `point.PointType == MapPointType.Monster`
- `point.PointType == MapPointType.Elite`
- `point.PointType == MapPointType.Boss`

所以推荐直接基于 `CurrentMapPoint.PointType` 判断。

示例逻辑：

```csharp
var state = RunManager.Instance?.DebugOnlyGetState();
var point = state?.CurrentMapPoint;

bool isMonster = point?.PointType == MapPointType.Monster;
bool isElite = point?.PointType == MapPointType.Elite;
bool isBoss = point?.PointType == MapPointType.Boss;
```

### 3.4 推荐的总判定伪代码

```csharp
if (!BlightModeManager.IsBlightModeActive)
    return false;

if (isBoss)
    return BlightModeManager.BlightAscensionLevel >= 5;

if (isElite)
    return BlightModeManager.BlightAscensionLevel >= 2;

if (isMonster)
    return IsCurrentNodeMutant();

return false;
```

这就是你全项目的 AI 覆写核心规则。

---

## 4. 原版 AI 是怎么写的：你要对照什么

### 4.1 原版怪物文件位置

所有原版怪物类在：

- `st-s-2/src/Core/Models/Monsters/<Monster>.cs`

每次你要改一个怪，先看它的原版文件。

### 4.2 先找这几样东西

每个怪物优先看：

1. `GenerateMoveStateMachine()`
2. HP getter：
   - `MinInitialHp`
   - `MaxInitialHp`
3. 伤害 getter：
   - 比如 `SlashDamage`、`BiteDamage`、`PeckDamage`
4. 行为函数：
   - 比如 `SlashMove()`、`BuffMove()`、`SpitMove()`
5. 可能的特殊逻辑：
   - 切阶段
   - 必须首回合某个动作
   - 依赖内部字段
   - 死亡后行为
   - 召唤物/分体协同

### 4.3 原版状态机通常长什么样

你在原版里会经常看到：

```csharp
MoveState move1 = new MoveState("BITE_MOVE", BiteMove, new SingleAttackIntent(BiteDamage));
MoveState move2 = new MoveState("BUFF_MOVE", BuffMove, new BuffIntent());
move1.FollowUpState = move2;
move2.FollowUpState = move1;
return new MonsterMoveStateMachine(new[] { move1, move2 }, move1);
```

你需要重点看四件事：

- **状态名**：如 `BITE_MOVE`
- **执行函数**：如 `BiteMove`
- **意图**：如 `new SingleAttackIntent(BiteDamage)`
- **状态流转**：`FollowUpState`

### 4.4 对照原版的目标

你不是为了照抄，而是为了弄清：

- 原版每回合怎么循环。
- 原版哪些动作是攻击，哪些是 Buff，哪些是 Debuff。
- 原版动作的动画 / 命名 / 数值是哪些字段。
- 原版是否有“首回合固定招式”。
- 原版是否有“随机分支”或“阶段切换”。

---

## 5. `GenerateBlightStateMachine` 到底要做什么

这是你编写 AI 的核心函数。

签名：

```csharp
MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
```

### 5.1 参数含义

#### `monster`
当前战斗中的怪物实例。

你可以用它：

- 作为 `DamageCmd.Attack(...).FromMonster(monster)` 的施法者
- 对它自己上 Buff：`PowerCmd.Apply<TPower>(monster.Creature, ...)`
- 读取怪物运行时对象：`monster.Creature`

#### `blightAscensionLevel`
当前荒疫难度。

你可以据此做：

- A1 / A2 / A3 / A5 不同链路
- 高难增加连段
- 高难改变多段 hit 数
- 高难给更强开场 Buff

### 5.2 返回值含义

返回一个新的 `MonsterMoveStateMachine`，完全替换原版 AI。

也就是说：

- 只要这个函数返回了状态机，原版 `GenerateMoveStateMachine()` 的结果就失效。

### 5.3 这个函数中常做的事

通常顺序是：

1. 算上下文条件
   - 是否变异
   - 当前难度
   - 是否精英 / Boss
2. 算数值
   - 攻击伤害
   - 多段次数
   - Buff 层数
3. 创建 `MoveState`
4. 串 `FollowUpState`
5. 决定起始状态
6. 返回 `MonsterMoveStateMachine`

### 5.4 推荐结构

```csharp
public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
{
    bool mutant = XxxAiHelper.IsCurrentNodeMutant();
    bool elite = XxxAiHelper.IsEliteFight();
    bool boss = XxxAiHelper.IsBossFight();

    int attackDamage = ...;
    int repeat = ...;
    int buffAmount = ...;

    var state1 = new MoveState(...);
    var state2 = new MoveState(...);
    var state3 = new MoveState(...);

    if (boss)
    {
        ...
    }
    else if (elite)
    {
        ...
    }
    else if (mutant)
    {
        ...
    }
    else
    {
        ...
    }

    return new MonsterMoveStateMachine(new[] { state1, state2, state3 }, state1);
}
```

---

## 6. `MoveState` 是什么，怎么写

原版 `MoveState` 构造函数是：

```csharp
new MoveState(string stateId, Func<IReadOnlyList<Creature>, Task> onPerform, params AbstractIntent[] intents)
```

也就是：

- `stateId`：状态名字
- `onPerform`：实际行动函数
- `intents`：头顶意图

### 6.1 `stateId`

这是状态内部 ID。

用途：

- 调试日志
- 历史记录
- 对照原版 move 名称

建议：

- 保持语义清楚
- 最好和原版同风格
- 荒疫专属可以加 `BLIGHT_` 前缀

例如：

- `BLIGHT_BITE`
- `BLIGHT_DOUBLE_SLASH`
- `BLIGHT_MUTANT_ROAR`

### 6.2 `onPerform`

这是状态执行时的实际逻辑。

最常见的内容：

- 打伤害
- 上 Power
- 生成格挡
- 召唤单位
- 切换内部标记

例如单攻：

```csharp
async targets =>
{
    foreach (Creature target in targets)
    {
        await DamageCmd.Attack(damage)
            .FromMonster(monster)
            .Targeting(target)
            .Execute(null);
    }
}
```

例如多段：

```csharp
async targets =>
{
    foreach (Creature target in targets)
    {
        await DamageCmd.Attack(damage)
            .WithHitCount(repeat)
            .FromMonster(monster)
            .Targeting(target)
            .Execute(null);
    }
}
```

例如上 Buff：

```csharp
async _ =>
{
    await PowerCmd.Apply<StrengthPower>(monster.Creature, 2m, monster.Creature, null);
}
```

### 6.3 `intents`

这里控制“头顶显示什么”。

比如：

- `new SingleAttackIntent(damage)`
- `new MultiAttackIntent(damage, repeat)`
- `new BuffIntent()`
- `new DebuffIntent()`
- `new DefendIntent()`
- `new SummonIntent()`
- `new HealIntent()`

也可以组合多个意图：

```csharp
new MoveState(
    "HEAVY_SLASH",
    HeavySlashMove,
    new SingleAttackIntent(damage),
    new DebuffIntent());
```

这代表：

- 头顶既显示攻击
- 也显示 Debuff 图标

---

## 7. 如何保持“普通状态仍走原版”

很多这轮实际需求并不需要“重写整套 AI”，只需要：

- 开场补一个 Buff
- 某个 move 多一层 Debuff
- 某个 move 改伤害或 hit count
- 某个 move 改塞牌堆位置

这时推荐优先采用“最小接管”策略，而不是把整只怪完全重写。

### 7.1 只改开场 Buff 的推荐写法

如果需求只是开场变化：

- `Strength`
- `Artifact`
- `Slippery`
- `CurlUp`
- `Infested`

推荐：

1. 新增 `XxxBlightAI : IBlightMonsterAI`
2. `ApplyBlightStartBuffs()` 中处理效果
3. `GenerateBlightStateMachine()` 直接返回原版 `_moveStateMachine`
4. 在 `BlightMonsterDirector` 注册
5. HP 模板全设为 0（如果要求不吃全局 HP 加成）

示例模式：

```csharp
public sealed class XxxBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "Xxx";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not Xxx typed || !BlightAIContext.ShouldOverrideMonsterAi(monster))
        {
            return;
        }

        if (!BlightAIContext.IsCurrentNodeMutant())
        {
            return;
        }

        _ = PowerCmd.Apply<SomePower>(typed.Creature, 1m, typed.Creature, null);
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
    }
}
```

### 7.2 原版已经给过 Buff 时，要补“差值”

这是非常容易踩坑的点。

如果原版 `AfterAddedToRoom()` 已经给了 Buff：

- `Inklet` 原版给 `Slippery 1`
- `CubexConstruct` 原版给 `Artifact 1`
- `PhrogParasite` 原版给 `Infested 4`

而你的目标是总量：

- `99`
- `99`
- `5`

那么你要补的是：

- `98`
- `98`
- `1`

而不是再直接给目标总量。

### 7.3 原版没有的 Buff，不要误判成“补差值”

例如 `BygoneEffigy` 原版开场只有 `SlowPower`，没有 `ArtifactPower`。

这类怪如果需求说“额外获得 1 层 Artifact”，那就是直接给 `1`，不是补差值。

### 7.4 只改一个 move 时的推荐写法

如果需求只改：

- `Pounce`
- `Slam`
- `Screech`
- `Burrow`
- `Constrict`

推荐：

1. 只在需要时覆写状态机
2. 用 `AccessTools.Property()` 读取原版私有属性
3. 用 `AccessTools.Method()` 调原版私有 move
4. 按原版 `FollowUpState` 重建状态链
5. 仅替换目标 move，其他 move 尽量走原版逻辑

### 7.5 头顶意图必须和实际效果同步

如果你把原版招式从：

- 单段攻击改为多段攻击
- 纯攻击改为攻击 + 状态
- 纯 Buff 改成 Buff + Defend

务必同步修改 Intent：

- `SingleAttackIntent`
- `MultiAttackIntent`
- `StatusIntent`
- `DebuffIntent`
- `BuffIntent`
- `DefendIntent`

否则战斗逻辑虽然对，头顶意图会错。

### 7.6 常见反射写法

读取私有属性：

```csharp
int damage = (int)AccessTools.Property(typeof(Xxx), "SomeDamage")!.GetValue(typed)!;
```

调用私有方法：

```csharp
private static Task InvokeOriginalMove(Xxx typed, string methodName, IReadOnlyList<Creature> targets)
{
    return (Task)AccessTools.Method(typeof(Xxx), methodName)!.Invoke(typed, new object[] { targets })!;
}
```

访问受保护属性：

```csharp
string castSfx = (string)AccessTools.Property(typeof(MonsterModel), "CastSfx")!.GetValue(monster)!;
```

### 7.7 Director 修改前先读当前文件

`BlightMonsterDirector.cs` 往往会被多轮对话和手工编辑反复修改。

因此注册前请先读当前内容，避免：

- patch 上下文失效
- 重复注册
- 把用户手工改动覆盖掉

### 7.8 如果只是防止全局 HP 加成，五个 HP 常量全部设 0

```csharp
public const int A0HpAdd = 0;
public const int A1To2HpAdd = 0;
public const int A3To4HpAdd = 0;
public const int A5PlusHpAdd = 0;
public const int MutantHpAdd = 0;
```

这点对以下类型尤其常见：

- 普通模板怪
- 只想改 Buff / 意图，但不想改血量
- 后补接线的旧文件

### 7.9 本项目里一个高效的工作顺序

1. 读本教程
2. 打开原版怪物文件
3. 判断是“开场 Buff”还是“单 move 改动”
4. 改 `blight\Scripts\AI\Bestiary\XxxBlightAI.cs`
5. 确保 HP 全 0
6. 注册到 `BlightMonsterDirector`
7. 跑 `dotnet build ./blight`

### 7.10 新对话提示词建议

可以直接在新对话里给出类似下面的提示：

```text
我们在 STS2 的 mod 工程里工作，只修改 blight 目录，不改原版代码。
请先阅读 blight\Scripts\AI\Bestiary\AI_OVERRIDE_TUTORIAL.md 和 AI_OVERRIDE_QUICKSTART.md。
原版怪物代码去 st-s-2\src\Core\Models\Monsters 里看。
根据教程修改 XxxBlightAI.cs。
要求：
1. 通过 Harmony / IBlightMonsterAI 实现
2. 不要应用全局生命值加成
3. 保持原版意图正常
4. 改完后注册到 BlightMonsterDirector
5. 最后运行 dotnet build ./blight 验证
```

这点非常关键。

你的目标不是让所有怪都被统一接管，而是：

- 普通怪只有变异时才接管
- 精英要到 A2
- Boss 要到 A5

### 7.1 最推荐的做法

在 `MonsterAIPatch` 或 `BlightMonsterDirector` 增加统一判断。

例如：

```csharp
if (!BlightAIContext.ShouldOverrideMonsterAi(__instance))
{
    return;
}
```

只有通过后才：

- `TryGenerateStateMachine`
- `TryApplyStartBuffs`

### 7.2 为什么不要在每个怪物文件里“返回原版状态机”

因为你拿不到原版 `GenerateMoveStateMachine()` 的 protected 实现，且不应该反射强行回调。

正确思路是：

- **不满足覆写条件时，根本不要替换状态机。**
- 这样自然保留原版行为与意图。

这就是最稳定的“普通状态仍保持原版”的实现方式。

---

## 8. 如何给不同难度写不同 AI

你明确提到：

- 不同难度下，变异个体 AI 也可能不同。

这意味着你不只是改数值，而是可能：

- A1：只换开场
- A2：加入新招式
- A3：多段次数增加
- A4：回合循环改变
- A5：加入专属大招

### 8.1 建议把“数值”和“链路”分开

推荐每个怪物文件内至少分两层：

#### `XxxBlightNumbers`
负责：

- `GetHpAdd(...)`
- `GetDamage(...)`
- `GetRepeat(...)`
- `GetBuffAmount(...)`

#### `XxxBlightAI`
负责：

- 读上下文
- 建状态机
- 组织状态流转

### 8.2 示例：按难度切链路

```csharp
if (boss && blightAscensionLevel >= 5)
{
    // Boss专属链
}
else if (elite && blightAscensionLevel >= 2)
{
    if (mutant)
    {
        // 精英变异链
    }
    else
    {
        // 精英普通荒疫链
    }
}
else if (mutant)
{
    // 普通怪的变异链
}
```

### 8.3 示例：按难度切意图数值

```csharp
int repeat = blightAscensionLevel switch
{
    <= 1 => 2,
    <= 3 => 3,
    _ => 4,
};
```

然后：

```csharp
new MultiAttackIntent(damage, repeat)
```

这样头顶意图数字会同步变化。

---

## 9. 如何修改数值（HP / 伤害 / hit 数 / Buff 量）

### 9.1 HP 修改

HP 最稳妥的方法就是你现在已经在做的：

- patch `get_MinInitialHp`
- patch `get_MaxInitialHp`

例如：

```csharp
[HarmonyPatch(typeof(Toadpole), "get_MinInitialHp")]
public static class ToadpoleMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += 3;
    }
}
```

### 9.2 伤害修改

如果你只想改原版技能伤害，不改 AI，可以 patch 原版伤害 getter：

- `get_BiteDamage`
- `get_WhirlDamage`
- `get_SlashDamage`

如果你已经完全改 AI，也可以直接在 `GenerateBlightStateMachine()` 里算新的伤害值，然后喂给 `Intent + DamageCmd`。

### 9.3 多段 hit 数修改

原版 getter 不一定有 “重复次数” 属性。

所以 hit 数更常见的改法不是 Harmony patch，而是：

- 你在自定义 AI 里直接决定 `repeat`
- `new MultiAttackIntent(damage, repeat)`
- `DamageCmd.Attack(damage).WithHitCount(repeat)`

### 9.4 Buff 量修改

比如力量、荆棘、格挡、虚弱层数，通常直接在你的执行函数里控制：

```csharp
await PowerCmd.Apply<StrengthPower>(monster.Creature, 2m, monster.Creature, null);
```

或者按难度：

```csharp
decimal strength = blightAscensionLevel >= 4 ? 3m : 2m;
```

---

## 10. 如何写“新行为”

新行为本质上就是新的 `MoveState` 执行函数。

### 10.1 行为函数最常见的 6 类

#### 1）单攻
```csharp
await DamageCmd.Attack(damage).FromMonster(monster).Targeting(target).Execute(null);
```

#### 2）多段攻击
```csharp
await DamageCmd.Attack(damage).WithHitCount(repeat).FromMonster(monster).Targeting(target).Execute(null);
```

#### 3）自 Buff
```csharp
await PowerCmd.Apply<StrengthPower>(monster.Creature, amount, monster.Creature, null);
```

#### 4）给玩家上 Debuff
```csharp
await PowerCmd.Apply<WeakPower>(target, amount, monster.Creature, null);
```

#### 5）攻击 + Debuff
先伤害再上负面：

```csharp
await DamageCmd.Attack(damage).FromMonster(monster).Targeting(target).Execute(null);
await PowerCmd.Apply<VulnerablePower>(target, 1m, monster.Creature, null);
```

#### 6）纯功能动作
例如切阶段、记状态、充能等。

你可以在文件内放内部字段或辅助状态，但要注意：

- `IBlightMonsterAI` 默认是无状态的
- 如果你需要“跨回合记忆”，优先用状态机结构，而不是用静态字段

### 10.2 什么时候不用复制原版行为函数

大多数情况，你没必要直接调用原版怪物类的私有动作函数。

更稳妥的方法是：

- 阅读原版行为效果
- 用 `DamageCmd` / `PowerCmd` / 其他命令自己重写同样效果

因为：

- 原版私有函数不稳定
- 反射调用容易脆弱
- 重写效果更可控

---

## 11. 如何修改意图显示

“意图”有两层含义：

1. **行为本身是什么**（真正执行什么）
2. **头顶显示什么图标/数字/提示文本**

这两者可以相同，也可以故意不同。

### 11.1 原版现成意图类型

你当前最常用的就是原版 `MegaCrit.Sts2.Core.MonsterMoves.Intents` 下这些：

- `SingleAttackIntent`
- `MultiAttackIntent`
- `BuffIntent`
- `DebuffIntent`
- `DefendIntent`
- `SummonIntent`
- `HealIntent`
- `StatusIntent`
- `StunIntent`
- `SleepIntent`
- `EscapeIntent`
- `UnknownIntent`
- `HiddenIntent`
- `DeathBlowIntent`
- `CardDebuffIntent`

### 11.2 一个动作可以挂多个意图

例如：

```csharp
new MoveState(
    "CURSE_STRIKE",
    CurseStrikeMove,
    new SingleAttackIntent(damage),
    new DebuffIntent());
```

这通常用于：

- 攻击 + 上负面
- 攻击 + 加格挡
- 攻击 + 召唤

### 11.3 `SingleAttackIntent` 和 `MultiAttackIntent` 的数字怎么来的

#### `SingleAttackIntent`
可以传固定值：

```csharp
new SingleAttackIntent(12)
```

也可以传函数：

```csharp
new SingleAttackIntent(() => currentDamage)
```

后者更适合：

- 动态伤害
- 难度变化
- 依赖当前状态的伤害

#### `MultiAttackIntent`
可传：

```csharp
new MultiAttackIntent(damage, repeat)
```

或者：

```csharp
new MultiAttackIntent(damage, () => repeat)
```

### 11.4 如果意图数字和实际伤害不一致会怎样

会直接误导玩家。

所以请始终保证：

- `Intent` 的数值
- `DamageCmd` / `PowerCmd` 真正执行的数值

是一致的。

最好的办法是：

- 提前算一份 `damage` / `repeat` / `buffAmount`
- 同时喂给 Intent 和执行逻辑

---

## 12. 如何创建“新意图”

如果原版 `BuffIntent`、`DebuffIntent`、`SingleAttackIntent` 不够表达你的怪物动作，你可以自己创建一个新 `Intent`。

### 12.1 新意图的继承关系

最基础的是继承：

- `AbstractIntent`

如果你是攻击类图标，通常继承：

- `AttackIntent`

### 12.2 新意图最少需要实现什么

#### 继承 `AbstractIntent` 时至少要实现：

- `IntentType`
- `IntentPrefix`
- `SpritePath`

通常还要覆盖：

- `GetIntentLabel(...)`
- `GetIntentDescription(...)`
- `GetAnimation(...)`（可选）

### 12.3 新意图示例：攻击并施加腐蚀

可以做一个：

- 图标显示攻击 + 特殊文本
- HoverTip 写明“造成 X 伤害并施加 Y 层腐蚀”

推荐做法：

- 如果只是组合语义，通常不一定要做新 Intent，直接 `SingleAttackIntent + DebuffIntent` 就够了。
- 如果你需要 **独有图标 / 独有文字 / 独有动画名**，再自定义 Intent。

### 12.4 新意图的本地化

原版 `AbstractIntent` 默认读：

- 表：`intents`
- key：`<IntentPrefix>.title`
- key：`<IntentPrefix>.description`

这意味着：

- 如果你写自定义 Intent，最好也在 `intents` 表里补本地化。

而你当前 mod 的 `ModLocalizationPatch.cs` 只合并了：

- `enchantments`
- `modifiers`

如果你要加自定义 Intent 文本，建议把本地化补丁扩展成也能 merge：

- `intents`

例如新增：

- `BlightLocalization.GetIntents(language)`
- 在 `ModLocalizationPatch` 里 `MergeTable(__instance, "intents", ...)`

这样你的新意图标题/描述才会正常显示。

---

## 13. 如何创建“新 Buff / 新 Power”

你明确说了：你有可能创建新的 Buff 给敌人。

在 STS2 这里，Buff 一般就是 `PowerModel`。

### 13.1 新 Buff 的本质

你要新建一个类，通常放在：

- `blight/Scripts/Powers/`
- 或 `blight/Scripts/AI/Powers/`

类一般继承：

- `PowerModel`

### 13.2 新 Power 需要关心什么

最重要的是：

- `Type`：Buff / Debuff
- `StackType`
- `Title`
- `Description`
- 实际效果触发点

### 13.3 实际效果从哪里触发

Power 的效果通常不是“定义了就生效”，还要实现对应 hook / override / patch。

你需要先看原版某个类似 Power 是怎么写的，例如：

- `st-s-2/src/Core/Models/Powers/StrengthPower.cs`
- `ArtifactPower.cs`
- `ThornsPower.cs`
- `WeakPower.cs`

做法通常是：

1. 先找一个最像你的效果的原版 Power
2. 复制设计思路
3. 把逻辑替换成你的效果

### 13.4 Power 的显示文本

原版 `PowerModel` 默认从 `powers` 表里读：

- `<PowerId>.title`
- `<PowerId>.description`

所以如果你增加新的 Power，本地化也要支持 `powers` 表。

而你当前 `ModLocalizationPatch` 还没有 merge `powers`。

建议扩展：

- `BlightLocalization.GetPowers(language)`
- `MergeTable(__instance, "powers", ...)`

### 13.5 图标来源

`PowerModel` 会尝试从约定路径找图：

- packed icon
- big icon
- beta icon
- 找不到就 missing icon

如果你要正式做新 Buff，建议补图标资源，否则 UI 体验会差。

### 13.6 怪物 AI 中如何使用新 Buff

创建好 `YourCustomPower` 后，在 AI 行为里直接：

```csharp
await PowerCmd.Apply<YourCustomPower>(monster.Creature, 1m, monster.Creature, null);
```

或对玩家：

```csharp
await PowerCmd.Apply<YourCustomPower>(target, 2m, monster.Creature, null);
```

---

## 14. 如何创建“新行为 + 新意图 + 新 Buff”组合招式

这是最常见的“变异怪专属招式”。

例如你想做：

- `MutantCorruptingStrike`
- 效果：造成伤害 + 施加自定义腐蚀 + 自己获得 1 层力量

推荐结构：

1. 新建 `CorruptingIntent`（如果原版意图不够）
2. 新建 `CorruptedBloodPower`
3. 在怪物 AI 里增加一个 `MoveState`

示例流程：

```csharp
var corruptingStrike = new MoveState(
    "BLIGHT_CORRUPTING_STRIKE",
    async targets =>
    {
        foreach (Creature target in targets)
        {
            await DamageCmd.Attack(damage).FromMonster(monster).Targeting(target).Execute(null);
            await PowerCmd.Apply<CorruptedBloodPower>(target, debuffAmount, monster.Creature, null);
        }

        await PowerCmd.Apply<StrengthPower>(monster.Creature, 1m, monster.Creature, null);
    },
    new SingleAttackIntent(damage),
    new DebuffIntent(),
    new BuffIntent());
```

如果你需要独有图标，再换成你的自定义 Intent。

---

## 15. 如何写“普通 / 变异 / 精英 / Boss”多套 AI

建议不要一个函数里全靠 if 堆满，而是分层写。

### 15.1 推荐分 4 种状态机构建函数

例如在某个怪物文件里：

- `BuildNormalMutantStateMachine(...)`
- `BuildEliteStateMachine(...)`
- `BuildEliteMutantStateMachine(...)`
- `BuildBossStateMachine(...)`

然后总入口：

```csharp
public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
{
    bool mutant = XxxAiContext.IsCurrentNodeMutant();
    bool elite = XxxAiContext.IsEliteFight();
    bool boss = XxxAiContext.IsBossFight();

    if (boss)
    {
        return BuildBossStateMachine(monster, blightAscensionLevel, mutant);
    }

    if (elite)
    {
        return mutant
            ? BuildEliteMutantStateMachine(monster, blightAscensionLevel)
            : BuildEliteStateMachine(monster, blightAscensionLevel);
    }

    return BuildNormalMutantStateMachine(monster, blightAscensionLevel);
}
```

这样代码更清楚，也方便后期维护所有敌人。

### 15.2 普通怪不需要写“普通非变异链”

因为普通非变异怪根本不应该进入荒疫 AI。

也就是说：

- 只要它进入了 `GenerateBlightStateMachine`
- 对普通怪来说，默认就该是“变异链”

这能大幅减少工作量。

---

## 16. 如何处理“首回合固定动作”

原版很多怪物第一回合不是随机，而是固定一招。

### 16.1 最简单的方法

把你想要的首回合状态作为 `MonsterMoveStateMachine` 的 initial state。

例如：

```csharp
return new MonsterMoveStateMachine(states, openingMove);
```

### 16.2 `MustPerformOnceBeforeTransitioning`

`MoveState` 有个属性：

- `MustPerformOnceBeforeTransitioning`

它的作用是：

- 在至少执行一次之前，状态机不能从这个状态离开。

虽然很多简单链路不一定需要它，但对“必须开场先做一次”的状态很有用。

---

## 17. 如何处理随机分支

如果你需要一个怪在多个招式之间随机切换，你需要自己做一个自定义 `MonsterState`，或者在状态切换上加入随机逻辑。

### 17.1 当前 `MoveState` 默认只会返回单一 `FollowUpState`

`MoveState.GetNextState()` 默认返回：

- `FollowUpState?.Id`
- 或 `FollowUpStateId`

它不自带随机选择。

### 17.2 推荐做法

如果你真的需要复杂分支：

- 自定义一个中间 `MonsterState`
- 在 `GetNextState(Creature owner, Rng rng)` 里根据 `rng` 返回不同下一个状态 ID

这适合：

- 50% 攻击 / 50% Buff
- 避免连续两回合同一招
- 不同难度增加随机池

### 17.3 什么时候不建议用随机

如果是批量给全敌人写变异 AI，前期建议：

- **先用固定链路**
- 后期再给少数重点怪做随机分支

因为固定链路更容易测、更稳。

---

## 18. 如何对照原版的动画 / move 名 / 行为表现

### 18.1 move 名称

原版 `MonsterModel` 的本地化 move 名通常是：

- `monsters:<MonsterId>.moves.<MoveId>.title`

如果你沿用原版 move id，可以更贴近原版语义。

但这不是硬要求。

### 18.2 动画

怪物的攻击/施法动画很多是在行为函数中隐式触发，或者依赖原版 Animator。

如果你只是用 `DamageCmd` / `PowerCmd` 自己拼动作，通常也能正常表现，但未必完全等同原版节奏。

因此：

- **先保证功能与意图正确**
- 再追求和原版完全一致的动画表现

### 18.3 特殊怪要特别注意

有些怪不是简单的一只怪：

- `Door`
- `Rocket`
- `Crusher`
- `DecimillipedeSegment*`
- 召唤型怪
- 分体 Boss

这类怪物可能：

- 依赖场景节点
- 依赖别的怪是否存活
- 依赖内部字段/背景节点/特效驱动

这类怪建议先从“数值增强 + 简单 AI 替换”开始，别第一版就做特别复杂。

---

## 19. 推荐的文件组织方式

你现在已经采用：

- `blight/Scripts/AI/Bestiary/<Monster>BlightAI.cs`

这是对的。

### 19.1 单敌人单文件建议继续保持

每个文件建议包含：

- `internal static class <Monster>BlightContext/Numbers`
- `public sealed class <Monster>BlightAI : IBlightMonsterAI`
- 该怪的 HP / Damage Patch
- 该怪特有的小型辅助函数

### 19.2 文件内推荐结构

推荐顺序：

1. `using`
2. `namespace`
3. `Numbers / Context / Helper`
4. `XxxBlightAI`
5. `HarmonyPatch` 数值补丁
6. 特殊 Intent / 特殊 Power（如果只被这个怪使用）

如果某个 Intent / Power 会被多个怪复用，建议拆到：

- `blight/Scripts/AI/Intents/`
- `blight/Scripts/Powers/`

---

## 20. 推荐你现在立刻补的基础设施

如果你准备给“所有敌人和精英、Boss”系统化写 AI，我建议先做下面这些基础文件。

### 20.1 `BlightAIContext.cs`

统一提供：

- `IsCurrentNodeMutant()`
- `IsMonsterFight()`
- `IsEliteFight()`
- `IsBossFight()`
- `ShouldOverrideMonsterAi(MonsterModel monster)`

### 20.2 `BlightMonsterCategory.cs`（可选）

如果你想更清楚地区分规则，也可以做枚举：

- `Normal`
- `Elite`
- `Boss`
- `Unknown`

### 20.3 `BlightLocalization` 扩展

建议新增：

- `GetIntents(language)`
- `GetPowers(language)`

然后在 `ModLocalizationPatch.cs` 增加：

- merge `intents`
- merge `powers`

因为只要你要做新意图、新 Buff，这两张表迟早要补。

---

## 21. 实战流程：给一个怪写完整变异 AI 的步骤

下面是我建议的标准流程。

### 第 1 步：读原版怪物文件

打开：

- `st-s-2/src/Core/Models/Monsters/<Monster>.cs`

记录：

- `TargetMonsterId`
- `GenerateMoveStateMachine()`
- HP getter
- 伤害 getter
- 行为函数名
- 特殊机制

### 第 2 步：确定生效条件

先问自己：

- 它是普通怪 / 精英 / Boss？
- 它在什么荒疫难度开始切 AI？
- 它是否只有变异才切？

### 第 3 步：写数值层

整理：

- HP 增量
- 攻击伤害
- 多段 hit 数
- Buff 量
- 变异额外值

### 第 4 步：先做最小状态机

只做 2-3 个状态：

- 攻击
- Buff
- 特殊招

先保证：

- 能出招
- 意图正确
- 数值正确

### 第 5 步：再分普通荒疫 / 变异 / 精英 / Boss 分支

逐步加复杂度。

### 第 6 步：写开场强化

在 `ApplyBlightStartBuffs` 中做：

- 变异开场 Buff
- 高难初始层数
- Boss 特殊状态

### 第 7 步：注册到 Director

在：

- `blight/Scripts/AI/BlightMonsterDirector.cs`

中 `Register(new Bestiary.XxxBlightAI());`

### 第 8 步：构建并进战验证

至少看：

- 是否只在该生效条件下接管 AI
- 普通状态是否仍走原版
- 变异状态是否切链
- 精英 A2 是否开始切换
- Boss A5 是否开始切换
- 意图数值是否与实伤一致
- 开场 Buff 是否命中

---

## 22. 常见坑

### 坑 1：普通怪被错误接管
原因：你在全局 patch 里只判断了 `A>=1`，没有细分普通/精英/Boss。

解决：

- 统一加 `ShouldOverrideMonsterAi`。

### 坑 2：意图对了，实际伤害不对
原因：Intent 和 `DamageCmd` 用了不同数值。

解决：

- 先算一个变量，再同时传给两边。

### 坑 3：变异逻辑在房间重进或重开时不稳定
原因：没有使用确定性节点判定。

解决：

- 统一用 `BlightModeManager.IsNodeMutant(point, seed)`。

### 坑 4：Boss/特殊怪动画异常
原因：原版怪依赖特殊内部逻辑、背景节点、场景驱动。

解决：

- 第一版先少动复杂 Boss 的动作结构。
- 先保数值和回合逻辑正确。

### 坑 5：自定义 Intent / Power 没有文本
原因：没有 merge `intents` / `powers` 本地化表。

解决：

- 扩展 `ModLocalizationPatch`。

### 坑 6：开场 Buff 没触发
原因：虽然替换了状态机，但没执行 `ApplyBlightStartBuffs()`。

解决：

- 检查 `MonsterAIPatch.cs` 调用链。

### 坑 7：你改的是 `Spiken`，但战斗开始就先多了 2 层荆棘
原因：

- 你想改的是 **行动状态 `Spiken`** 的效果。
- 但 `ApplyBlightStartBuffs()` 里可能还额外给怪物上了荆棘。
- 结果测试时看起来像“`Spiken` 还是原值”或者“总层数不对”。

典型表现：

- 你明明把普通个体 `Spiken` 改成了 `3`。
- 结果进战后看到的荆棘层数像是 `2 + 3`，或者测试中总是比预期多 `2`。

解决：

- 分清 **开场 Buff** 和 **状态机里的 Buff 行动** 是两条不同链路。
- 如果你只想改 `Spiken`，就不要在 `ApplyBlightStartBuffs()` 里重复上同类 Buff。
- 测试时先确认怪物的第一层 Buff 到底来自：
    - `ApplyBlightStartBuffs()`
    - 还是 `MoveState("...SPIKEN...")`

### 坑 8：你给怪物写了专属 HP patch，但它还是吃到了全局血量倍率
原因：

- 项目里还存在一个全局敌人血量 patch，例如：
    - `DifficultyStatPatch.cs`
- 你虽然给某个怪写了专属 `MinInitialHp/MaxInitialHp` patch，
- 但如果全局 patch 没有正确识别“这个怪已经有专属 AI/专属数值”，它仍然会在创建 `Creature` 后继续叠一次血量倍率。

典型表现：

- 日志显示：
    - 原版 `26`
    - 文件内加值后变成 `126`
- 但实战里怪物血量却是 `163`
- 这种通常就是：
    - `126 × 1.30 ≈ 163`
    - 被 A5 全局血量倍率再次加成了。

解决：

- 在全局血量 patch 中，优先过滤已注册专属 AI 的怪物。
- 过滤逻辑不要只依赖单一字段，建议同时检查：
    - `monster.Id.Entry`
    - `monster.GetType().Name`

### 坑 9：你以为“荒疫模式默认就是原版 A10 AI”，但实战却出现了低进阶意图/数值
原因：

- 项目整体**设计目标**确实是建立在原版 A10 基线上。
- 这一点主要由 `blight/Scripts/Patches/A10BaselinePatch.cs` 保证：
    - patch `RunManager.HasAscension`
    - patch `AscensionManager.HasLevel`
    - 在荒疫模式下把原版 `A1-A10` 判定都视为满足
- 但这只对**仍在走原版判定链 / 原版状态机生成链**的代码天然成立。
- 一旦怪物进入你手写的 `GenerateBlightStateMachine()`，你就已经在**自己重建一套 AI**：
    - 状态顺序是否还是原版 A10，由你决定
    - 伤害 / Buff / 次数是否还是原版 A10，由你决定
    - 如果你写死了低难度数值，或者拿到的不是 A10 基线值，怪物就会表现成“低进阶 AI”

典型表现：

- 你以为“荒疫模式 = 原版 A10”，所以手写状态机时直接抄了原版动作名。
- 但进战后发现：
    - 伤害偏低
    - Buff 层数偏低
    - 行动节奏像原版低进阶
- 尤其常见于：
    - 普通怪被 `MonsterAIPatch.cs` 过早统一接管
    - `GenerateBlightStateMachine()` 里手写了数值
    - 或者通过反射读取属性，但该路径并没有按你预期体现 A10 基线

根因拆解：

- `A10BaselinePatch.cs` 只能保证“原版的 ascension 判断函数”返回 A10 基线结果。
- 它**不能自动修正**你手写 AI 里的：
    - 常量
    - MoveState 链
    - 自定义伤害计算
    - 自定义 Buff 数值
- 所以：
    - **保留原版 `_moveStateMachine`** 的 AI，通常更容易自然保持 A10 基线
    - **手写状态机** 的 AI，必须显式确认每个数值是不是 A10 基线

当前项目里最容易导致这个问题的位置：

- `blight/Scripts/Patches/MonsterAIPatch.cs`
    - 现在的逻辑是：荒疫开启且 `BlightAscensionLevel >= 1` 就尝试覆写
    - 这会让普通怪也进入自定义 AI
    - 一旦进入自定义 AI，就不再等于“自动继承原版 A10”
- `blight/Scripts/AI/Bestiary/<Monster>BlightAI.cs`
    - 如果这里 `return` 原版 `_moveStateMachine`，通常比较安全
    - 如果这里重写了状态机，就必须自己保证它是 A10 基线或更高

推荐做法：

- 先修正 `MonsterAIPatch.cs`：
    - 普通怪：仅变异时覆写
    - 精英：A2+ 覆写
    - Boss：A5+ 覆写
- 写 `GenerateBlightStateMachine()` 时，先决定自己属于哪一类：
    - **保留原版 A10 基线**：直接复用原版 `_moveStateMachine`，只补开场 Buff / 局部 patch
    - **完全重写 AI**：把 A10 基线值显式写成常量或统一函数，不要默认“会自动继承”
- 对手写 AI，至少逐项确认：
    - 单段攻击伤害
    - 多段攻击伤害与段数
    - Buff / Debuff 层数
    - 开局强化
    - 回合循环顺序

一句话总结：

- **荒疫模式的 A10 基线，只能保证原版链路是 A10。**
- **只要你接管并手写了 AI，就要自己负责把它写成 A10 基线。**
- 推荐在 `BlightMonsterDirector.HasCustomOverride()` 内统一封装判断。
- 如果某个怪要走“原版血量 + 文件内加值，但不吃全局血量”，那就必须保证：
    - 专属 HP patch 保留
    - 全局 HP patch 对它直接 `return`

### 坑 10：开场 Buff 补丁放在 `Patches` 里，后面很容易和专属 AI 脱节
原因：

- 有些开场强化本质上是“某个怪的专属战斗初始化逻辑”，但早期为了图快，可能单独写在：
    - `blight/Scripts/Patches/*.cs`
- 这样短期能跑，但后面一旦你：
    - 给该怪补了 `IBlightMonsterAI`
    - 把 `ApplyBlightStartBuffs()` 接上
    - 或者把一部分怪移到 `Scripts/AI/Bestiary/`
- 就很容易出现职责分裂：
    - 一部分开场 Buff 在 AI 文件里
    - 一部分还散落在全局 patch 里

典型表现：

- 你以为“这个怪的开场强化都在 `ApplyBlightStartBuffs()` 里”。
- 结果实战发现：
    - Buff 重复上了两次
    - 某个怪移除了 AI 内的开场强化后，Buff 还在
    - 或者你只改了 AI 文件，漏掉了 `Patches` 里的旧补丁

解决：

- 只要某个开场 Buff 明显是“单怪/单系怪专属逻辑”，优先放到：
    - `blight/Scripts/AI/Bestiary/`
- 如果该怪已经注册了 `IBlightMonsterAI`：
    - 优先写进 `ApplyBlightStartBuffs()`
- 如果该怪还没有专属 AI，但只是需要一个很小的初始化补丁：
    - 也建议在 `AI/Bestiary/` 下单开文件，命名为：
        - `XxxStartBuffPatch.cs`
- 尽量避免把“怪物专属初始化逻辑”长期留在通用 `Patches/` 目录。

一句话总结：

- **怪物专属开场强化，最好跟怪物 AI 文件放在一起。**
- 这样后续排查“Buff 从哪来的”时，不容易漏查。

### 坑 11：给怪物补了专属 AI 之后，别忘了清理旧的通用初始化补丁
原因：

- 一个怪物从“仅有通用 patch”演进到“拥有专属 `IBlightMonsterAI`”时，最容易遗漏旧逻辑。
- 尤其是下面这种迁移：
    - 先在 `MonsterModel.SetUpForCombat` 的通用 patch 里给 `LivingFog` / `GasBomb` 上 Buff
    - 后来又给 `LivingFog` 写了 `ApplyBlightStartBuffs()`
- 如果旧的通用 patch 还保留着对 `LivingFog` 的处理，结果就会重复施加。

典型表现：

- `LivingFog` 这类怪物开局获得的 Buff 层数翻倍。
- 或者你在日志里看到 `ApplyBlightStartBuffs()` 已执行，但旧 patch 仍然额外命中。

解决：

- 每次把某个怪迁入专属 AI 后，反查下面两类位置：
    - `blight/Scripts/Patches/`
    - `blight/Scripts/AI/Bestiary/` 下其他共享 patch 文件
- 确认是否仍有：
    - `if (__instance is LivingFog || __instance is GasBomb)`
    - 或类似“多怪共用”的旧判断
- 如果专属 AI 已经覆盖该职责，就把旧判断删掉或只保留仍需要的怪。

一句话总结：

- **给怪物升级成专属 AI 时，要同步清理旧的共享补丁。**
- 不然最常见的问题不是“没生效”，而是“重复生效”。

这次 `DampCultist` 的实际踩坑就是一个典型例子：

- 你在 `DampCultistBlightAI.cs` 里写了：
        - `get_MinInitialHp`
        - `get_MaxInitialHp`
        - `get_IncantationAmount`
    这些 `HarmonyPatch`
- 但如果没有在 `BlightMonsterDirector` 里 `Register(new Bestiary.DampCultistBlightAI())`，
- 那么 `BlightMonsterDirector.HasCustomOverride(monster)` 仍然会返回 `false`。
- 结果就是：
        - 文件内的专属 HP patch 先加了一次血
        - `DifficultyStatPatch_Health` 又把它当成“普通未覆写怪”再乘一次全局生命倍率
- 最终实战血量就会明显高于你在怪物文件里手算的预期值。

额外注意：

- 仅仅写 `HarmonyPatch` 并不等于“这个怪已经被 Director 识别为专属覆写怪”。
- `HasCustomOverride()` 只认注册表，不会自动扫描某个文件里有没有对应 patch。
- 所以只要你希望某个怪：
        - 吃专属 AI
        - 或者只是为了跳过全局 HP patch
    都应该给它补一个 `IBlightMonsterAI` 实现并注册进 `BlightMonsterDirector`。

如果你暂时不想重写整套状态机，也至少要提供一个最小实现，用来：

- 让 Director 正确识别它属于“专属覆写怪”
- 避免全局血量补丁重复叠加
- 再配合单独的 `HarmonyPatch` 去改某个数值（例如 `IncantationAmount`）

### 坑 9：`MonsterAIPatch` 用反射设置 `MoveStateMachine` 时报 `Property set method not found`
原因：

- `MoveStateMachine` 属性没有公开 setter。
- 直接对属性调用 `SetValue(...)` 会抛：
    - `System.ArgumentException: Property set method not found.`

典型表现：

- 进战时直接报错。
- 堆栈落在：
    - `BlightMod.Patches.MonsterAIPatch.Postfix(MonsterModel __instance)`

解决：

- 不要反射设置 `MoveStateMachine` 属性。
- 直接写底层字段：
    - `MonsterModel._moveStateMachine`
- 也就是说，把替换状态机的逻辑写成“直接改字段”，而不是“改属性”。

### 坑 10：攻击命令报 `Already set to target opponents of attacker`
原因：

- `DamageCmd.Attack(...).FromMonster(monster)` 已经把目标设成“攻击者的对手”。
- 如果你之后又手动调用 `.Targeting(target)`，就会重复设置目标。

典型表现：

- 报错：
    - `System.InvalidOperationException: Already set to target opponents of attacker`

解决：

- 如果你的行为和原版一样是“对默认敌对方出手”，那就：
    - 用 `.FromMonster(monster)`
    - 不要再额外 `.Targeting(...)`
- 只有在你明确要指定非常规目标时，才手动 `Targeting(...)`。

### 坑 11：状态机顺序一旦改错，会出现负层数 Buff
原因：

- 某些怪的 AI 顺序不是随便排的。
- 某个动作可能依赖上一个动作先给自己叠 Buff。
- 如果你把顺序改成“先消耗，再上 Buff”，就会出现负层数。

`Toadpole` 的典型案例：

- 原版循环是：
    - `Whirl -> Spiken -> Spike Spit -> Whirl`
- 前排只是 **初始起手** 不同：
    - 前排先 `Spiken`
    - 后排先 `Whirl`
- 但后续循环仍然保持上面那条原版链。

错误改法示例：

- 把它改成：
    - `Whirl -> Spike Spit -> Spiken`
- 这样后排怪会在还没叠荆棘时就先执行 `Spike Spit`，导致荆棘被扣成负数。

解决：

- 不要只看“第一回合”，要同时看：
    - **初始分支**
    - **后续循环**
- 对照原版时，优先确认：
    - 哪个状态只是起手入口
    - 哪些 `FollowUpState` 才是真正长期循环

### 坑 12：开着游戏 `dotnet build` 成功了，但游戏里测试仍然是旧逻辑
原因：

- `blight.dll` 被 `SlayTheSpire2.exe` 占用。
- 构建虽然成功，但 `Copy Mod` 阶段没能把新 dll 覆盖到游戏 `mods/blight/` 下。

典型表现：

- 终端里 `dotnet build` 显示成功。
- 但同时有类似：
    - `The process cannot access the file ... blight.dll because it is being used by another process`
- 你进游戏后看到的仍然是旧行为/旧报错。

解决：

- 测试 mod 改动前，尽量：
    1. 关掉游戏
    2. 重新 `dotnet build`
    3. 确认 dll 已成功复制到游戏目录
    4. 再启动游戏测试

如果不这样做，你可能会误判“代码没修好”，其实只是游戏还没加载到新 dll。

---

## 23. 最推荐的开发顺序

如果你真的要给全敌人做荒疫 AI，建议顺序如下：

1. **先补统一 AI 判定层**
   - 普通怪 / 精英 / Boss 的生效门槛
2. **先做精英和 Boss 的门槛控制**
   - A2 / A5
3. **先做普通怪的变异链模板**
   - 攻击 / Buff / 强化攻击 三段
4. **先覆盖简单怪**
   - 单攻 / 多段 / Buff 型
5. **再做特殊机制怪**
   - 召唤 / 分体 / 阶段切换 / 特殊动画
6. **最后再做自定义 Intent / 新 Power**
   - 只有原版语义不够时再加

这样最稳。

---

## 24. 一份推荐模板骨架

下面是一份推荐骨架（不是最终代码，只是结构示意）：

```csharp
namespace BlightMod.AI.Bestiary;

internal static class XxxBlightNumbers
{
    public static bool IsMutant() => ...;
    public static bool IsElite() => ...;
    public static bool IsBoss() => ...;

    public static int GetAttackDamage(int a, bool mutant, bool elite, bool boss) => ...;
    public static int GetRepeat(int a, bool mutant, bool elite, bool boss) => ...;
    public static int GetBuffAmount(int a, bool mutant, bool elite, bool boss) => ...;
    public static int GetHpAdd(int a, bool mutant, bool elite, bool boss) => ...;
}

public sealed class XxxBlightAI : IBlightMonsterAI
{
    public string TargetMonsterId => "Xxx";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
        bool mutant = XxxBlightNumbers.IsMutant();
        bool elite = XxxBlightNumbers.IsElite();
        bool boss = XxxBlightNumbers.IsBoss();

        if (boss)
        {
            ...
            return;
        }

        if (elite)
        {
            ...
            return;
        }

        if (mutant)
        {
            ...
        }
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        bool mutant = XxxBlightNumbers.IsMutant();
        bool elite = XxxBlightNumbers.IsElite();
        bool boss = XxxBlightNumbers.IsBoss();

        int damage = XxxBlightNumbers.GetAttackDamage(blightAscensionLevel, mutant, elite, boss);
        int repeat = XxxBlightNumbers.GetRepeat(blightAscensionLevel, mutant, elite, boss);
        int buff = XxxBlightNumbers.GetBuffAmount(blightAscensionLevel, mutant, elite, boss);

        var attack = new MoveState(...);
        var multi = new MoveState(...);
        var powerUp = new MoveState(...);

        if (boss)
        {
            ...
        }
        else if (elite)
        {
            ...
        }
        else
        {
            ... // 普通怪在这里通常就是“变异链”
        }

        return new MonsterMoveStateMachine(...);
    }
}
```

---

## 25. 最后一句：你真正的核心原则

如果只记住一条，那就是：

**不要去“模拟保留原版 AI”，而是“不满足条件时根本不替换原版状态机”。**

这样你才能稳定做到：

- 普通怪普通状态保持原版
- 精英在 A2 之前保持原版
- Boss 在 A5 之前保持原版
- 只有进入条件后才切入荒疫 AI
- 变异链可以独立设计
- 不同荒疫难度下可以继续细分行为和意图

---

## 26. 你下一步最值得立刻做的事

如果要把这套体系真正投入批量开发，我建议下一步先做三件事：

1. 在 `blight` 中新增统一 `AIContext` 判定层。
2. 调整 `MonsterAIPatch.cs`，让它按“普通怪/精英/Boss门槛”决定是否覆写。
3. 给 `BlightLocalization` 补 `intents` 和 `powers` 的 merge 支持。

这三步做完，你后面批量给所有敌人写荒疫 AI 会轻松很多。
