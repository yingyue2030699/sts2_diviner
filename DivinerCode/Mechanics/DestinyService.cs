using BaseLib.Utils;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace Diviner.DivinerCode.Mechanics;

public static class DestinyService
{
    private static readonly SavedSpireField<RunState, int> SavedDestiny = new(
        () => DestinyConstants.DefaultDestiny,
        "DivinerDestiny"
    );

    private static readonly SavedSpireField<RunState, int> SavedLuckDestiny = new(
        () => DestinyConstants.DefaultDestiny,
        "DivinerLuckDestiny"
    );

    private static readonly SavedSpireField<Player, int> SavedPlayerDestiny = new(
        () => DestinyConstants.DefaultDestiny,
        "DivinerPlayerDestiny"
    );

    private static readonly SavedSpireField<Player, int> SavedPlayerLuckDestiny = new(
        () => DestinyConstants.DefaultDestiny,
        "DivinerPlayerLuckDestiny"
    );

    private static readonly Dictionary<Player, PlayerDestinyState> PlayerStates = [];

    private static int _destiny = DestinyConstants.DefaultDestiny;
    private static int _luckDestiny = DestinyConstants.DefaultDestiny;
    private static IRunState? _activeRunState;

    public static event Action<DestinySnapshot>? DestinyChanged;

    public static bool IsActive { get; private set; } = true;

    public static bool IsLoaded { get; private set; }

    public static int CurrentDestiny => ResolveCurrentState().Destiny;

    public static int CurrentLuckDestiny => ResolveCurrentState().LuckDestiny;

    public static DestinyOmen CurrentOmen => DestinyConstants.GetOmen(CurrentDestiny);

    public static DestinySnapshot Snapshot => new(
        CurrentDestiny,
        CurrentOmen,
        IsActive,
        IsLoaded);

    public static bool HasTrackedDivinerPlayer =>
        DivinerCombatRuntime.GetLastObservedPlayer() is { } player &&
        DivinerPlayerDetection.IsDivinerPlayer(player);

    public static bool CanUseDestiny(Player? player)
    {
        return player != null && DivinerPlayerDetection.IsDivinerPlayer(player);
    }

    public static void EnsureLoadedForPlayer(Player? player)
    {
        if (!CanUseDestiny(player))
        {
            EnsureLoadedForRun(player?.RunState);
            return;
        }

        _activeRunState = player!.RunState;
        IsLoaded = true;
        IsActive = true;
        _ = GetOrLoadPlayerState(player);
        DivinationService.LoadForPlayer(player);
        NotifyChanged(player);
    }

    public static void EnsureLoadedForRun(IRunState? runState)
    {
        if (IsLoaded && ReferenceEquals(_activeRunState, runState))
        {
            return;
        }

        _activeRunState = runState;
        IsLoaded = true;
        IsActive = true;
        _destiny = TryGetSavedRun(runState) is { } savedRun
            ? DestinyConstants.Clamp(SavedDestiny.Get(savedRun))
            : DestinyConstants.DefaultDestiny;
        _luckDestiny = TryGetSavedRun(runState) is { } savedRunForLuck
            ? DestinyConstants.Clamp(SavedLuckDestiny.Get(savedRunForLuck))
            : DestinyConstants.DefaultDestiny;
        DivinationService.LoadForRun(runState);
        NotifyChanged();
    }

    public static void ApplyLoadedState(IRunState? runState, int destiny)
    {
        _activeRunState = runState;
        IsLoaded = true;
        IsActive = true;
        _destiny = DestinyConstants.Clamp(destiny);
        _luckDestiny = TryGetSavedRun(runState) is { } savedRun
            ? DestinyConstants.Clamp(SavedLuckDestiny.Get(savedRun))
            : _destiny;
        PersistCurrentState(runState);
        NotifyChanged();
    }

    public static void RecordCombatEndLuck(IRunState? runState = null)
    {
        _activeRunState = runState ?? _activeRunState;
        _luckDestiny = DestinyConstants.Clamp(_destiny);
        if (TryGetSavedRun(_activeRunState) is { } savedRun)
        {
            SavedLuckDestiny.Set(savedRun, _luckDestiny);
        }

        NotifyChanged();
    }

