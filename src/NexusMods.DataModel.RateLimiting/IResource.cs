using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

public interface IResource
{
    string Name { get; }
    int MaxJobs { get; set; }
    IEnumerable<IJob> Jobs { get; }
}

/// <summary>
/// A Interface representing a typed resource. The methods on this interface can be used to
/// create jobs and report info about them. The methods may delay returning based on the current
/// rate limits of the resource.
/// </summary>
/// <typeparam name="TResource">A marker for the service owning this limiter</typeparam>
/// <typeparam name="TUnit">The unit of measurement of job size</typeparam>
public interface IResource<TResource, TUnit> : IResource
where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>, IDivisionOperators<TUnit, TUnit, double>
{
    /// <summary>
    /// Start a new job
    /// </summary>
    /// <param name="jobTitle">A name for the job</param>
    /// <param name="size">The number of items in the job</param>
    /// <param name="token">Cancellation token to exit the rate limiting early</param>
    /// <returns></returns>
    public ValueTask<IJob<TResource, TUnit>> Begin(string jobTitle, TUnit size, CancellationToken token);

    public void Finish(IJob<TResource, TUnit> job);

    ValueTask Report(Job<TResource, TUnit> job, TUnit processedSize, CancellationToken token);
    void ReportNoWait(Job<TResource, TUnit> job, TUnit processedSize);

    StatusReport<TUnit> StatusReport { get; }
    TUnit MaxThroughput { get; set; }
}
