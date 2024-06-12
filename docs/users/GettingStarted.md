!!! warning "Everything file system and path related should be done using `IFileSystem`."

## Installation

### Windows

**Requirements**: Windows 10 or newer and a x64 CPU, we don't support Windows on ARM.

Download and run the [Windows Installer] from the [latest release] on GitHub.

### Linux

The App requires the following programs to be available at runtime:

- `xdg-open` to open URLs in your browser.
- `update-desktop-database` to update the MIME cache after creating a desktop entry file for the NXM link handler.

#### AppImage

You can download the AppImage file `NexusMods.App.x86_64.AppImage` from the [latest release] on GitHub. Make sure you have the following dependencies installed before running the AppImage:

- [FUSE 2](https://github.com/AppImage/AppImageKit/wiki/FUSE) is required to run any AppImage.
- `glibc` 2.2.5 or newer
- `glibc++` 3.4 or newer

#### System Package Manager

If you're want to create a package for a platform, see [Contributing](./Contributing.md#for-package-maintainers) for more details.

[![Packaging status](https://repology.org/badge/vertical-allrepos/nexusmods-app.svg)](https://repology.org/project/nexusmods-app/versions)

### macOS

We currently don't publish releases for macOS.

---

[Windows Installer]: https://github.com/Nexus-Mods/NexusMods.App/releases/latest/download/NexusMods.App.x64.exe
[latest release]: https://github.com/Nexus-Mods/NexusMods.App/releases/latest
