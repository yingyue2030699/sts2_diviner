using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class FalseAlarm : DivinerCard
{
    public FalseAlarm()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(16, 4);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "False Alarm",
        "Gain !Block! Block. Lose 1 Destiny.",
        "虚惊",
        "获得 !Block! 点格挡。失去 1 点命运。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        DestinyService.AddDestiny(-1);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
