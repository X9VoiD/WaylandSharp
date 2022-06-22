using System.Text.RegularExpressions;

namespace WaylandSharpGen;

public readonly struct Signature : IEquatable<Signature>
{
    private static readonly Regex _signatureRegex = new(@"(?'version'\d*)(?'signature'[iufsonah?]+)", RegexOptions.Compiled);

    public readonly string Raw;
    public readonly int? Version;
    public readonly string SignatureOnly { get; }

    public Signature(string raw)
    {
        Raw = raw;

        var matcher = _signatureRegex.Match(raw);
        Version = matcher.Groups["version"] is { } version && !string.IsNullOrEmpty(version.Value)
            ? int.Parse(version.Value)
            : (int?)null;
        SignatureOnly = matcher.Groups["signature"].Value;
    }

    public string AsHash()
    {
        return SignatureOnly.Replace("?", "").Replace("h", "i").Replace("f", "i").Replace("n", "o");
    }

    public static bool operator ==(Signature left, Signature right) => left.Raw == right.Raw;
    public static bool operator !=(Signature left, Signature right) => left.Raw != right.Raw;

    public bool Equals(Signature other)
    {
        return Raw == other.Raw;
    }

    public override bool Equals(object? obj)
    {
        return obj is Signature other && Raw == other.Raw;
    }

    public override int GetHashCode()
    {
        return Raw.GetHashCode();
    }

    public SignatureEnumerator GetEnumerator()
    {
        return new SignatureEnumerator(SignatureOnly);
    }

    public struct SignatureEnumerator
    {
        private readonly string _signature;
        private int _index = -1;

        public SignatureEntry Current
        {
            get
            {
                var type = _signature[_index];
                if (type == '?')
                {
                    type = _signature[++_index];
                    return new SignatureEntry(type, true);
                }
                return new SignatureEntry(type, false);
            }
        }

        internal SignatureEnumerator(string signature)
        {
            _signature = signature;
        }

        public bool MoveNext()
        {
            _index += 1;
            return _index < _signature.Length;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    public readonly struct SignatureEntry
    {
        public readonly char Type;
        public readonly bool Nullable;

        internal SignatureEntry(char type, bool nullable)
        {
            Type = type;
            Nullable = nullable;
        }
    }
}
