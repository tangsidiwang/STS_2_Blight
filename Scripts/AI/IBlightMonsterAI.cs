using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI
{
    public interface IBlightMonsterAI
    {
        /// <summary>
        /// 怪物ID，用于与原版怪物进行匹配绑定
        /// </summary>
        string TargetMonsterId { get; }

        /// <summary>
        /// 提供荒疫模式下重构的怪物意图状态机图谱（用于完全覆写原版状态机）
        /// </summary>
        MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel);

        /// <summary>
        /// 进阶强化回调：战斗开始时给予额外的 Buff（如进阶1的精英强化）。
        /// </summary>
        void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel);
    }
}