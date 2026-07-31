using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Powers.CardPowers;
using Diviner.DivinerCode.Relics;
using Diviner.DivinerCode.UI;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Mechanics;

public static class DivinerCombatRuntime
{
    private static readonly Dictionary<Player, PlayerCombatRuntimeState> PlayerStates = [];
    private static readonly PlayerCombatRuntimeState FallbackState = new();
    private static WeakReference<Player>? _lastObservedPlayer;

    public static event Action? CombatOnlyStateReset;

    public static event Func<PlayerChoiceContext, Player, Task<int>>? ImmediateForetellRequested;

    public static CombatState? CombatState { get; private set; }

    public static bool StartOfCombatDestinyEffectsApplied => CurrentState.StartOfCombatDestinyEffectsApplied;

    public static bool DoomedTriggeredThisCombat => CurrentState.DoomedTriggeredThisCombat;

    public static int? DredgeCountdown => CurrentState.DredgeCountdown;

    public static int? DredgeCountdownFor(Player? player) => StateFor(player).DredgeCountdown;

    public static int EscapeCardsPlayedThisCombat => CurrentState.EscapeCardsPlayedThisCombat;

    public static int EscapeCardsPlayedThisCombatFor(Player? player)
    {
        return StateFor(player).EscapeCardsPlayedThisCombat;
    }

    public static int NextForetellDamageOrBlockBonus => CurrentState.NextForetellDamageOrBlockBonus;

    public static int NextForetellDamageOrBlockBonusFor(Player? player)
    {
        return StateFor(player).NextForetellDamageOrBlockBonus;
    }

    public static int NextDivinationEnergyBonus => CurrentState.NextDivinationEnergyBonus;

    public static IReadOnlyCollection<CardModel> EnlightenmentFreeThisTurnCards => CurrentState.FreeThisTurnCards;

    public static bool HasActiveDredgeCountdown => CurrentState.DredgeCountdown > 0;

    public static bool HasActiveDredgeCountdownFor(Player? player) => StateFor(player).DredgeCountdown > 0;

    public static int CombatDivinationCount => CurrentState.CombatDivinationCount;

    public static int CombatDivinationCountFor(Player? player)
    {
        return StateFor(player).CombatDivinationCount;
    }

    public static int QueuedForetellCount => QueuedForetellCountFor(GetLastObservedPlayer());

    public static string ForetellSummary => ForetellSummaryFor(GetLastObservedPlayer());

    public static string ForetellDetailSummary => ForetellDetailSummaryFor(GetLastObservedPlayer());

    public static int QueuedForetellCountFor(Player? player)
    {
        return StateFor(player).PendingForetellEffects.Values.Sum();
    }

    public static string ForetellSummaryFor(Player? player)
    {
        var state = StateFor(player);
        return state.PendingForetellEffects.Count == 0
            ? "none"
            : string.Join(", ", state.PendingForetellEffects.Select(entry => $"{entry.Key} x{entry.Value}"));
    }

    public static string ForetellDetailSummaryFor(Player? player)
    {
        var state = StateFor(player);
        return state.PendingForetellEffects.Count == 0
        ? DivinerLoc.Text("None.", "无。")
        : state.PendingForetellDetails.Count > 0
            ? string.Join("\n", state.PendingForetellDetails)
            : string.Join("\n", state.PendingForetellEffects.Select(entry => FormatForetellDetail(entry.Key, entry.Value)));
    }

    public static void TrackCombatState(CombatState combatState)
    {
        if (ReferenceEquals(CombatState, combatState))
        {
            return;
        }

        ResetCombatOnlyState();
        CombatState = combatState;
        var player = combatState.Players.FirstOrDefault(DivinerPlayerDetection.IsDivinerPlayer);
        if (player == null)
        {
            DestinyService.EnsureLoadedForRun(combatState.RunState);
            _lastObservedPlayer = null;
            DestinyService.NotifyChanged();
            return;
        }

        TrackPlayer(player);
    }

