namespace NexusMods.DataModel.Extensions;

public static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> coll)
    {
        var lst = new List<T>();
        await foreach (var itm in coll)
        {
            lst.Add(itm);
        }
        return lst;
    }
    
    public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> coll)
    {
        return (await coll.ToList()).ToArray();
    }
    public static async Task<HashSet<T>> ToHashSet<T>(this IAsyncEnumerable<T> coll)
    {
        var lst = new HashSet<T>();
        await foreach (var itm in coll)
        {
            lst.Add(itm);
        }
        return lst;
    }

    public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(this IAsyncEnumerable<TIn> coll, Func<TIn, TOut> func)
    {
        await foreach (var itm in coll)
            yield return func(itm);
    }
}