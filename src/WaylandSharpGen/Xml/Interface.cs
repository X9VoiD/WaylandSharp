using System.Collections.Immutable;
using System.Xml;

namespace WaylandSharpGen.Xml;

internal sealed record Interface
{
    public string Name { get; }
    public int Version { get; }
    public string? DocumentationSummary { get; }
    public string? Documentation { get; }

    public ImmutableArray<Enum> Enums { get; }
    public ImmutableArray<Method> Requests { get; }
    public ImmutableArray<Method> Events { get; }

    public Interface(string name, int version, string? documentationSummary, string? documentation, ImmutableArray<Enum> enums, ImmutableArray<Method> requests, ImmutableArray<Method> events)
    {
        Name = name;
        Version = version;
        DocumentationSummary = documentationSummary;
        Documentation = documentation;
        Enums = enums;
        Requests = requests;
        Events = events;
    }

    public static Interface FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var version = int.Parse(element.GetAttribute("version"));
        var documentationElement = element.SelectSingleNode("description") as XmlElement;
        var documentationSummary = documentationElement?.GetAttribute("summary").DefiniteNull();
        var documentation = documentationElement?.InnerText.Trim().DefiniteNull();
        var enums = element.SelectNodes("enum")
            .OfType<XmlElement>()
            .Select(Enum.FromXml)
            .ToImmutableArray();
        var requests = element.SelectNodes("request")
            .OfType<XmlElement>()
            .Select(Method.FromXml)
            .ToImmutableArray();
        var events = element.SelectNodes("event")
            .OfType<XmlElement>()
            .Select(Method.FromXml)
            .ToImmutableArray();
        return new Interface(name, version, documentationSummary, documentation, enums, requests, events);
    }

    public bool Equals(Interface other)
    {
        return Name == other.Name
            && Version == other.Version
            && DocumentationSummary == other.DocumentationSummary
            && Documentation == other.Documentation
            && Enums.SequenceEqual(other.Enums)
            && Requests.SequenceEqual(other.Requests)
            && Events.SequenceEqual(other.Events);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Name, Version, DocumentationSummary, Documentation);

        foreach (var @enum in Enums)
            HashCode.Combine(hash, @enum.GetHashCode());
        foreach (var request in Requests)
            HashCode.Combine(hash, request.GetHashCode());
        foreach (var event_ in Events)
            HashCode.Combine(hash, event_.GetHashCode());

        return hash;
    }
}
