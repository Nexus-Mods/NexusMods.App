# NexusMods.App - Roadmap

The goal of this roadmap is not to set hard deadlines or to provide a comprehensive upfront design. Instead this document intended as a callout of the general areas of
development on the project. In addition this document should be used to call out aspects of the project that can be developed in parallel. 

# Backend Systems
Unless otherwise noted, all backend systems are implemented using purely CLI interfaces, and should
have at least 95% test coverage. All non-OS specific tests should be run on the three major operating systems (Windows, OSX, Linux).

## Backend Milestone 1
* Creation of the Modlist Data Model
  * Launch Management of a game
  * Add, Remove, Re-order Sort mods
  * Basic "last write wins", no LOOT (yet)
  * Saving lists, Undoing changes
* Archive management
  * Tracking what mods used what archives
  * Tracking the source of the archives
* ModType installers
  * More-or-less clone what is in Vortex today
  * Priority system for file handlers
  * Make this system *extremely* easy to implement (wildcard matching, perhaps leverage YAML/JSON?)

* Basic support for 1 BGS game (Skyrim SE) and 1 other game (Cyberpunk)
  * Should include installing the extender mods (SKSE, CET, RedScript) and simple asset-replacement mods
  * Should include launching the game and setting plugin and preference files
* Development can fork from here into
  * UI development

## Backend Milestone 2
* Support for Download management
  * Nexus API integration
  * Generic HTTP downloader
  * Build a resumable, hashing, parallelizable downloader
* Implement a generic rate-limiting system
  * Users should be able to set limits on download speeds
  * Users should be able to set limits on CPU core usage during extraction
  * Users should be able to set limits on disk access (for slower HDDs)
* Implement LOOT sorting and rules based ordering (required by Collections)

## Backend Milestone 3
* Add support for installing Collections
* Add CLI interface for testing a collection's ability to install
  * Pick a few lists to add to an automated testing list


# UI Systems

## UI Milestone 1 - The Framework
* Starts anytime during development (even before the backend system)
* Setup the initial project structure with Avalonia
* Develop the look and feel of the widgets
* Decide on a structure for the main window systems and implement it
* Setup the logging plumbing so that users can view the log, and click on events to send focus to other parts of the app

## UI Milestone 2 - Modlist Management
* Connect in the Data Model from the Backend
* Add Modlist creation and visualization controls
* Add Archive visualization controls and windows
* Allow for "interaction free" archive/mod installs
  * User clicks `add modd` they are given a file picker
  * Archive is indexed, and added as a mod via the Data Model

## UI Milestone 3 - Nexus Download Support
* Log into the Nexus via the UI
* Add `nxm://` handlers
* Improve archive visualization to show links to the Nexus
* Add a job queue display window (for download status)

## UI Milestone 4 - Collection Installation Support
* Show previews for collections
* Allow users to start the download/install process for collections
