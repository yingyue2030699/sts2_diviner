using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class ReadTheRoom : DivinerCard
{
    public ReadTheRoom()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Scry);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Read the Room",
        "Scry 3.",
        "察言观势",
        "预见 3。",
        ("upgradedDesc", "Scry 5.", "预见 5。"),
        ("selectPrompt", "Choose cards to discard.", "选择要丢弃的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.Scry(this, choiceContext, IsUpgraded ? 5 : 3);
    }

    protected override void OnUpgrade()
    {
    }
}
