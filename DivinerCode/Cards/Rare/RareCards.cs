using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Rare;

public class Clairvoyance : DivinerCard
{
    public Clairvoyance()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Clairvoyance",
        "Search anywhere for 1 card. Put it into your hand; it is Fated.",
        "千里眼",
        "从任意位置选择 1 张牌加入手牌；它为注定。",
        ("upgradedDesc", "Search anywhere for 1 card. Put it into your hand; it is Fated.", "从任意位置选择 1 张牌加入手牌；它为注定。"),
        ("selectPrompt", "Choose a card to put into your hand.", "选择一张牌加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectableCards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => !ReferenceEquals(card, this))
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Concat(PileType.Exhaust.GetPile(Owner).Cards)
            .ToList();
        if (selectableCards.Count == 0)
        {
            return;
        }

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            selectableCards,
            Owner,
            new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectPrompt"), 1)
        );
        await DivinerCardActions.MoveToHandFated(this, selected);
    }

    protected override void OnUpgrade()
    {
    }
}

public class Apocalypse : DivinerCard
{
    public Apocalypse()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(9, 1);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Apocalypse",
        "Deal !Damage! damage to a random enemy 9 times. After other cost modifiers, add Destiny to this card's cost.",
        "天启",
        "随机对敌人造成 !Damage! 点伤害 9 次。结算其他费用调整后，本牌费用再增加等同于命运的数值。",
        ("upgradedDesc", "Deal !Damage! damage to a random enemy 10 times. After other cost modifiers, add Destiny to this card's cost.", "随机对敌人造成 !Damage! 点伤害 10 次。结算其他费用调整后，本牌费用再增加等同于命运的数值。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = DivinerCardActions.HittableEnemies(this);
        if (enemies.Count == 0 || CombatState == null)
        {
            return;
        }

        int hits = IsUpgraded ? 10 : 9;
        int damage = IsUpgraded ? 10 : 9;
        await using var attack = await AttackCommand.CreateContextAsync(CombatState, choiceContext, this);
        for (int i = 0; i < hits; i++)
        {
            var target = enemies[Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
            DivinerEffectCue.BombardmentImpact([target]);
            var results = await CreatureCmd.Damage(
                choiceContext,
                target,
                damage,
                DamageProps.card,
                Owner.Creature,
                this);
            attack.AddHit(results);
        }
    }

    public override bool TryModifyEnergyCostInCombatLate(MegaCrit.Sts2.Core.Models.CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) || !DestinyService.CanUseDestiny(Owner))
        {
            return false;
        }

        modifiedCost = originalCost + DestinyService.GetDestiny(Owner);
        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
    }
}

public class FallenSky : DivinerCard
{
    private const string ForetellLabel = "Fallen Sky";
    private static readonly Dictionary<Player, List<int>> PendingDamageByPlayer = [];

    static FallenSky()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingDamageByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public FallenSky()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithEnergyCostX();
        WithDamage(20, 5);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Fallen Sky",
        "Foretell: Deal !Damage! times X damage to all enemies.",
        "坠天征兆",
        "预言：对所有敌人造成 !Damage! 乘以 X 点伤害。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingDamageByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingDamageByPlayer[Owner] = pending;
        int x = Math.Max(0, cardPlay.Resources.EnergySpent);
        int damage = ((IsUpgraded ? 25 : 20) * x) + DivinerCombatRuntime.ConsumeNextForetellDamageOrBlockBonus(Owner);
        pending.Add(damage);
        DivinerCombatRuntime.QueueForetell(
            Owner,
            ForetellLabel,
            detail: DivinerLoc.Text(
                $"Omen of Fallen Sky: deal {damage} damage to all enemies.",
                $"天坠预言：对所有敌人造成 {damage} 点伤害。"));
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
            !PendingDamageByPlayer.Remove(Owner, out var pendingDamage))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingDamage.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int damage in pendingDamage)
            {
                var enemies = DivinerCardActions.HittableEnemies(this);
                if (enemies.Count > 0)
                {
                    DivinerEffectCue.BombardmentImpact(enemies);
                    await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, Owner.Creature);
                }
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingDamageByPlayer.Remove(player, out var pendingDamage))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingDamage.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int damage in pendingDamage)
            {
                var enemies = DivinerCombatRuntime.HittableEnemiesFor(player);
                if (enemies.Count > 0)
                {
                    DivinerEffectCue.BombardmentImpact(enemies);
                    await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, player.Creature);
                }
            }
        }

        return triggerCount * pendingDamage.Count;
    }

    protected override void OnUpgrade()
    {
    }
}

