using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// A step that extracts a file from an archive to a location.
/// </summary>
public record ExtractFile : IApplyStep
{
    
    /// <summary>
    /// The location where the file will be extracted to.
    /// </summary>
    public required AbsolutePath To { get; init; }
   
    /// <summary>
    /// The hash of the file being extracted.
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The size of the file being extracted.
    /// </summary>
    public required Size Size { get; init; }
}
