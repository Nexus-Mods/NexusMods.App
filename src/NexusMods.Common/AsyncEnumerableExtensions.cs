namespace NexusMods.Common;

/// <summary>
/// Extensions related to implementations of <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Filter a <see cref="IAsyncEnumerable{T}"/> by a given predicate.
    /// </summary>
    /// <param name="coll">The collection to filter.</param>
    /// <param name="func">The function to apply if the item should be returned.</param>
    /// <typeparam name="T">Type of item within the collection.</typeparam>
    /// <returns>Filtered collection that can be asynchronously retrieved.</returns>
    public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> coll, Func<T, bool> func)
    {
        await foreach (var itm in coll)
            if (func(itm))
                yield return itm;
    }
}