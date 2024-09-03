namespace NexusMods.DurableJobs;

/// <summary>
/// Messages passed to the job actor.
/// </summary>
public interface IJobMessage 
{
    
}

/// <summary>
/// Unconditionally run the job.
/// </summary>
public record RunMessage : IJobMessage
{
    /// <summary>
    /// Instance of the <see cref="RunMessage"/> message.
    /// </summary>
    public static RunMessage Instance { get; } = new();
}


/// <summary>
/// Used by child jobs to notify the parent job that they have completed.
/// </summary>
public record JobResultMessage(object Result, int Offset, bool IsFailure) : IJobMessage;


/// <summary>
/// Used to tell a job to cancel itself, it should then forward the message to all of its children and parents
/// </summary>
public record CancelMessage : IJobMessage
{
    /// <summary>
    /// Instance of the <see cref="CancelMessage"/> message.
    /// </summary>
    public static CancelMessage Instance { get; } = new();
}

/// <summary>
/// Used to tell a UnitOfWork actor that it's either faulted or completed
/// </summary>
public record SelfFinished(object Result, bool IsFailure) : IJobMessage;
