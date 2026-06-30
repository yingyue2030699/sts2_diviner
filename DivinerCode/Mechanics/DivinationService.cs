using System.Text.Json;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

    private static readonly List<DivinationRecord> Records = [];
    private static IRunState? _activeRunState;

    public static event Action? RecordsChanged;

    public static IReadOnlyList<DivinationRecord> CurrentRecords => Records;

    public static IReadOnlyList<DivinationRecord> GetVisibleRecords(bool hideInactive)
    {
        return hideInactive
            ? Records.Where(record => record.IsActive).ToList()
            : Records;
    }

    public static IReadOnlyList<ModelId> ActiveRelicDivinationIds => Records
        .Where(record => record.IsActive && GetCategoryGroup(record.Category) == "Relic")
        .SelectMany(record => record.GetPreviewRelicIds())
        .Distinct()
        .ToList();

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
        DivinerCombatRuntime.RecordCombatDivination();
        int energyBonus = DivinerCombatRuntime.ConsumeNextDivinationEnergyBonus();
        if (energyBonus > 0)
        {
            await PlayerCmd.GainEnergy(energyBonus, player);
        }

        if (choiceContext != null && player.Creature.GetPower<PropheticTrancePower>() != null)
        {
            await CardPileCmd.Draw(choiceContext, 2, player, false);
        }

        await DivinerStatusPowerSync.Sync(player, choiceContext);
        return record;
    }

    public static void RefreshActivity(IRunState? runState, Player? player)
    {
        _activeRunState = runState ?? _activeRunState;
        if (_activeRunState == null || Records.Count == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < Records.Count; i++)
        {
            var record = Records[i];
            bool isActive = IsRecordStillActive(record, _activeRunState, player);
            if (record.IsActive != isActive)
            {
                Records[i] = record with { IsActive = isActive };
                changed = true;
            }
        }

        if (changed)
        {
            PersistCurrentRecords(_activeRunState);
            RecordsChanged?.Invoke();
        }
    }

    public static bool TryDiscardForecastRelic(Player player, ModelId relicId)
    {
        var relic = ModelDb.GetByIdOrNull<RelicModel>(relicId);
        var grabBag = player.RelicGrabBag ?? player.RunState?.SharedRelicGrabBag;
        if (relic == null || grabBag == null)
        {
            return false;
        }

        try
        {
            grabBag.Remove(relic);
            bool changed = false;
            for (int i = 0; i < Records.Count; i++)
            {
                var record = Records[i];
                if (record.IsActive && record.GetPreviewRelicIds().Any(id => id.Equals(relicId)))
                {
                    Records[i] = record with { IsActive = false };
                    changed = true;
                }
            }

            if (changed)
            {
                PersistCurrentRecords(player.RunState);
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

    private static DivinationRecord RecordPlaceholderInternal(IRunState? runState, Player? player, string source)
    {
        _activeRunState = runState ?? _activeRunState;
        var record = CreateActualDivination(_activeRunState, player, source);

        Records.Add(record);
        PersistCurrentRecords(_activeRunState);
        RecordsChanged?.Invoke();
        MainFile.Logger.Info($"Diviner recorded divination: {record.Text}");
        return record;
    }

    public static void LoadForRun(IRunState? runState)
    {
        _activeRunState = runState;
        Records.Clear();

        if (TryGetSavedRun(runState) is not { } savedRun)
        {
            RecordsChanged?.Invoke();
            return;
        }

        string serialized = SavedRecords.Get(savedRun) ?? "[]";
        try
        {
            var loadedRecords = JsonSerializer.Deserialize<List<DivinationRecord>>(serialized);
            if (loadedRecords != null)
            {
                Records.AddRange(loadedRecords);
            }
        }
        catch (JsonException ex)
        {
            MainFile.Logger.Info($"Diviner failed to load saved divination records: {ex}");
            SavedRecords.Set(savedRun, "[]");
        }

        RecordsChanged?.Invoke();
    }

    public static void Clear()
    {
        Records.Clear();
        PersistCurrentRecords(_activeRunState);
        RecordsChanged?.Invoke();
    }

    private static void PersistCurrentRecords(IRunState? runState)
    {
        if (TryGetSavedRun(runState) is { } savedRun)
        {
            SavedRecords.Set(savedRun, JsonSerializer.Serialize(Records));
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

        List<ForecastCandidate> candidates = [];
        AddBossCandidates(candidates, runState);
        AddAncientCandidates(candidates, runState);
        AddRelicCandidates(candidates, runState, player);
        AddEliteCandidates(candidates, runState);
        AddEventCandidates(candidates, runState);

        if (candidates.Count == 0)
        {
            return FallbackRecord(source, "No committed future is currently readable.");
        }

        var freshCandidates = candidates.Where(candidate => !candidate.IsDuplicate).ToList();
        var selectionPool = freshCandidates.Count > 0 ? freshCandidates : candidates;
        var selected = PickWeightedByCategory(selectionPool);
        return new DivinationRecord(
            selected.Category,
            selected.Text,
            TryGetFloor(runState),
            selected.PreviewRelicIds is { Count: > 0 } previewIds
                ? DivinationRecord.EncodeRelicIds(previewIds)
                : null);
    }

    private static void AddBossCandidates(List<ForecastCandidate> candidates, IRunState runState)
    {
        if (runState.CurrentActIndex <= 0)
        {
            AddBossCandidate(candidates, runState, 1, "Act 2 boss", "Boss.Act2");
        }

        if (runState.CurrentActIndex <= 1)
        {
            AddBossCandidate(candidates, runState, 2, "Act 3 boss", "Boss.Act3.Primary");

            var act3 = GetAct(runState, 2);
            if (act3?.HasSecondBoss == true && act3.SecondBossEncounter != null)
            {
                AddBossCandidate(candidates, runState, 2, "Act 3 second boss", "Boss.Act3.Second", true);
            }
        }
    }

    private static void AddBossCandidate(
        List<ForecastCandidate> candidates,
        IRunState runState,
        int actIndex,
        string label,
        string category,
        bool useSecondBoss = false)
    {
        if (Records.Any(record => record.Category == category))
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

    private static void AddAncientCandidates(List<ForecastCandidate> candidates, IRunState runState)
    {
        if (runState.CurrentActIndex <= 0)
        {
            AddAncientCandidate(candidates, runState, 1, "Act 2");
        }

        if (runState.CurrentActIndex <= 1)
        {
            AddAncientCandidate(candidates, runState, 2, "Act 3");
        }
    }

    private static void AddAncientCandidate(
        List<ForecastCandidate> candidates,
        IRunState runState,
        int actIndex,
        string label)
    {
        var category = $"AncientReward.Act{actIndex + 1}";
        if (Records.Count(record => record.Category == category) >= 3)
        {
            return;
        }

        var ancient = GetAct(runState, actIndex)?.Ancient;
        if (ancient == null)
        {
            return;
        }

        var option = TryDescribeGeneratedAncientOption(ancient, category);
        if (option == null)
        {
            return;
        }

        string text = DivinerLoc.Text(
            $"{label} Ancient option: {option.Text} ({FormatModelName(ancient)}).",
            $"{TranslateActLabel(label)}远古奖励选项：{option.Text}（{FormatModelName(ancient)}）。");

        candidates.Add(new ForecastCandidate(
            category,
            "Ancient",
            text,
            PreviewRelicIds: option.PreviewRelicId is { } relicId ? [relicId] : []));
    }

    private static void AddRelicCandidates(List<ForecastCandidate> candidates, IRunState runState, Player? player)
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

        AddRelicCandidate(candidates, relicIdLists, RelicRarity.Common, 1, "common relic", false);
        AddRelicCandidate(candidates, relicIdLists, RelicRarity.Uncommon, 1, "uncommon relic", false);
        AddRelicCandidate(candidates, relicIdLists, RelicRarity.Rare, 1, "rare relic", false);
        AddRelicCandidate(candidates, relicIdLists, RelicRarity.Shop, 1, "shop relic", true);
    }

    private static void AddRelicCandidate(
        List<ForecastCandidate> candidates,
        IReadOnlyDictionary<RelicRarity, List<ModelId>> relicIdLists,
        RelicRarity rarity,
        int count,
        string label,
        bool fromBack)
    {
        if (!relicIdLists.TryGetValue(rarity, out var ids) || ids.Count == 0)
        {
            return;
        }

        var orderedIds = fromBack
            ? ids.AsEnumerable().Reverse().ToList()
            : ids.ToList();
        var category = $"Relic.{rarity}";
        var activeForecastIds = GetActiveQueuedRelicForecastIds(category, orderedIds);
        var selectedIds = SelectNextRelicForecastIds(category, orderedIds, count, activeForecastIds);
        if (selectedIds.Count == 0)
        {
            return;
        }

        var englishNames = string.Join(", ", selectedIds.Select(FormatModelId));
        var chineseNames = string.Join("，", selectedIds.Select(FormatModelId));
        var text = activeForecastIds.Count > 0
            ? DivinerLoc.Text(
                $"Next {label} after current forecasts: {englishNames}.",
                $"当前预示之后的下一件{TranslateRelicLabel(rarity)}：{chineseNames}。")
            : DivinerLoc.Text(
                $"Next {label}: {englishNames}.",
                $"下一件{TranslateRelicLabel(rarity)}：{chineseNames}。");
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
        IReadOnlyList<ModelId> orderedQueue)
    {
        return Records
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
        IReadOnlyList<ModelId> activeForecastIds)
    {
        int startIndex = FindMostRecentActiveForecastIndex(category, orderedQueue);
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

    private static int FindMostRecentActiveForecastIndex(string category, IReadOnlyList<ModelId> orderedQueue)
    {
        foreach (var record in Records.AsEnumerable().Reverse())
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

    private static void AddEventCandidates(List<ForecastCandidate> candidates, IRunState runState)
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
            IsDuplicate: Records.Any(record => record.Category == category && record.Text == text)));
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

    private static AncientOptionForecast? TryDescribeGeneratedAncientOption(AncientEventModel ancient, string category)
    {
        try
        {
            var options = ancient
                .GetType()
                .GetProperty("GeneratedOptions", System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(ancient) as IEnumerable<EventOption>;
            var optionList = options?.ToList();
            if (optionList == null || optionList.Count == 0)
            {
                return null;
            }

            var forecasts = optionList
                .Where(option => !option.IsLocked)
                .Select(TryBuildAncientOptionForecast)
                .Where(forecast => forecast != null)
                .Cast<AncientOptionForecast>()
                .ToList();
            if (forecasts.Count == 0)
            {
                forecasts = optionList
                    .Select(TryBuildAncientOptionForecast)
                    .Where(forecast => forecast != null)
                    .Cast<AncientOptionForecast>()
                    .ToList();
            }

            if (forecasts.Count == 0)
            {
                return null;
            }

            var seenRelics = Records
                .Where(record => record.Category == category)
                .SelectMany(record => record.GetPreviewRelicIds())
                .ToList();
            return forecasts.FirstOrDefault(forecast =>
                    forecast.PreviewRelicId is { } relicId &&
                    seenRelics.All(seenId => !seenId.Equals(relicId))) ??
                forecasts.FirstOrDefault(forecast =>
                    Records.Where(record => record.Category == category)
                        .All(record => !record.Text.Contains(forecast.Text, StringComparison.Ordinal))) ??
                forecasts[0];
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner ancient divination could not inspect options: {ex}");
            return null;
        }
    }

    private static AncientOptionForecast? TryBuildAncientOptionForecast(EventOption option)
    {
        if (option.Relic != null)
        {
            return new AncientOptionForecast(
                DivinerLoc.Text($"relic {FormatModelName(option.Relic)}", $"遗物 {FormatModelName(option.Relic)}"),
                option.Relic.Id);
        }

        var title = SafeFormat(option.Title);
        var description = SafeFormat(option.Description);
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(description))
        {
            return new AncientOptionForecast($"{title}: {description}", null);
        }

        var text = !string.IsNullOrWhiteSpace(title) ? title : description;
        return string.IsNullOrWhiteSpace(text) ? null : new AncientOptionForecast(text, null);
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

    private static string TranslateRelicLabel(RelicRarity rarity)
    {
        return rarity switch
        {
            RelicRarity.Common => "普通遗物",
            RelicRarity.Uncommon => "罕见遗物",
            RelicRarity.Rare => "稀有遗物",
            RelicRarity.Shop => "商店遗物",
            _ => "遗物"
        };
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

    private static ForecastCandidate PickWeightedByCategory(IReadOnlyList<ForecastCandidate> candidates)
    {
        var recentGroups = Records
            .TakeLast(2)
            .Select(record => GetCategoryGroup(record.Category))
            .ToHashSet(StringComparer.Ordinal);

        var groups = candidates
            .GroupBy(candidate => candidate.Group, StringComparer.Ordinal)
            .Select(group => new ForecastGroup(
                group.Key,
                group.ToList(),
                recentGroups.Contains(group.Key) ? 12 : 100))
            .ToList();
        int totalGroupWeight = groups.Sum(group => group.Weight);
        int groupRoll = Random.Shared.Next(Math.Max(1, totalGroupWeight));

        foreach (var group in groups)
        {
            groupRoll -= group.Weight;
            if (groupRoll < 0)
            {
                return PickWeightedWithinGroup(group.Candidates);
            }
        }

        return PickWeightedWithinGroup(groups[^1].Candidates);
    }

    private static ForecastCandidate PickWeightedWithinGroup(IReadOnlyList<ForecastCandidate> candidates)
    {
        var weights = candidates.Select(candidate => candidate.Weight).ToList();
        int totalWeight = weights.Sum();
        int roll = Random.Shared.Next(Math.Max(1, totalWeight));

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

    private sealed record AncientOptionForecast(string Text, ModelId? PreviewRelicId);
}
