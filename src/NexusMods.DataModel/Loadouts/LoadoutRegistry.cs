using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts;

[JsonName("NexusMods.DataModel.LoadoutRegistry")]
public record LoadoutRegistry: Entity, IEmptyWithDataStore<LoadoutRegistry>
{
    public required EntityDictionary<LoadoutId, Loadout> Lists {get; init; }
    public override EntityCategory Category => EntityCategory.Loadouts;
    public static LoadoutRegistry Empty(IDataStore store) => new()
    {
        Lists = EntityDictionary<LoadoutId, Loadout>.Empty(store),
        Store = store
    };
}