    public static void RecordCombatEndLuck(Player? player)
    {
        if (!CanUseDestiny(player))
        {
            RecordCombatEndLuck(player?.RunState);
            return;
        }

        var state = GetOrLoadPlayerState(player!);
        state.LuckDestiny = DestinyConstants.Clamp(state.Destiny);
        SavedPlayerLuckDestiny.Set(player!, state.LuckDestiny);
        _activeRunState = player!.RunState;
        SyncLegacyState(state);
        NotifyChanged(player);
    }

    public static void ResetForNewRun(IRunState? runState)
    {
        ApplyLoadedState(runState, DestinyConstants.DefaultDestiny);
    }

    public static int SetDestiny(int destiny)
    {
        var trackedPlayer = DivinerCombatRuntime.GetLastObservedPlayer();
        if (CanUseDestiny(trackedPlayer))
        {
            return SetDestiny(trackedPlayer, destiny);
        }

        if (!HasTrackedDivinerPlayer)
        {
            return _destiny;
        }

        int clamped = DestinyConstants.Clamp(destiny);
        if (FixedPointPower.IsActive())
        {
            clamped = _destiny;
        }

        if (_destiny == clamped)
        {
            return _destiny;
        }

        int previous = _destiny;
        _destiny = clamped;
        var player = DivinerCombatRuntime.GetLastObservedPlayer();
        if (_destiny > previous)
        {
            DivinerEffectCue.DestinyIncrease(player?.Creature);
        }
        else if (_destiny < previous)
        {
            DivinerEffectCue.DestinyDecrease(player?.Creature);
        }

        DivinerCombatRuntime.HandleDestinyChanged(previous, _destiny);
        NotifyChanged();
        return _destiny;
    }

    public static int SetDestiny(Player? player, int destiny)
    {
        if (!CanUseDestiny(player))
        {
            return SetDestiny(destiny);
        }

        var state = GetOrLoadPlayerState(player!);
        int clamped = DestinyConstants.Clamp(destiny);
        if (player!.Creature.GetPower<FixedPointPower>() != null)
        {
            clamped = state.Destiny;
        }

        if (state.Destiny == clamped)
        {
            return state.Destiny;
        }

        int previous = state.Destiny;
        state.Destiny = clamped;
        SyncLegacyState(state);

        if (state.Destiny > previous)
        {
            DivinerEffectCue.DestinyIncrease(player.Creature);
        }
        else if (state.Destiny < previous)
        {
            DivinerEffectCue.DestinyDecrease(player.Creature);
        }

        DivinerCombatRuntime.HandleDestinyChanged(player, previous, state.Destiny);
        NotifyChanged(player);
        return state.Destiny;
    }

    public static int AddDestiny(int delta)
    {
        var trackedPlayer = DivinerCombatRuntime.GetLastObservedPlayer();
        return CanUseDestiny(trackedPlayer)
            ? AddDestiny(trackedPlayer, delta)
            : SetDestiny(_destiny + delta);
    }

    public static int AddDestiny(Player? player, int delta)
    {
        if (CanUseDestiny(player) &&
            delta < 0 &&
            GetDestiny(player) == DestinyConstants.MinDestiny &&
            player!.Creature.GetPower<FixedPointPower>() == null &&
            DivinerCombatRuntime.TryLoseDredgeCountdown(player))
        {
            return DestinyConstants.MinDestiny;
        }

        return CanUseDestiny(player)
            ? SetDestiny(player, GetDestiny(player) + delta)
            : AddDestiny(delta);
    }

    public static int GetDestiny(Player? player)
    {
        return CanUseDestiny(player) ? GetOrLoadPlayerState(player!).Destiny : _destiny;
    }

    public static int GetLuckDestiny(Player? player)
    {
        return CanUseDestiny(player) ? GetOrLoadPlayerState(player!).LuckDestiny : _luckDestiny;
    }

    public static DestinySnapshot SnapshotFor(Player? player)
    {
        int destiny = GetDestiny(player);
        return new DestinySnapshot(
            destiny,
            DestinyConstants.GetOmen(destiny),
            IsActive,
            IsLoaded);
    }

    public static bool IsGoodOmen()
    {
        var player = DivinerCombatRuntime.GetLastObservedPlayer();
        return IsGoodOmen(player);
    }

