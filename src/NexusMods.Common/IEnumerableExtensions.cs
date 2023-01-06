namespace NexusMods.Common;

public static class IEnumerableExtensions
{
    /// <summary>
    /// Transforms a IEnumerable<T> into a IAsyncEnumerable<TOut> via a transform function
    /// </summary>
    /// <param name="coll"></param>
    /// <param name="fn"></param>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <returns></returns>
    public static async IAsyncEnumerable<TOut> SelectAsync<TIn, TOut>(this IEnumerable<TIn> coll,
        Func<TIn, ValueTask<TOut>> fn)
    {
        foreach (var itm in coll)
            yield return await fn(itm);
    }
    
    
    /// <summary>
    /// Reduces a IAsyncEnumerable of KeyValuePairs into a Dictionary
    /// </summary>
    /// <param name="coll"></param>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    /// <returns></returns>
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
    
    /// <summary>
    /// Transforms a IAsyncEnumerable into a dictionary creating keys with a key selector
    /// </summary>
    /// <param name="coll"></param>
    /// <param name="keySelector"></param>
    /// <typeparam name="TItm"></typeparam>
    /// <typeparam name="TK"></typeparam>
    /// <returns></returns>
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
    
    /// <summary>
    /// Transforms a IAsyncEnumerable into a dictionary creating keys with a key selector and values with a value selector
    /// </summary>
    /// <param name="coll"></param>
    /// <param name="keySelector"></param>
    /// <param name="valueSelector"></param>
    /// <typeparam name="TItm"></typeparam>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    /// <returns></returns>
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