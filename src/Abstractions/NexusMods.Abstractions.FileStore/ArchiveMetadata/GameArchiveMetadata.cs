﻿using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Abstractions.FileStore.ArchiveMetadata;


/// <summary>
/// Archive metadata for a download that was installed from an existing game archive.
/// </summary>
[JsonName("NexusMods.Abstractions.Games.ArchiveMetadata.GameArchiveMetadata")]
public record GameArchiveMetadata : AArchiveMetaData
{
    public required GameInstallation Installation { get; init; }
}
