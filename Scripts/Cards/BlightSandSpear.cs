using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlightMod.AI.Bestiary;
using BlightMod.AI.Buffs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.Cards;

public sealed class BlightSandSpear : CardModel
{
    private const decimal BaseDamage = 30m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(BaseDamage, ValueProp.Move) };

//消耗属性
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new CardKeyword[] { CardKeyword.Exhaust, CardKeyword.Retain };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] { HoverTipFactory.FromPower<SandShieldPower>(), StunIntent.GetStaticHoverTip() };

    // Reuse vanilla FranticEscape art to avoid extra asset packaging requirements.
    public override string PortraitPath => ModelDb.Card<FranticEscape>().PortraitPath;

    public override string BetaPortraitPath => ModelDb.Card<FranticEscape>().BetaPortraitPath;

    public BlightSandSpear()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        SandShieldPower? sandShield = cardPlay.Target.GetPower<SandShieldPower>();
        if (sandShield == null || sandShield.Amount <= 0)
        {
            return;
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithAttackerAnim("Cast", 0.5f)
            .BeforeDamage(async delegate
            {
                NHyperbeamVfx beamVfx = NHyperbeamVfx.Create(base.Owner.Creature, cardPlay.Target);
                if (beamVfx != null)
                {
                    NCombatRoom.Instance?.CombatVfxContainer.AddChild(beamVfx);
                    await Cmd.Wait(0.5f);
                }

                NHyperbeamImpactVfx impactVfx = NHyperbeamImpactVfx.Create(base.Owner.Creature, cardPlay.Target);
                if (impactVfx != null)
                {
                    NCombatRoom.Instance?.CombatVfxContainer.AddChild(impactVfx);
                }
            })
            .Execute(choiceContext);

        int shieldBefore = sandShield.Amount;
        int shieldAfter = await PowerCmd.ModifyAmount(sandShield, -1m, base.Owner.Creature, this);
        if (shieldBefore > 0 && shieldAfter <= 0 && cardPlay.Target.IsAlive && cardPlay.Target.IsMonster)
        {
            if (cardPlay.Target.Monster is MegaCrit.Sts2.Core.Models.Monsters.TheInsatiable)
            {
                await CreatureCmd.Stun(cardPlay.Target, TheInsatiableBlightAI.SandShieldMoveId);
            }
            else
            {
                await CreatureCmd.Stun(cardPlay.Target);
            }
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(10m);
    }
}