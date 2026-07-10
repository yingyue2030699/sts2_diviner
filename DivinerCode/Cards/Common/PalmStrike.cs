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
        WithDamage(10, 3);
        WithCards(1);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Palm Strike",
        "Deal !Damage! damage. Bad Omen: damage +6. Good Omen: draw !Cards! card.",
        "掌击",
        "造成 !Damage! 点伤害。凶兆：伤害 +6。吉兆：抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damage = IsUpgraded ? 13 : 10;
        if (DestinyService.IsBadOmen())
        {
            damage += 6;
        }

        if (cardPlay.Target != null)
        {
            await CommonActions.CardAttack(this, cardPlay.Target, damage).Execute(choiceContext);
        }

        if (DestinyService.IsGoodOmen())
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
