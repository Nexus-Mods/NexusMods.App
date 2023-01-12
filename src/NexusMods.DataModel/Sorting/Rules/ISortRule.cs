using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Sorting.Rules;

/// <summary>
/// A marker interface for rules used in sorting
/// </summary>
/// <typeparam name="TType"></typeparam>
/// <typeparam name="TId"></typeparam>
public interface ISortRule<TType, TId> 
where TType : IHasEntityId<TId>
{
    
}
