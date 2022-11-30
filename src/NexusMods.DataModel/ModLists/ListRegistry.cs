using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.ModLists;

public record ListRegistry(EntityDictionary<string, ModList> Lists) : Entity, IEmpty<ListRegistry>
{
    public static ListRegistry Empty => new(EntityDictionary<string, ModList>.Empty());
    
    public override EntityCategory Category => EntityCategory.ModLists;
}