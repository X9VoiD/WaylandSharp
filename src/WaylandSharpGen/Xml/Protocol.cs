using System.Collections.Immutable;
using System.Xml;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WaylandSharp.Tests")]
namespace WaylandSharpGen.Xml;

internal sealed record Protocol
{
    public string Name { get; }
    public ImmutableArray<Interface> Interfaces { get; }

    public Protocol(string name, ImmutableArray<Interface> interfaces)
    {
        Name = name;
        Interfaces = interfaces;
    }

    public static Protocol FromXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return FromXml(doc);
    }

    public static Protocol FromXml(Stream stream)
    {
        var doc = new XmlDocument();
        doc.Load(stream);
        return FromXml(doc);
    }

    public static Protocol FromXml(XmlDocument doc)
    {
        var name = (doc.SelectSingleNode("/protocol") as XmlElement)!.GetAttribute("name");
        var interfaces = doc.SelectNodes("/protocol/interface")
            .OfType<XmlElement>()
            .Select(Interface.FromXml)
            .ToImmutableArray();
        return new Protocol(name, interfaces);
    }

    public bool Equals(Protocol other)
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
