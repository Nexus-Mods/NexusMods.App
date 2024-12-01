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
    
    /// <summary>
    /// Runs the Garbage Collector in the specified mode.
    /// </summary>
    /// <param name="gcRunMode">The mode to run the GC in.</param>
    // ReSharper disable once InconsistentNaming
    void RunWithMode(GarbageCollectorRunMode gcRunMode)
    {
        switch (gcRunMode)
        {
            case GarbageCollectorRunMode.RunSynchronously:
                Run();
                break;
            case GarbageCollectorRunMode.RunAsyncInBackground:
                _ = Task.Run(Run);
                break;
            case GarbageCollectorRunMode.DoNotRun:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(gcRunMode), gcRunMode, null);
        }
    }
}
