using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

/// <summary>
/// Specifies that the file should be removed from the loadout because something else removed the file from the game folders
/// </summary>
public record RemoveFromLoadout : IIngestStep
{
    public required AbsolutePath To { get; init; }
}
