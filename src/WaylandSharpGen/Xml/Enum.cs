using System.Collections.Immutable;
using System.Xml;

namespace WaylandSharpGen.Xml;

internal sealed record Enum
{
    public string Name { get; }
    public string? DocumentationSummary { get; }
    public string? Documentation { get; }

    public ImmutableArray<EnumMember> Members { get; }

    public Enum(string name, string? documentationSummary, string? documentation, ImmutableArray<EnumMember> members)
    {
        Name = name;
        DocumentationSummary = documentationSummary;
        Documentation = documentation;
        Members = members;
    }

    public static Enum FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var documentationElement = element.SelectSingleNode("description") as XmlElement;
        var documentationSummary = documentationElement?.GetAttribute("summary").DefiniteNull();
        var documentation = documentationElement?.InnerText.Trim().DefiniteNull();
        var members = element.SelectNodes("entry")
            .OfType<XmlElement>()
            .Select(EnumMember.FromXml)
            .ToImmutableArray();
        return new Enum(name, documentationSummary, documentation, members);
    }

    public bool Equals(Enum other)
    {
        return Name == other.Name
            && DocumentationSummary == other.DocumentationSummary
            && Documentation == other.Documentation
            && Members.SequenceEqual(other.Members);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Name, DocumentationSummary, Documentation);

        foreach (var entry in Members)
            HashCode.Combine(hash, entry.GetHashCode());

        return hash;
    }
}
