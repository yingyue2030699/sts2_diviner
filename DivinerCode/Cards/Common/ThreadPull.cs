using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Diviner.DivinerCode.Cards.Common;

public class ThreadPull : DivinerCard
{
    private const string ForetellLabel = "Draw";
    private static readonly Dictionary<Player, List<int>> PendingDrawByPlayer = [];

    public ThreadPull()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithCards(2, 1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Thread Pull",
        "#Put a card from your hand on top of your draw pile. Foretell: draw !Cards! cards.",
        "牵引命线",
        "#将一张手牌放到抽牌堆顶。预言：抽 !Cards! 张牌。",
        ("selectPrompt", "Choose a card to put on top of your draw pile.", "选择一张牌放到抽牌堆顶。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool hasSelectableCard = PileType.Hand.GetPile(Owner).Cards.Any(card => !ReferenceEquals(card, this));
        if (hasSelectableCard)
        {
            var selectedCard = await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectPrompt"), 1, 1),
                card => !ReferenceEquals(card, this),
                this
            );

            if (selectedCard != null)
            {
                await CardPileCmd.Add(selectedCard, PileType.Draw, CardPilePosition.Top, this, false);
            }
        }

        var pending = PendingDrawByPlayer.GetValueOrDefault(Owner);
        if (pending == null)
        {
            pending = [];
            PendingDrawByPlayer[Owner] = pending;
        }

        pending.Add(IsUpgraded ? 3 : 2);
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState
    )
    {
        if (side != Owner.Creature.Side ||
            !participants.Contains(Owner.Creature) ||
            !PendingDrawByPlayer.Remove(Owner, out var pendingDraws))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingDraws.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var drawCount in pendingDraws)
            {
                await CardPileCmd.Draw(choiceContext, drawCount, Owner, false);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
