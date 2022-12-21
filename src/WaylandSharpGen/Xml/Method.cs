using System.Collections.Immutable;
using System.Xml;

namespace WaylandSharpGen.Xml;

internal sealed record Method
{
    public MethodType Type { get; }
    public string Name { get; }
    public int OpCode { get; }
    public int Since { get; }
    public string? DocumentationSummary { get; }
    public string? Documentation { get; }
    public string? ExtraTypeAnnotation { get; }

    public ImmutableArray<MethodArgument> Arguments { get; }

    public Method(MethodType type, string name, int opCode, int since, string? documentationSummary, string? documentation, string? extraTypeAnnotation, ImmutableArray<MethodArgument> arguments)
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

    public static Method FromXml(XmlElement element, int index)
    {
        var type = element.Name switch
        {
            "request" => MethodType.Request,
            "event" => MethodType.Event,
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
            .Select(MethodArgument.FromXml)
            .ToImmutableArray();
        return new Method(type, name, opCode, since, documentationSummary, documentation, extraTypeAnnotation, arguments);
    }

    public bool Equals(Method other)
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
