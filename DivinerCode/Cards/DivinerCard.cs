using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Diviner.DivinerCode.Cards;

[Pool(typeof(DivinerCardPool))]
public abstract class DivinerCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ConstructedCardModel(cost, type, rarity, NormalizeTarget(target))
{
    private string PortraitSlug => $"{BuildPortraitSlug()}.png";

    public override string CustomPortraitPath => ExistingPortraitPath(PortraitSlug.BigCardImagePath());

    public override string PortraitPath => ExistingPortraitPath(PortraitSlug.CardImagePath());

    public override string BetaPortraitPath => ExistingPortraitPath($"beta/{PortraitSlug}".CardImagePath());

    protected void WithDivinerKeywordTips(params CardKeyword[] keywords)
    {
        WithKeywords(keywords);
    }

    private static TargetType NormalizeTarget(TargetType target)
    {
        return target == TargetType.TargetedNoCreature ? TargetType.Self : target;
    }

    private static string ExistingPortraitPath(string path)
    {
        return ResourceLoader.Exists(path) ? path : "mod_image.png".ContentPath();
    }

    private string BuildPortraitSlug()
    {
        var raw = Id.Entry.RemovePrefix();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "card";
        }

        return raw.Replace("-", "_", StringComparison.Ordinal).ToLowerInvariant();
    }
}
