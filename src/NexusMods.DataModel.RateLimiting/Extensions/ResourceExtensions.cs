using NexusMods.Paths;

namespace NexusMods.DataModel.RateLimiting.Extensions;

/// <summary>
/// Extension methods for handy creation of new jobs via the <see cref="Resource{TResource,TUnit}"/> class.
/// </summary>
public static class ResourceExtensions
{
    /// <summary>
    /// Finds all files within a given directory and runs a user specified operation on them.
    /// </summary>
    /// <param name="resource">The resource which will run all the asynchronous operations.</param>
    /// <param name="root">The folder inside which all files will be found and processed.</param>
    /// <param name="fn">Function that processes every file found.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="jobName">Human friendly name for each job created.</param>
    /// <typeparam name="TResource">A marker for the service owning this limiter</typeparam>
    /// <typeparam name="TItem">Type of item returned from user function.</typeparam>
    /// <returns></returns>
    public static IAsyncEnumerable<TItem> ForEachFileAsync<TResource, TItem>(this IResource<TResource, Size> resource,
        AbsolutePath root, Func<IJob<Size>, IFileEntry, ValueTask<TItem>> fn,
        CancellationToken? token = null, string jobName = "Processing Files")
    {
        return ForEachFileAsync(resource, new[] { root }, fn, token, jobName);
    }

    /// <summary>
    /// Finds all files within a given directory and runs a user specified operation on them.
    /// </summary>
    /// <param name="resource">The resource which will run all the asynchronous operations.</param>
    /// <param name="roots">The folders inside which all files will be found and processed.</param>
    /// <param name="fn">Function that processes every file found.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="jobName">Human friendly name for each job created.</param>
    /// <typeparam name="TResource">A marker for the service owning this limiter</typeparam>
    /// <typeparam name="TItem">Type of item returned from user function.</typeparam>
    /// <returns></returns>
    /// <remarks>
    ///    In the current implementation, results are returned in batch; you
    ///    will only get results once a slice [group] of files are finished processing.
    /// </remarks>
    public static async IAsyncEnumerable<TItem> ForEachFileAsync<TResource, TItem>(this IResource<TResource, Size> resource,
        IEnumerable<AbsolutePath> roots, Func<IJob<Size>, IFileEntry, ValueTask<TItem>> fn,
        CancellationToken? token = null, string jobName = "Processing Files")
    {
        token ??= CancellationToken.None;

        var allFiles = roots.SelectMany(f => f.EnumerateFileEntries()).OrderBy(x => x.Size).ToList();
        var tasks = new List<Task<List<TItem>>>();
        var maxJobs = resource.MaxJobs;

        // TODO: Can we dedupe this?

        // The idea here is we partition the files into maxJobs slices.
        // Then, as a means of thread balancing the workload,
        tasks.AddRange(Enumerable.Range(0, maxJobs).Select(i => Task.Run(async () =>
        {
            var totalSize = allFiles.GetEveryXItem(i, maxJobs).Aggregate(Size.Zero, (acc, x) => acc + x.Size);

            using var job = await resource.BeginAsync(jobName, totalSize, token.Value);
            var list = new List<TItem>();

            foreach (var itm in allFiles.GetEveryXItem(i, maxJobs))
            {
                list.Add(await fn(job, itm));
            }
            return list;
        })));

        foreach (var result in tasks)
            foreach (var itm in (await result))
                yield return itm;
    }

    /// <summary>
    /// Runs a user specified operation on each user supplied item in parallel.
    /// </summary>
    /// <param name="resource">The resource which will run all the asynchronous operations.</param>
    /// <param name="src">The items to run operations on.</param>
    /// <param name="sizeFn">Function which determines the size of an item.</param>
    /// <param name="fn">Function that processes every file found.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="jobName">Human friendly name for each job created.</param>
    /// <typeparam name="TResource">A marker for the service owning this limiter</typeparam>
    /// <typeparam name="TItem">Type of item returned from user function.</typeparam>
    /// <returns></returns>
    public static async Task ForEachAsync<TResource, TItem>(this IResource<TResource, Size> resource,
        IEnumerable<TItem> src,
        Func<TItem, Size> sizeFn,
        Func<IJob<Size>, TItem, ValueTask> fn,
        CancellationToken? token = null,
        string jobName = "Processing Files")
    {
        token ??= CancellationToken.None;

        var asList = src.OrderByDescending(sizeFn).ToList();
        var tasks = new List<Task<List<TItem>>>();
        var maxJobs = resource.MaxJobs;

        tasks.AddRange(Enumerable.Range(0, maxJobs).Select(i => Task.Run(async () =>
        {
            var totalSize = asList.GetEveryXItem(i, maxJobs)
                .Aggregate(Size.Zero, (acc, x) => acc + sizeFn(x));

            using var job = await resource.BeginAsync(jobName, totalSize, token.Value);
            var list = new List<TItem>();
            foreach (var itm in asList.GetEveryXItem(i, maxJobs))
            {
                await fn(job, itm);
            }

            return list;
        })));

        await Task.WhenAll(tasks);
    }
}
