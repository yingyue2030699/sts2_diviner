using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards;

public class DefendDiviner : DivinerCard
{
    public DefendDiviner()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithBlock(5, 3);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Defend",
        "Gain !Block! Block.",
        "防御",
        "获得 !Block! 点格挡。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }

    protected override void OnUpgrade()
    {
    }
}
