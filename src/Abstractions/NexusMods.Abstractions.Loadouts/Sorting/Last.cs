using JetBrains.Annotations;

namespace NexusMods.Abstractions.DataModel.Entities.Sorting;

[PublicAPI]
public record Last<TType, TId> : ISortRule<TType, TId>;
