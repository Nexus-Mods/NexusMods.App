using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// All the state data required to apply a loadout.
/// </summary>
public record ApplyPlan
{
    /// <summary>
    /// The steps required to apply the loadout.
    /// </summary>
    public required IEnumerable<IApplyStep> Steps { get; init; }
    
    /// <summary>
    /// The flattened modlist as created during the plan generation.
    /// </summary>
    public required IReadOnlyDictionary<GamePath, ModFilePair> Flattened { get; init; }
    
    /// <summary>
    /// The sorted list of mods as created during the plan generation.
    /// </summary>
    public required IEnumerable<Mod> Mods { get; init; }

    /// <summary>
    /// The loadout from which the plan was generated
    /// </summary>
    public required Loadout Loadout { get; set; }
}
