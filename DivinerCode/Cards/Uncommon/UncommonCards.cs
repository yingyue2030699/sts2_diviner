using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Uncommon;

public class StarNeedle : DivinerCard
{
    public StarNeedle()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(3, 4);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Star Needle",
        "Deal !Damage! damage. If you have 7 or more recorded divinations, apply 7 Vulnerable.",
        "星针",
        "造成 !Damage! 点伤害。如果你有 7 条或更多已记录的占卜，给予 7 层易伤。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (cardPlay.Target != null && DivinationService.CurrentRecords.Count >= 7)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 7, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class Doomscript : DivinerCard
{
    private const string ForetellLabel = "Vulnerable";
    private static readonly Dictionary<Player, List<int>> PendingVulnerableByPlayer = [];

    public Doomscript()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Doomscript",
        "Deal !Damage! damage. Foretell: apply 2 Vulnerable to all enemies.",
        "厄文",
        "造成 !Damage! 点伤害。预言：给予所有敌人 2 层易伤。",
        ("upgradedDesc", "Deal !Damage! damage. Foretell: apply 3 Vulnerable to all enemies.", "造成 !Damage! 点伤害。预言：给予所有敌人 3 层易伤。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        var pending = PendingVulnerableByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingVulnerableByPlayer[Owner] = pending;
        pending.Add(IsUpgraded ? 3 : 2);
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
            !PendingVulnerableByPlayer.Remove(Owner, out var pendingAmounts))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingAmounts.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (int amount in pendingAmounts)
            {
                foreach (var enemy in DivinerCardActions.HittableEnemies(this))
                {
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, amount, Owner.Creature, this, false);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class Horoscope : DivinerCard
{
    public Horoscope()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Horoscope",
        "Deal !Damage! damage. If the top card of your draw pile is an Attack, draw it.",
        "星盘",
        "造成 !Damage! 点伤害。如果抽牌堆顶是攻击牌，抽它。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        var topCard = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault();
        if (topCard?.Type == CardType.Attack)
        {
            await CardPileCmd.Add(topCard, PileType.Hand, CardPilePosition.Bottom, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class RedThread : DivinerCard
{
    public RedThread()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Red Thread",
        "Deal !Damage! damage. Search your draw pile for an Attack and put it on top.",
        "红线",
        "造成 !Damage! 点伤害。从你的抽牌堆中选择一张攻击牌放到牌堆顶。",
        ("selectPrompt", "Choose an Attack to put on top of your draw pile.", "选择一张攻击牌放到抽牌堆顶。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        var selected = await DivinerCardActions.SelectFromDrawPile(
            this,
            choiceContext,
            "selectPrompt",
            1,
            1,
            card => card.Type == CardType.Attack);
        var attack = selected.FirstOrDefault();
        if (attack != null)
        {
            await CardPileCmd.Add(attack, PileType.Draw, CardPilePosition.Top, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class Hexagram : DivinerCard
{
    public Hexagram()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6, 1);
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Hexagram",
        "Deal !Damage! damage to a random enemy 6 times. Revelation: choose the target.",
        "六爻",
        "随机对敌人造成 !Damage! 点伤害 6 次。启示：选择目标。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = DivinerCardActions.HittableEnemies(this);
        if (enemies.Count == 0)
        {
            return;
        }

        Creature target = DivinerCombatRuntime.HasEnlightenmentEffect(Owner) && cardPlay.Target != null
            ? cardPlay.Target
            : enemies[0];
        for (int i = 0; i < 6; i++)
        {
            await CommonActions.CardAttack(this, target, IsUpgraded ? 7 : 6).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class BackdatedWound : DivinerCard
{
    private const string ForetellLabel = "Wound";
    private static readonly Dictionary<Player, List<PendingHit>> PendingHitsByPlayer = [];

    public BackdatedWound()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 2);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Backdated Wound",
        "Deal !Damage! damage. Foretell: deal 8 damage.",
        "倒填伤痕",
        "造成 !Damage! 点伤害。预言：造成 8 点伤害。",
        ("upgradedDesc", "Deal !Damage! damage. Foretell: deal 10 damage.", "造成 !Damage! 点伤害。预言：造成 10 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (cardPlay.Target == null)
        {
            return;
        }

        var pending = PendingHitsByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingHitsByPlayer[Owner] = pending;
        pending.Add(new PendingHit(cardPlay.Target, IsUpgraded ? 10 : 8));
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
            !PendingHitsByPlayer.Remove(Owner, out var pendingHits))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingHits.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            var hittableEnemies = DivinerCardActions.HittableEnemies(this).ToHashSet();
            foreach (var hit in pendingHits)
            {
                if (hittableEnemies.Contains(hit.Target))
                {
                    await CreatureCmd.Damage(choiceContext, hit.Target, hit.Damage, DamageProps.nonCardUnpowered, Owner.Creature, this);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }

    private readonly record struct PendingHit(Creature Target, int Damage);
}

public class Inevitability : DivinerCard
{
    public Inevitability()
        : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(30, 8);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Inevitability",
        "Deal !Damage! damage. Costs 1 less for every 2 recorded divinations.",
        "必然",
        "造成 !Damage! 点伤害。每有 2 条已记录的占卜，本牌费用减少 1 点。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this))
        {
            return false;
        }

        modifiedCost = Math.Max(0, originalCost - (DivinationService.CurrentRecords.Count / 2));
        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
    }
}

public class CursedPrediction : DivinerCard
{
    public CursedPrediction()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(10, 0);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Cursed Prediction",
        "Deal !Damage! damage. Add a Misfortune to your discard pile.",
        "受咒预言",
        "造成 !Damage! 点伤害。将一张厄运加入弃牌堆。",
        ("upgradedDesc", "Deal !Damage! damage. Add a Misfortune to your hand.", "造成 !Damage! 点伤害。将一张厄运加入手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
            this,
            IsUpgraded ? PileType.Hand : PileType.Discard,
            CardPilePosition.Bottom);
    }

    protected override void OnUpgrade()
    {
    }
}

public class DeadStar : DivinerCard
{
    public DeadStar()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(18, 4);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Dead Star",
        "Deal !Damage! damage. Lose 1 HP. Bad Omen: deal !Damage! damage again.",
        "死星",
        "造成 !Damage! 点伤害。失去 1 点生命。凶兆：再次造成 !Damage! 点伤害。",
        ("upgradedDesc", "Deal !Damage! damage. Lose 1 HP. Bad Omen: deal !Damage! damage again.", "造成 !Damage! 点伤害。失去 1 点生命。凶兆：再次造成 !Damage! 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 1, DamageProps.nonCardHpLoss, Owner.Creature, this);
        if (cardPlay.Target != null && DestinyService.IsBadOmen())
        {
            await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class Verdict : DivinerCard
{
    public Verdict()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, 3);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Verdict",
        "Deal !Damage! damage. If the target has Weak or Vulnerable, deal 12 instead.",
        "裁决",
        "造成 !Damage! 点伤害。如果目标有虚弱或易伤，改为造成 12 点伤害。",
        ("upgradedDesc", "Deal !Damage! damage. If the target has Weak or Vulnerable, deal 16 instead.", "造成 !Damage! 点伤害。如果目标有虚弱或易伤，改为造成 16 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null && DivinerCardActions.HasWeakOrVulnerable(cardPlay.Target))
        {
            await CommonActions.CardAttack(this, cardPlay.Target, IsUpgraded ? 16 : 12).Execute(choiceContext);
            return;
        }

        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}

public class Augury : DivinerCard
{
    public Augury()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithBlock(12, 4);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Augury",
        "If you have divinated this combat, gain 1 Destiny. Otherwise gain !Block! Block.",
        "占兆",
        "如果你本场战斗中占卜过，获得 1 点命运。否则获得 !Block! 点格挡。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (DivinerCombatRuntime.HasDivinatedThisCombat)
        {
            DestinyService.AddDestiny(1);
            DestinyService.PersistCurrentState(Owner.RunState);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
            return;
        }

        await CommonActions.CardBlock(this, cardPlay);
    }

    protected override void OnUpgrade()
    {
    }
}

public class SecondSight : DivinerCard
{
    public SecondSight()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Retain, CardKeyword.Exhaust]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Second Sight",
        "Draw until you have 5 cards in hand.",
        "二重视界",
        "抽牌直到你有 5 张手牌。",
        ("upgradedDesc", "Draw until you have 6 cards in hand.", "抽牌直到你有 6 张手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.DrawUntilHandSize(this, choiceContext, IsUpgraded ? 6 : 5);
    }

    protected override void OnUpgrade()
    {
    }
}

public class LoadedReading : DivinerCard
{
    public LoadedReading()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Loaded Reading",
        "Add a Fortune and a Misfortune to your hand.",
        "暗藏玄机",
        "将一张福运和一张厄运加入你的手牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.AddGeneratedToCombat<Fortune>(this, PileType.Hand, CardPilePosition.Bottom);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(this, PileType.Hand, CardPilePosition.Bottom);
    }

    protected override void OnUpgrade()
    {
    }
}

public class ColdReading : DivinerCard
{
    public ColdReading()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Cold Reading",
        "Apply 2 Weak and 2 Vulnerable. If you divinated this combat, apply to all enemies.",
        "冷读",
        "给予 2 层虚弱和 2 层易伤。如果你本场战斗中占卜过，改为给予所有敌人。",
        ("upgradedDesc", "Apply 3 Weak and 3 Vulnerable. If you divinated this combat, apply to all enemies.", "给予 3 层虚弱和 3 层易伤。如果你本场战斗中占卜过，改为给予所有敌人。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = IsUpgraded ? 3 : 2;
        if (DivinerCombatRuntime.HasDivinatedThisCombat)
        {
            await DivinerCardActions.ApplyWeakAndVulnerableToAll(this, choiceContext, amount);
            return;
        }

        if (cardPlay.Target != null)
        {
            await DivinerCardActions.ApplyWeakAndVulnerable(this, choiceContext, cardPlay.Target, amount);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class ReadAhead : DivinerCard
{
    public ReadAhead()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCards(2, 1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Read Ahead",
        "Draw !Cards! cards. Put !Cards! cards from your hand on top of your draw pile.",
        "预读",
        "抽 !Cards! 张牌。将 !Cards! 张手牌放到抽牌堆顶。",
        ("selectPrompt", "Choose cards to put on top of your draw pile.", "选择要放到抽牌堆顶的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = IsUpgraded ? 3 : 2;
        await CardPileCmd.Draw(choiceContext, count, Owner, false);
        var selectedCards = await DivinerCardActions.SelectFromHand(this, choiceContext, "selectPrompt", 0, count);
        foreach (var selectedCard in selectedCards.Reverse())
        {
            await CardPileCmd.Add(selectedCard, PileType.Draw, CardPilePosition.Top, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class UnaskedQuestion : DivinerCard
{
    public UnaskedQuestion()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Unasked Question",
        "Divinate. Lose 5 HP.",
        "未问之问",
        "占卜。失去 5 点生命。",
        ("upgradedDesc", "Divinate. Lose 3 HP.", "占卜。失去 3 点生命。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinationService.RecordPlaceholder(choiceContext, Owner, "Unasked Question");
        await CreatureCmd.Damage(choiceContext, Owner.Creature, IsUpgraded ? 3 : 5, DamageProps.nonCardHpLoss, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}

public class RewriteTheSign : DivinerCard
{
    public RewriteTheSign()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Rewrite the Sign",
        "Replace all Misfortunes in your hand, draw pile, and discard pile with Fortunes.",
        "改写征兆",
        "将你的手牌、抽牌堆和弃牌堆中所有厄运替换为福运。",
        ("upgradedDesc", "Replace all Misfortunes and Escapes from Destiny in your hand, draw pile, and discard pile with Fortunes.", "将你的手牌、抽牌堆和弃牌堆中所有厄运和逃离命运替换为福运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ReplaceInPile(choiceContext, PileType.Hand);
        await ReplaceInPile(choiceContext, PileType.Draw);
        await ReplaceInPile(choiceContext, PileType.Discard);
    }

    private async Task ReplaceInPile(PlayerChoiceContext choiceContext, PileType pileType)
    {
        var cardsToReplace = pileType.GetPile(Owner).Cards
            .Where(card => card is Misfortune || (IsUpgraded && card is EscapeFromDestiny))
            .ToList();
        foreach (var card in cardsToReplace)
        {
            await CardCmd.Exhaust(choiceContext, card, false, false);
            await DivinerCardActions.AddGeneratedToCombat<Fortune>(this, pileType, CardPilePosition.Bottom);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class PredestinedPath : DivinerCard
{
    private const string ForetellLabel = "Fated draw";
    private static readonly Dictionary<Player, List<CardModel>> PendingCardsByPlayer = [];

    public PredestinedPath()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell, DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Predestined Path",
        "Choose 2 cards from your draw pile. Foretell: put them into your hand; they are Fated that turn.",
        "既定路径",
        "从抽牌堆中选择 2 张牌。预言：将它们加入手牌；它们在该回合中为注定。",
        ("selectPrompt", "Choose cards to draw next turn.", "选择下回合要抽的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectedCards = (await DivinerCardActions.SelectFromDrawPile(this, choiceContext, "selectPrompt", 0, 2)).ToList();
        if (selectedCards.Count == 0)
        {
            return;
        }

        var pending = PendingCardsByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingCardsByPlayer[Owner] = pending;
        pending.AddRange(selectedCards);
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel, selectedCards.Count);
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
            !PendingCardsByPlayer.Remove(Owner, out var pendingCards))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingCards.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        await DivinerCardActions.MoveToHandFated(this, pendingCards);
        for (int trigger = 1; trigger < triggerCount; trigger++)
        {
            foreach (var card in pendingCards)
            {
                DivinerCardActions.MakeFated(card);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class EvilEye : DivinerCard
{
    public EvilEye()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Evil Eye",
        "Apply 3 Weak. Bad Omen: gain [E].",
        "邪眼",
        "给予 3 层虚弱。凶兆：获得 [E]。",
        ("upgradedDesc", "Apply 5 Weak. Bad Omen: gain [E].", "给予 5 层虚弱。凶兆：获得 [E]。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, IsUpgraded ? 5 : 3, Owner.Creature, this, false);
        }

        if (DestinyService.IsBadOmen())
        {
            await PlayerCmd.GainEnergy(1, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class ReadTheAshes : DivinerCard
{
    private const string DrawForetellLabel = "Ashes draw";
    private const string EnergyForetellLabel = "Ashes energy";
    private static readonly Dictionary<Player, List<PendingAshes>> PendingAshesByPlayer = [];

    public ReadTheAshes()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Read the Ashes",
        "Exhaust a card. Foretell: draw 1 card. If a Status or Curse is Exhausted, Foretell: gain [E] [E].",
        "读灰",
        "消耗一张牌。预言：抽 1 张牌。如果消耗的是状态牌或诅咒牌，预言：获得 [E] [E]。",
        ("selectPrompt", "Choose a card to Exhaust.", "选择一张牌消耗。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectedCard = (await DivinerCardActions.SelectFromHand(this, choiceContext, "selectPrompt", 1, 1)).FirstOrDefault();
        if (selectedCard == null)
        {
            return;
        }

        bool wasStatusOrCurse = selectedCard.Type is CardType.Status or CardType.Curse;
        await CardCmd.Exhaust(choiceContext, selectedCard, false, false);

        var pending = PendingAshesByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingAshesByPlayer[Owner] = pending;
        pending.Add(new PendingAshes(wasStatusOrCurse));
        DivinerCombatRuntime.QueueForetell(Owner, DrawForetellLabel);
        if (wasStatusOrCurse)
        {
            DivinerCombatRuntime.QueueForetell(Owner, EnergyForetellLabel);
        }

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
            !PendingAshesByPlayer.Remove(Owner, out var pendingAshes))
        {
            return;
        }

        int drawTriggerCount = DivinerCombatRuntime.ResolveForetell(Owner, DrawForetellLabel, pendingAshes.Count);
        int energyTriggerCount = DivinerCombatRuntime.ResolveForetell(Owner, EnergyForetellLabel, pendingAshes.Count(ashes => ashes.GainEnergy));
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < drawTriggerCount; trigger++)
        {
            foreach (var ashes in pendingAshes)
            {
                await CardPileCmd.Draw(choiceContext, 1, Owner, false);
            }
        }
        for (int trigger = 0; trigger < energyTriggerCount; trigger++)
        {
            foreach (var ashes in pendingAshes.Where(ashes => ashes.GainEnergy))
            {
                await PlayerCmd.GainEnergy(2, Owner);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }

    private readonly record struct PendingAshes(bool GainEnergy);
}

public class BorrowedTomorrow : DivinerCard
{
    private const string ForetellLabel = "Lose Energy";
    private static readonly Dictionary<Player, int> PendingEnergyLossByPlayer = [];

    public BorrowedTomorrow()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Borrowed Tomorrow",
        "Gain [E] [E] [E]. Foretell: lose [E] [E].",
        "借来的明天",
        "获得 [E] [E] [E]。预言：失去 [E] [E]。",
        ("upgradedDesc", "Gain [E] [E] [E] [E]. Foretell: lose [E] [E].", "获得 [E] [E] [E] [E]。预言：失去 [E] [E]。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(IsUpgraded ? 4 : 3, Owner);
        PendingEnergyLossByPlayer[Owner] = PendingEnergyLossByPlayer.GetValueOrDefault(Owner) + 2;
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
            !PendingEnergyLossByPlayer.Remove(Owner, out var energyLoss))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            await PlayerCmd.LoseEnergy(energyLoss, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class FuneralClock : DivinerCard
{
    public FuneralClock()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithBlock(8, 3);
        WithCards(1);
        WithDivinerKeywordTips(DivinerKeywords.Dredge);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Funeral Clock",
        "Gain !Block! Block. Doomed: costs 1 less; gain 1 Countdown of Destiny and draw !Cards! card.",
        "丧钟",
        "获得 !Block! 点格挡。劫兆：费用减少 1 点；获得 1 层命运倒计时并抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (DestinyConstants.IsDredgeDestiny(DestinyService.CurrentDestiny))
        {
            await DivinerCombatRuntime.IncreaseDredgeCountdown(choiceContext, Owner, 1);
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        }
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) || !DestinyConstants.IsDredgeDestiny(DestinyService.CurrentDestiny))
        {
            return false;
        }

        modifiedCost = Math.Max(0, originalCost - 1);
        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
    }
}

public class WhiteRoom : DivinerCard
{
    public WhiteRoom()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCards(2);
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment, DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "White Room",
        "Draw !Cards! cards. Revelation: choose up to 2 cards in your hand. They are Fated.",
        "白室",
        "抽 !Cards! 张牌。启示：选择至多 2 张手牌。它们为注定。",
        ("upgradedDesc", "Draw !Cards! cards. Revelation: choose up to 3 cards in your hand. They are Fated.", "抽 !Cards! 张牌。启示：选择至多 3 张手牌。它们为注定。"),
        ("selectPrompt", "Choose cards to make Fated.", "选择要变为注定的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 2, Owner, false);
        if (!DivinerCombatRuntime.HasEnlightenmentEffect(Owner))
        {
            return;
        }

        var selectedCards = await DivinerCardActions.SelectFromHand(this, choiceContext, "selectPrompt", 0, IsUpgraded ? 3 : 2);
        foreach (var selectedCard in selectedCards)
        {
            DivinerCardActions.MakeFated(selectedCard);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class PropheticTrance : DivinerCard
{
    public PropheticTrance()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Innate]);
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Prophetic Trance",
        "Innate. Whenever you Divinate, draw 2 cards.",
        "预言恍惚",
        "固有。每当你占卜时，抽 2 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PropheticTrancePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class TheWrittenHour : DivinerCard
{
    public TheWrittenHour()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "The Written Hour",
        "At the start of your turn, if Destiny is exactly 3, gain [E] and draw 1 card.",
        "写定时刻",
        "你的回合开始时，如果命运正好为 3，获得 [E] 并抽 1 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TheWrittenHourPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class PatternRecognition : DivinerCard
{
    public PatternRecognition()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Pattern Recognition",
        "Whenever you play the third card in a turn, Good Omen: gain 3 Block. Bad Omen: deal 3 damage to all enemies.",
        "模式识别",
        "每回合每当你打出第三张牌时，吉兆：获得 3 点格挡。凶兆：对所有敌人造成 3 点伤害。",
        ("upgradedDesc", "Whenever you play the third card in a turn, Good Omen: gain 4 Block. Bad Omen: deal 4 damage to all enemies.", "每回合每当你打出第三张牌时，吉兆：获得 4 点格挡。凶兆：对所有敌人造成 4 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PatternRecognitionPower>(choiceContext, Owner.Creature, IsUpgraded ? 4 : 3, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class HaruspexMethod : DivinerCard
{
    public HaruspexMethod()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Divinate, DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Haruspex Method",
        "The next time you Exhaust a card, Divinate and Foretell: add a Haruspex Method to your hand.",
        "肝占法",
        "下次你消耗一张牌时，占卜，并预言：将一张肝占法加入手牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HaruspexMethodPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class ChosenLine : DivinerCard
{
    public ChosenLine()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment, DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Chosen Line",
        "Only playable if Revelation. The first card you draw each turn is Fated. Draw 1 extra card per turn.",
        "所选命线",
        "仅限启示时打出。每回合你抽到的第一张牌为注定。每回合多抽 1 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (DivinerCombatRuntime.HasEnlightenmentEffect(Owner))
        {
            await PowerCmd.Apply<ChosenLinePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class DoomEngine : DivinerCard
{
    public DoomEngine()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Doom Engine",
        "Misfortunes HP lost reduced by 1 and deal 9 more damage. Add a Misfortune to your hand.",
        "厄运引擎",
        "厄运失去的生命减少 1 点，并额外造成 9 点伤害。将一张厄运加入手牌。",
        ("upgradedDesc", "Misfortunes HP lost reduced by 2 and deal 13 more damage. Add a Misfortune to your hand.", "厄运失去的生命减少 2 点，并额外造成 13 点伤害。将一张厄运加入手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DoomEnginePower>(choiceContext, Owner.Creature, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(this, PileType.Hand, CardPilePosition.Bottom);
    }

    protected override void OnUpgrade()
    {
    }
}

public class LedgerOfSigns : DivinerCard
{
    public LedgerOfSigns()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Ledger of Signs",
        "Every 3 times you Foretell, add a Fortune to your hand.",
        "征兆账簿",
        "每当你预言 3 次，将一张福运加入你的手牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LedgerOfSignsPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}
