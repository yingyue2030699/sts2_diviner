using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class Insurance : DivinerCard
{
    private static readonly Dictionary<Player, int> ActiveCopiesByPlayer = [];

    public Insurance()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(6, 3);
        WithCards(1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Insurance",
        "#Gain !Block! Block. Whenever you lose HP this turn, draw !Cards! card.",
        "预留后路",
        "#获得 !Block! 点格挡。本回合每当你失去生命时，抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        ActiveCopiesByPlayer[Owner] = ActiveCopiesByPlayer.GetValueOrDefault(Owner) + 1;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature receiver,
        DamageResult result,
        ValueProp props,
        Creature? source,
        CardModel? cardSource
    )
    {
        if (!ReferenceEquals(receiver, Owner.Creature) ||
            result.UnblockedDamage <= 0 ||
            !ActiveCopiesByPlayer.TryGetValue(Owner, out int activeCopies))
        {
            return;
        }

        for (int i = 0; i < activeCopies; i++)
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
        }
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    )
    {
        if (side == Owner.Creature.Side && participants.Contains(Owner.Creature))
        {
            ActiveCopiesByPlayer.Remove(Owner);
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}
