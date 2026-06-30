using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class SkepticsCharm : DivinerCard
{
    public SkepticsCharm()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(6, 2);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Skeptic's Charm",
        "#Gain !Block! Block. Bad Omen: gain 6 more Block.",
        "怀疑者护符",
        "#获得 !Block! 点格挡。凶兆：额外获得 6 点格挡。",
        ("upgradedDesc", "#Gain !Block! Block. Bad Omen: gain 8 more Block.", "#获得 !Block! 点格挡。凶兆：额外获得 8 点格挡。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);

        if (DestinyService.IsBadOmen())
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                IsUpgraded ? 8 : 6,
                BlockProps.cardUnpowered,
                cardPlay,
                false
            );
        }
    }

    protected override void OnUpgrade()
    {
    }
}
