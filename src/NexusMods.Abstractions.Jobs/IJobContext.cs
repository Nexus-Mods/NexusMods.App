using System.Numerics;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A job context, this is what jobs use internally to communicate with the job monitor
/// </summary>
public interface IJobContext : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Call this whenever the job needs to check if the execution should cancel or pause
    /// </summary>
    Task YieldAsync();

    /// <summary>
    /// Job cancellation token that supports pause/resume functionality
    /// </summary>
    JobCancellationToken JobCancellationToken { get; }
    
    /// <summary>
    /// Prefer to use <see cref="YieldAsync"/> instead of this but if you need a token to pass
    /// to a method that doesn't support our pause and cancel mechanism then use this
    /// </summary>
    CancellationToken CancellationToken => JobCancellationToken.Token;
    
    /// <summary>
    /// Get the connected job monitor
    /// </summary>
    IJobMonitor Monitor { get; }
    
    /// <summary>
    /// All the jobs that are currently in the group
    /// </summary>
    IJobGroup Group { get; }
    
    /// <summary>
    /// The job definition passed to the job when it was created
    /// </summary>
    IJobDefinition Definition { get; }

    /// <summary>
    /// Set the progress of the job as either a percentage or a rate of progress or both
    /// </summary>
    void SetPercent<TVal>(TVal current, TVal max)
        where TVal : IDivisionOperators<TVal, TVal, double>;

    /// <summary>
    /// Set the progress of the job as a rate of units per second
    /// </summary>
    void SetRateOfProgress(double rate);

    /// <summary>
    /// Explicitly cancel this job with a message. This will cancel the job and throw an <see cref="OperationCanceledException"/>.
    /// </summary>
    /// <param name="message">The cancellation message</param>
    /// <returns>This method never returns - it always throws</returns>
    void CancelAndThrow(string message);
    
    /// <summary>
    /// Helper for force pause exception handling.
    /// Determines if the given exception was caused by a force pause and handles the pause flow appropriately.
    /// </summary>
    /// <param name="ex">The <see cref="OperationCanceledException"/> to check.</param>
    /// <returns>
    /// A task that completes when the job is resumed (if it was a pause)
    /// or immediately (if not a pause).
    /// </returns>
    /// <exception cref="OperationCanceledException">Re-thrown if this was a true cancellation, not a pause.</exception>
    /// <remarks>
    /// This method should be called in catch blocks when handling <see cref="OperationCanceledException"/> in jobs that support force pause.
    /// If the exception was caused by a force pause, this method will wait for resume and return normally.
    /// If the exception was caused by true cancellation, it will re-throw the exception.
    /// </remarks>
    Task HandlePauseExceptionAsync(OperationCanceledException ex);
}

/// <summary>
/// A typed job context
/// </summary>
/// <typeparam name="TJobDefinition">The type of definition that the job uses</typeparam>
public interface IJobContext<out TJobDefinition> : IJobContext 
    where TJobDefinition: IJobDefinition
{
    /// <summary>
    /// The job definition passed to the job monitor when the job was created
    /// </summary>
    public new TJobDefinition Definition { get; }
}
