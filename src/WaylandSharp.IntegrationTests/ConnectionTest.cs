namespace WaylandSharp.IntegrationTests;

public class ConnectionTest
{
    [Fact]
    public void CanConnect()
    {
        using var display = WlDisplay.Connect();
    }

    [Fact]
    public void CanRoundtrip()
    {
        using var display = WlDisplay.Connect();
        display.Roundtrip().Should().NotBe(-1);
    }

    [Fact]
    public void CanGetRegistry()
    {
        using var display = WlDisplay.Connect();
        using var registry = display.GetRegistry();

        display.Roundtrip().Should().NotBe(-1);
    }

    [Fact]
    public void CanSync()
    {
        using var display = WlDisplay.Connect();
        using var registry = display.GetRegistry();
        using var callback = display.Sync();
        var monitor = callback.Monitor();

        display.Roundtrip().Should().NotBe(-1);
        monitor.Should().Raise(nameof(WlCallback.Done));
    }

    [Fact]
    public void CanGetGlobals()
    {
        using var display = WlDisplay.Connect();
        using var registry = display.GetRegistry();
        var monitor = registry.Monitor();

        display.Roundtrip().Should().NotBe(-1);
        monitor.Should().Raise(nameof(WlRegistry.Global));
    }

    [Fact]
    public void CanBindToGlobals()
    {
        using var display = WlDisplay.Connect();
        using var registry = display.GetRegistry();
        var outputs = new List<WlOutput>();

        registry.Global += (_, e) =>
        {
            if (e.Interface == WlInterface.WlOutput.Name)
            {
                var output = registry.Bind<WlOutput>(e.Name, e.Interface);
                outputs.Add(output);
            }
        };

        display.Roundtrip().Should().NotBe(-1);
        outputs.Count.Should().BeGreaterThan(0);

        foreach (var output in outputs)
        {
            output.Dispose();
        }
    }

    [Fact]
    public void CanBindToNonCoreGlobals()
    {
        using var display = WlDisplay.Connect();
        using var registry = display.GetRegistry();
        ZxdgOutputManagerV1? outputManager = null;

        registry.Global += (_, e) =>
        {
            if (e.Interface == WlInterface.ZxdgOutputManagerV1.Name)
                outputManager = registry.Bind<ZxdgOutputManagerV1>(e.Name, e.Interface);
        };

        display.Roundtrip().Should().NotBe(-1);
        outputManager.Should().NotBeNull();

        outputManager!.Dispose();
    }
}
