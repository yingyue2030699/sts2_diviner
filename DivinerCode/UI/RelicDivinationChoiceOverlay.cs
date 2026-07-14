using Diviner.DivinerCode.Localization;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace Diviner.DivinerCode.UI;

public static class RelicDivinationChoiceOverlay
{
    private const string OverlayName = "DivinerRelicDivinationChoiceOverlay";

    private static readonly Color PanelColor = new("111522f2");
    private static readonly Color BorderColor = new("c7b7ffcc");
    private static readonly Color TextColor = new("f2f0ff");
    private const float RelicButtonWidth = 86f;
    private const float RelicButtonHeight = 78f;
    private const float RelicSeparation = 12f;
    private const float HorizontalScreenPadding = 80f;
    private const float VerticalScreenPadding = 80f;

    public static Task<ModelId?> ChooseRelic(IReadOnlyList<ModelId> relicIds, bool allowSkip = true)
    {
        if (relicIds.Count == 0 || Engine.GetMainLoop() is not SceneTree tree)
        {
            return Task.FromResult<ModelId?>(null);
        }

        tree.Root.GetNodeOrNull<CanvasLayer>(OverlayName)?.QueueFree();

        var relics = relicIds
            .Select(relicId => (Relic: ModelDb.GetByIdOrNull<RelicModel>(relicId), Id: relicId))
            .Where(entry => entry.Relic != null)
            .ToList();
        if (relics.Count == 0)
        {
            return Task.FromResult<ModelId?>(null);
        }

        var viewportSize = tree.Root.GetVisibleRect().Size;
        var layout = CalculateLayout(viewportSize, relics.Count, allowSkip);

        var completion = new TaskCompletionSource<ModelId?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var layer = new CanvasLayer
        {
            Name = OverlayName,
            Layer = 256
        };
        layer.TreeExiting += () => completion.TrySetResult(null);

        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            TopLevel = true,
            Size = layout.PanelSize,
            Position = layout.PanelPosition,
            CustomMinimumSize = layout.PanelSize
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
        layer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 12);
        margin.AddChild(stack);

        var title = new Label
        {
            Text = DivinerLoc.Text("Choose a foretold relic to remove", "选择一件预示遗物移出序列"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeColorOverride("font_color", TextColor);
        title.AddThemeFontSizeOverride("font_size", 20);
        stack.AddChild(title);

        var relicScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(layout.GridWidth, layout.GridVisibleHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = layout.GridVisibleHeight < layout.GridHeight
                ? ScrollContainer.ScrollMode.Auto
                : ScrollContainer.ScrollMode.Disabled
        };
        stack.AddChild(relicScroll);

        var relicGrid = new GridContainer
        {
            Columns = layout.Columns,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        relicGrid.AddThemeConstantOverride("h_separation", (int)RelicSeparation);
        relicGrid.AddThemeConstantOverride("v_separation", (int)RelicSeparation);
        relicScroll.AddChild(relicGrid);

        foreach (var (relic, relicId) in relics)
        {
            relicGrid.AddChild(CreateRelicButton(relic!, relicId, Complete));
        }

        if (allowSkip)
        {
            var skip = new Button
            {
                Text = DivinerLoc.Text("Skip", "跳过"),
                CustomMinimumSize = new Vector2(120, 44),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            skip.Pressed += () => Complete(null);
            stack.AddChild(skip);
        }

        tree.Root.AddChild(layer);
        return completion.Task;

        void Complete(ModelId? chosen)
        {
            if (!completion.TrySetResult(chosen))
            {
                return;
            }

            if (GodotObject.IsInstanceValid(layer))
            {
                layer.QueueFree();
            }
        }
    }

    private static (
        Vector2 PanelSize,
        Vector2 PanelPosition,
        int Columns,
        float GridWidth,
        float GridHeight,
        float GridVisibleHeight) CalculateLayout(
        Vector2 viewportSize,
        int relicCount,
        bool allowSkip)
    {
        var safeViewportWidth = viewportSize.X > 0f ? viewportSize.X : 1280f;
        var safeViewportHeight = viewportSize.Y > 0f ? viewportSize.Y : 720f;
        var maxPanelWidth = Math.Max(360f, safeViewportWidth - HorizontalScreenPadding);
        var maxGridWidth = Math.Max(RelicButtonWidth, maxPanelWidth - 36f);
        var columns = Math.Max(
            1,
            Math.Min(
                relicCount,
                (int)Math.Floor((maxGridWidth + RelicSeparation) / (RelicButtonWidth + RelicSeparation))));
        var rows = Math.Max(1, (int)Math.Ceiling(relicCount / (float)columns));
        var gridWidth = columns * RelicButtonWidth + (columns - 1) * RelicSeparation;
        var gridHeight = rows * RelicButtonHeight + (rows - 1) * RelicSeparation;
        var panelWidth = Math.Max(360f, Math.Min(maxPanelWidth, gridWidth + 36f));
        var nonGridHeight = 14f + 28f + 12f + (allowSkip ? 12f + 44f : 0f) + 14f;
        var maxPanelHeight = Math.Max(240f, safeViewportHeight - VerticalScreenPadding);
        var gridVisibleHeight = Math.Min(gridHeight, Math.Max(RelicButtonHeight, maxPanelHeight - nonGridHeight));
        var panelHeight = Math.Min(maxPanelHeight, nonGridHeight + gridVisibleHeight);
        var panelPosition = new Vector2(
            Math.Max(20f, (safeViewportWidth - panelWidth) / 2f),
            Math.Max(20f, (safeViewportHeight - panelHeight) / 2f));

        return (new Vector2(panelWidth, panelHeight), panelPosition, columns, gridWidth, gridHeight, gridVisibleHeight);
    }

    private static Control CreateRelicButton(RelicModel relic, ModelId relicId, Action<ModelId?> complete)
    {
        var button = new Button
        {
            TooltipText = relic.Title.GetFormattedText(),
            CustomMinimumSize = new Vector2(RelicButtonWidth, RelicButtonHeight),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        button.Pressed += () => complete(relicId);

        var holder = NRelicBasicHolder.Create(relic);
        if (holder != null)
        {
            holder.Position = new Vector2(20, 6);
            holder.Scale = new Vector2(0.72f, 0.72f);
            button.AddChild(holder);
        }

        return button;
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = PanelColor,
            BorderColor = BorderColor
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        return style;
    }
}
