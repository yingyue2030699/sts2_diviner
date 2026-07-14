using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class Insurance : DivinerCard
{
    public Insurance()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(6, 3);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Insurance",
        "Gain !Block! Block. You no longer lose HP from Misfortune or Misfortune+ at the end of this turn.",
        "预留后路",
        "获得 !Block! 点格挡。本回合结束时，你不再因厄运或厄运+失去生命。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await PowerCmd.Apply<InsurancePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    public static bool PreventsMisfortuneEndHpLoss(Player player)
    {
        return player.Creature.GetPower<InsurancePower>() != null;
    }

    protected override void OnUpgrade()
    {
    }
}
