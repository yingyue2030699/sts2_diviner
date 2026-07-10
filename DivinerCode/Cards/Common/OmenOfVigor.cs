using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class OmenOfVigor : DivinerCard
{
    private const string ForetellLabel = "Energy";
    private static readonly Dictionary<Player, List<int>> PendingEnergyByPlayer = [];

    static OmenOfVigor()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingEnergyByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public OmenOfVigor()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Vigor",
        "Foretell: Gain 3 Energy.",
        "活力征兆",
        "预言：获得 3 点能量。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingEnergyByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingEnergyByPlayer[Owner] = pending;
        pending.Add(3);
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) ||
            !PendingEnergyByPlayer.Remove(Owner, out var pendingEnergy))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingEnergy.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int energy in pendingEnergy)
            {
                await PlayerCmd.GainEnergy(energy, Owner);
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingEnergyByPlayer.Remove(player, out var pendingEnergy))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingEnergy.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int energy in pendingEnergy)
            {
                await PlayerCmd.GainEnergy(energy, player);
            }
        }

        return triggerCount * pendingEnergy.Count;
    }

    protected override void OnUpgrade()
    {
    }
}
