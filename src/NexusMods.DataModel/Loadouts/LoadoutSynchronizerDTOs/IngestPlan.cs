using NexusMods.DataModel.Loadouts.IngestSteps;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// A plan to ingest the changes from the game folder into the loadout.
/// </summary>
public record IngestPlan : Plan
{
    /// <summary>
    /// The steps required to ingest the changes to the loadout.
    /// </summary>
    public required IEnumerable<IIngestStep> Steps { get; init; }    
}