public class HandOfFate : DivinerCard
{
    public HandOfFate()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(13, 3);
        WithCards(1);
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Hand of Fate",
        "Deal !Damage! damage. Draw !Cards! card. Revelation: trigger this effect 3 times.",
        "命运之手",
        "造成 !Damage! 点伤害。抽 !Cards! 张牌。启示：此效果触发 3 次。",
        ("upgradedDesc", "Deal !Damage! damage. Draw !Cards! card. Revelation: trigger this effect 4 times.", "造成 !Damage! 点伤害。抽 !Cards! 张牌。启示：此效果触发 4 次。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int repeats = await DivinerCombatRuntime.TryConsumeRevelationEffect(choiceContext, Owner)
            ? IsUpgraded ? 4 : 3
            : 1;
        for (int i = 0; i < repeats; i++)
        {
            await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class TheLastWord : DivinerCard
{
    public TheLastWord()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(20, 8);
        WithKeywords([CardKeyword.Retain, CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "The Last Word",
        "Deal !Damage! damage. If Fatal, gain 1 Destiny.",
        "终言",
        "造成 !Damage! 点伤害。如果致命，获得 1 点命运。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? target = cardPlay.Target;
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (target != null && !target.IsAlive)
        {
            DestinyService.AddDestiny(Owner, 1);
            DestinyService.PersistCurrentState(Owner);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class UnavoidableEnd : DivinerCard
{
    private const string ForetellLabel = "End";
    private static readonly Dictionary<Player, List<int>> PendingDamageByPlayer = [];

    static UnavoidableEnd()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingDamageByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public UnavoidableEnd()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Unavoidable End",
        "Deal !Damage! damage. Foretell: deal 3 times the damage actually dealt to all enemies.",
        "终局",
        "造成 !Damage! 点伤害。预言：对所有敌人造成此次实际伤害的 3 倍伤害。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null || CombatState == null)
        {
            return;
        }

        await using var attack = await AttackCommand.CreateContextAsync(CombatState, choiceContext, this);
        var results = await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            IsUpgraded ? 11 : 8,
            DamageProps.card,
            Owner.Creature,
            this);
        attack.AddHit(results);
        int actualDamage = results.Sum(result => result.TotalDamage + result.OverkillDamage);
        var pending = PendingDamageByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingDamageByPlayer[Owner] = pending;
        int foretellDamage = actualDamage * 3 + DivinerCombatRuntime.ConsumeNextForetellDamageOrBlockBonus(Owner);
        pending.Add(foretellDamage);
        DivinerCombatRuntime.QueueForetell(
            Owner,
            ForetellLabel,
            detail: DivinerLoc.Text(
                $"Unavoidable End: deal {foretellDamage} damage to all enemies.",
                $"终局：对所有敌人造成 {foretellDamage} 点伤害。"));
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
            !PendingDamageByPlayer.Remove(Owner, out var pendingDamage))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingDamage.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int damage in pendingDamage)
            {
                var enemies = DivinerCardActions.HittableEnemies(this);
                if (enemies.Count > 0)
                {
                    await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, Owner.Creature);
                }
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingDamageByPlayer.Remove(player, out var pendingDamage))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingDamage.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int damage in pendingDamage)
            {
                var enemies = DivinerCombatRuntime.HittableEnemiesFor(player);
                if (enemies.Count > 0)
                {
                    await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, player.Creature);
                }
            }
        }

        return triggerCount * pendingDamage.Count;
    }

    protected override void OnUpgrade()
    {
    }
}

