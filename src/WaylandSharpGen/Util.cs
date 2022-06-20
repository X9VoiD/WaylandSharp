using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

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

    public static Signature ToSignature(this ProtocolMessageDefinition definition)
    {
        return ToSignature(definition.Arguments);
    }

    private static Signature ToSignature(ImmutableArray<ProtocolMessageArgumentDefinition> arguments)
    {
        var signature = new StringBuilder();
        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var type = argument.Type switch
            {
                ProtocolMessageArgumentType.Int => "i",
                ProtocolMessageArgumentType.Uint => "u",
                ProtocolMessageArgumentType.Fixed => "f",
                ProtocolMessageArgumentType.String => "s",
                ProtocolMessageArgumentType.Object => "o",
                ProtocolMessageArgumentType.NewId => "n",
                ProtocolMessageArgumentType.Array => "a",
                ProtocolMessageArgumentType.FD => "h",
                _ => throw new InvalidOperationException($"Invalid type encountered: {argument.Type}"),
            };
            signature.Append(argument.Nullable ? $"?{type}" : type);
        }

        return new Signature(signature.ToString());
    }
}

