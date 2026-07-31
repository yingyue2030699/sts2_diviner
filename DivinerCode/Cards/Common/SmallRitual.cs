using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class SmallRitual : DivinerCard
{
    public SmallRitual()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithEnergy(1);
        WithKeywords([CardKeyword.Innate]);
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Small Ritual",
        "Whenever you Divinate, gain {Energy:energyIcons()}.",
        "小仪式",
        "每当你占卜时，获得 {Energy:energyIcons()}。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SmallRitualPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}
