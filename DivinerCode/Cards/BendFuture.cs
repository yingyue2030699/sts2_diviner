using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.UI;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards;

public class BendFuture : DivinerCard
{
    public BendFuture()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Retain, CardKeyword.Exhaust]);
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Destiny, DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Bend Future",
        "Gain 1 Destiny. If you have an active relic divination, you may choose 1 foretold relic and remove it from the relic sequence.",
        "扭转未来",
        "获得 1 点命运。如果你有有效的遗物占卜，你可以选择 1 件预示遗物并将其移出遗物序列。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.AddDestiny(1);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);

        DivinationService.RefreshActivity(Owner.RunState, Owner);
        var activeRelics = DivinationService.ActiveRelicDivinationIds;
        if (activeRelics.Count == 0)
        {
            return;
        }

        var chosen = await RelicDivinationChoiceOverlay.ChooseRelic(activeRelics);
        if (chosen is { } relicId)
        {
            DivinationService.TryDiscardForecastRelic(Owner, relicId);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
