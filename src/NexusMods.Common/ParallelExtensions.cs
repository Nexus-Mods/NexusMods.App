using System.Collections.Concurrent;

namespace NexusMods.Common;

public static class ParallelExtensions
{
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
