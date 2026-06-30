using System.Reflection;
using Diviner.DivinerCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Diviner.DivinerCode.Mechanics;

public static class DivinerPlayerDetection
{
    public static bool IsDivinerPlayer(Player player)
    {
        if (player.Relics.Any(relic => relic is CrystalBall || relic.GetType().Name == nameof(CrystalBall)))
        {
            return true;
        }

        var character = player.Character;
        if (character == null)
        {
            return false;
        }

        var type = character.GetType();
        if (type.FullName?.Contains(".Character.Diviner", StringComparison.Ordinal) == true ||
            type.Name.Contains("Diviner", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return GetCharacterTextTokens(character).Any(text =>
            text.Contains("DIVINER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "Diviner", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetCharacterTextTokens(object character)
    {
        if (character.ToString() is { Length: > 0 } direct)
        {
            yield return direct;
        }

        foreach (var propertyName in new[] { "Id", "ID", "CharacterId", "CharacterID", "Title", "Name", "PlaceholderID" })
        {
            var property = character.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var value = property?.GetValue(character);
            if (value == null)
            {
                continue;
            }

            if (value.ToString() is { Length: > 0 } text)
            {
                yield return text;
            }

            var entry = value.GetType().GetProperty("Entry", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);
            if (entry?.ToString() is { Length: > 0 } entryText)
            {
                yield return entryText;
            }
        }
    }
}
