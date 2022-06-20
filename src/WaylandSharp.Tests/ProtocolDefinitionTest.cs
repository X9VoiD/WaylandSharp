using System.Xml;
using WaylandSharpGen;

namespace WaylandSharp.Tests;

public class ProtocolDefinitionTest
{
    // Simple parsing tests

    [Fact]
    public void CanLoadEmptyProtocol()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <protocol name="wayland">
            </protocol>
            """);

        var protocolDefinition = ProtocolDefinition.FromXml(doc);

        protocolDefinition.Name.Should().Be("wayland");
        protocolDefinition.Interfaces.Should().BeEmpty();
    }

    [Fact]
    public void CanLoadEmptyInterface()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        protocolInterfaceDefinition.Name.Should().Be("foo");
        protocolInterfaceDefinition.Version.Should().Be(1);
        protocolInterfaceDefinition.DocumentationSummary.Should().BeNull();
        protocolInterfaceDefinition.Documentation.Should().BeNull();
        protocolInterfaceDefinition.Enums.Should().BeEmpty();
        protocolInterfaceDefinition.Requests.Should().BeEmpty();
        protocolInterfaceDefinition.Events.Should().BeEmpty();
    }

    [Fact]
    public void CanLoadInterfaceWithDocumentation()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <description summary="a foo">
                The foo interface. blabla
              </description>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        protocolInterfaceDefinition.DocumentationSummary.Should().Be("a foo");
        protocolInterfaceDefinition.Documentation.Should().Be("The foo interface. blabla");
    }

    [Fact]
    public void CanLoadEmptyMessage()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <request name="foo">
            </request>
            """);

        var protocolMessageDefinition = ProtocolMessageDefinition.FromXml(doc.DocumentElement!, 0);

        protocolMessageDefinition.Type.Should().Be(ProtocolMessageType.Request);
        protocolMessageDefinition.Name.Should().Be("foo");
        protocolMessageDefinition.OpCode.Should().Be(0);
        protocolMessageDefinition.DocumentationSummary.Should().BeNull();
        protocolMessageDefinition.Documentation.Should().BeNull();
        protocolMessageDefinition.ExtraTypeAnnotation.Should().BeNull();
        protocolMessageDefinition.Arguments.Should().BeEmpty();
    }

    [Theory]
    [InlineData("request", ProtocolMessageType.Request)]
    [InlineData("event", ProtocolMessageType.Event)]
    internal void CanLoadMessageWithType(string type, ProtocolMessageType expectedType)
    {
        var doc = new XmlDocument();
        var docText =
            """
            <{0} name="sync">
            </{0}>
            """;
        doc.LoadXml(string.Format(docText, type));

        var protocolMessageDefinition = ProtocolMessageDefinition.FromXml(doc.DocumentElement!, 0);

        protocolMessageDefinition.Type.Should().Be(expectedType);
    }

    [Fact]
    public void CanLoadMessageWithDocumentation()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <request name="foo">
              <description summary="a foo">
                A foo method. blabla
              </description>
            </request>
            """);

        var protocolMessageDefinition = ProtocolMessageDefinition.FromXml(doc.DocumentElement!, 0);

        protocolMessageDefinition.DocumentationSummary.Should().Be("a foo");
        protocolMessageDefinition.Documentation.Should().Be("A foo method. blabla");
    }

    [Fact]
    public void CanLoadMessageWithExtraTypeAnnotation()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <request name="foo" type="bar">
            </request>
            """);

        var protocolMessageDefinition = ProtocolMessageDefinition.FromXml(doc.DocumentElement!, 0);

        protocolMessageDefinition.ExtraTypeAnnotation.Should().Be("bar");
    }

    [Fact]
    public void CanLoadEmptyArgument()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <arg name="foo" type="int" />
            """);

        var protocolArgumentDefinition = ProtocolMessageArgumentDefinition.FromXml(doc.DocumentElement!);

        protocolArgumentDefinition.Name.Should().Be("foo");
        protocolArgumentDefinition.Type.Should().Be(ProtocolMessageArgumentType.Int);
        protocolArgumentDefinition.Nullable.Should().BeFalse();
        protocolArgumentDefinition.Interface.Should().BeNull();
        protocolArgumentDefinition.Documentation.Should().BeNull();
    }

    [Theory]
    [InlineData("int", ProtocolMessageArgumentType.Int)]
    [InlineData("uint", ProtocolMessageArgumentType.Uint)]
    [InlineData("fixed", ProtocolMessageArgumentType.Fixed)]
    [InlineData("string", ProtocolMessageArgumentType.String)]
    [InlineData("object", ProtocolMessageArgumentType.Object)]
    [InlineData("new_id", ProtocolMessageArgumentType.NewId)]
    [InlineData("array", ProtocolMessageArgumentType.Array)]
    [InlineData("fd", ProtocolMessageArgumentType.FD)]
    internal void CanLoadArgumentWithType(string type, ProtocolMessageArgumentType expectedType)
    {
        var doc = new XmlDocument();
        var docText =
            """
            <arg name="foo" type="{0}" />
            """;
        doc.LoadXml(string.Format(docText, type));

        var protocolArgumentDefinition = ProtocolMessageArgumentDefinition.FromXml(doc.DocumentElement!);

        protocolArgumentDefinition.Type.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void CanLoadArgumentWithNullable(string nullable, bool expectedNullable)
    {
        var doc = new XmlDocument();
        var docText =
            """
            <arg name="foo" type="int" nullable="{0}"/>
            """;
        doc.LoadXml(string.Format(docText, nullable));

        var protocolArgumentDefinition = ProtocolMessageArgumentDefinition.FromXml(doc.DocumentElement!);

        protocolArgumentDefinition.Nullable.Should().Be(expectedNullable);
    }

    [Fact]
    public void CanLoadArgumentWithInterface()
    {
        var doc = new XmlDocument();
        var docText =
            """
            <arg name="foo" type="int" interface="bar"/>
            """;
        doc.LoadXml(docText);

        var protocolArgumentDefinition = ProtocolMessageArgumentDefinition.FromXml(doc.DocumentElement!);

        protocolArgumentDefinition.Interface.Should().Be("bar");
    }

    [Fact]
    public void CanLoadArgumentWithDocumentation()
    {
        var doc = new XmlDocument();
        var docText =
            """
            <arg name="foo" type="int" interface="bar" summary="a foo" />
            """;
        doc.LoadXml(docText);

        var protocolArgumentDefinition = ProtocolMessageArgumentDefinition.FromXml(doc.DocumentElement!);

        protocolArgumentDefinition.Documentation.Should().Be("a foo");
    }

    [Fact]
    public void CanLoadEmptyEnum()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <enum name="foo">
            </enum>
            """);

        var protocolEnumDefinition = ProtocolEnumDefinition.FromXml(doc.DocumentElement!);

        protocolEnumDefinition.Name.Should().Be("foo");
        protocolEnumDefinition.DocumentationSummary.Should().BeNull();
        protocolEnumDefinition.Documentation.Should().BeNull();
        protocolEnumDefinition.Entries.Should().BeEmpty();
    }

    [Fact]
    public void CanLoadEnumWithDocumentation()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <enum name="foo">
              <description summary="a foo">
                The foo enum. blabla
              </description>
            </enum>
            """);

        var protocolEnumDefinition = ProtocolEnumDefinition.FromXml(doc.DocumentElement!);

        protocolEnumDefinition.DocumentationSummary.Should().Be("a foo");
        protocolEnumDefinition.Documentation.Should().Be("The foo enum. blabla");
    }

    [Fact]
    public void CanLoadEmptyEnumEntry()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <entry name="foo" value="1" />
            """);

        var protocolEnumEntryDefinition = ProtocolEnumEntryDefinition.FromXml(doc.DocumentElement!);

        protocolEnumEntryDefinition.Name.Should().Be("foo");
        protocolEnumEntryDefinition.Value.Should().Be(1);
    }

    [Fact]
    public void CanLoadEnumEntryWithDocumentation()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <entry name="foo" value="1" summary="a foo"/>
            """);

        var protocolEnumEntryDefinition = ProtocolEnumEntryDefinition.FromXml(doc.DocumentElement!);

        protocolEnumEntryDefinition.Documentation.Should().Be("a foo");
    }

    // Compound parsing tests

    [Fact]
    public void CanLoadProtocolWithInterface()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <protocol name="foo">
              <interface name="bar" version="1">
              </interface>
            </protocol>
            """);

        var protocolDefinition = ProtocolDefinition.FromXml(doc);

        protocolDefinition.Name.Should().Be("foo");
        protocolDefinition.Interfaces.Should().HaveCount(1);

        var protocolInterfaceDefinition = protocolDefinition.Interfaces.First();

        protocolInterfaceDefinition.Name.Should().Be("bar");
        protocolInterfaceDefinition.Version.Should().Be(1);
    }

    [Fact]
    public void ProtocolEquality()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <protocol name="foo">
              <interface name="bar" version="1">
              </interface>
            </protocol>
            """);

        var protocolDefinition1 = ProtocolDefinition.FromXml(doc);
        var protocolDefinition2 = ProtocolDefinition.FromXml(doc);

        (protocolDefinition1 == protocolDefinition2).Should().BeTrue();
    }

    [Fact]
    public void CanLoadInterfaceWithEnums()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <enum name="bar">
              </enum>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        protocolInterfaceDefinition.Name.Should().Be("foo");
        protocolInterfaceDefinition.Version.Should().Be(1);
        protocolInterfaceDefinition.Enums.Should().HaveCount(1);

        var protocolEnumDefinition = protocolInterfaceDefinition.Enums.First();

        protocolEnumDefinition.Name.Should().Be("bar");
    }

    [Fact]
    public void CanLoadInterfaceWithMessage()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <request name="bar">
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        protocolInterfaceDefinition.Name.Should().Be("foo");
        protocolInterfaceDefinition.Version.Should().Be(1);
        protocolInterfaceDefinition.Requests.Should().HaveCount(1);

        var protocolMessageDefinition = protocolInterfaceDefinition.Requests.First();

        protocolMessageDefinition.Name.Should().Be("bar");
        protocolMessageDefinition.Type.Should().Be(ProtocolMessageType.Request);
    }

    [Fact]
    public void MessagesHaveIncrementingOpCode()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <request name="bar">
              </request>
              <request name="baz">
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        var barRequest = protocolInterfaceDefinition.Requests.First(r => r.Name == "bar");
        var bazRequest = protocolInterfaceDefinition.Requests.First(r => r.Name == "baz");

        barRequest.OpCode.Should().Be(0);
        bazRequest.OpCode.Should().Be(1);
    }

    [Fact]
    public void MessagesWithDifferentTypesIncrementOpCodeSeparately()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <request name="bar">
              </request>
              <event name="baz">
              </event>
              <request name="bar2">
              </request>
              <event name="baz2">
              </event>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        var barRequest = protocolInterfaceDefinition.Requests.First(r => r.Name == "bar");
        var bazRequest = protocolInterfaceDefinition.Events.First(r => r.Name == "baz");
        var bar2Request = protocolInterfaceDefinition.Requests.First(r => r.Name == "bar2");
        var baz2Request = protocolInterfaceDefinition.Events.First(r => r.Name == "baz2");

        barRequest.OpCode.Should().Be(0);
        bazRequest.OpCode.Should().Be(0);
        bar2Request.OpCode.Should().Be(1);
        baz2Request.OpCode.Should().Be(1);
    }

    [Fact]
    public void CanLoadInterfaceWithEnum()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <enum name="bar">
              </enum>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        protocolInterfaceDefinition.Name.Should().Be("foo");
        protocolInterfaceDefinition.Version.Should().Be(1);
        protocolInterfaceDefinition.Enums.Should().HaveCount(1);

        var protocolEnumDefinition = protocolInterfaceDefinition.Enums.First();

        protocolEnumDefinition.Name.Should().Be("bar");
    }

    [Fact]
    public void CanLoadMessageWithArgument()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <request name="bar">
                <arg name="baz" type="int" />
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = ProtocolInterfaceDefinition.FromXml(doc.DocumentElement!);

        var protocolMessageDefinition = protocolInterfaceDefinition.Requests.First();

        protocolMessageDefinition.Arguments.Should().HaveCount(1);

        var protocolArgumentDefinition = protocolMessageDefinition.Arguments.First();

        protocolArgumentDefinition.Name.Should().Be("baz");
        protocolArgumentDefinition.Type.Should().Be(ProtocolMessageArgumentType.Int);
    }

    [Fact]
    public void CanLoadEnumWithEnumEntry()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <enum name="foo">
              <entry name="bar" value="1" />
            </enum>
            """);

        var protocolEnumDefinition = ProtocolEnumDefinition.FromXml(doc.DocumentElement!);

        protocolEnumDefinition.Name.Should().Be("foo");
        protocolEnumDefinition.Entries.Should().HaveCount(1);

        var protocolEnumEntryDefinition = protocolEnumDefinition.Entries.First();

        protocolEnumEntryDefinition.Name.Should().Be("bar");
        protocolEnumEntryDefinition.Value.Should().Be(1);
    }

}