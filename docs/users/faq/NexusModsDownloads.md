# Why can't I log in or download from Nexus Mods?

These issues most often occur when your PC is not correctly handling Nexus Mods `nxm://` links. These links are used for communication between your web browser and the app. To reset the connection, follow the instructions below.

## :fontawesome-brands-windows: Windows
On a Windows PC, you can re-register the app to handle `nxm://` links by simply closing and reopening the app. If this isn't working it may be related to your browser configuration, please see [Browsers](#browsers) for more. 

## :fontawesome-brands-linux: Linux
On a Linux PC, you can check the default app for `nxm://` downloads and reset it following the instructions below:

- Check which app is currently set as the default by running `xdg-mime query default x-scheme-handler/nxm`.
- If this doesn't return `nexusmods-app-nxm.desktop` run this command:`xdg-settings set default-url-scheme-handler nxm nexusmods-app-nxm.desktop`.
- Restart any open web browser windows to ensure the change is applied.
- In your browser, when selecting what application should be used to open the nxm link, select **NexusMods.App NXM Handler**.

### Setting up Gear Lever on Linux
For Linux users who have the AppImage version of the app and are unable to resolve the registration issue using the commands above, we recommend [Gear Lever](https://github.com/mijorus/gearlever).

1. Download and install Gear Level, preferable via a package manager such as [Flathub](https://flathub.org/en/apps/it.mijorus.gearlever). 
2. Launch Gear Lever, then drag and drop the AppImage file for the app into the window.
3. If required, click "Unblock" in the top-right of the screen to verify the AppImage. 
4. Now the app will appear in your system menus like any other application downloaded from Flathub.

??? tip "Verify Gear Lever is set up correctly"
    To make sure the app is now able to handle downloads from the Nexus Mods website, you can run the following tests:

    1. In your browser, type `nxm://premium` into the address bar and press enter.
    2. In your terminal, type `xdg-open "nxm://premium"` and press enter.

    In both cases, the Nexus Mods app should open or come into focus. 


## Browsers
Depending on your choice of browser, there may be additional considerations when troubleshooting this issue. 

??? info ":fontawesome-brands-firefox: Setting the default protocol in Firefox"
    Mozilla browsers (such as Firefox) include their own protocol handler settings, which override those set by your operating system. 

    To access these settings click the menu button to the right of the address bar and select "Settings". Under the "General" category locate the section entitled "Files and Applications". 

    ![The settings page in Mozilla Firefox 127.0.1 showing the Files and Applications section](../images/FirefoxProtocols.webp)

    Here you will see the `nxm` protocol and the default app which will be used to open it. Using the drop-down you can select the default application for Firefox to use. Setting it to whichever option is set as "Default" will use the application specified by your operating system. 
