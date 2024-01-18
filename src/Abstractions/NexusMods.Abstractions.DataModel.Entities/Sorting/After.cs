using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Abstractions.DataModel.Entities.Sorting;

/// <summary />
[JsonName("After")]
public record After<TType, TId> : ISortRule<TType, TId>
{
    /// <summary>
    ///     ID of the other mod this mod should be placed after.
    /// </summary>
    public required TId Other { get; init; }
}
