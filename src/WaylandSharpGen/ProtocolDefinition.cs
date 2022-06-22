using System.Collections.Immutable;
using System.Globalization;
using System.Xml;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WaylandSharp.Tests")]
namespace WaylandSharpGen;

internal sealed record ProtocolDefinition
{
    public string Name { get; }
    public ImmutableArray<ProtocolInterfaceDefinition> Interfaces { get; }

    public ProtocolDefinition(string name, ImmutableArray<ProtocolInterfaceDefinition> interfaces)
    {
        Name = name;
        Interfaces = interfaces;
    }

    public static ProtocolDefinition FromXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return FromXml(doc);
    }

    public static ProtocolDefinition FromXml(Stream stream)
    {
        var doc = new XmlDocument();
        doc.Load(stream);
        return FromXml(doc);
    }

    public static ProtocolDefinition FromXml(XmlDocument doc)
    {
        var name = (doc.SelectSingleNode("/protocol") as XmlElement)!.GetAttribute("name");
        var interfaces = doc.SelectNodes("/protocol/interface")
            .OfType<XmlElement>()
            .Select(ProtocolInterfaceDefinition.FromXml)
            .ToImmutableArray();
        return new ProtocolDefinition(name, interfaces);
    }

    public bool Equals(ProtocolDefinition other)
    {
        return Name == other.Name && Interfaces.SequenceEqual(other.Interfaces);
    }

    public override int GetHashCode()
    {
        var hash = Name.GetHashCode();

        foreach (var @interface in Interfaces)
            HashCode.Combine(hash, @interface.GetHashCode());

        return HashCode.Combine(Name, Interfaces);
    }
}

internal sealed record ProtocolInterfaceDefinition
{
    public string Name { get; }
    public int Version { get; }
    public string? DocumentationSummary { get; }
    public string? Documentation { get; }

    public ImmutableArray<ProtocolEnumDefinition> Enums { get; }
    public ImmutableArray<ProtocolMessageDefinition> Requests { get; }
    public ImmutableArray<ProtocolMessageDefinition> Events { get; }

    public ProtocolInterfaceDefinition(string name, int version, string? documentationSummary, string? documentation, ImmutableArray<ProtocolEnumDefinition> enums, ImmutableArray<ProtocolMessageDefinition> requests, ImmutableArray<ProtocolMessageDefinition> events)
    {
        Name = name;
        Version = version;
        DocumentationSummary = documentationSummary;
        Documentation = documentation;
        Enums = enums;
        Requests = requests;
        Events = events;
    }

    public static ProtocolInterfaceDefinition FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var version = int.Parse(element.GetAttribute("version"));
        var documentationElement = element.SelectSingleNode("description") as XmlElement;
        var documentationSummary = documentationElement?.GetAttribute("summary").DefiniteNull();
        var documentation = documentationElement?.InnerText.Trim().DefiniteNull();
        var enums = element.SelectNodes("enum")
            .OfType<XmlElement>()
            .Select(ProtocolEnumDefinition.FromXml)
            .ToImmutableArray();
        var requests = element.SelectNodes("request")
            .OfType<XmlElement>()
            .Select(ProtocolMessageDefinition.FromXml)
            .ToImmutableArray();
        var events = element.SelectNodes("event")
            .OfType<XmlElement>()
            .Select(ProtocolMessageDefinition.FromXml)
            .ToImmutableArray();
        return new ProtocolInterfaceDefinition(name, version, documentationSummary, documentation, enums, requests, events);
    }

    public bool Equals(ProtocolInterfaceDefinition other)
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

internal enum ProtocolMessageType
{
    None,
    Request,
    Event
}

internal sealed record ProtocolMessageDefinition
{
    public ProtocolMessageType Type { get; }
    public string Name { get; }
    public int OpCode { get; }
    public int Since { get; }
    public string? DocumentationSummary { get; }
    public string? Documentation { get; }
    public string? ExtraTypeAnnotation { get; }

    public ImmutableArray<ProtocolMessageArgumentDefinition> Arguments { get; }

    public ProtocolMessageDefinition(ProtocolMessageType type, string name, int opCode, int since, string? documentationSummary, string? documentation, string? extraTypeAnnotation, ImmutableArray<ProtocolMessageArgumentDefinition> arguments)
    {
        Type = type;
        Name = name;
        OpCode = opCode;
        Since = since;
        DocumentationSummary = documentationSummary;
        Documentation = documentation;
        ExtraTypeAnnotation = extraTypeAnnotation;
        Arguments = arguments;
    }

    public static ProtocolMessageDefinition FromXml(XmlElement element, int index)
    {
        var type = element.Name switch
        {
            "request" => ProtocolMessageType.Request,
            "event" => ProtocolMessageType.Event,
            _ => throw new InvalidOperationException($"Invalid message type: {element.GetAttribute("type")}")
        };
        var name = element.GetAttribute("name");
        var opCode = index;
        var since = int.TryParse(element.GetAttribute("since"), out var since_) ? since_ : 0;
        var documentationElement = element.SelectSingleNode("description") as XmlElement;
        var documentationSummary = documentationElement?.GetAttribute("summary").DefiniteNull();
        var documentation = documentationElement?.InnerText.Trim().DefiniteNull();
        var extraTypeAnnotation = element.GetAttribute("type").DefiniteNull();
        var arguments = element.SelectNodes("arg")
            .OfType<XmlElement>()
            .Select(ProtocolMessageArgumentDefinition.FromXml)
            .ToImmutableArray();
        return new ProtocolMessageDefinition(type, name, opCode, since, documentationSummary, documentation, extraTypeAnnotation, arguments);
    }

