using System.Text;
using Godot;

namespace Diviner.DivinerCode.Extensions;

public static class StringExtensions
{
    private static string ModContentRoot
    {
        get
        {
            var looseRoot = $"res://mods/{MainFile.ModId}";
            if (ResourceLoader.Exists(Path.Join(looseRoot, "mod_image.png")))
            {
                return looseRoot;
            }

            var packedRoot = $"res://{MainFile.ModId}";
            if (ResourceLoader.Exists(Path.Join(packedRoot, "mod_image.png")))
            {
                return packedRoot;
            }

            return looseRoot;
        }
    }

    public static string ContentPath(this string path)
    {
        return Path.Join(ModContentRoot, path);
    }

    public static string ImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "card_portraits", path);
    }

    public static string BigCardImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "card_portraits", "big", path);
    }

    public static string PowerImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "powers", path);
    }

    public static string BigPowerImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "powers", "big", path);
    }

    public static string RelicImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "relics", path);
    }

    public static string BigRelicImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "relics", "big", path);
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "charui", path);
    }

    public static string CharacterImagePath(this string path)
    {
        return Path.Join(ModContentRoot, "images", "character", path);
    }

    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
