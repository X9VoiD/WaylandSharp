namespace WaylandSharp.IntegrationTests;

public class ClientTest
{
    [Fact]
    public void WlFixedFromDouble_WlFixedToDouble_ShouldWork()
    {
        var f = Client.WlFixedFromDouble(16.0);
        var d = Client.WlFixedToDouble(f);

        Assert.Equal(16.0, d);
    }
}