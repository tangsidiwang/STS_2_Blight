# 战利品、锻造与篝火重构 - 代码实现规划 (Rewards & Campfire Details)

按照需求，荒疫模式在结算掉落、新增三选一“锻造奖励”以及强化篝火的回复上都有特定的设计（参见 `03_Rewards_and_Campfire.md`）。本文档对此类系统进行落地编码规划。

## 1. 文件结构规划 (Directory Structure)
在 `blight/Scripts/Rewards/`与 `blight/Scripts/Campfire/` 目录下组织相应文件。

```text
blight/Scripts/
  ├── Rewards/
  │    ├── ForgeRewardItem.cs          // 新增的“锻造奖励”项（继承自原版Reward）
  │    ├── ForgeOptions/               // 选择锻造奖励后的“三选一”具体逻辑实现
  │    │    ├── IForgeOption.cs
  │    │    ├── ForgeOptionAddEnchant.cs    // [选1]：给牌添加随机高级附魔
  │    │    ├── ForgeOptionExtract.cs       // [选2]：提取附魔变消耗品/遗物
  │    │    └── ForgeOptionRelic.cs         // [选3]：获得随机遗物
  │    └── Patches/
  │         └── CombatRewardPatch.cs   // 战斗结算生成补丁：强制向其中塞入 ForgeRewardItem
  └── Campfire/
       └── Patches/
            └── CampfireHealPatch.cs   // 补丁：覆盖原版 HealRestSiteOption 的逻辑
```

## 2. 详细功能与接口设计

### 2.1 新奖励类型 `ForgeRewardItem.cs`
游戏结算页面里的每一个长条（例如“12金币”、“重击”）都是一个 `RewardItem`，我们也需要继承它。

```csharp
namespace BlightMod.Rewards {
    public class ForgeRewardItem : MegaCrit.Sts2.Core.Rewards.Reward {
        
        public EnchantmentRarity BaseRewardLevel { get; private set; }

        public ForgeRewardItem(EnchantmentRarity rarityLevel) : base(RewardType.Custom) { 
            this.BaseRewardLevel = rarityLevel;
            // 初始化名称可以带上品质前缀，例如 "锻造奖励 (罕见)"
        }

        // 核心：当玩家在结算菜单点击该长条时触发
        public override bool DefaultClaim() {
            // 我们不从0写UI，而是复用 Knowledge Demon 类似的 Generic Choice Screen / Event Screen
            UI.BlightForgeChoiceScreen.Open(this.BaseRewardLevel, this.OnCompleted);
            
            return false; // UI阻挡进程，OnCompleted再标记领取完成
        }

        private void OnCompleted() {
            this.IsDone = true; 
        }
    }
}
```

### 2.2 “三选一”的锻造选项接口与实现 `IForgeOption.cs`
该模块将挂载到复用的 `Knowledge Demon` 式选择面板上。面板将展示3个此类选项实例。

```csharp
namespace BlightMod.Rewards.ForgeOptions {
    /// <summary>
    /// 标准化选择面板逻辑的接口，适配原生Choice模型
    /// </summary>
    public interface IForgeOption {
        string Title { get; }
        string Description { get; }

        /// <summary>
        /// 当玩家确认选择此选项时触发的行为
        /// </summary>
        void OnSelect(Action onTaskFinished);
    }
}

// 选项1/2 具体实现样例：[为卡牌附魔]
public class ForgeOptionAddEnchant : IForgeOption {
    private EnchantmentRarity _rarityToOffer;
    
    public ForgeOptionAddEnchant(EnchantmentRarity rarity) {
        _rarityToOffer = rarity;
    }

    public string Title => "荒疫注入";
    public string Description => $"选择一张牌，附加1个随机的 {_rarityToOffer} 品质附魔。";
    
    public void OnSelect(Action onTaskFinished) {
        CardGroup masterDeck = BlightMod.Core.RunState.Player.Deck;
        GenericCardSelectionScreen.Open(masterDeck, 1, "选择要附魔的卡牌", (selectedCards) => {
            if(selectedCards.Count > 0) {
                // 调用02文档的附魔管理器灌入指定稀有度的附魔
                EnchantmentPool.ApplySpecificRarityEnchantment(selectedCards[0], _rarityToOffer);
            }
            onTaskFinished?.Invoke(); 
        });
    }
}

// 选项3 具体实现样例：[盲盒奖励 - 获得遗物 / 敲牌 / 删牌]
public class ForgeOptionRandomUtility : IForgeOption {
    private int _rolledUtilityId; // 0=遗物, 1=敲牌, 2=删牌

    public ForgeOptionRandomUtility() {
        // 使用游戏自带随机数种子防 SL
        _rolledUtilityId = RunState.MiscRng.RandomRange(0, 3);
    }

    public string Title => _rolledUtilityId == 0 ? "隐秘宝藏" : (_rolledUtilityId == 1 ? "巧手打磨" : "断除执念");
    public string Description => _rolledUtilityId == 0 ? "获得一件随机遗物。" : (_rolledUtilityId == 1 ? "选择一张牌升级。" : "从牌组中移除一张牌。");

    public void OnSelect(Action onTaskFinished) {
        if (_rolledUtilityId == 0) {
            // 获得遗物逻辑
            AbstractRelic relic = RelicLibrary.GetRandomRelic();
            BlightMod.Core.RunState.Player.GetRelic(relic);
            onTaskFinished?.Invoke();
        } else if (_rolledUtilityId == 1) {
            // 敲牌逻辑
            // ... 打开原生 SmithScreen
        } else {
            // 删牌逻辑
            // ... 打开原生 RemoveCardScreen
        }
    }
}
```