    public static void ClearCombatState()
    {
        ResetCombatOnlyState();
        CombatState = null;
        DestinyService.NotifyChanged();
    }

    public static void ClearRunState()
    {
        ResetCombatOnlyState();
        CombatState = null;
        _lastObservedPlayer = null;
        DestinyService.ClearRunState();
    }

    public static void TrackPlayer(Player? player)
    {
        if (player == null)
        {
            _lastObservedPlayer = null;
            DestinyService.NotifyChanged();
            return;
        }

        _lastObservedPlayer = new WeakReference<Player>(player);
        DestinyService.EnsureLoadedForPlayer(player);
    }

    public static Player? GetLastObservedPlayer()
    {
        if (_lastObservedPlayer != null && _lastObservedPlayer.TryGetTarget(out var player))
        {
            return player;
        }

        return null;
    }

    public static void RecordCombatDivination()
    {
        RecordCombatDivination(GetLastObservedPlayer());
    }

    public static void RecordCombatDivination(Player? player)
    {
        StateFor(player).CombatDivinationCount += 1;
        DestinyService.NotifyChanged();
    }

    public static bool HasDivinatedThisCombat => CurrentState.CombatDivinationCount > 0;

    public static bool HasDivinatedThisCombatFor(Player? player)
    {
        return StateFor(player).CombatDivinationCount > 0;
    }

    public static async Task<bool> TryBeginStartOfCombatDestinyEffects(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel source
    )
    {
        var state = StateFor(player);
        if (CombatState is null || state.StartOfCombatDestinyEffectsApplied)
        {
            return false;
        }

        state.StartOfCombatDestinyEffectsApplied = true;

        int destiny = DestinyService.GetDestiny(player);
        if (DestinyConstants.IsDredgeDestiny(destiny))
        {
            await TryTriggerDoomed(player);
        }

        if (HasEnlightenmentEffect(player))
        {
            await BeginEnlightenmentStartOfCombat(choiceContext, player, source);
        }

        DestinyService.NotifyChanged();
        return true;
    }

    public static void HandleDestinyChanged(int previous, int current)
    {
        if (previous == current ||
            previous == DestinyConstants.DredgeDestiny ||
            current != DestinyConstants.DredgeDestiny)
        {
            return;
        }

        var player = GetLastObservedPlayer();
        if (player == null)
        {
            return;
        }

        _ = TryTriggerDoomed(player);
    }

    public static void HandleDestinyChanged(Player player, int previous, int current)
    {
        if (previous == current ||
            previous == DestinyConstants.DredgeDestiny ||
            current != DestinyConstants.DredgeDestiny)
        {
            return;
        }

        _ = TryTriggerDoomed(player);
    }

    public static async Task<bool> TryTriggerDoomed(Player player)
    {
        var state = StateFor(player);
        if (state.DoomedTriggeredThisCombat)
        {
            return false;
        }

        state.DoomedTriggeredThisCombat = true;
        try
        {
            await BeginDoomedCountdown(player);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Doomed countdown trigger failed: {ex}");
        }

        return true;
    }

    private static async Task BeginDoomedCountdown(Player player)
    {
        if (CombatState == null)
        {
            MainFile.Logger.Error("Diviner Dredge could not create Escape from Destiny cards because no combat state was tracked.");
            return;
        }

        var state = StateFor(player);
        SetDredgeCountdown(player, DivinerRelicHooks.DredgeStartingCountdown(player));
        state.EscapeCardsPlayedThisCombat = 0;

        var drawEscapeCards = Enumerable
            .Range(0, DestinyConstants.DredgeEscapeCardsPerPile)
            .Select(_ => CombatState.CreateCard(ModelDb.Card<EscapeFromDestiny>(), player))
            .ToList();
        var discardEscapeCards = Enumerable
            .Range(0, DestinyConstants.DredgeEscapeCardsPerPile)
            .Select(_ => CombatState.CreateCard(ModelDb.Card<EscapeFromDestiny>(), player))
            .ToList();

        await StartOfCombatDestinyEffect.PlayDoomedEscapeShuffle(drawEscapeCards.Count);

        await CardPileCmd.AddGeneratedCardsToCombat(
            drawEscapeCards,
            PileType.Draw,
            player,
            CardPilePosition.Random
        );
        await CardPileCmd.AddGeneratedCardsToCombat(
            discardEscapeCards,
            PileType.Discard,
            player,
            CardPilePosition.Random
        );
        MainFile.Logger.Info("Diviner Doomed countdown applied.");
    }

