
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// Cached metadata for a generated file.
/// </summary>
[JsonName("NexusMods.DataModel.Loadouts.ModFiles.CachedGeneratedFileData")]
public record CachedGeneratedFileData : Entity
{
    public override EntityCategory Category => EntityCategory.Fingerprints;
    
    public required Hash Hash { get; init; }
    public required Size Size { get; init; }
}
