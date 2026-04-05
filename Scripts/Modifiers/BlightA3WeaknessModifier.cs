using System.Threading.Tasks;
using BlightMod.Modifiers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace BlightMod.Modifiers.A3
{
    /// <summary>
    /// 开局被动(示例1)：所有玩家在每一场战斗开始时会被施加 1 层虚弱。
    /// （类似于每日挑战的负面 Buff）
    /// </summary>
    public class BlightA3WeaknessModifier : BlightBaseModifier
    {
        public override LocString Title => new LocString("modifiers", "BLIGHT_A3_WEAKNESS.title");

        public override LocString Description => new LocString("modifiers", "BLIGHT_A3_WEAKNESS.description");

        protected override string IconPath => ModelDb.Power<WeakPower>().ResolvedBigIconPath;

        // 游戏进房时会调用的事件钩子
        public override async Task AfterRoomEntered(AbstractRoom room)
        {
            // 只有进入战斗房间时生效
            if (!(room is CombatRoom combatRoom))
            {
                return;
            }

            // 遍历并给予所有玩家虚弱 buff
            foreach (Creature playerCreature in combatRoom.CombatState.PlayerCreatures)
            {
                await PowerCmd.Apply<WeakPower>(playerCreature, 1m, null, null);
            }
        }
    }
}
