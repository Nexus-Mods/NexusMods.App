using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

/// <summary>
/// Represents a step performed during the loadout application process.
/// </summary>
public interface IIngestStep
{ 
    /// <summary>
    /// The path to the file that was changed, used mostly for debugging and logging. Unlike
    /// IApplyStep.To (previously removed) is that in the case of ingestion all files have source
    /// path on disk.
    /// </summary>
    public AbsolutePath Source { get; }
}
