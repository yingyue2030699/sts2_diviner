using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class Divulge : DivinerCard
{
    public Divulge()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithCards(2, 1);
        WithDivinerKeywordTips(DivinerKeywords.Divinate, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Divulge",
        "If not Doomed, Divinate and draw !Cards! cards. Lose 1 Destiny.",
        "泄示",
        "如果不是劫兆，占卜并抽 !Cards! 张牌。失去 1 点命运。",
        ("upgradedDesc", "If not Doomed, Divinate and draw !Cards! cards. Lose 1 Destiny.", "如果不是劫兆，占卜并抽 !Cards! 张牌。失去 1 点命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!DestinyService.CanUseDestiny(Owner))
        {
            return;
        }

        if (!DestinyConstants.IsDredgeDestiny(DestinyService.GetDestiny(Owner)))
        {
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Divulge");
            await CardPileCmd.Draw(choiceContext, IsUpgraded ? 3 : 2, Owner, false);
        }

        DestinyService.AddDestiny(Owner, -1);
        DestinyService.PersistCurrentState(Owner);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
