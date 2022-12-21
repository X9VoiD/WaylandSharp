using System.Collections.Immutable;
using WaylandSharpGen.Xml;
using static WaylandSharpGen.Client.WlClientIdentifiers;
using static WaylandSharpGen.WlCommonIdentifiers;

namespace WaylandSharpGen.Client;

internal class WlClientBuilder
{
    private static readonly LocalFunctionStatementSyntax _dispatcherDelegate = GenerateDispatcherDelegate();

    private readonly WlInterfaceBuilder _interfaceBuilder = new();
    private readonly WlClientPInvokeBuilder _pInvokeBuilder = new();
    private readonly WlRegistryBuilder _registryBuilder = new();

    private readonly ImmutableArray<MemberDeclarationSyntax>.Builder _members =
        ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

    public WlClientBuilder()
    {
        var wlClientBase = CSharpSyntaxTree.ParseText(_WlClientBase);

        static IEnumerable<MemberDeclarationSyntax> commonClientTypes()
        {
            yield return CreateOpaqueStruct(_WlProxyTypeName);
            yield return CreateOpaqueStruct(_WlDisplayTypeName);
            yield return CreateOpaqueStruct(_WlEventQueueTypeName);
        }

        _members.AddRange(commonClientTypes());
        _members.AddRange(wlClientBase.GetCompilationUnitRoot().Members);
    }

    public ImmutableArray<MemberDeclarationSyntax> BuildMembers()
    {
        return _members.ToImmutable()
            .Add(_registryBuilder.Build())
            .Add(_pInvokeBuilder.Build())
            .Add(_interfaceBuilder.Build())
            .AddRange(WlBindingGenerator.GenerateCommonDefinitions());
    }

