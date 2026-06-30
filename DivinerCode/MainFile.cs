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

    private static readonly (Regex Pattern, string Replacement)[] KeywordHighlightRules =
        BuildKeywordHighlightRules();

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
        description = HighlightKeywordTerms(description);
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
        foreach (var (pattern, replacement) in KeywordHighlightRules)
        {
            description = pattern.Replace(description, replacement);
        }

        return description.Replace("#Countdown of #Destiny", "#Countdown of Destiny", StringComparison.Ordinal);
    }

    private static (Regex Pattern, string Replacement)[] BuildKeywordHighlightRules()
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
            "消耗",
            "状态",
            "诅咒"
        ];

        return englishTerms
            .Select(term => (Pattern: new Regex($@"(?<!#)\b{Regex.Escape(term)}\b", RegexOptions.CultureInvariant), Replacement: $"#{term}"))
            .Concat(chineseTerms.Select(term => (
                Pattern: new Regex($@"(?<!#){Regex.Escape(term)}", RegexOptions.CultureInvariant),
                Replacement: $"#{term}")))
            .ToArray();
    }
}
