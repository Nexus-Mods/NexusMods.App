using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

[JsonName("After")]
public record After<TType, TId>(TId Other) : ISortRule<TType, TId> 
{
    
}