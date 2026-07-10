using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Ancient;

public class ResonationOfFate : DivinerCard
{
    public ResonationOfFate()
        : base(3, CardType.Power, CardRarity.Ancient, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Resonation of Fate",
        "Fated cards are played an additional time. At start of turn, make 1 random card in your hand Fated.",
        "命运共鸣",
        "注定牌额外打出一次。回合开始时，使你手牌中的 1 张随机牌变为注定。",
        ("upgradedDesc", "Fated cards are played an additional time. At start of turn, make 2 random cards in your hand Fated.", "注定牌额外打出一次。回合开始时，使你手牌中的 2 张随机牌变为注定。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ResonationOfFatePower>(
            choiceContext,
            Owner.Creature,
            IsUpgraded ? 2 : 1,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class OmenOfPerishment : DivinerCard
{
    private const string ForetellLabel = "Perishment";
    private static readonly Dictionary<Player, List<PendingPerishment>> PendingPerishmentByPlayer = [];

    static OmenOfPerishment()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingPerishmentByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public OmenOfPerishment()
        : base(1, CardType.Skill, CardRarity.Ancient, TargetType.TargetedNoCreature)
    {
        WithDamage(22, 11);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Perishment",
        "Foretell: Deal !Damage! damage and apply 3 Weak and 3 Vulnerable to all enemies.",
        "殒灭征兆",
        "预言：对所有敌人造成 !Damage! 点伤害，并给予 3 层虚弱和 3 层易伤。",
        ("upgradedDesc", "Foretell: Deal !Damage! damage and apply 5 Weak and 5 Vulnerable to all enemies.", "预言：对所有敌人造成 !Damage! 点伤害，并给予 5 层虚弱和 5 层易伤。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingPerishmentByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingPerishmentByPlayer[Owner] = pending;
        pending.Add(new PendingPerishment(IsUpgraded ? 33 : 22, IsUpgraded ? 5 : 3));
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side ||
            !participants.Contains(Owner.Creature) ||
            !PendingPerishmentByPlayer.Remove(Owner, out var pending))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pending.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var effect in pending)
            {
                await Resolve(choiceContext, Owner, Owner.Creature, this, effect);
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingPerishmentByPlayer.Remove(player, out var pending))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pending.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var effect in pending)
            {
                await Resolve(choiceContext, player, player.Creature, null, effect);
            }
        }

        return triggerCount * pending.Count;
    }

    private static async Task Resolve(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature source,
        OmenOfPerishment? cardSource,
        PendingPerishment effect)
    {
        var enemies = DivinerCombatRuntime.HittableEnemiesFor(player);
        if (enemies.Count == 0)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, enemies, effect.Damage, DamageProps.nonCardUnpowered, source);
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, effect.Amount, source, cardSource, false);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, effect.Amount, source, cardSource, false);
        }
    }

    protected override void OnUpgrade()
    {
    }

    private readonly record struct PendingPerishment(int Damage, int Amount);
}
