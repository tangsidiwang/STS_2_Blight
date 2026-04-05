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
    /// 开局被动(示例2)：所有怪物在每一场战斗开始时获得 1 层力量。
    /// </summary>
    public class BlightA3EnemyStrengthModifier : BlightBaseModifier
    {
        public override LocString Title => new LocString("modifiers", "BLIGHT_A3_ENEMY_STRENGTH.title");

        public override LocString Description => new LocString("modifiers", "BLIGHT_A3_ENEMY_STRENGTH.description");

        protected override string IconPath => ModelDb.Power<StrengthPower>().ResolvedBigIconPath;

        public override async Task AfterRoomEntered(AbstractRoom room)
        {
            if (!(room is CombatRoom combatRoom))
            {
                return;
            }

            // 遍历并给予所有怪物力量 buff
            foreach (Creature monsterCreature in combatRoom.CombatState.Enemies)
            {
                await PowerCmd.Apply<StrengthPower>(monsterCreature, 1m, null, null);
            }
        }
    }
}
