using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Sorting;

public record After<TType, TId>(TId Other) : ISortRule<TType, TId> 
    where TType : IHasEntityId<TId>
{
    
}