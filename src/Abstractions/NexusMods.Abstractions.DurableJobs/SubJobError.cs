namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// An exception thrown when a job fails, the message will be the message of the exception that caused the job to fail.
/// </summary>
public class SubJobError(string message) : Exception(message)
{
    
}
