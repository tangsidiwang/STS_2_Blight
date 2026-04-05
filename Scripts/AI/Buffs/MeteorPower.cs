using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace BlightMod.AI.Buffs;

public sealed class MeteorPower : PowerModel
{
    private sealed class Data
    {
        public int SkillsPlayed;
    }

    private const string SkillsLeftKey = "SkillsLeft";
    private const int SkillsPerDazed = 3;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => DynamicVars[SkillsLeftKey].IntValue;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Dazed>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar(SkillsLeftKey, SkillsPerDazed) };

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card.Type != CardType.Skill || card.Owner?.Creature == null || !card.Owner.Creature.IsPlayer)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.SkillsPlayed++;

        int skillsLeft = SkillsPerDazed - data.SkillsPlayed % SkillsPerDazed;
        if (skillsLeft == SkillsPerDazed)
        {
            skillsLeft = 0;
        }

        DynamicVars[SkillsLeftKey].BaseValue = skillsLeft;
        InvokeDisplayAmountChanged();

        if (data.SkillsPlayed % SkillsPerDazed != 0)
        {
            return;
        }

        await CardPileCmd.AddToCombatAndPreview<Dazed>(card.Owner.Creature, PileType.Discard, 1, addedByPlayer: false);
        Flash();
    }
}