### 2.3 掉落物干涉器 `CombatRewardPatch.cs`
覆盖奖励生成，对于精英和变异战斗，分级计算锻造奖励的品质并强塞入展示列表。

```csharp
namespace BlightMod.Rewards.Patches {
    [HarmonyPatch(typeof(RunState), "GenerateCombatRewards")] 
    public class CombatRewardPatch {
        public static void Postfix(RunState __instance) {
            if (!BlightMod.Core.BlightModeManager.IsBlightModeActive) return;

            bool isElite = GameContext.CurrentRoom?.IsElite ?? false;
            // 变异状态由当前所处房间节点标记提供
            bool isMutant = BlightMod.Core.BlightModeManager.CurrentNodeIsMutant;

            // 锻造奖励只出现在精英怪和变异个体的战利品中
            if (isElite || isMutant) {
                
                EnchantmentRarity forgeLevel = EnchantmentRarity.Common;
                // 使用游戏自身 RNG 防止读档改变掉落
                float roll = RunState.MiscRng.RandomFloat();

                if (isMutant && isElite) {
                    // 变异精英：保底罕见，大概率/部分概率稀有
                    forgeLevel = roll < 0.40f ? EnchantmentRarity.Rare : EnchantmentRarity.Uncommon;
                } else if (isElite) {
                    // 普通精英：大概率普通，小概率罕见/稀有
                    if (roll < 0.10f) forgeLevel = EnchantmentRarity.Rare;
                    else if (roll < 0.30f) forgeLevel = EnchantmentRarity.Uncommon;
                } else if (isMutant) {
                    // 变异小怪：同精英（大概率普通）
                    if (roll < 0.05f) forgeLevel = EnchantmentRarity.Rare;
                    else if (roll < 0.20f) forgeLevel = EnchantmentRarity.Uncommon;
                }

                // 将我们携带着等级参数自定义的锻造奖励 Add 到原版战利品队列里面！
                __instance.CurrentRewards.Add(new ForgeRewardItem(forgeLevel));
            }
        }
    }
}
```

### 2.4 极效篝火修补计划 `CampfireHealPatch.cs`
针对篝火休息系统的增强相对直接，但不仅是替换参数，还可以考虑加上动画挂钩（可选）。

```csharp
namespace BlightMod.Campfire.Patches {
    
    [HarmonyPatch(typeof(HealRestSiteOption), "UseOption")]
    public class CampfireHealPatch {
        public static bool Prefix(HealRestSiteOption __instance) {
            if (!BlightMod.Core.BlightModeManager.IsBlightModeActive) return true;

            var player = BlightMod.Core.RunState.Player;
            
            // 1. 基准大幅度提升（比如50%上限 + 10固定，确保残血极强恢复）
            int baseHeal = (int)(player.MaxHealth * 0.5f) + 10;
            
            // 2. 如果后续进阶难度更高，可以带一些负面效果甚至额外惩罚以体现“荒疫”
            if (BlightMod.Core.BlightModeManager.BlightAscensionLevel >= 4) {
               // 高难度下：回血巨多，但是短暂流失 Max HP 或获得虚弱
               player.DecreaseMaxHealth(1);
               // 可以考虑推入屏幕提示: "生命流失于荒疫之中..."
               // EventTextManager.Show("荒疫侵蚀了生命上限...");
            }

            // 3. 执行生命恢复 (调用原版功能保证有绿字UI跳出)
            player.Heal(baseHeal);

            // 4. 返回 false，阻止原版那仅仅20/30%扣扣搜搜的回血逻辑被继续执行
            return false;
        }
    }
}
```

## 3. UI 研发预警 (UI Warnings)
本模块最大的挑战将存在于 `ForgeRewardItem` 的UI层：
由于需要在屏幕中间弹出一个**“三选一”**（选项A/B/C）而不是单纯的卡牌，往往很难复用直接的原版窗口。
*我们可能需要在 Godot UI 工具中（基于你给出的 `project.godot` 项目）或者纯代码新建一个实例化窗口，显示三个大按钮供回调监听。* 

## 4. 下一步开发建议
1.  **营地补丁快速实装验证：** 先从最简单的 `CampfireHealPatch.cs` 做起。拦截它，在普通休息后如果看到自己回了异常多（50%）的血，说明底层交互彻底畅通。
2.  **战利品伪造：** 创建干净简单的 `ForgeRewardItem`（先重写 `Claim()` 直接获得100金币测试即可），利用 `CombatRewardPatch` 塞进战利品屏幕，验证自定义类别加入不触发游戏崩溃。
3.  **完善复杂选项 UI：** 打通前两步后，最后再实现利用原生 Godot 组件手搓这个“锻造选择”的用户界面。