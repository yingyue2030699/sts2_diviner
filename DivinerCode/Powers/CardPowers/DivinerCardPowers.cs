using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Cards.Uncommon;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Powers.CardPowers;

public abstract class DivinerCardPower : DivinerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}

public class PropheticTrancePower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Prophetic Trance",
        "Whenever you Divinate, draw 2 cards.",
        "Whenever you Divinate, draw 2 cards.",
        "预言恍惚",
        "每当你占卜时，抽 2 张牌。",
        "每当你占卜时，抽 2 张牌。"
    );
}

public class SmallRitualPower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Small Ritual",
        "Whenever you Divinate, gain Energy.",
        "Whenever you Divinate, gain Energy.",
        "小仪式",
        "每当你占卜时，获得能量。",
        "每当你占卜时，获得能量。"
    );
}

public class TheWrittenHourPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "The Written Hour",
        "At the start of your turn, if Destiny is exactly 3, gain 1 Energy and draw 1 card.",
        "At the start of your turn, if Destiny is exactly 3, gain 1 Energy and draw 1 card.",
        "写定时刻",
        "你的回合开始时，如果命运正好为 3，获得 1 点能量并抽 1 张牌。",
        "你的回合开始时，如果命运正好为 3，获得 1 点能量并抽 1 张牌。"
    );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner?.Player) ||
            DestinyService.CurrentDestiny != DestinyConstants.DefaultDestiny)
        {
            return;
        }

        await PlayerCmd.GainEnergy(1, player);
        await CardPileCmd.Draw(choiceContext, 1, player, false);
    }
}

public class HaruspexMethodPower : DivinerCardPower
{
    private const string ForetellLabel = "Haruspex";
    private static readonly Dictionary<Player, List<bool>> PendingCopiesByPlayer = [];

    static HaruspexMethodPower()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += PendingCopiesByPlayer.Clear;
        DivinerCombatRuntime.ImmediateForetellRequested += ResolveImmediateForetell;
    }

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Haruspex",
        "The next time you Exhaust a card, Divinate and Foretell: add Haruspex to your hand.",
        "The next time you Exhaust a card, Divinate and Foretell: add Haruspex to your hand.",
        "肝占",
        "下次你消耗一张牌时，占卜，并预言：将一张肝占加入手牌。",
        "下次你消耗一张牌时，占卜，并预言：将一张肝占加入手牌。"
    );

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Owner?.Player is not { } player || card.Owner != player || Amount <= 0)
        {
            return;
        }

        bool addUpgraded = Amount > 1;
        await DivinationService.RecordPlaceholder(choiceContext, player, "Haruspex");
        await PowerCmd.Remove(this);

        var pending = PendingCopiesByPlayer.GetValueOrDefault(player) ?? [];
        PendingCopiesByPlayer[player] = pending;
        pending.Add(addUpgraded);
        DivinerCombatRuntime.QueueForetell(player, ForetellLabel);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is not { } player ||
            side != Owner.Side ||
            !participants.Contains(Owner) ||
            !PendingCopiesByPlayer.Remove(player, out var pendingCopies))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingCopies.Count);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (bool upgraded in pendingCopies)
            {
                await DivinerCardActions.AddGeneratedToCombat<HaruspexMethod>(
                    player,
                    PileType.Hand,
                    CardPilePosition.Bottom,
                    upgraded);
            }
        }
    }

    private static async Task<int> ResolveImmediateForetell(PlayerChoiceContext choiceContext, Player player)
    {
        if (!PendingCopiesByPlayer.Remove(player, out var pendingCopies))
        {
            return 0;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingCopies.Count);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (bool upgraded in pendingCopies)
            {
                await DivinerCardActions.AddGeneratedToCombat<HaruspexMethod>(
                    player,
                    PileType.Hand,
                    CardPilePosition.Bottom,
                    upgraded);
            }
        }

        return triggerCount * pendingCopies.Count;
    }
}

public class ChosenLinePower : DivinerCardPower
{
    private readonly HashSet<Player> _fatedDrawAppliedThisTurn = [];

    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Chosen Line",
        "The first card you draw each turn is Fated. Draw 1 extra card per turn.",
        "The first card you draw each turn is Fated. Draw 1 extra card per turn.",
        "所选命线",
        "每回合你抽到的第一张牌为注定。每回合多抽 1 张牌。",
        "每回合你抽到的第一张牌为注定。每回合多抽 1 张牌。"
    );

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is { } player && side == Owner.Side)
        {
            _fatedDrawAppliedThisTurn.Remove(player);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner?.Player is not { } player || card.Owner != player || !_fatedDrawAppliedThisTurn.Add(player))
        {
            return Task.CompletedTask;
        }

        DivinerCardActions.MakeFated(card);
        return Task.CompletedTask;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return Owner?.Player == player ? count + 1 : count;
    }
}

