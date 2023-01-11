using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Sorting;

public record First<TType, TId> : ISortRule<TType, TId> 
    where TType : IHasEntityId<TId>
{
    
}