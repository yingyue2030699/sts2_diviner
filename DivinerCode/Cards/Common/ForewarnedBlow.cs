using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class ForewarnedBlow : DivinerCard
{
    public ForewarnedBlow()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(15, 5);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Forewarned Blow",
        "Deal !Damage! damage. Add a Misfortune to your draw pile.",
        "先兆打击",
        "造成 !Damage! 点伤害。将一张厄运加入抽牌堆。",
        ("upgradedDesc", "Deal !Damage! damage. Add a Misfortune+ to your draw pile.", "造成 !Damage! 点伤害。将一张厄运+加入抽牌堆。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
            this,
            PileType.Draw,
            CardPilePosition.Bottom,
            IsUpgraded);
    }

    protected override void OnUpgrade()
    {
    }
}
