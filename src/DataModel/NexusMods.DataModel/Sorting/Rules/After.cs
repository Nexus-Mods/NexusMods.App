using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

/// <summary />
[JsonName("After")]
public record After<TType, TId> : ISortRule<TType, TId>
{
    public required TId Other { get; init; }
}
