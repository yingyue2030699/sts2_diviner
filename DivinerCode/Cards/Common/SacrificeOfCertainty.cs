using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class SacrificeOfCertainty : DivinerCard
{
    public SacrificeOfCertainty()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Sacrifice of Certainty",
        "Gain 3 Energy. Lose 1 Destiny.",
        "舍弃定数",
        "获得 3 点能量。失去 1 点命运。",
        ("upgradedDesc", "Gain 4 Energy. Lose 1 Destiny.", "获得 4 点能量。失去 1 点命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(IsUpgraded ? 4 : 3, Owner);
        DestinyService.AddDestiny(-1);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
