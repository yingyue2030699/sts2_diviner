using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards;

public class DivinationOfWoes : DivinerCard
{
    private const string ForetellLabel = "Woes";
    private static readonly Dictionary<Player, List<int>> PendingDamageByPlayer = [];

    public DivinationOfWoes()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.TargetedNoCreature)
    {
        WithDamage(9, 4);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Divination of Woes",
        "Foretell: Deal !Damage! damage and apply 1 Weak and 1 Vulnerable to all enemies.",
        "灾厄占卜",
        "预言：对所有敌人造成 !Damage! 点伤害，并给予 1 层虚弱和 1 层易伤。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingDamageByPlayer.GetValueOrDefault(Owner);
        if (pending == null)
        {
            pending = [];
            PendingDamageByPlayer[Owner] = pending;
        }

        pending.Add((IsUpgraded ? 13 : 9) + DivinerCombatRuntime.ConsumeNextForetellDamageOrBlockBonus());
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
            !PendingDamageByPlayer.Remove(Owner, out var pendingDamage))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingDamage.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var damage in pendingDamage)
            {
                var enemies = CombatState?.HittableEnemies.Where(creature => creature.Side != Owner.Creature.Side).ToList() ?? [];
                if (enemies.Count == 0)
                {
                    continue;
                }

                await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, Owner.Creature, this);

                foreach (var enemy in enemies)
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 1, Owner.Creature, this, false);
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1, Owner.Creature, this, false);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
