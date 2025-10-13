namespace NexusMods.Sdk.Jobs;

/// <summary>
/// A cancellation token that supports both cancellation and pause/resume
/// functionality for jobs. This class wraps a standard <see cref="CancellationToken"/>
/// and adds pause/resume capabilities.
/// </summary>
/// <remarks>
/// <para>
/// Jobs are paused immediately by calling <see cref="Pause()"/>, which cancels the current token.
/// Jobs resume by restarting with a fresh token when <see cref="JobContext.Start()"/> is called.
/// This is usually called as a result of calling <see cref="IJobMonitor.Resume"/>
/// </para>
/// </remarks>
public class JobCancellationToken : IDisposable
{
    private CancellationTokenSource _currentTokenSource;
    private CancellationReason _cancellationReason = CancellationReason.None;

    /// <summary>
    /// Initializes a new instance of <see cref="JobCancellationToken"/>.
    /// </summary>
    public JobCancellationToken()
    {
        _currentTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets a value indicating whether the job is currently paused.
    /// </summary>
    /// <remarks>
    /// A paused job will resume execution when <see cref="Resume()"/> is called.
    /// </remarks>
    public bool IsPaused => _cancellationReason == CancellationReason.Paused;
    
    /// <summary>
    /// Gets a value indicating whether the job has been cancelled.
    /// </summary>
    public bool IsCancelled => _cancellationReason == CancellationReason.Cancelled;
    
    /// <summary>
    /// Gets the underlying <see cref="CancellationToken"/> that can be used with standard .NET APIs.
    /// This token may be recycled on resume if force pause is supported.
    /// </summary>
    public CancellationToken Token => _currentTokenSource.Token;
    
    /// <summary>
    /// Implicitly converts this <see cref="JobCancellationToken"/> to a <see cref="CancellationToken"/>
    /// for compatibility with existing APIs.
    /// </summary>
    /// <param name="jobToken">The job cancellation token to convert.</param>
    /// <returns>The underlying cancellation token.</returns>
    public static implicit operator CancellationToken(JobCancellationToken jobToken) 
        => jobToken.Token;
    
    /// <summary>
    /// Throws an <see cref="OperationCanceledException"/> if cancellation has been requested.
    /// </summary>
    /// <exception cref="OperationCanceledException">
    /// The token has been canceled.
    /// </exception>
    /// <remarks>
    /// This method provides the same functionality as <see cref="CancellationToken.ThrowIfCancellationRequested"/>
    /// for convenience when working with <see cref="JobCancellationToken"/>.
    /// </remarks>
    public void ThrowIfCancellationRequested() 
    {
        // Note(sewer): The source of truth in cancellation is the _cancellationReason field.
        // The inner cancellation token is only used for compatibility with 
        // external APIs which are unaware of features like pause.
        if (_cancellationReason != CancellationReason.None)
            throw new OperationCanceledException();
    }
    
    /// <summary>
    /// Cancels the job, preventing it from resuming if paused.
    /// This operation cannot be undone.
    /// </summary>
    /// <remarks>
    /// Once cancelled, the job cannot be resumed and will throw <see cref="OperationCanceledException"/>
    /// when <see cref="IJobContext.YieldAsync()"/> is called.
    /// </remarks>
    public void Cancel()
    {
        _cancellationReason = CancellationReason.Cancelled;
        _currentTokenSource.Cancel();
    }
    
    /// <summary>
    /// Requests the job to pause immediately by cancelling the current token.
    /// </summary>
    /// <remarks>
    /// This operation immediately cancels the current token, allowing instant pause response.
    /// A fresh token is prepared immediately for eventual resume.
    /// If the job is already cancelled, this operation has no effect.
    /// </remarks>
    public void Pause()
    {
        if (_cancellationReason == CancellationReason.Cancelled)
            return; // Cannot pause if cancelled
            
        _cancellationReason = CancellationReason.Paused;
        _currentTokenSource.Cancel(); // Always cancel immediately
        RecycleToken(); // Prepare fresh token immediately for eventual resume
    }
    
    /// <summary>
    /// Resumes a paused job by clearing the pause flag.
    /// </summary>
    /// <remarks>
    /// This operation clears the pause flag, allowing the job to be restarted.
    /// This method is for job framework internal use - external callers should use <see cref="IJobMonitor.Resume"/> instead.
    /// If the job is not paused or has been cancelled, this operation is ignored.
    /// </remarks>
    public void Resume()
    {
        if (_cancellationReason != CancellationReason.Paused)
            return;

        _cancellationReason = CancellationReason.None; // Just clear the flag - no waiting
        // Token recycling handled by Start() method
    }
    
    
    /// <summary>
    /// Creates a fresh cancellation token source, disposing the previous one.
    /// This is used internally to provide new tokens after force pause resume.
    /// </summary>
    private void RecycleToken()
    {
        _currentTokenSource?.Dispose();
        _currentTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Releases all resources used by this <see cref="JobCancellationToken"/>.
    /// </summary>
    /// <remarks>
    /// After disposal, this token should not be used. Any pending operations may throw exceptions.
    /// </remarks>
    public void Dispose() => _currentTokenSource?.Dispose();
}

/// <summary>
/// Represents the reason why a job's execution was interrupted.
/// </summary>
internal enum CancellationReason
{
    /// <summary>
    /// The job is running normally and has not been interrupted.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// The job has been cancelled and will not resume.
    /// </summary>
    Cancelled = 1,
    
    /// <summary>
    /// The job has been paused and can be resumed later.
    /// </summary>
    Paused = 2,
}
