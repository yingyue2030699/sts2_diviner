using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class MarkCalendar : DivinerCard
{
    private const string ForetellLabel = "Block";
    private static readonly Dictionary<Player, List<int>> PendingBlockByPlayer = [];

    public MarkCalendar()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(12, 4);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Mark Calendar",
        "Foretell: gain !Block! Block.",
        "标记日历",
        "预言：获得 !Block! 点格挡。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingBlockByPlayer.GetValueOrDefault(Owner);
        if (pending == null)
        {
            pending = [];
            PendingBlockByPlayer[Owner] = pending;
        }

        pending.Add((IsUpgraded ? 16 : 12) + DivinerCombatRuntime.ConsumeNextForetellDamageOrBlockBonus());
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
            !PendingBlockByPlayer.Remove(Owner, out var pendingBlocks))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingBlocks.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var block in pendingBlocks)
            {
                await CreatureCmd.GainBlock(Owner.Creature, block, BlockProps.cardUnpowered, null, false);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
