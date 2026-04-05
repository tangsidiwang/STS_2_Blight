using System.Collections.Generic;
using System.Threading.Tasks;
using BlightMod.AI.Bestiary;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.AI.Buffs;

public sealed class TheInsatiableShriekPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => true;

    public override bool ShouldScaleInMultiplayer => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Stun) };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
        {
            return;
        }

        await PowerCmd.ModifyAmount(this, -result.UnblockedDamage, dealer, cardSource);
        if (Amount > 0)
        {
            return;
        }

        if (target.Monster is not TheInsatiable theInsatiable)
        {
            return;
        }

        Flash();
        await TheInsatiableBlightAI.AddSandSpearsToOpponents(theInsatiable);
        await CreatureCmd.Stun(Owner, TheInsatiableBlightAI.SandShieldMoveId);
        await PowerCmd.Remove(this);
    }
}
