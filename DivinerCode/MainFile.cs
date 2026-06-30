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
    private static readonly Regex GoldMarkerRegex = new(@"\*([^*]+)\*", RegexOptions.CultureInvariant);
    private static readonly Regex SimpleDynamicVarRegex = new(@"!([A-Za-z][A-Za-z0-9_]*)!", RegexOptions.CultureInvariant);
    private static readonly Regex ProcessedDynamicVarRegex = new(@"\{([A-Za-z][A-Za-z0-9_]*):diff\(\)\}", RegexOptions.CultureInvariant);

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
        description = ResolveDynamicValueMarkers(card, description);
        description = ApplyGoldHighlights(HighlightKeywordTerms(description));
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

    private static string ResolveDynamicValueMarkers(CardModel card, string description)
    {
        description = SimpleDynamicVarRegex.Replace(description, match => ResolveDynamicValueMarker(card, match));
        return ProcessedDynamicVarRegex.Replace(description, match => ResolveDynamicValueMarker(card, match));
    }

    private static string ResolveDynamicValueMarker(CardModel card, Match match)
    {
        var varName = match.Groups[1].Value;
        if (!card.DynamicVars.TryGetValue(varName, out var dynamicVar))
        {
            return match.Value;
        }

        return dynamicVar.ToHighlightedString(false);
    }

    private static string HighlightKeywordTerms(string description)
    {
        foreach (var pattern in KeywordHighlightRules)
        {
            description = ReplaceOutsideGoldHighlights(description, pattern);
        }

        return description;
    }

    private static string ApplyGoldHighlights(string description)
    {
        return GoldMarkerRegex
            .Replace(description, "[gold]$1[/gold]")
            .Replace("/*", "*", StringComparison.Ordinal);
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
