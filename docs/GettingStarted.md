!!! info "Describes how to compile and run the Nexu Mods App."

The Nexus Mods App is programmed in C# with latest .NET Runtime. Setup should be easy.


## Prerequisites

- Download the Latest [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
- Download an IDE, recommended options below:
    - [Visual Studio](https://visualstudio.microsoft.com/downloads/)
    - [JetBrains Rider](https://www.jetbrains.com/rider/)

Our development team uses Rider, however Visual Studio is a great free offering on Windows.
Other code editors like [VSCode](https://code.visualstudio.com/) may work for you with [extensions](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit),
however may be a bit more complicated to use.

That's all. Open the project (`.sln`), build, and you're done.

## Tools for UI Development

Visual Studio and JetBrains Rider both have avalonia extensions that allow previewing ui designs (`.axaml` files).

- [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS)
- [Rider Extension](https://plugins.jetbrains.com/plugin/14839-avaloniarider)

You can install via web or from inside the IDEs extension managers themselves.

There are also some [Item Templates](https://github.com/AvaloniaUI/avalonia-dotnet-templates) for Avalonia that can be very useful to have,
helping you make things like new 'Windows' and 'Controls' easier.

## Code Guidelines

Have a look inside [Development Guidelines](./development-guidelines/DependencyInjection.md) 😉.
