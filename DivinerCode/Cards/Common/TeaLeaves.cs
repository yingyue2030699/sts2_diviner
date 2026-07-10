using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class TeaLeaves : DivinerCard
{
    private const string ForetellLabel = "Divinate";
    private static readonly Dictionary<Player, int> PendingDivinationsByPlayer = [];

    static TeaLeaves()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingDivinationsByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public TeaLeaves()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Foretell, DivinerKeywords.Divinate);
        WithCostUpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Insight",
        "Foretell: Divinate.",
        "洞见征兆",
        "预言：占卜。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PendingDivinationsByPlayer[Owner] = PendingDivinationsByPlayer.GetValueOrDefault(Owner) + 1;
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) ||
            !PendingDivinationsByPlayer.Remove(Owner, out var count))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            for (int i = 0; i < count; i++)
            {
                await DivinationService.RecordPlaceholder(choiceContext, Owner, "Omen of Insight");
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingDivinationsByPlayer.Remove(player, out var count))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            for (int i = 0; i < count; i++)
            {
                await DivinationService.RecordPlaceholder(choiceContext, player, "Omen of Insight");
            }
        }

        return triggerCount * count;
    }

    protected override void OnUpgrade()
    {
    }
}
