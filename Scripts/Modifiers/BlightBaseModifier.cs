using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Modifiers
{
    /// <summary>
    /// 所有荒疫模式特有词缀（Modifier）的基类。
    /// 继承自原版 ModifierModel，在 RunState.CreateForNewRun 时混入游戏。
    /// </summary>
    public abstract class BlightBaseModifier : ModifierModel
    {
        // 游戏原本通过 ModelDb 通过反射加载 Modifier，必须要有这个以避开部分限制
        public override bool IsEquivalent(ModifierModel other)
        {
            return GetType() == other.GetType();
        }

        // 可以覆盖标题和描述，这里只是示范，实际应通过LocString提供翻译
        // public override LocString Title => new LocString("..."); 
    }
}
