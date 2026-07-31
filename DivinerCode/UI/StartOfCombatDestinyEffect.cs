using Diviner.DivinerCode.Extensions;
using Diviner.DivinerCode.Localization;
using Godot;

namespace Diviner.DivinerCode.UI;

public static class StartOfCombatDestinyEffect
{
    private const string LayerName = "DivinerStartOfCombatDestinyEffect";
    private const double FrameSeconds = 1.0 / 60.0;

    private static readonly Color DoomedBackdropColor = new("07030dcc");
    private static readonly Color DoomedPanelColor = new("1b101df0");
    private static readonly Color DoomedBorderColor = new("f07278dd");
    private static readonly Color DoomedTextColor = new("fff1f4");
    private static readonly Color DoomedMutedTextColor = new("e6a5b0");

    private static readonly Color RevelationBackdropColor = new("070817bd");
    private static readonly Color RevelationPanelColor = new("111a2ee8");
    private static readonly Color RevelationBorderColor = new("f7d36add");
    private static readonly Color RevelationTextColor = new("fff8d8");
    private static readonly Color RevelationMutedTextColor = new("bfe6ff");
    private static readonly Color RevelationInactivePipColor = new("263149dd");

    public static async Task PlayDoomedEscapeShuffle(int cardCount)
    {
        if (!TryCreateLayer(out var tree, out var layer, out var root, out var viewportSize))
        {
            return;
        }

        try
        {
            int previewCount = Math.Max(1, cardCount);
            var backdrop = CreateBackdrop(viewportSize, DoomedBackdropColor);
            var title = CreateTitleBlock(
                viewportSize,
                DivinerLoc.Text("DOOMED", "劫兆"),
                DivinerLoc.Text(
                    "3 Escape from Destiny cards shuffle into your draw pile and 3 into your discard pile.",
                    "将 3 张逃离命劫洗入抽牌堆，并将 3 张洗入弃牌堆。"),
                DoomedTextColor,
                DoomedMutedTextColor,
                -170);
            var drawPileTarget = CreateDrawPileMarker(viewportSize, DoomedPanelColor, DoomedBorderColor);
            var cards = CreateEscapeCardPreviews(previewCount, viewportSize);

            root.AddChild(backdrop);
            root.AddChild(drawPileTarget);
            root.AddChild(title);
            foreach (var card in cards)
            {
                root.AddChild(card);
            }

            await FadeIn(tree, 0.16f, backdrop, title, drawPileTarget);
            await FanInEscapeCards(tree, cards, viewportSize);
            await ShuffleEscapeCards(tree, cards, viewportSize);
            await FlyCardsToDrawPile(tree, cards, drawPileTarget, viewportSize);
            var fadeItems = new List<CanvasItem> { backdrop, title, drawPileTarget };
            fadeItems.AddRange(cards);
            await FadeOut(tree, 0.18f, fadeItems.ToArray());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Doomed start-of-combat effect failed: {ex}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(layer))
            {
                layer.QueueFree();
            }
        }
    }

    public static async Task PlayRevelation(int cardCount)
    {
        if (!TryCreateLayer(out var tree, out var layer, out var root, out var viewportSize))
        {
            return;
        }

        try
        {
            int previewCount = Math.Clamp(cardCount, 1, 3);
            var backdrop = CreateBackdrop(viewportSize, RevelationBackdropColor);
            var glow = CreateGlow(viewportSize);
            var title = CreateTitleBlock(
                viewportSize,
                DivinerLoc.Text("REVELATION", "启示"),
                DivinerLoc.Text("The future opens. Search for three cards, then lose 1 Destiny.", "未来开启。搜寻三张牌，然后失去 1 点命运。"),
                RevelationTextColor,
                RevelationMutedTextColor,
                -175);
            var pips = CreateRevelationPips(viewportSize);
            var drawPileTarget = CreateDrawPileMarker(viewportSize, RevelationPanelColor, RevelationBorderColor);
            var cards = CreateFatedCardPreviews(previewCount, viewportSize, drawPileTarget.Position);

            root.AddChild(backdrop);
            root.AddChild(glow);
            root.AddChild(drawPileTarget);
            root.AddChild(title);
            root.AddChild(pips.Container);
            foreach (var card in cards)
            {
                root.AddChild(card);
            }

            await FadeIn(tree, 0.15f, backdrop, title, drawPileTarget, glow, pips.Container);
            await LightRevelationPips(tree, pips.Pips);
            await RaiseFatedCards(tree, cards, viewportSize, drawPileTarget.Position);
            await Delay(tree, 0.18);
            var fadeItems = new List<CanvasItem> { backdrop, title, drawPileTarget, glow, pips.Container };
            fadeItems.AddRange(cards);
            await FadeOut(tree, 0.22f, fadeItems.ToArray());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Revelation start-of-combat effect failed: {ex}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(layer))
            {
                layer.QueueFree();
            }
        }
    }

    private static bool TryCreateLayer(
        out SceneTree tree,
        out CanvasLayer layer,
        out Control root,
        out Vector2 viewportSize)
    {
        tree = null!;
        layer = null!;
        root = null!;
        viewportSize = Vector2.Zero;

        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            return false;
        }

        tree = sceneTree;
        tree.Root.GetNodeOrNull<CanvasLayer>(LayerName)?.QueueFree();
        viewportSize = GetViewportSize(tree);
        layer = new CanvasLayer
        {
            Name = LayerName,
            Layer = 270
        };
        root = new Control
        {
            Name = "Root",
            Size = viewportSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false
        };
        layer.AddChild(root);
        tree.Root.AddChild(layer);
        return true;
    }

    private static Vector2 GetViewportSize(SceneTree tree)
    {
        var size = tree.Root.GetVisibleRect().Size;
        return size.X > 0 && size.Y > 0 ? size : new Vector2(1920, 1080);
    }

    private static ColorRect CreateBackdrop(Vector2 viewportSize, Color color)
    {
        var backdrop = new ColorRect
        {
            Name = "Backdrop",
            Size = viewportSize,
            Color = color,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        SetAlpha(backdrop, 0f);
        return backdrop;
    }

    private static VBoxContainer CreateTitleBlock(
        Vector2 viewportSize,
        string titleText,
        string subtitleText,
        Color titleColor,
        Color subtitleColor,
        float yOffset)
    {
        var stack = new VBoxContainer
        {
            Name = "TitleBlock",
            Position = new Vector2(viewportSize.X * 0.5f - 360f, viewportSize.Y * 0.5f + yOffset),
            Size = new Vector2(720f, 130f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        stack.AddThemeConstantOverride("separation", 5);
        SetAlpha(stack, 0f);

        var title = new Label
        {
            Text = titleText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        title.AddThemeColorOverride("font_color", titleColor);
        title.AddThemeFontSizeOverride("font_size", 48);
        stack.AddChild(title);

        var subtitle = new Label
        {
            Text = subtitleText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        subtitle.AddThemeColorOverride("font_color", subtitleColor);
        subtitle.AddThemeFontSizeOverride("font_size", 17);
        stack.AddChild(subtitle);

        return stack;
    }

    private static PanelContainer CreateGlow(Vector2 viewportSize)
    {
        var glow = new PanelContainer
        {
            Name = "RevelationGlow",
            Position = new Vector2(viewportSize.X * 0.5f - 190f, viewportSize.Y * 0.5f - 185f),
            Size = new Vector2(380f, 380f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            PivotOffset = new Vector2(190f, 190f),
            Scale = new Vector2(0.65f, 0.65f)
        };
        glow.AddThemeStyleboxOverride("panel", CreateStyle(new Color("f7d36a30"), new Color("f7d36a80"), 3, 190));
        SetAlpha(glow, 0f);
        return glow;
    }

    private static PanelContainer CreateDrawPileMarker(Vector2 viewportSize, Color panelColor, Color borderColor)
    {
        var marker = new PanelContainer
        {
            Name = "DrawPileTarget",
            Position = GetDrawPileMarkerPosition(viewportSize),
            Size = new Vector2(118f, 150f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        marker.AddThemeStyleboxOverride("panel", CreateStyle(panelColor, borderColor, 2, 7));
        SetAlpha(marker, 0f);

        var stack = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 2);
        marker.AddChild(stack);

        for (int i = 0; i < 3; i++)
        {
            var cardLine = new PanelContainer
            {
                CustomMinimumSize = new Vector2(64f + i * 8f, 8f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
            };
            cardLine.AddThemeStyleboxOverride("panel", CreateStyle(borderColor with { A = 0.65f }, borderColor, 0, 3));
            stack.AddChild(cardLine);
        }

        var label = new Label
        {
            Text = DivinerLoc.Text("Draw Pile", "抽牌堆"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", new Color("fff7e0"));
        label.AddThemeFontSizeOverride("font_size", 15);
        stack.AddChild(label);

        return marker;
    }

    private static List<PanelContainer> CreateEscapeCardPreviews(int count, Vector2 viewportSize)
    {
        var texture = ResourceLoader.Load<Texture2D>("escape_from_destiny.png".CardImagePath());
        var cards = new List<PanelContainer>(count);
        var center = GetCardFanCenter(viewportSize);
        for (int i = 0; i < count; i++)
        {
            var card = CreateCardPreview(
                texture,
                DivinerLoc.Text("Escape from Destiny", "逃离命运"),
                DoomedPanelColor,
                DoomedBorderColor,
                DoomedTextColor);
            card.Position = center + new Vector2(0f, 90f);
            card.Scale = new Vector2(0.72f, 0.72f);
            card.Rotation = Mathf.DegToRad(-8f + i * 8f);
            SetAlpha(card, 0f);
            cards.Add(card);
        }

        return cards;
    }

    private static List<PanelContainer> CreateFatedCardPreviews(int count, Vector2 viewportSize, Vector2 drawPilePosition)
    {
        var cards = new List<PanelContainer>(count);
        var start = drawPilePosition + new Vector2(22f, 28f);
        for (int i = 0; i < count; i++)
        {
            var card = CreateFatedCardPreview();
            card.Position = start;
            card.Scale = new Vector2(0.35f, 0.35f);
            card.Rotation = Mathf.DegToRad(-7f + i * 7f);
            SetAlpha(card, 0f);
            cards.Add(card);
        }

        return cards;
    }

    private static PanelContainer CreateCardPreview(
        Texture2D? texture,
        string title,
        Color panelColor,
        Color borderColor,
        Color textColor)
    {
        var card = new PanelContainer
        {
            Name = $"CardPreview{Guid.NewGuid():N}",
            Size = new Vector2(138f, 196f),
            CustomMinimumSize = new Vector2(138f, 196f),
            PivotOffset = new Vector2(69f, 98f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        card.AddThemeStyleboxOverride("panel", CreateStyle(panelColor, borderColor, 2, 8));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        card.AddChild(margin);

        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 6);
        margin.AddChild(stack);

        var image = new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Vector2(112f, 130f),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        stack.AddChild(image);

        var label = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeColorOverride("font_color", textColor);
        label.AddThemeFontSizeOverride("font_size", 13);
        stack.AddChild(label);

        return card;
    }

    private static PanelContainer CreateFatedCardPreview()
    {
        var card = new PanelContainer
        {
            Name = $"FatedCardPreview{Guid.NewGuid():N}",
            Size = new Vector2(124f, 174f),
            CustomMinimumSize = new Vector2(124f, 174f),
            PivotOffset = new Vector2(62f, 87f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        card.AddThemeStyleboxOverride("panel", CreateStyle(new Color("102541ee"), RevelationBorderColor, 2, 8));

        var stack = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 4);
        card.AddChild(stack);

        var star = new Label
        {
            Text = "*",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        star.AddThemeColorOverride("font_color", RevelationTextColor);
        star.AddThemeFontSizeOverride("font_size", 48);
        stack.AddChild(star);

        var label = new Label
        {
            Text = DivinerLoc.Text("Chosen", "选择"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", RevelationMutedTextColor);
        label.AddThemeFontSizeOverride("font_size", 15);
        stack.AddChild(label);

        return card;
    }

    private static (HBoxContainer Container, List<PanelContainer> Pips) CreateRevelationPips(Vector2 viewportSize)
    {
        var row = new HBoxContainer
        {
            Name = "RevelationPips",
            Position = new Vector2(viewportSize.X * 0.5f - 190f, viewportSize.Y * 0.5f + 78f),
            Size = new Vector2(380f, 22f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 7);
        SetAlpha(row, 0f);

        var pips = new List<PanelContainer>(6);
        for (int i = 0; i <= 5; i++)
        {
            var pip = new PanelContainer
            {
                CustomMinimumSize = new Vector2(56f, 14f),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            pip.AddThemeStyleboxOverride("panel", CreateStyle(RevelationInactivePipColor, RevelationInactivePipColor, 0, 5));
            row.AddChild(pip);
            pips.Add(pip);
        }

        return (row, pips);
    }

    private static async Task FanInEscapeCards(SceneTree tree, IReadOnlyList<PanelContainer> cards, Vector2 viewportSize)
    {
        var starts = cards.Select(card => card.Position).ToList();
        var ends = GetFanPositions(cards.Count, viewportSize);
        await Animate(tree, 0.28f, progress =>
        {
            float eased = EaseOutCubic(progress);
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Position = starts[i].Lerp(ends[i], eased);
                cards[i].Scale = new Vector2(0.72f, 0.72f).Lerp(Vector2.One, eased);
                cards[i].Rotation = Mathf.DegToRad((-12f + i * 12f) * eased);
                SetAlpha(cards[i], eased);
            }
        });
    }

    private static async Task ShuffleEscapeCards(SceneTree tree, IReadOnlyList<PanelContainer> cards, Vector2 viewportSize)
    {
        var center = GetCardFanCenter(viewportSize);
        await Animate(tree, 0.58f, progress =>
        {
            float fadeTowardStack = EaseInOut(progress);
            for (int i = 0; i < cards.Count; i++)
            {
                float indexOffset = i - (cards.Count - 1f) * 0.5f;
                float angle = progress * Mathf.Pi * (2.2f + i * 0.25f) + i * 1.95f;
                var orbit = new Vector2(Mathf.Cos(angle) * 82f, Mathf.Sin(angle) * 34f);
                var orderedOffset = new Vector2(indexOffset * 72f * (1f - fadeTowardStack), Math.Abs(indexOffset) * 10f);
                cards[i].Position = center + orbit + orderedOffset;
                cards[i].Rotation = angle * 0.25f;
                cards[i].Scale = Vector2.One.Lerp(new Vector2(0.9f, 0.9f), fadeTowardStack);
            }
        });
    }

    private static async Task FlyCardsToDrawPile(
        SceneTree tree,
        IReadOnlyList<PanelContainer> cards,
        PanelContainer drawPileTarget,
        Vector2 viewportSize)
    {
        var starts = cards.Select(card => card.Position).ToList();
        var target = drawPileTarget.Position + new Vector2(24f, 18f);
        await Animate(tree, 0.62f, progress =>
        {
            float eased = EaseInCubic(progress);
            PulseDrawPile(drawPileTarget, progress, DoomedBorderColor);
            for (int i = 0; i < cards.Count; i++)
            {
                var offset = new Vector2(i * 4f, i * -5f);
                cards[i].Position = starts[i].Lerp(target + offset, eased);
                cards[i].Rotation += 0.13f;
                cards[i].Scale = new Vector2(0.9f, 0.9f).Lerp(new Vector2(0.24f, 0.24f), eased);
                SetAlpha(cards[i], 1f - eased * 0.75f);
            }
        });
    }

    private static async Task LightRevelationPips(SceneTree tree, IReadOnlyList<PanelContainer> pips)
    {
        for (int i = 0; i < pips.Count; i++)
        {
            pips[i].AddThemeStyleboxOverride("panel", CreateStyle(RevelationBorderColor, RevelationTextColor, 1, 5));
            await Delay(tree, 0.055);
        }
    }

    private static async Task RaiseFatedCards(
        SceneTree tree,
        IReadOnlyList<PanelContainer> cards,
        Vector2 viewportSize,
        Vector2 drawPilePosition)
    {
        var start = drawPilePosition + new Vector2(22f, 28f);
        var ends = GetFatedCardPositions(cards.Count, viewportSize);
        await Animate(tree, 0.78f, progress =>
        {
            for (int i = 0; i < cards.Count; i++)
            {
                float local = Math.Clamp((progress - i * 0.09f) / 0.72f, 0f, 1f);
                float eased = EaseOutCubic(local);
                cards[i].Position = start.Lerp(ends[i], eased);
                cards[i].Scale = new Vector2(0.35f, 0.35f).Lerp(new Vector2(0.82f, 0.82f), eased);
                cards[i].Rotation = Mathf.DegToRad(-9f + i * 9f) * eased;
                SetAlpha(cards[i], eased);
            }
        });
    }

    private static async Task FadeIn(SceneTree tree, float duration, params CanvasItem[] items)
    {
        await Animate(tree, duration, progress =>
        {
            float eased = EaseOutCubic(progress);
            foreach (var item in items)
            {
                SetAlpha(item, eased);
            }
        });
    }

    private static async Task FadeOut(SceneTree tree, float duration, params CanvasItem[] items)
    {
        await Animate(tree, duration, progress =>
        {
            float eased = EaseInCubic(progress);
            foreach (var item in items)
            {
                SetAlpha(item, 1f - eased);
            }
        });
    }

    private static async Task Animate(SceneTree tree, float duration, Action<float> applyFrame)
    {
        int frames = Math.Max(1, (int)Math.Ceiling(duration / FrameSeconds));
        for (int frame = 0; frame <= frames; frame++)
        {
            applyFrame(Math.Clamp(frame / (float)frames, 0f, 1f));
            await Delay(tree, FrameSeconds);
        }
    }

    private static Task Delay(SceneTree tree, double seconds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timer = tree.CreateTimer(seconds);
        timer.Timeout += () => completion.TrySetResult();
        return completion.Task;
    }

    private static void PulseDrawPile(PanelContainer drawPileTarget, float progress, Color borderColor)
    {
        float pulse = 0.55f + Mathf.Sin(progress * Mathf.Pi * 6f) * 0.18f;
        drawPileTarget.AddThemeStyleboxOverride(
            "panel",
            CreateStyle(DoomedPanelColor, borderColor with { A = pulse }, 2, 7));
    }

    private static List<Vector2> GetFanPositions(int count, Vector2 viewportSize)
    {
        var center = GetCardFanCenter(viewportSize);
        var positions = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1f) * 0.5f) * 92f;
            positions.Add(center + new Vector2(offset, Math.Abs(offset) * 0.08f));
        }

        return positions;
    }

    private static List<Vector2> GetFatedCardPositions(int count, Vector2 viewportSize)
    {
        var center = new Vector2(viewportSize.X * 0.5f - 62f, viewportSize.Y * 0.5f - 58f);
        var positions = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1f) * 0.5f) * 82f;
            positions.Add(center + new Vector2(offset, 22f - Math.Abs(offset) * 0.08f));
        }

        return positions;
    }

    private static Vector2 GetCardFanCenter(Vector2 viewportSize)
    {
        return new Vector2(viewportSize.X * 0.5f - 69f, viewportSize.Y * 0.5f - 70f);
    }

    private static Vector2 GetDrawPileMarkerPosition(Vector2 viewportSize)
    {
        return new Vector2(Math.Max(28f, viewportSize.X * 0.075f), Math.Max(80f, viewportSize.Y - 205f));
    }

    private static void SetAlpha(CanvasItem item, float alpha)
    {
        var color = item.Modulate;
        color.A = Math.Clamp(alpha, 0f, 1f);
        item.Modulate = color;
    }

    private static float EaseOutCubic(float value)
    {
        return 1f - Mathf.Pow(1f - Math.Clamp(value, 0f, 1f), 3f);
    }

    private static float EaseInCubic(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * value;
    }

    private static float EaseInOut(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value < 0.5f
            ? 4f * value * value * value
            : 1f - Mathf.Pow(-2f * value + 2f, 3f) * 0.5f;
    }

    private static StyleBoxFlat CreateStyle(Color background, Color border, int borderWidth, int radius)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border
        };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(radius);
        return style;
    }
}
