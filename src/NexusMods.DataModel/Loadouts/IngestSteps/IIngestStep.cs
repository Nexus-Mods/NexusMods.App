using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

/// <summary>
/// Represents a step performed during the loadout application process.
/// </summary>
public interface IIngestStep
{ 
    /// <summary>
    /// The path to the file that was changed.
    /// </summary>
    public AbsolutePath To { get; }
}
