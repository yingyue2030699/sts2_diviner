namespace Diviner.DivinerCode.Mechanics;

using MegaCrit.Sts2.Core.Models;

public readonly record struct DivinationRecord(
    string Category,
    string Text,
    int Floor,
    string? PreviewRelicIds = null,
    bool IsActive = true)
{
    public IEnumerable<ModelId> GetPreviewRelicIds()
    {
        if (string.IsNullOrWhiteSpace(PreviewRelicIds))
        {
            yield break;
        }

        foreach (var token in PreviewRelicIds.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = token.Split('|', 2);
            if (parts.Length == 2 &&
                !string.IsNullOrWhiteSpace(parts[0]) &&
                !string.IsNullOrWhiteSpace(parts[1]))
            {
                yield return new ModelId(parts[0], parts[1]);
            }
        }
    }

    public static string EncodeRelicIds(IEnumerable<ModelId> relicIds)
    {
        return string.Join(";", relicIds.Select(id => $"{id.Category}|{id.Entry}"));
    }
}
