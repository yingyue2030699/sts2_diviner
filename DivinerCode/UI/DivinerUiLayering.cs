using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace Diviner.DivinerCode.UI;

internal static class DivinerUiLayering
{
    public static Control CreateOverlayRoot(string name)
    {
        var root = new Control
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return root;
    }

    public static void MountBelowHoverTips(Control overlay, SceneTree tree)
    {
        var hoverTips = NGame.Instance?.HoverTipsContainer;
        var parent = hoverTips?.GetParent();
        if (parent == null)
        {
            tree.Root.AddChild(overlay);
            return;
        }

        parent.AddChild(overlay);
        if (GodotObject.IsInstanceValid(hoverTips) && hoverTips.GetParent() == parent)
        {
            parent.MoveChild(overlay, hoverTips.GetIndex());
        }
    }
}
