using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

/// <summary>
/// A ingest step that replaces a file in a loadout with new information from the game folder
/// </summary>
public record ReplaceInLoadout : IIngestStep
{
    /// <summary>
    /// The location of the source file
    /// </summary>
    public required AbsolutePath Source { get; init; }
    
    /// <summary>
    /// The new hash of the file
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The new size of the file
    /// </summary>
    public required Size Size { get; init; }
    
    /// <summary>
    /// The id of the mod file to be replaced
    /// </summary>
    public required ModFileId ModFileId { get; init; }
    
    /// <summary>
    /// The id of the mod that the file belongs to
    /// </summary>
    public required ModId ModId { get; init; }
}
