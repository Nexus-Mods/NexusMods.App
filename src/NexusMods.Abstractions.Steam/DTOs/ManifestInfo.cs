using NexusMods.Abstractions.Steam.Values;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Steam.DTOs;

/// <summary>
/// Meta information about a manifest, not the actual contents, just the id
/// and the size of the files in aggregate.
/// </summary>
public class ManifestInfo
{
    /// <summary>
    /// The globally unique identifier of the manifest.
    /// </summary>
    public required ManifestId ManifestId { get; init; }
    
    /// <summary>
    /// The size of the downloaded files, decompressed
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The size of the files, compressed
    /// </summary>
    public required Size DownloadSize { get; init; }
}
