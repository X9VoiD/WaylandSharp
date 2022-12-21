using System.Text;
using System.Text.RegularExpressions;
using WaylandSharpGen.Xml;

namespace WaylandSharpGen;

internal static class Util
{
    private static readonly Regex _isStartingWithDigit = new(@"^\d", RegexOptions.Compiled);

    public static string? DefiniteNull(this string? s)
    {
        return string.IsNullOrEmpty(s) ? null : s;
    }

    public static string SnakeToPascalCase(this string s)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var c in s)
        {
            if (c == '_')
            {
                first = true;
            }
            else
            {
                if (first)
                {
                    sb.Append(char.ToUpper(c));
                    first = false;
                }
                else
                {
                    sb.Append(char.ToLower(c));
                }
            }
        }
        return sb.ToString();
    }

    public static string Escape(this string s)
    {
        if (s is "interface" or "enum")
        {
            return "@" + s;
        }
        else if (_isStartingWithDigit.IsMatch(s))
        {
            return "_" + s;
        }
        else
        {
            return s;
        }
    }

    public static string? ParseEnum(this string? s, string interfaceName)
    {
        if (s is null)
            return null;

        var split = s.Split('.');
        if (split.Length > 1)
        {
            for (var i = 0; i < split.Length; i++)
            {
                split[i] = split[i].SnakeToPascalCase();
            }

            return string.Join("", split);
        }
        else
        {
            return interfaceName + s.SnakeToPascalCase();
        }
    }

    public static Signature ToSignature(this Method definition)
    {
        var signature = new StringBuilder();
        var arguments = definition.Arguments;
        if (definition.Since != 0)
            signature.Append(definition.Since);

        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var type = argument.Type switch
            {
                ArgumentType.Int => "i",
                ArgumentType.Uint => "u",
                ArgumentType.Fixed => "f",
                ArgumentType.String => "s",
                ArgumentType.Object => "o",
                ArgumentType.NewId => "n",
                ArgumentType.Array => "a",
                ArgumentType.FD => "h",
                _ => throw new InvalidOperationException($"Invalid type encountered: {argument.Type}"),
            };
            signature.Append(argument.Nullable ? $"?{type}" : type);
        }

        return new Signature(signature.ToString());
    }
}

