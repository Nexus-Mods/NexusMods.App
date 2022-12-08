namespace NexusMods.Common;

public static class IAsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> coll, Func<T, bool> func)
    {
        await foreach (var itm in coll)
            if (func(itm))
                yield return itm;
    }
}