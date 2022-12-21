using System.Xml;

namespace WaylandSharpGen.Xml;

internal sealed record MethodArgument
{
    public string Name { get; }
    public ArgumentType Type { get; }
    public bool Nullable { get; }
    public string? Interface { get; }
    public string? Documentation { get; }
    public string? Enum { get; }

    public MethodArgument(string name, ArgumentType type, bool nullable, string? @interface, string? documentation, string? @enum)
    {
        Name = name;
        Type = type;
        Nullable = nullable;
        Interface = @interface;
        Documentation = documentation;
        Enum = @enum;
    }

    public static MethodArgument FromXml(XmlElement element)
    {
        var name = element.GetAttribute("name");
        var type = element.GetAttribute("type") switch
        {
            "int" => ArgumentType.Int,
            "uint" => ArgumentType.Uint,
            "fixed" => ArgumentType.Fixed,
            "string" => ArgumentType.String,
            "object" => ArgumentType.Object,
            "new_id" => ArgumentType.NewId,
            "array" => ArgumentType.Array,
            "fd" => ArgumentType.FD,
            _ => throw new InvalidOperationException($"Invalid message argument type: {element.GetAttribute("type")}")
        };
        var nullable = element.GetAttribute("nullable") == "true";
        var interfaceName = element.GetAttribute("interface").DefiniteNull();
        var documentation = element.GetAttribute("summary").DefiniteNull();
        var enumName = element.GetAttribute("enum").DefiniteNull();
        return new MethodArgument(name, type, nullable, interfaceName, documentation, enumName);
    }

    public bool Equals(MethodArgument other)
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
