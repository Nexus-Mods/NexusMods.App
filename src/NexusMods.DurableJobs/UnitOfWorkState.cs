using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

/// <summary>
/// State for a unit of work.
/// </summary>
public class UnitOfWorkState : AJobState
{
    /// <summary>
    /// The cancellation token source for the unit of work.
    /// </summary>
    public required CancellationTokenSource CancellationTokenSource { get; init; }
    
    /// <summary>
    /// The task that represents the unit of work that is executing
    /// </summary>
    public Task? RunningTask { get; set; }
    
}
