using Nito.AsyncEx;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A cancellation token that supports both cancellation and pause/resume
/// functionality for jobs. This class wraps a standard <see cref="CancellationToken"/>
/// and adds pause/resume capabilities while maintaining compatibility with
/// existing cancellation-based APIs.
/// </summary>
/// <remarks>
/// <para>
/// Jobs can be paused cooperatively by calling <see cref="Pause()"/> and will
/// only pause when the job calls <see cref="IJobContext.YieldAsync()"/>.
/// This ensures jobs are paused at optimal points. Non-yielding jobs cannot be paused.
/// 
/// When force pause is enabled via <see cref="SupportsForcePause"/>, pause operations
/// will immediately cancel the current token, allowing interruption of nested async operations.
/// The token will be recycled on resume to provide fresh cancellation tokens. But this
/// requires opt-in support from the job itself.
/// 
/// The token maintains backwards compatibility through implicit conversion to <see cref="CancellationToken"/>,
/// allowing it to be used with existing APIs that expect standard cancellation tokens.
/// </para>
/// </remarks>
public class JobCancellationToken : IDisposable
{
    private CancellationTokenSource _currentTokenSource;
    private readonly AsyncManualResetEvent _pauseEvent = new(true); // Not paused initially
    private CancellationReason? _reason;
    private readonly bool _supportsForcePause;

    /// <summary>
    /// Initializes a new instance of <see cref="JobCancellationToken"/>.
    /// </summary>
    /// <param name="supportsForcePause">Whether this token supports force pause via token cancellation.</param>
    public JobCancellationToken(bool supportsForcePause = false)
    {
        _supportsForcePause = supportsForcePause;
        _currentTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets a value indicating whether the job is currently paused.
    /// </summary>
    /// <remarks>
    /// A paused job will resume execution when <see cref="Resume()"/> is called
    /// and the job next calls <see cref="IJobContext.YieldAsync()"/>.
    /// </remarks>
    public bool IsPaused => _reason == CancellationReason.Paused;
    
    /// <summary>
    /// Gets a value indicating whether the job has been cancelled.
    /// </summary>
    public bool IsCancelled => _reason == CancellationReason.Cancelled;
    
    /// <summary>
    /// Gets a value indicating whether this instance supports force pause.
    /// When true, pause operations will immediately pause by cancelling the current token.
    /// </summary>
    public bool SupportsForcePause => _supportsForcePause;
    
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
    /// Throws an <see cref="OperationCanceledException"/> if cancellation has been requested for this token.
    /// </summary>
    /// <exception cref="OperationCanceledException">
    /// The token has been canceled.
    /// </exception>
    /// <remarks>
    /// This method provides the same functionality as <see cref="CancellationToken.ThrowIfCancellationRequested"/>
    /// for convenience when working with <see cref="JobCancellationToken"/>.
    /// </remarks>
    public void ThrowIfCancellationRequested() => Token.ThrowIfCancellationRequested();
    
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
        _reason = CancellationReason.Cancelled;
        _currentTokenSource.Cancel();
    }
    
    /// <summary>
    /// Requests the job to pause at the next yield point.
    /// The job will only pause when it calls <see cref="IJobContext.YieldAsync()"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a cooperative operation - the job must call <see cref="IJobContext.YieldAsync()"/>
    /// for the pause to take effect. Jobs that don't yield cannot be paused.
    /// </para>
    /// <para>
    /// If <see cref="SupportsForcePause"/> is true, this will also immediately cancel the current token,
    /// allowing interruption of nested async operations. The job should handle the resulting
    /// <see cref="OperationCanceledException"/> and call <see cref="IJobContext.HandlePauseExceptionAsync"/>
    /// to distinguish between pause and true cancellation.
    /// </para>
    /// <para>
    /// If the job is already cancelled, this operation has no effect.
    /// </para>
    /// </remarks>
    public void Pause()
    {
        if (_reason == CancellationReason.Cancelled)
            return; // Cannot pause if cancelled
            
        _reason = CancellationReason.Paused;
        _pauseEvent.Reset();
        
        // Force pause: cancel current token immediately
        if (_supportsForcePause)
            _currentTokenSource.Cancel();
    }
    
    /// <summary>
    /// Resumes a paused job, allowing it to continue execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This operation only has effect if the job is currently paused (<see cref="IsPaused"/> is <c>true</c>).
    /// If the job is not paused or has been cancelled, this operation is ignored.
    /// </para>
    /// <para>
    /// If <see cref="SupportsForcePause"/> is true, this will create a fresh token for resumed execution,
    /// allowing the job to continue with a new, non-cancelled token.
    /// </para>
    /// <para>
    /// After resuming, the job will continue from where it was paused when it next calls <see cref="IJobContext.YieldAsync()"/>.
    /// </para>
    /// </remarks>
    public void Resume()
    {
        if (_reason != CancellationReason.Paused)
            return;

        _reason = null;
        _pauseEvent.Set();
    }
    
    /// <summary>
    /// Determines whether the given <see cref="OperationCanceledException"/> was caused by a force pause
    /// rather than true cancellation.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if this was a pause-induced cancellation; false otherwise.</returns>
    /// <remarks>
    /// This method is useful for jobs that support force pause to distinguish between
    /// pause and true cancellation in exception handling blocks.
    /// </remarks>
    public bool IsPausingCancellation(OperationCanceledException ex)
    {
        return _supportsForcePause && 
               _reason == CancellationReason.Paused && 
               ex.CancellationToken == _currentTokenSource.Token;
    }
    
    /// <summary>
    /// Creates a fresh cancellation token source, disposing the previous one.
    /// This is used internally to provide new tokens after force pause resume.
    /// </summary>
    public void RecycleToken()
    {
        _currentTokenSource?.Dispose();
        _currentTokenSource = new CancellationTokenSource();
    }
    
    /// <summary>
    /// Waits asynchronously for the job to be resumed if it's currently paused.
    /// </summary>
    /// <returns>A task that completes when the job is resumed or cancelled.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the job is cancelled while waiting for resume.
    /// The actual exception type thrown is <see cref="TaskCanceledException"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the job is not paused, this method returns immediately.
    /// If the job is cancelled while waiting, a <see cref="TaskCanceledException"/> (which inherits from <see cref="OperationCanceledException"/>) is thrown.
    /// </para>
    /// <para>
    /// This method is typically called by the job framework when <see cref="IJobContext.YieldAsync()"/> detects a pause.
    /// </para>
    /// </remarks>
    // ReSharper disable once MethodSupportsCancellation (no token here, as it's recycled for force pause)
    public async Task WaitForResumeAsync() => await _pauseEvent.WaitAsync();

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
    /// The job has been cancelled and will not resume.
    /// </summary>
    Cancelled = 0,
    
    /// <summary>
    /// The job has been paused and can be resumed later.
    /// </summary>
    Paused = 1,
}
