using JetBrains.Annotations;

namespace NexusMods.Abstractions.Loadouts.Sorting;

[PublicAPI]
public record Last<TType, TId> : ISortRule<TType, TId>;
