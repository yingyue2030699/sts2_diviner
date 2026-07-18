using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class NarrowEscape : DivinerCard
{
    public NarrowEscape()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(5, 0);
        WithDivinerKeywordTips(DivinerKeywords.Dredge, DivinerKeywords.CountdownOfDestiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Narrow Escape",
        "Gain !Block! Block. Doomed: gain 1 Countdown of Destiny.",
        "险中脱身",
        "获得 !Block! 点格挡。劫兆：获得 1 层命运倒计时。",
        ("upgradedDesc", "Gain !Block! Block. Doomed: gain 2 Countdown of Destiny.", "获得 !Block! 点格挡。劫兆：获得 2 层命运倒计时。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (DestinyService.CanUseDestiny(Owner) &&
            DestinyConstants.IsDredgeDestiny(DestinyService.GetDestiny(Owner)))
        {
            await DivinerCombatRuntime.IncreaseDredgeCountdown(choiceContext, Owner, IsUpgraded ? 2 : 1);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
