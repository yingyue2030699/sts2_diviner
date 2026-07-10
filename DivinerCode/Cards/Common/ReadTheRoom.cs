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
        "Scry 6.",
        "察言观势",
        "预见 6。",
        ("upgradedDesc", "Scry 9.", "预见 9。"),
        ("selectPrompt", "Choose cards to discard.", "选择要丢弃的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.Scry(this, choiceContext, IsUpgraded ? 9 : 6);
    }

    protected override void OnUpgrade()
    {
    }
}
