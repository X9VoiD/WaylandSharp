using System.Xml;
using WaylandSharpGen;
using WaylandSharpGen.Client;

namespace WaylandSharp.Tests.Client;

public class WlClientPinvokeBuilderTest
{
    [Fact]
    public void CanBuild()
    {
        var clientPInvokeBuilder = new WlClientPInvokeBuilder();
        clientPInvokeBuilder.Build();
    }

    [Theory]
    [InlineData("int", "int")]
    [InlineData("uint", "uint")]
    [InlineData("fixed", "int")]
    [InlineData("string", "char*")]
    [InlineData("object", "_WlProxy*")]
    [InlineData("fd", "int")]
    public void GeneratesMarshalFlagsCorrectly(string type, string marshalledType)
    {
        var doc = new XmlDocument();
        var docText = """
            <request name="foo">
              <arg name="bar" type="new_id" />
              <arg name="bar" type="{0}" />
            </request>
            """;
        doc.LoadXml(string.Format(docText, type));

        var protocolMessageDefinition = ProtocolMessageDefinition.FromXml(doc.DocumentElement!, 0);
        var clientPInvokeBuilder = new WlClientPInvokeBuilder();

        clientPInvokeBuilder.GenerateMarshal(protocolMessageDefinition);
        var methodDeclaration = clientPInvokeBuilder.GetMethodDeclaration(protocolMessageDefinition);

        var fullText = methodDeclaration.ToFullString();
        var expectedText = string.Format(
            """
            [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_flags", ExactSpelling = true)]
            public static extern _WlProxy* WlProxyMarshalFlags(_WlProxy* proxy, uint opcode, _WlInterface* @interface, uint version, uint flags, {0} param0);
            """, marshalledType);

        fullText.Should().Be(expectedText);
    }

    [Fact]
    public void GeneratesAscendingParamsName()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <request name="foo">
              <arg name="bar" type="int" />
              <arg name="baz" type="int" />
            </request>
            """);

        var protocolMessageDefinition = ProtocolMessageDefinition.FromXml(doc.DocumentElement!, 0);
        var clientPInvokeBuilder = new WlClientPInvokeBuilder();

        clientPInvokeBuilder.GenerateMarshal(protocolMessageDefinition);
        var methodDeclaration = clientPInvokeBuilder.GetMethodDeclaration(protocolMessageDefinition);

        var fullText = methodDeclaration.ToFullString();
        var expectedText =
            """
            [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_flags", ExactSpelling = true)]
            public static extern _WlProxy* WlProxyMarshalFlags(_WlProxy* proxy, uint opcode, _WlInterface* @interface, uint version, uint flags, int param0, int param1);
            """;

        fullText.Should().Be(expectedText);
    }
}