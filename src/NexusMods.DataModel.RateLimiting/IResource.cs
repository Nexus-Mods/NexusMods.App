using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

/// <summary>
/// A Interface representing a resource. Methods on this interface can be
/// used to query jobs and obtain more info about them; in a way that
/// does not require knowing more about the jobs' underlying types.
/// </summary>
public interface IResource
{
    /// <summary>
    /// Friendly name for the resource, e.g. 'File Hashing'.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Maximum number of jobs this resource can have running at any given moment.
    /// </summary>
    /// <remarks>
    ///    This is usually restricted to CPU thread count for CPU bound operations.
    /// </remarks>
    int MaxJobs { get; set; }

    /// <summary>
    /// Collection of all the jobs/tasks that are currently being processed.
    /// </summary>
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
    /// Starts a new job.
    ///
    /// This function may stall execution if maximum level of parallelism dictated
    /// by <see cref="IResource.MaxJobs"/> is reached until an existing job finishes,
    /// allowing this job to take its spot.
    /// </summary>
    /// <param name="jobTitle">A name for the job.</param>
    /// <param name="size">The number of items in the job.</param>
    /// <param name="token">Cancellation token to exit the rate limiting early.</param>
    /// <returns>Awaitable job; this returns once execution has begun.</returns>
    public ValueTask<IJob<TResource, TUnit>> BeginAsync(string jobTitle, TUnit size, CancellationToken token);

    /// <summary>
    /// Call this to signal that a job has finished running.
    /// </summary>
    /// <param name="job">The job that has completed running.</param>
    /// <remarks>
    ///    This is usually called as part of a <see cref="Job{TResource,TUnit}"/>'s disposal routine.
    ///    Not calling this method is considered a fatal error; as this risks
    ///    filling up <see cref="IResource.MaxJobs"/> with no way back.
    /// </remarks>
    public void Finish(IJob<TResource, TUnit> job);

    /// <summary>
    /// Reports progress.
    /// When calling this function, execution might be stalled [throttled] to meet
    /// max throughput settings specified by the parent resource.
    /// </summary>
    /// <param name="processedSize">Total amount of work done since the last call to one of the report function(s).</param>
    /// <param name="token">Allows you to cancel the reporting operation.</param>
    /// <param name="job">The job whose progress is being reported.</param>
    ValueTask ReportAsync(Job<TResource, TUnit> job, TUnit processedSize, CancellationToken token);

    /// <summary>
    /// Reports progress without waiting/throttling.
    /// </summary>
    /// <param name="processedSize">Total amount of work done since the last call to one of the report function(s).</param>
    /// <param name="job">The job whose progress is being reported.</param>
    void ReportNoWait(Job<TResource, TUnit> job, TUnit processedSize);

    /// <summary>
    /// Creates a report of the current status for this resource.
    /// </summary>
    StatusReport<TUnit> StatusReport { get; }

    /// <summary>
    /// PER SECOND Maximum allowed rate of throughput for this resource; e.g. Max Download Speed [Bytes/s].
    /// When the rate of throughput exceeds this rate; jobs are throttled.
    /// </summary>
    TUnit MaxThroughput { get; set; }
}