public class ResonationOfFatePower : DivinerCardPower
{
    private readonly Dictionary<CardModel, int> _originalReplayCounts = [];

    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Resonation of Fate",
        "Fated cards are played an additional time. At start of turn, make random cards in your hand Fated.",
        "Fated cards are played an additional time. At start of turn, make random cards in your hand Fated.",
        "命运共鸣",
        "注定牌额外打出一次。回合开始时，使你手牌中的随机牌变为注定。",
        "注定牌额外打出一次。回合开始时，使你手牌中的随机牌变为注定。"
    );

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is not { } player || side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        var candidates = PileType.Hand.GetPile(player).Cards
            .Where(card => !DivinerCombatRuntime.IsFatedThisTurn(card))
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Max(1, Amount))
            .ToList();
        foreach (var card in candidates)
        {
            DivinerCardActions.MakeFated(card);
        }

        await Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner?.Player is not { } player ||
            cardPlay.Card.Owner != player ||
            !DivinerCombatRuntime.IsFatedThisTurn(cardPlay.Card) ||
            _originalReplayCounts.ContainsKey(cardPlay.Card))
        {
            return Task.CompletedTask;
        }

        _originalReplayCounts[cardPlay.Card] = cardPlay.Card.BaseReplayCount;
        cardPlay.Card.BaseReplayCount += 1;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_originalReplayCounts.Remove(cardPlay.Card, out int originalReplayCount))
        {
            cardPlay.Card.BaseReplayCount = originalReplayCount;
        }

        return Task.CompletedTask;
    }
}

public class DoomEnginePower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Doom Engine",
        "Misfortunes lose 2 less HP and deal 5 more damage. At start of turn, return a Misfortune from your discard pile to your hand.",
        "Misfortunes lose 2 less HP and deal 5 more damage. At start of turn, return a Misfortune from your discard pile to your hand.",
        "厄运引擎",
        "厄运少失去 2 点生命并额外造成 5 点伤害。回合开始时，将弃牌堆中的一张厄运返回手牌。",
        "厄运少失去 2 点生命并额外造成 5 点伤害。回合开始时，将弃牌堆中的一张厄运返回手牌。"
    );

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is not { } player ||
            side != Owner.Side ||
            !participants.Contains(Owner))
        {
            return;
        }

        var misfortune = PileType.Discard.GetPile(player).Cards.FirstOrDefault(card => card is Misfortune);
        if (misfortune != null)
        {
            await CardPileCmd.Add(misfortune, PileType.Hand, CardPilePosition.Bottom, this, false);
        }
    }

    public static int GetHpLoss(Player player, int baseLoss = 3)
    {
        var power = player.Creature.GetPower<DoomEnginePower>();
        return power == null ? baseLoss : Math.Max(0, baseLoss - 2);
    }

    public static int GetDamageBonus(Player player)
    {
        var power = player.Creature.GetPower<DoomEnginePower>();
        return power == null ? 0 : 5;
    }
}

public class LedgerOfSignsPower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Ledger of Signs",
        "Every 3 times you Foretell, add a Fortune to your hand.",
        "Every 3 times you Foretell, add a Fortune to your hand.",
        "征兆账簿",
        "每当你预言 3 次，将一张福运加入你的手牌。",
        "每当你预言 3 次，将一张福运加入你的手牌。"
    );

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is not { } player || side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        int fortunes = DivinerCombatRuntime.ConsumeLedgerFortunes(player);
        for (int i = 0; i < fortunes; i++)
        {
            await DivinerCardActions.AddGeneratedToCombat<Fortune>(
                player,
                PileType.Hand,
                CardPilePosition.Bottom,
                Amount > 1);
        }
    }
}

public class ForetoldFalterPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Foretold Falter",
        "Enemies with more than 10 Weak deal half damage to you.",
        "Enemies with more than 10 Weak deal half damage to you.",
        "预示踉跄",
        "拥有超过 10 层虚弱的敌人对你造成的伤害减半。",
        "拥有超过 10 层虚弱的敌人对你造成的伤害减半。"
    );

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Player == null ||
            target != Owner ||
            dealer == null ||
            !HasEnoughWeak(dealer))
        {
            return amount;
        }

        return amount / 2m;
    }

    private static bool HasEnoughWeak(Creature creature)
    {
        return creature.Powers.Any(power => power is WeakPower { Amount: > 10 });
    }
}

public class WeaveTheAegisPower : DivinerCardPower
{
    private readonly Dictionary<Player, int> _lastDestinyByPlayer = [];

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Weave the Aegis",
        "Whenever your Destiny changes, gain Block.",
        "Whenever your Destiny changes, gain Block.",
        "织成神盾",
        "每当你的命运变化，获得格挡。",
        "每当你的命运变化，获得格挡。"
    );

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player is { } player && cardPlay.Card.Owner == player)
        {
            await CheckDestinyChange(choiceContext, player);
        }
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is { } player && side == Owner.Side && participants.Contains(Owner))
        {
            await CheckDestinyChange(choiceContext, player);
        }
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner?.Player is { } player && side == Owner.Side && participants.Contains(Owner))
        {
            await CheckDestinyChange(choiceContext, player);
        }
    }

    private async Task CheckDestinyChange(PlayerChoiceContext choiceContext, Player player)
    {
        int current = DestinyService.CurrentDestiny;
        if (!_lastDestinyByPlayer.TryGetValue(player, out int previous))
        {
            _lastDestinyByPlayer[player] = current;
            return;
        }

        if (previous == current)
        {
            return;
        }

        _lastDestinyByPlayer[player] = current;
        await CreatureCmd.GainBlock(player.Creature, Math.Max(1, Amount), BlockProps.cardUnpowered, null, false);
    }
}

