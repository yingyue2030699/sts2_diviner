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
    private static readonly HashSet<CardModel> FreeThisTurnCards = [];
    private static readonly HashSet<CardModel> RetainThisTurnCards = [];
    private static readonly Dictionary<string, int> PendingForetellEffects = [];
    private static readonly List<string> PendingForetellDetails = [];
    private static readonly Dictionary<Player, int> PendingLedgerBlockByPlayer = [];
    private static readonly HashSet<Player> ForcedFullOmenForNextCard = [];
    private static readonly HashSet<Player> ForcedRevelationEffectsThisCombat = [];
    private static WeakReference<Player>? _lastObservedPlayer;

    public static event Action? CombatOnlyStateReset;

    public static event Func<PlayerChoiceContext, Player, Task<int>>? ImmediateForetellRequested;

    public static CombatState? CombatState { get; private set; }

    public static bool StartOfCombatDestinyEffectsApplied { get; private set; }

    public static bool DoomedTriggeredThisCombat { get; private set; }

    public static int? DredgeCountdown { get; private set; }

    private static readonly Dictionary<CardModel, int> EscapeCostIncreases = [];

    public static int EscapeCardsPlayedThisCombat { get; private set; }

    public static int NextForetellDamageOrBlockBonus { get; private set; }

    public static int NextDivinationEnergyBonus { get; private set; }

    public static IReadOnlyCollection<CardModel> EnlightenmentFreeThisTurnCards => FreeThisTurnCards;

    public static bool HasActiveDredgeCountdown => DredgeCountdown > 0;

    public static int CombatDivinationCount { get; private set; }

    public static int QueuedForetellCount => PendingForetellEffects.Values.Sum();

    public static string ForetellSummary => PendingForetellEffects.Count == 0
        ? "none"
        : string.Join(", ", PendingForetellEffects.Select(entry => $"{entry.Key} x{entry.Value}"));

    public static string ForetellDetailSummary => PendingForetellEffects.Count == 0
        ? DivinerLoc.Text("None.", "无。")
        : PendingForetellDetails.Count > 0
            ? string.Join("\n", PendingForetellDetails)
            : string.Join("\n", PendingForetellEffects.Select(entry => FormatForetellDetail(entry.Key, entry.Value)));

    public static void TrackCombatState(CombatState combatState)
    {
        if (ReferenceEquals(CombatState, combatState))
        {
            return;
        }

        ResetCombatOnlyState();
        CombatState = combatState;
        DestinyService.EnsureLoadedForRun(combatState.RunState);
        var player = combatState.Players.FirstOrDefault(DivinerPlayerDetection.IsDivinerPlayer);
        if (player == null)
        {
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
        DestinyService.EnsureLoadedForRun(player.RunState);
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
        CombatDivinationCount += 1;
        DestinyService.NotifyChanged();
    }

    public static bool HasDivinatedThisCombat => CombatDivinationCount > 0;

    public static async Task<bool> TryBeginStartOfCombatDestinyEffects(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel source
    )
    {
        if (CombatState is null || StartOfCombatDestinyEffectsApplied)
        {
            return false;
        }

        StartOfCombatDestinyEffectsApplied = true;

        int destiny = DestinyService.CurrentDestiny;
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

    public static async Task<bool> TryTriggerDoomed(Player player)
    {
        if (DoomedTriggeredThisCombat)
        {
            return false;
        }

        DoomedTriggeredThisCombat = true;
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

        SetDredgeCountdown(DivinerRelicHooks.DredgeStartingCountdown(player));
        EscapeCardsPlayedThisCombat = 0;
        EscapeCostIncreases.Clear();

        var escapeCards = Enumerable
            .Range(0, DestinyConstants.DredgeEscapeCardCount)
            .Select(_ => CombatState.CreateCard(ModelDb.Card<EscapeFromDestiny>(), player))
            .ToList();

        await StartOfCombatDestinyEffect.PlayDoomedEscapeShuffle(escapeCards.Count);

        await CardPileCmd.AddGeneratedCardsToCombat(
            escapeCards,
            PileType.Draw,
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
        FreeThisTurnCards.Clear();

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
        DestinyService.AddDestiny(-1);
        DestinyService.PersistCurrentState(player.RunState);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
        MainFile.Logger.Info("Diviner Enlightenment start-of-combat applied.");
    }

    public static void IncreaseDredgeCountdown(int amount)
    {
        if (DredgeCountdown is null)
        {
            DredgeCountdown = 0;
        }

        SetDredgeCountdown(DredgeCountdown.Value + amount);
    }

    public static Task IncreaseDredgeCountdown(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        IncreaseDredgeCountdown(amount);
        return Task.CompletedTask;
    }

    public static void IncreaseEscapeCost(CardModel card)
    {
        EscapeCostIncreases[card] = EscapeCostIncreases.GetValueOrDefault(card) + 1;
        EscapeCardsPlayedThisCombat += 1;
        DestinyService.NotifyChanged();
    }

    public static int EscapeCostIncreaseFor(CardModel card)
    {
        return EscapeCostIncreases.GetValueOrDefault(card);
    }

    public static void QueueForetell(string label, int count = 1, string? detail = null)
    {
        if (count <= 0)
        {
            return;
        }

        PendingForetellEffects[label] = PendingForetellEffects.GetValueOrDefault(label) + count;
        if (!string.IsNullOrWhiteSpace(detail))
        {
            for (int i = 0; i < count; i++)
            {
                PendingForetellDetails.Add(detail);
            }
        }

        DestinyService.NotifyChanged();
    }

    public static void QueueForetell(Player player, string label, int count = 1, string? detail = null)
    {
        QueueForetell(label, count, detail);
        if (count <= 0)
        {
            return;
        }

        int ledgerBlock = LedgerOfSignsPower.GetBlock(player);
        if (ledgerBlock > 0)
        {
            PendingLedgerBlockByPlayer[player] = PendingLedgerBlockByPlayer.GetValueOrDefault(player) + ledgerBlock * count;
        }
    }

    public static int ConsumePendingLedgerBlock(Player player)
    {
        int block = PendingLedgerBlockByPlayer.GetValueOrDefault(player);
        PendingLedgerBlockByPlayer.Remove(player);
        return block;
    }

    public static bool HasEnlightenmentEffect(Player player)
    {
        if (!DestinyService.CanUseDestiny(player))
        {
            return false;
        }

        if (ForcedRevelationEffectsThisCombat.Contains(player))
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
        return DestinyService.CurrentDestiny >= threshold;
    }

    public static bool CanTriggerRevelationEffect(Player? player)
    {
        return player != null && HasEnlightenmentEffect(player);
    }

    public static IReadOnlyList<Creature> HittableEnemiesFor(Player player)
    {
        return CombatState?.HittableEnemies
            .Where(creature => creature.Side != player.Creature.Side)
            .ToList() ?? [];
    }

    public static async Task<bool> TryConsumeRevelationEffect(PlayerChoiceContext choiceContext, Player player)
    {
        if (!HasEnlightenmentEffect(player))
        {
            return false;
        }

        DivinerEffectCue.Revelation(player.Creature);
        if (!IsNextCardForcedFullOmen(player) && !AscendedFormPower.PreventsRevelationDestinyLoss(player))
        {
            DestinyService.AddDestiny(-1);
            DestinyService.PersistCurrentState(player.RunState);
            await DivinerStatusPowerSync.Sync(player, choiceContext);
        }

        return true;
    }

    public static void ResolveForetell(string label, int count = 1)
    {
        if (count <= 0 || !PendingForetellEffects.TryGetValue(label, out var current))
        {
            return;
        }

        int updated = current - count;
        if (updated <= 0)
        {
            PendingForetellEffects.Remove(label);
        }
        else
        {
            PendingForetellEffects[label] = updated;
        }

        RemoveForetellDetails(count);
        DestinyService.NotifyChanged();
    }

    public static int ResolveForetell(Player player, string label, int count = 1)
    {
        ResolveForetell(label, count);
        return EchoedOmenPower.GetTriggerCount(player);
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
        if (!HasActiveDredgeCountdown ||
            side != playerCreature.Side ||
            !participants.Contains(playerCreature))
        {
            return;
        }

        SetDredgeCountdown(DredgeCountdown!.Value - 1);

        if (DredgeCountdown > 0)
        {
            return;
        }

        MainFile.Logger.Info("Countdown of Destiny reached 0. Defeating Diviner.");
        await CreatureCmd.Kill(playerCreature, true);
    }

    public static void PlayCriticalDoomedWarningIfNeeded()
    {
        if (DredgeCountdown != 1 || CriticalDoomedWarningPlayed)
        {
            return;
        }

        CriticalDoomedWarningPlayed = true;
        DivinerEffectCue.DoomedCountdownBell();
    }

    public static void MarkCardFreeThisTurn(CardModel card)
    {
        FreeThisTurnCards.Add(card);
    }

    public static bool IsCardFreeThisTurn(CardModel card)
    {
        return FreeThisTurnCards.Contains(card);
    }

    public static bool IsFatedThisTurn(CardModel card)
    {
        return FreeThisTurnCards.Contains(card);
    }

    public static void ClearFreeThisTurnCards()
    {
        FreeThisTurnCards.Clear();
    }

    public static void MarkCardRetainThisTurn(CardModel card)
    {
        RetainThisTurnCards.Add(card);
    }

    public static bool IsCardRetainedThisTurn(CardModel card)
    {
        return RetainThisTurnCards.Contains(card);
    }

    public static void ClearTemporaryRetainCards()
    {
        RetainThisTurnCards.Clear();
    }

    public static void SetNextForetellDamageOrBlockBonus(int amount)
    {
        NextForetellDamageOrBlockBonus = Math.Max(NextForetellDamageOrBlockBonus, amount);
    }

    public static int ConsumeNextForetellDamageOrBlockBonus()
    {
        return ConsumeNextForetellDamageOrBlockBonus(GetLastObservedPlayer());
    }

    public static int ConsumeNextForetellDamageOrBlockBonus(Player? player)
    {
        int bonus = NextForetellDamageOrBlockBonus;
        NextForetellDamageOrBlockBonus = 0;
        return bonus + DivinerRelicHooks.ForetellDamageOrBlockBonus(player);
    }

    public static void ForceNextCardFullOmen(Player player)
    {
        ForcedFullOmenForNextCard.Add(player);
        TrackPlayer(player);
        DestinyService.NotifyChanged();
    }

    public static void ForceRevelationEffectsThisCombat(Player player)
    {
        ForcedRevelationEffectsThisCombat.Add(player);
        TrackPlayer(player);
        DestinyService.NotifyChanged();
    }

    public static bool IsNextCardForcedFullOmen(Player? player)
    {
        return player != null && ForcedFullOmenForNextCard.Contains(player);
    }

    public static void ConsumeForcedFullOmen(Player? player)
    {
        if (player == null)
        {
            return;
        }

        if (ForcedFullOmenForNextCard.Remove(player))
        {
            DestinyService.NotifyChanged();
        }
    }

    public static void AddNextDivinationEnergyBonus(int amount)
    {
        NextDivinationEnergyBonus += amount;
    }

    public static int ConsumeNextDivinationEnergyBonus()
    {
        int bonus = NextDivinationEnergyBonus;
        NextDivinationEnergyBonus = 0;
        return bonus;
    }

    private static void ResetCombatOnlyState()
    {
        StartOfCombatDestinyEffectsApplied = false;
        DoomedTriggeredThisCombat = false;
        DredgeCountdown = null;
        EscapeCardsPlayedThisCombat = 0;
        EscapeCostIncreases.Clear();
        CriticalDoomedWarningPlayed = false;
        FreeThisTurnCards.Clear();
        RetainThisTurnCards.Clear();
        NextForetellDamageOrBlockBonus = 0;
        NextDivinationEnergyBonus = 0;
        CombatDivinationCount = 0;
        PendingForetellEffects.Clear();
        PendingForetellDetails.Clear();
        PendingLedgerBlockByPlayer.Clear();
        ForcedFullOmenForNextCard.Clear();
        ForcedRevelationEffectsThisCombat.Clear();
        DoomedCountdownOverlay.CloseAndDispose();
        CombatOnlyStateReset?.Invoke();
    }

    private static bool CriticalDoomedWarningPlayed { get; set; }

    private static void SetDredgeCountdown(int amount)
    {
        int? previous = DredgeCountdown;
        DredgeCountdown = Math.Max(0, amount);
        if (DredgeCountdown != 1)
        {
            CriticalDoomedWarningPlayed = false;
        }
        else if (previous != 1)
        {
            CriticalDoomedWarningPlayed = false;
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
                "灾厄征兆：对所有敌人造成伤害，给予虚弱和易伤。"),
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
                "灾厄手稿：给予所有敌人易伤。"),
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

    private static void RemoveForetellDetails(int count)
    {
        int toRemove = Math.Min(count, PendingForetellDetails.Count);
        if (toRemove <= 0)
        {
            return;
        }

        PendingForetellDetails.RemoveRange(0, toRemove);
    }
}
