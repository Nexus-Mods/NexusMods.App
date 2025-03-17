# Transferring Existing Stardew Valley Mods to the App

In this guide, we will cover the basics of importing existing mods from other mod managers into the app. This information is primarily intended for Stardew Valley players but may have applications in other games. 

!!! danger "Clean Installation Recommended"
    We highly recommend that you start modding with the Nexus Mods app using a completely clean game installation. Mods added prior to use of the app may cause issues managing your content.

## Vortex
[Vortex](https://www.nexusmods.com/site/mods/1) is the predecessor to the app and is a popular choice among Stardew Valley modders. With full Nexus Mods integration, there are two options for importing your mods to the app. 

=== "Option 1 - Making a Quick Collection"
    !!! warning "Quick Collections require Vortex 1.14.0 or later"

    Using the Quick Collections feature, your existing mod setup can be exported and re-downloaded through the app. To make a collection from your active profile in Vortex, follow the instructions below:

    1. Open the "Collections" tab from the side menu in Vortex.
    2. Click on the "Workshop" tab at the top of the page. 
    3. Hover over "Create a collection" and select "From Profile". Give your collection a name and press "Create". 
    ![The option to create a Quick Collection in Vortex.](../images/VortexCreateCollection.webp){ style="max-height: 400px; display: block;" }
    4. Read through the list of what is and is not included to ensure the feature will do what you expect.
    ![The Quick Collection creation wizard showing the limitations of the system.](../images/VortexQuickCollection1.webp){ style="max-height: 300px; display: block;" }
    5. Give you collection a name (or use the default) and proceed to the upload step.
    ![The Quick Collection creation wizard showing the limitations of the system.](../images/VortexQuickCollection2.webp){ style="max-height: 300px; display: block;" }
    6. Click "Open in Browser" from the notification to see your draft collection on the website. 
    ![The Quick Collection creation wizard showing the limitations of the system.](../images/VortexQuickCollection3.webp)
    7. On the Nexus Mods website, you'll be presented with your collection page. You'll need to fill in a few options to make it postable.
    ![The option to publish the collection on Nexus Mods.](../images/NexusModsCollectionPublish.webp)
    The following steps are required to publish:
        - Summary: Put anything you like in here.
        - Category: Miscellaneous is recommended.
        - Image: Add an image next to the title. 
        - Description: Make an edit to the page description.
    8. Next, click the "Save & Publish Revision 1" button and in the pop-up make sure you select **Unlisted**. This will ensure your collection isn't added to the searchable options visible to all users.
    ![The option to publish the collection on Nexus Mods.](../images/NexusModsCollectionListing.webp){ style="max-height: 300px; display: block;" }
    8. Back in Vortex, navigate to the "Mods" tab, select all your mods with ++ctrl+"A"++ and click "Disable" on the bottom toolbar, then click "Deploy Mods" on the top toolbar to confirm the changes. This will remove the mod files from the game.
    9. Close Vortex.
    10. Open the app and press the "Add collection" button on the collection page to begin the download/install process.

    Once you are confident all your mods are installed in the app, you can re-open Vortex and remove all the mods from there.

=== "Option 2 - Importing downloads"
    This method does not use the collections feature, but uses the files already downloaded to your PC. 

    ??? note "Note - Mod Updates"
        Importing mods using this method means they do not have any Nexus Mods data associated with them and cannot be checked for updates.

    1. In Vortex, head to the "Mods" tab, select all mods with ++ctrl+"A"++, then disable them.
    2. Click "Deploy Mods" to ensure all Vortex-managed files are removed from the game folder. 
    3. Select on the "Downloads" option in the side menu, the click "Open Folder" on the toolbar. 
    4. Take note of the download folder location - you'll need this shortly. You may now close Vortex.
    5. Open the Nexus Mods app and [add Stardew Valley](../gettingstarted/AddGames.md).
    6. In the Library, select "Get Mods: From Drive".
    7. Navigate to the download folder location found in Step 2.
    8. Press ++ctrl+"A"++ to select all mod archives in the folder.
    9. Click "Open" and wait for the app to import all the mods (this can take several minutes).
    10. You can now start adding these mods to your loadout.
    !!! tip "Select multiple mods from the library using ++ctrl+"Click"++ or ++shift+"click"++"


## Stardrop
[Stardrop](https://www.nexusmods.com/stardewvalley/mods/10455) is a community-created cross-platform mod manager that is also a popular choice for Stardew Valley modders. 

??? note "Note - Mod Updates"
    Importing mods using this method means they do not have any Nexus Mods data associated with them and cannot be checked for updates.

By default, Stardrop uses a subfolder of "Mods" in the game directory. To preserve the mods you already have installed with Stardrop, we suggest follow these instructions to relocated Stardrop's mod files. This means you can easily swap back to Stardrop, if you like:

1. Create a folder called "Mods" somewhere on your computer outside of the Stardew Valley game folder. Inside that folder create a "Stardrop Installed Mods" folder.
    - On Windows :fontawesome-brands-windows:, we recommend using `%localappdata%\Stardrop\Mods` and `%localappdata%\Stardrop\Mods\Stardrop Installed Mods`.
    - On Linux :fontawesome-brands-linux:,we recommend using `$XDG_CONFIG_HOME\Stardrop\Mods` and `$XDG_CONFIG_HOME\Stardrop\Mods\Stardrop Installed Mods`
2. In Stardrop, select View -> Settings from the top bar.
![The Stardrop settings popup.](../images/StardropSettings.webp)
3. Open the folder listed as "Mod Folder Path" in the Stardrop Settings.
4. Move all the files and folders from the Mod Folder path to the new "Mods" folder you created in Step 1. 
5. Update the folder in the Stardrop settings menu to match the new location.
6. Repeat steps 3-5 for the "Stardrop Installed Mods" folder.
7. Uninstall SMAPI from your game folder (Uninstall instructions [:fontawesome-brands-windows:](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Uninstall) [:fontawesome-brands-linux:](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Linux#Uninstall)).
8. For each folder in the new "Stardrop Installed Mods" folder, create a zipped folder containing it. These can then be [imported into the app](./DownloadAMod.md#adding-a-mod-to-the-library-manually).

## Mod Organizer 2
[Mod Organizer 2](https://www.nexusmods.com/skyrimspecialedition/mods/6194) is an alternative community-created mod manager used primarily for Bethesda games but also offers support for Stardew Valley. To add your mods downloaded into MO2 to the app, follow the steps below:

??? note "Note - Mod Updates"
    Importing mods using this method means they do not have any Nexus Mods data associated with them and cannot be checked for updates.

1. In Mod Organizer 2, verify you are managing Stardew Valley, then click the folder icon above the mod list and select "Open Downloads Folder".
    ![The Mod Organizer 2 UI showing the button to open the download folder](../images/MO2DownloadFolder.webp)
2. Take note of the download folder location - you'll need this shortly.
3. Uninstall SMAPI from your game folder ([Uninstall instructions](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Uninstall)).
4. Open the Nexus Mods app and [add Stardew Valley](../gettingstarted/AddGames.md).
5. In the Library, select "Get Mods: From Drive".
6. Navigate to the download folder location found in Step 2.
7. Press ++ctrl+"A"++ to select all mod archives in the folder.
8. Click "Open" and wait for the app to import all the mods (this can take several minutes).
9. You can now start adding these mods to your loadout.

## Manually Installed Mods
!!! warning "Manual Mods will be deleted when uninstalling the app"
    If you had manually installed mods when managing the game, they are added to the loadout as a special mod. Deleting the loadout of uninstalling the app will remove all of these files and revert your game folder back to game files only. 

Some users choose to install their mods by manually copying and pasting the downloaded mod files into their game folder. If you manage the game with mods already installed, the app will figure out which files are from the base game and which are from mods then adds all non-game files to [External Changes](../features/ExternalChanges.md) in the loadout. 

It is recommended that you reinstall the mods as part of the loadout and then remove them from "External Changes". 

If you'd rather start fresh with the app, here's how to clean up your game folder.

1. Open your game installation folder.
2. Move the "Mods" folder so that it is no longer inside the Stardew Valley game folder (you can also delete it, but that is not recommended).
3. Uninstall SMAPI from your game folder (Uninstall instructions [:fontawesome-brands-windows:](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Uninstall) [:fontawesome-brands-linux:](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Linux#Uninstall)).
4. To start modding with the app, install your mods again by [importing the downloaded archives into the app](./DownloadAMod.md#adding-a-mod-to-the-library-manually).
