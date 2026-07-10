using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;
using Godot;

namespace Diviner.DivinerCode.Relics;

[Pool(typeof(DivinerRelicPool))]
public abstract class DivinerRelic : CustomRelicModel
{
    public override string PackedIconPath => ExistingIconPath(big: false, outline: false);

    protected override string PackedIconOutlinePath => ExistingIconPath(big: false, outline: true);

    protected override string BigIconPath => ExistingIconPath(big: true, outline: false);

    private string ExistingIconPath(bool big, bool outline)
    {
        var slug = Id.Entry.RemovePrefix().ToAssetSlug();
        var iconName = outline ? $"{slug}_outline.png" : $"{slug}.png";
        var iconPath = big ? iconName.BigRelicImagePath() : iconName.RelicImagePath();
        if (ResourceLoader.Exists(iconPath))
        {
            return iconPath;
        }

        if (outline)
        {
            var plainIconPath = $"{slug}.png".RelicImagePath();
            if (ResourceLoader.Exists(plainIconPath))
            {
                return plainIconPath;
            }
        }

        return "relic_outline.png".RelicImagePath();
    }
}
