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


public record JobResultMessage(object Result, int Offset, bool IsFailure) : IJobMessage;
