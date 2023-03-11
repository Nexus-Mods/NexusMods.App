using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// The <see cref="Root{TRoot}"/> for all loadouts (profiles/mod lists) stored
/// within the application. Any mod not accessible through this registry is subject
/// to potential removal from the data store.
/// </summary>
[JsonName("LoadoutRegistry")]
public record LoadoutRegistry : Entity, IEmptyWithDataStore<LoadoutRegistry>
{
    /// <summary>
    /// A map of known loadouts (groups of installed mods) with their respective IDs.
    /// </summary>
    public required EntityDictionary<LoadoutId, Loadout> Lists { get; init; }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Loadouts;

    /// <inheritdoc />
    public static LoadoutRegistry Empty(IDataStore store) => new()
    {
        Lists = EntityDictionary<LoadoutId, Loadout>.Empty(store)
    };
}
