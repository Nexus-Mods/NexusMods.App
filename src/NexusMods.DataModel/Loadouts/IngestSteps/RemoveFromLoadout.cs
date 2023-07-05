using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

/// <summary>
/// Specifies that the file should be removed from the loadout because something else removed the file from the game folders
/// </summary>
public record RemoveFromLoadout : IIngestStep
{
    /// <summary>
    /// The file to remove from the loadout, all files in any mod that maps to this file will be removed
    /// </summary>
    public required AbsolutePath Source { get; init; }
}
