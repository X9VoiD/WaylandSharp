using static WaylandSharpGen.WlCommonIdentifiers;
using static WaylandSharpGen.Client.WlClientIdentifiers;

namespace WaylandSharpGen.Client;

internal class WlClientPInvokeBuilder
{
    private readonly Dictionary<string, MethodDeclarationSyntax> _marshal = new();

    private readonly ClassDeclarationSyntax _syntax;

    public WlClientPInvokeBuilder()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(_clientPinvoke);
        var root = syntaxTree.GetCompilationUnitRoot();
        _syntax = root.Members.OfType<ClassDeclarationSyntax>().First();
    }

    public void GenerateMarshal(ProtocolMessageDefinition definition)
    {
        var signature = definition.ToSignature();
        var hash = signature.AsHash();
        if (hash == "usu")
            return;
        if (_marshal.ContainsKey(hash))
            return;

        // if (definition.Arguments.Any(a => a.Type == ProtocolMessageArgumentType.NewId))
        // {
        //     GenerateMarshalFlags(definition, signature);
        //     return;
        // }
        // else
        // {
        //     GenerateMarshalFlags(definition, signature);
        //     GenerateMarshal(definition, signature);
        // }

        GenerateMarshalFlags(definition, hash);
    }

    public ClassDeclarationSyntax Build()
    {
        var memberDeclarationList = _syntax.Members.AddRange(_marshal.Values);
        return _syntax.WithMembers(memberDeclarationList).NormalizeWhitespace(eol: Environment.NewLine);
    }

    internal MethodDeclarationSyntax GetMethodDeclaration(ProtocolMessageDefinition definition)
    {
        var signature = definition.ToSignature().AsHash();
        return !definition.Arguments.Any(a => a.Type == ProtocolMessageArgumentType.Array)
            ? _marshal[signature].NormalizeWhitespace(eol: Environment.NewLine)
            : throw new NotSupportedException();
    }

    // private void GenerateMarshal(ProtocolMessageDefinition definition, Signature signature)
    // {
    //     /*
    //      * [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal", ExactSpelling = true)]
    //      * public static extern void WlProxyMarshal(_WlProxy* proxy, uint opcode, __arglist);
    //      */

    //     var arguments = definition.Arguments
    //         .Where(a => a.Type != ProtocolMessageArgumentType.NewId)
    //         .ToArray();

    //     var parameters = new List<ParameterSyntax>
    //     {
    //         Parameter(Identifier("proxy"))
    //             .WithType(_WlProxyPointerSyntax),
    //         Parameter(Identifier("opcode"))
    //             .WithType(PredefinedType(Token(SyntaxKind.UIntKeyword)))
    //     };
    //     for (var i = 0; i < arguments.Length; i++)
    //     {
    //         var argument = arguments[i];
    //         var type = GetMarshalledType(argument);
    //         parameters.Add(Parameter(Identifier($"param{i}")).WithType(type));
    //     }

    //     var attribute = GeneratePInvokeAttribute("wl_proxy_marshal");
    //     var method = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier("WlProxyMarshal"))
    //         .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ExternKeyword)))
    //         .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(attribute))))
    //         .WithParameterList(ParameterList(SeparatedList(parameters)))
    //         .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    //     _marshal.Add(signature, method);
    // }

    private void GenerateMarshalFlags(ProtocolMessageDefinition definition, string hash)
    {
        /*
         * [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_flags", ExactSpelling = true)]
         * public static extern _WlProxy* WlProxyMarshalFlags(_WlProxy* proxy, uint opcode,
         *                                                    _WlInterface* @interface,
         *                                                    uint version,
         *                                                    uint flags,
         *                                                    __arglist);
         */

        var arguments = definition.Arguments;

        var parameters = new List<ParameterSyntax>
        {
            Parameter(Identifier("proxy"))
                .WithType(_WlProxyPointerSyntax),
            Parameter(Identifier("opcode"))
                .WithType(PredefinedType(Token(SyntaxKind.UIntKeyword))),
            Parameter(Identifier("@interface"))
                .WithType(_WlInterfacePointerSyntax),
            Parameter(Identifier("version"))
                .WithType(PredefinedType(Token(SyntaxKind.UIntKeyword))),
            Parameter(Identifier("flags"))
                .WithType(PredefinedType(Token(SyntaxKind.UIntKeyword))),
        };
        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var type = GetMarshalledType(argument);
            parameters.Add(Parameter(Identifier($"param{i}")).WithType(type));
        }

        var attribute = GeneratePInvokeAttribute("wl_proxy_marshal_flags");
        var method = MethodDeclaration(_WlProxyPointerSyntax, Identifier("WlProxyMarshalFlags"))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ExternKeyword)))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(attribute))))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        _marshal.Add(hash, method);
    }

    private static TypeSyntax GetMarshalledType(ProtocolMessageArgumentDefinition argument)
    {
        return argument.Type switch
        {
            ProtocolMessageArgumentType.Int => PredefinedType(Token(SyntaxKind.IntKeyword)),
            ProtocolMessageArgumentType.Uint => PredefinedType(Token(SyntaxKind.UIntKeyword)),
            ProtocolMessageArgumentType.Fixed => PredefinedType(Token(SyntaxKind.IntKeyword)),
            ProtocolMessageArgumentType.String => PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
            ProtocolMessageArgumentType.Object => _WlProxyPointerSyntax,
            ProtocolMessageArgumentType.NewId => _WlProxyPointerSyntax,
            ProtocolMessageArgumentType.Array => _WlArrayPointerSyntax,
            ProtocolMessageArgumentType.FD => PredefinedType(Token(SyntaxKind.IntKeyword)),
            _ => throw new InvalidOperationException($"Invalid type encountered: {argument.Type}"),
        };
    }

    private static AttributeSyntax GeneratePInvokeAttribute(string functionName)
    {
        return Attribute(IdentifierName("DllImport"))
                .WithArgumentList(AttributeArgumentList(
                    SeparatedList(new[] {
                        AttributeArgument(
                            IdentifierName("LibWaylandClient")),
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(functionName)))
                        .WithNameEquals(
                            NameEquals(
                                IdentifierName("EntryPoint"))),
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.TrueLiteralExpression))
                        .WithNameEquals(
                            NameEquals(
                                IdentifierName("ExactSpelling")))})));
    }

    private const string _clientPinvoke =
