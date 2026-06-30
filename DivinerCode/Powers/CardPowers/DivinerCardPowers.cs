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

public class PatternRecognitionPower : DivinerCardPower
{
    private readonly Dictionary<Creature, int> _cardsPlayedThisTurn = [];

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Pattern Recognition",
        "Whenever you play the third card in a turn, Good Omen: gain Block. Bad Omen: deal damage to all enemies.",
        "Whenever you play the third card in a turn, Good Omen: gain Block. Bad Omen: deal damage to all enemies.",
        "模式识别",
        "每回合每当你打出第三张牌时，吉兆：获得格挡。凶兆：对所有敌人造成伤害。",
        "每回合每当你打出第三张牌时，吉兆：获得格挡。凶兆：对所有敌人造成伤害。"
    );

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner != null && side == Owner.Side)
        {
            _cardsPlayedThisTurn[Owner] = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null || cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        int count = _cardsPlayedThisTurn.GetValueOrDefault(Owner) + 1;
        _cardsPlayedThisTurn[Owner] = count;
        if (count % 3 != 0)
        {
            return;
        }

        int amount = Math.Max(1, Amount);
        if (DestinyService.IsGoodOmen())
        {
            await CreatureCmd.GainBlock(Owner, amount, BlockProps.cardUnpowered, null, false);
            return;
        }

        var enemies = DivinerCombatRuntime.CombatState?.HittableEnemies
            .Where(creature => creature.Side != Owner.Side)
            .ToList() ?? [];
        if (enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, amount, DamageProps.nonCardUnpowered, Owner, null);
        }
    }
}

public class HaruspexMethodPower : DivinerCardPower
{
    private const string ForetellLabel = "Haruspex";
    private readonly Dictionary<Player, List<bool>> _pendingCopiesByPlayer = [];

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Haruspex Method",
        "The next time you Exhaust a card, Divinate and Foretell: add Haruspex Method to your hand.",
        "The next time you Exhaust a card, Divinate and Foretell: add Haruspex Method to your hand.",
        "肝占法",
        "下次你消耗一张牌时，占卜，并预言：将一张肝占法加入手牌。",
        "下次你消耗一张牌时，占卜，并预言：将一张肝占法加入手牌。"
    );

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Owner?.Player is not { } player || card.Owner != player || Amount <= 0)
        {
            return;
        }

        await DivinationService.RecordPlaceholder(choiceContext, player, "Haruspex Method");
        await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null!, true);

        var pending = _pendingCopiesByPlayer.GetValueOrDefault(player) ?? [];
        _pendingCopiesByPlayer[player] = pending;
        pending.Add(Amount > 1);
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
            !_pendingCopiesByPlayer.Remove(player, out var pendingCopies))
        {
            return;
        }

        int triggerCount = DivinerCombatRuntime.ResolveForetell(player, ForetellLabel, pendingCopies.Count);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
        for (int trigger = 0; trigger < triggerCount; trigger++)
        {
            foreach (bool upgraded in pendingCopies)
            {
                await DivinerCardActions.AddGeneratedToCombat<HaruspexMethod>(player, PileType.Hand, CardPilePosition.Bottom);
            }
        }
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

public class DoomEnginePower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Doom Engine",
        "Misfortunes lose less HP and deal more damage.",
        "Misfortunes lose less HP and deal more damage.",
        "厄运引擎",
        "厄运失去更少生命并造成更多伤害。",
        "厄运失去更少生命并造成更多伤害。"
    );

    public static int GetHpLoss(Player player, int baseLoss = 3)
    {
        var power = player.Creature.GetPower<DoomEnginePower>();
        return Math.Max(0, baseLoss - (power?.Amount ?? 0));
    }

    public static int GetDamageBonus(Player player)
    {
        var power = player.Creature.GetPower<DoomEnginePower>();
        return power?.Amount switch
        {
            null or <= 0 => 0,
            >= 2 => 13,
            _ => 9
        };
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
            await DivinerCardActions.AddGeneratedToCombat<Fortune>(player, PileType.Hand, CardPilePosition.Bottom);
        }
    }
}

public class FixedPointPower : DivinerCardPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Fixed Point",
        "Destiny cannot decrease below 3 this combat. At the start of your turn, lose 1 HP.",
        "Destiny cannot decrease below 3 this combat. At the start of your turn, lose 1 HP.",
        "定点",
        "本场战斗中命运不会降至 3 以下。你的回合开始时，失去 1 点生命。",
        "本场战斗中命运不会降至 3 以下。你的回合开始时，失去 1 点生命。"
    );

    public static bool IsActive()
    {
        return DivinerCombatRuntime.GetLastObservedPlayer()?.Creature.GetPower<FixedPointPower>() != null;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner?.Player == null || side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Owner, 1, DamageProps.nonCardHpLoss, Owner, null);
    }
}

public class CheatTheEndingPower : DivinerCardPower
{
    private bool _triggered;

    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Cheat the Ending",
        "The next time you would die this combat, stay at 13 HP and set Destiny to 0.",
        "The next time you would die this combat, stay at 13 HP and set Destiny to 0.",
        "欺瞒终局",
        "本场战斗中下次你将要死亡时，保留 13 点生命并将命运设为 0。",
        "本场战斗中下次你将要死亡时，保留 13 点生命并将命运设为 0。"
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
        return Math.Max(0, Owner.CurrentHp - 13);
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
        "Card rewards have 1 additional option. When you Scry, choose from 1 additional card.",
        "Card rewards have 1 additional option. When you Scry, choose from 1 additional card.",
        "诸多未来",
        "卡牌奖励多 1 个选项。每当你预见时，多查看 1 张牌。",
        "卡牌奖励多 1 个选项。每当你预见时，多查看 1 张牌。"
    );

    public static int ExtraScryCards(Player player)
    {
        return player.Creature.GetPower<ManyFuturesPower>() == null ? 0 : 1;
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
        "At end of turn, if Bad Omen, add a Misfortune to your hand.",
        "At end of turn, if Bad Omen, add a Misfortune to your hand.",
        "厄运螺旋",
        "回合结束时，如果处于凶兆，将一张厄运加入你的手牌。",
        "回合结束时，如果处于凶兆，将一张厄运加入你的手牌。"
    );

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner?.Player is not { } player ||
            side != Owner.Side ||
            !participants.Contains(Owner) ||
            !DestinyService.IsBadOmen())
        {
            return;
        }

        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(player, PileType.Hand, CardPilePosition.Bottom);
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
}
