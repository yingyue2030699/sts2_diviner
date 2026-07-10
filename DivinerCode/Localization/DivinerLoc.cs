using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;

namespace Diviner.DivinerCode.Localization;

public static class DivinerLoc
{
    public static bool IsSimplifiedChinese => LocManager.Instance?.Language switch
    {
        "zhs" or "zh-CN" or "zh_Hans" or "zh-Hans" => true,
        _ => false
    };

    public static string Text(string english, string chinese)
    {
        return IsSimplifiedChinese ? chinese : english;
    }

    public static List<(string, string)> Card(
        string englishName,
        string englishDescription,
        string chineseName,
        string chineseDescription,
        params (string Key, string English, string Chinese)[] extraLoc
    )
    {
        return new CardLoc(
            IsSimplifiedChinese ? chineseName : englishName,
            IsSimplifiedChinese ? chineseDescription : englishDescription,
            LocalizeExtra(extraLoc)
        );
    }

    public static List<(string, string)> Relic(
        string englishName,
        string englishDescription,
        string englishFlavor,
        string chineseName,
        string chineseDescription,
        string chineseFlavor,
        params (string Key, string English, string Chinese)[] extraLoc
    )
    {
        return new RelicLoc(
            IsSimplifiedChinese ? chineseName : englishName,
            IsSimplifiedChinese ? chineseDescription : englishDescription,
            IsSimplifiedChinese ? chineseFlavor : englishFlavor,
            LocalizeExtra(extraLoc)
        );
    }

    public static List<(string, string)> Power(
        string englishName,
        string englishDescription,
        string englishAltDescription,
        string chineseName,
        string chineseDescription,
        string chineseAltDescription,
        params (string Key, string English, string Chinese)[] extraLoc
    )
    {
        return new PowerLoc(
            IsSimplifiedChinese ? chineseName : englishName,
            IsSimplifiedChinese ? chineseDescription : englishDescription,
            IsSimplifiedChinese ? chineseAltDescription : englishAltDescription,
            LocalizeExtra(extraLoc)
        );
    }

    public static List<(string, string)> Potion(
        string englishName,
        string englishDescription,
        string chineseName,
        string chineseDescription,
        params (string Key, string English, string Chinese)[] extraLoc
    )
    {
        return new PotionLoc(
            IsSimplifiedChinese ? chineseName : englishName,
            IsSimplifiedChinese ? chineseDescription : englishDescription,
            LocalizeExtra(extraLoc)
        );
    }

    private static (string, string)[] LocalizeExtra((string Key, string English, string Chinese)[] extraLoc)
    {
        return extraLoc
            .Select(entry => (entry.Key, IsSimplifiedChinese ? entry.Chinese : entry.English))
            .ToArray();
    }
}
