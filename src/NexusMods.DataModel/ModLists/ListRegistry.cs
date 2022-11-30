using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.ModLists;

public record ListRegistry(EntityDictionary<ModListId, ModList> Lists) : Entity, IEmpty<ListRegistry>
{
    public static ListRegistry Empty => new(EntityDictionary<ModListId, ModList>.Empty());
    
    public override EntityCategory Category => EntityCategory.ModLists;
}