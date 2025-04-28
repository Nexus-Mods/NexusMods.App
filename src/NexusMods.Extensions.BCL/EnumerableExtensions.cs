using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Extensions.BCL;

/// <summary>
/// Extensions for collections implementing the enumerable interface.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class EnumerableExtensions
{
    /// <summary>
    /// <see cref="Enumerable.FirstOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource})"/>
    /// in a try-get style. This is helpful for value types like structs that have a non-null default value.
    /// </summary>
    /// <returns></returns>
    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, [NotNullWhen(true)] out T? value)
    where T : notnull
    {
        foreach (var item in enumerable)
        {
            if (!predicate(item)) continue;
            value = item;
            return true;
        }

        value = default(T);
        return false;
    }
    
    /// <summary>
    /// <see cref="Enumerable.FirstOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource})"/>
    /// in a try-get style. This is helpful for value types like structs that have a non-null default value.
    /// </summary>
    /// <returns></returns>
    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, out T? value)
    {
        foreach (var item in enumerable)
        {
            value = item;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Transforms a <see cref="IEnumerable{T}"/> into a <see cref="IAsyncEnumerable{TOut}"/> via a transform function
    /// </summary>
    /// <param name="coll">The collection to apply the operation on.</param>
    /// <param name="fn">The function that returns an output for each input.</param>
    /// <typeparam name="TIn">Type of input item used.</typeparam>
    /// <typeparam name="TOut">Type of output item used.</typeparam>
    public static async IAsyncEnumerable<TOut> SelectAsync<TIn, TOut>(this IEnumerable<TIn> coll,
        Func<TIn, ValueTask<TOut>> fn)
    {
        foreach (var itm in coll)
            yield return await fn(itm);
    }


    /// <summary>
    /// Returns the first item in the collection that matches the predicate or the default value of the type.
    /// </summary>
    /// <param name="coll"></param>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async ValueTask<T> FirstOrDefault<T>(this IAsyncEnumerable<T> coll, Func<T, bool> predicate)
    {
        await foreach (var itm in coll)
        {
            if (!predicate(itm)) continue;
            return itm;
        }
        return default!;
    }

    /// <summary>
    /// Reduces a <see cref="IAsyncEnumerable{T}"/> of <see cref="KeyValuePair{TKey,TValue}"/> into a <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <param name="coll">The collection to apply the operation on.</param>
    /// <typeparam name="TKey">Type of key used.</typeparam>
    /// <typeparam name="TValue">Type of value used.</typeparam>
    public static async Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(this IAsyncEnumerable<KeyValuePair<TKey, TValue>> coll)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>();
        await foreach (var itm in coll)
            dict.Add(itm.Key, itm.Value);

        return dict;
    }

    /// <summary>
    /// Reduces a <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey,TValue}"/> into a <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <param name="coll">The collection to apply the operation on.</param>
    /// <typeparam name="TKey">Type of key used.</typeparam>
    /// <typeparam name="TValue">Type of value used.</typeparam>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> coll)
        where TKey : notnull
    {
        return coll.ToDictionary(itm => itm.Key, itm => itm.Value);
    }

    /// <summary>
    /// Transforms a IAsyncEnumerable into a dictionary creating keys with a key selector
    /// </summary>
    /// <param name="coll">The collection to apply the operation on.</param>
    /// <param name="keySelector">Function which returns the key given an item.</param>
    /// <typeparam name="TItem">The type of item we are operating on.</typeparam>
    /// <typeparam name="TKey">The key bound to each item.</typeparam>
    public static async Task<Dictionary<TKey, TItem>> ToDictionary<TItem, TKey>(this IAsyncEnumerable<TItem> coll, Func<TItem, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, TItem>();
        await foreach (var itm in coll)
            dict.Add(keySelector(itm), itm);

        return dict;
    }

    /// <summary>
    /// Does a linear search and returns the index of the first item that matches the predicate or -1
    /// if no item matches the predicate.
    /// </summary>
    public static int LinearSearch<T>(this IList<T> source, Func<T, bool> predicate)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (predicate(source[i])) return i;
        }

        return -1;
    }

    /// <summary>
    /// Applies a function to each item in the source collection in parallel. Results are returned in a undefined order.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="body"></param>
    /// <typeparam name="TFrom"></typeparam>
    /// <typeparam name="TTo"></typeparam>
    /// <returns></returns>
    public static async Task<IEnumerable<TTo>> ParallelForEach<TFrom, TTo>(this IEnumerable<TFrom> source, Func<TFrom, CancellationToken, ValueTask<TTo>> body)
    {
        var bag = new ConcurrentBag<TTo>();
        await Parallel.ForEachAsync(source, async (item, t) =>
        {
            var result = await body(item, t);
            bag.Add(result);
        });
        return bag;
    }
}
