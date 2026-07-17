using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class RetributiveRite : DivinerCard
{
    public RetributiveRite()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(9, 2);
        WithBlock(9, 2);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Retributive Rite",
        "Deal !Damage! damage. Bad Omen: gain !Block! Block.",
        "报偿仪式",
        "造成 !Damage! 点伤害。凶兆：获得 !Block! 点格挡。",
        ("upgradedDesc", "Deal !Damage! damage. Bad Omen: gain !Block! Block.", "造成 !Damage! 点伤害。凶兆：获得 !Block! 点格挡。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (DestinyService.IsBadOmen(Owner))
        {
            await CreatureCmd.GainBlock(Owner.Creature, IsUpgraded ? 11 : 9, BlockProps.cardUnpowered, cardPlay, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
