namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// A control flow exception that can be thrown when a job is paused because it is waiting for some sub-job to complete.
/// </summary>
public class WaitException : Exception
{
    
}
