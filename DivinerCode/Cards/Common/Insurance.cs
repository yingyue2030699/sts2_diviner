using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class Insurance : DivinerCard
{
    private static readonly HashSet<Player> ProtectedPlayers = [];

    static Insurance()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += ProtectedPlayers.Clear;
    }

    public Insurance()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(6, 3);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Insurance",
        "Gain !Block! Block. You no longer lose HP from Misfortune at the end of this turn.",
        "预留后路",
        "获得 !Block! 点格挡。本回合结束时，你不再因厄运失去生命。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        ProtectedPlayers.Add(Owner);
    }

    public static bool PreventsMisfortuneEndHpLoss(Player player)
    {
        return ProtectedPlayers.Contains(player);
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    )
    {
        if (side == Owner.Creature.Side && participants.Contains(Owner.Creature))
        {
            ProtectedPlayers.Remove(Owner);
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}
