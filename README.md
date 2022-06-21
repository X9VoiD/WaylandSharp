# WaylandSharp

An incremental source generator to automatically create bindings to Wayland
using given protocol xml files.
> `wayland.xml` should always be included to the list.

## Getting started

Install WaylandSharp nuget package.
```sh
dotnet add package WaylandSharp
```

Grab `wayland.xml` from [freedesktop.org](https://gitlab.freedesktop.org/wayland/wayland/-/blob/main/protocol/wayland.xml). Drop the file into your project.

Add this to your `csproj`
```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="WaylandProtocol" />
  <AdditionalFiles Include="wayland.xml" WaylandProtocol="client" />
</ItemGroup>
```
> No support for generating server-side bindings yet.

Benefit! :bread:

## Quick Guide

Connection to a wayland display server can be established by calling:
```cs
WlDisplay.Connect(string);
```

Global objects can be retrieved by creating a registry object and listening for
`Global` event
```cs
using wlDisplay = WlDisplay.Connect();
using wlRegistry = wlDisplay.GetRegistry();

wlRegistry.Global += (_, e) =>
{
    Console.WriteLine($"{e.Name}:{e.Interface}:{e.Version}");
};

wlDisplay.Roundtrip();
```

Either a `WlDisplay.Roundtrip()` or `WlDisplay.Dispatch()` is required to
generate event invocations. In this case, `Global` event will occur upon calling
`Roundtrip()`.

As described in Wayland's [official docs](https://wayland.freedesktop.org/docs/html/apb.html#Client-classwl__display),
`WlDisplay.Roundtrip()` dispatches all currently pending events. If no events
are pending, the function returns 0, otherwise it returns the number of pending
events that were processed. This internally calls `Sync` and waits for the
server's callback before returning.

On the other hand, `Dispatch` will block until there are events to process,
as such, it will never return 0. It is useful for setting up an event loop, like
in this example below.

```cs
while (someWlDisplayInstance.Dispatch() != -1)
{
    // intentionally empty
}
```

Binding to global objects are done by using the data received from
`GlobalEventArgs`, specifically `Name` (a unique `uint` given by the server
for this instance of global object), `Interface` (the contract used), and
`Version` and passing it as the arguments of `WlRegistry.Bind()`.

As an example, assuming that the user wants to bind to a `wl_output`

```cs
wlRegistry.Global += (_, e) =>
{
    if (e.Interface == WlInterface.WlOutput.Name)
    {
        // Passing a version is optional, it'll use the version specified in
        // the protocol xml by default.
        using var wlOutput = wlRegistry.Bind<WlOutput>(e.Name, e.Interface);

        // do something about wlOutput here.
    }
};
```

> A helper function will be introduced in the future to help shorten this
specific pattern.