$$"""
#pragma warning disable CA5392
internal static unsafe class Client
{
    private const string LibWaylandClient = "libwayland-client.so.0";

    [DllImport(LibWaylandClient, EntryPoint = "wl_array_init", ExactSpelling = true)]
    public static extern void WlArrayInit({{_WlArrayTypeName}}* array);

    [DllImport(LibWaylandClient, EntryPoint = "wl_array_release", ExactSpelling = true)]
    public static extern void WlArrayRelease({{_WlArrayTypeName}}* array);

    [DllImport(LibWaylandClient, EntryPoint = "wl_array_add", ExactSpelling = true)]
    public static extern void* WlArrayAdd({{_WlArrayTypeName}}* array, int size);

    [DllImport(LibWaylandClient, EntryPoint = "wl_array_copy", ExactSpelling = true)]
    public static extern int WlArrayCopy({{_WlArrayTypeName}}* array, {{_WlArrayTypeName}}* source);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_create_queue", ExactSpelling = true)]
    public static extern {{_WlEventQueueTypeName}}* WlDisplayCreateQueue({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_connect_to_fd", ExactSpelling = true)]
    public static extern {{_WlDisplayTypeName}}* WlDisplayConnectToFd(int fd);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_connect", ExactSpelling = true)]
    public static extern {{_WlDisplayTypeName}}* WlDisplayConnect(char* name);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_disconnect", ExactSpelling = true)]
    public static extern void WlDisplayDisconnect({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_get_fd", ExactSpelling = true)]
    public static extern int WlDisplayGetFd({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_roundtrip_queue", ExactSpelling = true)]
    public static extern int WlDisplayRoundtripQueue({{_WlDisplayTypeName}}* display, {{_WlEventQueueTypeName}}* queue);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_roundtrip", ExactSpelling = true)]
    public static extern int WlDisplayRoundtrip({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_read_events", ExactSpelling = true)]
    public static extern int WlDisplayReadEvents({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_prepare_read_queue", ExactSpelling = true)]
    public static extern int WlDisplayPrepareReadQueue({{_WlDisplayTypeName}}* display, {{_WlEventQueueTypeName}}* queue);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_prepare_read", ExactSpelling = true)]
    public static extern int WlDisplayPrepareRead({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_cancel_read", ExactSpelling = true)]
    public static extern void WlDisplayCancelRead({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_dispatch_queue", ExactSpelling = true)]
    public static extern int WlDisplayDispatchQueue({{_WlDisplayTypeName}}* display, {{_WlEventQueueTypeName}}* queue);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_dispatch_queue_pending", ExactSpelling = true)]
    public static extern int WlDisplayDispatchQueuePending({{_WlDisplayTypeName}}* display, {{_WlEventQueueTypeName}}* queue);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_dispatch", ExactSpelling = true)]
    public static extern int WlDisplayDispatch({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_dispatch_pending", ExactSpelling = true)]
    public static extern int WlDisplayDispatchPending({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_get_error", ExactSpelling = true)]
    public static extern int WlDisplayGetError({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_get_protocol_error", ExactSpelling = true)]
    public static extern uint WlDisplayGetProtocolError({{_WlDisplayTypeName}}* display,
                                                        {{_WlInterfaceTypeName}}** @interface,
                                                        uint* id);

    [DllImport(LibWaylandClient, EntryPoint = "wl_display_flush", ExactSpelling = true)]
    public static extern int WlDisplayFlush({{_WlDisplayTypeName}}* display);

    [DllImport(LibWaylandClient, EntryPoint = "wl_event_queue_destroy", ExactSpelling = true)]
    public static extern void WlEventQueueDestroy({{_WlEventQueueTypeName}}* queue);

    [DllImport(LibWaylandClient, EntryPoint = "wl_list_init", ExactSpelling = true)]
    public static extern void WlListInit({{_WlListTypeName}}* list);

    [DllImport(LibWaylandClient, EntryPoint = "wl_list_insert", ExactSpelling = true)]
    public static extern void WlListInsert({{_WlListTypeName}}* list, {{_WlListTypeName}}* elm);

    [DllImport(LibWaylandClient, EntryPoint = "wl_list_remove", ExactSpelling = true)]
    public static extern void WlListRemove({{_WlListTypeName}}* elm);

    [DllImport(LibWaylandClient, EntryPoint = "wl_list_length", ExactSpelling = true)]
    public static extern int WlListLength({{_WlListTypeName}}* list);

    [DllImport(LibWaylandClient, EntryPoint = "wl_list_empty", ExactSpelling = true)]
    public static extern int WlListEmpty({{_WlListTypeName}}* list);

    [DllImport(LibWaylandClient, EntryPoint = "wl_list_insert_list", ExactSpelling = true)]
    public static extern void WlListInsertList({{_WlListTypeName}}* list, {{_WlListTypeName}}* other);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_create", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyCreate({{_WlProxyTypeName}}* factory, {{_WlInterfaceTypeName}}* @interface);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_destroy", ExactSpelling = true)]
    public static extern void WlProxyDestroy({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_add_listener", ExactSpelling = true)]
    public static extern int WlProxyAddListener({{_WlProxyTypeName}}* proxy, void* implementation, void* data);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_get_listener", ExactSpelling = true)]
    public static extern void* WlProxyGetListener({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_add_dispatcher", ExactSpelling = true)]
    public static extern int WlProxyAddDispatcher({{_WlProxyTypeName}}* proxy,
                                                  {{_WlDispatcherFuncTTypeName}} dispatcher,
                                                  void* implementation,
                                                  void* data);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_flags", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyMarshalFlags({{_WlProxyTypeName}}* proxy,
                                                       uint opcode,
                                                       {{_WlInterfaceTypeName}}* @interface,
                                                       uint version,
                                                       uint flags,
                                                       uint param0,
                                                       char* param1,
                                                       uint param2);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_array_constructor", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyMarshalArrayConstructor({{_WlProxyTypeName}}* proxy,
                                                                 uint opcode,
                                                                 {{_WlArgumentTypeName}}* args,
                                                                 {{_WlInterfaceTypeName}}* @interface);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_array_constructor_versioned", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyMarshalArrayConstructorVersioned({{_WlProxyTypeName}}* proxy,
                                                                          uint opcode,
                                                                          {{_WlArgumentTypeName}}* args,
                                                                          {{_WlInterfaceTypeName}}* @interface,
                                                                          uint version);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_array_flags", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyMarshalArrayFlags({{_WlProxyTypeName}}* proxy,
                                                           uint opcode,
                                                           {{_WlInterfaceTypeName}}* @interface,
                                                           uint version,
                                                           uint flags,
                                                           {{_WlArgumentTypeName}}* args);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_constructor", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyMarshalConstructor({{_WlProxyTypeName}}* proxy,
                                                            uint opcode,
                                                            {{_WlInterfaceTypeName}}* @interface,
                                                            __arglist);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_constructor_versioned", ExactSpelling = true)]
    public static extern {{_WlProxyTypeName}}* WlProxyMarshalConstructorVersioned({{_WlProxyTypeName}}* proxy,
                                                                      uint opcode,
                                                                      {{_WlInterfaceTypeName}}* @interface,
                                                                      uint version,
                                                                      __arglist);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_marshal_array", ExactSpelling = true)]
    public static extern void WlProxyMarshalArray({{_WlProxyTypeName}}* proxy, uint opcode, {{_WlArgumentTypeName}}* args);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_set_user_data", ExactSpelling = true)]
    public static extern void WlProxySetUserData({{_WlProxyTypeName}}* proxy, void* user_data);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_get_user_data", ExactSpelling = true)]
    public static extern void* WlProxyGetUserData({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_get_version", ExactSpelling = true)]
    public static extern uint WlProxyGetVersion({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_get_id", ExactSpelling = true)]
    public static extern uint WlProxyGetId({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_set_tag", ExactSpelling = true)]
    public static extern void WlProxySetTag({{_WlProxyTypeName}}* proxy, char* tag);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_get_tag", ExactSpelling = true)]
    public static extern char* WlProxyGetTag({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_get_class", ExactSpelling = true)]
    public static extern char* WlProxyGetClass({{_WlProxyTypeName}}* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_set_queue", ExactSpelling = true)]
    public static extern void WlProxySetQueue({{_WlProxyTypeName}}* proxy, {{_WlEventQueueTypeName}}* queue);

    [DllImport(LibWaylandClient, EntryPoint = "wl_proxy_create_wrapper", ExactSpelling = true)]
    public static extern void* WlProxyCreateWrapper(void* proxy);

    [DllImport(LibWaylandClient, EntryPoint = "wl_fixed_to_double", ExactSpelling = true)]
    public static extern double WlFixedToDouble(_WlFixedT f);

    [DllImport(LibWaylandClient, EntryPoint = "wl_fixed_from_double", ExactSpelling = true)]
    public static extern _WlFixedT WlFixedFromDouble(double d);
}
""";
}
