using System.Collections.Generic;
using System.Threading.Tasks;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Relics
{
    public sealed class BlightArmorRelic : RelicModel
    {
        private int _platingAmount = 3;

        public override RelicRarity Rarity => RelicRarity.Common;

        public override string PackedIconPath => ModelDb.Power<PlatingPower>().IconPath;

        protected override string BigIconPath => ModelDb.Power<PlatingPower>().ResolvedBigIconPath;

        public override bool ShowCounter => true;

        public override int DisplayAmount => PlatingAmount;

        [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
        public int PlatingAmount
        {
            get => _platingAmount;
            set
            {
                AssertMutable();
                _platingAmount = value;
                base.DynamicVars["PlatingPower"].BaseValue = value;
                InvokeDisplayAmountChanged();
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PlatingPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<PlatingPower>();

        public override bool IsAllowed(IRunState runState)
        {
            return BlightModeManager.IsBlightModeActive && base.IsAllowed(runState);
        }

        public override async Task AfterRoomEntered(AbstractRoom room)
        {
            if (!BlightModeManager.IsBlightModeActive || room is not CombatRoom)
            {
                return;
            }

            Flash();
            await PowerCmd.Apply<PlatingPower>(base.Owner.Creature, PlatingAmount, base.Owner.Creature, null);
        }
    }
}