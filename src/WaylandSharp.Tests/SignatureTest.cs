using WaylandSharpGen;

namespace WaylandSharp.Tests;

public class SignatureTest
{
    [Fact]
    public void CanParseSimple()
    {
        var signature = new Signature("i");

        signature.Raw.Should().Be("i");
        signature.SignatureOnly.Should().Be("i");
        signature.Version.Should().Be(null);
    }

    [Fact]
    public void CanParseWithVersion()
    {
        var signature = new Signature("1i");

        signature.Raw.Should().Be("1i");
        signature.SignatureOnly.Should().Be("i");
        signature.Version.Should().Be(1);
    }

    [Theory]
    [InlineData("i", new[] { 'i' }, new[] { false })]
    [InlineData("?o", new[] { 'o' }, new[] { true })]
    [InlineData("io", new[] { 'i', 'o' }, new[] { false, false })]
    [InlineData("i?o", new[] { 'i', 'o' }, new[] { false, true })]
    public void CanEnumerateProperly(string rawSignature, char[] types, bool[] nullable)
    {
        var signature = new Signature(rawSignature);

        var iteration = 0;
        foreach (var signatureEntry in signature)
        {
            signatureEntry.Type.Should().Be(types[iteration]);
            signatureEntry.Nullable.Should().Be(nullable[iteration]);
            iteration++;
        }
    }
}