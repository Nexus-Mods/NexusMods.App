using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Sorting.Rules;

/// <summary />
[JsonName("First")]
public record First<TType, TId> : ISortRule<TType, TId>;
