using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class LuckyBreak : DivinerCard
{
    public LuckyBreak()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithBlock(5, 2);
        WithCards(1);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Lucky Break",
        "Gain !Block! Block. Good Omen: Draw !Cards! card.",
        "好运脱身",
        "获得 !Block! 点格挡。吉兆：抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (DestinyService.IsGoodOmen(Owner))
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
