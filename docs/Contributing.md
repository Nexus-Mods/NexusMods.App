## Code Contributions

### Prerequisites

- The latest version of the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- An IDE:
    - [JetBrains Rider](https://www.jetbrains.com/rider/) and the [Avalonia Rider Extension](https://plugins.jetbrains.com/plugin/14839-avaloniarider),
    - [Visual Studio](https://visualstudio.microsoft.com/downloads/) and the [Avalonia Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS).

There are also some [Item Templates](https://github.com/AvaloniaUI/avalonia-dotnet-templates) for Avalonia that can be very useful to have,
helping you make things like new 'Windows' and 'Controls' easier.

### Writing code

Make sure to follow our [Development Guidelines](./development-guidelines/UICodingGuidelines.md).

## Translations

Translations are currently handled via the IDE. See [this issue](https://github.com/Nexus-Mods/NexusMods.App/issues/598) for more details.

## For Package Maintainers

If you want to create a package for your distribution, here are some helpful tips to get started:

- If possible, use `nexusmods-app` for the package name.
- We ship a build of `7zz` and use that executable unless you set `NEXUSMODS_APP_USE_SYSTEM_EXTRACTOR=1` when publishing. See [this issue](https://github.com/Nexus-Mods/NexusMods.App/issues/1306#issuecomment-2095755699) for details.
- Set `INSTALLATION_METHOD_PACKAGE_MANAGER` when publishing. We have an integrated updater that will notify users about new versions. If you set this flag, we'll tell the user to update with their package manager.
- Let us know if you have questions or if you published a new package by joining our [Discord](https://discord.gg/ReWTxb93jS).
