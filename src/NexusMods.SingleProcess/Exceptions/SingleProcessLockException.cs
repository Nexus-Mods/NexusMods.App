namespace NexusMods.SingleProcess.Exceptions;


/// <summary>
/// Thrown when the single process lock cannot be acquired by the current process.
/// </summary>
public class SingleProcessLockException() : Exception("Failed to acquire the lock for the single process.")
{
    
}
