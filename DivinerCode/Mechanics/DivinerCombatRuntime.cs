using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Powers.CardPowers;
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
    private static readonly Dictionary<Player, int> LedgerForetellCountsByPlayer = [];
    private static WeakReference<Player>? _lastObservedPlayer;

    public static CombatState? CombatState { get; private set; }

    public static bool StartOfCombatDestinyEffectsApplied { get; private set; }

    public static int? DredgeCountdown { get; private set; }

    public static int EscapeCostTax { get; private set; }

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
            await BeginDredgeStartOfCombat(player);
        }

        if (DestinyConstants.IsEnlightenmentDestiny(destiny))
        {
            await BeginEnlightenmentStartOfCombat(choiceContext, player, source);
        }

        DestinyService.NotifyChanged();
        return true;
    }

    public static async Task BeginDredgeStartOfCombat(Player player)
    {
        if (CombatState == null)
        {
            MainFile.Logger.Error("Diviner Dredge could not create Escape from Destiny cards because no combat state was tracked.");
            return;
        }

        DredgeCountdown = DestinyConstants.DredgeStartingCountdown;
        EscapeCostTax = 0;

        var escapeCards = Enumerable
            .Range(0, DestinyConstants.DredgeEscapeCardCount)
            .Select(_ => CombatState.CreateCard(ModelDb.Card<EscapeFromDestiny>(), player))
            .ToList();

        await CardPileCmd.AddGeneratedCardsToCombat(
            escapeCards,
            PileType.Draw,
            player,
            CardPilePosition.Bottom
        );
        MainFile.Logger.Info("Diviner Dredge start-of-combat applied.");
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

        foreach (var card in selectedCards)
        {
            card.EnergyCost.SetThisTurnOrUntilPlayed(0, false);
            MarkCardFreeThisTurn(card);
        }

        await CardPileCmd.Add(selectedCards, PileType.Hand, CardPilePosition.Bottom, source, false);
        MainFile.Logger.Info("Diviner Enlightenment start-of-combat applied.");
    }

    public static void IncreaseDredgeCountdown(int amount)
    {
        if (DredgeCountdown is null)
        {
            DredgeCountdown = 0;
        }

        DredgeCountdown = Math.Max(0, DredgeCountdown.Value + amount);
        DestinyService.NotifyChanged();
    }

    public static Task IncreaseDredgeCountdown(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        IncreaseDredgeCountdown(amount);
        return Task.CompletedTask;
    }

    public static void IncreaseEscapeCostTax()
    {
        EscapeCostTax += 1;
        DestinyService.NotifyChanged();
    }

    public static void QueueForetell(string label, int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        PendingForetellEffects[label] = PendingForetellEffects.GetValueOrDefault(label) + count;
        DestinyService.NotifyChanged();
    }

    public static void QueueForetell(Player player, string label, int count = 1)
    {
        QueueForetell(label, count);
        if (count <= 0)
        {
            return;
        }

        LedgerForetellCountsByPlayer[player] = LedgerForetellCountsByPlayer.GetValueOrDefault(player) + count;
    }

    public static int ConsumeLedgerFortunes(Player player)
    {
        int currentCount = LedgerForetellCountsByPlayer.GetValueOrDefault(player);
        int fortunes = currentCount / 3;
        int remaining = currentCount % 3;
        if (remaining == 0)
        {
            LedgerForetellCountsByPlayer.Remove(player);
        }
        else
        {
            LedgerForetellCountsByPlayer[player] = remaining;
        }

        return fortunes;
    }

    public static bool HasEnlightenmentEffect(Player player)
    {
        int thresholdReduction = AscendedFormPower.GetThresholdReduction(player);
        int threshold = Math.Max(DestinyConstants.MinDestiny, DestinyConstants.EnlightenmentDestiny - thresholdReduction);
        return DestinyService.CurrentDestiny >= threshold;
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

        DestinyService.NotifyChanged();
    }

    public static int ResolveForetell(Player player, string label, int count = 1)
    {
        ResolveForetell(label, count);
        return EchoedOmenPower.GetTriggerCount(player);
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

        DredgeCountdown = Math.Max(0, DredgeCountdown!.Value - 1);
        DestinyService.NotifyChanged();

        if (DredgeCountdown > 0)
        {
            return;
        }

        MainFile.Logger.Info("Countdown of Destiny reached 0. Defeating Diviner.");
        await CreatureCmd.Kill(playerCreature, true);
    }

    public static void MarkCardFreeThisTurn(CardModel card)
    {
        FreeThisTurnCards.Add(card);
    }

    public static bool IsCardFreeThisTurn(CardModel card)
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
        int bonus = NextForetellDamageOrBlockBonus;
        NextForetellDamageOrBlockBonus = 0;
        return bonus;
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
        DredgeCountdown = null;
        EscapeCostTax = 0;
        FreeThisTurnCards.Clear();
        RetainThisTurnCards.Clear();
        NextForetellDamageOrBlockBonus = 0;
        NextDivinationEnergyBonus = 0;
        CombatDivinationCount = 0;
        PendingForetellEffects.Clear();
        LedgerForetellCountsByPlayer.Clear();
    }

    private static string FormatForetellDetail(string label, int count)
    {
        var detail = label switch
        {
            "Woes" => DivinerLoc.Text(
                "Divination of Woes: damage, Weak, and Vulnerable to all enemies.",
                "灾厄占卜：对所有敌人造成伤害，给予虚弱和易伤。"),
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
                "Destiny's Fall: damage the same enemy.",
                "命运坠落：对同一敌人造成伤害。"),
            "Divinate" => DivinerLoc.Text(
                "Tea Leaves: Divinate.",
                "茶叶占形：占卜。"),
            "Block" => DivinerLoc.Text(
                "Mark Calendar: gain Block.",
                "标记日历：获得格挡。"),
            "Haruspex" => DivinerLoc.Text(
                "Haruspex Method: add Haruspex Method to your hand.",
                "观兆法：将观兆法加入你的手牌。"),
            "Vulnerable" => DivinerLoc.Text(
                "Doomscript: apply Vulnerable to all enemies.",
                "灾厄手稿：给予所有敌人易伤。"),
            "Wound" => DivinerLoc.Text(
                "Backdated Wound: deal damage.",
                "倒填伤口：造成伤害。"),
            "Fated draw" => DivinerLoc.Text(
                "Predestined Path: put chosen cards into your hand; they are Fated.",
                "既定路径：将选择的牌加入手牌；它们为注定。"),
            "Ashes draw" => DivinerLoc.Text(
                "Read the Ashes: draw a card.",
                "读灰：抽一张牌。"),
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
}
