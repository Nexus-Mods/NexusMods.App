namespace NexusMods.DataModel.RateLimiting;

public interface IResource
{
    StatusReport StatusReport { get; }
    string Name { get; }
    int MaxTasks { get; set; }
    long MaxThroughput { get; set; }
    IEnumerable<IJob> Jobs { get; }
}

/// <summary>
/// A Interface representing a typed resource. The methods on this interface can be used to
/// create jobs and report info about them. The methods may delay returning based on the current
/// rate limits of the resource.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IResource<T> : IResource
{
    /// <summary>
    /// Start a new job
    /// </summary>
    /// <param name="jobTitle">A name for the job</param>
    /// <param name="size">The number of items in the job</param>
    /// <param name="token">Cancellation token to exit the rate limiting early</param>
    /// <returns></returns>
    public ValueTask<IJob<T>> Begin(string jobTitle, long size, CancellationToken token);
    ValueTask Report(IJob<T> job, int processedSize, CancellationToken token);
    void ReportNoWait(IJob<T> job, int processedSize);
    void Finish(IJob<T> job);
}