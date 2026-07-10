using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards;

public class Balance : DivinerCard
{
    public Balance()
        : base(0, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithCards(1, 1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen, DivinerKeywords.Destiny, DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Temper Fate",
        "Good Omen: lose 1 Destiny, draw !Cards! card, and Divinate. Bad Omen: add a Bend Future to your hand.",
        "调律命运",
        "吉兆：失去 1 点命运，抽 !Cards! 张牌，并占卜。凶兆：将一张扭转未来加入你的手牌。",
        ("upgradedDesc", "Good Omen: lose 1 Destiny, draw !Cards! cards, and Divinate. Bad Omen: add a Bend Future+ to your hand.", "吉兆：失去 1 点命运，抽 !Cards! 张牌，并占卜。凶兆：将一张扭转未来+加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.EnsureLoadedForRun(Owner.RunState);

        bool goodOmen = DestinyService.IsGoodOmen();
        bool badOmen = DestinyService.IsBadOmen();

        if (goodOmen)
        {
            DestinyService.AddDestiny(-1);
            DestinyService.PersistCurrentState(Owner.RunState);
            await CardPileCmd.Draw(choiceContext, IsUpgraded ? 2 : 1, Owner, false);
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Temper Fate");
        }

        if (badOmen)
        {
            await DivinerCardActions.AddGeneratedToCombat<BendFuture>(
                this,
                PileType.Hand,
                CardPilePosition.Bottom,
                IsUpgraded);
        }

        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
