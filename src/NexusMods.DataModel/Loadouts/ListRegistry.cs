using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts;

[JsonName("NexusMods.DataModel.ListRegistry")]
public record ListRegistry: Entity, IEmptyWithDataStore<ListRegistry>
{
    public required EntityDictionary<LoadoutId, Loadout> Lists {get; init; }
    public override EntityCategory Category => EntityCategory.Loadouts;
    public static ListRegistry Empty(IDataStore store) => new()
    {
        Lists = EntityDictionary<LoadoutId, Loadout>.Empty(store),
        Store = store
    };
}