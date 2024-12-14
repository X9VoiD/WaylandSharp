using System.Collections.Immutable;
using System.Text;
using System.Xml;
using WaylandSharpGen.Client;
using WaylandSharpGen.Xml;
using static WaylandSharpGen.WlCommonIdentifiers;

namespace WaylandSharpGen;

/*
 * Use the following regex to commonize schtuff from text.
 * Match:(?<!{_?)(_?Wl)([a-zA-Z]*)(?!})
 * Substitution: {{$1$2TypeName}}
 */

internal enum GenerationOption
{
    Unknown,
    Client,
    Server
}

[Generator(LanguageNames.CSharp)]
internal class WlBindingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var mainPipeline = context.AdditionalTextsProvider
            .Where(static t => Path.GetExtension(t.Path) == ".xml")
            .Collect()
            .Combine(context.AnalyzerConfigOptionsProvider)
            .SelectMany(static (t, cts) =>
            {
                var analyzerConfigOptions = t.Right;
                var protocolRecords = ImmutableArray.CreateBuilder<(AdditionalText, GenerationOption)>();
                foreach (var xmlFile in t.Left)
                {
                    if (cts.IsCancellationRequested)
                        cts.ThrowIfCancellationRequested();

                    if (analyzerConfigOptions.GetOptions(xmlFile)
                        .TryGetValue("build_metadata.AdditionalFiles.WaylandProtocol", out var generationOption))
                    {
                        var option = generationOption switch
                        {
                            "client" => GenerationOption.Client,
                            "server" => GenerationOption.Server,
                            _ => GenerationOption.Unknown,
                        };
                        protocolRecords.Add((xmlFile, option));
                    }
                }

                return protocolRecords.ToImmutable();
            })
            .Select(static (t, cts) =>
            {
                var xmlDoc = new XmlDocument();
                var xml = t.Item1.GetText(cts)!.ToString();
                xmlDoc.LoadXml(xml);

                return (Protocol.FromXml(xmlDoc), t.Item2);
            });

        var clientPipeline = mainPipeline
            .Where(static t => t.Item2 == GenerationOption.Client)
            .Combine(context.CompilationProvider)
            .Select(static (t, cts) => (Protocol: t.Left.Item1, CompilationOptions: t.Right.Options))
            .Collect();

        context.RegisterSourceOutput(clientPipeline, (ctx, a) =>
        {
            var wlClientBuilder = new WlClientBuilder();
            foreach (var tup in a)
            {
                wlClientBuilder.CompilationOptions = tup.CompilationOptions;
                wlClientBuilder.ProcessProtocolDefinition(tup.Protocol);
            }

            var compilationUnit = wlClientBuilder
                .BuildAsCompilationUnit()
                .GetText(Encoding.UTF8);

            ctx.AddSource("WaylandSharp.Generated.cs", compilationUnit);
        });
    }

    public static SyntaxList<MemberDeclarationSyntax> GenerateCommonDefinitions()
    {
        var definitions = CSharpSyntaxTree.ParseText(_commonDefinitions);
        var root = definitions.GetCompilationUnitRoot();
        return root.Members;
    }

    private const string _commonDefinitions =