public class FixedPointPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Fixed Point",
        "Destiny cannot change this combat.",
        "Destiny cannot change this combat.",
        "定点",
        "本场战斗中命运无法改变。",
        "本场战斗中命运无法改变。"
    );

    public static bool IsActive()
    {
        return DivinerCombatRuntime.GetLastObservedPlayer()?.Creature.GetPower<FixedPointPower>() != null;
    }

}

public class DualityPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Duality",
        "Good Omen and Bad Omen extra effects always trigger.",
        "Good Omen and Bad Omen extra effects always trigger.",
        "二相",
        "吉兆与凶兆的额外效果总是触发。",
        "吉兆与凶兆的额外效果总是触发。"
    );

    public static bool IsActive()
    {
        return DivinerCombatRuntime.GetLastObservedPlayer()?.Creature.GetPower<DualityPower>() != null;
    }
}

public class CheatTheEndingPower : DivinerCardPower
{
    private bool _triggered;

    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Cheat the Ending",
        "The next time you would die this combat, heal to 13% of max HP and set Destiny to 0.",
        "The next time you would die this combat, heal to 13% of max HP and set Destiny to 0.",
        "欺瞒终局",
        "本场战斗中下次你将要死亡时，恢复至最大生命值的 13% 并将命运设为 0。",
        "本场战斗中下次你将要死亡时，恢复至最大生命值的 13% 并将命运设为 0。"
    );

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_triggered || Owner == null || target != Owner || amount < Owner.CurrentHp)
        {
            return amount;
        }

        _triggered = true;
        DestinyService.SetDestiny(DestinyConstants.DredgeDestiny);
        DestinyService.PersistCurrentState(Owner.Player?.RunState);
        int targetHp = Math.Max(1, (int)Math.Ceiling(Owner.MaxHp * 0.13m));
        return Math.Max(0, Owner.CurrentHp - targetHp);
    }

    public override async Task AfterDamageReceivedLate(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_triggered && target == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public class ManyFuturesPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Many Futures",
        "Card rewards have 1 additional option. When you Scry, Scry 2 additional cards.",
        "Card rewards have 1 additional option. When you Scry, Scry 2 additional cards.",
        "诸多未来",
        "卡牌奖励多 1 个选项。每当你预见时，额外预见 2 张牌。",
        "卡牌奖励多 1 个选项。每当你预见时，额外预见 2 张牌。"
    );

    public static int ExtraScryCards(Player player)
    {
        return Math.Max(0, player.Creature.GetPower<ManyFuturesPower>()?.Amount ?? 0);
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        try
        {
            if (Owner?.Player != player)
            {
                return false;
            }

            var existingIds = cardRewardOptions
                .Select(option => option.Card.Id)
                .ToHashSet();
            var extraCard = creationOptions
                .GetPossibleCards(player)
                .FirstOrDefault(card => !existingIds.Contains(card.Id));
            if (extraCard == null)
            {
                return false;
            }

            cardRewardOptions.Add(new CardCreationResult(extraCard));
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Many Futures failed to add a card reward option: {ex}");
            return false;
        }
    }
}

public class DoomSpiralPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Doom Spiral",
        "At start of turn, lose 1 Destiny and add a Misfortune to your hand.",
        "At start of turn, lose 1 Destiny and add a Misfortune to your hand.",
        "厄运螺旋",
        "回合开始时，失去 1 点命运并将一张厄运加入你的手牌。",
        "回合开始时，失去 1 点命运并将一张厄运加入你的手牌。"
    );

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is not { } player ||
            side != Owner.Side ||
            !participants.Contains(Owner))
        {
            return;
        }

        DestinyService.AddDestiny(-1);
        DestinyService.PersistCurrentState(player.RunState);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
            player,
            PileType.Hand,
            CardPilePosition.Bottom,
            Amount > 1);
    }
}

public class EchoedOmenPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Echoed Omen",
        "Foretell effects trigger an additional time.",
        "Foretell effects trigger an additional time.",
        "回响征兆",
        "预言效果额外触发一次。",
        "预言效果额外触发一次。"
    );

    public static int GetTriggerCount(Player player)
    {
        return 1 + Math.Max(0, player.Creature.GetPower<EchoedOmenPower>()?.Amount ?? 0);
    }
}

public class AscendedFormPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Ascended Form",
        "Revelation card effects can be triggered with less Destiny.",
        "Revelation card effects can be triggered with less Destiny.",
        "升华形态",
        "启示卡牌效果可以用更低的命运触发。",
        "启示卡牌效果可以用更低的命运触发。"
    );

    public static int GetThresholdReduction(Player player)
    {
        return player.Creature.GetPower<AscendedFormPower>()?.Amount ?? 0;
    }

    public static bool PreventsRevelationDestinyLoss(Player player)
    {
        return player.Creature.GetPower<AscendedFormPower>() != null;
    }
}
