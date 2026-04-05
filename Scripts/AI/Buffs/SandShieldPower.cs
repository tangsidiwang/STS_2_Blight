// blight/Scripts/AI/Buffs/SandShieldPower.cs
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.AI.Buffs;

public sealed class SandShieldPower : PowerModel
{
    private const string SandSpearCardEntry = "BLIGHT_SAND_SPEAR";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // 免疫所有伤害/掉血（仅对拥有该 Buff 的单位）
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return amount;
        }

        // Blight Sand Spear is designed to pierce Sand Shield damage immunity.
        if (cardSource?.Id.Entry.Equals(SandSpearCardEntry, System.StringComparison.Ordinal) == true)
        {
            return amount;
        }

        return 0m;
    }

    // 免疫负面效果（Debuff Power）
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (target != Owner)
        {
            modifiedAmount = amount;
            return false;
        }

        if (canonicalPower.GetTypeForAmount(amount) != PowerType.Debuff)
        {
            modifiedAmount = amount;
            return false;
        }

        modifiedAmount = 0m;
        return true;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Flash();
        return Task.CompletedTask;
    }

    // 每回合掉一层（拥有者所在阵营回合结束时）
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side || Amount <= 0)
        {
            return;
        }

        await PowerCmd.TickDownDuration(this);
    }
}