using BaseLib.Patches.Localization;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Powers.CardPowers;

public abstract class DivinerCardPower : DivinerPower, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public virtual void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("Amount", Math.Max(1, Amount).ToString());
    }
}

public class PropheticTrancePower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Prophetic Trance",
        "Whenever you Divinate, draw {Cards} cards.",
        "Whenever you Divinate, draw {Cards} cards.",
        "占卜恍惚",
        "每当你占卜时，抽 {Cards} 张牌。",
        "每当你占卜时，抽 {Cards} 张牌。"
    );

    public override void AddDumbVariablesToPowerDescription(LocString description)
    {
        base.AddDumbVariablesToPowerDescription(description);
        description.Add("Cards", (Math.Max(1, Amount) * 2).ToString());
    }
}

public class SmallRitualPower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Small Ritual",
        "Whenever you Divinate, gain {Amount} Energy.",
        "Whenever you Divinate, gain {Amount} Energy.",
        "小仪式",
        "每当你占卜时，获得 {Amount} 点能量。",
        "每当你占卜时，获得 {Amount} 点能量。"
    );
}

public class TheWrittenHourPower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "The Written Hour",
        "At the start of your turn, if Destiny is exactly 3, gain {Amount} Energy and draw {Amount} card(s).",
        "At the start of your turn, if Destiny is exactly 3, gain {Amount} Energy and draw {Amount} card(s).",
        "写定时刻",
        "你的回合开始时，如果命运正好为 3，获得 {Amount} 点能量并抽 {Amount} 张牌。",
        "你的回合开始时，如果命运正好为 3，获得 {Amount} 点能量并抽 {Amount} 张牌。"
    );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner?.Player) ||
            !DestinyService.CanUseDestiny(player) ||
            DestinyService.GetDestiny(player) != DestinyConstants.DefaultDestiny)
        {
            return;
        }

        int amount = Math.Max(1, Amount);
        await PlayerCmd.GainEnergy(amount, player);
        await CardPileCmd.Draw(choiceContext, amount, player, false);
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
        "脏卜术",
        "下次你消耗一张牌时，占卜，并预言：将一张脏卜术加入手牌。",
        "下次你消耗一张牌时，占卜，并预言：将一张脏卜术加入手牌。"
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
    private readonly Dictionary<Player, int> _fatedDrawsAppliedThisTurn = [];

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Chosen Line",
        "The first {Amount} card(s) you draw each turn are Fated. Draw {Amount} extra card(s) per turn.",
        "The first {Amount} card(s) you draw each turn are Fated. Draw {Amount} extra card(s) per turn.",
        "所选命线",
        "每回合你抽到的前 {Amount} 张牌为注定。每回合多抽 {Amount} 张牌。",
        "每回合你抽到的前 {Amount} 张牌为注定。每回合多抽 {Amount} 张牌。"
    );

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player is { } player && side == Owner.Side)
        {
            _fatedDrawsAppliedThisTurn.Remove(player);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner?.Player is not { } player || card.Owner != player)
        {
            return Task.CompletedTask;
        }

        int applied = _fatedDrawsAppliedThisTurn.GetValueOrDefault(player);
        if (applied >= Math.Max(1, Amount))
        {
            return Task.CompletedTask;
        }

        _fatedDrawsAppliedThisTurn[player] = applied + 1;
        DivinerCardActions.MakeFated(card);
        return Task.CompletedTask;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return Owner?.Player == player ? count + Math.Max(1, Amount) : count;
    }
}

public class ResonationOfFatePower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Resonation of Fate",
        "Fated cards are played 1 additional time. At start of turn, make {Amount} random card(s) in your hand Fated.",
        "Fated cards are played 1 additional time. At start of turn, make {Amount} random card(s) in your hand Fated.",
        "命运共鸣",
        "注定牌额外打出 1 次。回合开始时，使你手牌中的 {Amount} 张随机牌变为注定。",
        "注定牌额外打出 1 次。回合开始时，使你手牌中的 {Amount} 张随机牌变为注定。"
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
            .ToList();
        player.RunState.Rng.CombatCardSelection.Shuffle(candidates);

        candidates = candidates
            .Take(Math.Max(1, Amount))
            .ToList();
        foreach (var card in candidates)
        {
            DivinerCardActions.MakeFated(card);
        }

        await Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (Owner?.Player is not { } player ||
            card.Owner != player ||
            !DivinerCombatRuntime.IsFatedThisTurn(card))
        {
            return playCount;
        }

        return playCount + 1;
    }
}

public class DoomEnginePower : DivinerCardPower
{
    private static readonly Dictionary<Player, int> EngineStacksByPlayer = [];

    static DoomEnginePower()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += EngineStacksByPlayer.Clear;
    }

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Doom Engine",
        "Misfortunes lose {HpLossReduction} less HP and deal {DamageBonus} more damage.",
        "Misfortunes lose {HpLossReduction} less HP and deal {DamageBonus} more damage.",
        "厄运引擎",
        "厄运少失去 {HpLossReduction} 点生命并额外造成 {DamageBonus} 点伤害。",
        "厄运少失去 {HpLossReduction} 点生命并额外造成 {DamageBonus} 点伤害。"
    );

    public override void AddDumbVariablesToPowerDescription(LocString description)
    {
        base.AddDumbVariablesToPowerDescription(description);
        int stacks = Owner?.Player is { } player ? GetEngineStacks(player) : 1;
        description.Add("HpLossReduction", (Math.Max(1, stacks) * 2).ToString());
        description.Add("DamageBonus", Math.Max(1, Amount).ToString());
    }

    public static void RecordEngine(Player player)
    {
        EngineStacksByPlayer[player] = GetEngineStacks(player) + 1;
    }

    public static int GetHpLoss(Player player, int baseLoss = 3)
    {
        var power = player.Creature.GetPower<DoomEnginePower>();
        return power == null ? baseLoss : Math.Max(0, baseLoss - 2 * GetEngineStacks(player));
    }

    public static int GetDamageBonus(Player player)
    {
        var power = player.Creature.GetPower<DoomEnginePower>();
        return power == null ? 0 : Math.Max(1, power.Amount);
    }

    private static int GetEngineStacks(Player player)
    {
        return EngineStacksByPlayer.GetValueOrDefault(player);
    }
}

