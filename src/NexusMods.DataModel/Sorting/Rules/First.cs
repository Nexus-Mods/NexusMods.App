using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

[JsonName("First")]
public record First<TType, TId> : ISortRule<TType, TId> 
    where TType : IHasEntityId<TId>
{
    
}