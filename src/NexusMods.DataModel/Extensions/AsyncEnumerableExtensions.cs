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
}