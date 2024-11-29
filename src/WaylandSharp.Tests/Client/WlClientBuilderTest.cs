using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using WaylandSharpGen;
using WaylandSharpGen.Client;
using WaylandSharpGen.Xml;
using static WaylandSharpGen.Client.WlClientIdentifiers;

namespace WaylandSharp.Tests.Client;

public class WlClientBuilderTest
{
    private const int _baseCount = 19;
    private const int _customMemberIndex = _baseCount - 12;

    [Fact]
    public void CanBuildEmpty()
    {
        var wlClientBuilder = new WlClientBuilder();

        var members = wlClientBuilder.BuildMembers();

        members.Should().HaveCount(_baseCount);
    }

    [Fact]
    public void CanBuildEmptyInterface()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildInterfaceWithOneEmptyEvent()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <event name="bar">
              </event>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public class BarEventArgs : EventArgs
                {
                    public BarEventArgs()
                    {
                    }
                }

                event EventHandler<BarEventArgs>? _bar;
                public event EventHandler<BarEventArgs>? Bar
                {
                    add
                    {
                        CheckIfDisposed();
                        _bar += value;
                        HookDispatcher();
                    }

                    remove => _bar -= value;
                }

                internal sealed override _WlDispatcherFuncT CreateDispatcher()
                {
                    int dispatcher(void* data, void* target, uint callbackOpcode, _WlMessage* messageSignature, _WlArgument* args)
                    {
                        switch (callbackOpcode)
                        {
                            case 0:
                                _bar?.Invoke(this, new BarEventArgs());
                                break;
                            default:
                                throw new WlClientException("Unknown event opcode");
                        }

                        return 0;
                    }

                    return dispatcher;
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildInterfaceWithOneEvent()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <event name="bar">
                <arg name="baz" type="int" />
              </event>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public class BarEventArgs : EventArgs
                {
                    public int Baz { get; }

                    public BarEventArgs(int baz)
                    {
                        Baz = baz;
                    }
                }

                event EventHandler<BarEventArgs>? _bar;
                public event EventHandler<BarEventArgs>? Bar
                {
                    add
                    {
                        CheckIfDisposed();
                        _bar += value;
                        HookDispatcher();
                    }

                    remove => _bar -= value;
                }

                internal sealed override _WlDispatcherFuncT CreateDispatcher()
                {
                    int dispatcher(void* data, void* target, uint callbackOpcode, _WlMessage* messageSignature, _WlArgument* args)
                    {
                        switch (callbackOpcode)
                        {
                            case 0:
                                var barArg0 = args[0].i;
                                _bar?.Invoke(this, new BarEventArgs(barArg0));
                                break;
                            default:
                                throw new WlClientException("Unknown event opcode");
                        }

                        return 0;
                    }

                    return dispatcher;
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildProtocolWithOneEventWithEnum()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <enum name="qux">
                <entry name="a" value="0" />
                <entry name="b" value="0x25" />
              </enum>
              <event name="bar">
                <arg name="baz" type="uint" enum="foo" />
              </event>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        // +1 for the enum
        members.Should().HaveCount(_baseCount + 2);
        // enum will be inserted where the class was
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public enum FooQux : uint
            {
                A = 0,
                B = 37
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildProtocolWithOneEventAndMarshalsAllTypesCorrectly()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <enum name="bar_something">
                <entry name="bar_something_1" value="1" />
              </enum>
              <event name="bar">
                <arg name="bar_int" type="int" />
                <arg name="bar_uint" type="uint" />
                <arg name="bar_enum" type="uint" enum="bar_something" />
                <arg name="bar_fixed" type="fixed" />
                <arg name="bar_string" type="string" />
                <arg name="bar_object" type="object" interface="baz" />
                <arg name="bar_interfaceless_object" type="object" />
                <arg name="bar_newid" type="new_id" interface="oh_nyo" />
                <arg name="bar_array" type="array" />
                <arg name="bar_fd" type="fd" />
              </event>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        // The enum is added as a member of the namespace, not the class.
        members.Should().HaveCount(_baseCount + 2);
        var protocolImplementationText = members[_customMemberIndex + 1].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            $$"""
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public class BarEventArgs : EventArgs
                {
                    public int BarInt { get; }

                    public uint BarUint { get; }

                    public FooBarSomething BarEnum { get; }

                    public double BarFixed { get; }

                    public string BarString { get; }

                    public Baz BarObject { get; }

                    public {{WlClientObjectTypeName}} BarInterfacelessObject { get; }

                    public OhNyo BarNewid { get; }

                    public WlArray BarArray { get; }

                    public int BarFd { get; }

                    public BarEventArgs(int bar_int, uint bar_uint, FooBarSomething bar_enum, double bar_fixed, string bar_string, Baz bar_object, {{WlClientObjectTypeName}} bar_interfaceless_object, OhNyo bar_newid, WlArray bar_array, int bar_fd)
                    {
                        BarInt = bar_int;
                        BarUint = bar_uint;
                        BarEnum = bar_enum;
                        BarFixed = bar_fixed;
                        BarString = bar_string;
                        BarObject = bar_object;
                        BarInterfacelessObject = bar_interfaceless_object;
                        BarNewid = bar_newid;
                        BarArray = bar_array;
                        BarFd = bar_fd;
                    }
                }

                event EventHandler<BarEventArgs>? _bar;
                public event EventHandler<BarEventArgs>? Bar
                {
                    add
                    {
                        CheckIfDisposed();
                        _bar += value;
                        HookDispatcher();
                    }

                    remove => _bar -= value;
                }

                internal sealed override _WlDispatcherFuncT CreateDispatcher()
                {
                    int dispatcher(void* data, void* target, uint callbackOpcode, _WlMessage* messageSignature, _WlArgument* args)
                    {
                        switch (callbackOpcode)
                        {
                            case 0:
                                var barArg0 = args[0].i;
                                var barArg1 = args[1].u;
                                var barArg2 = (FooBarSomething)args[2].u;
                                var barArg3 = WlFixedToDouble(args[3].f);
                                var barArg4 = Marshal.PtrToStringAnsi((IntPtr)args[4].s)!;
                                var barArg5 = WlClientObject.GetObject<Baz>((_WlProxy*)args[5].o);
                                var barArg6 = WlClientObject.GetObject((_WlProxy*)args[6].o);
                                var barArg7 = new OhNyo((_WlProxy*)args[7].n);
                                var barArg8 = new WlArray(args[8].a);
                                var barArg9 = args[9].h;
                                _bar?.Invoke(this, new BarEventArgs(barArg0, barArg1, barArg2, barArg3, barArg4, barArg5, barArg6, barArg7, barArg8, barArg9));
                                break;
                            default:
                                throw new WlClientException("Unknown event opcode");
                        }

                        return 0;
                    }

                    return dispatcher;
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildProtocolWithEmptyRequest()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <request name="bar">
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public void Bar()
                {
                    CheckIfDisposed();
                    WlProxyMarshalFlags(_proxyObject, 0, null, WlProxyGetVersion(_proxyObject), 0);
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildProtocolWithOneRequest()
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

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public void Bar(int baz)
                {
                    CheckIfDisposed();
                    var arg0 = baz;
                    WlProxyMarshalFlags(_proxyObject, 0, null, WlProxyGetVersion(_proxyObject), 0, arg0);
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildProtocolWithOneRequest_NewId()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <request name="bar">
                <arg name="baz" type="new_id" interface="some_interface" />
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public SomeInterface Bar()
                {
                    CheckIfDisposed();
                    var interfacePtr = WlInterface.SomeInterface.ToBlittable();
                    var arg0 = (_WlProxy*)null;
                    var newId = WlProxyMarshalFlags(_proxyObject, 0, interfacePtr, WlProxyGetVersion(_proxyObject), 0, arg0);
                    return new SomeInterface(newId);
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildProtocolWithOneRequestAndMarshalAllTypesCorrectly()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <enum name="bar_something">
                <entry name="bar_something_1" value="1" />
              </enum>
              <request name="bar">
                <arg name="bar_int" type="int" />
                <arg name="bar_uint" type="uint" />
                <arg name="bar_enum" type="uint" enum="bar_something" />
                <arg name="bar_fixed" type="fixed" />
                <arg name="bar_string" type="string" />
                <arg name="bar_object" type="object" interface="baz" />
                <arg name="bar_interfaceless_object" type="object" />
                <arg name="bar_newid" type="new_id" interface="oh_nyo" />
                <arg name="bar_array" type="array" />
                <arg name="bar_fd" type="fd" />
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 2);
        var protocolImplementationText = members[_customMemberIndex + 1].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public OhNyo Bar(int bar_int, uint bar_uint, FooBarSomething bar_enum, double bar_fixed, string bar_string, Baz bar_object, WlClientObject bar_interfaceless_object, WlArray bar_array, int bar_fd)
                {
                    CheckIfDisposed();
                    var interfacePtr = WlInterface.OhNyo.ToBlittable();
                    var arg0 = bar_int;
                    var arg1 = bar_uint;
                    var arg2 = (uint)bar_enum;
                    var arg3 = WlFixedFromDouble(bar_fixed);
                    var arg4 = (char*)Marshal.StringToHGlobalAnsi(bar_string)!;
                    var arg5 = bar_object._proxyObject;
                    var arg6 = bar_interfaceless_object._proxyObject;
                    var arg7 = (_WlProxy*)null;
                    var arg8 = (_WlArray*)bar_array.RawPointer;
                    var arg9 = bar_fd;
                    var newId = WlProxyMarshalFlags(_proxyObject, 0, interfacePtr, WlProxyGetVersion(_proxyObject), 0, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                    return new OhNyo(newId);
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    // Github Copilot somehow likes qux, so.... quux
    [Fact]
    public void CanBuildProtocolWithEventAndRequest()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="foo" version="1">
              <event name="bar">
                <arg name="baz" type="int" />
              </event>
              <request name="qux">
                <arg name="quux" type="int" />
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class Foo : WlClientObject
            {
                public Foo(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal Foo(_WlProxy* proxyObject) : base(proxyObject)
                {
                }

                public class BarEventArgs : EventArgs
                {
                    public int Baz { get; }

                    public BarEventArgs(int baz)
                    {
                        Baz = baz;
                    }
                }

                event EventHandler<BarEventArgs>? _bar;
                public event EventHandler<BarEventArgs>? Bar
                {
                    add
                    {
                        CheckIfDisposed();
                        _bar += value;
                        HookDispatcher();
                    }

                    remove => _bar -= value;
                }

                internal sealed override _WlDispatcherFuncT CreateDispatcher()
                {
                    int dispatcher(void* data, void* target, uint callbackOpcode, _WlMessage* messageSignature, _WlArgument* args)
                    {
                        switch (callbackOpcode)
                        {
                            case 0:
                                var barArg0 = args[0].i;
                                _bar?.Invoke(this, new BarEventArgs(barArg0));
                                break;
                            default:
                                throw new WlClientException("Unknown event opcode");
                        }

                        return 0;
                    }

                    return dispatcher;
                }

                public void Qux(int quux)
                {
                    CheckIfDisposed();
                    var arg0 = quux;
                    WlProxyMarshalFlags(_proxyObject, 0, null, WlProxyGetVersion(_proxyObject), 0, arg0);
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void SkipsWlRegistryBind()
    {
        var doc = new XmlDocument();
        doc.LoadXml(
            """
            <interface name="wl_registry" version="1">
              <request name="bind">
                <arg name="name" type="uint" />
                <arg name="id" type="new_id" />
              </request>
            </interface>
            """);

        var protocolInterfaceDefinition = Interface.FromXml(doc.DocumentElement!);
        var wlClientBuilder = new WlClientBuilder();
        wlClientBuilder.ProcessInterfaceDefinition(protocolInterfaceDefinition);

        var members = wlClientBuilder.BuildMembers();
        members.Should().HaveCount(_baseCount + 1);
        var protocolImplementationText = members[_customMemberIndex].NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

        var expectedText =
            """
            public unsafe partial class WlRegistry : WlClientObject
            {
                public WlRegistry(System.IntPtr proxyObject) : base((_WlProxy*)proxyObject)
                {
                }

                internal WlRegistry(_WlProxy* proxyObject) : base(proxyObject)
                {
                }
            }
            """;

        protocolImplementationText.Should().Be(expectedText);
    }

    [Fact]
    public void CanBuildWaylandCoreProtocolAndCompile()
    {
        var compilation = CreateCompilation();
        var generator = new WlBindingGenerator();
        var analyzerConfigOptions = new CustomAnalyzerConfigOptionsProvider();
        analyzerConfigOptions.Setup(CustomAnalyzerConfigOptionsProvider.AllTexts, new()
        {
            ["build_metadata.AdditionalFiles.WaylandProtocol"] = "client"
        });

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(new AdditionalText[] { new CustomAdditionalText("wayland.xml", WaylandProtocol.Text) }.ToImmutableArray())
            .WithUpdatedAnalyzerConfigOptions(analyzerConfigOptions);

        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        diagnostics.IsEmpty.Should().BeTrue();
        var outputDiagnostics = output.GetDiagnostics();

        // TODO: fix possible CS8604 warnings
        outputDiagnostics.Where(d => d.Id != "CS8604").Should().BeEmpty();
    }

    private class CustomAdditionalText : AdditionalText
    {
        private readonly string _text;

        public override string Path { get; }

        public CustomAdditionalText(string path, string content)
        {
            Path = path;
            _text = content;
        }

        public override SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
        {
            return SourceText.From(_text);
        }
    }

    private class CustomAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly List<Entry<AdditionalText>> _additionalTextOptions = new();

        public override AnalyzerConfigOptions GlobalOptions => new CustomAnalyzerConfigOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return EmptyOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return _additionalTextOptions.FirstOrDefault(e => e.Condition(textFile))?.Create() ?? EmptyOptions;
        }

        public void Setup(Func<AdditionalText, bool> condition, Dictionary<string, string> map)
        {
            _additionalTextOptions.Add(new Entry<AdditionalText>(condition, map));

            var a = new ConcurrentDictionary<string, int>();
            a.Remove("foo", out _);
        }

        public static Func<AdditionalText, bool> AllTexts => static _ => true;

        private static readonly CustomAnalyzerConfigOptions EmptyOptions = new();

        private class Entry<T>
        {
            public Func<T, bool> Condition { get; }
            public Dictionary<string, string> Map { get; }

            public Entry(Func<T, bool> condition, Dictionary<string, string> map)
            {
                Condition = condition;
                Map = map;
            }

            public CustomAnalyzerConfigOptions Create()
            {
                return new CustomAnalyzerConfigOptions(Map);
            }
        }
    }

    internal class CustomAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _map;

        public CustomAnalyzerConfigOptions() => _map = new Dictionary<string, string>();
        public CustomAnalyzerConfigOptions(Dictionary<string, string> map) => _map = map;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return _map.TryGetValue(key, out value);
        }
    }

    private static Compilation CreateCompilation()
    {
        return CSharpCompilation.Create("compilation",
            syntaxTrees: null,
            references: new[]{
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ConcurrentDictionary<,>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CollectionExtensions).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
                MetadataReference.CreateFromFile(typeof(Unsafe).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    }
}