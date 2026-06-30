using Godot;
using HarmonyLib;
using BaseLib.Patches.Localization;
using Diviner.DivinerCode.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using System.Text.RegularExpressions;

namespace Diviner.DivinerCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Diviner";

    private static readonly Regex[] KeywordHighlightRules = BuildKeywordHighlightRules();

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private static bool _descriptionHandlerRegistered;

    public static void Initialize()
    {
        Logger.Info("Initializing Diviner.");

        SimpleLoc.EnableSimpleLoc(ModId);
        RegisterDescriptionOverrides();

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }

    private static void RegisterDescriptionOverrides()
    {
        if (_descriptionHandlerRegistered)
        {
            return;
        }

        _descriptionHandlerRegistered = true;
        DescriptionOverrides.CustomizeDescription += CustomizeDivinerCardDescription;
    }

    private static void CustomizeDivinerCardDescription(CardModel card, Creature? target, ref string description)
    {
        if (card is not DivinerCard)
        {
            return;
        }

        UseUpgradedCardDescription(card, ref description);
        description = SimpleLoc.TrySimplify("#" + HighlightKeywordTerms(description));
    }

    private static void UseUpgradedCardDescription(CardModel card, ref string description)
    {
        if (!card.IsUpgraded)
        {
            return;
        }

        var baseDescription = card.Description.GetFormattedText();
        if (!string.Equals(description, baseDescription, StringComparison.Ordinal))
        {
            return;
        }

        var upgradedDescriptionKey = $"{card.Id.Entry}.upgradedDesc";
        if (LocString.Exists("cards", upgradedDescriptionKey))
        {
            description = new LocString("cards", upgradedDescriptionKey).GetFormattedText();
        }
    }

    private static string HighlightKeywordTerms(string description)
    {
        foreach (var pattern in KeywordHighlightRules)
        {
            description = ReplaceOutsideGoldHighlights(description, pattern);
        }

        return description;
    }

    private static string ReplaceOutsideGoldHighlights(string description, Regex pattern)
    {
        var segments = description.Split('*');
        for (int i = 0; i < segments.Length; i += 2)
        {
            segments[i] = pattern.Replace(segments[i], match => $"*{match.Value}*");
        }

        return string.Join("*", segments);
    }

    private static Regex[] BuildKeywordHighlightRules()
    {
        string[] englishTerms =
        [
            "Countdown of Destiny",
            "Good Omen",
            "Bad Omen",
            "Revelation",
            "Doomed",
            "Destiny",
            "Divinate",
            "Foretell",
            "Fated",
            "Scry",
            "Vulnerable",
            "Weak",
            "Retain",
            "Exhaust",
            "Status",
            "Curse"
        ];

        string[] chineseTerms =
        [
            "命运倒计时",
            "吉兆",
            "凶兆",
            "劫兆",
            "启示",
            "命运",
            "占卜",
            "预言",
            "注定",
            "预见",
            "虚弱",
            "易伤",
            "保留",
            "状态",
            "诅咒"
        ];

        return englishTerms
            .OrderByDescending(term => term.Length)
            .Select(term => new Regex(
                $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(term)}(?![\p{{L}}\p{{N}}_])",
                RegexOptions.CultureInvariant))
            .Concat(chineseTerms
                .OrderByDescending(term => term.Length)
                .Select(term => new Regex(Regex.Escape(term), RegexOptions.CultureInvariant)))
            .ToArray();
    }
}
