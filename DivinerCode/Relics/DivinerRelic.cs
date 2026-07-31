using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;
using Godot;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Diviner.DivinerCode.Relics;

[Pool(typeof(DivinerRelicPool))]
public abstract class DivinerRelic : CustomRelicModel
{
    public override string PackedIconPath => ExistingIconPath(big: false, outline: false);

    protected override string PackedIconOutlinePath => ExistingIconPath(big: false, outline: true);

    protected override string BigIconPath => ExistingIconPath(big: true, outline: false);

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            string description = Localization?
                .FirstOrDefault(entry => string.Equals(entry.Item1, "description", StringComparison.Ordinal))
                .Item2 ?? string.Empty;
            var tips = new List<IHoverTip>();

            AddKeywordTip(tips, description, DivinerKeywords.CountdownOfDestiny, "Countdown of Destiny", "命运倒计时");
            AddKeywordTip(tips, description, DivinerKeywords.GoodOmen, "Good Omen", "吉兆");
            AddKeywordTip(tips, description, DivinerKeywords.BadOmen, "Bad Omen", "凶兆");
            AddKeywordTip(tips, description, DivinerKeywords.Enlightenment, "Revelation", "启示");
            AddKeywordTip(tips, description, DivinerKeywords.Dredge, "Doomed", "劫兆");
            AddKeywordTip(tips, description, DivinerKeywords.Destiny, "Destiny", "命运");
            AddKeywordTip(tips, description, DivinerKeywords.Divinate, "Divinate", "Divination", "占卜");
            AddKeywordTip(tips, description, DivinerKeywords.Foretell, "Foretell", "预言");
            AddKeywordTip(tips, description, DivinerKeywords.Fated, "Fated", "注定");
            AddKeywordTip(tips, description, DivinerKeywords.Scry, "Scry", "预见");

            if (ContainsAny(description, "Block", "格挡"))
            {
                tips.Add(HoverTipFactory.Static(StaticHoverTip.Block));
            }

            AddPowerTip<StrengthPower>(tips, description, "Strength", "力量");
            AddPowerTip<DexterityPower>(tips, description, "Dexterity", "敏捷");
            AddPowerTip<WeakPower>(tips, description, "Weak", "虚弱");
            AddPowerTip<VulnerablePower>(tips, description, "Vulnerable", "易伤");
            return tips;
        }
    }

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

    private static void AddKeywordTip(
        ICollection<IHoverTip> tips,
        string description,
        MegaCrit.Sts2.Core.Entities.Cards.CardKeyword keyword,
        params string[] terms)
    {
        if (ContainsAny(description, terms))
        {
            tips.Add(HoverTipFactory.FromKeyword(keyword));
        }
    }

    private static void AddPowerTip<TPower>(ICollection<IHoverTip> tips, string description, params string[] terms)
        where TPower : PowerModel
    {
        if (ContainsAny(description, terms))
        {
            tips.Add(HoverTipFactory.FromPower(ModelDb.Power<TPower>()));
        }
    }

    private static bool ContainsAny(string description, params string[] terms)
    {
        return terms.Any(term => description.Contains(term, StringComparison.Ordinal));
    }
}
