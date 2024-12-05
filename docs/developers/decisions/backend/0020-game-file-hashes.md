# Hashing of Game Files

## Context and Problem Statement

At several parts of the app, code can be simplified by having an authoritative set of hashes for games files. Not only does
this provide a mapping between common hash formats (xxHash3, Sha1, Md5, etc. ), but also allows for detection of files that
are part of various game installations. 

Games can update at any time, and the app needs to be able to get up-to-date hashes of files as soon as possible. 

## The Solution

We will create a pre-calculated set of hashes for game files. These hashes will be stored in GitHub for easy 
access and for downloading on-the-fly by the app. Therefore, we want to keep these hashes in a central location
that will be as easily accessible as possible.

Files will be indexed and placed into a folder structure in the repository. The folder structure will be as follows:

```
scripts
    rename_folders.py
json/
    ids.json
    game_domain/
         gamedomain_store_version_os.json
```
        

The game domain is an easily used id for contributors, but is not used in the app very many places. Therefore the ids.json file is used 
by `rename_folders.py` to rename the folders from `game_domain` to `game_id`. The `game_id` is a unique Nexus Mods id used to identify
the game. 

The `gamedomain_store_version_os.json` file is an array of game hash objects. Each object contains the following fields:

- `Domain`: The domain of the game
- `Store`: The store the game file is from (e.g. Steam, GOG, etc.)
- `Version`: The version of the game
- `OS`: The operating system the game is for
- `Path`: The path to the file, relative to the game's root directory
- `Size`: The size of the file in bytes
- `XxHash3`: The xxHash3 hash of the file
- `MinimalHash`: The minimal hash of the file (more below)
- `Sha1`: The Sha1 hash of the file
- `Md5`: The Md5 hash of the file

A GitHub action in the repo will clone these files, and run the `rename_folders.py` script to rename the folders.
it will then zip up the `json` folder and upload it as an artifact to the `game-hashes` repository.

The Nexus Mods app then need only check for a new release of these hashes, and download the artifact to get the latest hashes.

### Usage of this data
Due to the rather verbose nature of this data, the app will not keep this data in memory at all times. Instead,
parts of the system can request the hashes for a given game, and will be given all the hashes that match that game id.
It is up to the code requesting the data to index it and keep it optimized in memory if needed. 

#### Example usage
When indexing a game for the first time, we can use the `MinimalHash` to quickly check if a file matches a known hash. If so, 
we can assume that the XxHash3, Sha1, and Md5 hashes are also correct. The minimal hash is very quick to calculate, so this 
will be a significant performance boost on many systems.

When a game updates, the hashes of the new files can be calculated and compared to the game hashes. This can be used to
determine if the game has updated or the user has simply changed some of the game files.

If we get support for integrating with GoG or other game stores, we can match a XxHash3 to map to a MD5 hash and use that
to reset the game to a previous version. 

### Minimal Hash
The minimal hash format assumes that two files will have the same contents if they have the same size, exist on the same path
and generally match the same content. The algorithm for this hash is as follows:

- If the file is less than or equal to 128KB in size, simply hash the file via xxHash3
- Create a buffer of the first 64KB, the last 64KB and the middle 64KB (in that order)
- Add to the buffer the size of the file
- Compute the xxHash3 of the buffer

There is some overlap in the middle if the file is below a certain size, this is expected. In general, 
this means that only 192KB of the file is read, instead of the entire file. In games such as Cyberpunk this may
stop the app from reading the entire 100GB of the game files.
