using NexusMods.Paths;

namespace NexusMods.DataModel.RateLimiting;

public static class ResourceExtensions
{
    private static IEnumerable<TItm> SkipItems<TItm>(IReadOnlyList<TItm> coll, int offset, int segmentSize)
    {
        for (var idx = offset; idx < coll.Count; idx += segmentSize)
        {
            yield return coll[idx];
        }
    }

    public static IAsyncEnumerable<TItem> ForEachFile<TResource, TItem>(this IResource<TResource, Size> resource,
        AbsolutePath root, Func<IJob<Size>, FileEntry,
            ValueTask<TItem>> fn,
        CancellationToken? token = null,
        string jobName = "Processing Files")
    {
        return ForEachFile(resource, new[] { root }, fn, token, jobName);
    }
    
    public static async IAsyncEnumerable<TItem> ForEachFile<TResource, TItem>(this IResource<TResource, Size> resource, 
        IEnumerable<AbsolutePath> roots, Func<IJob<Size>, FileEntry, 
            ValueTask<TItem>> fn,
        CancellationToken? token = null, 
        string jobName = "Processing Files")
    {
        token ??= CancellationToken.None;
        
        var asList = roots.SelectMany(f => f.EnumerateFileEntries()).OrderBy(x => x.Size).ToList();

        var tasks = new List<Task<List<TItem>>>();
        
        var maxJobs = resource.MaxJobs;

        
        tasks.AddRange(Enumerable.Range(0, maxJobs).Select(i => Task.Run(async () =>
        {
            var totalSize = SkipItems(asList, i, maxJobs)
                .Aggregate(Size.Zero, (acc, i) => acc + i.Size);
            
            using var job = await resource.Begin(jobName, totalSize, token.Value);
            var list = new List<TItem>();

            foreach (var itm in SkipItems(asList, i, maxJobs))
            {
                list.Add(await fn(job, itm));
            }
            return list;
        })));

        foreach (var result in tasks)
        {
            foreach (var itm in (await result))
            {
                yield return itm;
            }
        }
    }
    
}