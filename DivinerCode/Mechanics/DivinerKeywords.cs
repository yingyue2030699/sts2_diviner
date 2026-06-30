using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Diviner.DivinerCode.Mechanics;

public static class DivinerKeywords
{
    [CustomEnum("DESTINY")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Destiny;

    [CustomEnum("BAD_OMEN")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword BadOmen;

    [CustomEnum("GOOD_OMEN")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword GoodOmen;

    [CustomEnum("DIVINATE")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Divinate;

    [CustomEnum("FORETELL")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Foretell;

    [CustomEnum("DREDGE")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Dredge;

    [CustomEnum("ENLIGHTENMENT")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Enlightenment;

    [CustomEnum("FATED")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Fated;

    [CustomEnum("COUNTDOWN_OF_DESTINY")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword CountdownOfDestiny;

    [CustomEnum("SCRY")]
    [KeywordProperties(AutoKeywordPosition.None, true)]
    public static CardKeyword Scry;
}
