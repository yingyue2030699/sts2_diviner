using Diviner.DivinerCode.Mechanics;
using Godot;

namespace Diviner.DivinerCode.UI;

public static partial class DoomedCountdownOverlay
{
    private const string LayerName = "DivinerDoomedCountdownOverlay";
    private const string RootName = "Root";
    private const string FlameName = "Flame";
    private const string CountName = "Count";
    private const string TimerName = "RefreshTimer";

    private static readonly Vector2 FlameSize = new(132f, 156f);
    private static readonly Color CountColor = new("fff4d8");
    private static readonly Color CountShadowColor = new("300008aa");

    public static void EnsureMounted()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        if (tree.Root.GetNodeOrNull<CanvasLayer>(LayerName) != null)
        {
            return;
        }

        var layer = new CanvasLayer
        {
            Name = LayerName,
            Layer = 190,
            Visible = false
        };

        var root = new Control
        {
            Name = RootName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            Size = GetViewportSize(tree)
        };
        layer.AddChild(root);

        var flame = new DoomedFlameControl
        {
            Name = FlameName,
            Size = FlameSize,
            CustomMinimumSize = FlameSize,
            PivotOffset = FlameSize * 0.5f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(flame);

        var shadow = CreateCountLabel("CountShadow", CountShadowColor, new Vector2(3f, 4f));
        flame.AddChild(shadow);

        var count = CreateCountLabel(CountName, CountColor, Vector2.Zero);
        flame.AddChild(count);

        var timer = new Godot.Timer
        {
            Name = TimerName,
            WaitTime = 0.08,
            Autostart = true,
            OneShot = false,
            ProcessCallback = Godot.Timer.TimerProcessCallback.Idle
        };
        timer.Timeout += RefreshIfMounted;
        layer.AddChild(timer);

        tree.Root.CallDeferred(Node.MethodName.AddChild, layer);
    }

    public static void RefreshIfMounted()
    {
        if (Engine.GetMainLoop() is not SceneTree tree ||
            tree.Root.GetNodeOrNull<CanvasLayer>(LayerName) is not { } layer)
        {
            return;
        }

        Refresh(layer, tree);
    }

    public static void CloseAndDispose()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        tree.Root.GetNodeOrNull<CanvasLayer>(LayerName)?.QueueFree();
    }

    private static void Refresh(CanvasLayer layer, SceneTree tree)
    {
        var root = layer.GetNodeOrNull<Control>(RootName);
        var flame = layer.GetNodeOrNull<DoomedFlameControl>($"{RootName}/{FlameName}");
        var count = layer.GetNodeOrNull<Label>($"{RootName}/{FlameName}/{CountName}");
        var shadow = layer.GetNodeOrNull<Label>($"{RootName}/{FlameName}/CountShadow");
        if (root == null || flame == null || count == null || shadow == null)
        {
            return;
        }

        int countdown = DivinerCombatRuntime.DredgeCountdown ?? 0;
        bool visible = DivinerCombatRuntime.HasActiveDredgeCountdown;
        layer.Visible = visible;
        if (!visible)
        {
            return;
        }

        var viewportSize = GetViewportSize(tree);
        root.Size = viewportSize;

        float danger = Math.Clamp((4f - countdown) / 3f, 0f, 1f);
        float pulse = 0.04f * Mathf.Sin(Time.GetTicksMsec() / 1000f * Mathf.Tau * (1.2f + danger * 1.6f));
        float scale = 0.94f + danger * 0.52f + pulse;
        flame.Scale = new Vector2(scale, scale);
        flame.Position = GetFlamePosition(viewportSize, scale);
        flame.SetDanger(danger);

        count.Text = countdown.ToString();
        shadow.Text = count.Text;
        int fontSize = (int)MathF.Round(42f + danger * 22f);
        count.AddThemeFontSizeOverride("font_size", fontSize);
        shadow.AddThemeFontSizeOverride("font_size", fontSize);
    }

    private static Label CreateCountLabel(string name, Color color, Vector2 offset)
    {
        var label = new Label
        {
            Name = name,
            Position = new Vector2(0f, 39f) + offset,
            Size = new Vector2(FlameSize.X, 78f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 42);
        return label;
    }

    private static Vector2 GetViewportSize(SceneTree tree)
    {
        var size = tree.Root.GetVisibleRect().Size;
        return size.X > 0 && size.Y > 0 ? size : new Vector2(1920, 1080);
    }

    private static Vector2 GetFlamePosition(Vector2 viewportSize, float scale)
    {
        return new Vector2(
            viewportSize.X * 0.28f - FlameSize.X * 0.5f * scale,
            viewportSize.Y * 0.36f - FlameSize.Y * 0.5f * scale);
    }

    private sealed partial class DoomedFlameControl : Control
    {
        private static readonly Vector2[] OuterFlame =
        [
            new(66f, 0f),
            new(93f, 35f),
            new(124f, 72f),
            new(105f, 132f),
            new(66f, 154f),
            new(27f, 132f),
            new(8f, 75f),
            new(39f, 60f),
            new(34f, 25f)
        ];

        private static readonly Vector2[] InnerFlame =
        [
            new(67f, 36f),
            new(89f, 67f),
            new(82f, 119f),
            new(66f, 137f),
            new(48f, 119f),
            new(42f, 76f),
            new(57f, 84f)
        ];

        private static readonly Color[] OuterColors = Enumerable
            .Repeat(new Color("b7162bdd"), OuterFlame.Length)
            .ToArray();

        private static readonly Color[] InnerColors = Enumerable
            .Repeat(new Color("ff8a2fdc"), InnerFlame.Length)
            .ToArray();

        private float _danger;

        public void SetDanger(float danger)
        {
            _danger = Math.Clamp(danger, 0f, 1f);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() / 1000f * Mathf.Tau * (1.8f + _danger));
            DrawCircle(new Vector2(66f, 91f), 54f + _danger * 12f + pulse * 3f, new Color("5b051766"));
            DrawPolygon(OuterFlame, OuterColors);
            DrawPolygon(InnerFlame, InnerColors);
            DrawCircle(new Vector2(66f, 89f), 28f + _danger * 8f, new Color("ffe16eaa"));
            DrawArc(new Vector2(66f, 88f), 58f + _danger * 10f, -1.15f, 4.3f, 44, new Color("ff4050cc"), 4f + _danger * 2f);
        }
    }
}
