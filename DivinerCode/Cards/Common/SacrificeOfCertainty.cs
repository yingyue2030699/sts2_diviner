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
        WithEnergy(3, 1);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Sacrifice of Certainty",
        "Gain {Energy:energyIcons()}. Lose 1 Destiny.",
        "献祭概然",
        "获得 {Energy:energyIcons()}。失去 1 点命运。",
        ("upgradedDesc", "Gain {Energy:energyIcons()}. Lose 1 Destiny.", "获得 {Energy:energyIcons()}。失去 1 点命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(IsUpgraded ? 4 : 3, Owner);
        DestinyService.AddDestiny(Owner, -1);
        DestinyService.PersistCurrentState(Owner);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
