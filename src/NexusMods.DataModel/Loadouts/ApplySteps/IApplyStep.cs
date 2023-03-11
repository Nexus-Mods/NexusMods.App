using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Represents a step performed during the loadout application process.
/// </summary>
public interface IApplyStep
{
    /// <summary>
    /// The file to apply this operation to.
    /// </summary>
    public AbsolutePath To { get; }
}
