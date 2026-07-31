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
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Dredge, DivinerKeywords.CountdownOfDestiny, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Escape from Destiny",
        "Gain 1 Countdown of Destiny.",
        "逃离命劫",
        "获得 1 层命运倒计时。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCombatRuntime.IncreaseDredgeCountdown(choiceContext, Owner, 1);
        DivinerCombatRuntime.RecordEscapePlayed(Owner);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not EscapeFromDestiny || !DivinerRelicHooks.IsFirstEscapeFree(card.Owner))
        {
            return false;
        }

        modifiedCost = 0;
        return originalCost != 0;
    }

    protected override void OnUpgrade()
    {
    }
}
