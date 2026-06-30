using BaseLib.Utils;
using Diviner.DivinerCode.Powers.CardPowers;
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

    private static int _destiny = DestinyConstants.DefaultDestiny;
    private static int _luckDestiny = DestinyConstants.DefaultDestiny;
    private static IRunState? _activeRunState;

    public static event Action<DestinySnapshot>? DestinyChanged;

    public static bool IsActive { get; private set; } = true;

    public static bool IsLoaded { get; private set; }

    public static int CurrentDestiny => _destiny;

    public static int CurrentLuckDestiny => _luckDestiny;

    public static DestinyOmen CurrentOmen => DestinyConstants.GetOmen(_destiny);

    public static DestinySnapshot Snapshot => new(
        _destiny,
        CurrentOmen,
        IsActive,
        IsLoaded);

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

    public static void ResetForNewRun(IRunState? runState)
    {
        ApplyLoadedState(runState, DestinyConstants.DefaultDestiny);
    }

    public static int SetDestiny(int destiny)
    {
        int clamped = DestinyConstants.Clamp(destiny);
        if (FixedPointPower.IsActive() && clamped < DestinyConstants.DefaultDestiny)
        {
            clamped = DestinyConstants.DefaultDestiny;
        }

        if (_destiny == clamped)
        {
            return _destiny;
        }

        _destiny = clamped;
        NotifyChanged();
        return _destiny;
    }

    public static int AddDestiny(int delta)
    {
        return SetDestiny(_destiny + delta);
    }

    public static bool IsGoodOmen()
    {
        return DestinyConstants.IsGoodOmen(_destiny);
    }

    public static bool IsBadOmen()
    {
        return DestinyConstants.IsBadOmen(_destiny);
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

    public static void ClearRunState()
    {
        _activeRunState = null;
        IsLoaded = false;
        IsActive = true;
        _destiny = DestinyConstants.DefaultDestiny;
        _luckDestiny = DestinyConstants.DefaultDestiny;
        DivinationService.Clear();
        NotifyChanged();
    }

    public static void NotifyChanged()
    {
        DestinyChanged?.Invoke(Snapshot);
    }

    private static RunState? TryGetSavedRun(IRunState? runState)
    {
        return runState as RunState;
    }
}
