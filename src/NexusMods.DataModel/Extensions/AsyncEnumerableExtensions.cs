namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Provides various extensions for implementations of <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Asynchronously enumerates this enumerable, returning the results as a list.
    /// </summary>
    /// <param name="coll">The enumerable to be returned as list.</param>
    /// <typeparam name="T">Type of item stored in the collection.</typeparam>
    /// <returns>
    ///     The results of the enumerable.
    /// </returns>
    /// <remarks>
    ///     The result is only available once all items have been enumerated,
    ///     which may be undesirable. To consume items one by one as they are
    ///     available, consider using <see cref="SelectAsync{TIn,TOut}"/>.
    /// </remarks>
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> coll)
    {
        var lst = new List<T>();
        await foreach (var itm in coll)
            lst.Add(itm);

        return lst;
    }

    /// <summary>
    /// Asynchronously enumerates this enumerable, returning the results as an array.
    /// </summary>
    /// <param name="coll">The collection to enumerate to array.</param>
    /// <returns>The enumerated collection as array.</returns>
    /// <remarks>
    ///     Prefer using <see cref="ToListAsync{T}"/> instead of this method.
    ///     This just calls <see cref="ToListAsync{T}"/> and creates a copy of the
    ///     data.<br/><br/>
    ///
    ///     The result is only available once all items have been enumerated,
    ///     which may be undesirable. To consume items one by one as they are
    ///     available, consider using <see cref="SelectAsync{TIn,TOut}"/>.
    /// </remarks>
    public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> coll)
    {
        return (await coll.ToListAsync()).ToArray();
    }

    /// <summary>
    /// Asynchronously enumerates this enumerable, returning the results as a hashset.
    /// </summary>
    /// <param name="coll">The collection to enumerate to hashset.</param>
    /// <typeparam name="T">The type to create a hashset from.</typeparam>
    /// <returns>The enumerated collection as a hashset.</returns>
    /// <remarks>
    ///     The result is only available once all items have been enumerated,
    ///     which may be undesirable. To consume items one by one as they are
    ///     available, consider using <see cref="SelectAsync{TIn,TOut}"/>.
    /// </remarks>
    public static async Task<HashSet<T>> ToHashSetAsync<T>(this IAsyncEnumerable<T> coll)
    {
        var lst = new HashSet<T>();
        await foreach (var itm in coll)
            lst.Add(itm);

        return lst;
    }

    /// <summary>
    /// Asynchronously enumerates this enumerable, returning the results as a hashset.
    /// </summary>
    /// <param name="coll">Collection of items to pass to the selector/transform.</param>
    /// <param name="func">Function used to transform the item to output.</param>
    /// <typeparam name="TIn">Type of input item.</typeparam>
    /// <typeparam name="TOut">Type of output item.</typeparam>
    /// <remarks>
    ///    This function is analogous to <see cref="Enumerable.Select{TSource,TResult}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,int,TResult})"/>
    /// </remarks>
    public static async IAsyncEnumerable<TOut> SelectAsync<TIn, TOut>(this IAsyncEnumerable<TIn> coll, Func<TIn, TOut> func)
    {
        await foreach (var itm in coll)
            yield return func(itm);
    }
}
