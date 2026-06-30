using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class Reconsult : DivinerCard
{
    public Reconsult()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
        WithCostUpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Reconsult",
        "If you have divinated this combat, Divinate.",
        "重新占问",
        "如果你在本场战斗中占卜过，占卜。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (DivinerCombatRuntime.HasDivinatedThisCombat)
        {
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Reconsult");
        }
    }

    protected override void OnUpgrade()
    {
    }
}
