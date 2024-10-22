using System.Reactive;
using JetBrains.Annotations;
using Markdown.Avalonia.Plugins;
using Markdown.Avalonia.Utils;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

public class MarkdownRendererDesignViewModel : AViewModel<IMarkdownRendererViewModel>, IMarkdownRendererViewModel
{
    public string Contents { get; set; }
    public Uri? MarkdownUri { get; set; }

    public IMdAvPlugin ImageResolverPlugin => null!;
    public IPathResolver PathResolver => null!;

    public ReactiveCommand<string, Unit> OpenLinkCommand { get; } = ReactiveCommand.Create<string>(_ => { });

    [UsedImplicitly]
    public MarkdownRendererDesignViewModel() : this(DefaultContents) { }

    public MarkdownRendererDesignViewModel(string contents)
    {
        Contents = contents;
    }

    // From https://jaspervdj.be/lorem-markdownum/
    [LanguageInjection("markdown")]
    private const string DefaultContents =
"""
# v0.6.2 - 2024-10-16

**Important: To update the app, you must completely uninstall the old version, including all mods.** [Why do I have to uninstall everything to update the app?](https://nexus-mods.github.io/NexusMods.App/users/faq/#why-do-i-have-to-uninstall-everything-to-update-the-app)

***

1. The app will display a diagnostic message if a P
2. The app will display a diagnostic message if a P
3. The app will display a diagnostic message if a P

---

> Hello block quote
>
>> This release adds initial support for Baldur's Gate 3, detects GOG games installed through the Heroic Launcher on Linux and further develops the Collections feature.
>>
>> This release adds initial support for Baldur's Gate 3, detects GOG games installed through the Heroic Launcher on Linux and further develops the Collections feature.

```csharp
this is a code block
extra
this is a code block
```

This release adds initial support for Baldur's Gate 3, detects GOG games installed through the Heroic Launcher on Linux and further develops the Collections feature.

*If you are installing the app outside of the default location (Local App Data), the uninstallers for all previous versions may not work correctly. [How to manually reset your installation](https://nexus-mods.github.io/NexusMods.App/users/Uninstall/#manual-uninstall-fallback).*

## Baldur's Gate 3 
This version of the app adds Baldur's Gate 3 as the third supported game ([#2122](https://github.com/Nexus-Mods/NexusMods.App/pull/2122)). Currently, all common mod types will be installed correctly. Installations on Steam and GOG can be detected on Windows or Linux. 

The app will display a diagnostic message if a PAK file lists a dependency in the meta.lsx file that is not currently installed. We plan to improve the available data shown to the users in future releases. 

The current implementation does not include load order support. You will need to go to "Mod Manager" from the main menu and enable your mods to have them take effect in the game. Load order support is planned for a future release. 

*Note: When launching the GOG version of the game, the app will currently default to the Vulkan version. A choice between Vulkan and DX11 will be added in a future release. If you require the DX11 version, please launch from GOG Galaxy (Windows), Heroic Launcher (Linux) or via bg3_dx11.exe.*


## Heroic Launcher
Linux users can now manage GOG games installed or imported using the [Heroic Launcher](https://heroicgameslauncher.com/) ([#2103](https://github.com/Nexus-Mods/NexusMods.App/pull/2103)). Due to technical limitations, we've disabled REDmod deployment for Cyberpunk 2077 using Heroic, but it's possible to have the launcher run this process when starting the game. [Automated deployment for REDmods in Heroic](https://nexus-mods.github.io/NexusMods.App/users/games/Cyberpunk2077/#automated-deployment-for-redmods).

## Collections
**Important: Collections support is still an experimental feature and may not work as expected. Please check the Known Issues section if you choose to install a collection.**

In this build, we've made further updates to the process of downloading and installing collections. The feature does not have parity with Vortex yet so cannot be used to install a collection fully. 

![The new collections tile available from the "Collections WIP" tab.](./docs/changelog-assets/8ed9a020e1532940c4d57a2753eae55a.webp)

The changes we've made include:
* Updated the card design for collections (Above).
* Added support for FOMOD presets and binary patching during installation.

## Updating Mods
We're starting work on showing when a mod has an update in the app. The backend to enable this feature is mostly complete and we will be adding the UI elements to support it in an upcoming release. 

## Known Issues
* Most collection installations will not complete successfully. This is due to several features that have not yet been implemented. 
* The game version is not checked when adding a collection meaning you can install outdated mods without being warned. 
* Trying to install a collection with an unsupported type of mod (e.g. non-Nexus Mods files) will fail with no error message. This is not supported in the current build.
* Trying to install a collection as a non-Premium user will fail with no error message. This is not supported in the current build. 
* Once a collection is added to the app, it cannot be removed from the left menu.
* Collections allow users to modify the included mods but do not allow you to reset them to the original state. 
* The first row of the My Mods or Library tables will sometimes be misaligned with the headers. Scrolling or adjusting any column width will correct this. 
* The "Switch View" option does not persist in the Library/Installed Mods view.


## Bugfixes
* The app will now uninstall correctly when installed outside of the default directory on Windows. 
* The correct WINE prefix will now be used for games on Linux. 
* When the numerical badges in the left menu show 3 or more digits, the width of the badge will expand correctly.
* Fixed an issue where batch actions would not work correctly when adding/removing/deleting mods from the Library or Loadout pages. 
* The app will no longer re-download the user's avatar image every time a request is made to the Nexus Mods API. 

## External Contributors
* [@MistaOmega](https://github.com/MistaOmega): [#2118](https://github.com/Nexus-Mods/NexusMods.App/pull/2118), [#2119](https://github.com/Nexus-Mods/NexusMods.App/pull/2119), [#2128](https://github.com/Nexus-Mods/NexusMods.App/pull/2128), [#2130](https://github.com/Nexus-Mods/NexusMods.App/pull/2130)
* [@Patriot99](https://github.com/Patriot99): [#2145](https://github.com/Nexus-Mods/NexusMods.App/pull/2145)
* [@Michael-Kowata](https://github.com/Michael-Kowata): [#2163](https://github.com/Nexus-Mods/NexusMods.App/pull/2163)

[View the release on GitHub](https://github.com/Nexus-Mods/NexusMods.App/releases/tag/v0.6.2)

# v0.6.1 - 2024-09-24

**Important: To update the app, you must completely uninstall the old version, including all mods. [Why do I have to uninstall everything to update the app?](https://nexus-mods.github.io/NexusMods.App/users/faq/#why-do-i-have-to-uninstall-everything-to-update-the-app)**

This release adds a very basic implementation of downloading Collections, updates the UI to the new tree view and includes some enhancements when interacting with Windows applications via Linux. 

## New UI for My Mods and Library
The My Mods and Library pages have been completely reworked to use the new tree view. Mods are now grouped by the mod page on Nexus Mods, meaning if download several files from the same page they will be grouped together. A "Switch View" option has been added to the toolbar to toggle these groupings on or off. We are continuing to work towards to designs shown in the [previous changelog](./docs/changelog-assets/1b28e2fad5b5a6431a72c286d1bcd3fd.webp).

![An image showing mods in the Library nested by mod page (left) or ungrouped (right)](./docs/changelog-assets/823627a8ccb068dc1559d62cd3326ebe.webp)

## EXPERIMENTAL - Collections
**Important: The feature is unfinished and not considered stable. It will not accurately install complex collections and is currently only functional for Premium users.**

We've included a very early implementation of the Collections feature in this release. It's incomplete and will not install collections as the user has set them up in Vortex. Currently, only mods from Nexus Mods can be installed - anything from external websites or bundled with the collection will not install as expected. 

![A collection for Cyberpunk 2077 installed into a loadout.](./docs/changelog-assets/8a591449d6a8cddc5d2bad7d1fd5c849.webp)

Collections will appear as a separate list of mods in the left menu. Users can view all mods in the loadout from the new "Installed Mods" option at the top of the left menu. 

To start out, this will only be available to Premium users, but we are working on the free user journey separately which requires considerably more UI elements to be created. This will be available in a future release.


## Cyberpunk 2077 Enhancements
As a further enhancement to support for Cyberpunk 2077, we will now detect if the REDmod DLC is missing and prompt the user to install it if required. 

![The diagnostic message for REDmod shown in the Health Check.](./docs/changelog-assets/531dc13e8116620f8ded4a8a98b281da.webp)

We've also fixed the issue which prevented REDmod from deploying automatically on Linux. This work also sets up a framework for running Windows apps and tools on a Linux system using [Protontricks](https://github.com/Matoking/protontricks) ([#1989](https://github.com/Nexus-Mods/NexusMods.App/pull/1989)).

## Known Issues
* When batch selecting mods in My Mods and using the remove button the app will occasionally fail to remove mods that are not currently visible in the UI due to scrolling. 
* Trying to install a collection with an unsupported type of mod (e.g. Bundled or External) will fail with no error message. This is not supported in the current build.
* Trying to install a collection as a non-Premium user will fail with no error message. This is not supported in the current build. 
* Once a collection is added to the app, it cannot be removed from the left menu.
* Collections allow users to modify the included mods but do not allow you to reset them to a the original state. 
* The first row of the My Mods or Library tables will sometimes be misaligned with the headers. Scrolling or adjusting any column width will correct this. 
* The "Switch View" option does not persist.

## Other Features
* The name of the active loadout will now appear in the top bar ([#1953](https://github.com/Nexus-Mods/NexusMods.App/pull/1953)).
* The app now has a minimum window size of `360x360` to prevent it being resized to unusable dimensions ([#1947](https://github.com/Nexus-Mods/NexusMods.App/pull/1947)).

## Bugfixes
* Stardew Valley: Fixed enabled mods showing up as disabled in the diagnostics ([#1923](https://github.com/Nexus-Mods/NexusMods.App/pull/1953)).
* Linux: Fixed the game not launching when running through Steam ([#1917](https://github.com/Nexus-Mods/NexusMods.App/pull/1917)).

## Technical Changes 
* Added a system for storing and displaying images in the app. 

## External Contributors
* [@Patriot99](https://github.com/Patriot99): [#1896](https://github.com/Nexus-Mods/NexusMods.App/pull/1896)
* [@LoulouNoLegend](https://github.com/LoulouNoLegend): [#1997](https://github.com/Nexus-Mods/NexusMods.App/pull/1997), [#1998](https://github.com/Nexus-Mods/NexusMods.App/pull/1998), [#1999](https://github.com/Nexus-Mods/NexusMods.App/pull/1999)

[View the release on GitHub](https://github.com/Nexus-Mods/NexusMods.App/releases/tag/v0.6.1)
""";
}
