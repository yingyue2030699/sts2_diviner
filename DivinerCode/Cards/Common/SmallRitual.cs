using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class SmallRitual : DivinerCard
{
    public SmallRitual()
        : base(1, CardType.Power, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithEnergy(2, 1);
        WithKeywords([CardKeyword.Innate]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Small Ritual",
        "The next time you Divinate, gain [E] [E].",
        "小仪式",
        "下一次你占卜时，获得 [E] [E]。",
        ("upgradedDesc", "The next time you Divinate, gain [E] [E] [E].", "下一次你占卜时，获得 [E] [E] [E]。")
    );

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DivinerCombatRuntime.AddNextDivinationEnergyBonus(IsUpgraded ? 3 : 2);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}
