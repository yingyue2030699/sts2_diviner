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
    public override string PackedIconPath => ExistingIconPath(
        $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(Id.Entry.RemovePrefix())}.png".RelicImagePath()
    );

    protected override string PackedIconOutlinePath => ExistingIconPath(
        $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(Id.Entry.RemovePrefix())}_outline.png".RelicImagePath()
    );

    protected override string BigIconPath => ExistingIconPath(
        $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(Id.Entry.RemovePrefix())}.png".BigRelicImagePath()
    );

    private static string ExistingIconPath(string path)
    {
        return ResourceLoader.Exists(path) ? path : "mod_image.png".ContentPath();
    }
}
