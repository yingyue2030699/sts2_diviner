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
        var iconName = $"{BuildIconSlug()}.png";
        var iconPath = big ? iconName.BigPowerImagePath() : iconName.PowerImagePath();
        if (ResourceLoader.Exists(iconPath))
        {
            return iconPath;
        }

        return big ? "power.png".BigPowerImagePath() : "power.png".PowerImagePath();
    }

    private string BuildIconSlug()
    {
        var powerName = Id.Entry.RemovePrefix();
        if (powerName.EndsWith("_POWER", StringComparison.OrdinalIgnoreCase))
        {
            powerName = powerName[..^"_POWER".Length];
        }
        else if (powerName.EndsWith("-POWER", StringComparison.OrdinalIgnoreCase))
        {
            powerName = powerName[..^"-POWER".Length];
        }
        else if (powerName.EndsWith("Power", StringComparison.Ordinal))
        {
            powerName = powerName[..^"Power".Length];
        }

        return powerName.ToAssetSlug();
    }
}
