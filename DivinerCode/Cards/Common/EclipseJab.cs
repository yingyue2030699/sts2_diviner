using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class EclipseJab : DivinerCard
{
    public EclipseJab()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(2, 2);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Eclipse Jab",
        "#Deal !Damage! damage. Then deal 8 damage.",
        "蚀月刺击",
        "#造成 !Damage! 点伤害。然后造成 8 点伤害。",
        ("upgradedDesc", "#Deal !Damage! damage. Then deal 10 damage.", "#造成 !Damage! 点伤害。然后造成 10 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (cardPlay.Target != null)
        {
            await CommonActions.CardAttack(this, cardPlay.Target, IsUpgraded ? 10m : 8m).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
