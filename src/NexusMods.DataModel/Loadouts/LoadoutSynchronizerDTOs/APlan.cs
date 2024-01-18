using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers.DTO;

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
