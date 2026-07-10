using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;

namespace Diviner.DivinerCode.Relics;

[Pool(typeof(DivinerRelicPool))]
public abstract class DivinerRelic : CustomRelicModel
{
    public override string PackedIconPath =>
        $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(Id.Entry.RemovePrefix())}.png".RelicImagePath();

    protected override string PackedIconOutlinePath =>
        $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(Id.Entry.RemovePrefix())}_outline.png".RelicImagePath();

    protected override string BigIconPath =>
        $"{Diviner.DivinerCode.Extensions.StringExtensions.ToSnakeCase(Id.Entry.RemovePrefix())}.png".BigRelicImagePath();
}
