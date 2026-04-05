using System;
using System.Threading.Tasks;
using BlightMod.Modifiers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace BlightMod.Modifiers.A4
{
    /// <summary>
    /// A4 开局词缀示例：每场战斗开始时，所有敌人获得 +5% 最大生命值。
    /// </summary>
    public class BlightA4EnemyMaxHpModifier : BlightBaseModifier
    {
        private const decimal MaxHpIncreaseRatio = 0.05m;

        public override LocString Title => new LocString("modifiers", "BLIGHT_A4_ENEMY_MAX_HP.title");

        public override LocString Description => new LocString("modifiers", "BLIGHT_A4_ENEMY_MAX_HP.description");

        protected override string IconPath => ModelDb.Power<FeedingFrenzyPower>().ResolvedBigIconPath;

        public override async Task AfterRoomEntered(AbstractRoom room)
        {
            if (room is not CombatRoom combatRoom)
            {
                return;
            }

            foreach (Creature monsterCreature in combatRoom.CombatState.Enemies)
            {
                decimal gain = Math.Ceiling(monsterCreature.MaxHp * MaxHpIncreaseRatio);
                if (gain <= 0m)
                {
                    continue;
                }

                await CreatureCmd.GainMaxHp(monsterCreature, gain);
            }
        }
    }
}
