using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using Diviner.DivinerCode.UI;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Rare;

public class Clairvoyance : DivinerCard
{
    public Clairvoyance()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Clairvoyance",
        "Search your draw pile for 1 card. Put it into your hand; it is Fated.",
        "千里眼",
        "从你的抽牌堆中选择 1 张牌加入手牌；它为注定。",
        ("selectPrompt", "Choose a card to put into your hand.", "选择一张牌加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await DivinerCardActions.SelectFromDrawPile(this, choiceContext, "selectPrompt", 0, 1);
        await DivinerCardActions.MoveToHandFated(this, selected);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

public class Apocalypse : DivinerCard
{
    public Apocalypse()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(50, 10);
        WithDivinerKeywordTips(DivinerKeywords.Dredge, DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Apocalypse",
        "Deal !Damage! damage to all enemies. Set Destiny to 0. Doomed: costs 3 less.",
        "天启",
        "对所有敌人造成 !Damage! 点伤害。将命运设为 0。劫兆：费用减少 3 点。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = DivinerCardActions.HittableEnemies(this);
        if (enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, IsUpgraded ? 60 : 50, DamageProps.nonCardUnpowered, Owner.Creature, this);
        }

        DestinyService.SetDestiny(DestinyConstants.DredgeDestiny);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    public override bool TryModifyEnergyCostInCombat(MegaCrit.Sts2.Core.Models.CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) || !DestinyConstants.IsDredgeDestiny(DestinyService.CurrentDestiny))
        {
            return false;
        }

        modifiedCost = Math.Max(0, originalCost - 3);
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

    public FallenSky()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(12, 4);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Fallen Sky",
        "Deal !Damage! damage to all enemies. Foretell: play this card again.",
        "坠天",
        "对所有敌人造成 !Damage! 点伤害。预言：再次打出本牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damage = IsUpgraded ? 16 : 12;
        var enemies = DivinerCardActions.HittableEnemies(this);
        if (enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, Owner.Creature, this);
        }

        var pending = PendingDamageByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingDamageByPlayer[Owner] = pending;
        pending.Add(damage);
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
                    await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, Owner.Creature, this);
                }
            }
        }
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
        "Deal !Damage! damage. Draw !Cards! card. Revelation: play this 3 times.",
        "命运之手",
        "造成 !Damage! 点伤害。抽 !Cards! 张牌。启示：打出本牌 3 次。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int repeats = DivinerCombatRuntime.HasEnlightenmentEffect(Owner) ? 3 : 1;
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
            DestinyService.AddDestiny(1);
            DestinyService.PersistCurrentState(Owner.RunState);
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

    public UnavoidableEnd()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(10, 4);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Unavoidable End",
        "Deal !Damage! damage. Foretell: deal double that damage to all enemies.",
        "无可避免的结局",
        "造成 !Damage! 点伤害。预言：对所有敌人造成两倍该伤害。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damage = IsUpgraded ? 14 : 10;
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        var pending = PendingDamageByPlayer.GetValueOrDefault(Owner) ?? [];
        PendingDamageByPlayer[Owner] = pending;
        pending.Add(damage * 2);
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
                    await CreatureCmd.Damage(choiceContext, enemies, damage, DamageProps.nonCardUnpowered, Owner.Creature, this);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class GreaterPortent : DivinerCard
{
    public GreaterPortent()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Fated);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Greater Portent",
        "Search 3 cards from your draw pile. Put them into your hand; they are Fated.",
        "大预兆",
        "从你的抽牌堆中选择 3 张牌加入手牌；它们为注定。",
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
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Reversal",
        "Set Destiny to 5 minus its current value.",
        "逆转",
        "将命运设为 5 减去当前命运。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.SetDestiny(DestinyConstants.MaxDestiny - DestinyService.CurrentDestiny);
        DestinyService.PersistCurrentState(Owner.RunState);
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
        "Gain 1 Destiny and add 3 Misfortunes to your draw pile.",
        "神谕交易",
        "获得 1 点命运，并将 3 张厄运加入抽牌堆。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.AddDestiny(1);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(this, 3, PileType.Draw, CardPilePosition.Bottom);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}

public class PerfectForecast : DivinerCard
{
    public PerfectForecast()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Perfect Forecast",
        "Divinate twice. Gain [E] for each unique category ever recorded.",
        "完美预测",
        "占卜两次。每有一种已记录过的独特类别，获得 [E]。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinationService.RecordPlaceholder(choiceContext, Owner, "Perfect Forecast");
        await DivinationService.RecordPlaceholder(choiceContext, Owner, "Perfect Forecast");
        int uniqueCategories = DivinationService.CurrentRecords
            .Select(record => record.Category)
            .Distinct()
            .Count();
        if (uniqueCategories > 0)
        {
            await PlayerCmd.GainEnergy(uniqueCategories, Owner);
        }
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
        "The next time you would die this combat, heal to 13 and set Destiny to 0.",
        "欺瞒终局",
        "本场战斗中下次你将要死亡时，恢复至 13 点生命并将命运设为 0。"
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
        : base(3, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Fixed Point",
        "Destiny cannot decrease below 3 this combat. At the start of your turn, lose 1 HP.",
        "定点",
        "本场战斗中命运不会降至 3 以下。你的回合开始时，失去 1 点生命。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FixedPointPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
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
        "Card rewards at the end of this combat have 1 additional option. When you Scry, choose 1 additional card.",
        "诸多未来",
        "本场战斗结束后的卡牌奖励多 1 个选项。每当你预见时，多查看 1 张牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ManyFuturesPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class DoomSpiral : DivinerCard
{
    public DoomSpiral()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Doom Spiral",
        "At end of turn, if Bad Omen, add a Misfortune to your hand.",
        "厄运螺旋",
        "回合结束时，如果处于凶兆，将一张厄运加入你的手牌。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DoomSpiralPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
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

public class BreakTheSequence : DivinerCard
{
    public BreakTheSequence()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Scry, DivinerKeywords.Enlightenment);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Break the Sequence",
        "Scry 8. Revelation: if you have an active relic divination, choose up to 1 foretold relic and remove it from the relic sequence.",
        "断序",
        "预见 8。启示：如果你有有效的遗物占卜，选择至多 1 件预示遗物并将其移出遗物序列。",
        ("selectPrompt", "Choose cards to discard.", "选择要丢弃的牌。"),
        ("upgradedDesc", "Scry 10. Revelation: if you have an active relic divination, choose up to 1 foretold relic and remove it from the relic sequence.", "预见 10。启示：如果你有有效的遗物占卜，选择至多 1 件预示遗物并将其移出遗物序列。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.Scry(this, choiceContext, IsUpgraded ? 10 : 8);
        if (!DivinerCombatRuntime.HasEnlightenmentEffect(Owner))
        {
            return;
        }

        DivinationService.RefreshActivity(Owner.RunState, Owner);
        var activeRelics = DivinationService.ActiveRelicDivinationIds;
        if (activeRelics.Count == 0)
        {
            return;
        }

        var chosen = await RelicDivinationChoiceOverlay.ChooseRelic(activeRelics);
        if (chosen is { } relicId)
        {
            DivinationService.TryDiscardForecastRelic(Owner, relicId);
        }
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
        "Revelation card effects can be triggered with 2 less Destiny.",
        "升华形态",
        "启示卡牌效果可以用低 2 点的命运触发。",
        ("upgradedDesc", "Revelation card effects can be triggered with 3 less Destiny.", "启示卡牌效果可以用低 3 点的命运触发。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AscendedFormPower>(choiceContext, Owner.Creature, IsUpgraded ? 3 : 2, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}
