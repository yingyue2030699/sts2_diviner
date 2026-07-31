namespace Diviner.DivinerCode.Mechanics;

using Diviner.DivinerCode.Localization;

public enum DestinyOmen
{
    Bad,
    Good,
}

public static class DestinyConstants
{
    public const int MinDestiny = 0;
    public const int MaxDestiny = 5;
    public const int DefaultDestiny = 3;

    public const int BadOmenMaxDestiny = 2;
    public const int GoodOmenMinDestiny = 3;

    public const int DredgeDestiny = 0;
    public const int EnlightenmentDestiny = 5;
    public const int DredgeStartingCountdown = 3;
    public const int DredgeEscapeCardsPerPile = 3;
    public const int EnlightenmentCardCount = 3;

    public static int Clamp(int destiny)
    {
        return Math.Clamp(destiny, MinDestiny, MaxDestiny);
    }

    public static DestinyOmen GetOmen(int destiny)
    {
        return Clamp(destiny) >= GoodOmenMinDestiny ? DestinyOmen.Good : DestinyOmen.Bad;
    }

    public static bool IsGoodOmen(int destiny)
    {
        return GetOmen(destiny) == DestinyOmen.Good;
    }

    public static bool IsBadOmen(int destiny)
    {
        return GetOmen(destiny) == DestinyOmen.Bad;
    }

    public static bool IsDredgeDestiny(int destiny)
    {
        return Clamp(destiny) == DredgeDestiny;
    }

    public static bool IsEnlightenmentDestiny(int destiny)
    {
        return Clamp(destiny) == EnlightenmentDestiny;
    }

    public static string GetOmenLabel(int destiny)
    {
        return IsGoodOmen(destiny)
            ? DivinerLoc.Text("Good Omen", "吉兆")
            : DivinerLoc.Text("Bad Omen", "凶兆");
    }
}
