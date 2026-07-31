using System.Text.Json;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Powers.CardPowers;
using Diviner.DivinerCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace Diviner.DivinerCode.Mechanics;

public static class DivinationService
{
    private static readonly SavedSpireField<RunState, string> SavedRecords = new(
        () => "[]",
        "DivinerDivinationRecords"
    );

    private static readonly SavedSpireField<Player, string> SavedPlayerRecords = new(
        () => "",
        "DivinerPlayerDivinationRecords"
    );

    private static readonly List<DivinationRecord> Records = [];
    private static readonly Dictionary<Player, List<DivinationRecord>> RecordsByPlayer = [];
    private static IRunState? _activeRunState;
    private static Player? _activePlayer;

    public static event Action? RecordsChanged;

    public static IReadOnlyList<DivinationRecord> CurrentRecords => GetRecords(_activePlayer);

    public static IReadOnlyList<DivinationRecord> GetVisibleRecords(bool hideInactive)
    {
        return GetVisibleRecords(_activePlayer, hideInactive);
    }

    public static IReadOnlyList<DivinationRecord> GetVisibleRecords(Player? player, bool hideInactive)
    {
        var records = GetRecords(player);
        return hideInactive
            ? records.Where(record => record.IsActive).ToList()
            : records;
    }

    public static IReadOnlyList<DivinationRecord> GetRecords(Player? player)
    {
        return player != null
            ? GetOrLoadPlayerRecords(player)
            : Records;
    }

    public static IReadOnlyList<ModelId> ActiveRelicDivinationIds => GetActiveRelicDivinationIds(_activePlayer);

    public static IReadOnlyList<ModelId> GetActiveRelicDivinationIds(Player? player)
    {
        return GetRecords(player)
            .Where(record => record.IsActive && GetCategoryGroup(record.Category) == "Relic")
            .SelectMany(record => record.GetPreviewRelicIds())
            .Distinct()
            .ToList();
    }

    public static DivinationRecord RecordPlaceholder(IRunState? runState, string source)
    {
        return RecordPlaceholderInternal(runState, null, source);
    }

    public static async Task<DivinationRecord> RecordPlaceholder(Player player, string source)
    {
        return await RecordPlaceholder(null, player, source);
    }

    public static async Task<DivinationRecord> RecordPlaceholder(
        PlayerChoiceContext? choiceContext,
        Player player,
        string source)
    {
        DivinerCombatRuntime.TrackPlayer(player);
        var record = RecordPlaceholderInternal(player.RunState, player, source);
        await ApplyPostDivinationEffects(choiceContext, player);
        return record;
    }

    public static async Task<DivinationRecord> RecordRelicDivination(
        PlayerChoiceContext? choiceContext,
        Player player,
        string source)
    {
        DivinerCombatRuntime.TrackPlayer(player);
        var record = RecordRelicDivinationInternal(player.RunState, player, source);
        await ApplyPostDivinationEffects(choiceContext, player);
        return record;
    }

    private static async Task ApplyPostDivinationEffects(PlayerChoiceContext? choiceContext, Player player)
    {
        DivinerCombatRuntime.RecordCombatDivination(player);
        DivinerEffectCue.Divinate(player.Creature);
        await DivinerRelicHooks.AfterDivination(choiceContext, player, DivinerCombatRuntime.CombatDivinationCountFor(player));

        int energyBonus = DivinerCombatRuntime.ConsumeNextDivinationEnergyBonus(player) +
                          Math.Max(0, player.Creature.GetPower<SmallRitualPower>()?.Amount ?? 0);
        if (energyBonus > 0)
        {
            await PlayerCmd.GainEnergy(energyBonus, player);
        }

        if (choiceContext != null && player.Creature.GetPower<PropheticTrancePower>() is { } propheticTrance)
        {
            await CardPileCmd.Draw(choiceContext, 2 * Math.Max(1, propheticTrance.Amount), player, false);
        }

        await DivinerStatusPowerSync.Sync(player, choiceContext);
    }

    public static void RefreshActivity(IRunState? runState, Player? player)
    {
        _activeRunState = runState ?? _activeRunState;
        _activePlayer = player ?? _activePlayer;
        var records = GetMutableRecords(player);
        if (_activeRunState == null || records.Count == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            bool isActive = IsRecordStillActive(record, _activeRunState, player);
            if (record.IsActive != isActive)
            {
                records[i] = record with { IsActive = isActive };
                changed = true;
            }
        }

        if (changed)
        {
            PersistRecords(player, _activeRunState, records);
            RecordsChanged?.Invoke();
        }
    }

    public static bool TryDiscardForecastRelic(Player player, ModelId relicId)
    {
        var relic = ModelDb.GetByIdOrNull<RelicModel>(relicId);
        var grabBag = player.RelicGrabBag ?? player.RunState?.SharedRelicGrabBag;
        var records = GetOrLoadPlayerRecords(player);
        if (relic == null || grabBag == null)
        {
            return false;
        }

        try
        {
            grabBag.Remove(relic);
            bool changed = false;
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.IsActive && record.GetPreviewRelicIds().Any(id => id.Equals(relicId)))
                {
                    records[i] = record with { IsActive = false };
                    changed = true;
                }
            }

