using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Cards;

public class Balance : DivinerCard
{
    public Balance()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithCards(1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen, DivinerKeywords.Destiny, DivinerKeywords.Divinate);
        WithCostUpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Balance",
        "#Good Omen: lose 1 Destiny, Divinate, and draw !Cards! card. Bad Omen: this costs 2 extra Energy, then gain 1 Destiny.",
        "平衡",
        "#吉兆：失去 1 点命运，占卜，并抽 !Cards! 张牌。凶兆：本牌多消耗 2 点能量，然后获得 1 点命运。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.EnsureLoadedForRun(Owner.RunState);

        if (DestinyService.IsGoodOmen())
        {
            DestinyService.AddDestiny(-1);
            DestinyService.PersistCurrentState(Owner.RunState);
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Balance");
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
            return;
        }

        DestinyService.AddDestiny(1);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this))
        {
            return false;
        }

        if (Owner?.RunState == null)
        {
            return false;
        }

        DestinyService.EnsureLoadedForRun(Owner.RunState);
        if (!DestinyService.IsBadOmen())
        {
            return false;
        }

        modifiedCost = originalCost + 2;
        return true;
    }

    protected override void OnUpgrade()
    {
    }
}
