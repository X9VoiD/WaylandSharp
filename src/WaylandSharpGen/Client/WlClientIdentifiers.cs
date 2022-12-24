namespace WaylandSharpGen.Client;

internal static class WlClientIdentifiers
{
    public const string LibWlClientTypeName = "LibWlClient";
    public static readonly IdentifierNameSyntax LibWlClientTypeSyntax = IdentifierName(LibWlClientTypeName);

    public const string WlClientObjectTypeName = "WlClientObject";
    public static readonly IdentifierNameSyntax WlClientObjectTypeSyntax = IdentifierName(WlClientObjectTypeName);

    public const string _WlProxyTypeName = "_WlProxy";
    public static readonly IdentifierNameSyntax _WlProxyTypeSyntax = IdentifierName(_WlProxyTypeName);
    public static readonly PointerTypeSyntax _WlProxyPointerSyntax = PointerType(_WlProxyTypeSyntax);

    public const string _WlIntPtrTypeName = "System.IntPtr";
    public static readonly IdentifierNameSyntax _WlIntPtrTypeSyntax = IdentifierName(_WlIntPtrTypeName);

    public const string WlDisplayTypeName = "WlDisplay";
    public static readonly IdentifierNameSyntax WlDisplayTypeSyntax = IdentifierName(WlDisplayTypeName);

    public const string _WlDisplayTypeName = "_WlDisplay";
    public static readonly IdentifierNameSyntax _WlDisplayTypeSyntax = IdentifierName(_WlDisplayTypeName);
    public static readonly PointerTypeSyntax _WlDisplayPointerSyntax = PointerType(_WlDisplayTypeSyntax);

    public const string WlRegistryTypeName = "WlRegistry";
    public static readonly IdentifierNameSyntax WlRegistryTypeSyntax = IdentifierName(WlRegistryTypeName);

    public const string _WlEventQueueTypeName = "_WlEventQueue";
    public static readonly IdentifierNameSyntax _WlEventQueueTypeSyntax = IdentifierName(_WlEventQueueTypeName);
    public static readonly PointerTypeSyntax _WlEventQueuePointerSyntax = PointerType(_WlEventQueueTypeSyntax);

    public const string WlClientExceptionTypeName = "WlClientException";
    public static readonly IdentifierNameSyntax WlClientExceptionTypeSyntax = IdentifierName(WlClientExceptionTypeName);
}