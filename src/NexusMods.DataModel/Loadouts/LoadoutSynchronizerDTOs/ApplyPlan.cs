using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// All the state data required to apply a loadout.
/// </summary>
public record ApplyPlan : APlan
{
    /// <summary>
    /// The steps required to apply the loadout.
    /// </summary>
    public required IEnumerable<IApplyStep> Steps { get; init; }
}