            if (changed)
            {
                PersistRecords(player, player.RunState, records);
                RecordsChanged?.Invoke();
            }

            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner failed to discard forecast relic {relicId}: {ex}");
            return false;
        }
    }

    private static List<DivinationRecord> GetMutableRecords(Player? player)
    {
        return player != null
            ? GetOrLoadPlayerRecords(player)
            : Records;
    }

    private static List<DivinationRecord> GetOrLoadPlayerRecords(Player player)
    {
        if (!RecordsByPlayer.TryGetValue(player, out var records))
        {
            records = LoadPlayerRecords(player);
            RecordsByPlayer[player] = records;
        }

        return records;
    }

    private static List<DivinationRecord> LoadPlayerRecords(Player player)
    {
        string serialized = SavedPlayerRecords.Get(player) ?? "";
        if (!string.IsNullOrWhiteSpace(serialized) &&
            TryDeserializeRecords(
                serialized,
                $"player {player.NetId}",
                clearBadPlayerSave: player,
                out bool repairedPlayerRecords) is { } playerRecords)
        {
            if (repairedPlayerRecords)
            {
                SavedPlayerRecords.Set(player, JsonSerializer.Serialize(playerRecords));
            }

            return playerRecords;
        }

        if (TryGetSavedRun(player.RunState) is { } savedRun)
        {
            string legacySerialized = SavedRecords.Get(savedRun) ?? "[]";
            if (TryDeserializeRecords(
                    legacySerialized,
                    "legacy run",
                    clearBadPlayerSave: null,
                    out bool repairedLegacyRecords) is { } legacyRecords)
            {
                if (repairedLegacyRecords)
                {
                    SavedRecords.Set(savedRun, JsonSerializer.Serialize(legacyRecords));
                }

                return legacyRecords;
            }
        }

        return [];
    }

    private static List<DivinationRecord>? TryDeserializeRecords(
        string serialized,
        string source,
        Player? clearBadPlayerSave,
        out bool repaired)
    {
        repaired = false;
        try
        {
            var records = JsonSerializer.Deserialize<List<DivinationRecord>>(serialized) ?? [];
            int removedCount = RemoveDuplicateOneShotRecords(records);
            repaired = removedCount > 0;
            if (repaired)
            {
                MainFile.Logger.Info(
                    $"Diviner removed {removedCount} duplicate one-shot divination record(s) while loading {source}.");
            }

            return records;
        }
        catch (JsonException ex)
        {
            MainFile.Logger.Info($"Diviner failed to load {source} divination records: {ex}");
            if (clearBadPlayerSave != null)
            {
                SavedPlayerRecords.Set(clearBadPlayerSave, "[]");
            }

            return null;
        }
    }

    private static int RemoveDuplicateOneShotRecords(List<DivinationRecord> records)
    {
        HashSet<string> seenCategories = new(StringComparer.Ordinal);
        return records.RemoveAll(record =>
            IsOneShotCategory(record.Category) &&
            !seenCategories.Add(record.Category));
    }

    private static bool IsOneShotCategory(string category)
    {
        return category.StartsWith("Boss.", StringComparison.Ordinal);
    }

    private static DivinationRecord RecordPlaceholderInternal(IRunState? runState, Player? player, string source)
    {
        _activeRunState = runState ?? _activeRunState;
        _activePlayer = player ?? _activePlayer;
        var record = CreateActualDivination(_activeRunState, player, source);
        var records = GetMutableRecords(player);

        if (IsOneShotCategory(record.Category))
        {
            int existingIndex = records.FindIndex(existing => existing.Category == record.Category);
            if (existingIndex >= 0)
            {
                MainFile.Logger.Info(
                    $"Diviner suppressed duplicate one-shot divination category {record.Category} from {source}.");
                return records[existingIndex];
            }
        }

        records.Add(record);
        PersistRecords(player, _activeRunState, records);
        RecordsChanged?.Invoke();
        MainFile.Logger.Info($"Diviner recorded divination: {record.Text}");
        return record;
    }

    private static DivinationRecord RecordRelicDivinationInternal(IRunState? runState, Player? player, string source)
    {
        _activeRunState = runState ?? _activeRunState;
        _activePlayer = player ?? _activePlayer;
        var record = CreateRelicDivination(_activeRunState, player, source);
        var records = GetMutableRecords(player);

        records.Add(record);
        PersistRecords(player, _activeRunState, records);
        RecordsChanged?.Invoke();
        MainFile.Logger.Info($"Diviner recorded relic divination: {record.Text}");
        return record;
    }

    public static void LoadForPlayer(Player? player)
    {
        if (player == null)
        {
            LoadForRun(player?.RunState);
            return;
        }

        _activeRunState = player.RunState;
        _activePlayer = player;
        _ = GetOrLoadPlayerRecords(player);
        RecordsChanged?.Invoke();
    }

    public static void LoadForRun(IRunState? runState)
    {
        _activeRunState = runState;
        _activePlayer = null;
        Records.Clear();

        if (TryGetSavedRun(runState) is not { } savedRun)
        {
            RecordsChanged?.Invoke();
            return;
        }

        string serialized = SavedRecords.Get(savedRun) ?? "[]";
        if (TryDeserializeRecords(
                serialized,
                "saved run",
                clearBadPlayerSave: null,
                out bool repairedRecords) is { } loadedRecords)
        {
            Records.AddRange(loadedRecords);
            if (repairedRecords)
            {
                SavedRecords.Set(savedRun, JsonSerializer.Serialize(loadedRecords));
            }
        }
        else
        {
            SavedRecords.Set(savedRun, "[]");
        }

        RecordsChanged?.Invoke();
    }

    public static void Clear()
    {
        Clear(_activePlayer);
    }

    public static void ClearRuntimeState()
    {
        _activeRunState = null;
        _activePlayer = null;
        Records.Clear();
        RecordsByPlayer.Clear();
        RecordsChanged?.Invoke();
    }

    public static void Clear(Player? player)
    {
        var records = player != null ? GetOrLoadPlayerRecords(player) : Records;
        records.Clear();
        PersistRecords(player, player?.RunState ?? _activeRunState, records);
        RecordsChanged?.Invoke();
    }

    public static bool TryConsumeRecords(int count, IRunState? runState = null)
    {
        return TryConsumeRecords(_activePlayer, count, runState);
    }

    public static bool TryConsumeRecords(Player? player, int count, IRunState? runState = null)
    {
        if (count <= 0)
        {
            return true;
        }

        var records = GetMutableRecords(player);
        if (records.Count < count)
        {
            return false;
        }

        _activeRunState = runState ?? _activeRunState;
        _activePlayer = player ?? _activePlayer;
        var indexesToRemove = records
            .Select((record, index) => new { record, index })
            .OrderBy(entry => entry.record.IsActive ? 1 : 0)
            .ThenBy(entry => entry.index)
            .Take(count)
            .Select(entry => entry.index)
            .OrderByDescending(index => index)
            .ToList();

        foreach (int index in indexesToRemove)
        {
            records.RemoveAt(index);
        }

        PersistRecords(player, _activeRunState, records);
        RecordsChanged?.Invoke();
        return true;
    }

    public static bool TryConsumeRandomRecords(Player player, int count, IRunState? runState = null)
    {
        if (count <= 0)
        {
            return true;
        }

        var records = GetMutableRecords(player);
        if (records.Count < count)
        {
            return false;
        }

        _activeRunState = runState ?? _activeRunState;
        _activePlayer = player;
        var availableIndexes = Enumerable.Range(0, records.Count).ToList();
        var indexesToRemove = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            int selectedPosition = player.PlayerRng.Rewards.NextInt(availableIndexes.Count);
            indexesToRemove.Add(availableIndexes[selectedPosition]);
            availableIndexes.RemoveAt(selectedPosition);
        }

        foreach (int index in indexesToRemove.OrderByDescending(index => index))
        {
            records.RemoveAt(index);
        }

        PersistRecords(player, _activeRunState, records);
        RecordsChanged?.Invoke();
        return true;
    }

    private static void PersistCurrentRecords(IRunState? runState)
    {
        PersistRecords(_activePlayer, runState, GetRecords(_activePlayer));
    }

    private static void PersistRecords(Player? player, IRunState? runState, IReadOnlyList<DivinationRecord> records)
    {
        if (player != null)
        {
            SavedPlayerRecords.Set(player, JsonSerializer.Serialize(records));
            return;
        }

        if (TryGetSavedRun(runState) is { } savedRun)
        {
            SavedRecords.Set(savedRun, JsonSerializer.Serialize(records));
        }
    }

    private static int TryGetFloor(IRunState? runState)
    {
        return Math.Max(0, runState?.TotalFloor ?? 0);
    }

    private static RunState? TryGetSavedRun(IRunState? runState)
    {
        return runState as RunState;
    }

    private static DivinationRecord CreateActualDivination(IRunState? runState, Player? player, string source)
    {
        if (runState == null)
        {
            return FallbackRecord(source, "No active run state is available to read.");
        }

        RefreshActivity(runState, player);
        var records = GetRecords(player);

        List<ForecastCandidate> candidates = [];
        AddBossCandidates(candidates, runState, records);
        AddAncientCandidates(candidates, runState, player, records);
        AddRelicCandidates(candidates, runState, player, records);
        AddEliteCandidates(candidates, runState);
        AddEventCandidates(candidates, runState, records);

        if (candidates.Count == 0)
        {
            return FallbackRecord(source, "No committed future is currently readable.");
        }

        var freshCandidates = candidates.Where(candidate => !candidate.IsDuplicate).ToList();
        var selectionPool = freshCandidates.Count > 0 ? freshCandidates : candidates;
        var selected = PickWeightedByCategory(selectionPool, runState.Rng.Niche, records);
        return new DivinationRecord(
            selected.Category,
            selected.Text,
            TryGetFloor(runState),
            selected.PreviewRelicIds is { Count: > 0 } previewIds
                ? DivinationRecord.EncodeRelicIds(previewIds)
                : null);
    }

    private static DivinationRecord CreateRelicDivination(IRunState? runState, Player? player, string source)
    {
        if (runState == null)
        {
            return FallbackRecord(source, "No active run state is available to read.");
        }

        RefreshActivity(runState, player);
        var records = GetRecords(player);

        List<ForecastCandidate> candidates = [];
        AddRelicCandidates(candidates, runState, player, records);
        if (candidates.Count == 0)
        {
            return FallbackRecord(source, "No committed future is currently readable.");
        }

        var freshCandidates = candidates.Where(candidate => !candidate.IsDuplicate).ToList();
        var selectionPool = freshCandidates.Count > 0 ? freshCandidates : candidates;
        var selected = PickWeightedByCategory(selectionPool, runState.Rng.Niche, records);
        return new DivinationRecord(
            selected.Category,
            selected.Text,
            TryGetFloor(runState),
            selected.PreviewRelicIds is { Count: > 0 } previewIds
                ? DivinationRecord.EncodeRelicIds(previewIds)
                : null);
    }

    private static void AddBossCandidates(
        List<ForecastCandidate> candidates,
        IRunState runState,
        IReadOnlyList<DivinationRecord> records)
    {
        if (runState.CurrentActIndex <= 0)
        {
            AddBossCandidate(candidates, runState, records, 1, "Act 2 boss", "Boss.Act2");
        }

        if (runState.CurrentActIndex <= 1)
        {
            AddBossCandidate(candidates, runState, records, 2, "Act 3 boss", "Boss.Act3.Primary");

            var act3 = GetAct(runState, 2);
            if (act3?.HasSecondBoss == true && act3.SecondBossEncounter != null)
            {
                AddBossCandidate(
                    candidates,
                    runState,
                    records,
                    2,
                    "Act 3 second boss",
                    "Boss.Act3.Second",
                    true);
            }
        }
    }

    private static void AddBossCandidate(
        List<ForecastCandidate> candidates,
        IRunState runState,
        IReadOnlyList<DivinationRecord> records,
        int actIndex,
        string label,
        string category,
        bool useSecondBoss = false)
    {
        if (records.Any(record => record.Category == category))
        {
            return;
        }

        var act = GetAct(runState, actIndex);
        var boss = useSecondBoss ? act?.SecondBossEncounter : act?.BossEncounter;
        if (boss == null)
        {
            return;
        }

        var bossName = FormatModelName(boss);
        var chineseLabel = category switch
        {
            "Boss.Act2" => "第二幕首领",
            "Boss.Act3.Primary" => "第三幕首领",
            "Boss.Act3.Second" => "第三幕第二首领",
            _ => "首领"
        };

        candidates.Add(new ForecastCandidate(
            category,
            "Boss",
            DivinerLoc.Text($"{label}: {bossName}.", $"{chineseLabel}：{bossName}。")));
    }

    private static void AddAncientCandidates(
        List<ForecastCandidate> candidates,
        IRunState runState,
        Player? player,
        IReadOnlyList<DivinationRecord> records)
    {
        if (runState.CurrentActIndex <= 0)
        {
            AddAncientCandidate(candidates, runState, player, records, 1, "Act 2");
        }

        if (runState.CurrentActIndex <= 1)
        {
            AddAncientCandidate(candidates, runState, player, records, 2, "Act 3");
        }
    }

    private static void AddAncientCandidate(
        List<ForecastCandidate> candidates,
        IRunState runState,
        Player? player,
        IReadOnlyList<DivinationRecord> records,
        int actIndex,
        string label)
    {
        var category = $"AncientReward.Act{actIndex + 1}";
        if (records.Count(record => record.Category == category) >= 3)
        {
            return;
        }

        var ancient = GetAct(runState, actIndex)?.Ancient;
        if (ancient == null)
        {
            return;
        }

        var option = TryDescribeAncientOption(ancient, runState, player, category, records);
        if (option == null)
        {
            return;
        }

        var englishPrefix = option.IsProjected ? $"Projected {label}" : label;
        var chinesePrefix = option.IsProjected
            ? $"预计{TranslateActLabel(label)}"
            : TranslateActLabel(label);
        string text = DivinerLoc.Text(
            $"{englishPrefix} Ancient option: {option.Text} ({FormatModelName(ancient)}).",
            $"{chinesePrefix}远古奖励选项：{option.Text}（{FormatModelName(ancient)}）。");

        candidates.Add(new ForecastCandidate(
            category,
            "Ancient",
            text,
            PreviewRelicIds: option.PreviewRelicId is { } relicId ? [relicId] : []));
    }

    private static void AddRelicCandidates(
        List<ForecastCandidate> candidates,
        IRunState runState,
        Player? player,
        IReadOnlyList<DivinationRecord> records)
    {
        var grabBag = player?.RelicGrabBag ?? runState.SharedRelicGrabBag;
        if (grabBag == null || !grabBag.IsPopulated)
        {
            return;
        }

        Dictionary<RelicRarity, List<ModelId>> relicIdLists;
        try
        {
            relicIdLists = grabBag.ToSerializable().RelicIdLists;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner relic divination could not snapshot relic queue: {ex}");
            return;
        }

        AddRelicCandidate(candidates, relicIdLists, records, RelicRarity.Common, 1, "common relic", "普通遗物", false);
        AddRelicCandidate(candidates, relicIdLists, records, RelicRarity.Uncommon, 1, "uncommon relic", "罕见遗物", false);
        AddRelicCandidate(candidates, relicIdLists, records, RelicRarity.Rare, 1, "rare relic", "稀有遗物", false);
        AddRelicCandidate(candidates, relicIdLists, records, RelicRarity.Shop, 1, "shop relic", "商店遗物", true);
        AddRelicCandidate(
            candidates,
            relicIdLists,
            records,
            RelicRarity.Common,
            1,
            "shop common relic",
            "商店普通遗物",
            true,
            "Relic.ShopCommon");
        AddRelicCandidate(
            candidates,
            relicIdLists,
            records,
            RelicRarity.Uncommon,
            1,
            "shop uncommon relic",
            "商店罕见遗物",
            true,
            "Relic.ShopUncommon");
        AddRelicCandidate(
            candidates,
            relicIdLists,
            records,
            RelicRarity.Rare,
            1,
            "shop rare relic",
            "商店稀有遗物",
            true,
            "Relic.ShopRare");
    }

    private static void AddRelicCandidate(
        List<ForecastCandidate> candidates,
        IReadOnlyDictionary<RelicRarity, List<ModelId>> relicIdLists,
        IReadOnlyList<DivinationRecord> records,
        RelicRarity rarity,
        int count,
        string label,
        string chineseLabel,
        bool fromBack,
        string? categoryOverride = null)
    {
        if (!relicIdLists.TryGetValue(rarity, out var ids) || ids.Count == 0)
        {
            return;
        }

        var orderedIds = fromBack
            ? ids.AsEnumerable().Reverse().ToList()
            : ids.ToList();
        var category = categoryOverride ?? $"Relic.{rarity}";
        var activeForecastIds = GetActiveQueuedRelicForecastIds(category, orderedIds, records);
        var selectedIds = SelectNextRelicForecastIds(
            category,
            orderedIds,
            count,
            activeForecastIds,
            records);
        if (selectedIds.Count == 0)
        {
            return;
        }

        var englishNames = string.Join(", ", selectedIds.Select(FormatModelId));
        var chineseNames = string.Join("，", selectedIds.Select(FormatModelId));
        var text = activeForecastIds.Count > 0
            ? DivinerLoc.Text(
                $"Next {label} after current forecasts: {englishNames}.",
                $"当前预示之后的下一件{chineseLabel}：{chineseNames}。")
            : DivinerLoc.Text(
                $"Next {label}: {englishNames}.",
                $"下一件{chineseLabel}：{chineseNames}。");
        bool isDuplicate = selectedIds.All(id => activeForecastIds.Any(activeId => activeId.Equals(id)));

        candidates.Add(new ForecastCandidate(
            category,
            "Relic",
            text,
            PreviewRelicIds: selectedIds,
            IsDuplicate: isDuplicate));
    }

    private static IReadOnlyList<ModelId> GetActiveQueuedRelicForecastIds(
        string category,
        IReadOnlyList<ModelId> orderedQueue,
        IReadOnlyList<DivinationRecord> records)
    {
        return records
            .Where(record => record.IsActive && record.Category == category)
            .SelectMany(record => record.GetPreviewRelicIds())
            .Where(id => orderedQueue.Any(queuedId => queuedId.Equals(id)))
            .Distinct()
            .ToList();
    }

    private static List<ModelId> SelectNextRelicForecastIds(
        string category,
        IReadOnlyList<ModelId> orderedQueue,
        int count,
        IReadOnlyList<ModelId> activeForecastIds,
        IReadOnlyList<DivinationRecord> records)
    {
        int startIndex = FindMostRecentActiveForecastIndex(category, orderedQueue, records);
        var selectedIds = orderedQueue
            .Skip(startIndex + 1)
            .Where(id => activeForecastIds.All(activeId => !activeId.Equals(id)))
            .Take(count)
            .ToList();

        if (selectedIds.Count > 0)
        {
            return selectedIds;
        }

        return orderedQueue
            .Where(id => activeForecastIds.All(activeId => !activeId.Equals(id)))
            .Take(count)
            .ToList();
    }

    private static int FindMostRecentActiveForecastIndex(
        string category,
        IReadOnlyList<ModelId> orderedQueue,
        IReadOnlyList<DivinationRecord> records)
    {
        foreach (var record in records.Reverse())
        {
            if (!record.IsActive || record.Category != category)
            {
                continue;
            }

            foreach (var relicId in record.GetPreviewRelicIds().Reverse())
            {
                for (int i = 0; i < orderedQueue.Count; i++)
                {
                    if (orderedQueue[i].Equals(relicId))
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    private static void AddEliteCandidates(List<ForecastCandidate> candidates, IRunState runState)
    {
        if (CountVisitedElitesInCurrentAct(runState) > 1)
        {
            return;
        }

        // Current map points do not expose committed elite EncounterModels, only coordinates.
        // Skipping this provider avoids recording unintelligible "row/col" forecasts.
    }

    private static void AddEventCandidates(
        List<ForecastCandidate> candidates,
        IRunState runState,
        IReadOnlyList<DivinationRecord> records)
    {
        if (runState.BaseRoom is not MegaCrit.Sts2.Core.Rooms.EventRoom eventRoom ||
            eventRoom.CanonicalEvent == null)
        {
            return;
        }

        var eventName = FormatModelName(eventRoom.CanonicalEvent);
        var text = DivinerLoc.Text(
            $"Current committed event: {eventName}.",
            $"已确定事件：{eventName}。");
        const string category = "Event.Committed";
        candidates.Add(new ForecastCandidate(
            category,
            "Event",
            text,
            IsDuplicate: records.Any(record => record.Category == category && record.Text == text)));
    }

    private static ActModel? GetAct(IRunState runState, int actIndex)
    {
        return actIndex >= 0 && actIndex < runState.Acts.Count ? runState.Acts[actIndex] : null;
    }

    private static int CountVisitedElitesInCurrentAct(IRunState runState)
    {
        IReadOnlyList<MapPointHistoryEntry> actHistory = runState.CurrentActIndex >= 0 &&
                                                         runState.CurrentActIndex < runState.MapPointHistory.Count
            ? runState.MapPointHistory[runState.CurrentActIndex]
            : [];

        return actHistory.Count(entry => entry.MapPointType == MapPointType.Elite);
    }

    private static IEnumerable<MapPoint> GetCurrentFrontier(IRunState runState)
    {
        if (runState.CurrentMapPoint != null)
        {
            return runState.CurrentMapPoint.Children;
        }

        return runState.Map?.startMapPoints ?? Enumerable.Empty<MapPoint>();
    }

    private static IEnumerable<MapPoint> GetReachableMapPoints(IEnumerable<MapPoint> startPoints)
    {
        HashSet<MapPoint> visited = [];
        Queue<MapPoint> queue = new(startPoints);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            if (!visited.Add(point))
            {
                continue;
            }

            yield return point;
            foreach (var child in point.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    private static AncientOptionForecast? TryDescribeAncientOption(
        AncientEventModel ancient,
        IRunState runState,
        Player? player,
        string category,
        IReadOnlyList<DivinationRecord> records)
    {
        return TryDescribeGeneratedAncientOption(ancient, category, records) ??
               TryDescribeProjectedAncientOption(ancient, runState, player, category, records);
    }

    private static AncientOptionForecast? TryDescribeGeneratedAncientOption(
        AncientEventModel ancient,
        string category,
        IReadOnlyList<DivinationRecord> records)
    {
        var options = TryGetGeneratedAncientOptions(ancient);
        return options == null ? null : SelectAncientOptionForecast(options, category, false, records);
    }

    private static AncientOptionForecast? TryDescribeProjectedAncientOption(
        AncientEventModel ancient,
        IRunState runState,
        Player? player,
        string category,
        IReadOnlyList<DivinationRecord> records)
    {
        var simulationPlayer = TryGetAncientSimulationPlayer(runState, player);
        if (simulationPlayer == null)
        {
            return null;
        }

        try
        {
            var simulatedAncient = ancient.ToMutable() as AncientEventModel;
            if (simulatedAncient == null)
            {
                return null;
            }

            SetEventOwnerAndRng(simulatedAncient, runState, simulationPlayer);
            var options = InvokeGenerateInitialOptions(simulatedAncient);
            return options == null ? null : SelectAncientOptionForecast(options, category, true, records);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner ancient divination could not simulate options: {ex}");
            return null;
        }
    }

    private static IEnumerable<EventOption>? TryGetGeneratedAncientOptions(AncientEventModel ancient)
    {
        try
        {
            return ancient
                .GetType()
                .GetProperty("GeneratedOptions", System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(ancient) as IEnumerable<EventOption>;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner ancient divination could not inspect generated options: {ex}");
            return null;
        }
    }

    private static AncientOptionForecast? SelectAncientOptionForecast(
        IEnumerable<EventOption> options,
        string category,
        bool isProjected,
        IReadOnlyList<DivinationRecord> records)
    {
        var optionList = options.ToList();
        if (optionList.Count == 0)
        {
            return null;
        }

        var forecasts = optionList
            .Where(option => !option.IsLocked)
            .Select(option => TryBuildAncientOptionForecast(option, isProjected))
            .Where(forecast => forecast != null)
            .Cast<AncientOptionForecast>()
            .ToList();
        if (forecasts.Count == 0)
        {
            forecasts = optionList
                .Select(option => TryBuildAncientOptionForecast(option, isProjected))
                .Where(forecast => forecast != null)
                .Cast<AncientOptionForecast>()
                .ToList();
        }

        if (forecasts.Count == 0)
        {
            return null;
        }

        var seenRelics = records
            .Where(record => record.Category == category)
            .SelectMany(record => record.GetPreviewRelicIds())
            .ToList();
        return forecasts.FirstOrDefault(forecast =>
                forecast.PreviewRelicId is { } relicId &&
                seenRelics.All(seenId => !seenId.Equals(relicId))) ??
            forecasts.FirstOrDefault(forecast =>
                records.Where(record => record.Category == category)
                    .All(record => !record.Text.Contains(forecast.Text, StringComparison.Ordinal))) ??
            forecasts[0];
    }

    private static Player? TryGetAncientSimulationPlayer(IRunState runState, Player? player)
    {
        if (player != null)
        {
            return player;
        }

        return runState is IPlayerCollection playerCollection
            ? playerCollection.Players.FirstOrDefault()
            : null;
    }

    private static void SetEventOwnerAndRng(AncientEventModel ancient, IRunState runState, Player player)
    {
        var eventType = typeof(EventModel);
        eventType
            .GetProperty("Owner", System.Reflection.BindingFlags.Instance |
                                  System.Reflection.BindingFlags.Public |
                                  System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(ancient, player);
        eventType
            .GetProperty("Rng", System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(ancient, CreateEventRng(ancient, runState, player));
    }

    private static MegaCrit.Sts2.Core.Random.Rng CreateEventRng(EventModel eventModel, IRunState runState, Player player)
    {
        var playerSlotOffset = !eventModel.IsShared && runState is IPlayerCollection playerCollection
            ? playerCollection.GetPlayerSlotIndex(player)
            : 0;
        var modelHash = StringHelper.GetDeterministicHashCode(eventModel.Id.Entry);
        var eventSeed = unchecked(runState.Rng.Seed + (uint)playerSlotOffset + (uint)modelHash);
        return new MegaCrit.Sts2.Core.Random.Rng(eventSeed, 0);
    }

    private static IEnumerable<EventOption>? InvokeGenerateInitialOptions(AncientEventModel ancient)
    {
        try
        {
            var result = typeof(AncientEventModel)
                .GetMethod("GenerateInitialOptionsWrapper", System.Reflection.BindingFlags.Instance |
                                                            System.Reflection.BindingFlags.Public |
                                                            System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(ancient, null);
            if (result is IEnumerable<EventOption> options)
            {
                return options;
            }

            return TryGetGeneratedAncientOptions(ancient);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner ancient divination could not invoke option generation: {ex}");
            return null;
        }
    }

    private static AncientOptionForecast? TryBuildAncientOptionForecast(EventOption option, bool isProjected)
    {
        if (option.Relic != null)
        {
            return new AncientOptionForecast(
                DivinerLoc.Text($"relic {FormatModelName(option.Relic)}", $"遗物 {FormatModelName(option.Relic)}"),
                option.Relic.Id,
                isProjected);
        }

        var title = SafeFormat(option.Title);
        var description = SafeFormat(option.Description);
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(description))
        {
            return new AncientOptionForecast($"{title}: {description}", null, isProjected);
        }

        var text = !string.IsNullOrWhiteSpace(title) ? title : description;
        return string.IsNullOrWhiteSpace(text) ? null : new AncientOptionForecast(text, null, isProjected);
    }

    private static bool IsRecordStillActive(DivinationRecord record, IRunState runState, Player? player)
    {
        if (!record.IsActive)
        {
            return false;
        }

        if (record.Category.StartsWith("Boss.Act2", StringComparison.Ordinal))
        {
            return runState.CurrentActIndex < 1;
        }

        if (record.Category.StartsWith("Boss.Act3", StringComparison.Ordinal))
        {
            return runState.CurrentActIndex < 2;
        }

        if (record.Category.StartsWith("AncientReward.Act", StringComparison.Ordinal) &&
            int.TryParse(record.Category["AncientReward.Act".Length..], out int actNumber))
        {
            return runState.CurrentActIndex < actNumber;
        }

        if (GetCategoryGroup(record.Category) == "Relic")
        {
            return record.GetPreviewRelicIds().Any(id => IsRelicStillQueued(id, runState, player));
        }

        if (record.Category == "Event.Committed")
        {
            return TryGetFloor(runState) <= record.Floor;
        }

        if (record.Category == "Elite.CurrentAct")
        {
            return CountVisitedElitesInCurrentAct(runState) <= 1;
        }

        return true;
    }

    private static bool IsRelicStillQueued(ModelId relicId, IRunState runState, Player? player)
    {
        var grabBag = player?.RelicGrabBag ?? runState.SharedRelicGrabBag;
        if (grabBag == null || !grabBag.IsPopulated)
        {
            return false;
        }

        try
        {
            return grabBag.ToSerializable()
                .RelicIdLists
                .Values
                .Any(ids => ids.Any(id => id.Equals(relicId)));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner could not refresh relic divination activity: {ex}");
            return true;
        }
    }

    private static string TranslateActLabel(string label)
    {
        return label switch
        {
            "Act 2" => "第二幕",
            "Act 3" => "第三幕",
            _ => label
        };
    }

    private static ForecastCandidate PickWeightedByCategory(
        IReadOnlyList<ForecastCandidate> candidates,
        MegaCrit.Sts2.Core.Random.Rng rng,
        IReadOnlyList<DivinationRecord> records)
    {
        var recentCategories = records
            .TakeLast(2)
            .Select(record => record.Category)
            .ToHashSet(StringComparer.Ordinal);

        var groups = candidates
            .GroupBy(candidate => candidate.Category, StringComparer.Ordinal)
            .Select(group => new ForecastGroup(
                group.Key,
                group.ToList(),
                recentCategories.Contains(group.Key) ? 12 : 100))
            .ToList();
        int totalGroupWeight = groups.Sum(group => group.Weight);
        int groupRoll = rng.NextInt(Math.Max(1, totalGroupWeight));

        foreach (var group in groups)
        {
            groupRoll -= group.Weight;
            if (groupRoll < 0)
            {
                return PickWeightedWithinGroup(group.Candidates, rng);
            }
        }

        return PickWeightedWithinGroup(groups[^1].Candidates, rng);
    }

    private static ForecastCandidate PickWeightedWithinGroup(
        IReadOnlyList<ForecastCandidate> candidates,
        MegaCrit.Sts2.Core.Random.Rng rng)
    {
        var weights = candidates.Select(candidate => candidate.Weight).ToList();
        int totalWeight = weights.Sum();
        int roll = rng.NextInt(Math.Max(1, totalWeight));

        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return candidates[i];
            }
        }

        return candidates[^1];
    }

    private static string GetCategoryGroup(string category)
    {
        if (category.StartsWith("AncientReward.", StringComparison.Ordinal))
        {
            return "Ancient";
        }

        var dotIndex = category.IndexOf('.', StringComparison.Ordinal);
        return dotIndex > 0 ? category[..dotIndex] : category;
    }

    private static DivinationRecord FallbackRecord(string source, string reason)
    {
        return new DivinationRecord(
            "Unavailable",
            DivinerLoc.Text($"{source}: {reason}", $"{source}：{TranslateFallbackReason(reason)}"),
            TryGetFloor(_activeRunState));
    }

    private static string TranslateFallbackReason(string reason)
    {
        return reason switch
        {
            "No active run state is available to read." => "没有可读取的当前局内状态。",
            "No committed future is currently readable." => "当前没有可读取的已确定未来。",
            _ => reason
        };
    }

    private static string FormatModelName(AbstractModel model)
    {
        string? formatted = model switch
        {
            EncounterModel encounter => SafeFormat(encounter.Title),
            EventModel eventModel => SafeFormat(eventModel.Title),
            RelicModel relic => SafeFormat(relic.Title),
            _ => null
        };

        return string.IsNullOrWhiteSpace(formatted)
            ? FormatModelId(model.Id)
            : formatted;
    }

    private static string FormatModelId(ModelId id)
    {
        var entry = id.Entry;
        if (string.IsNullOrWhiteSpace(entry))
        {
            return id.ToString();
        }

        var separatorIndex = entry.LastIndexOfAny(['-', ':', '.', '/']);
        if (separatorIndex >= 0 && separatorIndex + 1 < entry.Length)
        {
            entry = entry[(separatorIndex + 1)..];
        }

        return HumanizeIdentifier(entry);
    }

    private static string HumanizeIdentifier(string value)
    {
        value = value
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace(".", " ", StringComparison.Ordinal)
            .Replace("Encounter", "", StringComparison.Ordinal)
            .Replace("Event", "", StringComparison.Ordinal)
            .Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        List<char> chars = [];
        for (int i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0 &&
                char.IsUpper(current) &&
                (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))) &&
                chars.LastOrDefault() != ' ')
            {
                chars.Add(' ');
            }

            chars.Add(current);
        }

        return new string(chars.ToArray());
    }

    private static string? SafeFormat(LocString? locString)
    {
        if (locString == null || locString.IsEmpty)
        {
            return null;
        }

        try
        {
            var formatted = locString.GetFormattedText();
            return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
        }
        catch
        {
            var fallback = locString.ToString();
            return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
        }
    }

    private sealed record ForecastCandidate(
        string Category,
        string Group,
        string Text,
        int Weight = 100,
        IReadOnlyList<ModelId>? PreviewRelicIds = null,
        bool IsDuplicate = false);

    private sealed record ForecastGroup(
        string Group,
        IReadOnlyList<ForecastCandidate> Candidates,
        int Weight);

    private sealed record AncientOptionForecast(string Text, ModelId? PreviewRelicId, bool IsProjected);
}