    public bool Equals(ProtocolMessageDefinition other)
    {
        return Type == other.Type
            && Name == other.Name
            && OpCode == other.OpCode
            && DocumentationSummary == other.DocumentationSummary
            && Documentation == other.Documentation
            && ExtraTypeAnnotation == other.ExtraTypeAnnotation
            && Arguments.SequenceEqual(other.Arguments);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Type, Name, OpCode, DocumentationSummary, Documentation, ExtraTypeAnnotation);

        foreach (var argument in Arguments)
            HashCode.Combine(hash, argument.GetHashCode());

        return hash;
    }
}

internal enum ProtocolMessageArgumentType
{
    None,
    Int,
    Uint,
    Fixed,
    String,
    Object,
    NewId,
    Array,
    FD,
}

internal sealed record ProtocolMessageArgumentDefinition
{
    public string Name { get; }
    public ProtocolMessageArgumentType Type { get; }
    public bool Nullable { get; }
    public string? Interface { get; }
    public string? Documentation { get; }
    public string? Enum { get; }

    public ProtocolMessageArgumentDefinition(string name, ProtocolMessageArgumentType type, bool nullable, string? @interface, string? documentation, string? @enum)
    {
        Name = name;
        Type = type;
        Nullable = nullable;
        Interface = @interface;
        Documentation = documentation;
        Enum = @enum;
    }

    public static ProtocolMessageArgumentDefinition FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var type = element.GetAttribute("type") switch
        {
            "int" => ProtocolMessageArgumentType.Int,
            "uint" => ProtocolMessageArgumentType.Uint,
            "fixed" => ProtocolMessageArgumentType.Fixed,
            "string" => ProtocolMessageArgumentType.String,
            "object" => ProtocolMessageArgumentType.Object,
            "new_id" => ProtocolMessageArgumentType.NewId,
            "array" => ProtocolMessageArgumentType.Array,
            "fd" => ProtocolMessageArgumentType.FD,
            _ => throw new InvalidOperationException($"Invalid message argument type: {element.GetAttribute("type")}")
        };
        var nullable = element.GetAttribute("nullable") == "true";
        var interfaceName = element.GetAttribute("interface").DefiniteNull();
        var documentation = element.GetAttribute("summary").DefiniteNull();
        var enumName = element.GetAttribute("enum").DefiniteNull();
        return new ProtocolMessageArgumentDefinition(name, type, nullable, interfaceName, documentation, enumName);
    }

    public bool Equals(ProtocolMessageArgumentDefinition other)
    {
        return Name == other.Name
            && Type == other.Type
            && Nullable == other.Nullable
            && Interface == other.Interface
            && Documentation == other.Documentation;
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Name, Type, Nullable, Interface, Documentation);
        return hash;
    }
}

internal sealed record ProtocolEnumDefinition
{
    public string Name { get; }
    public string? DocumentationSummary { get; }
    public string? Documentation { get; }

    public ImmutableArray<ProtocolEnumEntryDefinition> Entries { get; }

    public ProtocolEnumDefinition(string name, string? documentationSummary, string? documentation, ImmutableArray<ProtocolEnumEntryDefinition> entries)
    {
        Name = name;
        DocumentationSummary = documentationSummary;
        Documentation = documentation;
        Entries = entries;
    }

    public static ProtocolEnumDefinition FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var documentationElement = element.SelectSingleNode("description") as XmlElement;
        var documentationSummary = documentationElement?.GetAttribute("summary").DefiniteNull();
        var documentation = documentationElement?.InnerText.Trim().DefiniteNull();
        var entries = element.SelectNodes("entry")
            .OfType<XmlElement>()
            .Select(ProtocolEnumEntryDefinition.FromXml)
            .ToImmutableArray();
        return new ProtocolEnumDefinition(name, documentationSummary, documentation, entries);
    }

    public bool Equals(ProtocolEnumDefinition other)
    {
        return Name == other.Name
            && DocumentationSummary == other.DocumentationSummary
            && Documentation == other.Documentation
            && Entries.SequenceEqual(other.Entries);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Name, DocumentationSummary, Documentation);

        foreach (var entry in Entries)
            HashCode.Combine(hash, entry.GetHashCode());

        return hash;
    }
}

internal sealed record ProtocolEnumEntryDefinition
{
    public string Name { get; }
    public int Value { get; }
    public string? Documentation { get; }

    public ProtocolEnumEntryDefinition(string name, int value, string? documentation)
    {
        Name = name;
        Value = value;
        Documentation = documentation;
    }

    public static ProtocolEnumEntryDefinition FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var valueText = element.GetAttribute("value");
        var value = valueText.StartsWith("0x", StringComparison.InvariantCulture)
            ? int.Parse(valueText.Substring(2), NumberStyles.HexNumber)
            : int.Parse(valueText);

        var documentation = element.GetAttribute("summary").DefiniteNull();
        return new ProtocolEnumEntryDefinition(name, value, documentation);
    }

    public bool Equals(ProtocolEnumEntryDefinition other)
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