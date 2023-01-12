using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting;

[JsonName("Before")]
public record Before<TType, TId>(TId Other) : ISortRule<TType, TId> 
    where TType : IHasEntityId<TId>
{
    
}