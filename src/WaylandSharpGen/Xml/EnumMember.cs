using System.Globalization;
using System.Xml;

namespace WaylandSharpGen.Xml;

internal sealed record EnumMember
{
    public string Name { get; }
    public int Value { get; }
    public string? Documentation { get; }

    public EnumMember(string name, int value, string? documentation)
    {
        Name = name;
        Value = value;
        Documentation = documentation;
    }

    public static EnumMember FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var valueText = element.GetAttribute("value");
        var value = valueText.StartsWith("0x", StringComparison.InvariantCulture)
            ? int.Parse(valueText.Substring(2), NumberStyles.HexNumber)
            : int.Parse(valueText);

        var documentation = element.GetAttribute("summary").DefiniteNull();
        return new EnumMember(name, value, documentation);
    }

    public bool Equals(EnumMember other)
    {
        return Name == other.Name
            && Value == other.Value
            && Documentation == other.Documentation;
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Name, Value, Documentation);
        return hash;
    }
}