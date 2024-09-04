using DynamicData.Kernel;
using R3;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// An untyped job interface, this is the reporting end of a job. The writable side is the <see cref="IJobContext{TJobType}"/>
/// </summary>
public interface IJob
{
    /// <summary>
    /// The unique identifier of the job
    /// </summary>
    public JobId Id { get; }
    
    /// <summary>
    /// The status of the job
    /// </summary>
    public JobStatus Status { get; }
    
    /// <summary>
    /// The observable status of the job
    /// </summary>
    public Observable<JobStatus> ObservableStatus { get; }
    
    /// <summary>
    /// If the job has determinate progress, the percentage of the job that has been completed
    /// </summary>
    public Optional<Percent> Progress { get; }
    
    /// <summary>
    /// If the job reports progress, the rate of progress in units per second
    /// </summary>
    public Optional<double> RateOfProgress { get; }
    
    /// <summary>
    /// The job group that the job belongs to, all jobs have a group, even if they are the only member
    /// </summary>
    public IJobGroup Group { get; }
    
    /// <summary>
    /// Wait for the job to complete or throw an exception if the job fails
    /// </summary>
    public Task WaitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A job that returns a given type
/// </summary>
public interface IJobWithResult<TResult> : IJob
{
    /// <summary>
    /// The result of the job
    /// </summary>
    public TResult Result { get; }
    
    /// <summary>
    /// Wait for the job to complete and return the result or throw an exception if the job fails
    /// </summary>
    public Task<TResult> WaitForResult(CancellationToken cancellationToken = default);
}
