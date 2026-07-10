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

    static MarkCalendar()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingBlockByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public MarkCalendar()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(14, 4);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Shelter",
        "Foretell: gain !Block! Block.",
        "庇护征兆",
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

        pending.Add((IsUpgraded ? 18 : 14) + DivinerCombatRuntime.ConsumeNextForetellDamageOrBlockBonus());
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) ||
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

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingBlockByPlayer.Remove(player, out var pendingBlocks))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingBlocks.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int block in pendingBlocks)
            {
                await CreatureCmd.GainBlock(player.Creature, block, BlockProps.cardUnpowered, null, false);
            }
        }

        return triggerCount * pendingBlocks.Count;
    }

    protected override void OnUpgrade()
    {
    }
}
