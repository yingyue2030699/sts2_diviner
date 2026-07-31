using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Cards;

public class EscapeFromDestiny : DivinerCard
{
    public EscapeFromDestiny()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithEnergy(1);
        WithDivinerKeywordTips(DivinerKeywords.Dredge, DivinerKeywords.CountdownOfDestiny, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Escape from Destiny",
        "Gain 1 Countdown of Destiny. This card costs {Energy:energyIcons()} more this combat.",
        "逃离命劫",
        "获得 1 层命运倒计时。本牌在本场战斗中多消耗 {Energy:energyIcons()}。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCombatRuntime.IncreaseDredgeCountdown(choiceContext, Owner, 1);
        DivinerCombatRuntime.IncreaseEscapeCost(this);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not EscapeFromDestiny)
        {
            return false;
        }

        if (DivinerRelicHooks.IsFirstEscapeFree(card.Owner))
        {
            modifiedCost = 0;
            return originalCost != 0;
        }

        int costIncrease = DivinerCombatRuntime.EscapeCostIncreaseFor(card);
        modifiedCost = originalCost + costIncrease;
        return costIncrease != 0;
    }

    protected override void OnUpgrade()
    {
    }
}
