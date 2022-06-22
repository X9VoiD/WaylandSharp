using System.Xml;
using Microsoft.CodeAnalysis;
using WaylandSharpGen;
using WaylandSharpGen.Client;

namespace WaylandSharp.Tests.Client;

public class WlRegistryBuilderTest
{
    [Fact]
    public void CanBuildEmpty()
    {
        var wlRegistryBuilder = new WlRegistryBuilder();

        var wlRegistryClass = wlRegistryBuilder.Build();
        var fullText = wlRegistryClass.NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class WlRegistry : WlClientObject
            {
                public T Bind<T>(uint name, string interfaceName, uint version = 0)
                    where T : WlClientObject
                {
                    return (T)Bind(name, interfaceName, version);
                }

                public WlClientObject Bind(uint name, string interfaceName, uint version = 0)
                {
                    var interfacePtr = WlInterface.FromInterfaceName(interfaceName).ToBlittable();
                    version = version == 0 ? (uint)interfacePtr->Version : version;
                    var proxy = WlProxyMarshalFlags(_proxyObject, 0, interfacePtr, version, 0, name, interfacePtr->Name, version);
                    return interfaceName switch
                    {
                        _ => throw new WlClientException("Unknown interface")};
                }
            }
            """;

        fullText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildOneInterface()
    {
        var wlRegistryBuilder = new WlRegistryBuilder();
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);
        wlRegistryBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var wlRegistryClass = wlRegistryBuilder.Build();
        var fullText = wlRegistryClass.NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class WlRegistry : WlClientObject
            {
                public T Bind<T>(uint name, string interfaceName, uint version = 0)
                    where T : WlClientObject
                {
                    return (T)Bind(name, interfaceName, version);
                }

                public WlClientObject Bind(uint name, string interfaceName, uint version = 0)
                {
                    var interfacePtr = WlInterface.FromInterfaceName(interfaceName).ToBlittable();
                    version = version == 0 ? (uint)interfacePtr->Version : version;
                    var proxy = WlProxyMarshalFlags(_proxyObject, 0, interfacePtr, version, 0, name, interfacePtr->Name, version);
                    return interfaceName switch
                    {
                        "foo" => new Foo(proxy),
                        _ => throw new WlClientException("Unknown interface")};
                }
            }
            """;

        fullText.Should().Be(expectedText);
    }
}