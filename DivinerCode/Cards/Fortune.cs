using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards;

public class Fortune : DivinerCard
{
    public Fortune()
        : base(0, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithCards(2);
        WithKeywords([CardKeyword.Retain, CardKeyword.Exhaust]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Fortune",
        "Draw !Cards! cards.",
        "福运",
        "抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 2, Owner, false);
    }

    protected override void OnUpgrade()
    {
    }
}
