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

    static ThreadPull()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingDrawByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public ThreadPull()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCards(2, 1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Thread Pull",
        "Draw !Cards! cards. Foretell: draw 3 cards.",
        "牵引命线",
        "抽 !Cards! 张牌。预言：抽 3 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, IsUpgraded ? 3 : 2, Owner, false);

        var pending = PendingDrawByPlayer.GetValueOrDefault(Owner);
        if (pending == null)
        {
            pending = [];
            PendingDrawByPlayer[Owner] = pending;
        }

        pending.Add(3);
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

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingDrawByPlayer.Remove(player, out var pendingDraws))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingDraws.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int drawCount in pendingDraws)
            {
                await CardPileCmd.Draw(choiceContext, drawCount, player, false);
            }
        }

        return triggerCount * pendingDraws.Count;
    }

    protected override void OnUpgrade()
    {
    }
}
