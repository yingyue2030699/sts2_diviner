using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class PalmReading : DivinerCard
{
    public PalmReading()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCards(2, 0);
        WithBlock(7, 3);
        WithDivinerKeywordTips(DivinerKeywords.Scry, DivinerKeywords.GoodOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Palm Reading",
        "Gain !Block! Block. Scry 5. Good Omen: draw !Cards! cards.",
        "掌纹解读",
        "获得 !Block! 点格挡。预见 5。吉兆：抽 !Cards! 张牌。",
        ("upgradedDesc", "Gain !Block! Block. Scry 6. Good Omen: draw !Cards! cards.", "获得 !Block! 点格挡。预见 6。吉兆：抽 !Cards! 张牌。"),
        ("selectPrompt", "Choose cards to discard.", "选择要丢弃的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await DivinerCardActions.Scry(this, choiceContext, IsUpgraded ? 6 : 5);
        if (DestinyService.IsGoodOmen())
        {
            await CardPileCmd.Draw(choiceContext, 2, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
