# Blight Mod - Modifiers 及 怪物AI系统 说明

## 1. `Modifiers` 文件夹是干什么的？
`Modifiers` 文件夹负责存放控制和修改 **全局游戏规则（Game Modifiers）** 的逻辑代码。
在《杀戮尖塔2》的源代码架构中，`ModifierModel` 是一种被广泛运用于“每日挑战（Daily Challenges）”或“定制模式（Custom Runs）”中的系统基类。
在我们的 **Blight Mod** 里，我们复用这个底层的规则修改器来实现**进阶 3（A3）以上的“开局被动效果”**。

**它的核心优势包括**：
- **无痕生效**：不会像旧版设计那样在玩家手牌里塞入一张冗余的“诅咒卡”去执行战前逻辑。它运行在后端，不会污染玩家の卡组。
- **系统挂钩**：作为正式的 Modifier，它拥有众多与房间状态相互交互的接口（如 `AfterRoomEntered`、`OnCombatStart` 等），能够轻易实现全局的 Buff、Debuff 或规则修改。

---

## 2. 新增示例代码解读

为了帮助理解具体用法，我们刚刚在项目中创建了两个实战用例。

### 2.1 开局增益型 Buff 示例 - 新 Modifier
**文件位置**：`Scripts/Modifiers/BlightA3ArtifactModifier.cs`

**示例效果**：
如果随到了该修改器，任何进入战斗的回合，所有的我方玩家都会自动获得 **1 层人工制品 (Artifact)**。这是一个典型的正面 Buff，用来展示如何通过此系统带来玩家奖励效果。

**核心代码机制**：
```csharp
foreach (Creature playerCreature in combatRoom.CombatState.PlayerCreatures)
{
    // 调用原版底层指令，平滑地给玩家上Buff
    await PowerCmd.Apply<ArtifactPower>(playerCreature, 1m, null, null);
}
```
*这套规则会在 `BlightModifierPool.cs` 里被按照种子哈希随机发放到当前对局的修改器列表中。*

---

### 2.2 替换实战怪物 AI 示例 - 新型蛤蟆怪 (Toadpole)
**文件位置**：`Scripts/AI/ToadpoleBlightAI.cs`

**示例效果**：
通过我们之前设计的 AI 导演拦截器（`BlightMonsterDirector`），我们捕获了原版 `Toadpole`（蛤蟆怪）生成行动的生命周期，并覆盖给它灌输了一套全新的**残暴输出AI**，让这只怪物拥有全新的头顶意图（Intent）和招式链。

**被覆盖后的行动循环**：
1. **行动1（强化，头顶显示Buff意图）**：立刻获得 3层 力量 `StrengthPower`，并获取 5点 护甲。
2. **行动2（猛击，头顶显示单体攻击意图）**：向玩家释放一个高达 12 点基础面板的致命单体打击。
3. 由于两个状态通过 `buffMove.FollowUpState = attackMove;` 及 `attackMove.FollowUpState = buffMove;` 被互相连接，这只蛤蟆将一直无限死循环这套连招。

**核心运用技巧**：
- 在写好行为类之后，我们在 `BlightMonsterDirector` 的静态构造函数内调用了 `Register(new Impl.ToadpoleBlightAI())`，使其生效。任何高难度进阶模式下遇到蛤蟆怪时，它都会瞬间化身为疯狂上力量攻击的杀手。

这样你就能够明白如何使用该框架去大范围更改任何想魔改的核心内容了。