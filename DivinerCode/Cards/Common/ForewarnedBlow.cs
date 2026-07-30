using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class ForewarnedBlow : DivinerCard
{
    public ForewarnedBlow()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(15, 3);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Forewarned Blow",
        "Deal !Damage! damage. Bad Omen: add a Misfortune to your draw pile. Good Omen: add a Fortune to your draw pile.",
        "先兆重击",
        "造成 !Damage! 点伤害。凶兆：将一张噩运加入抽牌堆。吉兆：将一张福运加入抽牌堆。",
        ("upgradedDesc", "Deal !Damage! damage. Bad Omen: add a Misfortune+ to your draw pile. Good Omen: add a Fortune+ to your draw pile.", "造成 !Damage! 点伤害。凶兆：将一张噩运+加入抽牌堆。吉兆：将一张福运+加入抽牌堆。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (DestinyService.IsGoodOmen(Owner))
        {
            await DivinerCardActions.AddGeneratedToCombat<Fortune>(
                this,
                PileType.Draw,
                CardPilePosition.Bottom,
                IsUpgraded);
        }
        else if (DestinyService.IsBadOmen(Owner))
        {
            await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
                this,
                PileType.Draw,
                CardPilePosition.Bottom,
                IsUpgraded);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