    public CompilationUnitSyntax BuildAsCompilationUnit()
    {
        var members = BuildMembers();
        var compilationUnit =
            CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                NamespaceDeclaration(
                    IdentifierName("WaylandSharp"))
                .WithNamespaceKeyword(
                    Token(
                        TriviaList(
                            Trivia(
                                NullableDirectiveTrivia(
                                    Token(SyntaxKind.EnableKeyword),
                                    true))),
                        SyntaxKind.NamespaceKeyword,
                        TriviaList()))
                .WithMembers(List(members))))
            .WithUsings(List(new[]{
                UsingDirective(
                    IdentifierName("System")),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("Collections")),
                        IdentifierName("Concurrent"))),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("Collections")),
                        IdentifierName("Generic"))),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("Collections")),
                        IdentifierName("Immutable"))),
                UsingDirective(
                    QualifiedName(
                        IdentifierName("System"),
                        IdentifierName("Diagnostics"))),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("Runtime")),
                        IdentifierName("CompilerServices"))),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("Runtime")),
                        IdentifierName("InteropServices"))),
                UsingDirective(
                    QualifiedName(
                        IdentifierName("System"),
                        IdentifierName("Threading"))),
                UsingDirective(
                    QualifiedName(
                        IdentifierName("WaylandSharp"),
                        IdentifierName("Client")))
                .WithStaticKeyword(
                    Token(SyntaxKind.StaticKeyword))}));
        return compilationUnit
            .NormalizeWhitespace(eol: Environment.NewLine);
    }

    public void ProcessProtocolDefinition(Protocol protocolDefinition)
    {
        _interfaceBuilder.GenerateCache(protocolDefinition);
        foreach (var interfaceDefinition in protocolDefinition.Interfaces)
        {
            ProcessInterfaceDefinition(interfaceDefinition);
        }
    }

    internal void ProcessInterfaceDefinition(
        Interface interfaceDefinition)
    {
        _registryBuilder.ProcessInterfaceDefinition(interfaceDefinition);

        var interfaceName = interfaceDefinition.Name.SnakeToPascalCase();
        var members = new List<MemberDeclarationSyntax>();

        /*
         * Generate constructor
         * internal {interfaceName}(_WlProxy* proxyObject) : base(proxyObject)
         * {
         * }
         */

        var protocolClassConstructor =
            ConstructorDeclaration(
                    Identifier(interfaceName.Escape()))
               .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
               .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                    Identifier("proxyObject"))
                               .WithType(
                                    _WlIntPtrTypeSyntax))))
               .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    CastExpression(_WlProxyPointerSyntax, IdentifierName("proxyObject")))))))
               .WithBody(
                    Block());

        members.Add(protocolClassConstructor);

        protocolClassConstructor =
            ConstructorDeclaration(
                    Identifier(interfaceName.Escape()))
               .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.InternalKeyword)))
               .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                    Identifier("proxyObject"))
                               .WithType(
                                    _WlProxyPointerSyntax))))
               .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName("proxyObject"))))))
               .WithBody(
                    Block());

        members.Add(protocolClassConstructor);

        // Process enums

        foreach (var enumDefinition in interfaceDefinition.Enums)
        {
            var enumName = interfaceName + enumDefinition.Name.SnakeToPascalCase();
            var enumMembers = new List<EnumMemberDeclarationSyntax>();

            foreach (var enumMember in enumDefinition.Members)
            {
                var enumMemberDeclaration =
                    EnumMemberDeclaration(
                        enumMember.Name.SnakeToPascalCase().Escape())
                    .WithEqualsValue(
                        EqualsValueClause(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(enumMember.Value))));

                enumMembers.Add(enumMemberDeclaration);
            }

            var enumDeclaration =
                EnumDeclaration(enumName)
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                    SimpleBaseType(PredefinedType(Token(SyntaxKind.UIntKeyword))))))
                .WithMembers(SeparatedList(enumMembers));

            // Add enum to namespace directly
            _members.Add(enumDeclaration);
        }

        // Process events

        if (interfaceDefinition.Events.Length > 0)
        {
            // Generate events

            var dispatcherSections = new List<SwitchSectionSyntax>();
            foreach (var eventDefinition in interfaceDefinition.Events)
            {
                // Generate event args class
                var eventName = eventDefinition.Name.SnakeToPascalCase();
                var eventArgsMembers = new List<MemberDeclarationSyntax>();
                var eventArgsConstructorParameters = new List<ParameterSyntax>();
                var eventArgsConstructorStatements = new List<StatementSyntax>();

                foreach (var eventArgs in eventDefinition.Arguments)
                {
                    var eventArgType = GetEventTypeMapping(interfaceName, eventArgs);
                    var eventArgMember =
                        PropertyDeclaration(
                            eventArgType,
                            Identifier(eventArgs.Name.SnakeToPascalCase()))
                        .WithModifiers(
                            TokenList(Token(SyntaxKind.PublicKeyword)))
                        .WithAccessorList(
                            AccessorList(
                                SingletonList(
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        Token(SyntaxKind.SemicolonToken)))));
                    eventArgsMembers.Add(eventArgMember);

                    eventArgsConstructorParameters.Add(
                        Parameter(
                            Identifier(eventArgs.Name.Escape()))
                        .WithType(eventArgType));

                    eventArgsConstructorStatements.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(eventArgs.Name.SnakeToPascalCase()),
                                IdentifierName(eventArgs.Name.Escape()))));
                }

                var eventArgsClassName = $"{eventName}EventArgs";
                var eventArgsClassConstructor =
                    ConstructorDeclaration(
                        Identifier(eventArgsClassName))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(
                        ParameterList(SeparatedList(eventArgsConstructorParameters)))
                    .WithBody(
                        Block(eventArgsConstructorStatements));

                eventArgsMembers.Add(eventArgsClassConstructor);

                var eventArgsClassDeclaration =
                    GenerateEventArgsClass(eventArgsClassName)
                    .WithMembers(List(eventArgsMembers));

                members.Add(eventArgsClassDeclaration);

                // Generate event handler
                var backingEventHandlerName = "_" + eventDefinition.Name;
                var publicEventHandlerName = eventName;

                // private event EventHandler<{eventArgsName}> {backingEventHandlerName};
                var backingEventHandlerDeclaration =
                    EventFieldDeclaration(
                        VariableDeclaration(
                            NullableType(
                                GenericName(
                                    Identifier("EventHandler"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(eventArgsClassName))))))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(backingEventHandlerName)))));

                members.Add(backingEventHandlerDeclaration);

                /*
                 * public event EventHandler<{eventArgsName}> {publicEventHandlerName}
                 * {
                 *     add
                 *     {
                 *         CheckIfDisposed();
                 *         {backingEventHandlerName} += value;
                 *         HookDispatcher();
                 *     }
                 *     remove => {backingEventHandlerName} -= value;
                 * }
                 */
                var publicEventHandlerDeclaration =
                    EventDeclaration(NullableType(
                        GenericName(
                            Identifier("EventHandler"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(eventArgsClassName))))),
                        Identifier(publicEventHandlerName))
                    .WithModifiers(
                        TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(
                        AccessorList(List(
                            new AccessorDeclarationSyntax[]{
                                AccessorDeclaration(
                                    SyntaxKind.AddAccessorDeclaration)
                                .WithBody(Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName("CheckIfDisposed"))),
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.AddAssignmentExpression,
                                            IdentifierName(backingEventHandlerName),
                                            IdentifierName("value"))),
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName("HookDispatcher"))))),
                                AccessorDeclaration(
                                    SyntaxKind.RemoveAccessorDeclaration)
                                .WithExpressionBody(
                                    ArrowExpressionClause(
                                        AssignmentExpression(
                                            SyntaxKind.SubtractAssignmentExpression,
                                            IdentifierName(backingEventHandlerName),
                                            IdentifierName("value"))))
                                .WithSemicolonToken(
                                    Token(SyntaxKind.SemicolonToken))})));

                members.Add(publicEventHandlerDeclaration);

                // Process dispatcher

                var sectionStatements = new List<StatementSyntax>();
                var eventArgCreationArguments = new List<ArgumentSyntax>();
                for (var i = 0; i < eventDefinition.Arguments.Length; ++i)
                {
                    // Generate event arg conversion
                    // var {eventArgVariableName} = <some magic to convert _WlArgument>;
                    var eventArg = eventDefinition.Arguments[i];
                    var convertedValueExpression = GetEventArgumentConversionExpression(interfaceName, i, eventArg);
                    var eventArgVariableName = $"{eventDefinition.Name}Arg{i}";
                    var convertedValueDeclaration =
                        LocalDeclarationStatement(VariableDeclaration(
                            IdentifierName(
                                Identifier(
                                    TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(eventArgVariableName))
                                .WithInitializer(
                                    EqualsValueClause(
                                        convertedValueExpression)))));

                    sectionStatements.Add(
                        convertedValueDeclaration);

                    // save the event arg variable name for event arg creation later
                    eventArgCreationArguments.Add(
                        Argument(IdentifierName(eventArgVariableName)));
                }

                // Generate event arg creation
                // new {eventArgsName}({eventArgCreationArguments})
                var eventArgCreationExpression =
                    ObjectCreationExpression(IdentifierName(eventArgsClassName))
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList(eventArgCreationArguments)));

                // Generate event dispatch
                // {backingEventHandlerName}(this, {eventArgCreationExpression});
                var eventInvokeStatement =
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName(backingEventHandlerName),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke")))
                            .WithArgumentList(ArgumentList(
                                SeparatedList(new[]{
                                    Argument(ThisExpression()),
                                    Argument(eventArgCreationExpression)})))));

                sectionStatements.Add(eventInvokeStatement);
                sectionStatements.Add(BreakStatement());

                // Generate dispatcher switch section
                // case {opCode}:
                //     {sectionStatements}

                var dispatcherSection =
                    SwitchSection()
                    .WithLabels(
                        SingletonList<SwitchLabelSyntax>(
                            CaseSwitchLabel(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(eventDefinition.OpCode)))))
                    .WithStatements(
                        List(sectionStatements));

                dispatcherSections.Add(dispatcherSection);
            }

            // Add default case
            // default:
            //    throw new {WlClientException}("Unknown event opcode");
            dispatcherSections.Add(
                SwitchSection()
                .WithLabels(
                    SingletonList<SwitchLabelSyntax>(
                        DefaultSwitchLabel()))
                .WithStatements(
                    SingletonList<StatementSyntax>(
                        ThrowStatement(
                            ObjectCreationExpression(
                                WlClientExceptionTypeSyntax)
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal("Unknown event opcode"))))))))));

            /*
             * Generate dispatcher delegate
             * int dispatcher(void* data, void* target, uint callbackOpcode, _WlMessage* messageSignature, _WlArgument* args)
             * {
             *     switch (callbackOpcode)
             *     {
             *         {dispatcherSections}
             *     }
             *    return 0;
             * }
             */

            var dispatcherDelegate = _dispatcherDelegate
                .WithBody(Block(
                    SwitchStatement(
                        IdentifierName("callbackOpcode"))
                    .WithSections(
                        List(dispatcherSections)),
                    ReturnStatement(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)))));

            // Generate dispatcher creation method

            var dispatchCreateMethod =
                MethodDeclaration(
                    _WlDispatcherFuncTTypeSyntax,
                    Identifier("CreateDispatcher"))
                .WithModifiers(
                    TokenList(new[]{
                        Token(SyntaxKind.InternalKeyword),
                        Token(SyntaxKind.SealedKeyword),
                        Token(SyntaxKind.OverrideKeyword)}))
                .WithBody(Block(
                        dispatcherDelegate,
                        ReturnStatement(
                            IdentifierName("dispatcher"))));

            members.Add(dispatchCreateMethod);
        }

        // Process requests

        for (var i = 0; i < interfaceDefinition.Requests.Length; ++i)
        {
            var requestDefinition = interfaceDefinition.Requests[i];

            // Do not generate wl_registry.bind(), we have our own specialized implementation
            if (interfaceDefinition.Name == "wl_registry" && requestDefinition.Name == "bind")
                continue;

            var requestName = requestDefinition.Name.SnakeToPascalCase();
            var statements = new List<StatementSyntax>();
            var requestParameters = new List<ParameterSyntax>();
            TypeSyntax returnType;
            ArgumentSyntax interfacePtrArgument;
            var newIdArg = requestDefinition.Arguments.FirstOrDefault(a => a.Type == ArgumentType.NewId);

            // CheckIfDisposed();
            statements.Add(ExpressionStatement(InvocationExpression(
                IdentifierName("CheckIfDisposed"))));

            if (newIdArg is not null)
            {
                var returnTypeName = newIdArg.Interface!.SnakeToPascalCase();
                returnType = IdentifierName(returnTypeName);
                interfacePtrArgument = Argument(IdentifierName("interfacePtr"));

                // var interfacePtr = WlInterface.{returnTypeName}.ToBlittable();
                var getInterfaceCache =
                    LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName(
                            Identifier(
                                TriviaList(),
                                SyntaxKind.VarKeyword,
                                "var",
                                "var",
                                TriviaList())))
                        .WithVariables(SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier("interfacePtr"))
                            .WithInitializer(
                                EqualsValueClause(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                WlInterfaceTypeSyntax,
                                                IdentifierName(returnTypeName)),
                                            IdentifierName("ToBlittable"))))))));

                statements.Add(getInterfaceCache);
            }
            else
            {
                returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));
                interfacePtrArgument = Argument(LiteralExpression(
                    SyntaxKind.NullLiteralExpression));
            }

            var requestArgArguments = new List<ArgumentSyntax>()
            {
                Argument(IdentifierName("_proxyObject")),
                Argument(LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(requestDefinition.OpCode))),
                interfacePtrArgument,
                Argument(InvocationExpression(
                    IdentifierName("WlProxyGetVersion"))
                .WithArgumentList(ArgumentList(
                    SingletonSeparatedList(Argument(
                        IdentifierName("_proxyObject")))))),
                Argument(LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(0)))
            };

            for (var ii = 0; ii < requestDefinition.Arguments.Length; ++ii)
            {
                var requestArg = requestDefinition.Arguments[ii];

                if (requestArg.Type != ArgumentType.NewId)
                {
                    var parameter =
                        Parameter(Identifier(requestArg.Name.Escape()))
                        .WithType(GetRequestTypeMapping(interfaceName, requestArg));

                    // Add request definition as parameter to this request method
                    requestParameters.Add(parameter);
                }

                // Convert argument to blittable version for wl_proxy_marshal_flags()
                // var arg{ii} = {conversionExpression};
                var convertedValueExpression = GetRequestArgumentConversionExpression(requestArg);
                var convertedValueStatement =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(
                                Identifier(
                                    TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier("arg" + ii))
                            .WithInitializer(
                                EqualsValueClause(
                                    convertedValueExpression)))));

                statements.Add(convertedValueStatement);

                // Add to arguments of wl_proxy_marshal_flags()
                requestArgArguments.Add(Argument(IdentifierName("arg" + ii)));
            }

            var marshalInvocationExpression =
                InvocationExpression(
                    IdentifierName("WlProxyMarshalFlags"))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList(requestArgArguments)));

            if (newIdArg is not null)
            {
                // var newId = WlProxyMarshalFlags({requestArgArguments});
                var newIdStatement =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(
                                Identifier(
                                    TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier("newId"))
                            .WithInitializer(
                                EqualsValueClause(
                                    marshalInvocationExpression)))));

                statements.Add(newIdStatement);

                // return new {returnType}(newId);
                statements.Add(ReturnStatement(
                    ObjectCreationExpression(
                        returnType)
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName("newId")))))));
            }
            else
            {
                statements.Add(ExpressionStatement(marshalInvocationExpression));
            }

            // Generate request method

            var requestMethod =
                MethodDeclaration(
                    returnType,
                    Identifier(requestName))
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SeparatedList(requestParameters)))
                .WithBody(Block(statements));

            members.Add(requestMethod);

            // Generate pinvoke marshal function
            _pInvokeBuilder.GenerateMarshal(requestDefinition);
        }

        /*
        * Generate protocol class
        * public unsafe partial class {interfaceName}
        * {
        *     {members}
        * }
        */

        var protocolClassDeclaration =
            ClassDeclaration(interfaceName)
            .WithModifiers(
                TokenList(new[]{
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.UnsafeKeyword),
                Token(SyntaxKind.PartialKeyword)}))
            .WithBaseList(
                BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(
                        SimpleBaseType(
                            IdentifierName(WlClientObjectTypeName)))))
            .WithMembers(
                List(members));

        _members.Add(protocolClassDeclaration);
    }

    private static ClassDeclarationSyntax GenerateEventArgsClass(string name)
    {
        return
            ClassDeclaration(name)
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(
                BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(
                        SimpleBaseType(
                            IdentifierName("EventArgs")))));
    }

    private static LocalFunctionStatementSyntax GenerateDispatcherDelegate()
    {
        return
            LocalFunctionStatement(
                PredefinedType(
                    Token(SyntaxKind.IntKeyword)),
                Identifier("dispatcher"))
            .WithParameterList(
                ParameterList(
                    SeparatedList(new[]{
                        Parameter(
                            Identifier("data"))
                        .WithType(
                            PointerType(
                                PredefinedType(
                                    Token(SyntaxKind.VoidKeyword)))),
                        Parameter(
                            Identifier("target"))
                        .WithType(
                            PointerType(
                                PredefinedType(
                                    Token(SyntaxKind.VoidKeyword)))),
                        Parameter(
                            Identifier("callbackOpcode"))
                        .WithType(
                            PredefinedType(
                                Token(SyntaxKind.UIntKeyword))),
                        Parameter(
                            Identifier("messageSignature"))
                        .WithType(
                            _WlMessagePointerSyntax),
                        Parameter(
                            Identifier("args"))
                        .WithType(
                            _WlArgumentPointerSyntax)})));
    }

    private static TypeSyntax GetEventTypeMapping(string interfaceName, MethodArgument messageArgumentDefinition)
    {
        if (messageArgumentDefinition.Enum is { } @enum)
            return IdentifierName(@enum.ParseEnum(interfaceName)!);

        return messageArgumentDefinition.Type switch
        {
            ArgumentType.Int => PredefinedType(Token(SyntaxKind.IntKeyword)),
            ArgumentType.Uint => PredefinedType(Token(SyntaxKind.UIntKeyword)),
            ArgumentType.Fixed => PredefinedType(Token(SyntaxKind.DoubleKeyword)),
            ArgumentType.String => PredefinedType(Token(SyntaxKind.StringKeyword)),
            ArgumentType.Object => messageArgumentDefinition.Interface switch
            {
                null => WlClientObjectTypeSyntax,
                _ => IdentifierName(messageArgumentDefinition.Interface.SnakeToPascalCase())
            },
            ArgumentType.NewId => messageArgumentDefinition.Interface switch
            {
                null => throw new InvalidOperationException("NewId argument must have an interface"),
                _ => IdentifierName(messageArgumentDefinition.Interface.SnakeToPascalCase())
            },
            ArgumentType.Array => IdentifierName(WlArrayTypeName),
            ArgumentType.FD => PredefinedType(Token(SyntaxKind.IntKeyword)),
            _ => throw new NotImplementedException()
        };
    }

    private static TypeSyntax GetRequestTypeMapping(string interfaceName, MethodArgument messageArgumentDefinition)
    {
        if (messageArgumentDefinition.Enum is not null)
            return IdentifierName(messageArgumentDefinition.Enum.ParseEnum(interfaceName)!);

        return messageArgumentDefinition.Type switch
        {
            ArgumentType.Int => PredefinedType(Token(SyntaxKind.IntKeyword)),
            ArgumentType.Uint => PredefinedType(Token(SyntaxKind.UIntKeyword)),
            ArgumentType.Fixed => PredefinedType(Token(SyntaxKind.DoubleKeyword)),
            ArgumentType.String => PredefinedType(Token(SyntaxKind.StringKeyword)),
            ArgumentType.Object => messageArgumentDefinition.Interface switch
            {
                null => WlClientObjectTypeSyntax,
                _ => IdentifierName(messageArgumentDefinition.Interface.SnakeToPascalCase())
            },
            ArgumentType.Array => WlArrayTypeSyntax,
            ArgumentType.FD => PredefinedType(Token(SyntaxKind.IntKeyword)),
            _ => throw new NotImplementedException()
        };
    }

    private static ExpressionSyntax GetEventArgumentConversionExpression(string interfaceName, int argIndex, MethodArgument argDefinition)
    {
        static MemberAccessExpressionSyntax accessArgElementAt(int index, string memberName)
        {
            return
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ElementAccessExpression(
                        IdentifierName("args"))
                    .WithArgumentList(
                        BracketedArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(index)))))),
                    IdentifierName(memberName));
        }

        static ExpressionSyntax convertWlFixed(int index)
        {
            return
                InvocationExpression(
                    IdentifierName("WlFixedToDouble"))
                .WithArgumentList(ArgumentList(
                    SingletonSeparatedList(
                        Argument(accessArgElementAt(index, "f")))));
        }

        static ExpressionSyntax convertCharPointer(int index)
        {
            return
                PostfixUnaryExpression(
                    SyntaxKind.SuppressNullableWarningExpression,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Marshal"),
                            IdentifierName("PtrToStringAnsi")))
                    .WithArgumentList(ArgumentList(
                        SingletonSeparatedList(Argument(
                            CastExpression(
                                IdentifierName("IntPtr"),
                                accessArgElementAt(index, "s")))))));
        }

        static ExpressionSyntax convertObject(int index, string? interfaceName)
        {
            if (interfaceName == null)
            {
                return
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            WlClientObjectTypeSyntax,
                            IdentifierName("GetObject")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    CastExpression(
                                        _WlProxyPointerSyntax,
                                        accessArgElementAt(index, "o"))))));
            }

            return
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        WlClientObjectTypeSyntax,
                        GenericName(
                            Identifier("GetObject"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(interfaceName.SnakeToPascalCase()))))))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                CastExpression(
                                    _WlProxyPointerSyntax,
                                    accessArgElementAt(index, "o"))))));
        }

        static ExpressionSyntax convertNewId(int index, string? interfaceName)
        {
            if (interfaceName == null)
                throw new InvalidOperationException("Cannot marshal new_id without interface");

            return
                ObjectCreationExpression(
                    IdentifierName(interfaceName.SnakeToPascalCase()))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                CastExpression(
                                    _WlProxyPointerSyntax,
                                    accessArgElementAt(index, "n"))))));
        }

        static ExpressionSyntax convertArray(int index)
        {
            return
                ObjectCreationExpression(
                    WlArrayTypeSyntax)
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                accessArgElementAt(index, "a")))));
        }

        static ExpressionSyntax convertEnum(string interfaceName, string enumName, ExpressionSyntax accessor)
        {
            return
                CastExpression(
                    IdentifierName(enumName.ParseEnum(interfaceName)!),
                    accessor);
        }

        var argType = argDefinition.Type;

        if (argDefinition.Enum is not null)
        {
            var accessor = argDefinition.Type == ArgumentType.Int
                ? accessArgElementAt(argIndex, "i")
                : accessArgElementAt(argIndex, "u");

            return convertEnum(interfaceName, argDefinition.Enum, accessor);
        }

        return argType switch
        {
            ArgumentType.Int => accessArgElementAt(argIndex, "i"),
            ArgumentType.Uint => accessArgElementAt(argIndex, "u"),
            ArgumentType.Fixed => convertWlFixed(argIndex),
            ArgumentType.String => convertCharPointer(argIndex),
            ArgumentType.Object => convertObject(argIndex, argDefinition.Interface),
            ArgumentType.NewId => convertNewId(argIndex, argDefinition.Interface),
            ArgumentType.Array => convertArray(argIndex),
            ArgumentType.FD => accessArgElementAt(argIndex, "h"),
            _ => throw new NotSupportedException("Cannot marshal unknown argument type")
        };
    }

    private static ExpressionSyntax GetRequestArgumentConversionExpression(MethodArgument argDefinition)
    {
        var identifier = IdentifierName(argDefinition.Name);

        static ExpressionSyntax convertCharPointer(IdentifierNameSyntax identifier)
        {
            return
                CastExpression(
                    PointerType(
                        PredefinedType(
                            Token(SyntaxKind.CharKeyword))),
                    PostfixUnaryExpression(
                        SyntaxKind.SuppressNullableWarningExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Marshal"),
                                IdentifierName("StringToHGlobalAnsi")))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        identifier))))));
        }

        static ExpressionSyntax convertWlFixed(IdentifierNameSyntax identifier)
        {
            return
                InvocationExpression(
                    IdentifierName("WlFixedFromDouble"))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                identifier))));
        }

        static ExpressionSyntax convertWlArray(IdentifierNameSyntax identifier)
        {
            return
                CastExpression(
                    _WlArrayPointerSyntax,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        identifier,
                        IdentifierName("RawPointer")));
        }

        static ExpressionSyntax convertWlObject(IdentifierNameSyntax identifier)
        {
            return
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    identifier,
                    IdentifierName("_proxyObject"));
        }

        static ExpressionSyntax convertNewId()
        {
            return
                CastExpression(
                    _WlProxyPointerSyntax,
                    LiteralExpression(
                        SyntaxKind.NullLiteralExpression));
        }

        static ExpressionSyntax convertEnum(IdentifierNameSyntax identifier, ArgumentType type)
        {
            return
                CastExpression(
                    PredefinedType(
                        type switch
                        {
                            ArgumentType.Int => Token(SyntaxKind.IntKeyword),
                            ArgumentType.Uint => Token(SyntaxKind.UIntKeyword),
                            _ => Token(SyntaxKind.UIntKeyword)
                        }),
                    identifier);
        }

        if (argDefinition.Enum is not null)
            return convertEnum(identifier, argDefinition.Type);

        return argDefinition.Type switch
        {
            ArgumentType.Int => identifier,
            ArgumentType.Uint => identifier,
            ArgumentType.Fixed => convertWlFixed(identifier),
            ArgumentType.String => convertCharPointer(identifier),
            ArgumentType.Array => convertWlArray(identifier),
            ArgumentType.Object => convertWlObject(identifier),
            ArgumentType.NewId => convertNewId(),
            ArgumentType.FD => identifier,
            _ => throw new NotSupportedException("Cannot marshal unknown argument type")
        };
    }

    private static StructDeclarationSyntax CreateOpaqueStruct(string structName)
    {
        return StructDeclaration(structName)
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword),
                    Token(SyntaxKind.RefKeyword)));
    }

    private const string _WlClientBase =
