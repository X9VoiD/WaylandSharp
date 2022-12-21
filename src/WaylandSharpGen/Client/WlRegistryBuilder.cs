using WaylandSharpGen.Xml;
using static WaylandSharpGen.Client.WlClientIdentifiers;
using static WaylandSharpGen.WlCommonIdentifiers;

namespace WaylandSharpGen.Client;

internal class WlRegistryBuilder
{
    private readonly List<SwitchExpressionArmSyntax> _switchExpressionArmSyntaxes = new();

    public ClassDeclarationSyntax Build()
    {
        var wlRegistryClass = (ClassDeclarationSyntax)CSharpSyntaxTree.ParseText(_wlRegistryBase).GetCompilationUnitRoot().Members[0];

        _switchExpressionArmSyntaxes.Add(
            SwitchExpressionArm(
                DiscardPattern(),
                ThrowExpression(
                    ObjectCreationExpression(
                        WlClientExceptionTypeSyntax)
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal("Unknown interface")))))))));

        var methodBodyStatements = new List<StatementSyntax>()
        {
            // var interfacePtr = {WlInterfaceTypeName}.FromInterfaceName(interfaceName).ToBlittable();
            LocalDeclarationStatement(
                VariableDeclaration(
                    IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                .WithVariables(
                    SingletonSeparatedList(VariableDeclarator(
                        Identifier("interfacePtr"))
                    .WithInitializer(
                        EqualsValueClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            WlInterfaceTypeSyntax,
                                            IdentifierName("FromInterfaceName")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    IdentifierName("interfaceName"))))),
                                    IdentifierName("ToBlittable")))))))),
            // version = version == 0 ? (uint)interfacePtr->Version : version;
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("version"),
                    ConditionalExpression(
                        BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            IdentifierName("version"),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0))),
                        CastExpression(
                            PredefinedType(
                                Token(SyntaxKind.UIntKeyword)),
                            MemberAccessExpression(
                                SyntaxKind.PointerMemberAccessExpression,
                                IdentifierName("interfacePtr"),
                                IdentifierName("Version"))),
                        IdentifierName("version")))),
            // var proxy = WlProxyMarshalFlags(_proxyObject, 0, interfacePtr, version, 0, name, interfacePtr->Name, version)
            LocalDeclarationStatement(
                VariableDeclaration(
                    IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                .WithVariables(
                    SingletonSeparatedList(VariableDeclarator(Identifier("proxy"))
                    .WithInitializer(EqualsValueClause(
                        InvocationExpression(
                            IdentifierName("WlProxyMarshalFlags"))
                        .WithArgumentList(ArgumentList(
                            SeparatedList(new[]{
                                Argument(
                                    IdentifierName("_proxyObject")),
                                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal(0))),
                                Argument(
                                    IdentifierName("interfacePtr")),
                                Argument(
                                    IdentifierName("version")),
                                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal(0))),
                                Argument(
                                    IdentifierName("name")),
                                Argument(
                                    MemberAccessExpression(
                                        SyntaxKind.PointerMemberAccessExpression,
                                        IdentifierName("interfacePtr"),
                                        IdentifierName("Name"))),
                                Argument(
                                    IdentifierName("version"))})))))))),
            ReturnStatement(
                SwitchExpression(
                    IdentifierName("interfaceName"))
                .WithArms(
                    SeparatedList(_switchExpressionArmSyntaxes)))
        };

        var wlRegistryClassBindMethod =
            MethodDeclaration(
                WlClientObjectTypeSyntax,
                Identifier("Bind"))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(
                    SeparatedList<ParameterSyntax>(
                        new SyntaxNodeOrToken[]{
                            Parameter(
                                Identifier("name"))
                            .WithType(
                                PredefinedType(
                                    Token(SyntaxKind.UIntKeyword))),
                            Token(SyntaxKind.CommaToken),
                            Parameter(
                                Identifier("interfaceName"))
                            .WithType(
                                PredefinedType(
                                    Token(SyntaxKind.StringKeyword))),
                            Token(SyntaxKind.CommaToken),
                            Parameter(
                                Identifier("version"))
                            .WithType(
                                PredefinedType(
                                    Token(SyntaxKind.UIntKeyword)))
                            .WithDefault(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(0))))})))
            .WithBody(Block(methodBodyStatements));

        return wlRegistryClass.AddMembers(wlRegistryClassBindMethod);
    }

    public void ProcessInterfaceDefinition(Interface definition)
    {
        _switchExpressionArmSyntaxes.Add(
            SwitchExpressionArm(
                ConstantPattern(
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal(definition.Name))),
                ObjectCreationExpression(
                    IdentifierName(definition.Name.SnakeToPascalCase()))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                IdentifierName("proxy")))))));
    }

    private const string _wlRegistryBase =
        $$"""
        public unsafe partial class {{WlRegistryTypeName}} : {{WlClientObjectTypeName}}
        {
            public T Bind<T>(uint name, string interfaceName, uint version = 0) where T : WlClientObject
            {
                return (T)Bind(name, interfaceName, version);
            }
        }
        """;
}