namespace NexusMods.Abstractions.GC;

/// <summary>
/// Utility for running the garbage collection process.
/// </summary>
public interface IGarbageCollectorRunner
{
    /// <summary>
    /// Starts the garbage collector.
    /// </summary>
    void Run();
}