$$"""
[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct {{_WlMessageTypeName}}
{
    public readonly char* Name;
    public readonly char* Signature;
    public readonly {{_WlInterfaceTypeName}}** Types;

    public {{_WlMessageTypeName}}(char* name, char* signature, {{_WlInterfaceTypeName}}** types)
    {
        Name = name;
        Signature = signature;
        Types = types;
    }
}

public class {{WlMessageTypeName}}
{
    public string Name { get; }
    public string Signature { get; }
    public ImmutableArray<{{WlInterfaceTypeName}}?> Types { get; }

    internal {{WlMessageTypeName}}(string name, string signature, ImmutableArray<{{WlInterfaceTypeName}}?> types)
    {
        Name = name;
        Signature = signature;
        Types = types;
    }

    internal unsafe {{_WlMessageTypeName}} ToBlittable()
    {
        var rawName = (char*)Marshal.StringToHGlobalAnsi(Name);
        var rawSignature = (char*)Marshal.StringToHGlobalAnsi(Signature);
        Span<IntPtr> rawTypesSpan = GC.AllocateArray<IntPtr>(Types.Length);
        var rawTypes = rawTypesSpan.Length > 0
            ? ({{_WlInterfaceTypeName}}**)Unsafe.AsPointer(ref rawTypesSpan[0])
            : null;

        for (var i = 0; i < Types.Length; i++)
        {
            rawTypes[i] = Types[i] is { } type
                ? type.ToBlittable()
                : ({{_WlInterfaceTypeName}}*)null;
        }

        return new {{_WlMessageTypeName}}(rawName, rawSignature, rawTypes);
    }

    internal class Builder
    {
        private readonly string _name;
        private readonly string _signature;
        private readonly ImmutableArray<{{WlInterfaceTypeName}}?> _types;

        public Builder(string name, string signature, {{WlInterfaceTypeName}}?[] types)
        {
            _name = name;
            _signature = signature;
            _types = types.ToImmutableArray();
        }

        public {{WlMessageTypeName}} Build()
        {
            return new {{WlMessageTypeName}}(_name, _signature, _types);
        }

        public static implicit operator {{WlMessageTypeName}}(Builder builder) => builder.Build();
    }
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct {{_WlInterfaceTypeName}}
{
    public readonly char* Name;
    public readonly int Version;
    public readonly int MethodCount;
    public readonly {{_WlMessageTypeName}}* Methods;
    public readonly int EventCount;
    public readonly {{_WlMessageTypeName}}* Events;

    public {{_WlInterfaceTypeName}}(char* name, int version, int methodCount, {{_WlMessageTypeName}}* methods, int eventCount, {{_WlMessageTypeName}}* events)
    {
        Name = name;
        Version = version;
        MethodCount = methodCount;
        Methods = methods;
        EventCount = eventCount;
        Events = events;
    }
}

public unsafe partial class {{WlInterfaceTypeName}}
{
    private readonly {{_WlInterfaceTypeName}}* _blittable;

    public string Name { get; }
    public int Version { get; }
    public ImmutableArray<{{WlMessageTypeName}}> Methods { get; }
    public ImmutableArray<{{WlMessageTypeName}}> Events { get; }

    internal unsafe {{WlInterfaceTypeName}}(string name, int version, ImmutableArray<{{WlMessageTypeName}}> methods, ImmutableArray<{{WlMessageTypeName}}> events)
    {
        Name = name;
        Version = version;
        Methods = methods;
        Events = events;

        var rawName = (char*)Marshal.StringToHGlobalAnsi(Name);
        Span<{{_WlMessageTypeName}}> rawMethodSpan = GC.AllocateArray<{{_WlMessageTypeName}}>(Methods.Length, true);
        var rawMethods = rawMethodSpan.Length > 0
            ? ({{_WlMessageTypeName}}*)Unsafe.AsPointer(ref rawMethodSpan[0])
            : null;
        Span<{{_WlMessageTypeName}}> rawEventSpan = GC.AllocateArray<{{_WlMessageTypeName}}>(Events.Length, true);
        var rawEvents = rawEventSpan.Length > 0
            ? ({{_WlMessageTypeName}}*)Unsafe.AsPointer(ref rawEventSpan[0])
            : null;

        for (var i = 0; i < Methods.Length; ++i)
        {
            var method = Methods[i];
            rawMethods[i] = method.ToBlittable();
        }

        for (var i = 0; i < Events.Length; ++i)
        {
            var event_ = Events[i];
            rawEvents[i] = event_.ToBlittable();
        }

        _blittable = ({{_WlInterfaceTypeName}}*)Marshal.AllocHGlobal(sizeof({{_WlInterfaceTypeName}}));
        *_blittable = new {{_WlInterfaceTypeName}}(rawName, Version, Methods.Length, rawMethods, Events.Length, rawEvents);
    }

    internal {{_WlInterfaceTypeName}}* ToBlittable()
    {
        return _blittable;
    }

    internal class Builder
    {
        private readonly string _name;
        private readonly int _version;
        private readonly ImmutableArray<{{WlMessageTypeName}}>.Builder _methods;
        private readonly ImmutableArray<{{WlMessageTypeName}}>.Builder _events;

        public Builder(string name, int version)
        {
            _name = name;
            _version = version;
            _methods = ImmutableArray.CreateBuilder<{{WlMessageTypeName}}>();
            _events = ImmutableArray.CreateBuilder<{{WlMessageTypeName}}>();
        }

        public Builder Method(string name, string signature, {{WlInterfaceTypeName}}?[] types)
        {
            _methods.Add(new {{WlMessageTypeName}}.Builder(name, signature, types));
            return this;
        }

        public Builder Event(string name, string signature, {{WlInterfaceTypeName}}?[] types)
        {
            _events.Add(new {{WlMessageTypeName}}.Builder(name, signature, types));
            return this;
        }

        public {{WlInterfaceTypeName}} Build()
        {
            return new {{WlInterfaceTypeName}}(_name, _version, _methods.ToImmutable(), _events.ToImmutable());
        }

        public static implicit operator {{WlInterfaceTypeName}}(Builder builder) => builder.Build();
    }
}

#pragma warning disable CS0649

internal readonly unsafe struct {{_WlListTypeName}}
{
    public readonly {{_WlListTypeName}}* Prev;
    public readonly {{_WlListTypeName}}* Next;
}

internal readonly unsafe struct {{_WlArrayTypeName}}
{
    public readonly int Size;
    public readonly int Alloc;
    public readonly void* Data;
}

internal readonly struct {{_WlFixedTTypeName}} : IEquatable<{{_WlFixedTTypeName}}>
{
    private readonly uint _value;

    public {{_WlFixedTTypeName}}(double d)
    {
        _value = (uint) (d * 256.0);
    }

    public double ToDouble()
    {
        return _value / 256.0;
    }

    public bool Equals({{_WlFixedTTypeName}} other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is {{_WlFixedTTypeName}} t && Equals(t);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public static bool operator ==({{_WlFixedTTypeName}} left, {{_WlFixedTTypeName}} right)
    {
        return left.Equals(right);
    }

    public static bool operator !=({{_WlFixedTTypeName}} left, {{_WlFixedTTypeName}} right)
    {
        return !(left == right);
    }
}

#pragma warning restore CS0649

[StructLayout(LayoutKind.Explicit)]
internal readonly unsafe struct {{_WlArgumentTypeName}}
{
    [FieldOffset(0)] public readonly int i;
    [FieldOffset(0)] public readonly uint u;
    [FieldOffset(0)] public readonly {{_WlFixedTTypeName}} f;
    [FieldOffset(0)] public readonly char* s;
    [FieldOffset(0)] public readonly void* o;
    [FieldOffset(0)] public readonly void* n;
    [FieldOffset(0)] public readonly {{_WlArrayTypeName}}* a;
    [FieldOffset(0)] public readonly int h;
}

internal unsafe delegate int {{_WlDispatcherFuncTTypeName}}(void* data,
                                             void* target,
                                             uint callbackOpcode,
                                             {{_WlMessageTypeName}}* messageSignature,
                                             {{_WlArgumentTypeName}}* args);
""";
}