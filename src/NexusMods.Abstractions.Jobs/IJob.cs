using DynamicData.Kernel;
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
    public IObservable<JobStatus> ObservableStatus { get; }
    
    /// <summary>
    /// If the job has determinate progress, the percentage of the job that has been completed
    /// </summary>
    public Optional<Percent> Progress { get; }
    
    /// <summary>
    /// The observable progress of the job
    /// </summary>
    public IObservable<Optional<Percent>> ObservableProgress { get; }
    
    /// <summary>
    /// If the job reports progress, the rate of progress in units per second
    /// </summary>
    public Optional<double> RateOfProgress { get; }
    
    /// <summary>
    /// The observable rate of progress of the job
    /// </summary>
    public IObservable<Optional<double>> ObservableRateOfProgress { get; }
    
    /// <summary>
    /// The job group that the job belongs to, all jobs have a group, even if they are the only member
    /// </summary>
    public IJobGroup Group { get; }
    
    /// <summary>
    /// Wait for the job to complete or throw an exception if the job fails
    /// </summary>
    public Task WaitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the definition of the job
    /// </summary>
    public IJobDefinition Definition { get; }
    
    /// <summary>
    /// Gets whether this job can be cancelled
    /// </summary>
    bool CanBeCancelled { get; }
    
    /// <summary>
    /// Gets whether this job can be paused
    /// </summary>
    bool CanBePaused { get; }
    
    /// <summary>
    /// Get the job context for performing control operations like Resume, Pause, Cancel.
    /// IJob is the reader side, IJobContext is the writer side.
    /// </summary>
    internal IJobContext AsContext();
    
    /// <summary>
    /// Get the current job state data. Returns null if no state of this type exists.
    /// </summary>
    TState? GetJobStateData<TState>() where TState : class, IPublicJobStateData => Definition.GetJobStateData() as TState;
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