$$"""
public unsafe struct {{WlArrayTypeName}} : IEquatable<{{WlArrayTypeName}}>
{
    private readonly {{_WlArrayTypeName}}* _array;

    public int Size => _array->Size;
    public int Capacity => _array->Alloc;
    public IntPtr Data => (IntPtr)_array->Data;

    internal {{WlArrayTypeName}}({{_WlArrayTypeName}}* array)
    {
        _array = array;
    }

    public bool Equals({{WlArrayTypeName}} other)
    {
        return _array == other._array;
    }

    public override bool Equals(object? obj)
    {
        return obj is {{WlArrayTypeName}} array && Equals(array);
    }

    public override int GetHashCode()
    {
        return (int)_array;
    }

    public static bool operator ==({{WlArrayTypeName}} left, {{WlArrayTypeName}} right)
    {
        return left.Equals(right);
    }

    public static bool operator !=({{WlArrayTypeName}} left, {{WlArrayTypeName}} right)
    {
        return !(left == right);
    }
}

public abstract unsafe class {{WlClientObjectTypeName}} : IEquatable<{{WlClientObjectTypeName}}>, IDisposable
{
    internal readonly {{_WlProxyTypeName}}* _proxyObject;
    private GCHandle _dispatcherPin;
    private int _disposed;
    private readonly object _syncLock = new();
    private static readonly {{_WlDispatcherFuncTTypeName}} _eventSentinel = (_, _, _, _, _) => -1;
    private static readonly ConcurrentDictionary<ulong, {{WlClientObjectTypeName}}> _objects = new();

    public IntPtr RawPointer => (IntPtr)_proxyObject;

    internal {{WlClientObjectTypeName}}({{_WlProxyTypeName}}* proxyObject)
    {
        _proxyObject = proxyObject;
        if (!_objects.TryAdd((ulong)proxyObject, this))
            throw new {{WlClientExceptionTypeName}}("Attempted to track duplicate wl_proxy");
    }

    internal static {{WlClientObjectTypeName}} GetObject({{_WlProxyTypeName}}* proxyObject)
    {
        return _objects.TryGetValue((ulong)proxyObject, out var wlObj)
            ? wlObj
            : throw new {{WlClientExceptionTypeName}}("Attempted to retrieve untracked wl_proxy");
    }

    internal static T GetObject<T>({{_WlProxyTypeName}}* proxyObject) where T : {{WlClientObjectTypeName}}
    {
        return (T)GetObject(proxyObject);
    }

    public uint GetVersion()
    {
        CheckIfDisposed();
        return WlProxyGetVersion(_proxyObject);
    }

    public uint GetId()
    {
        CheckIfDisposed();
        return WlProxyGetId(_proxyObject);
    }

    protected void HookDispatcher()
    {
        lock (_syncLock)
        {
            if (_dispatcherPin.IsAllocated)
                return;

            var dispatcher = CreateDispatcher();
            if (dispatcher == _eventSentinel)
                throw new {{WlClientExceptionTypeName}}("Dispatcher not implemented");
            HookDispatcher(dispatcher);
        }
    }

    internal virtual {{_WlDispatcherFuncTTypeName}} CreateDispatcher()
    {
        return _eventSentinel;
    }

    internal void HookDispatcher({{_WlDispatcherFuncTTypeName}} dispatcher)
    {
        var handle = GCHandle.Alloc(dispatcher);
        _dispatcherPin = GCHandle.Alloc(handle);
        if (WlProxyAddDispatcher(_proxyObject, dispatcher, null, null) != 0)
            throw new {{WlClientExceptionTypeName}}("Failed to add dispatcher to proxy");
    }

    public bool Equals({{WlClientObjectTypeName}}? other)
    {
        return other != null && _proxyObject == other._proxyObject;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as {{WlClientObjectTypeName}});
    }

    public override int GetHashCode()
    {
        return (int)_proxyObject;
    }

    protected void CheckIfDisposed()
    {
        if (_disposed == 1)
            ThrowDisposed();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _objects.Remove((ulong)_proxyObject, out _);
            Destroy(_proxyObject);
            if (_dispatcherPin.IsAllocated)
                _dispatcherPin.Free();
        }

        GC.SuppressFinalize(this);
    }

    internal virtual void Destroy({{_WlProxyTypeName}}* proxy)
    {
        WlProxyDestroy(proxy);
    }

    [DebuggerHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowDisposed()
    {
        throw new ObjectDisposedException(GetType().Name);
    }
}

public unsafe partial class {{WlDisplayTypeName}} : {{WlClientObjectTypeName}}
{
    /// <summary>
    /// Connects to a Wayland display, optionally using the given name.
    /// </summary>
    /// <param name="name">The name of the wayland path to connect to.</param>
    /// <returns>An instance of <see cref="{{WlDisplayTypeName}}"/>.</returns>
    /// <exception cref="{{WlClientExceptionTypeName}}">Thrown when we can't connect to the wayland display server.</exception>
    public static {{WlDisplayTypeName}} Connect(string? name = null)
    {
        var displayName = name != null
            ? (char*)Marshal.StringToHGlobalAnsi(name)
            : null;

        var display = WlDisplayConnect(displayName);
        if (display == null)
        {
            throw new {{WlClientExceptionTypeName}}(
                "Failed to connect to wayland display server");
        }

        return new {{WlDisplayTypeName}}(({{_WlProxyTypeName}}*)display);
    }

    public int Roundtrip()
    {
        CheckIfDisposed();
        return WlDisplayRoundtrip(({{_WlDisplayTypeName}}*)_proxyObject);
    }

    public int Dispatch()
    {
        CheckIfDisposed();
        return WlDisplayDispatch(({{_WlDisplayTypeName}}*)_proxyObject);
    }
}

public class {{WlClientExceptionTypeName}} : Exception
{
    public {{WlClientExceptionTypeName}}(string message) : base(message)
    {
    }
}
""";
}