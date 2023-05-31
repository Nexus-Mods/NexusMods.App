using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

/// <summary>
/// A job is like a <see cref="Task{TResult}"/>, however provides additional information
/// (metadata) that allows for functionality such as reporting progress.
/// </summary>
public interface IJob : IDisposable
{
    /// <summary>
    /// Unique identifier for this job.
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    /// A short description of the job; usually phrased like a title.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The current estimated level of progress for the job.
    /// Represents any underlying operation such as a download, file extraction,
    /// analyzing a file or other things that are trackable.
    /// </summary>
    public Percent Progress { get; }

    /// <summary>
    /// When this job was started.
    /// </summary>
    public DateTime StartedAt { get; }
    
    /// <summary>
    /// The current state of the job.
    /// </summary>
    public JobState CurrentState { get; set; }
    
    /// <summary>
    /// This is the last time the job was resumed, in situations where it may have been paused and then resumed.
    /// This is for the accurate calculation of the time remaining.
    /// </summary>
    public DateTime ResumedAt { get; }

    /// <summary>
    /// The underlying resource used to create this job; used for rate limiting,
    /// i.e. restricting max use of cores for specific task(s).
    /// </summary>
    IResource Resource { get; }
}

/// <summary>
/// Extended variant of <see cref="IJob"/> with ability to report total amount
/// of work to be done; and the work currently already done.<br/><br/>
///
/// This allows for tracking progress of jobs in format
/// e.g. '5.9/12.9GB' rather than showing a flat percentage.
/// </summary>
/// <typeparam name="TSize">Underlying type used for storing the size of work done.</typeparam>
public interface IJob<TSize> : IJob
{
    /// <summary>
    /// Total amount of work to be done.
    /// </summary>
    public TSize? Size { get; set; }

    /// <summary>
    /// Amount of work that was already done.
    /// </summary>
    public TSize Current { get; }
    
    /// <summary>
    /// Amount of work that was done at last resume time.
    /// </summary>
    public TSize CurrentAtResumeTime { get; }

    /// <summary>
    /// Reports progress.
    /// When calling this function, execution might be stalled [throttled] to meet
    /// max throughput settings specified by the parent resource.
    /// </summary>
    /// <param name="processed">Total amount of work done since the last call to one of the report function(s).</param>
    /// <param name="token">Allows you to cancel the reporting operation.</param>
    public ValueTask ReportAsync(TSize processed, CancellationToken token);

    /// <summary>
    /// Reports progress without waiting/throttling.
    /// </summary>
    /// <param name="processed">Total amount of work done since the last call to one of the report function(s).</param>
    public void ReportNoWait(TSize processed);
}

/// <summary>
/// Extension methods to the <see cref="IJob"/> interface.
/// </summary>
public static class JobExtensions
{
    /// <summary>
    /// Calculates the current throughput of this job per second, based on the current progress.
    /// </summary>
    /// <remarks>
    ///     This returns null/default if the type does not support the required arithmetic operations,
    ///     else it returns the current TSize items per second.
    /// </remarks>
    /// <returns>Throughput of this job per second.</returns>
    public static TSize GetThroughput<TSize>(this IJob<TSize> job, IDateTimeProvider provider) where TSize : IDivisionOperators<TSize, double, TSize>, 
        ISubtractionOperators<TSize, TSize, TSize>, IAdditiveIdentity<TSize, TSize>
    {
        if (job.CurrentState != JobState.Running)
            return TSize.AdditiveIdentity;
        
        var workDone = job.Current - job.CurrentAtResumeTime;
        var timeElapsed = (provider.GetCurrentTimeUtc() - job.ResumedAt).TotalSeconds;
        return workDone / timeElapsed;
    }
    
    /// <summary>
    /// Calculates the average throughput of a given collection of non-paused jobs per second, based on the current progress.
    /// </summary>
    /// <remarks>
    ///     This returns null/default if the type does not support the required arithmetic operations,
    ///     else it returns the current TSize items per second.
    /// </remarks>
    /// <returns>Throughput of this job per second.</returns>
    public static TSize GetTotalThroughput<TSize>(this IEnumerable<IJob<TSize>> jobs, IDateTimeProvider provider) where TSize : IDivisionOperators<TSize, double, TSize>, 
        ISubtractionOperators<TSize, TSize, TSize>, IAdditionOperators<TSize, TSize, TSize>, IAdditiveIdentity<TSize, TSize>
    {
        var totalThroughput = TSize.AdditiveIdentity;
        foreach (var job in jobs)
            totalThroughput += GetThroughput(job, provider);

        return totalThroughput;
    }
}

/// <summary>
/// An <see cref="IJob{TUnit}"/> with a typed owner resource.
/// </summary>
/// <typeparam name="TResource">Underlying type used to represent the parent resource.</typeparam>
/// <typeparam name="TSize">Underlying type used for storing the size of work done.</typeparam>
public interface IJob<TResource, TSize> : IJob<TSize>
{
    /// <summary>
    /// Type of the owning resource.
    /// </summary>
    public Type Type => typeof(TResource);
}

/// <summary>
/// Exposes the current state of the job.
/// </summary>
public enum JobState
{
    /// <summary>
    /// The job is currently waiting to be started.
    /// </summary>
    Waiting,
    
    /// <summary>
    /// The job is currently running.
    /// </summary>
    Running, 
        
    /// <summary>
    /// The job is paused.
    /// </summary>
    Paused,
        
    /// <summary>
    /// The job has finished processing.
    /// </summary>
    Finished
}