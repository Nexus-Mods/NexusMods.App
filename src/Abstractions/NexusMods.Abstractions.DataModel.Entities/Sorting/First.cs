using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Abstractions.DataModel.Entities.Sorting;

/// <summary />
[JsonName("First")]
public record First<TType, TId> : ISortRule<TType, TId>;
