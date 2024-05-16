namespace NexusMods.SingleProcess.Exceptions;

/// <summary>
/// Thrown when a client tries to connect to a main process that is not started
/// </summary>
public class NoMainProcessStarted() : Exception("No main process is started")
{
    
}