public class GreaterPortent : DivinerCard
{
    public GreaterPortent()
        : base(4, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Greater Portent",
        "Search up to 3 cards from your draw pile. Put them into your hand; they are Fated.",
        "大预兆",
        "从你的抽牌堆中选择至多 3 张牌加入手牌；它们为注定。",
        ("upgradedDesc", "Search up to 3 cards from your draw pile. Put them into your hand; they are Fated.", "从你的抽牌堆中选择至多 3 张牌加入手牌；它们为注定。"),
        ("selectPrompt", "Choose cards to put into your hand.", "选择要加入手牌的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await DivinerCardActions.SelectFromDrawPile(this, choiceContext, "selectPrompt", 0, 3);
        await DivinerCardActions.MoveToHandFated(this, selected);
    }

    protected override void OnUpgrade()
    {
    }
}

public class Reversal : DivinerCard
{
    public Reversal()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Reversal",
        "Set Destiny to 5 minus its current value.",
        "逆转",
        "将命运设为 5 减去当前命运。",
        ("upgradedDesc", "Set Destiny to 5 minus its current value.", "将命运设为 5 减去当前命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.SetDestiny(Owner, DestinyConstants.MaxDestiny - DestinyService.GetDestiny(Owner));
        DestinyService.PersistCurrentState(Owner);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}

public class OraclesBargain : DivinerCard
{
    public OraclesBargain()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Oracle's Bargain",
        "Gain 1 Destiny. Shuffle 3 Misfortunes into your draw pile.",
        "神谕交易",
        "获得 1 点命运。将 3 张噩运洗入抽牌堆。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.AddDestiny(Owner, 1);
        DestinyService.PersistCurrentState(Owner);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(this, 3, PileType.Draw, CardPilePosition.Random);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}

public class PerfectForecast : DivinerCard
{
    public PerfectForecast()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithEnergy(1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Perfect Forecast",
        "Divinate. Gain {Energy:energyIcons()} for every 7 recorded divinations.",
        "完美预测",
        "占卜。每有 7 条已记录的占卜，获得 {Energy:energyIcons()}。",
        ("upgradedDesc", "Divinate. Gain {Energy:energyIcons()} for every 5 recorded divinations.", "占卜。每有 5 条已记录的占卜，获得 {Energy:energyIcons()}。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinationService.RecordPlaceholder(choiceContext, Owner, "Perfect Forecast");
        int energy = DivinationService.GetRecords(Owner).Count / (IsUpgraded ? 5 : 7);
        if (energy > 0)
        {
            await PlayerCmd.GainEnergy(energy, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class OmenOfTranscendence : DivinerCard
{
    private const string ForetellLabel = "Transcendence";
    private static readonly Dictionary<Player, List<PendingTranscendence>> PendingTranscendenceByPlayer = [];

    static OmenOfTranscendence()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingTranscendenceByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public OmenOfTranscendence()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCards(3, 1);
        WithEnergy(2, 1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Transcendence",
        "Foretell: Draw !Cards! cards and gain {Energy:energyIcons()}.",
        "超脱征兆",
        "预言：抽 !Cards! 张牌并获得 {Energy:energyIcons()}。",
        ("upgradedDesc", "Foretell: Draw !Cards! cards and gain {Energy:energyIcons()}.", "预言：抽 !Cards! 张牌并获得 {Energy:energyIcons()}。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingTranscendenceByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingTranscendenceByPlayer[Owner] = pending;
        pending.Add(new PendingTranscendence(IsUpgraded ? 4 : 3, IsUpgraded ? 3 : 2));
        int draw = IsUpgraded ? 4 : 3;
        int energy = IsUpgraded ? 3 : 2;
        DivinerCombatRuntime.QueueForetell(
            Owner,
            ForetellLabel,
            detail: DivinerLoc.Text(
                $"Omen of Transcendence: draw {draw} cards and gain {energy} Energy.",
                $"超脱征兆：抽 {draw} 张牌并获得 {energy} 点能量。"));
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) ||
            !PendingTranscendenceByPlayer.Remove(Owner, out var pendingTranscendence))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingTranscendence.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var transcendence in pendingTranscendence)
            {
                await CardPileCmd.Draw(choiceContext, transcendence.Draw, Owner, false);
                await PlayerCmd.GainEnergy(transcendence.Energy, Owner);
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingTranscendenceByPlayer.Remove(player, out var pendingTranscendence))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingTranscendence.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var transcendence in pendingTranscendence)
            {
                await CardPileCmd.Draw(choiceContext, transcendence.Draw, player, false);
                await PlayerCmd.GainEnergy(transcendence.Energy, player);
            }
        }

        return triggerCount * pendingTranscendence.Count;
    }

    protected override void OnUpgrade()
    {
    }

    private readonly record struct PendingTranscendence(int Draw, int Energy);
}

public class Veil : DivinerCard
{
    public Veil()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Veil",
        "Gain Block equal to the number of recorded divinations.",
        "面纱",
        "获得等同于已记录占卜数量的格挡。",
        ("upgradedDesc", "Gain Block equal to the number of recorded divinations.", "获得等同于已记录占卜数量的格挡。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DivinationService.GetRecords(Owner).Count,
            BlockProps.cardUnpowered,
            cardPlay,
            false);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

public class Duality : DivinerCard
{
    public Duality()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Duality",
        "Good Omen and Bad Omen extra effects always trigger.",
        "二相",
        "吉兆与凶兆的额外效果总是触发。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DualityPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class CheatTheEnding : DivinerCard
{
    public CheatTheEnding()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Retain, CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Dredge, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Cheat the Ending",
        "The next time you would die this combat, heal to 13% of max HP and set Destiny to 0.",
        "欺瞒终局",
        "本场战斗中下次你将要死亡时，恢复至最大生命值的 13% 并将命运设为 0。",
        ("upgradedDesc", "The next time you would die this combat, heal to 13% of max HP and set Destiny to 0.", "本场战斗中下次你将要死亡时，恢复至最大生命值的 13% 并将命运设为 0。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CheatTheEndingPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class FixedPoint : DivinerCard
{
    public FixedPoint()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithTip(CardKeyword.Retain);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Fixed Point",
        "Destiny cannot change this turn. Retain your hand this turn.",
        "命运定点",
        "本回合中命运无法改变。保留本回合的手牌。",
        ("upgradedDesc", "Destiny cannot change this turn. Retain your hand this turn.", "本回合中命运无法改变。保留本回合的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RetainHandPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);

        if (DestinyService.CanUseDestiny(Owner))
        {
            await PowerCmd.Apply<FixedPointPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class ManyFutures : DivinerCard
{
    public ManyFutures()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Scry);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Many Futures",
        "Card rewards at the end of this combat have 1 additional option. When you Scry, Scry 2 additional cards.",
        "诸多未来",
        "本场战斗结束后的卡牌奖励多 1 个选项。每当你预见时，额外预见 2 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ManyFuturesPower>(choiceContext, Owner.Creature, 2, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class DoomSpiral : DivinerCard
{
    public DoomSpiral()
        : base(0, CardType.Power, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Innate]);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Doom Spiral",
        "At start of turn, lose 1 Destiny and add a Misfortune to your hand.",
        "噩运螺旋",
        "回合开始时，失去 1 点命运并将一张噩运加入你的手牌。",
        ("upgradedDesc", "At start of turn, lose 1 Destiny and add a Misfortune+ to your hand.", "回合开始时，失去 1 点命运并将一张噩运+加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DoomSpiralPower>(choiceContext, Owner.Creature, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class EchoedOmen : DivinerCard
{
    public EchoedOmen()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Echoed Omen",
        "Foretell effects trigger an additional time.",
        "回响征兆",
        "预言效果额外触发一次。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EchoedOmenPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class AscendedForm : DivinerCard
{
    public AscendedForm()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Ascended Form",
        "Good Omen and Revelation card effects can be triggered with 1 less Destiny. Revelation extra effects no longer reduce Destiny.",
        "升华形态",
        "吉兆和启示卡牌效果可以用低 1 点的命运触发。启示额外效果不再降低命运。",
        ("upgradedDesc", "Good Omen and Revelation card effects can be triggered with 2 less Destiny. Revelation extra effects no longer reduce Destiny.", "吉兆和启示卡牌效果可以用低 2 点的命运触发。启示额外效果不再降低命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AscendedFormPower>(choiceContext, Owner.Creature, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}
