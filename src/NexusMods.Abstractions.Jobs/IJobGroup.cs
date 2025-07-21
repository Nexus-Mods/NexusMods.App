namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A group of jobs
/// </summary>
public interface IJobGroup : IReadOnlyCollection<IJob>
{
    public CancellationToken CancellationToken { get; }
    
    /// <summary>
    /// Cancels all jobs in this group
    /// </summary>
    void Cancel();
    
    /// <summary>
    /// Gets whether this job group has been cancelled
    /// </summary>
    bool IsCancelled { get; }
    
    /// <summary>
    /// Adds a job to this group
    /// </summary>
    void Attach(IJob job);
}
