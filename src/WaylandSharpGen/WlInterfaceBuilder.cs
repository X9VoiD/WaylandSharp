using System.Collections.Immutable;
using WaylandSharpGen.Xml;
using static WaylandSharpGen.WlCommonIdentifiers;
namespace WaylandSharpGen;

internal class WlInterfaceBuilder
{
    private readonly HashSet<string> _declaredInterfaces = new();
    private readonly List<ExpressionStatementSyntax> _initializers = new();
    private readonly List<MemberDeclarationSyntax> _members = new();
    private readonly List<SwitchExpressionArmSyntax> _switchArms = new();

    public void GenerateCache(Protocol protocolDefinition)
    {
        var protocolInterfaces =
            new LinkedList<Interface>(protocolDefinition.Interfaces);

        while (protocolInterfaces.Any())
        {
            var interfaceDefinition = protocolInterfaces.First.Value;
            protocolInterfaces.RemoveFirst();
            ProcessInterface(protocolDefinition, protocolInterfaces, interfaceDefinition);
        }
    }

    public ClassDeclarationSyntax Build()
    {
        /*
         * Generate the constructor
         * static WlInterface()
         * {
         *     {ExpressionStatements}
         * }
         */

        var constructor =
            ConstructorDeclaration(WlInterfaceTypeName)
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.StaticKeyword)))
            .WithBody(Block(_initializers));

        _members.Add(constructor);

        /*
         * Generate discard switch arm
         * _ => throw new ArgumentException($"Unknown interface name: {name}");
         */

        var discardSwitchArm =
            SwitchExpressionArm(
                DiscardPattern(),
                ThrowExpression(
                    ObjectCreationExpression(
                        IdentifierName("ArgumentException"))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    InterpolatedStringExpression(
                                        Token(SyntaxKind.InterpolatedStringStartToken))
                                    .WithContents(
                                        List(new InterpolatedStringContentSyntax[]{
                                            InterpolatedStringText()
                                            .WithTextToken(
                                                Token(
                                                    TriviaList(),
                                                    SyntaxKind.InterpolatedStringTextToken,
                                                    "Unknown interface name: ",
                                                    "Unknown interface name: ",
                                                    TriviaList())),
                                            Interpolation(
                                                IdentifierName("name"))}))))))));

        _switchArms.Add(discardSwitchArm);

        /*
         * Generate WlInterface FromInterfaceName(string)
         * public static WlInterface FromInterfaceName(string name)
         * {
         *     return name switch
         *     {
         *         {SwitchArms}
         *     };
         * }
         */

        var fromInterfaceName =
            MethodDeclaration(
                WlInterfaceTypeSyntax,
                Identifier("FromInterfaceName"))
            .WithModifiers(
                TokenList(
                    new[]{
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)}))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(
                        Parameter(
                            Identifier("name"))
                        .WithType(
                            PredefinedType(
                                Token(SyntaxKind.StringKeyword))))))
            .WithBody(
                Block(
                    SingletonList<StatementSyntax>(
                        ReturnStatement(
                            SwitchExpression(
                                IdentifierName("name"))
                            .WithArms(SeparatedList(
                                _switchArms))))));

        _members.Add(fromInterfaceName);

        /*
         * Generate WlInterface
         * public partial class WlInterface
         * {
         *     {Members}
         * }
         */

        var @interface =
            ClassDeclaration(WlInterfaceTypeName)
            .WithModifiers(
                TokenList(new[]{
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.PartialKeyword)}))
            .WithMembers(List(_members))
            .NormalizeWhitespace(eol: Environment.NewLine);

        return @interface;
    }

    private void ProcessInterface(Protocol protocolDefinition,
                               LinkedList<Interface> interfaces,
                               Interface interfaceDefinition)
    {
        var cacheIdentifier = interfaceDefinition.Name.SnakeToPascalCase();

        // Generate the interface cache field declaration.
        // public static readonly WlInterface {cacheIdentifier};

        var fieldDeclaration =
            FieldDeclaration(
                VariableDeclaration(
                    WlInterfaceTypeSyntax)
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(cacheIdentifier)))))
                .WithModifiers(
                    TokenList(
                        new[]{
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.StaticKeyword),
                            Token(SyntaxKind.ReadOnlyKeyword)}));
        _members.Add(fieldDeclaration);

        // Generate the interface cache builder.
        // new WlInterface.Builder({interfaceDefinition.Name}, {interfaceDefinition.Version})

        ExpressionSyntax interfaceBuilderSyntax =
            ObjectCreationExpression(
                QualifiedName(
                    WlInterfaceTypeSyntax,
                    IdentifierName("Builder")))
            .WithArgumentList(ArgumentList(
                SeparatedList(new[] {
                    Argument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(interfaceDefinition.Name))),
                    Argument(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(interfaceDefinition.Version)))})));

        // Register events of the interface
        //      {Expression}.Event({@event.Name}, {@event.ToSignature.Raw}, new WlInterface? []{Types})

        foreach (var @event in interfaceDefinition.Events)
        {
            interfaceBuilderSyntax = RegisterMessage(protocolDefinition,
                                                     interfaces,
                                                     interfaceDefinition,
                                                     interfaceBuilderSyntax,
                                                     "Event",
                                                     @event);
        }

        // Register methods of the interface
        //     {Expression}.Method({@method.Name}, {@method.ToSignature.Raw}, new WlInterface? []{Types})

        foreach (var method in interfaceDefinition.Requests)
        {
            interfaceBuilderSyntax = RegisterMessage(protocolDefinition,
                                                     interfaces,
                                                     interfaceDefinition,
                                                     interfaceBuilderSyntax,
                                                     "Method",
                                                     method);
        }

        // Generate assignment statement
        // {cacheIdentifier} = {Expression}

        var assignmentStatement =
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(cacheIdentifier),
                    interfaceBuilderSyntax));

        _initializers.Add(assignmentStatement);

        // Generate switch arm

        var switchArm =
            SwitchExpressionArm(
                ConstantPattern(
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal(interfaceDefinition.Name))),
                IdentifierName(cacheIdentifier));

        _switchArms.Add(switchArm);
    }

    private ExpressionSyntax RegisterMessage(Protocol protocolDefinition,
                                             LinkedList<Interface> interfaces,
                                             Interface interfaceDefinition,
                                             ExpressionSyntax interfaceBuilderSyntax,
                                             string messageType,
                                             Method message)
    {
        EnsureDependencies(protocolDefinition, interfaces, interfaceDefinition, message);

        if (interfaceDefinition.Name == "wl_registry" && message.Name == "bind")
        {
            message = new Method(
                message.Type,
                message.Name,
                message.OpCode,
                message.Since,
                message.DocumentationSummary,
                message.Documentation,
                message.ExtraTypeAnnotation,
                new MethodArgument[] {
                    message.Arguments[0],
                    new MethodArgument(
                        name: "interface",
                        type: ArgumentType.String,
                        nullable: false,
                        @interface: null,
                        documentation: null,
                        @enum: null
                    ),
                    new MethodArgument(
                        name: "version",
                        type: ArgumentType.Uint,
                        nullable: false,
                        @interface: null,
                        documentation: null,
                        @enum: null
                    ),
                    message.Arguments[1]
                }.ToImmutableArray());
        }

        var types = new List<ExpressionSyntax>();
        foreach (var arg in message.Arguments)
        {
            if (arg.Interface is null)
            {
                types.Add(LiteralExpression(SyntaxKind.NullLiteralExpression));
            }
            else
            {
                var interfaceName = arg.Interface.SnakeToPascalCase();
                types.Add(IdentifierName(interfaceName));
            }
        }

        var expressionSyntax =
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    interfaceBuilderSyntax,
                    IdentifierName(messageType)))
            .WithArgumentList(
                ArgumentList(SeparatedList(new[] {
                    Argument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(message.Name))),
                    Argument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(message.ToSignature().Raw))),
                    Argument(
                        ArrayCreationExpression(
                            ArrayType(
                                NullableType(
                                    WlInterfaceTypeSyntax))
                            .WithRankSpecifiers(
                                SingletonList(
                                    ArrayRankSpecifier(
                                        SingletonSeparatedList<ExpressionSyntax>(
                                            OmittedArraySizeExpression())))))
                            .WithInitializer(
                                InitializerExpression(
                                    SyntaxKind.ArrayInitializerExpression,
                                    SeparatedList(types))))})));

        return expressionSyntax;
    }

    private void EnsureDependencies(Protocol protocolDefinition,
                                    LinkedList<Interface> interfaces,
                                    Interface interfaceDefinition,
                                    Method @event)
    {
        foreach (var argument in @event.Arguments)
        {
            // If the argument uses a not-yet-declared interface, find it in
            // the list of interfaces, pop it then generate recursively.
            if (argument.Interface is not null
                && !_declaredInterfaces.Contains(argument.Interface))
            {
                var dep = PopInterface(interfaces, argument.Interface);
                if (dep is null)
                {
                    throw new InvalidOperationException(
                        $"Interface '{argument.Interface}' not found. Needed by '{interfaceDefinition.Name}.{@event.Name}'");
                }
                ProcessInterface(protocolDefinition, interfaces, dep.Value);
            }
        }
        _declaredInterfaces.Add(interfaceDefinition.Name);
    }

    private static LinkedListNode<Interface>? PopInterface(
        LinkedList<Interface> interfaces,
        string interfaceName)
    {
        var node = interfaces.First;
        while (node is not null)
        {
            if (node.Value.Name == interfaceName)
            {
                interfaces.Remove(node);
                return node;
            }
            node = node.Next;
        }

        return null;
    }
}