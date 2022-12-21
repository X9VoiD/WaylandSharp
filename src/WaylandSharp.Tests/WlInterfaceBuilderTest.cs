using System.Xml;
using WaylandSharpGen;
using WaylandSharpGen.Xml;

namespace WaylandSharp.Tests;

public class WlInterfaceBuilderTest
{
    [Fact]
    public void CanBuildEmpty()
    {
        var interfaceCacheBuilder = new WlInterfaceBuilder();

        var expected =
            """
            public partial class WlInterface
            {
                static WlInterface()
                {
                }

                public static WlInterface FromInterfaceName(string name)
                {
                    return name switch
                    {
                        _ => throw new ArgumentException($"Unknown interface name: {name}")};
                }
            }
            """;

        interfaceCacheBuilder.Build().ToFullString().Should().Be(expected);
    }

    [Theory]
    [InlineData("request", "Method")]
    [InlineData("event", "Event")]
    public void CanBuildSimpleProtocol(string messageType, string registerFunction)
    {
        var doc = new XmlDocument();
        var docText = string.Format(
            """
            <protocol name="simple_protocol">
                <interface name="simple_interface" version="1">
                    <{0} name="foo">
                        <arg name="bar" type="int" />
                    </{0}>
                </interface>
            </protocol>
            """, messageType);
        doc.LoadXml(docText);

        var protocolDefinition = Protocol.FromXml(doc);
        var interfaceCacheBuilder = new WlInterfaceBuilder();

        interfaceCacheBuilder.GenerateCache(protocolDefinition);
        var classDeclaration = interfaceCacheBuilder.Build();
        var fullText = classDeclaration.ToFullString();

        var expected =
            $$"""
            public partial class WlInterface
            {
                public static readonly WlInterface SimpleInterface;
                static WlInterface()
                {
                    SimpleInterface = new WlInterface.Builder("simple_interface", 1).{{registerFunction}}("foo", "i", new WlInterface? []{null});
                }

                public static WlInterface FromInterfaceName(string name)
                {
                    return name switch
                    {
                        "simple_interface" => SimpleInterface,
                        _ => throw new ArgumentException($"Unknown interface name: {name}")};
                }
            }
            """;

        fullText.Should().Be(expected);
    }

    [Fact]
    public void CanBuildProtocolWithSince()
    {
        var doc = new XmlDocument();
        var docText =
            """
            <protocol name="simple_protocol">
                <interface name="simple_interface" version="2">
                    <request name="foo" since="2">
                        <arg name="bar" type="int" />
                    </request>
                </interface>
            </protocol>
            """;
        doc.LoadXml(docText);

        var protocolDefinition = Protocol.FromXml(doc);
        var interfaceCacheBuilder = new WlInterfaceBuilder();

        interfaceCacheBuilder.GenerateCache(protocolDefinition);
        var classDeclaration = interfaceCacheBuilder.Build();
        var fullText = classDeclaration.ToFullString();

        var expected =
            $$"""
            public partial class WlInterface
            {
                public static readonly WlInterface SimpleInterface;
                static WlInterface()
                {
                    SimpleInterface = new WlInterface.Builder("simple_interface", 2).Method("foo", "2i", new WlInterface? []{null});
                }

                public static WlInterface FromInterfaceName(string name)
                {
                    return name switch
                    {
                        "simple_interface" => SimpleInterface,
                        _ => throw new ArgumentException($"Unknown interface name: {name}")};
                }
            }
            """;

        fullText.Should().Be(expected);
    }

    [Fact]
    public void CanBuildProtocolWithInOrderDeclaration()
    {
        var doc = new XmlDocument();
        var docText =
            """
            <protocol name="simple_protocol">
                <interface name="simple_interface_a" version="1">
                    <request name="foo">
                        <arg name="bar" type="int" />
                    </request>
                </interface>
                <interface name="simple_interface_b" version="1">
                    <request name="apple">
                        <arg name="banana" type="object" interface="simple_interface_a" />
                    </request>
                </interface>
            </protocol>
            """;
        doc.LoadXml(docText);

        var protocolDefinition = Protocol.FromXml(doc);
        var interfaceCacheBuilder = new WlInterfaceBuilder();

        interfaceCacheBuilder.GenerateCache(protocolDefinition);
        var classDeclaration = interfaceCacheBuilder.Build();
        var fullText = classDeclaration.ToFullString();

        var expected =
            $$"""
            public partial class WlInterface
            {
                public static readonly WlInterface SimpleInterfaceA;
                public static readonly WlInterface SimpleInterfaceB;
                static WlInterface()
                {
                    SimpleInterfaceA = new WlInterface.Builder("simple_interface_a", 1).Method("foo", "i", new WlInterface? []{null});
                    SimpleInterfaceB = new WlInterface.Builder("simple_interface_b", 1).Method("apple", "o", new WlInterface? []{SimpleInterfaceA});
                }

                public static WlInterface FromInterfaceName(string name)
                {
                    return name switch
                    {
                        "simple_interface_a" => SimpleInterfaceA,
                        "simple_interface_b" => SimpleInterfaceB,
                        _ => throw new ArgumentException($"Unknown interface name: {name}")};
                }
            }
            """;

        fullText.Should().Be(expected);
    }

    [Fact]
    public void CanBuildProtocolWithOutOfOrderDeclaration()
    {
        var doc = new XmlDocument();
        var docText =
            """
            <protocol name="simple_protocol">
                <interface name="simple_interface_a" version="1">
                    <request name="apple">
                        <arg name="banana" type="object" interface="simple_interface_b" />
                    </request>
                </interface>
                <interface name="simple_interface_b" version="1">
                    <request name="foo">
                        <arg name="bar" type="int" />
                    </request>
                </interface>
            </protocol>
            """;
        doc.LoadXml(docText);

        var protocolDefinition = Protocol.FromXml(doc);
        var interfaceCacheBuilder = new WlInterfaceBuilder();

        interfaceCacheBuilder.GenerateCache(protocolDefinition);
        var classDeclaration = interfaceCacheBuilder.Build();
        var fullText = classDeclaration.ToFullString();

        var expected =
            $$"""
            public partial class WlInterface
            {
                public static readonly WlInterface SimpleInterfaceA;
                public static readonly WlInterface SimpleInterfaceB;
                static WlInterface()
                {
                    SimpleInterfaceB = new WlInterface.Builder("simple_interface_b", 1).Method("foo", "i", new WlInterface? []{null});
                    SimpleInterfaceA = new WlInterface.Builder("simple_interface_a", 1).Method("apple", "o", new WlInterface? []{SimpleInterfaceB});
                }

                public static WlInterface FromInterfaceName(string name)
                {
                    return name switch
                    {
                        "simple_interface_b" => SimpleInterfaceB,
                        "simple_interface_a" => SimpleInterfaceA,
                        _ => throw new ArgumentException($"Unknown interface name: {name}")};
                }
            }
            """;

        fullText.Should().Be(expected);
    }
}