    public static bool IsGoodOmen(Player? player)
    {
        if (!CanUseDestiny(player))
        {
            return false;
        }

        int destiny = GetDestiny(player);
        int thresholdReduction = AscendedFormPower.GetThresholdReduction(player!);
        int threshold = Math.Max(DestinyConstants.MinDestiny, DestinyConstants.GoodOmenMinDestiny - thresholdReduction);
        return destiny >= threshold ||
               DivinerCombatRuntime.IsNextCardForcedFullOmen(player) ||
               player!.Creature.GetPower<DualityPower>() != null;
    }

    public static bool IsBadOmen()
    {
        var player = DivinerCombatRuntime.GetLastObservedPlayer();
        return IsBadOmen(player);
    }

    public static bool IsBadOmen(Player? player)
    {
        if (!CanUseDestiny(player))
        {
            return false;
        }

        return DestinyConstants.IsBadOmen(GetDestiny(player)) ||
               DivinerCombatRuntime.IsNextCardForcedFullOmen(player) ||
               player!.Creature.GetPower<DualityPower>() != null;
    }

    public static void PersistCurrentState(IRunState? runState = null)
    {
        _activeRunState = runState ?? _activeRunState;
        if (TryGetSavedRun(_activeRunState) is { } savedRun)
        {
            SavedDestiny.Set(savedRun, _destiny);
            SavedLuckDestiny.Set(savedRun, _luckDestiny);
        }
    }

    public static void PersistCurrentState(Player? player)
    {
        if (!CanUseDestiny(player))
        {
            PersistCurrentState(player?.RunState);
            return;
        }

        var state = GetOrLoadPlayerState(player!);
        SavedPlayerDestiny.Set(player!, state.Destiny);
        SavedPlayerLuckDestiny.Set(player!, state.LuckDestiny);
        _activeRunState = player!.RunState;
        SyncLegacyState(state);
    }

    public static void ClearRunState()
    {
        _activeRunState = null;
        IsLoaded = false;
        IsActive = true;
        _destiny = DestinyConstants.DefaultDestiny;
        _luckDestiny = DestinyConstants.DefaultDestiny;
        PlayerStates.Clear();
        DivinationService.ClearRuntimeState();
        NotifyChanged();
    }

    public static void NotifyChanged(Player? player = null)
    {
        DestinyChanged?.Invoke(player == null ? Snapshot : SnapshotFor(player));
    }

    private static RunState? TryGetSavedRun(IRunState? runState)
    {
        return runState as RunState;
    }

    private static PlayerDestinyState ResolveCurrentState()
    {
        var player = DivinerCombatRuntime.GetLastObservedPlayer();
        return CanUseDestiny(player) ? GetOrLoadPlayerState(player!) : new PlayerDestinyState(_destiny, _luckDestiny);
    }

    private static PlayerDestinyState GetOrLoadPlayerState(Player player)
    {
        if (PlayerStates.TryGetValue(player, out var state))
        {
            return state;
        }

        int destiny = DestinyConstants.Clamp(SavedPlayerDestiny.Get(player));
        int luckDestiny = DestinyConstants.Clamp(SavedPlayerLuckDestiny.Get(player));

        if (destiny == DestinyConstants.DefaultDestiny &&
            luckDestiny == DestinyConstants.DefaultDestiny &&
            TryGetSavedRun(player.RunState) is { } savedRun)
        {
            destiny = DestinyConstants.Clamp(SavedDestiny.Get(savedRun));
            luckDestiny = DestinyConstants.Clamp(SavedLuckDestiny.Get(savedRun));
        }

        state = new PlayerDestinyState(destiny, luckDestiny);
        PlayerStates[player] = state;
        SyncLegacyState(state);
        return state;
    }

    private static void SyncLegacyState(PlayerDestinyState state)
    {
        _destiny = state.Destiny;
        _luckDestiny = state.LuckDestiny;
    }

    private sealed class PlayerDestinyState(int destiny, int luckDestiny)
    {
        public int Destiny { get; set; } = DestinyConstants.Clamp(destiny);

        public int LuckDestiny { get; set; } = DestinyConstants.Clamp(luckDestiny);
    }
}
