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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Uncommon;

public class StarNeedle : DivinerCard
{
    public StarNeedle()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(3);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Star Needle",
        "Deal !Damage! damage. For every 7 recorded divinations, damage increases by 2 and apply 1 Vulnerable.",
        "星针",
        "造成 !Damage! 点伤害。每有 7 条已记录的占卜，伤害提高 2 点并给予 1 层易伤。",
        ("upgradedDesc", "Deal !Damage! damage. For every 7 recorded divinations, damage increases by 3 and apply 2 Vulnerable.", "造成 !Damage! 点伤害。每有 7 条已记录的占卜，伤害提高 3 点并给予 2 层易伤。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int sets = DivinationService.CurrentRecords.Count / 7;
        await CommonActions.CardAttack(this, cardPlay.Target, 3 + sets * (IsUpgraded ? 3 : 2)).Execute(choiceContext);
        int vulnerable = sets * (IsUpgraded ? 2 : 1);
        if (cardPlay.Target != null && vulnerable > 0)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, vulnerable, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class Doomscript : DivinerCard
{
    public Doomscript()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Doomscript",
        "Deal !Damage! damage. Add a Misfortune to your draw pile.",
        "厄文",
        "造成 !Damage! 点伤害。将一张厄运加入抽牌堆。",
        ("upgradedDesc", "Deal !Damage! damage. Add a Misfortune+ to your draw pile.", "造成 !Damage! 点伤害。将一张厄运+加入抽牌堆。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
            this,
            PileType.Draw,
            CardPilePosition.Bottom,
            IsUpgraded);
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
        "Deal !Damage! damage. If the top card of your draw pile is an Attack, play it.",
        "星盘",
        "造成 !Damage! 点伤害。如果抽牌堆顶是攻击牌，打出它。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        var topCard = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault();
        if (topCard?.Type == CardType.Attack && cardPlay.Target != null)
        {
            await CardCmd.AutoPlay(choiceContext, topCard, cardPlay.Target);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class RedThread : DivinerCard
{
    private static readonly Dictionary<Player, int> ImmediateForetellCardsByPlayer = [];

    static RedThread()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += ImmediateForetellCardsByPlayer.Clear;
    }

    public RedThread()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Red Thread",
        "Deal !Damage! damage. The next Foretell card you play this turn triggers right away.",
        "红线",
        "造成 !Damage! 点伤害。本回合你打出的下一张预言牌会立即触发。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        ImmediateForetellCardsByPlayer[Owner] = ImmediateForetellCardsByPlayer.GetValueOrDefault(Owner) + 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card.Owner, Owner) ||
            ReferenceEquals(cardPlay.Card, this) ||
            !ImmediateForetellCardsByPlayer.TryGetValue(Owner, out int remaining) ||
            !cardPlay.Card.CanonicalKeywords.Contains(DivinerKeywords.Foretell))
        {
            return;
        }

        if (remaining <= 1)
        {
            ImmediateForetellCardsByPlayer.Remove(Owner);
        }
        else
        {
            ImmediateForetellCardsByPlayer[Owner] = remaining - 1;
        }

        await DivinerCombatRuntime.TriggerAllForetellNow(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
    }
}

public class Hexagram : DivinerCard
{
    public Hexagram()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(3, 1);
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Hexagram",
        "Deal !Damage! damage to a random enemy 6 times. Revelation: damage increased by 9.",
        "六爻",
        "随机对敌人造成 !Damage! 点伤害 6 次。启示：伤害提高 9 点。",
        ("upgradedDesc", "Deal !Damage! damage to a random enemy 6 times. Revelation: damage increased by 11.", "随机对敌人造成 !Damage! 点伤害 6 次。启示：伤害提高 11 点。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = DivinerCardActions.HittableEnemies(this);
        if (enemies.Count == 0)
        {
            return;
        }

        int damage = (IsUpgraded ? 4 : 3) +
                     (await DivinerCombatRuntime.TryConsumeRevelationEffect(choiceContext, Owner) ? IsUpgraded ? 11 : 9 : 0);
        for (int i = 0; i < 6; i++)
        {
            var target = enemies[Random.Shared.Next(enemies.Count)];
            await CreatureCmd.Damage(choiceContext, target, damage, DamageProps.card, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class OmenOfPestilence : DivinerCard
{
    private const string ForetellLabel = "Pestilence";
    private static readonly Dictionary<Player, List<PendingPestilence>> PendingPestilenceByPlayer = [];

    static OmenOfPestilence()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingPestilenceByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public OmenOfPestilence()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omen of Pestilence",
        "Foretell: Apply 3 Weak and 9 Poison to all enemies.",
        "疫病征兆",
        "预言：给予所有敌人 3 层虚弱和 9 层中毒。",
        ("upgradedDesc", "Foretell: Apply 4 Weak and 12 Poison to all enemies.", "预言：给予所有敌人 4 层虚弱和 12 层中毒。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pending = PendingPestilenceByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingPestilenceByPlayer[Owner] = pending;
        pending.Add(new PendingPestilence(IsUpgraded ? 4 : 3, IsUpgraded ? 12 : 9));
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
            !PendingPestilenceByPlayer.Remove(Owner, out var pendingPestilence))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, pendingPestilence.Count);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var pestilence in pendingPestilence)
            {
                foreach (var enemy in DivinerCardActions.HittableEnemies(this))
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, enemy, pestilence.Weak, Owner.Creature, this, false);
                    await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, pestilence.Poison, Owner.Creature, this, false);
                }
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingPestilenceByPlayer.Remove(player, out var pendingPestilence))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingPestilence.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (var pestilence in pendingPestilence)
            {
                foreach (var enemy in DivinerCombatRuntime.HittableEnemiesFor(player))
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, enemy, pestilence.Weak, player.Creature, null!, false);
                    await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, pestilence.Poison, player.Creature, null!, false);
                }
            }
        }

        return triggerCount * pendingPestilence.Count;
    }

    protected override void OnUpgrade()
    {
    }

    private readonly record struct PendingPestilence(int Weak, int Poison);
}

