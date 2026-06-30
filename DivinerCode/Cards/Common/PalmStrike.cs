using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class PalmStrike : DivinerCard
{
    public PalmStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(9, 3);
        WithCards(3);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Palm Strike",
        "Deal !Damage! damage. Good Omen: draw !Cards! cards.",
        "掌击",
        "造成 !Damage! 点伤害。吉兆：抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (!DestinyService.IsGoodOmen())
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, 3, Owner, false);
    }

    protected override void OnUpgrade()
    {
    }
}
