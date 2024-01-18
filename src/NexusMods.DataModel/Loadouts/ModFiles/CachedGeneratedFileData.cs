using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// Cached metadata for a generated file.
/// </summary>
[JsonName("NexusMods.DataModel.Loadouts.ModFiles.CachedGeneratedFileData")]
public record CachedGeneratedFileData : Entity
{
    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Fingerprints;
    
    /// <summary>
    /// The hash of the generated data
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The size of the generated data
    /// </summary>
    public required Size Size { get; init; }
}
