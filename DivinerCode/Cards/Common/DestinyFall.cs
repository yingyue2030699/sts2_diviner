using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class DestinyFall : DivinerCard
{
    private const string ForetellLabel = "Falling damage";
    private static readonly Dictionary<Player, List<PendingHit>> PendingHitsByPlayer = [];

    public DestinyFall()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 2);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Destiny's Fall",
        "#Deal !Damage! damage. Foretell: Deal 8 damage to the same enemy.",
        "命运坠落",
        "#造成 !Damage! 点伤害。预言：对同一敌人造成 8 点伤害。",
        ("upgradedDesc", "#Deal !Damage! damage. Foretell: Deal 10 damage to the same enemy.", "#造成 !Damage! 点伤害。预言：对同一敌人造成 10 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (cardPlay.Target == null)
        {
            return;
        }

        var pending = PendingHitsByPlayer.GetValueOrDefault(Owner);
        if (pending == null)
        {
            pending = [];
            PendingHitsByPlayer[Owner] = pending;
        }

        pending.Add(new PendingHit(
            cardPlay.Target,
            (IsUpgraded ? 10 : 8) + DivinerCombatRuntime.ConsumeNextForetellDamageOrBlockBonus()
        ));
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
            !PendingHitsByPlayer.Remove(Owner, out var pendingHits))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingHits.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            var hittableEnemies = CombatState?.HittableEnemies.ToHashSet() ?? [];
            foreach (var pendingHit in pendingHits)
            {
                if (!hittableEnemies.Contains(pendingHit.Target))
                {
                    continue;
                }

                await CreatureCmd.Damage(
                    choiceContext,
                    pendingHit.Target,
                    pendingHit.Damage,
                    DamageProps.nonCardUnpowered,
                    Owner.Creature,
                    this
                );
            }
        }
    }

    protected override void OnUpgrade()
    {
    }

    private readonly record struct PendingHit(Creature Target, int Damage);
}
