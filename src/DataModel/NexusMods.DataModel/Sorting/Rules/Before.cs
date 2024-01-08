using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

/// <summary />
[JsonName("Before")]
public record Before<TType, TId>: ISortRule<TType, TId>
{
    public required TId Other { get; init; }
}
