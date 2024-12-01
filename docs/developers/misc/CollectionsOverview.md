## Collections Overview

### Introduction
Collections are, as the name suggests, mostly a collection (really a list) of mods. At their core they consist of a `.zip`
file that contains a `collection.json` file and various other files bundled with it. Most of the mods are stored as a Nexus
mod ID and fileId, and are therefore sourced from the normal download APIs. 

### Collections Metadata
Most of the collections metadata is stored and read from the Nexus Mods v2 GraphQL Api. The three main types of ids to be
aware of are:

- `slug` - a unique identifier for the collection, a 7 character alphanumeric string
- `revisionNumber` - a number that increments every time the collection is updated. It's unique only to the collection
- `revisionId` - a unique id for the revision of the collection, globally unique. Most likely this is an id in the Nexus Mods database

Based on a slug, GraphQL can be used to query the collection metadata, including the revision number and revision id. Based on
a revision id, and a slug, the download links for the collection can be obtained. Those links are then used to download the
collection `.zip` file. Inside that file is a `collection.json` file that contains the metadata for the collection.

Thus, the process for downloading a collection, given a slug and revision number are as follows:

1. Query the Nexus Mods v2 GraphQL API for the collection metadata. Each revision of the response data will include a `download_links`
field.
2. Use the `download_links` field to get a list of download links for the collection (one for each mirror and the CDN, just like files)
3. Download the collection `.zip` file from one of the download links
4. Analyze and add the collection file to the Library, and tag it with the collection metadata (mainly the slug and revision number)
5. Recast the collection file from the library as a `LibraryArchive` model, and find a child with the name `collection.json`
6. Parse the `collection.json` file


### Collection JSON

Here is an example `collection.json` file for reference

```json
{
  "info": {
    "author": "Anonymous",
    "authorUrl": "",
    "name": "Halgari's Helper",
    "description": "",
    "installInstructions": "",
    "domainName": "cyberpunk2077",
    "gameVersions": [
      "3.0.76.64179"
    ]
  },
  "mods": [
    {
      "name": "Appearance Menu Mod",
      "version": "2.7",
      "optional": false,
      "domainName": "cyberpunk2077",
      "source": {
        "type": "nexus",
        "modId": 790,
        "fileId": 66386,
        "md5": "0a6e3e603ef3bca799436f69510c79b7",
        "fileSize": 140159937,
        "logicalFilename": "Appearance Menu Mod",
        "updatePolicy": "prefer",
        "tag": "JqF6xzzWA"
      },
      "hashes": [
        {
          "path": "archive\\pc\\mod\\AMM_Dino_TattooFix.archive",
          "md5": "add39f916aa4f469b51881fe6b50a9c6"
        },
        {
          "path": "archive\\pc\\mod\\AMM_RitaWheeler_CombatEnabler.archive",
          "md5": "e1a03cf9eeb34288cb2d013f61381f63"
        }
      ],
      "author": "MaximiliumM and CtrlAltDaz",
      "details": {
        "category": "Appearance",
        "type": ""
      },
      "phase": 0
    },
    {
      "name": "Cyber Engine Tweaks - CET 1.32.2",
      "version": "1.32.2",
      "optional": false,
      "domainName": "cyberpunk2077",
      "source": {
        "type": "nexus",
        "modId": 107,
        "fileId": 73822,
        "md5": "4b1dd024876fdddfef2a2383492e1c1c",
        "fileSize": 34849878,
        "logicalFilename": "CET 1.32.2",
        "updatePolicy": "prefer",
        "tag": "x_A_Q2gQ3e"
      },
      "author": "yamashi",
      "details": {
        "category": "Modders Resources",
        "type": ""
      },
      "phase": 0
    },
    {
      "name": "Load Begone (Intro Splash Load and Checkpoint Removal - FOMOD) - Load Begone - 2.2.1 (FOMOD)",
      "version": "2.2.1",
      "optional": false,
      "domainName": "cyberpunk2077",
      "source": {
        "type": "nexus",
        "modId": 8144,
        "fileId": 59926,
        "md5": "f86b6241862c140891771306282abbf9",
        "fileSize": 3911564,
        "logicalFilename": "Load Begone - 2.2.1 (FOMOD)",
        "updatePolicy": "prefer",
        "tag": "vACbpm9SFd"
      },
      "choices": {
        "type": "fomod",
        "options": [
          {
            "name": "Installation",
            "groups": [
              {
                "name": "Features",
                "choices": [
                  {
                    "name": "Skip Intro Logos",
                    "idx": 0
                  },
                  {
                    "name": "No Splash Video",
                    "idx": 1
                  },
                  {
                    "name": "Faster Checkpoints",
                    "idx": 2
                  }
                ]
              }
            ]
          }
        ]
      },
      "author": "CyanideX",
      "details": {
        "category": "User Interface",
        "type": ""
      },
      "phase": 0
    }
  ],
  "modRules": [],
  "loadOrder": [],
  "tools": [],
  "collectionConfig": {
    "recommendNewProfile": false
  }
}



```

This data format isn't too complicated, and it is mostly self-explanatory. The `info` field contains metadata about the collection,
such as the author, name, description, and game versions. The `mods` field contains a list of mods in the collection. Each `mod` may
be sourced from one of several locations based on the `source` field. 

The way the mod is installed is influenced by the `hashes` field and the `choices` field. The `hashes` field indicates a list of files
from the mod that should be installed to a given path, indicated by the MD5 hash. This method is used when the user selects `Replicate` as the mod mode when
creating the collection. Installation with this method is fairly simple, the files in the mod are hashed and then copied based on the input hashes.

The `choices` field is used when the user selects `Fomod` as the mod mode when creating the collection. This is more complex, and is not yet implemented
in the app. 


## Known Unknowns

Here are some unknows that need to be resolved before collections can be fully implemented:

- What are phases? And do they matter at all for our purposes?
- What other types of `source` fields are there?
- Are there any other fields besides `hashes` and `choices` that are relevant to the installation process?
- What other sorts of installer `choices` are there besides `fomod`?
- What is the `tag` field in the `source` object?

