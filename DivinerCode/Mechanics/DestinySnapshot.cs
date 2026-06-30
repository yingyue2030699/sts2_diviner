namespace Diviner.DivinerCode.Mechanics;

public readonly record struct DestinySnapshot(
    int Destiny,
    DestinyOmen Omen,
    bool IsActive,
    bool IsLoaded)
{
    public string OmenLabel => DestinyConstants.GetOmenLabel(Destiny);

    public bool IsGoodOmen => Omen == DestinyOmen.Good;

    public bool IsBadOmen => Omen == DestinyOmen.Bad;
}
