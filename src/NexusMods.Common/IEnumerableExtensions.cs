namespace NexusMods.Common;

public static class IEnumerableExtensions
{
    public static async IAsyncEnumerable<TOut> SelectAsync<TIn, TOut>(this IEnumerable<TIn> coll,
        Func<TIn, ValueTask<TOut>> fn)
    {
        foreach (var itm in coll)
            yield return await fn(itm);
    }
    
    public static async Task<Dictionary<TK, TV>> ToDictionary<TK, TV>(this IAsyncEnumerable<KeyValuePair<TK, TV>> coll) 
        where TK : notnull
    {
        var dict = new Dictionary<TK, TV>();
        await foreach (var itm in coll)
        {
            dict.Add(itm.Key, itm.Value);
        }

        return dict;
    }
    
    public static async Task<Dictionary<TK, TItm>> ToDictionary<TItm, TK>(this IAsyncEnumerable<TItm> coll, Func<TItm, TK> keySelector) 
        where TK : notnull
    {
        var dict = new Dictionary<TK, TItm>();
        await foreach (var itm in coll)
        {
            dict.Add(keySelector(itm), itm);
        }

        return dict;
    }
    
    public static async Task<Dictionary<TK, TV>> ToDictionary<TItm, TK, TV>(this IAsyncEnumerable<TItm> coll,
    Func<TItm, TK> keySelector,
        Func<TItm, TV> valueSelector) 
        where TK : notnull
    {
        var dict = new Dictionary<TK, TV>();
        await foreach (var itm in coll)
        {
            dict.Add(keySelector(itm), valueSelector(itm));
        }
        return dict;
    }
}