# 05 开局 Buff 池与扩展指南

本文档说明三件事：
1. 当前已有哪些开局 Buff（按难度池划分）。
2. 新建一个开局 Buff 的标准流程。
3. 以「A4：所有敌人 +5% 最大生命值」为完整示例。

## 1. 当前开局 Buff 一览

### A3 池
- `BlightA3WeaknessModifier`
  - 效果：每场战斗开始时，所有玩家获得 1 层虚弱。
- `BlightA3EnemyStrengthModifier`
  - 效果：每场战斗开始时，所有敌人获得 1 点力量。

### A4 池
- `BlightA4EnemyMaxHpModifier`
  - 效果：每场战斗开始时，所有敌人获得 +5% 最大生命值（向上取整）。

### A5 池
- 当前为空（预留扩展）。

## 2. 运行时注入规则

位于 `Scripts/Patches/RunStartPatch.cs`：
- 对应难度的开局buff池子中抽取一个

说明：
- 词缀对象必须通过 `ModelDb.Modifier<T>().ToMutable()` 创建，不能直接 `new`，否则可能触发 `DuplicateModelException`。

## 3. 如何新增一个开局 Buff

### 步骤 1：创建 Modifier 类
建议放在 `Scripts/Modifiers/` 下，命名如：
- `BlightA4XxxModifier.cs`

模板要点：
- 继承 `BlightBaseModifier`。
- 覆盖 `Title` / `Description` / `IconPath`。
- 在 `AfterRoomEntered(AbstractRoom room)` 中判断 `CombatRoom` 后生效。

示例（核心结构）：

```csharp
public class BlightA4XxxModifier : BlightBaseModifier
{
    public override LocString Title => new LocString("modifiers", "BLIGHT_A4_XXX.title");
    public override LocString Description => new LocString("modifiers", "BLIGHT_A4_XXX.description");
    protected override string IconPath => ModelDb.Power<ArtifactPower>().ResolvedBigIconPath;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
        {
            return;
        }

        // 在这里实现你的开场效果
    }
}
```

### 步骤 2：注册到对应难度池
编辑 `Scripts/Modifiers/BlightModifierPool.cs`。

例如加到 A4 池：

```csharp
private static readonly List<Func<ModifierModel>> A4_Modifiers = new List<Func<ModifierModel>> {
    static () => ModelDb.Modifier<A4.BlightA4XxxModifier>().ToMutable(),
};
```

### 步骤 3：增加本地化文案
编辑 `Scripts/Localization/BlightLocalization.cs`。

当前本地化结构是：

1. 在 `English` 字典写英文
2. 在 `Chinese` 字典写中文
3. `Scripts/Localization/Patches/ModLocalizationPatch.cs` 会在切换语言时自动把对应文本注入 `modifiers` 表

推荐同时补中英两套；如果只补一套，至少先补英文，这样其它语言会安全回退到英文。

示例：

```csharp
["BLIGHT_A4_XXX.title"] = "Your Title",
["BLIGHT_A4_XXX.description"] = "Your description.",
```

```csharp
["BLIGHT_A4_XXX.title"] = "你的标题",
["BLIGHT_A4_XXX.description"] = "【关键词】你的描述",
```

### 步骤 4：编译并开新局验证
- 执行：`dotnet build blight/blight.csproj`
- 开启 A4 或更高新局，检查顶栏 Modifier 图标与悬浮描述。
- 进第一场战斗确认效果在战斗开始时触发。

## 4. 示例：A4 敌人 +5% 最大生命值

本次已实现文件：
- `Scripts/Modifiers/BlightA4EnemyMaxHpModifier.cs`
- `Scripts/Modifiers/BlightModifierPool.cs`（已加入 A4 池）
- `Scripts/Localization/BlightLocalization.cs`（已加入 title/description）

实现细节：
- 对 `combatRoom.CombatState.Enemies` 逐个处理。
- 增量公式：`ceil(enemy.MaxHp * 0.05)`。
- 使用 `CreatureCmd.GainMaxHp(monster, gain)`，同时提高最大生命与当前生命，保持开场血量状态自然。
