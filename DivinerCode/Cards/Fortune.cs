using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards;

public class Fortune : DivinerCard
{
    public Fortune()
        : base(0, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithCards(1, 1);
        WithKeywords([CardKeyword.Retain, CardKeyword.Exhaust]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Fortune",
        "Gain 1 Energy. Draw !Cards! card.",
        "福运",
        "获得 1 点能量。抽 !Cards! 张牌。",
        ("upgradedDesc", "Gain 1 Energy. Draw !Cards! cards.", "获得 1 点能量。抽 !Cards! 张牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(1, Owner);
        await CardPileCmd.Draw(choiceContext, IsUpgraded ? 2 : 1, Owner, false);
    }

    protected override void OnUpgrade()
    {
    }
}
