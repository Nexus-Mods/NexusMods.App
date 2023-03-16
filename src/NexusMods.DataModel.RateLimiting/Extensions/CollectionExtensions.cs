namespace NexusMods.DataModel.RateLimiting.Extensions;

/// <summary>
/// Extension methods tied to collections.
/// </summary>
public static class CollectionExtensions
{
    // TODO: This can be span-ified.

    /// <summary>
    /// Retrieves every Xth item from a collection.
    /// </summary>
    /// <param name="coll">Collection to get items from.</param>
    /// <param name="offset">Offset of first item to get.</param>
    /// <param name="stride">Get every stride-th item.</param>
    /// <typeparam name="TItm"></typeparam>
    /// <example>
    ///    Stride of 2 and offset of 1 means items 1,3,5,7 will be selected.
    /// </example>
    /// <returns></returns>
    public static IEnumerable<TItm> GetEveryXItem<TItm>(this IReadOnlyList<TItm> coll, int offset, int stride)
    {
        for (var idx = offset; idx < coll.Count; idx += stride)
            yield return coll[idx];
    }
}
