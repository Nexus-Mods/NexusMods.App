using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.ModLists;

[JsonName("NexusMods.DataModel.ListRegistry")]
public record ListRegistry: Entity, IEmptyWithDataStore<ListRegistry>
{
    public required EntityDictionary<ModListId, ModList> Lists {get; init; }
    public override EntityCategory Category => EntityCategory.ModLists;
    public static ListRegistry Empty(IDataStore store) => new()
    {
        Lists = EntityDictionary<ModListId, ModList>.Empty(),
        Store = store
    };
}