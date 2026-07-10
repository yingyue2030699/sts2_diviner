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
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Dredge, DivinerKeywords.CountdownOfDestiny, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Escape from Destiny",
        "Doomed only. Gain 1 Countdown of Destiny. Escape from Destiny costs 1 more this combat.",
        "逃离命运",
        "仅限劫兆。获得 1 层命运倒计时。逃离命运在本场战斗中多消耗 1 点能量。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCombatRuntime.IncreaseDredgeCountdown(choiceContext, Owner, 1);
        DivinerCombatRuntime.IncreaseEscapeCostTax();
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

        modifiedCost = originalCost + DivinerCombatRuntime.EscapeCostTax;
        return DivinerCombatRuntime.EscapeCostTax != 0;
    }

    protected override void OnUpgrade()
    {
    }
}
