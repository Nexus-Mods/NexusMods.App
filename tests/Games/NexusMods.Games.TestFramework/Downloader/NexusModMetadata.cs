﻿using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Games.TestFramework.Downloader;

/// <summary>
/// Metadata for a remote mod.
/// </summary>
public class NexusModMetadata : RemoteModMetadataBase
{
    /// <summary>
    /// Mod ID corresponding to the Nexus API.
    /// </summary>
    public ModId ModId { get; set; }

    /// <summary>
    /// File ID corresponding to the Nexus API.
    /// </summary>
    public FileId FileId { get; set; }

    /// <summary>
    /// Expected hash of the file (sanity check!).
    /// </summary>
    public Hash Hash { get; set; }
}
