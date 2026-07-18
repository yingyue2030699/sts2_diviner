using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class Reconsult : DivinerCard
{
    public Reconsult()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Reconsult",
        "Exhaust. If you have divinated this combat, Divinate.",
        "重新占问",
        "消耗。如果你在本场战斗中占卜过，占卜。",
        ("upgradedDesc", "Retain. If you have divinated this combat, Divinate.", "保留。如果你在本场战斗中占卜过，占卜。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (DivinerCombatRuntime.HasDivinatedThisCombatFor(Owner))
        {
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Reconsult");
        }
    }

    protected override void OnUpgrade()
    {
    }
}
