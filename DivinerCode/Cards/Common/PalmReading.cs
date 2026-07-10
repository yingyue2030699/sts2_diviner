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
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithCards(2, 1);
        WithBlock(9, 3);
        WithDivinerKeywordTips(DivinerKeywords.Scry, DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Palm Reading",
        "Scry 6. Good Omen: draw !Cards! cards. Bad Omen: gain !Block! Block.",
        "掌纹解读",
        "预见 6。吉兆：抽 !Cards! 张牌。凶兆：获得 !Block! 点格挡。",
        ("upgradedDesc", "Scry 8. Good Omen: draw !Cards! cards. Bad Omen: gain !Block! Block.", "预见 8。吉兆：抽 !Cards! 张牌。凶兆：获得 !Block! 点格挡。"),
        ("selectPrompt", "Choose cards to discard.", "选择要丢弃的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.Scry(this, choiceContext, IsUpgraded ? 8 : 6);
        if (DestinyService.IsGoodOmen())
        {
            await CardPileCmd.Draw(choiceContext, IsUpgraded ? 3 : 2, Owner, false);
        }

        if (DestinyService.IsBadOmen())
        {
            await CreatureCmd.GainBlock(Owner.Creature, IsUpgraded ? 12 : 9, BlockProps.cardUnpowered, cardPlay, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
