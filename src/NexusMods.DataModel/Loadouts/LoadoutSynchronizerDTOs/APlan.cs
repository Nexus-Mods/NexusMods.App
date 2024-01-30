using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// A generic plan, without the finalized steps (yet).
/// </summary>
public record Plan
{
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
    public required Loadout Loadout { get; init; }
}