    public static async Task BeginEnlightenmentStartOfCombat(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel source
    )
    {
        StateFor(player).FreeThisTurnCards.Clear();

        var drawPileCards = PileType.Draw.GetPile(player).Cards.ToList();
        if (drawPileCards.Count == 0)
        {
            return;
        }

        int maxCards = Math.Min(DestinyConstants.EnlightenmentCardCount, drawPileCards.Count);
        await StartOfCombatDestinyEffect.PlayRevelation(maxCards);

        CardSelectorPrefs prefs = new(
            new LocString("cards", "DIVINER-ENLIGHTENMENT.selectPrompt"),
            0,
            maxCards
        );
        List<CardModel> selectedCards;
        try
        {
            selectedCards = (await CardSelectCmd.FromSimpleGrid(choiceContext, drawPileCards, player, prefs))
                .Take(maxCards)
                .ToList();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Revelation card selection failed; falling back to the top of the draw pile: {ex}");
            selectedCards = [];
        }

        if (selectedCards.Count == 0)
        {
            selectedCards = drawPileCards.Take(maxCards).ToList();
        }

        await CardPileCmd.Add(selectedCards, PileType.Hand, CardPilePosition.Bottom, source, false);
        DestinyService.AddDestiny(player, -1);
        DestinyService.PersistCurrentState(player);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
        MainFile.Logger.Info("Diviner Enlightenment start-of-combat applied.");
    }

    public static void IncreaseDredgeCountdown(int amount)
    {
        IncreaseDredgeCountdown(GetLastObservedPlayer(), amount);
    }

    public static void IncreaseDredgeCountdown(Player? player, int amount)
    {
        var state = StateFor(player);
        if (state.DredgeCountdown is null)
        {
            state.DredgeCountdown = 0;
        }

        SetDredgeCountdown(player, state.DredgeCountdown.Value + amount);
    }

    public static Task IncreaseDredgeCountdown(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        IncreaseDredgeCountdown(player, amount);
        return Task.CompletedTask;
    }

    public static bool TryLoseDredgeCountdown(Player? player)
    {
        if (player == null || !HasActiveDredgeCountdownFor(player))
        {
            return false;
        }

        int nextCountdown = Math.Max(0, StateFor(player).DredgeCountdown!.Value - 1);
        SetDredgeCountdown(player, nextCountdown);
        if (nextCountdown == 0)
        {
            _ = DefeatFromDredgeCountdown(player);
        }

        return true;
    }

    public static void RecordEscapePlayed(Player? player)
    {
        var state = StateFor(player);
        state.EscapeCardsPlayedThisCombat += 1;
        DestinyService.NotifyChanged();
    }

    public static void QueueForetell(string label, int count = 1, string? detail = null)
    {
        QueueForetellFor(GetLastObservedPlayer(), label, count, detail);
    }

    private static void QueueForetellFor(Player? player, string label, int count = 1, string? detail = null)
    {
        if (count <= 0)
        {
            return;
        }

        var state = StateFor(player);
        state.PendingForetellEffects[label] = state.PendingForetellEffects.GetValueOrDefault(label) + count;
        if (!string.IsNullOrWhiteSpace(detail))
        {
            for (int i = 0; i < count; i++)
            {
                state.PendingForetellDetails.Add(detail);
            }
        }

        DestinyService.NotifyChanged();
    }

