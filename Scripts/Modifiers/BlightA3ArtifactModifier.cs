using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using BlightMod.Modifiers;

namespace BlightMod.Modifiers.A3
{
    /// <summary>
    /// 开局buff示例：所有玩家在每一场战斗开始时获得 1 层人工制品 (Artifact)。
    /// （增益型 buff，用于展示如果想在进阶模式中偶尔给予玩家好处该怎么做）
    /// </summary>
    public class BlightA3ArtifactModifier : BlightBaseModifier
    {
        public override async Task AfterRoomEntered(AbstractRoom room)
        {
            // 只有进入战斗房间时生效
            if (!(room is CombatRoom combatRoom))
            {
                return;
            }

            // 遍历并给予所有玩家人工制品 buff
            foreach (Creature playerCreature in combatRoom.CombatState.PlayerCreatures)
            {
                await PowerCmd.Apply<ArtifactPower>(playerCreature, 1m, null, null);
            }
        }
    }
}