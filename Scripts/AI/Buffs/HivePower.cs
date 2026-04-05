using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.AI.Buffs;

public sealed class HivePower : PowerModel
{
    private sealed class Data
    {
        public int HitsTaken;
    }

    private const string HitsLeftKey = "HitsLeft";
    private const int HitsPerWound = 3;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => DynamicVars[HitsLeftKey].IntValue;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Wound>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar(HitsLeftKey, HitsPerWound) };

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
        {
            return;
        }

        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.HitsTaken++;

        int hitsLeft = HitsPerWound - data.HitsTaken % HitsPerWound;
        if (hitsLeft == HitsPerWound)
        {
            hitsLeft = 0;
        }

        DynamicVars[HitsLeftKey].BaseValue = hitsLeft;
        InvokeDisplayAmountChanged();

        if (data.HitsTaken % HitsPerWound != 0)
        {
            return;
        }

        if (dealer != null && dealer.Monster != null && dealer.Player == null && dealer.PetOwner != null)
        {
            dealer = dealer.PetOwner.Creature;
        }

        IEnumerable<Creature> players = CombatState.Players.Select(player => player.Creature);
        await CardPileCmd.AddToCombatAndPreview<Wound>(players, PileType.Discard, 1, addedByPlayer: false);
        Flash();
    }
}