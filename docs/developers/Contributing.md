﻿## Code Contributions

### Prerequisites

- The latest version of the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- An IDE:
    - [JetBrains Rider](https://www.jetbrains.com/rider/) and the [Avalonia Rider Extension](https://plugins.jetbrains.com/plugin/14839-avaloniarider),
    - [Visual Studio](https://visualstudio.microsoft.com/downloads/) and the [Avalonia Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS).

There are also some [Item Templates](https://github.com/AvaloniaUI/avalonia-dotnet-templates) for Avalonia that can be very useful to have,
helping you make things like new 'Windows' and 'Controls' easier.

### Running the project

- Clone the repository (optionally make a fork first)
- Ensure submodules have been cloned as well, you may use `git submodule update --init --recursive` to do so
- Build and run the `NexusMods.App` project from your IDE to start the app, or use
  `dotnet run --project src/NexusMods.App/NexusMods.App.csproj `

### Writing code

Make sure to follow our [Development Guidelines](./development-guidelines/UICodingGuidelines.md).

## Translations

Translations are currently handled via the IDE. See [this issue](https://github.com/Nexus-Mods/NexusMods.App/issues/598) for more details.

## For Package Maintainers

If you want to create a package for your distribution, here are some helpful tips to get started. If you have questions or want to let us know about your new package, consider joining our [Discord](https://discord.gg/ReWTxb93jS).

If possible, use `nexusmods-app` for the package name and `com.nexusmods.app` as the rDNS ID for Flatpaks and similar. See [Linux Dependencies](../users/SystemRequirements.md#linux-dependencies) for a list of dependencies.

There are various build variables you should consider using. You can set compile constants using `-p:DefineConstants="NAME"` with `dotnet build` or `dotnet publish`. For a full list of available application-specific compile constants, see [`Directory.Build.props`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/Directory.Build.props):

- `NEXUSMODS_APP_USE_SYSTEM_EXTRACTOR`: We ship a build of `7zz` that you can disable. Setting this constant will force the code to use `7zz` available in `PATH`. See [#1306](https://github.com/Nexus-Mods/NexusMods.App/issues/1306#issuecomment-2095755699) for details. Do note that some versions of `7zz` don't support RAR files, due to licensing issues. The build that the App ships with supports RAR files. Many mods still come in RAR archives, for a better user experience, we expect `7zz` to support RAR files. We won't accept issues around extraction failures for builds that don't support RAR archives.
- `INSTALLATION_METHOD_PACKAGE_MANAGER`: This constant will prevent the App from generating a `.desktop` file at runtime, and will change the update notification that notifies the user about new versions. If this constant is set, the App will tell the user to update using their package manager.

We publish the App using [PupNet](https://github.com/kuiperzone/PupNet-Deploy). Releases are built using GitHub Actions, see [`build-linux-pupnet.yaml`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/.github/workflows/build-linux-pupnet.yaml) for details. PupNet will use `dotnet publish` before packaging the result in some specialized format.

If you don't wish to use PupNet, you should still prefer `dotnet publish` over `dotnet build`. The arguments we use with `dotnet publish` can be found in [`app.pupnet.conf`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App/app.pupnet.conf). Also see the [`dotnet publish` documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish#options) for a list of options.

We provide a [Desktop Entry](https://specifications.freedesktop.org/desktop-entry-spec/desktop-entry-spec-latest.html) file called [`com.nexusmods.app.desktop`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App/com.nexusmods.app.desktop). When building the package, you should use this file and replace `${INSTALL_EXEC}` with an absolute path to the `NexusMods.App` binary.

The App will [generate](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.CrossPlatform/ProtocolRegistration/ProtocolRegistrationLinux.cs) this `.desktop` file and replace the placeholder with the absolute path to the executable if `INSTALLATION_METHOD_PACKAGE_MANAGER` is not set.

Besides the Desktop Entry file, we also provide the following files that you should make use of, if possible:

- [AppStream](https://www.freedesktop.org/software/appstream/docs/) [`com.nexusmods.app.metainfo.xml`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App/com.nexusmods.app.metainfo.xml)
- [`icon.svg`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App/icon.svg) and [`icon.ico`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App/icon.ico)
