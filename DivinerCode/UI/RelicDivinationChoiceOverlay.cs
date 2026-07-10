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

    public static Task<ModelId?> ChooseRelic(IReadOnlyList<ModelId> relicIds, bool allowSkip = true)
    {
        if (relicIds.Count == 0 || Engine.GetMainLoop() is not SceneTree tree)
        {
            return Task.FromResult<ModelId?>(null);
        }

        tree.Root.GetNodeOrNull<CanvasLayer>(OverlayName)?.QueueFree();

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
            Size = new Vector2(560, 280),
            Position = new Vector2(360, 210),
            CustomMinimumSize = new Vector2(560, 280)
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

        var relicRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        relicRow.AddThemeConstantOverride("separation", 12);
        stack.AddChild(relicRow);

        foreach (var relicId in relicIds)
        {
            var relic = ModelDb.GetByIdOrNull<RelicModel>(relicId);
            if (relic == null)
            {
                continue;
            }

            relicRow.AddChild(CreateRelicButton(relic, relicId, Complete));
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

    private static Control CreateRelicButton(RelicModel relic, ModelId relicId, Action<ModelId?> complete)
    {
        var button = new Button
        {
            TooltipText = relic.Title.GetFormattedText(),
            CustomMinimumSize = new Vector2(86, 78),
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
