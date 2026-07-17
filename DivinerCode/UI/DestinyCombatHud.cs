using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Localization;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace Diviner.DivinerCode.UI;

public static class DestinyCombatHud
{
    public const string HudNodeName = "DivinerDestinyCombatHud";

    private const string PanelName = "Panel";
    private const string TimerName = "RefreshTimer";
    private const string DestinyValueName = "DestinyValue";
    private const string OmenName = "Omen";
    private const string DivinationName = "Divination";
    private const string DivinationListName = "DivinationList";
    private const string DisplayModeName = "DisplayMode";
    private const string CountdownName = "Countdown";
    private const string PipsName = "Pips";

    private static readonly Color PanelColor = new("10131de6");
    private static readonly Color BorderColor = new("c7b7ffcc");
    private static readonly Color TextColor = new("f2f0ff");
    private static readonly Color MutedTextColor = new("aeb2c6");
    private static readonly Color GoodOmenColor = new("7bdba0");
    private static readonly Color BadOmenColor = new("f07278");
    private static readonly Color NeutralOmenColor = new("e8b742");
    private static readonly Color EmptyPipColor = new("34394a");

    private static bool _isOpen;
    private static DivinationDisplayMode _displayMode = DivinationDisplayMode.Default;
    private static string _lastRenderedDivinationSignature = "";

    public static void Toggle()
    {
        _isOpen = !_isOpen;
        EnsureMounted();
        RefreshIfMounted();
    }

    public static void Close()
    {
        _isOpen = false;
        RefreshIfMounted();
    }

    public static void CloseAndDispose()
    {
        _isOpen = false;
        _lastRenderedDivinationSignature = "";
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        tree.Root.GetNodeOrNull<CanvasLayer>(HudNodeName)?.QueueFree();
    }

    public static void EnsureMounted()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        var existing = tree.Root.GetNodeOrNull<CanvasLayer>(HudNodeName);
        if (existing != null)
        {
            Refresh(existing);
            return;
        }

