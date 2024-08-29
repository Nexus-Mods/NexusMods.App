#Frequently Asked Questions (FAQ)

This page includes answers to the commonly asked questions about the app. 

## Why doesn't the app support my game?
The Nexus Mods app is still in development and will not support specific games because these features have not been included yet. Our primary focus is to establish core functionality and foundational elements. 

Adding support for a wide range of games it not a practical decision at this stage of development, as it can introduce additional complexity and obscure underlying issues that need to be resolved first. The goal is to ensure the mod manager works seamlessly for a select few games before expanding compatibility in subsequent beta and final releases.

Our plan is to go game-by-game and work with the modding community at each step to ensure we're building something great. To see which games are supported, coming next and planned for the future please see the [roadmap](https://trello.com/b/gPzMuIr3/nexus-mods-app-roadmap).

## Why do I have to uninstall everything to update the app?
The Nexus Mods app is still in the very early stages of development. This means that some of the core backend functionality of the app may still change significantly between releases. When this changes all existing data becomes incompatible with the new versions and users who wish to update will need to start over. 

In future releases we plan to offer an automated migration (where possible) to allow for seamless updating.

We have provided a guide on [how to uninstall the app](../Uninstall.md) on both Windows and Linux. 

## How do I find my log files?
To find your log files, please see [this guide](./LogFiles.md). 

## Why can't I log into the app or download from Nexus Mods?

These issues most often occur when your PC is not correctly handling Nexus Mods `nxm://` links. These links are used for communication between your web browser and the app. To reset the connection, follow the instructions below.

### :fontawesome-brands-windows: Windows
On a Windows PC, you can re-register the app to handle `nxm://` links by simply logging out and back in again. If this isn't working it may be related to your browser configuration, please see [Browsers](#browsers) for more. 

### :fontawesome-brands-linux: Linux
On a Linux PC, you can check the default app for `nxm://` downloads and reset it following the instructions below:

- Check which app is currently set as the default by running `xdg-mime query default x-scheme-handler/nxm`.
- If this doesn't return `nexusmods-app-nxm.desktop` run this command:`xdg-settings set default-url-scheme-handler nxm nexusmods-app-nxm.desktop`.
- Restart any open web browser windows to ensure the change is applied.
- In your browser, when selecting what application should be used to open the nxm link, select **NexusMods.App NXM Handler**.

### Browsers
Depending on your choice of browser, there may be additional considerations when troubleshooting this issue. 

??? info ":fontawesome-brands-firefox: Setting the default protcol in Firefox"
    Mozilla browsers (such as Firefox) include their own protocol handler settings, which override those set by your operating system. 

    To access these settings click the menu button to the right of the address bar and select "Settings". Under the "General" category locate the section entitled "Files and Applications". 

    ![The settings page in Mozilla Firefox 127.0.1 showing the Files and Applications section](../images/FirefoxProtocols.webp)

    Here you will see the `nxm` protocol and the default app which will be used to open it. Using the drop-down you can select the default application for Firefox to use. Setting it to whichever option is set as "Default" will use the application specified by your operating system. 
