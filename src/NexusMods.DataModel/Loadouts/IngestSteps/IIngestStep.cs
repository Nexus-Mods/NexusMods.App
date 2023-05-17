using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Represents a step performed during the loadout application process.
/// </summary>
public interface IIngestStep
{ 
    public AbsolutePath To { get; }
}