public class LedgerOfSignsPower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Ledger of Signs",
        "Whenever you queue a Foretell, gain {Amount} Block.",
        "Whenever you queue a Foretell, gain {Amount} Block.",
        "征兆账簿",
        "每当你排入一个预言，获得 {Amount} 点格挡。",
        "每当你排入一个预言，获得 {Amount} 点格挡。"
    );

    public static int GetBlock(Player player)
    {
        var power = player.Creature.GetPower<LedgerOfSignsPower>();
        return power == null ? 0 : Math.Max(1, power.Amount);
    }
}

public class SmokeAndMirrorsPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Smoke and Mirrors",
        "The next Foretell effect you play this combat resolves with {Amount} more damage or Block.",
        "The next Foretell effect you play this combat resolves with {Amount} more damage or Block.",
        "烟幕幻镜",
        "你本场战斗中打出的下一个预言效果结算时，伤害或格挡增加 {Amount} 点。",
        "你本场战斗中打出的下一个预言效果结算时，伤害或格挡增加 {Amount} 点。"
    );
}

public class InsurancePower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Insurance",
        "You do not lose HP from Misfortune at the end of this turn.",
        "You do not lose HP from Misfortune at the end of this turn.",
        "预留后路",
        "本回合结束时，你不会因厄运失去生命。",
        "本回合结束时，你不会因厄运失去生命。"
    );

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner == null || side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        await PowerCmd.Remove<InsurancePower>(Owner);
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
            return 1m;
        }

        return 0.5m;
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
        "Whenever your Destiny changes, gain {Amount} Block.",
        "Whenever your Destiny changes, gain {Amount} Block.",
        "织成神盾",
        "每当你的命运变化，获得 {Amount} 点格挡。",
        "每当你的命运变化，获得 {Amount} 点格挡。"
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
        if (!DestinyService.CanUseDestiny(player))
        {
            return;
        }

        int current = DestinyService.GetDestiny(player);
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
        DestinyService.SetDestiny(Owner.Player, DestinyConstants.DredgeDestiny);
        DestinyService.PersistCurrentState(Owner.Player);
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
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Many Futures",
        "Card rewards have {RewardOptions} additional option(s). When you Scry, Scry {Amount} additional cards.",
        "Card rewards have {RewardOptions} additional option(s). When you Scry, Scry {Amount} additional cards.",
        "诸多未来",
        "卡牌奖励多 {RewardOptions} 个选项。每当你预见时，额外预见 {Amount} 张牌。",
        "卡牌奖励多 {RewardOptions} 个选项。每当你预见时，额外预见 {Amount} 张牌。"
    );

    public override void AddDumbVariablesToPowerDescription(LocString description)
    {
        base.AddDumbVariablesToPowerDescription(description);
        description.Add("RewardOptions", RewardOptionBonus(Math.Max(1, Amount)).ToString());
    }

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

            int rewardOptions = RewardOptionBonus(Math.Max(1, Amount));
            var extraOptions = CardRewardOptionHelper.CreateExtraOptionsFromCurrentReward(
                player,
                cardRewardOptions,
                creationOptions,
                rewardOptions);

            cardRewardOptions.AddRange(extraOptions);
            return extraOptions.Count > 0;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Many Futures failed to add a card reward option: {ex}");
            return false;
        }
    }

    private static int RewardOptionBonus(int amount)
    {
        return Math.Max(1, (int)Math.Ceiling(amount / 2m));
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
            !participants.Contains(Owner) ||
            !DestinyService.CanUseDestiny(player))
        {
            return;
        }

        DestinyService.AddDestiny(player, -1);
        DestinyService.PersistCurrentState(player);
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
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Echoed Omen",
        "Foretell effects trigger {Amount} additional time(s).",
        "Foretell effects trigger {Amount} additional time(s).",
        "回响征兆",
        "预言效果额外触发 {Amount} 次。",
        "预言效果额外触发 {Amount} 次。"
    );

    public static int GetTriggerCount(Player player)
    {
        return 1 + Math.Max(0, player.Creature.GetPower<EchoedOmenPower>()?.Amount ?? 0);
    }
}

public class AscendedFormPower : DivinerCardPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Ascended Form",
        "Good Omen and Revelation card effects can be triggered with {Amount} less Destiny. Revelation extra effects no longer reduce Destiny.",
        "Good Omen and Revelation card effects can be triggered with {Amount} less Destiny. Revelation extra effects no longer reduce Destiny.",
        "升华形态",
        "吉兆和启示卡牌效果可以用低 {Amount} 点的命运触发。启示额外效果不再降低命运。",
        "吉兆和启示卡牌效果可以用低 {Amount} 点的命运触发。启示额外效果不再降低命运。"
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
