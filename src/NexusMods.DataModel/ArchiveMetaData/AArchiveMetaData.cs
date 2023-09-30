using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveMetaData;

/// <summary>
/// Info for a mod archive describing where it came from and the suggested name of the file
/// </summary>
[JsonName("NexusMods.DataModel.ArchiveMetaData.AArchiveMetaData")]
public abstract record AArchiveMetaData
{
    /// <summary>
    /// A human readable name for the archive.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// How accurate is this metadata? Data from a file on disk is more generic than data
    /// from a NexusMods API call.
    /// </summary>
    public required Quality Quality { get; init; }
}
