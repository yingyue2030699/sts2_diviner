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

    public TeaLeaves()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Foretell, DivinerKeywords.Divinate);
        WithCostUpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Tea Leaves",
        "#Foretell: Divinate.",
        "茶叶占形",
        "#预言：占卜。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PendingDivinationsByPlayer[Owner] = PendingDivinationsByPlayer.GetValueOrDefault(Owner) + 1;
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
                await DivinationService.RecordPlaceholder(choiceContext, Owner, "Tea Leaves");
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
