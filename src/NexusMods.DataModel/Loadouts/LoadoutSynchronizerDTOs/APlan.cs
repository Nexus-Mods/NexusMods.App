using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// A generic plan, without the finalized steps (yet).
/// </summary>
public record Plan
{
    /// <summary>
    /// The flattened modlist as created during the plan generation.
    /// </summary>
    public required IReadOnlyDictionary<GamePath, File.Model> Flattened { get; init; }

    /// <summary>
    /// The sorted list of mods as created during the plan generation.
    /// </summary>
    public required IEnumerable<Mod.Model> Mods { get; init; }

    /// <summary>
    /// The loadout from which the plan was generated
    /// </summary>
    public required Loadout.Model Loadout { get; init; }
}