public class Inevitability : DivinerCard
{
    public Inevitability()
        : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(52, 0);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Inevitability",
        "Deal !Damage! damage. Costs 1 less for every 4 recorded divinations.",
        "必然",
        "造成 !Damage! 点伤害。每有 4 条已记录的占卜，本牌费用减少 1 点。",
        ("upgradedDesc", "Deal !Damage! damage. Costs 1 less for every 3 recorded divinations.", "造成 !Damage! 点伤害。每有 3 条已记录的占卜，本牌费用减少 1 点。")
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

        int divisor = IsUpgraded ? 3 : 4;
        modifiedCost = Math.Max(0, originalCost - (DivinationService.CurrentRecords.Count / divisor));
        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
    }
}

public class CursedPrediction : DivinerCard
{
    public CursedPrediction()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(11, 0);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Cursed Prediction",
        "Deal !Damage! damage. Bad Omen: add a Misfortune to your hand.",
        "受咒预言",
        "造成 !Damage! 点伤害。凶兆：将一张厄运加入手牌。",
        ("upgradedDesc", "Deal !Damage! damage. Bad Omen: add a Misfortune+ to your hand.", "造成 !Damage! 点伤害。凶兆：将一张厄运+加入手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (DestinyService.IsBadOmen())
        {
            await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
                this,
                PileType.Hand,
                CardPilePosition.Bottom,
                IsUpgraded);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class DeadStar : DivinerCard
{
    public DeadStar()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(18, 4);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Dead Star",
        "Deal !Damage! damage. Lose 3 HP. Bad Omen: deal !Damage! damage again.",
        "死星",
        "造成 !Damage! 点伤害。失去 3 点生命。凶兆：再次造成 !Damage! 点伤害。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 3, DamageProps.nonCardHpLoss, Owner.Creature, this);
        if (cardPlay.Target != null && DestinyService.IsBadOmen())
        {
            await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class Augury : DivinerCard
{
    public Augury()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithBlock(12, 0);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Augury",
        "Gain !Block! Block. If you have divinated at least twice this combat, gain 1 Destiny.",
        "占兆",
        "获得 !Block! 点格挡。如果你本场战斗中占卜过至少两次，获得 1 点命运。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (DivinerCombatRuntime.CombatDivinationCount >= 2)
        {
            DestinyService.AddDestiny(1);
            DestinyService.PersistCurrentState(Owner.RunState);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        }
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
        WithKeywords([CardKeyword.Retain]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Second Sight",
        "Draw until you have 5 cards in hand.",
        "二重视界",
        "抽牌直到你有 5 张手牌。",
        ("upgradedDesc", "Draw until you have 7 cards in hand.", "抽牌直到你有 7 张手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.DrawUntilHandSize(this, choiceContext, IsUpgraded ? 7 : 5);
    }

    protected override void OnUpgrade()
    {
    }
}

public class WheelOfFortune : DivinerCard
{
    public WheelOfFortune()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Wheel of Fortune",
        "Add a Fortune and a Misfortune to your hand.",
        "命运之轮",
        "将一张福运和一张厄运加入你的手牌。",
        ("upgradedDesc", "Add a Fortune+ and a Misfortune+ to your hand.", "将一张福运+和一张厄运+加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.AddGeneratedToCombat<Fortune>(this, PileType.Hand, CardPilePosition.Bottom, IsUpgraded);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(this, PileType.Hand, CardPilePosition.Bottom, IsUpgraded);
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
        WithCards(2, 0);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Cold Reading",
        "Apply 1 Weak. Draw !Cards! cards.",
        "冷读",
        "给予 1 层虚弱。抽 !Cards! 张牌。",
        ("upgradedDesc", "Apply 2 Weak. Draw !Cards! cards.", "给予 2 层虚弱。抽 !Cards! 张牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
        }

        await CardPileCmd.Draw(choiceContext, 2, Owner, false);
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
        WithCards(3, 1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Read Ahead",
        "Draw !Cards! cards. Put up to !Cards! cards from your hand on top of your draw pile.",
        "预读",
        "抽 !Cards! 张牌。将至多 !Cards! 张手牌放到抽牌堆顶。",
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
        "Transform all Misfortunes in your hand, draw pile, and discard pile into Fortunes.",
        "改写征兆",
        "将你的手牌、抽牌堆和弃牌堆中所有厄运变化为福运。",
        ("upgradedDesc", "Transform all Misfortunes in your hand, draw pile, and discard pile into Fortune+.", "将你的手牌、抽牌堆和弃牌堆中所有厄运变化为福运+。")
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
            .Where(card => card is Misfortune)
            .ToList();
        foreach (var card in cardsToReplace)
        {
            var result = await CardCmd.TransformTo<Fortune>(card, CardPreviewStyle.HorizontalLayout);
            if (IsUpgraded && result is { success: true, cardAdded: { } replacement })
            {
                CardCmd.Upgrade(replacement);
            }
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

    static PredestinedPath()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingCardsByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public PredestinedPath()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Foretell, DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Predestined Path",
        "Choose up to 1 card from your draw pile. Foretell: put it into your hand; it is Fated that turn.",
        "既定路径",
        "从抽牌堆中选择至多 1 张牌。预言：将其加入手牌；它在该回合中为注定。",
        ("upgradedDesc", "Choose up to 2 cards from your draw pile. Foretell: put them into your hand; they are Fated that turn.", "从抽牌堆中选择至多 2 张牌。预言：将它们加入手牌；它们在该回合中为注定。"),
        ("selectPrompt", "Choose cards to draw next turn.", "选择下回合要抽的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectedCards = (await DivinerCardActions.SelectFromDrawPile(this, choiceContext, "selectPrompt", 0, IsUpgraded ? 2 : 1)).ToList();
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

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingCardsByPlayer.Remove(player, out var pendingCards))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingCards.Count);
        foreach (var card in pendingCards)
        {
            DivinerCardActions.MakeFated(card);
        }

        await CardPileCmd.Add(pendingCards, PileType.Hand, CardPilePosition.Bottom, null!, false);
        for (int trigger = 1; trigger < triggerCount; trigger++)
        {
            foreach (var card in pendingCards)
            {
                DivinerCardActions.MakeFated(card);
            }
        }

        return triggerCount * pendingCards.Count;
    }

    protected override void OnUpgrade()
    {
    }
}

public class EvilEye : DivinerCard
{
    public EvilEye()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Evil Eye",
        "Apply 3 Weak. Bad Omen: gain 1 Energy.",
        "邪眼",
        "给予 3 层虚弱。凶兆：获得 1 点能量。",
        ("upgradedDesc", "Apply 5 Weak. Bad Omen: gain 1 Energy.", "给予 5 层虚弱。凶兆：获得 1 点能量。")
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

    static ReadTheAshes()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingAshesByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public ReadTheAshes()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Read the Ashes",
        "Exhaust a card. Foretell: draw 2 cards. If a Status or Curse is Exhausted, Foretell: gain 2 Energy.",
        "读灰",
        "消耗一张牌。预言：抽 2 张牌。如果消耗的是状态牌或诅咒牌，预言：获得 2 点能量。",
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

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) ||
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
                await CardPileCmd.Draw(choiceContext, 2, Owner, false);
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

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingAshesByPlayer.Remove(player, out var pendingAshes))
        {
            return 0;
        }

        int drawTriggerCount = DivinerCombatRuntime.ResolveForetell(player, DrawForetellLabel, pendingAshes.Count);
        int energyForetellCount = pendingAshes.Count(ashes => ashes.GainEnergy);
        int energyTriggerCount = DivinerCombatRuntime.ResolveForetell(player, EnergyForetellLabel, energyForetellCount);
        for (int trigger = 0; trigger < drawTriggerCount; trigger++)
        {
            foreach (var ashes in pendingAshes)
            {
                await CardPileCmd.Draw(choiceContext, 2, player, false);
            }
        }

        for (int trigger = 0; trigger < energyTriggerCount; trigger++)
        {
            foreach (var ashes in pendingAshes.Where(ashes => ashes.GainEnergy))
            {
                await PlayerCmd.GainEnergy(2, player);
            }
        }

        return drawTriggerCount * pendingAshes.Count + energyTriggerCount * energyForetellCount;
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

    static BorrowedTomorrow()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingEnergyLossByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public BorrowedTomorrow()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Borrowed Tomorrow",
        "Gain 3 Energy. Foretell: lose 1 Energy.",
        "借来的明天",
        "获得 3 点能量。预言：失去 1 点能量。",
        ("upgradedDesc", "Gain 4 Energy. Foretell: lose 1 Energy.", "获得 4 点能量。预言：失去 1 点能量。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(IsUpgraded ? 4 : 3, Owner);
        PendingEnergyLossByPlayer[Owner] = PendingEnergyLossByPlayer.GetValueOrDefault(Owner) + 1;
        DivinerCombatRuntime.QueueForetell(Owner, ForetellLabel);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) ||
            !PendingEnergyLossByPlayer.Remove(Owner, out var energyLoss))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(Owner, ForetellLabel, energyLoss);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            await PlayerCmd.LoseEnergy(energyLoss, Owner);
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingEnergyLossByPlayer.Remove(player, out var energyLoss))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, energyLoss);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            await PlayerCmd.LoseEnergy(energyLoss, player);
        }