    public static void QueueForetell(Player player, string label, int count = 1, string? detail = null)
    {
        QueueForetellFor(player, label, count, detail);
        if (count <= 0)
        {
            return;
        }

        int ledgerBlock = LedgerOfSignsPower.GetBlock(player);
        if (ledgerBlock > 0)
        {
            var state = StateFor(player);
            state.PendingLedgerBlock += ledgerBlock * count;
        }
    }

    public static int ConsumePendingLedgerBlock(Player player)
    {
        var state = StateFor(player);
        int block = state.PendingLedgerBlock;
        state.PendingLedgerBlock = 0;
        return block;
    }

    public static bool HasEnlightenmentEffect(Player player)
    {
        if (!DestinyService.CanUseDestiny(player))
        {
            return false;
        }

        return DestinyConstants.IsEnlightenmentDestiny(DestinyService.GetDestiny(player));
    }

    public static bool CanTriggerRevelationEffect(Player? player)
    {
        if (player == null || !DestinyService.CanUseDestiny(player))
        {
            return false;
        }

        if (StateFor(player).ForcedRevelationEffectsThisCombat)
        {
            return true;
        }

        if (IsNextCardForcedFullOmen(player))
        {
            return true;
        }

        int thresholdReduction = AscendedFormPower.GetThresholdReduction(player) +
                                 DivinerRelicHooks.EnlightenmentThresholdReduction(player);
        int threshold = Math.Max(DestinyConstants.MinDestiny, DestinyConstants.EnlightenmentDestiny - thresholdReduction);
        return DestinyService.GetDestiny(player) >= threshold;
    }

    public static IReadOnlyList<Creature> HittableEnemiesFor(Player player)
    {
        return CombatState?.HittableEnemies
            .Where(creature => creature.Side != player.Creature.Side)
            .ToList() ?? [];
    }

    public static async Task<bool> TryConsumeRevelationEffect(PlayerChoiceContext choiceContext, Player player)
    {
        if (!CanTriggerRevelationEffect(player))
        {
            return false;
        }

        DivinerEffectCue.Revelation(player.Creature);
        if (!IsNextCardForcedFullOmen(player) &&
            !StateFor(player).ForcedRevelationEffectsThisCombat &&
            !AscendedFormPower.PreventsRevelationDestinyLoss(player))
        {
            DestinyService.AddDestiny(player, -1);
            DestinyService.PersistCurrentState(player);
            await DivinerStatusPowerSync.Sync(player, choiceContext);
        }

        return true;
    }

    public static void ResolveForetell(string label, int count = 1)
    {
        ResolveForetellFor(GetLastObservedPlayer(), label, count);
    }

    private static void ResolveForetellFor(Player? player, string label, int count = 1)
    {
        var state = StateFor(player);
        if (count <= 0 || !state.PendingForetellEffects.TryGetValue(label, out var current))
        {
            return;
        }

        int updated = current - count;
        if (updated <= 0)
        {
            state.PendingForetellEffects.Remove(label);
        }
        else
        {
            state.PendingForetellEffects[label] = updated;
        }

        RemoveForetellDetails(state, count);
        DestinyService.NotifyChanged();
    }

    public static int ResolveForetell(Player player, string label, int count = 1)
    {
        ResolveForetellFor(player, label, count);
        return EchoedOmenPower.GetTriggerCount(player);
    }

    public static int ResolveForetellWithoutEcho(Player player, string label, int count = 1)
    {
        ResolveForetellFor(player, label, count);
        return Math.Max(0, count);
    }

    public static async Task<int> TriggerAllForetellNow(PlayerChoiceContext choiceContext, Player player)
    {
        var handlers = ImmediateForetellRequested?.GetInvocationList();
        if (handlers == null || handlers.Length == 0)
        {
            return 0;
        }

        int resolvedTriggers = 0;
        foreach (var handler in handlers.Cast<Func<PlayerChoiceContext, Player, Task<int>>>())
        {
            resolvedTriggers += await handler(choiceContext, player);
        }

        await DivinerStatusPowerSync.Sync(player, choiceContext);
        return resolvedTriggers;
    }

    public static async Task TickDredgeCountdownAtTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants,
        Player player,
        Creature playerCreature
    )
    {
        var state = StateFor(player);
        int? countdown = state.DredgeCountdown;
        if (!countdown.HasValue ||
            countdown.Value <= 0 ||
            side != playerCreature.Side ||
            !participants.Contains(playerCreature))
        {
            return;
        }

        int nextCountdown = countdown.Value - 1;
        SetDredgeCountdown(player, nextCountdown);

        if (nextCountdown > 0)
        {
            return;
        }

        MainFile.Logger.Info("Countdown of Destiny reached 0. Defeating Diviner.");
        await CreatureCmd.Kill(playerCreature, true);
    }

    private static async Task DefeatFromDredgeCountdown(Player player)
    {
        try
        {
            MainFile.Logger.Info("Destiny loss reduced Countdown of Destiny to 0. Defeating Diviner.");
            if (player.Creature.IsAlive)
            {
                await CreatureCmd.Kill(player.Creature, true);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to defeat Diviner after Countdown of Destiny reached 0: {ex}");
        }
    }

    public static void PlayCriticalDoomedWarningIfNeeded()
    {
        PlayCriticalDoomedWarningIfNeeded(GetLastObservedPlayer());
    }

    public static void PlayCriticalDoomedWarningIfNeeded(Player? player)
    {
        var state = StateFor(player);
        if (state.DredgeCountdown != 1 || state.CriticalDoomedWarningPlayed)
        {
            return;
        }

        state.CriticalDoomedWarningPlayed = true;
        DivinerEffectCue.DoomedCountdownBell();
    }

    public static void MarkCardFreeThisTurn(CardModel card)
    {
        StateFor(card.Owner).FreeThisTurnCards.Add(card);
    }

    public static bool IsCardFreeThisTurn(CardModel card)
    {
        return StateFor(card.Owner).FreeThisTurnCards.Contains(card);
    }

    public static bool IsFatedThisTurn(CardModel card)
    {
        return StateFor(card.Owner).FreeThisTurnCards.Contains(card);
    }

    public static void ClearFreeThisTurnCards()
    {
        CurrentState.FreeThisTurnCards.Clear();
    }

    public static void MarkCardRetainThisTurn(CardModel card)
    {
        StateFor(card.Owner).RetainThisTurnCards.Add(card);
    }

    public static bool IsCardRetainedThisTurn(CardModel card)
    {
        return StateFor(card.Owner).RetainThisTurnCards.Contains(card);
    }

    public static void ClearTemporaryRetainCards()
    {
        CurrentState.RetainThisTurnCards.Clear();
    }

    public static void ClearTemporaryRetainCards(Player? player)
    {
        StateFor(player).RetainThisTurnCards.Clear();
    }

    public static void SetNextForetellDamageOrBlockBonus(int amount)
    {
        var state = CurrentState;
        state.NextForetellDamageOrBlockBonus = Math.Max(state.NextForetellDamageOrBlockBonus, amount);
    }

    public static void SetNextForetellDamageOrBlockBonus(Player? player, int amount)
    {
        var state = StateFor(player);
        state.NextForetellDamageOrBlockBonus = Math.Max(state.NextForetellDamageOrBlockBonus, amount);
    }

    public static int ConsumeNextForetellDamageOrBlockBonus()
    {
        return ConsumeNextForetellDamageOrBlockBonus(GetLastObservedPlayer());
    }

    public static int ConsumeNextForetellDamageOrBlockBonus(Player? player)
    {
        var state = StateFor(player);
        int bonus = state.NextForetellDamageOrBlockBonus;
        state.NextForetellDamageOrBlockBonus = 0;
        return bonus + DivinerRelicHooks.ForetellDamageOrBlockBonus(player);
    }

    public static void ForceNextCardFullOmen(Player player)
    {
        StateFor(player).ForcedFullOmenForNextCard = true;
        TrackPlayer(player);
        DestinyService.NotifyChanged();
    }

    public static void ForceRevelationEffectsThisCombat(Player player)
    {
        StateFor(player).ForcedRevelationEffectsThisCombat = true;
        TrackPlayer(player);
        DestinyService.NotifyChanged();
    }

    public static bool IsNextCardForcedFullOmen(Player? player)
    {
        return player != null && StateFor(player).ForcedFullOmenForNextCard;
    }

    public static void ConsumeForcedFullOmen(Player? player)
    {
        if (player == null)
        {
            return;
        }

        var state = StateFor(player);
        if (state.ForcedFullOmenForNextCard)
        {
            state.ForcedFullOmenForNextCard = false;
            DestinyService.NotifyChanged();
        }
    }

    public static void AddNextDivinationEnergyBonus(int amount)
    {
        CurrentState.NextDivinationEnergyBonus += amount;
    }

    public static void AddNextDivinationEnergyBonus(Player? player, int amount)
    {
        StateFor(player).NextDivinationEnergyBonus += amount;
    }

    public static int ConsumeNextDivinationEnergyBonus()
    {
        return ConsumeNextDivinationEnergyBonus(GetLastObservedPlayer());
    }

    public static int ConsumeNextDivinationEnergyBonus(Player? player)
    {
        var state = StateFor(player);
        int bonus = state.NextDivinationEnergyBonus;
        state.NextDivinationEnergyBonus = 0;
        return bonus;
    }

    private static void ResetCombatOnlyState()
    {
        FallbackState.Reset();
        PlayerStates.Clear();
        DoomedCountdownOverlay.CloseAndDispose();
        CombatOnlyStateReset?.Invoke();
    }

    private static void SetDredgeCountdown(int amount)
    {
        SetDredgeCountdown(GetLastObservedPlayer(), amount);
    }

    private static void SetDredgeCountdown(Player? player, int amount)
    {
        var state = StateFor(player);
        int? previous = state.DredgeCountdown;
        state.DredgeCountdown = Math.Max(0, amount);
        if (state.DredgeCountdown != 1)
        {
            state.CriticalDoomedWarningPlayed = false;
        }
        else if (previous != 1)
        {
            state.CriticalDoomedWarningPlayed = false;
        }

        DoomedCountdownOverlay.EnsureMounted();
        DoomedCountdownOverlay.RefreshIfMounted();
        DestinyService.NotifyChanged();
    }

    private static string FormatForetellDetail(string label, int count)
    {
        var detail = label switch
        {
            "Woes" => DivinerLoc.Text(
                "Omen of Woes: damage, Weak, and Vulnerable to all enemies.",
                "灾祸征兆：对所有敌人造成伤害，给予虚弱和易伤。"),
            "Draw" => DivinerLoc.Text(
                "Thread Pull: draw cards.",
                "牵引命线：抽牌。"),
            "Fallen Sky" => DivinerLoc.Text(
                "Fallen Sky: play the card again.",
                "坠天：再次打出该牌。"),
            "End" => DivinerLoc.Text(
                "Unavoidable End: damage all enemies.",
                "无可避免的结局：对所有敌人造成伤害。"),
            "Falling damage" => DivinerLoc.Text(
                "Foretold Strike: damage the same enemy.",
                "预兆打击：对同一敌人造成伤害。"),
            "Divinate" => DivinerLoc.Text(
                "Omen of Insight: Divinate.",
                "洞见征兆：占卜。"),
            "Block" => DivinerLoc.Text(
                "Omen of Shelter: gain Block.",
                "庇护征兆：获得格挡。"),
            "Energy" => DivinerLoc.Text(
                "Omen of Vigor: gain Energy.",
                "活力征兆：获得能量。"),
            "Haruspex" => DivinerLoc.Text(
                "Haruspex: add Haruspex to your hand.",
                "脏卜术：将脏卜术加入你的手牌。"),
            "Vulnerable" => DivinerLoc.Text(
                "Doomscript: apply Vulnerable to all enemies.",
                "灾祸手稿：给予所有敌人易伤。"),
            "Pestilence" => DivinerLoc.Text(
                "Omen of Pestilence: apply Weak and Poison to all enemies.",
                "疫病征兆：给予所有敌人虚弱和中毒。"),
            "Perishment" => DivinerLoc.Text(
                "Omen of Perishment: damage, Weak, and Vulnerable to all enemies.",
                "殒灭征兆：对所有敌人造成伤害，给予虚弱和易伤。"),
            "Fated draw" => DivinerLoc.Text(
                "Predestined Path: put chosen cards into your hand; they are Fated.",
                "既定路径：将选择的牌加入手牌；它们为注定。"),
            "Ashes draw" => DivinerLoc.Text(
                "Read the Ashes: draw cards.",
                "读灰：抽牌。"),
            "Ashes energy" => DivinerLoc.Text(
                "Read the Ashes: gain Energy.",
                "读灰：获得能量。"),
            "Lose Energy" => DivinerLoc.Text(
                "Borrowed Tomorrow: lose Energy.",
                "借来的明日：失去能量。"),
            _ => label
        };

        return count > 1
            ? DivinerLoc.Text($"{detail} x{count}", $"{detail} x{count}")
            : detail;
    }

    private static void RemoveForetellDetails(PlayerCombatRuntimeState state, int count)
    {
        int toRemove = Math.Min(count, state.PendingForetellDetails.Count);
        if (toRemove <= 0)
        {
            return;
        }

        state.PendingForetellDetails.RemoveRange(0, toRemove);
    }

    private static PlayerCombatRuntimeState CurrentState => StateFor(GetLastObservedPlayer());

    private static PlayerCombatRuntimeState StateFor(Player? player)
    {
        if (player == null)
        {
            return FallbackState;
        }

        if (PlayerStates.TryGetValue(player, out var state))
        {
            return state;
        }

        state = new PlayerCombatRuntimeState();
        PlayerStates[player] = state;
        return state;
    }

    private sealed class PlayerCombatRuntimeState
    {
        public readonly HashSet<CardModel> FreeThisTurnCards = [];
        public readonly HashSet<CardModel> RetainThisTurnCards = [];
        public readonly Dictionary<string, int> PendingForetellEffects = [];
        public readonly List<string> PendingForetellDetails = [];
        public bool StartOfCombatDestinyEffectsApplied;
        public bool DoomedTriggeredThisCombat;
        public int? DredgeCountdown;
        public int EscapeCardsPlayedThisCombat;
        public int NextForetellDamageOrBlockBonus;
        public int NextDivinationEnergyBonus;
        public int CombatDivinationCount;
        public int PendingLedgerBlock;
        public bool ForcedFullOmenForNextCard;
        public bool ForcedRevelationEffectsThisCombat;
        public bool CriticalDoomedWarningPlayed;

        public void Reset()
        {
            StartOfCombatDestinyEffectsApplied = false;
            DoomedTriggeredThisCombat = false;
            DredgeCountdown = null;
            EscapeCardsPlayedThisCombat = 0;
            NextForetellDamageOrBlockBonus = 0;
            NextDivinationEnergyBonus = 0;
            CombatDivinationCount = 0;
            PendingLedgerBlock = 0;
            ForcedFullOmenForNextCard = false;
            ForcedRevelationEffectsThisCombat = false;
            CriticalDoomedWarningPlayed = false;
            FreeThisTurnCards.Clear();
            RetainThisTurnCards.Clear();
            PendingForetellEffects.Clear();
            PendingForetellDetails.Clear();
        }
    }
}
