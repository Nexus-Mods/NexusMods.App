# System Requirements
!!! info "This page does not include the requirements to run any supported games. Please refer to the game store page for details."

This page lists the recommended system requirements to install and run the app on all supported platforms.

## Windows 

!!! example "Testing required"
    We are currently testing the app on a variety of different systems and will provide more detailed system requirements in future.

|| Recommended |
| --- | --- |
| OS | Windows 10/11 with updates (64-bit OS required) |
<!-- | Processor | A multi-core CPU is recommended |
| Memory | More than 4 GB RAM |
| Storage | 5GB available space* |

<sub>\* Storage space required does not factor in the number of games managed, the size of the managed games and the mods installed.</sub> -->

## SteamOS + Linux 
!!! example "Testing required"
    We are currently testing the app on a variety of different systems and will provide more detailed system requirements in future.

|| Recommended |
| --- | --- |
| OS | A currently supported Linux distribution |
<!-- | Processor | A multi-core CPU is recommended |
| Memory | More than 4 GB RAM |
| Storage | 5GB available space* |

<sub>\* Storage space required does not factor in the number of games managed, the size of the managed games and the mods installed.</sub> -->

### Linux Dependencies

The App requires the following programs to be available on the system `PATH` at runtime:

- `xdg-settings` (part of [xdg-utils](https://www.freedesktop.org/wiki/Software/xdg-utils/))
- `update-desktop-database` (part of [desktop-file-utils](https://www.freedesktop.org/wiki/Software/desktop-file-utils/)) to update the MIME cache after creating a desktop entry file for the NXM link handler.

In addition, the following libraries are required:

- [FUSE 2](https://github.com/AppImage/AppImageKit/wiki/FUSE) if running the AppImage
- `glibc` 2.2.5 or newer
- `glibc++` 3.4 or newer
- `fontconfig`

The app also requires the XDG Desktop Portal and one backend to be installed and available. You might need to restart your PC after installing. If you use a standard desktop environment like GNOME or KDE, then you should already have the portal and one backend installed. See the [portal docs](https://flatpak.github.io/xdg-desktop-portal/#using-portals) for more details.

### Linux Packages

The status of packages for various Linux builds can be seen below:

[![Packaging status](https://repology.org/badge/vertical-allrepos/nexusmods-app.svg)](https://repology.org/project/nexusmods-app/versions)

If you want to create a package for your platform, see [Contributing](../developers/Contributing.md#for-package-maintainers) for more details.

## macOS

!!! failure  "macOS is not currently supported."