        var layer = new CanvasLayer
        {
            Name = HudNodeName,
            Layer = 128,
            Visible = false
        };
        BuildHudRoot(layer);
        AddRefreshTimer(layer);
        tree.Root.CallDeferred(Node.MethodName.AddChild, layer);
    }

    public static void RefreshIfMounted()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        if (tree.Root.GetNodeOrNull<CanvasLayer>(HudNodeName) is { } layer)
        {
            Refresh(layer);
        }
    }

    private static void Refresh(CanvasLayer layer)
    {
        var player = GetTrackedDivinerPlayer();
        layer.Visible = player != null && _isOpen;
        if (player == null || !_isOpen)
        {
            return;
        }

        DivinerCombatRuntime.TrackPlayer(player);
        DivinationService.RefreshActivity(player.RunState, player);
        var snapshot = DestinyService.SnapshotFor(player);

        var destinyValue = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/Header/{DestinyValueName}");
        var omenLabel = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/Header/{OmenName}");
        var divinationLabel = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/{DivinationName}");
        var displayMode = layer.GetNodeOrNull<OptionButton>($"{PanelName}/Margin/Stack/ModeRow/{DisplayModeName}");
        var divinationList = layer.GetNodeOrNull<VBoxContainer>($"{PanelName}/Margin/Stack/Scroll/{DivinationListName}");
        var countdownLabel = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/{CountdownName}");
        if (destinyValue is null || omenLabel is null || divinationLabel is null || displayMode is null ||
            divinationList is null || countdownLabel is null)
        {
            return;
        }

        destinyValue.Text = snapshot.Destiny.ToString();
        omenLabel.Text = snapshot.OmenLabel;
        omenLabel.AddThemeColorOverride("font_color", GetOmenColor(snapshot.Destiny));
        var records = GetDisplayedRecords(player);
        int visibleCount = records.Count;
        int totalCount = DivinationService.GetRecords(player).Count;
        divinationLabel.Text = DivinerLoc.Text(
            $"Divinations: {visibleCount}/{totalCount}",
            $"占卜：{visibleCount}/{totalCount}");
        RefreshDisplayModeLabels(displayMode);
        RefreshDivinationList(divinationList, records);
        countdownLabel.Text = DivinerCombatRuntime.HasActiveDredgeCountdownFor(player)
            ? DivinerLoc.Text(
                $"Countdown of Destiny: {DivinerCombatRuntime.DredgeCountdownFor(player)}",
                $"命运倒计时：{DivinerCombatRuntime.DredgeCountdownFor(player)}")
            : "";
        countdownLabel.Visible = DivinerCombatRuntime.HasActiveDredgeCountdownFor(player);

        RefreshPips(layer, snapshot.Destiny);
    }

    private static Player? GetTrackedDivinerPlayer()
    {
        var observed = DivinerCombatRuntime.GetLastObservedPlayer();
        if (observed != null && DivinerPlayerDetection.IsDivinerPlayer(observed))
        {
            return observed;
        }

        return RunManager.Instance?.DebugOnlyGetState()?.Players
            .FirstOrDefault(DivinerPlayerDetection.IsDivinerPlayer);
    }

    private static void BuildHudRoot(CanvasLayer layer)
    {
        var panel = new PanelContainer
        {
            Name = PanelName,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TopLevel = true,
            Position = new Vector2(26, 156),
            Size = new Vector2(456, 424),
            ZIndex = 1000,
            CustomMinimumSize = new Vector2(456, 424)
        };
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
        layer.AddChild(panel);

        var margin = new MarginContainer { Name = "Margin" };
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var stack = new VBoxContainer
        {
            Name = "Stack",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 5);
        margin.AddChild(stack);

        var header = new HBoxContainer
        {
            Name = "Header",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        header.AddThemeConstantOverride("separation", 10);
        stack.AddChild(header);

        var title = new Label
        {
            Text = DivinerLoc.Text("Destiny", "命运"),
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        title.AddThemeColorOverride("font_color", TextColor);
        title.AddThemeFontSizeOverride("font_size", 22);
        header.AddChild(title);

        var destinyValue = new Label
        {
            Name = DestinyValueName,
            Text = "3",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(42, 44)
        };
        destinyValue.AddThemeColorOverride("font_color", TextColor);
        destinyValue.AddThemeFontSizeOverride("font_size", 38);
        header.AddChild(destinyValue);

        var omenLabel = new Label
        {
            Name = OmenName,
            Text = DivinerLoc.Text("Good Omen", "吉兆"),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(126, 32)
        };
        omenLabel.AddThemeFontSizeOverride("font_size", 17);
        header.AddChild(omenLabel);

        var pips = new HBoxContainer
        {
            Name = PipsName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        pips.AddThemeConstantOverride("separation", 5);
        stack.AddChild(pips);

        for (int i = 0; i <= DestinyConstants.MaxDestiny; i++)
        {
            var pip = new PanelContainer
            {
                Name = $"Pip{i}",
                MouseFilter = Control.MouseFilterEnum.Stop,
                CustomMinimumSize = new Vector2(46, 12),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            pip.AddThemeStyleboxOverride("panel", CreatePipStyle(EmptyPipColor));
            pips.AddChild(pip);
        }

        stack.AddChild(CreateStatusLabel(DivinationName, DivinerLoc.Text("Divinations: 0", "占卜：0"), TextColor));

        var modeRow = new HBoxContainer
        {
            Name = "ModeRow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        modeRow.AddThemeConstantOverride("separation", 8);
        stack.AddChild(modeRow);

        var modeLabel = new Label
        {
            Text = DivinerLoc.Text("Mode", "模式"),
            VerticalAlignment = VerticalAlignment.Center
        };
        modeLabel.AddThemeColorOverride("font_color", TextColor);
        modeLabel.AddThemeFontSizeOverride("font_size", 15);
        modeRow.AddChild(modeLabel);

        var displayMode = new OptionButton
        {
            Name = DisplayModeName,
            Selected = (int)_displayMode,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        displayMode.AddThemeFontSizeOverride("font_size", 15);
        displayMode.AddItem(DivinerLoc.Text("Default", "默认"), (int)DivinationDisplayMode.Default);
        displayMode.AddItem(DivinerLoc.Text("List", "列表"), (int)DivinationDisplayMode.List);
        displayMode.AddItem(DivinerLoc.Text("List all", "完整列表"), (int)DivinationDisplayMode.ListAll);
        displayMode.ItemSelected += index =>
        {
            _displayMode = (DivinationDisplayMode)(int)index;
            _lastRenderedDivinationSignature = "";
            RefreshIfMounted();
        };
        modeRow.AddChild(displayMode);

        var scroll = new ScrollContainer
        {
            Name = "Scroll",
            CustomMinimumSize = new Vector2(418, 250),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        stack.AddChild(scroll);

        var divinationList = new VBoxContainer
        {
            Name = DivinationListName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        divinationList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(divinationList);

        var countdownLabel = CreateStatusLabel(CountdownName, "", BadOmenColor);
        countdownLabel.Visible = false;
        stack.AddChild(countdownLabel);
    }

    private static void AddRefreshTimer(CanvasLayer layer)
    {
        var timer = new Godot.Timer
        {
            Name = TimerName,
            WaitTime = 0.35,
            Autostart = true,
            OneShot = false,
            ProcessCallback = Godot.Timer.TimerProcessCallback.Idle
        };
        timer.Timeout += RefreshIfMounted;
        layer.AddChild(timer);
    }

    private static void RefreshPips(CanvasLayer layer, int destiny)
    {
        var pips = layer.GetNodeOrNull<HBoxContainer>($"{PanelName}/Margin/Stack/{PipsName}");
        if (pips == null)
        {
            return;
        }

        var tooltip = DestinyRewardTuning.DescribeLuckBlock(destiny);
        pips.TooltipText = tooltip;

        for (int i = 0; i <= DestinyConstants.MaxDestiny; i++)
        {
            if (pips.GetNodeOrNull<PanelContainer>($"Pip{i}") is not { } pip)
            {
                continue;
            }

            pip.TooltipText = tooltip;

            pip.AddThemeStyleboxOverride(
                "panel",
                CreatePipStyle(i <= destiny ? GetOmenColor(i) : EmptyPipColor)
            );
        }
    }

    private static IReadOnlyList<DivinationRecord> GetDisplayedRecords(Player player)
    {
        return _displayMode == DivinationDisplayMode.ListAll
            ? DivinationService.GetVisibleRecords(player, false)
            : DivinationService.GetVisibleRecords(player, true);
    }

    private static void RefreshDisplayModeLabels(OptionButton displayMode)
    {
        displayMode.SetItemText(0, DivinerLoc.Text("Default", "默认"));
        displayMode.SetItemText(1, DivinerLoc.Text("List", "列表"));
        displayMode.SetItemText(2, DivinerLoc.Text("List all", "完整列表"));
        if (displayMode.Selected != (int)_displayMode)
        {
            displayMode.Selected = (int)_displayMode;
        }
    }

    private static void RefreshDivinationList(VBoxContainer list, IReadOnlyList<DivinationRecord> records)
    {
        var signature = BuildDivinationListSignature(records);
        if (signature == _lastRenderedDivinationSignature && list.GetChildCount() > 0)
        {
            return;
        }

        _lastRenderedDivinationSignature = signature;
        foreach (var child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }

        if (records.Count == 0)
        {
            list.AddChild(CreateRecordLabel(
                _displayMode == DivinationDisplayMode.ListAll
                    ? DivinerLoc.Text("No divinations recorded.", "没有占卜记录。")
                    : DivinerLoc.Text("No active divinations recorded.", "没有有效的占卜记录。"),
                MutedTextColor));
            return;
        }

        if (_displayMode == DivinationDisplayMode.Default)
        {
            AddGroupedDivinationRows(list, records);
            return;
        }

        foreach (var record in records)
        {
            list.AddChild(CreateRecordRow(record));
        }
    }

    private static void AddGroupedDivinationRows(VBoxContainer list, IReadOnlyList<DivinationRecord> records)
    {
        foreach (var group in records
                     .GroupBy(record => GetUiGroup(record.Category), StringComparer.Ordinal)
                     .OrderBy(group => GetUiGroupSortOrder(group.Key)))
        {
            list.AddChild(CreateGroupHeader(GetUiGroupLabel(group.Key)));
            if (group.Key == "Relic")
            {
                AddGroupedRelicRows(list, group.ToList());
                continue;
            }

            foreach (var record in group)
            {
                list.AddChild(CreateGroupedTextRow(record));
            }
        }
    }

    private static void AddGroupedRelicRows(VBoxContainer list, IReadOnlyList<DivinationRecord> records)
    {
        foreach (var group in records
                     .GroupBy(record => record.Category, StringComparer.Ordinal)
                     .OrderBy(group => GetRelicCategorySortOrder(group.Key)))
        {
            var relicIds = group.SelectMany(record => record.GetPreviewRelicIds()).Distinct().ToList();
            if (relicIds.Count == 0)
            {
                foreach (var record in group)
                {
                    list.AddChild(CreateGroupedTextRow(record));
                }

                continue;
            }

            list.AddChild(CreateRelicArrayRow(group.Key, relicIds));
        }
    }

    private static Control CreateGroupHeader(string text)
    {
        var label = CreateStatusLabel($"Group{Guid.NewGuid():N}", text, TextColor);
        label.AddThemeFontSizeOverride("font_size", 17);
        return label;
    }

    private static Control CreateGroupedTextRow(DivinationRecord record)
    {
        var relicIds = record.GetPreviewRelicIds().ToList();
        var labelText = $"{GetCategoryShortLabel(record.Category)}: {ExtractForecastValue(record.Text)}";
        if (relicIds.Count == 0)
        {
            return CreateRecordLabel(labelText, MutedTextColor);
        }

        var row = new HBoxContainer
        {
            Name = $"GroupedRecordRow{Guid.NewGuid():N}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);

        var label = CreateRecordLabel(labelText, MutedTextColor);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        foreach (var relicId in relicIds)
        {
            var relic = ModelDb.GetByIdOrNull<RelicModel>(relicId);
            if (relic == null)
            {
                continue;
            }

            row.AddChild(CreateRelicPreview(relic));
        }

        return row;
    }

    private static Control CreateRelicArrayRow(string category, IReadOnlyList<ModelId> relicIds)
    {
        var row = new HBoxContainer
        {
            Name = $"RelicArrayRow{Guid.NewGuid():N}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);

        var relics = relicIds
            .Select(ModelDb.GetByIdOrNull<RelicModel>)
            .Where(relic => relic != null)
            .Cast<RelicModel>()
            .ToList();
        var names = relics.Count > 0
            ? string.Join("  ", relics.Select(relic => BuildRelicDisplayName(relic)))
            : DivinerLoc.Text("Unknown", "未知");
        var label = CreateRecordLabel($"{GetRelicCategoryLabel(category)}: {names}", MutedTextColor);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        foreach (var relic in relics)
        {
            row.AddChild(CreateRelicPreview(relic));
        }

        return row;
    }

    private static Control CreateRecordRow(DivinationRecord record)
    {
        var row = new HBoxContainer
        {
            Name = $"RecordRow{Guid.NewGuid():N}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);

        var floor = record.Floor > 0 ? $"F{record.Floor}: " : "";
        var suffix = record.IsActive ? "" : DivinerLoc.Text(" [inactive]", " [已失效]");
        var label = CreateRecordLabel($"{floor}{record.Text}{suffix}", record.IsActive ? MutedTextColor : EmptyPipColor);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        foreach (var relicId in record.GetPreviewRelicIds())
        {
            var relic = ModelDb.GetByIdOrNull<RelicModel>(relicId);
            if (relic == null)
            {
                continue;
            }

            row.AddChild(CreateRelicPreview(relic));
        }

        return row;
    }

    private static string BuildDivinationListSignature(IReadOnlyList<DivinationRecord> records)
    {
        return string.Join(
            "|",
            records.Select(record =>
                $"{_displayMode}:{record.Category}:{record.Text}:{record.Floor}:{record.PreviewRelicIds}:{record.IsActive}"));
    }

    private static Control CreateRelicPreview(RelicModel relic)
    {
        var wrapper = new Control
        {
            Name = $"RelicPreview{Guid.NewGuid():N}",
            CustomMinimumSize = new Vector2(58, 54),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            ClipContents = false
        };
        wrapper.TooltipText = BuildRelicTooltipText(relic);

        var holder = NRelicBasicHolder.Create(relic);
        if (holder == null)
        {
            return wrapper;
        }

        holder.Position = new Vector2(3, 0);
        holder.Scale = new Vector2(0.56f, 0.56f);
        wrapper.AddChild(holder);
        return wrapper;
    }

    private static string BuildRelicTooltipText(RelicModel relic)
    {
        return BuildRelicDisplayName(relic);
    }

    private static string BuildRelicDisplayName(RelicModel relic)
    {
        return SafeFormat(relic.Title) ?? DivinerLoc.Text("Relic", "遗物");
    }

    private static Label CreateRecordLabel(string text, Color color)
    {
        var label = CreateStatusLabel($"Record{Guid.NewGuid():N}", text, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(306, 0);
        return label;
    }

    private static Label CreateStatusLabel(string name, string text, Color color)
    {
        var label = new Label
        {
            Name = name,
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 16);
        return label;
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = PanelColor,
            BorderColor = BorderColor,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        return style;
    }

    private static StyleBoxFlat CreatePipStyle(Color color)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color
        };
        style.SetCornerRadiusAll(4);
        return style;
    }

    private static Color GetOmenColor(int destiny)
    {
        return destiny switch
        {
            <= 2 => BadOmenColor,
            >= 3 => GoodOmenColor
        };
    }

    private static string GetUiGroup(string category)
    {
        var dotIndex = category.IndexOf('.', StringComparison.Ordinal);
        return dotIndex > 0 ? category[..dotIndex] : category;
    }

    private static int GetUiGroupSortOrder(string group)
    {
        return group switch
        {
            "Boss" => 0,
            "AncientReward" => 1,
            "Relic" => 2,
            "Event" => 3,
            "Elite" => 4,
            _ => 99
        };
    }

    private static string GetUiGroupLabel(string group)
    {
        return group switch
        {
            "Boss" => DivinerLoc.Text("Boss", "首领"),
            "AncientReward" => DivinerLoc.Text("Ancient", "远古奖励"),
            "Relic" => DivinerLoc.Text("Relic", "遗物"),
            "Event" => DivinerLoc.Text("Event", "事件"),
            "Elite" => DivinerLoc.Text("Elite", "精英"),
            _ => group
        };
    }

    private static string GetCategoryShortLabel(string category)
    {
        return category switch
        {
            "Boss.Act2" => DivinerLoc.Text("Act 2", "第二幕"),
            "Boss.Act3.Primary" => DivinerLoc.Text("Act 3", "第三幕"),
            "Boss.Act3.Second" => DivinerLoc.Text("Act 3 second", "第三幕第二首领"),
            "AncientReward.Act2" => DivinerLoc.Text("Act 2", "第二幕"),
            "AncientReward.Act3" => DivinerLoc.Text("Act 3", "第三幕"),
            "Event.Committed" => DivinerLoc.Text("Current", "当前"),
            "Elite.CurrentAct" => DivinerLoc.Text("Current act", "当前幕"),
            _ => category
        };
    }

    private static int GetRelicCategorySortOrder(string category)
    {
        return category switch
        {
            "Relic.Common" => 0,
            "Relic.Uncommon" => 1,
            "Relic.Rare" => 2,
            "Relic.Shop" => 3,
            "Relic.ShopCommon" => 4,
            "Relic.ShopUncommon" => 5,
            "Relic.ShopRare" => 6,
            _ => 99
        };
    }

    private static string GetRelicCategoryLabel(string category)
    {
        return category switch
        {
            "Relic.Common" => DivinerLoc.Text("Common", "普通"),
            "Relic.Uncommon" => DivinerLoc.Text("Uncommon", "罕见"),
            "Relic.Rare" => DivinerLoc.Text("Rare", "稀有"),
            "Relic.Shop" => DivinerLoc.Text("Shop", "商店"),
            "Relic.ShopCommon" => DivinerLoc.Text("Shop Common", "商店普通"),
            "Relic.ShopUncommon" => DivinerLoc.Text("Shop Uncommon", "商店罕见"),
            "Relic.ShopRare" => DivinerLoc.Text("Shop Rare", "商店稀有"),
            _ => category
        };
    }

    private static string ExtractForecastValue(string text)
    {
        var colonIndex = text.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex < 0)
        {
            colonIndex = text.IndexOf('：');
        }

        var value = colonIndex >= 0 && colonIndex + 1 < text.Length
            ? text[(colonIndex + 1)..]
            : text;
        return value.Trim().TrimEnd('.', '。');
    }

    private static string? SafeFormat(MegaCrit.Sts2.Core.Localization.LocString? locString)
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

    private enum DivinationDisplayMode
    {
        Default = 0,
        List = 1,
        ListAll = 2
    }
}
