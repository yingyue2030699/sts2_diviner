using BaseLib.Abstracts;
using BaseLib.Extensions;
using Diviner.DivinerCode.Extensions;
using Godot;

namespace Diviner.DivinerCode.Powers;

public abstract class DivinerPower : CustomPowerModel
{
    public override string CustomPackedIconPath => ExistingIconPath(false);

    public override string CustomBigIconPath => ExistingIconPath(true);

    private string ExistingIconPath(bool big)
    {
        var powerName = Id.Entry.RemovePrefix();
        if (powerName.EndsWith("Power", StringComparison.Ordinal))
        {
            powerName = powerName[..^"Power".Length];
        }

        var iconName = $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(powerName)}.png";
        var iconPath = big ? iconName.BigPowerImagePath() : iconName.PowerImagePath();
        if (ResourceLoader.Exists(iconPath))
        {
            return iconPath;
        }

        return big ? "power.png".BigPowerImagePath() : "power.png".PowerImagePath();
    }
}
