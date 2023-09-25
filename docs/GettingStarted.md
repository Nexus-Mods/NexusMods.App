## Getting Started in Development


The NexusMods.App is programed in C# and .NET 7. We have worked hard to make the startup process for the application as simple as possible.


### Prerequisites

To start with, you'll want to download the .NET 7 SDK (https://dotnet.microsoft.com/en-us/download/dotnet/7.0). You'll also need an IDE of some sort. Most of our developers prefer [JetBrains Rider](https://www.jetbrains.com/rider/), but [Visual Studio](https://visualstudio.microsoft.com/downloads/) is an alternative. While Visual Studio code may also work, these more advanced editors will provide superior feedback and tooling.

All other code and library dependencies are handled by NuGet and will be downloaded automatically when the application is built.
There are no other language or runtime dependencies.

### Useful tools:
Visual Studio and JetBrains Rider both have avalonia extensions that allow previewing ui designs (.axaml files).
- Visual Studio:  https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS
- Rider: https://plugins.jetbrains.com/plugin/14839-avaloniarider

Both extensions can be found directly from the IDE plugin/extension managers.
Using these extension is recommended for easier development.

There are also some project Templates for Avalonia that can be very useful to have, available here:
- https://github.com/AvaloniaUI/avalonia-dotnet-templates

### Code Overview

The NexusMods.App makes heavy use of Microsoft Dependency Injection. During application startup, modules are added to a `IServiceCollection` tagged by one or more type names. In may places in the code, you can see many examples of this registration

```csharp
services.AddSingleton<Foo>() // Register Foo as a provider of the Foo type
servcies.AddSingleton<IFoo, Foo>() // Register Foo as a provider of the IFoo interface
services.AddAllSingleton<IFoo, IFoo<long>, Foo>() // Register Foo as a provider of
```

Once the application starts, a `IServiceProvider` is created which can be used to get instances of registered types. This provider also auto-resolves all the requirements of classes and injects instances into classes as it constructs them.
Coding against interfaces then allows for parts of the application to be stubbed out during testing, swapped out to meet future requirements, or to have different configurations of the application for different environments.

Because `IServiceProvider` can also resolve a collection of implementations, it's possible to get all the possible implementations of a iterfaces. For example `services.GetRequiredService<IEnumerable<IGame>>()`
will return all the registered implementations of `IGame`. This is used in many places in the application to allow for the application to be extended to support new games without needing to change the code in many places.
This behavior is also leveraged to allow libraries to hook aspects of the application without having to register event callbacks. Instead libraries register themselves in the DI system and the framework calls them as needed.
