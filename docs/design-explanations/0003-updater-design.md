# Application Updater

## Problem
The application needs a way to update itself, preferably without a lot of
user interaction

The issue is that on Windows and perhaps other platforms an executable cannot
be overwritten while it is running. This means that the application cannot
simply download a new version and restart itself. Instead we need to have
a multi-step process.

## Design Goals

* Remain as simple as possible, we don't want to pull in a lot of dependencies
* The application is rather small, so we don't need to worry about any sort of delta
  patching
* It would be nice to not need a separate launcher application as then we have an A->B->A issue
  where we need an updater to update the updater
* It would be nice to be able to download an update while the app is running, and then let the user
  know that we will update the code during next time the app launches
* We should never stop the users from using the app just to run an update
* We should recover from failed updates
* We can't update when the CLI is running, so we need to not attempt to run updates when multiple
  processes are running.
* Make it as OS agnostic as possible, so we don't need to write a different updater for each OS



## Implementation

The solution to this problem is to download updates into a `_update` folder, inside the main
application folder. When the application starts it looks for this folder, and if there is a pending
update, the app relaunches itself with special flag, but runs the executable in the `_update` folder.
This command then reads the current folder and copies its contents into parent folder. This means
that all the logic is contained inside the application itself, and yet still allows for updates.


## Code Flow

### On CLI start
* Look for a `__update__` folder, and see if `UPDATE_READY` exists as a file in that folder
* If it does, log a message saying there's a pending update, the contents of the `UPDATE_READY` file contains the new version

### On normal app start
* If any other process is running with the same process name as the current process, exit the update routine
* If the `__update__` folder exists, but does not contain the `UPDATE_READY` file, then delete the folder, and continue a normal launch
* If the `__update__` folder exists, and contains a `UPDATE_READY` file, launch the app with the `copy-app-to-folder` command,
  passing in the current app folder, and the current app process ID. Then exit the app.

### During normal app operation
* Read the list of versions available from github
* If the current version is not the latest version, extract the latest version to the `__update__` folder
* Create a `UPDATE_READY` file in the `__update__` folder, and write the new version number to it
* If the current version is the latest, cache the latest version for 6 hours (so future app restarts don't ping github)
* Sleep for 6 hours

### If the app is run with `copy-app-to-folder`
* Wait for the parent process to exit
* Copy everything in the current folder to the parent folder
* Delete the `UPDATE_READY` file
* Launch the process in the parent, and exit