        return triggerCount * energyLoss;
    }

    protected override void OnUpgrade()
    {
    }
}

public class FuneralClock : DivinerCard
{
    public FuneralClock()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(13, 3);
        WithCards(2);
        WithDivinerKeywordTips(DivinerKeywords.Dredge);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Funeral Clock",
        "Gain !Block! Block. Doomed: costs 0 and draw !Cards! cards.",
        "丧钟",
        "获得 !Block! 点格挡。劫兆：费用变为 0 并抽 !Cards! 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (DestinyService.CanUseDestiny(Owner) &&
            DestinyConstants.IsDredgeDestiny(DestinyService.CurrentDestiny))
        {
            await CardPileCmd.Draw(choiceContext, 2, Owner, false);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        }
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) ||
            !DestinyService.CanUseDestiny(Owner) ||
            !DestinyConstants.IsDredgeDestiny(DestinyService.CurrentDestiny))
        {
            return false;
        }

        modifiedCost = 0;
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
        WithCards(3, 1);
        WithDivinerKeywordTips(DivinerKeywords.Enlightenment, DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "White Room",
        "Draw !Cards! cards. Revelation: choose up to 4 cards in your hand. They are Fated.",
        "白室",
        "抽 !Cards! 张牌。启示：选择至多 4 张手牌。它们为注定。",
        ("upgradedDesc", "Draw !Cards! cards. Revelation: choose up to 6 cards in your hand. They are Fated.", "抽 !Cards! 张牌。启示：选择至多 6 张手牌。它们为注定。"),
        ("selectPrompt", "Choose cards to make Fated.", "选择要变为注定的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, IsUpgraded ? 4 : 3, Owner, false);
        if (!await DivinerCombatRuntime.TryConsumeRevelationEffect(choiceContext, Owner))
        {
            return;
        }

        var selectedCards = await DivinerCardActions.SelectFromHand(this, choiceContext, "selectPrompt", 0, IsUpgraded ? 6 : 4);
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
        "占卜恍惚",
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
        "At the start of your turn, if Destiny is exactly 3, gain 1 Energy and draw 1 card.",
        "写定时刻",
        "你的回合开始时，如果命运正好为 3，获得 1 点能量并抽 1 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TheWrittenHourPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
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
        "Haruspex",
        "The next time you Exhaust a card, Divinate and Foretell: add a Haruspex to your hand.",
        "脏卜术",
        "下次你消耗一张牌时，占卜，并预言：将一张脏卜术加入手牌。",
        ("upgradedDesc", "The next time you Exhaust a card, Divinate and Foretell: add a Haruspex+ to your hand.", "下次你消耗一张牌时，占卜，并预言：将一张脏卜术+加入手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HaruspexMethodPower>(choiceContext, Owner.Creature, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
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
        "Revelation: draw 1 extra card per turn, and the first card you draw each turn is Fated.",
        "所选命线",
        "启示：每回合多抽 1 张牌，且每回合你抽到的第一张牌为注定。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (await DivinerCombatRuntime.TryConsumeRevelationEffect(choiceContext, Owner))
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
        : base(2, CardType.Power, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Dredge);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Doom Engine",
        "Misfortunes' HP loss is reduced by 2 and they deal 9 more damage. Doomed: this card costs 0.",
        "厄运引擎",
        "厄运失去的生命减少 2 点，并额外造成 9 点伤害。劫兆：本牌费用为 0。",
        ("upgradedDesc", "Misfortunes' HP loss is reduced by 2 and they deal 13 more damage. Doomed: this card costs 0.", "厄运失去的生命减少 2 点，并额外造成 13 点伤害。劫兆：本牌费用为 0。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DoomEnginePower.RecordEngine(Owner);
        await PowerCmd.Apply<DoomEnginePower>(choiceContext, Owner.Creature, IsUpgraded ? 13 : 9, Owner.Creature, this, false);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) || !DestinyConstants.IsDredgeDestiny(DestinyService.CurrentDestiny))
        {
            return false;
        }

        modifiedCost = 0;
        return modifiedCost != originalCost;
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
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Ledger of Signs",
        "Every time you queue a Foretell, gain 5 Block.",
        "征兆账簿",
        "每当你排入一个预言，获得 5 点格挡。",
        ("upgradedDesc", "Every time you queue a Foretell, gain 7 Block.", "每当你排入一个预言，获得 7 点格挡。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LedgerOfSignsPower>(choiceContext, Owner.Creature, IsUpgraded ? 7 : 5, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}
