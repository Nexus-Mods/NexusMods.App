using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

[JsonName("Before")]
public record Before<TType, TId>(TId Other) : ISortRule<TType, TId> 
{
    
}