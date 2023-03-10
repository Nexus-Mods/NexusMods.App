using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

/// <summary />
[JsonName("Before")]
public record Before<TType, TId>(TId Other) : ISortRule<TType, TId>;
