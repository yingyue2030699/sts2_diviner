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
    private const string HideInactiveName = "HideInactive";
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
    private static bool _hideInactive = true;

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
        var snapshot = DestinyService.Snapshot;

        var destinyValue = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/Header/{DestinyValueName}");
        var omenLabel = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/Header/{OmenName}");
        var divinationLabel = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/{DivinationName}");
        var hideInactive = layer.GetNodeOrNull<CheckBox>($"{PanelName}/Margin/Stack/{HideInactiveName}");
        var divinationList = layer.GetNodeOrNull<VBoxContainer>($"{PanelName}/Margin/Stack/Scroll/{DivinationListName}");
        var countdownLabel = layer.GetNodeOrNull<Label>($"{PanelName}/Margin/Stack/{CountdownName}");
        if (destinyValue is null || omenLabel is null || divinationLabel is null || hideInactive is null ||
            divinationList is null || countdownLabel is null)
        {
            return;
        }

        destinyValue.Text = snapshot.Destiny.ToString();
        omenLabel.Text = snapshot.OmenLabel;
        omenLabel.AddThemeColorOverride("font_color", GetOmenColor(snapshot.Destiny));
        int visibleCount = DivinationService.GetVisibleRecords(_hideInactive).Count;
        divinationLabel.Text = DivinerLoc.Text(
            $"Divinations: {visibleCount}/{DivinationService.CurrentRecords.Count}",
            $"占卜：{visibleCount}/{DivinationService.CurrentRecords.Count}");
        hideInactive.Text = DivinerLoc.Text("Hide inactive", "隐藏已失效");
        hideInactive.ButtonPressed = _hideInactive;
        RefreshDivinationList(divinationList);
        countdownLabel.Text = DivinerCombatRuntime.HasActiveDredgeCountdown
            ? DivinerLoc.Text(
                $"Countdown of Destiny: {DivinerCombatRuntime.DredgeCountdown}",
                $"命运倒计时：{DivinerCombatRuntime.DredgeCountdown}")
            : "";
        countdownLabel.Visible = DivinerCombatRuntime.HasActiveDredgeCountdown;

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

        var hideInactive = new CheckBox
        {
            Name = HideInactiveName,
            Text = DivinerLoc.Text("Hide inactive", "隐藏已失效"),
            ButtonPressed = _hideInactive,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        hideInactive.Toggled += pressed =>
        {
            _hideInactive = pressed;
            RefreshIfMounted();
        };
        hideInactive.AddThemeColorOverride("font_color", TextColor);
        hideInactive.AddThemeFontSizeOverride("font_size", 15);
        stack.AddChild(hideInactive);

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

    private static void RefreshDivinationList(VBoxContainer list)
    {
        foreach (var child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }

        var records = DivinationService.GetVisibleRecords(_hideInactive);
        if (records.Count == 0)
        {
            list.AddChild(CreateRecordLabel(
                _hideInactive
                    ? DivinerLoc.Text("No active divinations recorded.", "没有有效的占卜记录。")
                    : DivinerLoc.Text("No divinations recorded.", "没有占卜记录。"),
                MutedTextColor));
            return;
        }

        foreach (var record in records)
        {
            list.AddChild(CreateRecordRow(record));
        }
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

    private static Control CreateRelicPreview(RelicModel relic)
    {
        var wrapper = new Control
        {
            Name = $"RelicPreview{Guid.NewGuid():N}",
            CustomMinimumSize = new Vector2(48, 42),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            ClipContents = true
        };

        var holder = NRelicBasicHolder.Create(relic);
        if (holder == null)
        {
            return wrapper;
        }

        holder.Position = new Vector2(7, 2);
        holder.Scale = new Vector2(0.58f, 0.58f);
        wrapper.AddChild(holder);
        return wrapper;
    }

    private static Label CreateRecordLabel(string text, Color color)
    {
        var label = CreateStatusLabel($"Record{Guid.NewGuid():N}", text, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(346, 0);
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
}
