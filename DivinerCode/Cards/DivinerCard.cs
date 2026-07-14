using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;
using Diviner.DivinerCode.Mechanics;
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

    protected override bool ShouldGlowGoldInternal => base.ShouldGlowGoldInternal || ShouldGlowForDivinerCondition();

    protected void WithDivinerKeywordTips(params CardKeyword[] keywords)
    {
        WithKeywords(keywords);
    }

    protected void WithEnergyCostX()
    {
        MockSetEnergyCost(new CardEnergyCost(this, 0, true));
    }

    public virtual bool ShouldGlowForDivinerCondition()
    {
        if (Owner == null)
        {
            return false;
        }

        if (!DestinyService.CanUseDestiny(Owner))
        {
            return false;
        }

        var description = Description.GetFormattedText();
        return (HasKeywordOrText(DivinerKeywords.GoodOmen, description, "Good Omen", "吉兆") && DestinyService.IsGoodOmen()) ||
               (HasKeywordOrText(DivinerKeywords.BadOmen, description, "Bad Omen", "凶兆") && DestinyService.IsBadOmen()) ||
               (HasKeywordOrText(DivinerKeywords.Dredge, description, "Doomed", "劫兆") && DestinyService.CurrentDestiny == DestinyConstants.MinDestiny) ||
               (HasKeywordOrText(DivinerKeywords.Enlightenment, description, "Revelation", "启示") && DivinerCombatRuntime.CanTriggerRevelationEffect(Owner));
    }

    private bool HasKeywordOrText(CardKeyword keyword, string description, string englishText, string chineseText)
    {
        return CanonicalKeywords.Contains(keyword) ||
               description.Contains(englishText, StringComparison.Ordinal) ||
               description.Contains(chineseText, StringComparison.Ordinal);
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
